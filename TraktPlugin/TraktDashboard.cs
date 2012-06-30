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
            ShowFriendActivity
        }

        #endregion

        #region Private Variables

        GUIAnimation UpdateAnimationControl = null;

        private long ActivityStartTime = 0;

        private Timer ActivityTimer = null;
        private Timer TrendingMoviesTimer = null;
        private Timer TrendingShowsTimer = null;

        bool StopAvatarDownload = false;
        bool StopTrendingShowsDownload = false;
        bool StopTrendingMoviesDownload = false;

        bool GetFullActivityLoad = false;

        #endregion

        #region Constructor

        public TraktDashboard() { }

        #endregion

        #region Private Methods

        private GUIFacadeControl GetFacade(int facadeID)
        {
            // get current window
            var window = GUIWindowManager.GetWindow(GUIWindowManager.ActiveWindow);

            // get facade control
            var control = window.GetControl(facadeID);
            return control as GUIFacadeControl;
        }

        private void ClearActivityProperties()
        {
            GUIUtils.SetProperty("#Trakt.Selected.Activity.Type", "none");
            GUIUtils.SetProperty("#Trakt.Selected.Activity.Action", "none");

            GUICommon.ClearEpisodeProperties();
            GUICommon.ClearMovieProperties();
            GUICommon.ClearEpisodeProperties();

            GUIUtils.SetProperty("#Trakt.Activity.Count", "0");
            GUIUtils.SetProperty("#Trakt.Activity.Items", string.Format("0 {0}", Translation.Activities));
            GUIUtils.SetProperty("#Trakt.Activity.Description", TraktSettings.ShowCommunityActivity ? Translation.ActivityCommunityDesc : Translation.ActivityFriendsDesc);
        }

        private void LoadActivity()
        {
            GUIFacadeControl facade = null;
            int i = 0;

            // get the facade, may need to wait until
            // window has completely loaded
            do
            {
                facade = GetFacade((int)TraktDashboardControls.ActivityFacade);
                if (facade == null) Thread.Sleep(250);
                i++;
            }
            while (i < 10 && facade == null);

            // no luck, possible skinning error
            if (facade == null) return;

            lock (this)
            {
                // load facade if empty and we have activity already
                // facade is empty on re-load of window
                if (facade.Count == 0 && PreviousActivity != null && PreviousActivity.Activities.Count > 0)
                {
                    LoadActivityFacade(PreviousActivity, facade);
                }

                // get latest activity
                var activities = GetActivity(TraktSettings.ShowCommunityActivity);

                // load activity into list
                LoadActivityFacade(activities, facade);
            }
        }

        private void LoadActivityFacade(TraktActivity activities, GUIFacadeControl facade)
        {
            if (!TraktSkinSettings.DashBoardActivityWindows.Contains(GUIWindowManager.ActiveWindow.ToString()))
                return;

            // if no activities report to user
            if (activities == null || activities.Activities.Count == 0)
            {
                GUIListItem item = new GUIListItem(Translation.NoActivities);
                facade.Add(item);
                facade.SetCurrentLayout("List");
                ClearActivityProperties();
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
            facade.SetCurrentLayout("List");

            // set facade properties
            GUIUtils.SetProperty("#Trakt.Activity.Count", activities.Activities.Count().ToString());
            GUIUtils.SetProperty("#Trakt.Activity.Items", string.Format("{0} {1}", activities.Activities.Count().ToString(), activities.Activities.Count() > 1 ? Translation.Activities : Translation.Activity));
            GUIUtils.SetProperty("#Trakt.Activity.Description", TraktSettings.ShowCommunityActivity ? Translation.ActivityCommunityDesc : Translation.ActivityFriendsDesc);

            // Download avatar images Async and set to facade
            StopAvatarDownload = false;
            GetImages<TraktUserProfile>(avatarImages);
        }

        private void LoadTrendingMovies()
        {
            GUIFacadeControl facade = null;
            int i = 0;

            // get the facade, may need to wait until
            // window has completely loaded
            do
            {
                facade = GetFacade((int)TraktDashboardControls.TrendingMoviesFacade);
                if (facade == null) Thread.Sleep(250);
                i++;
            }
            while (i < 10 && facade == null);

            // no luck, possible skinning error
            if (facade == null) return;

            // load facade if empty and we have trending already
            // facade is empty on re-load of window
            if (facade.Count == 0 && PreviousTrendingMovies != null && PreviousTrendingMovies.Count() > 0)
            {
                LoadTrendingMoviesFacade(PreviousTrendingMovies, facade);
            }

            // get latest trending
            PreviousTrendingMovies = TraktAPI.TraktAPI.GetTrendingMovies().Take(10);

            // load trending into list
            LoadTrendingMoviesFacade(PreviousTrendingMovies, facade);
        }

        private void LoadTrendingMoviesFacade(IEnumerable<TraktTrendingMovie> movies, GUIFacadeControl facade)
        {
            if (!TraktSkinSettings.DashBoardTrendingMoviesWindows.Contains(GUIWindowManager.ActiveWindow.ToString()))
                return;

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
            facade.SetCurrentLayout("Filmstrip");

            // set facade properties
            GUIUtils.SetProperty("#Trakt.Trending.Movies.Items", string.Format("{0} {1}", movies.Count().ToString(), movies.Count() > 1 ? Translation.Movies : Translation.Movie));
            GUIUtils.SetProperty("#Trakt.Trending.Movies.PeopleCount", movies.Sum(s => s.Watchers).ToString());
            GUIUtils.SetProperty("#Trakt.Trending.Movies.Description", string.Format(Translation.TrendingTVShowsPeople, movies.Sum(s => s.Watchers).ToString(), movies.Count().ToString()));

            // Download images Async and set to facade
            StopTrendingMoviesDownload = false;
            GetImages<TraktMovie.MovieImages>(movieImages);
        }

        private void LoadTrendingShows()
        {
            GUIFacadeControl facade = null;
            int i = 0;

            // get the facade, may need to wait until
            // window has completely loaded
            do
            {
                facade = GetFacade((int)TraktDashboardControls.TrendingShowsFacade);
                if (facade == null) Thread.Sleep(250);
                i++;
            }
            while (i < 10 && facade == null);

            // no luck, possible skinning error
            if (facade == null) return;

            // load facade if empty and we have trending already
            // facade is empty on re-load of window
            if (facade.Count == 0 && PreviousTrendingShows != null && PreviousTrendingShows.Count() > 0)
            {
                LoadTrendingShowsFacade(PreviousTrendingShows, facade);
            }

            // get latest trending
            PreviousTrendingShows = TraktAPI.TraktAPI.GetTrendingShows().Take(10);

            // load trending into list
            LoadTrendingShowsFacade(PreviousTrendingShows, facade);
        }

        private void LoadTrendingShowsFacade(IEnumerable<TraktTrendingShow> shows, GUIFacadeControl facade)
        {
            if (!TraktSkinSettings.DashBoardTrendingShowsWindows.Contains(GUIWindowManager.ActiveWindow.ToString()))
                return;

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
            facade.SetCurrentLayout("Filmstrip");

            // set facade properties
            GUIUtils.SetProperty("#Trakt.Trending.Shows.Items", string.Format("{0} {1}", shows.Count().ToString(), shows.Count() > 1 ? Translation.SeriesPlural : Translation.Series));
            GUIUtils.SetProperty("#Trakt.Trending.Shows.PeopleCount", shows.Sum(s => s.Watchers).ToString());
            GUIUtils.SetProperty("#Trakt.Trending.Shows.Description", string.Format(Translation.TrendingTVShowsPeople, shows.Sum(s => s.Watchers).ToString(), shows.Count().ToString()));

            // Download images Async and set to facade
            StopTrendingShowsDownload = false;
            GetImages<TraktShow.ShowImages>(showImages);
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
                                name = activity.ListItem.Movie.Title;
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

            return name;
        }

        private TraktActivity GetActivity(bool community)
        {
            SetUpdateAnimation(true);

            if (PreviousActivity == null || ActivityStartTime <= 0 || GetFullActivityLoad)
            {
                PreviousActivity = community ? TraktAPI.TraktAPI.GetCommunityActivity() : TraktAPI.TraktAPI.GetFriendActivity();
                GetFullActivityLoad = false;
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
                    incrementalActivity = TraktAPI.TraktAPI.GetFriendActivity(null, null, ActivityStartTime, DateTime.UtcNow.ToEpoch());
                }

                // join the Previous request with the current
                if (incrementalActivity != null)
                {
                    TraktLogger.Debug("Response: {0}", incrementalActivity.ToJSON());
                    PreviousActivity.Activities = incrementalActivity.Activities.Union(PreviousActivity.Activities).Take(100).ToList();
                    PreviousActivity.Timestamps = incrementalActivity.Timestamps;
                }
            }

            // store current timestamp and only request incremental change next time
            if (PreviousActivity != null)
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
                                }
                                else if (item is TraktMovie.MovieImages)
                                {
                                    if (StopTrendingMoviesDownload) return;
                                    (item as TraktMovie.MovieImages).NotifyPropertyChanged("PosterImageFilename");
                                }
                                else if (item is TraktShow.ShowImages)
                                {
                                    if (StopTrendingShowsDownload) return;
                                    (item as TraktShow.ShowImages).NotifyPropertyChanged("PosterImageFilename");
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

            if (TraktSkinSettings.DashBoardActivityWindows.Contains(GUIWindowManager.ActiveWindow.ToString()))
                hasDashBoard = true;
            if (TraktSkinSettings.DashBoardTrendingMoviesWindows.Contains(GUIWindowManager.ActiveWindow.ToString()))
                hasDashBoard = true;
            if (TraktSkinSettings.DashBoardTrendingShowsWindows.Contains(GUIWindowManager.ActiveWindow.ToString()))
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
                UpdateAnimationControl = control as GUIAnimation;

                if (UpdateAnimationControl != null)
                {
                    if (enable)
                        UpdateAnimationControl.AllocResources();
                    else
                        UpdateAnimationControl.Dispose();

                    UpdateAnimationControl.Visible = enable;
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
                            GUICommon.CheckAndPlayFirstUnwatched(activity.Show);
                            return;
                        }
                    } 
                    GUICommon.CheckAndPlayEpisode(activity.Show, activity.Episode);
                    break;

                case "show":
                    GUICommon.CheckAndPlayFirstUnwatched(activity.Show);
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
                                GUICommon.CheckAndPlayFirstUnwatched(activity.Show);
                                break;

                            case "episode":
                                GUICommon.CheckAndPlayEpisode(activity.Show, activity.Episode);
                                break;

                            case "movie":
                                GUICommon.CheckAndPlayMovie(jumpTo, activity.Movie);
                                break;
                        }
                    }
                    break;
            }

        }

        private void PlayShow()
        {
            // get control
            var facade = GetFacade((int)TraktDashboardControls.TrendingShowsFacade);
            if (facade == null) return;

            // get selected item in facade
            TraktTrendingShow show = facade.SelectedListItem.TVTag as TraktTrendingShow;

            GUICommon.CheckAndPlayFirstUnwatched(show);
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

        #endregion

        #region Event Handlers

        private void OnActivitySelected(GUIListItem item, GUIControl parent)
        {
            TraktActivity.Activity activity = item.TVTag as TraktActivity.Activity;
            if (activity == null)
            {
                ClearActivityProperties();
                return;
            }

            // set type and action properties
            GUIUtils.SetProperty("#Trakt.Selected.Activity.Type", activity.Type);
            GUIUtils.SetProperty("#Trakt.Selected.Activity.Action", activity.Action);

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
                                GUICommon.SetShowProperties(activity.Show);
                                break;

                            case "episode":
                                GUICommon.SetShowProperties(activity.Show);
                                GUICommon.SetEpisodeProperties(activity.Episode);
                                break;

                            case "movie":
                                GUICommon.SetMovieProperties(activity.Movie);
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

            var activeWindow = GUIWindowManager.GetWindow(GUIWindowManager.ActiveWindow);
            
            switch (message.Message)
            {                   
                case GUIMessage.MessageType.GUI_MSG_CLICKED:
                    if (message.SenderControlId == (int)TraktDashboardControls.ActivityFacade)
                    {
                        PlayActivityItem(true);
                    }
                    if (message.SenderControlId == (int)TraktDashboardControls.TrendingShowsFacade)
                    {
                        PlayShow();
                    }
                    if (message.SenderControlId == (int)TraktDashboardControls.TrendingMoviesFacade)
                    {
                        PlayMovie(true);
                    }
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
                        PlayShow();
                    }
                    if (activeWindow.GetFocusControlId() == (int)TraktDashboardControls.TrendingMoviesFacade)
                    {
                        PlayMovie(false);
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
            if (TraktSkinSettings.DashBoardActivityWindows.Count > 0)
            {
                ClearActivityProperties();
                ActivityTimer = new Timer(new TimerCallback((o) => { LoadActivity(); }), null, Timeout.Infinite, Timeout.Infinite);
            }

            if (TraktSkinSettings.DashBoardTrendingMoviesWindows.Count > 0)
            {
                TrendingMoviesTimer = new Timer(new TimerCallback((o) => { LoadTrendingMovies(); }), null, Timeout.Infinite, Timeout.Infinite);
            }

            if (TraktSkinSettings.DashBoardTrendingShowsWindows.Count > 0)
            {
                TrendingShowsTimer = new Timer(new TimerCallback((o) => { LoadTrendingShows(); }), null, Timeout.Infinite, Timeout.Infinite);
            }
        }

        public void StartTrendingMoviesPolling()
        {
            if (TrendingMoviesTimer != null)
            {
                TrendingMoviesTimer.Change(0, 300000);
            }
        }

        public void StartTrendingShowsPolling()
        {
            if (TrendingShowsTimer != null)
            {
                TrendingShowsTimer.Change(0, 300000);
            }
        }

        public void StartActivityPolling()
        {
            if (ActivityTimer != null)
            {
                ActivityTimer.Change(0, 15000);
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
