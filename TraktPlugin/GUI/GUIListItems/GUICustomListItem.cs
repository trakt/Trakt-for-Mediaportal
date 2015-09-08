using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading;
using MediaPortal.GUI.Library;
using TraktPlugin.Extensions;
using TraktPlugin.TraktAPI.DataStructures;
using TraktPlugin.TraktAPI.Enums;

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

        public TraktItemType Type { get; set; }
        public TraktMovie Movie { get; set; }
        public TraktShow Show { get; set; }
        public TraktEpisode Episode { get; set; }
        public TraktSeason Season { get; set; }
        public TraktPerson Person { get; set; }
        public TraktList List { get; set; }

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
                    if (s is GUITraktImage && e.PropertyName == "MoviePoster")
                        SetImageToGui((s as GUITraktImage).MovieImages.Poster.LocalImageFilename(ArtworkType.MoviePoster));
                    if (s is GUITraktImage && e.PropertyName == "ShowPoster")
                        SetImageToGui((s as GUITraktImage).ShowImages.Poster.LocalImageFilename(ArtworkType.ShowPoster));
                    if (s is GUITraktImage && e.PropertyName == "HeadShot")
                        SetImageToGui((s as GUITraktImage).PeopleImages.HeadShot.LocalImageFilename(ArtworkType.PersonHeadshot));
                    // re-size season posters to same as series/movie posters
                    if (s is GUITraktImage && e.PropertyName == "Season")
                        SetImageToGui((s as GUITraktImage).SeasonImages.Poster.LocalImageFilename(ArtworkType.SeasonPoster), new Size(300, 434));
                    if (s is GUITraktImage && e.PropertyName == "Fanart")
                        this.UpdateItemIfSelected(WindowID, ItemId);
                };
            }
        }
        protected GUITraktImage _Images;

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

                new Thread(delegate(object o)
                {
                    var items = (List<GUITraktImage>)o;
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

                            remoteThumb = item.ShowImages.Poster.ThumbSize;
                            localThumb = item.ShowImages.Poster.LocalImageFilename(ArtworkType.ShowPoster);

                            if (!string.IsNullOrEmpty(remoteThumb) && !string.IsNullOrEmpty(localThumb))
                            {
                                if (GUIImageHandler.DownloadImage(remoteThumb, localThumb))
                                {
                                    if (StopDownload) break;

                                    // notify that image has been downloaded
                                    item.NotifyPropertyChanged("ShowPoster");
                                }
                            }
                            #endregion

                            #region Fanart
                            // stop download if we have exited window
                            if (StopDownload) break;
                            if (!TraktSettings.DownloadFanart) continue;

                            string remoteFanart = TraktSettings.DownloadFullSizeFanart ? item.ShowImages.Fanart.FullSize : item.ShowImages.Fanart.MediumSize;
                            string localFanart = item.ShowImages.Fanart.LocalImageFilename(ArtworkType.ShowFanart);

                            if (!string.IsNullOrEmpty(remoteFanart) && !string.IsNullOrEmpty(localFanart))
                            {
                                if (GUIImageHandler.DownloadImage(remoteFanart, localFanart))
                                {
                                    if (StopDownload) break;

                                    // notify that image has been downloaded
                                    item.NotifyPropertyChanged("Fanart");
                                }
                            }
                            #endregion
                        }
                        #endregion

                        #region Seasons
                        if (item.SeasonImages != null)
                        {
                            #region Show Season Poster
                            // stop download if we have exited window
                            if (StopDownload) break;

                            remoteThumb = item.SeasonImages.Poster.ThumbSize;
                            localThumb = item.SeasonImages.Poster.LocalImageFilename(ArtworkType.SeasonPoster);

                            if (!string.IsNullOrEmpty(remoteThumb) && !string.IsNullOrEmpty(localThumb))
                            {
                                if (GUIImageHandler.DownloadImage(remoteThumb, localThumb))
                                {
                                    if (StopDownload) break;

                                    // notify that image has been downloaded
                                    item.NotifyPropertyChanged("Season");
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

                            remoteThumb = item.MovieImages.Poster.ThumbSize;
                            localThumb = item.MovieImages.Poster.LocalImageFilename(ArtworkType.MoviePoster);

                            if (!string.IsNullOrEmpty(remoteThumb) && !string.IsNullOrEmpty(localThumb))
                            {
                                if (GUIImageHandler.DownloadImage(remoteThumb, localThumb))
                                {
                                    if (StopDownload) break;

                                    // notify that image has been downloaded
                                    item.NotifyPropertyChanged("MoviePoster");
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
                                    if (StopDownload) break;

                                    // notify that image has been downloaded
                                    item.NotifyPropertyChanged("Fanart");
                                }
                            }
                            #endregion
                        }
                        #endregion

                        #region People

                        if (item.PeopleImages != null)
                        {
                            #region Headshot
                            // stop download if we have exited window
                            if (StopDownload) break;

                            remoteThumb = item.PeopleImages.HeadShot.FullSize;
                            localThumb = item.PeopleImages.HeadShot.LocalImageFilename(ArtworkType.PersonHeadshot);

                            if (!string.IsNullOrEmpty(remoteThumb) && !string.IsNullOrEmpty(localThumb))
                            {
                                if (GUIImageHandler.DownloadImage(remoteThumb, localThumb))
                                {
                                    if (StopDownload) break;

                                    // notify that image has been downloaded
                                    item.NotifyPropertyChanged("HeadShot");
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

            if (TVTag is TraktListItem)
            {
                var listItem = TVTag as TraktListItem;
                if (listItem == null) return;

                if (listItem.IsWatchlisted())
                    mainOverlay = MainOverlayImage.Watchlist;
                else if (listItem.IsWatched())
                    mainOverlay = MainOverlayImage.Seenit;

                // add additional overlay if applicable
                if (listItem.IsCollected())
                    mainOverlay |= MainOverlayImage.Library;

                ratingOverlay = GUIImageHandler.GetRatingOverlay(listItem.UserRating());
            }
            else if (TVTag is TraktCommentItem)
            {
                var commentItem = TVTag as TraktCommentItem;
                if (commentItem == null) return;

                if (commentItem.IsWatchlisted())
                    mainOverlay = MainOverlayImage.Watchlist;
                else if (commentItem.IsWatched())
                    mainOverlay = MainOverlayImage.Seenit;

                if (commentItem.IsCollected())
                    mainOverlay |= MainOverlayImage.Library;

                ratingOverlay = GUIImageHandler.GetRatingOverlay(commentItem.UserRating());
            }
            else if (TVTag is TraktActivity.Activity)
            {
                var activity = TVTag as TraktActivity.Activity;
                if (activity == null) return;

                var movie = activity.Movie;
                var show = activity.Show;

                if (movie != null && movie.IsWatchlisted())
                    mainOverlay = MainOverlayImage.Watchlist;
                else if (show != null && show.IsWatchlisted())
                    mainOverlay = MainOverlayImage.Watchlist;
                else if (movie != null && movie.IsWatched())
                    mainOverlay = MainOverlayImage.Seenit;

                // add additional overlay if applicable
                if (movie != null && movie.IsCollected())
                    mainOverlay |= MainOverlayImage.Library;

                if (movie != null)
                    ratingOverlay = GUIImageHandler.GetRatingOverlay(movie.UserRating());
                else if (Episode != null && Show != null)
                    ratingOverlay = GUIImageHandler.GetRatingOverlay(Episode.UserRating(Show));
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
