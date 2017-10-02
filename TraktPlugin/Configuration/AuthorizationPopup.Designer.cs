namespace TraktPlugin
{
    partial class AuthorizationPopup
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AuthorizationPopup));
            this.pbQRCode = new System.Windows.Forms.PictureBox();
            this.lblScanQRCode = new System.Windows.Forms.Label();
            this.lblUserCode = new System.Windows.Forms.Label();
            this.pbAuthorizationPoll = new System.Windows.Forms.ProgressBar();
            this.lnkActivate = new System.Windows.Forms.LinkLabel();
            ((System.ComponentModel.ISupportInitialize)(this.pbQRCode)).BeginInit();
            this.SuspendLayout();
            // 
            // pbQRCode
            // 
            this.pbQRCode.Image = ((System.Drawing.Image)(resources.GetObject("pbQRCode.Image")));
            this.pbQRCode.InitialImage = null;
            this.pbQRCode.Location = new System.Drawing.Point(16, 119);
            this.pbQRCode.MaximumSize = new System.Drawing.Size(330, 330);
            this.pbQRCode.MinimumSize = new System.Drawing.Size(330, 330);
            this.pbQRCode.Name = "pbQRCode";
            this.pbQRCode.Size = new System.Drawing.Size(330, 330);
            this.pbQRCode.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pbQRCode.TabIndex = 0;
            this.pbQRCode.TabStop = false;
            // 
            // lblScanQRCode
            // 
            this.lblScanQRCode.Location = new System.Drawing.Point(12, 13);
            this.lblScanQRCode.Name = "lblScanQRCode";
            this.lblScanQRCode.Size = new System.Drawing.Size(330, 40);
            this.lblScanQRCode.TabIndex = 1;
            this.lblScanQRCode.Text = "Scan the QR code or browse to URL and enter code below.";
            // 
            // lblUserCode
            // 
            this.lblUserCode.Font = new System.Drawing.Font("Microsoft Sans Serif", 26F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblUserCode.Location = new System.Drawing.Point(12, 464);
            this.lblUserCode.Name = "lblUserCode";
            this.lblUserCode.Size = new System.Drawing.Size(330, 68);
            this.lblUserCode.TabIndex = 2;
            this.lblUserCode.Text = "USERCODE";
            // 
            // pbAuthorizationPoll
            // 
            this.pbAuthorizationPoll.Location = new System.Drawing.Point(12, 547);
            this.pbAuthorizationPoll.Name = "pbAuthorizationPoll";
            this.pbAuthorizationPoll.Size = new System.Drawing.Size(330, 36);
            this.pbAuthorizationPoll.Step = 5;
            this.pbAuthorizationPoll.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
            this.pbAuthorizationPoll.TabIndex = 3;
            // 
            // lnkActivate
            // 
            this.lnkActivate.AutoSize = true;
            this.lnkActivate.Location = new System.Drawing.Point(13, 71);
            this.lnkActivate.Name = "lnkActivate";
            this.lnkActivate.Size = new System.Drawing.Size(164, 20);
            this.lnkActivate.TabIndex = 4;
            this.lnkActivate.TabStop = true;
            this.lnkActivate.Text = "https://trakt.tv/activate";
            this.lnkActivate.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.lnkActivate_LinkClicked);
            // 
            // AuthorizationPopup
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(360, 595);
            this.Controls.Add(this.lnkActivate);
            this.Controls.Add(this.pbAuthorizationPoll);
            this.Controls.Add(this.lblUserCode);
            this.Controls.Add(this.lblScanQRCode);
            this.Controls.Add(this.pbQRCode);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "AuthorizationPopup";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Authorization";
            this.TopMost = true;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.AuthorizationPopup_FormClosing);
            this.Load += new System.EventHandler(this.AuthorizationPopup_Load);
            ((System.ComponentModel.ISupportInitialize)(this.pbQRCode)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Label lblScanQRCode;
        private System.Windows.Forms.Label lblUserCode;
        private System.Windows.Forms.ProgressBar pbAuthorizationPoll;
        private System.Windows.Forms.LinkLabel lnkActivate;
        internal System.Windows.Forms.PictureBox pbQRCode;
    }
}