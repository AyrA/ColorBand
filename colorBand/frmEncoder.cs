using System;
using System.Windows.Forms;
using System.Threading;
using System.Diagnostics;
using System.IO;

namespace colorBand
{
    public partial class frmEncoder : Form
    {
        private string TempDir, SourceFile, DestinationFile;
        private int BandHeight;
        private bool SingleColor;
        private Thread Encoder;
        private bool Aborted = false;
        private VideoInfo VI;

        public frmEncoder(string TempDir, string SourceFile, string DestinationFile, int BandHeight, bool SingleColor)
        {
            this.TempDir = TempDir;
            this.SourceFile = SourceFile;
            this.DestinationFile = DestinationFile;
            this.BandHeight = BandHeight;
            this.SingleColor = SingleColor;

            Tools.ThreadExit += Tools_ThreadExit;

            VI = Tools.GetVideoInfo(SourceFile);

            InitializeComponent();

            pbStatus.Maximum = (int)VI.Duration.TotalSeconds;

            Encoder = new Thread(delegate()
            {
                using (Process P = Tools.BeginConversion(SourceFile, TempDir, BandHeight))
                {
                    using (StreamReader SR = P.StandardError)
                    {
                        StatusLine SL;
                        while (!SR.EndOfStream)
                        {
                            if (Aborted)
                            {
                                P.StandardInput.WriteLine("q");
                            }
                            SL = new StatusLine(SR.ReadLine());
                            if (!Aborted)
                            {
                                TimeSpan Estimate = new TimeSpan(0, 0, (int)(VI.Duration.TotalSeconds / SL.Speed));
                                if (SL.Frame > 0)
                                {
                                    this.Invoke((MethodInvoker)delegate
                                    {
                                        lblEncodingProgress.Text = string.Format(@"Status:
Speed: {0} times playback speed
Current Time in Video: {1}
Current Frame: {2}
Progress: {3}
Estimated Runtime: {4}",
                                            SL.Speed, SL.Time, SL.Frame, Perc(SL.Frame, (int)VI.Duration.TotalSeconds), Estimate);
                                        //Set progress bar only if valid
                                        if (pbStatus.Maximum >= SL.Frame)
                                        {
                                            pbStatus.Value = SL.Frame;
                                        }
                                    });
                                }
                            }
                        }
                    }
                    P.WaitForExit();
                }
            })
            {
                IsBackground = true,
                Name = "Encoding of " + SourceFile
            };
            Encoder.Start();
            Tools.WatchThread(Encoder);
        }

        private int Perc(double Current, double Max)
        {
            if (Current > Max)
            {
                return 100;
            }
            if (Current < 0.0 || Max <= 0.0)
            {
                return 0;
            }
            return (int)(Current / Max * 100.0);
        }

        private void Tools_ThreadExit(Thread T)
        {
            if (T == Encoder)
            {
                if (!Aborted || MessageBox.Show("Do you still want to create the Frame Band?\r\nSelecting [YES] will use the existing frames", "Working with existing material", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    //Encoding Done
                    this.Invoke((MethodInvoker)EncodePNG);
                }
                this.Invoke((MethodInvoker)Close);
            }
        }

        private void EncodePNG()
        {
            btnAbort.Enabled = false;
            if (SingleColor)
            {
                //Extract color information from the PNG images.
                //GetFiles is rather slow, especially for a large number of files,
                //but it is certainly easier to use than manually using the Windows API
                var Colors = Tools.ExtractColorFromPixel(Directory.GetFiles(TempDir, "*.png"));


                //It is possible, that no colors were extracted,
                //for example if the input is an audio file.
                if (Colors.Length > 0)
                {
                    Tools.RenderBand(Colors, DestinationFile, Height);
                }
                else
                {
                    //Probably invalid video file
                    MessageBox.Show("No frames were extracted. Make sure the video file is not corrupted", "No frames extracted", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                }
            }
            else
            {
                Tools.JoinImages(Directory.GetFiles(TempDir, "*.png"), DestinationFile);
            }
        }

        private void btnAbort_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Abort the encoding process?", "Abort encoding", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) == DialogResult.Yes)
            {
                Aborted = true;
                //Encoder.Join();
                /*
                if (MessageBox.Show("Do you still want to create the Frame Band?\r\nSelecting [YES] will use the existing frames", "Working with existing material", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    EncodePNG();
                }
                Close();
                //*/
            }
        }

        private void frmEncoder_FormClosed(object sender, FormClosedEventArgs e)
        {
            Tools.ThreadExit -= Tools_ThreadExit;
        }
    }
}
