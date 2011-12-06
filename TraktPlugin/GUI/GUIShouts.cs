using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using MediaPortal.Configuration;
using MediaPortal.GUI.Library;
using MediaPortal.Video.Database;
using MediaPortal.GUI.Video;
using Action = MediaPortal.GUI.Library.Action;
using MediaPortal.Util;
using TraktPlugin.TraktAPI;
using TraktPlugin.TraktAPI.DataStructures;

namespace TraktPlugin.GUI
{
    public class GUIShouts : GUIWindow
    {
        #region Skin Controls

        [SkinControl(50)]
        protected GUIFacadeControl Facade = null;

        [SkinControl(2)]
        protected GUICheckButton hideSpoilersButton = null;

        #endregion

        #region Enums
         
        enum ContextMenuItem
        {
            Shout
        }

        public enum ShoutTypeEnum
        {
            movie,
            show,
            episode
        }

        #endregion

        #region Constructor

        public GUIShouts() { }        

        #endregion

        #region Private Properties

        bool StopDownload { get; set; }
       
        #endregion

        #region Public Properties

        public static ShoutTypeEnum ShoutType { get; set; }
        public static MovieShout MovieInfo { get; set; }
        public static ShowShout ShowInfo { get; set; }
        public static EpisodeShout EpisodeInfo { get; set; }
        public static string Fanart { get; set; }

        #endregion

        #region Base Overrides

        public override int GetID
        {
            get
            {
                return 87280;
            }
        }

        public override bool Init()
        {
            return Load(GUIGraphicsContext.Skin + @"\Trakt.Shouts.xml");
        }

        protected override void OnPageLoad()
        {
            // Clear GUI properties
            ClearProperties();

            // Requires Login
            if (!GUICommon.CheckLogin()) return;

            // Initialize
            InitProperties();

            // Load Shouts for Selected item
            LoadShoutsList();
        }

        protected override void OnPageDestroy(int new_windowId)
        {
            StopDownload = true;
            ClearProperties();

            if (hideSpoilersButton != null)
            {
                TraktSettings.HideSpoilersOnShouts = hideSpoilersButton.Selected;
            }

            base.OnPageDestroy(new_windowId);
        }

        protected override void OnClicked(int controlId, GUIControl control, Action.ActionType actionType)
        {
            // wait for any background action to finish
            if (GUIBackgroundTask.Instance.IsBusy) return;

            switch (controlId)
            {
                // Hide Spoilers Button
                case (2):
                    TraktSettings.HideSpoilersOnShouts = !TraktSettings.HideSpoilersOnShouts;
                    PublishShoutSkinProperties(Facade.SelectedListItem.TVTag as TraktShout);
                    break;

                default:
                    break;
            }
            base.OnClicked(controlId, control, actionType);
        }

        #endregion

        #region Private Methods

        private void LoadShoutsList()
        {
            GUIUtils.SetProperty("#Trakt.Items", string.Empty);

            GUIBackgroundTask.Instance.ExecuteInBackgroundAndCallback(() =>
            {
                switch (ShoutType)
                {
                    case ShoutTypeEnum.movie:
                        if (MovieInfo == null) return null;
                        GUIUtils.SetProperty("#Trakt.Shout.CurrentItem", MovieInfo.Title);
                        return GetMovieShouts();

                    case ShoutTypeEnum.show:
                        if (ShowInfo == null) return null;
                        GUIUtils.SetProperty("#Trakt.Shout.CurrentItem", ShowInfo.Title);
                        return GetShowShouts();

                    case ShoutTypeEnum.episode:
                        if (EpisodeInfo == null) return null;
                        GUIUtils.SetProperty("#Trakt.Shout.CurrentItem", EpisodeInfo.ToString());
                        return GetEpisodeShouts();

                    default:
                        return null;
                }
            },
            delegate(bool success, object result)
            {
                if (success)
                {
                    SendShoutsToFacade(result as IEnumerable<TraktShout>);
                }
            }, Translation.GettingShouts, true);
        }

        private IEnumerable<TraktShout> GetMovieShouts()
        {
            string title = string.Empty;
            if (!string.IsNullOrEmpty(MovieInfo.IMDbId))
                title = MovieInfo.IMDbId;
            else if(!string.IsNullOrEmpty(MovieInfo.TMDbId))
                title = MovieInfo.TMDbId;
            else
                title = string.Format("{0}-{1}", MovieInfo.Title, MovieInfo.Year).Replace(" ", "-");

            return TraktAPI.TraktAPI.GetMovieShouts(title);
        }

        private IEnumerable<TraktShout> GetShowShouts()
        {
            string title = string.Empty;
            if (!string.IsNullOrEmpty(ShowInfo.TVDbId))
                title = ShowInfo.TVDbId;
            else if (!string.IsNullOrEmpty(ShowInfo.IMDbId))
                title = ShowInfo.IMDbId;
            else
                title = ShowInfo.Title.Replace(" ", "-");

            return TraktAPI.TraktAPI.GetShowShouts(title);
        }

        private IEnumerable<TraktShout> GetEpisodeShouts()
        {
            string title = string.Empty;
            if (!string.IsNullOrEmpty(EpisodeInfo.TVDbId))
                title = EpisodeInfo.TVDbId;
            else if (!string.IsNullOrEmpty(EpisodeInfo.IMDbId))
                title = EpisodeInfo.IMDbId;
            else
                title = EpisodeInfo.Title.Replace(" ", "-");

            return TraktAPI.TraktAPI.GetEpisodeShouts(title, EpisodeInfo.SeasonIdx, EpisodeInfo.EpisodeIdx);
        }

        private void SendShoutsToFacade(IEnumerable<TraktShout> shouts)
        {
            // clear facade
            GUIControl.ClearControl(GetID, Facade.GetID);

            if (shouts == null || shouts.Count() == 0)
            {
                if (shouts != null)
                {
                    string title = string.Empty;
                    switch (ShoutType)
                    {
                        case ShoutTypeEnum.movie:
                            title = MovieInfo.Title;
                            break;
                        case ShoutTypeEnum.show:
                            title = ShowInfo.Title;
                            break;
                        case ShoutTypeEnum.episode:
                            title = string.Format(EpisodeInfo.ToString());
                            break;
                    }
                    GUIUtils.ShowNotifyDialog(GUIUtils.PluginName(), string.Format(Translation.NoShoutsForItem, title));
                }
                GUIWindowManager.ShowPreviousWindow();
                return;
            }

            GUIUtils.SetProperty("#itemcount", shouts.Count().ToString());
            GUIUtils.SetProperty("#Trakt.Items", string.Format("{0} {1}", shouts.Count(), shouts.Count() > 1 ? Translation.Shouts : Translation.Shout));            

            int id = 0;
            List<TraktShout.TraktUser> users = new List<TraktShout.TraktUser>();

            // Add each user that shouted to the list
            foreach (var shout in shouts)
            {
                GUITraktShoutListItem shoutItem = new GUITraktShoutListItem(shout.User.Username);

                shoutItem.Label2 = shout.InsertedDate.FromEpoch().ToShortDateString();
                shoutItem.Item = shout.User;
                shoutItem.TVTag = shout;
                shoutItem.ItemId = id++;
                shoutItem.IconImage = "defaultTraktUser.png";
                shoutItem.IconImageBig = "defaultTraktUserBig.png";
                shoutItem.ThumbnailImage = "defaultTraktUserBig.png";
                shoutItem.OnItemSelected += OnShoutSelected;
                Utils.SetDefaultIcons(shoutItem);
                Facade.Add(shoutItem);

                users.Add(shout.User);
            }

            // Set Facade Layout
            Facade.SetCurrentLayout("List");
            GUIControl.FocusControl(GetID, Facade.GetID);

            Facade.SelectedListItemIndex = 0;

            // Download avatars Async and set to facade            
            GetImages(users);
        }

        private void SetProperty(string property, string value)
        {
            string propertyValue = string.IsNullOrEmpty(value) ? "N/A" : value;
            GUIUtils.SetProperty(property, propertyValue);
        }

        private void InitProperties()
        {
            GUIUtils.SetProperty("#Trakt.Shout.Fanart", Fanart);

            if (hideSpoilersButton != null)
            {
                hideSpoilersButton.Label = Translation.HideSpoilers;
                hideSpoilersButton.Selected = TraktSettings.HideSpoilersOnShouts;
            }
        }

        private void ClearProperties()
        {
            GUIUtils.SetProperty("#Trakt.Shouts.CurrentItem", string.Empty);

            GUIUtils.SetProperty("#Trakt.User.About", string.Empty);
            GUIUtils.SetProperty("#Trakt.User.Age", string.Empty);
            GUIUtils.SetProperty("#Trakt.User.Avatar", string.Empty);
            GUIUtils.SetProperty("#Trakt.User.AvatarFileName", string.Empty);
            GUIUtils.SetProperty("#Trakt.User.FullName", string.Empty);
            GUIUtils.SetProperty("#Trakt.User.Gender", string.Empty);
            GUIUtils.SetProperty("#Trakt.User.JoinDate", string.Empty);
            GUIUtils.SetProperty("#Trakt.User.Location", string.Empty);
            GUIUtils.SetProperty("#Trakt.User.Protected", string.Empty);
            GUIUtils.SetProperty("#Trakt.User.Url", string.Empty);
            GUIUtils.SetProperty("#Trakt.User.Username", string.Empty);

            GUIUtils.SetProperty("#Trakt.Shout.Inserted", string.Empty);
            GUIUtils.SetProperty("#Trakt.Shout.Spoiler", "false");
            GUIUtils.SetProperty("#Trakt.Shout.Text", string.Empty);
        }

        private void PublishShoutSkinProperties(TraktShout shout)
        {
            if (shout == null) return;

            SetProperty("#Trakt.User.About", shout.User.About);
            SetProperty("#Trakt.User.Age", shout.User.Age);
            SetProperty("#Trakt.User.Avatar", shout.User.Avatar);
            SetProperty("#Trakt.User.AvatarFileName", shout.User.AvatarFilename);
            SetProperty("#Trakt.User.FullName", shout.User.FullName);
            SetProperty("#Trakt.User.Gender", shout.User.Gender);
            SetProperty("#Trakt.User.JoinDate", shout.User.JoinDate.FromEpoch().ToLongDateString());
            SetProperty("#Trakt.User.Location", shout.User.Location);
            SetProperty("#Trakt.User.Protected", shout.User.Protected);
            SetProperty("#Trakt.User.Url", shout.User.Url);
            SetProperty("#Trakt.User.Username", shout.User.Username);

            SetProperty("#Trakt.Shout.Inserted", shout.InsertedDate.FromEpoch().ToLongDateString());
            SetProperty("#Trakt.Shout.Spoiler", shout.Spoiler.ToString());
            if (TraktSettings.HideSpoilersOnShouts && shout.Spoiler)
            {
                SetProperty("#Trakt.Shout.Text", Translation.HiddenToPreventSpoilers);
            }
            else
            {
                SetProperty("#Trakt.Shout.Text", shout.Shout);
            }
        }

        private void OnShoutSelected(GUIListItem item, GUIControl parent)
        {
            PublishShoutSkinProperties(item.TVTag as TraktShout);
        }

        private void GetImages(List<TraktShout.TraktUser> itemsWithThumbs)
        {
            StopDownload = false;

            // split the downloads in 5+ groups and do multithreaded downloading
            int groupSize = (int)Math.Max(1, Math.Floor((double)itemsWithThumbs.Count / 5));
            int groups = (int)Math.Ceiling((double)itemsWithThumbs.Count() / groupSize);

            for (int i = 0; i < groups; i++)
            {
                List<TraktShout.TraktUser> groupList = new List<TraktShout.TraktUser>();
                for (int j = groupSize * i; j < groupSize * i + (groupSize * (i + 1) > itemsWithThumbs.Count ? itemsWithThumbs.Count - groupSize * i : groupSize); j++)
                {
                    groupList.Add(itemsWithThumbs[j]);
                }

                new Thread(delegate(object o)
                {
                    List<TraktShout.TraktUser> items = (List<TraktShout.TraktUser>)o;
                    foreach (var item in items)
                    {
                        // stop download if we have exited window
                        if (StopDownload) break;

                        string remoteThumb = item.Avatar;
                        string localThumb = item.AvatarFilename;

                        if (!string.IsNullOrEmpty(remoteThumb) && !string.IsNullOrEmpty(localThumb))
                        {
                            if (GUIImageHandler.DownloadImage(remoteThumb, localThumb))
                            {
                                // notify that image has been downloaded
                                item.NotifyPropertyChanged("AvatarFilename");
                            }
                        }                        
                    }
                    #if !MP12
                    // refresh the facade so thumbnails get displayed
                    // this is not needed in MP 1.2 Beta
                    GUIWindowManager.SendThreadMessage(new GUIMessage(GUIMessage.MessageType.GUI_MSG_REFRESH, GUIWindowManager.ActiveWindow, 0, 50, 0, 0, null));
                    #endif
                })
                {
                    IsBackground = true,
                    Name = "Trakt Shouts Image Downloader " + i.ToString()
                }.Start(groupList);
            }
        }

        #endregion

    }

    public class GUITraktShoutListItem : GUIListItem
    {
        public GUITraktShoutListItem(string strLabel) : base(strLabel) { }

        public object Item
        {
            get { return _Item; }
            set
            {
                _Item = value;
                INotifyPropertyChanged notifier = value as INotifyPropertyChanged;
                if (notifier != null) notifier.PropertyChanged += (s, e) =>
                {
                    if (s is TraktShout.TraktUser && e.PropertyName == "AvatarFilename")
                        SetImageToGui((s as TraktShout.TraktUser).AvatarFilename);
                };
            }
        } protected object _Item;

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
            UpdateCurrentSelection();
        }

        protected void UpdateCurrentSelection()
        {
            GUIShouts window = GUIWindowManager.GetWindow(GUIWindowManager.ActiveWindow) as GUIShouts;
            if (window != null)
            {
                GUIListItem selectedItem = GUIControl.GetSelectedListItem(87280, 50);
                if (selectedItem == this)
                {
                    GUIWindowManager.SendThreadMessage(new GUIMessage(GUIMessage.MessageType.GUI_MSG_ITEM_SELECT, GUIWindowManager.ActiveWindow, 0, 50, ItemId, 0, null));
                }
            }
        }
    }

    public class MovieShout
    {
        public string Title { get; set; }
        public string Year { get; set; }
        public string IMDbId { get; set; }
        public string TMDbId { get; set; }
    }

    public class ShowShout
    {
        public string Title { get; set; }
        public string IMDbId { get; set; }
        public string TVDbId { get; set; }
    }

    public class EpisodeShout
    {
        public string Title { get; set; }
        public string IMDbId { get; set; }
        public string TVDbId { get; set; }
        public string SeasonIdx { get; set; }
        public string EpisodeIdx { get; set; }

        public override string ToString()
        {
            return string.Format("{0} - {1}x{2}", Title, SeasonIdx, EpisodeIdx);
        }
    }
}