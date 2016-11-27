using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace colorBand
{
    public partial class frmMain : Form
    {
        string TempPath = Tools.GetTempDir("frameband");

        public frmMain()
        {
            Console.Error.WriteLine("Extracting FFmpeg...");
            Tools.LogToConsole = true;
            Tools.WriteEncoder(TempPath);
            Tools.LogToConsole = false;
#if !DEBUG
            AyrA.IO.Terminal.RemoveConsole();
#endif
            InitializeComponent();
        }

        private void btnSource_Click(object sender, EventArgs e)
        {
            if (OFD.ShowDialog() == DialogResult.OK)
            {
                tbSource.Text = OFD.FileName;
                if (string.IsNullOrEmpty(tbDestination.Text))
                {
                    AutoName();
                }
                SetInfo();
            }
        }

        private void btnDestination_Click(object sender, EventArgs e)
        {
            if (SFD.ShowDialog() == DialogResult.OK)
            {
                tbDestination.Text = SFD.FileName;
                if (!tbDestination.Text.EndsWith(".png"))
                {
                    MessageBox.Show("Output file type must be PNG. It will be changed now");
                    tbDestination.Text = Tools.SwapExt(tbDestination.Text, "png");
                }
            }
        }

        private void frmMain_FormClosed(object sender, FormClosedEventArgs e)
        {
            Directory.Delete(TempPath, true);
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            if (nudHeight.Value < 1)
            {
                MessageBox.Show("Please specify a height\r\nYou can open the video again to have this automatically assigned", "Height invalid", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
            else if (!File.Exists(tbSource.Text))
            {
                MessageBox.Show("The input file doesn't exists.", "Input file not found", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
            else if (!tbDestination.Text.ToLower().EndsWith(".png"))
            {
                MessageBox.Show("Output file must be PNG. We change that now.", "Output file format invalid", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                tbDestination.Text = Tools.SwapExt(tbDestination.Text, "png");
            }
            else
            {
                var Encoder = new frmEncoder(TempPath, tbSource.Text, tbDestination.Text, (int)nudHeight.Value, cbSingleColor.Checked);
                Encoder.ShowDialog();
                //everything OK. Begin conversion
            }
        }

        private void tbSource_Leave(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(tbSource.Text))
            {
                SetInfo();
            }
        }

        private void AutoName()
        {
            tbDestination.Text = Tools.SwapExt(tbSource.Text, "png");
        }

        private void SetInfo()
        {
            //remove quotes around file name
            if (tbSource.Text.StartsWith("\"") || tbSource.Text.EndsWith("\""))
            {
                tbSource.Text = tbSource.Text.Trim('\"');
            }
            if (File.Exists(tbSource.Text))
            {
                VideoInfo VI = Tools.GetVideoInfo(tbSource.Text);
                nudHeight.Value = VI.Resolution.Height;
                tbInfo.Text = string.Format(@"File: {0}
Type: {1}
Runtime: {2}
Resolution: {3}
Bitrate: {4} kbit/s
Frames: {5} (estimated from runtime)",
         VI.VideoFile, VI.CodecName, VI.Duration, VI.Resolution, VI.Bitrate/1000, Math.Floor(VI.Duration.TotalSeconds));
            }
            else
            {
                MessageBox.Show("The input file doesn't exists.", "Input file not found", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        private void frmMain_DragDrop(object sender, DragEventArgs e)
        {
            var L = new List<string>(e.Data.GetFormats());
            if (
                L.Contains(DataFormats.FileDrop) ||
                L.Contains(DataFormats.UnicodeText) ||
                L.Contains(DataFormats.Text))
            {
                string[] Files;
                if (L.Contains(DataFormats.FileDrop))
                {
                    //Dropped a file
                    Files = (string[])e.Data.GetData(DataFormats.FileDrop);
                }
                else if (L.Contains(DataFormats.UnicodeText))
                {
                    //Dropped a path in unicode
                    Files = new string[] { e.Data.GetData(DataFormats.UnicodeText).ToString().TrimStart().Split('\n')[0].Trim() };
                }
                else
                {
                    //Dropped a path in ANSI
                    Files = new string[] { e.Data.GetData(DataFormats.Text).ToString().TrimStart().Split('\n')[0].Trim() };
                }

                if (Files.Length > 0)
                {
                    tbSource.Text = Files[0];
                    SetInfo();
                    if (string.IsNullOrEmpty(tbDestination.Text))
                    {
                        AutoName();
                    }
                    if (Files.Length > 1)
                    {
                        MessageBox.Show("Only the first file was added", "Too many files", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    }
                }
                else
                {
                    MessageBox.Show("Could not extract file names from the drop", "Input file not found", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                }
            }
        }

        private void frmMain_DragOver(object sender, DragEventArgs e)
        {
            var L = new List<string>(e.Data.GetFormats());
            if (
                //File dropped itself
                L.Contains(DataFormats.FileDrop) ||
                //Path of a file dropped as unicode
                L.Contains(DataFormats.UnicodeText) ||
                //Path of a file dropped as ANSI
                L.Contains(DataFormats.Text))
            {
                e.Effect = DragDropEffects.Copy;
            }
            else
            {
                Console.Error.WriteLine(string.Join("\r\n", L.ToArray()));
            }
        }
    }
}
