using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using MediaPortal.GUI.Library;
using TraktPlugin.TraktAPI.DataStructures;

namespace TraktPlugin.GUI
{
    public class GUIUserListItem : GUIListItem
    {
        private int WindowID { get; set; }

        public GUIUserListItem(string strLabel, int windowID) : base(strLabel)
        {
            WindowID = windowID;
        }
                
        public bool IsFriend { get; set; }
        public bool IsFollower { get; set; }
        public bool IsFollowed { get; set; }
        public bool IsFollowerRequest { get; set; }
        public bool IsShout { get; set; }

        public object Item
        {
            get { return _Item; }
            set
            {
                _Item = value;
                var notifier = value as INotifyPropertyChanged;
                if (notifier != null) notifier.PropertyChanged += (s, e) =>
                {
                    if (s is TraktImage && e.PropertyName == "Avatar")
                        SetImageToGui((s as TraktImage).Avatar.LocalImageFilename(ArtworkType.Avatar));
                };
            }
        } 
        protected object _Item;

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
        internal static void GetImages(List<TraktImage> itemsWithThumbs)
        {
            StopDownload = false;

            // split the downloads in 5+ groups and do multithreaded downloading
            int groupSize = (int)Math.Max(1, Math.Floor((double)itemsWithThumbs.Count / 5));
            int groups = (int)Math.Ceiling((double)itemsWithThumbs.Count() / groupSize);

            for (int i = 0; i < groups; i++)
            {
                var groupList = new List<TraktImage>();
                for (int j = groupSize * i; j < groupSize * i + (groupSize * (i + 1) > itemsWithThumbs.Count ? itemsWithThumbs.Count - groupSize * i : groupSize); j++)
                {
                    groupList.Add(itemsWithThumbs[j]);
                }
                
                new Thread(delegate(object o)
                {
                    var items = (List<TraktImage>)o;
                    foreach (var item in items)
                    {
                        #region Avatar
                        if (item.Avatar != null)
                        {
                            // stop download if we have exited window
                            if (StopDownload) break;

                            string remoteThumb = item.Avatar;
                            string localThumb = item.Avatar.LocalImageFilename(ArtworkType.Avatar);

                            if (!string.IsNullOrEmpty(remoteThumb) && !string.IsNullOrEmpty(localThumb))
                            {
                                if (GUIImageHandler.DownloadImage(remoteThumb, localThumb))
                                {
                                    // notify that image has been downloaded
                                    item.NotifyPropertyChanged("Avatar");
                                }
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

            // check if this user item is a shout
            // we may need to apply a rating overlay to the avatar
            if (TVTag is TraktShout)
            {
                var shout = TVTag as TraktShout;

                // add a rating overlay if user has rated item
                var ratingOverlay = GUIImageHandler.GetRatingOverlay(shout.UserRatings);

                // get a reference to a MediaPortal Texture Identifier
                string suffix = Enum.GetName(typeof(RatingOverlayImage), ratingOverlay);
                string texture = GUIImageHandler.GetTextureIdentFromFile(imageFilePath, suffix);

                // build memory image, resize avatar as they come in different sizes sometimes
                Image memoryImage = null;
                if (ratingOverlay != RatingOverlayImage.None)
                {
                    memoryImage = GUIImageHandler.DrawOverlayOnAvatar(imageFilePath, ratingOverlay, new Size(140, 140));
                    if (memoryImage == null) return;

                    // load texture into facade item
                    if (GUITextureManager.LoadFromMemory(memoryImage, texture, 0, 0, 0) > 0)
                    {
                        ThumbnailImage = texture;
                        IconImage = texture;
                        IconImageBig = texture;
                    }
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
