using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Drawing;
using System.Diagnostics;
using System.IO.Compression;
using colorBand.Properties;
using AyrA.IO;
using System.Windows.Forms;

namespace colorBand
{
    public class Program
    {
        /// <summary>
        /// Main entry of Application
        /// </summary>
        /// <param name="args">Command line arguments</param>
        [STAThread]
        static void Main(string[] args)
        {
#if DEBUG
            args = new string[]
            {
                @"C:\Temp\media\Sim City SNES TAS (Tool Assisted Speedrun) - last input 06_52.09 _ 600k people 47_00-2g6uPk-A1HY.mp4",
                @"C:\Temp\Band.png"
            };
#endif
            //Show Help and exit if requested
            if (HasHelp(args))
            {
                PrintHelp();
                return;
            }
            //Show UI if no Command line arguments given
            if (args.Length == 0)
            {
                ShowUI();
                return;
            }

            //Parse Arguments
            Arguments A=ParseArgs(args);
            if (!A.Valid)
            {
                return;
            }

            Color[] Colors;

            string TempDir = Tools.GetTempDir("frameband");
            string FFmpegFile = Path.Combine(TempDir, "ffmpeg.exe");

            Console.Error.Write("Extracting FFmpeg...");
            Tools.WriteEncoder(TempDir);
            Console.Error.WriteLine("[DONE]");

            DateTime Start = DateTime.Now;

            //Generate single frames into PNG
            //Note: check if input is a video with at least 1 frame at all.
            CreateImages(A.InputFile, TempDir, FFmpegFile, A.SingleColor ? 1 : A.Height);

            DateTime VideoGenerated = DateTime.Now;

            if (A.SingleColor)
            {
                //Extract color information from the PNG images.
                //GetFiles is rather slow, especially for a large number of files,
                //but it is certainly easier to use than manually using the Windows API
                Colors = Tools.ExtractColorFromPixel(Directory.GetFiles(TempDir, "*.png"));


                //It is possible, that no colors were extracted,
                //for example if the input is an audio file.
                if (Colors.Length > 0)
                {
                    Tools.RenderBand(Colors, A.OutputFile, A.Height);
                }
                else
                {
                    //Probably invalid video file
                    Console.Error.WriteLine("No frames were extracted");
                }
            }
            else
            {
                Tools.JoinImages(Directory.GetFiles(TempDir, "*.png"), A.OutputFile);
            }

            //Remove temporary PNG files.
            //Ideally this would be in a try/catch block at the end,
            //that loops until it succeeds.
            Directory.Delete(TempDir, true);

            //Stats for nerds
            TimeSpan tsTotal = DateTime.Now.Subtract(Start);
            TimeSpan tsVideo = VideoGenerated.Subtract(Start);
            TimeSpan tsImage = DateTime.Now.Subtract(VideoGenerated);

            Console.Error.WriteLine(@"Done. Durations:
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

        /// <summary>
        /// Create an array of frame images from a video file
        /// </summary>
        /// <param name="Video">Video file</param>
        /// <param name="ImagePath">Path to hold frame band images</param>
        /// <param name="FFmpeg">ffmpeg executable path</param>
        /// <param name="Height">Height of generated frame bands.</param>
        private static void CreateImages(string Video, string ImagePath, string FFmpeg, int Height)
        {
            VideoInfo VI = Tools.GetVideoInfo(Video);
            if (Height < 1)
            {
                Height = VI.Resolution.Height;
            }
            DateTime Start = DateTime.Now;

            Console.Error.WriteLine("Press [q] to abort processing and use existing frames");
            using (Process P = Tools.BeginConversion(Video, ImagePath, Height))
            {
                using (StreamReader SR = P.StandardError)
                {
                    StatusLine SL;
                    int Y = Console.CursorTop;
                    while (!SR.EndOfStream)
                    {
                        if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Q)
                        {
                            P.StandardInput.WriteLine("q");
                        }
                        SL = new StatusLine(SR.ReadLine());
                        TimeSpan Estimate = new TimeSpan(0, 0, (int)(VI.Duration.TotalSeconds / SL.Speed));
                        TimeSpan CurrentTime = Tools.CutToSeconds(DateTime.Now.Subtract(Start));

                        if (SL.Frame > 0)
                        {
                            Console.SetCursorPosition(0, Y);
                            Console.Error.WriteLine(@"Time    : {0}
Speed   : {1,-6}
Frames  : {2,-6}

Estimate: {3}
Current : {4}", SL.Time, SL.Speed, SL.Frame, Estimate, CurrentTime);
                        }
                    }
                }
                //Instead of just waiting, spawn the Process hidden and show the progress of stderr.
                //I have some code for this already, but was too lazy to go and copy it.
                P.WaitForExit();
            }
        }

        /// <summary>
        /// Scans arguments for common help requests
        /// </summary>
        /// <param name="Args">Arguments</param>
        /// <returns>True, if help requested</returns>
        private static bool HasHelp(string[] Args)
        {
            foreach (string Arg in Args)
            {
                if (Arg.ToLower() == "--help" ||
                    Arg.ToLower() == "/h" ||
                    Arg == "/?" ||
                    Arg == "-?")
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Print Help message
        /// </summary>
        private static void PrintHelp()
        {
            Console.Error.WriteLine(@"ColorBand.exe <InputFile> [OutputFile] [Height] [/s]

InputFile   - Soure video file to extract frames
OutputFile  - Destination file to write color band. If not specified,
              The band is saved in the InputFile directory
Height      - Height of color band.
              If not specified, it uses the video height.
/s          - If specified, reduces each frame to a single pixel instead of
              a line. Not faster. Just looks differently");
        }

        /// <summary>
        /// Parses command line argument into formatted structure
        /// </summary>
        /// <param name="Args">Raw arguments</param>
        /// <returns>Command line arguments structure</returns>
        private static Arguments ParseArgs(string[] Args)
        {
            Arguments A = new Arguments()
            {
                InputFile = null,
                OutputFile = null,
                SingleColor = false,
                Height = 0,
                Valid = true
            };
            bool PNGSet = false;

            foreach (string Arg in Args)
            {
                switch (Arg.ToLower())
                {
                    case "/s":
                        A.SingleColor = true;
                        break;
                    default:
                        //if integer, assume it is the height
                        if (Tools.IsInt(Arg))
                        {
                            if (int.Parse(Arg) > 0)
                            {
                                if (A.Height == 0)
                                {
                                    A.Height = int.Parse(Arg);
                                }
                                else
                                {
                                    Console.Error.WriteLine("Height has been specified twice");
                                    A.Valid = false;
                                    return A;
                                }
                            }
                            else
                            {
                                Console.Error.WriteLine("Height must be bigger than 0");
                                A.Valid = false;
                                return A;
                            }
                        }
                        else
                        {
                            //either input or output file
                            if (A.InputFile == null)
                            {
                                //First file is input file
                                A.InputFile = Arg;
                                if (File.Exists(Arg))
                                {
                                    //automatically set output file sot it becomes an optional argument
                                    A.OutputFile = Tools.SwapExt(Arg, "png");
                                }
                                else
                                {
                                    Console.Error.WriteLine("InputFile does not exists or is inaccessible");
                                    A.Valid = false;
                                    return A;
                                }
                            }
                            else if (!PNGSet)
                            {
                                //second file is output file
                                PNGSet = true;
                                try
                                {
                                    File.Create(Arg).Close();
                                    File.Delete(Arg);
                                }
                                catch (Exception ex)
                                {
                                    Console.Error.WriteLine("Can't create output file. Error: {0}", ex.Message);
                                    A.Valid = false;
                                    return A;
                                }
                                A.OutputFile = Arg;
                            }
                            else
                            {
                                Console.Error.WriteLine("More than two files specified");
                                A.Valid = false;
                                return A;
                            }
                        }
                        break;
                }
            }
            return A;
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
        /// Show the graphical user interface
        /// </summary>
        private static void ShowUI()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new frmMain());
        }

    }

    public struct Arguments
    {
        public string InputFile, OutputFile;
        public int Height;
        public bool SingleColor, Valid;
    }
}
