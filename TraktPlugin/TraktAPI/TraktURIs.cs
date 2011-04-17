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
        public const string ApiKey = "0edee4275d03fe72117e3f69a28815939b082548";
        public const string ScrobbleShow = @"http://api.trakt.tv/show/{0}/" + ApiKey;
        public const string ScrobbleMovie = @"http://api.trakt.tv/movie/{0}/" + ApiKey;
        public const string UserWatchedEpisodes = @"http://api.trakt.tv/user/library/shows/watched.json/" + ApiKey + @"/{0}";
        public const string UserWatchedMovies = @"http://api.trakt.tv/user/watched/movies.json/" + ApiKey + @"/{0}";
        public const string UserProfile = @"http://api.trakt.tv/user/profile.json/" + ApiKey + @"/{0}";
        public const string SeriesOverview = @"http://api.trakt.tv/show/summary.json/" + ApiKey + @"/{0}";
        public const string UserEpisodesCollection = @"http://api.trakt.tv/user/library/shows/collection.json/" + ApiKey + @"/{0}";
        public const string UserMoviesCollection = @"http://api.trakt.tv/user/library/movies/collection.json/" + ApiKey + @"/{0}";
        public const string UserMoviesAll = @"http://api.trakt.tv/user/library/movies/all.json/" + ApiKey + @"/{0}";
        public const string SyncEpisodeLibrary = @"http://api.trakt.tv/show/episode/{0}/" + ApiKey;
        public const string SyncMovieLibrary = @"http://api.trakt.tv/movie/{0}/" + ApiKey;
        public const string UserCalendarShows = @"http://api.trakt.tv/user/calendar/shows.json/" + ApiKey + @"/{0}/{1}/{2}";
        public const string UserFriends = @"http://api.trakt.tv/user/friends.json/" + ApiKey + @"/{0}";
        public const string RateItem = @"http://api.trakt.tv/rate/{0}/" + ApiKey;
    }
}
