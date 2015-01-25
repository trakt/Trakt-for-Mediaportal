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
            this.linkTrakt = new System.Windows.Forms.LinkLabel();
            this.tbPassword = new System.Windows.Forms.TextBox();
            this.tbUsername = new System.Windows.Forms.TextBox();
            this.lbPassword = new System.Windows.Forms.Label();
            this.lbUsername = new System.Windows.Forms.Label();
            this.gbPlugins = new System.Windows.Forms.GroupBox();
            this.btnDown = new System.Windows.Forms.Button();
            this.btnUp = new System.Windows.Forms.Button();
            this.clbPlugins = new System.Windows.Forms.CheckedListBox();
            this.gbMisc = new System.Windows.Forms.GroupBox();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.cbSyncRatings = new System.Windows.Forms.CheckBox();
            this.cbSyncLibrary = new System.Windows.Forms.CheckBox();
            this.cbMyFilmsCategories = new System.Windows.Forms.CheckBox();
            this.cbMovingPicturesFilters = new System.Windows.Forms.CheckBox();
            this.cbMovingPicturesCategories = new System.Windows.Forms.CheckBox();
            this.lbSyncTimerLength = new System.Windows.Forms.Label();
            this.btnClearLibrary = new System.Windows.Forms.Button();
            this.cbKeepInSync = new System.Windows.Forms.CheckBox();
            this.btnOK = new System.Windows.Forms.Button();
            this.ttpConfig = new System.Windows.Forms.ToolTip(this.components);
            this.btnTVSeriesRestrictions = new System.Windows.Forms.Button();
            this.gbRestrictions = new System.Windows.Forms.GroupBox();
            this.btnMovieRestrictions = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.textBox2 = new System.Windows.Forms.TextBox();
            this.cbSyncPlayback = new System.Windows.Forms.CheckBox();
            this.numSyncResumeDelta = new System.Windows.Forms.NumericUpDown();
            this.lblSyncResumeDelta = new System.Windows.Forms.Label();
            this.numSyncInterval = new System.Windows.Forms.NumericUpDown();
            this.label2 = new System.Windows.Forms.Label();
            this.gbTraktAccount.SuspendLayout();
            this.gbPlugins.SuspendLayout();
            this.gbMisc.SuspendLayout();
            this.gbRestrictions.SuspendLayout();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numSyncResumeDelta)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numSyncInterval)).BeginInit();
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
            this.gbTraktAccount.Size = new System.Drawing.Size(316, 97);
            this.gbTraktAccount.TabIndex = 0;
            this.gbTraktAccount.TabStop = false;
            this.gbTraktAccount.Text = "Account";
            // 
            // linkTrakt
            // 
            this.linkTrakt.AutoSize = true;
            this.linkTrakt.Location = new System.Drawing.Point(238, 71);
            this.linkTrakt.Name = "linkTrakt";
            this.linkTrakt.Size = new System.Drawing.Size(71, 13);
            this.linkTrakt.TabIndex = 4;
            this.linkTrakt.TabStop = true;
            this.linkTrakt.Text = "Signup/Login";
            this.linkTrakt.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkTrakt_LinkClicked);
            // 
            // tbPassword
            // 
            this.tbPassword.Location = new System.Drawing.Point(67, 48);
            this.tbPassword.Name = "tbPassword";
            this.tbPassword.PasswordChar = '*';
            this.tbPassword.Size = new System.Drawing.Size(243, 20);
            this.tbPassword.TabIndex = 3;
            this.tbPassword.UseSystemPasswordChar = true;
            this.tbPassword.Enter += new System.EventHandler(this.tbPassword_Enter);
            // 
            // tbUsername
            // 
            this.tbUsername.Location = new System.Drawing.Point(67, 22);
            this.tbUsername.Name = "tbUsername";
            this.tbUsername.Size = new System.Drawing.Size(243, 20);
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
            this.gbPlugins.Size = new System.Drawing.Size(322, 124);
            this.gbPlugins.TabIndex = 1;
            this.gbPlugins.TabStop = false;
            this.gbPlugins.Text = "Plugins";
            // 
            // btnDown
            // 
            this.btnDown.Image = global::TraktPlugin.Properties.Resources.arrow_down;
            this.btnDown.Location = new System.Drawing.Point(287, 53);
            this.btnDown.Name = "btnDown";
            this.btnDown.Size = new System.Drawing.Size(29, 30);
            this.btnDown.TabIndex = 2;
            this.btnDown.UseVisualStyleBackColor = true;
            this.btnDown.Click += new System.EventHandler(this.btnDown_Click);
            // 
            // btnUp
            // 
            this.btnUp.Image = global::TraktPlugin.Properties.Resources.arrow_up;
            this.btnUp.Location = new System.Drawing.Point(287, 19);
            this.btnUp.Name = "btnUp";
            this.btnUp.Size = new System.Drawing.Size(29, 29);
            this.btnUp.TabIndex = 1;
            this.btnUp.UseVisualStyleBackColor = true;
            this.btnUp.Click += new System.EventHandler(this.btnUp_Click);
            // 
            // clbPlugins
            // 
            this.clbPlugins.ColumnWidth = 132;
            this.clbPlugins.FormattingEnabled = true;
            this.clbPlugins.Location = new System.Drawing.Point(9, 19);
            this.clbPlugins.MultiColumn = true;
            this.clbPlugins.Name = "clbPlugins";
            this.clbPlugins.Size = new System.Drawing.Size(272, 94);
            this.clbPlugins.TabIndex = 0;
            this.ttpConfig.SetToolTip(this.clbPlugins, resources.GetString("clbPlugins.ToolTip"));
            // 
            // gbMisc
            // 
            this.gbMisc.Controls.Add(this.label2);
            this.gbMisc.Controls.Add(this.numSyncInterval);
            this.gbMisc.Controls.Add(this.lblSyncResumeDelta);
            this.gbMisc.Controls.Add(this.numSyncResumeDelta);
            this.gbMisc.Controls.Add(this.cbSyncPlayback);
            this.gbMisc.Controls.Add(this.textBox1);
            this.gbMisc.Controls.Add(this.label1);
            this.gbMisc.Controls.Add(this.cbSyncRatings);
            this.gbMisc.Controls.Add(this.cbSyncLibrary);
            this.gbMisc.Controls.Add(this.cbMyFilmsCategories);
            this.gbMisc.Controls.Add(this.cbMovingPicturesFilters);
            this.gbMisc.Controls.Add(this.cbMovingPicturesCategories);
            this.gbMisc.Controls.Add(this.lbSyncTimerLength);
            this.gbMisc.Controls.Add(this.cbKeepInSync);
            this.gbMisc.Location = new System.Drawing.Point(340, 12);
            this.gbMisc.Name = "gbMisc";
            this.gbMisc.Size = new System.Drawing.Size(322, 286);
            this.gbMisc.TabIndex = 3;
            this.gbMisc.TabStop = false;
            this.gbMisc.Text = "Synchronisation";
            // 
            // textBox1
            // 
            this.textBox1.BackColor = System.Drawing.SystemColors.Control;
            this.textBox1.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.textBox1.Location = new System.Drawing.Point(49, 233);
            this.textBox1.Multiline = true;
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(258, 42);
            this.textBox1.TabIndex = 13;
            this.textBox1.TabStop = false;
            this.textBox1.Text = "more options can be found in the GUI Advanced Settings section. The Extensions pl" +
    "ugin must also be installed.";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(7, 234);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(38, 13);
            this.label1.TabIndex = 12;
            this.label1.Text = "Note:";
            // 
            // cbSyncRatings
            // 
            this.cbSyncRatings.AutoSize = true;
            this.cbSyncRatings.Location = new System.Drawing.Point(9, 129);
            this.cbSyncRatings.Name = "cbSyncRatings";
            this.cbSyncRatings.Size = new System.Drawing.Size(123, 17);
            this.cbSyncRatings.TabIndex = 7;
            this.cbSyncRatings.Text = "S&ynchronise Ratings";
            this.ttpConfig.SetToolTip(this.cbSyncRatings, resources.GetString("cbSyncRatings.ToolTip"));
            this.cbSyncRatings.UseVisualStyleBackColor = true;
            this.cbSyncRatings.CheckedChanged += new System.EventHandler(this.cbSyncRatings_CheckedChanged);
            // 
            // cbSyncLibrary
            // 
            this.cbSyncLibrary.AutoSize = true;
            this.cbSyncLibrary.Location = new System.Drawing.Point(9, 108);
            this.cbSyncLibrary.Name = "cbSyncLibrary";
            this.cbSyncLibrary.Size = new System.Drawing.Size(205, 17);
            this.cbSyncLibrary.TabIndex = 6;
            this.cbSyncLibrary.Text = "Sync &Library (Collected and Watched)";
            this.ttpConfig.SetToolTip(this.cbSyncLibrary, "Enable this setting to synchronise your collection and watched states to and from" +
        " trakt.tv. \r\nIf disabled, only scrobbling will be active.");
            this.cbSyncLibrary.UseVisualStyleBackColor = true;
            this.cbSyncLibrary.CheckedChanged += new System.EventHandler(this.cbSyncLibrary_CheckedChanged);
            // 
            // cbMyFilmsCategories
            // 
            this.cbMyFilmsCategories.AutoSize = true;
            this.cbMyFilmsCategories.Location = new System.Drawing.Point(9, 87);
            this.cbMyFilmsCategories.Name = "cbMyFilmsCategories";
            this.cbMyFilmsCategories.Size = new System.Drawing.Size(195, 17);
            this.cbMyFilmsCategories.TabIndex = 5;
            this.cbMyFilmsCategories.Text = "Create My Fi&lms Categories on Sync";
            this.cbMyFilmsCategories.UseVisualStyleBackColor = true;
            this.cbMyFilmsCategories.Click += new System.EventHandler(this.cbMyFilmsCategories_Click);
            // 
            // cbMovingPicturesFilters
            // 
            this.cbMovingPicturesFilters.AutoSize = true;
            this.cbMovingPicturesFilters.Location = new System.Drawing.Point(9, 66);
            this.cbMovingPicturesFilters.Name = "cbMovingPicturesFilters";
            this.cbMovingPicturesFilters.Size = new System.Drawing.Size(208, 17);
            this.cbMovingPicturesFilters.TabIndex = 4;
            this.cbMovingPicturesFilters.Text = "Create Moving Pictures &Filters on Sync";
            this.cbMovingPicturesFilters.UseVisualStyleBackColor = true;
            this.cbMovingPicturesFilters.Click += new System.EventHandler(this.cbMovingPicturesFilters_Click);
            // 
            // cbMovingPicturesCategories
            // 
            this.cbMovingPicturesCategories.AutoSize = true;
            this.cbMovingPicturesCategories.Location = new System.Drawing.Point(9, 45);
            this.cbMovingPicturesCategories.Name = "cbMovingPicturesCategories";
            this.cbMovingPicturesCategories.Size = new System.Drawing.Size(231, 17);
            this.cbMovingPicturesCategories.TabIndex = 3;
            this.cbMovingPicturesCategories.Text = "Create Moving Pictures &Categories on Sync";
            this.cbMovingPicturesCategories.UseVisualStyleBackColor = true;
            this.cbMovingPicturesCategories.Click += new System.EventHandler(this.cbMovingPicturesCategories_Click);
            // 
            // lbSyncTimerLength
            // 
            this.lbSyncTimerLength.AutoSize = true;
            this.lbSyncTimerLength.Location = new System.Drawing.Point(7, 22);
            this.lbSyncTimerLength.Name = "lbSyncTimerLength";
            this.lbSyncTimerLength.Size = new System.Drawing.Size(121, 13);
            this.lbSyncTimerLength.TabIndex = 0;
            this.lbSyncTimerLength.Text = "Sync with trakt.tv every ";
            this.ttpConfig.SetToolTip(this.lbSyncTimerLength, "Set this to the value in hours that you want to wait to resync with Trakt");
            // 
            // btnClearLibrary
            // 
            this.btnClearLibrary.Enabled = false;
            this.btnClearLibrary.Location = new System.Drawing.Point(9, 19);
            this.btnClearLibrary.Name = "btnClearLibrary";
            this.btnClearLibrary.Size = new System.Drawing.Size(307, 23);
            this.btnClearLibrary.TabIndex = 0;
            this.btnClearLibrary.Text = "&Delete all user data from trakt.tv";
            this.ttpConfig.SetToolTip(this.btnClearLibrary, "Click this button to remove all movies and episodes that you have synchronised, m" +
        "arked as watched or rated online.");
            this.btnClearLibrary.UseVisualStyleBackColor = true;
            this.btnClearLibrary.Click += new System.EventHandler(this.btnClearLibrary_Click);
            // 
            // cbKeepInSync
            // 
            this.cbKeepInSync.AutoSize = true;
            this.cbKeepInSync.Location = new System.Drawing.Point(9, 150);
            this.cbKeepInSync.Name = "cbKeepInSync";
            this.cbKeepInSync.Size = new System.Drawing.Size(278, 17);
            this.cbKeepInSync.TabIndex = 8;
            this.cbKeepInSync.Text = "&Remove Collected items if no longer in local database";
            this.ttpConfig.SetToolTip(this.cbKeepInSync, resources.GetString("cbKeepInSync.ToolTip"));
            this.cbKeepInSync.UseVisualStyleBackColor = true;
            this.cbKeepInSync.CheckedChanged += new System.EventHandler(this.cbKeepInSync_CheckedChanged);
            // 
            // btnOK
            // 
            this.btnOK.Location = new System.Drawing.Point(554, 371);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(106, 23);
            this.btnOK.TabIndex = 5;
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
            this.btnTVSeriesRestrictions.Location = new System.Drawing.Point(9, 56);
            this.btnTVSeriesRestrictions.Name = "btnTVSeriesRestrictions";
            this.btnTVSeriesRestrictions.Size = new System.Drawing.Size(149, 23);
            this.btnTVSeriesRestrictions.TabIndex = 1;
            this.btnTVSeriesRestrictions.Text = "&Series...";
            this.ttpConfig.SetToolTip(this.btnTVSeriesRestrictions, "Select the series you want to ignore from Syncronization and Scrobbling.");
            this.btnTVSeriesRestrictions.UseVisualStyleBackColor = true;
            this.btnTVSeriesRestrictions.Click += new System.EventHandler(this.btnTVSeriesRestrictions_Click);
            // 
            // gbRestrictions
            // 
            this.gbRestrictions.Controls.Add(this.textBox2);
            this.gbRestrictions.Controls.Add(this.btnMovieRestrictions);
            this.gbRestrictions.Controls.Add(this.btnTVSeriesRestrictions);
            this.gbRestrictions.Location = new System.Drawing.Point(12, 245);
            this.gbRestrictions.Name = "gbRestrictions";
            this.gbRestrictions.Size = new System.Drawing.Size(322, 120);
            this.gbRestrictions.TabIndex = 2;
            this.gbRestrictions.TabStop = false;
            this.gbRestrictions.Text = "Restrictions";
            // 
            // btnMovieRestrictions
            // 
            this.btnMovieRestrictions.Location = new System.Drawing.Point(164, 56);
            this.btnMovieRestrictions.Name = "btnMovieRestrictions";
            this.btnMovieRestrictions.Size = new System.Drawing.Size(152, 23);
            this.btnMovieRestrictions.TabIndex = 2;
            this.btnMovieRestrictions.Text = "&Movies...";
            this.btnMovieRestrictions.UseVisualStyleBackColor = true;
            this.btnMovieRestrictions.Click += new System.EventHandler(this.btnMovieRestrictions_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.btnClearLibrary);
            this.groupBox1.Location = new System.Drawing.Point(340, 304);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(322, 61);
            this.groupBox1.TabIndex = 4;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Maintenance";
            // 
            // textBox2
            // 
            this.textBox2.BackColor = System.Drawing.SystemColors.Control;
            this.textBox2.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.textBox2.Location = new System.Drawing.Point(11, 19);
            this.textBox2.Multiline = true;
            this.textBox2.Name = "textBox2";
            this.textBox2.Size = new System.Drawing.Size(298, 36);
            this.textBox2.TabIndex = 0;
            this.textBox2.TabStop = false;
            this.textBox2.Text = "Choose what movies and tv shows you would like to ignore during sync and scrobble" +
    " actions:";
            // 
            // cbSyncPlayback
            // 
            this.cbSyncPlayback.AutoSize = true;
            this.cbSyncPlayback.Location = new System.Drawing.Point(9, 171);
            this.cbSyncPlayback.Name = "cbSyncPlayback";
            this.cbSyncPlayback.Size = new System.Drawing.Size(164, 17);
            this.cbSyncPlayback.TabIndex = 9;
            this.cbSyncPlayback.Text = "Sync Playback (resume) data";
            this.cbSyncPlayback.UseVisualStyleBackColor = true;
            this.cbSyncPlayback.CheckedChanged += new System.EventHandler(this.cbSyncPlayback_CheckedChanged);
            // 
            // numSyncResumeDelta
            // 
            this.numSyncResumeDelta.Location = new System.Drawing.Point(239, 190);
            this.numSyncResumeDelta.Maximum = new decimal(new int[] {
            600,
            0,
            0,
            0});
            this.numSyncResumeDelta.Name = "numSyncResumeDelta";
            this.numSyncResumeDelta.Size = new System.Drawing.Size(61, 20);
            this.numSyncResumeDelta.TabIndex = 11;
            this.ttpConfig.SetToolTip(this.numSyncResumeDelta, "You may wish to re-play X seconds from where you left off, this setting \r\nallows " +
        "you control how far back to start when prompted.");
            this.numSyncResumeDelta.ValueChanged += new System.EventHandler(this.numSyncResumeDelta_ValueChanged);
            // 
            // lblSyncResumeDelta
            // 
            this.lblSyncResumeDelta.AutoSize = true;
            this.lblSyncResumeDelta.Location = new System.Drawing.Point(26, 192);
            this.lblSyncResumeDelta.Name = "lblSyncResumeDelta";
            this.lblSyncResumeDelta.Size = new System.Drawing.Size(197, 13);
            this.lblSyncResumeDelta.TabIndex = 10;
            this.lblSyncResumeDelta.Text = "Delta in seconds to apply to resume time";
            // 
            // numSyncInterval
            // 
            this.numSyncInterval.Location = new System.Drawing.Point(129, 19);
            this.numSyncInterval.Maximum = new decimal(new int[] {
            168,
            0,
            0,
            0});
            this.numSyncInterval.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numSyncInterval.Name = "numSyncInterval";
            this.numSyncInterval.Size = new System.Drawing.Size(55, 20);
            this.numSyncInterval.TabIndex = 1;
            this.ttpConfig.SetToolTip(this.numSyncInterval, "Enter the period in hours to sync with trakt.tv for your selected \r\nsync options " +
        "(Collection, Ratings, Watched etc).");
            this.numSyncInterval.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numSyncInterval.ValueChanged += new System.EventHandler(this.numSyncInterval_ValueChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(191, 22);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(33, 13);
            this.label2.TabIndex = 2;
            this.label2.Text = "hours";
            // 
            // Configuration
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(672, 400);
            this.Controls.Add(this.groupBox1);
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
            this.Text = "Trakt Configuration v10.0.0.0";
            this.gbTraktAccount.ResumeLayout(false);
            this.gbTraktAccount.PerformLayout();
            this.gbPlugins.ResumeLayout(false);
            this.gbMisc.ResumeLayout(false);
            this.gbMisc.PerformLayout();
            this.gbRestrictions.ResumeLayout(false);
            this.gbRestrictions.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.numSyncResumeDelta)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numSyncInterval)).EndInit();
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
        private System.Windows.Forms.Label lbSyncTimerLength;
        private System.Windows.Forms.LinkLabel linkTrakt;
        private System.Windows.Forms.CheckBox cbMovingPicturesCategories;
        private System.Windows.Forms.CheckBox cbMovingPicturesFilters;
        private System.Windows.Forms.CheckBox cbMyFilmsCategories;
        private System.Windows.Forms.CheckBox cbSyncLibrary;
        private System.Windows.Forms.CheckBox cbSyncRatings;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox textBox2;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label lblSyncResumeDelta;
        private System.Windows.Forms.NumericUpDown numSyncResumeDelta;
        private System.Windows.Forms.CheckBox cbSyncPlayback;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.NumericUpDown numSyncInterval;
    }
}