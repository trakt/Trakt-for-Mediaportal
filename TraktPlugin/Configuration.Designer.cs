namespace TraktPlugin
{
    partial class Configuration
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
            this.btnOk = new System.Windows.Forms.Button();
            this.btnSync = new System.Windows.Forms.Button();
            this.lbUsername = new System.Windows.Forms.Label();
            this.tbUsername = new System.Windows.Forms.TextBox();
            this.gbTraktDetails = new System.Windows.Forms.GroupBox();
            this.lbPassword = new System.Windows.Forms.Label();
            this.tbPassword = new System.Windows.Forms.TextBox();
            this.gbSyncronise = new System.Windows.Forms.GroupBox();
            this.pbSync = new System.Windows.Forms.ProgressBar();
            this.gbOptions = new System.Windows.Forms.GroupBox();
            this.cbKeepTraktInSync = new System.Windows.Forms.CheckBox();
            this.gbTraktDetails.SuspendLayout();
            this.gbSyncronise.SuspendLayout();
            this.gbOptions.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnOk
            // 
            this.btnOk.Location = new System.Drawing.Point(197, 301);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(75, 23);
            this.btnOk.TabIndex = 0;
            this.btnOk.Text = "OK";
            this.btnOk.UseVisualStyleBackColor = true;
            this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
            // 
            // btnSync
            // 
            this.btnSync.Location = new System.Drawing.Point(4, 29);
            this.btnSync.Name = "btnSync";
            this.btnSync.Size = new System.Drawing.Size(250, 27);
            this.btnSync.TabIndex = 3;
            this.btnSync.Text = "Sync Library";
            this.btnSync.UseVisualStyleBackColor = true;
            this.btnSync.Click += new System.EventHandler(this.btnSync_Click);
            // 
            // lbUsername
            // 
            this.lbUsername.AutoSize = true;
            this.lbUsername.Location = new System.Drawing.Point(17, 25);
            this.lbUsername.Name = "lbUsername";
            this.lbUsername.Size = new System.Drawing.Size(55, 13);
            this.lbUsername.TabIndex = 2;
            this.lbUsername.Text = "Username";
            // 
            // tbUsername
            // 
            this.tbUsername.Location = new System.Drawing.Point(78, 22);
            this.tbUsername.Name = "tbUsername";
            this.tbUsername.Size = new System.Drawing.Size(167, 20);
            this.tbUsername.TabIndex = 1;
            this.tbUsername.TextChanged += new System.EventHandler(this.tbUsername_TextChanged);
            // 
            // gbTraktDetails
            // 
            this.gbTraktDetails.Controls.Add(this.lbPassword);
            this.gbTraktDetails.Controls.Add(this.lbUsername);
            this.gbTraktDetails.Controls.Add(this.tbPassword);
            this.gbTraktDetails.Controls.Add(this.tbUsername);
            this.gbTraktDetails.Location = new System.Drawing.Point(12, 12);
            this.gbTraktDetails.Name = "gbTraktDetails";
            this.gbTraktDetails.Size = new System.Drawing.Size(260, 83);
            this.gbTraktDetails.TabIndex = 4;
            this.gbTraktDetails.TabStop = false;
            this.gbTraktDetails.Text = "Trakt Account";
            // 
            // lbPassword
            // 
            this.lbPassword.AutoSize = true;
            this.lbPassword.Location = new System.Drawing.Point(17, 51);
            this.lbPassword.Name = "lbPassword";
            this.lbPassword.Size = new System.Drawing.Size(53, 13);
            this.lbPassword.TabIndex = 2;
            this.lbPassword.Text = "Password";
            // 
            // tbPassword
            // 
            this.tbPassword.Location = new System.Drawing.Point(78, 48);
            this.tbPassword.Name = "tbPassword";
            this.tbPassword.Size = new System.Drawing.Size(167, 20);
            this.tbPassword.TabIndex = 2;
            this.tbPassword.UseSystemPasswordChar = true;
            this.tbPassword.TextChanged += new System.EventHandler(this.tbPassword_TextChanged);
            // 
            // gbSyncronise
            // 
            this.gbSyncronise.Controls.Add(this.pbSync);
            this.gbSyncronise.Controls.Add(this.btnSync);
            this.gbSyncronise.Location = new System.Drawing.Point(12, 101);
            this.gbSyncronise.Name = "gbSyncronise";
            this.gbSyncronise.Size = new System.Drawing.Size(260, 102);
            this.gbSyncronise.TabIndex = 5;
            this.gbSyncronise.TabStop = false;
            this.gbSyncronise.Text = "Syncronise";
            // 
            // pbSync
            // 
            this.pbSync.Location = new System.Drawing.Point(6, 62);
            this.pbSync.Name = "pbSync";
            this.pbSync.Size = new System.Drawing.Size(248, 23);
            this.pbSync.TabIndex = 2;
            // 
            // gbOptions
            // 
            this.gbOptions.Controls.Add(this.cbKeepTraktInSync);
            this.gbOptions.Location = new System.Drawing.Point(12, 209);
            this.gbOptions.Name = "gbOptions";
            this.gbOptions.Size = new System.Drawing.Size(260, 73);
            this.gbOptions.TabIndex = 6;
            this.gbOptions.TabStop = false;
            this.gbOptions.Text = "Options";
            // 
            // cbKeepTraktInSync
            // 
            this.cbKeepTraktInSync.AutoSize = true;
            this.cbKeepTraktInSync.Location = new System.Drawing.Point(6, 19);
            this.cbKeepTraktInSync.Name = "cbKeepTraktInSync";
            this.cbKeepTraktInSync.Size = new System.Drawing.Size(117, 17);
            this.cbKeepTraktInSync.TabIndex = 0;
            this.cbKeepTraktInSync.Text = "Keep Trakt in Sync";
            this.cbKeepTraktInSync.UseVisualStyleBackColor = true;
            this.cbKeepTraktInSync.CheckedChanged += new System.EventHandler(this.cbKeepTraktInSync_CheckedChanged);
            // 
            // Configuration
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(284, 330);
            this.Controls.Add(this.gbOptions);
            this.Controls.Add(this.gbSyncronise);
            this.Controls.Add(this.gbTraktDetails);
            this.Controls.Add(this.btnOk);
            this.Name = "Configuration";
            this.Text = "Configuration";
            this.gbTraktDetails.ResumeLayout(false);
            this.gbTraktDetails.PerformLayout();
            this.gbSyncronise.ResumeLayout(false);
            this.gbOptions.ResumeLayout(false);
            this.gbOptions.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.Button btnSync;
        private System.Windows.Forms.Label lbUsername;
        private System.Windows.Forms.TextBox tbUsername;
        private System.Windows.Forms.GroupBox gbTraktDetails;
        private System.Windows.Forms.GroupBox gbSyncronise;
        private System.Windows.Forms.Label lbPassword;
        private System.Windows.Forms.TextBox tbPassword;
        private System.Windows.Forms.ProgressBar pbSync;
        private System.Windows.Forms.GroupBox gbOptions;
        private System.Windows.Forms.CheckBox cbKeepTraktInSync;
    }
}