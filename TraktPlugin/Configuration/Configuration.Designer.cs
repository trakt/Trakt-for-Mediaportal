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
            this.lbSyncTimerLength = new System.Windows.Forms.Label();
            this.cbTraktSyncLength = new System.Windows.Forms.ComboBox();
            this.btnClearLibrary = new System.Windows.Forms.Button();
            this.cbKeepInSync = new System.Windows.Forms.CheckBox();
            this.btnOK = new System.Windows.Forms.Button();
            this.ttpConfig = new System.Windows.Forms.ToolTip(this.components);
            this.btnTVSeriesRestrictions = new System.Windows.Forms.Button();
            this.gbRestrictions = new System.Windows.Forms.GroupBox();
            this.btnMovieRestrictions = new System.Windows.Forms.Button();
            this.linkTrakt = new System.Windows.Forms.LinkLabel();
            this.gbTraktAccount.SuspendLayout();
            this.gbPlugins.SuspendLayout();
            this.gbMisc.SuspendLayout();
            this.gbRestrictions.SuspendLayout();
            this.SuspendLayout();
            // 
            // gbTraktAccount
            // 
            this.gbTraktAccount.Controls.Add(this.linkTrakt);
            this.gbTraktAccount.Controls.Add(this.tbPassword);
            this.gbTraktAccount.Controls.Add(this.tbUsername);
            this.gbTraktAccount.Controls.Add(this.lbPassword);
            this.gbTraktAccount.Controls.Add(this.lbUsername);
            this.gbTraktAccount.Location = new System.Drawing.Point(12, 12);
            this.gbTraktAccount.Name = "gbTraktAccount";
            this.gbTraktAccount.Size = new System.Drawing.Size(290, 97);
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
            this.tbPassword.TabIndex = 3;
            this.tbPassword.UseSystemPasswordChar = true;
            this.tbPassword.Enter += new System.EventHandler(this.tbPassword_Enter);
            // 
            // tbUsername
            // 
            this.tbUsername.Location = new System.Drawing.Point(67, 22);
            this.tbUsername.Name = "tbUsername";
            this.tbUsername.Size = new System.Drawing.Size(211, 20);
            this.tbUsername.TabIndex = 1;
            this.tbUsername.TextChanged += new System.EventHandler(this.tbUsername_TextChanged);
            // 
            // lbPassword
            // 
            this.lbPassword.AutoSize = true;
            this.lbPassword.Location = new System.Drawing.Point(6, 51);
            this.lbPassword.Name = "lbPassword";
            this.lbPassword.Size = new System.Drawing.Size(53, 13);
            this.lbPassword.TabIndex = 2;
            this.lbPassword.Text = "&Password";
            // 
            // lbUsername
            // 
            this.lbUsername.AutoSize = true;
            this.lbUsername.Location = new System.Drawing.Point(6, 25);
            this.lbUsername.Name = "lbUsername";
            this.lbUsername.Size = new System.Drawing.Size(55, 13);
            this.lbUsername.TabIndex = 0;
            this.lbUsername.Text = "&Username";
            // 
            // gbPlugins
            // 
            this.gbPlugins.Controls.Add(this.btnDown);
            this.gbPlugins.Controls.Add(this.btnUp);
            this.gbPlugins.Controls.Add(this.clbPlugins);
            this.gbPlugins.Location = new System.Drawing.Point(12, 115);
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
            this.btnDown.TabIndex = 2;
            this.btnDown.UseVisualStyleBackColor = true;
            this.btnDown.Click += new System.EventHandler(this.btnDown_Click);
            // 
            // btnUp
            // 
            this.btnUp.Image = global::TraktPlugin.Properties.Resources.arrow_up;
            this.btnUp.Location = new System.Drawing.Point(249, 19);
            this.btnUp.Name = "btnUp";
            this.btnUp.Size = new System.Drawing.Size(29, 29);
            this.btnUp.TabIndex = 1;
            this.btnUp.UseVisualStyleBackColor = true;
            this.btnUp.Click += new System.EventHandler(this.btnUp_Click);
            // 
            // clbPlugins
            // 
            this.clbPlugins.FormattingEnabled = true;
            this.clbPlugins.Location = new System.Drawing.Point(9, 19);
            this.clbPlugins.Name = "clbPlugins";
            this.clbPlugins.Size = new System.Drawing.Size(233, 64);
            this.clbPlugins.TabIndex = 0;
            this.ttpConfig.SetToolTip(this.clbPlugins, resources.GetString("clbPlugins.ToolTip"));
            // 
            // gbMisc
            // 
            this.gbMisc.Controls.Add(this.lbSyncTimerLength);
            this.gbMisc.Controls.Add(this.cbTraktSyncLength);
            this.gbMisc.Controls.Add(this.btnClearLibrary);
            this.gbMisc.Controls.Add(this.cbKeepInSync);
            this.gbMisc.Location = new System.Drawing.Point(12, 274);
            this.gbMisc.Name = "gbMisc";
            this.gbMisc.Size = new System.Drawing.Size(288, 97);
            this.gbMisc.TabIndex = 3;
            this.gbMisc.TabStop = false;
            this.gbMisc.Text = "Misc";
            // 
            // lbSyncTimerLength
            // 
            this.lbSyncTimerLength.AutoSize = true;
            this.lbSyncTimerLength.Location = new System.Drawing.Point(67, 21);
            this.lbSyncTimerLength.Name = "lbSyncTimerLength";
            this.lbSyncTimerLength.Size = new System.Drawing.Size(143, 13);
            this.lbSyncTimerLength.TabIndex = 3;
            this.lbSyncTimerLength.Text = "Sync with Trakt every [X] hrs";
            this.ttpConfig.SetToolTip(this.lbSyncTimerLength, "Set this to the value in hours that you want to wait to resync with Trakt");
            // 
            // cbTraktSyncLength
            // 
            this.cbTraktSyncLength.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbTraktSyncLength.FormattingEnabled = true;
            this.cbTraktSyncLength.Items.AddRange(new object[] {
            "1",
            "6",
            "12",
            "24"});
            this.cbTraktSyncLength.Location = new System.Drawing.Point(9, 18);
            this.cbTraktSyncLength.Name = "cbTraktSyncLength";
            this.cbTraktSyncLength.Size = new System.Drawing.Size(52, 21);
            this.cbTraktSyncLength.TabIndex = 0;
            this.ttpConfig.SetToolTip(this.cbTraktSyncLength, "Set this to the value in hours that you want to wait to resync with Trakt");
            this.cbTraktSyncLength.SelectedValueChanged += new System.EventHandler(this.cbTraktSyncLength_SelectedValueChanged);
            // 
            // btnClearLibrary
            // 
            this.btnClearLibrary.Location = new System.Drawing.Point(7, 68);
            this.btnClearLibrary.Name = "btnClearLibrary";
            this.btnClearLibrary.Size = new System.Drawing.Size(271, 23);
            this.btnClearLibrary.TabIndex = 2;
            this.btnClearLibrary.Text = "&Clear Trakt Library";
            this.ttpConfig.SetToolTip(this.btnClearLibrary, "Click this button to remove all movies and episodes that you have synchronised, m" +
                    "arked as watched or rated online.");
            this.btnClearLibrary.UseVisualStyleBackColor = true;
            this.btnClearLibrary.Click += new System.EventHandler(this.btnClearLibrary_Click);
            // 
            // cbKeepInSync
            // 
            this.cbKeepInSync.AutoSize = true;
            this.cbKeepInSync.Location = new System.Drawing.Point(9, 45);
            this.cbKeepInSync.Name = "cbKeepInSync";
            this.cbKeepInSync.Size = new System.Drawing.Size(202, 17);
            this.cbKeepInSync.TabIndex = 1;
            this.cbKeepInSync.Text = "&Keep library/collection clean on trakt.";
            this.ttpConfig.SetToolTip(this.cbKeepInSync, resources.GetString("cbKeepInSync.ToolTip"));
            this.cbKeepInSync.UseVisualStyleBackColor = true;
            this.cbKeepInSync.CheckedChanged += new System.EventHandler(this.cbKeepInSync_CheckedChanged);
            // 
            // btnOK
            // 
            this.btnOK.Location = new System.Drawing.Point(227, 377);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(75, 23);
            this.btnOK.TabIndex = 4;
            this.btnOK.Text = "&OK";
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
            this.ttpConfig.ToolTipTitle = "Help";
            // 
            // btnTVSeriesRestrictions
            // 
            this.btnTVSeriesRestrictions.Location = new System.Drawing.Point(9, 20);
            this.btnTVSeriesRestrictions.Name = "btnTVSeriesRestrictions";
            this.btnTVSeriesRestrictions.Size = new System.Drawing.Size(104, 23);
            this.btnTVSeriesRestrictions.TabIndex = 0;
            this.btnTVSeriesRestrictions.Text = "&Series...";
            this.ttpConfig.SetToolTip(this.btnTVSeriesRestrictions, "Select the series you want to ignore from Syncronization and Scrobbling.");
            this.btnTVSeriesRestrictions.UseVisualStyleBackColor = true;
            this.btnTVSeriesRestrictions.Click += new System.EventHandler(this.btnTVSeriesRestrictions_Click);
            // 
            // gbRestrictions
            // 
            this.gbRestrictions.Controls.Add(this.btnMovieRestrictions);
            this.gbRestrictions.Controls.Add(this.btnTVSeriesRestrictions);
            this.gbRestrictions.Location = new System.Drawing.Point(12, 215);
            this.gbRestrictions.Name = "gbRestrictions";
            this.gbRestrictions.Size = new System.Drawing.Size(288, 53);
            this.gbRestrictions.TabIndex = 2;
            this.gbRestrictions.TabStop = false;
            this.gbRestrictions.Text = "Restrictions";
            // 
            // btnMovieRestrictions
            // 
            this.btnMovieRestrictions.Location = new System.Drawing.Point(138, 20);
            this.btnMovieRestrictions.Name = "btnMovieRestrictions";
            this.btnMovieRestrictions.Size = new System.Drawing.Size(104, 23);
            this.btnMovieRestrictions.TabIndex = 1;
            this.btnMovieRestrictions.Text = "&Movies...";
            this.btnMovieRestrictions.UseVisualStyleBackColor = true;
            this.btnMovieRestrictions.Click += new System.EventHandler(this.btnMovieRestrictions_Click);
            // 
            // linkTrakt
            // 
            this.linkTrakt.AutoSize = true;
            this.linkTrakt.Location = new System.Drawing.Point(207, 73);
            this.linkTrakt.Name = "linkTrakt";
            this.linkTrakt.Size = new System.Drawing.Size(71, 13);
            this.linkTrakt.TabIndex = 4;
            this.linkTrakt.TabStop = true;
            this.linkTrakt.Text = "Signup/Login";
            this.linkTrakt.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkTrakt_LinkClicked);
            // 
            // Configuration
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(312, 406);
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
            this.Text = "Trakt Configuration v" + TraktSettings.Version;
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
        private System.Windows.Forms.ComboBox cbTraktSyncLength;
        private System.Windows.Forms.Label lbSyncTimerLength;
        private System.Windows.Forms.LinkLabel linkTrakt;
    }
}