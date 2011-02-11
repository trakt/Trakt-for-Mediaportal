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
            this.gbTraktAccount = new System.Windows.Forms.GroupBox();
            this.tbPassword = new System.Windows.Forms.TextBox();
            this.tbUsername = new System.Windows.Forms.TextBox();
            this.lbPassword = new System.Windows.Forms.Label();
            this.lbUsername = new System.Windows.Forms.Label();
            this.gbPlugins = new System.Windows.Forms.GroupBox();
            this.label1 = new System.Windows.Forms.Label();
            this.nudMovingPictures = new System.Windows.Forms.NumericUpDown();
            this.cbMovingPictures = new System.Windows.Forms.CheckBox();
            this.gbMisc = new System.Windows.Forms.GroupBox();
            this.cbKeepInSync = new System.Windows.Forms.CheckBox();
            this.btnOK = new System.Windows.Forms.Button();
            this.gbTraktAccount.SuspendLayout();
            this.gbPlugins.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudMovingPictures)).BeginInit();
            this.gbMisc.SuspendLayout();
            this.SuspendLayout();
            // 
            // gbTraktAccount
            // 
            this.gbTraktAccount.Controls.Add(this.tbPassword);
            this.gbTraktAccount.Controls.Add(this.tbUsername);
            this.gbTraktAccount.Controls.Add(this.lbPassword);
            this.gbTraktAccount.Controls.Add(this.lbUsername);
            this.gbTraktAccount.Location = new System.Drawing.Point(12, 12);
            this.gbTraktAccount.Name = "gbTraktAccount";
            this.gbTraktAccount.Size = new System.Drawing.Size(290, 89);
            this.gbTraktAccount.TabIndex = 0;
            this.gbTraktAccount.TabStop = false;
            this.gbTraktAccount.Text = "Trakt Account";
            // 
            // tbPassword
            // 
            this.tbPassword.Location = new System.Drawing.Point(67, 48);
            this.tbPassword.Name = "tbPassword";
            this.tbPassword.PasswordChar = '*';
            this.tbPassword.Size = new System.Drawing.Size(211, 20);
            this.tbPassword.TabIndex = 1;
            this.tbPassword.UseSystemPasswordChar = true;
            this.tbPassword.TextChanged += new System.EventHandler(this.tbPassword_TextChanged);
            // 
            // tbUsername
            // 
            this.tbUsername.Location = new System.Drawing.Point(67, 22);
            this.tbUsername.Name = "tbUsername";
            this.tbUsername.Size = new System.Drawing.Size(211, 20);
            this.tbUsername.TabIndex = 0;
            this.tbUsername.TextChanged += new System.EventHandler(this.tbUsername_TextChanged);
            // 
            // lbPassword
            // 
            this.lbPassword.AutoSize = true;
            this.lbPassword.Location = new System.Drawing.Point(6, 51);
            this.lbPassword.Name = "lbPassword";
            this.lbPassword.Size = new System.Drawing.Size(53, 13);
            this.lbPassword.TabIndex = 0;
            this.lbPassword.Text = "Password";
            // 
            // lbUsername
            // 
            this.lbUsername.AutoSize = true;
            this.lbUsername.Location = new System.Drawing.Point(6, 25);
            this.lbUsername.Name = "lbUsername";
            this.lbUsername.Size = new System.Drawing.Size(55, 13);
            this.lbUsername.TabIndex = 0;
            this.lbUsername.Text = "Username";
            // 
            // gbPlugins
            // 
            this.gbPlugins.Controls.Add(this.label1);
            this.gbPlugins.Controls.Add(this.nudMovingPictures);
            this.gbPlugins.Controls.Add(this.cbMovingPictures);
            this.gbPlugins.Location = new System.Drawing.Point(12, 107);
            this.gbPlugins.Name = "gbPlugins";
            this.gbPlugins.Size = new System.Drawing.Size(290, 67);
            this.gbPlugins.TabIndex = 1;
            this.gbPlugins.TabStop = false;
            this.gbPlugins.Text = "Plugins";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(212, 13);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(38, 13);
            this.label1.TabIndex = 3;
            this.label1.Text = "Priority";
            // 
            // nudMovingPictures
            // 
            this.nudMovingPictures.Location = new System.Drawing.Point(209, 31);
            this.nudMovingPictures.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            -2147483648});
            this.nudMovingPictures.Name = "nudMovingPictures";
            this.nudMovingPictures.Size = new System.Drawing.Size(47, 20);
            this.nudMovingPictures.TabIndex = 1;
            this.nudMovingPictures.ValueChanged += new System.EventHandler(this.nudMovingPictures_ValueChanged);
            // 
            // cbMovingPictures
            // 
            this.cbMovingPictures.AutoSize = true;
            this.cbMovingPictures.Location = new System.Drawing.Point(9, 31);
            this.cbMovingPictures.Name = "cbMovingPictures";
            this.cbMovingPictures.Size = new System.Drawing.Size(102, 17);
            this.cbMovingPictures.TabIndex = 0;
            this.cbMovingPictures.Text = "Moving Pictures";
            this.cbMovingPictures.UseVisualStyleBackColor = true;
            this.cbMovingPictures.CheckedChanged += new System.EventHandler(this.cbMovingPictures_CheckedChanged);
            // 
            // gbMisc
            // 
            this.gbMisc.Controls.Add(this.cbKeepInSync);
            this.gbMisc.Location = new System.Drawing.Point(12, 180);
            this.gbMisc.Name = "gbMisc";
            this.gbMisc.Size = new System.Drawing.Size(288, 51);
            this.gbMisc.TabIndex = 2;
            this.gbMisc.TabStop = false;
            this.gbMisc.Text = "Misc";
            // 
            // cbKeepInSync
            // 
            this.cbKeepInSync.AutoSize = true;
            this.cbKeepInSync.Location = new System.Drawing.Point(9, 20);
            this.cbKeepInSync.Name = "cbKeepInSync";
            this.cbKeepInSync.Size = new System.Drawing.Size(118, 17);
            this.cbKeepInSync.TabIndex = 3;
            this.cbKeepInSync.Text = "Keep Trakt In Sync";
            this.cbKeepInSync.UseVisualStyleBackColor = true;
            this.cbKeepInSync.CheckedChanged += new System.EventHandler(this.cbKeepInSync_CheckedChanged);
            // 
            // btnOK
            // 
            this.btnOK.Location = new System.Drawing.Point(227, 237);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(75, 23);
            this.btnOK.TabIndex = 3;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // Configuration
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(312, 270);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.gbMisc);
            this.Controls.Add(this.gbPlugins);
            this.Controls.Add(this.gbTraktAccount);
            this.Name = "Configuration";
            this.Text = "Configuration";
            this.gbTraktAccount.ResumeLayout(false);
            this.gbTraktAccount.PerformLayout();
            this.gbPlugins.ResumeLayout(false);
            this.gbPlugins.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudMovingPictures)).EndInit();
            this.gbMisc.ResumeLayout(false);
            this.gbMisc.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox gbTraktAccount;
        private System.Windows.Forms.Label lbUsername;
        private System.Windows.Forms.TextBox tbPassword;
        private System.Windows.Forms.TextBox tbUsername;
        private System.Windows.Forms.Label lbPassword;
        private System.Windows.Forms.GroupBox gbPlugins;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.NumericUpDown nudMovingPictures;
        private System.Windows.Forms.CheckBox cbMovingPictures;
        private System.Windows.Forms.GroupBox gbMisc;
        private System.Windows.Forms.CheckBox cbKeepInSync;
        private System.Windows.Forms.Button btnOK;
    }
}