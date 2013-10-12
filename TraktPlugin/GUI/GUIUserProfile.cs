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
    public class GUIUserProfile : GUIWindow
    {
        #region Skin Controls

        [SkinControl(50)]
        protected GUIFacadeControl Facade = null;

        [SkinControl(4)]
        protected GUIButtonControl refreshButton = null;
        
        #endregion

        #region Enums
        
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

        public GUIUserProfile() { }

        #endregion

        #region Private Properties

        static DateTime LastRequest = new DateTime();
        ActivityType SelectedActivity { get; set; }
        static int PreviousActivityTypeSelectedIndex = 0;
        static Dictionary<string, TraktUserProfile> UserProfiles = new Dictionary<string, TraktUserProfile>();

        static TraktUserProfile UserProfile
        {
            get
            {
                if (!UserProfiles.Keys.Contains(CurrentUser) || LastRequest < DateTime.UtcNow.Subtract(new TimeSpan(0, TraktSettings.WebRequestCacheMinutes, 0)))
                {
                    _UserProfile = TraktAPI.TraktAPI.GetUserProfile(CurrentUser);
                    if (UserProfiles.Keys.Contains(CurrentUser)) UserProfiles.Remove(CurrentUser);
                    GetUserProfileImage(_UserProfile);
                    UserProfiles.Add(CurrentUser, _UserProfile);
                    LastRequest = DateTime.UtcNow;
                    PreviousActivityTypeSelectedIndex = 0;
                }
                return UserProfiles[CurrentUser];
            }
        }
        static TraktUserProfile _UserProfile = null;

        #endregion

        #region Base Overrides

        public override int GetID
        {
            get
            {
                return (int)TraktGUIWindows.UserProfile;
            }
        }

        public override bool Init()
        {
            return Load(GUIGraphicsContext.Skin + @"\Trakt.UserProfile.xml");
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

            // Load User Profile
            LoadUserProfile();
        }

        protected override void OnPageDestroy(int new_windowId)
        {
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
                        // Launch Corresponding Activity window
                        switch (SelectedActivity)
                        {
                            case (ActivityType.RecentWatchedMovies):
                                GUIRecentWatchedMovies.CurrentUser = CurrentUser;
                                GUIWindowManager.ActivateWindow((int)TraktGUIWindows.RecentWatchedMovies);
                                break;

                            case (ActivityType.RecentWatchedEpisodes):
                                GUIRecentWatchedEpisodes.CurrentUser = CurrentUser;
                                GUIWindowManager.ActivateWindow((int)TraktGUIWindows.RecentWatchedEpisodes);
                                break;

                            case (ActivityType.RecentAddedEpisodes):
                                GUIRecentAddedEpisodes.CurrentUser = CurrentUser;
                                GUIWindowManager.ActivateWindow((int)TraktGUIWindows.RecentAddedEpisodes);
                                break;

                            case (ActivityType.RecentAddedMovies):
                                GUIRecentAddedMovies.CurrentUser = CurrentUser;
                                GUIWindowManager.ActivateWindow((int)TraktGUIWindows.RecentAddedMovies);
                                break;

                            case (ActivityType.RecentShouts):
                                GUIRecentShouts.CurrentUser = CurrentUser;
                                GUIWindowManager.ActivateWindow((int)TraktGUIWindows.RecentShouts);
                                break;

                            case (ActivityType.MovieWatchList):
                                GUIWatchListMovies.CurrentUser = CurrentUser;
                                GUIWindowManager.ActivateWindow((int)TraktGUIWindows.WatchedListMovies);
                                break;

                            case (ActivityType.ShowWatchList):
                                GUIWatchListShows.CurrentUser = CurrentUser;
                                GUIWindowManager.ActivateWindow((int)TraktGUIWindows.WatchedListShows);
                                break;

                            case (ActivityType.EpisodeWatchList):
                                GUIWatchListEpisodes.CurrentUser = CurrentUser;
                                GUIWindowManager.ActivateWindow((int)TraktGUIWindows.WatchedListEpisodes);
                                break;

                            case (ActivityType.Lists):
                                GUILists.CurrentUser = CurrentUser;
                                GUIWindowManager.ActivateWindow((int)TraktGUIWindows.Lists);
                                break;
                        }
                    }
                    break;

                case (4):
                    GUIControl.FocusControl(GetID, Facade.GetID);
                    _UserProfile = null;
                    LoadUserProfile();
                    break;
            }
            base.OnClicked(controlId, control, actionType);
        }

        public override void OnAction(Action action)
        {
            switch (action.wID)
            {
                case Action.ActionType.ACTION_PREVIOUS_MENU:
                    // restore current user
                    CurrentUser = TraktSettings.Username;
                    base.OnAction(action);
                    break;

                default:
                    base.OnAction(action);
                    break;
            }
        }

        protected override void OnShowContextMenu()
        {
            base.OnShowContextMenu();
        }

        #endregion

        #region Private Methods

        private void InitProperties()
        {
            // load profile for user
            if (string.IsNullOrEmpty(CurrentUser)) CurrentUser = TraktSettings.Username;
            // this property will be the same as the standard username property
            // with one exception, it will get set immediately upon page load
            GUICommon.SetProperty("#Trakt.UserProfile.CurrentUser", CurrentUser);
        }

        private void LoadUserProfile()
        {
            GUIBackgroundTask.Instance.ExecuteInBackgroundAndCallback(() =>
            {
                return UserProfile;
            },
            delegate(bool success, object result)
            {
                if (success)
                {
                    // Get UserProfile from Result Handler
                    TraktUserProfile userProfile = result as TraktUserProfile;
                    
                    // Publish User Profile Properties
                    PublishSkinProperties(userProfile);

                    // Load Activity Facade
                    LoadActivityTypes();
                }
            }, Translation.GettingUserProfile, true);
        }

        private void LoadActivityTypes()
        {
            // clear facade
            GUIControl.ClearControl(GetID, Facade.GetID);

            string avatar = UserProfile.Avatar.LocalImageFilename(ArtworkType.Avatar);

            // add each type to the list           
            var item = new GUIUserListItem(Translation.RecentWatchedEpisodes, (int)TraktGUIWindows.Network);
            item.IconImage = avatar;
            item.IconImageBig = avatar;
            item.ThumbnailImage = avatar;
            item.PinImage = "traktActivityWatched.png";
            item.OnItemSelected += OnActivityTypeSelected;
            Utils.SetDefaultIcons(item);
            Facade.Add(item);

            item = new GUIUserListItem(Translation.RecentWatchedMovies, (int)TraktGUIWindows.Network);
            item.IconImage = avatar;
            item.IconImageBig = avatar;
            item.ThumbnailImage = avatar;
            item.PinImage = "traktActivityWatched.png";
            item.OnItemSelected += OnActivityTypeSelected;
            Utils.SetDefaultIcons(item);
            Facade.Add(item);

            item = new GUIUserListItem(Translation.RecentAddedEpisodes, (int)TraktGUIWindows.Network);
            item.IconImage = avatar;
            item.IconImageBig = avatar;
            item.ThumbnailImage = avatar;
            item.PinImage = "traktActivityCollected.png";
            item.OnItemSelected += OnActivityTypeSelected;
            Utils.SetDefaultIcons(item);
            Facade.Add(item);

            item = new GUIUserListItem(Translation.RecentAddedMovies, (int)TraktGUIWindows.Network);
            item.IconImage = avatar;
            item.IconImageBig = avatar;
            item.ThumbnailImage = avatar;
            item.PinImage = "traktActivityCollected.png";
            item.OnItemSelected += OnActivityTypeSelected;
            Utils.SetDefaultIcons(item);
            Facade.Add(item);

            item = new GUIUserListItem(Translation.RecentShouts, (int)TraktGUIWindows.Network);
            item.IconImage = avatar;
            item.IconImageBig = avatar;
            item.ThumbnailImage = avatar;
            item.PinImage = "traktActivityShout.png";
            item.OnItemSelected += OnActivityTypeSelected;
            Utils.SetDefaultIcons(item);
            Facade.Add(item);

            item = new GUIUserListItem(Translation.Lists, (int)TraktGUIWindows.Network);
            item.IconImage = avatar;
            item.IconImageBig = avatar;
            item.ThumbnailImage = avatar;
            item.PinImage = "traktActivityList.png";
            item.OnItemSelected += OnActivityTypeSelected;
            Utils.SetDefaultIcons(item);
            Facade.Add(item);

            item = new GUIUserListItem(Translation.WatchListShows, (int)TraktGUIWindows.Network);
            item.IconImage = avatar;
            item.IconImageBig = avatar;
            item.ThumbnailImage = avatar;
            item.PinImage = "traktActivityWatchlist.png";
            item.OnItemSelected += OnActivityTypeSelected;
            Utils.SetDefaultIcons(item);
            Facade.Add(item);

            item = new GUIUserListItem(Translation.WatchListMovies, (int)TraktGUIWindows.Network);
            item.IconImage = avatar;
            item.IconImageBig = avatar;
            item.ThumbnailImage = avatar;
            item.PinImage = "traktActivityWatchlist.png";
            item.OnItemSelected += OnActivityTypeSelected;
            Utils.SetDefaultIcons(item);
            Facade.Add(item);

            item = new GUIUserListItem(Translation.WatchListEpisodes, (int)TraktGUIWindows.Network);
            item.IconImage = avatar;
            item.IconImageBig = avatar;
            item.ThumbnailImage = avatar;
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

        static void GetUserProfileImage(TraktUserProfile userProfile)
        {
            string url = userProfile.Avatar;
            string localFile = userProfile.Avatar.LocalImageFilename(ArtworkType.Avatar);

            GUIImageHandler.DownloadImage(url, localFile);
        }

        private void ClearProperties()
        {
            GUICommon.ClearUserProperties();
            GUICommon.ClearStatisticProperties();
        }
        
        private void PublishSkinProperties(TraktUserProfile userProfile)
        {
            if (userProfile == null) return;

            // Publish User Properties
            GUICommon.SetUserProperties(userProfile);

            // Publish Statistics
            GUICommon.SetStatisticProperties(userProfile.Stats);
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

            PreviousActivityTypeSelectedIndex = Facade.SelectedListItemIndex;
        }

        #endregion

        #region Public Static Properties

        public static string CurrentUser { get; set; }

        #endregion
    }
}