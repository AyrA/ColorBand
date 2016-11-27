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
            AyrA.IO.Terminal.RemoveConsole();
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
    }
}
