using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using MediaPortal.Configuration;
using MediaPortal.GUI.Library;
using Action = MediaPortal.GUI.Library.Action;
using MediaPortal.Util;
using TraktPlugin.TraktAPI;
using TraktPlugin.TraktAPI.DataStructures;

namespace TraktPlugin.GUI
{
    public class GUITraktFriends : GUIWindow
    {
        #region Skin Controls

        [SkinControlAttribute(50)]
        protected GUIFacadeControl Facade = null;

        #endregion

        #region Enums

        enum SkinProperty
        {
            NoFriends
        }

        enum Views
        {
            Friends,
            WatchedTypes,
            WatchedHistory           
        }

        enum WatchedHistoryType
        {
            Episodes,
            Movies
        }

        #endregion

        #region Constructor

        public GUITraktFriends() { }

        #endregion

        #region Private Properties

        bool StopDownload { get; set; }
        Views ViewLevel { get; set; }
        WatchedHistoryType SelectedType { get; set; }
        TraktFriend CurrentFriend { get; set; }

        IEnumerable<TraktFriend> TraktFriends
        {
            get
            {
                if (_Friends == null)
                {
                    _Friends = TraktAPI.TraktAPI.GetUserFriends(TraktSettings.Username);
                }
                return _Friends;
            }
        }
        private IEnumerable<TraktFriend> _Friends = null;

        #endregion

        #region Base Overrides

        public override int GetID
        {
            get
            {
                return 87260;
            }
        }

        public override bool Init()
        {
            return Load(GUIGraphicsContext.Skin + @"\Trakt.Friends.xml");
        }

        protected override void OnPageLoad()
        {
            // clear GUI properties
            ClearProperties();

            // Load Friends
            LoadFriendsList();
        }

        protected override void OnPageDestroy(int new_windowId)
        {
            _Friends = null;
            base.OnPageDestroy(new_windowId);
        }

        protected override void OnClicked(int controlId, GUIControl control, Action.ActionType actionType)
        {
            // wait for any background action to finish
            if (GUIBackgroundTask.Instance.IsBusy) return;

            switch (controlId)
            {
                case (50):
                    if (actionType == Action.ActionType.ACTION_SELECT_ITEM)
                    {
                        switch (ViewLevel)
                        {
                            case Views.Friends:
                                LoadWatchedTypes(Facade.SelectedListItem as GUITraktUserListItem);
                                break;

                            case Views.WatchedTypes:
                                LoadWatchedHistory(CurrentFriend);
                                break;
                        }
                    }
                    break;

                default:
                    break;
            }
            base.OnClicked(controlId, control, actionType);
        }

        public override void OnAction(Action action)
        {
            switch (action.wID)
            {
                case Action.ActionType.ACTION_PREVIOUS_MENU:
                    switch (ViewLevel)
                    {
                        case Views.WatchedHistory:
                            LoadWatchedTypes(Facade.SelectedListItem as GUITraktUserListItem);
                            return;

                        case Views.WatchedTypes:
                            LoadFriendsList();
                            return;                  
                    }
                    break;
            }
            base.OnAction(action);
        }
        #endregion

        #region Private Methods

        private void LoadFriendsList()
        {
            ViewLevel = Views.Friends;

            GUIBackgroundTask.Instance.ExecuteInBackgroundAndCallback(() =>
            {
                return TraktFriends;
            },
            delegate(bool success, object result)
            {
                if (success)
                {
                    IEnumerable<TraktFriend> friends = result as IEnumerable<TraktFriend>;
                    SendFriendsToFacade(friends);
                }
            }, Translation.GettingFriendsList, true);
        }

        private void LoadWatchedTypes(GUITraktUserListItem friend)
        {
            // only two types to choose from in this view level            
            // signal that we are now displaying the watched types view
            ViewLevel = Views.WatchedTypes;
            GUIUtils.SetProperty("#trakt.view.level", ViewLevel.ToString());
            GUIUtils.SetProperty("#itemcount", "2");
            
            // clear facade
            GUIControl.ClearControl(GetID, Facade.GetID);

            // add each type to the list           
            GUITraktUserListItem item = new GUITraktUserListItem(Translation.Episodes);
            item.Item = friend.Item;
            item.IconImage = CurrentFriend.AvatarFilename;
            item.IconImageBig = CurrentFriend.AvatarFilename;
            item.ThumbnailImage = CurrentFriend.AvatarFilename;
            item.OnItemSelected += OnWatchedTypeSelected;
            Utils.SetDefaultIcons(item);
            Facade.Add(item);

            item = new GUITraktUserListItem(Translation.Movies);
            item.Item = friend.Item;
            item.IconImage = CurrentFriend.AvatarFilename;
            item.IconImageBig = CurrentFriend.AvatarFilename;
            item.ThumbnailImage = CurrentFriend.AvatarFilename;
            item.OnItemSelected += OnWatchedTypeSelected;
            Utils.SetDefaultIcons(item);
            Facade.Add(item);

            Facade.SelectedListItemIndex = 0;
        }

        private void LoadWatchedHistory(TraktFriend friend)
        {
            if (friend == null) return;

            GUIBackgroundTask.Instance.ExecuteInBackgroundAndCallback(() =>
            {
                return friend;
            },
            delegate(bool success, object result)
            {
                if (success)
                {
                    if (SelectedType == WatchedHistoryType.Episodes)   
                        SendWatchedEpisodeHistoryToFacade(result as TraktFriend);
                    else
                        SendWatchedMovieHistoryToFacade(result as TraktFriend);
                }
            }, Translation.GettingFriendsWatchedHistory, true);
        }

        private void SendWatchedEpisodeHistoryToFacade(TraktFriend friend)
        {
            if (friend == null) return;

            if (friend.WatchedHistory == null || friend.WatchedHistory.Where(w => w.Type == "episode").Count() == 0)
            {
                GUIUtils.ShowNotifyDialog(GUIUtils.PluginName(), Translation.UserHasNotWatchedEpisodes);
                return;
            }

            // signal that we are now displaying the watched history view
            ViewLevel = Views.WatchedHistory;            
            GUIUtils.SetProperty("#trakt.view.level", ViewLevel.ToString());
            GUIUtils.SetProperty("#itemcount", friend.WatchedHistory.Where(w => w.Type == "episode").Count().ToString());

            // clear facade
            GUIControl.ClearControl(GetID, Facade.GetID);

            // add each episode to the list
            foreach (var episode in friend.WatchedHistory.Where(w => w.Type == "episode"))
            {
                GUITraktUserListItem episodeItem = new GUITraktUserListItem(episode.ToString());

                episodeItem.Label2 = episode.WatchedDate.FromEpoch().ToShortDateString();
                episodeItem.IconImage = "defaultVideo.png";
                episodeItem.IconImageBig = "defaultVideoBig.png";
                episodeItem.ThumbnailImage = "defaultVideoBig.png";
                episodeItem.Item = episode;
                episodeItem.OnItemSelected += OnEpisodeSelected;
                Utils.SetDefaultIcons(episodeItem);
                Facade.Add(episodeItem);
            }

            Facade.SelectedListItemIndex = 0;
        }

        private void SendWatchedMovieHistoryToFacade(TraktFriend friend)
        {
            if (friend == null) return;

            if (friend.WatchedHistory == null || friend.WatchedHistory.Where(w => w.Type == "movie").Count() == 0)
            {
                GUIUtils.ShowNotifyDialog(GUIUtils.PluginName(), Translation.UserHasNotWatchedMovies);
                return;
            }

            // signal that we are now displaying the watched history view
            ViewLevel = Views.WatchedHistory;
            GUIUtils.SetProperty("#trakt.view.level", ViewLevel.ToString());
            GUIUtils.SetProperty("#itemcount", friend.WatchedHistory.Where(w => w.Type == "movie").Count().ToString());

            // clear facade
            GUIControl.ClearControl(GetID, Facade.GetID);

            // add each episode to the list
            foreach (var movie in friend.WatchedHistory.Where(w => w.Type == "movie"))
            {
                GUITraktUserListItem movieItem = new GUITraktUserListItem(movie.ToString());

                movieItem.Label2 = movie.WatchedDate.FromEpoch().ToShortDateString();
                movieItem.IconImage = "defaultVideo.png";
                movieItem.IconImageBig = "defaultVideoBig.png";
                movieItem.ThumbnailImage = "defaultVideoBig.png";
                movieItem.Item = movie;
                movieItem.OnItemSelected += OnMovieSelected;
                Utils.SetDefaultIcons(movieItem);
                Facade.Add(movieItem);
            }

            Facade.SelectedListItemIndex = 0;
        }

        private void SendFriendsToFacade(IEnumerable<TraktFriend> friends)
        {
            // clear facade
            GUIControl.ClearControl(GetID, Facade.GetID);

            // signal that we are now displaying the watched history view
            ViewLevel = Views.Friends;
            GUIUtils.SetProperty("#trakt.view.level", ViewLevel.ToString());
            GUIUtils.SetProperty("#itemcount", friends.Count().ToString());

            if (friends.Count() == 0)
            {
                SetProperty(SkinProperty.NoFriends, Translation.NoFriends);
                GUIUtils.ShowNotifyDialog(GUIUtils.PluginName(), Translation.NoFriendsTaunt);
                GUIWindowManager.ShowPreviousWindow();
                return;
            }

            // Add each friend to the list
            foreach (var user in friends)
            {
                GUITraktUserListItem userItem = new GUITraktUserListItem(user.Username);

                userItem.Label2 = user.JoinDate.FromEpoch().ToShortDateString();
                userItem.Item = user;
                userItem.IconImage = "defaultTraktUser.png";
                userItem.IconImageBig = "defaultTraktUserBig.png";
                userItem.ThumbnailImage = "defaultTraktUserBig.png";
                userItem.OnItemSelected += OnFriendSelected;
                Utils.SetDefaultIcons(userItem);
                Facade.Add(userItem);
            }

            Facade.SelectedListItemIndex = 0;

            // Set Facade Layout
            Facade.SetCurrentLayout("List");
            GUIControl.FocusControl(GetID, Facade.GetID);

            // Download avatars Async and set to facade
            GetImages(friends.ToList());
        }

        private void SetProperty(SkinProperty property, string value)
        {
            SetProperty(property.ToString(), value);
        }

        private void SetProperty(string property, string value)
        {
            string propertyValue = string.IsNullOrEmpty(value) ? "N/A" : value;
            GUIUtils.SetProperty(property, propertyValue);
        }

        private void ClearProperties()
        {
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
            GUIUtils.SetProperty("#Trakt.User.Age", string.Empty);
            GUIUtils.SetProperty("#Trakt.User.Username", string.Empty);
        }

        private void PublishFriendSkinProperties(TraktFriend user)
        {
            SetProperty("#Trakt.User.About", user.About);
            SetProperty("#Trakt.User.Age", user.Age);
            SetProperty("#Trakt.User.Avatar", user.Avatar);
            SetProperty("#Trakt.User.AvatarFileName", user.AvatarFilename);
            SetProperty("#Trakt.User.FullName", user.FullName);
            SetProperty("#Trakt.User.Gender", user.Gender);
            SetProperty("#Trakt.User.JoinDate", user.JoinDate.FromEpoch().ToLongDateString());
            SetProperty("#Trakt.User.Location", user.Location);
            SetProperty("#Trakt.User.Protected", user.Protected);
            SetProperty("#Trakt.User.Url", user.Url);
            SetProperty("#Trakt.User.Username", user.Username);
        }

        private void PublishEpisodeSkinProperties(TraktFriend.WatchItem episode)
        {
            SetProperty("#Trakt.Selected.Type", "episode");

            SetProperty("#Trakt.Show.Imdb", episode.Show.Imdb);
            SetProperty("#Trakt.Show.Tvdb", episode.Show.Tvdb);
            SetProperty("#Trakt.Show.TvRage", episode.Show.TvRage);
            SetProperty("#Trakt.Show.Title", episode.Show.Title);
            SetProperty("#Trakt.Show.Url", episode.Show.Url);
            SetProperty("#Trakt.Episode.Number", episode.Episode.Number.ToString());
            SetProperty("#Trakt.Episode.Season", episode.Episode.Season.ToString());
            SetProperty("#Trakt.Episode.FirstAired", episode.Episode.FirstAired.FromEpoch().ToShortDateString());
            SetProperty("#Trakt.Episode.Title", episode.Episode.Title);
            SetProperty("#Trakt.Episode.Url", episode.Episode.Url);
        }

        private void PublishMovieSkinProperties(TraktFriend.WatchItem movie)
        {
            SetProperty("#Trakt.Selected.Type", "movie");

            SetProperty("#Trakt.Movie.Imdb", movie.Movie.Imdb);
            SetProperty("#Trakt.Movie.Certification", movie.Movie.Certification);
            SetProperty("#Trakt.Movie.Overview", movie.Movie.Overview);
            SetProperty("#Trakt.Movie.Released", movie.Movie.Released.FromEpoch().ToShortDateString());
            SetProperty("#Trakt.Movie.Runtime", movie.Movie.Runtime.ToString());
            SetProperty("#Trakt.Movie.Tagline", movie.Movie.Tagline);
            SetProperty("#Trakt.Movie.Title", movie.Movie.Title);
            SetProperty("#Trakt.Movie.Tmdb", movie.Movie.Tmdb);
            SetProperty("#Trakt.Movie.Trailer", movie.Movie.Trailer);
            SetProperty("#Trakt.Movie.Url", movie.Movie.Url);
            SetProperty("#Trakt.Movie.Year", movie.Movie.Year);
        }

        private void OnFriendSelected(GUIListItem item, GUIControl parent)
        {
            CurrentFriend = (item as GUITraktUserListItem).Item as TraktFriend;
            PublishFriendSkinProperties(CurrentFriend);
        }

        private void OnWatchedTypeSelected(GUIListItem item, GUIControl parent)
        {
            if (item.Label == Translation.Episodes)
                SelectedType = WatchedHistoryType.Episodes;
            else
                SelectedType = WatchedHistoryType.Movies;
        }

        private void OnEpisodeSelected(GUIListItem item, GUIControl parent)
        {
            PublishEpisodeSkinProperties((item as GUITraktUserListItem).Item as TraktFriend.WatchItem);
        }

        private void OnMovieSelected(GUIListItem item, GUIControl parent)
        {
            PublishMovieSkinProperties((item as GUITraktUserListItem).Item as TraktFriend.WatchItem);
        }

        private void GetImages(List<TraktFriend> itemsWithThumbs)
        {
            StopDownload = false;

            // split the downloads in 5+ groups and do multithreaded downloading
            int groupSize = (int)Math.Max(1, Math.Floor((double)itemsWithThumbs.Count / 5));
            int groups = (int)Math.Ceiling((double)itemsWithThumbs.Count() / groupSize);

            for (int i = 0; i < groups; i++)
            {
                List<TraktFriend> groupList = new List<TraktFriend>();
                for (int j = groupSize * i; j < groupSize * i + (groupSize * (i + 1) > itemsWithThumbs.Count ? itemsWithThumbs.Count - groupSize * i : groupSize); j++)
                {
                    groupList.Add(itemsWithThumbs[j]);
                }

                new Thread(delegate(object o)
                {
                    List<TraktFriend> items = (List<TraktFriend>)o;
                    foreach (TraktFriend item in items)
                    {
                        // stop download if we have exited window
                        if (StopDownload) break;

                        string remoteThumb = item.Avatar;
                        if (string.IsNullOrEmpty(remoteThumb)) continue;

                        string localThumb = item.AvatarFilename;
                        if (string.IsNullOrEmpty(localThumb)) continue;

                        if (GUIImageHandler.DownloadImage(remoteThumb, localThumb))
                        {
                            // notify that image has been downloaded
                            item.NotifyPropertyChanged("AvatarFilename");
                        }
                    }
                })
                {
                    IsBackground = true,
                    Name = "Trakt Avatar Image Downloader " + i.ToString()
                }.Start(groupList);
            }
        }

        #endregion

    }

    /// <summary>
    /// Extends TraktUserProfile with properties we can
    /// use to notify the facade for loading
    /// </summary>
    public class TraktFriend : TraktUserProfile, INotifyPropertyChanged
    {
        #region INotifyPropertyChanged

        /// <summary>
        /// Path to local Avatar Image
        /// </summary>
        public string AvatarFilename
        { 
            get
            {
                string filename = string.Empty;
                if (!string.IsNullOrEmpty(Avatar))
                {
                    string folder = Config.GetSubFolder(Config.Dir.Thumbs, @"Trakt\Avatars");
                    filename = Path.Combine(folder, string.Concat(Username, ".jpg"));
                }
                return filename;
            }
            set
            {
                _AvatarFilename = value; 
            } 
        }
        string _AvatarFilename = string.Empty;

        /// <summary>
        /// Notify image property change during async image downloading
        /// Sends messages to facade to update image
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }

    public class GUITraktUserListItem : GUIListItem
    {
        public GUITraktUserListItem(string strLabel) : base(strLabel) { }

        public object Item
        {
            get { return _Item; }
            set
            {
                _Item = value;
                INotifyPropertyChanged notifier = value as INotifyPropertyChanged;
                if (notifier != null) notifier.PropertyChanged += (s, e) =>
                {
                    if (s is TraktFriend && e.PropertyName == "AvatarFilename")
                        SetImageToGui((s as TraktFriend).AvatarFilename);
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

            // Get a reference to a MdiaPortal Texture Identifier
            string texture = GUIImageHandler.GetTextureIdentFromFile(imageFilePath);

            // load texture into facade item
            if (GUITextureManager.LoadFromMemory(ImageFast.FromFile(imageFilePath), texture, 0, 0, 0) > 0)
            {
                ThumbnailImage = texture;
                IconImage = texture;
                IconImageBig = texture;
            }

            // if selected and TraktFriends is current window force an update of thumbnail
            GUITraktFriends window = GUIWindowManager.GetWindow(GUIWindowManager.ActiveWindow) as GUITraktFriends;
            if (window != null)
            {
                GUIListItem selectedItem = GUIControl.GetSelectedListItem(87260, 50);
                if (selectedItem == this)
                {
                    GUIWindowManager.SendThreadMessage(new GUIMessage(GUIMessage.MessageType.GUI_MSG_ITEM_SELECT, GUIWindowManager.ActiveWindow, 0, 50, ItemId, 0, null));
                }
            }
        }        
    }

    public static class DateExtensions
    {
        /// <summary>
        /// Date Time extension method to return a unix epoch
        /// time as a long
        /// </summary>
        /// <returns> A long representing the Date Time as the number
        /// of seconds since 1/1/1970</returns>
        public static long ToEpoch(this DateTime dt)
        {
            return (long)(dt - new DateTime(1970, 1, 1)).TotalSeconds;
        }

        /// <summary>
        /// Long extension method to convert a Unix epoch
        /// time to a standard C# DateTime object.
        /// </summary>
        /// <returns>A DateTime object representing the unix
        /// time as seconds since 1/1/1970</returns>
        public static DateTime FromEpoch(this long unixTime)
        {
            return new DateTime(1970, 1, 1).AddSeconds(unixTime);
        }
    }
}