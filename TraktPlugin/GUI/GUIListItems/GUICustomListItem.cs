using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using MediaPortal.GUI.Library;
using TraktPlugin.TraktAPI.v1.DataStructures;

namespace TraktPlugin.GUI
{
    public class GUICustomListItem : GUIListItem
    {
        /// <summary>
        /// The id of the window that contains the gui list items (facade)
        /// </summary>
        private int WindowID { get; set; }

        public GUICustomListItem(string strLabel, int windowID) : base(strLabel.RemapHighOrderChars())
        {
            this.WindowID = windowID;
        }

        /// <summary>
        /// Images attached to a gui list item
        /// </summary>
        public GUIImage Images
        {
            get { return _Images; }
            set
            {
                _Images = value;
                var notifier = value as INotifyPropertyChanged;
                if (notifier != null) notifier.PropertyChanged += (s, e) =>
                {
                    if (s is GUIImage && e.PropertyName == "MoviePoster")
                        SetImageToGui((s as GUIImage).MovieImages.Poster.LocalImageFilename(ArtworkType.MoviePoster));
                    if (s is GUIImage && e.PropertyName == "ShowPoster")
                        SetImageToGui((s as GUIImage).ShowImages.Poster.LocalImageFilename(ArtworkType.ShowPoster));
                    // re-size season posters to same as series/movie posters
                    if (s is GUIImage && e.PropertyName == "Season")
                        SetImageToGui((s as GUIImage).ShowImages.Season.LocalImageFilename(ArtworkType.SeasonPoster), new Size(300, 434));
                    if (s is GUIImage && e.PropertyName == "Fanart")
                        this.UpdateItemIfSelected(WindowID, ItemId);
                };
            }
        }
        protected GUIImage _Images;

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
        internal static void GetImages(List<GUIImage> itemsWithThumbs)
        {
            StopDownload = false;

            // split the downloads in 5+ groups and do multithreaded downloading
            int groupSize = (int)Math.Max(1, Math.Floor((double)itemsWithThumbs.Count / 5));
            int groups = (int)Math.Ceiling((double)itemsWithThumbs.Count() / groupSize);

            for (int i = 0; i < groups; i++)
            {
                var groupList = new List<GUIImage>();
                for (int j = groupSize * i; j < groupSize * i + (groupSize * (i + 1) > itemsWithThumbs.Count ? itemsWithThumbs.Count - groupSize * i : groupSize); j++)
                {
                    groupList.Add(itemsWithThumbs[j]);
                }

                new Thread(delegate(object o)
                {
                    var items = (List<GUIImage>)o;
                    foreach (var item in items)
                    {
                        string remoteThumb = string.Empty;
                        string localThumb = string.Empty;

                        #region Shows
                        if (item.ShowImages != null)
                        {
                            #region Show Poster
                            // stop download if we have exited window
                            if (StopDownload) break;

                            remoteThumb = item.ShowImages.Poster.ToSmallPoster();
                            localThumb = item.ShowImages.Poster.LocalImageFilename(ArtworkType.ShowPoster);

                            if (!string.IsNullOrEmpty(remoteThumb) && !string.IsNullOrEmpty(localThumb))
                            {
                                if (GUIImageHandler.DownloadImage(remoteThumb, localThumb))
                                {
                                    // notify that image has been downloaded
                                    item.NotifyPropertyChanged("ShowPoster");
                                }
                            }
                            #endregion

                            #region Show Season Poster
                            // stop download if we have exited window
                            if (StopDownload) break;

                            remoteThumb = item.ShowImages.Season;
                            localThumb = item.ShowImages.Season.LocalImageFilename(ArtworkType.SeasonPoster);

                            if (!string.IsNullOrEmpty(remoteThumb) && !string.IsNullOrEmpty(localThumb))
                            {
                                if (GUIImageHandler.DownloadImage(remoteThumb, localThumb))
                                {
                                    // notify that image has been downloaded
                                    item.NotifyPropertyChanged("Season");
                                }
                            }
                            #endregion

                            #region Fanart
                            // stop download if we have exited window
                            if (StopDownload) break;
                            if (!TraktSettings.DownloadFanart) continue;

                            string remoteFanart = item.ShowImages.Fanart.ToSmallFanart();
                            string localFanart = item.ShowImages.Fanart.LocalImageFilename(ArtworkType.ShowFanart);

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
                        #endregion

                        #region Movies
                        if (item.MovieImages != null)
                        {
                            #region Movie Poster
                            // stop download if we have exited window
                            if (StopDownload) break;

                            remoteThumb = item.MovieImages.Poster.ToSmallPoster();
                            localThumb = item.MovieImages.Poster.LocalImageFilename(ArtworkType.MoviePoster);

                            if (!string.IsNullOrEmpty(remoteThumb) && !string.IsNullOrEmpty(localThumb))
                            {
                                if (GUIImageHandler.DownloadImage(remoteThumb, localThumb))
                                {
                                    // notify that image has been downloaded
                                    item.NotifyPropertyChanged("MoviePoster");
                                }
                            }
                            #endregion

                            #region Fanart
                            // stop download if we have exited window
                            if (StopDownload) break;
                            if (!TraktSettings.DownloadFanart) continue;

                            string remoteFanart = item.MovieImages.Fanart.ToSmallFanart();
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
            SetImageToGui(imageFilePath, new Size());
        }

        protected void SetImageToGui(string imageFilePath, Size size)
        {
            if (string.IsNullOrEmpty(imageFilePath)) return;

            // determine the overlay to add to poster
            var mainOverlay = MainOverlayImage.None;
            var ratingOverlay = RatingOverlayImage.None;

            if (TVTag is TraktUserListItem)
            {
                var listItem = TVTag as TraktUserListItem;
                if (listItem == null) return;

                if (listItem.InWatchList)
                    mainOverlay = MainOverlayImage.Watchlist;
                else if (listItem.Watched)
                    mainOverlay = MainOverlayImage.Seenit;

                // add additional overlay if applicable
                if (listItem.InCollection)
                    mainOverlay |= MainOverlayImage.Library;

                ratingOverlay = GUIImageHandler.GetRatingOverlay(listItem.RatingAdvanced);
            }
            else if (TVTag is TraktActivity.Activity)
            {
                var activity = TVTag as TraktActivity.Activity;
                if (activity == null) return;

                var movie = activity.Movie;
                var show = activity.Show;

                if (movie != null && movie.InWatchList)
                    mainOverlay = MainOverlayImage.Watchlist;
                else if (show != null && show.InWatchList)
                    mainOverlay = MainOverlayImage.Watchlist;
                else if (movie != null && movie.Watched)
                    mainOverlay = MainOverlayImage.Seenit;

                // add additional overlay if applicable
                if (movie != null && movie.InCollection)
                    mainOverlay |= MainOverlayImage.Library;

                if (movie != null)
                    ratingOverlay = GUIImageHandler.GetRatingOverlay(movie.RatingAdvanced);
                else
                    ratingOverlay = GUIImageHandler.GetRatingOverlay(show.RatingAdvanced);
            }

            // get a reference to a MediaPortal Texture Identifier
            string suffix = mainOverlay.ToString().Replace(", ", string.Empty) + Enum.GetName(typeof(RatingOverlayImage), ratingOverlay);
            string texture = GUIImageHandler.GetTextureIdentFromFile(imageFilePath, suffix);

            // build memory image
            Image memoryImage = null;
            if (mainOverlay != MainOverlayImage.None || ratingOverlay != RatingOverlayImage.None)
            {
                memoryImage = GUIImageHandler.DrawOverlayOnPoster(imageFilePath, mainOverlay, ratingOverlay, size);
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
