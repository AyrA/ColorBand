using System.IO;
using System.Drawing;
using AyrA.IO;
using System;
using System.Collections.Specialized;
using System.Diagnostics;
using colorBand.Properties;
using System.Collections.Generic;
using System.Threading;

namespace colorBand
{
    public delegate void ThreadExitHandler(Thread T);

    public static class Tools
    {
        private const long TICKFACTOR = 10000000L;
        private const string DURATION = "-show_entries format -v quiet \"{0}\"";
        private const string RESOLUTION = "-show_entries stream=width,height -select_streams v:0 -v quiet \"{0}\"";
        private const string FFMPEG = "-i \"{0}\" -lavfi fps=1,scale=1:{2}:flags=lanczos \"{1}\\%06d.png\"";

        private static string CurrentEncoder = "ffmpeg.exe";
        private static string CurrentInfoTool = "ffplay.exe";

        public static bool LogToConsole = false;

        public static event ThreadExitHandler ThreadExit;// = delegate { };

        /// <summary>
        /// Attempts to create a temporary folder
        /// </summary>
        /// <param name="Dirname">Name of the folder to create</param>
        /// <returns>Full path of folder created</returns>
        public static string GetTempDir(string Dirname)
        {
            int i = 0;
            string TempRoot = Path.Combine(Path.GetTempPath(), Dirname + "_");
            while (true)
            {
                string current = string.Format("{0}{1}", TempRoot, i++);
                try
                {
                    Directory.CreateDirectory(current);
                    return current;
                }
                catch
                {
                    //you can increase i here instead if you want.
                }
            }
        }

        /// <summary>
        /// Renders colors into a frame band
        /// </summary>
        /// <param name="Colors">List of colors</param>
        /// <param name="DestinationFile">Destination image file name</param>
        /// <param name="Height">Height of band</param>
        public static void RenderBand(Color[] Colors, string DestinationFile, int Height)
        {
            using (Bitmap Output = new Bitmap(Colors.Length, Height))
            {
                //if height is 1, then use SetPixel
                if (Height == 1)
                {
                    for (int i = 0; i < Colors.Length; i++)
                    {
                        Output.SetPixel(i, 0, Colors[i]);
                        if (LogToConsole)
                        {
                            //This actually slows down the application quite a lot.
                            Console.Error.Write('-');
                        }
                    }
                }
                else
                {
                    using (Graphics G = Graphics.FromImage(Output))
                    {
                        //Note: Setting G.InterpolationMode to a "cheap" value has no effect,
                        //as it is only used for scaling.
                        //Creating a 1 pixel high image and then scale the height up does not increases the speed.
                        for (int i = 0; i < Colors.Length; i++)
                        {
                            //Why is this an IDisposable?
                            using (Pen P = new Pen(Colors[i]))
                            {
                                //Drawing a line is by far faster than setting individual pixels.
                                G.DrawLine(P, new Point(i, 0), new Point(i, Height - 1));
                                if (LogToConsole)
                                {
                                    //This actually slows down the application quite a lot.
                                    Console.Error.Write('-');
                                }
                            }
                        }
                    }
                }
                //This detects the image format from the extension
                //See System.Drawing.Imaging.ImageFormat enumeration for supported types.
                Output.Save(DestinationFile);
                if (LogToConsole)
                {
                    Console.Error.WriteLine();
                }
            }
        }

        /// <summary>
        /// Joins an array of images horizontally
        /// </summary>
        /// <param name="Files">List of images</param>
        /// <param name="OutputFile">Destination File</param>
        public static void JoinImages(string[] Files, string OutputFile)
        {
            int x = 0;
            int Height = 0;
            using (Image I = Image.FromFile(Files[0]))
            {
                Height = I.Height;
            }
            using (Bitmap B = new Bitmap(Files.Length, Height))
            {
                using (Graphics G = Graphics.FromImage(B))
                {
                    foreach (string Name in Files)
                    {
                        using (Image I = Image.FromFile(Name))
                        {
                            G.DrawImageUnscaled(I, new Point(x++, 0));
                        }
                        Console.Write('-');
                    }
                }
                B.Save(OutputFile);
                Console.WriteLine();
            }
        }

        /// <summary>
        /// Extracts Color information from the top left pixel
        /// </summary>
        /// <param name="Files">List of files</param>
        /// <returns>List of pixel colors</returns>
        public static Color[] ExtractColorFromPixel(string[] Files)
        {
            List<Color> Colors = new List<Color>();
            foreach (string s in Files)
            {
                Bitmap I = null;
                try
                {
                    //Ironically Bitmap.FromFile comes from the Image class and will return an Image object,
                    //but it is fully compatible with the Bitmap type so we just cast.
                    I = (Bitmap)Bitmap.FromFile(s);
                }
                catch
                {
                    //I never had this happen but better add an error resolver.
                    //In this case, we just ignore that frame.
                    if (LogToConsole)
                    {
                        Console.Error.Write('X');
                    }
                    continue;
                }
                using (I)
                {
                    //Extract image color
                    Colors.Add(I.GetPixel(0, 0));
                    if (LogToConsole)
                    {
                        //This actually slows down the application quite a lot.
                        Console.Error.Write('.');
                    }
                }
            }
            if (LogToConsole)
            {
                Console.Error.WriteLine();
            }
            return Colors.ToArray();
        }

        /// <summary>
        /// Extract FFmpeg
        /// </summary>
        /// <param name="Dir">Destination Directory</param>
        public static void WriteEncoder(string Dir)
        {
            using (MemoryStream MS = new MemoryStream(Resources.blob, false))
            {
                Compressor.Decompress(Dir, MS, LogToConsole);
                CurrentEncoder = Path.Combine(Dir, "ffmpeg.exe");
                CurrentInfoTool = Path.Combine(Dir, "ffprobe.exe");
            }
        }

        /// <summary>
        /// Extracts some basic information from the supplied Video file
        /// </summary>
        /// <param name="FileName">Video file</param>
        /// <returns>Video information</returns>
        public static VideoInfo GetVideoInfo(string FileName)
        {
            VideoInfo VI = new VideoInfo()
            {
                CodecName = string.Empty,
                VideoFile = FileName,
                Resolution = new Size(0, 0),
                Duration = new TimeSpan(0)
            };

            double TempDuration = 0.0;
            long TempValue = 0;

            //Basic video info
            using (Process P = GetInfoProcess(DURATION, FileName))
            {
                var Lines = GetLines(P);
                foreach (string Line in Lines)
                {
                    if (Line.Contains("="))
                    {
                        var Name = Line.Substring(0, Line.IndexOf('=')).ToLower();
                        var Value = Line.Substring(Line.IndexOf('=') + 1);
                        switch (Name)
                        {
                            case "format_long_name":
                                VI.CodecName = Value;
                                break;
                            case "duration":
                                if (double.TryParse(Value, out TempDuration))
                                {
                                    VI.Duration = new TimeSpan(0, 0, (int)TempDuration);
                                }
                                break;
                            case "bit_rate":
                                if (long.TryParse(Value, out TempValue))
                                {
                                    VI.Bitrate = TempValue;
                                }
                                break;
                        }
                    }
                }
            }

            //Video dimensions

            using (Process P = GetInfoProcess(RESOLUTION, FileName))
            {
                int TempResolution = 0;
                var Lines = GetLines(P);
                foreach (string Line in Lines)
                {
                    if (Line.Contains("="))
                    {
                        var Name = Line.Substring(0, Line.IndexOf('=')).ToLower();
                        var Value = Line.Substring(Line.IndexOf('=') + 1);
                        switch (Name)
                        {
                            case "width":
                                if (int.TryParse(Value, out TempResolution))
                                {
                                    VI.Resolution.Width = TempResolution;
                                }
                                break;
                            case "height":
                                if (int.TryParse(Value, out TempResolution))
                                {
                                    VI.Resolution.Height = TempResolution;
                                }
                                break;
                        }
                    }
                }
            }
            return VI;
        }

        /// <summary>
        /// Calls an event when the thread exits
        /// </summary>
        /// <param name="T">Thread to watch for</param>
        public static void WatchThread(Thread T)
        {
            Thread Temp = new Thread(delegate()
            {
                T.Join();
                if (LogToConsole)
                {
                    Console.Error.WriteLine("Thread {0} exited", T.Name);
                }
                ThreadExit(T);

            }) { IsBackground = true, Name = "Watcher of " + T.Name };
            Temp.Start();
        }

        /// <summary>
        /// Gets a process object for ffprobe.exe
        /// </summary>
        /// <param name="ArgType">Type of info</param>
        /// <param name="FileName">File name to scan</param>
        /// <returns>Process object (not yet started)</returns>
        private static Process GetInfoProcess(string ArgType, string FileName)
        {
            return new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = CurrentInfoTool,
                    Arguments = string.Format(ArgType, FileName),
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                }
            };
        }

        private static string[] GetLines(Process P)
        {
            List<string> Lines = new List<string>();
            P.Start();
            P.WaitForExit();
            while (!P.StandardOutput.EndOfStream)
            {
                Lines.Add(P.StandardOutput.ReadLine().Trim());
            }
            return Lines.ToArray();
        }

        /// <summary>
        /// Begins conversion using FFmpeg
        /// </summary>
        /// <param name="VideoFile">Source Video File</param>
        /// <param name="ImagePath">Path to put extracted Frames</param>
        /// <param name="VideoHeight">Height of video File</param>
        /// <returns>FFmpeg process (already started)</returns>
        public static Process BeginConversion(string VideoFile, string TempPath, int VideoHeight)
        {
            Process P = new Process()
            {
                StartInfo = new ProcessStartInfo(CurrentEncoder, string.Format(FFMPEG, VideoFile, TempPath, VideoHeight))
                    {
                        CreateNoWindow = true,
                        RedirectStandardError = true,
                        RedirectStandardInput = true,
                        WindowStyle = ProcessWindowStyle.Hidden,
                        UseShellExecute = false
                    },
                EnableRaisingEvents = true
            };
            P.Start();
            return P;
        }

        /// <summary>
        /// Checks if a number is a valid integer
        /// </summary>
        /// <param name="s">Number as string</param>
        /// <returns>True if integer (int.Parse won't crash)</returns>
        public static bool IsInt(string s)
        {
            int i = 0;
            return int.TryParse(s, out i);
        }

        /// <summary>
        /// Swaps the extension of a file name with a new one
        /// </summary>
        /// <param name="FileName">File name (path optional)</param>
        /// <param name="NewExt">New extension</param>
        /// <returns>New File name</returns>
        public static string SwapExt(string FileName, string NewExt)
        {
            string[] Segments = FileName.Split(Path.DirectorySeparatorChar);
            int Last = Segments.Length - 1;

            //if the name contains at least one dot, replace the part after the last occurence
            if (Segments[Last].Contains("."))
            {
                Segments[Last] = Segments[Last].Substring(0, Segments[Last].LastIndexOf('.') + 1) + NewExt;
            }
            else
            {
                //No extension present. Just append the extension
                Segments[Last] += "." + NewExt;
            }
            return string.Join(Path.DirectorySeparatorChar.ToString(), Segments);
        }

        /// <summary>
        /// Cuts the millisecond part from a TimeSpan
        /// </summary>
        /// <param name="Source">TimeSpan</param>
        /// <returns>TimeSpan with MilliSeconds=0</returns>
        public static TimeSpan CutToSeconds(TimeSpan Source)
        {
            return new TimeSpan(0, 0, (int)(Source.Ticks/TICKFACTOR));
        }
    }

    public struct VideoInfo
    {
        public string VideoFile, CodecName;
        public long Bitrate;
        public Size Resolution;
        public TimeSpan Duration;
    }
}
