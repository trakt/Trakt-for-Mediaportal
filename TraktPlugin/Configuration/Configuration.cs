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
            TraktSettings.loadSettings();

            #region load settings
            tbUsername.Text = TraktSettings.Username;

            List<KeyValuePair<int, string>> items = new List<KeyValuePair<int, string>>();
            items.Add(new KeyValuePair<int, string>(TraktSettings.MovingPictures, "Moving Pictures"));
            items.Add(new KeyValuePair<int, string>(TraktSettings.TVSeries, "MP-TVSeries"));
            items.Sort(new Comparison<KeyValuePair<int, string>>((x, y) => 
            {
                // sort disabled at end of list
                int sortx = x.Key == -1 ? 1000 : x.Key;
                int sorty = y.Key == -1 ? 1000 : y.Key;
                return sortx.CompareTo(sorty); 
            }));

            foreach (var item in items)
            {
                clbPlugins.Items.Add(item.Value, item.Key != -1);
            }
            
            cbKeepInSync.Checked = TraktSettings.KeepTraktLibraryClean;
            #endregion

            clbPlugins.ItemCheck += new ItemCheckEventHandler(this.clbPlugins_ItemCheck);
        }

        private void tbUsername_TextChanged(object sender, EventArgs e)
        {
            TraktSettings.Username = tbUsername.Text;
        }

        private void tbPassword_TextChanged(object sender, EventArgs e)
        {
            TraktSettings.Password = tbPassword.Text.GetSha1();            
        }

        private void cbKeepInSync_CheckedChanged(object sender, EventArgs e)
        {
            //IMPORTANT NOTE on support for more than one library backend for the same video type (i.e movies) we shouldn't keep in sync ever.
            TraktSettings.KeepTraktLibraryClean = cbKeepInSync.Checked;            
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
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
            if (clbPlugins.SelectedIndex < clbPlugins.Items.Count - 1)
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
            }
        }

        private void btnClearLibrary_Click(object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(TraktSettings.Username) || String.IsNullOrEmpty(TraktSettings.Password))
            {
                MessageBox.Show("Please enter your Username and Password before attempting this");
                return;
            }
            
            string message = "Are sure you want to clear your library from trakt?\n\n";
            message += "Note: this will not clear your library completely, scrobbled items will need to be cleared manually.";
            DialogResult result = MessageBox.Show(message, "Clear Library", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                BackgroundWorker libraryClearer = new BackgroundWorker();
                libraryClearer.DoWork += new DoWorkEventHandler(libraryClearer_DoWork);
                libraryClearer.RunWorkerAsync();
            }
        }

        void libraryClearer_DoWork(object sender, DoWorkEventArgs e)
        {
            ProgressDialog pd = new ProgressDialog(this.Handle);
            TraktAPI.TraktAPI.ClearLibrary(TraktAPI.TraktClearingModes.all, pd);
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
