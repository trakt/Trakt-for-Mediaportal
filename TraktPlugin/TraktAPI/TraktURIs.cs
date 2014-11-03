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

        #endregion
    }
}
