using MediaPortal.Configuration;
using MediaPortal.GUI.Library;
using MediaPortal.Profile;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using TraktAPI.DataStructures;
using TraktAPI.Enums;
using TraktAPI.Extensions;
using TraktPlugin.GUI;
using TraktPlugin.TmdbAPI.DataStructures;

namespace TraktPlugin
{
    public class TraktSettings
    {
        private static Object lockObject = new object();

        #region Settings
        static int SettingsVersion = 11;
        
        public static int MovingPictures { get; set; }
        public static int TVSeries { get; set; }
        public static int MyVideos { get; set; }
        public static int MyFilms { get; set; }
        public static int OnlineVideos { get; set; }
        public static int MyTVRecordings { get; set; }
        public static int MyTVLive { get; set; }
        public static int ArgusRecordings { get; set; }
        public static int ArgusTVLive { get; set; }
        public static bool KeepTraktLibraryClean { get; set; }
        public static List<String> BlockedFilenames { get; set; }
        public static List<String> BlockedFolders { get; set; }
        //TODOpublic static SyncMovieCheck SkippedMovies { get; set; }
        //TODOpublic static SyncMovieCheck AlreadyExistMovies { get; set; }
        public static int LogLevel { get; set; }
        public static int SyncTimerLength { get; set; }
        public static int SyncStartDelay { get; set; }
        public static int TrendingMoviesDefaultLayout { get; set; }
        public static int TrendingShowsDefaultLayout { get; set; }
        public static int PopularMoviesDefaultLayout { get; set; }
        public static int PopularShowsDefaultLayout { get; set; }
        public static int ShowSeasonsDefaultLayout { get; set; }
        public static int SeasonEpisodesDefaultLayout { get; set; }
        public static int RecommendedMoviesDefaultLayout { get; set; }
        public static int RecommendedShowsDefaultLayout { get; set; }
        public static int WatchListMoviesDefaultLayout { get; set; }
        public static int WatchListShowsDefaultLayout { get; set; }
        public static int WatchListEpisodesDefaultLayout { get; set; }
        public static int ListsDefaultLayout { get; set; }
        public static int ListItemsDefaultLayout { get; set; }
        public static int RelatedMoviesDefaultLayout { get; set; }
        public static int RelatedShowsDefaultLayout { get; set; }
        public static int SearchMoviesDefaultLayout { get; set; }
        public static int SearchShowsDefaultLayout { get; set; }
        public static int SearchEpisodesDefaultLayout { get; set; }
        public static int SearchPeopleDefaultLayout { get; set; }
        public static int SearchUsersDefaultLayout { get; set; }
        public static int DefaultCalendarView { get; set; }
        public static int DefaultCalendarStartDate { get; set; }
        public static bool DownloadFullSizeFanart { get; set; }
        public static bool DownloadFanart { get; set; }
        public static int WebRequestCacheMinutes { get; set; }
        public static bool GetFollowerRequestsOnStartup { get; set; }
        public static bool MovingPicturesCategories { get; set; }
        public static bool MovingPicturesFilters { get; set; }
        public static bool CalendarHideTVShowsInWatchList { get; set; }
        public static bool HideWatchedRelatedMovies { get; set; }
        public static bool HideWatchedRelatedShows { get; set; }
        public static int WebRequestTimeout { get; set; }
        public static bool HideSpoilersOnShouts { get; set; }
        public static bool SyncRatings { get; set; }
        public static bool ShowRateDialogOnWatched { get; set; }
        public static TraktActivity LastActivityLoad { get; set; }
        public static IEnumerable<TraktMovieTrending> LastTrendingMovies { get; set; }
        public static IEnumerable<TraktShowTrending> LastTrendingShows { get; set; }
        public static int DashboardActivityPollInterval { get; set; }
        public static int DashboardTrendingPollInterval { get; set; }
        public static int DashboardLoadDelay { get; set; }
        public static TraktUserStatistics LastStatistics { get; set; }
        public static TraktUserSummary LastUserProfile { get; set; }
        public static bool DashboardMovieTrendingActive { get; set; }
        public static string MovieRecommendationGenre { get; set; }
        public static bool MovieRecommendationHideCollected { get; set; }
        public static bool MovieRecommendationHideWatchlisted { get; set; }
        public static int MovieRecommendationStartYear { get; set; }
        public static int MovieRecommendationEndYear { get; set; }
        public static string ShowRecommendationGenre { get; set; }
        public static bool ShowRecommendationHideCollected { get; set; }
        public static bool ShowRecommendationHideWatchlisted { get; set; }
        public static int ShowRecommendationStartYear { get; set; }
        public static int ShowRecommendationEndYear { get; set; }
        public static SortBy SortByTrendingMovies { get; set; }
        public static SortBy SortByPopularMovies { get; set; }
        public static SortBy SortByRecommendedMovies { get; set; }
        public static SortBy SortByWatchListMovies { get; set; }
        public static SortBy SortByCreditMovies { get; set; }
        public static SortBy SortByAnticipatedMovies { get; set; }
        public static SortBy SortByTrendingShows { get; set; }
        public static SortBy SortByPopularShows { get; set; }
        public static SortBy SortByRecommendedShows { get; set; }
        public static SortBy SortByWatchListShows { get; set; }
        public static SortBy SortByCreditShows { get; set; }
        public static SortBy SortByAnticipatedShows { get; set; }
        public static bool EnableJumpToForTVShows { get; set; }
        public static bool MyFilmsCategories { get; set; }
        public static bool SortSeasonsAscending { get; set; }
        public static bool RememberLastSelectedActivity { get; set; }
        public static int MovPicsRatingDlgDelay { get; set; }
        public static bool ShowRateDlgForPlaylists { get; set; }
        public static bool TrendingMoviesHideWatched { get; set; }
        public static bool TrendingMoviesHideWatchlisted { get; set; }
        public static bool TrendingMoviesHideCollected { get; set; }
        public static bool TrendingMoviesHideRated { get; set; }
        public static bool TrendingShowsHideWatched { get; set; }
        public static bool TrendingShowsHideWatchlisted { get; set; }
        public static bool TrendingShowsHideCollected { get; set; }
        public static bool TrendingShowsHideRated { get; set; }
        public static bool PopularMoviesHideWatched { get; set; }
        public static bool PopularMoviesHideWatchlisted { get; set; }
        public static bool PopularMoviesHideCollected { get; set; }
        public static bool PopularMoviesHideRated { get; set; }
        public static bool PopularShowsHideWatched { get; set; }
        public static bool PopularShowsHideWatchlisted { get; set; }
        public static bool PopularShowsHideCollected { get; set; }
        public static bool PopularShowsHideRated { get; set; }
        public static bool CreditMoviesHideWatched { get; set; }
        public static bool CreditMoviesHideWatchlisted { get; set; }
        public static bool CreditMoviesHideCollected { get; set; }
        public static bool CreditMoviesHideRated { get; set; }
        public static bool CreditShowsHideWatched { get; set; }
        public static bool CreditShowsHideWatchlisted { get; set; }
        public static bool CreditShowsHideCollected { get; set; }
        public static bool CreditShowsHideRated { get; set; }
        public static bool AnticipatedMoviesHideWatchlisted { get; set; }
        public static bool AnticipatedShowsHideWatchlisted { get; set; }
        public static int DefaultNetworkView { get; set; }
        public static int RecentWatchedMoviesDefaultLayout { get; set; }
        public static int RecentWatchedEpisodesDefaultLayout { get; set; }
        public static int RecentAddedMoviesDefaultLayout { get; set; }
        public static int RecentAddedEpisodesDefaultLayout { get; set; }
        public static bool SyncLibrary { get; set; }
        public static int SearchTypes { get; set; }
        public static bool ShowSearchResultsBreakdown { get; set; }
        public static int MaxSearchResults { get; set; }
        public static bool FilterTrendingOnDashboard { get; set; }
        public static bool IgnoreWatchedPercentOnDVD { get; set; }
        public static int ActivityStreamView { get; set; }
        public static TraktLastSyncActivities LastSyncActivities { get; set; }
        public static int SyncBatchSize { get; set; }
        public static bool UseCompNameOnPassKey { get; set; }
        public static bool SyncPlayback { get; set; }
        public static int SyncResumeDelta { get; set; }
        public static bool SyncPlaybackOnEnterPlugin { get; set; }
        public static int SyncPlaybackCacheExpiry { get; set; }
        public static int MaxTrendingMoviesRequest { get; set; }
        public static int MaxTrendingShowsRequest { get; set; }
        public static int MaxPopularMoviesRequest { get; set; }
        public static int MaxPopularShowsRequest { get; set; }
        public static int MaxAnticipatedMoviesRequest { get; set; }
        public static int MaxAnticipatedShowsRequest { get; set; }
        public static bool UseSSL { get; set; }
        public static IEnumerable<TraktCache.ListActivity> LastListActivities { get; set; }
        public static int MaxRelatedMoviesRequest { get; set; }
        public static int MaxRelatedMoviesUnWatchedRequest { get; set; }
        public static int MaxRelatedShowsRequest { get; set; }
        public static int MaxRelatedShowsUnWatchedRequest { get; set; }
        public static int MaxUserWatchedMoviesRequest { get; set; }
        public static int MaxUserWatchedEpisodesRequest { get; set; }
        public static int MaxUserCommentsRequest { get; set; }
        public static ActivityFilter DashboardActivityFilter { get; set; }
        public static bool SkipMoviesWithNoIdsOnSync { get; set; }
        public static int PersonMovieCreditsDefaultLayout { get; set; }
        public static int PersonShowCreditsDefaultLayout { get; set; }
        public static int CreditsMovieDefaultLayout { get; set; }
        public static int CreditsShowDefaultLayout { get; set; }
        public static int AnticipatedShowsDefaultLayout { get; set; }
        public static int AnticipatedMoviesDefaultLayout { get; set; }
        public static int BoxOfficeDefaultLayout { get; set; }
        public static bool ParentalControlsEnabled { get; set; }
        public static bool ParentalIgnoreAfterEnabled { get; set; }
        public static bool ParentalIgnoreShowRatingEnabled { get; set; }
        public static bool ParentalIgnoreMovieRatingEnabled { get; set; }
        public static string ParentalControlsPinCode { get; set; }
        public static string ParentalIgnoreAfterTime { get; set; }
        public static string ParentalIgnoreShowRating { get; set; }
        public static string ParentalIgnoreMovieRating { get; set; }
        public static TmdbConfiguration TmdbConfiguration { get; set; }
        public static string TmdbConfigurationAge { get; set; }
        public static string TmdbPreferredBackdropSize { get; set; }
        public static string TmdbPreferredPosterSize { get; set; }
        public static string TmdbPreferredEpisodeThumbSize { get; set; }
        public static string TmdbPreferredHeadshotSize { get; set; }
        public static int TmdbShowImageMaxCacheAge { get; set; }
        public static int TmdbMovieImageMaxCacheAge { get; set; }
        public static int TmdbSeasonImageMaxCacheAge { get; set; }
        public static int TmdbEpisodeImageMaxCacheAge { get; set; }
        public static int TmdbPersonImageMaxCacheAge { get; set; }
        public static string TmdbPreferredImageLanguage { get; set; }
        public static TraktAPI.DataStructures.TraktSettings OnlineSettings { get; set; }
        public static string Username { get; set; }
        public static string UserAccessTokenExpiry { get; set; }
        public static string UserRefreshToken { get; set; }
        #endregion

        #region Constants
        // trakt has 2 servers, live and staging
        private const string ClientId = "49e6907e6221d3c7e866f9d4d890c6755590cf4aa92163e8490a17753b905e57";
        private const string ClientSecret = "0547cb211deb6615e14e47901abf62c15002494ae691eeb1576dec2c2e629e10";
        private const string RedirectUri = "urn:ietf:wg:oauth:2.0:oob";

        public const string cGuid = "a9c3845a-8718-4712-85cc-26f56520bb9a";
        
        private static string cLastActivityFileCache = Path.Combine(Config.GetFolder(Config.Dir.Config), @"Trakt\{username}\Dashboard\NetworkActivity.json");
        private static string cLastStatisticsFileCache = Path.Combine(Config.GetFolder(Config.Dir.Config), @"Trakt\{username}\Dashboard\UserStatistics.json");
        private static string cLastUserProfileFileCache = Path.Combine(Config.GetFolder(Config.Dir.Config), @"Trakt\{username}\Dashboard\UserProfile.json");
        private static string cLastTrendingMovieFileCache = Path.Combine(Config.GetFolder(Config.Dir.Config), @"Trakt\Dashboard\TrendingMovies.json");
        private static string cLastTrendingShowFileCache = Path.Combine(Config.GetFolder(Config.Dir.Config), @"Trakt\Dashboard\TrendingShows.json");
        
        private const string cTrakt = "Trakt";
        private const string cSettingsVersion = "SettingsVersion";
        private const string cUsername = "Username";
        private const string cUserAccessToken = "UserAccessToken";
        private const string cUserRefreshToken = "UserRefreshToken";
        private const string cUserAccessTokenExpiry = "UserAccessTokenExpiry";
        private const string cMovingPictures = "MovingPictures";
        private const string cTVSeries = "TVSeries";
        private const string cMyVideos = "MyVideos";
        private const string cMyFilms = "MyFilms";
        private const string cOnlineVideos = "OnlineVideos";
        private const string cMyTVRecordings = "MyTVRecordings";
        private const string cMyTVLive = "MyTVLive";
        private const string cArgusRecordings = "ArgusRecordings";
        private const string cArgusTVLive = "ArgusTVLive";
        private const string cKeepTraktLibraryClean = "KeepLibraryClean";
        private const string cBlockedFilenames = "BlockedFilenames";
        private const string cBlockedFolders = "BlockedFolders";
        private const string cSkippedMovies = "SkippedMovies";
        private const string cAlreadyExistMovies = "AlreadyExistMovies";
        private const string cSyncTimerLength = "SyncTimerLength";
        private const string cSyncStartDelay = "SyncStartDelay";
        private const string cTrendingMoviesDefaultLayout = "TrendingMoviesDefaultLayout";
        private const string cTrendingShowsDefaultLayout = "TrendingShowsDefaultLayout";
        private const string cPopularMoviesDefaultLayout = "PopularMoviesDefaultLayout";
        private const string cPopularShowsDefaultLayout = "PopularShowsDefaultLayout";
        private const string cRecommendedMoviesDefaultLayout = "RecommendedMoviesDefaultLayout";
        private const string cRecommendedShowsDefaultLayout = "RecommendedShowsDefaultLayout";
        private const string cWatchListMoviesDefaultLayout = "WatchListMoviesDefaultLayout";
        private const string cWatchListShowsDefaultLayout = "WatchListShowsDefaultLayout";
        private const string cWatchListEpisodesDefaultLayout = "WatchListEpisodesDefaultLayout";
        private const string cListsDefaultLayout = "ListsDefaultLayout";
        private const string cListItemsDefaultLayout = "ListItemsDefaultLayout";
        private const string cRelatedMoviesDefaultLayout = "RelatedMoviesDefaultLayout";
        private const string cRelatedShowsDefaultLayout = "RelatedShowsDefaultLayout";
        private const string cShowSeasonsDefaultLayout = "ShowSeasonsLayout";
        private const string cSeasonEpisodesDefaultLayout = "SeasonEpisodesDefaultLayout";
        private const string cSearchMoviesDefaultLayout = "SearchMoviesDefaultLayout";
        private const string cSearchShowsDefaultLayout = "SearchShowsDefaultLayout";
        private const string cSearchEpisodesDefaultLayout = "SearchEpisodesDefaultLayout";
        private const string cSearchPeopleDefaultLayout = "SearchPeopleDefaultLayout";
        private const string cSearchUsersDefaultLayout = "SearchUsersDefaultLayout";
        private const string cDefaultCalendarView = "DefaultCalendarView";
        private const string cDefaultCalendarStartDate = "DefaultCalendarStartDate";
        private const string cDownloadFullSizeFanart = "DownloadFullSizeFanart";
        private const string cDownloadFanart = "DownloadFanart";
        private const string cWebRequestCacheMinutes = "WebRequestCacheMinutes";
        private const string cGetFollowerRequestsOnStartup = "GetFriendRequestsOnStartup";
        private const string cMovingPicturesCategories = "MovingPicturesCategories";
        private const string cMovingPicturesFilters = "MovingPicturesFilters";
        private const string cCalendarHideTVShowsInWatchList = "CalendarHideTVShowsInWatchList";
        private const string cHideWatchedRelatedMovies = "HideWatchedRelatedMovies";
        private const string cHideWatchedRelatedShows = "HideWatchedRelatedShows";
        private const string cUserLogins = "UserLogins";
        private const string cWebRequestTimeout = "WebRequestTimeout";
        private const string cHideSpoilersOnShouts = "HideSpoilersOnShouts";        
        private const string cSyncRatings = "SyncRatings";
        private const string cShowRateDialogOnWatched = "ShowRateDialogOnWatched";
        private const string cLastActivityLoad = "LastActivityLoad";
        private const string cLastTrendingMovies = "LastTrendingMovies";
        private const string cLastTrendingShows = "LastTrendingShows";
        private const string cLastStatistics = "LastStatistics";
        private const string cDashboardActivityPollInterval = "DashboardActivityPollInterval";
        private const string cDashboardTrendingPollInterval = "DashboardTrendingPollInterval";
        private const string cDashboardLoadDelay = "DashboardLoadDelay";
        private const string cDashboardMovieTrendingActive = "DashboardMovieTrendingActive";
        private const string cMovieRecommendationGenre = "MovieRecommendationGenre";
        private const string cMovieRecommendationHideCollected = "MovieRecommendationHideCollected";
        private const string cMovieRecommendationHideWatchlisted = "MovieRecommendationHideWatchlisted";
        private const string cMovieRecommendationStartYear = "MovieRecommendationStartYear";
        private const string cMovieRecommendationEndYear = "MovieRecommendationEndYear";
        private const string cShowRecommendationGenre = "ShowRecommendationGenre";
        private const string cShowRecommendationHideCollected = "ShowRecommendationHideCollected";
        private const string cShowRecommendationHideWatchlisted = "ShowRecommendationHideWatchlisted";
        private const string cShowRecommendationStartYear = "ShowRecommendationStartYear";
        private const string cShowRecommendationEndYear = "ShowRecommendationEndYear";
        private const string cSortByTrendingMovies = "SortByTrendingMovies";
        private const string cSortByPopularMovies = "SortByPopularMovies";
        private const string cSortByRecommendedMovies = "SortByRecommendedMovies";
        private const string cSortByWatchListMovies = "SortByWatchListMovies";
        private const string cSortByCreditMovies = "SortByCreditMovies";
        private const string cSortByAnticipatedMovies = "SortByAnticipatedMovies";
        private const string cSortByTrendingShows = "SortByTrendingShows";
        private const string cSortByPopularShows = "SortByPopularShows";
        private const string cSortByRecommendedShows = "SortByRecommendedShows";
        private const string cSortByWatchListShows = "SortByWatchListShows";
        private const string cSortByCreditShows = "SortByCreditShows";
        private const string cSortByAnticipatedShows = "SortByAnticipatedShows";
        private const string cEnableJumpToForTVShows = "EnableJumpToForTVShows";
        private const string cMyFilmsCategories = "MyFilmsCategories";
        private const string cSortSeasonsAscending = "SortSeasonsAscending";
        private const string cRememberLastSelectedActivity = "RememberLastSelectedActivity";
        private const string cMovPicsRatingDlgDelay = "MovPicsRatingDlgDelay";
        private const string cShowRateDlgForPlaylists = "ShowRateDlgForPlaylists";
        private const string cTrendingMoviesHideWatched = "TrendingMoviesHideWatched";
        private const string cTrendingMoviesHideWatchlisted = "TrendingMoviesHideWatchlisted";
        private const string cTrendingMoviesHideCollected = "TrendingMoviesHideCollected";
        private const string cTrendingMoviesHideRated = "TrendingMoviesHideRated";
        private const string cTrendingShowsHideWatched = "TrendingShowsHideWatched";
        private const string cTrendingShowsHideWatchlisted = "TrendingShowsHideWatchlisted";
        private const string cTrendingShowsHideCollected = "TrendingShowsHideCollected";
        private const string cTrendingShowsHideRated = "TrendingShowsHideRated";
        private const string cPopularMoviesHideWatched = "PopularMoviesHideWatched";
        private const string cPopularMoviesHideWatchlisted = "PopularMoviesHideWatchlisted";
        private const string cPopularMoviesHideCollected = "PopularMoviesHideCollected";
        private const string cPopularMoviesHideRated = "PopularMoviesHideRated";
        private const string cPopularShowsHideWatched = "PopularShowsHideWatched";
        private const string cPopularShowsHideWatchlisted = "PopularShowsHideWatchlisted";
        private const string cPopularShowsHideCollected = "PopularShowsHideCollected";
        private const string cPopularShowsHideRated = "PopularShowsHideRated";
        private const string cCreditMoviesHideWatched = "CreditMoviesHideWatched";
        private const string cCreditMoviesHideWatchlisted = "CreditMoviesHideWatchlisted";
        private const string cCreditMoviesHideCollected = "CreditMoviesHideCollected";
        private const string cCreditMoviesHideRated = "CreditMoviesHideRated";
        private const string cCreditShowsHideWatched = "CreditShowsHideWatched";
        private const string cCreditShowsHideWatchlisted = "CreditShowsHideWatchlisted";
        private const string cCreditShowsHideCollected = "CreditShowsHideCollected";
        private const string cCreditShowsHideRated = "CreditShowsHideRated";
        private const string cAnticipatedShowsHideWatchlisted = "AnticipatedShowsHideWatchlisted";
        private const string cAnticipatedMoviesHideWatchlisted = "AnticipatedMoviesHideWatchlisted";
        private const string cDefaultNetworkView = "DefaultNetworkView";
        private const string cRecentWatchedMoviesDefaultLayout = "RecentWatchedMoviesDefaultLayout";
        private const string cRecentWatchedEpisodesDefaultLayout = "RecentWatchedEpisodesDefaultLayout";
        private const string cRecentAddedMoviesDefaultLayout = "RecentAddedMoviesDefaultLayout";
        private const string cRecentAddedEpisodesDefaultLayout = "RecentAddedEpisodesDefaultLayout";
        private const string cSyncLibrary = "SyncLibrary";
        private const string cSearchTypes = "SearchTypes";
        private const string cShowSearchResultsBreakdown = "ShowSearchResultsBreakdown";
        private const string cMaxSearchResults = "MaxSearchResults";
        private const string cFilterTrendingOnDashboard = "FilterTrendingOnDashboard";
        private const string cIgnoreWatchedPercentOnDVD = "IgnoreWatchedPercentOnDVD";
        private const string cActivityStreamView = "ActivityStreamView";
        private const string cLastSyncActivities = "LastSyncActivities";
        private const string cSyncBatchSize = "SyncBatchSize";
        private const string cUseCompNameOnPassKey = "UseCompNameOnPassKey";
        private const string cSyncPlayback = "SyncPlayback";
        private const string cSyncResumeDelta = "SyncResumeDelta";
        private const string cSyncPlaybackOnEnterPlugin = "SyncPlaybackOnEnterPlugin";
        private const string cSyncPlaybackCacheExpiry = "SyncPlaybackCacheExpiry";
        private const string cMaxTrendingMoviesRequest = "MaxTrendingMoviesRequest";
        private const string cMaxTrendingShowsRequest = "MaxTrendingShowsRequest";
        private const string cMaxPopularMoviesRequest = "MaxPopularMoviesRequest";
        private const string cMaxPopularShowsRequest = "MaxPopularShowsRequest";
        private const string cMaxAnticipatedMoviesRequest = "MaxAnticipatedMoviesRequest";
        private const string cMaxAnticipatedShowsRequest = "MaxAnticipatedShowsRequest";
        private const string cUseSSL = "UseSSL";
        private const string cLastListActivities = "LastListActivities";
        private const string cMaxRelatedMoviesRequest = "MaxRelatedMoviesRequest";
        private const string cMaxRelatedMoviesUnWatchedRequest = "MaxRelatedMoviesUnWatchedRequest";
        private const string cMaxRelatedShowsRequest = "MaxRelatedShowsRequest";
        private const string cMaxRelatedShowsUnWatchedRequest = "MaxRelatedShowsUnWatchedRequest";
        private const string cMaxUserWatchedMoviesRequest = "MaxUserWatchedMoviesRequest";
        private const string cMaxUserWatchedEpisodesRequest = "MaxUserWatchedEpisodesRequest";
        private const string cMaxUserCommentsRequest = "MaxUserCommentsRequest";
        private const string cDashboardActivityFilter = "DashboardActivityFilter";
        private const string cSkipMoviesWithNoIdsOnSync = "SkipMoviesWithNoIdsOnSync";
        private const string cPersonMovieCreditsDefaultLayout = "PersonMovieCreditsDefaultLayout";
        private const string cPersonShowCreditsDefaultLayout = "PersonShowCreditsDefaultLayout";
        private const string cCreditsMovieDefaultLayout = "CreditsMovieDefaultLayout";
        private const string cCreditsShowDefaultLayout = "CreditsShowDefaultLayout";
        private const string cAnticipatedMoviesDefaultLayout = "AnticipatedMoviesDefaultLayout";
        private const string cAnticipatedShowsDefaultLayout = "AnticipatedShowsDefaultLayout";
        private const string cBoxOfficeDefaultLayout = "BoxOfficeDefaultLayout";
        private const string cParentalControlsEnabled = "ParentalControlsEnabled";
        private const string cParentalIgnoreAfterEnabled = "ParentalIgnoreAfterEnabled";
        private const string cParentalIgnoreShowRatingEnabled = "ParentalIgnoreShowRatingEnabled";
        private const string cParentalIgnoreMovieRatingEnabled = "ParentalIgnoreMovieRatingEnabled";
        private const string cParentalControlsPinCode = "ParentalControlsPinCode";
        private const string cParentalIgnoreAfterTime = "ParentalIgnoreAfterTime";
        private const string cParentalIgnoreShowRating = "ParentalIgnoreShowRating";
        private const string cParentalIgnoreMovieRating = "ParentalIgnoreMovieRating";
        private const string cTmdbConfiguration = "TmdbConfiguration";
        private const string cTmdbConfigurationAge = "TmdbConfigurationAge";
        private const string cTmdbPreferredBackdropSize = "PreferredBackdropSize";
        private const string cTmdbPreferredPosterSize = "PreferredPosterSize";
        private const string cTmdbPreferredEpisodeThumbSize = "PreferredEpisodeThumbSize";
        private const string cTmdbPreferredHeadshotSize = "PreferredHeadshotSize";
        private const string cTmdbShowImageMaxCacheAge = "TmdbShowImageMaxCacheAge";
        private const string cTmdbMovieImageMaxCacheAge = "TmdbMovieImageMaxCacheAge";
        private const string cTmdbSeasonImageMaxCacheAge = "TmdbSeasonImageMaxCacheAge";
        private const string cTmdbEpisodeImageMaxCacheAge = "TmdbEpisodeImageMaxCacheAge";
        private const string cTmdbPersonImageMaxCacheAge = "TmdbPersonImageMaxCacheAge";
        private const string cTmdbPreferredImageLanguage = "TmdbPreferredImageLanguage";
        private const string cTraktOnlineSettings = "TraktOnlineSettings";
        #endregion
        
        #region Properties
        
        public static string UserAccessToken
        {
            get
            {
                return _userAccessToken;
            }
            set
            {
                _userAccessToken = value;
                TraktAPI.TraktAPI.UserAccessToken = _userAccessToken;
            }
        }
        static string _userAccessToken = null;
        
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
                return count;
            }
        }

        /// <summary>
        /// Version of Plugin
        /// </summary>
        public static string Version
        {
            get
            {
                return Assembly.GetCallingAssembly().GetName().Version.ToString();
            }
        }

        /// <summary>
        /// Build Date of Plugin
        /// </summary>
        public static string BuildDate
        {
            get
            {
                if (_BuildDate == null)
                {
                    const int PeHeaderOffset = 60;
                    const int LinkerTimestampOffset = 8;

                    byte[] buffer = new byte[2047];
                    using (Stream stream = new FileStream(Assembly.GetAssembly(typeof(TraktSettings)).Location, FileMode.Open, FileAccess.Read))
                    {
                        stream.Read(buffer, 0, 2047);
                    }

                    int secondsSince1970 = BitConverter.ToInt32(buffer, BitConverter.ToInt32(buffer, PeHeaderOffset) + LinkerTimestampOffset);

                    _BuildDate = new DateTime(1970, 1, 1, 0, 0, 0).AddSeconds(secondsSince1970).ToString("yyyy-MM-dd");
                }
                return _BuildDate;
            }
        }
        private static string _BuildDate;

        /// <summary>
        /// MediaPortal Version
        /// </summary>
        public static Version MPVersion
        { 
            get
            {
                return Assembly.GetEntryAssembly().GetName().Version;
            }
        }

        /// <summary>
        /// UserAgent used for Web Requests
        /// </summary>
        public static string UserAgent
        {
            get
            {
                return string.Format("TraktForMediaPortal/{0}", Version);
            }
        }

        public static bool? IsConfiguration
        {
            get
            {
                if (_isConfiguration == null)
                {
                    try
                    {
                        var entryAssembly = Assembly.GetEntryAssembly();

                        _isConfiguration = !Path.GetFileNameWithoutExtension(entryAssembly.Location).Equals("mediaportal", StringComparison.InvariantCultureIgnoreCase);
                    }
                    catch
                    {
                        _isConfiguration = false;
                    }
                }
                return _isConfiguration;
            }
        }
        static bool? _isConfiguration;

        /// <summary>
        /// The current connection status to trakt.tv
        /// </summary>
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

                        TraktLogger.Info("Logging into trakt.tv");

                        if (string.IsNullOrEmpty(TraktSettings.UserAccessToken))
                        {
                            TraktLogger.Info("Unable to login to trakt.tv, access token is not set. Go to settings and authorise application to use your trakt.tv account");
                            return ConnectionState.Disconnected;
                        }

                        #region check if our access token has expired 
                        DateTime expiryDate = new DateTime();
                        if (DateTime.TryParse(UserAccessTokenExpiry, out expiryDate))
                        {
                            if (expiryDate < DateTime.UtcNow)
                            {
                                TraktLogger.Info("The trakt access token has now expired as of {0}, requesting refresh token", UserAccessTokenExpiry);
                                var refreshResponse = TraktAPI.TraktAPI.RefreshAccessToken(TraktSettings.UserRefreshToken);
                                if (refreshResponse != null && !string.IsNullOrEmpty(refreshResponse.AccessToken))
                                {
                                    TraktSettings.UserAccessToken = refreshResponse.AccessToken;
                                    TraktSettings.UserRefreshToken = refreshResponse.RefreshToken;

                                    // new access token expires in 90 days
                                    TraktSettings.UserAccessTokenExpiry = DateTime.UtcNow.AddSeconds(refreshResponse.ExpiresIn).ToString();
                                }
                                else
                                {
                                    _AccountStatus = ConnectionState.UnAuthorised;

                                    // force user to manually authorise again - this will only occur every 90 days as a worse case scenario
                                    TraktSettings.UserAccessToken = string.Empty;
                                    TraktSettings.UserRefreshToken = string.Empty;
                                    TraktSettings.UserAccessTokenExpiry = string.Empty;

                                    if (refreshResponse != null && refreshResponse.Description != null)
                                    {
                                        TraktLogger.Error("Failed to refresh access token from trakt.tv, you must go to settings and re-authorise application, Code = '{0}', Reason = '{1}'", refreshResponse.Code, refreshResponse.Description);
                                    }
                                    return _AccountStatus;
                                }
                            }
                        }
                        #endregion

                        // make a request for the users settings to test the connection, 
                        // there is no login method anymore.
                        var response = TraktAPI.TraktAPI.GetUserSettings();
                        if (response != null && response.User != null)
                        {
                            TraktLogger.Info("User {0} successfully signed in and retrieved online settings from trakt.tv", response.User.Username);
                            _AccountStatus = ConnectionState.Connected;

                            // update username incase it has been updated online
                            TraktSettings.Username = response.User.Username;
                            TraktSettings.OnlineSettings = response;
                        }
                        else
                        {
                            // check the error code for the type of error returned
                            if (response != null && response.Description != null)
                            {
                                TraktLogger.Error("Failed to sign-in and get online settings from trakt.tv, Code = '{0}', Reason = '{1}'", response.Code, response.Description);

                                switch (response.Code)
                                {
                                    case 401:
                                    case 403:
                                        _AccountStatus = ConnectionState.UnAuthorised;
                                        // force user to manually authorise again
                                        TraktSettings.UserAccessToken = string.Empty;
                                        TraktSettings.UserRefreshToken = string.Empty;
                                        TraktSettings.UserAccessTokenExpiry = string.Empty;

                                        TraktLogger.Warning("The application must be re-authorised to use your account from trakt.tv from settings");
                                        break;

                                    default:
                                        _AccountStatus = ConnectionState.Invalid;
                                        break;
                                }
                            }
                            else
                            {
                                // very unlikely to ever hit this condition since we should get some sort of protocol error
                                // if a problem with login or server error
                                _AccountStatus = ConnectionState.Invalid;
                            }
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
        public static ConnectionState _AccountStatus = ConnectionState.Pending;

        public static IEnumerable<LanguageISO> ImageLanguages
        {
            get
            {
                if (_languages == null)
                {
                    var cultures = CultureInfo.GetCultures(CultureTypes.AllCultures).Except(CultureInfo.GetCultures(CultureTypes.SpecificCultures));

                    _languages = from c in cultures
                                 select new LanguageISO { DisplayName = c.DisplayName, TwoLetterCode = c.TwoLetterISOLanguageName };
                }

                return _languages;
            }
        }
        static IEnumerable<LanguageISO> _languages;

        #endregion

        #region Methods

        static int GetValueAsIntAndValidate(string section, string key, int defaultValue, int minValue, int maxValue)
        {
            int retValue;

            using (Settings xmlreader = new MPSettings())
            {
                retValue = xmlreader.GetValueAsInt(section, key, defaultValue);
            }

            // validate
            if (retValue < minValue || retValue > maxValue)
                retValue = defaultValue;

            return retValue;
        }

        /// <summary>
        /// Loads the Settings
        /// </summary>
        internal static void LoadSettings(bool loadPersistedCache = true)
        {
            TraktLogger.Info("Loading local settings");

            using (Settings xmlreader = new MPSettings())
            {
                UseCompNameOnPassKey = xmlreader.GetValueAsBool(cTrakt, cUseCompNameOnPassKey, true);
                Username = xmlreader.GetValueAsString(cTrakt, cUsername, "");
                UserAccessToken = xmlreader.GetValueAsString(cTrakt, cUserAccessToken, "");
                UserAccessTokenExpiry = xmlreader.GetValueAsString(cTrakt, cUserAccessTokenExpiry, "");
                UserRefreshToken = xmlreader.GetValueAsString(cTrakt, cUserRefreshToken, "");
                MovingPictures = xmlreader.GetValueAsInt(cTrakt, cMovingPictures, -1);
                TVSeries = xmlreader.GetValueAsInt(cTrakt, cTVSeries, -1);
                MyVideos = xmlreader.GetValueAsInt(cTrakt, cMyVideos, -1);
                MyFilms = xmlreader.GetValueAsInt(cTrakt, cMyFilms, -1);
                OnlineVideos = xmlreader.GetValueAsInt(cTrakt, cOnlineVideos, -1);
                MyTVRecordings = xmlreader.GetValueAsInt(cTrakt, cMyTVRecordings, -1);
                MyTVLive = xmlreader.GetValueAsInt(cTrakt, cMyTVLive, -1);
                ArgusRecordings = xmlreader.GetValueAsInt(cTrakt, cArgusRecordings, -1);
                ArgusTVLive = xmlreader.GetValueAsInt(cTrakt, cArgusTVLive, -1);
                KeepTraktLibraryClean = xmlreader.GetValueAsBool(cTrakt, cKeepTraktLibraryClean, false);
                BlockedFilenames = xmlreader.GetValueAsString(cTrakt, cBlockedFilenames, "[]").FromJSONArray<string>().ToList();
                BlockedFolders = xmlreader.GetValueAsString(cTrakt, cBlockedFolders, "[]").FromJSONArray<string>().ToList();
                //TODOSkippedMovies = xmlreader.GetValueAsString(cTrakt, cSkippedMovies, "{}").FromJSON<SyncMovieCheck>();
                //TODOAlreadyExistMovies = xmlreader.GetValueAsString(cTrakt, cAlreadyExistMovies, "{}").FromJSON<SyncMovieCheck>();
                LogLevel = xmlreader.GetValueAsInt("general", "loglevel", 1);
                SyncTimerLength = GetValueAsIntAndValidate(cTrakt, cSyncTimerLength, 24, 1, 168);
                SyncStartDelay = GetValueAsIntAndValidate(cTrakt, cSyncStartDelay, 5000, 0, 300000);
                TrendingMoviesDefaultLayout = xmlreader.GetValueAsInt(cTrakt, cTrendingMoviesDefaultLayout, 0);
                TrendingShowsDefaultLayout = xmlreader.GetValueAsInt(cTrakt, cTrendingShowsDefaultLayout, 0);
                PopularMoviesDefaultLayout = xmlreader.GetValueAsInt(cTrakt, cPopularMoviesDefaultLayout, 0);
                PopularShowsDefaultLayout = xmlreader.GetValueAsInt(cTrakt, cPopularShowsDefaultLayout, 0);
                RecommendedMoviesDefaultLayout = xmlreader.GetValueAsInt(cTrakt, cRecommendedMoviesDefaultLayout, 0);
                RecommendedShowsDefaultLayout = xmlreader.GetValueAsInt(cTrakt, cRecommendedShowsDefaultLayout, 0);
                WatchListMoviesDefaultLayout = xmlreader.GetValueAsInt(cTrakt, cWatchListMoviesDefaultLayout, 0);
                WatchListShowsDefaultLayout = xmlreader.GetValueAsInt(cTrakt, cWatchListShowsDefaultLayout, 0);
                WatchListEpisodesDefaultLayout = xmlreader.GetValueAsInt(cTrakt, cWatchListEpisodesDefaultLayout, 0);
                ListsDefaultLayout = xmlreader.GetValueAsInt(cTrakt, cListsDefaultLayout, 0);
                ListItemsDefaultLayout = xmlreader.GetValueAsInt(cTrakt, cListItemsDefaultLayout, 0);
                RelatedMoviesDefaultLayout = xmlreader.GetValueAsInt(cTrakt, cRelatedMoviesDefaultLayout, 0);
                RelatedShowsDefaultLayout = xmlreader.GetValueAsInt(cTrakt, cRelatedShowsDefaultLayout, 0);
                ShowSeasonsDefaultLayout = xmlreader.GetValueAsInt(cTrakt, cShowSeasonsDefaultLayout, 0);
                SeasonEpisodesDefaultLayout = xmlreader.GetValueAsInt(cTrakt, cSeasonEpisodesDefaultLayout, 0);
                DefaultCalendarView = xmlreader.GetValueAsInt(cTrakt, cDefaultCalendarView, 0);
                DefaultCalendarStartDate = xmlreader.GetValueAsInt(cTrakt, cDefaultCalendarStartDate, 0);
                DownloadFullSizeFanart = xmlreader.GetValueAsBool(cTrakt, cDownloadFullSizeFanart, false);
                DownloadFanart = xmlreader.GetValueAsBool(cTrakt, cDownloadFanart, true);
                WebRequestCacheMinutes = GetValueAsIntAndValidate(cTrakt, cWebRequestCacheMinutes, 15, 1, 1440);
                WebRequestTimeout = GetValueAsIntAndValidate(cTrakt, cWebRequestTimeout, 30000, 15000, 90000);
                GetFollowerRequestsOnStartup = xmlreader.GetValueAsBool(cTrakt, cGetFollowerRequestsOnStartup, false);
                MovingPicturesCategories = xmlreader.GetValueAsBool(cTrakt, cMovingPicturesCategories, false);
                MovingPicturesFilters = xmlreader.GetValueAsBool(cTrakt, cMovingPicturesFilters, false);
                CalendarHideTVShowsInWatchList = xmlreader.GetValueAsBool(cTrakt, cCalendarHideTVShowsInWatchList, false);
                HideWatchedRelatedMovies = xmlreader.GetValueAsBool(cTrakt, cHideWatchedRelatedMovies, false);
                HideWatchedRelatedShows = xmlreader.GetValueAsBool(cTrakt, cHideWatchedRelatedShows, false);
                HideSpoilersOnShouts = xmlreader.GetValueAsBool(cTrakt, cHideSpoilersOnShouts, false);                
                SyncRatings = xmlreader.GetValueAsBool(cTrakt, cSyncRatings, true);
                ShowRateDialogOnWatched = xmlreader.GetValueAsBool(cTrakt, cShowRateDialogOnWatched, true);
                DashboardActivityPollInterval = GetValueAsIntAndValidate(cTrakt, cDashboardActivityPollInterval, 15000, 15000, 300000);
                DashboardTrendingPollInterval = GetValueAsIntAndValidate(cTrakt, cDashboardTrendingPollInterval, 300000, 300000, 18000000);
                DashboardLoadDelay = GetValueAsIntAndValidate(cTrakt, cDashboardLoadDelay, 200, 100, 1000);
                DashboardMovieTrendingActive = xmlreader.GetValueAsBool(cTrakt, cDashboardMovieTrendingActive, false);
                MovieRecommendationGenre = xmlreader.GetValueAsString(cTrakt, cMovieRecommendationGenre, "All");
                MovieRecommendationHideCollected = xmlreader.GetValueAsBool(cTrakt, cMovieRecommendationHideCollected, false);
                MovieRecommendationHideWatchlisted = xmlreader.GetValueAsBool(cTrakt, cMovieRecommendationHideWatchlisted, false);
                MovieRecommendationStartYear = xmlreader.GetValueAsInt(cTrakt, cMovieRecommendationStartYear, 0);
                MovieRecommendationEndYear = xmlreader.GetValueAsInt(cTrakt, cMovieRecommendationEndYear, 0);
                ShowRecommendationGenre = xmlreader.GetValueAsString(cTrakt, cShowRecommendationGenre, "All");
                ShowRecommendationHideCollected = xmlreader.GetValueAsBool(cTrakt, cShowRecommendationHideCollected, false);
                ShowRecommendationHideWatchlisted = xmlreader.GetValueAsBool(cTrakt, cShowRecommendationHideWatchlisted, false);
                ShowRecommendationStartYear = xmlreader.GetValueAsInt(cTrakt, cShowRecommendationStartYear, 0);
                ShowRecommendationEndYear = xmlreader.GetValueAsInt(cTrakt, cShowRecommendationEndYear, 0);
                SortByRecommendedMovies = xmlreader.GetValueAsString(cTrakt, cSortByRecommendedMovies, "{\"Field\": 0,\"Direction\": 0}").FromJSON<SortBy>();
                SortByRecommendedShows = xmlreader.GetValueAsString(cTrakt, cSortByRecommendedShows, "{\"Field\": 0,\"Direction\": 0}").FromJSON<SortBy>();
                SortByTrendingMovies = xmlreader.GetValueAsString(cTrakt, cSortByTrendingMovies, "{\"Field\": 5,\"Direction\": 1}").FromJSON<SortBy>();
                SortByTrendingShows = xmlreader.GetValueAsString(cTrakt, cSortByTrendingShows, "{\"Field\": 5,\"Direction\": 1}").FromJSON<SortBy>();
                SortByPopularMovies = xmlreader.GetValueAsString(cTrakt, cSortByPopularMovies, "{\"Field\": 7,\"Direction\": 1}").FromJSON<SortBy>();
                SortByPopularShows = xmlreader.GetValueAsString(cTrakt, cSortByPopularShows, "{\"Field\": 7,\"Direction\": 1}").FromJSON<SortBy>();
                SortByWatchListMovies = xmlreader.GetValueAsString(cTrakt, cSortByWatchListMovies, "{\"Field\": 6,\"Direction\": 1}").FromJSON<SortBy>();
                SortByWatchListShows = xmlreader.GetValueAsString(cTrakt, cSortByWatchListShows, "{\"Field\": 6,\"Direction\": 1}").FromJSON<SortBy>();
                EnableJumpToForTVShows = xmlreader.GetValueAsBool(cTrakt, cEnableJumpToForTVShows, false);
                MyFilmsCategories = xmlreader.GetValueAsBool(cTrakt, cMyFilmsCategories, false);
                SortSeasonsAscending = xmlreader.GetValueAsBool(cTrakt, cSortSeasonsAscending, false);
                RememberLastSelectedActivity = xmlreader.GetValueAsBool(cTrakt, cRememberLastSelectedActivity, true);
                MovPicsRatingDlgDelay = GetValueAsIntAndValidate(cTrakt, cMovPicsRatingDlgDelay, 500, 250, 1000);
                ShowRateDlgForPlaylists = xmlreader.GetValueAsBool(cTrakt, cShowRateDlgForPlaylists, false);
                TrendingMoviesHideWatched = xmlreader.GetValueAsBool(cTrakt, cTrendingMoviesHideWatched, false);
                TrendingMoviesHideWatchlisted = xmlreader.GetValueAsBool(cTrakt, cTrendingMoviesHideWatchlisted, false);
                TrendingMoviesHideCollected = xmlreader.GetValueAsBool(cTrakt, cTrendingMoviesHideCollected, false);
                TrendingMoviesHideRated = xmlreader.GetValueAsBool(cTrakt, cTrendingMoviesHideRated, false);
                TrendingShowsHideWatched = xmlreader.GetValueAsBool(cTrakt, cTrendingShowsHideWatched, false);
                TrendingShowsHideWatchlisted = xmlreader.GetValueAsBool(cTrakt, cTrendingShowsHideWatchlisted, false);
                TrendingShowsHideCollected = xmlreader.GetValueAsBool(cTrakt, cTrendingShowsHideCollected, false);
                TrendingShowsHideRated = xmlreader.GetValueAsBool(cTrakt, cTrendingShowsHideRated, false);
                PopularMoviesHideWatched = xmlreader.GetValueAsBool(cTrakt, cPopularMoviesHideWatched, false);
                PopularMoviesHideWatchlisted = xmlreader.GetValueAsBool(cTrakt, cPopularMoviesHideWatchlisted, false);
                PopularMoviesHideCollected = xmlreader.GetValueAsBool(cTrakt, cPopularMoviesHideCollected, false);
                PopularMoviesHideRated = xmlreader.GetValueAsBool(cTrakt, cPopularMoviesHideRated, false);
                PopularShowsHideWatched = xmlreader.GetValueAsBool(cTrakt, cPopularShowsHideWatched, false);
                PopularShowsHideWatchlisted = xmlreader.GetValueAsBool(cTrakt, cPopularShowsHideWatchlisted, false);
                PopularShowsHideCollected = xmlreader.GetValueAsBool(cTrakt, cPopularShowsHideCollected, false);
                PopularShowsHideRated = xmlreader.GetValueAsBool(cTrakt, cPopularShowsHideRated, false);
                AnticipatedMoviesHideWatchlisted = xmlreader.GetValueAsBool(cTrakt, cAnticipatedMoviesHideWatchlisted, false);
                AnticipatedShowsHideWatchlisted = xmlreader.GetValueAsBool(cTrakt, cAnticipatedShowsHideWatchlisted, false);
                DefaultNetworkView = xmlreader.GetValueAsInt(cTrakt, cDefaultNetworkView, 1);
                RecentWatchedMoviesDefaultLayout = xmlreader.GetValueAsInt(cTrakt, cRecentWatchedMoviesDefaultLayout, 0);
                RecentWatchedEpisodesDefaultLayout = xmlreader.GetValueAsInt(cTrakt, cRecentWatchedEpisodesDefaultLayout, 0);
                RecentAddedMoviesDefaultLayout = xmlreader.GetValueAsInt(cTrakt, cRecentAddedMoviesDefaultLayout, 0);
                RecentAddedEpisodesDefaultLayout = xmlreader.GetValueAsInt(cTrakt, cRecentAddedEpisodesDefaultLayout, 0);
                SyncLibrary = xmlreader.GetValueAsBool(cTrakt, cSyncLibrary, true);
                SearchMoviesDefaultLayout = xmlreader.GetValueAsInt(cTrakt, cSearchMoviesDefaultLayout, 0);
                SearchShowsDefaultLayout = xmlreader.GetValueAsInt(cTrakt, cSearchShowsDefaultLayout, 0);
                SearchEpisodesDefaultLayout = xmlreader.GetValueAsInt(cTrakt, cSearchEpisodesDefaultLayout, 0);
                SearchPeopleDefaultLayout = xmlreader.GetValueAsInt(cTrakt, cSearchPeopleDefaultLayout, 0);
                SearchUsersDefaultLayout = xmlreader.GetValueAsInt(cTrakt, cSearchUsersDefaultLayout, 0);
                SearchTypes = xmlreader.GetValueAsInt(cTrakt, cSearchTypes, 1);
                ShowSearchResultsBreakdown = xmlreader.GetValueAsBool(cTrakt, cShowSearchResultsBreakdown, true);
                MaxSearchResults = GetValueAsIntAndValidate(cTrakt, cMaxSearchResults, 30, 1, 200);
                FilterTrendingOnDashboard = xmlreader.GetValueAsBool(cTrakt, cFilterTrendingOnDashboard, false);
                IgnoreWatchedPercentOnDVD = xmlreader.GetValueAsBool(cTrakt, cIgnoreWatchedPercentOnDVD, true);
                ActivityStreamView = xmlreader.GetValueAsInt(cTrakt, cActivityStreamView, 5);
                LastSyncActivities = xmlreader.GetValueAsString(cTrakt, cLastSyncActivities, new TraktLastSyncActivities().ToJSON()).FromJSON<TraktLastSyncActivities>();
                SyncBatchSize = GetValueAsIntAndValidate(cTrakt, cSyncBatchSize, 100, 25, 1000);
                SyncPlayback = xmlreader.GetValueAsBool(cTrakt, cSyncPlayback, true);
                SyncResumeDelta = GetValueAsIntAndValidate(cTrakt, cSyncResumeDelta, 5, 0, 600);
                SyncPlaybackOnEnterPlugin = xmlreader.GetValueAsBool(cTrakt, cSyncPlaybackOnEnterPlugin, false);
                SyncPlaybackCacheExpiry = GetValueAsIntAndValidate(cTrakt, cSyncPlaybackCacheExpiry, 5, 1, 1440);
                MaxTrendingMoviesRequest = GetValueAsIntAndValidate(cTrakt, cMaxTrendingMoviesRequest, 40, 1, 1000);
                MaxTrendingShowsRequest = GetValueAsIntAndValidate(cTrakt, cMaxTrendingShowsRequest, 40, 1, 1000);
                MaxPopularMoviesRequest = GetValueAsIntAndValidate(cTrakt, cMaxPopularMoviesRequest, 40, 1, 1000);
                MaxPopularShowsRequest = GetValueAsIntAndValidate(cTrakt, cMaxPopularShowsRequest, 40, 1, 1000);
                MaxAnticipatedMoviesRequest = GetValueAsIntAndValidate(cTrakt, cMaxAnticipatedMoviesRequest, 40, 1, 1000);
                MaxAnticipatedShowsRequest = GetValueAsIntAndValidate(cTrakt, cMaxAnticipatedShowsRequest, 40, 1, 1000);
                UseSSL = xmlreader.GetValueAsBool(cTrakt, cUseSSL, false);
                LastListActivities = xmlreader.GetValueAsString(cTrakt, cLastListActivities, "[]").FromJSONArray<TraktCache.ListActivity>();
                MaxRelatedMoviesRequest = GetValueAsIntAndValidate(cTrakt, cMaxRelatedMoviesRequest, 10, 1, 100);
                MaxRelatedMoviesUnWatchedRequest = GetValueAsIntAndValidate(cTrakt, cMaxRelatedMoviesUnWatchedRequest, 40, 1, 100);
                MaxRelatedShowsRequest = GetValueAsIntAndValidate(cTrakt, cMaxRelatedShowsRequest, 10, 1, 100);
                MaxRelatedShowsUnWatchedRequest = GetValueAsIntAndValidate(cTrakt, cMaxRelatedShowsUnWatchedRequest, 40, 1, 1000);
                MaxUserWatchedMoviesRequest = GetValueAsIntAndValidate(cTrakt, cMaxUserWatchedMoviesRequest, 40, 1, 1000);
                MaxUserWatchedEpisodesRequest = GetValueAsIntAndValidate(cTrakt, cMaxUserWatchedEpisodesRequest, 40, 1, 1000);
                MaxUserCommentsRequest = GetValueAsIntAndValidate(cTrakt, cMaxUserCommentsRequest, 40, 1, 1000);
                DashboardActivityFilter = xmlreader.GetValueAsString(cTrakt, cDashboardActivityFilter, "{}").FromJSON<ActivityFilter>();
                SkipMoviesWithNoIdsOnSync = xmlreader.GetValueAsBool(cTrakt, cSkipMoviesWithNoIdsOnSync, true);
                PersonMovieCreditsDefaultLayout = xmlreader.GetValueAsInt(cTrakt, cPersonMovieCreditsDefaultLayout, 0);
                PersonShowCreditsDefaultLayout = xmlreader.GetValueAsInt(cTrakt, cPersonShowCreditsDefaultLayout, 0);
                SortByCreditMovies = xmlreader.GetValueAsString(cTrakt, cSortByCreditMovies, "{\"Field\": 7,\"Direction\": 1}").FromJSON<SortBy>();
                SortByCreditShows = xmlreader.GetValueAsString(cTrakt, cSortByCreditShows, "{\"Field\": 7,\"Direction\": 1}").FromJSON<SortBy>();
                CreditMoviesHideWatched = xmlreader.GetValueAsBool(cTrakt, cCreditMoviesHideWatched, false);
                CreditMoviesHideWatchlisted = xmlreader.GetValueAsBool(cTrakt, cCreditMoviesHideWatchlisted, false);
                CreditMoviesHideCollected = xmlreader.GetValueAsBool(cTrakt, cCreditMoviesHideCollected, false);
                CreditMoviesHideRated = xmlreader.GetValueAsBool(cTrakt, cCreditMoviesHideRated, false);
                CreditShowsHideWatched = xmlreader.GetValueAsBool(cTrakt, cCreditShowsHideWatched, false);
                CreditShowsHideWatchlisted = xmlreader.GetValueAsBool(cTrakt, cCreditShowsHideWatchlisted, false);
                CreditShowsHideCollected = xmlreader.GetValueAsBool(cTrakt, cCreditShowsHideCollected, false);
                CreditShowsHideRated = xmlreader.GetValueAsBool(cTrakt, cCreditShowsHideRated, false);
                CreditsMovieDefaultLayout = xmlreader.GetValueAsInt(cTrakt, cCreditsMovieDefaultLayout, 0);
                CreditsShowDefaultLayout = xmlreader.GetValueAsInt(cTrakt, cCreditsShowDefaultLayout, 0);
                AnticipatedMoviesDefaultLayout = xmlreader.GetValueAsInt(cTrakt, cAnticipatedMoviesDefaultLayout, 0);
                AnticipatedShowsDefaultLayout = xmlreader.GetValueAsInt(cTrakt, cAnticipatedShowsDefaultLayout, 0);
                SortByAnticipatedMovies = xmlreader.GetValueAsString(cTrakt, cSortByAnticipatedMovies, "{\"Field\": 8,\"Direction\": 1}").FromJSON<SortBy>();
                SortByAnticipatedShows = xmlreader.GetValueAsString(cTrakt, cSortByAnticipatedShows, "{\"Field\": 8,\"Direction\": 1}").FromJSON<SortBy>();
                BoxOfficeDefaultLayout = xmlreader.GetValueAsInt(cTrakt, cBoxOfficeDefaultLayout, 0);
                ParentalControlsEnabled = xmlreader.GetValueAsBool(cTrakt, cParentalControlsEnabled, false);
                ParentalControlsPinCode = xmlreader.GetValueAsString(cTrakt, cParentalControlsPinCode, "");
                ParentalIgnoreAfterEnabled = xmlreader.GetValueAsBool(cTrakt, cParentalIgnoreAfterEnabled, false);
                ParentalIgnoreAfterTime = xmlreader.GetValueAsString(cTrakt, cParentalIgnoreAfterTime, new DateTime().ToShortTimeString());
                ParentalIgnoreMovieRatingEnabled = xmlreader.GetValueAsBool(cTrakt, cParentalIgnoreMovieRatingEnabled, false);
                ParentalIgnoreShowRatingEnabled = xmlreader.GetValueAsBool(cTrakt, cParentalIgnoreShowRatingEnabled, false);
                ParentalIgnoreMovieRating = xmlreader.GetValueAsString(cTrakt, cParentalIgnoreMovieRating, "G");
                ParentalIgnoreShowRating = xmlreader.GetValueAsString(cTrakt, cParentalIgnoreShowRating, "TV-G");
                TmdbConfiguration = xmlreader.GetValueAsString(cTrakt, cTmdbConfiguration, "{}").FromJSON<TmdbConfiguration>();
                TmdbConfigurationAge = xmlreader.GetValueAsString(cTrakt, cTmdbConfigurationAge, new DateTime().ToString());
                TmdbPreferredBackdropSize = xmlreader.GetValueAsString(cTrakt, cTmdbPreferredBackdropSize, "w1280");
                TmdbPreferredPosterSize = xmlreader.GetValueAsString(cTrakt, cTmdbPreferredPosterSize, "w342");
                TmdbPreferredEpisodeThumbSize = xmlreader.GetValueAsString(cTrakt, cTmdbPreferredEpisodeThumbSize, "w300");
                TmdbPreferredHeadshotSize = xmlreader.GetValueAsString(cTrakt, cTmdbPreferredHeadshotSize, "h632");
                TmdbShowImageMaxCacheAge = GetValueAsIntAndValidate(cTrakt, cTmdbShowImageMaxCacheAge, 30, 1, 365);
                TmdbMovieImageMaxCacheAge = GetValueAsIntAndValidate(cTrakt, cTmdbMovieImageMaxCacheAge, 30, 1, 365);
                TmdbSeasonImageMaxCacheAge = GetValueAsIntAndValidate(cTrakt, cTmdbSeasonImageMaxCacheAge, 30, 1, 365);
                TmdbEpisodeImageMaxCacheAge = GetValueAsIntAndValidate(cTrakt, cTmdbEpisodeImageMaxCacheAge, 30, 1, 365);
                TmdbPersonImageMaxCacheAge = GetValueAsIntAndValidate(cTrakt, cTmdbPersonImageMaxCacheAge, 30, 1, 365);
                TmdbPreferredImageLanguage = xmlreader.GetValueAsString(cTrakt, cTmdbPreferredImageLanguage, "en");
                OnlineSettings = xmlreader.GetValueAsString(cTrakt, cTraktOnlineSettings, "{}").FromJSON<TraktAPI.DataStructures.TraktSettings>();
            }

            // initialise API settings
            TraktAPI.TraktAPI.ClientId = ClientId;
            TraktAPI.TraktAPI.ClientSecret = ClientSecret;
            TraktAPI.TraktAPI.RedirectUri = RedirectUri;
            
            TraktAPI.TraktAPI.UserAgent = UserAgent;
            TraktAPI.TraktAPI.UseSSL = UseSSL;

            TmdbAPI.TmdbAPI.UserAgent = UserAgent;

            // initialise the last sync activities 
            if (LastSyncActivities == null) LastSyncActivities = new TraktLastSyncActivities();
            if (LastSyncActivities.Movies == null) LastSyncActivities.Movies = new TraktLastSyncActivities.MovieActivities();
            if (LastSyncActivities.Episodes == null) LastSyncActivities.Episodes = new TraktLastSyncActivities.EpisodeActivities();
            if (LastSyncActivities.Seasons == null) LastSyncActivities.Seasons = new TraktLastSyncActivities.SeasonActivities();
            if (LastSyncActivities.Shows == null) LastSyncActivities.Shows = new TraktLastSyncActivities.ShowActivities();
            if (LastSyncActivities.Lists == null) LastSyncActivities.Lists = new TraktLastSyncActivities.ListActivities();
            if (LastSyncActivities.Comments == null) LastSyncActivities.Comments = new TraktLastSyncActivities.CommentActivities();

            // initialise the dashboard activity filter
            if (DashboardActivityFilter == null) DashboardActivityFilter = new ActivityFilter();
            if (DashboardActivityFilter.Actions == null) DashboardActivityFilter.Actions = new ActivityFilter.Action();
            if (DashboardActivityFilter.Types == null) DashboardActivityFilter.Types = new ActivityFilter.Type();

            if (loadPersistedCache)
            {
                TraktLogger.Info("Loading persisted file cache");
                LastActivityLoad = TraktCache.LoadFileCache(cLastActivityFileCache, "{}").FromJSON<TraktActivity>();
                LastStatistics = TraktCache.LoadFileCache(cLastStatisticsFileCache, null).FromJSON<TraktUserStatistics>();
                LastUserProfile = TraktCache.LoadFileCache(cLastUserProfileFileCache, null).FromJSON<TraktUserSummary>();
                LastTrendingMovies = TraktCache.LoadFileCache(cLastTrendingMovieFileCache, "[]").FromJSONArray<TraktMovieTrending>();
                LastTrendingShows = TraktCache.LoadFileCache(cLastTrendingShowFileCache, "[]").FromJSONArray<TraktShowTrending>();
            }

            // correct any settings in internal plugins if needed
            UpdateInternalPluginSettings();

            TraktLogger.Info("Finished loading local settings");
        }

        /// <summary>
        /// Saves the Settings
        /// </summary>
        internal static void SaveSettings(bool savePersistedCache = true)
        {
            TraktLogger.Info("Saving settings");
            using (Settings xmlwriter = new MPSettings())
            {
                xmlwriter.SetValue(cTrakt, cSettingsVersion, SettingsVersion);
                xmlwriter.SetValue(cTrakt, cUsername, Username);
                xmlwriter.SetValue(cTrakt, cUserAccessToken, UserAccessToken);
                xmlwriter.SetValue(cTrakt, cUserAccessTokenExpiry, UserAccessTokenExpiry);
                xmlwriter.SetValue(cTrakt, cUserRefreshToken, UserRefreshToken);
                xmlwriter.SetValue(cTrakt, cMovingPictures, MovingPictures);
                xmlwriter.SetValue(cTrakt, cTVSeries, TVSeries);
                xmlwriter.SetValue(cTrakt, cMyVideos, MyVideos);
                xmlwriter.SetValue(cTrakt, cMyFilms, MyFilms);
                xmlwriter.SetValue(cTrakt, cOnlineVideos, OnlineVideos);
                xmlwriter.SetValue(cTrakt, cMyTVRecordings, MyTVRecordings);
                xmlwriter.SetValue(cTrakt, cMyTVLive, MyTVLive);
                xmlwriter.SetValue(cTrakt, cArgusRecordings, ArgusRecordings);
                xmlwriter.SetValue(cTrakt, cArgusTVLive, ArgusTVLive);
                xmlwriter.SetValueAsBool(cTrakt, cKeepTraktLibraryClean, KeepTraktLibraryClean);
                xmlwriter.SetValue(cTrakt, cBlockedFilenames, BlockedFilenames.ToJSON());
                xmlwriter.SetValue(cTrakt, cBlockedFolders, BlockedFolders.ToJSON());
                //TODOxmlwriter.SetValue(cTrakt, cSkippedMovies, SkippedMovies.ToJSON());
                //TODOxmlwriter.SetValue(cTrakt, cAlreadyExistMovies, AlreadyExistMovies.ToJSON());
                xmlwriter.SetValue(cTrakt, cSyncTimerLength, SyncTimerLength);
                xmlwriter.SetValue(cTrakt, cSyncStartDelay, SyncStartDelay);
                xmlwriter.SetValue(cTrakt, cTrendingMoviesDefaultLayout, TrendingMoviesDefaultLayout);
                xmlwriter.SetValue(cTrakt, cTrendingShowsDefaultLayout, TrendingShowsDefaultLayout);
                xmlwriter.SetValue(cTrakt, cPopularMoviesDefaultLayout, PopularMoviesDefaultLayout);
                xmlwriter.SetValue(cTrakt, cPopularShowsDefaultLayout, PopularShowsDefaultLayout);
                xmlwriter.SetValue(cTrakt, cRecommendedMoviesDefaultLayout, RecommendedMoviesDefaultLayout);
                xmlwriter.SetValue(cTrakt, cRecommendedShowsDefaultLayout, RecommendedShowsDefaultLayout);
                xmlwriter.SetValue(cTrakt, cWatchListMoviesDefaultLayout, WatchListMoviesDefaultLayout);
                xmlwriter.SetValue(cTrakt, cWatchListShowsDefaultLayout, WatchListShowsDefaultLayout);
                xmlwriter.SetValue(cTrakt, cWatchListEpisodesDefaultLayout, WatchListEpisodesDefaultLayout);
                xmlwriter.SetValue(cTrakt, cRelatedMoviesDefaultLayout, RelatedMoviesDefaultLayout);
                xmlwriter.SetValue(cTrakt, cRelatedShowsDefaultLayout, RelatedShowsDefaultLayout);
                xmlwriter.SetValue(cTrakt, cListsDefaultLayout, ListsDefaultLayout);
                xmlwriter.SetValue(cTrakt, cListItemsDefaultLayout, ListItemsDefaultLayout);
                xmlwriter.SetValue(cTrakt, cShowSeasonsDefaultLayout, ShowSeasonsDefaultLayout);
                xmlwriter.SetValue(cTrakt, cSeasonEpisodesDefaultLayout, SeasonEpisodesDefaultLayout);
                xmlwriter.SetValue(cTrakt, cDefaultCalendarView, DefaultCalendarView);
                xmlwriter.SetValue(cTrakt, cDefaultCalendarStartDate, DefaultCalendarStartDate);
                xmlwriter.SetValueAsBool(cTrakt, cDownloadFullSizeFanart, DownloadFullSizeFanart);
                xmlwriter.SetValueAsBool(cTrakt, cDownloadFanart, DownloadFanart);
                xmlwriter.SetValue(cTrakt, cWebRequestCacheMinutes, WebRequestCacheMinutes);
                xmlwriter.SetValue(cTrakt, cWebRequestTimeout, WebRequestTimeout);
                xmlwriter.SetValueAsBool(cTrakt, cGetFollowerRequestsOnStartup, GetFollowerRequestsOnStartup);
                xmlwriter.SetValueAsBool(cTrakt, cMovingPicturesCategories, MovingPicturesCategories);
                xmlwriter.SetValueAsBool(cTrakt, cMovingPicturesFilters, MovingPicturesFilters);
                xmlwriter.SetValueAsBool(cTrakt, cCalendarHideTVShowsInWatchList, CalendarHideTVShowsInWatchList);
                xmlwriter.SetValueAsBool(cTrakt, cHideWatchedRelatedMovies, HideWatchedRelatedMovies);
                xmlwriter.SetValueAsBool(cTrakt, cHideWatchedRelatedShows, HideWatchedRelatedShows);
                xmlwriter.SetValueAsBool(cTrakt, cHideSpoilersOnShouts, HideSpoilersOnShouts);
                xmlwriter.SetValueAsBool(cTrakt, cSyncRatings, SyncRatings);
                xmlwriter.SetValueAsBool(cTrakt, cShowRateDialogOnWatched, ShowRateDialogOnWatched);
                xmlwriter.SetValue(cTrakt, cDashboardActivityPollInterval, DashboardActivityPollInterval);
                xmlwriter.SetValue(cTrakt, cDashboardTrendingPollInterval, DashboardTrendingPollInterval);
                xmlwriter.SetValue(cTrakt, cDashboardLoadDelay, DashboardLoadDelay);
                xmlwriter.SetValueAsBool(cTrakt, cDashboardMovieTrendingActive, DashboardMovieTrendingActive);
                xmlwriter.SetValue(cTrakt, cMovieRecommendationGenre, MovieRecommendationGenre);
                xmlwriter.SetValueAsBool(cTrakt, cMovieRecommendationHideCollected, MovieRecommendationHideCollected);
                xmlwriter.SetValueAsBool(cTrakt, cMovieRecommendationHideWatchlisted, MovieRecommendationHideWatchlisted);
                xmlwriter.SetValue(cTrakt, cMovieRecommendationStartYear, MovieRecommendationStartYear);
                xmlwriter.SetValue(cTrakt, cMovieRecommendationEndYear, MovieRecommendationEndYear);
                xmlwriter.SetValue(cTrakt, cShowRecommendationGenre, ShowRecommendationGenre);
                xmlwriter.SetValueAsBool(cTrakt, cShowRecommendationHideCollected, ShowRecommendationHideCollected);
                xmlwriter.SetValueAsBool(cTrakt, cShowRecommendationHideWatchlisted, ShowRecommendationHideWatchlisted);
                xmlwriter.SetValue(cTrakt, cShowRecommendationStartYear, ShowRecommendationStartYear);
                xmlwriter.SetValue(cTrakt, cShowRecommendationEndYear, ShowRecommendationEndYear);
                xmlwriter.SetValue(cTrakt, cSortByRecommendedMovies, SortByRecommendedMovies.ToJSON());
                xmlwriter.SetValue(cTrakt, cSortByRecommendedShows, SortByRecommendedShows.ToJSON());
                xmlwriter.SetValue(cTrakt, cSortByTrendingMovies, SortByTrendingMovies.ToJSON());
                xmlwriter.SetValue(cTrakt, cSortByTrendingShows, SortByTrendingShows.ToJSON());
                xmlwriter.SetValue(cTrakt, cSortByPopularMovies, SortByPopularMovies.ToJSON());
                xmlwriter.SetValue(cTrakt, cSortByPopularShows, SortByPopularShows.ToJSON());
                xmlwriter.SetValue(cTrakt, cSortByWatchListMovies, SortByWatchListMovies.ToJSON());
                xmlwriter.SetValue(cTrakt, cSortByWatchListShows, SortByWatchListShows.ToJSON());
                xmlwriter.SetValue(cTrakt, cSortByAnticipatedMovies, SortByAnticipatedMovies.ToJSON());
                xmlwriter.SetValue(cTrakt, cSortByAnticipatedShows, SortByAnticipatedShows.ToJSON());
                xmlwriter.SetValueAsBool(cTrakt, cEnableJumpToForTVShows, EnableJumpToForTVShows);
                xmlwriter.SetValueAsBool(cTrakt, cMyFilmsCategories, MyFilmsCategories);
                xmlwriter.SetValueAsBool(cTrakt, cSortSeasonsAscending, SortSeasonsAscending);
                xmlwriter.SetValueAsBool(cTrakt, cRememberLastSelectedActivity, RememberLastSelectedActivity);
                xmlwriter.SetValueAsBool(cTrakt, cShowRateDlgForPlaylists, ShowRateDlgForPlaylists);
                xmlwriter.SetValueAsBool(cTrakt, cTrendingMoviesHideWatched, TrendingMoviesHideWatched);
                xmlwriter.SetValueAsBool(cTrakt, cTrendingMoviesHideWatchlisted, TrendingMoviesHideWatchlisted);
                xmlwriter.SetValueAsBool(cTrakt, cTrendingMoviesHideCollected, TrendingMoviesHideCollected);
                xmlwriter.SetValueAsBool(cTrakt, cTrendingMoviesHideRated, TrendingMoviesHideRated);
                xmlwriter.SetValueAsBool(cTrakt, cTrendingShowsHideWatched, TrendingShowsHideWatched);
                xmlwriter.SetValueAsBool(cTrakt, cTrendingShowsHideWatchlisted, TrendingShowsHideWatchlisted);
                xmlwriter.SetValueAsBool(cTrakt, cTrendingShowsHideCollected, TrendingShowsHideCollected);
                xmlwriter.SetValueAsBool(cTrakt, cTrendingShowsHideRated, TrendingShowsHideRated);
                xmlwriter.SetValueAsBool(cTrakt, cPopularMoviesHideWatched, PopularMoviesHideWatched);
                xmlwriter.SetValueAsBool(cTrakt, cPopularMoviesHideWatchlisted, PopularMoviesHideWatchlisted);
                xmlwriter.SetValueAsBool(cTrakt, cPopularMoviesHideCollected, PopularMoviesHideCollected);
                xmlwriter.SetValueAsBool(cTrakt, cPopularMoviesHideRated, PopularMoviesHideRated);
                xmlwriter.SetValueAsBool(cTrakt, cPopularShowsHideWatched, PopularShowsHideWatched);
                xmlwriter.SetValueAsBool(cTrakt, cPopularShowsHideWatchlisted, PopularShowsHideWatchlisted);
                xmlwriter.SetValueAsBool(cTrakt, cPopularShowsHideCollected, PopularShowsHideCollected);
                xmlwriter.SetValueAsBool(cTrakt, cPopularShowsHideRated, PopularShowsHideRated);
                xmlwriter.SetValueAsBool(cTrakt, cAnticipatedMoviesHideWatchlisted, AnticipatedMoviesHideWatchlisted);
                xmlwriter.SetValueAsBool(cTrakt, cAnticipatedShowsHideWatchlisted, AnticipatedShowsHideWatchlisted);
                xmlwriter.SetValue(cTrakt, cDefaultNetworkView, DefaultNetworkView);
                xmlwriter.SetValue(cTrakt, cRecentWatchedMoviesDefaultLayout, RecentWatchedMoviesDefaultLayout);
                xmlwriter.SetValue(cTrakt, cRecentWatchedEpisodesDefaultLayout, RecentWatchedEpisodesDefaultLayout);
                xmlwriter.SetValue(cTrakt, cRecentAddedMoviesDefaultLayout, RecentAddedMoviesDefaultLayout);
                xmlwriter.SetValue(cTrakt, cRecentAddedEpisodesDefaultLayout, RecentAddedEpisodesDefaultLayout);
                xmlwriter.SetValueAsBool(cTrakt, cSyncLibrary, SyncLibrary);
                xmlwriter.SetValue(cTrakt, cSearchMoviesDefaultLayout, SearchMoviesDefaultLayout);
                xmlwriter.SetValue(cTrakt, cSearchShowsDefaultLayout, SearchShowsDefaultLayout);
                xmlwriter.SetValue(cTrakt, cSearchEpisodesDefaultLayout, SearchEpisodesDefaultLayout);
                xmlwriter.SetValue(cTrakt, cSearchPeopleDefaultLayout, SearchPeopleDefaultLayout);
                xmlwriter.SetValue(cTrakt, cSearchUsersDefaultLayout, SearchUsersDefaultLayout);
                xmlwriter.SetValue(cTrakt, cSearchTypes, SearchTypes);
                xmlwriter.SetValueAsBool(cTrakt, cShowSearchResultsBreakdown, ShowSearchResultsBreakdown);
                xmlwriter.SetValue(cTrakt, cMaxSearchResults, MaxSearchResults);
                xmlwriter.SetValueAsBool(cTrakt, cFilterTrendingOnDashboard, FilterTrendingOnDashboard);
                xmlwriter.SetValueAsBool(cTrakt, cIgnoreWatchedPercentOnDVD, IgnoreWatchedPercentOnDVD);
                xmlwriter.SetValue(cTrakt, cActivityStreamView, ActivityStreamView);
                xmlwriter.SetValue(cTrakt, cLastSyncActivities, LastSyncActivities.ToJSON());
                xmlwriter.SetValue(cTrakt, cSyncBatchSize, SyncBatchSize);
                xmlwriter.SetValueAsBool(cTrakt, cUseCompNameOnPassKey, UseCompNameOnPassKey);
                xmlwriter.SetValueAsBool(cTrakt, cSyncPlayback, SyncPlayback);
                xmlwriter.SetValue(cTrakt, cSyncResumeDelta, SyncResumeDelta);
                xmlwriter.SetValueAsBool(cTrakt, cSyncPlaybackOnEnterPlugin, SyncPlaybackOnEnterPlugin);
                xmlwriter.SetValue(cTrakt, cSyncPlaybackCacheExpiry, SyncPlaybackCacheExpiry);
                xmlwriter.SetValue(cTrakt, cMaxTrendingMoviesRequest, MaxTrendingMoviesRequest);
                xmlwriter.SetValue(cTrakt, cMaxTrendingShowsRequest, MaxTrendingShowsRequest);
                xmlwriter.SetValue(cTrakt, cMaxPopularMoviesRequest, MaxPopularMoviesRequest);
                xmlwriter.SetValue(cTrakt, cMaxPopularShowsRequest, MaxPopularShowsRequest);
                xmlwriter.SetValue(cTrakt, cMaxAnticipatedMoviesRequest, MaxAnticipatedMoviesRequest);
                xmlwriter.SetValue(cTrakt, cMaxAnticipatedShowsRequest, MaxAnticipatedShowsRequest);
                xmlwriter.SetValueAsBool(cTrakt, cUseSSL, UseSSL);
                xmlwriter.SetValue(cTrakt, cLastListActivities, LastListActivities.ToJSON());
                xmlwriter.SetValue(cTrakt, cMaxRelatedMoviesRequest, MaxRelatedMoviesRequest);
                xmlwriter.SetValue(cTrakt, cMaxRelatedMoviesUnWatchedRequest, MaxRelatedMoviesUnWatchedRequest);
                xmlwriter.SetValue(cTrakt, cMaxRelatedShowsRequest, MaxRelatedShowsRequest);
                xmlwriter.SetValue(cTrakt, cMaxRelatedShowsUnWatchedRequest, MaxRelatedShowsUnWatchedRequest);
                xmlwriter.SetValue(cTrakt, cMaxUserWatchedEpisodesRequest, MaxUserWatchedEpisodesRequest);
                xmlwriter.SetValue(cTrakt, cMaxUserWatchedMoviesRequest, MaxUserWatchedMoviesRequest);
                xmlwriter.SetValue(cTrakt, cMaxUserCommentsRequest, MaxUserCommentsRequest);
                xmlwriter.SetValue(cTrakt, cDashboardActivityFilter, DashboardActivityFilter.ToJSON());
                xmlwriter.SetValueAsBool(cTrakt, cSkipMoviesWithNoIdsOnSync, SkipMoviesWithNoIdsOnSync);
                xmlwriter.SetValue(cTrakt, cPersonMovieCreditsDefaultLayout, PersonMovieCreditsDefaultLayout);
                xmlwriter.SetValue(cTrakt, cPersonShowCreditsDefaultLayout, PersonShowCreditsDefaultLayout);
                xmlwriter.SetValue(cTrakt, cSortByCreditMovies, SortByCreditMovies.ToJSON());
                xmlwriter.SetValue(cTrakt, cSortByCreditShows, SortByCreditShows.ToJSON());
                xmlwriter.SetValueAsBool(cTrakt, cCreditMoviesHideWatched, CreditMoviesHideWatched);
                xmlwriter.SetValueAsBool(cTrakt, cCreditMoviesHideWatchlisted, CreditMoviesHideWatchlisted);
                xmlwriter.SetValueAsBool(cTrakt, cCreditMoviesHideCollected, CreditMoviesHideCollected);
                xmlwriter.SetValueAsBool(cTrakt, cCreditMoviesHideRated, CreditMoviesHideRated);
                xmlwriter.SetValueAsBool(cTrakt, cCreditShowsHideWatched, CreditShowsHideWatched);
                xmlwriter.SetValueAsBool(cTrakt, cCreditShowsHideWatchlisted, CreditShowsHideWatchlisted);
                xmlwriter.SetValueAsBool(cTrakt, cCreditShowsHideCollected, CreditShowsHideCollected);
                xmlwriter.SetValueAsBool(cTrakt, cCreditShowsHideRated, CreditShowsHideRated);
                xmlwriter.SetValue(cTrakt, cCreditsMovieDefaultLayout, CreditsMovieDefaultLayout);
                xmlwriter.SetValue(cTrakt, cCreditsShowDefaultLayout, CreditsShowDefaultLayout);
                xmlwriter.SetValue(cTrakt, cAnticipatedMoviesDefaultLayout, AnticipatedMoviesDefaultLayout);
                xmlwriter.SetValue(cTrakt, cAnticipatedShowsDefaultLayout, AnticipatedShowsDefaultLayout);
                xmlwriter.SetValueAsBool(cTrakt, cParentalControlsEnabled, ParentalControlsEnabled);
                xmlwriter.SetValue(cTrakt, cParentalControlsPinCode, ParentalControlsPinCode);
                xmlwriter.SetValueAsBool(cTrakt, cParentalIgnoreAfterEnabled, ParentalIgnoreAfterEnabled);
                xmlwriter.SetValue(cTrakt, cParentalIgnoreAfterTime, ParentalIgnoreAfterTime);
                xmlwriter.SetValueAsBool(cTrakt, cParentalIgnoreMovieRatingEnabled, ParentalIgnoreMovieRatingEnabled);
                xmlwriter.SetValueAsBool(cTrakt, cParentalIgnoreShowRatingEnabled, ParentalIgnoreShowRatingEnabled);
                xmlwriter.SetValue(cTrakt, cParentalIgnoreMovieRating, ParentalIgnoreMovieRating);
                xmlwriter.SetValue(cTrakt, cParentalIgnoreShowRating, ParentalIgnoreShowRating);
                xmlwriter.SetValue(cTrakt, cTmdbConfiguration, TmdbConfiguration.ToJSON());
                xmlwriter.SetValue(cTrakt, cTmdbConfigurationAge, TmdbConfigurationAge);
                xmlwriter.SetValue(cTrakt, cTmdbPreferredBackdropSize, TmdbPreferredBackdropSize);
                xmlwriter.SetValue(cTrakt, cTmdbPreferredPosterSize, TmdbPreferredPosterSize);
                xmlwriter.SetValue(cTrakt, cTmdbPreferredEpisodeThumbSize, TmdbPreferredEpisodeThumbSize);
                xmlwriter.SetValue(cTrakt, cTmdbPreferredHeadshotSize, TmdbPreferredHeadshotSize);
                xmlwriter.SetValue(cTrakt, cTmdbShowImageMaxCacheAge, TmdbShowImageMaxCacheAge);
                xmlwriter.SetValue(cTrakt, cTmdbMovieImageMaxCacheAge, TmdbMovieImageMaxCacheAge);
                xmlwriter.SetValue(cTrakt, cTmdbSeasonImageMaxCacheAge, TmdbSeasonImageMaxCacheAge);
                xmlwriter.SetValue(cTrakt, cTmdbEpisodeImageMaxCacheAge, TmdbEpisodeImageMaxCacheAge);
                xmlwriter.SetValue(cTrakt, cTmdbPersonImageMaxCacheAge, TmdbPersonImageMaxCacheAge);
                xmlwriter.SetValue(cTrakt, cTmdbPreferredImageLanguage, TmdbPreferredImageLanguage);
                xmlwriter.SetValue(cTrakt, cTraktOnlineSettings, OnlineSettings);
            }

            Settings.SaveCache();

            if (savePersistedCache)
            {
                TraktLogger.Info("Saving persistent file cache");
                TraktCache.SaveFileCache(cLastActivityFileCache, LastActivityLoad.ToJSON());
                TraktCache.SaveFileCache(cLastStatisticsFileCache, LastStatistics.ToJSON());
                TraktCache.SaveFileCache(cLastUserProfileFileCache, LastUserProfile.ToJSON());
                TraktCache.SaveFileCache(cLastTrendingShowFileCache, (LastTrendingShows ?? "[]".FromJSONArray<TraktShowTrending>()).ToList().ToJSON());
                TraktCache.SaveFileCache(cLastTrendingMovieFileCache, (LastTrendingMovies ?? "[]".FromJSONArray<TraktMovieTrending>()).ToList().ToJSON());

                // save user activity cache
                TraktCache.Save();
            }
        }

        /// <summary>
        /// Modify External Plugin Settings
        /// </summary>
        internal static void UpdateInternalPluginSettings()
        {
            // disable internal plugin rate dialogs if we show trakt dialog
            if (TraktSettings.ShowRateDialogOnWatched)
            {
                if (TraktHelper.IsMovingPicturesAvailableAndEnabled && MovingPictures >= 0)
                    TraktHandlers.MovingPictures.UpdateSettingAsBool("auto_prompt_for_rating", false);

                if (TraktHelper.IsMPTVSeriesAvailableAndEnabled && TVSeries >= 0)
                    TraktHandlers.TVSeries.UpdateSettingAsBool("askToRate", false);
            }
        }

        /// <summary>
        /// Perform any maintenance tasks on the settings
        /// </summary>
        internal static void PerformMaintenance()
        {
            TraktLogger.Info("Performing maintenance tasks");

            using (Settings xmlreader = new MPSettings())
            {
                int currentSettingsVersion = xmlreader.GetValueAsInt(cTrakt, cSettingsVersion, SettingsVersion);

                // check if any maintenance task is required
                if (currentSettingsVersion >= SettingsVersion) return;

                // upgrade settings for each version
                while (currentSettingsVersion < SettingsVersion)
                {
                    switch (currentSettingsVersion)
                    {
                        case 0:
                            xmlreader.RemoveEntry(cTrakt, cLastActivityLoad);
                            xmlreader.RemoveEntry(cTrakt, cLastTrendingMovies);
                            xmlreader.RemoveEntry(cTrakt, cLastTrendingShows);
                            xmlreader.RemoveEntry(cTrakt, cLastStatistics);
                            currentSettingsVersion++;
                            break;

                        case 1:
                            // trailers plugin now supports tvshows, seasons and episodes.
                            xmlreader.SetValueAsBool(cTrakt, "UseTrailersPlugin", true);
                            currentSettingsVersion++;
                            break;

                        case 2:
                            // Only use Trailers plugin now for Trailers functionality.
                            xmlreader.RemoveEntry(cTrakt, "UseTrailersPlugin");
                            xmlreader.RemoveEntry(cTrakt, "DefaultTVShowTrailerSite");
                            xmlreader.RemoveEntry(cTrakt, "DefaultMovieTrailerSite");
         
                            // Remove old activity settings
                            xmlreader.RemoveEntry(cTrakt, "ShowCommunityActivity");
                            xmlreader.RemoveEntry(cTrakt, "IncludeMeInFriendsActivity");

                            // Remove old category/filter node ids for MovingPictures (not needed)
                            xmlreader.RemoveEntry(cTrakt, "MovingPicturesCategoryId");
                            xmlreader.RemoveEntry(cTrakt, "MovingPicturesFilterId");

                            currentSettingsVersion++;
                            break;

                        case 3:
                            // Remove 4TR / My Anime plugin handlers (plugins no longer developed or superceded)
                            xmlreader.RemoveEntry(cTrakt, "ForTheRecordRecordings");
                            xmlreader.RemoveEntry(cTrakt, "ForTheRecordTVLive");
                            xmlreader.RemoveEntry(cTrakt, "MyAnime");

                            // Clear existing passwords as they're no longer hashed in new API v2
                            xmlreader.RemoveEntry(cTrakt, "Password");
                            xmlreader.RemoveEntry(cTrakt, cUserLogins);

                            // Remove Advanced Rating setting, there is only one now
                            xmlreader.RemoveEntry(cTrakt, "ShowAdvancedRatingsDialog");

                            // Remove SkippedMovies and AlreadyExistMovies as data structures changed
                            xmlreader.RemoveEntry(cTrakt, "SkippedMovies");
                            xmlreader.RemoveEntry(cTrakt, "AlreadyExistMovies");

                            // Remove old show collection cache
                            xmlreader.RemoveEntry(cTrakt, "ShowsInCollection");

                            // Reset some defaults
                            xmlreader.RemoveEntry(cTrakt, cSyncRatings);
                            xmlreader.RemoveEntry(cTrakt, cDashboardActivityPollInterval);
                            xmlreader.RemoveEntry(cTrakt, cDashboardTrendingPollInterval);
                            xmlreader.RemoveEntry(cTrakt, cDashboardLoadDelay);
                            xmlreader.RemoveEntry(cTrakt, cShowRateDlgForPlaylists);
                            xmlreader.RemoveEntry(cTrakt, cSearchTypes);

                            // Remove any persisted data that has changed with with new API v2
                            try
                            {
                                if (File.Exists(cLastActivityFileCache)) File.Delete(cLastActivityFileCache);
                                if (File.Exists(cLastTrendingShowFileCache)) File.Delete(cLastTrendingShowFileCache);
                                if (File.Exists(cLastTrendingMovieFileCache)) File.Delete(cLastTrendingMovieFileCache);
                                if (File.Exists(cLastStatisticsFileCache)) File.Delete(cLastStatisticsFileCache);

                                // Remove old artwork - filenames have changed
                                string imagePath = Config.GetFolder(Config.Dir.Thumbs) + "\\Trakt";
                                if (Directory.Exists(imagePath))
                                {
                                    Directory.Delete(imagePath, true);
                                }
                            }
                            catch (Exception e)
                            {
                                TraktLogger.Error("Failed to remove v1 API persisted data from disk, Reason = '{0}'", e.Message);
                            }

                            currentSettingsVersion++;
                            break;

                        case 4:
                            try
                            {
                                // Fix bad upgrade from previous release
                                string dashboardPersistence = Config.GetFolder(Config.Dir.Config) + "\\Trakt\\Dashboard";
                                if (Directory.Exists(dashboardPersistence))
                                {
                                    Directory.Delete(dashboardPersistence, true);
                                }
                            }
                            catch (Exception e)
                            {
                                TraktLogger.Error("Failed to remove v1 API persisted data from disk, Reason = '{0}'", e.Message);
                            }
                            currentSettingsVersion++;
                            break;

                        case 5:
                            // Clear existing passwords, change of encryption/decryption technique
                            xmlreader.RemoveEntry(cTrakt, "Password");
                            xmlreader.RemoveEntry(cTrakt, cUserLogins);
                            currentSettingsVersion++;
                            break;

                        case 6:
                            // Save Sync Interval in Hours from Milliseconds
                            int syncTimerLength = xmlreader.GetValueAsInt(cTrakt, cSyncTimerLength, 24);
                            if (syncTimerLength > 24)
                            {
                                // requires upgrade
                                xmlreader.SetValue(cTrakt, cSyncTimerLength, syncTimerLength / 3600000);
                            }
                            currentSettingsVersion++;
                            break;

                        case 7:
                            // upgrade last activity view
                            xmlreader.RemoveEntry(cTrakt, cActivityStreamView);
                            
                            // remove last paused item processed - stored in last activities
                            xmlreader.RemoveEntry(cTrakt, "LastPausedItemProcessed");

                            currentSettingsVersion++;
                            break;

                        case 8:
                            // cleanup cached likes, API changed to include a user object for lists
                            // i.e. the user that owns the list
                            try
                            {
                                var folderName = Path.Combine(Config.GetFolder(Config.Dir.Config), @"Trakt");

                                var matches = Directory.GetFiles(folderName, "Liked.json", SearchOption.AllDirectories);
                                foreach (string file in matches)
                                {
                                    File.Delete(file);
                                }
                            }
                            catch (Exception e)
                            {
                                TraktLogger.Error("Failed to remove previously cached likes from disk, Reason = '{0}'", e.Message);
                            }
                            currentSettingsVersion++;
                            break;

                        case 9:
                            // remove old thumbs folder
                            try
                            {
                                DirectoryInfo di = new DirectoryInfo(Path.Combine(Config.GetFolder(Config.Dir.Thumbs), @"Trakt"));

                                foreach (FileInfo file in di.GetFiles())
                                {
                                    file.Delete();
                                }
                                foreach (DirectoryInfo dir in di.GetDirectories())
                                {
                                    dir.Delete(true);
                                }
                            }
                            catch (Exception e)
                            {
                                TraktLogger.Error("Failed to remove previously cached thumbs from disk, Reason = '{0}'", e.Message);
                            }

                            // update default sizes for requests so we dont hit any limits from TMDb by default
                            xmlreader.SetValue(cTrakt, cMaxAnticipatedMoviesRequest, 40);
                            xmlreader.SetValue(cTrakt, cMaxAnticipatedShowsRequest, 40);
                            xmlreader.SetValue(cTrakt, cMaxPopularMoviesRequest, 40);
                            xmlreader.SetValue(cTrakt, cMaxPopularShowsRequest, 40);
                            xmlreader.SetValue(cTrakt, cMaxRelatedMoviesUnWatchedRequest, 40);
                            xmlreader.SetValue(cTrakt, cMaxRelatedShowsUnWatchedRequest, 40);
                            xmlreader.SetValue(cTrakt, cMaxTrendingMoviesRequest, 40);
                            xmlreader.SetValue(cTrakt, cMaxTrendingShowsRequest, 40);
                            xmlreader.SetValue(cTrakt, cMaxUserWatchedEpisodesRequest, 40);
                            xmlreader.SetValue(cTrakt, cMaxUserWatchedMoviesRequest, 40);
                            xmlreader.SetValue(cTrakt, cMaxUserCommentsRequest, 40);

                            currentSettingsVersion++;
                            break;
                        case 10:
                            // remove old authentication info
                            xmlreader.RemoveEntry(cTrakt, cUsername);
                            xmlreader.RemoveEntry(cTrakt, cUserLogins);

                            // keep password for mp-tvseries check when downloading community rating
                            // future version will only check for user access token
                            //xmlreader.RemoveEntry(cTrakt, "Password");

                            currentSettingsVersion++;
                            break;
                    }
                }
            }
            Settings.SaveCache();
        }

        #endregion

        public class LanguageISO
        {
            public string DisplayName { get; set; }
            public string TwoLetterCode { get; set; }

            public override string ToString()
            {
                return string.Format("{0} ({1})", this.DisplayName, this.TwoLetterCode);
            }
        }
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
                Name = "Settings",
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
                TraktSettings.LoadSettings(false);

                // determine when next library sync is due
                var nextSyncDate = TraktPlugin.SyncStartTime.Add(TimeSpan.FromHours(TraktSettings.SyncTimerLength));

                // determine how long to wait from 'now' to start the next sync, set the start delay if it is 'now'
                int startDelay = nextSyncDate <= DateTime.Now ? TraktSettings.SyncStartDelay : (int)(nextSyncDate.Subtract(DateTime.Now).TotalMilliseconds);

                // re-initialize sync Interval
                TraktPlugin.ChangeSyncTimer(startDelay, TraktSettings.SyncTimerLength * 3600000);

                // update any internal plugin settings required
                TraktSettings.UpdateInternalPluginSettings();
            }
        }
        #endregion
    }
}
