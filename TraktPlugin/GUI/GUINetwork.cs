using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
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
    public class GUINetwork : GUIWindow
    {
        #region Skin Controls

        [SkinControl(50)]
        protected GUIFacadeControl Facade = null;

        [SkinControl(2)]
        protected GUIButtonControl viewButton = null;

        [SkinControl(3)]
        protected GUIButtonControl searchButton = null;

        [SkinControl(4)]
        protected GUIButtonControl refreshButton = null;

        #endregion

        #region Enums

        enum View
        {
            Friends,
            Following,
            Followers,
            Requests
        }

        enum ViewLevel
        {
            Network,
            ActivityTypes
        }

        enum ContextMenuItem
        {
            SearchUser,
            UnFollowUser,
            FollowUser,
            Approve,
            ApproveAndFollow,
            Deny,
            ChangeView
        }

        enum ActivityType
        {
            RecentWatchedEpisodes,
            RecentWatchedMovies,
            RecentAddedEpisodes,
            RecentAddedMovies,
            RecentShouts,
            EpisodeWatchList,
            RatedMovies,
            RatedShows,
            RatedEpisodes,
            ShowWatchList,
            MovieWatchList,
            Lists
        }

        #endregion

        #region Constructor

        public GUINetwork() {}

        #endregion

        #region Private Properties

        static DateTime LastRequest = new DateTime();
        static ViewLevel CurrentViewLevel { get; set; }
        View CurrentView { get; set; }
        ActivityType SelectedActivity { get; set; }
        TraktUser CurrentSelectedUser { get; set; }
        int PreviousUserSelectedIndex = 0;
        int PreviousActivityTypeSelectedIndex = 0;
        bool StopDownload = false;

        IEnumerable<TraktNetworkUser> TraktFriends
        {
            get
            {
                if (_TraktFriends == null || LastRequest < DateTime.UtcNow.Subtract(new TimeSpan(0, TraktSettings.WebRequestCacheMinutes, 0)))
                {
                    _TraktFriends = TraktAPI.TraktAPI.GetNetworkFriends();
                    LastRequest = DateTime.UtcNow;
                    PreviousUserSelectedIndex = 0;
                }
                return _TraktFriends;
            }
        }
        static IEnumerable<TraktNetworkUser> _TraktFriends = null;

        IEnumerable<TraktNetworkUser> TraktFollowers
        {
            get
            {
                if (_TraktFollowers == null || LastRequest < DateTime.UtcNow.Subtract(new TimeSpan(0, TraktSettings.WebRequestCacheMinutes, 0)))
                {
                    _TraktFollowers = TraktAPI.TraktAPI.GetNetworkFollowers();
                    LastRequest = DateTime.UtcNow;
                    PreviousUserSelectedIndex = 0;
                }
                return _TraktFollowers;
            }
        }
        static IEnumerable<TraktNetworkUser> _TraktFollowers = null;

        IEnumerable<TraktNetworkUser> TraktFollowing
        {
            get
            {
                if (_TraktFollowing == null || LastRequest < DateTime.UtcNow.Subtract(new TimeSpan(0, TraktSettings.WebRequestCacheMinutes, 0)))
                {
                    _TraktFollowing = TraktAPI.TraktAPI.GetNetworkFollowing();
                    LastRequest = DateTime.UtcNow;
                    PreviousUserSelectedIndex = 0;
                }
                return _TraktFollowing;
            }
        }
        static IEnumerable<TraktNetworkUser> _TraktFollowing = null;

        #endregion

        #region Base Overrides

        public override int GetID
        {
            get
            {
                return (int)TraktGUIWindows.Network;
            }
        }

        public override bool Init()
        {
            return Load(GUIGraphicsContext.Skin + @"\Trakt.Network.xml");
        }

        protected override void OnPageLoad()
        {
            base.OnPageLoad();

            // Clear GUI properties
            ClearProperties();

            // Requires Login
            if (!GUICommon.CheckLogin()) return;

            // Initialize
            InitProperties();

            // Load Last View
            LoadView();
        }

        protected override void OnPageDestroy(int new_windowId)
        {
            StopDownload = true;
            ClearProperties();

            // remember settings
            TraktSettings.DefaultNetworkView = (int)CurrentView;

            base.OnPageDestroy(new_windowId);
        }

        protected override void OnClicked(int controlId, GUIControl control, Action.ActionType actionType)
        {
            // wait for any background action to finish
            if (GUIBackgroundTask.Instance.IsBusy) return;

            switch (controlId)
            {
                case (2):
                    GUIControl.FocusControl(GetID, Facade.GetID);
                    ShowViewMenu();
                    break;

                case (3):
                    GUIControl.FocusControl(GetID, Facade.GetID);
                    SearchForUser();
                    break;

                case (4):
                    GUIControl.FocusControl(GetID, Facade.GetID);
                    ClearCache();
                    LoadView();
                    break;

                case (50):
                    if (actionType == Action.ActionType.ACTION_SELECT_ITEM)
                    {
                        switch (CurrentViewLevel)
                        {
                            case ViewLevel.Network:
                                GUITraktUserListItem selectedUser = Facade.SelectedListItem as GUITraktUserListItem;
                                if (selectedUser.IsFolder)
                                {
                                    // return to previous view list
                                    LoadView();
                                }
                                else
                                {
                                    LoadActivityTypes();
                                }
                                break;

                            case ViewLevel.ActivityTypes:
                                // Launch Corresponding Activity window
                                switch (SelectedActivity)
                                {
                                    case (ActivityType.RecentWatchedMovies):
                                        GUIRecentWatchedMovies.CurrentUser = CurrentSelectedUser.Username;
                                        GUIWindowManager.ActivateWindow((int)TraktGUIWindows.RecentWatchedMovies);
                                        break;

                                    case (ActivityType.RecentWatchedEpisodes):
                                        GUIRecentWatchedEpisodes.CurrentUser = CurrentSelectedUser.Username;
                                        GUIWindowManager.ActivateWindow((int)TraktGUIWindows.RecentWatchedEpisodes);
                                        break;

                                    case (ActivityType.RecentAddedEpisodes):
                                        GUIRecentAddedEpisodes.CurrentUser = CurrentSelectedUser.Username;
                                        GUIWindowManager.ActivateWindow((int)TraktGUIWindows.RecentAddedEpisodes);
                                        break;

                                    case (ActivityType.RecentAddedMovies):
                                        GUIRecentAddedMovies.CurrentUser = CurrentSelectedUser.Username;
                                        GUIWindowManager.ActivateWindow((int)TraktGUIWindows.RecentAddedMovies);
                                        break;

                                    case (ActivityType.RecentShouts):
                                        GUIRecentShouts.CurrentUser = CurrentSelectedUser.Username;
                                        GUIWindowManager.ActivateWindow((int)TraktGUIWindows.RecentShouts);
                                        break;

                                    case (ActivityType.MovieWatchList):
                                        GUIWatchListMovies.CurrentUser = CurrentSelectedUser.Username;
                                        GUIWindowManager.ActivateWindow((int)TraktGUIWindows.WatchedListMovies);
                                        break;

                                    case (ActivityType.ShowWatchList):
                                        GUIWatchListShows.CurrentUser = CurrentSelectedUser.Username;
                                        GUIWindowManager.ActivateWindow((int)TraktGUIWindows.WatchedListShows);
                                        break;

                                    case (ActivityType.EpisodeWatchList):
                                        GUIWatchListEpisodes.CurrentUser = CurrentSelectedUser.Username;
                                        GUIWindowManager.ActivateWindow((int)TraktGUIWindows.WatchedListEpisodes);
                                        break;

                                    case (ActivityType.Lists):
                                        GUILists.CurrentUser = CurrentSelectedUser.Username;
                                        GUIWindowManager.ActivateWindow((int)TraktGUIWindows.Lists);
                                        break;
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
                    switch (CurrentViewLevel)
                    {
                        case ViewLevel.ActivityTypes:
                            // go back one view level
                            CurrentViewLevel = ViewLevel.Network;
                            LoadView();
                            return;
                    }
                    break;
            }
            base.OnAction(action);
        }

        protected override void OnShowContextMenu()
        {
            GUITraktUserListItem selectedItem = this.Facade.SelectedListItem as GUITraktUserListItem;
            if (selectedItem == null) return;

            IDialogbox dlg = (IDialogbox)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
            if (dlg == null) return;

            dlg.Reset();
            dlg.SetHeading(Translation.Network);

            GUIListItem listItem = null;
            int itemCount = 0;

            // search for new people to follow
            if (CurrentViewLevel == ViewLevel.Network)
            {
                listItem = new GUIListItem(Translation.SearchForUser);
                dlg.Add(listItem);
                listItem.ItemId = (int)ContextMenuItem.SearchUser;
                itemCount++;
            }

            // follow user found in search or from followers list (i.e. become friend)
            if (CurrentViewLevel == ViewLevel.Network && (selectedItem.IsSearchItem || selectedItem.IsFollower))
            {
                listItem = new GUIListItem(Translation.Follow);
                dlg.Add(listItem);
                listItem.ItemId = (int)ContextMenuItem.FollowUser;
                itemCount++;
            }

            // approve follow request
            if (CurrentViewLevel == ViewLevel.Network && selectedItem.IsFollowerRequest)
            {
                listItem = new GUIListItem(Translation.Approve);
                dlg.Add(listItem);
                listItem.ItemId = (int)ContextMenuItem.Approve;
                itemCount++;
            }

            // approve and followback request
            if (CurrentViewLevel == ViewLevel.Network && selectedItem.IsFollowerRequest)
            {
                listItem = new GUIListItem(Translation.ApproveAndFollowBack);
                dlg.Add(listItem);
                listItem.ItemId = (int)ContextMenuItem.ApproveAndFollow;
                itemCount++;
            }

            // deny follow request
            if (CurrentViewLevel == ViewLevel.Network && selectedItem.IsFollowerRequest)
            {
                listItem = new GUIListItem(Translation.Deny);
                dlg.Add(listItem);
                listItem.ItemId = (int)ContextMenuItem.Deny;
                itemCount++;
            }

            // unfollow user
            if (CurrentViewLevel == ViewLevel.Network && (selectedItem.IsFriend || selectedItem.IsFollowed))
            {
                listItem = new GUIListItem(Translation.UnFollow);
                dlg.Add(listItem);
                listItem.ItemId = (int)ContextMenuItem.UnFollowUser;
                itemCount++;
            }

            // change view
            if (CurrentViewLevel == ViewLevel.Network)
            {
                listItem = new GUIListItem(Translation.ChangeView);
                dlg.Add(listItem);
                listItem.ItemId = (int)ContextMenuItem.ChangeView;
                itemCount++;
            }

            // don't display menu if no context menu items to display
            if (itemCount == 0) return;

            // Show Context Menu
            dlg.DoModal(GUIWindowManager.ActiveWindow);
            if (dlg.SelectedId < 0) return;

            switch (dlg.SelectedId)
            {
                case ((int)ContextMenuItem.FollowUser):
                    if (GUIUtils.ShowYesNoDialog(Translation.Network, string.Format(Translation.SendFollowRequest, selectedItem.Label), true))
                    {
                        FollowUser(selectedItem.Item as TraktUser);
                        selectedItem.IsFollowed = true;
                        _TraktFollowing = null;
                        _TraktFriends = null;
                        LoadView();
                    }
                    break;

                case ((int)ContextMenuItem.SearchUser):
                    SearchForUser();
                    break;

                case ((int)ContextMenuItem.UnFollowUser):
                    if (GUIUtils.ShowYesNoDialog(Translation.UnFollow, string.Format(Translation.UnFollowMessage, selectedItem.Label)))
                    {
                        // Unfollow user
                        UnfollowUser(selectedItem.Item as TraktUser);

                        // Clear Cache - remove user from relavent lists
                        if (CurrentView == View.Following)
                            _TraktFollowing = _TraktFollowing.Except(_TraktFollowing.Where(f => f.Username == selectedItem.Label));
                        else if (CurrentView == View.Friends)
                            _TraktFriends = _TraktFriends.Except(_TraktFriends.Where(f => f.Username == selectedItem.Label));

                        // Re-Load list
                        LoadView();
                    }
                    break;

                case ((int)ContextMenuItem.Approve):
                    // pending follower request, get approval from user
                    if (GUIUtils.ShowYesNoDialog(Translation.FollowerRequest, string.Format(Translation.ApproveFollowerMessage, selectedItem.Label), true))
                    {
                        // follower approved, send to trakt
                        ApproveFollowerRequest(selectedItem.Item as TraktUser);

                        // remove follower from requests, we have approved already
                        _TraktFollowerRequests = _TraktFollowerRequests.Except(_TraktFollowerRequests.Where(f => f.Username == selectedItem.Label));

                        // clear followers cache
                        _TraktFollowers = null;

                        // Re-Load list
                        LoadView();
                    }
                    break;

                case ((int)ContextMenuItem.ApproveAndFollow):
                    // pending follower request, get approval from user
                    if (GUIUtils.ShowYesNoDialog(Translation.FollowerRequest, string.Format(Translation.ApproveFollowerAndFollowBackMessage, selectedItem.Label), true))
                    {
                        // follower approved, send to trakt with follow back 
                        ApproveFollowerRequest(selectedItem.Item as TraktUser, true);

                        // remove follower from requests, we have approved already
                        _TraktFollowerRequests = _TraktFollowerRequests.Except(_TraktFollowerRequests.Where(f => f.Username == selectedItem.Label));

                        // clear respective caches
                        _TraktFollowers = null;
                        _TraktFollowing = null;
                        _TraktFriends = null;

                        // Re-Load list
                        LoadView();
                    }
                    break;

                case ((int)ContextMenuItem.Deny):
                    if (GUIUtils.ShowYesNoDialog(Translation.FollowerRequest, string.Format(Translation.DenyFollowRequest, selectedItem.Label), true))
                    {
                        // follower denied, remove user from pending requests
                        DenyFollowerRequest(selectedItem.Item as TraktUser);

                        _TraktFollowerRequests = _TraktFollowerRequests.Except(_TraktFollowerRequests.Where(f => f.Username == selectedItem.Label));

                        // Re-Load list
                        LoadView();
                    }
                    break;

                case ((int)ContextMenuItem.ChangeView):
                    ShowViewMenu();
                    break;

                default:
                    break;
            }

            base.OnShowContextMenu();
        }

        #endregion

        #region Private Methods

        private void InitProperties()
        {
            // Set Network view as default if cache has expired
            if (CurrentSelectedUser == null || LastRequest < DateTime.UtcNow.Subtract(new TimeSpan(0, TraktSettings.WebRequestCacheMinutes, 0)))
                CurrentViewLevel = ViewLevel.Network;

            CurrentView = (View)TraktSettings.DefaultNetworkView;

            GUIUtils.SetProperty("#Trakt.Selected.Type", string.Empty);
            GUIUtils.SetProperty("#Trakt.Items", string.Empty);
            SetCurrentView();
        }

        private void SetCurrentView()
        {
            GUIUtils.SetProperty("#Trakt.View.Level", CurrentViewLevel.ToString());

            if (CurrentView == View.Friends)
                GUICommon.SetProperty("#Trakt.CurrentView", Translation.Friends);
            else if (CurrentView == View.Followers)
                GUICommon.SetProperty("#Trakt.CurrentView", Translation.Followers);
            else if (CurrentView == View.Following)
                GUICommon.SetProperty("#Trakt.CurrentView", Translation.Following);
            else if (CurrentView == View.Requests)
                GUICommon.SetProperty("#Trakt.CurrentView", Translation.FollowerRequests);

            if (viewButton != null)
                viewButton.Label = Translation.View + ": " + GetViewTypeName(CurrentView);
        }

        private TraktNetwork CreateNetworkData(TraktUser user)
        {
            TraktNetwork network = new TraktNetwork
            {
                Username = TraktSettings.Username,
                Password = TraktSettings.Password,
                User = user.Username
            };
            return network;
        }

        private TraktNetworkApprove CreateNetworkData(TraktUser user, bool followBack)
        {
            TraktNetworkApprove network = new TraktNetworkApprove
            {
                Username = TraktSettings.Username,
                Password = TraktSettings.Password,
                User = user.Username,
                FollowBack = followBack
            };
            return network;
        }

        private void SearchForUser()
        {
            string userSearchTerm = string.Empty;
            if (GUIUtils.GetStringFromKeyboard(ref userSearchTerm))
            {
                LoadSearchResults(userSearchTerm);
            }
        }

        private string GetViewTypeName(View viewType)
        {
            switch (viewType)
            {
                case View.Followers:
                    return Translation.Followers;

                case View.Following:
                    return Translation.Following;

                case View.Friends:
                    return Translation.Friends;

                case View.Requests:
                    return Translation.Requests;

                default:
                    return Translation.Following;
            }
        }

        private void ShowViewMenu()
        {
            IDialogbox dlg = (IDialogbox)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
            if (dlg == null) return;

            dlg.Reset();
            dlg.SetHeading(Translation.View);

            foreach (int value in Enum.GetValues(typeof(View)))
            {
                View type = (View)Enum.Parse(typeof(View), value.ToString());
                string label = GetViewTypeName(type);

                // Create new item
                GUIListItem listItem = new GUIListItem(label);
                listItem.ItemId = value;

                // Set selected if current
                if (type == CurrentView) listItem.Selected = true;

                // Add new item to context menu
                dlg.Add(listItem);
            }

            dlg.DoModal(GUIWindowManager.ActiveWindow);
            if (dlg.SelectedId <= 0) return;

            // Set new Selection            
            CurrentView = (View)Enum.GetValues(typeof(View)).GetValue(dlg.SelectedLabel);
            SetCurrentView();

            // Reset Views and Apply
            PreviousUserSelectedIndex = 0;
            PreviousActivityTypeSelectedIndex = 0;

            // reset view level re-load view
            CurrentViewLevel = ViewLevel.Network;
            LoadView();
        }

        private void FollowUser(TraktUser user)
        {
            Thread addFriendThread = new Thread(delegate(object obj)
            {
                var currUser = obj as TraktUser;

                TraktNetworkFollowResponse response = TraktAPI.TraktAPI.NetworkFollow(CreateNetworkData(currUser));
                TraktAPI.TraktAPI.LogTraktResponse<TraktNetworkFollowResponse>(response);

                // notify user if follow is pending approval by user
                if (response.Pending)
                {
                    GUIUtils.ShowNotifyDialog(Translation.Follow, string.Format(Translation.FollowPendingApproval, currUser.Username));
                }
            })
            {
                IsBackground = true,
                Name = "FollowUser"
            };

            addFriendThread.Start(user);
        }

        private void ApproveFollowerRequest(TraktUser user, bool followBack = false)
        {
            Thread approveFollowerThread = new Thread(delegate(object obj)
            {
                TraktResponse response = TraktAPI.TraktAPI.NetworkApprove(CreateNetworkData(user, followBack));
                TraktAPI.TraktAPI.LogTraktResponse<TraktResponse>(response);
            })
            {
                IsBackground = true,
                Name = "FollowerRequest"
            };

            approveFollowerThread.Start(user);
        }

        private void DenyFollowerRequest(TraktUser user)
        {
            Thread denyFollowerRequest = new Thread(delegate(object obj)
            {
                TraktResponse response = TraktAPI.TraktAPI.NetworkDeny(CreateNetworkData(user));
                TraktAPI.TraktAPI.LogTraktResponse<TraktResponse>(response);
            })
            {
                IsBackground = true,
                Name = "DenyFollowerRequest"
            };

            denyFollowerRequest.Start(user);
        }

        private void UnfollowUser(TraktUser user)
        {
            Thread unfollowUserThread = new Thread(delegate(object obj)
            {
                TraktResponse response = TraktAPI.TraktAPI.NetworkUnFollow(CreateNetworkData(user));
                TraktAPI.TraktAPI.LogTraktResponse<TraktResponse>(response);
            })
            {
                IsBackground = true,
                Name = "UnfollowUser"
            };

            unfollowUserThread.Start(user);
        }

        private void LoadView()
        {
            switch (CurrentViewLevel)
            {
                case ViewLevel.Network:
                    switch (CurrentView)
                    {
                        case View.Followers:
                            LoadFollowerList();
                            break;
                        case View.Following:
                            LoadFollowingList();
                            break;
                        case View.Friends:
                            LoadFriendList();
                            break;
                        case View.Requests:
                            LoadFollowerRequestList();
                            break;
                        default:
                            LoadFriendList();
                            break;
                    }
                    break;

                case ViewLevel.ActivityTypes:
                    LoadActivityTypes();
                    break;
            }

            SetCurrentView();
        }

        #region Load and Display Activity Types
        private void LoadActivityTypes()
        {
            if (CurrentSelectedUser == null) return;

            // signal that we are now displaying the users activity view
            CurrentViewLevel = ViewLevel.ActivityTypes;
            SetCurrentView();

            // clear facade
            GUIControl.ClearControl(GetID, Facade.GetID);

            // add each type to the list           
            GUITraktUserListItem item = new GUITraktUserListItem(Translation.RecentWatchedEpisodes);
            item.IconImage = CurrentSelectedUser.AvatarFilename;
            item.IconImageBig = CurrentSelectedUser.AvatarFilename;
            item.ThumbnailImage = CurrentSelectedUser.AvatarFilename;
            item.PinImage = "traktActivityWatched.png";
            item.OnItemSelected += OnActivityTypeSelected;
            Utils.SetDefaultIcons(item);
            Facade.Add(item);

            item = new GUITraktUserListItem(Translation.RecentWatchedMovies);
            item.IconImage = CurrentSelectedUser.AvatarFilename;
            item.IconImageBig = CurrentSelectedUser.AvatarFilename;
            item.ThumbnailImage = CurrentSelectedUser.AvatarFilename;
            item.PinImage = "traktActivityWatched.png";
            item.OnItemSelected += OnActivityTypeSelected;
            Utils.SetDefaultIcons(item);
            Facade.Add(item);

            item = new GUITraktUserListItem(Translation.RecentAddedEpisodes);
            item.IconImage = CurrentSelectedUser.AvatarFilename;
            item.IconImageBig = CurrentSelectedUser.AvatarFilename;
            item.ThumbnailImage = CurrentSelectedUser.AvatarFilename;
            item.PinImage = "traktActivityCollected.png";
            item.OnItemSelected += OnActivityTypeSelected;
            Utils.SetDefaultIcons(item);
            Facade.Add(item);

            item = new GUITraktUserListItem(Translation.RecentAddedMovies);
            item.IconImage = CurrentSelectedUser.AvatarFilename;
            item.IconImageBig = CurrentSelectedUser.AvatarFilename;
            item.ThumbnailImage = CurrentSelectedUser.AvatarFilename;
            item.PinImage = "traktActivityCollected.png";
            item.OnItemSelected += OnActivityTypeSelected;
            Utils.SetDefaultIcons(item);
            Facade.Add(item);

            item = new GUITraktUserListItem(Translation.RecentShouts);
            item.IconImage = CurrentSelectedUser.AvatarFilename;
            item.IconImageBig = CurrentSelectedUser.AvatarFilename;
            item.ThumbnailImage = CurrentSelectedUser.AvatarFilename;
            item.PinImage = "traktActivityShout.png";
            item.OnItemSelected += OnActivityTypeSelected;
            Utils.SetDefaultIcons(item);
            Facade.Add(item);

            item = new GUITraktUserListItem(Translation.Lists);
            item.IconImage = CurrentSelectedUser.AvatarFilename;
            item.IconImageBig = CurrentSelectedUser.AvatarFilename;
            item.ThumbnailImage = CurrentSelectedUser.AvatarFilename;
            item.PinImage = "traktActivityList.png";
            item.OnItemSelected += OnActivityTypeSelected;
            Utils.SetDefaultIcons(item);
            Facade.Add(item);

            item = new GUITraktUserListItem(Translation.WatchListShows);
            item.IconImage = CurrentSelectedUser.AvatarFilename;
            item.IconImageBig = CurrentSelectedUser.AvatarFilename;
            item.ThumbnailImage = CurrentSelectedUser.AvatarFilename;
            item.PinImage = "traktActivityWatchlist.png";
            item.OnItemSelected += OnActivityTypeSelected;
            Utils.SetDefaultIcons(item);
            Facade.Add(item);

            item = new GUITraktUserListItem(Translation.WatchListMovies);
            item.IconImage = CurrentSelectedUser.AvatarFilename;
            item.IconImageBig = CurrentSelectedUser.AvatarFilename;
            item.ThumbnailImage = CurrentSelectedUser.AvatarFilename;
            item.PinImage = "traktActivityWatchlist.png";
            item.OnItemSelected += OnActivityTypeSelected;
            Utils.SetDefaultIcons(item);
            Facade.Add(item);

            item = new GUITraktUserListItem(Translation.WatchListEpisodes);
            item.IconImage = CurrentSelectedUser.AvatarFilename;
            item.IconImageBig = CurrentSelectedUser.AvatarFilename;
            item.ThumbnailImage = CurrentSelectedUser.AvatarFilename;
            item.PinImage = "traktActivityWatchlist.png";
            item.OnItemSelected += OnActivityTypeSelected;
            Utils.SetDefaultIcons(item);
            Facade.Add(item);

            Facade.SelectedListItemIndex = PreviousActivityTypeSelectedIndex;

            // Set Facade Layout
            Facade.SetCurrentLayout("List");
            GUIControl.FocusControl(GetID, Facade.GetID);

            GUIUtils.SetProperty("#itemcount", Facade.Count.ToString());
            GUIUtils.SetProperty("#Trakt.Items", string.Format("{0} {1}", Facade.Count.ToString(), GUILocalizeStrings.Get(507)));
        }
        #endregion

        #region Load and Display Search Results
        private void LoadSearchResults(string searchTerm)
        {
            GUIBackgroundTask.Instance.ExecuteInBackgroundAndCallback(() =>
            {
                return TraktAPI.TraktAPI.SearchForUsers(searchTerm);
            },
            delegate(bool success, object result)
            {
                if (success)
                {
                    SendSearchResultsToFacade(result as IEnumerable<TraktUser>);
                }
            }, Translation.GettingSearchResults, true);
        }

        private void SendSearchResultsToFacade(IEnumerable<TraktUser> searchResults)
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
            userItem.OnItemSelected += OnUserSelected;
            Facade.Add(userItem);

            foreach (var user in searchResults)
            {
                // don't need to find oneself
                if (user.Username == TraktSettings.Username) continue;

                userItem = new GUITraktUserListItem(user.Username);

                userItem.Label2 = user.JoinDate.FromEpoch().ToShortDateString();
                userItem.Item = user;
                userItem.ItemId = id++;
                userItem.IsSearchItem = true;
                userItem.IconImage = "defaultTraktUser.png";
                userItem.IconImageBig = "defaultTraktUserBig.png";
                userItem.ThumbnailImage = "defaultTraktUserBig.png";
                userItem.OnItemSelected += OnUserSelected;
                Facade.Add(userItem);
            }

            // select first item
            Facade.SelectedListItemIndex = 1;

            // Set Facade Layout
            Facade.SetCurrentLayout("List");
            GUIControl.FocusControl(GetID, Facade.GetID);

            // Download avatars Async and set to facade
            List<TraktUser> images = new List<TraktUser>(searchResults.ToList());
            GetImages<TraktUser>(images);
        }
        #endregion

        #region Load and Display Friend List
        private void LoadFriendList()
        {
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
                    IEnumerable<TraktNetworkUser> friends = result as IEnumerable<TraktNetworkUser>;
                    SendFriendsToFacade(friends);
                }
            }, Translation.GettingFriendsList, true);
        }

        private void SendFriendsToFacade(IEnumerable<TraktNetworkUser> friends)
        {
            // clear facade
            GUIControl.ClearControl(GetID, Facade.GetID);
            ClearProperties();

            int friendCount = friends.Count();

            GUIUtils.SetProperty("#itemcount", friendCount.ToString());
            GUIUtils.SetProperty("#Trakt.Items", string.Format("{0} {1}", friendCount.ToString(), friendCount > 1 ? Translation.Friends : Translation.Friend));

            if (friendCount == 0)
            {
                GUIUtils.ShowNotifyDialog(GUIUtils.PluginName(), Translation.NoFriendsTaunt);
                if (viewButton != null)
                {
                    // let user select another view since there is nothing to show here
                    GUIControl.FocusControl(GetID, viewButton.GetID);
                }
                else
                {
                    GUIWindowManager.ShowPreviousWindow();
                }
                return;
            }

            int id = 0;

            // Add each friend to the list
            foreach (var friend in friends)
            {
                GUITraktUserListItem userItem = new GUITraktUserListItem(friend.Username);

                userItem.Label2 = friend.ApprovedDate.FromEpoch().ToShortDateString();
                userItem.Item = friend;
                userItem.ItemId = id++;
                userItem.IsFriend = true;
                userItem.IconImage = "defaultTraktUser.png";
                userItem.IconImageBig = "defaultTraktUserBig.png";
                userItem.ThumbnailImage = "defaultTraktUserBig.png";
                userItem.OnItemSelected += OnUserSelected;
                Utils.SetDefaultIcons(userItem);
                Facade.Add(userItem);
            }

            // restore previous selection
            if (Facade.Count <= PreviousUserSelectedIndex)
                Facade.SelectedListItemIndex = 0;
            else
                Facade.SelectedListItemIndex = PreviousUserSelectedIndex;

            // Set Facade Layout
            Facade.SetCurrentLayout("List");
            GUIControl.FocusControl(GetID, Facade.GetID);

            // Download avatars Async and set to facade
            List<TraktNetworkUser> images = new List<TraktNetworkUser>(friends.ToList());
            GetImages<TraktNetworkUser>(images);
        }
        #endregion

        #region Load and Display Following List
        private void LoadFollowingList()
        {
            GUIUtils.SetProperty("#Trakt.Items", string.Empty);

            GUIBackgroundTask.Instance.ExecuteInBackgroundAndCallback(() =>
            {
                return TraktFollowing;
            },
            delegate(bool success, object result)
            {
                if (success)
                {
                    // Get Following List from Result Handler
                    IEnumerable<TraktNetworkUser> following = result as IEnumerable<TraktNetworkUser>;
                    SendFollowingToFacade(following);
                }
            }, Translation.GettingFollowingList, true);
        }

        private void SendFollowingToFacade(IEnumerable<TraktNetworkUser> following)
        {
            // clear facade
            GUIControl.ClearControl(GetID, Facade.GetID);
            ClearProperties();

            int followingCount = following.Count();

            GUIUtils.SetProperty("#itemcount", followingCount.ToString());
            GUIUtils.SetProperty("#Trakt.Items", string.Format("{0} {1}", followingCount.ToString(), Translation.Followed));

            if (followingCount == 0)
            {
                GUIUtils.ShowNotifyDialog(GUIUtils.PluginName(), Translation.NoFollowingTaunt);
                if (viewButton != null)
                {
                    // let user select another view since there is nothing to show here
                    GUIControl.FocusControl(GetID, viewButton.GetID);
                }
                else
                {
                    GUIWindowManager.ShowPreviousWindow();
                }
                return;
            }

            int id = 0;

            // Add each user to the list
            foreach (var user in following)
            {
                GUITraktUserListItem userItem = new GUITraktUserListItem(user.Username);

                userItem.Label2 = user.ApprovedDate.FromEpoch().ToShortDateString();
                userItem.Item = user;
                userItem.ItemId = id++;
                userItem.IsFollowed = true;
                userItem.IconImage = "defaultTraktUser.png";
                userItem.IconImageBig = "defaultTraktUserBig.png";
                userItem.ThumbnailImage = "defaultTraktUserBig.png";
                userItem.OnItemSelected += OnUserSelected;
                Utils.SetDefaultIcons(userItem);
                Facade.Add(userItem);
            }

            // restore previous selection
            if (Facade.Count <= PreviousUserSelectedIndex)
                Facade.SelectedListItemIndex = 0;
            else
                Facade.SelectedListItemIndex = PreviousUserSelectedIndex;

            // Set Facade Layout
            Facade.SetCurrentLayout("List");
            GUIControl.FocusControl(GetID, Facade.GetID);

            // Download avatars Async and set to facade
            List<TraktNetworkUser> images = new List<TraktNetworkUser>(following.ToList());
            GetImages<TraktNetworkUser>(images);
        }
        #endregion

        #region Load and Display Followers List
        private void LoadFollowerList()
        {
            GUIUtils.SetProperty("#Trakt.Items", string.Empty);

            GUIBackgroundTask.Instance.ExecuteInBackgroundAndCallback(() =>
            {
                return TraktFollowers;
            },
            delegate(bool success, object result)
            {
                if (success)
                {
                    // Get Followers List from Result Handler
                    IEnumerable<TraktNetworkUser> followers = result as IEnumerable<TraktNetworkUser>;
                    SendFollowerToFacade(followers);
                }
            }, Translation.GettingFollowerList, true);
        }

        private void SendFollowerToFacade(IEnumerable<TraktNetworkUser> followers)
        {
            // clear facade
            GUIControl.ClearControl(GetID, Facade.GetID);
            ClearProperties();

            int followerCount = followers.Count();

            GUIUtils.SetProperty("#itemcount", followerCount.ToString());
            GUIUtils.SetProperty("#Trakt.Items", string.Format("{0} {1}", followerCount.ToString(), followerCount > 1 ? Translation.Follower : Translation.Followers));

            if (followerCount == 0)
            {
                GUIUtils.ShowNotifyDialog(GUIUtils.PluginName(), Translation.NoFollowersTaunt);
                if (viewButton != null)
                {
                    // let user select another view since there is nothing to show here
                    GUIControl.FocusControl(GetID, viewButton.GetID);
                }
                else
                {
                    GUIWindowManager.ShowPreviousWindow();
                }
                return;
            }

            int id = 0;

            // Add each user to the list
            foreach (var user in followers)
            {
                GUITraktUserListItem userItem = new GUITraktUserListItem(user.Username);

                userItem.Label2 = user.ApprovedDate.FromEpoch().ToShortDateString();
                userItem.Item = user;
                userItem.ItemId = id++;
                userItem.IsFollower = true;
                userItem.IconImage = "defaultTraktUser.png";
                userItem.IconImageBig = "defaultTraktUserBig.png";
                userItem.ThumbnailImage = "defaultTraktUserBig.png";
                userItem.OnItemSelected += OnUserSelected;
                Utils.SetDefaultIcons(userItem);
                Facade.Add(userItem);
            }

            // restore previous selection
            if (Facade.Count <= PreviousUserSelectedIndex)
                Facade.SelectedListItemIndex = 0;
            else
                Facade.SelectedListItemIndex = PreviousUserSelectedIndex;

            // Set Facade Layout
            Facade.SetCurrentLayout("List");
            GUIControl.FocusControl(GetID, Facade.GetID);

            // Download avatars Async and set to facade
            List<TraktNetworkUser> images = new List<TraktNetworkUser>(followers.ToList());
            GetImages<TraktNetworkUser>(images);
        }
        #endregion

        #region Load and Display Follower Requests List
        private void LoadFollowerRequestList()
        {
            GUIUtils.SetProperty("#Trakt.Items", string.Empty);

            GUIBackgroundTask.Instance.ExecuteInBackgroundAndCallback(() =>
            {
                return TraktFollowerRequests;
            },
            delegate(bool success, object result)
            {
                if (success)
                {
                    // Get Request List from Result Handler
                    IEnumerable<TraktNetworkReqUser> requests = result as IEnumerable<TraktNetworkReqUser>;
                    SendFollowerRequestsToFacade(requests);
                }
            }, Translation.GettingFollowerRequests, true);
        }

        private void SendFollowerRequestsToFacade(IEnumerable<TraktNetworkReqUser> requests)
        {
            // clear facade
            GUIControl.ClearControl(GetID, Facade.GetID);
            ClearProperties();

            int followerReqCount = requests.Count();

            GUIUtils.SetProperty("#itemcount", followerReqCount.ToString());
            GUIUtils.SetProperty("#Trakt.Items", string.Format("{0} {1}", followerReqCount.ToString(), followerReqCount > 1 ? Translation.FollowerRequest : Translation.FollowerRequests));

            if (followerReqCount == 0)
            {
                GUIUtils.ShowNotifyDialog(GUIUtils.PluginName(), Translation.NoFollowerReqTaunt);
                if (viewButton != null)
                {
                    // let user select another view since there is nothing to show here
                    GUIControl.FocusControl(GetID, viewButton.GetID);
                }
                else
                {
                    GUIWindowManager.ShowPreviousWindow();
                }
                return;
            }

            int id = 0;

            // Add each user to the list
            foreach (var user in requests)
            {
                GUITraktUserListItem userItem = new GUITraktUserListItem(user.Username);

                userItem.Label2 = user.RequestDate.FromEpoch().ToShortDateString();
                userItem.Item = user;
                userItem.ItemId = id++;
                userItem.IsFollowerRequest = true;
                userItem.IconImage = "defaultTraktUser.png";
                userItem.IconImageBig = "defaultTraktUserBig.png";
                userItem.ThumbnailImage = "defaultTraktUserBig.png";
                userItem.OnItemSelected += OnUserSelected;
                Utils.SetDefaultIcons(userItem);
                Facade.Add(userItem);
            }

            // restore previous selection
            if (Facade.Count <= PreviousUserSelectedIndex)
                Facade.SelectedListItemIndex = 0;
            else
                Facade.SelectedListItemIndex = PreviousUserSelectedIndex;

            // Set Facade Layout
            Facade.SetCurrentLayout("List");
            GUIControl.FocusControl(GetID, Facade.GetID);

            // Download avatars Async and set to facade
            List<TraktNetworkReqUser> images = new List<TraktNetworkReqUser>(requests.ToList());
            GetImages<TraktNetworkReqUser>(images);
        }
        #endregion

        private void ClearProperties()
        {
            GUICommon.ClearUserProperties();
        }

        private void PublishUserSkinProperties(TraktUser user)
        {
            GUICommon.SetUserProperties(user);
        }

        private void OnUserSelected(GUIListItem item, GUIControl parent)
        {
            if (item.IsFolder)
            {
                // nothing to show except a back button
                ClearProperties();
                return;
            }

            CurrentSelectedUser = (item as GUITraktUserListItem).Item as TraktUser;
            PublishUserSkinProperties(CurrentSelectedUser);

            // reset selected indicies
            PreviousUserSelectedIndex = Facade.SelectedListItemIndex;
            PreviousActivityTypeSelectedIndex = 0;
        }

        private void OnActivityTypeSelected(GUIListItem item, GUIControl parent)
        {
            if (item.Label == Translation.RecentWatchedEpisodes)
                SelectedActivity = ActivityType.RecentWatchedEpisodes;
            else if (item.Label == Translation.RecentWatchedMovies)
                SelectedActivity = ActivityType.RecentWatchedMovies;
            else if (item.Label == Translation.RecentAddedEpisodes)
                SelectedActivity = ActivityType.RecentAddedEpisodes;
            else if (item.Label == Translation.RecentAddedMovies)
                SelectedActivity = ActivityType.RecentAddedMovies;
            else if (item.Label == Translation.RecentShouts)
                SelectedActivity = ActivityType.RecentShouts;
            else if (item.Label == Translation.WatchListMovies)
                SelectedActivity = ActivityType.MovieWatchList;
            else if (item.Label == Translation.WatchListShows)
                SelectedActivity = ActivityType.ShowWatchList;
            else if (item.Label == Translation.WatchListEpisodes)
                SelectedActivity = ActivityType.EpisodeWatchList;
            else if (item.Label == Translation.Lists)
                SelectedActivity = ActivityType.Lists;

            PublishUserSkinProperties(CurrentSelectedUser);
            PreviousActivityTypeSelectedIndex = Facade.SelectedListItemIndex;
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
                        
                        if (item is TraktUser)
                        {
                            remoteThumb = (item as TraktUser).Avatar;
                            localThumb = (item as TraktUser).AvatarFilename;
                        }

                        if (!string.IsNullOrEmpty(remoteThumb) && !string.IsNullOrEmpty(localThumb))
                        {
                            if (GUIImageHandler.DownloadImage(remoteThumb, localThumb))
                            {
                                // notify that image has been downloaded
                                if (item is TraktUser)
                                {
                                    (item as TraktUser).NotifyPropertyChanged("AvatarFilename");
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

        #endregion

        #region Public Static Properties

        public static IEnumerable<TraktNetworkReqUser> TraktFollowerRequests
        {
            get
            {
                if (_TraktFollowerRequests == null || LastRequest < DateTime.UtcNow.Subtract(new TimeSpan(0, TraktSettings.WebRequestCacheMinutes, 0)))
                {
                    _TraktFollowerRequests = TraktAPI.TraktAPI.GetNetworkRequests();
                    LastRequest = DateTime.UtcNow;
                }
                return _TraktFollowerRequests;
            }
        }
        static IEnumerable<TraktNetworkReqUser> _TraktFollowerRequests = null;

        #endregion

        #region Public Static Methods

        public static void ClearCache()
        {
            _TraktFriends = null;
            _TraktFollowers = null;
            _TraktFollowing = null;
            _TraktFollowerRequests = null;
            CurrentViewLevel = ViewLevel.Network;
        }

        #endregion
    }

    public class GUITraktUserListItem : GUIListItem
    {
        public GUITraktUserListItem(string strLabel) : base(strLabel) { }

        public bool IsSearchItem { get; set; }
        public bool IsFriend { get; set; }
        public bool IsFollower { get; set; }
        public bool IsFollowed { get; set; }
        public bool IsFollowerRequest { get; set; }

        public object Item
        {
            get { return _Item; }
            set
            {
                _Item = value;
                INotifyPropertyChanged notifier = value as INotifyPropertyChanged;
                if (notifier != null) notifier.PropertyChanged += (s, e) =>
                {
                    if (s is TraktUser && e.PropertyName == "AvatarFilename")
                        SetImageToGui((s as TraktUser).AvatarFilename);
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
            this.UpdateItemIfSelected((int)TraktGUIWindows.Network, ItemId);
        }
    }
}