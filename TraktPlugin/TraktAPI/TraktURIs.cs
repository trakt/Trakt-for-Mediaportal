
namespace TraktPlugin.TraktAPI
{
    /// <summary>
    /// List of URIs for the Trakt API
    /// </summary>
    public static class TraktURIs
    {
        public const string Login = "https://api-v2launch.trakt.tv/auth/login";

        public const string SyncLastActivities = "https://api-v2launch.trakt.tv/sync/last_activities";

        public const string SyncPlayback = "https://api-v2launch.trakt.tv/sync/playback";

        public const string SyncCollectionMovies = "https://api-v2launch.trakt.tv/sync/collection/movies";
        public const string SyncWatchedMovies = "https://api-v2launch.trakt.tv/sync/watched/movies";
        public const string SyncRatedMovies = "https://api-v2launch.trakt.tv/sync/ratings/movies";

        public const string SyncCollectionEpisodes = "https://api-v2launch.trakt.tv/sync/collection/shows";
        public const string SyncWatchedEpisodes = "https://api-v2launch.trakt.tv/sync/watched/shows";
        public const string SyncRatedEpisodes = "https://api-v2launch.trakt.tv/sync/ratings/episodes";
        public const string SyncRatedShows = "https://api-v2launch.trakt.tv/sync/ratings/shows";

        public const string SyncCollectionAdd = "https://api-v2launch.trakt.tv/sync/collection";
        public const string SyncCollectionRemove = "https://api-v2launch.trakt.tv/sync/collection/remove";
        public const string SyncWatchedHistoryAdd = "https://api-v2launch.trakt.tv/sync/history";
        public const string SyncWatchedHistoryRemove = "https://api-v2launch.trakt.tv/sync/history/remove";
        public const string SyncRatingsAdd = "https://api-v2launch.trakt.tv/sync/ratings";
        public const string SyncRatingsRemove = "https://api-v2launch.trakt.tv/sync/ratings/remove";
        public const string SyncWatchlistAdd = "https://api-v2launch.trakt.tv/sync/watchlist";
        public const string SyncWatchlistRemove = "https://api-v2launch.trakt.tv/sync/watchlist/remove";

        public const string UserLists = "https://api-v2launch.trakt.tv/users/{0}/lists";
        public const string UserListItems = "https://api-v2launch.trakt.tv/users/{0}/lists/{1}/items?extended={2}";
        
        public const string UserListAdd = "https://api-v2launch.trakt.tv/users/{0}/lists";
        public const string UserListEdit = "https://api-v2launch.trakt.tv/users/{0}/lists/{1}";

        public const string UserListItemsAdd = "https://api-v2launch.trakt.tv/users/{0}/lists/{1}/items";
        public const string UserListItemsRemove = "https://api-v2launch.trakt.tv/users/{0}/lists/{1}/items/remove";

        public const string UserWatchlistMovies = "https://api-v2launch.trakt.tv/users/{0}/watchlist/movies?extended={1}";
        public const string UserWatchlistShows = "https://api-v2launch.trakt.tv/users/{0}/watchlist/shows?extended=full,images";
        public const string UserWatchlistEpisodes = "https://api-v2launch.trakt.tv/users/{0}/watchlist/episodes?extended=full,images";

        public const string UserProfile = "https://api-v2launch.trakt.tv/users/{0}?extended=full,images";
        public const string UserFollowerRequests = "https://api-v2launch.trakt.tv/users/requests?extended=full,images";
        public const string UserStats = "https://api-v2launch.trakt.tv/users/{0}/stats";

        public const string RecommendedMovies = "https://api-v2launch.trakt.tv/recommendations/movies?extended={0}";
        public const string RecommendedShows = "https://api-v2launch.trakt.tv/recommendations/shows?extended=full,images";

        public const string RelatedMovies = "https://api-v2launch.trakt.tv/movies/{0}/related?extended=full,images";
        public const string RelatedShows = "https://api-v2launch.trakt.tv/shows/{0}/related?extended=full,images";

        public const string TrendingMovies = "https://api-v2launch.trakt.tv/movies/trending?extended=full,images&page={0}&limit={1}";
        public const string TrendingShows = "https://api-v2launch.trakt.tv/shows/trending?extended=full,images&page={0}&limit={1}";

        public const string MovieComments = "https://api-v2launch.trakt.tv/movies/{0}/comments?extended=full,images&page={1}&limit={2}";
        public const string ShowComments = "https://api-v2launch.trakt.tv/shows/{0}/comments?extended=full,images&page={1}&limit={2}";
        public const string EpisodeComments = "https://api-v2launch.trakt.tv/shows/{0}/seasons/{1}/episodes/{2}/comments?extended=full,images&page={3}&limit={4}";

        public const string CommentLike = "https://api-v2launch.trakt.tv/comments/{0}/like";
        public const string CommentReplies = "https://api-v2launch.trakt.tv/comments/{0}/replies";

        public const string SearchMovies = "https://api-v2launch.trakt.tv/search?query={0}&type=movie&page={1}&limit={2}?extended=full,images";
        public const string SearchShows = "https://api-v2launch.trakt.tv/search?query={0}&type=show&page={1}&limit={2}?extended=full,images";
        public const string SearchEpisodes = "https://api-v2launch.trakt.tv/search?query={0}&type=episode&page={1}&limit={2}?extended=full,images";
        public const string SearchPeople = "https://api-v2launch.trakt.tv/search?query={0}&type=person&page={1}&limit={2}?extended=full,images";
        public const string SearchUsers = "https://api-v2launch.trakt.tv/search?query={0}&type=user&page={1}&limit={2}?extended=full,images"; // not implemented!
        public const string SearchLists = "https://api-v2launch.trakt.tv/search?query={0}&type=list&page={1}&limit={2}?extended=full,images";

        public const string NetworkFriends = "https://api-v2launch.trakt.tv/users/{0}/friends?extended=full,images";
        public const string NetworkFollowers = "https://api-v2launch.trakt.tv/users/{0}/followers?extended=full,images";
        public const string NetworkFollowing = "https://api-v2launch.trakt.tv/users/{0}/following?extended=full,images";

        public const string NetworkFollowRequest = "https://api-v2launch.trakt.tv/users/requests/{0}";
        public const string NetworkFollowUser = "https://api-v2launch.trakt.tv/users/{0}/follow";

        public const string ShowSummary = "https://api-v2launch.trakt.tv/shows/{0}?extended=full,images";
        public const string MovieSummary = "https://api-v2launch.trakt.tv/movies/{0}?extended=full,images";
        public const string EpisodeSummary = "https://api-v2launch.trakt.tv/shows/{0}/seasons/{1}/episodes/{2}?extended=full,images";
        public const string PersonSummary = "https://api-v2launch.trakt.tv/people/{0}?extended=full,images";

        public const string ShowSeasons = "https://api-v2launch.trakt.tv/shows/{0}/seasons?extended=full,images";
        public const string SeasonEpisodes = "https://api-v2launch.trakt.tv/shows/{0}/seasons/{1}?extended=full,images";

        public const string CalendarShows = "https://api-v2launch.trakt.tv/calendars/shows/{0}/{1}?extended=full,images";
        public const string CalendarPremieres = "https://api-v2launch.trakt.tv/calendars/shows/premieres/{0}/{1}?extended=full,images";
        public const string CalendarNewPremieres = "https://api-v2launch.trakt.tv/calendars/shows/new/{0}/{1}?extended=full,images";

        public const string ScrobbleStart = "https://api-v2launch.trakt.tv/scrobble/start";
        public const string ScrobblePause = "https://api-v2launch.trakt.tv/scrobble/pause";
        public const string ScrobbleStop = "https://api-v2launch.trakt.tv/scrobble/stop";

        public const string DismissRecommendedMovie = "https://api-v2launch.trakt.tv/recommendations/movies/{0}";
        public const string DismissRecommendedShow = "https://api-v2launch.trakt.tv/recommendations/shows/{0}";

        public const string DeleteList = "https://api-v2launch.trakt.tv/users/{0}/lists/{1}";

    }
}
