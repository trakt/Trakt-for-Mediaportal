using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using MediaPortal.GUI.Library;
using TraktPlugin.Cache;
using TraktPlugin.TmdbAPI.DataStructures;
using TraktPlugin.Extensions;
using TraktPlugin.TraktAPI.DataStructures;

namespace TraktPlugin.GUI
{
    public class GUIPersonListItem : GUIListItem
    {
        /// <summary>
        /// The id of the window that contains the gui list items (facade)
        /// </summary>
        private int WindowID { get; set; }

        public GUIPersonListItem(string strLabel, int windowID) : base(strLabel.RemapHighOrderChars())
        {
            this.WindowID = windowID;
        }

        public TraktPersonSummary Person { get; set; }
        public Credit CreditType { get; set; }
        public TraktJob Job { get; set; }
        public TraktCharacter Character { get; set; }

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
                    if (s is GUITmdbImage && e.PropertyName == "HeadShot")
                        SetImageToGui(TmdbCache.GetPersonHeadshotFilename((s as GUITmdbImage).PeopleImages));
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
        internal static void GetImages(List<GUITmdbImage> itemsWithThumbs, bool downloadFanart = true)
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
                        if (item.PeopleImages != null)
                        {
                            // stop download if we have exited window
                            if (StopDownload) break;

                            var peopleImages = TmdbCache.GetPersonImages(item.PeopleImages.Id);
                            if (peopleImages == null)
                                return;

                            item.PeopleImages = peopleImages;

                            string remoteThumb = TmdbCache.GetPersonHeadshotUrl(peopleImages);
                            string localThumb = TmdbCache.GetPersonHeadshotFilename(peopleImages);

                            if (!string.IsNullOrEmpty(remoteThumb) && !string.IsNullOrEmpty(localThumb))
                            {
                                if (GUIImageHandler.DownloadImage(remoteThumb, localThumb))
                                {
                                    // notify that image has been downloaded
                                    item.NotifyPropertyChanged("HeadShot");
                                }
                            }

                            // not all methods have Fanart for people
                            // only get it, if we need it
                            //if (downloadFanart && item.PeopleImages.Fanart != null)
                            //{
                            //    remoteThumb = TraktSettings.DownloadFullSizeFanart ? item.PeopleImages.Fanart.FullSize : item.PeopleImages.Fanart.MediumSize;
                            //    localThumb = item.PeopleImages.Fanart.LocalImageFilename(ArtworkType.PersonFanart);

                            //    if (!string.IsNullOrEmpty(remoteThumb) && !string.IsNullOrEmpty(localThumb))
                            //    {
                            //        if (GUIImageHandler.DownloadImage(remoteThumb, localThumb))
                            //        {
                            //            // notify that image has been downloaded
                            //            item.NotifyPropertyChanged("Fanart");
                            //        }
                            //    }
                            //}
                        }
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

            ThumbnailImage = imageFilePath;
            IconImage = imageFilePath;
            IconImageBig = imageFilePath;

            // if selected and is current window force an update of thumbnail
            this.UpdateItemIfSelected(WindowID, ItemId);
        }
    }
}
