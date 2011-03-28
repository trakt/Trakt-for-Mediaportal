using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;

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
            if (TraktSettings.MovingPictures > -1)
            {
                //Load the Movies from Moving Pictures
                try
                {
                    List<MediaPortal.Plugins.MovingPictures.Database.DBMovieInfo> movies = MediaPortal.Plugins.MovingPictures.Database.DBMovieInfo.GetAll();
                    unCheckedMovies.AddRange(from movie in movies where !_blockedFilenames.Contains(movie.LocalMedia[0].FullPath) select new MovieSelectItem { MovieTitle = movie.Title, Filename = movie.LocalMedia.Select(media => media.FullPath).ToList() });
                    checkedMovies.AddRange(from movie in movies where _blockedFilenames.Contains(movie.LocalMedia[0].FullPath) select new MovieSelectItem { MovieTitle = movie.Title, Filename = movie.LocalMedia.Select(media => media.FullPath).ToList() });
                }
                catch (IOException)
                {
                    TraktLogger.Info("Trakt: Failed to load Moving Pictures! DLL is missing?");
                }
            }

            foreach (MovieSelectItem movie in unCheckedMovies)
                checkedListBoxMovies.Items.Add(movie, false);
            foreach (MovieSelectItem movie in checkedMovies)
                checkedListBoxMovies.Items.Add(movie, true);

            checkedListBoxMovies.ItemCheck += new ItemCheckEventHandler(checkedListBoxMovies_ItemCheck);
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
