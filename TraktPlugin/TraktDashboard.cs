using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using MediaPortal.Util;
using MediaPortal.GUI.Library;
using TraktPlugin.GUI;
using TraktPlugin.TraktAPI;
using TraktPlugin.TraktAPI.DataStructures;
using Action = MediaPortal.GUI.Library.Action;

namespace TraktPlugin
{
    internal class TraktDashboard
    {
        #region Enums

        enum ContextMenuItem
        {
            ShowCommunityActivity,
            ShowFriendActivity,
            IncludeMeInFriendsActivity,
            DontIncludeMeInFriendsActivity
        }

        #endregion

        #region Private Variables
        
        private long ActivityStartTime = 0;

        private Timer ActivityTimer = null;
        private Timer TrendingMoviesTimer = null;
        private Timer TrendingShowsTimer = null;
        private Timer StatisticsTimer = null;

        bool StopAvatarDownload = false;
        bool StopTrendingShowsDownload = false;
        bool StopTrendingMoviesDownload = false;

        bool GetFullActivityLoad = false;

        DateTime LastTrendingShowUpdate = DateTime.MinValue;
        DateTime LastTrendingMovieUpdate = DateTime.MinValue;

        #endregion

        #region Constructor

        public TraktDashboard() { }

        #endregion

        #region Private Methods

        private GUIFacadeControl GetFacade(int facadeID)
        {
            int i = 0;
            GUIFacadeControl facade = null;

            // window init message does not work unless overridden from a guiwindow class
            // so we need to be ensured that the window is fully loaded
            // before we can get reference to a skin control
            do
            {
                // get current window
                var window = GUIWindowManager.GetWindow(GUIWindowManager.ActiveWindow);

                // get facade control
                facade = window.GetControl(facadeID) as GUIFacadeControl;
                if (facade == null)
                {
                    TraktLogger.Debug("Facade [id:{0}] not ready!", facadeID.ToString());
                    Thread.Sleep(100);
                }
                i++;
            }
            while (i < 50 && facade == null);

            return facade;
        }

        private void GetStatistics()
        {
            // initial publish from persisted settings
            if (TraktSettings.LastStatistics != null)
            {
                PublishStatistics(TraktSettings.LastStatistics);
                TraktSettings.LastStatistics = null;
            }

            // retrieve statistics from online
            var userProfile = TraktAPI.TraktAPI.GetUserProfile(TraktSettings.Username);
            if (userProfile != null)
            {
                PublishStatistics(userProfile.Stats);
                PreviousStatistics = userProfile.Stats;
            }
        }

        private void PublishStatistics(TraktUserProfile.Statistics stats)
        {
            if (stats == null) return;

            #region Friends Statistics
            if (stats.Friends != null)
            {
                GUIUtils.SetProperty("#Trakt.Statistics.Friends", stats.Friends);
            }
            #endregion

            #region Shows Statistics
            if (stats.Shows != null)
            {
                GUIUtils.SetProperty("#Trakt.Statistics.Shows.Library", stats.Shows.Library);
                GUIUtils.SetProperty("#Trakt.Statistics.Shows.Watched", stats.Shows.Watched);
                GUIUtils.SetProperty("#Trakt.Statistics.Shows.Collection", stats.Shows.Collection);
                GUIUtils.SetProperty("#Trakt.Statistics.Shows.Shouts", stats.Shows.Shouts);
                GUIUtils.SetProperty("#Trakt.Statistics.Shows.Loved", stats.Shows.Loved);
                GUIUtils.SetProperty("#Trakt.Statistics.Shows.Hated", stats.Shows.Hated);
            }
            #endregion

            #region Episodes Statistics
            if (stats.Episodes != null)
            {
                GUIUtils.SetProperty("#Trakt.Statistics.Episodes.Checkins", stats.Episodes.Checkins);
                GUIUtils.SetProperty("#Trakt.Statistics.Episodes.CheckinsUnique", stats.Episodes.CheckinsUnique);
                GUIUtils.SetProperty("#Trakt.Statistics.Episodes.Collection", stats.Episodes.Collection);
                GUIUtils.SetProperty("#Trakt.Statistics.Episodes.Hated", stats.Episodes.Hated);
                GUIUtils.SetProperty("#Trakt.Statistics.Episodes.Loved", stats.Episodes.Loved);
                GUIUtils.SetProperty("#Trakt.Statistics.Episodes.Scrobbles", stats.Episodes.Scrobbles);
                GUIUtils.SetProperty("#Trakt.Statistics.Episodes.ScrobblesUnique", stats.Episodes.ScrobblesUnique);
                GUIUtils.SetProperty("#Trakt.Statistics.Episodes.Seen", stats.Episodes.Seen);
                GUIUtils.SetProperty("#Trakt.Statistics.Episodes.Shouts", stats.Episodes.Shouts);
                GUIUtils.SetProperty("#Trakt.Statistics.Episodes.UnWatched", stats.Episodes.UnWatched);
                GUIUtils.SetProperty("#Trakt.Statistics.Episodes.Watched", stats.Episodes.Watched);
                GUIUtils.SetProperty("#Trakt.Statistics.Episodes.WatchedElseWhere", stats.Episodes.WatchedElseWhere);
                GUIUtils.SetProperty("#Trakt.Statistics.Episodes.WatchedTrakt", stats.Episodes.WatchedTrakt);
                GUIUtils.SetProperty("#Trakt.Statistics.Episodes.WatchedTraktUnique", stats.Episodes.WatchedTraktUnique);
                GUIUtils.SetProperty("#Trakt.Statistics.Episodes.WatchedUnique", stats.Episodes.WatchedUnique);
            }
            #endregion

            #region Movies Statistics
            if (stats.Movies != null)
            {
                GUIUtils.SetProperty("#Trakt.Statistics.Movies.Checkins", stats.Movies.Checkins);
                GUIUtils.SetProperty("#Trakt.Statistics.Movies.CheckinsUnique", stats.Movies.CheckinsUnique);
                GUIUtils.SetProperty("#Trakt.Statistics.Movies.Collection", stats.Movies.Collection);
                GUIUtils.SetProperty("#Trakt.Statistics.Movies.Hated", stats.Movies.Hated);
                GUIUtils.SetProperty("#Trakt.Statistics.Movies.Library", stats.Movies.Library);
                GUIUtils.SetProperty("#Trakt.Statistics.Movies.Loved", stats.Movies.Loved);
                GUIUtils.SetProperty("#Trakt.Statistics.Movies.Scrobbles", stats.Movies.Scrobbles);
                GUIUtils.SetProperty("#Trakt.Statistics.Movies.ScrobblesUnique", stats.Movies.ScrobblesUnique);
                GUIUtils.SetProperty("#Trakt.Statistics.Movies.Seen", stats.Movies.Seen);
                GUIUtils.SetProperty("#Trakt.Statistics.Movies.Shouts", stats.Movies.Shouts);
                GUIUtils.SetProperty("#Trakt.Statistics.Movies.UnWatched", stats.Movies.UnWatched);
                GUIUtils.SetProperty("#Trakt.Statistics.Movies.Watched", stats.Movies.Watched);
                GUIUtils.SetProperty("#Trakt.Statistics.Movies.WatchedElseWhere", stats.Movies.WatchedElseWhere);
                GUIUtils.SetProperty("#Trakt.Statistics.Movies.WatchedTrakt", stats.Movies.WatchedTrakt);
                GUIUtils.SetProperty("#Trakt.Statistics.Movies.WatchedTraktUnique", stats.Movies.WatchedTraktUnique);
                GUIUtils.SetProperty("#Trakt.Statistics.Movies.WatchedUnique", stats.Movies.WatchedUnique);
            }
            #endregion
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
            GUIUtils.SetProperty("#Trakt.Activity.Description", TraktSettings.ShowCommunityActivity ? Translation.ActivityCommunityDesc : Translation.ActivityFriendsDesc);
        }

        private void LoadActivity()
        {
            TraktLogger.Debug("Loading Trakt Activity...");
            GUIFacadeControl facade = null;

            // get the facade, may need to wait until
            // window has completely loaded
            if (TraktSkinSettings.DashboardActivityFacadeType.ToLowerInvariant() != "none")
            {
                facade = GetFacade((int)TraktDashboardControls.ActivityFacade);
                if (facade == null) return;

                // we may trigger a re-load by switching from
                // community->friends->community
                lock (this)
                {
                    // load facade if empty and we have activity already
                    // facade is empty on re-load of window
                    if (facade.Count == 0 && PreviousActivity != null && PreviousActivity.Activities.Count > 0)
                    {
                        PublishActivityProperties(PreviousActivity);
                        LoadActivityFacade(PreviousActivity, facade);
                    }

                    // get latest activity
                    var activities = GetActivity(TraktSettings.ShowCommunityActivity);

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
                var activities = GetActivity(TraktSettings.ShowCommunityActivity);
                if (activities == null || activities.Activities == null) return;

                // publish properties
                PublishActivityProperties(activities);
                
                // download images
                var avatarImages = new List<TraktUserProfile>();
                avatarImages.AddRange(activities.Activities.Select(a => a.User).Take(TraktSkinSettings.DashboardActivityPropertiesMaxItems));
                GetImages<TraktUserProfile>(avatarImages);
            }
        }

        private void PublishActivityProperties()
        {
            PublishActivityProperties(PreviousActivity);
        }
        private void PublishActivityProperties(TraktActivity activity)
        {
            var activities = activity.Activities;
            if (activities == null) return;

            int maxItems = activities.Count() < TraktSkinSettings.DashboardActivityFacadeMaxItems ? activities.Count() : TraktSkinSettings.DashboardActivityFacadeMaxItems;

            for (int i = 0; i < maxItems; i++)
            {
                GUIUtils.SetProperty(string.Format("#Trakt.Activity.{0}.Action", i), activities[i].Action);
                GUIUtils.SetProperty(string.Format("#Trakt.Activity.{0}.Type", i), activities[i].Type);
                GUIUtils.SetProperty(string.Format("#Trakt.Activity.{0}.ActivityPinIcon", i), GetActivityImage(activities[i]));
                GUIUtils.SetProperty(string.Format("#Trakt.Activity.{0}.ActivityPinIconNoExt", i), GetActivityImage(activities[i]).Replace(".png", string.Empty));
                GUIUtils.SetProperty(string.Format("#Trakt.Activity.{0}.Title", i), GetActivityItemName(activities[i]));
                GUIUtils.SetProperty(string.Format("#Trakt.Activity.{0}.Time", i), activities[i].Timestamp.FromEpoch().ToLocalTime().ToShortTimeString());
                GUIUtils.SetProperty(string.Format("#Trakt.Activity.{0}.Day", i), activities[i].Timestamp.FromEpoch().ToLocalTime().DayOfWeek.ToString().Substring(0,3));
                GUIUtils.SetProperty(string.Format("#Trakt.Activity.{0}.Shout", i), GetActivityShoutText(activities[i]));
            }
        }

        private void LoadActivityFacade(TraktActivity activities, GUIFacadeControl facade)
        {
            if (TraktSkinSettings.DashBoardActivityWindows == null || !TraktSkinSettings.DashBoardActivityWindows.Contains(GUIWindowManager.ActiveWindow.ToString()))
                return;

            TraktLogger.Debug("Loading Trakt Activity Facade");

            // if no activities report to user
            if (activities == null || activities.Activities.Count == 0)
            {
                GUIListItem item = new GUIListItem(Translation.NoActivities);
                facade.Add(item);
                facade.SetCurrentLayout(TraktSkinSettings.DashboardActivityFacadeType);
                ClearSelectedActivityProperties();
                return;
            }

            // if no new activities then nothing to do
            if (facade.Count > 0)
            {
                var mostRecentActivity = facade[0].TVTag as TraktActivity.Activity;
                if (mostRecentActivity != null)
                {
                    if (mostRecentActivity.Timestamp == activities.Activities.First().Timestamp &&
                        mostRecentActivity.User.Username == activities.Activities.First().User.Username)
                    {
                        return;
                    }
                }
            }

            // stop any existing image downloads
            StopAvatarDownload = true;

            // clear facade
            GUIControl.ClearControl(GUIWindowManager.ActiveWindow, facade.GetID);

            int itemId = 0;
            var avatarImages = new List<TraktUserProfile>();

            // Add each activity item to the facade
            foreach (var activity in activities.Activities)
            {
                GUITraktDashboardListItem item = new GUITraktDashboardListItem(GetListItemTitle(activity));

                string activityImage = GetActivityImage(activity);
                string avatarImage = GetAvatarImage(activity);

                item.Label2 = activity.Timestamp.FromEpoch().ToLocalTime().ToShortTimeString();
                item.TVTag = activity;
                item.Item = activity.User;
                item.ItemId = Int32.MaxValue - itemId;
                item.IconImage = avatarImage;
                item.IconImageBig = avatarImage;
                item.ThumbnailImage = avatarImage;
                item.PinImage = activityImage;
                item.OnItemSelected += OnActivitySelected;
                facade.Add(item);
                itemId++;

                // add image for download
                if (avatarImage == "defaultTraktUser.png")
                    avatarImages.Add(activity.User);
            }

            // Set Facade Layout
            facade.SetCurrentLayout(TraktSkinSettings.DashboardActivityFacadeType);

            // set facade properties
            GUIUtils.SetProperty("#Trakt.Activity.Count", activities.Activities.Count().ToString());
            GUIUtils.SetProperty("#Trakt.Activity.Items", string.Format("{0} {1}", activities.Activities.Count().ToString(), activities.Activities.Count() > 1 ? Translation.Activities : Translation.Activity));
            GUIUtils.SetProperty("#Trakt.Activity.Description", TraktSettings.ShowCommunityActivity ? Translation.ActivityCommunityDesc : Translation.ActivityFriendsDesc);

            // Download avatar images Async and set to facade
            StopAvatarDownload = false;
            GetImages<TraktUserProfile>(avatarImages);

            TraktLogger.Debug("Finished Loading Activity facade...");
        }

        /// <summary>
        /// Skinners can use this property to toggle visibility of trending facades/properties
        /// </summary>
        private void SetTrendingVisibility()
        {
            var window = GUIWindowManager.GetWindow(GUIWindowManager.ActiveWindow);
            var toggleButton = window.GetControl((int)TraktDashboardControls.ToggleTrendingCheckButton) as GUICheckButton;

            // if skin does not have checkmark control to toggle trending then exit
            if (toggleButton == null) return;

            if (toggleButton != null)
            {
                var trendingShowsFacade = GetFacade((int)TraktDashboardControls.TrendingShowsFacade);
                var trendingMoviesFacade = GetFacade((int)TraktDashboardControls.TrendingMoviesFacade);

                bool moviesVisible = TraktSettings.DashboardMovieTrendingActive;

                toggleButton.Selected = moviesVisible;
                //toggleButton.Label = moviesVisible ? Translation.Movies : Translation.TVShows;
                GUIUtils.SetProperty("#Trakt.Dashboard.TrendingType.Active", moviesVisible ? "movies" : "shows");

                if (trendingMoviesFacade != null)
                {
                    trendingMoviesFacade.Visible = moviesVisible;
                    if (trendingMoviesFacade.FilmstripLayout != null) trendingMoviesFacade.FilmstripLayout.Visible = moviesVisible;
                    if (trendingMoviesFacade.ListLayout != null) trendingMoviesFacade.ListLayout.Visible = moviesVisible;
                    if (trendingMoviesFacade.AlbumListLayout != null) trendingMoviesFacade.AlbumListLayout.Visible = moviesVisible;
                    if (trendingMoviesFacade.ThumbnailLayout != null) trendingMoviesFacade.ThumbnailLayout.Visible = moviesVisible;
                    if (trendingMoviesFacade.CoverFlowLayout != null) trendingMoviesFacade.CoverFlowLayout.Visible = moviesVisible;
                }

                if (trendingShowsFacade != null)
                {
                    trendingShowsFacade.Visible = !moviesVisible;
                    if (trendingShowsFacade.FilmstripLayout != null) trendingShowsFacade.FilmstripLayout.Visible = !moviesVisible;
                    if (trendingShowsFacade.ListLayout != null) trendingShowsFacade.ListLayout.Visible = !moviesVisible;
                    if (trendingShowsFacade.AlbumListLayout != null) trendingShowsFacade.AlbumListLayout.Visible = !moviesVisible;
                    if (trendingShowsFacade.ThumbnailLayout != null) trendingShowsFacade.ThumbnailLayout.Visible = !moviesVisible;
                    if (trendingShowsFacade.CoverFlowLayout != null) trendingShowsFacade.CoverFlowLayout.Visible = !moviesVisible;
                }
            }
        }

        private void LoadTrendingMovies()
        {
            TraktLogger.Debug("Loading Trakt Trending Movies...");

            GUIFacadeControl facade = null;
            bool isCached;

            if (TraktSkinSettings.DashboardTrendingFacadeType.ToLowerInvariant() != "none")
            {
                // update toggle visibility
                SetTrendingVisibility();

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
                var trendingMovies = GetTrendingMovies(TraktSkinSettings.DashboardTrendingFacadeMaxItems, out isCached);

                // prevent an unnecessary reload
                if (!isCached)
                {
                    // publish properties
                    PublishMovieProperties(trendingMovies);

                    // load trending into list
                    LoadTrendingMoviesFacade(trendingMovies, facade);
                }
            }
            
            // only publish skin properties
            if (facade == null && TraktSkinSettings.DashboardTrendingPropertiesMaxItems > 0)
            {
                // get latest trending
                var trendingMovies = GetTrendingMovies(TraktSkinSettings.DashboardTrendingPropertiesMaxItems, out isCached);

                if (!isCached)
                {
                    if (trendingMovies == null || trendingMovies.Count() == 0) return;

                    // publish properties
                    PublishMovieProperties(trendingMovies);

                    // download images
                    var movieImages = new List<TraktMovie.MovieImages>();
                    movieImages.AddRange(trendingMovies.Select(m => m.Images));
                    GetImages<TraktMovie.MovieImages>(movieImages);
                }
            }
        }

        private void PublishMovieProperties()
        {
            PublishMovieProperties(PreviousTrendingMovies);
        }
        private void PublishMovieProperties(IEnumerable<TraktTrendingMovie> movies)
        {
            if (movies == null) return;

            var movieList = movies.ToList();
            int maxItems = movies.Count() < TraktSkinSettings.DashboardTrendingPropertiesMaxItems ? movies.Count() : TraktSkinSettings.DashboardTrendingPropertiesMaxItems;

            for (int i = 0; i < maxItems; i++)
            {
                var movie = movieList[i];
                if (movie == null) continue;

                GUICommon.SetProperty(string.Format("#Trakt.Movie.{0}.Watchers", i), movie.Watchers.ToString());
                GUICommon.SetProperty(string.Format("#Trakt.Movie.{0}.Watchers.Extra", i), movie.Watchers > 1 ? string.Format(Translation.PeopleWatching, movie.Watchers) : Translation.PersonWatching);

                GUICommon.SetProperty(string.Format("#Trakt.Movie.{0}.Imdb", i), movie.Imdb);
                GUICommon.SetProperty(string.Format("#Trakt.Movie.{0}.Certification", i), movie.Certification);
                GUICommon.SetProperty(string.Format("#Trakt.Movie.{0}.Overview", i), string.IsNullOrEmpty(movie.Overview) ? Translation.NoMovieSummary : movie.Overview);
                GUICommon.SetProperty(string.Format("#Trakt.Movie.{0}.Released", i), movie.Released.FromEpoch().ToShortDateString());
                GUICommon.SetProperty(string.Format("#Trakt.Movie.{0}.Runtime", i), movie.Runtime.ToString());
                GUICommon.SetProperty(string.Format("#Trakt.Movie.{0}.Tagline", i), movie.Tagline);
                GUICommon.SetProperty(string.Format("#Trakt.Movie.{0}.Title", i), movie.Title);
                GUICommon.SetProperty(string.Format("#Trakt.Movie.{0}.Tmdb", i), movie.Tmdb);
                GUICommon.SetProperty(string.Format("#Trakt.Movie.{0}.Trailer", i), movie.Trailer);
                GUICommon.SetProperty(string.Format("#Trakt.Movie.{0}.Url", i), movie.Url);
                GUICommon.SetProperty(string.Format("#Trakt.Movie.{0}.Year", i), movie.Year);
                GUICommon.SetProperty(string.Format("#Trakt.Movie.{0}.Genres", i), string.Join(", ", movie.Genres.ToArray()));
                GUICommon.SetProperty(string.Format("#Trakt.Movie.{0}.PosterImageFilename", i), movie.Images.PosterImageFilename);
                GUICommon.SetProperty(string.Format("#Trakt.Movie.{0}.FanartImageFilename", i), movie.Images.FanartImageFilename);
                GUICommon.SetProperty(string.Format("#Trakt.Movie.{0}.InCollection", i), movie.InCollection.ToString());
                GUICommon.SetProperty(string.Format("#Trakt.Movie.{0}.InWatchList", i), movie.InWatchList.ToString());
                GUICommon.SetProperty(string.Format("#Trakt.Movie.{0}.Plays", i), movie.Plays.ToString());
                GUICommon.SetProperty(string.Format("#Trakt.Movie.{0}.Watched", i), movie.Watched.ToString());
                GUICommon.SetProperty(string.Format("#Trakt.Movie.{0}.Rating", i), movie.Rating);
                GUICommon.SetProperty(string.Format("#Trakt.Movie.{0}.RatingAdvanced", i), movie.RatingAdvanced.ToString());
                GUICommon.SetProperty(string.Format("#Trakt.Movie.{0}.Ratings.Icon", i), (movie.Ratings.LovedCount > movie.Ratings.HatedCount) ? "love" : "hate");
                GUICommon.SetProperty(string.Format("#Trakt.Movie.{0}.Ratings.HatedCount", i), movie.Ratings.HatedCount.ToString());
                GUICommon.SetProperty(string.Format("#Trakt.Movie.{0}.Ratings.LovedCount", i), movie.Ratings.LovedCount.ToString());
                GUICommon.SetProperty(string.Format("#Trakt.Movie.{0}.Ratings.Percentage", i), movie.Ratings.Percentage.ToString());
                GUICommon.SetProperty(string.Format("#Trakt.Movie.{0}.Ratings.Votes", i), movie.Ratings.Votes.ToString());
            }
        }

        private void LoadTrendingMoviesFacade(IEnumerable<TraktTrendingMovie> movies, GUIFacadeControl facade)
        {
            if (TraktSkinSettings.DashBoardTrendingMoviesWindows == null || !TraktSkinSettings.DashBoardTrendingMoviesWindows.Contains(GUIWindowManager.ActiveWindow.ToString()))
                return;

            TraktLogger.Debug("Loading Trakt Trending Movies facade...");

            // if no trending, then nothing to do
            if (movies == null || movies.Count() == 0)
                return;

            // stop any existing image downloads
            StopTrendingMoviesDownload = true;

            // clear facade
            GUIControl.ClearControl(GUIWindowManager.ActiveWindow, facade.GetID);

            int itemId = 0;
            var movieImages = new List<TraktMovie.MovieImages>();

            // Add each activity item to the facade
            foreach (var movie in movies)
            {
                GUITraktDashboardListItem item = new GUITraktDashboardListItem(movie.Title);

                item.Label2 = movie.Year.ToString();
                item.TVTag = movie;
                item.Item = movie.Images;
                item.ItemId = Int32.MaxValue - itemId;
                item.IconImage = "defaultVideo.png";
                item.IconImageBig = "defaultVideoBig.png";
                item.ThumbnailImage = "defaultVideoBig.png";
                item.OnItemSelected += OnTrendingMovieSelected;
                try
                {
                    facade.Add(item);
                }
                catch { }
                itemId++;

                // add image for download
                movieImages.Add(movie.Images);
            }

            // Set Facade Layout
            facade.SetCurrentLayout(TraktSkinSettings.DashboardTrendingFacadeType);

            // set facade properties
            GUIUtils.SetProperty("#Trakt.Trending.Movies.Items", string.Format("{0} {1}", movies.Count().ToString(), movies.Count() > 1 ? Translation.Movies : Translation.Movie));
            GUIUtils.SetProperty("#Trakt.Trending.Movies.PeopleCount", movies.Sum(s => s.Watchers).ToString());
            GUIUtils.SetProperty("#Trakt.Trending.Movies.Description", string.Format(Translation.TrendingTVShowsPeople, movies.Sum(s => s.Watchers).ToString(), movies.Count().ToString()));

            // Download images Async and set to facade
            StopTrendingMoviesDownload = false;
            GetImages<TraktMovie.MovieImages>(movieImages);

            SetTrendingVisibility();

            TraktLogger.Debug("Finished Loading Trending Movies facade...");
        }

        private void LoadTrendingShows()
        {
            TraktLogger.Debug("Loading Trakt Trending Shows...");

            GUIFacadeControl facade = null;
            bool isCached;

            if (TraktSkinSettings.DashboardTrendingFacadeType.ToLowerInvariant() != "none")
            {
                // update toggle visibility
                SetTrendingVisibility();

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
                var trendingShows = GetTrendingShows(TraktSkinSettings.DashboardTrendingFacadeMaxItems, out isCached);

                // prevent an unnecessary reload
                if (!isCached)
                {
                    // publish properties
                    PublishShowProperties(trendingShows);

                    // load trending into list
                    LoadTrendingShowsFacade(trendingShows, facade);
                }
            }
            
            // only publish skin properties
            if (facade == null && TraktSkinSettings.DashboardTrendingPropertiesMaxItems > 0)
            {
                // get latest trending
                var trendingShows = GetTrendingShows(TraktSkinSettings.DashboardTrendingPropertiesMaxItems, out isCached);
                
                if (!isCached)
                {
                    if (trendingShows == null || trendingShows.Count() == 0) return;

                    // publish properties
                    PublishShowProperties(trendingShows);

                    // download images
                    var showImages = new List<TraktShow.ShowImages>();
                    showImages.AddRange(trendingShows.Select(s => s.Images));
                    GetImages<TraktShow.ShowImages>(showImages);
                }
            }
        }

        private void PublishShowProperties()
        {
            PublishShowProperties(PreviousTrendingShows);
        }
        private void PublishShowProperties(IEnumerable<TraktTrendingShow> shows)
        {
            if (shows == null) return;

            var showList = shows.ToList();
            int maxItems = shows.Count() < TraktSkinSettings.DashboardTrendingPropertiesMaxItems ? shows.Count() : TraktSkinSettings.DashboardTrendingPropertiesMaxItems;

            for (int i = 0; i < maxItems; i++)
            {
                var show = showList[i];
                if (show == null) continue;

                GUICommon.SetProperty(string.Format("#Trakt.Show.{0}.Watchers", i), show.Watchers.ToString());
                GUICommon.SetProperty(string.Format("#Trakt.Show.{0}.Watchers.Extra", i), show.Watchers > 1 ? string.Format(Translation.PeopleWatching, show.Watchers) : Translation.PersonWatching);

                GUICommon.SetProperty(string.Format("#Trakt.Show.{0}.Imdb", i), show.Imdb);
                GUICommon.SetProperty(string.Format("#Trakt.Show.{0}.Tvdb", i), show.Tvdb);
                GUICommon.SetProperty(string.Format("#Trakt.Show.{0}.TvRage", i), show.TvRage);
                GUICommon.SetProperty(string.Format("#Trakt.Show.{0}.Title", i), show.Title);
                GUICommon.SetProperty(string.Format("#Trakt.Show.{0}.Url", i), show.Url);
                GUICommon.SetProperty(string.Format("#Trakt.Show.{0}.AirDay", i), show.AirDay);
                GUICommon.SetProperty(string.Format("#Trakt.Show.{0}.AirTime", i), show.AirTime);
                GUICommon.SetProperty(string.Format("#Trakt.Show.{0}.Certification", i), show.Certification);
                GUICommon.SetProperty(string.Format("#Trakt.Show.{0}.Country", i), show.Country);
                GUICommon.SetProperty(string.Format("#Trakt.Show.{0}.FirstAired", i), show.FirstAired.FromEpoch().ToShortDateString());
                GUICommon.SetProperty(string.Format("#Trakt.Show.{0}.Network", i), show.Network);
                GUICommon.SetProperty(string.Format("#Trakt.Show.{0}.Overview", i), string.IsNullOrEmpty(show.Overview) ? Translation.NoShowSummary : show.Overview);
                GUICommon.SetProperty(string.Format("#Trakt.Show.{0}.Runtime", i), show.Runtime.ToString());
                GUICommon.SetProperty(string.Format("#Trakt.Show.{0}.Year", i), show.Year.ToString());
                GUICommon.SetProperty(string.Format("#Trakt.Show.{0}.Genres", i), string.Join(", ", show.Genres.ToArray()));
                GUICommon.SetProperty(string.Format("#Trakt.Show.{0}.InWatchList", i), show.InWatchList.ToString());
                GUICommon.SetProperty(string.Format("#Trakt.Show.{0}.Watched", i), show.Watched.ToString());
                GUICommon.SetProperty(string.Format("#Trakt.Show.{0}.Plays", i), show.Plays.ToString());
                GUICommon.SetProperty(string.Format("#Trakt.Show.{0}.Rating", i), show.Rating);
                GUICommon.SetProperty(string.Format("#Trakt.Show.{0}.RatingAdvanced", i), show.RatingAdvanced.ToString());
                GUICommon.SetProperty(string.Format("#Trakt.Show.{0}.Ratings.Icon", i), (show.Ratings.LovedCount > show.Ratings.HatedCount) ? "love" : "hate");
                GUICommon.SetProperty(string.Format("#Trakt.Show.{0}.Ratings.HatedCount", i), show.Ratings.HatedCount.ToString());
                GUICommon.SetProperty(string.Format("#Trakt.Show.{0}.Ratings.LovedCount", i), show.Ratings.LovedCount.ToString());
                GUICommon.SetProperty(string.Format("#Trakt.Show.{0}.Ratings.Percentage", i), show.Ratings.Percentage.ToString());
                GUICommon.SetProperty(string.Format("#Trakt.Show.{0}.Ratings.Votes", i), show.Ratings.Votes.ToString());
                GUICommon.SetProperty(string.Format("#Trakt.Show.{0}.FanartImageFilename", i), show.Images.FanartImageFilename);
            }
        }

        private void LoadTrendingShowsFacade(IEnumerable<TraktTrendingShow> shows, GUIFacadeControl facade)
        {
            if (TraktSkinSettings.DashBoardTrendingShowsWindows == null || !TraktSkinSettings.DashBoardTrendingShowsWindows.Contains(GUIWindowManager.ActiveWindow.ToString()))
                return;

            TraktLogger.Debug("Loading Trakt Trending Shows facade...");

            // if no trending, then nothing to do
            if (shows == null || shows.Count() == 0)
                return;

            // stop any existing image downloads
            StopTrendingShowsDownload = true;

            // clear facade
            GUIControl.ClearControl(GUIWindowManager.ActiveWindow, facade.GetID);

            int itemId = 0;
            var showImages = new List<TraktShow.ShowImages>();

            // Add each activity item to the facade
            foreach (var show in shows)
            {
                GUITraktDashboardListItem item = new GUITraktDashboardListItem(show.Title);

                item.Label2 = show.Year.ToString();
                item.TVTag = show;
                item.Item = show.Images;
                item.ItemId = Int32.MaxValue - itemId;
                item.IconImage = "defaultVideo.png";
                item.IconImageBig = "defaultVideoBig.png";
                item.ThumbnailImage = "defaultVideoBig.png";
                item.OnItemSelected += OnTrendingShowSelected;
                try
                {
                    facade.Add(item);
                }
                catch { }
                itemId++;

                // add image for download
                showImages.Add(show.Images);
            }

            // Set Facade Layout
            facade.SetCurrentLayout(TraktSkinSettings.DashboardTrendingFacadeType);

            // set facade properties
            GUIUtils.SetProperty("#Trakt.Trending.Shows.Items", string.Format("{0} {1}", shows.Count().ToString(), shows.Count() > 1 ? Translation.SeriesPlural : Translation.Series));
            GUIUtils.SetProperty("#Trakt.Trending.Shows.PeopleCount", shows.Sum(s => s.Watchers).ToString());
            GUIUtils.SetProperty("#Trakt.Trending.Shows.Description", string.Format(Translation.TrendingTVShowsPeople, shows.Sum(s => s.Watchers).ToString(), shows.Count().ToString()));

            // Download images Async and set to facade
            StopTrendingShowsDownload = false;
            GetImages<TraktShow.ShowImages>(showImages);
            
            SetTrendingVisibility();

            TraktLogger.Debug("Finished Loading Trending Shows facade...");
        }

        private string GetActivityImage(TraktActivity.Activity activity)
        {
            string imageFilename = string.Empty;

            switch (activity.Action)
            {
                case "checkin":
                case "watching":
                    imageFilename = "traktActivityWatching.png";
                    break;

                case "seen":
                case "scrobble":
                    imageFilename = "traktActivityWatched.png";
                    break;

                case "collection":
                    imageFilename = "traktActivityCollected.png";
                    break;

                case "rating":
                    imageFilename = int.Parse(activity.RatingAdvanced) > 5 ? "traktActivityLove.png" : "traktActivityHate.png";
                    break;

                case "watchlist":
                    imageFilename = "traktActivityWatchlist.png";
                    break;

                case "shout":
                    imageFilename = "traktActivityShout.png";
                    break;

                case "item_added":
                case "created":
                    imageFilename = "traktActivityList.png";
                    break;
            }

            return imageFilename;
        }

        private string GetAvatarImage(TraktActivity.Activity activity)
        {
            string filename = activity.User.AvatarFilename;
            if (string.IsNullOrEmpty(filename) || !System.IO.File.Exists(filename))
            {
                filename = "defaultTraktUser.png";
            }
            return filename;
        }

        private string GetActivityShoutText(TraktActivity.Activity activity)
        {
            if (activity.Action != "shout") return string.Empty;
            if (activity.Shout.Spoiler) return Translation.HiddenToPreventSpoilers;
            return activity.Shout.Text;
        }

        private string GetListItemTitle(TraktActivity.Activity activity)
        {
            string itemName = GetActivityItemName(activity);
            string userName = activity.User.Username;
            string title = string.Empty;

            switch (activity.Action)
            {
                case "watching":
                    title = string.Format(Translation.ActivityWatching, userName, itemName);
                    break;

                case "scrobble":
                    title = string.Format(Translation.ActivityWatched, userName, itemName);
                    break;

                case "checkin":
                    title = string.Format(Translation.ActivityCheckedIn, userName, itemName);
                    break;

                case "seen":
                    if (activity.Type == "episode" && activity.Episodes.Count > 1)
                    {
                        title = string.Format(Translation.ActivitySeenEpisodes, userName, activity.Episodes.Count, itemName);
                    }
                    else
                    {
                        title = string.Format(Translation.ActivitySeen, userName, itemName);
                    }
                    break;

                case "collection":
                    if (activity.Type == "episode" && activity.Episodes.Count > 1)
                    {
                        title = string.Format(Translation.ActivityCollectedEpisodes, userName, activity.Episodes.Count, itemName);
                    }
                    else
                    {
                        title = string.Format(Translation.ActivityCollected, userName, itemName);
                    }
                    break;

                case "rating":
                    if (activity.UseRatingAdvanced)
                    {
                        title = string.Format(Translation.ActivityRatingAdvanced, userName, itemName, activity.RatingAdvanced);
                    }
                    else
                    {
                        title = string.Format(Translation.ActivityRating, userName, itemName);
                    }
                    break;

                case "watchlist":
                    title = string.Format(Translation.ActivityWatchlist, userName, itemName);
                    break;

                case "shout":
                    title = string.Format(Translation.ActivityShouts, userName, itemName);
                    break;

                case "created": // created list
                    title = string.Format(Translation.ActivityCreatedList, userName, itemName);
                    break;

                case "item_added": // added item to list
                    title = string.Format(Translation.ActivityAddToList, userName, itemName, activity.List.Name);
                    break;
            }

            return title;
        }

        private string GetActivityItemName(TraktActivity.Activity activity)
        {
            string name = string.Empty;

            try
            {
                switch (activity.Type)
                {
                    case "episode":
                        if (activity.Action == "seen" || activity.Action == "collection")
                        {
                            if (activity.Episodes.Count > 1)
                            {
                                // just return show name
                                name = activity.Show.Title;
                            }
                            else
                            {
                                //  get the first and only item in collection of episodes
                                string episodeIndex = activity.Episodes.First().Number.ToString();
                                string seasonIndex = activity.Episodes.First().Season.ToString();
                                string episodeName = activity.Episodes.First().Title;

                                if (string.IsNullOrEmpty(episodeName))
                                    episodeName = string.Format("{0} {1}", Translation.Episode, episodeIndex);

                                name = string.Format("{0} - {1}x{2} - {3}", activity.Show.Title, seasonIndex, episodeIndex, episodeName);
                            }
                        }
                        else
                        {
                            string episodeName = activity.Episode.Title;

                            if (string.IsNullOrEmpty(episodeName))
                                episodeName = string.Format("{0} {1}", Translation.Episode, activity.Episode.Number.ToString());

                            name = string.Format("{0} - {1}x{2} - {3}", activity.Show.Title, activity.Episode.Season.ToString(), activity.Episode.Number.ToString(), episodeName);
                        }
                        break;

                    case "show":
                        name = activity.Show.Title;
                        break;

                    case "movie":
                        name = string.Format("{0} ({1})", activity.Movie.Title, activity.Movie.Year);
                        break;

                    case "list":
                        if (activity.Action == "item_added")
                        {
                            // return the name of the item added to the list
                            switch (activity.ListItem.Type)
                            {
                                case "show":
                                    name = activity.ListItem.Show.Title;
                                    break;

                                case "episode":
                                    string episodeIndex = activity.ListItem.Episode.Number.ToString();
                                    string seasonIndex = activity.ListItem.Episode.Season.ToString();
                                    string episodeName = activity.ListItem.Episode.Title;

                                    if (string.IsNullOrEmpty(episodeName))
                                        episodeName = string.Format("{0} {1}", Translation.Episode, episodeIndex);

                                    name = string.Format("{0} - {1}x{2} - {3}", activity.ListItem.Show.Title, seasonIndex, episodeIndex, episodeName);
                                    break;

                                case "movie":
                                    name = string.Format("{0} ({1})", activity.ListItem.Movie.Title, activity.ListItem.Movie.Year);
                                    break;
                            }
                        }
                        else if (activity.Action == "created")
                        {
                            // return the list name
                            name = activity.List.Name;
                        }
                        break;
                }
            }
            catch
            {
                // most likely trakt returned a null object
                name = string.Empty;
            }

            return name;
        }

        private IEnumerable<TraktTrendingMovie> GetTrendingMovies(int maxItems, out bool isCached)
        {
            isCached = false;
            double timeSinceLastUpdate = DateTime.Now.Subtract(LastTrendingMovieUpdate).TotalMilliseconds;

            if (PreviousTrendingMovies == null || TraktSettings.DashboardTrendingPollInterval <= timeSinceLastUpdate)
            {
                var trendingMovies = TraktAPI.TraktAPI.GetTrendingMovies();
                if (trendingMovies != null && trendingMovies.Count() > 0)
                {
                    TraktLogger.Debug("Getting trending movies from trakt");
                    LastTrendingMovieUpdate = DateTime.Now;
                    PreviousTrendingMovies = trendingMovies.Take(maxItems);
                }
            }
            else
            {
                TraktLogger.Debug("Getting trending movies from cache");
                isCached = true;
                // update start interval
                int startInterval = (int)(TraktSettings.DashboardTrendingPollInterval - timeSinceLastUpdate);
                TrendingMoviesTimer.Change(startInterval, TraktSettings.DashboardTrendingPollInterval);
            }
            return PreviousTrendingMovies;
        }

        private IEnumerable<TraktTrendingShow> GetTrendingShows(int maxItems, out bool isCached)
        {
            isCached = false;
            double timeSinceLastUpdate = DateTime.Now.Subtract(LastTrendingShowUpdate).TotalMilliseconds;

            if (PreviousTrendingShows == null || TraktSettings.DashboardTrendingPollInterval <= timeSinceLastUpdate)
            {
                TraktLogger.Debug("Getting trending shows from trakt");
                var trendingShows = TraktAPI.TraktAPI.GetTrendingShows();
                if (trendingShows != null && trendingShows.Count() > 0)
                {
                    LastTrendingShowUpdate = DateTime.Now;
                    PreviousTrendingShows = trendingShows.Take(maxItems);
                }
            }
            else
            {
                TraktLogger.Debug("Getting trending shows from cache");
                isCached = true;
                // update start interval
                int startInterval = (int)(TraktSettings.DashboardTrendingPollInterval - timeSinceLastUpdate);
                TrendingShowsTimer.Change(startInterval, TraktSettings.DashboardTrendingPollInterval);
            }
            return PreviousTrendingShows;
        }

        private TraktActivity GetActivity(bool community)
        {
            SetUpdateAnimation(true);

            if (PreviousActivity == null || ActivityStartTime <= 0 || GetFullActivityLoad)
            {
                PreviousActivity = community ? TraktAPI.TraktAPI.GetCommunityActivity() : TraktAPI.TraktAPI.GetFriendActivity(TraktSettings.IncludeMeInFriendsActivity);
                GetFullActivityLoad = false;

                // check that we have any friend activity, if not switch to friends+me
                // not everyone has friends! We could use friend count but that means an extra uneeded request
                if (PreviousActivity != null && PreviousActivity.Activities != null)
                {
                    if (!TraktSettings.ShowCommunityActivity && !TraktSettings.IncludeMeInFriendsActivity)
                    {
                        TraktSettings.IncludeMeInFriendsActivity = true;
                        PreviousActivity = TraktAPI.TraktAPI.GetFriendActivity(null, null, ActivityStartTime, DateTime.UtcNow.ToEpoch(), true);
                    }
                }
            }
            else
            {
                TraktActivity incrementalActivity = null;

                // get latest incremental change using last current timestamp as start point
                if (community)
                {
                    incrementalActivity = TraktAPI.TraktAPI.GetCommunityActivity(null, null, ActivityStartTime, DateTime.UtcNow.ToEpoch());
                }
                else
                {
                    incrementalActivity = TraktAPI.TraktAPI.GetFriendActivity(null, null, ActivityStartTime, DateTime.UtcNow.ToEpoch(), TraktSettings.IncludeMeInFriendsActivity);
                }

                // join the Previous request with the current
                if (incrementalActivity != null && incrementalActivity.Activities != null)
                {
                    TraktLogger.Debug("Response: {0}", incrementalActivity.ToJSON());
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

        private void GetImages<T>(List<T> itemsWithThumbs)
        {
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
                        string remoteThumb = string.Empty;
                        string localThumb = string.Empty;

                        if (item is TraktUserProfile)
                        {
                            if (StopAvatarDownload) return;
                            remoteThumb = (item as TraktUserProfile).Avatar;
                            localThumb = (item as TraktUserProfile).AvatarFilename;
                        }
                        else if (item is TraktMovie.MovieImages)
                        {
                            if (StopTrendingMoviesDownload) return;
                            if ((item as TraktMovie.MovieImages) != null)
                            {
                                remoteThumb = (item as TraktMovie.MovieImages).Poster;
                                localThumb = (item as TraktMovie.MovieImages).PosterImageFilename;
                            }
                        }
                        else if (item is TraktShow.ShowImages)
                        {
                            if (StopTrendingShowsDownload) return;
                            if ((item as TraktShow.ShowImages) != null)
                            {
                                remoteThumb = (item as TraktShow.ShowImages).Poster;
                                localThumb = (item as TraktShow.ShowImages).PosterImageFilename;
                            }
                        }

                        if (!string.IsNullOrEmpty(remoteThumb) && !string.IsNullOrEmpty(localThumb))
                        {
                            if (GUIImageHandler.DownloadImage(remoteThumb, localThumb))
                            {
                                // notify that image has been downloaded
                                if (item is TraktUserProfile)
                                {
                                    if (StopAvatarDownload) return;
                                    (item as TraktUserProfile).NotifyPropertyChanged("AvatarFilename");
                                    PublishActivityProperties();
                                }
                                else if (item is TraktMovie.MovieImages)
                                {
                                    if (StopTrendingMoviesDownload) return;
                                    (item as TraktMovie.MovieImages).NotifyPropertyChanged("PosterImageFilename");
                                    PublishMovieProperties();
                                }
                                else if (item is TraktShow.ShowImages)
                                {
                                    if (StopTrendingShowsDownload) return;
                                    (item as TraktShow.ShowImages).NotifyPropertyChanged("PosterImageFilename");
                                    PublishShowProperties();
                                }
                            }
                        }
                    }
                })
                {
                    IsBackground = true,
                    Name = "Trakt Dashboard Image Downloader " + i.ToString()
                }.Start(groupList);
            }
        }

        private bool IsDashBoardWindow()
        {
            bool hasDashBoard = false;

            if (TraktSkinSettings.DashBoardActivityWindows != null && TraktSkinSettings.DashBoardActivityWindows.Contains(GUIWindowManager.ActiveWindow.ToString()))
                hasDashBoard = true;
            if (TraktSkinSettings.DashBoardTrendingMoviesWindows != null && TraktSkinSettings.DashBoardTrendingMoviesWindows.Contains(GUIWindowManager.ActiveWindow.ToString()))
                hasDashBoard = true;
            if (TraktSkinSettings.DashBoardTrendingShowsWindows != null && TraktSkinSettings.DashBoardTrendingShowsWindows.Contains(GUIWindowManager.ActiveWindow.ToString()))
                hasDashBoard = true;

            return hasDashBoard;
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

            // switch between Community / Friends
            if (!TraktSettings.ShowCommunityActivity)
            {
                listItem = new GUIListItem(Translation.ShowCommunityActivity);
                dlg.Add(listItem);
                listItem.ItemId = (int)ContextMenuItem.ShowCommunityActivity;

                if (!TraktSettings.IncludeMeInFriendsActivity)
                {
                    listItem = new GUIListItem(Translation.IncludeMeInFriendsActivity);
                    dlg.Add(listItem);
                    listItem.ItemId = (int)ContextMenuItem.IncludeMeInFriendsActivity;
                }
                else
                {
                    listItem = new GUIListItem(Translation.DontIncludeMeInFriendsActivity);
                    dlg.Add(listItem);
                    listItem.ItemId = (int)ContextMenuItem.DontIncludeMeInFriendsActivity;
                }
            }
            else
            {
                listItem = new GUIListItem(Translation.ShowFriendActivity);
                dlg.Add(listItem);
                listItem.ItemId = (int)ContextMenuItem.ShowFriendActivity;
            }

            // Show Context Menu
            dlg.DoModal(GUIWindowManager.ActiveWindow);
            if (dlg.SelectedId < 0) return;

            switch (dlg.SelectedId)
            {
                case ((int)ContextMenuItem.ShowCommunityActivity):
                    TraktSettings.ShowCommunityActivity = true;
                    GetFullActivityLoad = true;
                    StartActivityPolling();
                    break;

                case ((int)ContextMenuItem.ShowFriendActivity):
                    TraktSettings.ShowCommunityActivity = false;
                    GetFullActivityLoad = true;
                    StartActivityPolling();
                    break;

                case ((int)ContextMenuItem.IncludeMeInFriendsActivity):
                    TraktSettings.IncludeMeInFriendsActivity = true;
                    GetFullActivityLoad = true;
                    StartActivityPolling();
                    break;

                case ((int)ContextMenuItem.DontIncludeMeInFriendsActivity):
                    TraktSettings.IncludeMeInFriendsActivity = false;
                    GetFullActivityLoad = true;
                    StartActivityPolling();
                    break;
            }
        }

        private void SetUpdateAnimation(bool enable)
        {
            // get control
            var window = GUIWindowManager.GetWindow(GUIWindowManager.ActiveWindow);
            var control = window.GetControl((int)TraktDashboardControls.DashboardAnimation);
            if (control == null) return;

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

        private void PlayActivityItem(bool jumpTo)
        {
            // get control
            var activityFacade = GetFacade((int)TraktDashboardControls.ActivityFacade);
            if (activityFacade == null) return;

            // get selected item in facade
            TraktActivity.Activity activity = activityFacade.SelectedListItem.TVTag as TraktActivity.Activity;

            switch (activity.Type)
            {
                case "episode":
                    if (activity.Action == "seen" || activity.Action == "collection")
                    {
                        if (activity.Episodes.Count > 1)
                        {
                            GUICommon.CheckAndPlayFirstUnwatched(activity.Show, jumpTo);
                            return;
                        }
                    } 
                    GUICommon.CheckAndPlayEpisode(activity.Show, activity.Episode);
                    break;

                case "show":
                    GUICommon.CheckAndPlayFirstUnwatched(activity.Show, jumpTo);
                    break;

                case "movie":
                    GUICommon.CheckAndPlayMovie(jumpTo, activity.Movie);
                    break;

                case "list":
                    if (activity.Action == "item_added")
                    {
                        // return the name of the item added to the list
                        switch (activity.ListItem.Type)
                        {
                            case "show":
                                GUICommon.CheckAndPlayFirstUnwatched(activity.ListItem.Show, jumpTo);
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
            TraktTrendingShow show = facade.SelectedListItem.TVTag as TraktTrendingShow;

            GUICommon.CheckAndPlayFirstUnwatched(show, jumpTo);
        }

        private void PlayMovie(bool jumpTo)
        {
            // get control
            var facade = GetFacade((int)TraktDashboardControls.TrendingMoviesFacade);
            if (facade == null) return;

            // get selected item in facade
            TraktTrendingMovie movie = facade.SelectedListItem.TVTag as TraktTrendingMovie;

            GUICommon.CheckAndPlayMovie(jumpTo, movie);
        }        

        #endregion

        #region Public Properties

        public TraktActivity PreviousActivity { get; set; }
        public IEnumerable<TraktTrendingMovie> PreviousTrendingMovies { get; set; }
        public IEnumerable<TraktTrendingShow> PreviousTrendingShows { get; set; }
        public TraktUserProfile.Statistics PreviousStatistics { get; set; }        

        #endregion

        #region Event Handlers

        private void OnActivitySelected(GUIListItem item, GUIControl parent)
        {
            TraktActivity.Activity activity = item.TVTag as TraktActivity.Activity;
            if (activity == null)
            {
                ClearSelectedActivityProperties();
                return;
            }

            // set type and action properties
            GUIUtils.SetProperty("#Trakt.Selected.Activity.Type", activity.Type);
            GUIUtils.SetProperty("#Trakt.Selected.Activity.Action", activity.Action);

            GUICommon.SetUserProperties(activity.User);

            switch (activity.Type)
            {
                case "episode":
                    if (activity.Action == "seen" || activity.Action == "collection")
                    {
                        if (activity.Episodes.Count > 1)
                        {
                            GUICommon.SetEpisodeProperties(activity.Episodes.First());
                        }
                        else
                        {
                            GUICommon.SetEpisodeProperties(activity.Episode);
                        }
                    }
                    else
                    {
                        GUICommon.SetEpisodeProperties(activity.Episode);
                    }
                    GUICommon.SetShowProperties(activity.Show);
                    break;

                case "show":
                    GUICommon.SetShowProperties(activity.Show);
                    break;

                case "movie":
                    GUICommon.SetMovieProperties(activity.Movie);
                    break;

                case "list":
                    if (activity.Action == "item_added")
                    {
                        // return the name of the item added to the list
                        switch (activity.ListItem.Type)
                        {
                            case "show":
                                GUICommon.SetShowProperties(activity.ListItem.Show);
                                break;

                            case "episode":
                                GUICommon.SetShowProperties(activity.ListItem.Show);
                                GUICommon.SetEpisodeProperties(activity.ListItem.Episode);
                                break;

                            case "movie":
                                GUICommon.SetMovieProperties(activity.ListItem.Movie);
                                break;
                        }
                    }
                    break;
            }
        }

        private void OnTrendingShowSelected(GUIListItem item, GUIControl parent)
        {
            TraktTrendingShow show = item.TVTag as TraktTrendingShow;
            if (show == null)
            {
                GUICommon.ClearShowProperties();
                return;
            }

            GUICommon.SetProperty("#Trakt.Show.Watchers", show.Watchers.ToString());
            GUICommon.SetProperty("#Trakt.Show.Watchers.Extra", show.Watchers > 1 ? string.Format(Translation.PeopleWatching, show.Watchers) : Translation.PersonWatching);
            GUICommon.SetShowProperties(show);
        }

        private void OnTrendingMovieSelected(GUIListItem item, GUIControl parent)
        {
            TraktTrendingMovie movie = item.TVTag as TraktTrendingMovie;
            if (movie == null)
            {
                GUICommon.ClearMovieProperties();
                return;
            }

            GUICommon.SetProperty("#Trakt.Movie.Watchers", movie.Watchers.ToString());
            GUICommon.SetProperty("#Trakt.Movie.Watchers.Extra", movie.Watchers > 1 ? string.Format(Translation.PeopleWatching, movie.Watchers) : Translation.PersonWatching);
            GUICommon.SetMovieProperties(movie);
        }

        private void GUIWindowManager_Receivers(GUIMessage message)
        {
            if (!IsDashBoardWindow()) return;

            switch (message.Message)
            {                   
                case GUIMessage.MessageType.GUI_MSG_CLICKED:
                    if (message.SenderControlId == (int)TraktDashboardControls.ToggleTrendingCheckButton)
                    {
                        TraktSettings.DashboardMovieTrendingActive = !TraktSettings.DashboardMovieTrendingActive;
                        SetTrendingVisibility();
                    }

                    if (message.Param1 != 7) return; // mouse click, enter key, remote ok, only
                    if (message.SenderControlId == (int)TraktDashboardControls.ActivityFacade)
                    {
                        PlayActivityItem(true);
                    }
                    if (message.SenderControlId == (int)TraktDashboardControls.TrendingShowsFacade)
                    {
                        PlayShow(true);
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
                        ShowActivityContextMenu();
                    }
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
                    if (activeWindow.GetFocusControlId() == (int)TraktDashboardControls.TrendingShowsFacade)
                    {
                        var control = GetFacade(activeWindow.GetFocusControlId());
                        if (control == null) return;

                        if (control.CurrentLayout != GUIFacadeControl.Layout.Filmstrip) return;

                        // set focus on correct control
                        GUIControl.FocusControl(GUIWindowManager.ActiveWindow, (int)TraktDashboardControls.TrendingMoviesFacade);
                    }
                    else if (activeWindow.GetFocusControlId() == (int)TraktDashboardControls.TrendingMoviesFacade)
                    {
                        var control = GetFacade(activeWindow.GetFocusControlId());
                        if (control == null) return;

                        if (control.CurrentLayout != GUIFacadeControl.Layout.Filmstrip) return;

                        // set focus on correct control
                        GUIControl.FocusControl(GUIWindowManager.ActiveWindow, (int)TraktDashboardControls.ActivityFacade);
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
            if (string.IsNullOrEmpty(TraktSettings.Username) || string.IsNullOrEmpty(TraktSettings.Password))
                return;

            GUIWindowManager.Receivers += new SendMessageHandler(GUIWindowManager_Receivers);
            GUIWindowManager.OnNewAction +=new OnActionHandler(GUIWindowManager_OnNewAction);

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

            if (TraktSkinSettings.DashBoardTrendingMoviesWindows != null && TraktSkinSettings.DashBoardTrendingMoviesWindows.Count > 0)
            {
                TrendingMoviesTimer = new Timer(new TimerCallback((o) => { LoadTrendingMovies(); }), null, Timeout.Infinite, Timeout.Infinite);
            }

            if (TraktSkinSettings.DashBoardTrendingShowsWindows != null && TraktSkinSettings.DashBoardTrendingShowsWindows.Count > 0)
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

    public class GUITraktDashboardListItem : GUIListItem
    {
        public GUITraktDashboardListItem(string strLabel) : base(strLabel) { }

        public object Item
        {
            get { return _Item; }
            set
            {
                _Item = value;
                INotifyPropertyChanged notifier = value as INotifyPropertyChanged;
                if (notifier != null) notifier.PropertyChanged += (s, e) =>
                {
                    if (s is TraktUserProfile && e.PropertyName == "AvatarFilename")
                        SetImageToGui((s as TraktUserProfile).AvatarFilename);
                    if (s is TraktShow.ShowImages && e.PropertyName == "PosterImageFilename")
                        SetImageToGui((s as TraktShow.ShowImages).PosterImageFilename);
                    if (s is TraktMovie.MovieImages && e.PropertyName == "PosterImageFilename")
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

            string texture = imageFilePath;
            MainOverlayImage mainOverlay = MainOverlayImage.None;

            if (TVTag is TraktTrendingMovie)
            {
                // determine the overlay to add to poster
                TraktTrendingMovie movie = TVTag as TraktTrendingMovie;

                if (movie.InWatchList)
                    mainOverlay = MainOverlayImage.Watchlist;
                else if (movie.Watched)
                    mainOverlay = MainOverlayImage.Seenit;

                // add additional overlay if applicable
                if (movie.InCollection)
                    mainOverlay |= MainOverlayImage.Library;

                RatingOverlayImage ratingOverlay = GUIImageHandler.GetRatingOverlay(movie.RatingAdvanced);

                // get a reference to a MediaPortal Texture Identifier
                string suffix = mainOverlay.ToString().Replace(", ", string.Empty) + Enum.GetName(typeof(RatingOverlayImage), ratingOverlay);
                string textureName = GUIImageHandler.GetTextureIdentFromFile(imageFilePath, suffix);

                // build memory image
                Image memoryImage = null;
                if (mainOverlay != MainOverlayImage.None || ratingOverlay != RatingOverlayImage.None)
                {
                    memoryImage = GUIImageHandler.DrawOverlayOnPoster(imageFilePath, mainOverlay, ratingOverlay);
                    if (memoryImage != null)
                    {
                        // load texture into facade item
                        if (GUITextureManager.LoadFromMemory(memoryImage, textureName, 0, 0, 0) > 0)
                        {
                            texture = textureName;
                        }
                    }
                }
            }
            else if (TVTag is TraktTrendingShow)
            {
                // determine the overlays to add to poster
                TraktTrendingShow show = TVTag as TraktTrendingShow;

                if (show.InWatchList)
                    mainOverlay = MainOverlayImage.Watchlist;
                else if (show.Watched)
                    mainOverlay = MainOverlayImage.Seenit;

                RatingOverlayImage ratingOverlay = GUIImageHandler.GetRatingOverlay(show.RatingAdvanced);

                // get a reference to a MediaPortal Texture Identifier
                string suffix = Enum.GetName(typeof(MainOverlayImage), mainOverlay) + Enum.GetName(typeof(RatingOverlayImage), ratingOverlay);
                string textureName = GUIImageHandler.GetTextureIdentFromFile(imageFilePath, suffix);

                // build memory image
                Image memoryImage = null;
                if (mainOverlay != MainOverlayImage.None || ratingOverlay != RatingOverlayImage.None)
                {
                    memoryImage = GUIImageHandler.DrawOverlayOnPoster(imageFilePath, mainOverlay, ratingOverlay);
                    if (memoryImage != null)
                    {
                        // load texture into facade item
                        if (GUITextureManager.LoadFromMemory(memoryImage, textureName, 0, 0, 0) > 0)
                        {
                            texture = textureName;
                        }
                    }
                }
            }

            ThumbnailImage = texture;
            IconImage = texture;
            IconImageBig = texture;
        }
    }
}
