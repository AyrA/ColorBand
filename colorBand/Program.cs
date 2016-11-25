using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Drawing;
using System.Diagnostics;
using System.IO.Compression;
using colorBand.Properties;
using AyrA.IO;

namespace colorBand
{
    public struct StatusLine
    {
        public int frame;
        public double fps, speed;
        public TimeSpan Time;

        public StatusLine(string Line)
        {
            bool skip = false;
            string Trimmed = string.Empty;

            frame = 0;
            fps = speed = 0.0;
            Time = new TimeSpan(0L);

            if (Line.StartsWith("frame="))
            {
                foreach (char c in Line.Trim())
                {
                    if (skip)
                    {
                        if (c != ' ')
                        {
                            Trimmed += c;
                            skip = false;
                        }
                    }
                    else
                    {
                        if (c == '=')
                        {
                            skip = true;
                        }
                        Trimmed += c;
                    }
                }
                foreach (string Part in Trimmed.Split(' '))
                {
                    if (Part.Contains("="))
                    {
                        switch (Part.Split('=')[0])
                        {
                            case "time":
                                string[] Segments = Part.Split('=')[1].Split(':');

                                if (Segments.Length == 3)
                                {
                                    Time = new TimeSpan(
                                        TryInt(Segments[0], 0),
                                        TryInt(Segments[1], 0),
                                        TryInt(Segments[2].Split('.')[0], 0));
                                }

                                break;
                            case "frame":
                                int.TryParse(Part.Split('=')[1], out frame);
                                break;
                            case "fps":
                                double.TryParse(Part.Split('=')[1], out fps);
                                break;
                            case "speed":
                                double.TryParse(Part.Split('=')[1].Trim('x'), out speed);
                                break;
                        }
                    }
                }
            }
        }

        private int TryInt(string s, int Default)
        {
            int i = 0;
            return int.TryParse(s, out i) ? i : Default;
        }
    }

    public class Program
    {
        /// <summary>
        /// Command line for FFMPEG.
        /// Explanation: takes video input and renders it with 1 fps and downscaled to a single pixel to an array of png files
        /// </summary>
        const string CMDLINE = "-i \"{0}\" -lavfi fps=1,scale=1:{2}:flags=lanczos \"{1}\\%06d.png\"";

        /// <summary>
        /// Main entry of Application
        /// </summary>
        /// <param name="args">Command line arguments</param>
        static void Main(string[] args)
        {
            Color[] Colors;

            string TempDir = GetTempDir("frameband");
            string FFmpegFile = Path.Combine(TempDir, "ffmpeg.exe");
#if DEBUG
            //in debug mode I use hardcoded information because lazy.
            //The source is a Youtube video with the ID 2g6uPk-A1HY
            string VideoFile = @"C:\Temp\media\Sim City SNES TAS (Tool Assisted Speedrun) - last input 06_52.09 _ 600k people 47_00-2g6uPk-A1HY.mp4";
            string OutputFile = @"C:\temp\band.png";
            bool SingleColor = false;
            int Height = 240;
#else
            string VideoFile = Ask("Source video file").Trim('"');
            string OutputFile = Ask("Destination image file").Trim('"');
            int Height = 0;
            while (!int.TryParse(Ask("Image Height"), out Height) || Height < 1) ;
            bool SingleColor = Ask("Use single color [y/n]").ToLower() == "y";
#endif
            Console.Write("Extracting FFmpeg...");
            WriteEncoder(TempDir);
            Console.WriteLine("[DONE]");

            DateTime Start = DateTime.Now;

            //Generate single frames into PNG
            //Note: check if input is a video with at least 1 frame at all.
            CreateImages(VideoFile, TempDir, FFmpegFile, SingleColor ? 1 : Height);

            DateTime VideoGenerated = DateTime.Now;

            if (SingleColor)
            {
                //Extract color information from the PNG images.
                //GetFiles is rather slow, especially for a large number of files,
                //but it is certainly easier to use than manually using the Windows API
                Colors = ExtractColorFromPixel(Directory.GetFiles(TempDir, "*.png"));


                //It is possible, that no colors were extracted,
                //for example if the input is an audio file.
                if (Colors.Length > 0)
                {
                    RenderBand(Colors, OutputFile, Height);
                }
                else
                {
                    //Probably invalid video file
                    Console.WriteLine("No frames were extracted");
                }
            }
            else
            {
                JoinImages(Directory.GetFiles(TempDir, "*.png"), OutputFile);
            }

            //Remove temporary PNG files.
            //Ideally this would be in a try/catch block at the end,
            //that loops until it succeeds.
            Directory.Delete(TempDir, true);

            //Stats for nerds
            TimeSpan tsTotal = DateTime.Now.Subtract(Start);
            TimeSpan tsVideo = VideoGenerated.Subtract(Start);
            TimeSpan tsImage = DateTime.Now.Subtract(VideoGenerated);

            Console.WriteLine(@"Done. Durations:
Total: {0:00}:{1:00}:{2:00}
Video: {3:00}:{4:00}:{5:00}
Image: {6:00}:{7:00}:{8:00}",
                tsTotal.Hours, tsTotal.Minutes, tsTotal.Seconds,
                tsVideo.Hours, tsVideo.Minutes, tsVideo.Seconds,
                tsImage.Hours, tsImage.Minutes, tsImage.Seconds);
#if DEBUG
            Console.WriteLine("#END");
            //Flush key buffer
            while (Console.KeyAvailable)
            {
                Console.ReadKey();
            }
            Console.ReadKey();
#endif
        }

        private static void WriteEncoder(string Dir)
        {
            using(MemoryStream MS=new MemoryStream(Resources.blob,false))
            {
                Compressor.Decompress(Dir, MS);
            }
        }

        /// <summary>
        /// Renders colors into a frame band
        /// </summary>
        /// <param name="Colors">List of colors</param>
        /// <param name="DestinationFile">Destination image file name</param>
        /// <param name="Height">Height of band</param>
        private static void RenderBand(Color[] Colors, string DestinationFile, int Height)
        {
            using (Bitmap Output = new Bitmap(Colors.Length, Height))
            {
                //if height is 1, then use SetPixel
                if (Height == 1)
                {
                    for (int i = 0; i < Colors.Length; i++)
                    {
                        Output.SetPixel(i, 0, Colors[i]);
                        //This actually slows down the application quite a lot.
                        Console.Write('-');
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
                                //This actually slows down the application quite a lot.
                                Console.Write('-');
                            }
                        }
                    }
                }
                //This detects the image format from the extension
                //See System.Drawing.Imaging.ImageFormat enumeration for supported types.
                Output.Save(DestinationFile);
                Console.WriteLine();
            }
        }

        /// <summary>
        /// Create an array of frame images from a video file
        /// </summary>
        /// <param name="Video">Video file</param>
        /// <param name="ImagePath">Path to hold frame band images</param>
        /// <param name="FFmpeg">ffmpeg executable path</param>
        /// <param name="Height">Height of generated frame bands.</param>
        private static void CreateImages(string Video, string ImagePath, string FFmpeg, int Height)
        {
            Console.WriteLine("Press [q] to abort processing and use existing frames");
            using (Process P = Process.Start(new ProcessStartInfo(FFmpeg, string.Format(CMDLINE, Video, ImagePath, Height))
                {
                    CreateNoWindow = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    UseShellExecute = false
                }))
            {
                using (StreamReader SR = P.StandardError)
                {
                    StatusLine SL;
                    int Y = Console.CursorTop;
                    while (!SR.EndOfStream)
                    {
                        if (Console.KeyAvailable && Console.ReadKey().Key == ConsoleKey.Q)
                        {
                            P.StandardInput.WriteLine("q");
                        }
                        SL = new StatusLine(SR.ReadLine());
                        if (SL.frame > 0)
                        {
                            Console.SetCursorPosition(0, Y);
                            Console.WriteLine(@"Time  : {0}
FPS   : {1}
Speed : {2}
Frames: {3}",SL.Time,SL.fps,SL.speed,SL.frame);
                        }
                    }
                }
                //Instead of just waiting, spawn the Process hidden and show the progress of stderr.
                //I have some code for this already, but was too lazy to go and copy it.
                P.WaitForExit();
            }
        }

        /// <summary>
        /// Extracts Color information from the top left pixel
        /// </summary>
        /// <param name="Files">List of files</param>
        /// <returns>List of pixel colors</returns>
        private static Color[] ExtractColorFromPixel(string[] Files)
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
                    Console.Write('X');
                    continue;
                }
                using (I)
                {
                    //Extract image color
                    Colors.Add(I.GetPixel(0, 0));
                    //This actually slows down the application quite a lot.
                    Console.Write('.');
                }
            }
            Console.WriteLine();
            return Colors.ToArray();
        }

        /// <summary>
        /// Joins an array of images horizontally
        /// </summary>
        /// <param name="Files">List of images</param>
        /// <param name="OutputFile">Destination File</param>
        private static void JoinImages(string[] Files, string OutputFile)
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
                    foreach(string Name in Files)
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
        /// Prints a line of text and asks for input
        /// </summary>
        /// <param name="p">Text to show. Will append colon and space</param>
        /// <returns>Line input</returns>
        private static string Ask(string p)
        {
            Console.Write("{0}: ", p);
            return Console.ReadLine();
        }

        /// <summary>
        /// Attempts to create a temporary folder
        /// </summary>
        /// <param name="Dirname">Name of the folder to create</param>
        /// <returns>Full path of folder created</returns>
        static string GetTempDir(string Dirname)
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
    }
}
