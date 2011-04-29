using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using SQLite.NET;
using MediaPortal.Configuration;
using MediaPortal.Database;
using MediaPortal.Video.Database;

namespace TraktPlugin
{
    public partial class MovieSelect : Form
    {
        public MovieSelect()
        {
            InitializeComponent();
        }

        public List<String> BlockedFilenames { get { return _blockedFilenames; } set { _blockedFilenames = value; } } private List<String> _blockedFilenames = new List<String>();

        private List<MovieSelectItem> checkedMovies = new List<MovieSelectItem>();
        private List<MovieSelectItem> unCheckedMovies = new List<MovieSelectItem>();

        private void MovieSelect_Load(object sender, EventArgs e)
        {
            //If MovingPictures is selected
            if (TraktSettings.MovingPictures > -1 && File.Exists(Path.Combine(Config.GetFolder(Config.Dir.Plugins), @"Windows\MovingPictures.dll")))
            {
                //Load the Movies from Moving Pictures
                try
                {
                    LoadMoviesFromMovingPictures();
                }
                catch (IOException)
                {
                    TraktLogger.Info("Failed to load Moving Pictures! DLL is missing?");
                }
            }

            //If MyVideos is selected, always installed
            if (TraktSettings.MyVideos > -1)
            {
                string sql = "SELECT movieinfo.strTitle, files.strFilename " +
                             "FROM movieInfo " +
                             "LEFT JOIN files " +
                             "ON movieInfo.idMovie=files.idMovie " +
                             "LEFT JOIN path " +
                             "ON files.idPath=path.idPath " +
                             "ORDER BY strTitle";

                SQLiteResultSet results = VideoDatabase.GetResults(sql);

                for (int row = 0; row < results.Rows.Count; row++ )
                {
                    string title = DatabaseUtility.Get(results, row, 0);
                    string filename = Path.Combine(DatabaseUtility.Get(results, row, 1), DatabaseUtility.Get(results, row, 2));                    

                    if (!_blockedFilenames.Contains(filename))
                        unCheckedMovies.Add(new MovieSelectItem { MovieTitle = title, Filename = new List<string>{filename} });
                    else
                        checkedMovies.Add(new MovieSelectItem { MovieTitle = title, Filename = new List<string> { filename } });
                }
            }

            foreach (MovieSelectItem movie in checkedMovies)
                if (!checkedListBoxMovies.Items.Contains(movie))
                    checkedListBoxMovies.Items.Add(movie, true);
            
            foreach (MovieSelectItem movie in unCheckedMovies)
                if(!checkedListBoxMovies.Items.Contains(movie))
                    checkedListBoxMovies.Items.Add(movie, false);
            
            checkedListBoxMovies.ItemCheck += new ItemCheckEventHandler(checkedListBoxMovies_ItemCheck);
        }

        void LoadMoviesFromMovingPictures()
        {
            List<MediaPortal.Plugins.MovingPictures.Database.DBMovieInfo> movies = MediaPortal.Plugins.MovingPictures.Database.DBMovieInfo.GetAll();
            movies.Sort();
            unCheckedMovies.AddRange(from movie in movies where !_blockedFilenames.Contains(movie.LocalMedia[0].FullPath) select new MovieSelectItem { MovieTitle = movie.Title, Filename = movie.LocalMedia.Select(media => media.FullPath).ToList() });
            checkedMovies.AddRange(from movie in movies where _blockedFilenames.Contains(movie.LocalMedia[0].FullPath) select new MovieSelectItem { MovieTitle = movie.Title, Filename = movie.LocalMedia.Select(media => media.FullPath).ToList() });
        }

        void checkedListBoxMovies_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            int index = e.Index;
            MovieSelectItem item = (MovieSelectItem)checkedListBoxMovies.Items[index];
            if (item != null)
            {
                // Item state before item was clicked 
                if (checkedListBoxMovies.GetItemChecked(index))
                {

                    // Store items changes
                    if (!unCheckedMovies.Contains(item))
                    {
                        unCheckedMovies.Add(item);
                    }
                    if (checkedMovies.Contains(item))
                    {
                        checkedMovies.Remove(item);
                    }

                }
                else
                {
                    // Store items changes
                    if (!checkedMovies.Contains(item))
                    {
                        checkedMovies.Add(item);
                    }
                    if (unCheckedMovies.Contains(item))
                    {
                        unCheckedMovies.Remove(item);
                    }
                }
            }
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            foreach (MovieSelectItem movie in unCheckedMovies)
            {
                foreach (String filename in movie.Filename)
                {
                    while(_blockedFilenames.Count(f => f == filename) > 0)
                        _blockedFilenames.Remove(filename);
                }
            }
            foreach (MovieSelectItem movie in checkedMovies)
            {
                foreach (String filename in movie.Filename)
                {
                    if(!_blockedFilenames.Contains(filename))
                        _blockedFilenames.Add(filename);
                }
            }
            DialogResult = DialogResult.OK;
            Close();
        }

        private void btnFolderRestrictions_Click(object sender, EventArgs e)
        {
            FolderList FolderListDlg = new FolderList();
            FolderListDlg.Folders = TraktSettings.BlockedFolders;
            DialogResult result = FolderListDlg.ShowDialog(this);

            if (result == DialogResult.OK)
            {
                TraktSettings.BlockedFolders = FolderListDlg.Folders;
            }
        }

    }

    public class MovieSelectItem
    {
        public String MovieTitle { get; set; }
        public List<String> Filename { get; set; }

        public override string ToString()
        {
            return MovieTitle;
        }
    }
}
