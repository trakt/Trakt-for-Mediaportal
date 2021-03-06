﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using MediaPortal.Configuration;
using TraktPlugin.Extensions;
using TraktAPI.DataStructures;
using TraktAPI.Extensions;

namespace TraktPlugin
{
    /// <summary>
    /// The TraktCache is used to store anything online that me need often
    /// e.g. collected, seen, ratings for movies, shows and episodes during plugin syncing
    /// </summary>
    public static class TraktCache
    {
        static Object syncLists = new object();
        static Object syncLastActivities = new object();

        private static string MoviesWatchlistedFile = Path.Combine(Config.GetFolder(Config.Dir.Config), @"Trakt\{username}\Library\Movies\Watchlisted.json");
        private static string MoviesRecommendedFile = Path.Combine(Config.GetFolder(Config.Dir.Config), @"Trakt\{username}\Library\Movies\Recommended.json");
        private static string MoviesCollectedFile = Path.Combine(Config.GetFolder(Config.Dir.Config), @"Trakt\{username}\Library\Movies\Collected.json");
        private static string MoviesWatchedFile = Path.Combine(Config.GetFolder(Config.Dir.Config), @"Trakt\{username}\Library\Movies\Watched.json");
        private static string MoviesRatedFile = Path.Combine(Config.GetFolder(Config.Dir.Config), @"Trakt\{username}\Library\Movies\Rated.json");
        private static string MoviesCommentedFile = Path.Combine(Config.GetFolder(Config.Dir.Config), @"Trakt\{username}\Library\Movies\Commented.json");
        private static string MoviesPausedFile = Path.Combine(Config.GetFolder(Config.Dir.Config), @"Trakt\{username}\Library\Movies\Paused.json");
        private static string MoviesHiddenFile = Path.Combine(Config.GetFolder(Config.Dir.Config), @"Trakt\{username}\Library\Movies\Hidden.json");

        private static string EpisodesWatchlistedFile = Path.Combine(Config.GetFolder(Config.Dir.Config), @"Trakt\{username}\Library\Episodes\Watchlisted.json");
        private static string EpisodesCollectedFile = Path.Combine(Config.GetFolder(Config.Dir.Config), @"Trakt\{username}\Library\Episodes\Collected.json");
        private static string EpisodesWatchedFile = Path.Combine(Config.GetFolder(Config.Dir.Config), @"Trakt\{username}\Library\Episodes\Watched.json");
        private static string EpisodesRatedFile = Path.Combine(Config.GetFolder(Config.Dir.Config), @"Trakt\{username}\Library\Episodes\Rated.json");
        private static string EpisodesCommentedFile = Path.Combine(Config.GetFolder(Config.Dir.Config), @"Trakt\{username}\Library\Episodes\Commented.json");
        private static string EpisodesPausedFile = Path.Combine(Config.GetFolder(Config.Dir.Config), @"Trakt\{username}\Library\Episodes\Paused.json");

        private static string ShowsWatchlistedFile = Path.Combine(Config.GetFolder(Config.Dir.Config), @"Trakt\{username}\Library\Shows\Watchlisted.json");
        private static string ShowsRatedFile = Path.Combine(Config.GetFolder(Config.Dir.Config), @"Trakt\{username}\Library\Shows\Rated.json");
        private static string ShowsCommentedFile = Path.Combine(Config.GetFolder(Config.Dir.Config), @"Trakt\{username}\Library\Shows\Commented.json");
        private static string ShowsHiddenFile = Path.Combine(Config.GetFolder(Config.Dir.Config), @"Trakt\{username}\Library\Shows\Hidden.json");

        private static string SeasonsWatchlistedFile = Path.Combine(Config.GetFolder(Config.Dir.Config), @"Trakt\{username}\Library\Seasons\Watchlisted.json");
        private static string SeasonsRatedFile = Path.Combine(Config.GetFolder(Config.Dir.Config), @"Trakt\{username}\Library\Seasons\Rated.json");
        private static string SeasonsCommentedFile = Path.Combine(Config.GetFolder(Config.Dir.Config), @"Trakt\{username}\Library\Seasons\Commented.json");
        private static string SeasonsHiddenFile = Path.Combine(Config.GetFolder(Config.Dir.Config), @"Trakt\{username}\Library\Seasons\Hidden.json");

        private static string CustomListsFile = Path.Combine(Config.GetFolder(Config.Dir.Config), @"Trakt\{username}\Library\Lists\Menu.json");
        private static string CustomListFile = Path.Combine(Config.GetFolder(Config.Dir.Config), @"Trakt\{username}\Library\Lists\{listname}.json");
        private static string CustomListCommentedFile = Path.Combine(Config.GetFolder(Config.Dir.Config), @"Trakt\{username}\Library\Lists\Commented.json");
        private static string CustomListLikedFile = Path.Combine(Config.GetFolder(Config.Dir.Config), @"Trakt\{username}\Library\Lists\Liked.json");

        private static string CommentsLikedFile = Path.Combine(Config.GetFolder(Config.Dir.Config), @"Trakt\{username}\Library\Comments\Liked.json");

        private static DateTime MovieRecommendationsAge;
        private static DateTime CustomListAge;

        private static DateTime LastFollowerRequest = new DateTime();

        #region Sync

        #region Refresh

        /// <summary>
        /// Refreshes local cache from online data
        /// This should be called before doing a sync
        /// </summary>
        public static bool RefreshData()
        {
            try
            {
                TraktLogger.Info("Started refresh of tv show user data from trakt.tv");

                // clear the last time(s) we did anything online
                ClearLastActivityCache();

                // get latest tv data from online
                // get unwatched before watched first as we need to compare old vs new
                // unwatched data is synthetic, there is no online API for it
                if (GetUnWatchedEpisodesFromTrakt().ToNullableList() != null)
                    GetWatchedEpisodesFromTrakt();

                GetCollectedEpisodesFromTrakt();
                GetRatedShowsFromTrakt();
                GetRatedSeasonsFromTrakt();
                GetRatedEpisodesFromTrakt();
                GetWatchlistedShowsFromTrakt();
                GetWatchlistedSeasonsFromTrakt();
                GetWatchlistedEpisodesFromTrakt();
                GetCommentedEpisodesFromTrakt();
                GetCommentedSeasonsFromTrakt();
                GetCommentedShowsFromTrakt();
                GetHiddenShowsFromTrakt();
                GetHiddenSeasonsFromTrakt();

                TraktLogger.Info("Finished refresh of tv show user data from trakt.tv");
                TraktLogger.Info("Started refresh of movie user data from trakt.tv");

                // get latest movie data from online
                // get unwatched first as we need to compare old vs new
                // unwatched data is synthetic, there is no online API for it
                if (GetUnWatchedMoviesFromTrakt() != null)
                    GetWatchedMoviesFromTrakt();

                GetCollectedMoviesFromTrakt();
                GetRatedMoviesFromTrakt();
                GetWatchlistedMoviesFromTrakt();
                GetCommentedMoviesFromTrakt();
                GetHiddenMoviesFromTrakt();

                TraktLogger.Info("Finished refresh of movie user data from trakt.tv");
                TraktLogger.Info("Started refresh of custom list user data from trakt.tv");

                // get custom lists from online
                GetCustomLists();
                GetCommentedListsFromTrakt();
                GetLikedListsFromTrakt();

                TraktLogger.Info("Finished refresh of custom list user data from trakt.tv");
                TraktLogger.Info("Started refresh of comment user data from trakt.tv");

                GetLikedCommentsFromTrakt();

                TraktLogger.Info("Finished refresh of comment user data from trakt.tv");

                return true;
            }
            catch (Exception ex)
            {
                TraktLogger.Error("Error getting user data from trakt.tv. Error = '{0}'", ex.Message);
                return false;
            }
        }

        #endregion

        #region Movies

        /// <summary>
        /// Get the users unwatched movies since last sync
        /// This is something that has been previously watched but
        /// now has been removed from the users watched history either
        /// by toggling the watched state on a client or from online
        /// </summary>
        public static IEnumerable<TraktMovie> GetUnWatchedMoviesFromTrakt()
        {
            if (UnWatchedMovies != null)
                return UnWatchedMovies;

            TraktLogger.Info("Getting current user unwatched movies from trakt");

            // trakt.tv does not provide an unwatched API
            // There are plans after initial launch of v2 to have a re-watch API.

            // First we need to get the previously cached watched movies
            var previouslyWatched = WatchedMovies;
            if (previouslyWatched == null)
                return new List<TraktMovie>();

            // now get the latest watched
            var currentWatched = GetWatchedMoviesFromTrakt();
            if (currentWatched == null)
                return null;

            TraktLogger.Debug("Comparing previous watched movies against current watched movies such that unwatched can be determined");

            // anything not in the currentwatched that is previously watched
            // must be unwatched now.
            var unwatchedMovies = from pw in previouslyWatched
                                  where !currentWatched.Any(m => (m.Movie.Ids.Trakt == pw.Movie.Ids.Trakt || m.Movie.Ids.Imdb == pw.Movie.Ids.Imdb))
                                  select new TraktMovie
                                  {
                                      Ids = pw.Movie.Ids,
                                      Title = pw.Movie.Title,
                                      Year = pw.Movie.Year
                                  };

            UnWatchedMovies = unwatchedMovies ?? new List<TraktMovie>();

            return UnWatchedMovies;
        }

        /// <summary>
        /// returns the users unwatched movies for current session
        /// </summary>
        static IEnumerable<TraktMovie> UnWatchedMovies { get; set; }

        /// <summary>
        /// Get the users watched movies from Trakt
        /// </summary>
        public static IEnumerable<TraktMovieWatched> GetWatchedMoviesFromTrakt(bool ignoreLastSyncTime = false)
        {
            // get from cache regardless of last sync time
            if (ignoreLastSyncTime)
                return WatchedMovies;

            TraktLogger.Info("Getting current user watched movies from trakt");

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

            TraktLogger.Info("Movie watched history cache is out of date, requesting updated data. Local Date = '{0}', Online Date = '{1}'", TraktSettings.LastSyncActivities.Movies.Watched ?? "<empty>", lastSyncActivities.Movies.Watched ?? "<empty>");

            // we get from online, local cache is not up to date
            var onlineItems = TraktAPI.TraktAPI.GetWatchedMovies();
            if (onlineItems == null)
                return null;

            _WatchedMovies = onlineItems;

            // save to local file cache
            SaveFileCache(MoviesWatchedFile, _WatchedMovies.ToJSON());

            // save new activity time for next time
            TraktSettings.LastSyncActivities.Movies.Watched = lastSyncActivities.Movies.Watched;
            
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
        public static IEnumerable<TraktMovieCollected> GetCollectedMoviesFromTrakt(bool ignoreLastSyncTime = false)
        {
            // get from cache regardless of last sync time
            if (ignoreLastSyncTime)
                return CollectedMovies;

            TraktLogger.Info("Getting current user collected movies from trakt");

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

            TraktLogger.Info("Movie collection cache is out of date, requesting updated data. Local Date = '{0}', Online Date = '{1}'", TraktSettings.LastSyncActivities.Movies.Collection ?? "<empty>", lastSyncActivities.Movies.Collection ?? "<empty>");

            // we get from online, local cache is not up to date
            var onlineItems = TraktAPI.TraktAPI.GetCollectedMovies();
            if (onlineItems == null)
                return null;
           
            _CollectedMovies = onlineItems;

            // save to local file cache
            SaveFileCache(MoviesCollectedFile, _CollectedMovies.ToJSON());

            // save new activity time for next time
            TraktSettings.LastSyncActivities.Movies.Collection = lastSyncActivities.Movies.Collection;
            
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
        public static IEnumerable<TraktMovieRated> GetRatedMoviesFromTrakt(bool ignoreLastSyncTime = false)
        {
            // get from cache regardless of last sync time
            if (ignoreLastSyncTime)
                return RatedMovies;

            TraktLogger.Info("Getting current user rated movies from trakt");

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

            TraktLogger.Info("Movie ratings cache is out of date, requesting updated data. Local Date = '{0}', Online Date = '{1}'", TraktSettings.LastSyncActivities.Movies.Rating ?? "<empty>", lastSyncActivities.Movies.Rating ?? "<empty>");

            // we get from online, local cache is not up to date
            var onlineItems = TraktAPI.TraktAPI.GetRatedMovies();
            if (onlineItems == null)
                return null;

            _RatedMovies = onlineItems;

            // save to local file cache
            SaveFileCache(MoviesRatedFile, _RatedMovies.ToJSON());

            // save new activity time for next time
            TraktSettings.LastSyncActivities.Movies.Rating = lastSyncActivities.Movies.Rating;
            
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

        /// <summary>
        /// Get the users hidden movies from Trakt (calendar and recommendations)
        /// </summary>
        public static IEnumerable<TraktHiddenItem> GetHiddenMoviesFromTrakt(bool ignoreLastSyncTime = false)
        {
            // get from cache regardless of last sync time
            if (ignoreLastSyncTime)
                return HiddenMovies;

            TraktLogger.Info("Getting current user hidden movies from trakt");

            // get the last time we did anything to our library online
            var lastSyncActivities = LastSyncActivities;

            // something bad happened e.g. site not available
            if (lastSyncActivities == null || lastSyncActivities.Movies == null)
                return null;

            // check the last time we have against the online time
            // if the times are the same try to load from cache
            if (lastSyncActivities.Movies.HiddenAt == TraktSettings.LastSyncActivities.Movies.HiddenAt)
            {
                var cachedItems = HiddenMovies;
                if (cachedItems != null)
                    return cachedItems;
            }

            TraktLogger.Info("Movie hidden cache is out of date, requesting updated data. Local Date = '{0}', Online Date = '{1}'", TraktSettings.LastSyncActivities.Movies.HiddenAt ?? "<empty>", lastSyncActivities.Movies.HiddenAt ?? "<empty>");

            // There are two sections of movie hidden data: calendar and recommendations
            // each section is paged. we need to get all this information and return a list
            var hiddenMovies = new List<TraktHiddenItem>();
            int pageCount = 0;
            int currentPage = 0;

            #region Calendar
            do
            {
                TraktLogger.Info("Getting current user hidden calendar movies from trakt. Page = {0}", ++currentPage);
                var onlineItems = TraktAPI.TraktAPI.GetHiddenItems("calendar", "movie", "min", currentPage, 100);
                if (onlineItems == null)
                    return null;

                pageCount = onlineItems.TotalPages;
                hiddenMovies.AddRange(onlineItems.HiddenItems);
            }
            while (currentPage < pageCount);
            #endregion

            #region Recommendations
            currentPage = 0;
            do
            {
                TraktLogger.Info("Getting current user hidden recommended movies from trakt. Page = {0}", ++currentPage);
                var onlineItems = TraktAPI.TraktAPI.GetHiddenItems("recommendations", "movie", "min", currentPage, 100);
                if (onlineItems == null)
                    return null;

                pageCount = onlineItems.TotalPages;
                hiddenMovies.AddRange(onlineItems.HiddenItems);
            }
            while (currentPage < pageCount);
            #endregion

            _HiddenMovies = hiddenMovies;

            // save to local file cache
            SaveFileCache(MoviesHiddenFile, _HiddenMovies.ToJSON());

            // save new activity time for next time
            TraktSettings.LastSyncActivities.Movies.HiddenAt = lastSyncActivities.Movies.HiddenAt;

            return _HiddenMovies;
        }

        /// <summary>
        /// returns the cached users hidden movies on trakt.tv
        /// </summary>
        static IEnumerable<TraktHiddenItem> HiddenMovies
        {
            get
            {
                if (_HiddenMovies == null)
                {
                    var persistedItems = LoadFileCache(MoviesHiddenFile, null);
                    if (persistedItems != null)
                        _HiddenMovies = persistedItems.FromJSONArray<TraktHiddenItem>();
                }
                return _HiddenMovies;
            }
        }
        static IEnumerable<TraktHiddenItem> _HiddenMovies = null;

        #endregion

        #region Episodes

        /// <summary>
        /// Get the users collected episodes from Trakt
        /// </summary>
        public static IEnumerable<EpisodeCollected> GetCollectedEpisodesFromTrakt(bool ignoreLastSyncTime = false)
        {
            // get from cache regardless of last sync time
            if (ignoreLastSyncTime)
                return CollectedEpisodes;

            TraktLogger.Info("Getting current user collected tv episodes from trakt.tv");

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

            TraktLogger.Info("TV episode collection cache is out of date, requesting updated data. Local Date = '{0}', Online Date = '{1}'", TraktSettings.LastSyncActivities.Episodes.Collection ?? "<empty>", lastSyncActivities.Episodes.Collection ?? "<empty>");

            // we get from online, local cache is not up to date
            var onlineItems = TraktAPI.TraktAPI.GetCollectedEpisodes();
            if (onlineItems == null)
                return null;

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
                            ShowTmdbId = show.Show.Ids.Tmdb,
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
        public static IEnumerable<EpisodeWatched> GetWatchedEpisodesFromTrakt(bool ignoreLastSyncTime = false)
        {
            // get from cache regardless of last sync time
            if (ignoreLastSyncTime)
                return WatchedEpisodes;

            TraktLogger.Info("Getting current user watched episodes from trakt.tv");

            // get the last time we did anything to our library online
            var lastSyncActivities = LastSyncActivities;

            // something bad happened e.g. site not available
            if (lastSyncActivities == null || lastSyncActivities.Episodes == null)
                return null;

            // check the last time we have against the online time
            // if the times are the same try to load from cache
            if (lastSyncActivities.Episodes.Watched == TraktSettings.LastSyncActivities.Episodes.Watched)
            {
                var cachedItems = WatchedEpisodes;
                if (cachedItems != null)
                    return cachedItems;
            }

            TraktLogger.Info("TV episode watched history cache is out of date, requesting updated data. Local Date = '{0}', Online Date = '{1}'", TraktSettings.LastSyncActivities.Episodes.Watched ?? "<empty>", lastSyncActivities.Episodes.Watched ?? "<empty>");

            // we get from online, local cache is not up to date
            var onlineItems = TraktAPI.TraktAPI.GetWatchedEpisodes();
            if (onlineItems == null)
                return null;
            
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
                            ShowTmdbId = show.Show.Ids.Tmdb,
                            ShowTitle = show.Show.Title,
                            ShowYear = show.Show.Year,
                            Number = episode.Number,
                            Season = season.Number,
                            Plays = episode.Plays,
                            WatchedAt = episode.WatchedAt
                        });
                    }
                }
            }

            _WatchedEpisodes = episodesWatched;

            // save to local file cache
            SaveFileCache(EpisodesWatchedFile, _WatchedEpisodes.ToJSON());

            // save new activity time for next time
            TraktSettings.LastSyncActivities.Episodes.Watched = lastSyncActivities.Episodes.Watched;
            
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
            if (UnWatchedEpisodes != null)
                return UnWatchedEpisodes;

            TraktLogger.Info("Getting current user unwatched episodes from trakt.tv");

            // trakt.tv does not provide an unwatched API
            // There are plans after initial launch of v2 to have a re-watch API.

            // First we need to get the previously cached watched episodes
            var previouslyWatched = WatchedEpisodes;
            if (previouslyWatched == null)
                return new List<Episode>();

            // now get the latest watched
            var currentWatched = GetWatchedEpisodesFromTrakt();
            if (currentWatched == null)
                return null;

            TraktLogger.Debug("Comparing previous watched episodes against current watched episodes such that unwatched can be determined");

            // anything not in the currentwatched that is previously watched
            // must be unwatched now.
            // Note: we can add to internal cache from external events, so we can't always rely on trakt id for comparisons
            var dictCurrWatched = currentWatched.ToLookup(cwe => cwe.ShowTvdbId + "_" + cwe.Season + "_" + cwe.Number);                 

            var unwatchedEpisodes = from pwe in previouslyWatched
                                    where !dictCurrWatched[pwe.ShowTvdbId + "_" + pwe.Season + "_" + pwe.Number].Any()
                                    select new Episode
                                    {
                                        ShowId = pwe.ShowId,
                                        ShowTvdbId = pwe.ShowTvdbId,
                                        ShowImdbId = pwe.ShowImdbId,
                                        ShowTmdbId = pwe.ShowTmdbId,
                                        ShowTitle = pwe.ShowTitle,
                                        ShowYear = pwe.ShowYear,
                                        Season = pwe.Season,
                                        Number = pwe.Number
                                    };

            UnWatchedEpisodes = unwatchedEpisodes ?? new List<Episode>();

            return UnWatchedEpisodes;
        }

        /// <summary>
        /// returns the users unwatched episodes for current session        
        /// </summary>
        static IEnumerable<Episode> UnWatchedEpisodes { get; set; }

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
        public static IEnumerable<TraktEpisodeRated> GetRatedEpisodesFromTrakt(bool ignoreLastSyncTime = false)
        {
            // get from cache regardless of last sync time
            if (ignoreLastSyncTime)
                return RatedEpisodes;

            TraktLogger.Info("Getting current user rated episodes from trakt.tv");

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

            TraktLogger.Info("TV episode ratings cache is out of date, requesting updated data. Local Date = '{0}', Online Date = '{1}'", TraktSettings.LastSyncActivities.Episodes.Rating ?? "<empty>", lastSyncActivities.Episodes.Rating ?? "<empty>");

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

        #region Seasons

        /// <summary>
        /// Get the users rated seasons from Trakt
        /// </summary>
        public static IEnumerable<TraktSeasonRated> GetRatedSeasonsFromTrakt(bool ignoreLastSyncTime = false)
        {
            // get from cache regardless of last sync time
            if (ignoreLastSyncTime)
                return RatedSeasons;

            TraktLogger.Info("Getting current user rated seasons from trakt.tv");

            // get the last time we did anything to our library online
            var lastSyncActivities = LastSyncActivities;

            // something bad happened e.g. site not available
            if (lastSyncActivities == null || lastSyncActivities.Seasons == null)
                return null;

            // check the last time we have against the online time
            // if the times are the same try to load from cache
            if (lastSyncActivities.Seasons.Rating == TraktSettings.LastSyncActivities.Seasons.Rating)
            {
                var cachedItems = RatedSeasons;
                if (cachedItems != null)
                    return cachedItems;
            }

            TraktLogger.Info("TV season ratings cache is out of date, requesting updated data. Local Date = '{0}', Online Date = '{1}'", TraktSettings.LastSyncActivities.Seasons.Rating ?? "<empty>", lastSyncActivities.Seasons.Rating ?? "<empty>");

            // we get from online, local cache is not up to date
            var onlineItems = TraktAPI.TraktAPI.GetRatedSeasons();
            if (onlineItems != null)
            {
                _RatedSeasons = onlineItems;

                // save to local file cache
                SaveFileCache(SeasonsRatedFile, _RatedSeasons.ToJSON());

                // save new activity time for next time
                TraktSettings.LastSyncActivities.Seasons.Rating = lastSyncActivities.Seasons.Rating;
            }

            return onlineItems;
        }

        /// <summary>
        /// returns the cached users rated seasons on trakt.tv
        /// </summary>
        static IEnumerable<TraktSeasonRated> RatedSeasons
        {
            get
            {
                if (_RatedSeasons == null)
                {
                    var persistedItems = LoadFileCache(SeasonsRatedFile, null);
                    if (persistedItems != null)
                        _RatedSeasons = persistedItems.FromJSONArray<TraktSeasonRated>();
                }
                return _RatedSeasons;
            }
        }
        static IEnumerable<TraktSeasonRated> _RatedSeasons = null;

        /// <summary>
        /// Get the users hidden seasons from Trakt (calendar, recommendations, watched progress and collected progress)
        /// </summary>
        public static IEnumerable<TraktHiddenItem> GetHiddenSeasonsFromTrakt(bool ignoreLastSyncTime = false)
        {
            // get from cache regardless of last sync time
            if (ignoreLastSyncTime)
                return HiddenSeasons;

            TraktLogger.Info("Getting current user hidden seasons from trakt");

            // get the last time we did anything to our library online
            var lastSyncActivities = LastSyncActivities;

            // something bad happened e.g. site not available
            if (lastSyncActivities == null || lastSyncActivities.Seasons == null)
                return null;

            // check the last time we have against the online time
            // if the times are the same try to load from cache
            if (lastSyncActivities.Seasons.HiddenAt == TraktSettings.LastSyncActivities.Seasons.HiddenAt)
            {
                var cachedItems = HiddenSeasons;
                if (cachedItems != null)
                    return cachedItems;
            }

            TraktLogger.Info("Season hidden cache is out of date, requesting updated data. Local Date = '{0}', Online Date = '{1}'", TraktSettings.LastSyncActivities.Seasons.HiddenAt ?? "<empty>", lastSyncActivities.Seasons.HiddenAt ?? "<empty>");

            // There are two sections of seasons hidden data: progress_watched and progress_collected 
            // each section is paged. we need to get all this information and return a list
            var hiddenSeasons = new List<TraktHiddenItem>();
            int pageCount = 0;
            int currentPage = 0;

            #region Collected Progress
            do
            {
                TraktLogger.Info("Getting current user hidden collected progress seasons from trakt. Page = {0}", ++currentPage);
                var onlineItems = TraktAPI.TraktAPI.GetHiddenItems("progress_collected", "season", "min", currentPage, 100);
                if (onlineItems == null)
                    return null;

                pageCount = onlineItems.TotalPages;
                hiddenSeasons.AddRange(onlineItems.HiddenItems);
            }
            while (currentPage < pageCount);
            #endregion

            #region Watched Progress
            currentPage = 0;
            do
            {
                TraktLogger.Info("Getting current user hidden watched progress seasons from trakt. Page = {0}", ++currentPage);
                var onlineItems = TraktAPI.TraktAPI.GetHiddenItems("progress_watched", "season", "min", currentPage, 100);
                if (onlineItems == null)
                    return null;

                pageCount = onlineItems.TotalPages;
                hiddenSeasons.AddRange(onlineItems.HiddenItems);
            }
            while (currentPage < pageCount);
            #endregion

            _HiddenSeasons = hiddenSeasons;

            // save to local file cache
            SaveFileCache(SeasonsHiddenFile, _HiddenSeasons.ToJSON());

            // save new activity time for next time
            TraktSettings.LastSyncActivities.Seasons.HiddenAt = lastSyncActivities.Seasons.HiddenAt;

            return _HiddenSeasons;
        }

        /// <summary>
        /// returns the cached users hidden seasons on trakt.tv
        /// </summary>
        static IEnumerable<TraktHiddenItem> HiddenSeasons
        {
            get
            {
                if (_HiddenSeasons == null)
                {
                    var persistedItems = LoadFileCache(SeasonsHiddenFile, null);
                    if (persistedItems != null)
                        _HiddenSeasons = persistedItems.FromJSONArray<TraktHiddenItem>();
                }
                return _HiddenSeasons;
            }
        }
        static IEnumerable<TraktHiddenItem> _HiddenSeasons = null;

        #endregion

        #region Shows

        /// <summary>
        /// Get the users rated shows from Trakt
        /// </summary>
        public static IEnumerable<TraktShowRated> GetRatedShowsFromTrakt(bool ignoreLastSyncTime = false)
        {
            // get from cache regardless of last sync time
            if (ignoreLastSyncTime)
                return RatedShows;

            TraktLogger.Info("Getting current user rated shows from trakt.tv");

            // get the last time we did anything to our library online
            var lastSyncActivities = LastSyncActivities;

            // something bad happened e.g. site not available
            if (lastSyncActivities == null || lastSyncActivities.Shows == null)
                return null;

            // check the last time we have against the online time
            // if the times are the same try to load from cache
            if (lastSyncActivities.Shows.Rating == TraktSettings.LastSyncActivities.Shows.Rating)
            {
                var cachedItems = RatedShows;
                if (cachedItems != null)
                    return cachedItems;
            }

            TraktLogger.Info("TV show ratings cache is out of date, requesting updated data. Local Date = '{0}', Online Date = '{1}'", TraktSettings.LastSyncActivities.Shows.Rating ?? "<empty>", lastSyncActivities.Shows.Rating ?? "<empty>");

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

        /// <summary>
        /// Get the users hidden shows from Trakt (calendar, recommendations, watched progress and collected progress)
        /// </summary>
        public static IEnumerable<TraktHiddenItem> GetHiddenShowsFromTrakt(bool ignoreLastSyncTime = false)
        {
            // get from cache regardless of last sync time
            if (ignoreLastSyncTime)
                return HiddenShows;

            TraktLogger.Info("Getting current user hidden shows from trakt");

            // get the last time we did anything to our library online
            var lastSyncActivities = LastSyncActivities;

            // something bad happened e.g. site not available
            if (lastSyncActivities == null || lastSyncActivities.Shows == null)
                return null;

            // check the last time we have against the online time
            // if the times are the same try to load from cache
            if (lastSyncActivities.Shows.HiddenAt == TraktSettings.LastSyncActivities.Shows.HiddenAt)
            {
                var cachedItems = HiddenShows;
                if (cachedItems != null)
                    return cachedItems;
            }

            TraktLogger.Info("Show hidden cache is out of date, requesting updated data. Local Date = '{0}', Online Date = '{1}'", TraktSettings.LastSyncActivities.Shows.HiddenAt ?? "<empty>", lastSyncActivities.Shows.HiddenAt ?? "<empty>");

            // There are four sections of shows hidden data: calendar, recommendations, progress_watched and progress_collected 
            // each section is paged. we need to get all this information and return a list
            List<TraktHiddenItem> hiddenShows = new List<TraktHiddenItem>();
            int pageCount = 0;
            int currentPage = 0;

            #region Calendar
            do
            {
                TraktLogger.Info("Getting current user hidden calendar shows from trakt. Page = {0}", ++currentPage);
                var onlineItems = TraktAPI.TraktAPI.GetHiddenItems("calendar", "show", "min", currentPage, 100);
                if (onlineItems == null)
                    return null;

                pageCount = onlineItems.TotalPages;
                hiddenShows.AddRange(onlineItems.HiddenItems);
            }
            while (currentPage < pageCount);
            #endregion

            #region Recommendations
            currentPage = 0;
            do
            {
                TraktLogger.Info("Getting current user hidden recommended shows from trakt. Page = {0}", ++currentPage);
                var onlineItems = TraktAPI.TraktAPI.GetHiddenItems("recommendations", "show", "min", currentPage, 100);
                if (onlineItems == null)
                    return null;

                pageCount = onlineItems.TotalPages;
                hiddenShows.AddRange(onlineItems.HiddenItems);
            }
            while (currentPage < pageCount);
            #endregion

            #region Collected Progress
            currentPage = 0;
            do
            {
                TraktLogger.Info("Getting current user hidden collected progress shows from trakt. Page = {0}", ++currentPage);
                var onlineItems = TraktAPI.TraktAPI.GetHiddenItems("progress_collected", "show", "min", currentPage, 100);
                if (onlineItems == null)
                    return null;

                pageCount = onlineItems.TotalPages;
                hiddenShows.AddRange(onlineItems.HiddenItems);
            }
            while (currentPage < pageCount);
            #endregion

            #region Watched Progress
            currentPage = 0;
            do
            {
                TraktLogger.Info("Getting current user hidden watched progress shows from trakt. Page = {0}", ++currentPage);
                var onlineItems = TraktAPI.TraktAPI.GetHiddenItems("progress_watched", "show", "min", currentPage, 100);
                if (onlineItems == null)
                    return null;

                pageCount = onlineItems.TotalPages;
                hiddenShows.AddRange(onlineItems.HiddenItems);
            }
            while (currentPage < pageCount);
            #endregion

            _HiddenShows = hiddenShows;

            // save to local file cache
            SaveFileCache(ShowsHiddenFile, _HiddenShows.ToJSON());

            // save new activity time for next time
            TraktSettings.LastSyncActivities.Shows.HiddenAt = lastSyncActivities.Shows.HiddenAt;

            return _HiddenShows;
        }

        /// <summary>
        /// returns the cached users hidden shows on trakt.tv
        /// </summary>
        static IEnumerable<TraktHiddenItem> HiddenShows
        {
            get
            {
                if (_HiddenShows == null)
                {
                    var persistedItems = LoadFileCache(ShowsHiddenFile, null);
                    if (persistedItems != null)
                        _HiddenShows = persistedItems.FromJSONArray<TraktHiddenItem>();
                }
                return _HiddenShows;
            }
        }
        static IEnumerable<TraktHiddenItem> _HiddenShows = null;

        #endregion

        #region Lists

        #region Movies

        public static IEnumerable<TraktMovieWatchList> GetWatchlistedMoviesFromTrakt(bool ignoreLastSyncTime = false)
        {
            lock (syncLists)
            {
                // get from cache regardless of last sync time
                if (ignoreLastSyncTime)
                    return WatchListMovies;

                TraktLogger.Info("Getting current user watchlisted movies from trakt");

                // get the last time we did anything to our library online
                var lastSyncActivities = LastSyncActivities;

                // something bad happened e.g. site not available
                if (lastSyncActivities == null || lastSyncActivities.Movies == null)
                    return null;

                // check the last time we have against the online time
                // if the times are the same try to load from cache
                if (lastSyncActivities.Movies.Watchlist == TraktSettings.LastSyncActivities.Movies.Watchlist)
                {
                    var cachedItems = WatchListMovies;
                    if (cachedItems != null)
                        return cachedItems;
                }

                TraktLogger.Info("Movie watchlist cache is out of date, requesting updated data. Local Date = '{0}', Online Date = '{1}'", TraktSettings.LastSyncActivities.Movies.Watchlist ?? "<empty>", lastSyncActivities.Movies.Watchlist ?? "<empty>");

                // we get from online, local cache is not up to date
                var onlineItems = TraktAPI.TraktAPI.GetWatchListMovies();
                if (onlineItems != null)
                {
                    _WatchListMovies = onlineItems;

                    // save to local file cache
                    SaveFileCache(MoviesWatchlistedFile, _WatchListMovies.ToJSON());

                    // save new activity time for next time
                    TraktSettings.LastSyncActivities.Movies.Watchlist = lastSyncActivities.Movies.Watchlist;
                }
                return onlineItems;
            }
        }

        static IEnumerable<TraktMovieWatchList> WatchListMovies
        {
            get
            {
                if (_WatchListMovies == null)
                {
                    var persistedItems = LoadFileCache(MoviesWatchlistedFile, null);
                    if (persistedItems != null)
                        _WatchListMovies = persistedItems.FromJSONArray<TraktMovieWatchList>();
                }
                return _WatchListMovies;
            }
        }
        static IEnumerable<TraktMovieWatchList> _WatchListMovies = null;

        public static IEnumerable<TraktMovie> GetRecommendedMoviesFromTrakt()
        {
            lock (syncLists)
            {
                // check the last time we have retrieved the watchlist
                // if the time is recent, try to load from cache
                if (_RecommendedMovies != null && (DateTime.Now - MovieRecommendationsAge) > TimeSpan.FromMinutes(TraktSettings.WebRequestCacheMinutes))
                {
                    var cachedItems = RecommendedMovies;
                    if (cachedItems != null)
                        return cachedItems;
                }

                TraktLogger.Debug("Recommended movies cache is out of date, requesting updated data");

                // we get from online, local cache is not up to date
                var onlineItems = TraktAPI.TraktAPI.GetRecommendedMovies();
                if (onlineItems != null)
                {
                    _RecommendedMovies = onlineItems;

                    // save to local file cache
                    SaveFileCache(MoviesRecommendedFile, _RecommendedMovies.ToJSON());

                    // save retrieve data to compare next time
                    MovieRecommendationsAge = DateTime.Now;
                }
                return onlineItems;
            }
        }

        static IEnumerable<TraktMovie> RecommendedMovies
        {
            get
            {
                if (_RecommendedMovies == null)
                {
                    var persistedItems = LoadFileCache(MoviesRecommendedFile, null);
                    if (persistedItems != null)
                        _RecommendedMovies = persistedItems.FromJSONArray<TraktMovie>();
                }
                return _RecommendedMovies;
            }
        }
        static IEnumerable<TraktMovie> _RecommendedMovies = null;

        #endregion

        #region Shows

        /// <summary>
        /// Get the users watchlisted shows from Trakt
        /// </summary>
        public static IEnumerable<TraktShowWatchList> GetWatchlistedShowsFromTrakt(bool ignoreLastSyncTime = false)
        {
            lock (syncLists)
            {
                // get from cache regardless of last sync time
                if (ignoreLastSyncTime)
                    return WatchListShows;

                TraktLogger.Info("Getting current user watchlisted shows from trakt.tv");

                // get the last time we did anything to our library online
                var lastSyncActivities = LastSyncActivities;

                // something bad happened e.g. site not available
                if (lastSyncActivities == null || lastSyncActivities.Shows == null)
                    return null;

                // check the last time we have against the online time
                // if the times are the same try to load from cache
                if (lastSyncActivities.Shows.Watchlist == TraktSettings.LastSyncActivities.Shows.Watchlist)
                {
                    var cachedItems = WatchListShows;
                    if (cachedItems != null)
                        return cachedItems;
                }

                TraktLogger.Info("TV show watchlist cache is out of date, requesting updated data. Local Date = '{0}', Online Date = '{1}'", TraktSettings.LastSyncActivities.Shows.Watchlist ?? "<empty>", lastSyncActivities.Shows.Watchlist ?? "<empty>");

                // we get from online, local cache is not up to date
                var onlineItems = TraktAPI.TraktAPI.GetWatchListShows();
                if (onlineItems == null)
                    return null;

                _WatchListShows = onlineItems;

                // save to local file cache
                SaveFileCache(ShowsWatchlistedFile, _WatchListShows.ToJSON());

                // save new activity time for next time
                TraktSettings.LastSyncActivities.Shows.Watchlist = lastSyncActivities.Shows.Watchlist;
                
                return onlineItems;
            }
        }

        /// <summary>
        /// Returns the cached users watchlisted shows on trakt.tv
        /// </summary>
        static IEnumerable<TraktShowWatchList> WatchListShows
        {
            get
            {
                if (_WatchListShows == null)
                {
                    var persistedItems = LoadFileCache(ShowsWatchlistedFile, null);
                    if (persistedItems != null)
                        _WatchListShows = persistedItems.FromJSONArray<TraktShowWatchList>();
                }
                return _WatchListShows;
            }
        }
        static IEnumerable<TraktShowWatchList> _WatchListShows = null;

        #endregion

        #region Seasons

        /// <summary>
        /// Get the users watchlisted seasons from Trakt
        /// </summary>
        public static IEnumerable<TraktSeasonWatchList> GetWatchlistedSeasonsFromTrakt(bool ignoreLastSyncTime = false)
        {
            lock (syncLists)
            {
                // get from cache regardless of last sync time
                if (ignoreLastSyncTime)
                    return WatchListSeasons;

                TraktLogger.Info("Getting current user watchlisted seasons from trakt.tv");

                // get the last time we did anything to our library online
                var lastSyncActivities = LastSyncActivities;

                // something bad happened e.g. site not available
                if (lastSyncActivities == null || lastSyncActivities.Seasons == null)
                    return null;

                // check the last time we have against the online time
                // if the times are the same try to load from cache
                if (lastSyncActivities.Seasons.Watchlist == TraktSettings.LastSyncActivities.Seasons.Watchlist)
                {
                    var cachedItems = WatchListSeasons;
                    if (cachedItems != null)
                        return cachedItems;
                }

                TraktLogger.Info("TV seasons watchlist cache is out of date, requesting updated data. Local Date = '{0}', Online Date = '{1}'", TraktSettings.LastSyncActivities.Seasons.Watchlist ?? "<empty>", lastSyncActivities.Seasons.Watchlist ?? "<empty>");

                // we get from online, local cache is not up to date
                var onlineItems = TraktAPI.TraktAPI.GetWatchListSeasons();
                if (onlineItems == null)
                    return null;

                _WatchListSeasons = onlineItems;

                // save to local file cache
                SaveFileCache(SeasonsWatchlistedFile, _WatchListSeasons.ToJSON());

                // save new activity time for next time
                TraktSettings.LastSyncActivities.Seasons.Watchlist = lastSyncActivities.Seasons.Watchlist;

                return onlineItems;
            }
        }

        /// <summary>
        /// Returns the cached users watchlisted seasons on trakt.tv
        /// </summary>
        static IEnumerable<TraktSeasonWatchList> WatchListSeasons
        {
            get
            {
                if (_WatchListSeasons == null)
                {
                    var persistedItems = LoadFileCache(SeasonsWatchlistedFile, null);
                    if (persistedItems != null)
                        _WatchListSeasons = persistedItems.FromJSONArray<TraktSeasonWatchList>();
                }
                return _WatchListSeasons;
            }
        }
        static IEnumerable<TraktSeasonWatchList> _WatchListSeasons = null;
       
        #endregion

        #region Episodes

        /// <summary>
        /// Get the users watchlisted episodes from Trakt
        /// </summary>
        public static IEnumerable<TraktEpisodeWatchList> GetWatchlistedEpisodesFromTrakt(bool ignoreLastSyncTime = false)
        {
            lock (syncLists)
            {
                // get from cache regardless of last sync time
                if (ignoreLastSyncTime)
                    return WatchListEpisodes;

                TraktLogger.Info("Getting current user watchlisted episodes from trakt.tv", TraktSettings.Username);

                // get the last time we did anything to our library online
                var lastSyncActivities = LastSyncActivities;

                // something bad happened e.g. site not available
                if (lastSyncActivities == null || lastSyncActivities.Episodes == null)
                    return null;

                // check the last time we have against the online time
                // if the times are the same try to load from cache
                if (lastSyncActivities.Episodes.Watchlist == TraktSettings.LastSyncActivities.Episodes.Watchlist)
                {
                    var cachedItems = WatchListEpisodes;
                    if (cachedItems != null)
                        return cachedItems;
                }

                TraktLogger.Info("TV episode watchlist cache is out of date, requesting updated data. Local Date = '{0}', Online Date = '{1}'", TraktSettings.LastSyncActivities.Episodes.Watchlist ?? "<empty>", lastSyncActivities.Episodes.Watchlist ?? "<empty>");

                // we get from online, local cache is not up to date
                var onlineItems = TraktAPI.TraktAPI.GetWatchListEpisodes();
                if (onlineItems == null)
                    return null;

                _WatchListEpisodes = onlineItems;

                // save to local file cache
                SaveFileCache(EpisodesWatchlistedFile, _WatchListEpisodes.ToJSON());

                // save new activity time for next time
                TraktSettings.LastSyncActivities.Episodes.Watchlist = lastSyncActivities.Episodes.Watchlist;
                
                return onlineItems;
            }
        }

        /// <summary>
        /// Returns the cached users watchlisted episodes on trakt.tv
        /// </summary>
        static IEnumerable<TraktEpisodeWatchList> WatchListEpisodes
        {
            get
            {
                if (_WatchListEpisodes == null)
                {
                    var persistedItems = LoadFileCache(EpisodesWatchlistedFile, null);
                    if (persistedItems != null)
                        _WatchListEpisodes = persistedItems.FromJSONArray<TraktEpisodeWatchList>();
                }
                return _WatchListEpisodes;
            }
        }
        static IEnumerable<TraktEpisodeWatchList> _WatchListEpisodes = null;

        #endregion

        #region Custom Lists

        /// <summary>
        /// returns the cached users custom lists on trakt.tv
        /// </summary>
        static IEnumerable<TraktListDetail> CustomLists
        {
            get
            {
                if (_CustomLists == null)
                {
                    var persistedItems = LoadFileCache(CustomListsFile, null);
                    if (persistedItems != null)
                        _CustomLists = persistedItems.FromJSONArray<TraktListDetail>();
                }
                return _CustomLists;
            }
        }
        static IEnumerable<TraktListDetail> _CustomLists = null;

        static IEnumerable<TraktListDetail> GetCustomListsFromTrakt(bool ignoreLastSyncTime = false)
        {
            // get from cache regardless of last sync time
            if (ignoreLastSyncTime)
                return CustomLists;

            // get the last time we did anything to our library online
            var lastSyncActivities = LastSyncActivities;

            // something bad happened e.g. site not available
            if (lastSyncActivities == null || lastSyncActivities.Lists == null)
                return null;

            // check the last time we have against the online time
            // if the times are the same try to load from cache
            if (lastSyncActivities.Lists.UpdatedAt == TraktSettings.LastSyncActivities.Lists.UpdatedAt)
            {
                TraktLogger.Info("Getting current user custom lists from local cache");
                var cachedItems = CustomLists;
                if (cachedItems != null)
                    return cachedItems;
            }

            TraktLogger.Info("Custom Lists cache is out of date, requesting updated data. Local Date = '{0}', Online Date = '{1}'", TraktSettings.LastSyncActivities.Lists.UpdatedAt ?? "<empty>", lastSyncActivities.Lists.UpdatedAt ?? "<empty>");

            // we get from online, local cache is not up to date
            var onlineItems = TraktAPI.TraktAPI.GetUserLists();
            if (onlineItems == null)
                return null;

            _CustomLists = onlineItems;

            // save to local file cache
            SaveFileCache(CustomListsFile, _CustomLists.ToJSON());

            // save new activity time for next time
            TraktSettings.LastSyncActivities.Lists.UpdatedAt = lastSyncActivities.Lists.UpdatedAt;

            return onlineItems;
        }

        /// <summary>
        /// Returns the users custom lists with list item details
        /// </summary>
        /// <returns></returns>
        internal static Dictionary<TraktListDetail, List<TraktListItem>> GetCustomLists(bool ignoreLastSyncTime = false)
        {
            lock (syncLists)
            {
                if (_CustomListsAndItems == null || (DateTime.Now - CustomListAge) > TimeSpan.FromMinutes(TraktSettings.WebRequestCacheMinutes))
                {
                    TraktLogger.Info("Getting current user custom lists from trakt");

                    // first get the users custom lists from trakt exluding any details for individual lists
                    var userLists = GetCustomListsFromTrakt(ignoreLastSyncTime);
                    if (userLists == null) return null;

                    // get last time individual lists were updated online
                    var lastActivities = TraktSettings.LastListActivities.ToNullableList();

                    // get details of each list including items
                    _CustomListsAndItems = new Dictionary<TraktListDetail, List<TraktListItem>>();

                    foreach (var list in userLists.Where(l => l.ItemCount > 0))
                    {
                        bool listUpdated = false;

                        // load from cache
                        TraktLogger.Info("Getting list details for custom list from local cache. Name = '{0}', ID = '{1}', Slug = '{2}'", list.Name, list.Ids.Trakt, list.Ids.Slug);
                        string filename = CustomListFile.Replace("{listname}", list.Ids.Slug);
                        var userList = LoadFileCache(filename, null).FromJSONArray<TraktListItem>();

                        // check if we have got this list before
                        ListActivity listActivityCache = null;
                        if (lastActivities != null)
                        {
                            listActivityCache = lastActivities.FirstOrDefault(c => c.Id == list.Ids.Trakt);
                        }

                        // check if we need to get update from online
                        if (userList == null || listActivityCache == null || listActivityCache.UpdatedAt != list.UpdatedAt)
                        {
                            TraktLogger.Info("Getting list details for custom list from trakt.tv, local cache is out of date. Name = '{0}', Total Items = '{1}', ID = '{2}', Slug = '{3}', Last Updated = '{4}'", list.Name, list.ItemCount, list.Ids.Trakt, list.Ids.Slug, list.UpdatedAt);
                            userList = TraktAPI.TraktAPI.GetUserListItems("me", list.Ids.Trakt.ToString());
                            listUpdated = true;
                        }

                        if (userList == null)
                            continue;

                        // update cache update time
                        if (listActivityCache != null)
                        {
                            listActivityCache.UpdatedAt = list.UpdatedAt;
                        }
                        else
                        {
                            lastActivities.Add(new ListActivity
                            {
                                Id = list.Ids.Trakt,
                                UpdatedAt = list.UpdatedAt
                            });
                        }

                        // persist cache to disk
                        if (listUpdated)
                        {
                            SaveFileCache(filename, userList.ToJSON());
                        }

                        // add list to the cache
                        _CustomListsAndItems.Add(list, userList.ToList());
                    }
                    CustomListAge = DateTime.Now;
                    TraktSettings.LastListActivities = lastActivities;
                }
            }

            return _CustomListsAndItems;
        }
        static Dictionary<TraktListDetail, List<TraktListItem>> _CustomListsAndItems = null;

        public static IEnumerable<TraktLike> GetLikedListsFromTrakt(bool ignoreLastSyncTime = false)
        {
            // get from cache regardless of last sync time
            if (ignoreLastSyncTime)
                return LikedLists;

            TraktLogger.Info("Getting current user liked lists from trakt.tv");

            // get the last time we did anything to our library online
            var lastSyncActivities = LastSyncActivities;

            // something bad happened e.g. site not available
            if (lastSyncActivities == null || lastSyncActivities.Lists == null)
                return null;

            // check the last time we have against the online time
            // if the times are the same try to load from cache
            if (lastSyncActivities.Lists.LikedAt == TraktSettings.LastSyncActivities.Lists.LikedAt)
            {
                var cachedItems = LikedLists;
                if (cachedItems != null)
                    return cachedItems;
            }

            TraktLogger.Info("Liked lists cache is out of date, requesting updated data. Local Date = '{0}', Online Date = '{1}'", TraktSettings.LastSyncActivities.Lists.LikedAt ?? "<empty>", lastSyncActivities.Lists.LikedAt ?? "<empty>");

            // we get from online, local cache is not up to date
            var onlineItems = TraktAPI.TraktAPI.GetLikedItems("lists");
            if (onlineItems != null)
            {
                bool listExists = false;
                var pagedItems = onlineItems.Likes;

                // check if we need to request more pages
                if (LikedLists != null && pagedItems.IsAny())
                {
                    // if the list id exists then we already have all liked lists
                    listExists = LikedLists.Any(l => l.List.Ids.Trakt == pagedItems.Last().List.Ids.Trakt);

                    // add the latest page to our previous cached comments
                    pagedItems = pagedItems.Union(LikedLists);
                }

                // get more pages
                if (!listExists && pagedItems.IsAny() && onlineItems.Likes.Count() == onlineItems.TotalItemsPerPage)
                {
                    for (int i = 2; i <= onlineItems.TotalPages; i++)
                    {
                        var nextPage = TraktAPI.TraktAPI.GetLikedItems("lists", "min", i);
                        if (nextPage == null || !nextPage.Likes.IsAny()) break;

                        // if the list id exists then we already have all liked lists
                        if (pagedItems.Any(c => c.List.Ids.Trakt == nextPage.Likes.Last().List.Ids.Trakt))
                            listExists = true;

                        // add the latest page to our previous requested liked lists
                        pagedItems = pagedItems.Union(nextPage.Likes);

                        if (listExists || nextPage.Likes.Count() < nextPage.TotalItemsPerPage)
                            break;
                    }
                }

                // evaluate any union additions
                if (pagedItems != null)
                    pagedItems = pagedItems.ToList();

                _LikedLists = pagedItems;

                // save to local file cache
                SaveFileCache(CustomListLikedFile, _LikedLists.ToJSON());

                // save new activity time for next time
                TraktSettings.LastSyncActivities.Lists.LikedAt = lastSyncActivities.Lists.LikedAt;

                return pagedItems == null ? null : pagedItems.OrderByDescending(l => l.LikedAt);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// returns the cached liked lists on trakt.tv
        /// </summary>
        static IEnumerable<TraktLike> LikedLists
        {
            get
            {
                if (_LikedLists == null)
                {
                    var persistedItems = LoadFileCache(CustomListLikedFile, null);
                    if (persistedItems != null)
                        _LikedLists = persistedItems.FromJSONArray<TraktLike>();
                }
                return _LikedLists;
            }
        }
        static IEnumerable<TraktLike> _LikedLists = null;

        #endregion

        #endregion

        #region Comments

        /// <summary>
        /// Get the users commented episodes from Trakt
        /// </summary>
        public static IEnumerable<TraktCommentItem> GetCommentedEpisodesFromTrakt(bool ignoreLastSyncTime = false)
        {
            // get from cache regardless of last sync time
            if (ignoreLastSyncTime)
                return CommentedEpisodes;

            TraktLogger.Info("Getting current user commented episodes from trakt.tv");

            // get the last time we did anything to our library online
            var lastSyncActivities = LastSyncActivities;

            // something bad happened e.g. site not available
            if (lastSyncActivities == null || lastSyncActivities.Episodes == null)
                return null;

            // check the last time we have against the online time
            // if the times are the same try to load from cache
            if (lastSyncActivities.Episodes.Comment == TraktSettings.LastSyncActivities.Episodes.Comment)
            {
                var cachedItems = CommentedEpisodes;
                if (cachedItems != null)
                    return cachedItems;
            }

            TraktLogger.Info("TV episode comments cache is out of date, requesting updated data. Local Date = '{0}', Online Date = '{1}'", TraktSettings.LastSyncActivities.Episodes.Comment ?? "<empty>", lastSyncActivities.Episodes.Comment ?? "<empty>");

            // we get from online, local cache is not up to date
            var onlineItems = TraktAPI.TraktAPI.GetUsersComments("me", "all", "episodes", "min");
            if (onlineItems != null)
            {
                bool commentExists = false;
                var pagedItems = onlineItems.Comments;

                // check if we need to request more pages
                if (CommentedEpisodes != null && pagedItems.IsAny())
                {
                    // if the comment id exists then we already have all comments
                    commentExists = CommentedEpisodes.Any(c => c.Comment.Id == pagedItems.Last().Comment.Id);

                    // add the latest page to our previous cached comments
                    pagedItems = pagedItems.Union(CommentedEpisodes);
                }

                // get more pages
                if (!commentExists && pagedItems.IsAny() && onlineItems.Comments.Count() == onlineItems.TotalItemsPerPage)
                {
                    for (int i = 2; i <= onlineItems.TotalPages; i++)
                    {
                        var nextPage = TraktAPI.TraktAPI.GetUsersComments("me", "all", "episodes", "min", i);
                        if (nextPage == null || !nextPage.Comments.IsAny()) break;

                        // if the comment id exists then we already have all comments
                        if (pagedItems.Any(c => c.Comment.Id == nextPage.Comments.Last().Comment.Id))
                            commentExists = true;

                        // add the latest page to our previous requested comments
                        pagedItems = pagedItems.Union(nextPage.Comments);

                        if (commentExists || nextPage.Comments.Count() < nextPage.TotalItemsPerPage)
                            break;
                    }
                }

                // evaluate any union additions
                if (pagedItems != null)
                    pagedItems = pagedItems.ToList();

                _CommentedEpisodes = pagedItems;

                // save to local file cache
                SaveFileCache(EpisodesCommentedFile, _CommentedEpisodes.ToJSON());

                // save new activity time for next time
                TraktSettings.LastSyncActivities.Episodes.Comment = lastSyncActivities.Episodes.Comment;

                return pagedItems == null ? null : pagedItems.OrderByDescending(c => c.Comment.CreatedAt);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// returns the cached commented episodes on trakt.tv
        /// </summary>
        static IEnumerable<TraktCommentItem> CommentedEpisodes
        {
            get
            {
                if (_CommentedEpisodes == null)
                {
                    var persistedItems = LoadFileCache(EpisodesCommentedFile, null);
                    if (persistedItems != null)
                        _CommentedEpisodes = persistedItems.FromJSONArray<TraktCommentItem>();
                }
                return _CommentedEpisodes;
            }
        }
        static IEnumerable<TraktCommentItem> _CommentedEpisodes = null;

        /// <summary>
        /// Get the users commented shows from Trakt
        /// </summary>
        public static IEnumerable<TraktCommentItem> GetCommentedShowsFromTrakt(bool ignoreLastSyncTime = false)
        {
            // get from cache regardless of last sync time
            if (ignoreLastSyncTime)
                return CommentedShows;

            TraktLogger.Info("Getting current user commented shows from trakt.tv");

            // get the last time we did anything to our library online
            var lastSyncActivities = LastSyncActivities;

            // something bad happened e.g. site not available
            if (lastSyncActivities == null || lastSyncActivities.Shows == null)
                return null;

            // check the last time we have against the online time
            // if the times are the same try to load from cache
            if (lastSyncActivities.Shows.Comment == TraktSettings.LastSyncActivities.Shows.Comment)
            {
                var cachedItems = CommentedShows;
                if (cachedItems != null)
                    return cachedItems;
            }

            TraktLogger.Info("TV show comments cache is out of date, requesting updated data. Local Date = '{0}', Online Date = '{1}'", TraktSettings.LastSyncActivities.Shows.Comment ?? "<empty>", lastSyncActivities.Shows.Comment ?? "<empty>");

            // we get from online, local cache is not up to date
            var onlineItems = TraktAPI.TraktAPI.GetUsersComments("me", "all", "shows", "min");
            if (onlineItems != null)
            {
                bool commentExists = false;
                var pagedItems = onlineItems.Comments;

                // check if we need to request more pages
                if (CommentedShows != null && pagedItems.IsAny())
                {
                    // if the comment id exists then we already have all comments
                    commentExists = CommentedShows.Any(c => c.Comment.Id == pagedItems.Last().Comment.Id);

                    // add the latest page to our previous cached comments
                    pagedItems = pagedItems.Union(CommentedShows);
                }

                // get more pages
                if (!commentExists && pagedItems.IsAny() && onlineItems.Comments.Count() == onlineItems.TotalItemsPerPage)
                {
                    for (int i = 2; i <= onlineItems.TotalPages; i++)
                    {
                        var nextPage = TraktAPI.TraktAPI.GetUsersComments("me", "all", "shows", "min", i);
                        if (nextPage == null || !nextPage.Comments.IsAny()) break;

                        // if the comment id exists then we already have all comments
                        if (pagedItems.Any(c => c.Comment.Id == nextPage.Comments.Last().Comment.Id))
                            commentExists = true;

                        // add the latest page to our previous requested comments
                        pagedItems = pagedItems.Union(nextPage.Comments);

                        if (commentExists || nextPage.Comments.Count() < nextPage.TotalItemsPerPage)
                            break;
                    }
                }

                // evaluate any union additions
                if (pagedItems != null)
                    pagedItems = pagedItems.ToList();

                _CommentedShows = pagedItems;

                // save to local file cache
                SaveFileCache(ShowsCommentedFile, _CommentedShows.ToJSON());

                // save new activity time for next time
                TraktSettings.LastSyncActivities.Shows.Comment = lastSyncActivities.Shows.Comment;

                return pagedItems == null ? null : pagedItems.OrderByDescending(c => c.Comment.CreatedAt);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// returns the cached commented shows on trakt.tv
        /// </summary>
        static IEnumerable<TraktCommentItem> CommentedShows
        {
            get
            {
                if (_CommentedShows == null)
                {
                    var persistedItems = LoadFileCache(ShowsCommentedFile, null);
                    if (persistedItems != null)
                        _CommentedShows = persistedItems.FromJSONArray<TraktCommentItem>();
                }
                return _CommentedShows;
            }
        }
        static IEnumerable<TraktCommentItem> _CommentedShows = null;

        /// <summary>
        /// Get the users commented seasons from Trakt
        /// </summary>
        public static IEnumerable<TraktCommentItem> GetCommentedSeasonsFromTrakt(bool ignoreLastSyncTime = false)
        {
            // get from cache regardless of last sync time
            if (ignoreLastSyncTime)
                return CommentedSeasons;

            TraktLogger.Info("Getting current user commented seasons from trakt.tv");

            // get the last time we did anything to our library online
            var lastSyncActivities = LastSyncActivities;

            // something bad happened e.g. site not available
            if (lastSyncActivities == null || lastSyncActivities.Seasons == null)
                return null;

            // check the last time we have against the online time
            // if the times are the same try to load from cache
            if (lastSyncActivities.Seasons.Comment == TraktSettings.LastSyncActivities.Seasons.Comment)
            {
                var cachedItems = CommentedSeasons;
                if (cachedItems != null)
                    return cachedItems;
            }

            TraktLogger.Info("TV season comments cache is out of date, requesting updated data. Local Date = '{0}', Online Date = '{1}'", TraktSettings.LastSyncActivities.Seasons.Comment ?? "<empty>", lastSyncActivities.Seasons.Comment ?? "<empty>");

            // we get from online, local cache is not up to date
            var onlineItems = TraktAPI.TraktAPI.GetUsersComments("me", "all", "seasons", "min");
            if (onlineItems != null)
            {
                bool commentExists = false;
                var pagedItems = onlineItems.Comments;

                // check if we need to request more pages
                if (CommentedSeasons != null && pagedItems.IsAny())
                {
                    // if the comment id exists then we already have all comments
                    commentExists = CommentedSeasons.Any(c => c.Comment.Id == pagedItems.Last().Comment.Id);

                    // add the latest page to our previous cached comments
                    pagedItems = pagedItems.Union(CommentedSeasons);
                }

                // get more pages
                if (!commentExists && pagedItems.IsAny() && onlineItems.Comments.Count() == onlineItems.TotalItemsPerPage)
                {
                    for (int i = 2; i <= onlineItems.TotalPages; i++)
                    {
                        var nextPage = TraktAPI.TraktAPI.GetUsersComments("me", "all", "seasons", "min", i);
                        if (nextPage == null || !nextPage.Comments.IsAny()) break;

                        // if the comment id exists then we already have all comments
                        if (pagedItems.Any(c => c.Comment.Id == nextPage.Comments.Last().Comment.Id))
                            commentExists = true;

                        // add the latest page to our previous requested comments
                        pagedItems = pagedItems.Union(nextPage.Comments);

                        if (commentExists || nextPage.Comments.Count() < nextPage.TotalItemsPerPage)
                            break;
                    }
                }

                // evaluate any union additions
                if (pagedItems != null)
                    pagedItems = pagedItems.ToList();

                _CommentedSeasons = pagedItems;

                // save to local file cache
                SaveFileCache(SeasonsCommentedFile, _CommentedSeasons.ToJSON());

                // save new activity time for next time
                TraktSettings.LastSyncActivities.Seasons.Comment = lastSyncActivities.Seasons.Comment;

                return pagedItems == null ? null : pagedItems.OrderByDescending(c => c.Comment.CreatedAt);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// returns the cached commented seasons on trakt.tv
        /// </summary>
        static IEnumerable<TraktCommentItem> CommentedSeasons
        {
            get
            {
                if (_CommentedSeasons == null)
                {
                    var persistedItems = LoadFileCache(SeasonsCommentedFile, null);
                    if (persistedItems != null)
                        _CommentedSeasons = persistedItems.FromJSONArray<TraktCommentItem>();
                }
                return _CommentedSeasons;
            }
        }
        static IEnumerable<TraktCommentItem> _CommentedSeasons = null;


        /// <summary>
        /// Get the users commented movies from Trakt
        /// </summary>
        public static IEnumerable<TraktCommentItem> GetCommentedMoviesFromTrakt(bool ignoreLastSyncTime = false)
        {
            // get from cache regardless of last sync time
            if (ignoreLastSyncTime)
                return CommentedMovies;

            TraktLogger.Info("Getting current user commented movies from trakt.tv");

            // get the last time we did anything to our library online
            var lastSyncActivities = LastSyncActivities;

            // something bad happened e.g. site not available
            if (lastSyncActivities == null || lastSyncActivities.Movies == null)
                return null;

            // check the last time we have against the online time
            // if the times are the same try to load from cache
            if (lastSyncActivities.Movies.Comment == TraktSettings.LastSyncActivities.Movies.Comment)
            {
                var cachedItems = CommentedMovies;
                if (cachedItems != null)
                    return cachedItems;
            }

            TraktLogger.Info("Movie comments cache is out of date, requesting updated data. Local Date = '{0}', Online Date = '{1}'", TraktSettings.LastSyncActivities.Movies.Comment ?? "<empty>", lastSyncActivities.Movies.Comment ?? "<empty>");

            // we get from online, local cache is not up to date
            var onlineItems = TraktAPI.TraktAPI.GetUsersComments("me", "all", "movies", "min");
            if (onlineItems != null)
            {
                bool commentExists = false;
                var pagedItems = onlineItems.Comments;

                // check if we need to request more pages
                if (CommentedMovies != null && pagedItems.IsAny())
                {
                    // if the comment id exists then we already have all comments
                    commentExists = CommentedMovies.Any(c => c.Comment.Id == pagedItems.Last().Comment.Id);

                    // add the latest page to our previous cached comments
                    pagedItems = pagedItems.Union(CommentedMovies);
                }

                // get more pages
                if (!commentExists && pagedItems.IsAny() && onlineItems.Comments.Count() == onlineItems.TotalItemsPerPage)
                {
                    for (int i = 2; i <= onlineItems.TotalPages; i++)
                    {
                        var nextPage = TraktAPI.TraktAPI.GetUsersComments("me", "all", "movies", "min", i);
                        if (nextPage == null || !nextPage.Comments.IsAny()) break;

                        // if the comment id exists then we already have all comments
                        if (pagedItems.Any(c => c.Comment.Id == nextPage.Comments.Last().Comment.Id))
                            commentExists = true;

                        // add the latest page to our previous requested comments
                        pagedItems = pagedItems.Union(nextPage.Comments);

                        if (commentExists || nextPage.Comments.Count() < nextPage.TotalItemsPerPage)
                            break;
                    }
                }

                // evaluate any union additions
                if (pagedItems != null)
                    pagedItems = pagedItems.ToList();

                _CommentedMovies = pagedItems;

                // save to local file cache
                SaveFileCache(MoviesCommentedFile, _CommentedMovies.ToJSON());

                // save new activity time for next time
                TraktSettings.LastSyncActivities.Movies.Comment = lastSyncActivities.Movies.Comment;

                return pagedItems == null ? null : pagedItems.OrderByDescending(c => c.Comment.CreatedAt);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// returns the cached commented movies on trakt.tv
        /// </summary>
        static IEnumerable<TraktCommentItem> CommentedMovies
        {
            get
            {
                if (_CommentedMovies == null)
                {
                    var persistedItems = LoadFileCache(MoviesCommentedFile, null);
                    if (persistedItems != null)
                        _CommentedMovies = persistedItems.FromJSONArray<TraktCommentItem>();
                }
                return _CommentedMovies;
            }
        }
        static IEnumerable<TraktCommentItem> _CommentedMovies = null;

        /// <summary>
        /// Get the users commented lists from Trakt
        /// </summary>
        public static IEnumerable<TraktCommentItem> GetCommentedListsFromTrakt(bool ignoreLastSyncTime = false)
        {
            // get from cache regardless of last sync time
            if (ignoreLastSyncTime)
                return CommentedMovies;

            TraktLogger.Info("Getting current user commented lists from trakt.tv");

            // get the last time we did anything to our library online
            var lastSyncActivities = LastSyncActivities;

            // something bad happened e.g. site not available
            if (lastSyncActivities == null || lastSyncActivities.Lists == null)
                return null;

            // check the last time we have against the online time
            // if the times are the same try to load from cache
            if (lastSyncActivities.Lists.Comment == TraktSettings.LastSyncActivities.Lists.Comment)
            {
                var cachedItems = CommentedLists;
                if (cachedItems != null)
                    return cachedItems;
            }

            TraktLogger.Info("List comments cache is out of date, requesting updated data. Local Date = '{0}', Online Date = '{1}'", TraktSettings.LastSyncActivities.Lists.Comment ?? "<empty>", lastSyncActivities.Lists.Comment ?? "<empty>");

            // we get from online, local cache is not up to date
            var onlineItems = TraktAPI.TraktAPI.GetUsersComments("me", "all", "lists", "min");
            if (onlineItems != null)
            {
                bool commentExists = false;
                var pagedItems = onlineItems.Comments;

                // check if we need to request more pages
                if (CommentedLists != null && pagedItems.IsAny())
                {
                    // if the comment id exists then we already have all comments
                    commentExists = CommentedLists.Any(c => c.Comment.Id == pagedItems.Last().Comment.Id);

                    // add the latest page to our previous cached comments
                    pagedItems = pagedItems.Union(CommentedLists);
                }

                // get more pages
                if (!commentExists && pagedItems.IsAny() && onlineItems.Comments.Count() == onlineItems.TotalItemsPerPage)
                {
                    for (int i = 2; i <= onlineItems.TotalPages; i++)
                    {
                        var nextPage = TraktAPI.TraktAPI.GetUsersComments("me", "all", "lists", "min", i);
                        if (nextPage == null || !nextPage.Comments.IsAny()) break;

                        // if the comment id exists then we already have all comments
                        if (pagedItems.Any(c => c.Comment.Id == nextPage.Comments.Last().Comment.Id))
                            commentExists = true;

                        // add the latest page to our previous requested comments
                        pagedItems = pagedItems.Union(nextPage.Comments);

                        if (commentExists || nextPage.Comments.Count() < nextPage.TotalItemsPerPage)
                            break;
                    }
                }

                // evaluate any union additions
                if (pagedItems != null)
                    pagedItems = pagedItems.ToList();

                _CommentedLists = pagedItems;

                // save to local file cache
                SaveFileCache(CustomListCommentedFile, _CommentedLists.ToJSON());

                // save new activity time for next time
                TraktSettings.LastSyncActivities.Lists.Comment = lastSyncActivities.Lists.Comment;

                return pagedItems == null ? null : pagedItems.OrderByDescending(c => c.Comment.CreatedAt);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// returns the cached commented lists on trakt.tv
        /// </summary>
        static IEnumerable<TraktCommentItem> CommentedLists
        {
            get
            {
                if (_CommentedLists == null)
                {
                    var persistedItems = LoadFileCache(CustomListCommentedFile, null);
                    if (persistedItems != null)
                        _CommentedLists = persistedItems.FromJSONArray<TraktCommentItem>();
                }
                return _CommentedLists;
            }
        }
        static IEnumerable<TraktCommentItem> _CommentedLists = null;

        public static IEnumerable<TraktLike> GetLikedCommentsFromTrakt(bool ignoreLastSyncTime = false)
        {
            // get from cache regardless of last sync time
            if (ignoreLastSyncTime)
                return LikedComments;

            TraktLogger.Info("Getting current user liked comments from trakt.tv");

            // get the last time we did anything to our library online
            var lastSyncActivities = LastSyncActivities;

            // something bad happened e.g. site not available
            if (lastSyncActivities == null || lastSyncActivities.Comments == null)
                return null;

            // check the last time we have against the online time
            // if the times are the same try to load from cache
            if (lastSyncActivities.Comments.LikedAt == TraktSettings.LastSyncActivities.Comments.LikedAt)
            {
                var cachedItems = LikedComments;
                if (cachedItems != null)
                    return cachedItems;
            }

            TraktLogger.Info("Liked comments cache is out of date, requesting updated data. Local Date = '{0}', Online Date = '{1}'", TraktSettings.LastSyncActivities.Comments.LikedAt ?? "<empty>", lastSyncActivities.Comments.LikedAt ?? "<empty>");

            // we get from online, local cache is not up to date
            var onlineItems = TraktAPI.TraktAPI.GetLikedItems("comments");
            if (onlineItems != null)
            {
                bool commentExists = false;
                var pagedItems = onlineItems.Likes;

                // check if we need to request more pages
                if (LikedComments != null && pagedItems.IsAny())
                {
                    // if the comment id exists then we already have all liked comments
                    commentExists = LikedComments.Any(l => l.Comment.Id == pagedItems.Last().Comment.Id);

                    // add the latest page to our previous cached comments
                    pagedItems = pagedItems.Union(LikedComments);
                }

                // get more pages
                if (!commentExists && pagedItems.IsAny() && onlineItems.Likes.Count() == onlineItems.TotalItemsPerPage)
                {
                    for (int i = 2; i <= onlineItems.TotalPages; i++)
                    {
                        var nextPage = TraktAPI.TraktAPI.GetLikedItems("comments", "min", i);
                        if (nextPage == null || !nextPage.Likes.IsAny()) break;

                        // if the comment id exists then we already have all liked comments
                        if (pagedItems.Any(c => c.Comment.Id == nextPage.Likes.Last().Comment.Id))
                            commentExists = true;

                        // add the latest page to our previous requested liked comments
                        pagedItems = pagedItems.Union(nextPage.Likes);

                        if (commentExists || nextPage.Likes.Count() < nextPage.TotalItemsPerPage)
                            break;
                    }
                }

                // evaluate any union additions
                if (pagedItems != null)
                    pagedItems = pagedItems.ToList();

                _LikedComments = pagedItems;

                // save to local file cache
                SaveFileCache(CommentsLikedFile, _LikedComments.ToJSON());

                // save new activity time for next time
                TraktSettings.LastSyncActivities.Comments.LikedAt = lastSyncActivities.Comments.LikedAt;

                return pagedItems == null ? null : pagedItems.OrderByDescending(l => l.LikedAt);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// returns the cached liked comments on trakt.tv
        /// </summary>
        static IEnumerable<TraktLike> LikedComments
        {
            get
            {
                if (_LikedComments == null)
                {
                    var persistedItems = LoadFileCache(CommentsLikedFile, null);
                    if (persistedItems != null)
                        _LikedComments = persistedItems.FromJSONArray<TraktLike>();
                }
                return _LikedComments;
            }
        }
        static IEnumerable<TraktLike> _LikedComments = null;

        #endregion

        #region Last Activity
        /// <summary>
        /// Get last sync activities from trakt to see if we need to get an update on the various sync methods
        /// This should be done atleast once before a local/online sync
        /// </summary>
        public static TraktLastSyncActivities LastSyncActivities
        {
            get
            {
                lock (syncLastActivities)
                {
                    if (_LastSyncActivities == null)
                    {
                        TraktLogger.Info("Getting current user last activity times from trakt.tv");
                        _LastSyncActivities = TraktAPI.TraktAPI.GetLastSyncActivities();
                    }
                    return _LastSyncActivities;
                }
            }
        }
        static TraktLastSyncActivities _LastSyncActivities = null;
        #endregion

        #region Paused

        public static IEnumerable<TraktSyncPausedMovie> GetPausedMovies(out string lastMovieProcessedAt, bool ignoreLastSyncTime = false)
        {
            lastMovieProcessedAt = TraktSettings.LastSyncActivities.Movies.PausedAt;

            // get from cache regardless of last sync time
            if (ignoreLastSyncTime)
                return PausedMovies;

            TraktLogger.Info("Getting current user paused movies from trakt.tv");

            // get the last time we did anything to our library online
            var lastSyncActivities = LastSyncActivities;

            // something bad happened e.g. site not available
            if (lastSyncActivities == null || lastSyncActivities.Movies == null)
                return null;

            // check the last time we have against the online time
            // if the times are the same try to load from cache
            if (lastSyncActivities.Movies.PausedAt == TraktSettings.LastSyncActivities.Movies.PausedAt)
            {
                var cachedItems = PausedMovies;
                if (cachedItems != null)
                    return cachedItems;
            }

            TraktLogger.Info("Movie paused cache is out of date, requesting updated data. Local Date = '{0}', Online Date = '{1}'", TraktSettings.LastSyncActivities.Movies.PausedAt ?? "<empty>", lastSyncActivities.Movies.PausedAt ?? "<empty>");

            // we get from online, local cache is not up to date
            var onlineItems = TraktAPI.TraktAPI.GetPausedMovies();
            if (onlineItems != null)
            {
                _PausedMovies = onlineItems;

                // save to local file cache
                SaveFileCache(MoviesPausedFile, _PausedMovies.ToJSON());

                // save new activity time for next time
                TraktSettings.LastSyncActivities.Movies.PausedAt = lastSyncActivities.Movies.PausedAt;
            }

            return onlineItems;
        }

        static IEnumerable<TraktSyncPausedMovie> PausedMovies
        {
            get
            {
                if (_PausedMovies == null)
                {
                    var persistedItems = LoadFileCache(MoviesPausedFile, null);
                    if (persistedItems != null)
                        _PausedMovies = persistedItems.FromJSONArray<TraktSyncPausedMovie>();
                }
                return _PausedMovies;
            }
        }
        static IEnumerable<TraktSyncPausedMovie> _PausedMovies;

        public static IEnumerable<TraktSyncPausedEpisode> GetPausedEpisodes(out string lastEpisodeProcessedAt, bool ignoreLastSyncTime = false)
        {
            lastEpisodeProcessedAt = TraktSettings.LastSyncActivities.Episodes.PausedAt;

            // get from cache regardless of last sync time
            if (ignoreLastSyncTime)
                return PausedEpisodes;

            TraktLogger.Info("Getting current user paused episodes from trakt.tv");

            // get the last time we did anything to our library online
            var lastSyncActivities = LastSyncActivities;

            // something bad happened e.g. site not available
            if (lastSyncActivities == null || lastSyncActivities.Movies == null)
                return null;

            // check the last time we have against the online time
            // if the times are the same try to load from cache
            if (lastSyncActivities.Episodes.PausedAt == TraktSettings.LastSyncActivities.Episodes.PausedAt)
            {
                var cachedItems = PausedEpisodes;
                if (cachedItems != null)
                    return cachedItems;
            }

            TraktLogger.Info("TV episode paused cache is out of date, requesting updated data. Local Date = '{0}', Online Date = '{1}'", TraktSettings.LastSyncActivities.Episodes.PausedAt ?? "<empty>", lastSyncActivities.Episodes.PausedAt ?? "<empty>");

            // we get from online, local cache is not up to date
            var onlineItems = TraktAPI.TraktAPI.GetPausedEpisodes();
            if (onlineItems != null)
            {
                _PausedEpisodes = onlineItems;

                // save to local file cache
                SaveFileCache(EpisodesPausedFile, _PausedEpisodes.ToJSON());

                // save new activity time for next time
                TraktSettings.LastSyncActivities.Episodes.PausedAt = lastSyncActivities.Episodes.PausedAt;
            }

            return onlineItems;
        }

        static IEnumerable<TraktSyncPausedEpisode> PausedEpisodes
        {
            get
            {
                if (_PausedEpisodes == null)
                {
                    var persistedItems = LoadFileCache(EpisodesPausedFile, null);
                    if (persistedItems != null)
                        _PausedEpisodes = persistedItems.FromJSONArray<TraktSyncPausedEpisode>();
                }
                return _PausedEpisodes;
            }
        }
        static IEnumerable<TraktSyncPausedEpisode> _PausedEpisodes;

        #endregion

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
            if ((file.Contains("{username}") && string.IsNullOrEmpty(TraktSettings.Username)) || value == null)
                return;

            // add username to filename
            string filename = file.Replace("{username}", TraktSettings.Username.ToSafeFilename());

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
            string filename = file.Replace("{username}", TraktSettings.Username.ToSafeFilename());

            string returnValue = defaultValue;

            try
            {
                if (File.Exists(filename))
                {
                    TraktLogger.Debug("Loading file from disk. Filename = '{0}'", filename);
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

        #region Get User Data

        #region Statistics

        public static int StatsMoviesLoved()
        {
            if (RatedMovies == null)
                return 0;

            return RatedMovies.Where(m => m.Rating >= 6).Count();
        }

        public static int StatsMoviesHated()
        {
            if (RatedMovies == null)
                return 0;

            return RatedMovies.Where(m => m.Rating < 6).Count();
        }

        public static int StatsShowsLoved()
        {
            if (RatedShows == null)
                return 0;

            return RatedShows.Where(s => s.Rating >= 6).Count();
        }

        public static int StatsShowsHated()
        {
            if (RatedShows == null)
                return 0;

            return RatedShows.Where(s => s.Rating < 6).Count();
        }

        public static int StatsEpisodesLoved()
        {
            if (RatedEpisodes == null)
                return 0;

            return RatedEpisodes.Where(e => e.Rating >= 6).Count();
        }

        public static int StatsEpisodesHated()
        {
            if (RatedEpisodes == null)
                return 0;

            return RatedEpisodes.Where(e => e.Rating < 6).Count();
        }

        #endregion

        #region Movies

        public static bool IsWatched(this TraktMovie movie)
        {
            if (WatchedMovies == null)
                return false;

            return WatchedMovies.Any(m => ((m.Movie.Ids.Trakt == movie.Ids.Trakt) && m.Movie.Ids.Trakt != null) ||
                                          ((m.Movie.Ids.Imdb == movie.Ids.Imdb) && m.Movie.Ids.Imdb.ToNullIfEmpty() != null) ||
                                          ((m.Movie.Ids.Tmdb == movie.Ids.Tmdb) && m.Movie.Ids.Tmdb != null));
        }

        public static bool IsCollected(this TraktMovie movie)
        {
            if (CollectedMovies == null)
                return false;

            return CollectedMovies.Any(m => ((m.Movie.Ids.Trakt == movie.Ids.Trakt) && m.Movie.Ids.Trakt != null) ||
                                            ((m.Movie.Ids.Imdb == movie.Ids.Imdb) && m.Movie.Ids.Imdb.ToNullIfEmpty() != null) ||
                                            ((m.Movie.Ids.Tmdb == movie.Ids.Tmdb) && m.Movie.Ids.Tmdb != null));
        }

        public static bool IsWatchlisted(this TraktMovie movie)
        {
            if (WatchListMovies == null)
                return false;

            return WatchListMovies.Any(m => ((m.Movie.Ids.Trakt == movie.Ids.Trakt) && m.Movie.Ids.Trakt != null) ||
                                            ((m.Movie.Ids.Imdb == movie.Ids.Imdb) && m.Movie.Ids.Imdb.ToNullIfEmpty() != null) ||
                                            ((m.Movie.Ids.Tmdb == movie.Ids.Tmdb) && m.Movie.Ids.Tmdb != null));
        }

        public static int? UserRating(this TraktMovie movie)
        {
            if (RatedMovies == null)
                return null;

            var ratedMovie = RatedMovies.FirstOrDefault(m => ((m.Movie.Ids.Trakt == movie.Ids.Trakt) && m.Movie.Ids.Trakt != null) ||
                                                             ((m.Movie.Ids.Imdb == movie.Ids.Imdb) && m.Movie.Ids.Imdb.ToNullIfEmpty() != null) ||
                                                             ((m.Movie.Ids.Tmdb == movie.Ids.Tmdb) && m.Movie.Ids.Tmdb != null));
            if (ratedMovie == null)
                return null;

            return ratedMovie.Rating;
        }

        public static int Plays(this TraktMovie movie)
        {
            if (WatchedMovies == null)
                return 0;

            // we may added a play from a real-time scrobble or added a new play from a sync
            var watchedMovies = WatchedMovies.Where(m => ((m.Movie.Ids.Trakt == movie.Ids.Trakt) && m.Movie.Ids.Trakt != null) ||
                                                         ((m.Movie.Ids.Imdb == movie.Ids.Imdb) && m.Movie.Ids.Imdb.ToNullIfEmpty() != null) ||
                                                         ((m.Movie.Ids.Tmdb == movie.Ids.Tmdb) && m.Movie.Ids.Tmdb != null));
            if (watchedMovies == null)
                return 0;

            return watchedMovies.Sum(m => m.Plays);
        }

        public static bool IsHidden(this TraktMovie movie, string section)
        {
            if (HiddenMovies == null)
                return false;

            return HiddenMovies.Any(m => (((m.Movie.Ids.Trakt == movie.Ids.Trakt) && m.Movie.Ids.Trakt != null) || ((m.Movie.Ids.Imdb == movie.Ids.Imdb) && movie.Ids.Imdb != null)) && m.Section == section);
        }

        #endregion

        #region Shows

        public static bool IsWatched(this TraktShowSummary show)
        {
            if (show.AiredEpisodes == 0)
                return false;

            var watchedEpisodes = TraktCache.WatchedEpisodes;
            if (watchedEpisodes == null)
                return false;

            // check that the shows aired episode count >= episodes watched in show
            // trakt does not include specials in count, nor should we
            int watchedEpisodeCount = watchedEpisodes.Where(e => (((e.ShowId == show.Ids.Trakt) && e.ShowId != null) || ((e.ShowTvdbId == show.Ids.Tvdb) && show.Ids.Tvdb != null)) && 
                                                                    e.Season != 0).Count();
            return watchedEpisodeCount >= show.AiredEpisodes;
        }

        public static bool IsCollected(this TraktShowSummary show)
        {
            if (show.AiredEpisodes == 0)
                return false;

            var collectedEpisodes = TraktCache.CollectedEpisodes;
            if (collectedEpisodes == null)
                return false;

            // check that the shows aired episode count >= episodes collected in show
            // trakt does not include specials in count, nor should we
            int collectedEpisodeCount = collectedEpisodes.Where(e => (((e.ShowId == show.Ids.Trakt) && e.ShowId != null) || ((e.ShowTvdbId == show.Ids.Tvdb) && show.Ids.Tvdb != null)) &&
                                                                        e.Season != 0).Count();
            return collectedEpisodeCount >= show.AiredEpisodes;
        }

        public static bool IsWatchlisted(this TraktShow show)
        {
            if (WatchListShows == null)
                return false;

            return WatchListShows.Any(s => (((s.Show.Ids.Trakt == show.Ids.Trakt) && s.Show.Ids.Trakt != null) || ((s.Show.Ids.Tvdb == show.Ids.Tvdb) && show.Ids.Tvdb != null)));
        }

        public static int? UserRating(this TraktShow show)
        {
            if (RatedShows == null)
                return null;

            var ratedShow = RatedShows.FirstOrDefault(s => (((s.Show.Ids.Trakt == show.Ids.Trakt) && s.Show.Ids.Trakt != null) || ((s.Show.Ids.Tvdb == show.Ids.Tvdb) && show.Ids.Tvdb != null)));
            if (ratedShow == null)
                return null;

            return ratedShow.Rating;
        }

        public static int Plays(this TraktShow show)
        {
            var watchedEpisodes = TraktCache.WatchedEpisodes;
            if (watchedEpisodes == null)
                return 0;

            // sum up all the plays per episode in show
            return watchedEpisodes.Where(e => (((e.ShowId == show.Ids.Trakt) && e.ShowId != null) || (e.ShowTvdbId == show.Ids.Tvdb) && e.ShowTvdbId != null)).Sum(e => e.Plays);
        }

        public static int Collected(this TraktShow show)
        {
            var collectedEpisodes = TraktCache.CollectedEpisodes;
            if (collectedEpisodes == null)
                return 0;

            // count all the episodes collected in the show
            return collectedEpisodes.Where(e => (((e.ShowId == show.Ids.Trakt) && e.ShowId != null) || (e.ShowTvdbId == show.Ids.Tvdb) && e.ShowTvdbId != null)).Count();
        }

        public static bool IsHidden(this TraktShow show, string section)
        {
            if (HiddenShows == null)
                return false;

            return HiddenShows.Any(s => (((s.Show.Ids.Trakt == show.Ids.Trakt) && s.Show.Ids.Trakt != null) || ((s.Show.Ids.Tvdb == show.Ids.Tvdb) && show.Ids.Tvdb != null)) && s.Section == section);
        }

        #endregion

        #region Seasons

        public static bool IsWatchlisted(this TraktSeasonSummary season, TraktShowSummary show)
        {
            if (WatchListSeasons == null)
                return false;

            return WatchListSeasons.Any(s => ((((s.Show.Ids.Trakt == show.Ids.Trakt) && s.Show.Ids.Trakt != null) || ((s.Show.Ids.Tvdb == show.Ids.Tvdb) && show.Ids.Tvdb != null))) &&
                                                 s.Season.Number == season.Number);
        }

        public static bool IsWatched(this TraktSeasonSummary season, TraktShowSummary show)
        {
            if (season.EpisodeCount == 0 || season.EpisodeAiredCount == 0)
                return false;

            var watchedEpisodes = TraktCache.WatchedEpisodes;
            if (watchedEpisodes == null)
                return false;

            // check that the seasons aired episode count >= episodes watched in season
            // trakt does not include specials in count, nor should we
            int watchedEpisodeCount = watchedEpisodes.Where(e => (((e.ShowId == show.Ids.Trakt) && e.ShowId != null) || ((e.ShowTvdbId == show.Ids.Tvdb) && show.Ids.Tvdb != null)) &&
                                                                    e.Season == season.Number).Count();
            return watchedEpisodeCount >= season.EpisodeAiredCount;
        }

        public static bool IsCollected(this TraktSeasonSummary season, TraktShowSummary show)
        {
            if (season.EpisodeCount == 0 || season.EpisodeAiredCount == 0)
                return false;

            var collectedEpisodes = TraktCache.CollectedEpisodes;
            if (collectedEpisodes == null)
                return false;

            // check that the seasons aired episode count >= episodes watched in season
            // trakt does not include specials in count, nor should we
            int collectedEpisodeCount = collectedEpisodes.Where(e => (((e.ShowId == show.Ids.Trakt) && e.ShowId != null) || ((e.ShowTvdbId == show.Ids.Tvdb) && show.Ids.Tvdb != null)) &&
                                                                        e.Season == season.Number).Count();
            return collectedEpisodeCount >= season.EpisodeAiredCount;
        }

        public static int? UserRating(this TraktSeasonSummary season, TraktShowSummary show)
        {
            if (RatedSeasons == null)
                return null;

            var ratedSeason = RatedSeasons.FirstOrDefault(s => ((((s.Show.Ids.Trakt == show.Ids.Trakt) && s.Show.Ids.Trakt != null) || ((s.Show.Ids.Tvdb == show.Ids.Tvdb) && show.Ids.Tvdb != null))) && 
                                                                 s.Season.Number == season.Number);


            if (ratedSeason == null)
                return null;

            return ratedSeason.Rating;
        }

        public static int Plays(this TraktSeasonSummary season, TraktShowSummary show)
        {
            var watchedEpisodes = TraktCache.WatchedEpisodes;
            if (watchedEpisodes == null)
                return 0;

            // sum up all the plays per episode in season
            return watchedEpisodes.Where(e => e.ShowId == show.Ids.Trakt && e.Season == season.Number).Sum(e => e.Plays);
        }

        public static int Collected(this TraktSeasonSummary season, TraktShowSummary show)
        {
            var collectedEpisodes = TraktCache.CollectedEpisodes;
            if (collectedEpisodes == null)
                return 0;

            // count all the episodes collected in the season
            return collectedEpisodes.Where(e => e.ShowId == show.Ids.Trakt && e.Season == season.Number).Count();
        }

        #endregion

        #region Episodes

        public static bool IsWatched(this TraktEpisode episode, TraktShow show)
        {
            if (WatchedEpisodes == null || show == null)
                return false;

            return WatchedEpisodes.Any(e => (((e.ShowId == show.Ids.Trakt) && e.ShowId != null) || ((e.ShowTvdbId == show.Ids.Tvdb) && show.Ids.Tvdb != null)) &&
                                               e.Season == episode.Season &&
                                               e.Number == episode.Number);
        }

        public static bool IsCollected(this TraktEpisode episode, TraktShow show)
        {
            if (CollectedEpisodes == null || show == null)
                return false;

            return CollectedEpisodes.Any(e => (((e.ShowId == show.Ids.Trakt) && e.ShowId != null) || ((e.ShowTvdbId == show.Ids.Tvdb) && show.Ids.Tvdb != null)) &&
                                                 e.Season == episode.Season &&
                                                 e.Number == episode.Number);
        }

        public static bool IsWatchlisted(this TraktEpisode episode)
        {
            if (WatchListEpisodes == null)
                return false;

            return WatchListEpisodes.Any(w => w.Episode.Ids.Trakt == episode.Ids.Trakt);
        }

        public static bool IsWatchlisted(this TraktEpisode episode, TraktShow show)
        {
            if (WatchListEpisodes == null || show == null)
                return false;

            return WatchListEpisodes.Any(e => (((e.Show.Ids.Trakt == show.Ids.Trakt) && e.Show.Ids.Trakt != null) || ((e.Show.Ids.Tvdb == show.Ids.Tvdb) && show.Ids.Tvdb != null)) &&
                                                  e.Episode.Season == episode.Season &&
                                                  e.Episode.Number == episode.Number);
        }

        public static int? UserRating(this TraktEpisode episode, TraktShow show)
        {
            if (RatedEpisodes == null)
                return null;

            // either match by episode id or if not available in cache (which could occur when added by sync caching) by show id and episode/season numbers
            var ratedEpisode = RatedEpisodes.FirstOrDefault(re => ((re.Episode.Ids.Trakt == episode.Ids.Trakt) && re.Episode.Ids.Trakt != null) ||
                                                                 (((re.Show.Ids.Tvdb == show.Ids.Tvdb) && show.Ids.Tvdb != null) && 
                                                                    re.Episode.Season == episode.Season && re.Episode.Number == episode.Number));
            if (ratedEpisode == null)
                return null;

            return ratedEpisode.Rating;
        }

        public static int Plays(this TraktEpisode episode, TraktShow show)
        {
            if (WatchedEpisodes == null || show == null)
                return 0;

            // we may added a play from a real-time scrobble or added a new play from a sync
            var watchedEpisodes = WatchedEpisodes.Where(e => (((e.ShowId == show.Ids.Trakt) && e.ShowId != null) || ((e.ShowTvdbId == show.Ids.Tvdb) && show.Ids.Tvdb != null)) &&
                                                                e.Season == episode.Season &&
                                                                e.Number == episode.Number);
            if (watchedEpisodes == null)
                return 0;

            return watchedEpisodes.Sum(e => e.Plays);
        }

        #endregion

        #region List Items

        public static int Plays(this TraktListItem item)
        {
            if (item.Type == "movie" && item.Movie != null)
                return item.Movie.Plays();
            if (item.Type == "show" && item.Show != null)
                return item.Show.Plays();
            if (item.Type == "season" && item.Season != null)
                return item.Season.Plays(item.Show);
            else if (item.Type == "episode" && item.Episode != null)
                return item.Episode.Plays(item.Show);

            return 0;
        }

        public static bool IsWatched(this TraktListItem item)
        {
            if (item.Type == "movie" && item.Movie != null)
                return item.Movie.IsWatched();
            if (item.Type == "show" && item.Show != null)
                return item.Show.IsWatched();
            if (item.Type == "season" && item.Season != null)
                return item.Season.IsWatched(item.Show);
            else if (item.Type == "episode" && item.Episode != null)
                return item.Episode.IsWatched(item.Show);

            return false;
        }

        public static bool IsCollected(this TraktListItem item)
        {
            if (item.Type == "movie" && item.Movie != null)
                return item.Movie.IsCollected();
            if (item.Type == "show" && item.Show != null)
                return item.Show.IsCollected();
            if (item.Type == "season" && item.Season != null)
                return item.Season.IsCollected(item.Show);
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
            if (item.Type == "season" && item.Season != null)
                return item.Season.IsWatchlisted(item.Show);
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
            else if (item.Type == "season" && item.Season != null)
                return item.Season.UserRating(item.Show);
            else if (item.Type == "episode" && item.Episode != null)
                return item.Episode.UserRating(item.Show);

            return null;
        }

        #endregion

        #region Lists

        public static bool IsLiked(this TraktListDetail list)
        {
            if (LikedLists == null || list == null || list.Ids == null)
                return false;

            return LikedLists.Any(l => l.List.Ids.Trakt == list.Ids.Trakt);
        }

        #endregion

        #region Comments

        public static bool IsLiked(this TraktComment comment)
        {
            if (LikedComments == null || comment == null)
                return false;

            return LikedComments.Any(l => l.Comment.Id == comment.Id);
        }

        #endregion

        #region Activities

        public static bool IsWatched(this TraktActivity.Activity activity)
        {
            if (activity.Movie != null)
                return activity.Movie.IsWatched();
            else if (activity.Episode != null && activity.Show != null)
                return activity.Episode.IsWatched(activity.Show);
            else if (activity.Episodes != null && activity.Episodes.Count == 1 && activity.Show != null)
                return activity.Episodes.First().IsWatched(activity.Show);
            else if (activity.Show != null)
                return activity.Show.IsWatched();
            else
                return false;
        }

        public static bool IsCollected(this TraktActivity.Activity activity)
        {
            if (activity.Movie != null)
                return activity.Movie.IsCollected();
            else if (activity.Episode != null && activity.Show != null)
                return activity.Episode.IsCollected(activity.Show);
            else if (activity.Episodes != null && activity.Episodes.Count == 1 && activity.Show != null)
                return activity.Episodes.First().IsCollected(activity.Show);
            else if (activity.Show != null)
                return activity.Show.IsCollected();
            else
                return false;
        }

        public static bool IsWatchlisted(this TraktActivity.Activity activity)
        {
            if (activity.Movie != null)
                return activity.Movie.IsWatchlisted();
            else if (activity.Episode != null && activity.Show != null)
                return activity.Episode.IsWatchlisted(activity.Show);
            else if (activity.Episodes != null && activity.Episodes.Count == 1 && activity.Show != null)
                return activity.Episodes.First().IsWatchlisted(activity.Show);
            else if (activity.Season != null)
                return activity.Season.IsWatchlisted(activity.Show);
            else if (activity.Show != null)
                return activity.Show.IsWatchlisted();
            else
                return false;
        }

        public static int Plays(this TraktActivity.Activity activity)
        {
            if (activity.Movie != null)
                return activity.Movie.Plays();
            else if (activity.Episode != null && activity.Show != null)
                return activity.Episode.Plays(activity.Show);
            else if (activity.Episodes != null && activity.Episodes.Count == 1 && activity.Show != null)
                return activity.Episodes.First().Plays(activity.Show);
            else if (activity.Show != null)
                return activity.Show.Plays();
            else
                return 0;
        }

        #endregion

        #region Comments

        public static bool IsWatched(this TraktCommentItem comment)
        {
            if (comment.Movie != null)
                return comment.Movie.IsWatched();
            else if (comment.Episode != null && comment.Show != null)
                return comment.Episode.IsWatched(comment.Show);
            else if (comment.Season != null && comment.Show != null)
                return comment.Season.IsWatched(comment.Show);
            else if (comment.Show != null)
                return comment.Show.IsWatched();
            else
                return false;
        }

        public static bool IsCollected(this TraktCommentItem comment)
        {
            if (comment.Movie != null)
                return comment.Movie.IsCollected();
            else if (comment.Episode != null && comment.Show != null)
                return comment.Episode.IsCollected(comment.Show);
            else if (comment.Season != null && comment.Show != null)
                return comment.Season.IsCollected(comment.Show);
            else if (comment.Show != null)
                return comment.Show.IsCollected();
            else
                return false;
        }

        public static bool IsWatchlisted(this TraktCommentItem comment)
        {
            if (comment.Movie != null)
                return comment.Movie.IsWatchlisted();
            else if (comment.Episode != null && comment.Show != null)
                return comment.Episode.IsWatchlisted(comment.Show);
            else if (comment.Season != null && comment.Show != null)
                return comment.Season.IsWatchlisted(comment.Show);
            else if (comment.Show != null)
                return comment.Show.IsWatchlisted();
            else
                return false;
        }

        public static int Plays(this TraktCommentItem comment)
        {
            if (comment.Movie != null)
                return comment.Movie.Plays();
            else if (comment.Episode != null && comment.Show != null)
                return comment.Episode.Plays(comment.Show);
            else if (comment.Season != null && comment.Show != null)
                return comment.Season.Plays(comment.Show);
            else if (comment.Show != null)
                return comment.Show.Plays();
            else
                return 0;
        }

        public static int? UserRating(this TraktCommentItem comment)
        {
            if (comment.Movie != null)
                return comment.Movie.UserRating();
            else if (comment.Type == "show" && comment.Show != null)
                return comment.Show.UserRating();
            else if (comment.Season != null)
                return comment.Season.UserRating(comment.Show);
            else if (comment.Episode != null)
                return comment.Episode.UserRating(comment.Show);

            return null;
        }

        #endregion

        #endregion

        #region Clear Cache

        internal static void ClearRecommendationsCache()
        {
            lock (syncLists)
            {
                _RecommendedMovies = null;
            }
        }

        internal static void ClearCustomListCache(string listName = null)
        {
            lock (syncLists)
            {
                // check if something to clear
                if (_CustomListsAndItems == null) return;
                    
                // clear all lists
                if (string.IsNullOrEmpty(listName))
                {
                    _CustomListsAndItems = null;
                    return;
                }

                // clear selected list
                var list = _CustomListsAndItems.FirstOrDefault(t => t.Key.Name == listName);
                if (list.Key != null)
                {
                    _CustomListsAndItems.Remove(list.Key);
                }
            }
        }

        static DateTime lastClearedAt = DateTime.MinValue;
        internal static void ClearLastActivityCache(bool force = false)
        {
            // don't be too aggressive at clearing the last activities
            // its possible that we sync paused and library together or
            // enter/exit plugins frequently which enable paused sync
            if (force || lastClearedAt < DateTime.Now.Subtract(new TimeSpan(0, TraktSettings.SyncPlaybackCacheExpiry, 0)))
            {
                _LastSyncActivities = null;

                UnWatchedEpisodes = null;
                UnWatchedMovies = null;

                lastClearedAt = DateTime.Now;
            }
        }

        internal static void ClearLastShowsActivityCache()
        {
            if (_LastSyncActivities != null)
            {
                _LastSyncActivities.Shows = null;
                _LastSyncActivities.Episodes = null;
                _LastSyncActivities.Seasons = null;
            }

            UnWatchedEpisodes = null;
        }

        internal static void ClearLastMoviesActivityCache()
        {
            if (_LastSyncActivities != null)
            {
                _LastSyncActivities.Movies = null;
                _LastSyncActivities.Lists = null;
            }

            UnWatchedMovies = null;
        }

        internal static void ClearSyncCache()
        {
            _RatedEpisodes = null;
            _RatedSeasons = null;
            _RatedMovies = null;
            _RatedShows = null;

            _CollectedEpisodes = null;
            _CollectedMovies = null;

            _WatchedEpisodes = null;
            _WatchedMovies = null;

            _WatchListMovies = null;
            _WatchListShows = null;
            _WatchListSeasons = null;
            _WatchListEpisodes = null;

            _PausedEpisodes = null;
            _PausedMovies = null;

            _CommentedEpisodes = null;
            _CommentedLists = null;
            _CommentedMovies = null;
            _CommentedSeasons = null;
            _CommentedShows = null;

            _LikedComments = null;
            _LikedLists = null;

            _HiddenMovies = null;
            _HiddenSeasons = null;
            _HiddenShows = null;
        }

        #endregion

        #region Add To Cache

        #region Movies

        internal static void AddMoviesToWatchHistory(List<TraktSyncMovieWatched> movies)
        {
            var watchedMovies = (_WatchedMovies ?? new List<TraktMovieWatched>()).ToList();

            watchedMovies.AddRange(
                from movie in movies
                select new TraktMovieWatched
                {
                    LastWatchedAt = movie.WatchedAt ?? DateTime.UtcNow.ToISO8601(),
                    Movie = new TraktMovie
                    {
                        Ids = movie.Ids,
                        Title = movie.Title,
                        Year = movie.Year
                    },
                    Plays = 1
                });

            _WatchedMovies = watchedMovies;
        }

        internal static void AddMovieToWatchHistory(TraktMovie movie)
        {
            var watchedMovies = (_WatchedMovies ?? new List<TraktMovieWatched>()).ToList();

            var existingWatchedMovie = watchedMovies.FirstOrDefault(m => ((m.Movie.Ids.Trakt == movie.Ids.Trakt) && movie.Ids.Trakt != null) ||
                                                                         ((m.Movie.Ids.Imdb == movie.Ids.Imdb) && movie.Ids.Imdb.ToNullIfEmpty() != null) ||
                                                                         ((m.Movie.Ids.Tmdb == movie.Ids.Tmdb) && movie.Ids.Tmdb != null));
            
            // if it exists already, increment the play count only
            if (existingWatchedMovie != null)
            {
                existingWatchedMovie.Plays++;
                existingWatchedMovie.LastWatchedAt = DateTime.UtcNow.ToISO8601();
            }
            else
            {
                watchedMovies.Add(new TraktMovieWatched
                {
                    LastWatchedAt = DateTime.UtcNow.ToISO8601(),
                    Movie = new TraktMovie
                    {
                        Ids = movie.Ids,
                        Title = movie.Title,
                        Year = movie.Year
                    },
                    Plays = 1
                });
            }

            _WatchedMovies = watchedMovies;

            // now remove from watchlist and paused state since it will be removed from online in these cases
            RemoveMovieFromWatchlist(movie);
            RemoveMovieFromPausedData(movie);
        }

        internal static void AddMovieToWatchlist(TraktMovie movie)
        {
            var watchlistMovies = (_WatchListMovies ?? new List<TraktMovieWatchList>()).ToList();

            watchlistMovies.Add(new TraktMovieWatchList
            {
                ListedAt = DateTime.UtcNow.ToISO8601(),
                Movie = new TraktMovieSummary
                {
                    Ids = movie.Ids,
                    Title = movie.Title,
                    Year = movie.Year
                }
            });

            _WatchListMovies = watchlistMovies;
        }

        internal static void AddMoviesToCollection(List<TraktSyncMovieCollected> movies)
        {
            var collectedMovies = (_CollectedMovies ?? new List<TraktMovieCollected>()).ToList();

            collectedMovies.AddRange(
                from movie in movies
                select new TraktMovieCollected
                {
                    CollectedAt = movie.CollectedAt ?? DateTime.UtcNow.ToISO8601(),
                    Movie = new TraktMovie
                    {
                        Ids = movie.Ids,
                        Title = movie.Title,
                        Year = movie.Year
                    }
                });

            _CollectedMovies = collectedMovies;
        }

        internal static void AddMovieToCollection(TraktMovie movie)
        {
            var collectedMovies = (_CollectedMovies ?? new List<TraktMovieCollected>()).ToList();

            collectedMovies.Add(new TraktMovieCollected
            {
                CollectedAt = DateTime.UtcNow.ToISO8601(),
                Movie = new TraktMovieSummary
                {
                    Ids = movie.Ids,
                    Title = movie.Title,
                    Year = movie.Year
                }
            });

            _CollectedMovies = collectedMovies;
        }

        internal static void AddMoviesToRatings(List<TraktSyncMovieRated> movies)
        {
            var ratedMovies = (_RatedMovies ?? new List<TraktMovieRated>()).ToList();

            ratedMovies.AddRange(
                from movie in movies
                select new TraktMovieRated
                {
                    RatedAt = movie.RatedAt ?? DateTime.UtcNow.ToISO8601(),
                    Rating = movie.Rating,
                    Movie = new TraktMovie
                    {
                        Ids = movie.Ids,
                        Title = movie.Title,
                        Year = movie.Year
                    }
                });

            _RatedMovies = ratedMovies;
        }

        internal static void AddMovieToRatings(TraktMovie movie, int rating)
        {
            var ratedMovies = (_RatedMovies ?? new List<TraktMovieRated>()).ToList();

            ratedMovies.Add(new TraktMovieRated
            {
                RatedAt = DateTime.UtcNow.ToISO8601(),
                Rating = rating,
                Movie = new TraktMovie
                {
                    Ids = movie.Ids,
                    Title = movie.Title,
                    Year = movie.Year
                }
            });

            _RatedMovies = ratedMovies;
        }

        internal static void AddMovieToPausedData(TraktMovie movie, float progress)
        {
            var pausedMovies = (_PausedMovies ?? new List<TraktSyncPausedMovie>()).ToList();

            var existingPausedMovie = pausedMovies.FirstOrDefault(m => ((m.Movie.Ids.Trakt == movie.Ids.Trakt) && movie.Ids.Trakt != null) ||
                                                                        ((m.Movie.Ids.Imdb == movie.Ids.Imdb) && movie.Ids.Imdb.ToNullIfEmpty() != null) ||
                                                                        ((m.Movie.Ids.Tmdb == movie.Ids.Tmdb) && movie.Ids.Tmdb != null));
            
            // if it exists already, increment the play count only
            if (existingPausedMovie != null)
            {
                existingPausedMovie.Progress = progress;
                existingPausedMovie.PausedAt = DateTime.UtcNow.ToISO8601();
            }
            else
            {
                pausedMovies.Add(new TraktSyncPausedMovie
                {
                    PausedAt = DateTime.UtcNow.ToISO8601(),
                    Progress = progress,
                    Movie = new TraktMovie
                    {
                        Ids = movie.Ids,
                        Title = movie.Title,
                        Year = movie.Year
                    }
                });
            }

            _PausedMovies = pausedMovies;
        }

        internal static void AddMovieToHiddenData(TraktMovie movie, string section)
        {
            var hiddenMovies = (_HiddenMovies ?? new List<TraktHiddenItem>()).ToList();

            hiddenMovies.Add(new TraktHiddenItem
            {
                HiddenAt = DateTime.UtcNow.ToISO8601(),
                Section = section,
                Movie = new TraktMovieSummary
                {
                    Ids = movie.Ids,
                    Title = movie.Title,
                    Year = movie.Year
                }
            });

            _HiddenMovies = hiddenMovies;
        }

        #endregion

        #region Shows

        internal static void AddShowToWatchedHistory(TraktShow show, List<TraktEpisode> episodes)
        {
            return;
        }

        internal static void AddShowToWatchlist(TraktShow show)
        {
            var watchlistShows = (_WatchListShows ?? new List<TraktShowWatchList>()).ToList();

            watchlistShows.Add(new TraktShowWatchList
            {
                ListedAt = DateTime.UtcNow.ToISO8601(),
                Show = new TraktShowSummary
                {
                    Ids = show.Ids,
                    Title = show.Title,
                    Year = show.Year
                }
            });

            _WatchListShows = watchlistShows;
        }

        internal static void AddShowToCollection(TraktShow show, List<TraktEpisode> episodes)
        {
            return;
        }

        internal static void AddShowsToRatings(List<TraktSyncShowRated> shows)
        {
            var ratedShows = (_RatedShows ?? new List<TraktShowRated>()).ToList();

            ratedShows.AddRange(
                from show in shows
                select new TraktShowRated
                {
                    RatedAt = show.RatedAt ?? DateTime.UtcNow.ToISO8601(),
                    Rating = show.Rating,
                    Show = new TraktShow
                    {
                        Ids = show.Ids,
                        Title = show.Title,
                        Year = show.Year
                    }
                });

            _RatedShows = ratedShows;
        }

        internal static void AddShowToRatings(TraktShow show, int rating)
        {
            var ratedShows = (_RatedShows ?? new List<TraktShowRated>()).ToList();

            ratedShows.Add(new TraktShowRated
            {
                RatedAt = DateTime.UtcNow.ToISO8601(),
                Rating = rating,
                Show = new TraktShow
                {
                    Ids = show.Ids,
                    Title = show.Title,
                    Year = show.Year
                }
            });

            _RatedShows = ratedShows;
        }

        internal static void AddShowToHiddenData(TraktShow show, string section)
        {
            var hiddenShows = (_HiddenShows ?? new List<TraktHiddenItem>()).ToList();

            hiddenShows.Add(new TraktHiddenItem
            {
                HiddenAt = DateTime.UtcNow.ToISO8601(),
                Section = section,
                Show = new TraktShowSummary
                {
                    Ids = show.Ids,
                    Title = show.Title,
                    Year = show.Year
                }
            });

            _HiddenShows = hiddenShows;
        }

        #endregion

        #region Seasons

        internal static void AddSeasonToWatchlist(TraktShow show, TraktSeason season)
        {
            var watchlistSeasons = (_WatchListSeasons ?? new List<TraktSeasonWatchList>()).ToList();

            watchlistSeasons.Add(new TraktSeasonWatchList
            {
                ListedAt = DateTime.UtcNow.ToISO8601(),
                Show = new TraktShowSummary
                {
                    Ids = show.Ids,
                    Title = show.Title,
                    Year = show.Year
                },
                Season = new TraktSeasonSummary
                {
                    Ids = season.Ids,
                    Number = season.Number
                }
            });

            _WatchListSeasons = watchlistSeasons;
        }

        internal static void AddSeasonToRatings(TraktShow show, TraktSeason season, int rating)
        {
            var ratedSeasons = (_RatedSeasons ?? new List<TraktSeasonRated>()).ToList();

            ratedSeasons.Add(new TraktSeasonRated
            {
                RatedAt = DateTime.UtcNow.ToISO8601(),
                Rating = rating,
                Show = new TraktShow
                {
                    Ids = show.Ids,
                    Title = show.Title,
                    Year = show.Year
                },
                Season = new TraktSeason
                {
                    Ids = season.Ids,
                    Number = season.Number
                }
            });

            _RatedSeasons = ratedSeasons;
        }

        internal static void AddSeasonToHiddenData(TraktSeason season, string section)
        {
            var hiddenSeasons = (_HiddenSeasons ?? new List<TraktHiddenItem>()).ToList();

            hiddenSeasons.Add(new TraktHiddenItem
            {
                HiddenAt = DateTime.UtcNow.ToISO8601(),
                Section = section,
                Season = new TraktSeasonSummary
                {
                    Ids = season.Ids,
                    Number = season.Number
                }
            });

            _HiddenSeasons = hiddenSeasons;
        }
        
        #endregion

        #region Episodes

        internal static void AddEpisodesToWatchHistory(TraktSyncShowWatchedEx show)
        {
            var watchedEpisodes = (_WatchedEpisodes ?? new List<EpisodeWatched>()).ToList();
            var episodesToAdd = new List<EpisodeWatched>();

            foreach (var season in show.Seasons)
            {
                foreach (var episode in season.Episodes)
                {
                    episodesToAdd.Add(new EpisodeWatched
                    {
                        Number = episode.Number,
                        Season = season.Number,
                        ShowId = show.Ids.Trakt,
                        ShowImdbId = show.Ids.Imdb,
                        ShowTvdbId = show.Ids.Tvdb,
                        ShowTmdbId = show.Ids.Tmdb,
                        ShowTitle = show.Title,
                        ShowYear = show.Year,
                        Plays = 1,
                        WatchedAt = episode.WatchedAt ?? DateTime.UtcNow.ToISO8601()
                    });
                }
            }

            watchedEpisodes.AddRange(episodesToAdd);

            _WatchedEpisodes = watchedEpisodes;
        }

        internal static void AddEpisodeToWatchHistory(TraktShow show, TraktEpisode episode)
        {
            var watchedEpisodes = (_WatchedEpisodes ?? new List<EpisodeWatched>()).ToList();

            var existingWatchedEpisode = watchedEpisodes.FirstOrDefault(e => (((e.ShowId == show.Ids.Trakt) && e.ShowId != null) || ((e.ShowTvdbId == show.Ids.Tvdb) && e.ShowTvdbId != null)) &&
                                                                                e.Season == episode.Season &&
                                                                                e.Number == episode.Number);

            // if it exists already, increment the play count only
            if (existingWatchedEpisode != null)
            {
                existingWatchedEpisode.Plays++;
                existingWatchedEpisode.WatchedAt = DateTime.UtcNow.ToISO8601();
            }
            else
            {
                watchedEpisodes.Add(new EpisodeWatched
                {
                    Number = episode.Number,
                    Season = episode.Season,
                    ShowId = show.Ids.Trakt,
                    ShowImdbId = show.Ids.Imdb,
                    ShowTvdbId = show.Ids.Tvdb,
                    ShowTmdbId = show.Ids.Tmdb,
                    ShowTitle = show.Title,
                    ShowYear = show.Year,
                    Plays = 1,
                    WatchedAt = DateTime.UtcNow.ToISO8601()
                });
            }

            _WatchedEpisodes = watchedEpisodes;

            // now remove from watchlist and paused state since it will be removed from online in these cases
            RemoveEpisodeFromWatchlist(episode);
            RemoveEpisodeFromPausedData(show, episode);
            RemoveShowFromWatchlist(show);
        }

        internal static void AddEpisodeToWatchlist(TraktShowSummary show, TraktEpisodeSummary episode)
        {
            var watchlistEpisodes = (_WatchListEpisodes ?? new List<TraktEpisodeWatchList>()).ToList();

            watchlistEpisodes.Add(new TraktEpisodeWatchList
            {
                ListedAt = DateTime.UtcNow.ToISO8601(),
                Show = show,
                Episode = episode
            });

            _WatchListEpisodes = watchlistEpisodes;
        }

        internal static void AddEpisodesToCollection(TraktSyncShowCollectedEx show)
        {
            var collectedEpisodes = (_CollectedEpisodes ?? new List<EpisodeCollected>()).ToList();
            var episodesToAdd = new List<EpisodeCollected>();

            foreach (var season in show.Seasons)
            {
                foreach (var episode in season.Episodes)
                {
                    episodesToAdd.Add(new EpisodeCollected
                    {
                        Number = episode.Number,
                        Season = season.Number,
                        ShowId = show.Ids.Trakt,
                        ShowImdbId = show.Ids.Imdb,
                        ShowTvdbId = show.Ids.Tvdb,
                        ShowTmdbId = show.Ids.Tmdb,
                        ShowTitle = show.Title,
                        ShowYear = show.Year,
                        CollectedAt = episode.CollectedAt ?? DateTime.UtcNow.ToISO8601()
                    });
                }
            }

            collectedEpisodes.AddRange(episodesToAdd);

            _CollectedEpisodes = collectedEpisodes;
        }

        internal static void AddEpisodeToCollection(TraktShow show, TraktEpisode episode)
        {
            var collectedEpisodes = (_CollectedEpisodes ?? new List<EpisodeCollected>()).ToList();

            collectedEpisodes.Add(new EpisodeCollected
            {
                Number = episode.Number,
                Season = episode.Season,
                ShowId = show.Ids.Trakt,
                ShowImdbId = show.Ids.Imdb,
                ShowTvdbId = show.Ids.Tvdb,
                ShowTmdbId = show.Ids.Tmdb,
                ShowTitle = show.Title,
                ShowYear = show.Year,
                CollectedAt = DateTime.UtcNow.ToISO8601()
            });

            _CollectedEpisodes = collectedEpisodes;
        }

        internal static void AddEpisodesToRatings(TraktSyncShowRatedEx show)
        {
            var ratedEpisodes = (_RatedEpisodes ?? new List<TraktEpisodeRated>()).ToList();
            var episodesToAdd = new List<TraktEpisodeRated>();

            foreach (var season in show.Seasons)
            {
                foreach (var episode in season.Episodes)
                {
                    episodesToAdd.Add(new TraktEpisodeRated
                    {
                        RatedAt = episode.RatedAt ?? DateTime.UtcNow.ToISO8601(),
                        Rating = episode.Rating,
                        Show = new TraktShow
                        {
                            Ids = show.Ids,
                            Title = show.Title,
                            Year = show.Year
                        },
                        Episode = new TraktEpisode
                        {
                            Ids= new TraktEpisodeId(),
                            Number = episode.Number,
                            Season = season.Number
                        }
                    });
                }
            }

            ratedEpisodes.AddRange(episodesToAdd);

            _RatedEpisodes = ratedEpisodes;
        }

        internal static void AddEpisodeToRatings(TraktShow show, TraktEpisode episode, int rating)
        {
            var ratedEpisodes = (_RatedEpisodes ?? new List<TraktEpisodeRated>()).ToList();

            ratedEpisodes.Add(new TraktEpisodeRated
            {
                RatedAt = DateTime.UtcNow.ToISO8601(),
                Rating = rating,
                Show = new TraktShow
                {
                    Ids = show.Ids,
                    Title = show.Title,
                    Year = show.Year
                },
                Episode = new TraktEpisode
                {
                    Ids = episode.Ids,
                    Number = episode.Number,
                    Season = episode.Season,
                    Title = episode.Title
                }
            });

            _RatedEpisodes = ratedEpisodes;
        }

        internal static void AddEpisodeToPausedData(TraktShow show, TraktEpisode episode, float progress)
        {
            var pausedEpisodes = (_PausedEpisodes ?? new List<TraktSyncPausedEpisode>()).ToList();

            var existingPausedEpisode = pausedEpisodes.FirstOrDefault(e => (((e.Show.Ids.Trakt == show.Ids.Trakt) && e.Show.Ids.Trakt != null) || ((e.Show.Ids.Tvdb == show.Ids.Tvdb) && e.Show.Ids.Tvdb != null)) &&
                                                                              e.Episode.Season == episode.Season &&
                                                                              e.Episode.Number == episode.Number);

            // if it exists already, increment the play count only
            if (existingPausedEpisode != null)
            {
                existingPausedEpisode.Progress = progress;
                existingPausedEpisode.PausedAt = DateTime.UtcNow.ToISO8601();
            }
            else
            {
                pausedEpisodes.Add(new TraktSyncPausedEpisode
                {
                    PausedAt = DateTime.UtcNow.ToISO8601(),
                    Progress = progress,
                    Show = new TraktShow
                    {
                        Ids = show.Ids,
                        Title = show.Title,
                        Year = show.Year
                    },
                    Episode = new TraktEpisode
                    {
                        Ids = episode.Ids,
                        Number = episode.Number,
                        Season = episode.Season,
                        Title = episode.Title
                    }
                });
            }

            _PausedEpisodes = pausedEpisodes;
        }

        #endregion

        #region Comments

        internal static void AddCommentToLikes(TraktComment comment)
        {
            var likedComments = (_LikedComments ?? new List<TraktLike>()).ToList();

            likedComments.Add(new TraktLike
            {
                LikedAt = DateTime.UtcNow.ToISO8601(),
                Comment = comment,
                Type = "comment"
            });

            _LikedComments = likedComments;
        }

        #endregion

        #region List

        internal static void AddListToLikes(TraktListDetail list)
        {
            var likedLists = (_LikedLists ?? new List<TraktLike>()).ToList();

            likedLists.Add(new TraktLike
            {
                LikedAt = DateTime.UtcNow.ToISO8601(),
                List = list,
                Type = "list"
            });

            _LikedLists = likedLists;
        }

        #endregion
        
        #endregion

        #region Remove From Cache

        #region Movies

        internal static void RemoveMoviesFromWatchHistory(List<TraktMovie> movies)
        {
            foreach (var movie in movies)
            {
                RemoveMovieFromWatchHistory(movie);
            }
        }

        internal static void RemoveMovieFromWatchHistory(TraktMovie movie)
        {
            if (_WatchedMovies == null || movie.Ids == null)
                return;

            var watchedMovies = _WatchedMovies.ToList();
            watchedMovies.RemoveAll(m => ((m.Movie.Ids.Trakt == movie.Ids.Trakt) && m.Movie.Ids.Trakt != null) ||
                                         ((m.Movie.Ids.Imdb == movie.Ids.Imdb) && m.Movie.Ids.Imdb.ToNullIfEmpty() != null) ||
                                         ((m.Movie.Ids.Tmdb == movie.Ids.Tmdb) && m.Movie.Ids.Tmdb != null));

            // remove using Title + Year
            if (movie.Ids.Trakt == null && movie.Ids.Imdb.ToNullIfEmpty() == null && movie.Ids.Tmdb == null)
            {
                watchedMovies.RemoveAll(m => m.Movie.Title.ToLowerInvariant() == movie.Title.ToLower() && m.Movie.Year == movie.Year);
            }

            _WatchedMovies = watchedMovies;
        }

        internal static void RemoveMovieFromWatchlist(TraktMovie movie)
        {
            if (_WatchListMovies == null || movie.Ids == null)
                return;

            var watchlistMovies = _WatchListMovies.ToList();
            watchlistMovies.RemoveAll(m => ((m.Movie.Ids.Trakt == movie.Ids.Trakt) && m.Movie.Ids.Trakt != null) ||
                                           ((m.Movie.Ids.Imdb == movie.Ids.Imdb) && m.Movie.Ids.Imdb.ToNullIfEmpty() != null) ||
                                           ((m.Movie.Ids.Tmdb == movie.Ids.Tmdb) && m.Movie.Ids.Tmdb != null));

            // remove using Title + Year
            if (movie.Ids.Trakt == null && movie.Ids.Imdb.ToNullIfEmpty() == null && movie.Ids.Tmdb == null)
            {
                watchlistMovies.RemoveAll(m => m.Movie.Title.ToLowerInvariant() == movie.Title.ToLower() && m.Movie.Year == movie.Year);
            }

            _WatchListMovies = watchlistMovies;
        }

        internal static void RemoveMoviesFromCollection(List<TraktMovie> movies)
        {
            foreach (var movie in movies)
            {
                RemoveMovieFromCollection(movie);
            }
        }

        internal static void RemoveMovieFromCollection(TraktMovie movie)
        {
            if (_CollectedMovies == null || movie.Ids == null)
                return;

            var collectedMovies = _CollectedMovies.ToList();
            collectedMovies.RemoveAll(m => ((m.Movie.Ids.Trakt == movie.Ids.Trakt) && m.Movie.Ids.Trakt != null) ||
                                           ((m.Movie.Ids.Imdb == movie.Ids.Imdb) && m.Movie.Ids.Imdb.ToNullIfEmpty() != null) ||
                                           ((m.Movie.Ids.Tmdb == movie.Ids.Tmdb) && m.Movie.Ids.Tmdb != null));

            // remove using Title + Year
            if (movie.Ids.Trakt == null && movie.Ids.Imdb.ToNullIfEmpty() == null && movie.Ids.Tmdb == null)
            {
                collectedMovies.RemoveAll(m => m.Movie.Title.ToLowerInvariant() == movie.Title.ToLower() && m.Movie.Year == movie.Year);
            }

            _CollectedMovies = collectedMovies;
        }

        internal static void RemoveMoviesFromRatings(List<TraktMovie> movies)
        {
            foreach(var movie in movies)
            {
                RemoveMovieFromRatings(movie);
            }
        }

        internal static void RemoveMovieFromRatings(TraktMovie movie)
        {
            if (_RatedMovies == null || movie.Ids == null)
                return;

            var ratedMovies = _RatedMovies.ToList();
            ratedMovies.RemoveAll(m => ((m.Movie.Ids.Trakt == movie.Ids.Trakt) && m.Movie.Ids.Trakt != null) ||
                                       ((m.Movie.Ids.Imdb == movie.Ids.Imdb) && m.Movie.Ids.Imdb.ToNullIfEmpty() != null) ||
                                       ((m.Movie.Ids.Tmdb == movie.Ids.Tmdb) && m.Movie.Ids.Tmdb != null));

            // remove using Title + Year
            if (movie.Ids.Trakt == null && movie.Ids.Imdb.ToNullIfEmpty() == null && movie.Ids.Tmdb == null)
            {
                ratedMovies.RemoveAll(m => m.Movie.Title.ToLowerInvariant() == movie.Title.ToLower() && m.Movie.Year == movie.Year);
            }

            _RatedMovies = ratedMovies;
        }

        internal static void RemoveMovieFromPausedData(TraktMovie movie)
        {
            if (_PausedMovies == null || movie.Ids == null)
                return;

            var pausedMovies = _PausedMovies.ToList();
            pausedMovies.RemoveAll(m => ((m.Movie.Ids.Trakt == movie.Ids.Trakt) && m.Movie.Ids.Trakt != null) ||
                                        ((m.Movie.Ids.Imdb == movie.Ids.Imdb) && m.Movie.Ids.Imdb.ToNullIfEmpty() != null) ||
                                        ((m.Movie.Ids.Tmdb == movie.Ids.Tmdb) && m.Movie.Ids.Tmdb != null));

            // remove using Title + Year
            if (movie.Ids.Trakt == null && movie.Ids.Imdb.ToNullIfEmpty() == null && movie.Ids.Tmdb == null)
            {
                pausedMovies.RemoveAll(m => m.Movie.Title.ToLowerInvariant() == movie.Title.ToLower() && m.Movie.Year == movie.Year);
            }

            _PausedMovies = pausedMovies;
        }

        internal static void RemoveMovieFromHiddenItems(TraktMovie movie, string section)
        {
            if (_HiddenMovies == null || movie.Ids == null)
                return;

            var hiddenMovies = _HiddenMovies.ToList();
            hiddenMovies.RemoveAll(m => (((m.Movie.Ids.Trakt == movie.Ids.Trakt) && m.Movie.Ids.Trakt != null) ||
                                         ((m.Movie.Ids.Imdb == movie.Ids.Imdb) && m.Movie.Ids.Imdb.ToNullIfEmpty() != null) ||
                                         ((m.Movie.Ids.Tmdb == movie.Ids.Tmdb) && m.Movie.Ids.Tmdb != null)) && m.Section == section);

            // remove using Title + Year
            if (movie.Ids.Trakt == null && movie.Ids.Imdb.ToNullIfEmpty() == null && movie.Ids.Tmdb == null)
            {
                hiddenMovies.RemoveAll(m => (m.Movie.Title.ToLowerInvariant() == movie.Title.ToLower() && m.Movie.Year == movie.Year) && m.Section == section);
            }

            _HiddenMovies = hiddenMovies;
        }

        #endregion

        #region Shows

        internal static void RemoveShowFromWatchedHistory(TraktShow show)
        {
            if (_WatchedEpisodes == null || show.Ids == null)
                return;

            var watchedEpisodes = _WatchedEpisodes.ToList();
            watchedEpisodes.RemoveAll(s => ((s.ShowId == show.Ids.Trakt) && s.ShowId != null) ||
                                           ((s.ShowImdbId == show.Ids.Imdb) && s.ShowImdbId.ToNullIfEmpty() != null) ||
                                           ((s.ShowTvdbId == show.Ids.Tvdb) && s.ShowTvdbId != null));

            // remove using Title + Year
            if (show.Ids.Trakt == null && show.Ids.Imdb.ToNullIfEmpty() == null && show.Ids.Tvdb == null)
            {
                watchedEpisodes.RemoveAll(e => e.ShowTitle.ToLowerInvariant() == show.Title.ToLower() && e.ShowYear == show.Year);
            }

            _WatchedEpisodes = watchedEpisodes;
        }

        internal static void RemoveShowFromWatchlist(TraktShow show)
        {
            if (_WatchListShows == null || show.Ids == null)
                return;

            var watchlistShows = _WatchListShows.ToList();
            watchlistShows.RemoveAll(s => ((s.Show.Ids.Trakt == show.Ids.Trakt) && s.Show.Ids.Trakt != null) ||
                                          ((s.Show.Ids.Imdb == show.Ids.Imdb) && s.Show.Ids.Imdb.ToNullIfEmpty() != null) ||
                                          ((s.Show.Ids.Tvdb == show.Ids.Tvdb) && s.Show.Ids.Tvdb != null));

            // remove using Title + Year
            if (show.Ids.Trakt == null && show.Ids.Imdb.ToNullIfEmpty() == null && show.Ids.Tvdb == null)
            {
                watchlistShows.RemoveAll(s => s.Show.Title.ToLowerInvariant() == show.Title.ToLower() && s.Show.Year == show.Year);
            }

            _WatchListShows = watchlistShows;
        }

        internal static void RemoveShowFromCollection(TraktShow show)
        {
            if (_CollectedEpisodes == null || show.Ids == null)
                return;

            var collectedEpisodes = _CollectedEpisodes.ToList();
            collectedEpisodes.RemoveAll(s => ((s.ShowId == show.Ids.Trakt) && s.ShowId != null) ||
                                             ((s.ShowImdbId == show.Ids.Imdb) && s.ShowImdbId.ToNullIfEmpty() != null) ||
                                             ((s.ShowTvdbId == show.Ids.Tvdb) && s.ShowTvdbId != null));

            // remove using Title + Year
            if (show.Ids.Trakt == null && show.Ids.Imdb.ToNullIfEmpty() == null && show.Ids.Tvdb == null)
            {
                collectedEpisodes.RemoveAll(e => e.ShowTitle.ToLowerInvariant() == show.Title.ToLower() && e.ShowYear == show.Year);
            }

            _CollectedEpisodes = collectedEpisodes;
        }

        internal static void RemoveShowsFromRatings(List<TraktShow> shows)
        {
            foreach (var show in shows)
            {
                RemoveShowFromRatings(show);
            }
        }

        internal static void RemoveShowFromRatings(TraktShow show)
        {
            if (_RatedShows == null || show.Ids == null)
                return;

            var ratedShows = _RatedShows.ToList();
            ratedShows.RemoveAll(s => ((s.Show.Ids.Trakt == show.Ids.Trakt) && s.Show.Ids.Trakt != null) ||
                                      ((s.Show.Ids.Imdb == show.Ids.Imdb) && s.Show.Ids.Imdb.ToNullIfEmpty() != null) ||
                                      ((s.Show.Ids.Tvdb == show.Ids.Tvdb) && s.Show.Ids.Tvdb != null));

            // remove using Title + Year
            if (show.Ids.Trakt == null && show.Ids.Imdb.ToNullIfEmpty() == null && show.Ids.Tvdb == null)
            {
                ratedShows.RemoveAll(s => s.Show.Title.ToLowerInvariant() == show.Title.ToLower() && s.Show.Year == show.Year);
            }

            _RatedShows = ratedShows;
        }

        internal static void RemoveShowFromHiddenItems(TraktShow show, string section)
        {
            if (_HiddenShows == null || show.Ids == null)
                return;

            var hiddenShows = _HiddenShows.ToList();
            hiddenShows.RemoveAll(s => (((s.Show.Ids.Trakt == show.Ids.Trakt) && s.Show.Ids.Trakt != null) ||
                                        ((s.Show.Ids.Imdb == show.Ids.Imdb) && s.Show.Ids.Imdb.ToNullIfEmpty() != null) ||
                                        ((s.Show.Ids.Tmdb == show.Ids.Tmdb) && s.Show.Ids.Tmdb != null)) && s.Section == section);

            // remove using Title + Year
            if (show.Ids.Trakt == null && show.Ids.Imdb.ToNullIfEmpty() == null && show.Ids.Tmdb == null)
            {
                hiddenShows.RemoveAll(s => (s.Show.Title.ToLowerInvariant() == show.Title.ToLower() && s.Show.Year == show.Year) && s.Section == section);
            }

            _HiddenShows = hiddenShows;
        }

        #endregion

        #region Seasons

        internal static void RemoveSeasonFromWatchlist(TraktShow show, TraktSeason season)
        {
            if (_WatchListSeasons == null || show.Ids == null)
                return;

            var watchlistSeasons = _WatchListSeasons.ToList();
            watchlistSeasons.RemoveAll(s => (((s.Show.Ids.Trakt == show.Ids.Trakt) && s.Show.Ids.Trakt != null) ||
                                             ((s.Show.Ids.Imdb == show.Ids.Imdb) && s.Show.Ids.Imdb.ToNullIfEmpty() != null) ||
                                             ((s.Show.Ids.Tvdb == show.Ids.Tvdb) && s.Show.Ids.Tvdb != null)) &&
                                               s.Season.Number == season.Number);

            // remove using Title + Year
            if (show.Ids.Trakt == null && show.Ids.Imdb.ToNullIfEmpty() == null && show.Ids.Tvdb == null)
            {
                watchlistSeasons.RemoveAll(s => s.Show.Title.ToLowerInvariant() == show.Title.ToLower() && s.Show.Year == show.Year &&
                                                s.Season.Number == season.Number);
            }

            _WatchListSeasons = watchlistSeasons;
        }

        internal static void RemoveSeasonFromRatings(TraktShow show, TraktSeason season)
        {
            if (_RatedSeasons == null || show.Ids == null)
                return;

            var ratedSeasons = _RatedSeasons.ToList();
            ratedSeasons.RemoveAll(s => (((s.Show.Ids.Trakt == show.Ids.Trakt) && s.Show.Ids.Trakt != null) ||
                                         ((s.Show.Ids.Imdb == show.Ids.Imdb) && s.Show.Ids.Imdb.ToNullIfEmpty() != null) ||
                                         ((s.Show.Ids.Tvdb == show.Ids.Tvdb) && s.Show.Ids.Tvdb != null)) &&
                                           s.Season.Number == season.Number);

            // remove using Title + Year
            if (show.Ids.Trakt == null && show.Ids.Imdb.ToNullIfEmpty() == null && show.Ids.Tvdb == null)
            {
                ratedSeasons.RemoveAll(s => s.Show.Title.ToLowerInvariant() == show.Title.ToLower() && s.Show.Year == show.Year &&
                                            s.Season.Number == season.Number);
            }

            _RatedSeasons = ratedSeasons;
        }

        internal static void RemoveSeasonFromHiddenItems(TraktSeason season, string section)
        {
            if (_HiddenSeasons == null || season.Ids == null)
                return;

            var hiddenSeasons = _HiddenSeasons.ToList();
            hiddenSeasons.RemoveAll(s => (s.Season.Ids.Trakt == season.Ids.Trakt) && s.Section == section);

            _HiddenSeasons = hiddenSeasons;
        }

        #endregion

        #region Episodes

        internal static void RemoveEpisodesFromWatchHistory(TraktSyncShowEx show)
        {
            if (_WatchedEpisodes == null)
                return;

            foreach (var season in show.Seasons)
            {
                foreach (var episode in season.Episodes)
                {
                    RemoveEpisodeFromWatchHistory(new TraktShow { Ids = show.Ids }, 
                                                  new TraktEpisode { Number = episode.Number, Season = season.Number });
                }
            }
        }

        internal static void RemoveEpisodeFromWatchHistory(TraktShow show, TraktEpisode episode)
        {
            if (_WatchedEpisodes == null || show.Ids == null)
                return;

            var watchedEpisodes = _WatchedEpisodes.ToList();
            watchedEpisodes.RemoveAll(e => (((e.ShowId == show.Ids.Trakt) && e.ShowId != null) || ((e.ShowTvdbId == show.Ids.Tvdb) && e.ShowTvdbId != null)) &&
                                              e.Season == episode.Season && 
                                              e.Number == episode.Number);

            // remove using Title + Year
            if (show.Ids.Trakt == null && show.Ids.Tvdb == null)
            {
                watchedEpisodes.RemoveAll(e => e.ShowTitle.ToLowerInvariant() == show.Title.ToLowerInvariant() && e.ShowYear == show.Year &&
                                               e.Season == episode.Season &&
                                               e.Number == episode.Number);
            }

            _WatchedEpisodes = watchedEpisodes;
        }

        internal static void RemoveEpisodeFromWatchlist(TraktEpisode episode)
        {
            if (_WatchListEpisodes == null || episode.Ids == null)
                return;

            var watchlistEpisodes = _WatchListEpisodes.ToList();
            watchlistEpisodes.RemoveAll(e => ((e.Episode.Ids.Trakt == episode.Ids.Trakt) && e.Episode.Ids.Trakt != null) ||
                                             ((e.Episode.Ids.Imdb == episode.Ids.Imdb) && e.Episode.Ids.Imdb != null) ||
                                             ((e.Episode.Ids.Tvdb == episode.Ids.Tvdb && e.Episode.Ids.Tvdb != null)));

            _WatchListEpisodes = watchlistEpisodes;
        }

        internal static void RemoveEpisodeFromWatchlist(TraktShow show, TraktEpisode episode)
        {
            if (_WatchListEpisodes == null || show.Ids == null)
                return;

            var watchlistEpisodes = _WatchListEpisodes.ToList();
            watchlistEpisodes.RemoveAll(e => (((e.Show.Ids.Trakt == show.Ids.Trakt) && e.Show.Ids.Trakt != null) || ((e.Show.Ids.Tvdb == show.Ids.Tvdb) && e.Show.Ids.Tvdb != null)) &&
                                                e.Episode.Season == episode.Season &&
                                                e.Episode.Number == episode.Number);

            // remove using Title + Year
            if (show.Ids.Trakt == null && show.Ids.Tvdb == null)
            {
                watchlistEpisodes.RemoveAll(e => e.Show.Title.ToLowerInvariant() == show.Title.ToLowerInvariant() && e.Show.Year == show.Year &&
                                                 e.Episode.Season == episode.Season &&
                                                 e.Episode.Number == episode.Number);
            }

            _WatchListEpisodes = watchlistEpisodes;
        }

        internal static void RemoveEpisodesFromCollection(TraktSyncShowEx show)
        {
            if (_CollectedEpisodes == null)
                return;

            foreach (var season in show.Seasons)
            {
                foreach (var episode in season.Episodes)
                {
                    RemoveEpisodeFromCollection(new TraktShow { Ids = show.Ids },
                                                new TraktEpisode { Number = episode.Number, Season = season.Number });
                }
            }
        }

        internal static void RemoveEpisodeFromCollection(TraktShow show, TraktEpisode episode)
        {
            if (_CollectedEpisodes == null || show.Ids == null)
                return;

            var collectedEpisodes = _CollectedEpisodes.ToList();
            collectedEpisodes.RemoveAll(e => (((e.ShowId == show.Ids.Trakt) && e.ShowId != null) || ((e.ShowTvdbId == show.Ids.Tvdb) && e.ShowTvdbId != null)) &&
                                                e.Season == episode.Season &&
                                                e.Number == episode.Number);

            // remove using Title + Year
            if (show.Ids.Trakt == null && show.Ids.Tvdb == null)
            {
                collectedEpisodes.RemoveAll(e => e.ShowTitle.ToLowerInvariant() == show.Title.ToLowerInvariant() && e.ShowYear == show.Year &&
                                                 e.Season == episode.Season &&
                                                 e.Number == episode.Number);
            }

            _CollectedEpisodes = collectedEpisodes;
        }

        internal static void RemoveEpisodeFromRatings(TraktEpisode episode)
        {
            if (_RatedEpisodes == null || episode.Ids == null)
                return;

            var ratedEpisodes = _RatedEpisodes.ToList();
            ratedEpisodes.RemoveAll(e => ((e.Episode.Ids.Trakt == episode.Ids.Trakt) && e.Episode.Ids.Trakt != null) ||
                                         ((e.Episode.Ids.Imdb == episode.Ids.Imdb) && e.Episode.Ids.Imdb.ToNullIfEmpty() != null) ||
                                         ((e.Episode.Ids.Tvdb == episode.Ids.Tvdb) && e.Episode.Ids.Tvdb != null));

            _RatedEpisodes = ratedEpisodes;
        }

        internal static void RemoveEpisodeFromPausedData(TraktShow show, TraktEpisode episode)
        {
            if (_PausedEpisodes == null || show.Ids == null)
                return;

            var pausedEpisodes = _PausedEpisodes.ToList();
            pausedEpisodes.RemoveAll(e => (((e.Show.Ids.Trakt == show.Ids.Trakt) && e.Show.Ids.Trakt != null) || ((e.Show.Ids.Tvdb == show.Ids.Tvdb) && e.Show.Ids.Tvdb != null)) &&
                                             e.Episode.Season == episode.Season &&
                                             e.Episode.Number == episode.Number);

            // remove using Title + Year
            if (show.Ids.Trakt == null && show.Ids.Tvdb == null)
            {
                pausedEpisodes.RemoveAll(e => e.Show.Title.ToLowerInvariant() == show.Title.ToLowerInvariant() && e.Show.Year == show.Year &&
                                              e.Episode.Season == episode.Season &&
                                              e.Episode.Number == episode.Number);
            }

            _PausedEpisodes = pausedEpisodes;
        }

        #endregion

        #region Comments

        internal static void RemoveCommentFromLikes(TraktComment comment)
        {
            if (_LikedComments == null)
                return;

            var likedComments = _LikedComments.ToList();
            likedComments.RemoveAll(l => l.Comment.Id == comment.Id);

            _LikedComments = likedComments;
        }

        #endregion

        #region Lists

        internal static void RemoveListFromLikes(TraktListDetail list)
        {
            if (_LikedLists == null)
                return;

            var likedLists = _LikedLists.ToList();
            likedLists.RemoveAll(l => l.List.Ids.Trakt == list.Ids.Trakt);

            _LikedLists = likedLists;
        }

        #endregion
        
        #endregion

        #region Save Cache

        /// <summary>
        /// Save cached data which gets updated from real-time events
        /// </summary>
        internal static void Save()
        {
            SaveFileCache(MoviesWatchlistedFile, _WatchListMovies.ToJSON());
            SaveFileCache(MoviesCollectedFile, _CollectedMovies.ToJSON());
            SaveFileCache(MoviesWatchedFile, _WatchedMovies.ToJSON());
            SaveFileCache(MoviesRatedFile, _RatedMovies.ToJSON());
            SaveFileCache(MoviesPausedFile, _PausedMovies.ToJSON());
            SaveFileCache(MoviesCommentedFile, _CommentedMovies.ToJSON());
            SaveFileCache(MoviesHiddenFile, _HiddenMovies.ToJSON());

            SaveFileCache(EpisodesWatchlistedFile, _WatchListEpisodes.ToJSON());
            SaveFileCache(EpisodesCollectedFile, _CollectedEpisodes.ToJSON());
            SaveFileCache(EpisodesWatchedFile, _WatchedEpisodes.ToJSON());
            SaveFileCache(EpisodesRatedFile, _RatedEpisodes.ToJSON());
            SaveFileCache(EpisodesPausedFile, _PausedEpisodes.ToJSON());
            SaveFileCache(EpisodesCommentedFile, _CommentedEpisodes.ToJSON());

            SaveFileCache(ShowsWatchlistedFile, _WatchListShows.ToJSON());
            SaveFileCache(ShowsRatedFile, _RatedShows.ToJSON());
            SaveFileCache(ShowsCommentedFile, _CommentedShows.ToJSON());
            SaveFileCache(ShowsHiddenFile, _HiddenShows.ToJSON());

            SaveFileCache(SeasonsWatchlistedFile, _WatchListSeasons.ToJSON());
            SaveFileCache(SeasonsRatedFile, _RatedSeasons.ToJSON());
            SaveFileCache(SeasonsCommentedFile, _CommentedSeasons.ToJSON());
            SaveFileCache(SeasonsHiddenFile, _HiddenSeasons.ToJSON());

            SaveFileCache(CustomListCommentedFile, _CommentedLists.ToJSON());
            SaveFileCache(CustomListLikedFile, _LikedLists.ToJSON());

            SaveFileCache(CommentsLikedFile, _LikedComments.ToJSON());
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
            public int? ShowTmdbId { get; set; }
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

            [DataMember]
            public string WatchedAt { get; set; }
        }

        [DataContract]
        public class EpisodeCollected : Episode
        {
            [DataMember]
            public string CollectedAt { get; set; }
        }

        [DataContract]
        public class ListActivity
        {
            [DataMember]
            public int? Id { get; set; }

            [DataMember]
            public string UpdatedAt { get; set; }
        }

        #endregion
    }
}
