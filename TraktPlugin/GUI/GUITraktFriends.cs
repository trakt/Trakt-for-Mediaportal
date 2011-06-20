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
    public class GUITraktFriends : GUIWindow
    {
        #region Skin Controls

        [SkinControl(50)]
        protected GUIFacadeControl Facade = null;

        [SkinControlAttribute(60)]
        protected GUIImage FanartBackground = null;

        [SkinControlAttribute(61)]
        protected GUIImage FanartBackground2 = null;

        [SkinControlAttribute(62)]
        protected GUIImage loadingImage = null;


        #endregion

        #region Enums

        enum Views
        {
            Friends,
            WatchedTypes,
            WatchedHistory           
        }

        enum TrailerSite
        {
            IMDb,
            iTunes,
            YouTube
        }

        enum ContextMenuItem
        {
            Trailers
        }

        enum WatchedHistoryType
        {
            Episodes,
            Movies
        }

        #endregion

        #region Constructor

        public GUITraktFriends()
        {
            backdrop = new ImageSwapper();
            backdrop.PropertyOne = "#Trakt.Friends.Fanart.1";
            backdrop.PropertyTwo = "#Trakt.Friends.Fanart.2";
        }

        #endregion

        #region Private Properties

        bool StopDownload { get; set; }
        Views ViewLevel { get; set; }
        WatchedHistoryType SelectedType { get; set; }
        TraktFriend CurrentFriend { get; set; }
        ImageSwapper backdrop;
        DateTime LastRequest = new DateTime();

        IEnumerable<TraktFriend> TraktFriends
        {
            get
            {
                if (_Friends == null || LastRequest < DateTime.UtcNow.Subtract(new TimeSpan(0, TraktSettings.WebRequestCacheMinutes, 0)))
                {
                    _Friends = TraktAPI.TraktAPI.GetUserFriends(TraktSettings.Username);
                    LastRequest = DateTime.UtcNow;
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

                            case Views.WatchedHistory:
                                if (SelectedType == WatchedHistoryType.Movies)
                                {
                                    GUIListItem selectedItem = this.Facade.SelectedListItem;
                                    if (selectedItem == null) break;

                                    TraktFriend.WatchItem selectedMovie = (TraktFriend.WatchItem)selectedItem.TVTag;

                                    string title = selectedMovie.Movie.Title;
                                    string imdbid = selectedMovie.Movie.Imdb;
                                    int year = Convert.ToInt32(selectedMovie.Movie.Year);

                                    bool handled = false;

                                    #if MP12
                                    // check if its in MovingPictures database
                                    // Loading Parameter only works in MediaPortal 1.2
                                    if (TraktHelper.IsMovingPicturesAvailableAndEnabled)
                                    {
                                        int? movieid = null;

                                        // Find Movie ID in MovingPictures
                                        // Movie List is now cached internally in MovingPictures so it will be fast
                                        if (TraktHandlers.MovingPictures.FindMovieID(title, year, imdbid, ref movieid))
                                        {
                                            // Open MovingPictures Details view so user can play movie
                                            string loadingParameter = string.Format("movieid:{0}", movieid);
                                            GUIWindowManager.ActivateWindow((int)ExternalPluginWindows.MovingPictures, loadingParameter);
                                            handled = true;
                                        }
                                    }
                                    #endif

                                    // check if its in My Videos database
                                    if (TraktSettings.MyVideos > 0 && handled == false)
                                    {
                                        IMDBMovie movie = null;
                                        if (TraktHandlers.MyVideos.FindMovieID(title, year, imdbid, ref movie))
                                        {
                                            // Open My Videos Video Info view so user can play movie
                                            GUIVideoInfo videoInfo = (GUIVideoInfo)GUIWindowManager.GetWindow((int)Window.WINDOW_VIDEO_INFO);
                                            videoInfo.Movie = movie;
                                            GUIWindowManager.ActivateWindow((int)Window.WINDOW_VIDEO_INFO);
                                            handled = true;
                                        }
                                    }
                                }
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

        #if MP12
        protected override void OnShowContextMenu()
        {
            if (!TraktHelper.IsOnlineVideosAvailableAndEnabled || !(ViewLevel == Views.WatchedHistory && SelectedType == WatchedHistoryType.Movies))
            {
                base.OnShowContextMenu();
                return;
            }

            GUIListItem selectedItem = this.Facade.SelectedListItem;
            if (selectedItem == null) return;

            TraktFriend.WatchItem selectedMovie = (TraktFriend.WatchItem)selectedItem.TVTag;

            IDialogbox dlg = (IDialogbox)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
            if (dlg == null) return;

            dlg.Reset();
            dlg.SetHeading(GUIUtils.PluginName());

            GUIListItem listItem = null;

            // Trailers
            listItem = new GUIListItem(Translation.Trailers);
            dlg.Add(listItem);
            listItem.ItemId = (int)ContextMenuItem.Trailers;

            // Show Context Menu
            dlg.DoModal(GUIWindowManager.ActiveWindow);
            if (dlg.SelectedId < 0) return;

            switch (dlg.SelectedId)
            {
                #if MP12
                case ((int)ContextMenuItem.Trailers):
                    ShowTrailersMenu(selectedMovie);
                    break;
                #endif

                default:
                    break;
            }

            base.OnShowContextMenu();
        }
        #endif
        #endregion

        #region Private Methods

        #if MP12
        private void ShowTrailersMenu(TraktFriend.WatchItem movie)
        {
            IDialogbox dlg = (IDialogbox)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
            dlg.Reset();
            dlg.SetHeading(Translation.Trailer);

            foreach (TrailerSite site in Enum.GetValues(typeof(TrailerSite)))
            {
                string menuItem = Enum.GetName(typeof(TrailerSite), site);
                GUIListItem pItem = new GUIListItem(menuItem);
                dlg.Add(pItem);
            }

            dlg.DoModal(GUIWindowManager.ActiveWindow);

            if (dlg.SelectedLabel >= 0)
            {
                string siteUtil = string.Empty;
                string searchParam = string.Empty;

                switch (dlg.SelectedLabelText)
                {
                    case ("IMDb"):
                        siteUtil = "IMDb Movie Trailers";
                        if (!string.IsNullOrEmpty(movie.Movie.Imdb))
                            // Exact search
                            searchParam = movie.Movie.Imdb;
                        else
                            searchParam = movie.Movie.Title;
                        break;

                    case ("iTunes"):
                        siteUtil = "iTunes Movie Trailers";
                        searchParam = movie.Movie.Title;
                        break;

                    case ("YouTube"):
                        siteUtil = "YouTube";
                        searchParam = movie.Movie.Title;
                        break;
                }

                string loadingParam = string.Format("site:{0}|search:{1}|return:Locked", siteUtil, searchParam);
                // Launch OnlineVideos Trailer search
                GUIWindowManager.ActivateWindow((int)ExternalPluginWindows.OnlineVideos, loadingParam);
            }
        }
        #endif

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
            List<TraktImage> showImages = new List<TraktImage>();

            int id = 0;

            // add each episode to the list
            foreach (var episode in friend.WatchedEpisodes)
            {
                GUITraktUserListItem episodeItem = new GUITraktUserListItem(episode.ToString());

                // add images for download
                TraktImage images = new TraktImage
                {
                    EpisodeImages = episode.Episode.Images,
                    ShowImages = episode.Show.Images
                };
                showImages.Add(images);

                episodeItem.Label2 = episode.WatchedDate.FromEpoch().ToShortDateString();
                episodeItem.Item = images;
                episodeItem.TVTag = episode;
                episodeItem.ItemId = id++;
                episodeItem.IconImage = "defaultTraktEpisode.png";
                episodeItem.IconImageBig = "defaultTraktEpisodeBig.png";
                episodeItem.ThumbnailImage = "defaultTraktEpisodeBig.png";
                episodeItem.Item = episode;
                episodeItem.OnItemSelected += OnEpisodeSelected;
                Utils.SetDefaultIcons(episodeItem);
                Facade.Add(episodeItem);
            }

            Facade.SelectedListItemIndex = 0;

            // Download Episode Thumbnails Async and set to facade
            GetImages<TraktImage>(showImages);
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
            // Fanart
            backdrop.GUIImageOne = FanartBackground;
            backdrop.GUIImageTwo = FanartBackground2;
            backdrop.LoadingImage = loadingImage;

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
            GUIUtils.SetProperty("#Trakt.Show.FanartFileName", string.Empty);
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
            GUIUtils.SetProperty("#Trakt.Movie.FanartImageFilename", string.Empty);
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
            SetProperty("#Trakt.Show.Overview", string.IsNullOrEmpty(episode.Show.Overview) ? Translation.NoShowSummary : episode.Show.Overview);
            SetProperty("#Trakt.Show.Runtime", episode.Show.Runtime.ToString());
            SetProperty("#Trakt.Show.Year", episode.Show.Year.ToString());
            SetProperty("#Trakt.Show.FanartImageFilename", episode.Show.Images.FanartImageFilename);
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
            SetProperty("#Trakt.Movie.Overview", string.IsNullOrEmpty(movie.Movie.Overview) ? Translation.NoMovieSummary : movie.Movie.Overview);
            SetProperty("#Trakt.Movie.Released", movie.Movie.Released.FromEpoch().ToShortDateString());
            SetProperty("#Trakt.Movie.Runtime", movie.Movie.Runtime.ToString());
            SetProperty("#Trakt.Movie.Tagline", movie.Movie.Tagline);
            SetProperty("#Trakt.Movie.Title", movie.Movie.Title);
            SetProperty("#Trakt.Movie.Tmdb", movie.Movie.Tmdb);
            SetProperty("#Trakt.Movie.Trailer", movie.Movie.Trailer);
            SetProperty("#Trakt.Movie.Url", movie.Movie.Url);
            SetProperty("#Trakt.Movie.Year", movie.Movie.Year);
            SetProperty("#Trakt.Movie.PosterImageFilename", movie.Movie.Images.PosterImageFilename);
            SetProperty("#Trakt.Movie.FanartImageFilename", movie.Movie.Images.FanartImageFilename);
        }

        private void OnFriendSelected(GUIListItem item, GUIControl parent)
        {
            CurrentFriend = (item as GUITraktUserListItem).Item as TraktFriend;
            PublishFriendSkinProperties(CurrentFriend);
            GUIImageHandler.LoadFanart(backdrop, string.Empty);
        }

        private void OnWatchedTypeSelected(GUIListItem item, GUIControl parent)
        {
            if (item.Label == Translation.Episodes)
                SelectedType = WatchedHistoryType.Episodes;
            else
                SelectedType = WatchedHistoryType.Movies;
            
            PublishFriendSkinProperties(CurrentFriend);
            GUIImageHandler.LoadFanart(backdrop, string.Empty);
        }

        private void OnEpisodeSelected(GUIListItem item, GUIControl parent)
        {
            TraktFriend.WatchItem episode = item.TVTag as TraktFriend.WatchItem;
            PublishEpisodeSkinProperties(episode);
            GUIImageHandler.LoadFanart(backdrop, episode.Show.Images.FanartImageFilename);
        }

        private void OnMovieSelected(GUIListItem item, GUIControl parent)
        {
            TraktFriend.WatchItem movie = item.TVTag as TraktFriend.WatchItem;
            PublishMovieSkinProperties(movie);
            GUIImageHandler.LoadFanart(backdrop, movie.Movie.Images.FanartImageFilename);
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
                        #region Facade Items
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
                            remoteThumb = (item as TraktImage).EpisodeImages.Screen;
                            localThumb = (item as TraktImage).EpisodeImages.EpisodeImageFilename;                            
                        }

                        if (!string.IsNullOrEmpty(remoteThumb) && !string.IsNullOrEmpty(localThumb))
                        {
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
                                    (item as TraktImage).NotifyPropertyChanged("EpisodeImages");
                                }
                            }
                        }
                        #endregion

                        #region Fanart
                        // stop download if we have exited window
                        if (StopDownload) break;
                        if (!TraktSettings.DownloadFanart) continue;

                        string remoteFanart = string.Empty;
                        string localFanart = string.Empty;

                        if (item is TraktMovie.MovieImages)
                        {
                            remoteThumb = (item as TraktMovie.MovieImages).Poster;
                            localThumb = (item as TraktMovie.MovieImages).PosterImageFilename;

                            remoteFanart = (item as TraktMovie.MovieImages).Fanart;
                            localFanart = (item as TraktMovie.MovieImages).FanartImageFilename;
                        }
                        else if (item is TraktImage)
                        {
                            remoteFanart = (item as TraktImage).ShowImages.Fanart;
                            localFanart = (item as TraktImage).ShowImages.FanartImageFilename;
                        }

                        if (!string.IsNullOrEmpty(remoteFanart) && !string.IsNullOrEmpty(localFanart))
                        {
                            if (GUIImageHandler.DownloadImage(remoteFanart, localFanart))
                            {
                                // notify that image has been downloaded                               
                                if (item is TraktMovie.MovieImages)
                                {
                                    (item as TraktMovie.MovieImages).NotifyPropertyChanged("FanartImageFilename");
                                }
                                else if (item is TraktImage)
                                {
                                    (item as TraktImage).NotifyPropertyChanged("ShowImages");
                                }
                            }
                        }
                        #endregion
                    }
                    #if !MP12
                    // refresh the facade so thumbnails get displayed
                    // this is not needed in MP 1.2 Beta
                    GUIWindowManager.SendThreadMessage(new GUIMessage(GUIMessage.MessageType.GUI_MSG_REFRESH, GUIWindowManager.ActiveWindow, 0, 50, 0, 0, null));
                    #endif
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
                    else if (s is TraktMovie.MovieImages && e.PropertyName == "PosterImageFilename")
                        SetImageToGui((s as TraktMovie.MovieImages).PosterImageFilename);
                    else if (s is TraktImage && e.PropertyName == "EpisodeImages")
                        SetImageToGui((s as TraktImage).EpisodeImages.EpisodeImageFilename);
                    else if (s is TraktMovie.MovieImages && e.PropertyName == "FanartImageFilename")
                        UpdateCurrentSelection();                    
                    else if (s is TraktImage && e.PropertyName == "ShowImages")
                        UpdateCurrentSelection();

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

            // if selected and is current window force an update of thumbnail
            UpdateCurrentSelection();
        }

        protected void UpdateCurrentSelection()
        {
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