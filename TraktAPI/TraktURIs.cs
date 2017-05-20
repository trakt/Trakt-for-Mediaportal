
namespace TraktAPI
{
    /// <summary>
    /// List of URIs for the Trakt API
    /// Staging:    https://api.staging.trakt.tv
    /// Production: https://api.trakt.tv
    /// </summary>
    public static class TraktURIs
    {
        public const string Login = "https://api.trakt.tv/auth/login";

        public const string SyncLastActivities = "https://api.trakt.tv/sync/last_activities";

        public const string SyncPausedMovies = "https://api.trakt.tv/sync/playback/movies";
        public const string SyncPausedEpisodes = "https://api.trakt.tv/sync/playback/episodes";

        public const string SyncCollectionMovies = "https://api.trakt.tv/sync/collection/movies";
        public const string SyncWatchedMovies = "https://api.trakt.tv/sync/watched/movies";
        public const string SyncRatedMovies = "https://api.trakt.tv/sync/ratings/movies";

        public const string SyncCollectionEpisodes = "https://api.trakt.tv/sync/collection/shows";
        public const string SyncWatchedEpisodes = "https://api.trakt.tv/sync/watched/shows";
        public const string SyncRatedEpisodes = "https://api.trakt.tv/sync/ratings/episodes";
        public const string SyncRatedSeasons = "https://api.trakt.tv/sync/ratings/seasons";
        public const string SyncRatedShows = "https://api.trakt.tv/sync/ratings/shows";

        public const string SyncCollectionAdd = "https://api.trakt.tv/sync/collection";
        public const string SyncCollectionRemove = "https://api.trakt.tv/sync/collection/remove";
        public const string SyncWatchedHistoryAdd = "https://api.trakt.tv/sync/history";
        public const string SyncWatchedHistoryRemove = "https://api.trakt.tv/sync/history/remove";
        public const string SyncRatingsAdd = "https://api.trakt.tv/sync/ratings";
        public const string SyncRatingsRemove = "https://api.trakt.tv/sync/ratings/remove";
        public const string SyncWatchlistAdd = "https://api.trakt.tv/sync/watchlist";
        public const string SyncWatchlistRemove = "https://api.trakt.tv/sync/watchlist/remove";

        public const string UserLists = "https://api.trakt.tv/users/{0}/lists";
        public const string UserListItems = "https://api.trakt.tv/users/{0}/lists/{1}/items?extended={2}";
        
        public const string UserListAdd = "https://api.trakt.tv/users/{0}/lists";
        public const string UserListEdit = "https://api.trakt.tv/users/{0}/lists/{1}";

        public const string UserListItemsAdd = "https://api.trakt.tv/users/{0}/lists/{1}/items";
        public const string UserListItemsRemove = "https://api.trakt.tv/users/{0}/lists/{1}/items/remove";

        public const string UserListLike = "https://api.trakt.tv/users/{0}/lists/{1}/like";

        public const string UserWatchlistMovies = "https://api.trakt.tv/users/{0}/watchlist/movies?extended={1}";
        public const string UserWatchlistShows = "https://api.trakt.tv/users/{0}/watchlist/shows?extended={1}";
        public const string UserWatchlistSeasons = "https://api.trakt.tv/users/{0}/watchlist/seasons?extended={1}";
        public const string UserWatchlistEpisodes = "https://api.trakt.tv/users/{0}/watchlist/episodes?extended={1}";

        public const string UserProfile = "https://api.trakt.tv/users/{0}?extended=full,images";
        public const string UserFollowerRequests = "https://api.trakt.tv/users/requests?extended=full,images";
        public const string UserStats = "https://api.trakt.tv/users/{0}/stats";

        public const string UserWatchedHistoryMovies = "https://api.trakt.tv/users/{0}/history/movies?extended=full&page={1}&limit={2}";
        public const string UserWatchedHistoryEpisodes = "https://api.trakt.tv/users/{0}/history/episodes?extended=full&page={1}&limit={2}";
        
        public const string UserCollectedMovies = "https://api.trakt.tv/users/{0}/collection/movies?extended=full";
        public const string UserCollectedShows = "https://api.trakt.tv/users/{0}/collection/shows?extended=full";

        public const string UserComments = "https://api.trakt.tv/users/{0}/comments/{1}/{2}?extended={3}&page={4}&limit={5}";

        public const string UserLikedItems = "https://api.trakt.tv/users/likes/{0}?extended={1}&page={2}&limit={3}";

        public const string RecommendedMovies = "https://api.trakt.tv/recommendations/movies?extended={0}";
        public const string RecommendedShows = "https://api.trakt.tv/recommendations/shows?extended=full";

        public const string RelatedMovies = "https://api.trakt.tv/movies/{0}/related?extended=full&limit={1}";
        public const string RelatedShows = "https://api.trakt.tv/shows/{0}/related?extended=full&limit={1}";

        public const string TrendingMovies = "https://api.trakt.tv/movies/trending?extended=full&page={0}&limit={1}";
        public const string TrendingShows = "https://api.trakt.tv/shows/trending?extended=full&page={0}&limit={1}";

        public const string PopularMovies = "https://api.trakt.tv/movies/popular?extended=full&page={0}&limit={1}";
        public const string PopularShows = "https://api.trakt.tv/shows/popular?extended=full&page={0}&limit={1}";

        public const string AnticipatedMovies = "https://api.trakt.tv/movies/anticipated?extended=full&page={0}&limit={1}";
        public const string AnticipatedShows = "https://api.trakt.tv/shows/anticipated?extended=full&page={0}&limit={1}";

        public const string BoxOffice = "https://api.trakt.tv/movies/boxoffice?extended=full";

        public const string MovieComments = "https://api.trakt.tv/movies/{0}/comments?extended=full&page={1}&limit={2}";
        public const string ShowComments = "https://api.trakt.tv/shows/{0}/comments?extended=full&page={1}&limit={2}";
        public const string SeasonComments = "https://api.trakt.tv/shows/{0}/seasons/{1}/comments?extended=full&page={2}&limit={3}";
        public const string EpisodeComments = "https://api.trakt.tv/shows/{0}/seasons/{1}/episodes/{2}/comments?extended=full&page={3}&limit={4}";

        public const string CommentLike = "https://api.trakt.tv/comments/{0}/like";
        public const string CommentReplies = "https://api.trakt.tv/comments/{0}/replies";

        public const string SearchAll = "https://api.trakt.tv/search/{0}?query={1}&page={2}&limit={3}&extended=full";
        public const string SearchMovies = "https://api.trakt.tv/search/movie?query={0}&page={1}&limit={2}&extended=full";
        public const string SearchShows = "https://api.trakt.tv/search/show?query={0}&page={1}&limit={2}&extended=full";
        public const string SearchEpisodes = "https://api.trakt.tv/search/episode?query={0}&page={1}&limit={2}&extended=full";
        public const string SearchPeople = "https://api.trakt.tv/search/person?query={0}&page={1}&limit={2}&extended=full";
        public const string SearchUsers = "https://api.trakt.tv/search/user?query={0}&page={1}&limit={2}&extended=full";
        public const string SearchLists = "https://api.trakt.tv/search/list?query={0}&page={1}&limit={2}&extended=full";
        public const string SearchById = "https://api.trakt.tv/search/{0}/{1}?type={2}&page={3}&limit={4}&extended=full";

        public const string NetworkFriends = "https://api.trakt.tv/users/{0}/friends?extended=full,images";
        public const string NetworkFollowers = "https://api.trakt.tv/users/{0}/followers?extended=full,images";
        public const string NetworkFollowing = "https://api.trakt.tv/users/{0}/following?extended=full,images";

        public const string NetworkFollowRequest = "https://api.trakt.tv/users/requests/{0}";
        public const string NetworkFollowUser = "https://api.trakt.tv/users/{0}/follow";

        public const string ShowSummary = "https://api.trakt.tv/shows/{0}?extended=full";
        public const string MovieSummary = "https://api.trakt.tv/movies/{0}?extended=full";
        public const string EpisodeSummary = "https://api.trakt.tv/shows/{0}/seasons/{1}/episodes/{2}?extended=full";

        public const string ShowSeasons = "https://api.trakt.tv/shows/{0}/seasons?extended={1}";
        public const string SeasonEpisodes = "https://api.trakt.tv/shows/{0}/seasons/{1}?extended=full";

        public const string CalendarShows = "https://api.trakt.tv/calendars/shows/{0}/{1}?extended=full";
        public const string CalendarPremieres = "https://api.trakt.tv/calendars/shows/premieres/{0}/{1}?extended=full";
        public const string CalendarNewPremieres = "https://api.trakt.tv/calendars/shows/new/{0}/{1}?extended=full";

        public const string ScrobbleStart = "https://api.trakt.tv/scrobble/start";
        public const string ScrobblePause = "https://api.trakt.tv/scrobble/pause";
        public const string ScrobbleStop = "https://api.trakt.tv/scrobble/stop";

        public const string ShowRatings = "https://api.trakt.tv/shows/{0}/ratings";
        public const string SeasonRatings = "https://api.trakt.tv/shows/{0}/seasons/{1}/ratings";
        public const string EpisodeRatings = "https://api.trakt.tv/shows/{0}/seasons/{1}/episodes/{2}/ratings";

        public const string PersonMovieCredits = "https://api.trakt.tv/people/{0}/movies?extended=full";
        public const string PersonShowCredits = "https://api.trakt.tv/people/{0}/shows?extended=full";
        public const string PersonSummary = "https://api.trakt.tv/people/{0}?extended=full";

        public const string MoviePeople = "https://api.trakt.tv/movies/{0}/people?extended=full";
        public const string ShowPeople = "https://api.trakt.tv/shows/{0}/people?extended=full";

        public const string ShowUpdates = "https://api.trakt.tv/shows/updates/{0}?page={1}&limit={2}";
        public const string MovieUpdates = "https://api.trakt.tv/movies/updates/{0}?page={1}&limit={2}";

        public const string DismissRecommendedMovie = "https://api.trakt.tv/recommendations/movies/{0}";
        public const string DismissRecommendedShow = "https://api.trakt.tv/recommendations/shows/{0}";

        public const string DeleteList = "https://api.trakt.tv/users/{0}/lists/{1}";
    }
}
