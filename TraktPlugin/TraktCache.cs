using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using MediaPortal.Configuration;
using TraktPlugin.TraktAPI.DataStructures;
using TraktPlugin.TraktAPI.Extensions;

namespace TraktPlugin
{
    /// <summary>
    /// The TraktCache is used to store anything online that me need often
    /// e.g. collected, seen, ratings for movies, shows and episodes during plugin syncing
    /// </summary>
    public static class TraktCache
    {
        static Object syncLists = new object();

        private static string MoviesCollectedFile = Path.Combine(Config.GetFolder(Config.Dir.Config), @"Trakt\{username}\Library\Movies\Collected.json");
        private static string MoviesWatchedFile = Path.Combine(Config.GetFolder(Config.Dir.Config), @"Trakt\{username}\Library\Movies\Watched.json");
        private static string MoviesRatedFile = Path.Combine(Config.GetFolder(Config.Dir.Config), @"Trakt\{username}\Library\Movies\Rated.json");

        private static string EpisodesCollectedFile = Path.Combine(Config.GetFolder(Config.Dir.Config), @"Trakt\{username}\Library\Episodes\Collected.json");
        private static string EpisodesWatchedFile = Path.Combine(Config.GetFolder(Config.Dir.Config), @"Trakt\{username}\Library\Episodes\Watched.json");
        private static string EpisodesRatedFile = Path.Combine(Config.GetFolder(Config.Dir.Config), @"Trakt\{username}\Library\Episodes\Rated.json");

        private static string ShowsRatedFile = Path.Combine(Config.GetFolder(Config.Dir.Config), @"Trakt\{username}\Library\Shows\Rated.json");

        private static DateTime RecommendationsAge;
        private static DateTime WatchListAge;
        private static DateTime CustomListAge;

        private static DateTime LastFollowerRequest = new DateTime();

        #region Sync

        #region Movies

        /// <summary>
        /// Get the users unwatched movies since last sync
        /// This is something that has been previously watched but
        /// now has been removed from the users watched history either
        /// by toggling the watched state on a client or from online
        /// </summary>
        public static IEnumerable<TraktMovie> GetUnWatchedMoviesFromTrakt()
        {
            // trakt.tv does not provide an unwatched API
            // There are plans after initial launch of v2 to have a re-watch API.

            // First we need to get the previously cached watched movies
            var previouslyWatched = WatchedMovies;
            if (previouslyWatched == null)
                return new List<TraktMovie>();

            // now get the latest watched
            var currentWatched = GetWatchedMoviesFromTrakt();
            if (currentWatched == null)
                return new List<TraktMovie>();

            TraktLogger.Info("Comparing previous watched movies against current watched movies such that unwatched can be determined");

            // anything not in the currentwatched that is previously watched
            // must be unwatched now.
            var unwatchedMovies =   from movie in previouslyWatched
                                    where !currentWatched.Any(m => m.Movie.Ids.Trakt == movie.Movie.Ids.Trakt)
                                    select new TraktMovie
                                    {
                                        Ids = movie.Movie.Ids,
                                        Title = movie.Movie.Title,
                                        Year = movie.Movie.Year
                                    };

            return unwatchedMovies ?? new List<TraktMovie>();
        }

        /// <summary>
        /// Get the users watched movies from Trakt
        /// </summary>
        public static IEnumerable<TraktMovieWatched> GetWatchedMoviesFromTrakt()
        {
            // get the last time we did anything to our library online
            var lastSyncActivities = LastSyncActivities;

            // something bad happened e.g. site not available
            if (lastSyncActivities == null || lastSyncActivities.Movies == null)
                return null;

            // check the last time we have against the online time
            // if the times are the same try to load from cache
            if (lastSyncActivities.Movies.Watched == TraktSettings.LastSyncActivities.Movies.Watched)
            {
                var cachedItems = WatchedMovies;
                if (cachedItems != null)
                    return cachedItems;
            }

            TraktLogger.Debug("Movie watched history cache is out of date and does not match online data. Local Date = '{0}', Online Date = '{1}'", TraktSettings.LastSyncActivities.Movies.Watched ?? "<empty>", lastSyncActivities.Movies.Watched ?? "<empty>");

            // we get from online, local cache is not up to date
            var onlineItems = TraktAPI.TraktAPI.GetWatchedMovies();
            if (onlineItems != null)
            {
                _WatchedMovies = onlineItems;

                // save to local file cache
                SaveFileCache(MoviesWatchedFile, _WatchedMovies.ToJSON());

                // save new activity time for next time
                TraktSettings.LastSyncActivities.Movies.Watched = lastSyncActivities.Movies.Watched;
            }

            return onlineItems;
        }

        /// <summary>
        /// returns the cached users watched movies on trakt.tv
        /// </summary>
        static IEnumerable<TraktMovieWatched> WatchedMovies
        {
            get
            {
                if (_WatchedMovies == null)
                {
                    var persistedItems = LoadFileCache(MoviesWatchedFile, null);
                    if (persistedItems != null)
                        _WatchedMovies = persistedItems.FromJSONArray<TraktMovieWatched>();
                }
                return _WatchedMovies;
            }
        }
        static IEnumerable<TraktMovieWatched> _WatchedMovies = null;

        /// <summary>
        /// Get the users collected movies from Trakt
        /// </summary>
        public static IEnumerable<TraktMovieCollected> GetCollectedMoviesFromTrakt()
        {
            // get the last time we did anything to our library online
            var lastSyncActivities = LastSyncActivities;

            // something bad happened e.g. site not available
            if (lastSyncActivities == null || lastSyncActivities.Movies == null) 
                return null;

            // check the last time we have against the online time
            // if the times are the same try to load from cache
            if (lastSyncActivities.Movies.Collection == TraktSettings.LastSyncActivities.Movies.Collection)
            {
                var cachedItems = CollectedMovies;
                if (cachedItems != null)
                    return cachedItems;
            }

            TraktLogger.Debug("Movie collection cache is out of date and does not match online data. Local Date = '{0}', Online Date = '{1}'", TraktSettings.LastSyncActivities.Movies.Collection ?? "<empty>", lastSyncActivities.Movies.Collection ?? "<empty>");

            // we get from online, local cache is not up to date
            var onlineItems = TraktAPI.TraktAPI.GetCollectedMovies();
            if (onlineItems != null)
            {
                _CollectedMovies = onlineItems;

                // save to local file cache
                SaveFileCache(MoviesCollectedFile, _CollectedMovies.ToJSON());

                // save new activity time for next time
                TraktSettings.LastSyncActivities.Movies.Collection = lastSyncActivities.Movies.Collection;
            }

            return onlineItems;
        }

        /// <summary>
        /// returns the cached users collected movies on trakt.tv
        /// </summary>
        static IEnumerable<TraktMovieCollected> CollectedMovies
        {
            get
            {
                if (_CollectedMovies == null)
                {
                    var persistedItems = LoadFileCache(MoviesCollectedFile, null);
                    if (persistedItems != null)
                        _CollectedMovies = persistedItems.FromJSONArray<TraktMovieCollected>();
                }
                return _CollectedMovies;
            }
        }
        static IEnumerable<TraktMovieCollected> _CollectedMovies = null;

        /// <summary>
        /// Get the users rated movies from Trakt
        /// </summary>
        public static IEnumerable<TraktMovieRated> GetRatedMoviesFromTrakt()
        {
            // get the last time we did anything to our library online
            var lastSyncActivities = LastSyncActivities;

            // something bad happened e.g. site not available
            if (lastSyncActivities == null || lastSyncActivities.Movies == null)
                return null;

            // check the last time we have against the online time
            // if the times are the same try to load from cache
            if (lastSyncActivities.Movies.Rating == TraktSettings.LastSyncActivities.Movies.Rating)
            {
                var cachedItems = RatedMovies;
                if (cachedItems != null)
                    return cachedItems;
            }

            TraktLogger.Debug("Movie ratings cache is out of date and does not match online data. Local Date = '{0}', Online Date = '{1}'", TraktSettings.LastSyncActivities.Movies.Rating ?? "<empty>", lastSyncActivities.Movies.Rating ?? "<empty>");

            // we get from online, local cache is not up to date
            var onlineItems = TraktAPI.TraktAPI.GetRatedMovies();
            if (onlineItems != null)
            {
                _RatedMovies = onlineItems;

                // save to local file cache
                SaveFileCache(MoviesRatedFile, _RatedMovies.ToJSON());

                // save new activity time for next time
                TraktSettings.LastSyncActivities.Movies.Rating = lastSyncActivities.Movies.Rating;
            }

            return onlineItems;
        }

        /// <summary>
        /// returns the cached users rated movies on trakt.tv
        /// </summary>
        static IEnumerable<TraktMovieRated> RatedMovies
        {
            get
            {
                if (_RatedMovies == null)
                {
                    var persistedItems = LoadFileCache(MoviesRatedFile, null);
                    if (persistedItems != null)
                        _RatedMovies = persistedItems.FromJSONArray<TraktMovieRated>();
                }
                return _RatedMovies;
            }
        }
        static IEnumerable<TraktMovieRated> _RatedMovies = null;

        #endregion

        #region Episodes

        /// <summary>
        /// Get the users collected episodes from Trakt
        /// </summary>
        public static IEnumerable<EpisodeCollected> GetCollectedEpisodesFromTrakt()
        {
            // get the last time we did anything to our library online
            var lastSyncActivities = LastSyncActivities;

            // something bad happened e.g. site not available
            if (lastSyncActivities == null || lastSyncActivities.Episodes == null)
                return null;

            // check the last time we have against the online time
            // if the times are the same try to load from cache
            if (lastSyncActivities.Episodes.Collection == TraktSettings.LastSyncActivities.Episodes.Collection)
            {
                var cachedItems = CollectedEpisodes;
                if (cachedItems != null)
                    return cachedItems;
            }

            TraktLogger.Debug("TV episode collection cache is out of date and does not match online data. Local Date = '{0}', Online Date = '{1}'", TraktSettings.LastSyncActivities.Episodes.Collection ?? "<empty>", lastSyncActivities.Episodes.Collection ?? "<empty>");

            // we get from online, local cache is not up to date
            var onlineItems = TraktAPI.TraktAPI.GetCollectedEpisodes();
            if (onlineItems != null)
            {
                // convert trakt structure to more flat heirarchy (more managable)
                TraktLogger.Debug("Converting list of collected episodes from trakt to internal data structure");
                var episodesCollected = new List<EpisodeCollected>();
                foreach (var show in onlineItems)
                {
                    foreach (var season in show.Seasons)
                    {
                        foreach (var episode in season.Episodes)
                        {
                            episodesCollected.Add(new EpisodeCollected
                            {
                                ShowId = show.Show.Ids.Trakt,
                                ShowTvdbId = show.Show.Ids.Tvdb,
                                ShowImdbId = show.Show.Ids.Imdb,
                                ShowTitle = show.Show.Title,
                                ShowYear = show.Show.Year,
                                Number = episode.Number,
                                Season = season.Number,
                                CollectedAt = episode.CollectedAt
                            });
                        }
                    }
                }

                _CollectedEpisodes = episodesCollected;

                // save to local file cache
                SaveFileCache(EpisodesCollectedFile, _CollectedEpisodes.ToJSON());

                // save new activity time for next time
                TraktSettings.LastSyncActivities.Episodes.Collection = lastSyncActivities.Episodes.Collection;
            }

            return _CollectedEpisodes;
        }

        /// <summary>
        /// returns the cached users collected episodes on trakt.tv
        /// </summary>
        static IEnumerable<EpisodeCollected> CollectedEpisodes
        {
            get
            {
                if (_CollectedEpisodes == null)
                {
                    var persistedItems = LoadFileCache(EpisodesCollectedFile, null);
                    if (persistedItems != null)
                        _CollectedEpisodes = persistedItems.FromJSONArray<EpisodeCollected>();
                }
                return _CollectedEpisodes;
            }
        }
        static IEnumerable<EpisodeCollected> _CollectedEpisodes = null;

        /// <summary>
        /// Get the users watched episodes from Trakt
        /// </summary>
        public static IEnumerable<EpisodeWatched> GetWatchedEpisodesFromTrakt()
        {
            // get the last time we did anything to our library online
            var lastSyncActivities = LastSyncActivities;

            // something bad happened e.g. site not available
            if (lastSyncActivities == null || lastSyncActivities.Movies == null)
                return null;

            // check the last time we have against the online time
            // if the times are the same try to load from cache
            if (lastSyncActivities.Episodes.Watched == TraktSettings.LastSyncActivities.Episodes.Watched)
            {
                var cachedItems = WatchedEpisodes;
                if (cachedItems != null)
                    return cachedItems;
            }

            TraktLogger.Debug("TV episode watched history cache is out of date and does not match online data. Local Date = '{0}', Online Date = '{1}'", TraktSettings.LastSyncActivities.Episodes.Watched ?? "<empty>", lastSyncActivities.Episodes.Watched ?? "<empty>");

            // we get from online, local cache is not up to date
            var onlineItems = TraktAPI.TraktAPI.GetWatchedEpisodes();
            if (onlineItems != null)
            {
                // convert trakt structure to more flat heirarchy (more managable)
                TraktLogger.Debug("Converting list of watched episodes from trakt to internal data structure");
                var episodesWatched = new List<EpisodeWatched>();
                foreach (var show in onlineItems)
                {
                    foreach(var season in show.Seasons)
                    {
                        foreach(var episode in season.Episodes)
                        {
                            episodesWatched.Add( new EpisodeWatched
                            {
                                ShowId = show.Show.Ids.Trakt,
                                ShowTvdbId = show.Show.Ids.Tvdb,
                                ShowImdbId = show.Show.Ids.Imdb,
                                ShowTitle = show.Show.Title,
                                ShowYear = show.Show.Year,
                                Number = episode.Number,
                                Season = season.Number,
                                Plays = episode.Plays
                            });
                        }
                    }
                }

                _WatchedEpisodes = episodesWatched;

                // save to local file cache
                SaveFileCache(EpisodesWatchedFile, _WatchedEpisodes.ToJSON());

                // save new activity time for next time
                TraktSettings.LastSyncActivities.Episodes.Watched = lastSyncActivities.Episodes.Watched;
            }

            return _WatchedEpisodes;
        }

        /// <summary>
        /// Get the users unwatched episodes since last sync
        /// This is something that has been previously watched but
        /// now has been removed from the users watched history either
        /// by toggling the watched state on a client or from online
        /// </summary>
        public static IEnumerable<Episode> GetUnWatchedEpisodesFromTrakt()
        {
            // trakt.tv does not provide an unwatched API
            // There are plans after initial launch of v2 to have a re-watch API.

            // First we need to get the previously cached watched episodes
            var previouslyWatched = WatchedEpisodes;
            if (previouslyWatched == null)
                return new List<Episode>();

            // now get the latest watched
            var currentWatched = GetWatchedEpisodesFromTrakt();
            if (currentWatched == null)
                return new List<Episode>();

            TraktLogger.Info("Comparing previous watched episodes against current watched episodes such that unwatched can be determined");

            // anything not in the currentwatched that is previously watched
            // must be unwatched now.
            var unwatchedEpisodes = from pwe in previouslyWatched
                                    where !currentWatched.Any(cwe => cwe.ShowId == pwe.ShowId &&
                                                                     cwe.Number == pwe.Number &&
                                                                     cwe.Season == pwe.Season)
                                    select new Episode
                                    {
                                        ShowId = pwe.ShowId,
                                        ShowTvdbId = pwe.ShowTvdbId,
                                        ShowImdbId = pwe.ShowImdbId,
                                        ShowTitle = pwe.ShowTitle,
                                        ShowYear = pwe.ShowYear,
                                        Season = pwe.Season,
                                        Number = pwe.Number
                                    };

            return unwatchedEpisodes ?? new List<TraktCache.Episode>();
        }

        /// <summary>
        /// returns the cached users watched episodes on trakt.tv
        /// </summary>
        static IEnumerable<EpisodeWatched> WatchedEpisodes
        {
            get
            {
                if (_WatchedEpisodes == null)
                {
                    var persistedItems = LoadFileCache(EpisodesWatchedFile, null);
                    if (persistedItems != null)
                        _WatchedEpisodes = persistedItems.FromJSONArray<EpisodeWatched>();
                }
                return _WatchedEpisodes;
            }
        }
        static IEnumerable<EpisodeWatched> _WatchedEpisodes = null;

        /// <summary>
        /// Get the users rated episodes from Trakt
        /// </summary>
        public static IEnumerable<TraktEpisodeRated> GetRatedEpisodesFromTrakt()
        {
            // get the last time we did anything to our library online
            var lastSyncActivities = LastSyncActivities;

            // something bad happened e.g. site not available
            if (lastSyncActivities == null || lastSyncActivities.Episodes == null)
                return null;

            // check the last time we have against the online time
            // if the times are the same try to load from cache
            if (lastSyncActivities.Episodes.Rating == TraktSettings.LastSyncActivities.Episodes.Rating)
            {
                var cachedItems = RatedEpisodes;
                if (cachedItems != null)
                    return cachedItems;
            }

            TraktLogger.Debug("TV episode ratings cache is out of date and does not match online data. Local Date = '{0}', Online Date = '{1}'", TraktSettings.LastSyncActivities.Episodes.Rating ?? "<empty>", lastSyncActivities.Episodes.Rating ?? "<empty>");

            // we get from online, local cache is not up to date
            var onlineItems = TraktAPI.TraktAPI.GetRatedEpisodes();
            if (onlineItems != null)
            {
                _RatedEpisodes = onlineItems;

                // save to local file cache
                SaveFileCache(EpisodesRatedFile, _RatedEpisodes.ToJSON());

                // save new activity time for next time
                TraktSettings.LastSyncActivities.Episodes.Rating = lastSyncActivities.Episodes.Rating;
            }

            return onlineItems;
        }

        /// <summary>
        /// returns the cached users rated episodes on trakt.tv
        /// </summary>
        static IEnumerable<TraktEpisodeRated> RatedEpisodes
        {
            get
            {
                if (_RatedEpisodes == null)
                {
                    var persistedItems = LoadFileCache(EpisodesRatedFile, null);
                    if (persistedItems != null)
                        _RatedEpisodes = persistedItems.FromJSONArray<TraktEpisodeRated>();
                }
                return _RatedEpisodes;
            }
        }
        static IEnumerable<TraktEpisodeRated> _RatedEpisodes = null;

        #endregion

        #region Shows

        /// <summary>
        /// Get the users rated shows from Trakt
        /// </summary>
        public static IEnumerable<TraktShowRated> GetRatedShowsFromTrakt()
        {
            // get the last time we did anything to our library online
            var lastSyncActivities = LastSyncActivities;

            // something bad happened e.g. site not available
            if (lastSyncActivities == null || lastSyncActivities.Episodes == null)
                return null;

            // check the last time we have against the online time
            // if the times are the same try to load from cache
            if (lastSyncActivities.Shows.Rating == TraktSettings.LastSyncActivities.Shows.Rating)
            {
                var cachedItems = RatedShows;
                if (cachedItems != null)
                    return cachedItems;
            }

            TraktLogger.Debug("TV show ratings cache is out of date and does not match online data. Local Date = '{0}', Online Date = '{1}'", TraktSettings.LastSyncActivities.Shows.Rating ?? "<empty>", lastSyncActivities.Shows.Rating ?? "<empty>");

            // we get from online, local cache is not up to date
            var onlineItems = TraktAPI.TraktAPI.GetRatedShows();
            if (onlineItems != null)
            {
                _RatedShows = onlineItems;

                // save to local file cache
                SaveFileCache(ShowsRatedFile, _RatedShows.ToJSON());

                // save new activity time for next time
                TraktSettings.LastSyncActivities.Shows.Rating = lastSyncActivities.Shows.Rating;
            }

            return onlineItems;
        }

        /// <summary>
        /// returns the cached users rated shows on trakt.tv
        /// </summary>
        static IEnumerable<TraktShowRated> RatedShows
        {
            get
            {
                if (_RatedShows == null)
                {
                    var persistedItems = LoadFileCache(ShowsRatedFile, null);
                    if (persistedItems != null)
                        _RatedShows = persistedItems.FromJSONArray<TraktShowRated>();
                }
                return _RatedShows;
            }
        }
        static IEnumerable<TraktShowRated> _RatedShows = null;

        #endregion

        /// <summary>
        /// Get last sync activities from trakt to see if we need to get an update on the various sync methods
        /// This should be done atleast once before a local/online sync
        /// </summary>
        static TraktLastSyncActivities LastSyncActivities
        {
            get
            {
                if (_LastSyncActivities == null)
                {                    
                    _LastSyncActivities = TraktAPI.TraktAPI.GetLastSyncActivities();
                }
                return _LastSyncActivities;
            }
        }
        static TraktLastSyncActivities _LastSyncActivities = null;

        #endregion

        #region User

        public static IEnumerable<TraktFollowerRequest> FollowerRequests
        {
            get
            {
                if (_FollowerRequests == null || LastFollowerRequest < DateTime.UtcNow.Subtract(new TimeSpan(0, TraktSettings.WebRequestCacheMinutes, 0)))
                {
                    _FollowerRequests = TraktAPI.TraktAPI.GetFollowerRequests();
                    LastFollowerRequest = DateTime.UtcNow;
                }
                return _FollowerRequests;
            }
            set
            {
                _FollowerRequests = value;
            }
        }
        static IEnumerable<TraktFollowerRequest> _FollowerRequests = null;

        #endregion

        #region File IO

        internal static void SaveFileCache(string file, string value)
        {
            if (file.Contains("{username}") &&  string.IsNullOrEmpty(TraktSettings.Username))
                return;

            // add username to filename
            string filename = file.Replace("{username}", TraktSettings.Username);

            TraktLogger.Debug("Saving file to disk. Filename = '{0}'", filename);

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filename));
                File.WriteAllText(filename, value, Encoding.UTF8);
            }
            catch (Exception e)
            {
                TraktLogger.Error(string.Format("Error saving file. Filename = '{0}', Error = '{1}'", filename, e.Message));
            }
        }

        internal static string LoadFileCache(string file, string defaultValue)
        {
            if (file.Contains("{username}") && string.IsNullOrEmpty(TraktSettings.Username))
                return null;

            // add username to filename
            string filename = file.Replace("{username}", TraktSettings.Username);

            TraktLogger.Debug("Loading file from disk. Filename = '{0}'", filename);

            string returnValue = defaultValue;

            try
            {
                if (File.Exists(filename))
                {
                    returnValue = File.ReadAllText(filename, Encoding.UTF8);
                }
            }
            catch (Exception e)
            {
                TraktLogger.Error(string.Format("Error loading file from disk. Filename = '{0}', Error = '{1}'", filename, e.Message));
                return defaultValue;
            }

            return returnValue;
        }

        #endregion

        #region Lists

        internal static IEnumerable<TraktMovieWatchList> TraktWatchListMovies
        {
            get
            {
                lock (syncLists)
                {
                    if (_traktWatchListMovies == null || (DateTime.Now - WatchListAge) > TimeSpan.FromMinutes(TraktSettings.WebRequestCacheMinutes))
                    {
                        TraktLogger.Info("Retrieving current users watchlist from trakt.tv");
                        _traktWatchListMovies = TraktAPI.TraktAPI.GetWatchListMovies(TraktSettings.Username);
                        WatchListAge = DateTime.Now;
                    }
                    return _traktWatchListMovies;
                }
            }
        }
        static IEnumerable<TraktMovieWatchList> _traktWatchListMovies = null;

        internal static IEnumerable<TraktMovie> TraktRecommendedMovies
        {
            get
            {
                lock (syncLists)
                {
                    if (_traktRecommendations == null || (DateTime.Now - RecommendationsAge) > TimeSpan.FromMinutes(TraktSettings.WebRequestCacheMinutes))
                    {
                        TraktLogger.Info("Retrieving current users recommendations from trakt.tv");
                        _traktRecommendations = TraktAPI.TraktAPI.GetRecommendedMovies();
                        RecommendationsAge = DateTime.Now;
                    }
                    return _traktRecommendations;
                }
            }
        }
        static IEnumerable<TraktMovie> _traktRecommendations = null;

        internal static Dictionary<TraktListDetail, List<TraktListItem>> TraktCustomLists
        {
            get
            {
                lock (syncLists)
                {
                    if (_traktCustomLists == null || (DateTime.Now - CustomListAge) > TimeSpan.FromMinutes(TraktSettings.WebRequestCacheMinutes))
                    {
                        _traktCustomLists = new Dictionary<TraktListDetail, List<TraktListItem>>();

                        // first get the users custom lists from trakt
                        TraktLogger.Info("Retrieving current users custom lists from trakt.tv");
                        var userLists = TraktAPI.TraktAPI.GetUserLists(TraktSettings.Username);

                        // get details of each list including items
                        foreach (var list in userLists.Where(l => l.ItemCount > 0))
                        {
                            TraktLogger.Info("Retrieving list details for custom list from trakt.tv. Name = '{0}', Total Items = '{1}', ID = '{2}', Slug = '{3}'", list.Name, list.ItemCount, list.Ids.Trakt, list.Ids.Slug);
                            var userList = TraktAPI.TraktAPI.GetUserListItems(TraktSettings.Username, list.Ids.Trakt.ToString());

                            if (userList == null)
                                continue;

                            // add them to the cache
                            _traktCustomLists.Add(list, userList.ToList());
                        }
                        CustomListAge = DateTime.Now;
                    }
                    return _traktCustomLists;
                }
            }
        }
        static Dictionary<TraktListDetail, List<TraktListItem>> _traktCustomLists = null;

        #endregion

        #region User Data

        #region Movies

        public static bool IsWatched(this TraktMovie movie)
        {
            if (WatchedMovies == null)
                return false;

            return WatchedMovies.Any(m => m.Movie.Ids.Trakt == movie.Ids.Trakt);
        }

        public static bool IsCollected(this TraktMovie movie)
        {
            if (CollectedMovies == null)
                return false;

            return CollectedMovies.Any(m => m.Movie.Ids.Trakt == movie.Ids.Trakt);
        }

        public static bool IsWatchlisted(this TraktMovie movie)
        {
            // TODO
            return false;
        }

        public static int? UserRating(this TraktMovie movie)
        {
            if (RatedMovies == null)
                return null;

            var ratedMovie = RatedMovies.FirstOrDefault(m => m.Movie.Ids.Trakt == movie.Ids.Trakt);
            if (ratedMovie == null)
                return null;

            return ratedMovie.Rating;
        }

        public static int Plays(this TraktMovie movie)
        {
            if (WatchedMovies == null)
                return 0;

            var watchedMovie = WatchedMovies.FirstOrDefault(m => m.Movie.Ids.Trakt == movie.Ids.Trakt);
            if (watchedMovie == null)
                return 0;

            return watchedMovie.Plays;
        }

        #endregion

        #region Shows

        public static bool IsWatched(this TraktShow show)
        {
            // TODO
            return false;
        }

        public static bool IsCollected(this TraktShow show)
        {
            // TODO
            return false;
        }

        public static bool IsWatchlisted(this TraktShow show)
        {
            // TODO
            return false;
        }

        public static int? UserRating(this TraktShow show)
        {
            if (RatedShows == null)
                return null;

            var ratedShow = RatedShows.FirstOrDefault(s => s.Show.Ids.Trakt == show.Ids.Trakt);
            if (ratedShow == null)
                return null;

            return ratedShow.Rating;
        }

        public static int Plays(this TraktShow show)
        {
            //TODO
            return 0;
        }

        #endregion

        #region Episodes

        public static bool IsWatched(this TraktEpisode episode, TraktShow show)
        {
            if (WatchedEpisodes == null || show == null)
                return false;

            return WatchedEpisodes.Any(e => e.ShowId == show.Ids.Trakt &&
                                            e.Season == episode.Season &&
                                            e.Number == episode.Number);
        }

        public static bool IsCollected(this TraktEpisode episode, TraktShow show)
        {
            if (CollectedEpisodes == null || show == null)
                return false;

            return CollectedEpisodes.Any(e => e.ShowId == show.Ids.Trakt &&
                                              e.Season == episode.Season &&
                                              e.Number == episode.Number);
        }

        public static bool IsWatchlisted(this TraktEpisode episode)
        {
            // TODO
            return false;
        }

        public static int? UserRating(this TraktEpisode episode)
        {
            if (RatedEpisodes == null)
                return null;

            var ratedEpisode = RatedEpisodes.FirstOrDefault(e => e.Episode.Ids.Trakt == episode.Ids.Trakt);
            if (ratedEpisode == null)
                return null;

            return ratedEpisode.Rating;
        }

        public static int Plays(this TraktEpisode episode, TraktShow show)
        {
            if (WatchedEpisodes == null || show == null)
                return 0;

            var watchedEpisode = WatchedEpisodes.FirstOrDefault(e => e.ShowId == show.Ids.Trakt &&
                                                                     e.Season == episode.Season &&
                                                                     e.Number == episode.Number);
            if (watchedEpisode == null)
                return 0;

            return watchedEpisode.Plays;

        }

        #endregion

        #endregion

        #region Lists

        public static int Plays(this TraktListItem item)
        {
            if (item.Type == "movie" && item.Movie != null)
                return item.Movie.Plays();
            else if (item.Type == "episode" && item.Episode != null)
                return item.Episode.Plays(item.Show);

            return 0;
        }

        public static bool IsWatched(this TraktListItem item)
        {
            if (item.Type == "movie" && item.Movie != null)
                return item.Movie.IsWatched();
            else if (item.Type == "episode" && item.Episode != null)
                return item.Episode.IsWatched(item.Show);

            return false;
        }

        public static bool IsCollected(this TraktListItem item)
        {
            if (item.Type == "movie" && item.Movie != null)
                return item.Movie.IsCollected();
            else if (item.Type == "episode" && item.Episode != null)
                return item.Episode.IsCollected(item.Show);

            return false;
        }

        public static bool IsWatchlisted(this TraktListItem item)
        {
            if (item.Type == "movie" && item.Movie != null)
                return item.Movie.IsWatchlisted();
            else if (item.Type == "show" && item.Show != null)
                return item.Show.IsWatchlisted();
            else if (item.Type == "episode" && item.Episode != null)
                return item.Episode.IsWatchlisted();

            return false;
        }

        public static int? UserRating(this TraktListItem item)
        {
            if (item.Type == "movie" && item.Movie != null)
                return item.Movie.UserRating();
            else if (item.Type == "show" && item.Show != null)
                return item.Show.UserRating();
            else if (item.Type == "episode" && item.Episode != null)
                return item.Episode.UserRating();

            return null;
        }

        #endregion

        #region Clear Cache

        internal static void ClearWatchlistMoviesCache()
        {
            lock (syncLists)
            {
                _traktWatchListMovies = null;
            }
        }

        internal static void ClearRecommendationsCache()
        {
            lock (syncLists)
            {
                _traktRecommendations = null;
            }
        }

        internal static void ClearCustomListCache(string listName = null)
        {
            lock (syncLists)
            {
                // check if something to clear
                if (_traktCustomLists == null) return;

                // clear all lists
                if (string.IsNullOrEmpty(listName))
                {
                    _traktCustomLists = null;
                    return;
                }

                // clear selected list
                var list = _traktCustomLists.FirstOrDefault(t => t.Key.Name == listName);
                _traktCustomLists.Remove(list.Key);
            }
        }

        internal static void ClearLastActivityCache()
        {
            _LastSyncActivities = null;
        }

        internal static void ClearSyncCache()
        {
            _RatedEpisodes = null;
            _RatedMovies = null;
            _RatedShows = null;

            _CollectedEpisodes = null;
            _CollectedMovies = null;

            _WatchedEpisodes = null;
            _WatchedMovies = null;
        }

        #endregion

        #region Data Structures

        [DataContract]
        public class Episode
        {
            [DataMember]
            public int? ShowId { get; set; }
            [DataMember]
            public int? ShowTvdbId { get; set; }
            [DataMember]
            public string ShowImdbId { get; set; }
            [DataMember]
            public string ShowTitle { get; set; }
            [DataMember]
            public int? ShowYear { get; set; }
            [DataMember]
            public int Season { get; set; }
            [DataMember]
            public int Number { get; set; }
        }

        [DataContract]
        public class EpisodeWatched : Episode
        {
            [DataMember]
            public int Plays { get; set; }
        }

        [DataContract]
        public class EpisodeCollected : Episode
        {
            [DataMember]
            public string CollectedAt { get; set; }
        }

        #endregion
    }
}
