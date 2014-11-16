using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TraktPlugin.TraktAPI
{
    /// <summary>
    /// List of URIs for the Trakt API
    /// </summary>
    public static class TraktURIs
    {
        #region Authentication

        public const string Login = "http://api.v2.trakt.tv/auth/login";

        #endregion

        #region User

        public const string UserFollowerRequests = "http://api.v2.trakt.tv/user/requests";

        #endregion

        #region Sync

        // GET
        public const string SyncLastActivities = "http://api.v2.trakt.tv/sync/last_activities";
        
        public const string SyncCollectionMovies = "http://api.v2.trakt.tv/sync/collection/movies";
        public const string SyncWatchedMovies = "http://api.v2.trakt.tv/sync/watched/movies";
        public const string SyncRatedMovies = "http://api.v2.trakt.tv/sync/ratings/movies";

        public const string SyncCollectionEpisodes = "http://api.v2.trakt.tv/sync/collection/shows";
        public const string SyncWatchedEpisodes = "http://api.v2.trakt.tv/sync/watched/shows";
        public const string SyncRatedEpisodes = "http://api.v2.trakt.tv/sync/ratings/episodes";
        public const string SyncRatedShows = "http://api.v2.trakt.tv/sync/ratings/shows";

        public const string UserLists = "http://api.v2.trakt.tv/users/{0}/lists";
        public const string UserListItems = "http://api.v2.trakt.tv/users/{0}/lists/{1}/items";

        public const string UserWatchlistMovies = "http://api.v2.trakt.tv/users/{0}/watchlist/movies";
        public const string UserWatchlistShows = "http://api.v2.trakt.tv/users/{0}/watchlist/shows";
        public const string UserWatchlistEpisodes = "http://api.v2.trakt.tv/users/{0}/watchlist/episodes";

        public const string RecommendedMovies = "http://api.v2.trakt.tv/recommendations/movies";
        public const string RecommendedShows = "http://api.v2.trakt.tv/recommendations/shows";

        // POST
        public const string SyncCollectionAdd = "http://api.v2.trakt.tv/sync/collection";
        public const string SyncCollectionRemove = "http://api.v2.trakt.tv/sync/collection/remove";
        public const string SyncWatchedHistoryAdd = "http://api.v2.trakt.tv/sync/history";
        public const string SyncWatchedHistoryRemove = "http://api.v2.trakt.tv/sync/history/remove";
        public const string SyncRatingsAdd = "http://api.v2.trakt.tv/sync/ratings";
        public const string SyncRatingsRemove = "http://api.v2.trakt.tv/sync/ratings/remove";
        public const string SyncWatchlistAdd = "http://api.v2.trakt.tv/sync/watchlist";
        public const string SyncWatchlistRemove = "http://api.v2.trakt.tv/sync/watchlist/remove";

        public const string ScrobbleStart = "http://api.v2.trakt.tv/scrobble/start";
        public const string ScrobblePause = "http://api.v2.trakt.tv/scrobble/pause";
        public const string ScrobbleStop = "http://api.v2.trakt.tv/scrobble/stop";

        // DELETE
        public const string DismissRecommendedMovie = "http://api.v2.trakt.tv/recommendations/movies/{0}";
        public const string DismissRecommendedShow = "http://api.v2.trakt.tv/recommendations/shows/{0}";

        public const string DeleteList = "http://api.v2.trakt.tv/users/{0}/lists/{1}";

        #endregion
    }
}
