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

        [SkinControl(50)]
        protected GUIFacadeControl Facade = null;

        #endregion

        #region Enums

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
            // Requires Login
            if (!GUICommon.CheckLogin()) return;

            // clear GUI properties
            ClearProperties();

            // Initialize
            InitProperties();

            // Load Friends
            LoadFriendsList();
        }

        protected override void OnPageDestroy(int new_windowId)
        {
            _Friends = null;
            StopDownload = true;
            ClearProperties();

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
            GUIUtils.SetProperty("#Trakt.Items", string.Empty);

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
            SetCurrentView();
            GUIUtils.SetProperty("#Trakt.View.Level", ViewLevel.ToString());
            GUIUtils.SetProperty("#itemcount", "2");
            GUIUtils.SetProperty("#Trakt.Items", string.Format("2 {0}", GUILocalizeStrings.Get(507)));
            
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

            if (friend.WatchedEpisodes == null || friend.WatchedEpisodes.Count() == 0)
            {
                GUIUtils.ShowNotifyDialog(GUIUtils.PluginName(), Translation.UserHasNotWatchedEpisodes);
                return;
            }

            // signal that we are now displaying the watched history view
            ViewLevel = Views.WatchedHistory;
            SetCurrentView();
            GUIUtils.SetProperty("#Trakt.View.Level", ViewLevel.ToString());
            GUIUtils.SetProperty("#itemcount", friend.WatchedEpisodes.Count().ToString());
            GUIUtils.SetProperty("#Trakt.Items", string.Format("{0} {1}", friend.WatchedEpisodes.Count().ToString(), friend.WatchedEpisodes.Count() > 1 ? Translation.Episodes : Translation.Episode));

            // clear facade
            GUIControl.ClearControl(GetID, Facade.GetID);

            // store a list of thumbnails to download
            List<TraktEpisode.ShowImages> episodeImages = new List<TraktEpisode.ShowImages>();

            int id = 0;

            // add each episode to the list
            foreach (var episode in friend.WatchedEpisodes)
            {
                GUITraktUserListItem episodeItem = new GUITraktUserListItem(episode.ToString());

                episodeItem.Label2 = episode.WatchedDate.FromEpoch().ToShortDateString();
                episodeItem.Item = episode.Episode.Images;
                episodeItem.TVTag = episode;
                episodeItem.ItemId = id++;
                episodeItem.IconImage = "defaultTraktEpisode.png";
                episodeItem.IconImageBig = "defaultTraktEpisodeBig.png";
                episodeItem.ThumbnailImage = "defaultTraktEpisodeBig.png";
                episodeItem.Item = episode;
                episodeItem.OnItemSelected += OnEpisodeSelected;
                Utils.SetDefaultIcons(episodeItem);
                Facade.Add(episodeItem);

                // add image to download
                episodeImages.Add(episode.Episode.Images);
            }

            Facade.SelectedListItemIndex = 0;

            // Download Episode Thumbnails Async and set to facade
            GetImages<TraktEpisode.ShowImages>(episodeImages);
        }

        private void SendWatchedMovieHistoryToFacade(TraktFriend friend)
        {
            if (friend == null) return;

            if (friend.WatchedMovies == null || friend.WatchedMovies.Count() == 0)
            {
                GUIUtils.ShowNotifyDialog(GUIUtils.PluginName(), Translation.UserHasNotWatchedMovies);
                return;
            }

            // signal that we are now displaying the watched history view
            ViewLevel = Views.WatchedHistory;
            SetCurrentView();
            GUIUtils.SetProperty("#Trakt.View.Level", ViewLevel.ToString());
            GUIUtils.SetProperty("#itemcount", friend.WatchedMovies.Count().ToString());
            GUIUtils.SetProperty("#Trakt.Items", string.Format("{0} {1}", friend.WatchedMovies.Count().ToString(), friend.WatchedMovies.Count() > 1 ? Translation.Movies : Translation.Movie));

            // clear facade
            GUIControl.ClearControl(GetID, Facade.GetID);

            // store a list of posters to download
            List<TraktMovie.MovieImages> movieImages = new List<TraktMovie.MovieImages>();

            int id = 0;

            // add each episode to the list
            foreach (var movie in friend.WatchedMovies)
            {
                GUITraktUserListItem movieItem = new GUITraktUserListItem(movie.ToString());

                movieItem.Label2 = movie.WatchedDate.FromEpoch().ToShortDateString();
                movieItem.Item = movie.Movie.Images;
                movieItem.TVTag = movie;
                movieItem.ItemId = id++;
                movieItem.IconImage = "defaultVideo.png";
                movieItem.IconImageBig = "defaultVideoBig.png";
                movieItem.ThumbnailImage = "defaultVideoBig.png";
                movieItem.Item = movie;
                movieItem.OnItemSelected += OnMovieSelected;
                Utils.SetDefaultIcons(movieItem);
                Facade.Add(movieItem);

                // add image to download
                movieImages.Add(movie.Movie.Images);
            }

            Facade.SelectedListItemIndex = 0;

            // Download Movie Posters Async and set to facade
            GetImages<TraktMovie.MovieImages>(movieImages);
        }

        private void SendFriendsToFacade(IEnumerable<TraktFriend> friends)
        {
            // clear facade
            GUIControl.ClearControl(GetID, Facade.GetID);

            // signal that we are now displaying the watched history view
            ViewLevel = Views.Friends;
            SetCurrentView();
            GUIUtils.SetProperty("#Trakt.View.Level", ViewLevel.ToString());
            GUIUtils.SetProperty("#itemcount", friends.Count().ToString());
            GUIUtils.SetProperty("#Trakt.Items", string.Format("{0} {1}", friends.Count().ToString(), friends.Count() > 1 ? Translation.Friends : Translation.Friend));

            if (friends.Count() == 0)
            {
                GUIUtils.ShowNotifyDialog(GUIUtils.PluginName(), Translation.NoFriendsTaunt);
                GUIWindowManager.ShowPreviousWindow();
                return;
            }

            int id = 0;

            // Add each friend to the list
            foreach (var user in friends)
            {
                GUITraktUserListItem userItem = new GUITraktUserListItem(user.Username);

                userItem.Label2 = user.JoinDate.FromEpoch().ToShortDateString();
                userItem.Item = user;
                userItem.ItemId = id++;
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
            GetImages<TraktFriend>(friends.ToList());
        }

        private void SetProperty(string property, string value)
        {
            string propertyValue = string.IsNullOrEmpty(value) ? "N/A" : value;
            GUIUtils.SetProperty(property, propertyValue);
        }
         
        private void InitProperties()
        {
            ViewLevel = Views.Friends;
            GUIUtils.SetProperty("#Trakt.View.Level", "Friends");
            GUIUtils.SetProperty("#Trakt.Selected.Type", string.Empty);
            GUIUtils.SetProperty("#Trakt.Items", string.Empty);
            SetCurrentView();
        }

        private void SetCurrentView()
        {
            if (ViewLevel == Views.Friends)
                SetProperty("#Trakt.CurrentView", "Trakt " + Translation.Friends);
            else if (ViewLevel == Views.WatchedTypes)
                SetProperty("#Trakt.CurrentView", CurrentFriend.Username);
            else
                SetProperty("#Trakt.CurrentView", string.Format("{0} | {1}", CurrentFriend.Username, SelectedType == WatchedHistoryType.Episodes ? Translation.WatchedEpisodes : Translation.WatchedMovies));
        }

        private void ClearProperties()
        {
            #region User
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
            #endregion

            #region Episodes
            GUIUtils.SetProperty("#Trakt.Show.Imdb", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.Tvdb", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.TvRage", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.Title", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.Url", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.AirDay", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.AirTime", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.Certification", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.Country", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.FirstAired", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.Network", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.Overview", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.Runtime", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.Year", string.Empty);
            GUIUtils.SetProperty("#Trakt.Episode.Number", string.Empty);
            GUIUtils.SetProperty("#Trakt.Episode.Season", string.Empty);
            GUIUtils.SetProperty("#Trakt.Episode.FirstAired", string.Empty);
            GUIUtils.SetProperty("#Trakt.Episode.Title", string.Empty);
            GUIUtils.SetProperty("#Trakt.Episode.Url", string.Empty);
            GUIUtils.SetProperty("#Trakt.Episode.Overview", string.Empty);
            GUIUtils.SetProperty("#Trakt.Episode.Runtime", string.Empty);
            GUIUtils.SetProperty("#Trakt.Episode.EpisodeImageFilename", string.Empty);
            #endregion

            #region Movies
            GUIUtils.SetProperty("#Trakt.Movie.Imdb", string.Empty);
            GUIUtils.SetProperty("#Trakt.Movie.Certification", string.Empty);
            GUIUtils.SetProperty("#Trakt.Movie.Overview", string.Empty);
            GUIUtils.SetProperty("#Trakt.Movie.Released", string.Empty);
            GUIUtils.SetProperty("#Trakt.Movie.Runtime", string.Empty);
            GUIUtils.SetProperty("#Trakt.Movie.Tagline", string.Empty);
            GUIUtils.SetProperty("#Trakt.Movie.Title", string.Empty);
            GUIUtils.SetProperty("#Trakt.Movie.Tmdb", string.Empty);
            GUIUtils.SetProperty("#Trakt.Movie.Trailer", string.Empty);
            GUIUtils.SetProperty("#Trakt.Movie.Url", string.Empty);
            GUIUtils.SetProperty("#Trakt.Movie.Year", string.Empty);
            GUIUtils.SetProperty("#Trakt.Movie.PosterImageFilename", string.Empty);
            #endregion
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
            SetProperty("#Trakt.Show.AirDay", episode.Show.AirDay);
            SetProperty("#Trakt.Show.AirTime", episode.Show.AirTime);
            SetProperty("#Trakt.Show.Certification", episode.Show.Certification);
            SetProperty("#Trakt.Show.Country", episode.Show.Country);
            SetProperty("#Trakt.Show.FirstAired", episode.Show.FirstAired.FromEpoch().ToShortDateString());
            SetProperty("#Trakt.Show.Network", episode.Show.Network);
            SetProperty("#Trakt.Show.Overview", episode.Show.Overview);
            SetProperty("#Trakt.Show.Runtime", episode.Show.Runtime.ToString());
            SetProperty("#Trakt.Show.Year", episode.Show.Year.ToString());
            SetProperty("#Trakt.Episode.Number", episode.Episode.Number.ToString());
            SetProperty("#Trakt.Episode.Season", episode.Episode.Season.ToString());
            SetProperty("#Trakt.Episode.FirstAired", episode.Episode.FirstAired.FromEpoch().ToShortDateString());
            SetProperty("#Trakt.Episode.Title", string.IsNullOrEmpty(episode.Episode.Title) ? string.Format("{0} {1}", Translation.Episode, episode.Episode.Number.ToString()) : episode.Episode.Title);
            SetProperty("#Trakt.Episode.Url", episode.Episode.Url);
            SetProperty("#Trakt.Episode.Overview", string.IsNullOrEmpty(episode.Episode.Overview) ? Translation.NoEpisodeSummary : episode.Episode.Overview);
            SetProperty("#Trakt.Episode.Runtime", episode.Episode.Runtime.ToString());
            SetProperty("#Trakt.Episode.EpisodeImageFilename", episode.Episode.Images.EpisodeImageFilename);
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
            SetProperty("#Trakt.Movie.PosterImageFilename", movie.Movie.Images.PosterImageFilename);
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
            
            PublishFriendSkinProperties(CurrentFriend);
        }

        private void OnEpisodeSelected(GUIListItem item, GUIControl parent)
        {
            PublishEpisodeSkinProperties(item.TVTag as TraktFriend.WatchItem);
        }

        private void OnMovieSelected(GUIListItem item, GUIControl parent)
        {
            PublishMovieSkinProperties(item.TVTag as TraktFriend.WatchItem);
        }

        private void GetImages<T>(List<T> itemsWithThumbs)
        {
            StopDownload = false;

            // split the downloads in 5+ groups and do multithreaded downloading
            int groupSize = (int)Math.Max(1, Math.Floor((double)itemsWithThumbs.Count / 5));
            int groups = (int)Math.Ceiling((double)itemsWithThumbs.Count() / groupSize);

            for (int i = 0; i < groups; i++)
            {
                List<T> groupList = new List<T>();
                for (int j = groupSize * i; j < groupSize * i + (groupSize * (i + 1) > itemsWithThumbs.Count ? itemsWithThumbs.Count - groupSize * i : groupSize); j++)
                {
                    groupList.Add(itemsWithThumbs[j]);
                }

                new Thread(delegate(object o)
                {
                    List<T> items = (List<T>)o;
                    foreach (T item in items)
                    {
                        // stop download if we have exited window
                        if (StopDownload) break;
                        
                        string remoteThumb = string.Empty;
                        string localThumb = string.Empty;

                        if (item is TraktFriend)
                        {
                            remoteThumb = (item as TraktFriend).Avatar;
                            localThumb = (item as TraktFriend).AvatarFilename;
                        }
                        else if (item is TraktMovie.MovieImages)
                        {
                            remoteThumb = (item as TraktMovie.MovieImages).Poster;
                            localThumb = (item as TraktMovie.MovieImages).PosterImageFilename;
                        }
                        else
                        {
                            remoteThumb = (item as TraktEpisode.ShowImages).Screen;
                            localThumb = (item as TraktEpisode.ShowImages).EpisodeImageFilename;
                        }

                        if (string.IsNullOrEmpty(remoteThumb)) continue;
                        if (string.IsNullOrEmpty(localThumb)) continue;

                        
                        if (GUIImageHandler.DownloadImage(remoteThumb, localThumb))
                        {
                            // notify that image has been downloaded
                            if (item is TraktFriend)
                            {
                                (item as TraktFriend).NotifyPropertyChanged("AvatarFilename");
                            }
                            else if (item is TraktMovie.MovieImages)
                            {
                                (item as TraktMovie.MovieImages).NotifyPropertyChanged("PosterImageFilename");
                            }
                            else
                            {
                                (item as TraktEpisode.ShowImages).NotifyPropertyChanged("EpisodeImageFilename");
                            }
                        }
                    }
                })
                {
                    IsBackground = true,
                    Name = "Trakt Friends Image Downloader " + i.ToString()
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
                    else if (s is TraktEpisode.ShowImages && e.PropertyName == "EpisodeImageFilename")
                        SetImageToGui((s as TraktEpisode.ShowImages).EpisodeImageFilename);
                    else if (s is TraktMovie.MovieImages && e.PropertyName == "PosterImageFilename")
                        SetImageToGui((s as TraktMovie.MovieImages).PosterImageFilename);
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
            if (GUITextureManager.LoadFromMemory(GUIImageHandler.LoadImage(imageFilePath), texture, 0, 0, 0) > 0)
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