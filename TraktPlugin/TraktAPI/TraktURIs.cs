
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

        public const string UserFollowerRequests = "http://api.v2.trakt.tv/user/requests?extended=full,images";

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

        public const string UserLists = "http://api.v2.trakt.tv/users/{0}/lists?extended={0}";
        public const string UserListItems = "http://api.v2.trakt.tv/users/{0}/lists/{1}/items?extended={0}";
        
        public const string UserListAdd = "http://api.v2.trakt.tv/users/{0}/lists";
        public const string UserListEdit = "http://api.v2.trakt.tv/users/{0}/lists/{1}";

        public const string UserListItemsAdd = "http://api.v2.trakt.tv/users/{0}/lists/{1}/items";
        public const string UserListItemsDelete = "http://api.v2.trakt.tv/users/{0}/lists/{1}/remove";

        public const string UserWatchlistMovies = "http://api.v2.trakt.tv/users/{0}/watchlist/movies?extended={0}";
        public const string UserWatchlistShows = "http://api.v2.trakt.tv/users/{0}/watchlist/shows?extended=full,images";
        public const string UserWatchlistEpisodes = "http://api.v2.trakt.tv/users/{0}/watchlist/episodes?extended=full,images";

        public const string UserProfile = "http://api.v2.trakt.tv/users/{0}?extended={0}";

        public const string RecommendedMovies = "http://api.v2.trakt.tv/recommendations/movies?extended={0}";
        public const string RecommendedShows = "http://api.v2.trakt.tv/recommendations/shows?extended=full,images";

        public const string RelatedMovies = "http://api.v2.trakt.tv/movies/{0}/related?extended=full,images";
        public const string RelatedShows = "http://api.v2.trakt.tv/shows/{0}/related?extended=full,images";

        public const string TrendingMovies = "http://api.v2.trakt.tv/movies/trending?extended=full,images&page={0}&limit={1}";
        public const string TrendingShows = "http://api.v2.trakt.tv/shows/trending?extended=full,images&page={0}&limit={1}";

        public const string MovieComments = "http://api.v2.trakt.tv/movies/{0}/comments?extended=full,images";
        public const string ShowComments = "http://api.v2.trakt.tv/shows/{0}/comments?extended=full,images";
        public const string EpisodeComments = "http://api.v2.trakt.tv/shows/{0}/seasons/{1}/episodes/{2}/comments?extended=full,images";

        public const string SearchMovies = "http://api.v2.trakt.tv/search?query={0}&type=movie&page={1}&limit={2}?extended=full,images";
        public const string SearchShows = "http://api.v2.trakt.tv/search?query={0}&type=show&page={1}&limit={2}?extended=full,images";
        public const string SearchEpisodes = "http://api.v2.trakt.tv/search?query={0}&type=episode&page={1}&limit={2}?extended=full,images";
        public const string SearchPeople = "http://api.v2.trakt.tv/search?query={0}&type=person&page={1}&limit={2}?extended=full,images";
        public const string SearchUsers = "http://api.v2.trakt.tv/search?query={0}&type=user&page={1}&limit={2}?extended=full,images"; // not implemented!
        public const string SearchLists = "http://api.v2.trakt.tv/search?query={0}&type=list&page={1}&limit={2}?extended=full,images";
        
        public const string NetworkFriends = "http://api.v2.trakt.tv/users/{0}/friends?extended=full,images";
        public const string NetworkFollowers = "http://api.v2.trakt.tv/users/{0}/followers?extended=full,images";
        public const string NetworkFollowing = "http://api.v2.trakt.tv/users/{0}/following?extended=full,images";

        public const string NetworkFollowRequest = "http://api.v2.trakt.tv/users/requests/{0}";
        public const string NetworkFollowUser = "http://api.v2.trakt.tv/users/{0}/follow";

        public const string ShowSeasons = "http://api.v2.trakt.tv/shows/{0}/seasons?extended=full,images";
        public const string SeasonEpisodes = "http://api.v2.trakt.tv/shows/{0}/seasons/{1}?extended=full,images";

        public const string CalendarShows = "http://api.v2.trakt.tv/calendars/shows/{0}/{1}?extended=full,images";
        public const string CalendarPremieres = "http://api.v2.trakt.tv/calendars/shows/premieres/{0}/{1}?extended=full,images";
        public const string CalendarNewPremieres = "http://api.v2.trakt.tv/calendars/shows/new/{0}/{1}?extended=full,images";

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
