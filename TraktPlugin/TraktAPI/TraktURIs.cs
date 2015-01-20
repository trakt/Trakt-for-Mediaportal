
namespace TraktPlugin.TraktAPI
{
    /// <summary>
    /// List of URIs for the Trakt API
    /// </summary>
    public static class TraktURIs
    {
        public const string Login = "http://api.trakt.tv/auth/login";

        // GET
        public const string SyncLastActivities = "http://api.trakt.tv/sync/last_activities";

        public const string SyncPlayback = "http://api.trakt.tv/sync/playback";

        public const string SyncCollectionMovies = "http://api.trakt.tv/sync/collection/movies";
        public const string SyncWatchedMovies = "http://api.trakt.tv/sync/watched/movies";
        public const string SyncRatedMovies = "http://api.trakt.tv/sync/ratings/movies";

        public const string SyncCollectionEpisodes = "http://api.trakt.tv/sync/collection/shows";
        public const string SyncWatchedEpisodes = "http://api.trakt.tv/sync/watched/shows";
        public const string SyncRatedEpisodes = "http://api.trakt.tv/sync/ratings/episodes";
        public const string SyncRatedShows = "http://api.trakt.tv/sync/ratings/shows";

        public const string UserLists = "http://api.trakt.tv/users/{0}/lists";
        public const string UserListItems = "http://api.trakt.tv/users/{0}/lists/{1}/items?extended={2}";
        
        public const string UserListAdd = "http://api.trakt.tv/users/{0}/lists";
        public const string UserListEdit = "http://api.trakt.tv/users/{0}/lists/{1}";

        public const string UserListItemsAdd = "http://api.trakt.tv/users/{0}/lists/{1}/items";
        public const string UserListItemsRemove = "http://api.trakt.tv/users/{0}/lists/{1}/items/remove";

        public const string UserWatchlistMovies = "http://api.trakt.tv/users/{0}/watchlist/movies?extended={1}";
        public const string UserWatchlistShows = "http://api.trakt.tv/users/{0}/watchlist/shows?extended=full,images";
        public const string UserWatchlistEpisodes = "http://api.trakt.tv/users/{0}/watchlist/episodes?extended=full,images";

        public const string UserProfile = "http://api.trakt.tv/users/{0}?extended=full,images";
        public const string UserFollowerRequests = "http://api.trakt.tv/users/requests?extended=full,images";
        public const string UserStats = "http://api.trakt.tv/users/{0}/stats";

        public const string RecommendedMovies = "http://api.trakt.tv/recommendations/movies?extended={0}";
        public const string RecommendedShows = "http://api.trakt.tv/recommendations/shows?extended=full,images";

        public const string RelatedMovies = "http://api.trakt.tv/movies/{0}/related?extended=full,images";
        public const string RelatedShows = "http://api.trakt.tv/shows/{0}/related?extended=full,images";

        public const string TrendingMovies = "http://api.trakt.tv/movies/trending?extended=full,images&page={0}&limit={1}";
        public const string TrendingShows = "http://api.trakt.tv/shows/trending?extended=full,images&page={0}&limit={1}";

        public const string MovieComments = "http://api.trakt.tv/movies/{0}/comments?extended=full,images&page={1}&limit={2}";
        public const string ShowComments = "http://api.trakt.tv/shows/{0}/comments?extended=full,images&page={1}&limit={2}";
        public const string EpisodeComments = "http://api.trakt.tv/shows/{0}/seasons/{1}/episodes/{2}/comments?extended=full,images&page={3}&limit={4}";

        public const string SearchMovies = "http://api.trakt.tv/search?query={0}&type=movie&page={1}&limit={2}?extended=full,images";
        public const string SearchShows = "http://api.trakt.tv/search?query={0}&type=show&page={1}&limit={2}?extended=full,images";
        public const string SearchEpisodes = "http://api.trakt.tv/search?query={0}&type=episode&page={1}&limit={2}?extended=full,images";
        public const string SearchPeople = "http://api.trakt.tv/search?query={0}&type=person&page={1}&limit={2}?extended=full,images";
        public const string SearchUsers = "http://api.trakt.tv/search?query={0}&type=user&page={1}&limit={2}?extended=full,images"; // not implemented!
        public const string SearchLists = "http://api.trakt.tv/search?query={0}&type=list&page={1}&limit={2}?extended=full,images";

        public const string NetworkFriends = "http://api.trakt.tv/users/{0}/friends?extended=full,images";
        public const string NetworkFollowers = "http://api.trakt.tv/users/{0}/followers?extended=full,images";
        public const string NetworkFollowing = "http://api.trakt.tv/users/{0}/following?extended=full,images";

        public const string NetworkFollowRequest = "http://api.trakt.tv/users/requests/{0}";
        public const string NetworkFollowUser = "http://api.trakt.tv/users/{0}/follow";

        public const string ShowSummary = "http://api.trakt.tv/shows/{0}?extended=full,images";
        public const string MovieSummary = "http://api.trakt.tv/movies/{0}?extended=full,images";
        public const string EpisodeSummary = "http://api.trakt.tv/shows/{0}/seasons/{1}/episodes/{2}?extended=full,images";
        public const string PersonSummary = "http://api.trakt.tv/people/{0}?extended=full,images";

        public const string ShowSeasons = "http://api.trakt.tv/shows/{0}/seasons?extended=full,images";
        public const string SeasonEpisodes = "http://api.trakt.tv/shows/{0}/seasons/{1}?extended=full,images";

        public const string CalendarShows = "http://api.trakt.tv/calendars/shows/{0}/{1}?extended=full,images";
        public const string CalendarPremieres = "http://api.trakt.tv/calendars/shows/premieres/{0}/{1}?extended=full,images";
        public const string CalendarNewPremieres = "http://api.trakt.tv/calendars/shows/new/{0}/{1}?extended=full,images";

        // POST
        public const string SyncCollectionAdd = "http://api.trakt.tv/sync/collection";
        public const string SyncCollectionRemove = "http://api.trakt.tv/sync/collection/remove";
        public const string SyncWatchedHistoryAdd = "http://api.trakt.tv/sync/history";
        public const string SyncWatchedHistoryRemove = "http://api.trakt.tv/sync/history/remove";
        public const string SyncRatingsAdd = "http://api.trakt.tv/sync/ratings";
        public const string SyncRatingsRemove = "http://api.trakt.tv/sync/ratings/remove";
        public const string SyncWatchlistAdd = "http://api.trakt.tv/sync/watchlist";
        public const string SyncWatchlistRemove = "http://api.trakt.tv/sync/watchlist/remove";

        public const string ScrobbleStart = "http://api.trakt.tv/scrobble/start";
        public const string ScrobblePause = "http://api.trakt.tv/scrobble/pause";
        public const string ScrobbleStop = "http://api.trakt.tv/scrobble/stop";

        // DELETE
        public const string DismissRecommendedMovie = "http://api.trakt.tv/recommendations/movies/{0}";
        public const string DismissRecommendedShow = "http://api.trakt.tv/recommendations/shows/{0}";

        public const string DeleteList = "http://api.trakt.tv/users/{0}/lists/{1}";

    }
}
