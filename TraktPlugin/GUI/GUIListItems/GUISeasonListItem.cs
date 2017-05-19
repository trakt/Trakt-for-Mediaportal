using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading;
using MediaPortal.GUI.Library;
using TraktPlugin.Cache;
using TraktAPI.DataStructures;

namespace TraktPlugin.GUI
{
    public class GUISeasonListItem : GUIListItem
    {
        /// <summary>
        /// The id of the window that contains the gui list items (facade)
        /// </summary>
        private int WindowID { get; set; }

        public GUISeasonListItem(string strLabel, int windowID) : base(strLabel)
        {
            this.WindowID = windowID;
        }

        public TraktShowSummary Show { get; set; }
        public TraktSeasonSummary Season { get; set; }

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
                    if (s is GUITmdbImage && e.PropertyName == "SeasonPoster")
                    {                    
                        SetImageToGui(TmdbCache.GetSeasonPosterFilename((s as GUITmdbImage).SeasonImages));
                    }
                    else if (s is GUITmdbImage && e.PropertyName == "ShowPoster")
                    {
                        SetImageToGui(TmdbCache.GetShowPosterFilename((s as GUITmdbImage).ShowImages));
                    }                    
                    else if (s is GUITmdbImage && e.PropertyName == "Fanart")
                    {
                        this.UpdateItemIfSelected(WindowID, ItemId);
                    }
                };
            }
        } protected GUITmdbImage _Images;

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

                // sort images so that images that already exist are displayed first
                //groupList.Sort((s1, s2) =>
                //{
                //    int x = Convert.ToInt32(File.Exists(s1.SeasonImages.Poster.LocalImageFilename(ArtworkType.SeasonPoster)));
                //    int y = Convert.ToInt32(File.Exists(s2.SeasonImages.Poster.LocalImageFilename(ArtworkType.SeasonPoster)));
                //    return y.CompareTo(x);
                //});

                new Thread(obj =>
                {
                    var items = (List<GUITmdbImage>)obj;
                    if (items == null || items.Count == 0)
                        return;

                    // all seasons should have the same show reference
                    var showImages = TmdbCache.GetShowImages(items.First().SeasonImages.Id);
                    if (showImages != null)
                    {
                        items.ForEach(s => s.ShowImages = showImages);
                    }

                    foreach (var item in items)
                    {
                        #region Season Poster
                        // stop download if we have exited window
                        if (StopDownload) break;

                        bool downloadShowPoster = false;

                        string remoteThumb = string.Empty;
                        string localThumb = string.Empty;

                        var seasonImages = TmdbCache.GetSeasonImages(item.SeasonImages.Id, item.SeasonImages.Season);
                        if (seasonImages != null)
                        {
                            item.SeasonImages = seasonImages;
                        }

                        if (seasonImages != null && seasonImages.Posters != null && seasonImages.Posters.Count > 0)
                        {
                            remoteThumb = TmdbCache.GetSeasonPosterUrl(seasonImages);
                            localThumb = TmdbCache.GetSeasonPosterFilename(seasonImages);
                        }
                        else
                        {
                            downloadShowPoster = true;

                            // use show image if season poster not available
                            remoteThumb = TmdbCache.GetShowPosterUrl(showImages);
                            localThumb = TmdbCache.GetShowPosterFilename(showImages);
                        }

                        if (!string.IsNullOrEmpty(remoteThumb) && !string.IsNullOrEmpty(localThumb))
                        {
                            if (GUIImageHandler.DownloadImage(remoteThumb, localThumb))
                            {
                                // notify that image has been downloaded
                                item.NotifyPropertyChanged(downloadShowPoster ? "ShowPoster" : "SeasonPoster");
                            }
                        }
                        #endregion

                        #region Fanart
                        // stop download if we have exited window
                        if (StopDownload) break;
                        if (!TraktSettings.DownloadFanart) continue;

                        string remoteFanart = TmdbCache.GetShowBackdropUrl(showImages);
                        string localFanart = TmdbCache.GetShowBackdropFilename(showImages);

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
            if (string.IsNullOrEmpty(imageFilePath)) return;

            // determine the overlays to add to poster
            var mainOverlay = MainOverlayImage.None;

            if (Season.IsWatchlisted(Show))
                mainOverlay = MainOverlayImage.Watchlist;
            else if (Season.IsWatched(Show))
                mainOverlay = MainOverlayImage.Seenit;

            // add additional overlay if applicable
            if (Season.IsCollected(Show))
                mainOverlay |= MainOverlayImage.Library;

            RatingOverlayImage ratingOverlay = GUIImageHandler.GetRatingOverlay(Season.UserRating(Show));

            // get a reference to a MediaPortal Texture Identifier
            string suffix = Enum.GetName(typeof(MainOverlayImage), mainOverlay) + Enum.GetName(typeof(RatingOverlayImage), ratingOverlay);
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
