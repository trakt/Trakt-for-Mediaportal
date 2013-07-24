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
        int PreviousActivityTypeSelectedIndex = 0;

        TraktUserProfile UserProfile
        {
            get
            {
                if (_UserProfile == null || LastRequest < DateTime.UtcNow.Subtract(new TimeSpan(0, TraktSettings.WebRequestCacheMinutes, 0)))
                {
                    _UserProfile = TraktAPI.TraktAPI.GetUserProfile(TraktSettings.Username);
                    LastRequest = DateTime.UtcNow;
                    PreviousActivityTypeSelectedIndex = 0;
                }
                return _UserProfile;
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
                                GUIRecentWatchedMovies.CurrentUser = TraktSettings.Username;
                                GUIWindowManager.ActivateWindow((int)TraktGUIWindows.RecentWatchedMovies);
                                break;

                            case (ActivityType.RecentWatchedEpisodes):
                                GUIRecentWatchedEpisodes.CurrentUser = TraktSettings.Username;
                                GUIWindowManager.ActivateWindow((int)TraktGUIWindows.RecentWatchedEpisodes);
                                break;

                            case (ActivityType.RecentAddedEpisodes):
                                GUIRecentAddedEpisodes.CurrentUser = TraktSettings.Username;
                                GUIWindowManager.ActivateWindow((int)TraktGUIWindows.RecentAddedEpisodes);
                                break;

                            case (ActivityType.RecentAddedMovies):
                                GUIRecentAddedMovies.CurrentUser = TraktSettings.Username;
                                GUIWindowManager.ActivateWindow((int)TraktGUIWindows.RecentAddedMovies);
                                break;

                            case (ActivityType.RecentShouts):
                                GUIRecentShouts.CurrentUser = TraktSettings.Username;
                                GUIWindowManager.ActivateWindow((int)TraktGUIWindows.RecentShouts);
                                break;

                            case (ActivityType.MovieWatchList):
                                GUIWatchListMovies.CurrentUser = TraktSettings.Username;
                                GUIWindowManager.ActivateWindow((int)TraktGUIWindows.WatchedListMovies);
                                break;

                            case (ActivityType.ShowWatchList):
                                GUIWatchListShows.CurrentUser = TraktSettings.Username;
                                GUIWindowManager.ActivateWindow((int)TraktGUIWindows.WatchedListShows);
                                break;

                            case (ActivityType.EpisodeWatchList):
                                GUIWatchListEpisodes.CurrentUser = TraktSettings.Username;
                                GUIWindowManager.ActivateWindow((int)TraktGUIWindows.WatchedListEpisodes);
                                break;

                            case (ActivityType.Lists):
                                GUILists.CurrentUser = TraktSettings.Username;
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
            }, Translation.GettingFollowerList, true);
        }

        private void LoadActivityTypes()
        {
            // clear facade
            GUIControl.ClearControl(GetID, Facade.GetID);

            // add each type to the list           
            GUIListItem item = new GUIListItem(Translation.RecentWatchedEpisodes);
            item.IconImage = UserProfile.AvatarFilename;
            item.IconImageBig = UserProfile.AvatarFilename;
            item.ThumbnailImage = UserProfile.AvatarFilename;
            item.PinImage = "traktActivityWatched.png";
            item.OnItemSelected += OnActivityTypeSelected;
            Utils.SetDefaultIcons(item);
            Facade.Add(item);

            item = new GUITraktUserListItem(Translation.RecentWatchedMovies);
            item.IconImage = UserProfile.AvatarFilename;
            item.IconImageBig = UserProfile.AvatarFilename;
            item.ThumbnailImage = UserProfile.AvatarFilename;
            item.PinImage = "traktActivityWatched.png";
            item.OnItemSelected += OnActivityTypeSelected;
            Utils.SetDefaultIcons(item);
            Facade.Add(item);

            item = new GUITraktUserListItem(Translation.RecentAddedEpisodes);
            item.IconImage = UserProfile.AvatarFilename;
            item.IconImageBig = UserProfile.AvatarFilename;
            item.ThumbnailImage = UserProfile.AvatarFilename;
            item.PinImage = "traktActivityCollected.png";
            item.OnItemSelected += OnActivityTypeSelected;
            Utils.SetDefaultIcons(item);
            Facade.Add(item);

            item = new GUITraktUserListItem(Translation.RecentAddedMovies);
            item.IconImage = UserProfile.AvatarFilename;
            item.IconImageBig = UserProfile.AvatarFilename;
            item.ThumbnailImage = UserProfile.AvatarFilename;
            item.PinImage = "traktActivityCollected.png";
            item.OnItemSelected += OnActivityTypeSelected;
            Utils.SetDefaultIcons(item);
            Facade.Add(item);

            item = new GUITraktUserListItem(Translation.RecentShouts);
            item.IconImage = UserProfile.AvatarFilename;
            item.IconImageBig = UserProfile.AvatarFilename;
            item.ThumbnailImage = UserProfile.AvatarFilename;
            item.PinImage = "traktActivityShout.png";
            item.OnItemSelected += OnActivityTypeSelected;
            Utils.SetDefaultIcons(item);
            Facade.Add(item);

            item = new GUITraktUserListItem(Translation.Lists);
            item.IconImage = UserProfile.AvatarFilename;
            item.IconImageBig = UserProfile.AvatarFilename;
            item.ThumbnailImage = UserProfile.AvatarFilename;
            item.PinImage = "traktActivityList.png";
            item.OnItemSelected += OnActivityTypeSelected;
            Utils.SetDefaultIcons(item);
            Facade.Add(item);

            item = new GUITraktUserListItem(Translation.WatchListShows);
            item.IconImage = UserProfile.AvatarFilename;
            item.IconImageBig = UserProfile.AvatarFilename;
            item.ThumbnailImage = UserProfile.AvatarFilename;
            item.PinImage = "traktActivityWatchlist.png";
            item.OnItemSelected += OnActivityTypeSelected;
            Utils.SetDefaultIcons(item);
            Facade.Add(item);

            item = new GUITraktUserListItem(Translation.WatchListMovies);
            item.IconImage = UserProfile.AvatarFilename;
            item.IconImageBig = UserProfile.AvatarFilename;
            item.ThumbnailImage = UserProfile.AvatarFilename;
            item.PinImage = "traktActivityWatchlist.png";
            item.OnItemSelected += OnActivityTypeSelected;
            Utils.SetDefaultIcons(item);
            Facade.Add(item);

            item = new GUITraktUserListItem(Translation.WatchListEpisodes);
            item.IconImage = UserProfile.AvatarFilename;
            item.IconImageBig = UserProfile.AvatarFilename;
            item.ThumbnailImage = UserProfile.AvatarFilename;
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

        #endregion
    }
}