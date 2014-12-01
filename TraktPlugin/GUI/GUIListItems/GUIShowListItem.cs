using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using MediaPortal.GUI.Library;
using TraktPlugin.TraktAPI.DataStructures;

namespace TraktPlugin.GUI
{
    public class GUIShowListItem : GUIListItem
    {
        /// <summary>
        /// The id of the window that contains the gui list items (facade)
        /// </summary>
        private int WindowID { get; set; }

        public GUIShowListItem(string strLabel, int windowID) : base(strLabel.RemapHighOrderChars())
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
                    if (s is GUIImage && e.PropertyName == "Poster")
                        SetImageToGui((s as GUIImage).ShowImages.Poster.LocalImageFilename(ArtworkType.ShowPoster));
                    if (s is GUIImage && e.PropertyName == "Fanart")
                        this.UpdateItemIfSelected(WindowID, ItemId);
                };
            }
        } protected GUIImage _Images;

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

                // sort images so that images that already exist are displayed first
                groupList.Sort((s1, s2) =>
                {
                    int x = Convert.ToInt32(File.Exists(s1.ShowImages.Poster.LocalImageFilename(ArtworkType.ShowPoster))) + Convert.ToInt32(File.Exists(s1.ShowImages.Fanart.LocalImageFilename(ArtworkType.ShowFanart)));
                    int y = Convert.ToInt32(File.Exists(s2.ShowImages.Poster.LocalImageFilename(ArtworkType.ShowPoster))) + Convert.ToInt32(File.Exists(s2.ShowImages.Fanart.LocalImageFilename(ArtworkType.ShowFanart)));
                    return y.CompareTo(x);
                });

                new Thread(delegate(object o)
                {
                    var items = (List<GUIImage>)o;
                    foreach (var item in items)
                    {
                        #region Poster
                        // stop download if we have exited window
                        if (StopDownload) break;

                        string remoteThumb = item.ShowImages.Poster.ThumbSize;
                        string localThumb = item.ShowImages.Poster.LocalImageFilename(ArtworkType.ShowPoster);

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

                        string remoteFanart = TraktSettings.DownloadFullSizeFanart ? item.ShowImages.Fanart.FullSize : item.ShowImages.Fanart.MediumSize;
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
            var show = TVTag as TraktShowSummary;
            var mainOverlay = MainOverlayImage.None;

            // don't show watchlist overlay in personal watchlist window
            if (WindowID == (int)TraktGUIWindows.WatchedListShows)
            {
                if ((GUIWatchListShows.CurrentUser != TraktSettings.Username) && show.IsWatchlisted())
                    mainOverlay = MainOverlayImage.Watchlist;
                //else if (show.Watched)
                //    mainOverlay = MainOverlayImage.Seenit;
            }
            else
            {
                if (show.IsWatchlisted())
                    mainOverlay = MainOverlayImage.Watchlist;
                //else if (show.Watched)
                //    mainOverlay = MainOverlayImage.Seenit;
            }

            RatingOverlayImage ratingOverlay = GUIImageHandler.GetRatingOverlay(show.UserRating());

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
