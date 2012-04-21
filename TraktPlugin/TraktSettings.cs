using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using MediaPortal.GUI.Library;
using MediaPortal.Profile;
using TraktPlugin.TraktAPI;
using TraktPlugin.TraktAPI.DataStructures;

namespace TraktPlugin
{
    public static class TraktSettings
    {
        private static Object lockObject = new object();

        #region Settings
        public static string Username { get; set; }
        public static string Password { get; set; }
        public static List<TraktAuthentication> UserLogins { get; set; }
        public static int MovingPictures { get; set; }
        public static int TVSeries { get; set; }
        public static int MyVideos { get; set; }
        public static int MyFilms { get; set; }
        public static int OnlineVideos { get; set; }
        public static int MyAnime { get; set; }
        public static int MyTVRecordings { get; set; }
        public static int MyTVLive { get; set; }
        public static int ForTheRecordRecordings { get; set; }
        public static int ForTheRecordTVLive { get; set; }
        public static bool KeepTraktLibraryClean { get; set; }
        public static List<String> BlockedFilenames { get; set; }
        public static List<String> BlockedFolders { get; set; }
        public static SyncMovieCheck SkippedMovies { get; set; }
        public static SyncMovieCheck AlreadyExistMovies { get; set; }
        public static int LogLevel { get; set; }
        public static int SyncTimerLength { get; set; }
        public static int SyncStartDelay { get; set; }
        public static int TrendingMoviesDefaultLayout { get; set; }
        public static int TrendingShowsDefaultLayout { get; set; }
        public static int RecommendedMoviesDefaultLayout { get; set; }
        public static int RecommendedShowsDefaultLayout { get; set; }
        public static int WatchListMoviesDefaultLayout { get; set; }
        public static int WatchListShowsDefaultLayout { get; set; }
        public static int WatchListEpisodesDefaultLayout { get; set; }
        public static int ListsDefaultLayout { get; set; }
        public static int ListItemsDefaultLayout { get; set; }
        public static int RelatedMoviesDefaultLayout { get; set; }
        public static int RelatedShowsDefaultLayout { get; set; }
        public static int DefaultCalendarView { get; set; }
        public static int DefaultCalendarStartDate { get; set; }
        public static bool DownloadFullSizeFanart { get; set; }
        public static bool DownloadFanart { get; set; }
        public static int WebRequestCacheMinutes { get; set; }
        public static bool GetFriendRequestsOnStartup { get; set; }
        public static int MovingPicturesCategoryId { get; set; }
        public static bool MovingPicturesCategories { get; set; }
        public static int MovingPicturesFiltersId { get; set; }
        public static bool MovingPicturesFilters { get; set; }
        public static bool CalendarHideTVShowsInWatchList { get; set; }
        public static bool HideWatchedRelatedMovies { get; set; }
        public static bool HideWatchedRelatedShows { get; set; }
        public static int WebRequestTimeout { get; set; }
        public static bool HideSpoilersOnShouts { get; set; }
        public static int LoveMinimumValue { get; set; }
        public static int HateMaximumValue { get; set; }
        #endregion

        #region Constants
        public const string cGuid = "a9c3845a-8718-4712-85cc-26f56520bb9a";

        private const string cTrakt = "Trakt";
        private const string cUsername = "Username";
        private const string cPassword = "Password";
        private const string cMovingPictures = "MovingPictures";
        private const string cTVSeries = "TVSeries";
        private const string cMyVideos = "MyVideos";
        private const string cMyFilms = "MyFilms";
        private const string cOnlineVideos = "OnlineVideos";
        private const string cMyAnime = "MyAnime";
        private const string cMyTVRecordings = "MyTVRecordings";
        private const string cMyTVLive = "MyTVLive";
        private const string cForTheRecordRecordings = "ForTheRecordRecordings";
        private const string cForTheRecordTVLive = "ForTheRecordTVLive";
        private const string cKeepTraktLibraryClean = "KeepLibraryClean";
        private const string cBlockedFilenames = "BlockedFilenames";
        private const string cBlockedFolders = "BlockedFolders";
        private const string cSkippedMovies = "SkippedMovies";
        private const string cAlreadyExistMovies = "AlreadyExistMovies";
        private const string cSyncTimerLength = "SyncTimerLength";
        private const string cSyncStartDelay = "SyncStartDelay";
        private const string cTrendingMoviesDefaultLayout = "TrendingMoviesDefaultLayout";
        private const string cTrendingShowsDefaultLayout = "TrendingShowsDefaultLayout";
        private const string cRecommendedMoviesDefaultLayout = "RecommendedMoviesDefaultLayout";
        private const string cRecommendedShowsDefaultLayout = "RecommendedShowsDefaultLayout";
        private const string cWatchListMoviesDefaultLayout = "WatchListMoviesDefaultLayout";
        private const string cWatchListShowsDefaultLayout = "WatchListShowsDefaultLayout";
        private const string cWatchListEpisodesDefaultLayout = "WatchListEpisodesDefaultLayout";
        private const string cListsDefaultLayout = "ListsDefaultLayout";
        private const string cListItemsDefaultLayout = "ListItemsDefaultLayout";
        private const string cRelatedMoviesDefaultLayout = "RelatedMoviesDefaultLayout";
        private const string cRelatedShowsDefaultLayout = "RelatedShowsDefaultLayout";
        private const string cDefaultCalendarView = "DefaultCalendarView";
        private const string cDefaultCalendarStartDate = "DefaultCalendarStartDate";
        private const string cDownloadFullSizeFanart = "DownloadFullSizeFanart";
        private const string cDownloadFanart = "DownloadFanart";
        private const string cWebRequestCacheMinutes = "WebRequestCacheMinutes";
        private const string cGetFriendRequestsOnStartup = "GetFriendRequestsOnStartup";
        private const string cMovingPicturesCategoryId = "MovingPicturesCategoryId";
        private const string cMovingPicturesCategories = "MovingPicturesCategories";
        private const string cMovingPicturesFilterId = "MovingPicturesFilterId";
        private const string cMovingPicturesFilters = "MovingPicturesFilters";
        private const string cCalendarHideTVShowsInWatchList = "CalendarHideTVShowsInWatchList";
        private const string cHideWatchedRelatedMovies = "HideWatchedRelatedMovies";
        private const string cHideWatchedRelatedShows = "HideWatchedRelatedShows";
        private const string cUserLogins = "UserLogins";
        private const string cWebRequestTimeout = "WebRequestTimeout";
        private const string cHideSpoilersOnShouts = "HideSpoilersOnShouts";
        private const string cLoveMinimumValue = "LoveMinimumValue";
        private const string cHateMaximumValue = "HateMaximumValue";
        private const string cShowAdvancedRatingsDialog = "ShowAdvancedRatingsDialog";
        #endregion

        #region Properties

        /// <summary>
        /// Show Advanced or Simple Ratings Dialog
        /// Settings is Synced from Server
        /// </summary>
        public static bool ShowAdvancedRatingsDialog
        {
            get
            {
                return _showAdvancedRatingsDialogs;
            }
            set
            {
                // allow last saved setting to be available immediately
                _showAdvancedRatingsDialogs = value;

                // sync setting - delay on startup
                Thread syncSetting = new Thread((o) =>
                {
                    if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
                        return;

                    TraktLogger.Info("Loading Online Settings");
                    Thread.Sleep(5000);

                    TraktAccountSettings settings = TraktAPI.TraktAPI.GetAccountSettings();
                    if (settings.Status == "success")
                    {
                        _showAdvancedRatingsDialogs = settings.ViewingSettings.RatingSettings.Mode == "advanced";
                    }
                })
                {
                    IsBackground = true,
                    Name = "Settings"
                };
                syncSetting.Start();
            }
        }
        static bool _showAdvancedRatingsDialogs;

        /// <summary>
        /// Get Movie Plugin Count
        /// </summary>
        public static int MoviePluginCount
        {
            get
            {
                int count = 0;
                if (MovingPictures >= 0) count++;
                if (MyVideos >= 0) count++;
                if (MyFilms >= 0) count++;
                return count;
            }
        }

        /// <summary>
        /// Get TV Show Plugin Count
        /// </summary>
        public static int TvShowPluginCount
        {
            get
            {
                int count = 0;
                if (TVSeries >= 0) count++;
                if (MyAnime >= 0) count++;
                return count;
            }
        }

        public static string Version
        {
            get
            {
                return Assembly.GetCallingAssembly().GetName().Version.ToString();
            }
        }

        public static string UserAgent
        {
            get
            {
                return string.Format("TraktForMediaPortal/{0}", Version);
            }
        }

        public static ConnectionState AccountStatus
        {
            get
            {
                lock (lockObject)
                {
                    if (_AccountStatus == ConnectionState.Pending)
                    {
                        // update state, to inform we are connecting now
                        _AccountStatus = ConnectionState.Connecting;

                        TraktLogger.Info("Signing into trakt.tv");

                        if (string.IsNullOrEmpty(TraktSettings.Username) || string.IsNullOrEmpty(TraktSettings.Password))
                        {
                            TraktLogger.Info("Username and/or Password is empty in settings!");
                            return ConnectionState.Disconnected;
                        }

                        // test connection
                        TraktAccount account = new TraktAccount
                        {
                            Username = TraktSettings.Username,
                            Password = TraktSettings.Password
                        };

                        TraktResponse response = TraktAPI.TraktAPI.TestAccount(account);
                        TraktLogger.Debug("Response: " + response.ToJSON());
                        if (response != null && response.Status == "success")
                        {
                            TraktLogger.Info("User {0} signed into trakt.", TraktSettings.Username);
                            _AccountStatus = ConnectionState.Connected;

                            if (!UserLogins.Exists(u => u.Username == Username))
                            {
                                UserLogins.Add(new TraktAuthentication { Username = Username, Password = Password });
                            }
                        }
                        else
                        {
                            TraktLogger.Info("Username and/or Password is Invalid!");
                            _AccountStatus = ConnectionState.Invalid;
                        }
                    }
                }
                return _AccountStatus;
            }
            set
            {
                lock (lockObject)
                {
                    _AccountStatus = value;
                }
            }
        }
        static ConnectionState _AccountStatus = ConnectionState.Pending;

        #endregion

        #region Methods
        /// <summary>
        /// Loads the Settings
        /// </summary>
        public static void loadSettings()
        {
            TraktLogger.Info("Loading Local Settings");
            using (Settings xmlreader = new MPSettings())
            {
                Username = xmlreader.GetValueAsString(cTrakt, cUsername, "");
                Password = xmlreader.GetValueAsString(cTrakt, cPassword, "");
                UserLogins = xmlreader.GetValueAsString(cTrakt, cUserLogins, "").FromJSONArray<TraktAuthentication>().ToList();
                MovingPictures = xmlreader.GetValueAsInt(cTrakt, cMovingPictures, -1);
                TVSeries = xmlreader.GetValueAsInt(cTrakt, cTVSeries, -1);
                MyVideos = xmlreader.GetValueAsInt(cTrakt, cMyVideos, -1);
                MyFilms = xmlreader.GetValueAsInt(cTrakt, cMyFilms, -1);
                OnlineVideos = xmlreader.GetValueAsInt(cTrakt, cOnlineVideos, -1);
                MyAnime = xmlreader.GetValueAsInt(cTrakt, cMyAnime, -1);
                MyTVRecordings = xmlreader.GetValueAsInt(cTrakt, cMyTVRecordings, -1);
                MyTVLive = xmlreader.GetValueAsInt(cTrakt, cMyTVLive, -1);
                ForTheRecordRecordings = xmlreader.GetValueAsInt(cTrakt, cForTheRecordRecordings, -1);
                ForTheRecordTVLive = xmlreader.GetValueAsInt(cTrakt, cForTheRecordTVLive, -1);
                KeepTraktLibraryClean = xmlreader.GetValueAsBool(cTrakt, cKeepTraktLibraryClean, false);
                BlockedFilenames = xmlreader.GetValueAsString(cTrakt, cBlockedFilenames, "").FromJSONArray<string>().ToList();
                BlockedFolders = xmlreader.GetValueAsString(cTrakt, cBlockedFolders, "").FromJSONArray<string>().ToList();
                SkippedMovies = xmlreader.GetValueAsString(cTrakt, cSkippedMovies, "{}").FromJSON<SyncMovieCheck>();
                AlreadyExistMovies = xmlreader.GetValueAsString(cTrakt, cAlreadyExistMovies, "{}").FromJSON<SyncMovieCheck>();
                LogLevel = xmlreader.GetValueAsInt("general", "loglevel", 1);
                SyncTimerLength = xmlreader.GetValueAsInt(cTrakt, cSyncTimerLength, 86400000);
                SyncStartDelay = xmlreader.GetValueAsInt(cTrakt, cSyncStartDelay, 0);
                TrendingMoviesDefaultLayout = xmlreader.GetValueAsInt(cTrakt, cTrendingMoviesDefaultLayout, 0);
                TrendingShowsDefaultLayout = xmlreader.GetValueAsInt(cTrakt, cTrendingShowsDefaultLayout, 0);
                RecommendedMoviesDefaultLayout = xmlreader.GetValueAsInt(cTrakt, cRecommendedMoviesDefaultLayout, 0);
                RecommendedShowsDefaultLayout = xmlreader.GetValueAsInt(cTrakt, cRecommendedShowsDefaultLayout, 0);
                WatchListMoviesDefaultLayout = xmlreader.GetValueAsInt(cTrakt, cWatchListMoviesDefaultLayout, 0);
                WatchListShowsDefaultLayout = xmlreader.GetValueAsInt(cTrakt, cWatchListShowsDefaultLayout, 0);
                WatchListEpisodesDefaultLayout = xmlreader.GetValueAsInt(cTrakt, cWatchListEpisodesDefaultLayout, 0);
                ListsDefaultLayout = xmlreader.GetValueAsInt(cTrakt, cListsDefaultLayout, 0);
                ListItemsDefaultLayout = xmlreader.GetValueAsInt(cTrakt, cListItemsDefaultLayout, 0);
                RelatedMoviesDefaultLayout = xmlreader.GetValueAsInt(cTrakt, cRelatedMoviesDefaultLayout, 0);
                RelatedShowsDefaultLayout = xmlreader.GetValueAsInt(cTrakt, cRelatedShowsDefaultLayout, 0);
                DefaultCalendarView = xmlreader.GetValueAsInt(cTrakt, cDefaultCalendarView, 0);
                DefaultCalendarStartDate = xmlreader.GetValueAsInt(cTrakt, cDefaultCalendarStartDate, 0);
                DownloadFullSizeFanart = xmlreader.GetValueAsBool(cTrakt, cDownloadFullSizeFanart, false);
                DownloadFanart = xmlreader.GetValueAsBool(cTrakt, cDownloadFanart, true);
                WebRequestCacheMinutes = xmlreader.GetValueAsInt(cTrakt, cWebRequestCacheMinutes, 15);
                WebRequestTimeout = xmlreader.GetValueAsInt(cTrakt, cWebRequestTimeout, 30000);
                GetFriendRequestsOnStartup = xmlreader.GetValueAsBool(cTrakt, cGetFriendRequestsOnStartup, true);
                MovingPicturesCategoryId = xmlreader.GetValueAsInt(cTrakt, cMovingPicturesCategoryId, -1);
                MovingPicturesCategories = xmlreader.GetValueAsBool(cTrakt, cMovingPicturesCategories, false);
                MovingPicturesFiltersId = xmlreader.GetValueAsInt(cTrakt, cMovingPicturesFilterId, -1);
                MovingPicturesFilters = xmlreader.GetValueAsBool(cTrakt, cMovingPicturesFilters, false);
                CalendarHideTVShowsInWatchList = xmlreader.GetValueAsBool(cTrakt, cCalendarHideTVShowsInWatchList, false);
                HideWatchedRelatedMovies = xmlreader.GetValueAsBool(cTrakt, cHideWatchedRelatedMovies, false);
                HideWatchedRelatedShows = xmlreader.GetValueAsBool(cTrakt, cHideWatchedRelatedShows, false);
                HideSpoilersOnShouts = xmlreader.GetValueAsBool(cTrakt, cHideSpoilersOnShouts, false);
                LoveMinimumValue = xmlreader.GetValueAsInt(cTrakt, cLoveMinimumValue, 7);
                HateMaximumValue = xmlreader.GetValueAsInt(cTrakt, cHateMaximumValue, 3);
                ShowAdvancedRatingsDialog = xmlreader.GetValueAsBool(cTrakt, cShowAdvancedRatingsDialog, false);
            }
        }

        /// <summary>
        /// Saves the Settings
        /// </summary>
        public static void saveSettings()
        {
            TraktLogger.Info("Saving Settings");
            using (Settings xmlwriter = new MPSettings())
            {
                xmlwriter.SetValue(cTrakt, cUsername, Username);
                xmlwriter.SetValue(cTrakt, cPassword, Password);
                xmlwriter.SetValue(cTrakt, cUserLogins, UserLogins.ToJSON());
                xmlwriter.SetValue(cTrakt, cMovingPictures, MovingPictures);
                xmlwriter.SetValue(cTrakt, cTVSeries, TVSeries);
                xmlwriter.SetValue(cTrakt, cMyVideos, MyVideos);
                xmlwriter.SetValue(cTrakt, cMyFilms, MyFilms);
                xmlwriter.SetValue(cTrakt, cOnlineVideos, OnlineVideos);
                xmlwriter.SetValue(cTrakt, cMyAnime, MyAnime);
                xmlwriter.SetValue(cTrakt, cMyTVRecordings, MyTVRecordings);
                xmlwriter.SetValue(cTrakt, cMyTVLive, MyTVLive);
                xmlwriter.SetValue(cTrakt, cForTheRecordRecordings, ForTheRecordRecordings);
                xmlwriter.SetValue(cTrakt, cForTheRecordTVLive, ForTheRecordTVLive);
                xmlwriter.SetValueAsBool(cTrakt, cKeepTraktLibraryClean, KeepTraktLibraryClean);
                xmlwriter.SetValue(cTrakt, cBlockedFilenames, BlockedFilenames.ToJSON());
                xmlwriter.SetValue(cTrakt, cBlockedFolders, BlockedFolders.ToJSON());
                xmlwriter.SetValue(cTrakt, cSkippedMovies, SkippedMovies.ToJSON());
                xmlwriter.SetValue(cTrakt, cAlreadyExistMovies, AlreadyExistMovies.ToJSON());
                xmlwriter.SetValue(cTrakt, cSyncTimerLength, SyncTimerLength);
                xmlwriter.SetValue(cTrakt, cSyncStartDelay, SyncStartDelay);
                xmlwriter.SetValue(cTrakt, cTrendingMoviesDefaultLayout, TrendingMoviesDefaultLayout);
                xmlwriter.SetValue(cTrakt, cTrendingShowsDefaultLayout, TrendingShowsDefaultLayout);
                xmlwriter.SetValue(cTrakt, cRecommendedMoviesDefaultLayout, RecommendedMoviesDefaultLayout);
                xmlwriter.SetValue(cTrakt, cRecommendedShowsDefaultLayout, RecommendedShowsDefaultLayout);
                xmlwriter.SetValue(cTrakt, cWatchListMoviesDefaultLayout, WatchListMoviesDefaultLayout);
                xmlwriter.SetValue(cTrakt, cWatchListShowsDefaultLayout, WatchListShowsDefaultLayout);
                xmlwriter.SetValue(cTrakt, cWatchListEpisodesDefaultLayout, WatchListEpisodesDefaultLayout);
                xmlwriter.SetValue(cTrakt, cRelatedMoviesDefaultLayout, RelatedMoviesDefaultLayout);
                xmlwriter.SetValue(cTrakt, cRelatedShowsDefaultLayout, RelatedShowsDefaultLayout);
                xmlwriter.SetValue(cTrakt, cListsDefaultLayout, ListsDefaultLayout);
                xmlwriter.SetValue(cTrakt, cListItemsDefaultLayout, ListItemsDefaultLayout);
                xmlwriter.SetValue(cTrakt, cDefaultCalendarView, DefaultCalendarView);
                xmlwriter.SetValue(cTrakt, cDefaultCalendarStartDate, DefaultCalendarStartDate);
                xmlwriter.SetValueAsBool(cTrakt, cDownloadFullSizeFanart, DownloadFullSizeFanart);
                xmlwriter.SetValueAsBool(cTrakt, cDownloadFanart, DownloadFanart);
                xmlwriter.SetValue(cTrakt, cWebRequestCacheMinutes, WebRequestCacheMinutes);
                xmlwriter.SetValue(cTrakt, cWebRequestTimeout, WebRequestTimeout);
                xmlwriter.SetValueAsBool(cTrakt, cGetFriendRequestsOnStartup, GetFriendRequestsOnStartup);
                xmlwriter.SetValue(cTrakt, cMovingPicturesCategoryId, MovingPicturesCategoryId);
                xmlwriter.SetValueAsBool(cTrakt, cMovingPicturesCategories, MovingPicturesCategories);
                xmlwriter.SetValue(cTrakt, cMovingPicturesFilterId, MovingPicturesFiltersId);
                xmlwriter.SetValueAsBool(cTrakt, cMovingPicturesFilters, MovingPicturesFilters);
                xmlwriter.SetValueAsBool(cTrakt, cCalendarHideTVShowsInWatchList, CalendarHideTVShowsInWatchList);
                xmlwriter.SetValueAsBool(cTrakt, cHideWatchedRelatedMovies, HideWatchedRelatedMovies);
                xmlwriter.SetValueAsBool(cTrakt, cHideWatchedRelatedShows, HideWatchedRelatedShows);
                xmlwriter.SetValueAsBool(cTrakt, cHideSpoilersOnShouts, HideSpoilersOnShouts);
                xmlwriter.SetValue(cTrakt, cLoveMinimumValue, LoveMinimumValue);
                xmlwriter.SetValue(cTrakt, cHateMaximumValue, HateMaximumValue);
                xmlwriter.SetValueAsBool(cTrakt, cShowAdvancedRatingsDialog, ShowAdvancedRatingsDialog);
            }

            Settings.SaveCache();
        }

        #endregion
    }

    public class ExtensionSettings
    {
        #region Init
        public void Init()
        {
            Thread hookThread = new Thread(delegate()
            {
                try
                {
                    TraktLogger.Info("Adding hooks to MPEI Settings");
                    AddHooksIntoMPEISettings();
                }
                catch
                {
                    TraktLogger.Warning("Unable to add hooks into MPEI Settings, Extensions plugin not installed or out of date!");
                }
            })
            {
                Name = "Extension Settings",
                IsBackground = true
            };

            hookThread.Start();
        }
        #endregion

        #region Hooks
        private void AddHooksIntoMPEISettings()
        {
            // sleep until we know that there has been enough time
            // for window manager to have loaded extension settings window
            // todo: find a better way...
            Thread.Sleep(10000);

            // get a reference to the extension settings window
            MPEIPlugin.GUISettings extensionSettings = (MPEIPlugin.GUISettings)GUIWindowManager.GetWindow((int)GUI.ExternalPluginWindows.MPEISettings);
            extensionSettings.OnSettingsChanged += new MPEIPlugin.GUISettings.SettingsChangedHandler(Extensions_OnSettingsChanged);
        }

        private void Extensions_OnSettingsChanged(string guid)
        {
            // settings change occured
            if (guid == TraktSettings.cGuid)
            {
                TraktLogger.Info("Settings updated externally");

                // re-load settings
                TraktSettings.loadSettings();

                // re-initialize sync Interval
                TraktPlugin.ChangeSyncTimer(TraktSettings.SyncTimerLength, TraktSettings.SyncTimerLength);
            }
        }
        #endregion
    }
}
