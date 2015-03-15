using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using MediaPortal.GUI.Library;
using TraktPlugin.Extensions;
using TraktPlugin.TraktAPI.DataStructures;

namespace TraktPlugin.GUI
{
    public class GUIMovieListItem : GUIListItem
    {
        /// <summary>
        /// The id of the window that contains the gui list items (facade)
        /// </summary>
        private int WindowID { get; set; }

        public GUIMovieListItem(string strLabel, int windowId) : base(strLabel.RemapHighOrderChars())
        {
            this.WindowID = windowId;
        }

        public string Date { get; set; }

        public TraktMovie Movie { get; set; }

        public bool IsNextPageItem { get; set; }
        public bool IsPrevPageItem { get; set; }

        /// <summary>
        /// Images attached to a gui list item
        /// </summary>
        public GUITraktImage Images
        {
            get { return _Images; }
            set
            {
                _Images = value;
                var notifier = value as INotifyPropertyChanged;
                if (notifier != null) notifier.PropertyChanged += (s, e) =>
                {
                    if (s is GUITraktImage && e.PropertyName == "Poster")
                        SetImageToGui((s as GUITraktImage).MovieImages.Poster.LocalImageFilename(ArtworkType.MoviePoster));
                    if (s is GUITraktImage && e.PropertyName == "Fanart")
                        this.UpdateItemIfSelected(WindowID, ItemId);
                };
            }
        } protected GUITraktImage _Images;

        /// <summary>
        /// Set this to true to stop downloading any images
        /// e.g. when exiting the window
        /// </summary>
        internal static bool StopDownload { get; set; }
        
        /// <summary>
        /// Download all images attached to the GUI List Control
        /// TODO: Make part of a GUI Base Window
        /// </summary>
        /// <param name="itemsWithThumbs">List of images to get</param>
        internal static void GetImages(List<GUITraktImage> itemsWithThumbs)
        {
            StopDownload = false;

            // split the downloads in 5+ groups and do multithreaded downloading
            int groupSize = (int)Math.Max(1, Math.Floor((double)itemsWithThumbs.Count / 5));
            int groups = (int)Math.Ceiling((double)itemsWithThumbs.Count() / groupSize);

            for (int i = 0; i < groups; i++)
            {
                var groupList = new List<GUITraktImage>();
                for (int j = groupSize * i; j < groupSize * i + (groupSize * (i + 1) > itemsWithThumbs.Count ? itemsWithThumbs.Count - groupSize * i : groupSize); j++)
                {
                    groupList.Add(itemsWithThumbs[j]);
                }

                // sort images so that images that already exist are displayed first
                groupList.Sort((m1, m2) =>
                {
                    int x = Convert.ToInt32(File.Exists(m1.MovieImages.Poster.LocalImageFilename(ArtworkType.MoviePoster))) + Convert.ToInt32(File.Exists(m1.MovieImages.Fanart.LocalImageFilename(ArtworkType.MovieFanart)));
                    int y = Convert.ToInt32(File.Exists(m2.MovieImages.Poster.LocalImageFilename(ArtworkType.MoviePoster))) + Convert.ToInt32(File.Exists(m2.MovieImages.Fanart.LocalImageFilename(ArtworkType.MovieFanart)));
                    return y.CompareTo(x);
                });

                new Thread(delegate(object o)
                {
                    var items = (List<GUITraktImage>)o;
                    foreach (var item in items)
                    {
                        #region Poster
                        // stop download if we have exited window
                        if (StopDownload) break;

                        string remoteThumb = item.MovieImages.Poster.ThumbSize;
                        string localThumb = item.MovieImages.Poster.LocalImageFilename(ArtworkType.MoviePoster);

                        if (!string.IsNullOrEmpty(remoteThumb) && !string.IsNullOrEmpty(localThumb))
                        {
                            if (GUIImageHandler.DownloadImage(remoteThumb, localThumb))
                            {
                                // notify that image has been downloaded
                                item.NotifyPropertyChanged("Poster");
                            }
                        }
                        #endregion

                        #region Fanart
                        // stop download if we have exited window
                        if (StopDownload) break;
                        if (!TraktSettings.DownloadFanart) continue;

                        string remoteFanart = TraktSettings.DownloadFullSizeFanart ? item.MovieImages.Fanart.FullSize : item.MovieImages.Fanart.MediumSize;
                        string localFanart = item.MovieImages.Fanart.LocalImageFilename(ArtworkType.MovieFanart);

                        if (!string.IsNullOrEmpty(remoteFanart) && !string.IsNullOrEmpty(localFanart))
                        {
                            if (GUIImageHandler.DownloadImage(remoteFanart, localFanart))
                            {
                                // notify that image has been downloaded
                                item.NotifyPropertyChanged("Fanart");
                            }
                        }
                        #endregion
                    }
                })
                {
                    IsBackground = true,
                    Name = "ImageDownloader" + i.ToString()
                }.Start(groupList);
            }
        }

        /// <summary>
        /// Loads an Image from memory into a facade item
        /// </summary>
        /// <param name="imageFilePath">Filename of image</param>
        protected void SetImageToGui(string imageFilePath)
        {
            if (string.IsNullOrEmpty(imageFilePath) || Movie == null) return;

            // determine the overlay to add to poster
            var mainOverlay = MainOverlayImage.None;

            // don't show watchlist overlay in personal watchlist window
            if (WindowID == (int)TraktGUIWindows.WatchedListMovies)
            {
                if ((GUIWatchListMovies.CurrentUser != TraktSettings.Username) && Movie.IsWatchlisted())
                    mainOverlay = MainOverlayImage.Watchlist;
                else if (Movie.IsWatched())
                    mainOverlay = MainOverlayImage.Seenit;
            }
            else
            {
                if (Movie.IsWatchlisted())
                    mainOverlay = MainOverlayImage.Watchlist;
                else if (Movie.IsWatched())
                    mainOverlay = MainOverlayImage.Seenit;
            }

            // add additional overlay if applicable
            if (Movie.IsCollected())
                mainOverlay |= MainOverlayImage.Library;

            RatingOverlayImage ratingOverlay = GUIImageHandler.GetRatingOverlay(Movie.UserRating());

            // get a reference to a MediaPortal Texture Identifier
            string suffix = mainOverlay.ToString().Replace(", ", string.Empty) + Enum.GetName(typeof(RatingOverlayImage), ratingOverlay);
            string texture = GUIImageHandler.GetTextureIdentFromFile(imageFilePath, suffix);

            // build memory image
            Image memoryImage = null;
            if (mainOverlay != MainOverlayImage.None || ratingOverlay != RatingOverlayImage.None)
            {
                memoryImage = GUIImageHandler.DrawOverlayOnPoster(imageFilePath, mainOverlay, ratingOverlay);
                if (memoryImage == null) return;

                // load texture into facade item
                if (GUITextureManager.LoadFromMemory(memoryImage, texture, 0, 0, 0) > 0)
                {
                    ThumbnailImage = texture;
                    IconImage = texture;
                    IconImageBig = texture;
                }
            }
            else
            {
                ThumbnailImage = imageFilePath;
                IconImage = imageFilePath;
                IconImageBig = imageFilePath;
            }

            // if selected and is current window force an update of thumbnail
            this.UpdateItemIfSelected(WindowID, ItemId);
        }
    }
}
