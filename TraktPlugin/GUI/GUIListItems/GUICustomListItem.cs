using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading;
using MediaPortal.GUI.Library;
using TraktPlugin.Cache;
using TraktPlugin.Extensions;
using TraktPlugin.TmdbAPI.DataStructures;
using TraktAPI.DataStructures;
using TraktAPI.Enums;

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
        public GUITmdbImage Images
        {
            get { return _Images; }
            set
            {
                _Images = value;
                var notifier = value as INotifyPropertyChanged;
                if (notifier != null) notifier.PropertyChanged += (s, e) =>
                {
                    if (s is GUITmdbImage && e.PropertyName == "MoviePoster")
                        SetImageToGui(TmdbCache.GetMoviePosterFilename((s as GUITmdbImage).MovieImages));
                    if (s is GUITmdbImage && e.PropertyName == "ShowPoster")
                        SetImageToGui(TmdbCache.GetShowPosterFilename((s as GUITmdbImage).ShowImages));
                    if (s is GUITmdbImage && e.PropertyName == "HeadShot")
                        SetImageToGui(TmdbCache.GetPersonHeadshotFilename((s as GUITmdbImage).PeopleImages));
                    if (s is GUITmdbImage && e.PropertyName == "SeasonPoster")
                        SetImageToGui(TmdbCache.GetSeasonPosterFilename((s as GUITmdbImage).SeasonImages));
                    if (s is GUITmdbImage && e.PropertyName == "Fanart")
                        this.UpdateItemIfSelected(WindowID, ItemId);
                };
            }
        }
        protected GUITmdbImage _Images;

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
        internal static void GetImages(List<GUITmdbImage> itemsWithThumbs)
        {
            StopDownload = false;

            // split the downloads in 5+ groups and do multithreaded downloading
            int groupSize = (int)Math.Max(1, Math.Floor((double)itemsWithThumbs.Count / 5));
            int groups = (int)Math.Ceiling((double)itemsWithThumbs.Count() / groupSize);

            for (int i = 0; i < groups; i++)
            {
                var groupList = new List<GUITmdbImage>();
                for (int j = groupSize * i; j < groupSize * i + (groupSize * (i + 1) > itemsWithThumbs.Count ? itemsWithThumbs.Count - groupSize * i : groupSize); j++)
                {
                    groupList.Add(itemsWithThumbs[j]);
                }

                new Thread(delegate(object o)
                {
                    var items = (List<GUITmdbImage>)o;
                    foreach (var item in items)
                    {
                        string remoteThumb = string.Empty;
                        string localThumb = string.Empty;

                        #region Seasons / Episodes
                        if (item.SeasonImages != null)
                        {
                            // check if we have the image in our cache
                            var seasonImages = TmdbCache.GetSeasonImages(item.SeasonImages.Id, item.SeasonImages.Season);
                            if (seasonImages == null)
                                continue;

                            item.SeasonImages = seasonImages;

                            #region Show Season Poster
                            // stop download if we have exited window
                            if (StopDownload) break;

                            remoteThumb = TmdbCache.GetSeasonPosterUrl(seasonImages);
                            localThumb = TmdbCache.GetSeasonPosterFilename(seasonImages);

                            if (!string.IsNullOrEmpty(remoteThumb) && !string.IsNullOrEmpty(localThumb))
                            {
                                if (GUIImageHandler.DownloadImage(remoteThumb, localThumb))
                                {
                                    if (StopDownload) break;

                                    // notify that image has been downloaded
                                    item.NotifyPropertyChanged("SeasonPoster");
                                }
                            }
                            #endregion
                        }
                        #endregion
                        
                        #region Shows / Seasons / Episodes
                        if (item.ShowImages != null)
                        {
                            #region Show Poster

                            var showImages = TmdbCache.GetShowImages(item.ShowImages.Id);
                            if (showImages == null)
                                continue;

                            item.ShowImages = showImages;

                            // don't download the show poster if we have a season poster
                            if (item.SeasonImages == null || item.SeasonImages.Posters == null || item.SeasonImages.Posters.Count == 0)
                            {
                                // stop download if we have exited window
                                if (StopDownload) break;

                                remoteThumb = TmdbCache.GetShowPosterUrl(showImages);
                                localThumb = TmdbCache.GetShowPosterFilename(showImages);

                                if (!string.IsNullOrEmpty(remoteThumb) && !string.IsNullOrEmpty(localThumb))
                                {
                                    if (GUIImageHandler.DownloadImage(remoteThumb, localThumb))
                                    {
                                        if (StopDownload) break;

                                        // notify that image has been downloaded
                                        item.NotifyPropertyChanged("ShowPoster");
                                    }
                                }
                            }
                            #endregion

                            #region Fanart
                            // stop download if we have exited window
                            if (StopDownload) break;
                            if (!TraktSettings.DownloadFanart) continue;

                            string remoteFanart = TmdbCache.GetShowBackdropUrl(showImages); ;
                            string localFanart = TmdbCache.GetShowBackdropFilename(showImages);

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

                        #region Movies
                        if (item.MovieImages != null)
                        {
                            // check if we have the image in our cache
                            var movieImages = TmdbCache.GetMovieImages(item.MovieImages.Id);
                            if (movieImages == null)
                                continue;

                            item.MovieImages = movieImages;

                            #region Movie Poster
                            // stop download if we have exited window
                            if (StopDownload) break;

                            remoteThumb = TmdbCache.GetMoviePosterUrl(movieImages);
                            localThumb = TmdbCache.GetMoviePosterFilename(movieImages);

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

                            string remoteFanart = TmdbCache.GetMovieBackdropUrl(movieImages); ;
                            string localFanart = TmdbCache.GetMovieBackdropFilename(movieImages);

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
                            // check if we have the image in our cache
                            var peopleImages = TmdbCache.GetPersonImages(item.PeopleImages.Id);
                            if (peopleImages == null)
                                continue;

                            item.PeopleImages = peopleImages;

                            #region Headshot
                            // stop download if we have exited window
                            if (StopDownload) break;

                            remoteThumb = TmdbCache.GetPersonHeadshotUrl(peopleImages);
                            localThumb = TmdbCache.GetPersonHeadshotFilename(peopleImages);

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
