using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using MediaPortal.GUI.Library;
using TraktPlugin.Extensions;

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
                    if (s is GUITraktImage && e.PropertyName == "HeadShot")
                        SetImageToGui((s as GUITraktImage).PeopleImages.HeadShot.LocalImageFilename(ArtworkType.PersonHeadshot));
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
                        if (item.PeopleImages != null)
                        {
                            // stop download if we have exited window
                            if (StopDownload) break;

                            string remoteThumb = item.PeopleImages.HeadShot.FullSize;
                            string localThumb = item.PeopleImages.HeadShot.LocalImageFilename(ArtworkType.PersonHeadshot);

                            if (!string.IsNullOrEmpty(remoteThumb) && !string.IsNullOrEmpty(localThumb))
                            {
                                if (GUIImageHandler.DownloadImage(remoteThumb, localThumb))
                                {
                                    // notify that image has been downloaded
                                    item.NotifyPropertyChanged("HeadShot");
                                }
                            }

                            // not all methods have Fanart for people
                            if (item.PeopleImages.Fanart != null)
                            {
                                remoteThumb = item.PeopleImages.Fanart.FullSize;
                                localThumb = item.PeopleImages.Fanart.LocalImageFilename(ArtworkType.PersonFanart);

                                if (!string.IsNullOrEmpty(remoteThumb) && !string.IsNullOrEmpty(localThumb))
                                {
                                    if (GUIImageHandler.DownloadImage(remoteThumb, localThumb))
                                    {
                                        // notify that image has been downloaded
                                        item.NotifyPropertyChanged("Fanart");
                                    }
                                }
                            }
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
