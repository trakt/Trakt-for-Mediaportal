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
            Trailers,
            DeleteFriend,
            SearchFriend,
            AddFriend,
            AddToList,
            Shouts
        }

        enum ViewType
        {
            EpisodeWatchHistory,
            MovieWatchHistory,
            EpisodeWatchList,
            ShowWatchList,
            MovieWatchList
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
        static Views ViewLevel { get; set; }
        ViewType SelectedType { get; set; }
        GUIFriendItem CurrentFriend { get; set; }
        ImageSwapper backdrop;
        static DateTime LastRequest = new DateTime();
        Dictionary<string, IEnumerable<TraktWatchedEpisode>> friendEpisodeHistory = new Dictionary<string, IEnumerable<TraktWatchedEpisode>>();
        Dictionary<string, IEnumerable<TraktWatchedMovie>> friendMovieHistory = new Dictionary<string, IEnumerable<TraktWatchedMovie>>();
        int PreviousFriendSelectedIndex = 0;
        int PreviousTypeSelectedIndex = 0;
        int PreviousEpisodeSelectedIndex = 0;
        int PreviousMovieSelelectedIndex = 0;

        IEnumerable<TraktWatchedEpisode> WatchedEpisodes
        {
            get
            {
                if (!friendEpisodeHistory.Keys.Contains(CurrentFriend.Username) || LastRequest < DateTime.UtcNow.Subtract(new TimeSpan(0, TraktSettings.WebRequestCacheMinutes, 0)))
                {
                    _WatchedEpisodes = TraktAPI.TraktAPI.GetUserEpisodeWatchedHistory(CurrentFriend.Username);
                    if (friendEpisodeHistory.Keys.Contains(CurrentFriend.Username)) friendEpisodeHistory.Remove(CurrentFriend.Username);
                    friendEpisodeHistory.Add(CurrentFriend.Username, _WatchedEpisodes);
                    LastRequest = DateTime.UtcNow;
                }
                return friendEpisodeHistory[CurrentFriend.Username];
            }
        }
        private IEnumerable<TraktWatchedEpisode> _WatchedEpisodes = null;

        IEnumerable<TraktWatchedMovie> WatchedMovies
        {
            get
            {
                if (!friendMovieHistory.Keys.Contains(CurrentFriend.Username) || LastRequest < DateTime.UtcNow.Subtract(new TimeSpan(0, TraktSettings.WebRequestCacheMinutes, 0)))
                {
                    _WatchedMovies = TraktAPI.TraktAPI.GetUserMovieWatchedHistory(CurrentFriend.Username);
                    if (friendMovieHistory.Keys.Contains(CurrentFriend.Username)) friendMovieHistory.Remove(CurrentFriend.Username);
                    friendMovieHistory.Add(CurrentFriend.Username, _WatchedMovies);
                    LastRequest = DateTime.UtcNow;
                }
                return friendMovieHistory[CurrentFriend.Username];
            }
        }
        private IEnumerable<TraktWatchedMovie> _WatchedMovies = null;

        IEnumerable<GUIFriendItem> TraktFriends
        {
            get
            {
                if (_Friends == null || LastRequest < DateTime.UtcNow.Subtract(new TimeSpan(0, TraktSettings.WebRequestCacheMinutes, 0)))
                {
                    _Friends = TraktAPI.TraktAPI.GetFriends();
                    LastRequest = DateTime.UtcNow;
                    PreviousFriendSelectedIndex = 0;
                    friendMovieHistory.Clear();
                    friendEpisodeHistory.Clear();
                }
                return _Friends;
            }
        }
        static IEnumerable<GUIFriendItem> _Friends = null;

        #endregion

        #region Public Properties

        public static IEnumerable<GUIFriendItem> FriendRequests
        {
            get
            {
                if (_FriendRequests == null || LastRequest < DateTime.UtcNow.Subtract(new TimeSpan(0, TraktSettings.WebRequestCacheMinutes, 0)))
                {
                    _FriendRequests = TraktAPI.TraktAPI.GetFriendRequests();
                    LastRequest = DateTime.UtcNow;
                }
                return _FriendRequests;
            }
        }
        static IEnumerable<GUIFriendItem> _FriendRequests = null;

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
            // Clear GUI properties
            ClearProperties();

            // Requires Login
            if (!GUICommon.CheckLogin()) return;

            // Initialize
            InitProperties();

            // Load Last View
            switch (ViewLevel)
            {
                case Views.Friends:
                    LoadFriendsList();
                    break;

                case Views.WatchedTypes:
                    LoadWatchedTypes();
                    break;

                case Views.WatchedHistory:
                    PublishFriendSkinProperties(CurrentFriend);
                    LoadWatchedHistory();
                    break;

                default:
                    LoadFriendsList();
                    break;
            }
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
                                GUITraktUserListItem friend = Facade.SelectedListItem as GUITraktUserListItem;
                                if (friend.IsRemote)
                                {
                                    // Pending Friend Request, Get Approval from user
                                    if (GUIUtils.ShowYesNoDialog(Translation.FriendRequest, string.Format(Translation.ApproveFriendMessage, friend.Label), true))
                                    {
                                        // Friend Approved, add Friend
                                        ApproveFriend(friend.Item as GUIFriendItem);

                                        // update cache
                                        (friend.Item as GUIFriendItem).ApprovedDate = DateTime.UtcNow.ToEpoch();
                                        _Friends = _Friends.Concat(_FriendRequests.Where(f => f.Username == friend.Label));
                                        _FriendRequests = _FriendRequests.Except(_FriendRequests.Where(f => f.Username == friend.Label));
                                    }
                                    else
                                    {
                                        // Friend Denied, remove Friend from Pending Requests
                                        DenyFriend(friend.Item as GUIFriendItem);

                                        // update cache
                                        _FriendRequests = _FriendRequests.Except(_FriendRequests.Where(f => f.Username == friend.Label));
                                    }

                                    // reload
                                    SendFriendsToFacade(_Friends, _FriendRequests);
                                }
                                else if (friend.IsSearchItem)
                                {
                                    if (GUIUtils.ShowYesNoDialog(Translation.Friends, string.Format(Translation.SendFriendRequest, friend.Label), true))
                                    {
                                        AddFriend(friend.Item as GUIFriendItem);
                                        // return to friends list
                                        LoadFriendsList();
                                    }
                                }
                                else if (friend.IsFolder)
                                {
                                    // return to friends list
                                    LoadFriendsList();
                                }
                                else
                                    LoadWatchedTypes();
                                break;

                            case Views.WatchedTypes:
                                if (SelectedType == ViewType.EpisodeWatchHistory || SelectedType == ViewType.MovieWatchHistory)
                                {
                                    LoadWatchedHistory();
                                }
                                else
                                {
                                    // Launch Corresponding Watch List window
                                    switch (SelectedType)
                                    {
                                        case (ViewType.MovieWatchList):
                                            GUIWatchListMovies.CurrentUser = CurrentFriend.Username;
                                            GUIWindowManager.ActivateWindow((int)TraktGUIWindows.WatchedListMovies);
                                            break;

                                        case (ViewType.ShowWatchList):
                                            GUIWatchListShows.CurrentUser = CurrentFriend.Username;
                                            GUIWindowManager.ActivateWindow((int)TraktGUIWindows.WatchedListShows);
                                            break;

                                        case (ViewType.EpisodeWatchList):
                                            GUIWatchListEpisodes.CurrentUser = CurrentFriend.Username;
                                            GUIWindowManager.ActivateWindow((int)TraktGUIWindows.WatchedListEpisodes);
                                            break;
                                    }
                                }
                                break;

                            case Views.WatchedHistory:
                                if (SelectedType == ViewType.MovieWatchHistory)
                                {
                                    CheckAndPlayMovie(true);
                                }

                                if (SelectedType == ViewType.EpisodeWatchHistory)
                                {
                                    CheckAndPlayEpisode();
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
                            LoadWatchedTypes();
                            return;

                        case Views.WatchedTypes:
                            LoadFriendsList();
                            return;
                    }
                    break;

                case Action.ActionType.ACTION_PLAY:
                case Action.ActionType.ACTION_MUSIC_PLAY:
                    if (SelectedType == ViewType.MovieWatchHistory)
                    {
                        CheckAndPlayMovie(false);
                    }

                    if (SelectedType == ViewType.EpisodeWatchHistory)
                    {
                        CheckAndPlayEpisode();
                    }
                    break;
            }
            base.OnAction(action);
        }
        
        protected override void OnShowContextMenu()
        {            
            TraktWatchedMovie selectedMovie = null;            
            TraktWatchedEpisode selectedEpisode = null;

            GUITraktUserListItem selectedItem = this.Facade.SelectedListItem as GUITraktUserListItem;
            if (selectedItem == null) return;

            if (ViewLevel == Views.WatchedHistory && SelectedType == ViewType.MovieWatchHistory)
                selectedMovie = (TraktWatchedMovie)selectedItem.TVTag;

            if (ViewLevel == Views.WatchedHistory && SelectedType == ViewType.EpisodeWatchHistory)
                selectedEpisode = (TraktWatchedEpisode)selectedItem.TVTag;

            IDialogbox dlg = (IDialogbox)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
            if (dlg == null) return;

            dlg.Reset();
            dlg.SetHeading(GUIUtils.PluginName());

            GUIListItem listItem = null;
            int itemCount = 0;

            // Search for new friends
            if (ViewLevel == Views.Friends)
            {
                listItem = new GUIListItem(Translation.SearchForFriend);
                dlg.Add(listItem);
                listItem.ItemId = (int)ContextMenuItem.SearchFriend;
                itemCount++;
            }

            // Add friend found in search
            if (ViewLevel == Views.Friends && selectedItem.IsSearchItem)
            {
                listItem = new GUIListItem(Translation.AddFriend);
                dlg.Add(listItem);
                listItem.ItemId = (int)ContextMenuItem.AddFriend;
                itemCount++;
            }

            // Trailers
            #if MP12
            if ((selectedMovie != null) && TraktHelper.IsOnlineVideosAvailableAndEnabled)
            {
                listItem = new GUIListItem(Translation.Trailers);
                dlg.Add(listItem);
                listItem.ItemId = (int)ContextMenuItem.Trailers;
                itemCount++;
            }
            if ((selectedEpisode != null) && TraktHelper.IsOnlineVideosAvailableAndEnabled)
            {
                listItem = new GUIListItem(Translation.Trailers);
                dlg.Add(listItem);
                listItem.ItemId = (int)ContextMenuItem.Trailers;
                itemCount++;
            }
            #endif

            // Add to Custom List
            if (selectedMovie != null || selectedEpisode != null)
            {
                listItem = new GUIListItem(Translation.AddToList);
                dlg.Add(listItem);
                listItem.ItemId = (int)ContextMenuItem.AddToList;
                itemCount++;
            }

            // Shouts
            if (ViewLevel == Views.WatchedHistory)
            {
                listItem = new GUIListItem(Translation.Shouts + "...");
                dlg.Add(listItem);
                listItem.ItemId = (int)ContextMenuItem.Shouts;
                itemCount++;
            }

            // Delete a Friend
            if ((ViewLevel == Views.Friends) && selectedItem.IsFriend)
            {
                listItem = new GUIListItem(Translation.DeleteFriend);
                dlg.Add(listItem);
                listItem.ItemId = (int)ContextMenuItem.DeleteFriend;
                itemCount++;
            }

            if (itemCount == 0) return;

            // Show Context Menu
            dlg.DoModal(GUIWindowManager.ActiveWindow);
            if (dlg.SelectedId < 0) return;

            switch (dlg.SelectedId)
            {
                case ((int)ContextMenuItem.AddFriend):
                    if (GUIUtils.ShowYesNoDialog(Translation.Friends, string.Format(Translation.SendFriendRequest, selectedItem.Label), true))
                    {
                        AddFriend(selectedItem.Item as GUIFriendItem);
                        // return to friends list
                        LoadFriendsList();
                    }
                    break;

                case ((int)ContextMenuItem.SearchFriend):
                    string userSearchTerm = string.Empty;
                    if (GUIUtils.GetStringFromKeyboard(ref userSearchTerm))
                    {
                        LoadSearchResults(userSearchTerm);
                    }
                    break;

                #if MP12
                case ((int)ContextMenuItem.Trailers):
                    if (selectedMovie != null)
                        ShowTrailersMenu<TraktWatchedMovie>(selectedMovie);
                    else
                        ShowTrailersMenu<TraktWatchedEpisode>(selectedEpisode);
                    break;
                #endif

                case ((int)ContextMenuItem.AddToList):
                    if (selectedMovie != null)
                    {
                        TraktHelper.AddRemoveMovieInUserList(selectedMovie.Movie.Title, selectedMovie.Movie.Year, selectedMovie.Movie.Imdb, false);
                    }
                    else
                    {
                        TraktHelper.AddRemoveEpisodeInUserList(selectedEpisode.Show.Title, selectedEpisode.Show.Year.ToString(), selectedEpisode.Episode.Season.ToString(), selectedEpisode.Episode.Number.ToString(), selectedEpisode.Show.Tvdb, false);
                    }
                    break;

                case ((int)ContextMenuItem.Shouts):
                    if (SelectedType == ViewType.EpisodeWatchHistory && selectedEpisode != null)
                    {
                        GUIShouts.ShoutType = GUIShouts.ShoutTypeEnum.episode;
                        GUIShouts.EpisodeInfo = new EpisodeShout
                        {
                            TVDbId = selectedEpisode.Show.Tvdb,
                            IMDbId = selectedEpisode.Show.Imdb,
                            Title = selectedEpisode.Show.Title,
                            SeasonIdx = selectedEpisode.Episode.Season.ToString(),
                            EpisodeIdx = selectedEpisode.Episode.Number.ToString()
                        };
                        GUIShouts.Fanart = selectedEpisode.Show.Images.FanartImageFilename;
                        GUIWindowManager.ActivateWindow((int)TraktGUIWindows.Shouts);
                    }
                    else if (SelectedType == ViewType.MovieWatchHistory && selectedMovie != null)
                    {
                        GUIShouts.ShoutType = GUIShouts.ShoutTypeEnum.movie;
                        GUIShouts.MovieInfo = new MovieShout 
                        { 
                            IMDbId = selectedMovie.Movie.Imdb, 
                            TMDbId = selectedMovie.Movie.Tmdb, 
                            Title = selectedMovie.Movie.Title, 
                            Year = selectedMovie.Movie.Year 
                        };
                        GUIShouts.Fanart = selectedMovie.Movie.Images.FanartImageFilename;
                        GUIWindowManager.ActivateWindow((int)TraktGUIWindows.Shouts);
                    }
                    break;

                case ((int)ContextMenuItem.DeleteFriend):
                    if (GUIUtils.ShowYesNoDialog(Translation.DeleteFriend, string.Format(Translation.DeleteFriendMessage, selectedItem.Label)))
                    {
                        // Delete friend
                        DeleteFriend(selectedItem.Item as GUIFriendItem);
                        // Clear Cache
                        _Friends = _Friends.Except(_Friends.Where(f => f.Username == selectedItem.Label));
                        // Re-Load list
                        SendFriendsToFacade(_Friends, _FriendRequests);
                    }
                    break;

                default:
                    break;
            }

            base.OnShowContextMenu();
        }
       
        #endregion

        #region Private Methods

        private void CheckAndPlayMovie(bool jumpTo)
        {
            GUIListItem selectedItem = this.Facade.SelectedListItem;
            if (selectedItem == null) return;

            TraktWatchedMovie selectedMovie = selectedItem.TVTag as TraktWatchedMovie;
            if (selectedItem == null) return;

            string title = selectedMovie.Movie.Title;
            string imdbid = selectedMovie.Movie.Imdb;
            int year = Convert.ToInt32(selectedMovie.Movie.Year);

            GUICommon.CheckAndPlayMovie(jumpTo, title, year, imdbid);
        }

        private void CheckAndPlayEpisode()
        {
            GUIListItem selectedItem = this.Facade.SelectedListItem;
            if (selectedItem == null) return;

            TraktWatchedEpisode episode = selectedItem.TVTag as TraktWatchedEpisode;
            if (episode == null) return;

            int seriesid = Convert.ToInt32(episode.Show.Tvdb);
            int seasonidx = episode.Episode.Season;
            int episodeidx = episode.Episode.Number;
            string searchterm = string.IsNullOrEmpty(episode.Show.Imdb) ? episode.Show.Title : episode.Show.Imdb;

            GUICommon.CheckAndPlayEpisode(seriesid, searchterm, seasonidx, episodeidx);
        }

        private TraktFriend CreateFriendData(GUIFriendItem user)
        {
            TraktFriend friend = new TraktFriend
            {
                Username = TraktSettings.Username,
                Password = TraktSettings.Password,
                Friend = user.Username
            };
            return friend;
        }

        private void AddFriend(GUIFriendItem user)
        {
            Thread addFriendThread = new Thread(delegate(object obj)
            {
                TraktResponse response = TraktAPI.TraktAPI.FriendAdd(CreateFriendData(user));
                TraktAPI.TraktAPI.LogTraktResponse<TraktResponse>(response);
            })
            {
                IsBackground = true,
                Name = "Adding Friend"
            };

            addFriendThread.Start(user);
        }

        private void ApproveFriend(GUIFriendItem user)
        {
            Thread approveFriendThread = new Thread(delegate(object obj)
            {
                TraktResponse response = TraktAPI.TraktAPI.FriendApprove(CreateFriendData(user));
                TraktAPI.TraktAPI.LogTraktResponse<TraktResponse>(response);
            })
            {
                IsBackground = true,
                Name = "Approving Friend Request"
            };

            approveFriendThread.Start(user);
        }

        private void DenyFriend(GUIFriendItem user)
        {
            Thread denyFriendThread = new Thread(delegate(object obj)
            {
                TraktResponse response = TraktAPI.TraktAPI.FriendDeny(CreateFriendData(user));
                TraktAPI.TraktAPI.LogTraktResponse<TraktResponse>(response);
            })
            {
                IsBackground = true,
                Name = "Denying Friend Request"
            };

            denyFriendThread.Start(user);
        }

        private void DeleteFriend(GUIFriendItem user)
        {
            Thread deleteFriendThread = new Thread(delegate(object obj)
            {
                TraktResponse response = TraktAPI.TraktAPI.FriendDelete(CreateFriendData(user));
                TraktAPI.TraktAPI.LogTraktResponse<TraktResponse>(response);
            })
            {
                IsBackground = true,
                Name = "Deleting Friend Request"
            };

            deleteFriendThread.Start(user);
        }

        #if MP12
        private void ShowTrailersMenu<T>(T item)
        {
            IDialogbox dlg = (IDialogbox)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
            dlg.Reset();
            dlg.SetHeading(Translation.Trailer);

            foreach (TrailerSite site in Enum.GetValues(typeof(TrailerSite)))
            {
                string menuItem = Enum.GetName(typeof(TrailerSite), site);
                // iTunes site only supports movie trailers
                if (item is TraktWatchedEpisode && menuItem == "iTunes") continue;
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
                        if (item is TraktWatchedMovie)
                        {
                            if (!string.IsNullOrEmpty((item as TraktWatchedMovie).Movie.Imdb))
                                // Exact search
                                searchParam = (item as TraktWatchedMovie).Movie.Imdb;
                            else
                                searchParam = (item as TraktWatchedMovie).Movie.Title;
                        }
                        else
                        {
                            if (!string.IsNullOrEmpty((item as TraktWatchedEpisode).Show.Imdb))
                                // Exact search
                                searchParam = (item as TraktWatchedEpisode).Show.Imdb;
                            else
                                searchParam = (item as TraktWatchedEpisode).Show.Title;
                        }
                        break;

                    case ("iTunes"):
                        siteUtil = "iTunes Movie Trailers";
                        searchParam = (item as TraktWatchedMovie).Movie.Title;
                        break;

                    case ("YouTube"):
                        siteUtil = "YouTube";
                        if (item is TraktWatchedMovie)
                            searchParam = (item as TraktWatchedMovie).Movie.Title;
                        else
                            searchParam = (item as TraktWatchedEpisode).Show.Title;
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
                    // Get Friend List from Result Handler
                    IEnumerable<GUIFriendItem> friends = result as IEnumerable<GUIFriendItem>;

                    #region Get Friend Requests for user as well
                    GUIBackgroundTask.Instance.ExecuteInBackgroundAndCallback(() =>
                    {
                        return FriendRequests;
                    },
                    delegate(bool frSuccess, object frResult)
                    {
                        IEnumerable<GUIFriendItem> friendRequests = null;
                        if (frSuccess)
                        {
                            // Get Friend Requests from Result Handler
                            friendRequests = result as IEnumerable<GUIFriendItem>;
                        }
                    }, Translation.GettingFriendsRequests, true);
                    #endregion

                    SendFriendsToFacade(friends, FriendRequests);
                }
            }, Translation.GettingFriendsList, true);
        }

        private void LoadSearchResults(string searchTerm)
        {
            GUIBackgroundTask.Instance.ExecuteInBackgroundAndCallback(() =>
            {
                return TraktAPI.TraktAPI.SearchForFriends(searchTerm);
            },
            delegate(bool success, object result)
            {
                if (success)
                {
                    SendSearchResultsToFacade(result as IEnumerable<GUIFriendItem>);
                }
            }, Translation.GettingSearchResults, true);
        }

        private void LoadWatchedTypes()
        {
            if (CurrentFriend == null) return;

            // only two types to choose from in this view level
            // signal that we are now displaying the watched types view
            ViewLevel = Views.WatchedTypes;
            SetCurrentView();            
            
            // clear facade
            GUIControl.ClearControl(GetID, Facade.GetID);

            // add each type to the list           
            GUITraktUserListItem item = new GUITraktUserListItem(Translation.WatchedEpisodes);
            item.IconImage = CurrentFriend.AvatarFilename;
            item.IconImageBig = CurrentFriend.AvatarFilename;
            item.ThumbnailImage = CurrentFriend.AvatarFilename;
            item.OnItemSelected += OnWatchedTypeSelected;
            Utils.SetDefaultIcons(item);
            Facade.Add(item);

            item = new GUITraktUserListItem(Translation.WatchedMovies);
            item.IconImage = CurrentFriend.AvatarFilename;
            item.IconImageBig = CurrentFriend.AvatarFilename;
            item.ThumbnailImage = CurrentFriend.AvatarFilename;
            item.OnItemSelected += OnWatchedTypeSelected;
            Utils.SetDefaultIcons(item);
            Facade.Add(item);

            item = new GUITraktUserListItem(Translation.WatchListShows);
            item.IconImage = CurrentFriend.AvatarFilename;
            item.IconImageBig = CurrentFriend.AvatarFilename;
            item.ThumbnailImage = CurrentFriend.AvatarFilename;
            item.OnItemSelected += OnWatchedTypeSelected;
            Utils.SetDefaultIcons(item);
            Facade.Add(item);

            item = new GUITraktUserListItem(Translation.WatchListMovies);
            item.IconImage = CurrentFriend.AvatarFilename;
            item.IconImageBig = CurrentFriend.AvatarFilename;
            item.ThumbnailImage = CurrentFriend.AvatarFilename;
            item.OnItemSelected += OnWatchedTypeSelected;
            Utils.SetDefaultIcons(item);
            Facade.Add(item);

            item = new GUITraktUserListItem(Translation.WatchListEpisodes);
            item.IconImage = CurrentFriend.AvatarFilename;
            item.IconImageBig = CurrentFriend.AvatarFilename;
            item.ThumbnailImage = CurrentFriend.AvatarFilename;
            item.OnItemSelected += OnWatchedTypeSelected;
            Utils.SetDefaultIcons(item);
            Facade.Add(item);

            Facade.SelectedListItemIndex = PreviousTypeSelectedIndex;            

            GUIUtils.SetProperty("#itemcount", Facade.Count.ToString());
            GUIUtils.SetProperty("#Trakt.Items", string.Format("{0} {1}", Facade.Count.ToString(), GUILocalizeStrings.Get(507)));
        }

        private void LoadWatchedHistory()
        {
            if (CurrentFriend == null) return;

            GUIBackgroundTask.Instance.ExecuteInBackgroundAndCallback(() =>
            {
                if (SelectedType == ViewType.EpisodeWatchHistory)
                    return WatchedEpisodes;
                else
                    return WatchedMovies;
            },
            delegate(bool success, object result)
            {
                if (success)
                {
                    if (SelectedType == ViewType.EpisodeWatchHistory)
                        SendWatchedEpisodeHistoryToFacade(result as IEnumerable<TraktWatchedEpisode>);
                    else
                        SendWatchedMovieHistoryToFacade(result as IEnumerable<TraktWatchedMovie>);
                }
            }, Translation.GettingFriendsWatchedHistory, true);
        }

        private void SendWatchedEpisodeHistoryToFacade(IEnumerable<TraktWatchedEpisode> episodes)
        {
            if (episodes.Count() == 0)
            {
                GUIUtils.ShowNotifyDialog(GUIUtils.PluginName(), Translation.UserHasNotWatchedEpisodes);
                return;
            }

            // signal that we are now displaying the watched history view
            ViewLevel = Views.WatchedHistory;
            SetCurrentView();
            GUIUtils.SetProperty("#itemcount", episodes.Count().ToString());
            GUIUtils.SetProperty("#Trakt.Items", string.Format("{0} {1}", episodes.Count().ToString(), episodes.Count() > 1 ? Translation.Episodes : Translation.Episode));

            // clear facade
            GUIControl.ClearControl(GetID, Facade.GetID);

            // store a list of thumbnails to download
            List<TraktImage> showImages = new List<TraktImage>();

            int id = 0;

            // add each episode to the list
            foreach (var episode in episodes)
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

            Facade.SelectedListItemIndex = PreviousEpisodeSelectedIndex;

            // Download Episode Thumbnails Async and set to facade
            GetImages<TraktImage>(showImages);
        }

        private void SendWatchedMovieHistoryToFacade(IEnumerable<TraktWatchedMovie> movies)
        {
            if (movies.Count() == 0)
            {
                GUIUtils.ShowNotifyDialog(GUIUtils.PluginName(), Translation.UserHasNotWatchedMovies);
                return;
            }

            // signal that we are now displaying the watched history view
            ViewLevel = Views.WatchedHistory;
            SetCurrentView();
            GUIUtils.SetProperty("#itemcount", movies.Count().ToString());
            GUIUtils.SetProperty("#Trakt.Items", string.Format("{0} {1}", movies.Count().ToString(), movies.Count() > 1 ? Translation.Movies : Translation.Movie));

            // clear facade
            GUIControl.ClearControl(GetID, Facade.GetID);

            // store a list of posters to download
            List<TraktMovie.MovieImages> movieImages = new List<TraktMovie.MovieImages>();

            int id = 0;

            // add each episode to the list
            foreach (var movie in movies)
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

            Facade.SelectedListItemIndex = PreviousMovieSelelectedIndex;

            // Download Movie Posters Async and set to facade
            GetImages<TraktMovie.MovieImages>(movieImages);
        }

        private void SendSearchResultsToFacade(IEnumerable<GUIFriendItem> searchResults)
        {
            int itemCount = searchResults.Count();

            if (itemCount == 0)
            {
                GUIUtils.ShowNotifyDialog(GUIUtils.PluginName(), Translation.NoSearchResultsFound);
                return;
            }

            // clear facade
            GUIControl.ClearControl(GetID, Facade.GetID);
            ClearProperties();

            GUIUtils.SetProperty("#itemcount", itemCount.ToString());
            GUIUtils.SetProperty("#Trakt.Items", string.Format("{0} {1}", itemCount.ToString(), itemCount > 1 ? Translation.Users : Translation.User));

            int id = 0;

            // Create Back item to return to friends list
            GUITraktUserListItem userItem = new GUITraktUserListItem("..");
            userItem.ItemId = id++;
            userItem.IsFolder = true;
            userItem.IconImage = "defaultFolderBack.png";
            userItem.IconImageBig = "defaultFolderBackBig.png";
            userItem.ThumbnailImage = "defaultFolderBackBig.png";
            userItem.OnItemSelected += OnFriendSelected;
            Facade.Add(userItem);

            foreach (var user in searchResults)
            {
                if (user.Username == TraktSettings.Username) continue;
                userItem = new GUITraktUserListItem(user.Username);

                userItem.Label2 = user.JoinDate.FromEpoch().ToShortDateString();
                userItem.Item = user;
                userItem.ItemId = id++;
                userItem.IsSearchItem = true;
                userItem.IconImage = "defaultTraktUser.png";
                userItem.IconImageBig = "defaultTraktUserBig.png";
                userItem.ThumbnailImage = "defaultTraktUserBig.png";
                userItem.OnItemSelected += OnFriendSelected;
                Facade.Add(userItem);
            }

            // select first item
            Facade.SelectedListItemIndex = 1;

            // Set Facade Layout
            Facade.SetCurrentLayout("List");
            GUIControl.FocusControl(GetID, Facade.GetID);

            // Download avatars Async and set to facade
            List<GUIFriendItem> images = new List<GUIFriendItem>(searchResults.ToList());
            GetImages<GUIFriendItem>(images);
        }

        private void SendFriendsToFacade(IEnumerable<GUIFriendItem> friends, IEnumerable<GUIFriendItem> friendRequests)
        {
            // clear facade
            GUIControl.ClearControl(GetID, Facade.GetID);

            // signal that we are now displaying the watched history view
            ViewLevel = Views.Friends;
            SetCurrentView();
            int friendCount = friends.Count() + (friendRequests == null ? 0 : friendRequests.Count());
            GUIUtils.SetProperty("#itemcount", friendCount.ToString());
            GUIUtils.SetProperty("#Trakt.Items", string.Format("{0} {1}", friendCount.ToString(), friendCount > 1 ? Translation.Friends : Translation.Friend));

            if (friendCount == 0)
            {
                GUIUtils.ShowNotifyDialog(GUIUtils.PluginName(), Translation.NoFriendsTaunt);
                GUIWindowManager.ShowPreviousWindow();
                return;
            }

            int id = 0;

            // Add friend requests at top of list
            if (friendRequests != null)
            {
                foreach (var user in friendRequests)
                {
                    GUITraktUserListItem userItem = new GUITraktUserListItem(user.Username);

                    userItem.Label2 = user.JoinDate.FromEpoch().ToShortDateString();
                    userItem.Item = user;
                    userItem.ItemId = id++;
                    userItem.IsRemote = true;
                    userItem.IconImage = "defaultTraktUser.png";
                    userItem.IconImageBig = "defaultTraktUserBig.png";
                    userItem.ThumbnailImage = "defaultTraktUserBig.png";
                    userItem.OnItemSelected += OnFriendSelected;
                    Utils.SetDefaultIcons(userItem);
                    Facade.Add(userItem);
                }
            }

            // Add each friend to the list
            foreach (var user in friends)
            {
                GUITraktUserListItem userItem = new GUITraktUserListItem(user.Username);

                userItem.Label2 = user.ApprovedDate.FromEpoch().ToShortDateString();
                userItem.Item = user;
                userItem.ItemId = id++;
                userItem.IsFriend = true;
                userItem.IconImage = "defaultTraktUser.png";
                userItem.IconImageBig = "defaultTraktUserBig.png";
                userItem.ThumbnailImage = "defaultTraktUserBig.png";
                userItem.OnItemSelected += OnFriendSelected;
                Utils.SetDefaultIcons(userItem);
                Facade.Add(userItem);
            }

            if (Facade.Count <= PreviousFriendSelectedIndex)
                Facade.SelectedListItemIndex = 0;
            else
                Facade.SelectedListItemIndex = PreviousFriendSelectedIndex;

            // Set Facade Layout
            Facade.SetCurrentLayout("List");
            GUIControl.FocusControl(GetID, Facade.GetID);

            // Download avatars Async and set to facade
            List<GUIFriendItem> friendImages = new List<GUIFriendItem>(friends.ToList());
            if (friendRequests != null) friendImages.AddRange(friendRequests);
            GetImages<GUIFriendItem>(friendImages);
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

            // Set Friends view as default if cache has expired
            if (CurrentFriend == null || LastRequest < DateTime.UtcNow.Subtract(new TimeSpan(0, TraktSettings.WebRequestCacheMinutes, 0)))
                ViewLevel = Views.Friends;
            
            GUIUtils.SetProperty("#Trakt.Selected.Type", string.Empty);
            GUIUtils.SetProperty("#Trakt.Items", string.Empty);
            SetCurrentView();
        }

        private void SetCurrentView()
        {
            GUIUtils.SetProperty("#Trakt.View.Level", ViewLevel.ToString());

            if (ViewLevel == Views.Friends)
                SetProperty("#Trakt.CurrentView", "Trakt " + Translation.Friends);
            else if (ViewLevel == Views.WatchedTypes)
                SetProperty("#Trakt.CurrentView", CurrentFriend.Username);
            else
                SetProperty("#Trakt.CurrentView", string.Format("{0} | {1}", CurrentFriend.Username, SelectedType == ViewType.EpisodeWatchHistory ? Translation.WatchedEpisodes : Translation.WatchedMovies));
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
            GUIUtils.SetProperty("#Trakt.User.ApprovedDate", string.Empty);
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

        private void PublishFriendSkinProperties(GUIFriendItem user)
        {
            SetProperty("#Trakt.User.About", user.About);
            SetProperty("#Trakt.User.Age", user.Age);
            SetProperty("#Trakt.User.Avatar", user.Avatar);
            SetProperty("#Trakt.User.AvatarFileName", user.AvatarFilename);
            SetProperty("#Trakt.User.FullName", user.FullName);
            SetProperty("#Trakt.User.Gender", user.Gender);
            SetProperty("#Trakt.User.JoinDate", user.JoinDate.FromEpoch().ToLongDateString());
            SetProperty("#Trakt.User.ApprovedDate", user.ApprovedDate == 0 ? "N/A" : user.ApprovedDate.FromEpoch().ToLongDateString());
            SetProperty("#Trakt.User.Location", user.Location);
            SetProperty("#Trakt.User.Protected", user.Protected);
            SetProperty("#Trakt.User.Url", user.Url);
            SetProperty("#Trakt.User.Username", user.Username);
        }

        private void PublishEpisodeSkinProperties(TraktWatchedEpisode episode)
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
            SetProperty("#Trakt.Episode.EpisodeImageFilename", episode.Episode.Images == null ? string.Empty : episode.Episode.Images.EpisodeImageFilename);
        }

        private void PublishMovieSkinProperties(TraktWatchedMovie movie)
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
            SetProperty("#Trakt.Movie.PosterImageFilename", movie.Movie.Images == null ? string.Empty : movie.Movie.Images.PosterImageFilename);
            SetProperty("#Trakt.Movie.FanartImageFilename", movie.Movie.Images == null ? string.Empty : movie.Movie.Images.FanartImageFilename);
        }

        private void OnFriendSelected(GUIListItem item, GUIControl parent)
        {
            if (item.IsFolder)
            {
                ClearProperties();
                return;
            }

            CurrentFriend = (item as GUITraktUserListItem).Item as GUIFriendItem;
            PublishFriendSkinProperties(CurrentFriend);
            GUIImageHandler.LoadFanart(backdrop, string.Empty);
            // reset selected indexes
            PreviousFriendSelectedIndex = Facade.SelectedListItemIndex;
            PreviousTypeSelectedIndex = 0;
            PreviousMovieSelelectedIndex = 0;
            PreviousEpisodeSelectedIndex = 0;
        }

        private void OnWatchedTypeSelected(GUIListItem item, GUIControl parent)
        {
            if (item.Label == Translation.WatchedEpisodes)
                SelectedType = ViewType.EpisodeWatchHistory;
            else if (item.Label == Translation.WatchedMovies)
                SelectedType = ViewType.MovieWatchHistory;
            else if (item.Label == Translation.WatchListMovies)
                SelectedType = ViewType.MovieWatchList;
            else if (item.Label == Translation.WatchListShows)
                SelectedType = ViewType.ShowWatchList;
            else
                SelectedType = ViewType.EpisodeWatchList;
            
            PublishFriendSkinProperties(CurrentFriend);
            GUIImageHandler.LoadFanart(backdrop, string.Empty);
            PreviousTypeSelectedIndex = Facade.SelectedListItemIndex;
        }

        private void OnEpisodeSelected(GUIListItem item, GUIControl parent)
        {
            TraktWatchedEpisode episode = item.TVTag as TraktWatchedEpisode;
            PublishEpisodeSkinProperties(episode);
            GUIImageHandler.LoadFanart(backdrop, episode.Show.Images.FanartImageFilename);
            PreviousEpisodeSelectedIndex = Facade.SelectedListItemIndex;
        }

        private void OnMovieSelected(GUIListItem item, GUIControl parent)
        {
            TraktWatchedMovie movie = item.TVTag as TraktWatchedMovie;
            PublishMovieSkinProperties(movie);
            GUIImageHandler.LoadFanart(backdrop, movie.Movie.Images.FanartImageFilename);
            PreviousMovieSelelectedIndex = Facade.SelectedListItemIndex;
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
                        
                        if (item is GUIFriendItem)
                        {
                            remoteThumb = (item as GUIFriendItem).Avatar;
                            localThumb = (item as GUIFriendItem).AvatarFilename;
                        }
                        else if (item is TraktMovie.MovieImages)
                        {
                            if ((item as TraktMovie.MovieImages) != null)
                            {
                                remoteThumb = (item as TraktMovie.MovieImages).Poster;
                                localThumb = (item as TraktMovie.MovieImages).PosterImageFilename;
                            }
                        }
                        else
                        {
                            if ((item as TraktImage).EpisodeImages != null)
                            {
                                remoteThumb = (item as TraktImage).EpisodeImages.Screen;
                                localThumb = (item as TraktImage).EpisodeImages.EpisodeImageFilename;
                            }
                        }

                        if (!string.IsNullOrEmpty(remoteThumb) && !string.IsNullOrEmpty(localThumb))
                        {
                            if (GUIImageHandler.DownloadImage(remoteThumb, localThumb))
                            {
                                // notify that image has been downloaded
                                if (item is GUIFriendItem)
                                {
                                    (item as GUIFriendItem).NotifyPropertyChanged("AvatarFilename");
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
                            if ((item as TraktMovie.MovieImages) != null)
                            {
                                remoteFanart = (item as TraktMovie.MovieImages).Fanart;
                                localFanart = (item as TraktMovie.MovieImages).FanartImageFilename;
                            }
                        }
                        else if (item is TraktImage)
                        {
                            if ((item as TraktImage).ShowImages != null)
                            {
                                remoteFanart = (item as TraktImage).ShowImages.Fanart;
                                localFanart = (item as TraktImage).ShowImages.FanartImageFilename;
                            }
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

        #region Public Methods

        public static void ClearCache()
        {
            _Friends = null;
            _FriendRequests = null;
            ViewLevel = Views.Friends;
        }

        #endregion

    }

    /// <summary>
    /// Extends TraktUserProfile with properties we can
    /// use to notify the facade for loading
    /// </summary>
    public class GUIFriendItem : TraktUserProfile, INotifyPropertyChanged
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

        public bool IsSearchItem { get; set; }
        public bool IsFriend { get; set; }

        public object Item
        {
            get { return _Item; }
            set
            {
                _Item = value;
                INotifyPropertyChanged notifier = value as INotifyPropertyChanged;
                if (notifier != null) notifier.PropertyChanged += (s, e) =>
                {
                    if (s is GUIFriendItem && e.PropertyName == "AvatarFilename")
                        SetImageToGui((s as GUIFriendItem).AvatarFilename);
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

            ThumbnailImage = imageFilePath;
            IconImage = imageFilePath;
            IconImageBig = imageFilePath;

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
}