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
            this.gbSync = new System.Windows.Forms.GroupBox();
            this.cbSyncPlaybackOnEnterPlugin = new System.Windows.Forms.CheckBox();
            this.label2 = new System.Windows.Forms.Label();
            this.numSyncInterval = new System.Windows.Forms.NumericUpDown();
            this.lblSyncResumeDelta = new System.Windows.Forms.Label();
            this.numSyncResumeDelta = new System.Windows.Forms.NumericUpDown();
            this.cbSyncPlayback = new System.Windows.Forms.CheckBox();
            this.cbSyncRatings = new System.Windows.Forms.CheckBox();
            this.cbSyncLibrary = new System.Windows.Forms.CheckBox();
            this.cbMyFilmsCategories = new System.Windows.Forms.CheckBox();
            this.cbMovingPicturesFilters = new System.Windows.Forms.CheckBox();
            this.cbMovingPicturesCategories = new System.Windows.Forms.CheckBox();
            this.lbSyncTimerLength = new System.Windows.Forms.Label();
            this.cbKeepInSync = new System.Windows.Forms.CheckBox();
            this.btnOK = new System.Windows.Forms.Button();
            this.ttpConfig = new System.Windows.Forms.ToolTip(this.components);
            this.btnTVSeriesRestrictions = new System.Windows.Forms.Button();
            this.cbParentControls = new System.Windows.Forms.CheckBox();
            this.txtPinCode = new System.Windows.Forms.TextBox();
            this.gbRestrictions = new System.Windows.Forms.GroupBox();
            this.textBox2 = new System.Windows.Forms.TextBox();
            this.btnMovieRestrictions = new System.Windows.Forms.Button();
            this.progressBarSync = new System.Windows.Forms.ProgressBar();
            this.lblSyncStatus = new System.Windows.Forms.Label();
            this.btnStartLibrarySync = new System.Windows.Forms.Button();
            this.dtParentalControlsTime = new System.Windows.Forms.DateTimePicker();
            this.cbParentalControlsTime = new System.Windows.Forms.CheckBox();
            this.gbParentalControls = new System.Windows.Forms.GroupBox();
            this.cboMovieCertifications = new System.Windows.Forms.ComboBox();
            this.cbParentalIgnoreMovieCertifications = new System.Windows.Forms.CheckBox();
            this.cboTVCertifications = new System.Windows.Forms.ComboBox();
            this.cbParentalIgnoreShowCertifications = new System.Windows.Forms.CheckBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.label1 = new System.Windows.Forms.Label();
            this.cboPreferredImageLanguage = new System.Windows.Forms.ComboBox();
            this.gbTraktAccount.SuspendLayout();
            this.gbPlugins.SuspendLayout();
            this.gbSync.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numSyncInterval)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numSyncResumeDelta)).BeginInit();
            this.gbRestrictions.SuspendLayout();
            this.gbParentalControls.SuspendLayout();
            this.groupBox1.SuspendLayout();
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
            // gbSync
            // 
            this.gbSync.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.gbSync.Controls.Add(this.cbSyncPlaybackOnEnterPlugin);
            this.gbSync.Controls.Add(this.label2);
            this.gbSync.Controls.Add(this.numSyncInterval);
            this.gbSync.Controls.Add(this.lblSyncResumeDelta);
            this.gbSync.Controls.Add(this.numSyncResumeDelta);
            this.gbSync.Controls.Add(this.cbSyncPlayback);
            this.gbSync.Controls.Add(this.cbSyncRatings);
            this.gbSync.Controls.Add(this.cbSyncLibrary);
            this.gbSync.Controls.Add(this.cbMyFilmsCategories);
            this.gbSync.Controls.Add(this.cbMovingPicturesFilters);
            this.gbSync.Controls.Add(this.cbMovingPicturesCategories);
            this.gbSync.Controls.Add(this.lbSyncTimerLength);
            this.gbSync.Controls.Add(this.cbKeepInSync);
            this.gbSync.Location = new System.Drawing.Point(340, 12);
            this.gbSync.Name = "gbSync";
            this.gbSync.Size = new System.Drawing.Size(322, 245);
            this.gbSync.TabIndex = 3;
            this.gbSync.TabStop = false;
            this.gbSync.Text = "Synchronisation";
            // 
            // cbSyncPlaybackOnEnterPlugin
            // 
            this.cbSyncPlaybackOnEnterPlugin.AutoSize = true;
            this.cbSyncPlaybackOnEnterPlugin.Location = new System.Drawing.Point(9, 192);
            this.cbSyncPlaybackOnEnterPlugin.Name = "cbSyncPlaybackOnEnterPlugin";
            this.cbSyncPlaybackOnEnterPlugin.Size = new System.Drawing.Size(245, 17);
            this.cbSyncPlaybackOnEnterPlugin.TabIndex = 10;
            this.cbSyncPlaybackOnEnterPlugin.Text = "Sync Playback when entering enabled Plugins";
            this.ttpConfig.SetToolTip(this.cbSyncPlaybackOnEnterPlugin, "Sync playback/resume data when entering an enabled plugin.\r\nThis is in addition t" +
        "o syncing when the system starts up and resumes from standby.");
            this.cbSyncPlaybackOnEnterPlugin.UseVisualStyleBackColor = true;
            this.cbSyncPlaybackOnEnterPlugin.CheckedChanged += new System.EventHandler(this.cbSyncPlaybackOnEnterPlugin_CheckedChanged);
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
            // lblSyncResumeDelta
            // 
            this.lblSyncResumeDelta.AutoSize = true;
            this.lblSyncResumeDelta.Location = new System.Drawing.Point(7, 214);
            this.lblSyncResumeDelta.Name = "lblSyncResumeDelta";
            this.lblSyncResumeDelta.Size = new System.Drawing.Size(197, 13);
            this.lblSyncResumeDelta.TabIndex = 11;
            this.lblSyncResumeDelta.Text = "Delta in seconds to apply to resume time";
            // 
            // numSyncResumeDelta
            // 
            this.numSyncResumeDelta.Location = new System.Drawing.Point(252, 212);
            this.numSyncResumeDelta.Maximum = new decimal(new int[] {
            600,
            0,
            0,
            0});
            this.numSyncResumeDelta.Name = "numSyncResumeDelta";
            this.numSyncResumeDelta.Size = new System.Drawing.Size(61, 20);
            this.numSyncResumeDelta.TabIndex = 12;
            this.ttpConfig.SetToolTip(this.numSyncResumeDelta, "You may wish to re-play X seconds from where you left off, this setting \r\nallows " +
        "you control how far back to start when prompted.");
            this.numSyncResumeDelta.ValueChanged += new System.EventHandler(this.numSyncResumeDelta_ValueChanged);
            // 
            // cbSyncPlayback
            // 
            this.cbSyncPlayback.AutoSize = true;
            this.cbSyncPlayback.Location = new System.Drawing.Point(9, 171);
            this.cbSyncPlayback.Name = "cbSyncPlayback";
            this.cbSyncPlayback.Size = new System.Drawing.Size(260, 17);
            this.cbSyncPlayback.TabIndex = 9;
            this.cbSyncPlayback.Text = "Sync Playback (resume) data on Startup/Resume";
            this.ttpConfig.SetToolTip(this.cbSyncPlayback, "Sync playback / resume data for partially watched videos. This allows\r\nyou to con" +
        "tinue where you left off.\r\n\r\nThis is done when the system starts up or when you " +
        "resume from standby.");
            this.cbSyncPlayback.UseVisualStyleBackColor = true;
            this.cbSyncPlayback.CheckedChanged += new System.EventHandler(this.cbSyncPlayback_CheckedChanged);
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
            this.btnOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOK.Location = new System.Drawing.Point(556, 466);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(106, 23);
            this.btnOK.TabIndex = 8;
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
            // cbParentControls
            // 
            this.cbParentControls.AutoSize = true;
            this.cbParentControls.Location = new System.Drawing.Point(10, 19);
            this.cbParentControls.Name = "cbParentControls";
            this.cbParentControls.Size = new System.Drawing.Size(191, 17);
            this.cbParentControls.TabIndex = 0;
            this.cbParentControls.Text = "Enable Parental Controls Pin Code:";
            this.ttpConfig.SetToolTip(this.cbParentControls, "When enabled, will prevent playback of inappropriate material.");
            this.cbParentControls.UseVisualStyleBackColor = true;
            this.cbParentControls.CheckedChanged += new System.EventHandler(this.cbParentControls_CheckedChanged);
            // 
            // txtPinCode
            // 
            this.txtPinCode.Location = new System.Drawing.Point(252, 16);
            this.txtPinCode.MaxLength = 4;
            this.txtPinCode.Name = "txtPinCode";
            this.txtPinCode.PasswordChar = '*';
            this.txtPinCode.Size = new System.Drawing.Size(62, 20);
            this.txtPinCode.TabIndex = 1;
            this.ttpConfig.SetToolTip(this.txtPinCode, "Enter in a 4-digit pin code to prevent playback of inappropriate material.");
            this.txtPinCode.TextChanged += new System.EventHandler(this.txtPinCode_TextChanged);
            this.txtPinCode.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtPinCode_KeyPress);
            // 
            // gbRestrictions
            // 
            this.gbRestrictions.Controls.Add(this.textBox2);
            this.gbRestrictions.Controls.Add(this.btnMovieRestrictions);
            this.gbRestrictions.Controls.Add(this.btnTVSeriesRestrictions);
            this.gbRestrictions.Location = new System.Drawing.Point(12, 245);
            this.gbRestrictions.Name = "gbRestrictions";
            this.gbRestrictions.Size = new System.Drawing.Size(322, 89);
            this.gbRestrictions.TabIndex = 2;
            this.gbRestrictions.TabStop = false;
            this.gbRestrictions.Text = "Restrictions";
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
            // progressBarSync
            // 
            this.progressBarSync.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.progressBarSync.Location = new System.Drawing.Point(12, 401);
            this.progressBarSync.Name = "progressBarSync";
            this.progressBarSync.Size = new System.Drawing.Size(650, 23);
            this.progressBarSync.TabIndex = 5;
            // 
            // lblSyncStatus
            // 
            this.lblSyncStatus.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblSyncStatus.AutoEllipsis = true;
            this.lblSyncStatus.Location = new System.Drawing.Point(12, 469);
            this.lblSyncStatus.Name = "lblSyncStatus";
            this.lblSyncStatus.Size = new System.Drawing.Size(538, 14);
            this.lblSyncStatus.TabIndex = 7;
            this.lblSyncStatus.Text = "Ready for anything!";
            // 
            // btnStartLibrarySync
            // 
            this.btnStartLibrarySync.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btnStartLibrarySync.Location = new System.Drawing.Point(12, 430);
            this.btnStartLibrarySync.Name = "btnStartLibrarySync";
            this.btnStartLibrarySync.Size = new System.Drawing.Size(650, 24);
            this.btnStartLibrarySync.TabIndex = 6;
            this.btnStartLibrarySync.Text = "Start Library and Playback Sync";
            this.btnStartLibrarySync.UseVisualStyleBackColor = true;
            this.btnStartLibrarySync.Click += new System.EventHandler(this.btnStartLibrarySync_Click);
            // 
            // dtParentalControlsTime
            // 
            this.dtParentalControlsTime.Format = System.Windows.Forms.DateTimePickerFormat.Time;
            this.dtParentalControlsTime.Location = new System.Drawing.Point(226, 38);
            this.dtParentalControlsTime.Name = "dtParentalControlsTime";
            this.dtParentalControlsTime.ShowUpDown = true;
            this.dtParentalControlsTime.Size = new System.Drawing.Size(88, 20);
            this.dtParentalControlsTime.TabIndex = 3;
            this.dtParentalControlsTime.Value = new System.DateTime(2016, 3, 25, 21, 0, 0, 0);
            this.dtParentalControlsTime.ValueChanged += new System.EventHandler(this.dtParentalControlsTime_ValueChanged);
            // 
            // cbParentalControlsTime
            // 
            this.cbParentalControlsTime.AutoSize = true;
            this.cbParentalControlsTime.Location = new System.Drawing.Point(10, 43);
            this.cbParentalControlsTime.Name = "cbParentalControlsTime";
            this.cbParentalControlsTime.Size = new System.Drawing.Size(163, 17);
            this.cbParentalControlsTime.TabIndex = 2;
            this.cbParentalControlsTime.Text = "Ignore Parental Controls after";
            this.cbParentalControlsTime.UseVisualStyleBackColor = true;
            this.cbParentalControlsTime.CheckedChanged += new System.EventHandler(this.cbParentalControlsTime_CheckedChanged);
            // 
            // gbParentalControls
            // 
            this.gbParentalControls.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.gbParentalControls.Controls.Add(this.cboMovieCertifications);
            this.gbParentalControls.Controls.Add(this.cbParentalIgnoreMovieCertifications);
            this.gbParentalControls.Controls.Add(this.cboTVCertifications);
            this.gbParentalControls.Controls.Add(this.cbParentalIgnoreShowCertifications);
            this.gbParentalControls.Controls.Add(this.cbParentalControlsTime);
            this.gbParentalControls.Controls.Add(this.cbParentControls);
            this.gbParentalControls.Controls.Add(this.dtParentalControlsTime);
            this.gbParentalControls.Controls.Add(this.txtPinCode);
            this.gbParentalControls.Location = new System.Drawing.Point(340, 264);
            this.gbParentalControls.Name = "gbParentalControls";
            this.gbParentalControls.Size = new System.Drawing.Size(320, 126);
            this.gbParentalControls.TabIndex = 4;
            this.gbParentalControls.TabStop = false;
            this.gbParentalControls.Text = "Parental Controls";
            // 
            // cboMovieCertifications
            // 
            this.cboMovieCertifications.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboMovieCertifications.FormattingEnabled = true;
            this.cboMovieCertifications.Items.AddRange(new object[] {
            "G",
            "PG",
            "PG-13",
            "R"});
            this.cboMovieCertifications.Location = new System.Drawing.Point(252, 83);
            this.cboMovieCertifications.Name = "cboMovieCertifications";
            this.cboMovieCertifications.Size = new System.Drawing.Size(61, 21);
            this.cboMovieCertifications.TabIndex = 7;
            this.cboMovieCertifications.SelectedValueChanged += new System.EventHandler(this.cboMovieCertifications_SelectedValueChanged);
            // 
            // cbParentalIgnoreMovieCertifications
            // 
            this.cbParentalIgnoreMovieCertifications.AutoSize = true;
            this.cbParentalIgnoreMovieCertifications.Location = new System.Drawing.Point(10, 90);
            this.cbParentalIgnoreMovieCertifications.Name = "cbParentalIgnoreMovieCertifications";
            this.cbParentalIgnoreMovieCertifications.Size = new System.Drawing.Size(188, 17);
            this.cbParentalIgnoreMovieCertifications.TabIndex = 6;
            this.cbParentalIgnoreMovieCertifications.Text = "Ignore on Movies with Certification";
            this.cbParentalIgnoreMovieCertifications.UseVisualStyleBackColor = true;
            this.cbParentalIgnoreMovieCertifications.CheckedChanged += new System.EventHandler(this.cbParentalIgnoreMovieCertifications_CheckedChanged);
            // 
            // cboTVCertifications
            // 
            this.cboTVCertifications.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboTVCertifications.FormattingEnabled = true;
            this.cboTVCertifications.Items.AddRange(new object[] {
            "TV-Y",
            "TV-Y7",
            "TV-G",
            "TV-PG",
            "TV-14",
            "TV-M"});
            this.cboTVCertifications.Location = new System.Drawing.Point(252, 60);
            this.cboTVCertifications.Name = "cboTVCertifications";
            this.cboTVCertifications.Size = new System.Drawing.Size(61, 21);
            this.cboTVCertifications.TabIndex = 5;
            this.cboTVCertifications.SelectedValueChanged += new System.EventHandler(this.cboTVCertifications_SelectedValueChanged);
            // 
            // cbParentalIgnoreShowCertifications
            // 
            this.cbParentalIgnoreShowCertifications.AutoSize = true;
            this.cbParentalIgnoreShowCertifications.Location = new System.Drawing.Point(10, 66);
            this.cbParentalIgnoreShowCertifications.Name = "cbParentalIgnoreShowCertifications";
            this.cbParentalIgnoreShowCertifications.Size = new System.Drawing.Size(186, 17);
            this.cbParentalIgnoreShowCertifications.TabIndex = 4;
            this.cbParentalIgnoreShowCertifications.Text = "Ignore on Shows with Certification";
            this.cbParentalIgnoreShowCertifications.UseVisualStyleBackColor = true;
            this.cbParentalIgnoreShowCertifications.CheckedChanged += new System.EventHandler(this.cbParentalIgnoreShowCertifications_CheckedChanged);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.cboPreferredImageLanguage);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Location = new System.Drawing.Point(12, 341);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(322, 49);
            this.groupBox1.TabIndex = 9;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Images";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(11, 20);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(104, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Preferred Language:";
            // 
            // cboPreferredImageLanguage
            // 
            this.cboPreferredImageLanguage.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboPreferredImageLanguage.FormattingEnabled = true;
            this.cboPreferredImageLanguage.Location = new System.Drawing.Point(122, 16);
            this.cboPreferredImageLanguage.Name = "cboPreferredImageLanguage";
            this.cboPreferredImageLanguage.Size = new System.Drawing.Size(194, 21);
            this.cboPreferredImageLanguage.TabIndex = 1;
            this.cboPreferredImageLanguage.SelectedIndexChanged += new System.EventHandler(this.cboPreferredImageLanguage_SelectedIndexChanged);
            // 
            // Configuration
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(672, 501);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.gbParentalControls);
            this.Controls.Add(this.btnStartLibrarySync);
            this.Controls.Add(this.lblSyncStatus);
            this.Controls.Add(this.progressBarSync);
            this.Controls.Add(this.gbRestrictions);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.gbSync);
            this.Controls.Add(this.gbPlugins);
            this.Controls.Add(this.gbTraktAccount);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(688, 539);
            this.Name = "Configuration";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Trakt Configuration v10.0.0.0";
            this.Load += new System.EventHandler(this.Configuration_Load);
            this.gbTraktAccount.ResumeLayout(false);
            this.gbTraktAccount.PerformLayout();
            this.gbPlugins.ResumeLayout(false);
            this.gbSync.ResumeLayout(false);
            this.gbSync.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numSyncInterval)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numSyncResumeDelta)).EndInit();
            this.gbRestrictions.ResumeLayout(false);
            this.gbRestrictions.PerformLayout();
            this.gbParentalControls.ResumeLayout(false);
            this.gbParentalControls.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox gbTraktAccount;
        private System.Windows.Forms.Label lbUsername;
        private System.Windows.Forms.TextBox tbPassword;
        private System.Windows.Forms.TextBox tbUsername;
        private System.Windows.Forms.Label lbPassword;
        private System.Windows.Forms.GroupBox gbPlugins;
        private System.Windows.Forms.GroupBox gbSync;
        private System.Windows.Forms.CheckBox cbKeepInSync;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button btnDown;
        private System.Windows.Forms.Button btnUp;
        private System.Windows.Forms.CheckedListBox clbPlugins;
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
        private System.Windows.Forms.TextBox textBox2;
        private System.Windows.Forms.Label lblSyncResumeDelta;
        private System.Windows.Forms.NumericUpDown numSyncResumeDelta;
        private System.Windows.Forms.CheckBox cbSyncPlayback;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.NumericUpDown numSyncInterval;
        private System.Windows.Forms.ProgressBar progressBarSync;
        private System.Windows.Forms.Button btnStartLibrarySync;
        public System.Windows.Forms.Label lblSyncStatus;
        private System.Windows.Forms.CheckBox cbSyncPlaybackOnEnterPlugin;
        private System.Windows.Forms.TextBox txtPinCode;
        private System.Windows.Forms.CheckBox cbParentControls;
        private System.Windows.Forms.DateTimePicker dtParentalControlsTime;
        private System.Windows.Forms.CheckBox cbParentalControlsTime;
        private System.Windows.Forms.GroupBox gbParentalControls;
        private System.Windows.Forms.ComboBox cboMovieCertifications;
        private System.Windows.Forms.CheckBox cbParentalIgnoreMovieCertifications;
        private System.Windows.Forms.ComboBox cboTVCertifications;
        private System.Windows.Forms.CheckBox cbParentalIgnoreShowCertifications;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.ComboBox cboPreferredImageLanguage;
        private System.Windows.Forms.Label label1;
    }
}