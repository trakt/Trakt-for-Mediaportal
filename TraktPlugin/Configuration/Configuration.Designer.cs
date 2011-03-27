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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Configuration));
            this.gbTraktAccount = new System.Windows.Forms.GroupBox();
            this.tbPassword = new System.Windows.Forms.TextBox();
            this.tbUsername = new System.Windows.Forms.TextBox();
            this.lbPassword = new System.Windows.Forms.Label();
            this.lbUsername = new System.Windows.Forms.Label();
            this.gbPlugins = new System.Windows.Forms.GroupBox();
            this.btnDown = new System.Windows.Forms.Button();
            this.btnUp = new System.Windows.Forms.Button();
            this.clbPlugins = new System.Windows.Forms.CheckedListBox();
            this.gbMisc = new System.Windows.Forms.GroupBox();
            this.btnClearLibrary = new System.Windows.Forms.Button();
            this.cbKeepInSync = new System.Windows.Forms.CheckBox();
            this.btnOK = new System.Windows.Forms.Button();
            this.ttpConfig = new System.Windows.Forms.ToolTip(this.components);
            this.btnTVSeriesRestrictions = new System.Windows.Forms.Button();
            this.gbRestrictions = new System.Windows.Forms.GroupBox();
            this.btnMovieRestrictions = new System.Windows.Forms.Button();
            this.gbTraktAccount.SuspendLayout();
            this.gbPlugins.SuspendLayout();
            this.gbMisc.SuspendLayout();
            this.gbRestrictions.SuspendLayout();
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
            this.gbTraktAccount.Text = "Account";
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
            this.gbPlugins.Controls.Add(this.btnDown);
            this.gbPlugins.Controls.Add(this.btnUp);
            this.gbPlugins.Controls.Add(this.clbPlugins);
            this.gbPlugins.Location = new System.Drawing.Point(12, 107);
            this.gbPlugins.Name = "gbPlugins";
            this.gbPlugins.Size = new System.Drawing.Size(290, 94);
            this.gbPlugins.TabIndex = 1;
            this.gbPlugins.TabStop = false;
            this.gbPlugins.Text = "Plugins";
            // 
            // btnDown
            // 
            this.btnDown.Image = global::TraktPlugin.Properties.Resources.arrow_down;
            this.btnDown.Location = new System.Drawing.Point(249, 53);
            this.btnDown.Name = "btnDown";
            this.btnDown.Size = new System.Drawing.Size(29, 30);
            this.btnDown.TabIndex = 7;
            this.btnDown.UseVisualStyleBackColor = true;
            this.btnDown.Click += new System.EventHandler(this.btnDown_Click);
            // 
            // btnUp
            // 
            this.btnUp.Image = global::TraktPlugin.Properties.Resources.arrow_up;
            this.btnUp.Location = new System.Drawing.Point(249, 19);
            this.btnUp.Name = "btnUp";
            this.btnUp.Size = new System.Drawing.Size(29, 29);
            this.btnUp.TabIndex = 6;
            this.btnUp.UseVisualStyleBackColor = true;
            this.btnUp.Click += new System.EventHandler(this.btnUp_Click);
            // 
            // clbPlugins
            // 
            this.clbPlugins.FormattingEnabled = true;
            this.clbPlugins.Location = new System.Drawing.Point(9, 19);
            this.clbPlugins.Name = "clbPlugins";
            this.clbPlugins.Size = new System.Drawing.Size(233, 64);
            this.clbPlugins.TabIndex = 5;
            this.ttpConfig.SetToolTip(this.clbPlugins, resources.GetString("clbPlugins.ToolTip"));
            // 
            // gbMisc
            // 
            this.gbMisc.Controls.Add(this.btnClearLibrary);
            this.gbMisc.Controls.Add(this.cbKeepInSync);
            this.gbMisc.Location = new System.Drawing.Point(12, 266);
            this.gbMisc.Name = "gbMisc";
            this.gbMisc.Size = new System.Drawing.Size(288, 78);
            this.gbMisc.TabIndex = 2;
            this.gbMisc.TabStop = false;
            this.gbMisc.Text = "Misc";
            // 
            // btnClearLibrary
            // 
            this.btnClearLibrary.Location = new System.Drawing.Point(6, 45);
            this.btnClearLibrary.Name = "btnClearLibrary";
            this.btnClearLibrary.Size = new System.Drawing.Size(271, 23);
            this.btnClearLibrary.TabIndex = 4;
            this.btnClearLibrary.Text = "Clear My Library";
            this.btnClearLibrary.UseVisualStyleBackColor = true;
            this.btnClearLibrary.Click += new System.EventHandler(this.btnClearLibrary_Click);
            // 
            // cbKeepInSync
            // 
            this.cbKeepInSync.AutoSize = true;
            this.cbKeepInSync.Location = new System.Drawing.Point(9, 20);
            this.cbKeepInSync.Name = "cbKeepInSync";
            this.cbKeepInSync.Size = new System.Drawing.Size(243, 17);
            this.cbKeepInSync.TabIndex = 3;
            this.cbKeepInSync.Text = "Remove items no longer in database on Sync.";
            this.ttpConfig.SetToolTip(this.cbKeepInSync, resources.GetString("cbKeepInSync.ToolTip"));
            this.cbKeepInSync.UseVisualStyleBackColor = true;
            this.cbKeepInSync.CheckedChanged += new System.EventHandler(this.cbKeepInSync_CheckedChanged);
            // 
            // btnOK
            // 
            this.btnOK.Location = new System.Drawing.Point(227, 350);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(75, 23);
            this.btnOK.TabIndex = 3;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // ttpConfig
            // 
            this.ttpConfig.AutoPopDelay = 18000;
            this.ttpConfig.InitialDelay = 500;
            this.ttpConfig.IsBalloon = true;
            this.ttpConfig.ReshowDelay = 100;
            this.ttpConfig.ToolTipIcon = System.Windows.Forms.ToolTipIcon.Info;
            // 
            // btnTVSeriesRestrictions
            // 
            this.btnTVSeriesRestrictions.Location = new System.Drawing.Point(9, 20);
            this.btnTVSeriesRestrictions.Name = "btnTVSeriesRestrictions";
            this.btnTVSeriesRestrictions.Size = new System.Drawing.Size(104, 23);
            this.btnTVSeriesRestrictions.TabIndex = 0;
            this.btnTVSeriesRestrictions.Text = "Series...";
            this.ttpConfig.SetToolTip(this.btnTVSeriesRestrictions, "Select the series you want to ignore from Syncronization and Scrobbling.");
            this.btnTVSeriesRestrictions.UseVisualStyleBackColor = true;
            this.btnTVSeriesRestrictions.Click += new System.EventHandler(this.btnTVSeriesRestrictions_Click);
            // 
            // gbRestrictions
            // 
            this.gbRestrictions.Controls.Add(this.btnMovieRestrictions);
            this.gbRestrictions.Controls.Add(this.btnTVSeriesRestrictions);
            this.gbRestrictions.Location = new System.Drawing.Point(12, 207);
            this.gbRestrictions.Name = "gbRestrictions";
            this.gbRestrictions.Size = new System.Drawing.Size(288, 53);
            this.gbRestrictions.TabIndex = 5;
            this.gbRestrictions.TabStop = false;
            this.gbRestrictions.Text = "Restrictions";
            // 
            // btnMovieRestrictions
            // 
            this.btnMovieRestrictions.Location = new System.Drawing.Point(138, 20);
            this.btnMovieRestrictions.Name = "btnMovieRestrictions";
            this.btnMovieRestrictions.Size = new System.Drawing.Size(104, 23);
            this.btnMovieRestrictions.TabIndex = 0;
            this.btnMovieRestrictions.Text = "Movies...";
            this.btnMovieRestrictions.UseVisualStyleBackColor = true;
            this.btnMovieRestrictions.Click += new System.EventHandler(this.btnMovieRestrictions_Click);
            // 
            // Configuration
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(312, 382);
            this.Controls.Add(this.gbRestrictions);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.gbMisc);
            this.Controls.Add(this.gbPlugins);
            this.Controls.Add(this.gbTraktAccount);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "Configuration";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Trakt Configuration";
            this.gbTraktAccount.ResumeLayout(false);
            this.gbTraktAccount.PerformLayout();
            this.gbPlugins.ResumeLayout(false);
            this.gbMisc.ResumeLayout(false);
            this.gbMisc.PerformLayout();
            this.gbRestrictions.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox gbTraktAccount;
        private System.Windows.Forms.Label lbUsername;
        private System.Windows.Forms.TextBox tbPassword;
        private System.Windows.Forms.TextBox tbUsername;
        private System.Windows.Forms.Label lbPassword;
        private System.Windows.Forms.GroupBox gbPlugins;
        private System.Windows.Forms.GroupBox gbMisc;
        private System.Windows.Forms.CheckBox cbKeepInSync;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button btnDown;
        private System.Windows.Forms.Button btnUp;
        private System.Windows.Forms.CheckedListBox clbPlugins;
        private System.Windows.Forms.Button btnClearLibrary;
        private System.Windows.Forms.ToolTip ttpConfig;
        private System.Windows.Forms.GroupBox gbRestrictions;
        private System.Windows.Forms.Button btnTVSeriesRestrictions;
        private System.Windows.Forms.Button btnMovieRestrictions;
    }
}