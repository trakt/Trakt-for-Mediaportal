using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Security.Cryptography;
using MediaPortal.Configuration;

namespace TraktPlugin
{
    public partial class Configuration : Form
    {
        public Configuration()
        {
            InitializeComponent();
            this.Text = "Trakt Configuration v" + TraktSettings.Version;
            TraktSettings.loadSettings();

            #region load settings
            tbUsername.Text = TraktSettings.Username;
            // since password is Sha1, just show a dummy password
            tbPassword.Text = string.IsNullOrEmpty(TraktSettings.Password) ? string.Empty : TraktSettings.Password.Substring(0, 10);

            List<KeyValuePair<int, string>> items = new List<KeyValuePair<int, string>>();
            items.Add(new KeyValuePair<int, string>(TraktSettings.MovingPictures, "Moving Pictures"));
            items.Add(new KeyValuePair<int, string>(TraktSettings.TVSeries, "MP-TVSeries"));
            items.Add(new KeyValuePair<int, string>(TraktSettings.MyVideos, "My Videos"));
            items.Add(new KeyValuePair<int, string>(TraktSettings.MyFilms, "My Films"));
            items.Add(new KeyValuePair<int, string>(TraktSettings.OnlineVideos, "OnlineVideos"));
            items.Add(new KeyValuePair<int, string>(TraktSettings.MyAnime, "My Anime"));
            items.Add(new KeyValuePair<int, string>(TraktSettings.MyTVRecordings, "My TV Recordings"));
            items.Add(new KeyValuePair<int, string>(TraktSettings.MyTVLive, "My TV Live"));
            items.Add(new KeyValuePair<int, string>(TraktSettings.ForTheRecordRecordings, "4TR TV Recordings"));
            items.Add(new KeyValuePair<int, string>(TraktSettings.ForTheRecordTVLive, "4TR TV Live"));
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
            
            cbKeepInSync.Checked = TraktSettings.KeepTraktLibraryClean;
            cbTraktSyncLength.SelectedItem = (TraktSettings.SyncTimerLength / 3600000).ToString();

            cbMovingPicturesCategories.Checked = TraktSettings.MovingPicturesCategories;
            cbMovingPicturesFilters.Checked = TraktSettings.MovingPicturesFilters;
            cbMyFilmsCategories.Checked = TraktSettings.MyFilmsCategories;
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
            TraktSettings.Password = tbPassword.Text.GetSha1();            
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
            TraktSettings.saveSettings();
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
                    case "My Anime":
                        TraktSettings.MyAnime = clbPlugins.GetItemChecked(i) ? i : -1;
                        break;
                    case "My TV Recordings":
                        TraktSettings.MyTVRecordings = clbPlugins.GetItemChecked(i) ? i : -1;
                        break;
                    case "My TV Live":
                        TraktSettings.MyTVLive = clbPlugins.GetItemChecked(i) ? i : -1;
                        break;
                    case "4TR TV Recordings":
                        TraktSettings.ForTheRecordRecordings = clbPlugins.GetItemChecked(i) ? i : -1;
                        break;
                    case "4TR TV Live":
                        TraktSettings.ForTheRecordTVLive = clbPlugins.GetItemChecked(i) ? i : -1;
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
                case "My Anime":
                    TraktSettings.MyAnime = clbPlugins.GetItemChecked(ndx) ? -1 : ndx;
                    break;
                case "My TV Recordings":
                    TraktSettings.MyTVRecordings = clbPlugins.GetItemChecked(ndx) ? -1 : ndx;
                    break;
                case "My TV Live":
                    TraktSettings.MyTVLive = clbPlugins.GetItemChecked(ndx) ? -1 : ndx;
                    break;
                case "4TR TV Recordings":
                    TraktSettings.ForTheRecordRecordings = clbPlugins.GetItemChecked(ndx) ? -1 : ndx;
                    break;
                case "4TR TV Live":
                    TraktSettings.ForTheRecordTVLive = clbPlugins.GetItemChecked(ndx) ? -1 : ndx;
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

        void libraryClearer_DoWork(object sender, DoWorkEventArgs e)
        {
            ProgressDialog pd = new ProgressDialog(this.Handle);
            TraktAPI.TraktAPI.ClearLibrary(TraktAPI.TraktClearingModes.all, pd, (bool)e.Argument);
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
            //Check that Moving Pictures is installed
            if (!File.Exists(Path.Combine(Config.GetFolder(Config.Dir.Plugins), @"Windows\MovingPictures.dll")))
            {
                MessageBox.Show("Moving Pictures isn't installed! Disabling...");
                cbMovingPicturesCategories.Checked = false;
                return;
            }

            if (TraktSettings.MovingPicturesCategories)
            {
                //Remove
                TraktSettings.MovingPicturesCategories = false;
                TraktHandlers.MovingPictures.RemoveMovingPicturesCategories();
            }
            else
            {
                //Add
                TraktSettings.MovingPicturesCategories = true;
                BackgroundWorker categoriesCreator = new BackgroundWorker();
                categoriesCreator.DoWork += new DoWorkEventHandler(categoriesCreator_DoWork);
                categoriesCreator.RunWorkerAsync();
            }

            cbMovingPicturesCategories.Checked = TraktSettings.MovingPicturesCategories;
        }

        void categoriesCreator_DoWork(object sender, DoWorkEventArgs e)
        {
            ProgressDialog pd = new ProgressDialog(this.Handle);
            pd.ShowDialog();
            pd.Line1 = "Creating Categories";
            
            TraktHandlers.MovingPictures.CreateMovingPicturesCategories();
            //Update
            pd.Line1 = "Updating Categories";
            TraktHandlers.MovingPictures.UpdateMovingPicturesCategories();
            pd.CloseDialog();
        }

        private void cbMovingPicturesFilters_Click(object sender, EventArgs e)
        {
            //Check that Moving Pictures is installed
            if (!File.Exists(Path.Combine(Config.GetFolder(Config.Dir.Plugins), @"Windows\MovingPictures.dll")))
            {
                MessageBox.Show("Moving Pictures isn't installed! Disabling...");
                cbMovingPicturesFilters.Checked = false;
                return;
            }

            if (TraktSettings.MovingPicturesFilters)
            {
                //Remove
                TraktSettings.MovingPicturesFilters = false;
                TraktHandlers.MovingPictures.RemoveMovingPicturesFilters();
            }
            else
            {
                //Add
                TraktSettings.MovingPicturesFilters = true;
                BackgroundWorker filtersCreator = new BackgroundWorker();
                filtersCreator.DoWork += new DoWorkEventHandler(filtersCreator_DoWork);
                filtersCreator.RunWorkerAsync();
            }

            cbMovingPicturesFilters.Checked = TraktSettings.MovingPicturesFilters;
        }

        void filtersCreator_DoWork(object sender, DoWorkEventArgs e)
        {
            ProgressDialog pd = new ProgressDialog(this.Handle);
            pd.ShowDialog();
            pd.Line1 = "Creating Filters";

            TraktHandlers.MovingPictures.CreateMovingPicturesFilters();
            //Update
            pd.Line1 = "Updating Filters";
            TraktHandlers.MovingPictures.UpdateMovingPicturesFilters();
            pd.CloseDialog();
        }

        private void cbMyFilmsCategories_Click(object sender, EventArgs e)
        {
            TraktSettings.MyFilmsCategories = !TraktSettings.MyFilmsCategories;
        }

    }

    #region String Extension

    /// <summary>
    /// Creats a SHA1 Hash of a string
    /// </summary>
    public static class SHA1StringExtension
    {
        public static string GetSha1(this string value)
        {
            var data = Encoding.ASCII.GetBytes(value);
            var hashData = new SHA1Managed().ComputeHash(data);

            var hash = string.Empty;

            foreach (var b in hashData)
                hash += b.ToString("X2");

            return hash;
        }
    }
    #endregion
}
