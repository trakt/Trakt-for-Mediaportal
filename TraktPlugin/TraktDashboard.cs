using MediaPortal.GUI.Library;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using TraktAPI.DataStructures;
using TraktAPI.Extensions;
using TraktPlugin.Cache;
using TraktPlugin.Extensions;
using TraktPlugin.GUI;
using TraktPlugin.TmdbAPI.DataStructures;
using Action = MediaPortal.GUI.Library.Action;

namespace TraktPlugin
{
    public enum ActivityView
    {
        community,
        followers,
        following,
        friends,
        friendsandme,
        me
    }

    #region Data Structures

    [DataContract]
    public class ActivityFilter
    {
        [DataContract]
        public class Type
        {
            [DataMember]
            public bool Episodes = true;

            [DataMember]
            public bool Seasons = true;

            [DataMember]
            public bool Shows = true;

            [DataMember]
            public bool Movies = true;

            [DataMember]
            public bool Lists = true;

            [DataMember]
            public bool Comments = true;

            [DataMember]
            public bool People = true;
        }

        [DataContract]
        public class Action
        {
            [DataMember]
            public bool Watched = true;

            [DataMember]
            public bool Collected = true;

            [DataMember]
            public bool Rated = true;

            [DataMember]
            public bool Watchlisted = true;

            [DataMember]
            public bool Commented = true;

            [DataMember]
            public bool Paused = true;

            [DataMember]
            public bool Updated = true;

            [DataMember]
            public bool Added = true;

            [DataMember]
            public bool Liked = true;

            [DataMember]
            public bool HiddenCalendarItems = true;

            [DataMember]
            public bool HiddenRecommendations = true;

            [DataMember]
            public bool HiddedCollectedProgress = true;

            [DataMember]
            public bool HiddenWatchedProgress = true;
        }

        [DataMember]
        public Action Actions { get; set; }

        [DataMember]
        public Type Types { get; set; }
    }

    #endregion

    internal class TraktDashboard
    {
        #region Enums

        #endregion

        #region Private Variables

        private static Object lockObject = new object();

        private long ActivityStartTime = 0;

        private Timer ActivityTimer = null;
        private Timer TrendingMoviesTimer = null;
        private Timer TrendingShowsTimer = null;
        private Timer StatisticsTimer = null;

        bool GetFullActivityLoad = false;
        bool TrendingContextMenuIsActive = false;
        bool ReloadActivityView = false;

        DateTime LastTrendingShowUpdate = DateTime.MinValue;
        DateTime LastTrendingMovieUpdate = DateTime.MinValue;

        TraktActivity.Activity PreviousSelectedActivity = null;

        #endregion

        #region Constructor

        public TraktDashboard() { }

        #endregion

        #region Private Methods

        private DashboardTrendingSettings GetTrendingSettings()
        {
            // skinners should set unique window ids per trending so it doesn't matter if we pick the first
            // the whole point of having a collection is to define unique dashboard settings per window otherwise all windows share the same settings

            if (TraktSkinSettings.DashboardTrendingCollection == null)
                return new DashboardTrendingSettings();

            string windowID = GUIWindowManager.ActiveWindow.ToString();

            var trendingSettings = TraktSkinSettings.DashboardTrendingCollection.FirstOrDefault(d => d.MovieWindows.Contains(windowID) || d.TVShowWindows.Contains(windowID));
            return trendingSettings;
        }

        private int GetMaxTrendingProperties()
        {
            if (TraktSkinSettings.DashboardTrendingCollection == null) return 0;
            return TraktSkinSettings.DashboardTrendingCollection.Select(d => d.FacadeMaxItems).Max();
        }

        private GUIFacadeControl GetFacade(int facadeID)
        {
            lock (lockObject)
            {
                int i = 0;
                GUIFacadeControl facade = null;

                // window init message does not work unless overridden from a guiwindow class
                // so we need to be ensured that the window is fully loaded
                // before we can get reference to a skin control
                try
                {
                    bool bReady;
                    
                    do
                    {
                        // get current window
                        var window = GUIWindowManager.GetWindow(GUIWindowManager.ActiveWindow);

                        // get facade control
                        facade = window.GetControl(facadeID) as GUIFacadeControl;

                        // ensure we're ready for action
                        if (!window.WindowLoaded || facade == null)
                        {
                            bReady = false;
                            Thread.Sleep(100);
                        }
                        else
                        {
                            bReady = true;
                        }

                        i++;
                    }
                    while (i < 25 && !bReady);
                }
                catch (Exception ex)
                {
                    TraktLogger.Error("MediaPortal failed to get the active control");
                    TraktLogger.Error(ex.StackTrace);
                }

                if (facade == null)
                {
                    TraktLogger.Debug("Unable to find facade control [id:{0}], check that trakt skin settings are correctly defined!", facadeID.ToString());

                    // remove windows from future checks
                    foreach (var tc in TraktSkinSettings.DashboardTrendingCollection)
                    {
                        tc.MovieWindows.RemoveAll(w => w == GUIWindowManager.ActiveWindow.ToString());
                        tc.TVShowWindows.RemoveAll(w => w == GUIWindowManager.ActiveWindow.ToString());
                    }

                    TraktSkinSettings.DashBoardActivityWindows.RemoveAll(d => d == GUIWindowManager.ActiveWindow.ToString());
                }

                return facade;
            }
        }

        private void GetStatistics()
        {
            Thread.CurrentThread.Name = "DashStats";

            // initial publish from persisted settings            
            if (TraktSettings.LastStatistics != null)
            {
                GUICommon.SetStatisticProperties(TraktSettings.LastStatistics, TraktSettings.Username);
                TraktSettings.LastStatistics = null;
            }

            // retrieve statistics from online
            var userStats = TraktAPI.TraktAPI.GetUserStatistics();
            if (userStats != null)
            {
                GUICommon.SetStatisticProperties(userStats, TraktSettings.Username);
                PreviousStatistics = userStats;
            }
        }

        private TraktUserSummary GetUserProfile()
        {
            // get cached user profile
            // this will be updated whenever a user enters the UserProfile GUI
            if (TraktSettings.LastUserProfile == null)
            {
                var userProfile = TraktAPI.TraktAPI.GetUserProfile();
                if (userProfile != null)
                {
                    TraktSettings.LastUserProfile = userProfile;
                }
            }
            return TraktSettings.LastUserProfile;
        }

        private void ClearSelectedActivityProperties()
        {
            GUIUtils.SetProperty("#Trakt.Selected.Activity.Type", "none");
            GUIUtils.SetProperty("#Trakt.Selected.Activity.Action", "none");

            GUICommon.ClearUserProperties();
            GUICommon.ClearEpisodeProperties();
            GUICommon.ClearMovieProperties();
            GUICommon.ClearShowProperties();

            GUIUtils.SetProperty("#Trakt.Activity.Count", "0");
            GUIUtils.SetProperty("#Trakt.Activity.Items", string.Format("0 {0}", Translation.Activities));
            GUIUtils.SetProperty("#Trakt.Activity.Description", GetActivityDescription((ActivityView)TraktSettings.ActivityStreamView));
        }

        private string GetActivityDescription(ActivityView activityView)
        {
            string description = string.Empty;

            switch (activityView)
            {
                case ActivityView.community:
                    description = Translation.ActivityCommunityDesc;
                    break;

                case ActivityView.followers:
                    description = Translation.ActivityFollowersDesc;
                    break;

                case ActivityView.following:
                    description = Translation.ActivityFollowingDesc;
                    break;

                case ActivityView.friends:
                    description = Translation.ActivityFriendsDesc;
                    break;

                case ActivityView.friendsandme:
                    description = Translation.ActivityFriendsAndMeDesc;
                    break;

                case ActivityView.me:
                    description = Translation.ActivityMeDesc;
                    break;
            }

            return description;
        }

        private void LoadActivity()
        {
            Thread.CurrentThread.Name = "DashActivity";

            GUIFacadeControl facade = null;

            // get the facade, may need to wait until
            // window has completely loaded
            if (TraktSkinSettings.DashboardActivityFacadeType.ToLowerInvariant() != "none")
            {
                facade = GetFacade((int)TraktDashboardControls.ActivityFacade);
                if (facade == null)
                    return;

                // we may trigger a re-load by switching from
                // community->friends->community etc
                lock (this)
                {
                    // load facade if empty and we have activity already
                    // facade is empty on re-load of window
                    if (facade.Count == 0 && PreviousActivity != null && PreviousActivity.Activities != null && PreviousActivity.Activities.Count > 0)
                    {
                        PublishActivityProperties(PreviousActivity);
                        LoadActivityFacade(PreviousActivity, facade);
                    }

                    // get latest activity
                    var activities = GetActivity((ActivityView)TraktSettings.ActivityStreamView);

                    // publish properties
                    PublishActivityProperties(activities);

                    // load activity into list
                    LoadActivityFacade(activities, facade);
                }
            }
            
            // only need to publish properties
            if (facade == null && TraktSkinSettings.DashboardActivityPropertiesMaxItems > 0)
            {
                // get latest activity
                var activities = GetActivity((ActivityView)TraktSettings.ActivityStreamView);
                if (activities == null || activities.Activities == null)
                    return;

                // publish properties
                PublishActivityProperties(activities);
                
                // download images
                var avatarImages = new List<GUITraktImage>();
                foreach (var activity in activities.Activities.Take(TraktSkinSettings.DashboardActivityPropertiesMaxItems))
                {
                    if (activity.User != null)
                    {
                        avatarImages.Add(new GUITraktImage { UserImages = activity.User.Images });
                    }
                }
                GUIUserListItem.GetImages(avatarImages);
            }
        }

        private void PublishActivityProperties()
        {
            PublishActivityProperties(PreviousActivity);
        }
        private void PublishActivityProperties(TraktActivity activity)
        {
            if (activity == null) return;

            var activities = activity.Activities;
            if (activities == null) return;

            int maxItems = activities.Count() < TraktSkinSettings.DashboardActivityFacadeMaxItems ? activities.Count() : TraktSkinSettings.DashboardActivityFacadeMaxItems;

            for (int i = 0; i < maxItems; i++)
            {
                GUIUtils.SetProperty(string.Format("#Trakt.Activity.{0}.Action", i), activities[i].Action);
                GUIUtils.SetProperty(string.Format("#Trakt.Activity.{0}.Type", i), activities[i].Type);
                GUIUtils.SetProperty(string.Format("#Trakt.Activity.{0}.ActivityPinIcon", i), GetActivityImage(activities[i]));
                GUIUtils.SetProperty(string.Format("#Trakt.Activity.{0}.ActivityPinIconNoExt", i), GetActivityImage(activities[i]).Replace(".png", string.Empty));
                GUIUtils.SetProperty(string.Format("#Trakt.Activity.{0}.Title", i), GUICommon.GetActivityItemName(activities[i]));
                GUIUtils.SetProperty(string.Format("#Trakt.Activity.{0}.Time", i), activities[i].Timestamp.FromISO8601().ToLocalTime().ToShortTimeString());
                GUIUtils.SetProperty(string.Format("#Trakt.Activity.{0}.Day", i), activities[i].Timestamp.FromISO8601().ToLocalTime().DayOfWeek.ToString().Substring(0, 3));
                GUIUtils.SetProperty(string.Format("#Trakt.Activity.{0}.Shout", i), GetActivityShoutText(activities[i]));
            }
        }

        private void LoadActivityFacade(TraktActivity activities, GUIFacadeControl facade)
        {
            if (TraktSkinSettings.DashBoardActivityWindows == null || !TraktSkinSettings.DashBoardActivityWindows.Contains(GUIWindowManager.ActiveWindow.ToString()))
                return;

            // if no activities report to user
            if (activities == null || activities.Activities == null || activities.Activities.Count == 0)
            {
                facade.Clear();

                var item = new GUIListItem(Translation.NoActivities);
                facade.Add(item);
                facade.CurrentLayout = (GUIFacadeControl.Layout)Enum.Parse(typeof(GUIFacadeControl.Layout), TraktSkinSettings.DashboardActivityFacadeType);
                ClearSelectedActivityProperties();
                return;
            }

            // get the current view
            var view = (ActivityView)TraktSettings.ActivityStreamView;

            // if no new activities then nothing to do
            if (facade.Count > 0 && !ReloadActivityView)
            {
                var mostRecentActivity = facade[0].TVTag as TraktActivity.Activity;
                var lastActivity = facade[facade.Count - 1].TVTag as TraktActivity.Activity;

                if (mostRecentActivity != null && lastActivity != null)
                {
                    // only check the timestamp if only showing yourself
                    // check first and last because we may insert something in the middle between last load
                    if (view == ActivityView.me && mostRecentActivity.Timestamp == activities.Activities.First().Timestamp
                                                && lastActivity.Timestamp == activities.Activities.Last().Timestamp)
                        return;

                    if (mostRecentActivity.Timestamp == activities.Activities.First().Timestamp &&
                        mostRecentActivity.User.Username == activities.Activities.First().User.Username)
                    {
                        return;
                    }
                }
            }

            ReloadActivityView = false;
            TraktLogger.Debug("Loading Trakt Activity Facade");

            // stop any existing image downloads
            GUIUserListItem.StopDownload = true;

            // clear facade
            GUIControl.ClearControl(GUIWindowManager.ActiveWindow, facade.GetID);

            int itemId = 0;
            int PreviousSelectedIdx = -1;
            var userImages = new List<GUITraktImage>();
            
            // Add each activity item to the facade
            foreach (var activity in activities.Activities.Distinct().OrderByDescending(a => a.Timestamp))
            {
                if (PreviousSelectedIdx == -1 && PreviousSelectedActivity != null && TraktSettings.RememberLastSelectedActivity)
                {
                    if (activity.Equals(PreviousSelectedActivity))
                        PreviousSelectedIdx = itemId;
                }

                string itemLabel = GUICommon.GetActivityListItemTitle(activity, view);
                if (string.IsNullOrEmpty(itemLabel))
                    continue;

                var item = new GUIUserListItem(itemLabel, GUIWindowManager.ActiveWindow);

                string activityImage = GetActivityImage(activity);
                string avatarImage = GetAvatarImage(activity);

                // add image to download
                var images = new GUITraktImage { UserImages = activity.User.Images };
                if (avatarImage == "defaultTraktUser.png")
                    userImages.Add(images);

                item.Label2 = activity.Timestamp.ToPrettyDateTime();
                item.TVTag = activity;
                item.User = activity.User;
                item.Images = images;
                item.ItemId = Int32.MaxValue - itemId;
                item.IconImage = avatarImage;
                item.IconImageBig = avatarImage;
                item.ThumbnailImage = avatarImage;
                item.PinImage = activityImage;
                item.OnItemSelected += OnActivitySelected;
                facade.Add(item);
                itemId++;
            }

            // Set Facade Layout
            facade.CurrentLayout = (GUIFacadeControl.Layout)Enum.Parse(typeof(GUIFacadeControl.Layout), TraktSkinSettings.DashboardActivityFacadeType);
            facade.SetVisibleFromSkinCondition();

            // Select previously selected item
            if (facade.LayoutControl.IsFocused && PreviousSelectedIdx >= 0)
                facade.SelectIndex(PreviousSelectedIdx);

            // set facade properties
            GUIUtils.SetProperty("#Trakt.Activity.Count", activities.Activities.Count().ToString());
            GUIUtils.SetProperty("#Trakt.Activity.Items", string.Format("{0} {1}", activities.Activities.Count().ToString(), activities.Activities.Count() > 1 ? Translation.Activities : Translation.Activity));
            GUIUtils.SetProperty("#Trakt.Activity.Description", GetActivityDescription((ActivityView)TraktSettings.ActivityStreamView));

            // Download avatar images Async and set to facade
            GUIUserListItem.StopDownload = false;
            GUIUserListItem.GetImages(userImages);
            
            TraktLogger.Debug("Finished Loading Activity facade");
        }

        private void LoadTrendingMovies()
        {
            LoadTrendingMovies(false);
        }
        private void LoadTrendingMovies(bool forceReload)
        {
            if (Thread.CurrentThread.Name == null)
                Thread.CurrentThread.Name = "DashMovies";

            GUIFacadeControl facade = null;
            bool isCached;

            var trendingSettings = GetTrendingSettings();
            if (trendingSettings == null) return;

            if (trendingSettings.FacadeType.ToLowerInvariant() != "none")
            {
                facade = GetFacade((int)TraktDashboardControls.TrendingMoviesFacade);
                if (facade == null) return;

                // load facade if empty and we have trending already
                // facade is empty on re-load of window
                if (facade.Count == 0 && PreviousTrendingMovies != null && PreviousTrendingMovies.Count() > 0)
                {
                    PublishMovieProperties(PreviousTrendingMovies);
                    LoadTrendingMoviesFacade(PreviousTrendingMovies, facade);
                }

                // get latest trending
                var trendingMovies = GetTrendingMovies(out isCached);

                // prevent an unnecessary reload
                if (!isCached || forceReload)
                {
                    // publish properties
                    PublishMovieProperties(trendingMovies);

                    // load trending into list
                    LoadTrendingMoviesFacade(trendingMovies, facade);
                }
            }
            
            // only publish skin properties
            if (facade == null && trendingSettings.PropertiesMaxItems > 0)
            {
                // get latest trending
                var trendingMovies = GetTrendingMovies(out isCached);

                if (!isCached)
                {
                    if (trendingMovies == null || trendingMovies.Count() == 0) return;

                    // publish properties
                    PublishMovieProperties(trendingMovies);

                    // download images
                    var movieImages = new List<GUITmdbImage>();
                    foreach (var trendingItem in trendingMovies)
                    {
                        movieImages.Add(new GUITmdbImage
                                            { 
                                                MovieImages = new TmdbMovieImages { Id = trendingItem.Movie.Ids.Tmdb } 
                                            });
                    }
                    GUIMovieListItem.GetImages(movieImages);
                }
            }
        }

        private void PublishMovieProperties()
        {
            PublishMovieProperties(PreviousTrendingMovies);
        }
        private void PublishMovieProperties(IEnumerable<TraktMovieTrending> trendingItems)
        {
            if (trendingItems == null) return;

            if (TraktSettings.FilterTrendingOnDashboard)
                trendingItems = GUICommon.FilterTrendingMovies(trendingItems);

            var trendingList = trendingItems.ToList();
            int maxItems = trendingItems.Count() < GetMaxTrendingProperties() ? trendingItems.Count() : GetMaxTrendingProperties();

            for (int i = 0; i < maxItems; i++)
            {
                var trendingItem = trendingList[i];
                if (trendingItem == null || trendingItem.Movie == null) continue;

                GUICommon.SetProperty(string.Format("#Trakt.Movie.{0}.Watchers", i), trendingItem.Watchers.ToString());
                GUICommon.SetProperty(string.Format("#Trakt.Movie.{0}.Watchers.Extra", i), trendingItem.Watchers > 1 ? string.Format(Translation.PeopleWatching, trendingItem.Watchers) : Translation.PersonWatching);

                GUICommon.SetProperty(string.Format("#Trakt.Movie.{0}.Id", i), trendingItem.Movie.Ids.Trakt);
                GUICommon.SetProperty(string.Format("#Trakt.Movie.{0}.TmdbId", i), trendingItem.Movie.Ids.Tmdb);
                GUICommon.SetProperty(string.Format("#Trakt.Movie.{0}.ImdbId", i), trendingItem.Movie.Ids.Imdb);
                GUICommon.SetProperty(string.Format("#Trakt.Movie.{0}.Certification", i), trendingItem.Movie.Certification);
                GUICommon.SetProperty(string.Format("#Trakt.Movie.{0}.Language", i), Translation.GetLanguageFromISOCode(trendingItem.Movie.Language));
                GUICommon.SetProperty(string.Format("#Trakt.Movie.{0}.Overview", i), string.IsNullOrEmpty(trendingItem.Movie.Overview) ? Translation.NoMovieSummary : trendingItem.Movie.Overview);
                GUICommon.SetProperty(string.Format("#Trakt.Movie.{0}.Released", i), trendingItem.Movie.Released.FromISO8601().ToShortDateString());
                GUICommon.SetProperty(string.Format("#Trakt.Movie.{0}.Runtime", i), trendingItem.Movie.Runtime.ToString());
                GUICommon.SetProperty(string.Format("#Trakt.Movie.{0}.Tagline", i), trendingItem.Movie.Tagline);
                GUICommon.SetProperty(string.Format("#Trakt.Movie.{0}.Title", i), trendingItem.Movie.Title);
                GUICommon.SetProperty(string.Format("#Trakt.Movie.{0}.Trailer", i), trendingItem.Movie.Trailer);
                GUICommon.SetProperty(string.Format("#Trakt.Movie.{0}.Url", i), string.Format("http://trakt.tv/movies/{0}", trendingItem.Movie.Ids.Slug));
                GUICommon.SetProperty(string.Format("#Trakt.Movie.{0}.Year", i), trendingItem.Movie.Year.ToString());
                GUICommon.SetProperty(string.Format("#Trakt.Movie.{0}.Genres", i), string.Join(", ", TraktGenres.Translate(trendingItem.Movie.Genres)));
                GUICommon.SetProperty(string.Format("#Trakt.Movie.{0}.InCollection", i), trendingItem.Movie.IsCollected().ToString());
                GUICommon.SetProperty(string.Format("#Trakt.Movie.{0}.InWatchList", i), trendingItem.Movie.IsWatchlisted().ToString());
                GUICommon.SetProperty(string.Format("#Trakt.Movie.{0}.Plays", i), trendingItem.Movie.Plays());
                GUICommon.SetProperty(string.Format("#Trakt.Movie.{0}.Watched", i), trendingItem.Movie.IsWatched().ToString());
                GUICommon.SetProperty(string.Format("#Trakt.Movie.{0}.Rating", i), trendingItem.Movie.UserRating());
                GUICommon.SetProperty(string.Format("#Trakt.Movie.{0}.Ratings.Votes", i), trendingItem.Movie.Votes);
                GUICommon.SetProperty(string.Format("#Trakt.Movie.{0}.Ratings.Icon", i), (trendingItem.Movie.Rating >= 6) ? "love" : "hate");
                GUICommon.SetProperty(string.Format("#Trakt.Movie.{0}.Ratings.Percentage", i), trendingItem.Movie.Rating.ToPercentage());

                var images = TmdbCache.GetMovieImages(trendingItem.Movie.Ids.Tmdb);
                if (images != null)
                {
                    GUICommon.SetProperty(string.Format("#Trakt.Movie.{0}.PosterImageFilename", i), TmdbCache.GetMoviePosterFilename(images));
                    GUICommon.SetProperty(string.Format("#Trakt.Movie.{0}.FanartImageFilename", i), TmdbCache.GetMovieBackdropFilename(images));
                }
            }
        }

        private void ClearMovieProperties()
        {
            for (int i = 0; i < GetMaxTrendingProperties(); i++)
            {
                GUIUtils.SetProperty(string.Format("#Trakt.Movie.{0}.Watchers", i), string.Empty);
                GUIUtils.SetProperty(string.Format("#Trakt.Movie.{0}.Watchers.Extra", i), string.Empty);

                GUIUtils.SetProperty(string.Format("#Trakt.Movie.{0}.Id", i), string.Empty);
                GUIUtils.SetProperty(string.Format("#Trakt.Movie.{0}.ImdbId", i), string.Empty);
                GUIUtils.SetProperty(string.Format("#Trakt.Movie.{0}.TmdbId", i), string.Empty);
                GUIUtils.SetProperty(string.Format("#Trakt.Movie.{0}.Certification", i), string.Empty);
                GUIUtils.SetProperty(string.Format("#Trakt.Movie.{0}.Language", i), string.Empty);
                GUIUtils.SetProperty(string.Format("#Trakt.Movie.{0}.Overview", i), string.Empty);
                GUIUtils.SetProperty(string.Format("#Trakt.Movie.{0}.Released", i), string.Empty);
                GUIUtils.SetProperty(string.Format("#Trakt.Movie.{0}.Runtime", i), string.Empty);
                GUIUtils.SetProperty(string.Format("#Trakt.Movie.{0}.Tagline", i), string.Empty);
                GUIUtils.SetProperty(string.Format("#Trakt.Movie.{0}.Title", i), string.Empty);
                GUIUtils.SetProperty(string.Format("#Trakt.Movie.{0}.Trailer", i), string.Empty);
                GUIUtils.SetProperty(string.Format("#Trakt.Movie.{0}.Url", i), string.Empty);
                GUIUtils.SetProperty(string.Format("#Trakt.Movie.{0}.Year", i), string.Empty);
                GUIUtils.SetProperty(string.Format("#Trakt.Movie.{0}.Genres", i), string.Empty);
                GUIUtils.SetProperty(string.Format("#Trakt.Movie.{0}.PosterImageFilename", i), string.Empty);
                GUIUtils.SetProperty(string.Format("#Trakt.Movie.{0}.FanartImageFilename", i), string.Empty);
                GUIUtils.SetProperty(string.Format("#Trakt.Movie.{0}.InCollection", i), string.Empty);
                GUIUtils.SetProperty(string.Format("#Trakt.Movie.{0}.InWatchList", i), string.Empty);
                GUIUtils.SetProperty(string.Format("#Trakt.Movie.{0}.Plays", i), string.Empty);
                GUIUtils.SetProperty(string.Format("#Trakt.Movie.{0}.Watched", i), string.Empty);
                GUIUtils.SetProperty(string.Format("#Trakt.Movie.{0}.Rating", i), string.Empty);
                GUIUtils.SetProperty(string.Format("#Trakt.Movie.{0}.RatingAdvanced", i), string.Empty);
                GUIUtils.SetProperty(string.Format("#Trakt.Movie.{0}.Ratings.Icon", i), string.Empty);
                GUIUtils.SetProperty(string.Format("#Trakt.Movie.{0}.Ratings.Percentage", i), string.Empty);
                GUIUtils.SetProperty(string.Format("#Trakt.Movie.{0}.Ratings.Votes", i), string.Empty);
            }
        }

        private void LoadTrendingMoviesFacade(IEnumerable<TraktMovieTrending> trendingItems, GUIFacadeControl facade)
        {
            if (TraktSkinSettings.DashboardTrendingCollection == null || !TraktSkinSettings.DashboardTrendingCollection.Exists(d => d.MovieWindows.Contains(GUIWindowManager.ActiveWindow.ToString())))
                return;
            
            // get trending settings for window
            var trendingSettings = GetTrendingSettings();
            if (trendingSettings == null) return;

            TraktLogger.Debug("Loading Trakt Trending Movies facade");

            // if no trending, then nothing to do
            if (trendingItems == null || trendingItems.Count() == 0)
                return;

            // stop any existing image downloads
            GUIMovieListItem.StopDownload = true;

            // clear facade
            GUIControl.ClearControl(GUIWindowManager.ActiveWindow, facade.GetID);

            int itemId = 0;
            var movieImages = new List<GUITmdbImage>();

            // filter movies
            if (TraktSettings.FilterTrendingOnDashboard)
                trendingItems = GUICommon.FilterTrendingMovies(trendingItems);

            // Add each activity item to the facade
            foreach (var trendingItem in trendingItems.Take(trendingSettings.FacadeMaxItems))
            {
                if (trendingItem.Movie == null)
                    continue;

                // add image for download
                var images = new GUITmdbImage { MovieImages = new TmdbMovieImages { Id = trendingItem.Movie.Ids.Tmdb } };
                movieImages.Add(images);

                var item = new GUIMovieListItem(trendingItem.Movie.Title, GUIWindowManager.ActiveWindow);

                item.Label2 = trendingItem.Movie.Year.ToString();
                item.TVTag = trendingItem;
                item.Movie = trendingItem.Movie;
                item.Images = images;
                item.ItemId = Int32.MaxValue - itemId;
                item.IconImage = GUIImageHandler.GetDefaultPoster(false);
                item.IconImageBig = GUIImageHandler.GetDefaultPoster();
                item.ThumbnailImage = GUIImageHandler.GetDefaultPoster();
                item.OnItemSelected += OnTrendingMovieSelected;
                try
                {
                    facade.Add(item);
                }
                catch { }
                itemId++;
            }

            // Set Facade Layout
            facade.CurrentLayout = (GUIFacadeControl.Layout)Enum.Parse(typeof(GUIFacadeControl.Layout), trendingSettings.FacadeType);
            facade.SetVisibleFromSkinCondition();

            // set facade properties
            GUIUtils.SetProperty("#Trakt.Trending.Movies.Items", string.Format("{0} {1}", trendingItems.Count().ToString(), trendingItems.Count() > 1 ? Translation.Movies : Translation.Movie));
            GUIUtils.SetProperty("#Trakt.Trending.Movies.PeopleCount", trendingItems.Sum(t => t.Watchers).ToString());
            GUIUtils.SetProperty("#Trakt.Trending.Movies.Description", string.Format(Translation.TrendingTVShowsPeople, trendingItems.Sum(t => t.Watchers).ToString(), trendingItems.Count().ToString()));

            // Download images Async and set to facade
            GUIMovieListItem.StopDownload = false;
            GUIMovieListItem.GetImages(movieImages);

            TraktLogger.Debug("Finished Loading Trending Movies facade");
        }

        private void LoadTrendingShows()
        {
            LoadTrendingShows(false);
        }
        private void LoadTrendingShows(bool forceReload)
        {
            if (Thread.CurrentThread.Name == null)
                Thread.CurrentThread.Name = "DashShows";

            GUIFacadeControl facade = null;
            bool isCached;

            var trendingSettings = GetTrendingSettings();
            if (trendingSettings == null) return;

            if (trendingSettings.FacadeType.ToLowerInvariant() != "none")
            {
                facade = GetFacade((int)TraktDashboardControls.TrendingShowsFacade);
                if (facade == null) return;

                // load facade if empty and we have trending already
                // facade is empty on re-load of window
                if (facade.Count == 0 && PreviousTrendingShows != null && PreviousTrendingShows.Count() > 0)
                {
                    PublishShowProperties(PreviousTrendingShows);
                    LoadTrendingShowsFacade(PreviousTrendingShows, facade);
                }

                // get latest trending
                var trendingShows = GetTrendingShows(out isCached);

                // prevent an unnecessary reload
                if (!isCached || forceReload)
                {
                    // publish properties
                    PublishShowProperties(trendingShows);

                    // load trending into list
                    LoadTrendingShowsFacade(trendingShows, facade);
                }
            }
            
            // only publish skin properties
            if (facade == null && GetMaxTrendingProperties() > 0)
            {
                // get latest trending
                var trendingShows = GetTrendingShows(out isCached);
                
                if (!isCached)
                {
                    if (trendingShows == null || trendingShows.Count() == 0) return;

                    // publish properties
                    PublishShowProperties(trendingShows);

                    // download images
                    var showImages = new List<GUITmdbImage>();
                    foreach (var trendingItem in trendingShows)
                    {
                        showImages.Add(new GUITmdbImage { ShowImages = new TmdbShowImages { Id = trendingItem.Show.Ids.Tmdb } });
                    }
                    GUIShowListItem.GetImages(showImages);
                }
            }
        }

        private void PublishShowProperties()
        {
            PublishShowProperties(PreviousTrendingShows);
        }
        private void PublishShowProperties(IEnumerable<TraktShowTrending> trendingItems)
        {
            if (trendingItems == null) return;

            if (TraktSettings.FilterTrendingOnDashboard)
                trendingItems = GUICommon.FilterTrendingShows(trendingItems);

            var trendingList = trendingItems.ToList();
            int maxItems = trendingItems.Count() < GetMaxTrendingProperties() ? trendingItems.Count() : GetMaxTrendingProperties();

            for (int i = 0; i < maxItems; i++)
            {
                var trendingItem = trendingList[i];
                if (trendingItem == null || trendingItem.Show == null) continue;

                GUICommon.SetProperty(string.Format("#Trakt.Show.{0}.Watchers", i), trendingItem.Watchers.ToString());
                GUICommon.SetProperty(string.Format("#Trakt.Show.{0}.Watchers.Extra", i), trendingItem.Watchers > 1 ? string.Format(Translation.PeopleWatching, trendingItem.Watchers) : Translation.PersonWatching);

                GUICommon.SetProperty(string.Format("#Trakt.Show.{0}.Id", i), trendingItem.Show.Ids.Imdb);
                GUICommon.SetProperty(string.Format("#Trakt.Show.{0}.ImdbId", i), trendingItem.Show.Ids.Imdb);
                GUICommon.SetProperty(string.Format("#Trakt.Show.{0}.TmdbId", i), trendingItem.Show.Ids.Tmdb);
                GUICommon.SetProperty(string.Format("#Trakt.Show.{0}.TvdbId", i), trendingItem.Show.Ids.Tvdb);
                GUICommon.SetProperty(string.Format("#Trakt.Show.{0}.TvRageId", i), trendingItem.Show.Ids.TvRage);
                GUICommon.SetProperty(string.Format("#Trakt.Show.{0}.Title", i), trendingItem.Show.Title);
                GUICommon.SetProperty(string.Format("#Trakt.Show.{0}.Url", i), string.Format("http://trakt.tv/shows/{0}", trendingItem.Show.Ids.Slug));
                GUICommon.SetProperty(string.Format("#Trakt.Show.{0}.AirDay", i), trendingItem.Show.Airs.Day);
                GUICommon.SetProperty(string.Format("#Trakt.Show.{0}.AirTime", i), trendingItem.Show.Airs.Time);
                GUICommon.SetProperty(string.Format("#Trakt.Show.{0}.AirTimezone", i), trendingItem.Show.Airs.Timezone);
                GUICommon.SetProperty(string.Format("#Trakt.Show.{0}.Certification", i), trendingItem.Show.Certification);
                GUICommon.SetProperty(string.Format("#Trakt.Show.{0}.Country", i), trendingItem.Show.Country);
                GUICommon.SetProperty(string.Format("#Trakt.Show.{0}.FirstAired", i), trendingItem.Show.FirstAired.FromISO8601().ToShortDateString());
                GUICommon.SetProperty(string.Format("#Trakt.Show.{0}.Network", i), trendingItem.Show.Network);
                GUICommon.SetProperty(string.Format("#Trakt.Show.{0}.Overview", i), string.IsNullOrEmpty(trendingItem.Show.Overview) ? Translation.NoShowSummary : trendingItem.Show.Overview);
                GUICommon.SetProperty(string.Format("#Trakt.Show.{0}.Runtime", i), trendingItem.Show.Runtime.ToString());
                GUICommon.SetProperty(string.Format("#Trakt.Show.{0}.Year", i), trendingItem.Show.Year.ToString());
                GUICommon.SetProperty(string.Format("#Trakt.Show.{0}.Genres", i), string.Join(", ", TraktGenres.Translate(trendingItem.Show.Genres)));
                GUICommon.SetProperty(string.Format("#Trakt.Show.{0}.InWatchList", i), trendingItem.Show.IsWatchlisted());
                GUICommon.SetProperty(string.Format("#Trakt.Show.{0}.Watched", i), trendingItem.Show.IsWatched());
                GUICommon.SetProperty(string.Format("#Trakt.Show.{0}.InCollection", i), trendingItem.Show.IsCollected());
                GUICommon.SetProperty(string.Format("#Trakt.Show.{0}.Plays", i), trendingItem.Show.Plays());
                GUICommon.SetProperty(string.Format("#Trakt.Show.{0}.Rating", i), trendingItem.Show.UserRating());
                GUICommon.SetProperty(string.Format("#Trakt.Show.{0}.Ratings.Votes", i), trendingItem.Show.Votes);
                GUICommon.SetProperty(string.Format("#Trakt.Show.{0}.Ratings.Icon", i), (trendingItem.Show.Rating >= 6) ? "love" : "hate");
                GUICommon.SetProperty(string.Format("#Trakt.Show.{0}.Ratings.Percentage", i), trendingItem.Show.Rating.ToPercentage());

                var images = TmdbCache.GetShowImages(trendingItem.Show.Ids.Tmdb);
                if (images != null)
                {
                    GUICommon.SetProperty(string.Format("#Trakt.Show.{0}.PosterImageFilename", i), TmdbCache.GetShowPosterFilename(images));
                    GUICommon.SetProperty(string.Format("#Trakt.Show.{0}.FanartImageFilename", i), TmdbCache.GetShowBackdropFilename(images));
                }
            }
        }

        private void ClearShowProperties()
        {
            for (int i = 0; i < GetMaxTrendingProperties(); i++)
            {
                GUIUtils.SetProperty(string.Format("#Trakt.Show.{0}.Watchers", i), string.Empty);
                GUIUtils.SetProperty(string.Format("#Trakt.Show.{0}.Watchers.Extra", i), string.Empty);

                GUIUtils.SetProperty(string.Format("#Trakt.Show.{0}.Id", i), string.Empty);
                GUIUtils.SetProperty(string.Format("#Trakt.Show.{0}.ImdbId", i), string.Empty);
                GUIUtils.SetProperty(string.Format("#Trakt.Show.{0}.TvdbId", i), string.Empty);
                GUIUtils.SetProperty(string.Format("#Trakt.Show.{0}.TmdbId", i), string.Empty);
                GUIUtils.SetProperty(string.Format("#Trakt.Show.{0}.TvRageId", i), string.Empty);
                GUIUtils.SetProperty(string.Format("#Trakt.Show.{0}.Title", i), string.Empty);
                GUIUtils.SetProperty(string.Format("#Trakt.Show.{0}.Url", i), string.Empty);
                GUIUtils.SetProperty(string.Format("#Trakt.Show.{0}.AirDay", i), string.Empty);
                GUIUtils.SetProperty(string.Format("#Trakt.Show.{0}.AirTime", i), string.Empty);
                GUIUtils.SetProperty(string.Format("#Trakt.Show.{0}.AirTimezone", i), string.Empty);
                GUIUtils.SetProperty(string.Format("#Trakt.Show.{0}.Certification", i), string.Empty);
                GUIUtils.SetProperty(string.Format("#Trakt.Show.{0}.Country", i), string.Empty);
                GUIUtils.SetProperty(string.Format("#Trakt.Show.{0}.FirstAired", i), string.Empty);
                GUIUtils.SetProperty(string.Format("#Trakt.Show.{0}.Network", i), string.Empty);
                GUIUtils.SetProperty(string.Format("#Trakt.Show.{0}.Overview", i), string.Empty);
                GUIUtils.SetProperty(string.Format("#Trakt.Show.{0}.Runtime", i), string.Empty);
                GUIUtils.SetProperty(string.Format("#Trakt.Show.{0}.Year", i), string.Empty);
                GUIUtils.SetProperty(string.Format("#Trakt.Show.{0}.Genres", i), string.Empty);
                GUIUtils.SetProperty(string.Format("#Trakt.Show.{0}.InWatchList", i), string.Empty);
                GUIUtils.SetProperty(string.Format("#Trakt.Show.{0}.Watched", i), string.Empty);
                GUIUtils.SetProperty(string.Format("#Trakt.Show.{0}.InCollection", i), string.Empty);
                GUIUtils.SetProperty(string.Format("#Trakt.Show.{0}.Plays", i), string.Empty);
                GUIUtils.SetProperty(string.Format("#Trakt.Show.{0}.Rating", i), string.Empty);
                GUIUtils.SetProperty(string.Format("#Trakt.Show.{0}.RatingAdvanced", i), string.Empty);
                GUIUtils.SetProperty(string.Format("#Trakt.Show.{0}.Ratings.Icon", i), string.Empty);
                GUIUtils.SetProperty(string.Format("#Trakt.Show.{0}.Ratings.Percentage", i), string.Empty);
                GUIUtils.SetProperty(string.Format("#Trakt.Show.{0}.Ratings.Votes", i), string.Empty);
                GUIUtils.SetProperty(string.Format("#Trakt.Show.{0}.PosterImageFilename", i), string.Empty);
                GUIUtils.SetProperty(string.Format("#Trakt.Show.{0}.FanartImageFilename", i), string.Empty);
            }
        }

        private void LoadTrendingShowsFacade(IEnumerable<TraktShowTrending> trendingItems, GUIFacadeControl facade)
        {
            if (TraktSkinSettings.DashboardTrendingCollection == null || !TraktSkinSettings.DashboardTrendingCollection.Exists(d => d.TVShowWindows.Contains(GUIWindowManager.ActiveWindow.ToString())))
                return;

            // get trending settings
            var trendingSettings = GetTrendingSettings();
            if (trendingSettings == null) return;

            TraktLogger.Debug("Loading Trakt Trending Shows facade");

            // if no trending, then nothing to do
            if (trendingItems == null || trendingItems.Count() == 0)
                return;

            // stop any existing image downloads
            GUIShowListItem.StopDownload = true;

            // clear facade
            GUIControl.ClearControl(GUIWindowManager.ActiveWindow, facade.GetID);

            int itemId = 0;
            var showImages = new List<GUITmdbImage>();

            // filter shows
            if (TraktSettings.FilterTrendingOnDashboard)
                trendingItems = GUICommon.FilterTrendingShows(trendingItems);

            // Add each activity item to the facade
            foreach (var trendingItem in trendingItems.Take(trendingSettings.FacadeMaxItems))
            {
                if (trendingItem.Show == null)
                    continue;

                // add image for download
                var images = new GUITmdbImage { ShowImages = new TmdbShowImages { Id = trendingItem.Show.Ids.Tmdb } };
                showImages.Add(images);

                var item = new GUIShowListItem(trendingItem.Show.Title, GUIWindowManager.ActiveWindow);

                item.Label2 = trendingItem.Show.Year.ToString();
                item.TVTag = trendingItem;
                item.Show = trendingItem.Show;
                item.Images = images;
                item.ItemId = Int32.MaxValue - itemId;
                item.IconImage = GUIImageHandler.GetDefaultPoster(false);
                item.IconImageBig = GUIImageHandler.GetDefaultPoster();
                item.ThumbnailImage = GUIImageHandler.GetDefaultPoster();
                item.OnItemSelected += OnTrendingShowSelected;
                try
                {
                    facade.Add(item);
                }
                catch { }
                itemId++;
            }

            // Set Facade Layout
            facade.CurrentLayout = (GUIFacadeControl.Layout)Enum.Parse(typeof(GUIFacadeControl.Layout), trendingSettings.FacadeType);
            facade.SetVisibleFromSkinCondition();

            // set facade properties
            GUIUtils.SetProperty("#Trakt.Trending.Shows.Items", string.Format("{0} {1}", trendingItems.Count().ToString(), trendingItems.Count() > 1 ? Translation.SeriesPlural : Translation.Series));
            GUIUtils.SetProperty("#Trakt.Trending.Shows.PeopleCount", trendingItems.Sum(t => t.Watchers).ToString());
            GUIUtils.SetProperty("#Trakt.Trending.Shows.Description", string.Format(Translation.TrendingTVShowsPeople, trendingItems.Sum(t => t.Watchers).ToString(), trendingItems.Count().ToString()));

            // Download images Async and set to facade
            GUIShowListItem.StopDownload = false;
            GUIShowListItem.GetImages(showImages);
            
            TraktLogger.Debug("Finished Loading Trending Shows facade");
        }

        private string GetActivityImage(TraktActivity.Activity activity)
        {
            if (activity == null || string.IsNullOrEmpty(activity.Action))
                return string.Empty;

            string imageFilename = string.Empty;
            ActivityAction action = (ActivityAction)Enum.Parse(typeof(ActivityAction), activity.Action);

            switch (action)
            {
                case ActivityAction.checkin:
                case ActivityAction.watching:
                case ActivityAction.pause:
                    imageFilename = "traktActivityWatching.png";
                    break;

                case ActivityAction.seen:
                case ActivityAction.scrobble:
                    imageFilename = "traktActivityWatched.png";
                    break;

                case ActivityAction.collection:
                    imageFilename = "traktActivityCollected.png";
                    break;

                case ActivityAction.rating:
                    imageFilename = activity.Rating > 5 ? "traktActivityLove.png" : "traktActivityHate.png";
                    break;

                case ActivityAction.watchlist:
                    imageFilename = "traktActivityWatchlist.png";
                    break;

                case ActivityAction.shout:
                case ActivityAction.review:
                    imageFilename = "traktActivityShout.png";
                    break;

                case ActivityAction.item_added:
                case ActivityAction.created:
                case ActivityAction.updated:
                    imageFilename = "traktActivityList.png";
                    break;

                case ActivityAction.like:
                    imageFilename = "traktActivityLike.png";
                    break;

                case ActivityAction.hide_calendar:
                case ActivityAction.hide_recommendation:
                case ActivityAction.hide_collected_progress:
                case ActivityAction.hide_watched_progress:
                    imageFilename = "traktHide.png";
                    break;
            }

            return imageFilename;
        }

        private string GetAvatarImage(TraktActivity.Activity activity)
        {
            if (activity.User == null)
                return "defaultTraktUser.png";

            string filename = activity.User.Images.Avatar.LocalImageFilename(ArtworkType.Avatar);
            if (string.IsNullOrEmpty(filename) || !System.IO.File.Exists(filename))
            {
                filename = "defaultTraktUser.png";
            }
            return filename;
        }

        private string GetActivityShoutText(TraktActivity.Activity activity)
        {
            if (activity.Action != ActivityAction.shout.ToString()) return string.Empty;
            if (activity.Shout.IsSpoiler) return Translation.HiddenToPreventSpoilers;
            return activity.Shout.Text;
        }

        private IEnumerable<TraktMovieTrending> GetTrendingMovies(out bool isCached)
        {
            isCached = false;
            double timeSinceLastUpdate = DateTime.Now.Subtract(LastTrendingMovieUpdate).TotalMilliseconds;

            if (PreviousTrendingMovies == null || TraktSettings.DashboardTrendingPollInterval <= timeSinceLastUpdate)
            {
                TraktLogger.Debug("Getting current list of movies trending from trakt.tv");
                var trendingResult = TraktAPI.TraktAPI.GetTrendingMovies(1, TraktSettings.FilterTrendingOnDashboard ? 100 : TraktSkinSettings.MaxTrendingItems);
                if (trendingResult != null && trendingResult.Movies.Count() > 0)
                {
                    LastTrendingMovieUpdate = DateTime.Now;
                    PreviousTrendingMovies = trendingResult.Movies;
                }
            }
            else
            {
                TraktLogger.Debug("Getting cached list of movies trending");
                isCached = true;
                // update start interval
                int startInterval = (int)(TraktSettings.DashboardTrendingPollInterval - timeSinceLastUpdate);
                TrendingMoviesTimer.Change(startInterval, TraktSettings.DashboardTrendingPollInterval);
            }
            return PreviousTrendingMovies;
        }

        private IEnumerable<TraktShowTrending> GetTrendingShows(out bool isCached)
        {
            isCached = false;
            double timeSinceLastUpdate = DateTime.Now.Subtract(LastTrendingShowUpdate).TotalMilliseconds;

            if (PreviousTrendingShows == null || TraktSettings.DashboardTrendingPollInterval <= timeSinceLastUpdate)
            {
                TraktLogger.Debug("Getting current list of tv shows trending from trakt.tv");
                var trendingItems = TraktAPI.TraktAPI.GetTrendingShows(1, TraktSettings.FilterTrendingOnDashboard ? 100 : TraktSkinSettings.MaxTrendingItems);
                if (trendingItems != null && trendingItems.Shows.Count() > 0)
                {
                    LastTrendingShowUpdate = DateTime.Now;
                    PreviousTrendingShows = trendingItems.Shows;
                }
            }
            else
            {
                TraktLogger.Debug("Getting cached list of tv shows trending");
                isCached = true;
                // update start interval
                int startInterval = (int)(TraktSettings.DashboardTrendingPollInterval - timeSinceLastUpdate);
                TrendingShowsTimer.Change(startInterval, TraktSettings.DashboardTrendingPollInterval);
            }
            return PreviousTrendingShows;
        }

        /// <summary>
        /// gets the activity for the currently logged in user from 
        /// the local cache
        /// </summary>
        private TraktActivity GetMyActivityFromCache()
        {
            int i = 0;
            int maxActivityItems = TraktSkinSettings.DashboardActivityFacadeMaxItems;
            TraktActivity activity = new TraktActivity();

            // create activities from cached data
            activity.Timestamps = new TraktActivity.TraktTimestamps { Current = DateTime.UtcNow.ToEpoch() };
            activity.Activities = new List<TraktActivity.Activity>();

            TraktLogger.Debug("Getting current user cached activity");

            #region watched episodes
            if (TraktSettings.DashboardActivityFilter.Types.Episodes && TraktSettings.DashboardActivityFilter.Actions.Watched)
            {
                var watchedEpisodes = TraktCache.GetWatchedEpisodesFromTrakt(true);
                if (watchedEpisodes != null)
                {
                    foreach (var episode in watchedEpisodes.OrderByDescending(e => e.WatchedAt).Take(maxActivityItems))
                    {
                        var watchedEpActivity = new TraktActivity.Activity
                        {
                            Id = i++,
                            Action = ActivityAction.scrobble.ToString(),
                            Type = ActivityType.episode.ToString(),
                            Episode = new TraktEpisodeSummary
                            {
                                Ids = new TraktEpisodeId(),
                                Number = episode.Number,
                                Season = episode.Season
                            },
                            Show = new TraktShowSummary
                            {
                                Title = episode.ShowTitle,
                                Year = episode.ShowYear,
                                Ids = new TraktShowId
                                {
                                    Imdb = episode.ShowImdbId,
                                    Trakt = episode.ShowId,
                                    Tvdb = episode.ShowTvdbId
                                },
                            },
                            Timestamp = episode.WatchedAt,
                            User = GetUserProfile()
                        };

                        // add activity to the list
                        activity.Activities.Add(watchedEpActivity);
                    }
                }
            }
            #endregion

            #region watched movies
            if (TraktSettings.DashboardActivityFilter.Types.Movies && TraktSettings.DashboardActivityFilter.Actions.Watched)
            {
                var watchedMovies = TraktCache.GetWatchedMoviesFromTrakt(true);
                if (watchedMovies != null)
                {
                    foreach (var movie in watchedMovies.OrderByDescending(m => m.LastWatchedAt).Take(maxActivityItems))
                    {
                        var watchedEpActivity = new TraktActivity.Activity
                        {
                            Id = i++,
                            Action = ActivityAction.scrobble.ToString(),
                            Type = ActivityType.movie.ToString(),
                            Movie = new TraktMovieSummary
                            {
                                Ids = movie.Movie.Ids,
                                Title = movie.Movie.Title,
                                Year = movie.Movie.Year
                            },
                            Timestamp = movie.LastWatchedAt,
                            User = GetUserProfile()
                        };

                        // add activity to the list
                        activity.Activities.Add(watchedEpActivity);
                    }
                }
            }
            #endregion

            #region collected episodes
            if (TraktSettings.DashboardActivityFilter.Types.Episodes && TraktSettings.DashboardActivityFilter.Actions.Collected)
            {
                var collectedEpisodes = TraktCache.GetCollectedEpisodesFromTrakt(true);
                if (collectedEpisodes != null)
                {
                    foreach (var episode in collectedEpisodes.OrderByDescending(e => e.CollectedAt).Take(maxActivityItems))
                    {
                        var collectedEpActivity = new TraktActivity.Activity
                        {
                            Id = i++,
                            Action = ActivityAction.collection.ToString(),
                            Type = ActivityType.episode.ToString(),
                            Episodes = new List<TraktEpisodeSummary>
                            {
                                new TraktEpisodeSummary
                                {
                                    Ids = new TraktEpisodeId(),
                                    Number = episode.Number,
                                    Season = episode.Season
                                }
                            },
                            Show = new TraktShowSummary
                            {
                                Title = episode.ShowTitle,
                                Year = episode.ShowYear,
                                Ids = new TraktShowId
                                {
                                    Imdb = episode.ShowImdbId,
                                    Trakt = episode.ShowId,
                                    Tvdb = episode.ShowTvdbId
                                },
                            },
                            Timestamp = episode.CollectedAt,
                            User = GetUserProfile()
                        };

                        // add activity to the list
                        activity.Activities.Add(collectedEpActivity);
                    }
                }
            }
            #endregion

            #region collected movies
            if (TraktSettings.DashboardActivityFilter.Types.Movies && TraktSettings.DashboardActivityFilter.Actions.Collected)
            {
                var collectedMovies = TraktCache.GetCollectedMoviesFromTrakt(true);
                if (collectedMovies != null)
                {
                    foreach (var movie in collectedMovies.OrderByDescending(m => m.CollectedAt).Take(maxActivityItems))
                    {
                        var collectedEpActivity = new TraktActivity.Activity
                        {
                            Id = i++,
                            Action = ActivityAction.collection.ToString(),
                            Type = ActivityType.movie.ToString(),
                            Movie = new TraktMovieSummary
                            {
                                Ids = movie.Movie.Ids,
                                Title = movie.Movie.Title,
                                Year = movie.Movie.Year
                            },
                            Timestamp = movie.CollectedAt,
                            User = GetUserProfile()
                        };

                        // add activity to the list
                        activity.Activities.Add(collectedEpActivity);
                    }
                }
            }
            #endregion

            #region watchlisted episodes
            if (TraktSettings.DashboardActivityFilter.Types.Episodes && TraktSettings.DashboardActivityFilter.Actions.Watchlisted)
            {
                var watchlistedEpisodes = TraktCache.GetWatchlistedEpisodesFromTrakt(true);
                if (watchlistedEpisodes != null)
                {
                    foreach (var episode in watchlistedEpisodes.OrderByDescending(e => e.ListedAt).Take(maxActivityItems))
                    {
                        var watchlistedEpActivity = new TraktActivity.Activity
                        {
                            Id = i++,
                            Action = ActivityAction.watchlist.ToString(),
                            Type = ActivityType.episode.ToString(),
                            Episode = new TraktEpisodeSummary
                            {
                                Ids = episode.Episode.Ids,
                                Number = episode.Episode.Number,
                                Season = episode.Episode.Season,
                                Title = episode.Episode.Title
                            },
                            Show = new TraktShowSummary
                            {
                                Title = episode.Show.Title,
                                Year = episode.Show.Year,
                                Ids = episode.Show.Ids
                            },
                            Timestamp = episode.ListedAt,
                            User = GetUserProfile()
                        };

                        // add activity to the list
                        activity.Activities.Add(watchlistedEpActivity);
                    }
                }
            }
            #endregion

            #region watchlisted shows
            if (TraktSettings.DashboardActivityFilter.Types.Shows && TraktSettings.DashboardActivityFilter.Actions.Watchlisted)
            {
                var watchlistedShows = TraktCache.GetWatchlistedShowsFromTrakt(true);
                if (watchlistedShows != null)
                {
                    foreach (var show in watchlistedShows.OrderByDescending(e => e.ListedAt).Take(maxActivityItems))
                    {
                        var watchlistedShowActivity = new TraktActivity.Activity
                        {
                            Id = i++,
                            Action = ActivityAction.watchlist.ToString(),
                            Type = ActivityType.show.ToString(),
                            Show = new TraktShowSummary
                            {
                                Title = show.Show.Title,
                                Year = show.Show.Year,
                                Ids = show.Show.Ids
                            },
                            Timestamp = show.ListedAt,
                            User = GetUserProfile()
                        };

                        // add activity to the list
                        activity.Activities.Add(watchlistedShowActivity);
                    }
                }
            }
            #endregion

            #region watchlisted seasons
            if (TraktSettings.DashboardActivityFilter.Types.Seasons && TraktSettings.DashboardActivityFilter.Actions.Watchlisted)
            {
                var watchlistedSeasons = TraktCache.GetWatchlistedSeasonsFromTrakt(true);
                if (watchlistedSeasons != null)
                {
                    foreach (var item in watchlistedSeasons.OrderByDescending(e => e.ListedAt).Take(maxActivityItems))
                    {
                        var watchlistedSeasonActivity = new TraktActivity.Activity
                        {
                            Id = i++,
                            Action = ActivityAction.watchlist.ToString(),
                            Type = ActivityType.season.ToString(),
                            Show = new TraktShowSummary
                            {
                                Title = item.Show.Title,
                                Year = item.Show.Year,
                                Ids = item.Show.Ids
                            },
                            Season = new TraktSeasonSummary
                            {
                                Ids = item.Season.Ids,
                                Number = item.Season.Number
                            },
                            Timestamp = item.ListedAt,
                            User = GetUserProfile()
                        };

                        // add activity to the list
                        activity.Activities.Add(watchlistedSeasonActivity);
                    }
                }
            }
            #endregion

            #region watchlisted movies
            if (TraktSettings.DashboardActivityFilter.Types.Movies && TraktSettings.DashboardActivityFilter.Actions.Watchlisted)
            {
                var watchlistedMovies = TraktCache.GetWatchlistedMoviesFromTrakt(true);
                if (watchlistedMovies != null)
                {
                    foreach (var movie in watchlistedMovies.OrderByDescending(e => e.ListedAt).Take(maxActivityItems))
                    {
                        var watchlistedMovieActivity = new TraktActivity.Activity
                        {
                            Id = i++,
                            Action = ActivityAction.watchlist.ToString(),
                            Type = ActivityType.movie.ToString(),
                            Movie = new TraktMovieSummary
                            {
                                Ids = movie.Movie.Ids,
                                Title = movie.Movie.Title,
                                Year = movie.Movie.Year
                            },
                            Timestamp = movie.ListedAt,
                            User = GetUserProfile()
                        };

                        // add activity to the list
                        activity.Activities.Add(watchlistedMovieActivity);
                    }
                }
            }
            #endregion

            #region rated episodes
            if (TraktSettings.DashboardActivityFilter.Types.Episodes && TraktSettings.DashboardActivityFilter.Actions.Rated)
            {
                var ratedEpisodes = TraktCache.GetRatedEpisodesFromTrakt(true);
                if (ratedEpisodes != null)
                {
                    foreach (var episode in ratedEpisodes.OrderByDescending(e => e.RatedAt).Take(maxActivityItems))
                    {
                        var ratedEpActivity = new TraktActivity.Activity
                        {
                            Id = i++,
                            Action = ActivityAction.rating.ToString(),
                            Type = ActivityType.episode.ToString(),
                            Episode = new TraktEpisodeSummary
                            {
                                Ids = episode.Episode.Ids,
                                Number = episode.Episode.Number,
                                Season = episode.Episode.Season,
                                Title = episode.Episode.Title
                            },
                            Show = new TraktShowSummary
                            {
                                Title = episode.Show.Title,
                                Year = episode.Show.Year,
                                Ids = episode.Show.Ids
                            },
                            Rating = episode.Rating,
                            Timestamp = episode.RatedAt,
                            User = GetUserProfile()
                        };

                        // add activity to the list
                        activity.Activities.Add(ratedEpActivity);
                    }
                }
            }
            #endregion

            #region rated seasons
            if (TraktSettings.DashboardActivityFilter.Types.Seasons && TraktSettings.DashboardActivityFilter.Actions.Rated)
            {
                var ratedSeasons = TraktCache.GetRatedSeasonsFromTrakt(true);
                if (ratedSeasons != null)
                {
                    foreach (var season in ratedSeasons.OrderByDescending(e => e.RatedAt).Take(maxActivityItems))
                    {
                        var ratedShowActivity = new TraktActivity.Activity
                        {
                            Id = i++,
                            Action = ActivityAction.rating.ToString(),
                            Type = ActivityType.season.ToString(),
                            Show = new TraktShowSummary
                            {
                                Title = season.Show.Title,
                                Year = season.Show.Year,
                                Ids = season.Show.Ids
                            },
                            Season = new TraktSeasonSummary
                            {
                                Ids = season.Season.Ids ?? new TraktSeasonId(),
                                Number = season.Season.Number
                            },
                            Rating = season.Rating,
                            Timestamp = season.RatedAt,
                            User = GetUserProfile()
                        };

                        // add activity to the list
                        activity.Activities.Add(ratedShowActivity);
                    }
                }
            }
            #endregion

            #region rated shows
            if (TraktSettings.DashboardActivityFilter.Types.Shows && TraktSettings.DashboardActivityFilter.Actions.Rated)
            {
                var ratedShows = TraktCache.GetRatedShowsFromTrakt(true);
                if (ratedShows != null)
                {
                    foreach (var show in ratedShows.OrderByDescending(e => e.RatedAt).Take(maxActivityItems))
                    {
                        var ratedShowActivity = new TraktActivity.Activity
                        {
                            Id = i++,
                            Action = ActivityAction.rating.ToString(),
                            Type = ActivityType.show.ToString(),
                            Show = new TraktShowSummary
                            {
                                Title = show.Show.Title,
                                Year = show.Show.Year,
                                Ids = show.Show.Ids
                            },
                            Rating = show.Rating,
                            Timestamp = show.RatedAt,
                            User = GetUserProfile()
                        };

                        // add activity to the list
                        activity.Activities.Add(ratedShowActivity);
                    }
                }
            }
            #endregion

            #region rated movies
            if (TraktSettings.DashboardActivityFilter.Types.Movies && TraktSettings.DashboardActivityFilter.Actions.Rated)
            {
                var ratedMovies = TraktCache.GetRatedMoviesFromTrakt(true);
                if (ratedMovies != null)
                {
                    foreach (var movie in ratedMovies.OrderByDescending(e => e.RatedAt).Take(maxActivityItems))
                    {
                        var ratedMovieActivity = new TraktActivity.Activity
                        {
                            Id = i++,
                            Action = ActivityAction.rating.ToString(),
                            Type = ActivityType.movie.ToString(),
                            Movie = new TraktMovieSummary
                            {
                                Ids = movie.Movie.Ids,
                                Title = movie.Movie.Title,
                                Year = movie.Movie.Year
                            },
                            Rating = movie.Rating,
                            Timestamp = movie.RatedAt,
                            User = GetUserProfile()
                        };

                        // add activity to the list
                        activity.Activities.Add(ratedMovieActivity);
                    }
                }
            }
            #endregion
            
            #region lists
            if (TraktSettings.DashboardActivityFilter.Types.Lists)
            {
                var lists = TraktCache.GetCustomLists(true);
                if (lists != null)
                {
                    foreach (var list in lists.OrderByDescending(l => l.Key.UpdatedAt).Take(maxActivityItems))
                    {
                        var userList = list.Key;

                        if (TraktSettings.DashboardActivityFilter.Actions.Updated)
                        {
                            var listActivity = new TraktActivity.Activity
                            {
                                Id = i++,
                                Action = ActivityAction.updated.ToString(),
                                Type = ActivityType.list.ToString(),
                                List = userList,
                                Timestamp = list.Key.UpdatedAt,
                                User = GetUserProfile()
                            };

                            // add activity to the list
                            activity.Activities.Add(listActivity);
                        }

                        if (TraktSettings.DashboardActivityFilter.Actions.Added)
                        {
                            foreach (var listItem in list.Value.OrderByDescending(l => l.ListedAt).Take(maxActivityItems))
                            {
                                var listItemActivity = new TraktActivity.Activity
                                {
                                    Id = i++,
                                    Action = ActivityAction.item_added.ToString(),
                                    Type = listItem.Type,
                                    Timestamp = listItem.ListedAt,
                                    List = userList,
                                    Movie = listItem.Movie,
                                    Show = listItem.Show,
                                    Episode = listItem.Episode,
                                    Season = listItem.Season,
                                    Person = listItem.Person,
                                    User = GetUserProfile()
                                };

                                // add activity to the list
                                activity.Activities.Add(listItemActivity);
                            }
                        }
                    }
                }
            }
            #endregion

            #region commented episodes
            if (TraktSettings.DashboardActivityFilter.Types.Episodes && TraktSettings.DashboardActivityFilter.Actions.Commented)
            {
                var commentedEpisodes = TraktCache.GetCommentedEpisodesFromTrakt(true);
                if (commentedEpisodes != null)
                {
                    foreach (var comment in commentedEpisodes.OrderByDescending(c => c.Comment.CreatedAt).Take(maxActivityItems))
                    {
                        var commentedEpisodeActivity = new TraktActivity.Activity
                        {
                            Id = i++,
                            Action = comment.Comment.IsReview ? ActivityAction.review.ToString() : ActivityAction.shout.ToString(),
                            Type = comment.Type,
                            Show = new TraktShowSummary
                            {
                                Ids = comment.Show.Ids,
                                Title = comment.Show.Title,
                                Year = comment.Show.Year
                            },
                            Episode = new TraktEpisodeSummary
                            {
                                Ids = comment.Episode.Ids,
                                Number = comment.Episode.Number,
                                Season = comment.Episode.Season,
                                Title = comment.Episode.Title
                            },
                            Shout = comment.Comment,
                            Timestamp = comment.Comment.CreatedAt,
                            User = GetUserProfile()
                        };

                        // add activity to the list
                        activity.Activities.Add(commentedEpisodeActivity);
                    }
                }
            }
            #endregion

            #region commented seasons
            if (TraktSettings.DashboardActivityFilter.Types.Seasons && TraktSettings.DashboardActivityFilter.Actions.Commented)
            {
                var commentedSeasons = TraktCache.GetCommentedSeasonsFromTrakt(true);
                if (commentedSeasons != null)
                {
                    foreach (var comment in commentedSeasons.OrderByDescending(c => c.Comment.CreatedAt).Take(maxActivityItems))
                    {
                        var commentedSeasonActivity = new TraktActivity.Activity
                        {
                            Id = i++,
                            Action = comment.Comment.IsReview ? ActivityAction.review.ToString() : ActivityAction.shout.ToString(),
                            Type = comment.Type,
                            Show = new TraktShowSummary
                            {
                                Ids = comment.Show.Ids,
                                Title = comment.Show.Title,
                                Year = comment.Show.Year
                            },
                            Season = new TraktSeasonSummary
                            {
                                Ids = comment.Season.Ids,
                                Number = comment.Season.Number
                            },
                            Shout = comment.Comment,
                            Timestamp = comment.Comment.CreatedAt,
                            User = GetUserProfile()
                        };

                        // add activity to the list
                        activity.Activities.Add(commentedSeasonActivity);
                    }
                }
            }
            #endregion

            #region commented shows
            if (TraktSettings.DashboardActivityFilter.Types.Shows && TraktSettings.DashboardActivityFilter.Actions.Commented)
            {
                var commentedShows = TraktCache.GetCommentedShowsFromTrakt(true);
                if (commentedShows != null)
                {
                    foreach (var comment in commentedShows.OrderByDescending(c => c.Comment.CreatedAt).Take(maxActivityItems))
                    {
                        var commentedShowActivity = new TraktActivity.Activity
                        {
                            Id = i++,
                            Action = comment.Comment.IsReview ? ActivityAction.review.ToString() : ActivityAction.shout.ToString(),
                            Type = comment.Type,
                            Show = new TraktShowSummary
                            {
                                Ids = comment.Show.Ids,
                                Title = comment.Show.Title,
                                Year = comment.Show.Year
                            },
                            Shout = comment.Comment,
                            Timestamp = comment.Comment.CreatedAt,
                            User = GetUserProfile()
                        };

                        // add activity to the list
                        activity.Activities.Add(commentedShowActivity);
                    }
                }
            }
            #endregion

            #region commented movies
            if (TraktSettings.DashboardActivityFilter.Types.Movies && TraktSettings.DashboardActivityFilter.Actions.Commented)
            {
                var commentedMovies = TraktCache.GetCommentedMoviesFromTrakt(true);
                if (commentedMovies != null)
                {
                    foreach (var comment in commentedMovies.OrderByDescending(c => c.Comment.CreatedAt).Take(maxActivityItems))
                    {
                        var commentedMovieActivity = new TraktActivity.Activity
                        {
                            Id = i++,
                            Action = comment.Comment.IsReview ? ActivityAction.review.ToString() : ActivityAction.shout.ToString(),
                            Type = comment.Type,
                            Movie = new TraktMovieSummary
                            {
                                Ids = comment.Movie.Ids,
                                Title = comment.Movie.Title,
                                Year = comment.Movie.Year
                            },
                            Shout = comment.Comment,
                            Timestamp = comment.Comment.CreatedAt,
                            User = GetUserProfile()
                        };

                        // add activity to the list
                        activity.Activities.Add(commentedMovieActivity);
                    }
                }
            }
            #endregion

            #region commented lists
            if (TraktSettings.DashboardActivityFilter.Types.Lists && TraktSettings.DashboardActivityFilter.Actions.Commented)
            {
                var commentedLists = TraktCache.GetCommentedMoviesFromTrakt(true);
                if (commentedLists != null)
                {
                    foreach (var comment in commentedLists.OrderByDescending(c => c.Comment.CreatedAt).Take(maxActivityItems))
                    {
                        var commentedListActivity = new TraktActivity.Activity
                        {
                            Id = i++,
                            Action = comment.Comment.IsReview ? ActivityAction.review.ToString() : ActivityAction.shout.ToString(),
                            Type = comment.Type,
                            List = comment.List,
                            Shout = comment.Comment,
                            Timestamp = comment.Comment.CreatedAt,
                            User = GetUserProfile()
                        };

                        // add activity to the list
                        activity.Activities.Add(commentedListActivity);
                    }
                }
            }
            #endregion

            #region paused episodes
            if (TraktSettings.DashboardActivityFilter.Types.Episodes && TraktSettings.DashboardActivityFilter.Actions.Paused)
            {
                string lastEpisodeProcessedAt;
                var pausedEpisodes = TraktCache.GetPausedEpisodes(out lastEpisodeProcessedAt, true);
                if (pausedEpisodes != null)
                {
                    foreach (var pause in pausedEpisodes.OrderByDescending(e => e.PausedAt).Take(maxActivityItems))
                    {
                        var pausedEpisodeActivity = new TraktActivity.Activity
                        {
                            Id = i++,
                            Action = ActivityAction.pause.ToString(),
                            Type = ActivityType.episode.ToString(),
                            Show = new TraktShowSummary
                            {
                                Title = pause.Show.Title,
                                Year = pause.Show.Year,
                                Ids = pause.Show.Ids
                            },
                            Episode = new TraktEpisodeSummary
                            {
                                Ids = pause.Episode.Ids,
                                Number = pause.Episode.Number,
                                Season = pause.Episode.Season,
                                Title = pause.Episode.Title
                            },
                            Progress = pause.Progress,
                            Timestamp = pause.PausedAt,
                            User = GetUserProfile()
                        };

                        // add activity to the list
                        activity.Activities.Add(pausedEpisodeActivity);
                    }
                }
            }
            #endregion

            #region paused movies
            if (TraktSettings.DashboardActivityFilter.Types.Movies && TraktSettings.DashboardActivityFilter.Actions.Paused)
            {
                string lastMovieProcessedAt;
                var pausedMovies = TraktCache.GetPausedMovies(out lastMovieProcessedAt, true);
                if (pausedMovies != null)
                {
                    foreach (var pause in pausedMovies.OrderByDescending(e => e.PausedAt).Take(maxActivityItems))
                    {
                        var pausedMovieActivity = new TraktActivity.Activity
                        {
                            Id = i++,
                            Action = ActivityAction.pause.ToString(),
                            Type = ActivityType.movie.ToString(),
                            Movie = new TraktMovieSummary
                            {
                                Ids = pause.Movie.Ids,
                                Title = pause.Movie.Title,
                                Year = pause.Movie.Year
                            },
                            Progress = pause.Progress,
                            Timestamp = pause.PausedAt,
                            User = GetUserProfile()
                        };

                        // add activity to the list
                        activity.Activities.Add(pausedMovieActivity);
                    }
                }
            }
            #endregion

            #region liked comments
            if (TraktSettings.DashboardActivityFilter.Types.Comments && TraktSettings.DashboardActivityFilter.Actions.Liked)
            {
                var likedComments = TraktCache.GetLikedCommentsFromTrakt(true);
                if (likedComments != null)
                {
                    foreach (var like in likedComments.OrderByDescending(c => c.LikedAt).Take(maxActivityItems))
                    {
                        var likedCommentActivity = new TraktActivity.Activity
                        {
                            Id = i++,
                            Action = ActivityAction.like.ToString(),
                            Type = ActivityType.comment.ToString(),
                            Shout = like.Comment,
                            Timestamp = like.LikedAt,
                            User = GetUserProfile()
                        };

                        // add activity to the list
                        activity.Activities.Add(likedCommentActivity);
                    }
                }
            }
            #endregion

            #region liked lists
            if (TraktSettings.DashboardActivityFilter.Types.Lists && TraktSettings.DashboardActivityFilter.Actions.Liked)
            {
                var likedLists = TraktCache.GetLikedListsFromTrakt(true);
                if (likedLists != null)
                {
                    foreach (var like in likedLists.OrderByDescending(c => c.LikedAt).Take(maxActivityItems))
                    {
                        var likedListActivity = new TraktActivity.Activity
                        {
                            Id = i++,
                            Action = ActivityAction.like.ToString(),
                            Type = ActivityType.list.ToString(),
                            List = like.List,
                            Timestamp = like.LikedAt,
                            User = GetUserProfile()
                        };

                        // add activity to the list
                        activity.Activities.Add(likedListActivity);
                    }
                }
            }
            #endregion

            #region hidden shows calender
            if (TraktSettings.DashboardActivityFilter.Types.Shows && TraktSettings.DashboardActivityFilter.Actions.HiddenCalendarItems)
            {
                var hiddenShows = TraktCache.GetHiddenShowsFromTrakt(true);
                if (hiddenShows != null)
                {
                    foreach (var item in hiddenShows.Where(h => h.Section == "calendar").OrderByDescending(c => c.HiddenAt).Take(maxActivityItems))
                    {
                        var hiddenShowActivity = new TraktActivity.Activity
                        {
                            Id = i++,
                            Action = ActivityAction.hide_calendar.ToString(),
                            Type = ActivityType.show.ToString(),
                            Show = item.Show,
                            Timestamp = item.HiddenAt,
                            User = GetUserProfile()
                        };

                        // add activity to the list
                        activity.Activities.Add(hiddenShowActivity);
                    }
                }
            }
            #endregion

            #region hidden shows collected progress
            if (TraktSettings.DashboardActivityFilter.Types.Shows && TraktSettings.DashboardActivityFilter.Actions.HiddedCollectedProgress)
            {
                var hiddenShows = TraktCache.GetHiddenShowsFromTrakt(true);
                if (hiddenShows != null)
                {
                    foreach (var item in hiddenShows.Where(h => h.Section == "progress_collected").OrderByDescending(c => c.HiddenAt).Take(maxActivityItems))
                    {
                        var hiddenShowActivity = new TraktActivity.Activity
                        {
                            Id = i++,
                            Action = ActivityAction.hide_collected_progress.ToString(),
                            Type = ActivityType.show.ToString(),
                            Show = item.Show,
                            Timestamp = item.HiddenAt,
                            User = GetUserProfile()
                        };

                        // add activity to the list
                        activity.Activities.Add(hiddenShowActivity);
                    }
                }
            }
            #endregion

            #region hidden shows watched progress
            if (TraktSettings.DashboardActivityFilter.Types.Shows && TraktSettings.DashboardActivityFilter.Actions.HiddenWatchedProgress)
            {
                var hiddenShows = TraktCache.GetHiddenShowsFromTrakt(true);
                if (hiddenShows != null)
                {
                    foreach (var item in hiddenShows.Where(h => h.Section == "progress_watched").OrderByDescending(c => c.HiddenAt).Take(maxActivityItems))
                    {
                        var hiddenShowActivity = new TraktActivity.Activity
                        {
                            Id = i++,
                            Action = ActivityAction.hide_watched_progress.ToString(),
                            Type = ActivityType.show.ToString(),
                            Show = item.Show,
                            Timestamp = item.HiddenAt,
                            User = GetUserProfile()
                        };

                        // add activity to the list
                        activity.Activities.Add(hiddenShowActivity);
                    }
                }
            }
            #endregion

            #region hidden shows recommended
            if (TraktSettings.DashboardActivityFilter.Types.Shows && TraktSettings.DashboardActivityFilter.Actions.HiddenRecommendations)
            {
                var hiddenShows = TraktCache.GetHiddenShowsFromTrakt(true);
                if (hiddenShows != null)
                {
                    foreach (var item in hiddenShows.Where(h => h.Section == "recommendations").OrderByDescending(c => c.HiddenAt).Take(maxActivityItems))
                    {
                        var hiddenShowActivity = new TraktActivity.Activity
                        {
                            Id = i++,
                            Action = ActivityAction.hide_recommendation.ToString(),
                            Type = ActivityType.show.ToString(),
                            Show = item.Show,
                            Timestamp = item.HiddenAt,
                            User = GetUserProfile()
                        };

                        // add activity to the list
                        activity.Activities.Add(hiddenShowActivity);
                    }
                }
            }
            #endregion

            #region hidden seasons collected progress
            if (TraktSettings.DashboardActivityFilter.Types.Seasons && TraktSettings.DashboardActivityFilter.Actions.HiddedCollectedProgress)
            {
                var hiddenSeasons = TraktCache.GetHiddenSeasonsFromTrakt(true);
                if (hiddenSeasons != null)
                {
                    foreach (var item in hiddenSeasons.Where(h => h.Section == "progress_collected").OrderByDescending(c => c.HiddenAt).Take(maxActivityItems))
                    {
                        var hiddenSeasonActivity = new TraktActivity.Activity
                        {
                            Id = i++,
                            Action = ActivityAction.hide_collected_progress.ToString(),
                            Type = ActivityType.season.ToString(),
                            Season = item.Season,
                            Show = item.Show,
                            Timestamp = item.HiddenAt,
                            User = GetUserProfile()
                        };

                        // add activity to the list
                        activity.Activities.Add(hiddenSeasonActivity);
                    }
                }
            }
            #endregion

            #region hidden seasons watched progress
            if (TraktSettings.DashboardActivityFilter.Types.Seasons && TraktSettings.DashboardActivityFilter.Actions.HiddenWatchedProgress)
            {
                var hiddenSeasons = TraktCache.GetHiddenSeasonsFromTrakt(true);
                if (hiddenSeasons != null)
                {
                    foreach (var item in hiddenSeasons.Where(h => h.Section == "progress_watched").OrderByDescending(c => c.HiddenAt).Take(maxActivityItems))
                    {
                        var hiddenSeasonActivity = new TraktActivity.Activity
                        {
                            Id = i++,
                            Action = ActivityAction.hide_watched_progress.ToString(),
                            Type = ActivityType.season.ToString(),
                            Season = item.Season,
                            Show = item.Show,
                            Timestamp = item.HiddenAt,
                            User = GetUserProfile()
                        };

                        // add activity to the list
                        activity.Activities.Add(hiddenSeasonActivity);
                    }
                }
            }
            #endregion

            #region hidden movies calendar
            if (TraktSettings.DashboardActivityFilter.Types.Movies && TraktSettings.DashboardActivityFilter.Actions.HiddenCalendarItems)
            {
                var hiddenMovies = TraktCache.GetHiddenMoviesFromTrakt(true);
                if (hiddenMovies != null)
                {
                    foreach (var item in hiddenMovies.Where(h => h.Section == "calendar").OrderByDescending(h => h.HiddenAt).Take(maxActivityItems))
                    {
                        var hiddenMovieActivity = new TraktActivity.Activity
                        {
                            Id = i++,
                            Action = ActivityAction.hide_calendar.ToString(),
                            Type = ActivityType.movie.ToString(),
                            Movie = item.Movie,
                            Timestamp = item.HiddenAt,
                            User = GetUserProfile()
                        };

                        // add activity to the list
                        activity.Activities.Add(hiddenMovieActivity);
                    }
                }
            }
            #endregion

            #region hidden movies recommendations
            if (TraktSettings.DashboardActivityFilter.Types.Movies && TraktSettings.DashboardActivityFilter.Actions.HiddenRecommendations)
            {
                var hiddenMovies = TraktCache.GetHiddenMoviesFromTrakt(true);
                if (hiddenMovies != null)
                {
                    foreach (var item in hiddenMovies.Where(h => h.Section == "recommendations").OrderByDescending(h => h.HiddenAt).Take(maxActivityItems))
                    {
                        var hiddenMovieActivity = new TraktActivity.Activity
                        {
                            Id = i++,
                            Action = ActivityAction.hide_recommendation.ToString(),
                            Type = ActivityType.movie.ToString(),
                            Movie = item.Movie,
                            Timestamp = item.HiddenAt,
                            User = GetUserProfile()
                        };

                        // add activity to the list
                        activity.Activities.Add(hiddenMovieActivity);
                    }
                }
            }
            #endregion

            TraktLogger.Debug("Finished getting users cached activity");

            // sort by time inserted into library
            activity.Activities = activity.Activities.OrderByDescending(a => a.Timestamp).Take(TraktSkinSettings.DashboardActivityFacadeMaxItems).ToList();

            return activity;
        }

        private TraktActivity GetActivity(ActivityView activityView)
        {
            // if we're getting stuff locally for our own activity
            // there is no need to set the update animation nor get incremental updates
            if (activityView == ActivityView.me)
            {
                SetUpdateAnimation(false);
                PreviousActivity = GetMyActivityFromCache();
                return PreviousActivity;
            }

            SetUpdateAnimation(true);

            if (PreviousActivity == null || PreviousActivity.Activities == null || ActivityStartTime <= 0 || GetFullActivityLoad)
            {
                switch (activityView)
                {
                    case ActivityView.community:
                        //PreviousActivity = TraktAPI.TraktAPI.GetCommunityActivity();
                        break;

                    case ActivityView.followers:
                        //PreviousActivity = TraktAPI.TraktAPI.GetFollowersActivity();
                        break;

                    case ActivityView.following:
                        //PreviousActivity = TraktAPI.TraktAPI.GetFollowingActivity();
                        break;

                    case ActivityView.friends:
                        //PreviousActivity = TraktAPI.TraktAPI.GetFriendActivity(false);
                        break;

                    case ActivityView.friendsandme:
                        //PreviousActivity = TraktAPI.TraktAPI.GetFriendActivity(true);
                        break;

                    case ActivityView.me:
                        //PreviousActivity = GetMyActivityFromCache();
                        break;
                }
                GetFullActivityLoad = false;
            }
            else
            {
                TraktActivity incrementalActivity = null;

                // get latest incremental change using last current timestamp as start point
                switch (activityView)
                {
                    case ActivityView.community:
                        //incrementalActivity = TraktAPI.TraktAPI.GetCommunityActivity(null, null, ActivityStartTime, DateTime.UtcNow.ToEpoch());
                        break;

                    case ActivityView.followers:
                        //incrementalActivity = TraktAPI.TraktAPI.GetFollowersActivity(null, null, ActivityStartTime, DateTime.UtcNow.ToEpoch());
                        break;

                    case ActivityView.following:
                        //incrementalActivity = TraktAPI.TraktAPI.GetFollowingActivity(null, null, ActivityStartTime, DateTime.UtcNow.ToEpoch());
                        break;

                    case ActivityView.friends:
                        //incrementalActivity = TraktAPI.TraktAPI.GetFriendActivity(null, null, ActivityStartTime, DateTime.UtcNow.ToEpoch(), false);
                        break;

                    case ActivityView.friendsandme:
                        //incrementalActivity = TraktAPI.TraktAPI.GetFriendActivity(null, null, ActivityStartTime, DateTime.UtcNow.ToEpoch(), true);
                        break;

                    case ActivityView.me:
                        //incrementalActivity = GetMyActivityFromCache();
                        break;
                }
               
                // join the Previous request with the current
                if (incrementalActivity != null && incrementalActivity.Activities != null)
                {
                    PreviousActivity.Activities = incrementalActivity.Activities.Union(PreviousActivity.Activities).Take(TraktSkinSettings.DashboardActivityFacadeMaxItems).ToList();
                    PreviousActivity.Timestamps = incrementalActivity.Timestamps;
                }
            }

            // store current timestamp and only request incremental change next time
            if (PreviousActivity != null && PreviousActivity.Timestamps != null)
            {
                ActivityStartTime = PreviousActivity.Timestamps.Current;
            }

            SetUpdateAnimation(false);

            return PreviousActivity;
        }

        private bool IsDashBoardWindow()
        {
            bool hasDashBoard = false;

            if (TraktSkinSettings.DashBoardActivityWindows != null && TraktSkinSettings.DashBoardActivityWindows.Contains(GUIWindowManager.ActiveWindow.ToString()))
                hasDashBoard = true;
            if (TraktSkinSettings.DashboardTrendingCollection != null && TraktSkinSettings.DashboardTrendingCollection.Exists(d => d.MovieWindows.Contains(GUIWindowManager.ActiveWindow.ToString())))
                hasDashBoard = true;
            if (TraktSkinSettings.DashboardTrendingCollection != null && TraktSkinSettings.DashboardTrendingCollection.Exists(d=> d.TVShowWindows.Contains(GUIWindowManager.ActiveWindow.ToString())))
                hasDashBoard = true;

            return hasDashBoard;
        }

        private bool ShowActivityFilterActionsMenu()
        {
            var items = new List<MultiSelectionItem>();

            items.Add(new MultiSelectionItem
            {
                IsToggle = true,
                ItemID = ActivityAction.scrobble.ToString(),
                ItemTitle = Translation.HideWatched,
                ItemTitle2 = !TraktSettings.DashboardActivityFilter.Actions.Watched ? Translation.Yes : Translation.No
            });

            items.Add(new MultiSelectionItem
            {
                IsToggle = true,
                ItemID = ActivityAction.collection.ToString(),
                ItemTitle = Translation.HideCollected,
                ItemTitle2 = !TraktSettings.DashboardActivityFilter.Actions.Collected ? Translation.Yes : Translation.No
            });

            items.Add(new MultiSelectionItem
            {
                IsToggle = true,
                ItemID = ActivityAction.rating.ToString(),
                ItemTitle = Translation.HideRated,
                ItemTitle2 = !TraktSettings.DashboardActivityFilter.Actions.Rated ? Translation.Yes : Translation.No
            });

            items.Add(new MultiSelectionItem
            {
                IsToggle = true,
                ItemID = ActivityAction.watchlist.ToString(),
                ItemTitle = Translation.HideWatchlisted,
                ItemTitle2 = !TraktSettings.DashboardActivityFilter.Actions.Watchlisted ? Translation.Yes : Translation.No
            });

            items.Add(new MultiSelectionItem
            {
                IsToggle = true,
                ItemID = ActivityAction.pause.ToString(),
                ItemTitle = Translation.HidePaused,
                ItemTitle2 = !TraktSettings.DashboardActivityFilter.Actions.Paused ? Translation.Yes : Translation.No
            });

            items.Add(new MultiSelectionItem
            {
                IsToggle = true,
                ItemID = ActivityAction.shout.ToString(),
                ItemTitle = Translation.HideCommented,
                ItemTitle2 = !TraktSettings.DashboardActivityFilter.Actions.Commented ? Translation.Yes : Translation.No
            });

            items.Add(new MultiSelectionItem
            {
                IsToggle = true,
                ItemID = ActivityAction.like.ToString(),
                ItemTitle = Translation.HideLiked,
                ItemTitle2 = !TraktSettings.DashboardActivityFilter.Actions.Liked ? Translation.Yes : Translation.No
            });

            items.Add(new MultiSelectionItem
            {
                IsToggle = true,
                ItemID = ActivityAction.item_added.ToString(),
                ItemTitle = Translation.HideAdded,
                ItemTitle2 = !TraktSettings.DashboardActivityFilter.Actions.Added ? Translation.Yes : Translation.No
            });

            items.Add(new MultiSelectionItem
            {
                IsToggle = true,
                ItemID = ActivityAction.updated.ToString(),
                ItemTitle = Translation.HideUpdated,
                ItemTitle2 = !TraktSettings.DashboardActivityFilter.Actions.Updated ? Translation.Yes : Translation.No
            });

            items.Add(new MultiSelectionItem
            {
                IsToggle = true,
                ItemID = ActivityAction.hide_calendar.ToString(),
                ItemTitle = Translation.HideHiddenCalendarItems,
                ItemTitle2 = !TraktSettings.DashboardActivityFilter.Actions.HiddenCalendarItems ? Translation.Yes : Translation.No
            });

            items.Add(new MultiSelectionItem
            {
                IsToggle = true,
                ItemID = ActivityAction.hide_recommendation.ToString(),
                ItemTitle = Translation.HideHiddenRecommendations,
                ItemTitle2 = !TraktSettings.DashboardActivityFilter.Actions.HiddenRecommendations ? Translation.Yes : Translation.No
            });

            items.Add(new MultiSelectionItem
            {
                IsToggle = true,
                ItemID = ActivityAction.hide_collected_progress.ToString(),
                ItemTitle = Translation.HideHiddenCollectedProgress,
                ItemTitle2 = !TraktSettings.DashboardActivityFilter.Actions.HiddedCollectedProgress ? Translation.Yes : Translation.No
            });

            items.Add(new MultiSelectionItem
            {
                IsToggle = true,
                ItemID = ActivityAction.hide_watched_progress.ToString(),
                ItemTitle = Translation.HideHiddenWatchedProgress,
                ItemTitle2 = !TraktSettings.DashboardActivityFilter.Actions.HiddenWatchedProgress ? Translation.Yes : Translation.No
            });

            List<MultiSelectionItem> selectedItems = GUIUtils.ShowMultiSelectionDialog(Translation.Filters, items);
            if (selectedItems == null) return false;

            foreach (var item in selectedItems.Where(l => l.Selected == true))
            {
                // toggle state of all selected items
                switch ((ActivityAction)Enum.Parse(typeof(ActivityAction), item.ItemID, true))
                {
                    case ActivityAction.scrobble:
                        TraktSettings.DashboardActivityFilter.Actions.Watched = !TraktSettings.DashboardActivityFilter.Actions.Watched;
                        break;

                    case ActivityAction.collection:
                        TraktSettings.DashboardActivityFilter.Actions.Collected = !TraktSettings.DashboardActivityFilter.Actions.Collected;
                        break;

                    case ActivityAction.rating:
                        TraktSettings.DashboardActivityFilter.Actions.Rated = !TraktSettings.DashboardActivityFilter.Actions.Rated;
                        break;

                    case ActivityAction.watchlist:
                        TraktSettings.DashboardActivityFilter.Actions.Watchlisted = !TraktSettings.DashboardActivityFilter.Actions.Watchlisted;
                        break;

                    case ActivityAction.pause:
                        TraktSettings.DashboardActivityFilter.Actions.Paused = !TraktSettings.DashboardActivityFilter.Actions.Paused;
                        break;

                    case ActivityAction.shout:
                        TraktSettings.DashboardActivityFilter.Actions.Commented = !TraktSettings.DashboardActivityFilter.Actions.Commented;
                        break;

                    case ActivityAction.like:
                        TraktSettings.DashboardActivityFilter.Actions.Liked = !TraktSettings.DashboardActivityFilter.Actions.Liked;
                        break;

                    case ActivityAction.item_added:
                        TraktSettings.DashboardActivityFilter.Actions.Added = !TraktSettings.DashboardActivityFilter.Actions.Added;
                        break;

                    case ActivityAction.updated:
                        TraktSettings.DashboardActivityFilter.Actions.Updated = !TraktSettings.DashboardActivityFilter.Actions.Updated;
                        break;

                    case ActivityAction.hide_calendar:
                        TraktSettings.DashboardActivityFilter.Actions.HiddenCalendarItems = !TraktSettings.DashboardActivityFilter.Actions.HiddenCalendarItems;
                        break;

                    case ActivityAction.hide_recommendation:
                        TraktSettings.DashboardActivityFilter.Actions.HiddenRecommendations = !TraktSettings.DashboardActivityFilter.Actions.HiddenRecommendations;
                        break;

                    case ActivityAction.hide_collected_progress:
                        TraktSettings.DashboardActivityFilter.Actions.HiddedCollectedProgress = !TraktSettings.DashboardActivityFilter.Actions.HiddedCollectedProgress;
                        break;

                    case ActivityAction.hide_watched_progress:
                        TraktSettings.DashboardActivityFilter.Actions.HiddenWatchedProgress = !TraktSettings.DashboardActivityFilter.Actions.HiddenWatchedProgress;
                        break;
                }
            }

            return true;
        }

        private bool ShowActivityFilterTypesMenu()
        {
            var items = new List<MultiSelectionItem>();

            items.Add(new MultiSelectionItem
            {
                IsToggle = true,
                ItemID = ActivityType.movie.ToString(),
                ItemTitle = Translation.HideMovies,
                ItemTitle2 = !TraktSettings.DashboardActivityFilter.Types.Movies ? Translation.Yes : Translation.No
            });

            items.Add(new MultiSelectionItem
            {
                IsToggle = true,
                ItemID = ActivityType.show.ToString(),
                ItemTitle = Translation.HideShows,
                ItemTitle2 = !TraktSettings.DashboardActivityFilter.Types.Shows ? Translation.Yes : Translation.No
            });

            items.Add(new MultiSelectionItem
            {
                IsToggle = true,
                ItemID = ActivityType.season.ToString(),
                ItemTitle = Translation.HideSeasons,
                ItemTitle2 = !TraktSettings.DashboardActivityFilter.Types.Seasons ? Translation.Yes : Translation.No
            });

            items.Add(new MultiSelectionItem
            {
                IsToggle = true,
                ItemID = ActivityType.episode.ToString(),
                ItemTitle = Translation.HideEpisodes,
                ItemTitle2 = !TraktSettings.DashboardActivityFilter.Types.Episodes ? Translation.Yes : Translation.No
            });

            items.Add(new MultiSelectionItem
            {
                IsToggle = true,
                ItemID = ActivityType.comment.ToString(),
                ItemTitle = Translation.HideComments,
                ItemTitle2 = !TraktSettings.DashboardActivityFilter.Types.Comments ? Translation.Yes : Translation.No
            });

            items.Add(new MultiSelectionItem
            {
                IsToggle = true,
                ItemID = ActivityType.list.ToString(),
                ItemTitle = Translation.HideLists,
                ItemTitle2 = !TraktSettings.DashboardActivityFilter.Types.Lists ? Translation.Yes : Translation.No
            });

            items.Add(new MultiSelectionItem
            {
                IsToggle = true,
                ItemID = ActivityType.person.ToString(),
                ItemTitle = Translation.HidePeople,
                ItemTitle2 = !TraktSettings.DashboardActivityFilter.Types.People ? Translation.Yes : Translation.No
            });

            List<MultiSelectionItem> selectedItems = GUIUtils.ShowMultiSelectionDialog(Translation.Filters, items);
            if (selectedItems == null) return false;

            foreach (var item in selectedItems.Where(l => l.Selected == true))
            {
                // toggle state of all selected items
                switch ((ActivityType)Enum.Parse(typeof(ActivityType), item.ItemID, true))
                {
                    case ActivityType.movie:
                        TraktSettings.DashboardActivityFilter.Types.Movies = !TraktSettings.DashboardActivityFilter.Types.Movies;
                        break;

                    case ActivityType.show:
                        TraktSettings.DashboardActivityFilter.Types.Shows = !TraktSettings.DashboardActivityFilter.Types.Shows;
                        break;

                    case ActivityType.season:
                        TraktSettings.DashboardActivityFilter.Types.Seasons = !TraktSettings.DashboardActivityFilter.Types.Seasons;
                        break;

                    case ActivityType.episode:
                        TraktSettings.DashboardActivityFilter.Types.Episodes = !TraktSettings.DashboardActivityFilter.Types.Episodes;
                        break;

                    case ActivityType.list:
                        TraktSettings.DashboardActivityFilter.Types.Lists = !TraktSettings.DashboardActivityFilter.Types.Lists;
                        break;

                    case ActivityType.comment:
                        TraktSettings.DashboardActivityFilter.Types.Comments = !TraktSettings.DashboardActivityFilter.Types.Comments;
                        break;

                    case ActivityType.person:
                        TraktSettings.DashboardActivityFilter.Types.People = !TraktSettings.DashboardActivityFilter.Types.People;
                        break;
                }
            }

            return true;
        }

        private void ShowTrendingShowsContextMenu()
        {
            var trendingShowsFacade = GetFacade((int)TraktDashboardControls.TrendingShowsFacade);
            if (trendingShowsFacade == null) return;

            var dlg = (IDialogbox)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
            if (dlg == null) return;

            dlg.Reset();
            dlg.SetHeading(GUIUtils.PluginName());

            var selectedItem = trendingShowsFacade.SelectedListItem as GUIShowListItem;
            var selectedTrendingItem = selectedItem.TVTag as TraktShowTrending;

            GUICommon.CreateShowsContextMenu(ref dlg, selectedTrendingItem.Show, true);

            // Show Context Menu
            dlg.DoModal(GUIWindowManager.ActiveWindow);
            if (dlg.SelectedId < 0) return;

            switch (dlg.SelectedId)
            {
                case ((int)MediaContextMenuItem.AddToWatchList):
                    TraktHelper.AddShowToWatchList(selectedTrendingItem.Show);
                    OnTrendingShowSelected(selectedItem, trendingShowsFacade);
                    (selectedItem as GUIShowListItem).Images.NotifyPropertyChanged("Poster");
                    break;

                case ((int)MediaContextMenuItem.ShowSeasonInfo):
                    GUIWindowManager.ActivateWindow((int)TraktGUIWindows.ShowSeasons, selectedTrendingItem.Show.ToJSON());
                    break;

                case ((int)MediaContextMenuItem.MarkAsWatched):
                    GUICommon.MarkShowAsWatched(selectedTrendingItem.Show);
                    break;

                case ((int)MediaContextMenuItem.AddToLibrary):
                    GUICommon.AddShowToCollection(selectedTrendingItem.Show);
                    break;

                case ((int)MediaContextMenuItem.RemoveFromWatchList):
                    TraktHelper.RemoveShowFromWatchList(selectedTrendingItem.Show);
                    OnTrendingShowSelected(selectedItem, trendingShowsFacade);
                    (selectedItem as GUIShowListItem).Images.NotifyPropertyChanged("Poster");
                    break;

                case ((int)MediaContextMenuItem.AddToList):
                    TraktHelper.AddRemoveShowInUserList(selectedTrendingItem.Show, false);
                    break;

                case ((int)MediaContextMenuItem.Related):
                    TraktHelper.ShowRelatedShows(selectedTrendingItem.Show);
                    break;

                case ((int)MediaContextMenuItem.Rate):
                    GUICommon.RateShow(selectedTrendingItem.Show);
                    OnTrendingShowSelected(selectedItem, trendingShowsFacade);
                    (selectedItem as GUIShowListItem).Images.NotifyPropertyChanged("Poster");
                    break;

                case ((int)MediaContextMenuItem.Shouts):
                    TraktHelper.ShowTVShowShouts(selectedTrendingItem.Show);
                    break;

                case ((int)MediaContextMenuItem.Cast):
                    GUICreditsShow.Show = selectedTrendingItem.Show;
                    GUICreditsShow.Type = GUICreditsShow.CreditType.Cast;
                    GUICreditsShow.Fanart = TmdbCache.GetShowBackdropFilename(selectedItem.Images.ShowImages);
                    GUIWindowManager.ActivateWindow((int)TraktGUIWindows.CreditsShow);
                    break;

                case ((int)MediaContextMenuItem.Crew):
                    GUICreditsShow.Show = selectedTrendingItem.Show;
                    GUICreditsShow.Type = GUICreditsShow.CreditType.Crew;
                    GUICreditsShow.Fanart = TmdbCache.GetShowBackdropFilename(selectedItem.Images.ShowImages);
                    GUIWindowManager.ActivateWindow((int)TraktGUIWindows.CreditsShow);
                    break;

                case ((int)MediaContextMenuItem.Filters):
                    if (GUICommon.ShowTVShowFiltersMenu())
                        LoadTrendingShows(true);
                    break;

                case ((int)MediaContextMenuItem.Trailers):
                    GUICommon.ShowTVShowTrailersMenu(selectedTrendingItem.Show);
                    break;

                case ((int)MediaContextMenuItem.SearchWithMpNZB):
                    string loadingParam = string.Format("search:{0}", selectedTrendingItem.Show.Title);
                    GUIWindowManager.ActivateWindow((int)ExternalPluginWindows.MpNZB, loadingParam);
                    break;

                case ((int)MediaContextMenuItem.SearchTorrent):
                    string loadPar = selectedTrendingItem.Show.Title;
                    GUIWindowManager.ActivateWindow((int)ExternalPluginWindows.MyTorrents, loadPar);
                    break;

                default:
                    break;
            }
        }

        private void ShowTrendingMoviesContextMenu()
        {
            var trendingMoviesFacade = GetFacade((int)TraktDashboardControls.TrendingMoviesFacade);
            if (trendingMoviesFacade == null) return;

            var dlg = (IDialogbox)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
            if (dlg == null) return;

            dlg.Reset();
            dlg.SetHeading(GUIUtils.PluginName());

            var selectedItem = trendingMoviesFacade.SelectedListItem as GUIMovieListItem;
            var selectedTrendingItem = selectedItem.TVTag as TraktMovieTrending;

            GUICommon.CreateMoviesContextMenu(ref dlg, selectedTrendingItem.Movie, true);

            // Show Context Menu
            dlg.DoModal(GUIWindowManager.ActiveWindow);
            if (dlg.SelectedId < 0) return;

            switch (dlg.SelectedId)
            {
                case ((int)MediaContextMenuItem.MarkAsWatched):
                    TraktHelper.AddMovieToWatchHistory(selectedTrendingItem.Movie);
                    selectedItem.IsPlayed = true;
                    OnTrendingMovieSelected(selectedItem, trendingMoviesFacade);
                    (selectedItem as GUIMovieListItem).Images.NotifyPropertyChanged("Poster");
                    break;

                case ((int)MediaContextMenuItem.MarkAsUnWatched):
                    TraktHelper.RemoveMovieFromWatchHistory(selectedTrendingItem.Movie);
                    selectedItem.IsPlayed = false;
                    OnTrendingMovieSelected(selectedItem, trendingMoviesFacade);
                    (selectedItem as GUIMovieListItem).Images.NotifyPropertyChanged("Poster");
                    break;

                case ((int)MediaContextMenuItem.AddToWatchList):
                    TraktHelper.AddMovieToWatchList(selectedTrendingItem.Movie, true);
                    OnTrendingMovieSelected(selectedItem, trendingMoviesFacade);
                    (selectedItem as GUIMovieListItem).Images.NotifyPropertyChanged("Poster");
                    break;

                case ((int)MediaContextMenuItem.RemoveFromWatchList):
                    TraktHelper.RemoveMovieFromWatchList(selectedTrendingItem.Movie, true);
                    OnTrendingMovieSelected(selectedItem, trendingMoviesFacade);
                    (selectedItem as GUIMovieListItem).Images.NotifyPropertyChanged("Poster");
                    break;

                case ((int)MediaContextMenuItem.AddToList):
                    TraktHelper.AddRemoveMovieInUserList(selectedTrendingItem.Movie, false);
                    break;

                case ((int)MediaContextMenuItem.Filters):
                    if (GUICommon.ShowMovieFiltersMenu())
                        LoadTrendingMovies(true);
                    break;

                case ((int)MediaContextMenuItem.AddToLibrary):
                    TraktHelper.AddMovieToCollection(selectedTrendingItem.Movie);
                    OnTrendingMovieSelected(selectedItem, trendingMoviesFacade);
                    (selectedItem as GUIMovieListItem).Images.NotifyPropertyChanged("Poster");
                    break;

                case ((int)MediaContextMenuItem.RemoveFromLibrary):
                    TraktHelper.RemoveMovieFromCollection(selectedTrendingItem.Movie);
                    OnTrendingMovieSelected(selectedItem, trendingMoviesFacade);
                    (selectedItem as GUIMovieListItem).Images.NotifyPropertyChanged("Poster");
                    break;

                case ((int)MediaContextMenuItem.Related):
                    TraktHelper.ShowRelatedMovies(selectedTrendingItem.Movie);
                    break;

                case ((int)MediaContextMenuItem.Rate):
                    GUICommon.RateMovie(selectedTrendingItem.Movie);
                    OnTrendingMovieSelected(selectedItem, trendingMoviesFacade);
                    (selectedItem as GUIMovieListItem).Images.NotifyPropertyChanged("Poster");
                    break;

                case ((int)MediaContextMenuItem.Shouts):
                    TraktHelper.ShowMovieShouts(selectedTrendingItem.Movie);
                    break;

                case ((int)MediaContextMenuItem.Cast):
                    GUICreditsMovie.Movie = selectedTrendingItem.Movie;
                    GUICreditsMovie.Type = GUICreditsMovie.CreditType.Cast;
                    GUICreditsMovie.Fanart = TmdbCache.GetMovieBackdropFilename(selectedItem.Images.MovieImages);
                    GUIWindowManager.ActivateWindow((int)TraktGUIWindows.CreditsMovie);
                    break;

                case ((int)MediaContextMenuItem.Crew):
                    GUICreditsMovie.Movie = selectedTrendingItem.Movie;
                    GUICreditsMovie.Type = GUICreditsMovie.CreditType.Crew;
                    GUICreditsMovie.Fanart = TmdbCache.GetMovieBackdropFilename(selectedItem.Images.MovieImages);
                    GUIWindowManager.ActivateWindow((int)TraktGUIWindows.CreditsMovie);
                    break;

                case ((int)MediaContextMenuItem.Trailers):
                    GUICommon.ShowMovieTrailersMenu(selectedTrendingItem.Movie);
                    break;

                case ((int)MediaContextMenuItem.SearchWithMpNZB):
                    string loadingParam = string.Format("search:{0}", selectedTrendingItem.Movie.Title);
                    GUIWindowManager.ActivateWindow((int)ExternalPluginWindows.MpNZB, loadingParam);
                    break;

                case ((int)MediaContextMenuItem.SearchTorrent):
                    string loadPar = selectedTrendingItem.Movie.Title;
                    GUIWindowManager.ActivateWindow((int)ExternalPluginWindows.MyTorrents, loadPar);
                    break;

                default:
                    break;
            }
        }

        private bool ShowActivityViewMenu()
        {
            var dlg = (IDialogbox)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
            if (dlg == null) return false;

            dlg.Reset();
            dlg.SetHeading(Translation.View);

            GUIListItem listItem = null;

            listItem = new GUIListItem(Translation.Community);
            dlg.Add(listItem);
            listItem.ItemId = (int)ActivityView.community;

            listItem = new GUIListItem(Translation.Followers);
            dlg.Add(listItem);
            listItem.ItemId = (int)ActivityView.followers;

            listItem = new GUIListItem(Translation.Following);
            dlg.Add(listItem);
            listItem.ItemId = (int)ActivityView.following;

            listItem = new GUIListItem(Translation.Friends);
            dlg.Add(listItem);
            listItem.ItemId = (int)ActivityView.friends;

            listItem = new GUIListItem(Translation.FriendsAndMe);
            dlg.Add(listItem);
            listItem.ItemId = (int)ActivityView.friendsandme;

            listItem = new GUIListItem(Translation.Me);
            dlg.Add(listItem);
            listItem.ItemId = (int)ActivityView.me;

            // Show Context Menu
            dlg.DoModal(GUIWindowManager.ActiveWindow);
            if (dlg.SelectedId < 0) return false;

            //TODO: API does not yet support activity views as per v1
            if (dlg.SelectedId != (int)ActivityView.me)
            {
                GUIUtils.ShowNotifyDialog(GUIUtils.PluginName(), Translation.FeatureNotAvailable);
                return false;
            }

            TraktSettings.ActivityStreamView = dlg.SelectedId;
            GUIUtils.SetProperty("#Trakt.Activity.Description", GetActivityDescription((ActivityView)TraktSettings.ActivityStreamView));
            return true;
        }

        private void ShowActivityContextMenu()
        {
            var activityFacade = GetFacade((int)TraktDashboardControls.ActivityFacade);
            if (activityFacade == null) return;

            var dlg = (IDialogbox)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
            if (dlg == null) return;

            dlg.Reset();
            dlg.SetHeading(GUIUtils.PluginName());

            GUIListItem listItem = null;

            listItem = new GUIListItem(Translation.ActivityFilterTypes);
            dlg.Add(listItem);
            listItem.ItemId = (int)ActivityContextMenuItem.FilterTypes;

            listItem = new GUIListItem(Translation.ActivityFilterActions);
            dlg.Add(listItem);
            listItem.ItemId = (int)ActivityContextMenuItem.FilterActions;

            // activity view menu
            // currently not supported
            listItem = new GUIListItem(Translation.ChangeView);
            dlg.Add(listItem);
            listItem.ItemId = (int)ActivityContextMenuItem.ChangeView;

            var activity = activityFacade.SelectedListItem.TVTag as TraktActivity.Activity;

            if (activity != null && !string.IsNullOrEmpty(activity.Action) && !string.IsNullOrEmpty(activity.Type))
            {
                // userprofile - only load for unprotected users
                if (activity.User != null && !activity.User.IsPrivate && TraktSettings.ActivityStreamView != (int)ActivityView.me)
                {
                    listItem = new GUIListItem(Translation.UserProfile);
                    dlg.Add(listItem);
                    listItem.ItemId = (int)ActivityContextMenuItem.UserProfile;
                }

                if (((ActivityView)TraktSettings.ActivityStreamView == ActivityView.community ||
                     (ActivityView)TraktSettings.ActivityStreamView == ActivityView.followers) && !((activityFacade.SelectedListItem as GUIUserListItem).IsFollowed))
                {
                    // allow user to follow person
                    listItem = new GUIListItem(Translation.Follow);
                    dlg.Add(listItem);
                    listItem.ItemId = (int)ActivityContextMenuItem.FollowUser;
                }

                // if selected activity is an episode, season or show, add 'Season Info'
                if (activity.Show != null)
                {
                    listItem = new GUIListItem(Translation.ShowSeasonInfo);
                    dlg.Add(listItem);
                    listItem.ItemId = (int)ActivityContextMenuItem.ShowSeasonInfo;
                }

                // get a list of common actions to perform on the selected item
                if (activity.Movie != null || activity.Show != null)
                {
                    var listItems = GUICommon.GetContextMenuItemsForActivity(activity);
                    foreach (var item in listItems)
                    {
                        int itemId = item.ItemId;
                        dlg.Add(item);
                        item.ItemId = itemId;
                    }
                }

                // if selected activity is a 'like', show unlike item
                // like list
                if (activity.Action == "like" && TraktSettings.ActivityStreamView == (int)ActivityView.me)
                {
                    listItem = new GUIListItem(Translation.UnLike);
                    dlg.Add(listItem);
                    listItem.ItemId = (int)ActivityContextMenuItem.Unlike;
                }
            }

            // Show Context Menu
            dlg.DoModal(GUIWindowManager.ActiveWindow);
            if (dlg.SelectedId < 0) return;

            switch (dlg.SelectedId)
            {
                case ((int)ActivityContextMenuItem.FilterActions):
                    if (ShowActivityFilterActionsMenu())
                    {
                        ReloadActivityView = true;
                        StartActivityPolling();
                    }
                    break;

                case ((int)ActivityContextMenuItem.FilterTypes):
                    if (ShowActivityFilterTypesMenu())
                    {
                        ReloadActivityView = true;
                        StartActivityPolling();
                    }
                    break;

                case ((int)ActivityContextMenuItem.ChangeView):
                    if (ShowActivityViewMenu())
                    {
                        GetFullActivityLoad = true;
                        StartActivityPolling();
                    }
                    else
                    {
                        ShowActivityContextMenu();
                        return;
                    }
                    break;

                case ((int)ActivityContextMenuItem.UserProfile):
                    GUIUserProfile.CurrentUser = activity.User.Username;
                    GUIWindowManager.ActivateWindow((int)TraktGUIWindows.UserProfile);
                    break;

                case ((int)ActivityContextMenuItem.FollowUser):
                    if (GUIUtils.ShowYesNoDialog(Translation.Network, string.Format(Translation.SendFollowRequest, activity.User.Username), true))
                    {
                        GUINetwork.FollowUser(activity.User);
                        GUINetwork.ClearCache();
                        (activityFacade.SelectedListItem as GUIUserListItem).IsFollowed = true;
                    }
                    break;
                case ((int)ActivityContextMenuItem.ShowSeasonInfo):
                    GUIWindowManager.ActivateWindow((int)TraktGUIWindows.ShowSeasons, activity.Show.ToJSON());
                    break;

                case ((int)ActivityContextMenuItem.AddToList):
                    if (activity.Movie != null)
                        TraktHelper.AddRemoveMovieInUserList(activity.Movie, false);
                    else if (activity.Episode != null)
                        TraktHelper.AddRemoveEpisodeInUserList(activity.Episode, false);
                    else if (activity.Season != null)
                        TraktHelper.AddRemoveSeasonInUserList(activity.Season, false);
                    else
                        TraktHelper.AddRemoveShowInUserList(activity.Show, false);
                    break;

                case ((int)ActivityContextMenuItem.AddToWatchList):
                    if (activity.Movie != null)
                        TraktHelper.AddMovieToWatchList(activity.Movie, true);
                    else if (activity.Episode != null)
                        TraktHelper.AddEpisodeToWatchList(activity.Show, activity.Episode);
                    else if (activity.Episodes != null && activity.Episodes.Count == 1)
                        TraktHelper.AddEpisodeToWatchList(activity.Show, activity.Episodes.First());
                    else if (activity.Season != null)
                        TraktHelper.AddSeasonToWatchList(activity.Show, activity.Season.Number);
                    else
                        TraktHelper.AddShowToWatchList(activity.Show);
                    break;

                case ((int)ActivityContextMenuItem.RemoveFromWatchList):
                    if (activity.Movie != null)
                        TraktHelper.RemoveMovieFromWatchList(activity.Movie, true);
                    else if (activity.Episode != null)
                        TraktHelper.RemoveEpisodeFromWatchList(activity.Show, activity.Episode);
                    else if (activity.Episodes != null && activity.Episodes.Count == 1)
                        TraktHelper.RemoveEpisodeFromWatchList(activity.Show, activity.Episodes.First());
                    else if (activity.Season != null)
                        TraktHelper.RemoveSeasonFromWatchList(activity.Show, activity.Season.Number);
                    else
                        TraktHelper.RemoveShowFromWatchList(activity.Show);

                    // force reload of activity view as we only check if the most recent item has changed
                    ReloadActivityView = true;
                    break;

                case ((int)ActivityContextMenuItem.MarkAsWatched):
                    if (activity.Movie != null)
                        TraktHelper.AddMovieToWatchHistory(activity.Movie);
                    else if (activity.Episode != null)
                        TraktHelper.AddEpisodeToWatchHistory(activity.Show, activity.Episode);
                    else if (activity.Episodes != null && activity.Episodes.Count == 1)
                        TraktHelper.AddEpisodeToWatchHistory(activity.Show, activity.Episodes.First());
                    break;

                case ((int)ActivityContextMenuItem.MarkAsUnwatched):
                    if (activity.Movie != null)
                        TraktHelper.RemoveMovieFromWatchHistory(activity.Movie);
                    else if (activity.Episode != null)
                        TraktHelper.RemoveEpisodeFromWatchHistory(activity.Show, activity.Episode);
                    else if (activity.Episodes != null && activity.Episodes.Count == 1)
                        TraktHelper.RemoveEpisodeFromWatchHistory(activity.Show, activity.Episodes.First());

                    ReloadActivityView = true;
                    break;

                case ((int)ActivityContextMenuItem.AddToCollection):
                    if (activity.Movie != null)
                        TraktHelper.AddMovieToCollection(activity.Movie);
                    else if (activity.Episode != null)
                        TraktHelper.AddEpisodeToCollection(activity.Show, activity.Episode);
                    else if (activity.Episodes != null && activity.Episodes.Count == 1)
                        TraktHelper.AddEpisodeToCollection(activity.Show, activity.Episodes.First());
                    break;

                case ((int)ActivityContextMenuItem.RemoveFromCollection):
                     if (activity.Movie != null)
                        TraktHelper.RemoveMovieFromCollection(activity.Movie);
                    else if (activity.Episode != null)
                        TraktHelper.RemoveEpisodeFromCollection(activity.Show, activity.Episode);
                    else if (activity.Episodes != null && activity.Episodes.Count == 1)
                        TraktHelper.RemoveEpisodeFromCollection(activity.Show, activity.Episodes.First());

                    ReloadActivityView = true;
                    break;

                case ((int)ActivityContextMenuItem.Shouts):
                    if (activity.Movie != null)
                        TraktHelper.ShowMovieShouts(activity.Movie);
                    else if (activity.Episode != null)
                        TraktHelper.ShowEpisodeShouts(activity.Show, activity.Episode);
                    else if (activity.Episodes != null && activity.Episodes.Count == 1)
                        TraktHelper.ShowEpisodeShouts(activity.Show, activity.Episodes.First());
                    else
                        TraktHelper.ShowTVShowShouts(activity.Show);
                    break;

                case ((int)ActivityContextMenuItem.Rate):
                    if (activity.Movie != null)
                        GUICommon.RateMovie(activity.Movie);
                    else if (activity.Episode != null)
                        GUICommon.RateEpisode(activity.Show, activity.Episode);
                    else if (activity.Episodes != null && activity.Episodes.Count == 1)
                        GUICommon.RateEpisode(activity.Show, activity.Episodes.First());
                    else if (activity.Season != null)
                        GUICommon.RateSeason(activity.Show, activity.Season);
                    else
                        GUICommon.RateShow(activity.Show);
                    break;

                case (int)ActivityContextMenuItem.Unlike:
                    if (activity.Shout != null)
                        GUICommon.UnLikeComment(activity.Shout);
                    else if (activity.List != null)
                        GUICommon.UnLikeList(activity.List, "me");

                    ReloadActivityView = true;
                    break;

                case ((int)ActivityContextMenuItem.Cast):
                    if (activity.Movie != null)
                    {
                        var images = TmdbCache.GetMovieImages(activity.Movie.Ids.Tmdb, true);

                        GUICreditsMovie.Movie = activity.Movie;
                        GUICreditsMovie.Type = GUICreditsMovie.CreditType.Cast;
                        GUICreditsMovie.Fanart = TmdbCache.GetMovieBackdropFilename(images);
                        GUIWindowManager.ActivateWindow((int)TraktGUIWindows.CreditsMovie);
                    }
                    else if (activity.Show != null)
                    {
                        var images = TmdbCache.GetShowImages(activity.Show.Ids.Tmdb, true);

                        GUICreditsShow.Show = activity.Show;
                        GUICreditsShow.Type = GUICreditsShow.CreditType.Cast;
                        GUICreditsShow.Fanart = TmdbCache.GetShowBackdropFilename(images);
                        GUIWindowManager.ActivateWindow((int)TraktGUIWindows.CreditsShow);
                    }
                    break;

                case ((int)ActivityContextMenuItem.Crew):
                    if (activity.Movie != null)
                    {
                        var images = TmdbCache.GetMovieImages(activity.Movie.Ids.Tmdb, true);

                        GUICreditsMovie.Movie = activity.Movie;
                        GUICreditsMovie.Type = GUICreditsMovie.CreditType.Crew;
                        GUICreditsMovie.Fanart = TmdbCache.GetMovieBackdropFilename(images);
                        GUIWindowManager.ActivateWindow((int)TraktGUIWindows.CreditsMovie);
                    }
                    else if (activity.Show != null)
                    {
                        var images = TmdbCache.GetShowImages(activity.Show.Ids.Tmdb, true);

                        GUICreditsShow.Show = activity.Show;
                        GUICreditsShow.Type = GUICreditsShow.CreditType.Crew;
                        GUICreditsShow.Fanart = TmdbCache.GetShowBackdropFilename(images);
                        GUIWindowManager.ActivateWindow((int)TraktGUIWindows.CreditsShow);
                    }
                    break;

                case ((int)ActivityContextMenuItem.Trailers):
                    if (activity.Movie != null) 
                        GUICommon.ShowMovieTrailersMenu(activity.Movie); 
                    else if (activity.Episodes != null && activity.Episodes.Count == 1)
                        GUICommon.ShowTVShowTrailersMenu(activity.Show, activity.Episodes.First());
                    else                        
                        GUICommon.ShowTVShowTrailersMenu(activity.Show, activity.Episode);
                    break;
            }
        }

        private void SetUpdateAnimation(bool enable)
        {
            // get control
            var window = GUIWindowManager.GetWindow(GUIWindowManager.ActiveWindow);
            var control = window.GetControl((int)TraktDashboardControls.DashboardAnimation);
            if (control == null)
                return;

            try
            {
                var animation = control as GUIAnimation;

                if (animation != null)
                {
                    if (enable)
                        animation.AllocResources();
                    else
                        animation.Dispose();

                    animation.Visible = enable;
                }
            }
            catch (Exception) { }
        }

        private void ViewShout(TraktActivity.Activity activity)
        {
            switch (activity.Type)
            {
                case "movie":
                    TraktHelper.ShowMovieShouts(activity.Movie);
                    break;

                case "show":
                    TraktHelper.ShowTVShowShouts(activity.Show);
                    break;

                case "episode":
                    TraktHelper.ShowEpisodeShouts(activity.Show, activity.Episode);
                    break;

                default:
                    break;
            }
        }

        private void PlayActivityItem(bool jumpTo)
        {
            // get control
            var activityFacade = GetFacade((int)TraktDashboardControls.ActivityFacade);
            if (activityFacade == null) return;

            // get selected item in facade
            TraktActivity.Activity activity = activityFacade.SelectedListItem.TVTag as TraktActivity.Activity;

            if (activity == null || string.IsNullOrEmpty(activity.Action) || string.IsNullOrEmpty(activity.Type))
                return;

            ActivityAction action = (ActivityAction)Enum.Parse(typeof(ActivityAction), activity.Action);
            ActivityType type = (ActivityType)Enum.Parse(typeof(ActivityType), activity.Type);

            switch (type)
            {
                case ActivityType.episode:
                    if (action == ActivityAction.seen || action == ActivityAction.collection)
                    {
                        if (activity.Episodes.Count > 1)
                        {
                            GUICommon.CheckAndPlayFirstUnwatchedEpisode(activity.Show, jumpTo);
                            return;
                        }
                        else if (activity.Episodes != null && activity.Episodes.Count == 1)
                        {
                            GUICommon.CheckAndPlayEpisode(activity.Show, activity.Episodes.First());
                        }
                    } 
                    GUICommon.CheckAndPlayEpisode(activity.Show, activity.Episode);
                    break;

                case ActivityType.show:
                    GUICommon.CheckAndPlayFirstUnwatchedEpisode(activity.Show, jumpTo);
                    break;

                case ActivityType.movie:
                    GUICommon.CheckAndPlayMovie(jumpTo, activity.Movie);
                    break;

                case ActivityType.list:
                    if (action == ActivityAction.item_added)
                    {
                        // return the name of the item added to the list
                        switch (activity.ListItem.Type)
                        {
                            case "show":
                                GUICommon.CheckAndPlayFirstUnwatchedEpisode(activity.ListItem.Show, jumpTo);
                                break;

                            case "episode":
                                GUICommon.CheckAndPlayEpisode(activity.ListItem.Show, activity.ListItem.Episode);
                                break;

                            case "movie":
                                GUICommon.CheckAndPlayMovie(jumpTo, activity.ListItem.Movie);
                                break;
                        }
                    }
                    break;
            }
        }

        private void PlayShow(bool jumpTo)
        {
            // get control
            var facade = GetFacade((int)TraktDashboardControls.TrendingShowsFacade);
            if (facade == null) return;

            // get selected item in facade
            var trendingItem = facade.SelectedListItem.TVTag as TraktShowTrending;

            GUICommon.CheckAndPlayFirstUnwatchedEpisode(trendingItem.Show, jumpTo);
        }

        private void PlayMovie(bool jumpTo)
        {
            // get control
            var facade = GetFacade((int)TraktDashboardControls.TrendingMoviesFacade);
            if (facade == null) return;

            // get selected item in facade
            var trendingItem = facade.SelectedListItem.TVTag as TraktMovieTrending;

            GUICommon.CheckAndPlayMovie(jumpTo, trendingItem.Movie);
        }        

        #endregion

        #region Public Properties

        public TraktActivity PreviousActivity { get; set; }
        public IEnumerable<TraktMovieTrending> PreviousTrendingMovies { get; set; }
        public IEnumerable<TraktShowTrending> PreviousTrendingShows { get; set; }
        public TraktUserStatistics PreviousStatistics { get; set; }

        #endregion

        #region Event Handlers

        private void OnActivitySelected(GUIListItem item, GUIControl parent)
        {
            TraktActivity.Activity activity = item.TVTag as TraktActivity.Activity;
            if (activity == null || string.IsNullOrEmpty(activity.Action) || string.IsNullOrEmpty(activity.Type))
            {
                ClearSelectedActivityProperties();
                return;
            }

            // remember last selected item
            PreviousSelectedActivity = activity;

            // set type and action properties
            GUIUtils.SetProperty("#Trakt.Selected.Activity.Type", activity.Type);
            GUIUtils.SetProperty("#Trakt.Selected.Activity.Action", activity.Action);

            GUICommon.SetUserProperties(activity.User);

            ActivityAction action = (ActivityAction)Enum.Parse(typeof(ActivityAction), activity.Action);
            ActivityType type = (ActivityType)Enum.Parse(typeof(ActivityType), activity.Type);

            switch (type)
            {
                case ActivityType.episode:
                    if (action == ActivityAction.seen || action == ActivityAction.collection)
                    {
                        if (activity.Episodes.Count > 1)
                        {
                            GUICommon.SetEpisodeProperties(activity.Show, activity.Episodes.First());
                        }
                        else
                        {
                            GUICommon.SetEpisodeProperties(activity.Show, activity.Episode);
                        }
                    }
                    else
                    {
                        GUICommon.SetEpisodeProperties(activity.Show, activity.Episode);
                    }
                    GUICommon.SetShowProperties(activity.Show);
                    break;

                case ActivityType.show:
                    GUICommon.SetShowProperties(activity.Show);
                    break;

                case ActivityType.season:
                    GUICommon.SetShowProperties(activity.Show);
                    GUICommon.SetSeasonProperties(activity.Show, activity.Season);
                    break;

                case ActivityType.movie:
                    GUICommon.SetMovieProperties(activity.Movie);
                    break;

                case ActivityType.list:
                    if (action == ActivityAction.item_added)
                    {
                        // return the name of the item added to the list
                        switch (activity.ListItem.Type)
                        {
                            case "show":
                                GUICommon.SetShowProperties(activity.ListItem.Show);
                                break;
                            
                            case "season":
                                GUICommon.SetShowProperties(activity.ListItem.Show);
                                GUICommon.SetSeasonProperties(activity.ListItem.Show, activity.ListItem.Season);
                                break;

                            case "episode":
                                GUICommon.SetShowProperties(activity.ListItem.Show);
                                GUICommon.SetEpisodeProperties(activity.ListItem.Show, activity.ListItem.Episode);
                                break;

                            case "movie":
                                GUICommon.SetMovieProperties(activity.ListItem.Movie);
                                break;
                        }
                    }
                    break;

                case ActivityType.comment:
                    GUICommon.SetCommentProperties(activity.Shout, false);
                    break;
            }
        }

        private void OnTrendingShowSelected(GUIListItem item, GUIControl parent)
        {
            var trendingItem = item.TVTag as TraktShowTrending;
            if (trendingItem == null)
            {
                GUICommon.ClearShowProperties();
                return;
            }

            GUICommon.SetProperty("#Trakt.Show.Watchers", trendingItem.Watchers.ToString());
            GUICommon.SetProperty("#Trakt.Show.Watchers.Extra", trendingItem.Watchers > 1 ? string.Format(Translation.PeopleWatching, trendingItem.Watchers) : Translation.PersonWatching);
            GUICommon.SetShowProperties(trendingItem.Show);
        }

        private void OnTrendingMovieSelected(GUIListItem item, GUIControl parent)
        {
            var trendingItem = item.TVTag as TraktMovieTrending;
            if (trendingItem == null)
            {
                GUICommon.ClearMovieProperties();
                return;
            }

            GUICommon.SetProperty("#Trakt.Movie.Watchers", trendingItem.Watchers.ToString());
            GUICommon.SetProperty("#Trakt.Movie.Watchers.Extra", trendingItem.Watchers > 1 ? string.Format(Translation.PeopleWatching, trendingItem.Watchers) : Translation.PersonWatching);
            GUICommon.SetMovieProperties(trendingItem.Movie);
        }

        private void GUIWindowManager_Receivers(GUIMessage message)
        {
            if (!IsDashBoardWindow()) return;

            switch (message.Message)
            {                   
                case GUIMessage.MessageType.GUI_MSG_CLICKED:
                    if (message.Param1 != 7) return; // mouse click, enter key, remote ok, only

                    if (message.SenderControlId == (int)TraktDashboardControls.ActivityFacade)
                    {
                        var activityFacade = GetFacade((int)TraktDashboardControls.ActivityFacade);
                        if (activityFacade == null) return;

                        var activity = activityFacade.SelectedListItem.TVTag as TraktActivity.Activity;
                        if (activity == null || string.IsNullOrEmpty(activity.Action) || string.IsNullOrEmpty(activity.Type))
                            return;

                        var action = (ActivityAction)Enum.Parse(typeof(ActivityAction), activity.Action);
                        var type = (ActivityType)Enum.Parse(typeof(ActivityType), activity.Type);

                        switch (action)
                        {
                            case ActivityAction.review:
                            case ActivityAction.shout:
                                // view shout in shouts window
                                ViewShout(activity);
                                break;

                            case ActivityAction.item_added:
                            case ActivityAction.updated:
                                // load users list
                                GUIListItems.CurrentList = activity.List;
                                GUIListItems.CurrentUser = activity.User.Username;
                                GUIWindowManager.ActivateWindow((int)TraktGUIWindows.CustomListItems);
                                break;

                            case ActivityAction.created:
                                // load lists menu
                                GUILists.CurrentUser = activity.User.Username;
                                GUIWindowManager.ActivateWindow((int)TraktGUIWindows.CustomLists);
                                break;

                            case ActivityAction.watchlist:
                                // load users watchlist
                                if (type == ActivityType.movie)
                                {
                                    GUIWatchListMovies.CurrentUser = activity.User.Username;
                                    GUIWindowManager.ActivateWindow((int)TraktGUIWindows.WatchedListMovies);
                                }
                                else if (type == ActivityType.show)
                                {
                                    GUIWatchListShows.CurrentUser = activity.User.Username;
                                    GUIWindowManager.ActivateWindow((int)TraktGUIWindows.WatchedListShows);
                                }
                                else
                                {
                                    GUIWatchListEpisodes.CurrentUser = activity.User.Username;
                                    GUIWindowManager.ActivateWindow((int)TraktGUIWindows.WatchedListEpisodes);
                                }
                                break;

                            case ActivityAction.like:
                                if (type == ActivityType.comment)
                                {
                                    // view comment
                                    GUIUtils.ShowTextDialog(Translation.Comment, activity.Shout.Text);
                                }
                                else if (type == ActivityType.list)
                                {
                                    // load the liked list
                                    if (activity.List.User != null)
                                    {
                                        GUIListItems.CurrentList = activity.List;
                                        GUIListItems.CurrentUser = activity.List.User.Username;
                                        GUIWindowManager.ActivateWindow((int)TraktGUIWindows.CustomListItems);
                                    }
                                    else
                                    {
                                        TraktLogger.Warning("No user associated with liked list. ID = '{0}', Name = '{1}', Privacy = '{2}'", activity.List.Ids.Trakt, activity.List.Name, activity.List.Privacy); 
                                    }
                                }
                                break;

                            default:
                                PlayActivityItem(true);
                                break;
                        }
                    }
                    if (message.SenderControlId == (int)TraktDashboardControls.TrendingShowsFacade)
                    {
                        if (TraktSettings.EnableJumpToForTVShows)
                        {
                            PlayShow(true);
                        }
                        else
                        {
                            var facade = GetFacade((int)TraktDashboardControls.TrendingShowsFacade);
                            if (facade == null) return;

                            var trendingItem = facade.SelectedListItem.TVTag as TraktShowTrending;
                            if (trendingItem == null) return;

                            GUIWindowManager.ActivateWindow((int)TraktGUIWindows.ShowSeasons, trendingItem.Show.ToJSON());
                        }
                    }
                    if (message.SenderControlId == (int)TraktDashboardControls.TrendingMoviesFacade)
                    {
                        PlayMovie(true);
                    }
                    break;

                case GUIMessage.MessageType.GUI_MSG_WINDOW_INIT:
                    // doesn't work, only if overridden from a guiwindow class
                    break;

                default:
                    break;
            }
        }

        private void GUIWindowManager_OnNewAction(Action action)
        {
            if (!IsDashBoardWindow()) return;

            var activeWindow = GUIWindowManager.GetWindow(GUIWindowManager.ActiveWindow);

            switch (action.wID)
            {
                case Action.ActionType.ACTION_CONTEXT_MENU:
                    if (activeWindow.GetFocusControlId() == (int)TraktDashboardControls.ActivityFacade)
                    {
                        TrendingContextMenuIsActive = true;
                        ShowActivityContextMenu();
                    }
                    else if (activeWindow.GetFocusControlId() == (int)TraktDashboardControls.TrendingMoviesFacade)
                    {
                        TrendingContextMenuIsActive = true;
                        ShowTrendingMoviesContextMenu();
                    }
                    else if (activeWindow.GetFocusControlId() == (int)TraktDashboardControls.TrendingShowsFacade)
                    {
                        TrendingContextMenuIsActive = true;
                        ShowTrendingShowsContextMenu();
                    }
                    TrendingContextMenuIsActive = false;
                    break;

                case Action.ActionType.ACTION_PLAY:
                case Action.ActionType.ACTION_MUSIC_PLAY:
                    if (activeWindow.GetFocusControlId() == (int)TraktDashboardControls.ActivityFacade)
                    {
                        PlayActivityItem(false);
                    }
                    if (activeWindow.GetFocusControlId() == (int)TraktDashboardControls.TrendingShowsFacade)
                    {
                        PlayShow(false);
                    }
                    if (activeWindow.GetFocusControlId() == (int)TraktDashboardControls.TrendingMoviesFacade)
                    {
                        PlayMovie(false);
                    }
                    break;
                
                case Action.ActionType.ACTION_MOVE_DOWN:
                    // handle ondown for filmstrips as mediaportal skin navigation for ondown is broken
                    // issue has been resolved in MP 1.5.0 so only do it for earlier releases
                    if (TraktSettings.MPVersion < new Version(1, 5, 0, 0))
                    {
                        if (!TrendingContextMenuIsActive && activeWindow.GetFocusControlId() == (int)TraktDashboardControls.TrendingShowsFacade)
                        {
                            var control = GetFacade(activeWindow.GetFocusControlId());
                            if (control == null) return;

                            if (control.CurrentLayout != GUIFacadeControl.Layout.Filmstrip) return;

                            // set focus on correct control
                            GUIControl.FocusControl(GUIWindowManager.ActiveWindow, (int)TraktDashboardControls.TrendingMoviesFacade);
                        }
                        else if (!TrendingContextMenuIsActive && activeWindow.GetFocusControlId() == (int)TraktDashboardControls.TrendingMoviesFacade)
                        {
                            var control = GetFacade(activeWindow.GetFocusControlId());
                            if (control == null) return;

                            if (control.CurrentLayout != GUIFacadeControl.Layout.Filmstrip) return;

                            // set focus on correct control
                            GUIControl.FocusControl(GUIWindowManager.ActiveWindow, (int)TraktDashboardControls.ActivityFacade);
                        }
                    }
                    break;

                default:
                    break;
            }
        }
         
        #endregion

        #region Public Methods

        public void Init()
        {
            GUIWindowManager.Receivers += new SendMessageHandler(GUIWindowManager_Receivers);
            GUIWindowManager.OnNewAction +=new OnActionHandler(GUIWindowManager_OnNewAction);

            // Clear Properties
            ClearMovieProperties();
            ClearShowProperties();

            // Load from Persisted Settings
            if (TraktSettings.LastActivityLoad != null && TraktSettings.LastActivityLoad.Activities != null)
            {
                PreviousActivity = TraktSettings.LastActivityLoad;
                if (TraktSettings.LastActivityLoad.Timestamps != null)
                {
                    ActivityStartTime = TraktSettings.LastActivityLoad.Timestamps.Current;
                }
            }
            if (TraktSettings.LastTrendingShows != null)
            {
                PreviousTrendingShows = TraktSettings.LastTrendingShows;
            }
            if (TraktSettings.LastTrendingMovies != null)
            {
                PreviousTrendingMovies = TraktSettings.LastTrendingMovies;
            }

            // initialize timercallbacks
            if (TraktSkinSettings.DashBoardActivityWindows != null && TraktSkinSettings.DashBoardActivityWindows.Count > 0)
            {
                ClearSelectedActivityProperties();
                ActivityTimer = new Timer(new TimerCallback((o) => { LoadActivity(); }), null, Timeout.Infinite, Timeout.Infinite);
            }

            if (TraktSkinSettings.DashboardTrendingCollection != null && TraktSkinSettings.DashboardTrendingCollection.Exists(d => d.MovieWindows.Count > 0))
            {
                TrendingMoviesTimer = new Timer(new TimerCallback((o) => { LoadTrendingMovies(); }), null, Timeout.Infinite, Timeout.Infinite);
            }

            if (TraktSkinSettings.DashboardTrendingCollection != null && TraktSkinSettings.DashboardTrendingCollection.Exists(d => d.TVShowWindows.Count > 0))
            {
                TrendingShowsTimer = new Timer(new TimerCallback((o) => { LoadTrendingShows(); }), null, Timeout.Infinite, Timeout.Infinite);
            }

            if (TraktSkinSettings.HasDashboardStatistics)
            {
                StatisticsTimer = new Timer(new TimerCallback((o) => { GetStatistics(); }), null, 3000, 3600000);
            }
        }

        public void StartTrendingMoviesPolling()
        {
            if (TrendingMoviesTimer != null)
            {
                TrendingMoviesTimer.Change(TraktSettings.DashboardLoadDelay, TraktSettings.DashboardTrendingPollInterval);
            }
        }

        public void StartTrendingShowsPolling()
        {
            if (TrendingShowsTimer != null)
            {
                TrendingShowsTimer.Change(TraktSettings.DashboardLoadDelay, TraktSettings.DashboardTrendingPollInterval);
            }
        }

        public void StartActivityPolling()
        {
            if (ActivityTimer != null)
            {
                ActivityTimer.Change(TraktSettings.DashboardLoadDelay, TraktSettings.DashboardActivityPollInterval);
            }
        }

        public void StopActivityPolling()
        {
            if (ActivityTimer != null)
            {
                ActivityTimer.Change(Timeout.Infinite, Timeout.Infinite);
            }
        }

        public void StopTrendingMoviesPolling()
        {
            if (TrendingMoviesTimer != null)
            {
                TrendingMoviesTimer.Change(Timeout.Infinite, Timeout.Infinite);
            }
        }

        public void StopTrendingShowsPolling()
        {
            if (TrendingShowsTimer != null)
            {
                TrendingShowsTimer.Change(Timeout.Infinite, Timeout.Infinite);
            }
        }

        #endregion        
    }
}
