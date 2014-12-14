using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using MediaPortal.Configuration;

namespace TraktPlugin
{
    public partial class Configuration : Form
    {
        public Configuration()
        {
            InitializeComponent();
            this.Text = "Trakt Configuration v" + TraktSettings.Version;

            TraktSettings.PerformMaintenance();
            TraktSettings.LoadSettings();

            #region load settings
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
            
            cbTraktSyncLength.SelectedItem = (TraktSettings.SyncTimerLength / 3600000).ToString();
            cbMovingPicturesCategories.Checked = TraktSettings.MovingPicturesCategories;
            cbMovingPicturesFilters.Checked = TraktSettings.MovingPicturesFilters;
            cbMyFilmsCategories.Checked = TraktSettings.MyFilmsCategories;
            cbKeepInSync.Checked = TraktSettings.KeepTraktLibraryClean;
            cbSyncLibrary.Checked = TraktSettings.SyncLibrary;
            cbSyncRatings.Checked = TraktSettings.SyncRatings;

            // disable controls if not applicable
            if (!TraktSettings.SyncLibrary)
            {
                cbKeepInSync.Enabled = false;
                cbSyncRatings.Enabled = false;
            }

            #endregion

            // handle events now that we have populated default settings
            clbPlugins.ItemCheck += new ItemCheckEventHandler(this.clbPlugins_ItemCheck);
            tbPassword.TextChanged += new EventHandler(this.tbPassword_TextChanged);
        }

        private void tbUsername_TextChanged(object sender, EventArgs e)
        {
            TraktSettings.Username = tbUsername.Text;
        }

        private void tbPassword_TextChanged(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(tbPassword.Text))
            {
                TraktSettings.Password = string.Empty;
                return;
            }
            TraktSettings.Password = tbPassword.Text;            
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

        private void btnOK_Click(object sender, EventArgs e)
        {
            if (TraktSettings.KeepTraktLibraryClean && (TraktSettings.MoviePluginCount > 1 || TraktSettings.TvShowPluginCount > 1))
            {
                // warn and disable clean library
                string message = "You can not have 'Clean Library' option enabled with more than one movie or show plugin enabled. Option will be disabled.";
                MessageBox.Show(message, "trakt", MessageBoxButtons.OK, MessageBoxIcon.Information);
                TraktSettings.KeepTraktLibraryClean = false;
            }
            TraktSettings.SaveSettings();
            this.Close();
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

        private void btnClearLibrary_Click(object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(TraktSettings.Username) || String.IsNullOrEmpty(TraktSettings.Password))
            {
                MessageBox.Show("Please enter your Username and Password before attempting this");
                return;
            }
            
            string message = "Are you sure you want to clear your library from trakt? All items marked as 'In Collection' will be removed.";
            DialogResult result = MessageBox.Show(message, "Clear Library", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result != DialogResult.Yes) return;

            message = "Would you also like to mark all items as unwatched?\n\n";
            message += "Note: this will not clear your watched library completely, scrobbled items will need to be cleared manually from website.";
            result = MessageBox.Show(message, "Clear Library", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);

            BackgroundWorker libraryClearer = new BackgroundWorker();
            libraryClearer.DoWork += new DoWorkEventHandler(libraryClearer_DoWork);
            libraryClearer.RunWorkerAsync(result == DialogResult.Yes ? true : false);
            
        }

        private void libraryClearer_DoWork(object sender, DoWorkEventArgs e)
        {
            ProgressDialog pd = new ProgressDialog(this.Handle);
            //TODOClearLibrary(TraktAPI.v1.TraktClearingModes.all, pd, (bool)e.Argument);
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

        private void cbTraktSyncLength_SelectedValueChanged(object sender, EventArgs e)
        {
            TraktSettings.SyncTimerLength = int.Parse(cbTraktSyncLength.SelectedItem.ToString()) * 3600000;
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

        /// <summary>
        /// //TODOClears our library on Trakt as best as the api lets us
        /// </summary>
        /// <param name="mode">What to remove from Trakt</param>
        //private void ClearLibrary(TraktClearingModes mode, ProgressDialog progressDialog, bool clearSeen)
        //{
            
            //progressDialog.Title = "Clearing Library";
            //progressDialog.CancelMessage = "Attempting to Cancel";
            //progressDialog.Maximum = 100;
            //progressDialog.Value = 0;
            //progressDialog.Line1 = "Clearing your Trakt Library";
            //progressDialog.ShowDialog(ProgressDialog.PROGDLG.Modal, ProgressDialog.PROGDLG.NoMinimize, ProgressDialog.PROGDLG.NoTime);

            ////Movies
            //if (mode == TraktClearingModes.all || mode == TraktClearingModes.movies)
            //{
            //    TraktLogger.Info("Removing Movies from Trakt");
            //    TraktLogger.Info("NOTE: WILL NOT REMOVE SCROBBLED MOVIES DUE TO API LIMITATION");
            //    progressDialog.Line2 = "Getting movies for user";

            //    TraktLogger.Info("Getting user {0}'s movies from trakt", TraktSettings.Username);
            //    var movies = TraktAPI.v1.TraktAPI.GetAllMoviesForUser(TraktSettings.Username).ToList();

            //    var syncData = TraktHandlers.BasicHandler.CreateMovieSyncData(movies);
            //    TraktResponse response = null;

            //    if (clearSeen)
            //    {
            //        TraktLogger.Info("First removing movies from seen");
            //        progressDialog.Line2 = "Setting seen movies as unseen";
            //        response = TraktAPI.v1.TraktAPI.SyncMovieLibrary(syncData, TraktSyncModes.unseen);
            //        TraktLogger.LogTraktResponse(response);
            //    }

            //    TraktLogger.Info("Now removing movies from library");
            //    progressDialog.Line2 = "Removing movies from library";
            //    response = TraktAPI.v1.TraktAPI.SyncMovieLibrary(syncData, TraktSyncModes.unlibrary);
            //    TraktLogger.LogTraktResponse(response);

            //    TraktLogger.Info("Removed all movies possible, some manual clean up may be required");
            //}
            //if (mode == TraktClearingModes.all)
            //    progressDialog.Value = 15;
            //if (progressDialog.HasUserCancelled)
            //{
            //    TraktLogger.Info("Cancelling Library Clearing");
            //    progressDialog.CloseDialog();
            //    return;
            //}
            ////Episodes
            //if (mode == TraktClearingModes.all || mode == TraktClearingModes.episodes)
            //{
            //    TraktLogger.Info("Removing Shows from Trakt");
            //    TraktLogger.Info("NOTE: WILL NOT REMOVE SCROBBLED SHOWS DUE TO API LIMITATION");

            //    if (clearSeen)
            //    {
            //        TraktLogger.Info("First removing shows from seen");
            //        progressDialog.Line2 = "Getting Watched Episodes from Trakt";

            //        TraktLogger.Info("Getting user {0}'s 'watched/seen' episodes from trakt", TraktSettings.Username);
            //        var watchedEpisodes = TraktAPI.v1.TraktAPI.GetWatchedEpisodesForUser(TraktSettings.Username);
            //        if (watchedEpisodes != null)
            //        {
            //            foreach (var series in watchedEpisodes.ToList())
            //            {
            //                TraktLogger.Info("Removing '{0}' from seen", series.ToString());
            //                progressDialog.Line2 = string.Format("Setting {0} as unseen", series.ToString());
            //                var response = TraktAPI.v1.TraktAPI.SyncEpisodeLibrary(TraktHandlers.BasicHandler.CreateEpisodeSyncData(series), TraktSyncModes.unseen);
            //                TraktLogger.LogTraktResponse(response);
            //                System.Threading.Thread.Sleep(500);
            //                if (progressDialog.HasUserCancelled)
            //                {
            //                    TraktLogger.Info("Cancelling Library Clearing");
            //                    progressDialog.CloseDialog();
            //                    return;
            //                }
            //            }
            //        }
            //    }
            //    progressDialog.Value = 85;
            //    TraktLogger.Info("Now removing shows from library");
            //    progressDialog.Line2 = "Getting Library Episodes from Trakt";

            //    TraktLogger.Info("Getting user {0}'s 'library' episodes from trakt", TraktSettings.Username);
            //    var libraryEpisodes = TraktAPI.v1.TraktAPI.GetLibraryEpisodesForUser(TraktSettings.Username);
            //    if (libraryEpisodes != null)
            //    {
            //        foreach (var series in libraryEpisodes.ToList())
            //        {
            //            TraktLogger.Info("Removing '{0}' from library", series.ToString());
            //            progressDialog.Line2 = string.Format("Removing {0} from library", series.ToString());
            //            var response = TraktAPI.v1.TraktAPI.SyncEpisodeLibrary(TraktHandlers.BasicHandler.CreateEpisodeSyncData(series), TraktSyncModes.unlibrary);
            //            TraktLogger.LogTraktResponse(response);
            //            System.Threading.Thread.Sleep(500);
            //            if (progressDialog.HasUserCancelled)
            //            {
            //                TraktLogger.Info("Cancelling Library Clearing");
            //                progressDialog.CloseDialog();
            //                return;
            //            }
            //        }
            //    }
            //    TraktLogger.Info("Removed all shows possible, some manual clean up may be required");
            //}
            //progressDialog.Value = 100;
            //progressDialog.CloseDialog();
        //}
    }
}
