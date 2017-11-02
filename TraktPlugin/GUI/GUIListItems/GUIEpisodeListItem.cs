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

namespace TraktPlugin.GUI
{
    public class GUIEpisodeListItem : GUIListItem
    {
        /// <summary>
        /// The id of the window that contains the gui list items (facade)
        /// </summary>
        public int WindowID { get; set; }

        public GUIEpisodeListItem(string strLabel, int windowID) : base(strLabel.RemapHighOrderChars())
        {
            this.WindowID = windowID;
        }

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
                    if (s is GUITmdbImage && e.PropertyName == "ShowScreenStill")
                    {
                        SetImageToGui(TmdbCache.GetEpisodeThumbFilename((s as GUITmdbImage).EpisodeImages));
                    }
                    else if (s is GUITmdbImage && e.PropertyName == "ShowScreenStillAsBackdrop")
                    {
                        SetImageToGui(TmdbCache.GetShowBackdropFilename((s as GUITmdbImage).ShowImages, true));
                    }
                    else if (s is GUITraktImage && e.PropertyName == "Fanart")
                    {
                        this.UpdateItemIfSelected(WindowID, ItemId);
                    }
                };
            }
        } protected GUITmdbImage _Images;

        public string Date { get; set; }
        public string SelectedIndex { get; set; }

        public TraktEpisode Episode { get; set; }
        public TraktShow Show { get; set; }

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
                //    int x = Convert.ToInt32(File.Exists(s1.EpisodeImages.ScreenShot.LocalImageFilename(ArtworkType.EpisodeImage))) + (s1.ShowImages == null ? 0 : Convert.ToInt32(File.Exists(s1.ShowImages.Fanart.LocalImageFilename(ArtworkType.ShowFanart))));
                //    int y = Convert.ToInt32(File.Exists(s2.EpisodeImages.ScreenShot.LocalImageFilename(ArtworkType.EpisodeImage))) + (s2.ShowImages == null ? 0 : Convert.ToInt32(File.Exists(s2.ShowImages.Fanart.LocalImageFilename(ArtworkType.ShowFanart))));
                //    return y.CompareTo(x);
                //});

                new Thread(delegate(object o)
                {
                    var items = (List<GUITmdbImage>)o;
                    foreach (var item in items)
                    {
                        #region Episode Image
                        // stop download if we have exited window
                        if (StopDownload) break;

                        bool downloadShowBackdrop = false;

                        string remoteThumb = string.Empty;
                        string localThumb = string.Empty;

                        TmdbEpisodeImages episodeImages = null;
                        TmdbShowImages showImages = null;

                        // Don't try to get episode images that air after today, they most likely do not exist and contain spoilers
                        if (item.EpisodeImages.AirDate != null && item.EpisodeImages.AirDate.ToDateTime() <= Convert.ToDateTime(DateTime.Now.ToShortDateString()))
                        {
                            episodeImages = TmdbCache.GetEpisodeImages(item.EpisodeImages.Id, item.EpisodeImages.Season, item.EpisodeImages.Episode);
                            if (episodeImages != null)
                            {
                                item.EpisodeImages = episodeImages;
                            }
                        }

                        showImages = TmdbCache.GetShowImages(item.EpisodeImages.Id);
                        if (showImages != null)
                        {
                            item.ShowImages = showImages;
                        }

                        // if the episode image exists get it, otherwise get the show fanart
                        if (episodeImages != null && episodeImages.Stills != null && episodeImages.Stills.Count > 0)
                        {
                            remoteThumb = TmdbCache.GetEpisodeThumbUrl(episodeImages);
                            localThumb = TmdbCache.GetEpisodeThumbFilename(episodeImages);
                        }
                        else
                        {
                            downloadShowBackdrop = true;
                            
                            // use fanart for episode image, get one with a logo
                            remoteThumb = TmdbCache.GetShowBackdropUrl(item.ShowImages, true);
                            localThumb = TmdbCache.GetShowBackdropFilename(item.ShowImages, true);
                        }

                        if (!string.IsNullOrEmpty(remoteThumb) && !string.IsNullOrEmpty(localThumb))
                        {
                            if (GUIImageHandler.DownloadImage(remoteThumb, localThumb))
                            {
                                if (StopDownload) break;

                                // notify that image has been downloaded
                                item.NotifyPropertyChanged(downloadShowBackdrop ? "ShowScreenStillAsBackdrop" : "ShowScreenStill");
                            }
                        }
                        #endregion

                        #region Fanart
                        // stop download if we have exited window
                        if (StopDownload) break;
                        if (!TraktSettings.DownloadFanart) continue;

                        remoteThumb = TmdbCache.GetShowBackdropUrl(item.ShowImages);
                        localThumb = TmdbCache.GetShowBackdropFilename(item.ShowImages);

                        if (!string.IsNullOrEmpty(remoteThumb) && !string.IsNullOrEmpty(localThumb))
                        {
                            if (GUIImageHandler.DownloadImage(remoteThumb, localThumb))
                            {
                                if (StopDownload) break;

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

            if (Show == null || Episode == null)
                return;

            // determine the overlay to add to poster
            MainOverlayImage mainOverlay = MainOverlayImage.None;

            // don't show watchlist overlay in personal watchlist window
            if (WindowID == (int)TraktGUIWindows.WatchedListEpisodes)
            {
                if ((GUIWatchListEpisodes.CurrentUser != TraktSettings.Username) && Episode.IsWatchlisted())
                    mainOverlay = MainOverlayImage.Watchlist;
                else if (Episode.IsWatched(Show))
                    mainOverlay = MainOverlayImage.Seenit;
            }
            else
            {
                if (Episode.IsWatchlisted())
                    mainOverlay = MainOverlayImage.Watchlist;
                else if (Episode.IsWatched(Show))
                    mainOverlay = MainOverlayImage.Seenit;
            }

            // add additional overlay if applicable
            if (Episode.IsCollected(Show))
                mainOverlay |= MainOverlayImage.Library;

            RatingOverlayImage ratingOverlay = GUIImageHandler.GetRatingOverlay(Episode.UserRating(Show));

            // get a reference to a MediaPortal Texture Identifier
            string suffix = mainOverlay.ToString().Replace(", ", string.Empty) + Enum.GetName(typeof(RatingOverlayImage), ratingOverlay);
            string texture = GUIImageHandler.GetTextureIdentFromFile(imageFilePath, suffix);

            // build memory image, resize thumbnail in case its a fanart
            Image memoryImage = null;
            if (mainOverlay != MainOverlayImage.None || ratingOverlay != RatingOverlayImage.None)
            {
                memoryImage = GUIImageHandler.DrawOverlayOnEpisodeThumb(imageFilePath, mainOverlay, ratingOverlay, new Size(400, 225));
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
