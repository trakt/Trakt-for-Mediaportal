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
    public enum ActivityType
    {
        episode,
        list,
        movie,
        show
    }

    internal class TraktDashboard
    {
        #region Enums

        #endregion

        #region Private Variables
        
        private long ActivityStartTime = 0;

        private Timer ActivityTimer = null;
        private Timer TrendingMoviesTimer = null;
        private Timer TrendingShowsTimer = null;
        private Timer StatisticsTimer = null;

        bool GetFullActivityLoad = false;
        bool TrendingContextMenuIsActive = false;

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
            int i = 0;
            GUIFacadeControl facade = null;

            // window init message does not work unless overridden from a guiwindow class
            // so we need to be ensured that the window is fully loaded
            // before we can get reference to a skin control
            try
            {
                do
                {
                    // get current window
                    var window = GUIWindowManager.GetWindow(GUIWindowManager.ActiveWindow);

                    // get facade control
                    facade = window.GetControl(facadeID) as GUIFacadeControl;
                    if (facade == null) Thread.Sleep(100);

                    i++;
                }
                while (i < 50 && facade == null);
            }
            catch (Exception ex)
            {
                TraktLogger.Error("MediaPortal failed to get the active control");
                TraktLogger.Error(ex.StackTrace);
            }

            if (facade == null)
            {
                TraktLogger.Debug("Unable to find Facade [id:{0}], check that trakt skin settings are correctly defined!", facadeID.ToString());
            }

            return facade;
        }

        private void GetStatistics()
        {
            Thread.CurrentThread.Name = "DashStats";

            // initial publish from persisted settings
            if (TraktSettings.LastStatistics != null)
            {
                GUICommon.SetStatisticProperties(TraktSettings.LastStatistics);
                TraktSettings.LastStatistics = null;
            }

            // retrieve statistics from online
            var userProfile = TraktAPI.TraktAPI.GetUserProfile(TraktSettings.Username);
            if (userProfile != null)
            {
                GUICommon.SetStatisticProperties(userProfile.Stats);
                PreviousStatistics = userProfile.Stats;
            }
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
            Thread.CurrentThread.Name = "DashActivity";

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
                    if (facade.Count == 0 && PreviousActivity != null && PreviousActivity.Activities != null && PreviousActivity.Activities.Count > 0)
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
                var avatarImages = new List<TraktImage>();
                foreach (var activity in activities.Activities.Take(TraktSkinSettings.DashboardActivityPropertiesMaxItems))
                {
                    avatarImages.Add(new TraktImage { Avatar = activity.User.Avatar });
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
                GUIUtils.SetProperty(string.Format("#Trakt.Activity.{0}.Time", i), activities[i].Timestamp.FromEpoch().ToLocalTime().ToShortTimeString());
                GUIUtils.SetProperty(string.Format("#Trakt.Activity.{0}.Day", i), activities[i].Timestamp.FromEpoch().ToLocalTime().DayOfWeek.ToString().Substring(0,3));
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

            TraktLogger.Debug("Loading Trakt Activity Facade");

            // stop any existing image downloads
            GUIUserListItem.StopDownload = true;

            // clear facade
            GUIControl.ClearControl(GUIWindowManager.ActiveWindow, facade.GetID);

            int itemId = 0;
            int PreviousSelectedIdx = -1;
            var userImages = new List<TraktImage>();

            // Add each activity item to the facade
            foreach (var activity in activities.Activities.Distinct().OrderByDescending(a => a.Timestamp))
            {
                if (PreviousSelectedIdx == -1 && PreviousSelectedActivity != null && TraktSettings.RememberLastSelectedActivity)
                {
                    if (activity.Equals(PreviousSelectedActivity))
                        PreviousSelectedIdx = itemId;
                }

                var item = new GUIUserListItem(GUICommon.GetActivityListItemTitle(activity), GUIWindowManager.ActiveWindow);

                string activityImage = GetActivityImage(activity);
                string avatarImage = GetAvatarImage(activity);

                // add image to download
                var images = new TraktImage { Avatar = activity.User.Avatar };
                if (avatarImage == "defaultTraktUser.png") userImages.Add(images);
                    
                item.Label2 = activity.Timestamp.FromEpoch().ToLocalTime().ToShortTimeString();
                item.TVTag = activity;
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
            facade.SetCurrentLayout(TraktSkinSettings.DashboardActivityFacadeType);
            facade.SetVisibleFromSkinCondition();

            // Select previously selected item
            if (facade.LayoutControl.IsFocused && PreviousSelectedIdx >= 0)
                facade.SelectIndex(PreviousSelectedIdx);

            // set facade properties
            GUIUtils.SetProperty("#Trakt.Activity.Count", activities.Activities.Count().ToString());
            GUIUtils.SetProperty("#Trakt.Activity.Items", string.Format("{0} {1}", activities.Activities.Count().ToString(), activities.Activities.Count() > 1 ? Translation.Activities : Translation.Activity));
            GUIUtils.SetProperty("#Trakt.Activity.Description", TraktSettings.ShowCommunityActivity ? Translation.ActivityCommunityDesc : Translation.ActivityFriendsDesc);

            // Download avatar images Async and set to facade
            GUIUserListItem.StopDownload = false;
            GUIUserListItem.GetImages(userImages);

            TraktLogger.Debug("Finished Loading Activity facade");
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
                    var movieImages = new List<TraktImage>();
                    foreach (var movie in trendingMovies)
                    {
                        movieImages.Add(new TraktImage { MovieImages = movie.Images });
                    }
                    GUIMovieListItem.GetImages(movieImages);
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

            if (TraktSettings.FilterTrendingOnDashboard)
                movies = GUICommon.FilterTrendingMovies(movies);

            var movieList = movies.ToList();
            int maxItems = movies.Count() < GetMaxTrendingProperties() ? movies.Count() : GetMaxTrendingProperties();

            for (int i = 0; i < maxItems; i++)
            {
                var movie = movieList[i];
                if (movie == null) continue;

                GUICommon.SetProperty(string.Format("#Trakt.Movie.{0}.Watchers", i), movie.Watchers.ToString());
                GUICommon.SetProperty(string.Format("#Trakt.Movie.{0}.Watchers.Extra", i), movie.Watchers > 1 ? string.Format(Translation.PeopleWatching, movie.Watchers) : Translation.PersonWatching);

                GUICommon.SetProperty(string.Format("#Trakt.Movie.{0}.Imdb", i), movie.IMDBID);
                GUICommon.SetProperty(string.Format("#Trakt.Movie.{0}.Certification", i), movie.Certification);
                GUICommon.SetProperty(string.Format("#Trakt.Movie.{0}.Overview", i), string.IsNullOrEmpty(movie.Overview) ? Translation.NoMovieSummary : movie.Overview);
                GUICommon.SetProperty(string.Format("#Trakt.Movie.{0}.Released", i), movie.Released.FromEpoch().ToShortDateString());
                GUICommon.SetProperty(string.Format("#Trakt.Movie.{0}.Runtime", i), movie.Runtime.ToString());
                GUICommon.SetProperty(string.Format("#Trakt.Movie.{0}.Tagline", i), movie.Tagline);
                GUICommon.SetProperty(string.Format("#Trakt.Movie.{0}.Title", i), movie.Title);
                GUICommon.SetProperty(string.Format("#Trakt.Movie.{0}.Tmdb", i), movie.TMDBID);
                GUICommon.SetProperty(string.Format("#Trakt.Movie.{0}.Trailer", i), movie.Trailer);
                GUICommon.SetProperty(string.Format("#Trakt.Movie.{0}.Url", i), movie.Url);
                GUICommon.SetProperty(string.Format("#Trakt.Movie.{0}.Year", i), movie.Year.ToString());
                GUICommon.SetProperty(string.Format("#Trakt.Movie.{0}.Genres", i), string.Join(", ", movie.Genres.ToArray()));
                GUICommon.SetProperty(string.Format("#Trakt.Movie.{0}.PosterImageFilename", i), movie.Images.Poster.LocalImageFilename(ArtworkType.MoviePoster));
                GUICommon.SetProperty(string.Format("#Trakt.Movie.{0}.FanartImageFilename", i), movie.Images.Fanart.LocalImageFilename(ArtworkType.MovieFanart));
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

        private void ClearMovieProperties()
        {
            for (int i = 0; i < GetMaxTrendingProperties(); i++)
            {
                GUIUtils.SetProperty(string.Format("#Trakt.Movie.{0}.Watchers", i), string.Empty);
                GUIUtils.SetProperty(string.Format("#Trakt.Movie.{0}.Watchers.Extra", i), string.Empty);

                GUIUtils.SetProperty(string.Format("#Trakt.Movie.{0}.Imdb", i), string.Empty);
                GUIUtils.SetProperty(string.Format("#Trakt.Movie.{0}.Certification", i), string.Empty);
                GUIUtils.SetProperty(string.Format("#Trakt.Movie.{0}.Overview", i), string.Empty);
                GUIUtils.SetProperty(string.Format("#Trakt.Movie.{0}.Released", i), string.Empty);
                GUIUtils.SetProperty(string.Format("#Trakt.Movie.{0}.Runtime", i), string.Empty);
                GUIUtils.SetProperty(string.Format("#Trakt.Movie.{0}.Tagline", i), string.Empty);
                GUIUtils.SetProperty(string.Format("#Trakt.Movie.{0}.Title", i), string.Empty);
                GUIUtils.SetProperty(string.Format("#Trakt.Movie.{0}.Tmdb", i), string.Empty);
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
                GUIUtils.SetProperty(string.Format("#Trakt.Movie.{0}.Ratings.HatedCount", i), string.Empty);
                GUIUtils.SetProperty(string.Format("#Trakt.Movie.{0}.Ratings.LovedCount", i), string.Empty);
                GUIUtils.SetProperty(string.Format("#Trakt.Movie.{0}.Ratings.Percentage", i), string.Empty);
                GUIUtils.SetProperty(string.Format("#Trakt.Movie.{0}.Ratings.Votes", i), string.Empty);
            }
        }

        private void LoadTrendingMoviesFacade(IEnumerable<TraktTrendingMovie> movies, GUIFacadeControl facade)
        {
            if (TraktSkinSettings.DashboardTrendingCollection == null || !TraktSkinSettings.DashboardTrendingCollection.Exists(d => d.MovieWindows.Contains(GUIWindowManager.ActiveWindow.ToString())))
                return;
            
            // get trending settings for window
            var trendingSettings = GetTrendingSettings();
            if (trendingSettings == null) return;

            TraktLogger.Debug("Loading Trakt Trending Movies facade");

            // if no trending, then nothing to do
            if (movies == null || movies.Count() == 0)
                return;

            // stop any existing image downloads
            GUIMovieListItem.StopDownload = true;

            // clear facade
            GUIControl.ClearControl(GUIWindowManager.ActiveWindow, facade.GetID);

            int itemId = 0;
            var movieImages = new List<TraktImage>();

            // filter movies
            if (TraktSettings.FilterTrendingOnDashboard)
                movies = GUICommon.FilterTrendingMovies(movies);

            // Add each activity item to the facade
            foreach (var movie in movies.Take(trendingSettings.FacadeMaxItems))
            {
                // add image for download
                var images = new TraktImage { MovieImages = movie.Images };
                movieImages.Add(images);

                var item = new GUIMovieListItem(movie.Title, GUIWindowManager.ActiveWindow);

                item.Label2 = movie.Year.ToString();
                item.TVTag = movie;
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
            facade.SetCurrentLayout(trendingSettings.FacadeType);
            facade.SetVisibleFromSkinCondition();

            // set facade properties
            GUIUtils.SetProperty("#Trakt.Trending.Movies.Items", string.Format("{0} {1}", movies.Count().ToString(), movies.Count() > 1 ? Translation.Movies : Translation.Movie));
            GUIUtils.SetProperty("#Trakt.Trending.Movies.PeopleCount", movies.Sum(s => s.Watchers).ToString());
            GUIUtils.SetProperty("#Trakt.Trending.Movies.Description", string.Format(Translation.TrendingTVShowsPeople, movies.Sum(s => s.Watchers).ToString(), movies.Count().ToString()));

            // Download images Async and set to facade
            GUIMovieListItem.StopDownload = false;
            GUIMovieListItem.GetImages(movieImages);

            SetTrendingVisibility();

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
                    var showImages = new List<TraktImage>();
                    foreach (var show in trendingShows)
                    {
                        showImages.Add(new TraktImage { ShowImages = show.Images });
                    }
                    GUIShowListItem.GetImages(showImages);
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

            if (TraktSettings.FilterTrendingOnDashboard)
                shows = GUICommon.FilterTrendingShows(shows);

            var showList = shows.ToList();
            int maxItems = shows.Count() < GetMaxTrendingProperties() ? shows.Count() : GetMaxTrendingProperties();

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
                GUICommon.SetProperty(string.Format("#Trakt.Show.{0}.FanartImageFilename", i), show.Images.Fanart.LocalImageFilename(ArtworkType.ShowFanart));
            }
        }

        private void ClearShowProperties()
        {
            for (int i = 0; i < GetMaxTrendingProperties(); i++)
            {
                GUIUtils.SetProperty(string.Format("#Trakt.Show.{0}.Watchers", i), string.Empty);
                GUIUtils.SetProperty(string.Format("#Trakt.Show.{0}.Watchers.Extra", i), string.Empty);

                GUIUtils.SetProperty(string.Format("#Trakt.Show.{0}.Imdb", i), string.Empty);
                GUIUtils.SetProperty(string.Format("#Trakt.Show.{0}.Tvdb", i), string.Empty);
                GUIUtils.SetProperty(string.Format("#Trakt.Show.{0}.TvRage", i), string.Empty);
                GUIUtils.SetProperty(string.Format("#Trakt.Show.{0}.Title", i), string.Empty);
                GUIUtils.SetProperty(string.Format("#Trakt.Show.{0}.Url", i), string.Empty);
                GUIUtils.SetProperty(string.Format("#Trakt.Show.{0}.AirDay", i), string.Empty);
                GUIUtils.SetProperty(string.Format("#Trakt.Show.{0}.AirTime", i), string.Empty);
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
                GUIUtils.SetProperty(string.Format("#Trakt.Show.{0}.Plays", i), string.Empty);
                GUIUtils.SetProperty(string.Format("#Trakt.Show.{0}.Rating", i), string.Empty);
                GUIUtils.SetProperty(string.Format("#Trakt.Show.{0}.RatingAdvanced", i), string.Empty);
                GUIUtils.SetProperty(string.Format("#Trakt.Show.{0}.Ratings.Icon", i), string.Empty);
                GUIUtils.SetProperty(string.Format("#Trakt.Show.{0}.Ratings.HatedCount", i), string.Empty);
                GUIUtils.SetProperty(string.Format("#Trakt.Show.{0}.Ratings.LovedCount", i), string.Empty);
                GUIUtils.SetProperty(string.Format("#Trakt.Show.{0}.Ratings.Percentage", i), string.Empty);
                GUIUtils.SetProperty(string.Format("#Trakt.Show.{0}.Ratings.Votes", i), string.Empty);
                GUIUtils.SetProperty(string.Format("#Trakt.Show.{0}.FanartImageFilename", i), string.Empty);
            }
        }

        private void LoadTrendingShowsFacade(IEnumerable<TraktTrendingShow> shows, GUIFacadeControl facade)
        {
            if (TraktSkinSettings.DashboardTrendingCollection == null || !TraktSkinSettings.DashboardTrendingCollection.Exists(d => d.MovieWindows.Contains(GUIWindowManager.ActiveWindow.ToString())))
                return;

            // get trending settings
            var trendingSettings = GetTrendingSettings();
            if (trendingSettings == null) return;

            TraktLogger.Debug("Loading Trakt Trending Shows facade");

            // if no trending, then nothing to do
            if (shows == null || shows.Count() == 0)
                return;

            // stop any existing image downloads
            GUIShowListItem.StopDownload = true;

            // clear facade
            GUIControl.ClearControl(GUIWindowManager.ActiveWindow, facade.GetID);

            int itemId = 0;
            var showImages = new List<TraktImage>();

            // filter shows
            if (TraktSettings.FilterTrendingOnDashboard)
                shows = GUICommon.FilterTrendingShows(shows);

            // Add each activity item to the facade
            foreach (var show in shows.Take(trendingSettings.FacadeMaxItems))
            {
                // add image for download
                var images = new TraktImage { ShowImages = show.Images };
                showImages.Add(images);

                var item = new GUIShowListItem(show.Title, GUIWindowManager.ActiveWindow);

                item.Label2 = show.Year.ToString();
                item.TVTag = show;
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
            facade.SetCurrentLayout(trendingSettings.FacadeType);
            facade.SetVisibleFromSkinCondition();

            // set facade properties
            GUIUtils.SetProperty("#Trakt.Trending.Shows.Items", string.Format("{0} {1}", shows.Count().ToString(), shows.Count() > 1 ? Translation.SeriesPlural : Translation.Series));
            GUIUtils.SetProperty("#Trakt.Trending.Shows.PeopleCount", shows.Sum(s => s.Watchers).ToString());
            GUIUtils.SetProperty("#Trakt.Trending.Shows.Description", string.Format(Translation.TrendingTVShowsPeople, shows.Sum(s => s.Watchers).ToString(), shows.Count().ToString()));

            // Download images Async and set to facade
            GUIShowListItem.StopDownload = false;
            GUIShowListItem.GetImages(showImages);
            
            SetTrendingVisibility();

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
                    imageFilename = int.Parse(activity.RatingAdvanced) > 5 ? "traktActivityLove.png" : "traktActivityHate.png";
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
                    imageFilename = "traktActivityList.png";
                    break;
            }

            return imageFilename;
        }

        private string GetAvatarImage(TraktActivity.Activity activity)
        {
            string filename = activity.User.Avatar.LocalImageFilename(ArtworkType.Avatar);
            if (string.IsNullOrEmpty(filename) || !System.IO.File.Exists(filename))
            {
                filename = "defaultTraktUser.png";
            }
            return filename;
        }

        private string GetActivityShoutText(TraktActivity.Activity activity)
        {
            if (activity.Action != ActivityAction.shout.ToString()) return string.Empty;
            if (activity.Shout.Spoiler) return Translation.HiddenToPreventSpoilers;
            return activity.Shout.Text;
        }

        private string GetActivityReviewText(TraktActivity.Activity activity)
        {
            if (activity.Action != ActivityAction.review.ToString()) return string.Empty;
            if (activity.Review.Spoiler) return Translation.HiddenToPreventSpoilers;
            return activity.Review.Text;
        }

        private IEnumerable<TraktTrendingMovie> GetTrendingMovies(out bool isCached)
        {
            isCached = false;
            double timeSinceLastUpdate = DateTime.Now.Subtract(LastTrendingMovieUpdate).TotalMilliseconds;

            if (PreviousTrendingMovies == null || TraktSettings.DashboardTrendingPollInterval <= timeSinceLastUpdate)
            {
                TraktLogger.Debug("Getting trending movies from trakt");
                var trendingMovies = TraktAPI.TraktAPI.GetTrendingMovies();
                if (trendingMovies != null && trendingMovies.Count() > 0)
                {
                    LastTrendingMovieUpdate = DateTime.Now;
                    PreviousTrendingMovies = trendingMovies;
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

        private IEnumerable<TraktTrendingShow> GetTrendingShows(out bool isCached)
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
                    PreviousTrendingShows = trendingShows;
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

            if (PreviousActivity == null || PreviousActivity.Activities == null || ActivityStartTime <= 0 || GetFullActivityLoad)
            {
                PreviousActivity = community ? TraktAPI.TraktAPI.GetCommunityActivity() : TraktAPI.TraktAPI.GetFriendActivity(TraktSettings.IncludeMeInFriendsActivity);
                GetFullActivityLoad = false;

                // check that we have any friend activity, if not switch to friends+me
                // not everyone has friends! We could use friend count but that means an extra uneeded request
                if (PreviousActivity == null || PreviousActivity.Activities == null || PreviousActivity.Activities.Count == 0)
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

        private void ShowTrendingShowsContextMenu()
        {
            var trendingShowsFacade = GetFacade((int)TraktDashboardControls.TrendingShowsFacade);
            if (trendingShowsFacade == null) return;

            var dlg = (IDialogbox)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
            if (dlg == null) return;

            dlg.Reset();
            dlg.SetHeading(GUIUtils.PluginName());

            var selectedItem = trendingShowsFacade.SelectedListItem;
            var selectedShow = selectedItem.TVTag as TraktTrendingShow;

            GUICommon.CreateTrendingShowsContextMenu(ref dlg, selectedShow, true);

            // Show Context Menu
            dlg.DoModal(GUIWindowManager.ActiveWindow);
            if (dlg.SelectedId < 0) return;

            switch (dlg.SelectedId)
            {
                case ((int)TrendingContextMenuItem.AddToWatchList):
                    TraktHelper.AddShowToWatchList(selectedShow);
                    selectedShow.InWatchList = true;
                    OnTrendingShowSelected(selectedItem, trendingShowsFacade);
                    (selectedItem as GUIShowListItem).Images.NotifyPropertyChanged("Poster");
                    break;

                case ((int)TrendingContextMenuItem.ShowSeasonInfo):
                    GUIWindowManager.ActivateWindow((int)TraktGUIWindows.ShowSeasons, selectedShow.ToJSON());
                    break;

                case ((int)TrendingContextMenuItem.MarkAsWatched):
                    GUICommon.MarkShowAsSeen(selectedShow);
                    break;

                case ((int)TrendingContextMenuItem.AddToLibrary):
                    GUICommon.AddShowToLibrary(selectedShow);
                    break;

                case ((int)TrendingContextMenuItem.RemoveFromWatchList):
                    TraktHelper.RemoveShowFromWatchList(selectedShow);
                    selectedShow.InWatchList = false;
                    OnTrendingShowSelected(selectedItem, trendingShowsFacade);
                    (selectedItem as GUIShowListItem).Images.NotifyPropertyChanged("Poster");
                    break;

                case ((int)TrendingContextMenuItem.AddToList):
                    TraktHelper.AddRemoveShowInUserList(selectedShow.Title, selectedShow.Year.ToString(), selectedShow.Tvdb, false);
                    break;

                case ((int)TrendingContextMenuItem.Related):
                    TraktHelper.ShowRelatedShows(selectedShow);
                    break;

                case ((int)TrendingContextMenuItem.Filters):
                    if (GUICommon.ShowTVShowFiltersMenu())
                        LoadTrendingShows(true);
                    break;

                case ((int)TrendingContextMenuItem.Trailers):
                    GUICommon.ShowTVShowTrailersMenu(selectedShow);
                    break;

                case ((int)TrendingContextMenuItem.Shouts):
                    TraktHelper.ShowTVShowShouts(selectedShow);
                    break;

                case ((int)TrendingContextMenuItem.Rate):
                    GUICommon.RateShow(selectedShow);
                    OnTrendingShowSelected(selectedItem, trendingShowsFacade);
                    (selectedItem as GUIShowListItem).Images.NotifyPropertyChanged("Poster");
                    break;

                case ((int)TrendingContextMenuItem.SearchWithMpNZB):
                    string loadingParam = string.Format("search:{0}", selectedShow.Title);
                    GUIWindowManager.ActivateWindow((int)ExternalPluginWindows.MpNZB, loadingParam);
                    break;

                case ((int)TrendingContextMenuItem.SearchTorrent):
                    string loadPar = selectedShow.Title;
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

            var selectedItem = trendingMoviesFacade.SelectedListItem;
            var selectedMovie = selectedItem.TVTag as TraktTrendingMovie;

            GUICommon.CreateTrendingMoviesContextMenu(ref dlg, selectedMovie, true);

            // Show Context Menu
            dlg.DoModal(GUIWindowManager.ActiveWindow);
            if (dlg.SelectedId < 0) return;

            switch (dlg.SelectedId)
            {
                case ((int)TrendingContextMenuItem.MarkAsWatched):
                    TraktHelper.MarkMovieAsWatched(selectedMovie);
                    if (selectedMovie.Plays == 0) selectedMovie.Plays = 1;
                    selectedMovie.Watched = true;
                    selectedItem.IsPlayed = true;
                    OnTrendingMovieSelected(selectedItem, trendingMoviesFacade);
                    (selectedItem as GUIMovieListItem).Images.NotifyPropertyChanged("Poster");
                    break;

                case ((int)TrendingContextMenuItem.MarkAsUnWatched):
                    TraktHelper.MarkMovieAsUnWatched(selectedMovie);
                    selectedMovie.Watched = false;
                    selectedItem.IsPlayed = false;
                    OnTrendingMovieSelected(selectedItem, trendingMoviesFacade);
                    (selectedItem as GUIMovieListItem).Images.NotifyPropertyChanged("Poster");
                    break;

                case ((int)TrendingContextMenuItem.AddToWatchList):
                    TraktHelper.AddMovieToWatchList(selectedMovie, true);
                    selectedMovie.InWatchList = true;
                    OnTrendingMovieSelected(selectedItem, trendingMoviesFacade);
                    (selectedItem as GUIMovieListItem).Images.NotifyPropertyChanged("Poster");
                    break;

                case ((int)TrendingContextMenuItem.RemoveFromWatchList):
                    TraktHelper.RemoveMovieFromWatchList(selectedMovie, true);
                    selectedMovie.InWatchList = false;
                    OnTrendingMovieSelected(selectedItem, trendingMoviesFacade);
                    (selectedItem as GUIMovieListItem).Images.NotifyPropertyChanged("Poster");
                    break;

                case ((int)TrendingContextMenuItem.AddToList):
                    TraktHelper.AddRemoveMovieInUserList(selectedMovie, false);
                    break;

                case ((int)TrendingContextMenuItem.Filters):
                    if (GUICommon.ShowMovieFiltersMenu())
                        LoadTrendingMovies(true);
                    break;

                case ((int)TrendingContextMenuItem.AddToLibrary):
                    TraktHelper.AddMovieToLibrary(selectedMovie);
                    selectedMovie.InCollection = true;
                    OnTrendingMovieSelected(selectedItem, trendingMoviesFacade);
                    (selectedItem as GUIMovieListItem).Images.NotifyPropertyChanged("Poster");
                    break;

                case ((int)TrendingContextMenuItem.RemoveFromLibrary):
                    TraktHelper.RemoveMovieFromLibrary(selectedMovie);
                    selectedMovie.InCollection = false;
                    OnTrendingMovieSelected(selectedItem, trendingMoviesFacade);
                    (selectedItem as GUIMovieListItem).Images.NotifyPropertyChanged("Poster");
                    break;

                case ((int)TrendingContextMenuItem.Related):
                    TraktHelper.ShowRelatedMovies(selectedMovie);
                    break;

                case ((int)TrendingContextMenuItem.Rate):
                    GUICommon.RateMovie(selectedMovie);
                    OnTrendingMovieSelected(selectedItem, trendingMoviesFacade);
                    (selectedItem as GUIMovieListItem).Images.NotifyPropertyChanged("Poster");
                    break;

                case ((int)TrendingContextMenuItem.Shouts):
                    TraktHelper.ShowMovieShouts(selectedMovie);
                    break;

                case ((int)TrendingContextMenuItem.Trailers):
                    GUICommon.ShowMovieTrailersMenu(selectedMovie);
                    break;

                case ((int)TrendingContextMenuItem.SearchWithMpNZB):
                    string loadingParam = string.Format("search:{0}", selectedMovie.Title);
                    GUIWindowManager.ActivateWindow((int)ExternalPluginWindows.MpNZB, loadingParam);
                    break;

                case ((int)TrendingContextMenuItem.SearchTorrent):
                    string loadPar = selectedMovie.Title;
                    GUIWindowManager.ActivateWindow((int)ExternalPluginWindows.MyTorrents, loadPar);
                    break;

                default:
                    break;
            }
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
                listItem.ItemId = (int)ActivityContextMenuItem.ShowCommunityActivity;

                if (!TraktSettings.IncludeMeInFriendsActivity)
                {
                    listItem = new GUIListItem(Translation.IncludeMeInFriendsActivity);
                    dlg.Add(listItem);
                    listItem.ItemId = (int)ActivityContextMenuItem.IncludeMeInFriendsActivity;
                }
                else
                {
                    listItem = new GUIListItem(Translation.DontIncludeMeInFriendsActivity);
                    dlg.Add(listItem);
                    listItem.ItemId = (int)ActivityContextMenuItem.DontIncludeMeInFriendsActivity;
                }
            }
            else
            {
                listItem = new GUIListItem(Translation.ShowFriendActivity);
                dlg.Add(listItem);
                listItem.ItemId = (int)ActivityContextMenuItem.ShowFriendActivity;
            }

            var activity = activityFacade.SelectedListItem.TVTag as TraktActivity.Activity;

            if (activity != null && !string.IsNullOrEmpty(activity.Action) && !string.IsNullOrEmpty(activity.Type))
            {
                // userprofile - only load for unprotected users
                if (!activity.User.Protected || !TraktSettings.ShowCommunityActivity)
                {
                    listItem = new GUIListItem(Translation.UserProfile);
                    dlg.Add(listItem);
                    listItem.ItemId = (int)ActivityContextMenuItem.UserProfile;
                }

                if (TraktSettings.ShowCommunityActivity && !((activityFacade.SelectedListItem as GUIUserListItem).IsFollowed))
                {
                    // allow user to follow person
                    listItem = new GUIListItem(Translation.Follow);
                    dlg.Add(listItem);
                    listItem.ItemId = (int)ActivityContextMenuItem.FollowUser;
                }

                // if selected activity is an episode or show, add 'Season Info'
                if (activity.Show != null)
                {
                    listItem = new GUIListItem(Translation.ShowSeasonInfo);
                    dlg.Add(listItem);
                    listItem.ItemId = (int)ActivityContextMenuItem.ShowSeasonInfo;
                }

                // get a list of common actions to perform on the selected item
                if (activity.Movie != null || activity.Show != null)
                {
                    var listItems = GUICommon.GetContextMenuItemsForActivity();
                    foreach (var item in listItems)
                    {
                        int itemId = item.ItemId;
                        dlg.Add(item);
                        item.ItemId = itemId;
                    }
                }
            }

            // Show Context Menu
            dlg.DoModal(GUIWindowManager.ActiveWindow);
            if (dlg.SelectedId < 0) return;

            switch (dlg.SelectedId)
            {
                case ((int)ActivityContextMenuItem.ShowCommunityActivity):
                    TraktSettings.ShowCommunityActivity = true;
                    GetFullActivityLoad = true;
                    StartActivityPolling();
                    break;

                case ((int)ActivityContextMenuItem.ShowFriendActivity):
                    TraktSettings.ShowCommunityActivity = false;
                    GetFullActivityLoad = true;
                    StartActivityPolling();
                    break;

                case ((int)ActivityContextMenuItem.IncludeMeInFriendsActivity):
                    TraktSettings.IncludeMeInFriendsActivity = true;
                    GetFullActivityLoad = true;
                    StartActivityPolling();
                    break;

                case ((int)ActivityContextMenuItem.DontIncludeMeInFriendsActivity):
                    TraktSettings.IncludeMeInFriendsActivity = false;
                    GetFullActivityLoad = true;
                    StartActivityPolling();
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
                        TraktHelper.AddRemoveEpisodeInUserList(activity.Show, activity.Episode, false);
                    else
                        TraktHelper.AddRemoveShowInUserList(activity.Show, false);
                    break;

                case ((int)ActivityContextMenuItem.AddToWatchList):
                    if (activity.Movie != null)
                        TraktHelper.AddMovieToWatchList(activity.Movie, true);
                    else if (activity.Episode != null)
                        TraktHelper.AddEpisodeToWatchList(activity.Show, activity.Episode);
                    else
                        TraktHelper.AddShowToWatchList(activity.Show);
                    break;

                case ((int)ActivityContextMenuItem.Shouts):
                    if (activity.Movie != null)
                        TraktHelper.ShowMovieShouts(activity.Movie);
                    else if (activity.Episode != null)
                        TraktHelper.ShowEpisodeShouts(activity.Show, activity.Episode);
                    else
                        TraktHelper.ShowTVShowShouts(activity.Show);
                    break;

                case ((int)ActivityContextMenuItem.Rate):
                    if (activity.Movie != null)
                        GUICommon.RateMovie(activity.Movie);
                    else if (activity.Episode != null)
                        GUICommon.RateEpisode(activity.Show, activity.Episode);
                    else
                        GUICommon.RateShow(activity.Show);
                    break;

                case ((int)ActivityContextMenuItem.Trailers):
                    if (activity.Movie != null) 
                        GUICommon.ShowMovieTrailersMenu(activity.Movie); 
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
                            GUICommon.CheckAndPlayFirstUnwatched(activity.Show, jumpTo);
                            return;
                        }
                    } 
                    GUICommon.CheckAndPlayEpisode(activity.Show, activity.Episode);
                    break;

                case ActivityType.show:
                    GUICommon.CheckAndPlayFirstUnwatched(activity.Show, jumpTo);
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

                case ActivityType.show:
                    GUICommon.SetShowProperties(activity.Show);
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
            var movie = item.TVTag as TraktTrendingMovie;
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
                        var activityFacade = GetFacade((int)TraktDashboardControls.ActivityFacade);
                        if (activityFacade == null) return;

                        var activity = activityFacade.SelectedListItem.TVTag as TraktActivity.Activity;
                        if (activity == null || string.IsNullOrEmpty(activity.Action) || string.IsNullOrEmpty(activity.Type))
                            return;

                        ActivityAction action = (ActivityAction)Enum.Parse(typeof(ActivityAction), activity.Action);
                        ActivityType type = (ActivityType)Enum.Parse(typeof(ActivityType), activity.Type);

                        switch (action)
                        {
                            case ActivityAction.review:
                            case ActivityAction.shout:
                                // view shout in shouts window
                                ViewShout(activity);
                                break;

                            case ActivityAction.item_added:
                                // load users list
                                GUIListItems.CurrentList = new TraktUserList { Slug = activity.List.Slug, Name = activity.List.Name };
                                GUIListItems.CurrentUser = activity.User.Username;
                                GUIWindowManager.ActivateWindow((int)TraktGUIWindows.ListItems);
                                break;

                            case ActivityAction.created:
                                // load users lists
                                GUILists.CurrentUser = activity.User.Username;
                                GUIWindowManager.ActivateWindow((int)TraktGUIWindows.Lists);
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

                            var selectedShow = facade.SelectedListItem.TVTag as TraktTrendingShow;

                            GUIWindowManager.ActivateWindow((int)TraktGUIWindows.ShowSeasons, selectedShow.ToJSON());
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
