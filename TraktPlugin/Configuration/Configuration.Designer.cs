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
            this.gbImages = new System.Windows.Forms.GroupBox();
            this.cboPreferredImageLanguage = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.btnAuthoriseApplication = new System.Windows.Forms.Button();
            this.lblAuthorizeApplication = new System.Windows.Forms.Label();
            this.gbTraktAccount.SuspendLayout();
            this.gbPlugins.SuspendLayout();
            this.gbSync.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numSyncInterval)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numSyncResumeDelta)).BeginInit();
            this.gbRestrictions.SuspendLayout();
            this.gbParentalControls.SuspendLayout();
            this.gbImages.SuspendLayout();
            this.SuspendLayout();
            // 
            // gbTraktAccount
            // 
            this.gbTraktAccount.Controls.Add(this.linkTrakt);
            this.gbTraktAccount.Controls.Add(this.lblAuthorizeApplication);
            this.gbTraktAccount.Controls.Add(this.btnAuthoriseApplication);
            this.gbTraktAccount.Location = new System.Drawing.Point(18, 18);
            this.gbTraktAccount.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.gbTraktAccount.Name = "gbTraktAccount";
            this.gbTraktAccount.Padding = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.gbTraktAccount.Size = new System.Drawing.Size(474, 149);
            this.gbTraktAccount.TabIndex = 0;
            this.gbTraktAccount.TabStop = false;
            this.gbTraktAccount.Text = "Account";
            // 
            // linkTrakt
            // 
            this.linkTrakt.AutoSize = true;
            this.linkTrakt.Location = new System.Drawing.Point(402, 112);
            this.linkTrakt.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.linkTrakt.Name = "linkTrakt";
            this.linkTrakt.Size = new System.Drawing.Size(57, 20);
            this.linkTrakt.TabIndex = 4;
            this.linkTrakt.TabStop = true;
            this.linkTrakt.Text = "trakt.tv";
            this.linkTrakt.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkTrakt_LinkClicked);
            // 
            // gbPlugins
            // 
            this.gbPlugins.Controls.Add(this.btnDown);
            this.gbPlugins.Controls.Add(this.btnUp);
            this.gbPlugins.Controls.Add(this.clbPlugins);
            this.gbPlugins.Location = new System.Drawing.Point(18, 177);
            this.gbPlugins.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.gbPlugins.Name = "gbPlugins";
            this.gbPlugins.Padding = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.gbPlugins.Size = new System.Drawing.Size(483, 191);
            this.gbPlugins.TabIndex = 1;
            this.gbPlugins.TabStop = false;
            this.gbPlugins.Text = "Plugins";
            // 
            // btnDown
            // 
            this.btnDown.Image = global::TraktPlugin.Properties.Resources.arrow_down;
            this.btnDown.Location = new System.Drawing.Point(430, 82);
            this.btnDown.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btnDown.Name = "btnDown";
            this.btnDown.Size = new System.Drawing.Size(44, 46);
            this.btnDown.TabIndex = 2;
            this.btnDown.UseVisualStyleBackColor = true;
            this.btnDown.Click += new System.EventHandler(this.btnDown_Click);
            // 
            // btnUp
            // 
            this.btnUp.Image = global::TraktPlugin.Properties.Resources.arrow_up;
            this.btnUp.Location = new System.Drawing.Point(430, 29);
            this.btnUp.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btnUp.Name = "btnUp";
            this.btnUp.Size = new System.Drawing.Size(44, 45);
            this.btnUp.TabIndex = 1;
            this.btnUp.UseVisualStyleBackColor = true;
            this.btnUp.Click += new System.EventHandler(this.btnUp_Click);
            // 
            // clbPlugins
            // 
            this.clbPlugins.ColumnWidth = 132;
            this.clbPlugins.FormattingEnabled = true;
            this.clbPlugins.Location = new System.Drawing.Point(14, 29);
            this.clbPlugins.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.clbPlugins.MultiColumn = true;
            this.clbPlugins.Name = "clbPlugins";
            this.clbPlugins.Size = new System.Drawing.Size(406, 130);
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
            this.gbSync.Location = new System.Drawing.Point(510, 18);
            this.gbSync.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.gbSync.Name = "gbSync";
            this.gbSync.Padding = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.gbSync.Size = new System.Drawing.Size(483, 377);
            this.gbSync.TabIndex = 3;
            this.gbSync.TabStop = false;
            this.gbSync.Text = "Synchronisation";
            // 
            // cbSyncPlaybackOnEnterPlugin
            // 
            this.cbSyncPlaybackOnEnterPlugin.AutoSize = true;
            this.cbSyncPlaybackOnEnterPlugin.Location = new System.Drawing.Point(14, 295);
            this.cbSyncPlaybackOnEnterPlugin.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.cbSyncPlaybackOnEnterPlugin.Name = "cbSyncPlaybackOnEnterPlugin";
            this.cbSyncPlaybackOnEnterPlugin.Size = new System.Drawing.Size(357, 24);
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
            this.label2.Location = new System.Drawing.Point(286, 34);
            this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(49, 20);
            this.label2.TabIndex = 2;
            this.label2.Text = "hours";
            // 
            // numSyncInterval
            // 
            this.numSyncInterval.Location = new System.Drawing.Point(194, 29);
            this.numSyncInterval.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
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
            this.numSyncInterval.Size = new System.Drawing.Size(82, 26);
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
            this.lblSyncResumeDelta.Location = new System.Drawing.Point(10, 329);
            this.lblSyncResumeDelta.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblSyncResumeDelta.Name = "lblSyncResumeDelta";
            this.lblSyncResumeDelta.Size = new System.Drawing.Size(295, 20);
            this.lblSyncResumeDelta.TabIndex = 11;
            this.lblSyncResumeDelta.Text = "Delta in seconds to apply to resume time";
            // 
            // numSyncResumeDelta
            // 
            this.numSyncResumeDelta.Location = new System.Drawing.Point(378, 326);
            this.numSyncResumeDelta.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.numSyncResumeDelta.Maximum = new decimal(new int[] {
            600,
            0,
            0,
            0});
            this.numSyncResumeDelta.Name = "numSyncResumeDelta";
            this.numSyncResumeDelta.Size = new System.Drawing.Size(92, 26);
            this.numSyncResumeDelta.TabIndex = 12;
            this.ttpConfig.SetToolTip(this.numSyncResumeDelta, "You may wish to re-play X seconds from where you left off, this setting \r\nallows " +
        "you control how far back to start when prompted.");
            this.numSyncResumeDelta.ValueChanged += new System.EventHandler(this.numSyncResumeDelta_ValueChanged);
            // 
            // cbSyncPlayback
            // 
            this.cbSyncPlayback.AutoSize = true;
            this.cbSyncPlayback.Location = new System.Drawing.Point(14, 263);
            this.cbSyncPlayback.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.cbSyncPlayback.Name = "cbSyncPlayback";
            this.cbSyncPlayback.Size = new System.Drawing.Size(383, 24);
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
            this.cbSyncRatings.Location = new System.Drawing.Point(14, 198);
            this.cbSyncRatings.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.cbSyncRatings.Name = "cbSyncRatings";
            this.cbSyncRatings.Size = new System.Drawing.Size(181, 24);
            this.cbSyncRatings.TabIndex = 7;
            this.cbSyncRatings.Text = "S&ynchronise Ratings";
            this.ttpConfig.SetToolTip(this.cbSyncRatings, resources.GetString("cbSyncRatings.ToolTip"));
            this.cbSyncRatings.UseVisualStyleBackColor = true;
            this.cbSyncRatings.CheckedChanged += new System.EventHandler(this.cbSyncRatings_CheckedChanged);
            // 
            // cbSyncLibrary
            // 
            this.cbSyncLibrary.AutoSize = true;
            this.cbSyncLibrary.Location = new System.Drawing.Point(14, 166);
            this.cbSyncLibrary.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.cbSyncLibrary.Name = "cbSyncLibrary";
            this.cbSyncLibrary.Size = new System.Drawing.Size(300, 24);
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
            this.cbMyFilmsCategories.Location = new System.Drawing.Point(14, 134);
            this.cbMyFilmsCategories.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.cbMyFilmsCategories.Name = "cbMyFilmsCategories";
            this.cbMyFilmsCategories.Size = new System.Drawing.Size(290, 24);
            this.cbMyFilmsCategories.TabIndex = 5;
            this.cbMyFilmsCategories.Text = "Create My Fi&lms Categories on Sync";
            this.cbMyFilmsCategories.UseVisualStyleBackColor = true;
            this.cbMyFilmsCategories.Click += new System.EventHandler(this.cbMyFilmsCategories_Click);
            // 
            // cbMovingPicturesFilters
            // 
            this.cbMovingPicturesFilters.AutoSize = true;
            this.cbMovingPicturesFilters.Location = new System.Drawing.Point(14, 102);
            this.cbMovingPicturesFilters.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.cbMovingPicturesFilters.Name = "cbMovingPicturesFilters";
            this.cbMovingPicturesFilters.Size = new System.Drawing.Size(306, 24);
            this.cbMovingPicturesFilters.TabIndex = 4;
            this.cbMovingPicturesFilters.Text = "Create Moving Pictures &Filters on Sync";
            this.cbMovingPicturesFilters.UseVisualStyleBackColor = true;
            this.cbMovingPicturesFilters.Click += new System.EventHandler(this.cbMovingPicturesFilters_Click);
            // 
            // cbMovingPicturesCategories
            // 
            this.cbMovingPicturesCategories.AutoSize = true;
            this.cbMovingPicturesCategories.Location = new System.Drawing.Point(14, 69);
            this.cbMovingPicturesCategories.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.cbMovingPicturesCategories.Name = "cbMovingPicturesCategories";
            this.cbMovingPicturesCategories.Size = new System.Drawing.Size(340, 24);
            this.cbMovingPicturesCategories.TabIndex = 3;
            this.cbMovingPicturesCategories.Text = "Create Moving Pictures &Categories on Sync";
            this.cbMovingPicturesCategories.UseVisualStyleBackColor = true;
            this.cbMovingPicturesCategories.Click += new System.EventHandler(this.cbMovingPicturesCategories_Click);
            // 
            // lbSyncTimerLength
            // 
            this.lbSyncTimerLength.AutoSize = true;
            this.lbSyncTimerLength.Location = new System.Drawing.Point(10, 34);
            this.lbSyncTimerLength.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lbSyncTimerLength.Name = "lbSyncTimerLength";
            this.lbSyncTimerLength.Size = new System.Drawing.Size(173, 20);
            this.lbSyncTimerLength.TabIndex = 0;
            this.lbSyncTimerLength.Text = "Sync with trakt.tv every ";
            this.ttpConfig.SetToolTip(this.lbSyncTimerLength, "Set this to the value in hours that you want to wait to resync with Trakt");
            // 
            // cbKeepInSync
            // 
            this.cbKeepInSync.AutoSize = true;
            this.cbKeepInSync.Location = new System.Drawing.Point(14, 231);
            this.cbKeepInSync.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.cbKeepInSync.Name = "cbKeepInSync";
            this.cbKeepInSync.Size = new System.Drawing.Size(411, 24);
            this.cbKeepInSync.TabIndex = 8;
            this.cbKeepInSync.Text = "&Remove Collected items if no longer in local database";
            this.ttpConfig.SetToolTip(this.cbKeepInSync, resources.GetString("cbKeepInSync.ToolTip"));
            this.cbKeepInSync.UseVisualStyleBackColor = true;
            this.cbKeepInSync.CheckedChanged += new System.EventHandler(this.cbKeepInSync_CheckedChanged);
            // 
            // btnOK
            // 
            this.btnOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOK.Location = new System.Drawing.Point(834, 717);
            this.btnOK.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(159, 35);
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
            this.btnTVSeriesRestrictions.Location = new System.Drawing.Point(14, 86);
            this.btnTVSeriesRestrictions.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btnTVSeriesRestrictions.Name = "btnTVSeriesRestrictions";
            this.btnTVSeriesRestrictions.Size = new System.Drawing.Size(224, 35);
            this.btnTVSeriesRestrictions.TabIndex = 1;
            this.btnTVSeriesRestrictions.Text = "&Series...";
            this.ttpConfig.SetToolTip(this.btnTVSeriesRestrictions, "Select the series you want to ignore from Syncronization and Scrobbling.");
            this.btnTVSeriesRestrictions.UseVisualStyleBackColor = true;
            this.btnTVSeriesRestrictions.Click += new System.EventHandler(this.btnTVSeriesRestrictions_Click);
            // 
            // cbParentControls
            // 
            this.cbParentControls.AutoSize = true;
            this.cbParentControls.Location = new System.Drawing.Point(15, 29);
            this.cbParentControls.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.cbParentControls.Name = "cbParentControls";
            this.cbParentControls.Size = new System.Drawing.Size(283, 24);
            this.cbParentControls.TabIndex = 0;
            this.cbParentControls.Text = "Enable Parental Controls Pin Code:";
            this.ttpConfig.SetToolTip(this.cbParentControls, "When enabled, will prevent playback of inappropriate material.");
            this.cbParentControls.UseVisualStyleBackColor = true;
            this.cbParentControls.CheckedChanged += new System.EventHandler(this.cbParentControls_CheckedChanged);
            // 
            // txtPinCode
            // 
            this.txtPinCode.Location = new System.Drawing.Point(378, 25);
            this.txtPinCode.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.txtPinCode.MaxLength = 4;
            this.txtPinCode.Name = "txtPinCode";
            this.txtPinCode.PasswordChar = '*';
            this.txtPinCode.Size = new System.Drawing.Size(91, 26);
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
            this.gbRestrictions.Location = new System.Drawing.Point(18, 377);
            this.gbRestrictions.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.gbRestrictions.Name = "gbRestrictions";
            this.gbRestrictions.Padding = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.gbRestrictions.Size = new System.Drawing.Size(483, 137);
            this.gbRestrictions.TabIndex = 2;
            this.gbRestrictions.TabStop = false;
            this.gbRestrictions.Text = "Restrictions";
            // 
            // textBox2
            // 
            this.textBox2.BackColor = System.Drawing.SystemColors.Control;
            this.textBox2.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.textBox2.Location = new System.Drawing.Point(16, 29);
            this.textBox2.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.textBox2.Multiline = true;
            this.textBox2.Name = "textBox2";
            this.textBox2.Size = new System.Drawing.Size(447, 55);
            this.textBox2.TabIndex = 0;
            this.textBox2.TabStop = false;
            this.textBox2.Text = "Choose what movies and tv shows you would like to ignore during sync and scrobble" +
    " actions:";
            // 
            // btnMovieRestrictions
            // 
            this.btnMovieRestrictions.Location = new System.Drawing.Point(246, 86);
            this.btnMovieRestrictions.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btnMovieRestrictions.Name = "btnMovieRestrictions";
            this.btnMovieRestrictions.Size = new System.Drawing.Size(228, 35);
            this.btnMovieRestrictions.TabIndex = 2;
            this.btnMovieRestrictions.Text = "&Movies...";
            this.btnMovieRestrictions.UseVisualStyleBackColor = true;
            this.btnMovieRestrictions.Click += new System.EventHandler(this.btnMovieRestrictions_Click);
            // 
            // progressBarSync
            // 
            this.progressBarSync.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.progressBarSync.Location = new System.Drawing.Point(18, 617);
            this.progressBarSync.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.progressBarSync.Name = "progressBarSync";
            this.progressBarSync.Size = new System.Drawing.Size(975, 35);
            this.progressBarSync.TabIndex = 5;
            // 
            // lblSyncStatus
            // 
            this.lblSyncStatus.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblSyncStatus.AutoEllipsis = true;
            this.lblSyncStatus.Location = new System.Drawing.Point(18, 722);
            this.lblSyncStatus.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblSyncStatus.Name = "lblSyncStatus";
            this.lblSyncStatus.Size = new System.Drawing.Size(807, 22);
            this.lblSyncStatus.TabIndex = 7;
            this.lblSyncStatus.Text = "Ready for anything!";
            // 
            // btnStartLibrarySync
            // 
            this.btnStartLibrarySync.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btnStartLibrarySync.Location = new System.Drawing.Point(18, 662);
            this.btnStartLibrarySync.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btnStartLibrarySync.Name = "btnStartLibrarySync";
            this.btnStartLibrarySync.Size = new System.Drawing.Size(975, 37);
            this.btnStartLibrarySync.TabIndex = 6;
            this.btnStartLibrarySync.Text = "Start Library and Playback Sync";
            this.btnStartLibrarySync.UseVisualStyleBackColor = true;
            this.btnStartLibrarySync.Click += new System.EventHandler(this.btnStartLibrarySync_Click);
            // 
            // dtParentalControlsTime
            // 
            this.dtParentalControlsTime.Format = System.Windows.Forms.DateTimePickerFormat.Time;
            this.dtParentalControlsTime.Location = new System.Drawing.Point(339, 58);
            this.dtParentalControlsTime.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.dtParentalControlsTime.Name = "dtParentalControlsTime";
            this.dtParentalControlsTime.ShowUpDown = true;
            this.dtParentalControlsTime.Size = new System.Drawing.Size(130, 26);
            this.dtParentalControlsTime.TabIndex = 3;
            this.dtParentalControlsTime.Value = new System.DateTime(2016, 3, 25, 21, 0, 0, 0);
            this.dtParentalControlsTime.ValueChanged += new System.EventHandler(this.dtParentalControlsTime_ValueChanged);
            // 
            // cbParentalControlsTime
            // 
            this.cbParentalControlsTime.AutoSize = true;
            this.cbParentalControlsTime.Location = new System.Drawing.Point(15, 66);
            this.cbParentalControlsTime.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.cbParentalControlsTime.Name = "cbParentalControlsTime";
            this.cbParentalControlsTime.Size = new System.Drawing.Size(244, 24);
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
            this.gbParentalControls.Location = new System.Drawing.Point(510, 406);
            this.gbParentalControls.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.gbParentalControls.Name = "gbParentalControls";
            this.gbParentalControls.Padding = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.gbParentalControls.Size = new System.Drawing.Size(480, 194);
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
            this.cboMovieCertifications.Location = new System.Drawing.Point(378, 128);
            this.cboMovieCertifications.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.cboMovieCertifications.Name = "cboMovieCertifications";
            this.cboMovieCertifications.Size = new System.Drawing.Size(90, 28);
            this.cboMovieCertifications.TabIndex = 7;
            this.cboMovieCertifications.SelectedValueChanged += new System.EventHandler(this.cboMovieCertifications_SelectedValueChanged);
            // 
            // cbParentalIgnoreMovieCertifications
            // 
            this.cbParentalIgnoreMovieCertifications.AutoSize = true;
            this.cbParentalIgnoreMovieCertifications.Location = new System.Drawing.Point(15, 138);
            this.cbParentalIgnoreMovieCertifications.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.cbParentalIgnoreMovieCertifications.Name = "cbParentalIgnoreMovieCertifications";
            this.cbParentalIgnoreMovieCertifications.Size = new System.Drawing.Size(276, 24);
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
            this.cboTVCertifications.Location = new System.Drawing.Point(378, 92);
            this.cboTVCertifications.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.cboTVCertifications.Name = "cboTVCertifications";
            this.cboTVCertifications.Size = new System.Drawing.Size(90, 28);
            this.cboTVCertifications.TabIndex = 5;
            this.cboTVCertifications.SelectedValueChanged += new System.EventHandler(this.cboTVCertifications_SelectedValueChanged);
            // 
            // cbParentalIgnoreShowCertifications
            // 
            this.cbParentalIgnoreShowCertifications.AutoSize = true;
            this.cbParentalIgnoreShowCertifications.Location = new System.Drawing.Point(15, 102);
            this.cbParentalIgnoreShowCertifications.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.cbParentalIgnoreShowCertifications.Name = "cbParentalIgnoreShowCertifications";
            this.cbParentalIgnoreShowCertifications.Size = new System.Drawing.Size(275, 24);
            this.cbParentalIgnoreShowCertifications.TabIndex = 4;
            this.cbParentalIgnoreShowCertifications.Text = "Ignore on Shows with Certification";
            this.cbParentalIgnoreShowCertifications.UseVisualStyleBackColor = true;
            this.cbParentalIgnoreShowCertifications.CheckedChanged += new System.EventHandler(this.cbParentalIgnoreShowCertifications_CheckedChanged);
            // 
            // gbImages
            // 
            this.gbImages.Controls.Add(this.cboPreferredImageLanguage);
            this.gbImages.Controls.Add(this.label1);
            this.gbImages.Location = new System.Drawing.Point(18, 525);
            this.gbImages.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.gbImages.Name = "gbImages";
            this.gbImages.Padding = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.gbImages.Size = new System.Drawing.Size(483, 75);
            this.gbImages.TabIndex = 9;
            this.gbImages.TabStop = false;
            this.gbImages.Text = "Images";
            // 
            // cboPreferredImageLanguage
            // 
            this.cboPreferredImageLanguage.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboPreferredImageLanguage.FormattingEnabled = true;
            this.cboPreferredImageLanguage.Location = new System.Drawing.Point(183, 25);
            this.cboPreferredImageLanguage.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.cboPreferredImageLanguage.Name = "cboPreferredImageLanguage";
            this.cboPreferredImageLanguage.Size = new System.Drawing.Size(289, 28);
            this.cboPreferredImageLanguage.TabIndex = 1;
            this.cboPreferredImageLanguage.SelectedIndexChanged += new System.EventHandler(this.cboPreferredImageLanguage_SelectedIndexChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(16, 31);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(155, 20);
            this.label1.TabIndex = 0;
            this.label1.Text = "Preferred Language:";
            // 
            // btnAuthoriseApplication
            // 
            this.btnAuthoriseApplication.Location = new System.Drawing.Point(14, 29);
            this.btnAuthoriseApplication.Name = "btnAuthoriseApplication";
            this.btnAuthoriseApplication.Size = new System.Drawing.Size(445, 44);
            this.btnAuthoriseApplication.TabIndex = 5;
            this.btnAuthoriseApplication.Text = "Authorize Application...";
            this.btnAuthoriseApplication.UseVisualStyleBackColor = true;
            this.btnAuthoriseApplication.Click += new System.EventHandler(this.btnAuthoriseApplication_Click);
            // 
            // lblAuthorizeApplication
            // 
            this.lblAuthorizeApplication.Location = new System.Drawing.Point(12, 80);
            this.lblAuthorizeApplication.Name = "lblAuthorizeApplication";
            this.lblAuthorizeApplication.Size = new System.Drawing.Size(445, 40);
            this.lblAuthorizeApplication.TabIndex = 6;
            this.lblAuthorizeApplication.Text = "You must authorize the application to use your account at trakt.tv.";
            // 
            // Configuration
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1008, 771);
            this.Controls.Add(this.gbImages);
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
            this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(1021, 799);
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
            this.gbImages.ResumeLayout(false);
            this.gbImages.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox gbTraktAccount;
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
        private System.Windows.Forms.GroupBox gbImages;
        private System.Windows.Forms.ComboBox cboPreferredImageLanguage;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label lblAuthorizeApplication;
        private System.Windows.Forms.Button btnAuthoriseApplication;
    }
}