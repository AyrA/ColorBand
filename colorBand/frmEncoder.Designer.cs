namespace colorBand
{
    partial class frmEncoder
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.pbStatus = new System.Windows.Forms.ProgressBar();
            this.btnAbort = new System.Windows.Forms.Button();
            this.lblEncodingProgress = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // pbStatus
            // 
            this.pbStatus.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.pbStatus.Location = new System.Drawing.Point(12, 12);
            this.pbStatus.Name = "pbStatus";
            this.pbStatus.Size = new System.Drawing.Size(344, 23);
            this.pbStatus.TabIndex = 0;
            // 
            // btnAbort
            // 
            this.btnAbort.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnAbort.Location = new System.Drawing.Point(362, 12);
            this.btnAbort.Name = "btnAbort";
            this.btnAbort.Size = new System.Drawing.Size(70, 23);
            this.btnAbort.TabIndex = 1;
            this.btnAbort.Text = "&Abort";
            this.btnAbort.UseVisualStyleBackColor = true;
            this.btnAbort.Click += new System.EventHandler(this.btnAbort_Click);
            // 
            // lblEncodingProgress
            // 
            this.lblEncodingProgress.AutoSize = true;
            this.lblEncodingProgress.Font = new System.Drawing.Font("Courier New", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblEncodingProgress.Location = new System.Drawing.Point(12, 45);
            this.lblEncodingProgress.Name = "lblEncodingProgress";
            this.lblEncodingProgress.Size = new System.Drawing.Size(160, 17);
            this.lblEncodingProgress.TabIndex = 2;
            this.lblEncodingProgress.Text = "Starting Encoder...";
            // 
            // frmEncoder
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(444, 175);
            this.ControlBox = false;
            this.Controls.Add(this.lblEncodingProgress);
            this.Controls.Add(this.btnAbort);
            this.Controls.Add(this.pbStatus);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "frmEncoder";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Encoding";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.frmEncoder_FormClosed);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ProgressBar pbStatus;
        private System.Windows.Forms.Button btnAbort;
        private System.Windows.Forms.Label lblEncodingProgress;
    }
}