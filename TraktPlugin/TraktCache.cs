using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MediaPortal.Configuration;
using TraktPlugin.TraktAPI;
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
        private static string cMoviesCollected = Path.Combine(Config.GetFolder(Config.Dir.Config), @"Trakt\Library\Movies\Collected.json");
        private static string cMoviesWatched = Path.Combine(Config.GetFolder(Config.Dir.Config), @"Trakt\Library\Movies\Watched.json");
        private static string cMoviesRated = Path.Combine(Config.GetFolder(Config.Dir.Config), @"Trakt\Library\Movies\Rated.json");

        private static DateTime recommendationsAge;
        private static DateTime watchListAge;
        private static DateTime customListAge;

        static Object syncLists = new object();

        static DateTime LastFollowerRequest = new DateTime();

        #region Sync

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
                                    where !currentWatched.Any(m => m.Movie.Ids.Id == movie.Movie.Ids.Id)
                                    select new TraktMovie
                                    {
                                        Ids = movie.Movie.Ids,
                                        Title = movie.Movie.Title,
                                        Year = movie.Movie.Year
                                    };

            return unwatchedMovies;
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
                SaveFileCache(cMoviesWatched, _WatchedMovies.ToJSON());

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
                    var persistedItems = LoadFileCache(cMoviesWatched, null);
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
                SaveFileCache(cMoviesCollected, _CollectedMovies.ToJSON());

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
                    var persistedItems = LoadFileCache(cMoviesCollected, null);
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
                SaveFileCache(cMoviesRated, _RatedMovies.ToJSON());

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
                    var persistedItems = LoadFileCache(cMoviesRated, null);
                    if (persistedItems != null)
                        _RatedMovies = persistedItems.FromJSONArray<TraktMovieRated>();
                }
                return _RatedMovies;
            }
        }
        static IEnumerable<TraktMovieRated> _RatedMovies = null;

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
        }
        static IEnumerable<TraktFollowerRequest> _FollowerRequests = null;

        #endregion

        #region File IO

        public static void SaveFileCache(string file, string value)
        {
            TraktLogger.Debug("Saving file to disk. Filename = '{0}'", file);

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(file));
                File.WriteAllText(file, value, Encoding.UTF8);
            }
            catch (Exception e)
            {
                TraktLogger.Error(string.Format("Error saving file. Filename = '{0}', Error = '{1}'", file, e.Message));
            }
        }

        public static string LoadFileCache(string file, string defaultValue)
        {
            TraktLogger.Debug("Loading file from disk. Filename = '{0}'", file);

            string returnValue = defaultValue;

            try
            {
                if (File.Exists(file))
                {
                    returnValue = File.ReadAllText(file, Encoding.UTF8);
                }
            }
            catch (Exception e)
            {
                TraktLogger.Error(string.Format("Error loading file from disk. Filename = '{0}', Error = '{1}'", file, e.Message));
                return defaultValue;
            }

            return returnValue;
        }

        #endregion

        #region Lists

        internal static IEnumerable<TraktWatchListMovie> TraktWatchListMovies
        {
            get
            {
                lock (syncLists)
                {
                    if (_traktWatchListMovies == null || (DateTime.Now - watchListAge) > TimeSpan.FromMinutes(TraktSettings.WebRequestCacheMinutes))
                    {
                        TraktLogger.Info("Retrieving current users watchlist from trakt.tv");
                        _traktWatchListMovies = TraktAPI.TraktAPI.GetWatchListMovies(TraktSettings.Username);
                        watchListAge = DateTime.Now;
                    }
                    return _traktWatchListMovies;
                }
            }
        }
        static IEnumerable<TraktWatchListMovie> _traktWatchListMovies = null;

        internal static IEnumerable<TraktMovie> TraktRecommendedMovies
        {
            get
            {
                lock (syncLists)
                {
                    if (_traktRecommendations == null || (DateTime.Now - recommendationsAge) > TimeSpan.FromMinutes(TraktSettings.WebRequestCacheMinutes))
                    {
                        TraktLogger.Info("Retrieving current users recommendations from trakt.tv");
                        _traktRecommendations = TraktAPI.TraktAPI.GetRecommendedMovies();
                        recommendationsAge = DateTime.Now;
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
                    if (_traktCustomLists == null || (DateTime.Now - customListAge) > TimeSpan.FromMinutes(TraktSettings.WebRequestCacheMinutes))
                    {
                        _traktCustomLists = new Dictionary<TraktListDetail, List<TraktListItem>>();

                        // first get the users custom lists from trakt
                        TraktLogger.Info("Retrieving current users custom lists from trakt.tv");
                        var userLists = TraktAPI.TraktAPI.GetUserLists(TraktSettings.Username);

                        // get details of each list including items
                        foreach (var list in userLists.Where(l => l.ItemCount > 0))
                        {
                            TraktLogger.Info("Retrieving list details for custom list from trakt.tv. Name = '{0}', Total Items = '{1}', ID = '{2}', Slug = '{3}'", list.Name, list.ItemCount, list.Ids.Id, list.Ids.Slug);
                            var userList = TraktAPI.TraktAPI.GetUserListItems(TraktSettings.Username, list.Ids.Id.ToString());

                            if (userList == null)
                                continue;

                            // add them to the cache
                            _traktCustomLists.Add(list, userList.ToList());
                        }
                        customListAge = DateTime.Now;
                    }
                    return _traktCustomLists;
                }
            }
        }
        static Dictionary<TraktListDetail, List<TraktListItem>> _traktCustomLists = null;

        internal static void ClearWatchlistMoviesCache()
        {
            lock (syncLists)
            {
                _traktWatchListMovies = null;
            }
        }

        #endregion

        #region Clear Cache

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

        #endregion
    }
}
