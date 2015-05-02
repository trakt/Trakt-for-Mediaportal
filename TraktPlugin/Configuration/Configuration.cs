using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using MediaPortal.Configuration;
using TraktPlugin.TraktAPI.Enums;
using TraktPlugin.TraktHandlers;

namespace TraktPlugin
{
    public partial class Configuration : Form
    {
        #region Command Line Options

        private bool SilentMode { get; set; }
        private bool AutoSync { get; set; }
        private bool AutoCloseAfterSync { get; set; }

        #endregion

        public Configuration() { }

        public Configuration(string[] args)
        {
            TraktLogger.OnLogReceived += new TraktLogger.OnLogReceivedDelegate(OnLogMessage);
            
            InitializeComponent();
            this.Text = "Trakt Configuration v" + TraktSettings.Version;

            ParseCommandLine(args);

            TraktSettings.PerformMaintenance();
            TraktSettings.LoadSettings(false);
            
            #region Load Settings
            tbUsername.Text = TraktSettings.Username;
            tbPassword.Text = TraktSettings.Password;

            List<KeyValuePair<int, string>> items = new List<KeyValuePair<int, string>>();
            items.Add(new KeyValuePair<int, string>(TraktSettings.MovingPictures, "Moving Pictures"));
            items.Add(new KeyValuePair<int, string>(TraktSettings.TVSeries, "MP-TVSeries"));
            items.Add(new KeyValuePair<int, string>(TraktSettings.MyVideos, "My Videos"));
            items.Add(new KeyValuePair<int, string>(TraktSettings.MyFilms, "My Films"));
            items.Add(new KeyValuePair<int, string>(TraktSettings.OnlineVideos, "OnlineVideos"));
            items.Add(new KeyValuePair<int, string>(TraktSettings.MyTVRecordings, "My TV Recordings"));
            items.Add(new KeyValuePair<int, string>(TraktSettings.MyTVLive, "My TV Live"));
            items.Add(new KeyValuePair<int, string>(TraktSettings.ArgusRecordings, "Argus TV Recordings"));
            items.Add(new KeyValuePair<int, string>(TraktSettings.ArgusTVLive, "Argus TV Live"));            

            // add enabled ones to top of list, sort by priority
            foreach (var item in items.Where(p => p.Key >= 0).OrderBy(p => p.Key))
            {
                clbPlugins.Items.Add(item.Value, true);
            }
            // add disabled ones last, sort by name
            foreach (var item in items.Where(p => p.Key < 0).OrderBy(p => p.Value))
            {
              clbPlugins.Items.Add(item.Value, false);
            }
                        
            cbMovingPicturesCategories.Checked = TraktSettings.MovingPicturesCategories;
            cbMovingPicturesFilters.Checked = TraktSettings.MovingPicturesFilters;
            cbMyFilmsCategories.Checked = TraktSettings.MyFilmsCategories;
            cbKeepInSync.Checked = TraktSettings.KeepTraktLibraryClean;
            cbSyncLibrary.Checked = TraktSettings.SyncLibrary;
            cbSyncRatings.Checked = TraktSettings.SyncRatings;
            cbSyncPlayback.Checked = TraktSettings.SyncPlayback;
            cbSyncPlaybackOnEnterPlugin.Checked = TraktSettings.SyncPlaybackOnEnterPlugin;

            numSyncInterval.Value = TraktSettings.SyncTimerLength;
            numSyncResumeDelta.Value = TraktSettings.SyncResumeDelta;

            // disable controls if not applicable
            if (!TraktSettings.SyncLibrary)
            {
                cbKeepInSync.Enabled = false;
                cbSyncRatings.Enabled = false;
            }

            if (!TraktSettings.SyncPlayback)
            {
                numSyncResumeDelta.Enabled = false;
                lblSyncResumeDelta.Enabled = false;
                cbSyncPlaybackOnEnterPlugin.Enabled = false;
            }

            #endregion

            // handle events now that we have populated default settings
            clbPlugins.ItemCheck += new ItemCheckEventHandler(this.clbPlugins_ItemCheck);
            tbPassword.TextChanged += new EventHandler(this.tbPassword_TextChanged);
        }

        private void ParseCommandLine(string[] args)
        {
            if (args == null)
                return;

            foreach (var argument in args)
            {
                switch (argument.ToLower().TrimStart('-'))
                {
                    case "silentmode":
                        SilentMode = true;
                        break;

                    case "sync":
                        AutoSync = true;
                        break;

                    case "closeaftersync":
                        AutoCloseAfterSync = true;
                        break;
                }
            }

            TraktLogger.Info("Command Line Options Set, SilentMode = '{0}', AutoSync = '{1}', CloseAfterSync = '{2}'", SilentMode, AutoSync, AutoCloseAfterSync);
        }

        private void OnLogMessage(string message, bool error)
        {
            UpdateStatus(message, error);
        }

        private void tbUsername_TextChanged(object sender, EventArgs e)
        {
            TraktSettings.Username = tbUsername.Text;
            TraktSettings.AccountStatus = ConnectionState.Pending;
        }

        private void tbPassword_TextChanged(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(tbPassword.Text))
            {
                TraktSettings.Password = string.Empty;
                return;
            }
            TraktSettings.Password = tbPassword.Text;
            TraktSettings.AccountStatus = ConnectionState.Pending;
        }

        private void tbPassword_Enter(object sender, EventArgs e)
        {
            // clear password field so can be re-entered easily, when re-entering config
            // it wont look like original because its hashed, so it's less confusing if cleared
            tbPassword.Text = string.Empty;
        }

        private void cbKeepInSync_CheckedChanged(object sender, EventArgs e)
        {
            //IMPORTANT NOTE on support for more than one library backend for the same video type (i.e movies) we shouldn't keep in sync ever.
            TraktSettings.KeepTraktLibraryClean = cbKeepInSync.Checked;
        }

        private void CloseConfig()
        {
            if (TraktSettings.KeepTraktLibraryClean && (TraktSettings.MoviePluginCount > 1 || TraktSettings.TvShowPluginCount > 1))
            {
                // warn and disable clean library
                string message = "You can not have 'Clean Library' option enabled with more than one movie or show plugin enabled. Option will be disabled.";
                MessageBox.Show(message, "trakt", MessageBoxButtons.OK, MessageBoxIcon.Information);
                TraktSettings.KeepTraktLibraryClean = false;
            }
            TraktSettings.SaveSettings(false);

            TraktLogger.Info("Exiting Configuration");
            this.Close();
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            CloseConfig();
        }

        private void btnUp_Click(object sender, EventArgs e)
        {
            if (clbPlugins.SelectedIndex > 0)
            {
                int ndx = clbPlugins.SelectedIndex;
                bool enabled = clbPlugins.GetItemChecked(ndx);
                
                string item = (string)clbPlugins.SelectedItem;
                clbPlugins.Items.RemoveAt(ndx);
                clbPlugins.Items.Insert(ndx - 1, item);
                clbPlugins.SetItemChecked(ndx - 1, enabled);
                clbPlugins.SelectedIndex = ndx - 1;
                SetPriorityOrder();
            }
        }

        private void btnDown_Click(object sender, EventArgs e)
        {
            if (clbPlugins.SelectedIndex != -1 && clbPlugins.SelectedIndex < clbPlugins.Items.Count - 1)
            {
                int ndx = clbPlugins.SelectedIndex;
                bool enabled = clbPlugins.GetItemChecked(ndx);

                string item = (string)clbPlugins.SelectedItem;
                clbPlugins.Items.RemoveAt(ndx);
                clbPlugins.Items.Insert(ndx + 1, item);
                clbPlugins.SetItemChecked(ndx + 1, enabled);
                clbPlugins.SelectedIndex = ndx + 1;
                SetPriorityOrder();
            }
        }

        private void SetPriorityOrder()
        {
            int i = 0;
            foreach (var item in clbPlugins.Items)
            {
                switch (item.ToString())
                {
                    case "Moving Pictures":
                        TraktSettings.MovingPictures = clbPlugins.GetItemChecked(i) ? i : -1;
                        break;
                    case "MP-TVSeries":
                        TraktSettings.TVSeries = clbPlugins.GetItemChecked(i) ? i : -1;
                        break;
                    case "My Videos":
                        TraktSettings.MyVideos = clbPlugins.GetItemChecked(i) ? i : -1;
                        break;
                    case "My Films":
                        TraktSettings.MyFilms = clbPlugins.GetItemChecked(i) ? i : -1;
                        break;
                    case "OnlineVideos":
                        TraktSettings.OnlineVideos = clbPlugins.GetItemChecked(i) ? i : -1;
                        break;
                    case "My TV Recordings":
                        TraktSettings.MyTVRecordings = clbPlugins.GetItemChecked(i) ? i : -1;
                        break;
                    case "My TV Live":
                        TraktSettings.MyTVLive = clbPlugins.GetItemChecked(i) ? i : -1;
                        break;
                    case "Argus TV Recordings":
                        TraktSettings.ArgusRecordings = clbPlugins.GetItemChecked(i) ? i : -1;
                        break;
                    case "Argus TV Live":
                        TraktSettings.ArgusTVLive = clbPlugins.GetItemChecked(i) ? i : -1;
                        break;
                }
                i++;
            }
        }

        private void clbPlugins_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            int ndx = e.Index;
            string plugin = (string)clbPlugins.Items[ndx];

            switch (plugin)
            {
                case "Moving Pictures":
                    TraktSettings.MovingPictures = clbPlugins.GetItemChecked(ndx) ? -1 : ndx;
                    break;
                case "MP-TVSeries":
                    TraktSettings.TVSeries = clbPlugins.GetItemChecked(ndx) ? -1 : ndx;
                    break;
                case "My Videos":
                    TraktSettings.MyVideos = clbPlugins.GetItemChecked(ndx) ? -1 : ndx;
                    break;
                case "My Films":
                    TraktSettings.MyFilms = clbPlugins.GetItemChecked(ndx) ? -1 : ndx;
                    break;
                case "OnlineVideos":
                    TraktSettings.OnlineVideos = clbPlugins.GetItemChecked(ndx) ? -1 : ndx;
                    break;
                case "My TV Recordings":
                    TraktSettings.MyTVRecordings = clbPlugins.GetItemChecked(ndx) ? -1 : ndx;
                    break;
                case "My TV Live":
                    TraktSettings.MyTVLive = clbPlugins.GetItemChecked(ndx) ? -1 : ndx;
                    break;
                case "Argus TV Recordings":
                    TraktSettings.ArgusRecordings = clbPlugins.GetItemChecked(ndx) ? -1 : ndx;
                    break;
                case "Argus TV Live":
                    TraktSettings.ArgusTVLive = clbPlugins.GetItemChecked(ndx) ? -1 : ndx;
                    break;
            }
        }

        private void btnTVSeriesRestrictions_Click(object sender, EventArgs e)
        {
            if (!File.Exists(Path.Combine(Config.GetFolder(Config.Dir.Plugins), @"windows\mp-tvseries.dll")))
            {
                MessageBox.Show("Could not load series list, ensure that MP-TVSeries plugin is installed.", "Series Select", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            SeriesSelect SeriesSelectDlg = new SeriesSelect();
            SeriesSelectDlg.ShowDialog(this);
        }

        private void btnMovieRestrictions_Click(object sender, EventArgs e)
        {
            MovieSelect MovieSelectDlg = new MovieSelect();
            MovieSelectDlg.BlockedFilenames = TraktSettings.BlockedFilenames;
            MovieSelectDlg.ShowDialog(this);
            if (MovieSelectDlg.DialogResult == DialogResult.OK)
            {
                TraktSettings.BlockedFilenames = MovieSelectDlg.BlockedFilenames;
            }
        }

        private void linkTrakt_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start(@"http://trakt.tv/");
        }

        private void cbMovingPicturesCategories_Click(object sender, EventArgs e)
        {
            TraktSettings.MovingPicturesCategories = !TraktSettings.MovingPicturesCategories;
        }

        private void cbMovingPicturesFilters_Click(object sender, EventArgs e)
        {
            TraktSettings.MovingPicturesFilters = !TraktSettings.MovingPicturesFilters;
        }

        private void cbMyFilmsCategories_Click(object sender, EventArgs e)
        {
            TraktSettings.MyFilmsCategories = !TraktSettings.MyFilmsCategories;
        }

        private void cbSyncLibrary_CheckedChanged(object sender, EventArgs e)
        {
            cbKeepInSync.Enabled = cbSyncLibrary.Checked;
            cbSyncRatings.Enabled = cbSyncLibrary.Checked;

            TraktSettings.SyncLibrary = cbSyncLibrary.Checked;
        }

        private void cbSyncRatings_CheckedChanged(object sender, EventArgs e)
        {
            TraktSettings.SyncRatings = cbSyncRatings.Checked;
        }

        private void cbSyncPlayback_CheckedChanged(object sender, EventArgs e)
        {
            numSyncResumeDelta.Enabled = cbSyncPlayback.Checked;
            lblSyncResumeDelta.Enabled = cbSyncPlayback.Checked;
            cbSyncPlaybackOnEnterPlugin.Enabled = cbSyncPlayback.Checked;

            TraktSettings.SyncPlayback = cbSyncPlayback.Checked;
        }

        private void numSyncResumeDelta_ValueChanged(object sender, EventArgs e)
        {
            int result = 0;
            if (int.TryParse(numSyncResumeDelta.Value.ToString(), out result))
            {
                TraktSettings.SyncResumeDelta = result;
            }
        }

        private void numSyncInterval_ValueChanged(object sender, EventArgs e)
        {
            int result = 0;
            if (int.TryParse(numSyncInterval.Value.ToString(), out result))
            {
                TraktSettings.SyncTimerLength = result;
            }
        }

        private void cbSyncPlaybackOnEnterPlugin_CheckedChanged(object sender, EventArgs e)
        {
            TraktSettings.SyncPlaybackOnEnterPlugin = cbSyncPlaybackOnEnterPlugin.Checked;
        }

        private void btnStartLibrarySync_Click(object sender, EventArgs e)
        {           
            StartSync();
        }

        private void SetSyncControlProperties(bool syncRunning)
        {
            if (syncRunning)
            {
                progressBarSync.Style = ProgressBarStyle.Marquee;
                btnStartLibrarySync.Enabled = false;
                gbTraktAccount.Enabled = false;
                gbPlugins.Enabled = false;
                gbRestrictions.Enabled = false;
                gbSync.Enabled = false;
                btnOK.Enabled = false;
            }
            else
            {
                progressBarSync.Style = ProgressBarStyle.Continuous;
                btnStartLibrarySync.Enabled = true;
                gbTraktAccount.Enabled = true;
                gbPlugins.Enabled = true;
                gbRestrictions.Enabled = true;
                gbSync.Enabled = true;
                btnOK.Enabled = true;
            }
        }

        private void StartSync()
        {
            SetSyncControlProperties(true);

            var syncThread = new Thread(() =>
                {
                    if (TraktSettings.AccountStatus != ConnectionState.Connected)
                    {
                        // stop sync
                        SetSyncControlProperties(false);
                        TraktSettings.AccountStatus = ConnectionState.Pending;
                        return;
                    }

                    TraktLogger.Info("Library and Playback Sync started for all enabled plugins");

                    foreach (var item in clbPlugins.CheckedItems)
                    {
                        try
                        {
                            switch (item.ToString())
                            {
                                case "Moving Pictures":
                                    var movingPictures = new MovingPictures(TraktSettings.MovingPictures);
                                    movingPictures.SyncLibrary();
                                    movingPictures.SyncProgress();
                                    break;

                                case "MP-TVSeries":
                                    var tvSeries = new TVSeries(TraktSettings.TVSeries);
                                    tvSeries.SyncLibrary();
                                    tvSeries.SyncProgress();
                                    break;

                                case "My Videos":
                                    var myVideos = new MyVideos(TraktSettings.MyVideos);
                                    myVideos.SyncLibrary();
                                    myVideos.SyncProgress();
                                    break;

                                case "My Films":
                                    var myFilms = new MyFilmsHandler(TraktSettings.MyFilms);
                                    myFilms.SyncLibrary();
                                    myFilms.SyncProgress();
                                    break;
                            }

                        }
                        catch (Exception ex)
                        {
                            TraktLogger.Error("Error synchronising library, Plugin = '{0}', Error = '{1}'", item.ToString(), ex.Message);
                            continue;
                        }
                    }

                    // save last paused item so we only process after this date in future
                    if (TraktCache.PlaybackData != null && TraktCache.PlaybackData.Count() > 0)
                    {
                        TraktSettings.LastPausedItemProcessed = TraktCache.PlaybackData.First().PausedAt;
                    }

                    TraktLogger.Info("Library and Playback Sync completed for all enabled plugins");
                    SetSyncControlProperties(false);

                    if (SilentMode || AutoCloseAfterSync)
                    {
                        CloseConfig();
                    }
                })
                {
                    Name = "Sync",
                    IsBackground = true
                };

            syncThread.Start();
        }

        delegate void UpdateProgressDelegate(string message, bool error);

        private void UpdateStatus(string message)
        {
            UpdateStatus(message, false);
        }

        private void UpdateStatus(string message, bool error)
        {
            if (lblSyncStatus.InvokeRequired)
            {
                var updateProgress = new UpdateProgressDelegate(UpdateStatus);
                object[] args = { message, error };
                lblSyncStatus.Invoke(updateProgress, args);
                return;
            }

            lblSyncStatus.Text = message;
            lblSyncStatus.ForeColor = error ? Color.Red : Color.Black;
        }

        private void Configuration_Load(object sender, EventArgs e)
        {
            // hide the form if silent
            if (SilentMode)
            {
                this.Opacity = 0;
                this.ShowInTaskbar = true;
                this.Visible = false;
            }

            if (AutoSync)
            {
                StartSync();
            }
        }

    }
}
