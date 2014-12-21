using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using TraktPlugin.GUI;
using TraktPlugin.TraktAPI;
using TraktPlugin.TraktAPI.DataStructures;
using TraktPlugin.TraktAPI.Enums;
using TraktPlugin.TraktAPI.Extensions;
using MediaPortal.Player;
using MediaPortal.Configuration;
using System.Reflection;
using System.ComponentModel;
using MyFilmsPlugin.MyFilms;
using MyFilmsPlugin.MyFilms.MyFilmsGUI;
using System.Threading;

namespace TraktPlugin.TraktHandlers
{
    class MyFilmsHandler : ITraktHandler
    {
        #region Variables

        MFMovie CurrentMovie = null;
        bool SyncInProgress = false;

        #endregion

        #region Constructor

        public MyFilmsHandler(int priority)
        {
            // check if plugin exists otherwise plugin could accidently get added to list
            string pluginFilename = Path.Combine(Config.GetSubFolder(Config.Dir.Plugins, "Windows"), "MyFilms.dll");
            if (!File.Exists(pluginFilename))
                throw new FileNotFoundException("Plugin not found!");
            else
            {
                FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(pluginFilename);
                string version = fvi.ProductVersion;
                if (new Version(version) < new Version(6,0,0,2616))
                    throw new FileLoadException("Plugin does not meet the minimum requirements!");
            }

            // Subscribe to Events
            TraktLogger.Debug("Adding Hooks to My Films");
            MyFilmsDetail.RateItem += new MyFilmsDetail.RatingEventDelegate(OnRateItem);
            MyFilmsDetail.WatchedItem += new MyFilmsDetail.WatchedEventDelegate(OnToggleWatched);
            MyFilmsDetail.MovieStarted += new MyFilmsDetail.MovieStartedEventDelegate(OnStartedMovie);
            MyFilmsDetail.MovieStopped += new MyFilmsDetail.MovieStoppedEventDelegate(OnStoppedMovie);
            MyFilmsDetail.MovieWatched += new MyFilmsDetail.MovieWatchedEventDelegate(OnWatchedMovie);
            MyFilms.ImportComplete += new MyFilms.ImportCompleteEventDelegate(OnImportComplete);

            Priority = priority;
        }

        #endregion

        #region ITraktHandler

        public string Name
        {
            get { return "My Films"; }
        }

        public int Priority { get; set; }
       
        public void SyncLibrary()
        {
            TraktLogger.Info("My Films Starting Sync");
            SyncInProgress = true;

            ArrayList myvideos = new ArrayList();

            #region Get online data from trakt.tv

            #region Get unwatched movies from trakt.tv
            TraktLogger.Info("Getting user {0}'s unwatched movies from trakt", TraktSettings.Username);
            var traktUnWatchedMovies = TraktCache.GetUnWatchedMoviesFromTrakt();
            TraktLogger.Info("There are {0} unwatched movies since the last sync with trakt.tv", (traktUnWatchedMovies ?? new List<TraktMovie>()).Count());
            #endregion

            #region Get watched movies from trakt.tv
            TraktLogger.Info("Getting user {0}'s watched movies from trakt", TraktSettings.Username);
            var traktWatchedMovies = TraktCache.GetWatchedMoviesFromTrakt();
            if (traktWatchedMovies == null)
            {
                SyncInProgress = false;
                TraktLogger.Error("Error getting watched movies from trakt server, cancelling sync");
                return;
            }
            TraktLogger.Info("There are {0} watched movies in trakt.tv library", traktWatchedMovies.Count().ToString());
            #endregion

            #region Get collected movies from trakt.tv
            TraktLogger.Info("Getting user {0}'s collected movies from trakt", TraktSettings.Username);
            var traktCollectedMovies = TraktCache.GetCollectedMoviesFromTrakt();
            if (traktCollectedMovies == null)
            {
                SyncInProgress = false;
                TraktLogger.Error("Error getting collected movies from trakt server, cancelling sync");
                return;
            }
            TraktLogger.Info("There are {0} collected movies in trakt.tv library", traktCollectedMovies.Count());
            #endregion

            #region Get rated movies from trakt.tv
            var traktRatedMovies = new List<TraktMovieRated>();
            if (TraktSettings.SyncRatings)
            {
                TraktLogger.Info("Getting user {0}'s rated movies from trakt", TraktSettings.Username);
                var temp = TraktCache.GetRatedMoviesFromTrakt();
                if (traktRatedMovies == null)
                {
                    SyncInProgress = false;
                    TraktLogger.Error("Error getting rated movies from trakt server, cancelling sync");
                    return;
                }
                traktRatedMovies.AddRange(temp);
                TraktLogger.Info("There are {0} rated movies in trakt.tv library", traktRatedMovies.Count);
            }
            #endregion

            #endregion

            // optionally do library sync
            if (TraktSettings.SyncLibrary)
            {
                // get all movies
                BaseMesFilms.GetMovies(ref myvideos);
                var collectedMovies = (from MFMovie movie in myvideos select movie).ToList();

                // Remove any blocked movies
                collectedMovies.RemoveAll(movie => TraktSettings.BlockedFolders.Any(f => movie.File.ToLowerInvariant().Contains(f.ToLowerInvariant())));
                collectedMovies.RemoveAll(movie => TraktSettings.BlockedFilenames.Contains(movie.File));

                #region Skipped Movies Check
                // Remove Skipped Movies from previous Sync
                //TODO
                //if (TraktSettings.SkippedMovies != null)
                //{
                //    // allow movies to re-sync again after 7-days in the case user has addressed issue ie. edited movie or added to themoviedb.org
                //    if (TraktSettings.SkippedMovies.LastSkippedSync.FromEpoch() > DateTime.UtcNow.Subtract(new TimeSpan(7, 0, 0, 0)))
                //    {
                //        if (TraktSettings.SkippedMovies.Movies != null && TraktSettings.SkippedMovies.Movies.Count > 0)
                //        {
                //            TraktLogger.Info("Skipping {0} movies due to invalid data or movies don't exist on http://themoviedb.org. Next check will be {1}.", TraktSettings.SkippedMovies.Movies.Count, TraktSettings.SkippedMovies.LastSkippedSync.FromEpoch().Add(new TimeSpan(7, 0, 0, 0)));
                //            foreach (var movie in TraktSettings.SkippedMovies.Movies)
                //            {
                //                TraktLogger.Info("Skipping movie, Title: {0}, Year: {1}, IMDb: {2}", movie.Title, movie.Year, movie.IMDBID);
                //                MovieList.RemoveAll(m => (m.Title == movie.Title) && (m.Year.ToString() == movie.Year) && (m.IMDBNumber == movie.IMDBID));
                //            }
                //        }
                //    }
                //    else
                //    {
                //        if (TraktSettings.SkippedMovies.Movies != null) TraktSettings.SkippedMovies.Movies.Clear();
                //        TraktSettings.SkippedMovies.LastSkippedSync = DateTime.UtcNow.ToEpoch();
                //    }
                //}
                #endregion

                #region Already Exists Movie Check
                // Remove Already-Exists Movies, these are typically movies that are using aka names and no IMDb/TMDb set
                // When we compare our local collection with trakt collection we have english only titles, so if no imdb/tmdb exists
                // we need to fallback to title matching. When we sync aka names are sometimes accepted if defined on themoviedb.org so we need to 
                // do this to revent syncing these movies every sync interval.
                //TODO
                //if (TraktSettings.AlreadyExistMovies != null && TraktSettings.AlreadyExistMovies.Movies != null && TraktSettings.AlreadyExistMovies.Movies.Count > 0)
                //{
                //    TraktLogger.Debug("Skipping {0} movies as they already exist in trakt library but failed local match previously.", TraktSettings.AlreadyExistMovies.Movies.Count.ToString());
                //    var movies = new List<TraktMovieSync.Movie>(TraktSettings.AlreadyExistMovies.Movies);
                //    foreach (var movie in movies)
                //    {
                //        Predicate<MFMovie> criteria = m => (m.Title == movie.Title) && (m.Year.ToString() == movie.Year) && (m.IMDBNumber == movie.IMDBID);
                //        if (MovieList.Exists(criteria))
                //        {
                //            TraktLogger.Debug("Skipping movie, Title: {0}, Year: {1}, IMDb: {2}", movie.Title, movie.Year, movie.IMDBID);
                //            MovieList.RemoveAll(criteria);
                //        }
                //        else
                //        {
                //            // remove as we have now removed from our local collection or updated movie signature
                //            if (TraktSettings.MoviePluginCount == 1)
                //            {
                //                TraktLogger.Debug("Removing 'AlreadyExists' movie, Title: {0}, Year: {1}, IMDb: {2}", movie.Title, movie.Year, movie.IMDBID);
                //                TraktSettings.AlreadyExistMovies.Movies.Remove(movie);
                //            }
                //        }
                //    }
                //}
                #endregion

                TraktLogger.Info("Found {0} movies available to sync in My Films database", collectedMovies.Count);

                // get the movies that we have watched
                var watchedMovies = collectedMovies.Where(m => m.Watched == true).ToList();
                TraktLogger.Info("Found {0} watched movies available to sync in My Films database", watchedMovies.Count);

                // get the movies that we have rated/unrated
                var ratedMovies = collectedMovies.Where(m => m.RatingUser > 0).ToList();
                TraktLogger.Info("Found {0} rated movies available to sync in My Films database", ratedMovies.Count);

                // clear the last time(s) we did anything online
                TraktCache.ClearLastActivityCache();
                
                #region Mark movies as unwatched in local database
                if (traktUnWatchedMovies != null && traktUnWatchedMovies.Count() > 0)
                {
                    foreach (var movie in traktUnWatchedMovies)
                    {
                        var localMovie = watchedMovies.FirstOrDefault(m => MovieMatch(m, movie));
                        if (localMovie == null) continue;

                        TraktLogger.Info("Marking movie as unwatched in local database, movie is not watched on trakt.tv. Title = '{0}', Year = '{1}', IMDb ID = '{2}', TMDb ID = '{3}'",
                                          movie.Title, movie.Year.HasValue ? movie.Year.ToString() : "<empty>", movie.Ids.Imdb ?? "<empty>", movie.Ids.Tmdb.HasValue ? movie.Ids.Tmdb.ToString() : "<empty>");

                        localMovie.Watched = false;
                        localMovie.Username = TraktSettings.Username;
                        localMovie.Commit();
                    }

                    // update watched set
                    watchedMovies = collectedMovies.Where(m => m.Watched == true).ToList();
                }
                #endregion

                #region Mark movies as watched in local database
                if (traktWatchedMovies.Count() > 0)
                {
                    foreach (var twm in traktWatchedMovies)
                    {
                        var localMovie = collectedMovies.FirstOrDefault(m => MovieMatch(m, twm.Movie));
                        if (localMovie == null) continue;

                        if (!localMovie.Watched || localMovie.WatchedCount < twm.Plays)
                        {
                            TraktLogger.Info("Updating local movie watched state / play count to match trakt.tv. Plays = '{0}', Title = '{1}', Year = '{2}', IMDb ID = '{3}', TMDb ID = '{4}'",
                                              twm.Plays, twm.Movie.Title, twm.Movie.Year.HasValue ? twm.Movie.Year.ToString() : "<empty>", twm.Movie.Ids.Imdb ?? "<empty>", twm.Movie.Ids.Tmdb.HasValue ? twm.Movie.Ids.Tmdb.ToString() : "<empty>");

                            localMovie.Watched = true;
                            localMovie.WatchedCount = twm.Plays;
                            localMovie.Username = TraktSettings.Username;
                            localMovie.Commit();
                        }
                    }
                }
                #endregion

                #region Add movies to watched history at trakt.tv
                var syncWatchedMovies = new List<TraktSyncMovieWatched>();
                TraktLogger.Info("Finding movies to add to trakt.tv watched history");

                syncWatchedMovies = (from movie in watchedMovies
                                     where !traktWatchedMovies.ToList().Exists(c => MovieMatch(movie, c.Movie))
                                     select new TraktSyncMovieWatched
                                     {
                                         Ids = new TraktMovieId { Imdb = movie.IMDBNumber, Tmdb = movie.TMDBNumber.ToNullableInt32() },
                                         Title = movie.Title,
                                         Year = movie.Year,
                                         WatchedAt = DateTime.UtcNow.ToISO8601(),
                                     }).ToList();

                TraktLogger.Info("Adding {0} movies to trakt.tv watched history", syncWatchedMovies.Count);

                if (syncWatchedMovies.Count > 0)
                {
                    int pageSize = TraktSettings.SyncBatchSize;
                    int pages = (int)Math.Ceiling((double)syncWatchedMovies.Count / pageSize);
                    for (int i = 0; i < pages; i++)
                    {
                        TraktLogger.Info("Adding movies [{0}/{1}] to trakt.tv watched history", i + 1, pages);

                        var pagedMovies = syncWatchedMovies.Skip(i * pageSize).Take(pageSize).ToList();

                        pagedMovies.ForEach(s => TraktLogger.Info("Adding movie to trakt.tv watched history. Title = '{0}', Year = '{1}', IMDb ID = '{2}', TMDb ID = '{3}', Date Watched = '{4}'",
                                                                         s.Title, s.Year.HasValue ? s.Year.ToString() : "<empty>", s.Ids.Imdb ?? "<empty>", s.Ids.Tmdb.HasValue ? s.Ids.Tmdb.ToString() : "<empty>", s.WatchedAt));

                        var response = TraktAPI.TraktAPI.AddMoviesToWatchedHistory(new TraktSyncMoviesWatched { Movies = pagedMovies });
                        TraktLogger.LogTraktResponse<TraktSyncResponse>(response);
                    }
                }
                #endregion

                #region Add movies to collection at trakt.tv
                var syncCollectedMovies = new List<TraktSyncMovieCollected>();
                TraktLogger.Info("Finding movies to add to trakt.tv collection");

                syncCollectedMovies = (from movie in collectedMovies
                                       where !traktCollectedMovies.ToList().Exists(c => MovieMatch(movie, c.Movie))
                                       select new TraktSyncMovieCollected
                                       {
                                           Ids = new TraktMovieId { Imdb = movie.IMDBNumber, Tmdb = movie.TMDBNumber.ToNullableInt32() },
                                           Title = movie.Title,
                                           Year = movie.Year,
                                           CollectedAt = movie.DateAdded.ToISO8601(),
                                           MediaType = null,
                                           Resolution = null,
                                           AudioCodec = null,
                                           AudioChannels = null,
                                           Is3D = false
                                       }).ToList();

                TraktLogger.Info("Adding {0} movies to trakt.tv collection", syncCollectedMovies.Count);

                if (syncCollectedMovies.Count > 0)
                {
                    int pageSize = TraktSettings.SyncBatchSize;
                    int pages = (int)Math.Ceiling((double)syncCollectedMovies.Count / pageSize);
                    for (int i = 0; i < pages; i++)
                    {
                        TraktLogger.Info("Adding movies [{0}/{1}] to trakt.tv collection", i + 1, pages);

                        var pagedMovies = syncCollectedMovies.Skip(i * pageSize).Take(pageSize).ToList();

                        pagedMovies.ForEach(s => TraktLogger.Info("Adding movie to trakt.tv collection. Title = '{0}', Year = '{1}', IMDb ID = '{2}', TMDb ID = '{3}', Date Added = '{4}', MediaType = '{5}', Resolution = '{6}', Audio Codec = '{7}', Audio Channels = '{8}'",
                                                                    s.Title, s.Year.HasValue ? s.Year.ToString() : "<empty>", s.Ids.Imdb ?? "<empty>", s.Ids.Tmdb.HasValue ? s.Ids.Tmdb.ToString() : "<empty>",
                                                                    s.CollectedAt, s.MediaType ?? "<empty>", s.Resolution ?? "<empty>", s.AudioCodec ?? "<empty>", s.AudioChannels ?? "<empty>"));

                        var response = TraktAPI.TraktAPI.AddMoviesToCollecton(new TraktSyncMoviesCollected { Movies = pagedMovies });
                        TraktLogger.LogTraktResponse(response);
                    }
                }
                #endregion

                #region Remove movies no longer in collection from trakt.tv
                if (TraktSettings.KeepTraktLibraryClean && TraktSettings.MoviePluginCount == 1)
                {
                    var syncUnCollectedMovies = new List<TraktMovie>();
                    TraktLogger.Info("Finding movies to remove from trakt.tv collection");

                    // workout what movies that are in trakt collection that are not in local collection
                    syncUnCollectedMovies = (from tcm in traktCollectedMovies
                                             where !collectedMovies.Exists(c => MovieMatch(c, tcm.Movie))
                                             select new TraktMovie { Ids = tcm.Movie.Ids }).ToList();

                    TraktLogger.Info("Removing {0} movies from trakt.tv collection", syncUnCollectedMovies.Count);

                    if (syncUnCollectedMovies.Count > 0)
                    {
                        int pageSize = TraktSettings.SyncBatchSize;
                        int pages = (int)Math.Ceiling((double)syncUnCollectedMovies.Count / pageSize);
                        for (int i = 0; i < pages; i++)
                        {
                            TraktLogger.Info("Removing movies [{0}/{1}] from trakt.tv collection", i + 1, pages);

                            var pagedMovies = syncUnCollectedMovies.Skip(i * pageSize).Take(pageSize).ToList();

                            pagedMovies.ForEach(s => TraktLogger.Info("Removing movie from trakt.tv collection, movie no longer exists locally. Title = '{0}', Year = '{1}', IMDb ID = '{2}', TMDb ID = '{3}'",
                                                                        s.Title, s.Year.HasValue ? s.Year.ToString() : "<empty>", s.Ids.Imdb ?? "<empty>", s.Ids.Tmdb.HasValue ? s.Ids.Tmdb.ToString() : "<empty>"));

                            var response = TraktAPI.TraktAPI.RemoveMoviesFromCollecton(new TraktSyncMovies { Movies = pagedMovies });
                            TraktLogger.LogTraktResponse(response);
                        }
                    }
                }
                #endregion

                #region Add movie ratings to trakt.tv
                if (TraktSettings.SyncRatings)
                {
                    var syncRatedMovies = new List<TraktSyncMovieRated>();
                    TraktLogger.Info("Finding movies to add to trakt.tv ratings");

                    syncRatedMovies = (from movie in ratedMovies
                                       where !traktRatedMovies.ToList().Exists(c => MovieMatch(movie, c.Movie))
                                       select new TraktSyncMovieRated
                                       {
                                           Ids = new TraktMovieId { Imdb = movie.IMDBNumber, Tmdb = movie.TMDBNumber.ToNullableInt32() },
                                           Title = movie.Title,
                                           Year = movie.Year,
                                           Rating = Convert.ToInt32(Math.Round(Convert.ToDecimal(movie.RatingUser), MidpointRounding.AwayFromZero)),
                                           RatedAt = null,
                                       }).ToList();

                    TraktLogger.Info("Adding {0} movies to trakt.tv ratings", syncRatedMovies.Count);

                    if (syncRatedMovies.Count > 0)
                    {
                        int pageSize = TraktSettings.SyncBatchSize;
                        int pages = (int)Math.Ceiling((double)syncRatedMovies.Count / pageSize);
                        for (int i = 0; i < pages; i++)
                        {
                            TraktLogger.Info("Adding movies [{0}/{1}] to trakt.tv ratings", i + 1, pages);

                            var pagedMovies = syncRatedMovies.Skip(i * pageSize).Take(pageSize).ToList();

                            pagedMovies.ForEach(a => TraktLogger.Info("Adding movie to trakt.tv ratings. Title = '{0}', Year = '{1}', IMDb ID = '{2}', TMDb ID = '{3}', Rating = '{4}/10'",
                                                                        a.Title, a.Year.HasValue ? a.Year.ToString() : "<empty>", a.Ids.Imdb ?? "<empty>", a.Ids.Tmdb.HasValue ? a.Ids.Tmdb.ToString() : "<empty>", a.Rating));

                            var response = TraktAPI.TraktAPI.AddMoviesToRatings(new TraktSyncMoviesRated { Movies = pagedMovies });
                            TraktLogger.LogTraktResponse(response);
                        }
                    }
                }
                #endregion       

                #region Rate movies not rated in local database
                if (TraktSettings.SyncRatings && traktRatedMovies.Count > 0)
                {
                    foreach (var trm in traktRatedMovies)
                    {
                        var localMovie = collectedMovies.FirstOrDefault(m => MovieMatch(m, trm.Movie));
                        if (localMovie == null) continue;

                        int currentRating = Convert.ToInt32(Math.Round(Convert.ToDecimal(localMovie.RatingUser), MidpointRounding.AwayFromZero));

                        if (currentRating != trm.Rating)
                        {
                            TraktLogger.Info("Adding movie rating to match trakt.tv. Rated = '{0}/10', Title = '{1}', Year = '{2}', IMDb ID = '{3}', TMDb ID = '{4}'",
                                              trm.Rating, trm.Movie.Title, trm.Movie.Year.HasValue ? trm.Movie.Year.ToString() : "<empty>", trm.Movie.Ids.Imdb ?? "<empty>", trm.Movie.Ids.Tmdb.HasValue ? trm.Movie.Ids.Tmdb.ToString() : "<empty>");

                            localMovie.RatingUser = trm.Rating;
                            localMovie.Username = TraktSettings.Username; 
                            localMovie.Commit();
                        }
                    }
                }
                #endregion
            }

            #region Trakt Category Tags
            // add tags also to blocked movies, as it is only local
            var allMovies = (from MFMovie movie in myvideos select movie).ToList(); 
            // get the movies that locally have trakt categories
            var categoryTraktList = allMovies.Where(m => m.CategoryTrakt.Count > 0).ToList();

            if (TraktSettings.MyFilmsCategories)
            {
                TraktLogger.Info("Found {0} trakt-categorised movies available in My Films database", categoryTraktList.Count.ToString());

                #region Update Watchlist Tags
                var traktWatchListMovies = TraktCache.GetWatchlistedMoviesFromTrakt();

                if (traktWatchListMovies != null)
                {
                    string category = Translation.WatchList;
                    TraktLogger.Info("Retrieved {0} watchlist items from trakt", traktWatchListMovies.Count());

                    var cleanupList = allMovies.Where(m => m.CategoryTrakt.Contains(category)).ToList();
                    foreach (var trm in traktWatchListMovies)
                    {
                        TraktLogger.Debug("Processing trakt watchlist movie. Title = '{0}', Year = '{1}' = IMDb ID '{2}', TMDb ID = '{3}'", trm.Movie.Title ?? "<empty>", trm.Movie.Year.HasValue ? trm.Movie.Year.ToString() : "<empty>", trm.Movie.Ids.Imdb ?? "<empty>", trm.Movie.Ids.Tmdb.HasValue ? trm.Movie.Ids.Tmdb.ToString() : "<empty>");
                        foreach (var movie in allMovies.Where(m => MovieMatch(m, trm.Movie)))
                        {
                            if (!movie.CategoryTrakt.Contains(category))
                            {
                                TraktLogger.Info("Inserting trakt category for movie. Category = '{0}', Title = '{1}', Year = '{2}', IMDb ID = '{3}', TMDb ID = '{4}'", category, movie.Title, movie.Year, movie.IMDBNumber ?? "<empty>", movie.TMDBNumber ?? "<empty>");
                                movie.CategoryTrakt.Add(category);
                                movie.Username = TraktSettings.Username;
                                movie.Commit();
                            }
                            cleanupList.Remove(movie);
                        }
                    }
                    // remove tag from remaining films
                    foreach (var movie in cleanupList)
                    {
                        TraktLogger.Info("Removing trakt category for movie. Category = '{0}', Title = '{1}', Year = '{2}', IMDb ID = '{3}', TMDb ID = '{4}'", category, movie.Title, movie.Year, movie.IMDBNumber ?? "<empty>", movie.TMDBNumber ?? "<empty>");
                        movie.CategoryTrakt.Remove(category);
                        movie.Username = TraktSettings.Username;
                        movie.Commit();
                    }
                }
                #endregion

                #region Update Custom List Tags
                var traktUserLists = TraktCache.CustomLists;

                if (traktUserLists != null)
                {
                    TraktLogger.Info("Retrieved {0} user lists from trakt", traktUserLists.Count());

                    foreach (var list in traktUserLists)
                    {
                        string userListName = Translation.List + ": " + list.Key.Name;
                        var cleanupList = allMovies.Where(m => m.CategoryTrakt.Contains(userListName)).ToList();
                        TraktLogger.Info("Processing trakt user list. Name = '{0}', Tag = '{1}', Items = '{2}'", list.Key.Name, userListName, list.Value.Count);

                        // process 'movies' only 
                        foreach (var trm in list.Value.Where(m => m.Type == "movie"))
                        {
                            TraktLogger.Debug("Processing trakt user list movie. Title = '{0}', Year = '{1}', IMDb ID = '{2}', TMDb ID = '{3}'", trm.Movie.Title ?? "null", trm.Movie.Year.HasValue ? trm.Movie.Year.ToString() : "<empty>", trm.Movie.Ids.Imdb ?? "<empty>", trm.Movie.Ids.Tmdb.HasValue ? trm.Movie.Ids.Tmdb.ToString() : "<empty>");
                            foreach (var movie in allMovies.Where(m => MovieMatch(m, trm.Movie)))
                            {
                                if (!movie.CategoryTrakt.Contains(userListName))
                                {
                                    // update local trakt category
                                    TraktLogger.Info("Inserting trakt user list for movie. Category = '{0}', Title = '{1}', Year = '{2}', IMDb ID = '{3}', TMDb ID = '{4}'", userListName, movie.Title, movie.Year, movie.IMDBNumber ?? "<empty>", movie.TMDBNumber ?? "<empty>");
                                    movie.CategoryTrakt.Add(userListName);
                                    movie.Username = TraktSettings.Username;
                                    movie.Commit();
                                }
                                cleanupList.Remove(movie);
                            }
                        }

                        // remove tag from remaining films
                        foreach (var movie in cleanupList)
                        {
                            TraktLogger.Info("Removing trakt user list for movie. Category = '{0}', Title = '{1}', Year = '{2}', IMDb ID = '{3}', TMDb ID = '{4}'", userListName, movie.Title, movie.Year, movie.IMDBNumber ?? "<empty>", movie.TMDBNumber ?? "<empty>");
                            movie.CategoryTrakt.Remove(userListName);
                            movie.Username = TraktSettings.Username;
                            movie.Commit();
                        }
                    }
                }
                #endregion

                #region Update Recommendation Tags
                var traktRecommendationMovies = TraktCache.GetRecommendedMoviesFromTrakt();

                if (traktRecommendationMovies != null)
                {
                    string category = Translation.Recommendations;
                    TraktLogger.Info("Retrieved {0} recommendations items from trakt", traktRecommendationMovies.Count());

                    var cleanupList = allMovies.Where(m => m.CategoryTrakt.Contains(category)).ToList();
                    foreach (var trm in traktRecommendationMovies)
                    {
                        TraktLogger.Debug("Processing trakt recommendations movie. Title = '{0}', Year = '{1}', IMDb ID = '{2}', TMDb ID = '{3}'", trm.Title ?? "<empty>", trm.Year.HasValue ? trm.Year.ToString() : "<empty>", trm.Ids.Imdb ?? "<empty>", trm.Ids.Tmdb.HasValue ? trm.Ids.Tmdb.ToString() : "<empty>");
                        foreach (var movie in allMovies.Where(m => MovieMatch(m, trm)))
                        {
                            if (!movie.CategoryTrakt.Contains(category))
                            {
                                // update local trakt category
                                TraktLogger.Info("Inserting trakt category for movie. Category = '{0}', Title = '{1}', Year = '{2}', IMDb ID = '{3}', TMDb ID = '{4}'", category, movie.Title, movie.Year, movie.IMDBNumber ?? "<empty>", movie.TMDBNumber ?? "<empty>");
                                movie.CategoryTrakt.Add(category);
                                movie.Username = TraktSettings.Username;
                                movie.Commit();
                            }
                            cleanupList.Remove(movie);
                        }
                    }
                    // remove tag from remaining films
                    foreach (var movie in cleanupList)
                    {
                        // update local trakt category
                        TraktLogger.Info("Removing trakt category for movie. Category = '{0}', Title = '{1}', Year = '{2}', IMDb ID = '{3}', TMDb ID = '{4}'", category, movie.Title, movie.Year, movie.IMDBNumber ?? "<empty>", movie.TMDBNumber ?? "<empty>");
                        movie.CategoryTrakt.Remove(category);
                        movie.Username = TraktSettings.Username;
                        movie.Commit();
                    }
                }
                #endregion
            }
            else
            {
                if (categoryTraktList.Count > 0)
                {
                    TraktLogger.Info("Clearing {0} trakt-categorised movies from My Films database", categoryTraktList.Count.ToString());

                    foreach (var movie in categoryTraktList)
                    {
                        movie.CategoryTrakt.Clear();
                        movie.Commit();
                    }
                }
            }
            #endregion

            myvideos.Clear();

            SyncInProgress = false;
            TraktLogger.Info("My Films Sync Completed");
        }

        public bool Scrobble(string filename)
        {
            // check movie is from my films
            if (CurrentMovie == null)
                return false;

            var scrobbleData = CreateScrobbleData(CurrentMovie);
            var scrobbleThread = new Thread(objScrobble =>
            {
                var tScrobbleData = objScrobble as TraktScrobbleMovie;
                if (tScrobbleData == null) return;

                TraktLogger.Info("Sending start scrobble of movie to trakt.tv. Title = '{0}', Year = '{1}', IMDb ID = '{2}', TMDb ID = '{3}'", tScrobbleData.Movie.Title, tScrobbleData.Movie.Year, tScrobbleData.Movie.Ids.Imdb ?? "<empty>", tScrobbleData.Movie.Ids.Tmdb.HasValue ? tScrobbleData.Movie.Ids.Tmdb.ToString() : "<empty>");
                var response = TraktAPI.TraktAPI.StartMovieScrobble(tScrobbleData);
                TraktLogger.LogTraktResponse(response);
            })
            {
                Name = "Scrobble",
                IsBackground = true
            };

            scrobbleThread.Start(scrobbleData);

            return true;
        }

        public void StopScrobble()
        {
            // handled by myfilms events
            return;
        }

        #endregion

        #region DataCreators

        /// <summary>
        /// Creates Scrobble data based on a MFMovie object
        /// </summary>
        /// <param name="movie">The movie to base the object on</param>
        /// <returns>The Trakt scrobble data to send</returns>
        public static TraktScrobbleMovie CreateScrobbleData(MFMovie movie)
        {
            double duration = g_Player.Duration;
            double progress = 0.0;

            // get current progress of player (in seconds) to work out percent complete
            if (duration > 0.0)
                progress = (g_Player.CurrentPosition / duration) * 100.0;

            var scrobbleData = new TraktScrobbleMovie
            {
                Movie = new TraktMovie
                {
                    Ids = new TraktMovieId { Imdb = movie.IMDBNumber, Tmdb = movie.TMDBNumber.ToNullableInt32() },
                    Title = movie.Title,
                    Year = movie.Year
                },
                Progress = Math.Round(progress, 2),
                AppVersion = TraktSettings.Version,
                AppDate = TraktSettings.BuildDate
            };

            return scrobbleData;
        }

        #endregion

        #region Public Methods
        
        public void DisposeEvents()
        {
            TraktLogger.Debug("Removing Hooks from My Films");
            
            // unsubscribe from events
            MyFilmsDetail.RateItem -= new MyFilmsDetail.RatingEventDelegate(OnRateItem);
            MyFilmsDetail.WatchedItem -= new MyFilmsDetail.WatchedEventDelegate(OnToggleWatched);
            MyFilmsDetail.MovieStarted -= new MyFilmsDetail.MovieStartedEventDelegate(OnStartedMovie);
            MyFilmsDetail.MovieStopped -= new MyFilmsDetail.MovieStoppedEventDelegate(OnStoppedMovie);
            MyFilmsDetail.MovieWatched -= new MyFilmsDetail.MovieWatchedEventDelegate(OnWatchedMovie);
            MyFilms.ImportComplete -= new MyFilms.ImportCompleteEventDelegate(OnImportComplete);
        }

        #endregion

        #region Player Events

        private void OnStartedMovie(MFMovie movie)
        {
            if (TraktSettings.AccountStatus != ConnectionState.Connected) return;

            if (!TraktSettings.BlockedFilenames.Contains(movie.File) && !TraktSettings.BlockedFolders.Any(f => movie.File.ToLowerInvariant().Contains(f.ToLowerInvariant())))
            {
                TraktLogger.Info("Starting My Films movie playback. Title = '{0}', Year = '{1}', IMDb ID = '{2}', TMDb ID = '{3}'", movie.Title, movie.Year, movie.IMDBNumber ?? "<empty>", movie.TMDBNumber ?? "<empty>");
                CurrentMovie = movie;
            }
        }

        private void OnStoppedMovie(MFMovie movie)
        {
            CurrentMovie = null;
            if (TraktSettings.AccountStatus != ConnectionState.Connected) return;

            if (!TraktSettings.BlockedFilenames.Contains(movie.File) && !TraktSettings.BlockedFolders.Any(f => movie.File.ToLowerInvariant().Contains(f.ToLowerInvariant())))
            {
                TraktLogger.Info("Stopped My Films movie playback. Title = '{0}', Year = '{1}', IMDb ID = '{2}', TMDb ID = '{3}'", movie.Title, movie.Year, movie.IMDBNumber ?? "<empty>", movie.TMDBNumber ?? "<empty>");

                // stop scrobbling
                var scrobbleData = CreateScrobbleData(movie);
                var stopScrobble = new Thread(objScrobble =>
                {
                    var tScrobbleData = objScrobble as TraktScrobbleMovie;
                    if (tScrobbleData == null) return;

                    var response = TraktAPI.TraktAPI.StopMovieScrobble(tScrobbleData);
                    TraktLogger.LogTraktResponse(response);
                })
                {
                    IsBackground = true,
                    Name = "Scrobble"
                };

                stopScrobble.Start(scrobbleData);
            }
        }

        private void OnWatchedMovie(MFMovie movie)
        {
            CurrentMovie = null;
            if (TraktSettings.AccountStatus != ConnectionState.Connected) return;

            if (!TraktSettings.BlockedFilenames.Contains(movie.File) && !TraktSettings.BlockedFolders.Any(f => movie.File.ToLowerInvariant().Contains(f.ToLowerInvariant())))
            {
                TraktLogger.Info("My Films movie considered watched. Title = '{0}', Year = '{1}', IMDb ID = '{2}', TMDb ID = '{3}'", movie.Title, movie.Year, movie.IMDBNumber ?? "<empty>", movie.TMDBNumber ?? "<empty>");

                // show trakt rating dialog
                ShowRateDialog(movie);

                // no longer need movie in recommendations or watchlist
                RemoveMovieFromRecommendations(movie);
                RemoveMovieFromWatchlist(movie);

                // stop scrobbling
                var scrobbleData = CreateScrobbleData(movie);
                var stopScrobble = new Thread(objScrobble =>
                {
                    var tScrobbleData = objScrobble as TraktScrobbleMovie;
                    if (tScrobbleData == null) return;

                    // check progress is enough to mark as watched online
                    if (tScrobbleData.Progress < 80)
                        tScrobbleData.Progress = 100;

                    var response = TraktAPI.TraktAPI.StopMovieScrobble(tScrobbleData);
                    TraktLogger.LogTraktResponse(response);
                })
                {
                    IsBackground = true,
                    Name = "Scrobble"
                };

                stopScrobble.Start(scrobbleData);
            }
        }

        #endregion

        #region GUI Events

        private void OnRateItem(MFMovie movie, string value)
        {
            TraktLogger.Info("Received rating event from MyFilms. Title = '{0}', Year = '{1}', IMDb ID = '{2}', TMDb ID = '{3}'", movie.Title, movie.Year, movie.IMDBNumber ?? "<empty>", movie.TMDBNumber ?? "<empty>");

            if (TraktSettings.AccountStatus != ConnectionState.Connected) return;

            // don't do anything if movie is blocked
            if (TraktSettings.BlockedFilenames.Contains(movie.File) || TraktSettings.BlockedFolders.Any(f => movie.File.ToLowerInvariant().Contains(f.ToLowerInvariant())))
            {
                TraktLogger.Info("Movie {0} is on the blocked list so we didn't update Trakt", movie.Title);
                return;
            }

            var rateThread = new Thread((o) =>
            {
                var tMovie = o as MFMovie;

                // My Films is a 100 point scale out of 10. Treat as decimal and then round off
                int rating = Convert.ToInt32(Math.Round(Convert.ToDecimal(value), MidpointRounding.AwayFromZero));

                var rateMovie = new TraktSyncMovieRated
                {
                    Ids = new TraktMovieId { Imdb = tMovie.IMDBNumber, Tmdb = tMovie.TMDBNumber.ToNullableInt32() },
                    Title = tMovie.Title,
                    Year = tMovie.Year,
                    Rating = rating,
                    RatedAt = DateTime.UtcNow.ToISO8601()
                };

                var response = TraktAPI.TraktAPI.AddMovieToRatings(rateMovie);

                TraktLogger.LogTraktResponse(response);
            })
            {
                IsBackground = true,
                Name = "Rate"
            };

            rateThread.Start(movie);
        }

        private void OnToggleWatched(MFMovie movie, bool watched, int count)
        {
            TraktLogger.Info("Received togglewatched event from My Films. Title = '{0}', Year = '{1}', IMDb ID = '{2}', TMDb ID = '{3}'", movie.Title, movie.Year, movie.IMDBNumber ?? "<empty>", movie.TMDBNumber ?? "<empty>");

            if (TraktSettings.AccountStatus != ConnectionState.Connected) return;

            // don't do anything if movie is blocked
            if (TraktSettings.BlockedFilenames.Contains(movie.File) || TraktSettings.BlockedFolders.Any(f => movie.File.ToLowerInvariant().Contains(f.ToLowerInvariant())))
            {
                TraktLogger.Info("Movie {0} is on the blocked list so we didn't update Trakt", movie.Title);
                return;
            }

            var toggleWatchedThread = new Thread((o) =>
            {
                MFMovie tMovie = o as MFMovie;

                if (!watched)
                {
                    var traktMovie = new TraktMovie
                    {
                        Ids = new TraktMovieId { Imdb = tMovie.IMDBNumber, Tmdb = movie.TMDBNumber.ToNullableInt32() },
                        Title = tMovie.Title,
                        Year = tMovie.Year
                    };

                    var response = TraktAPI.TraktAPI.RemoveMovieFromWatchedHistory(traktMovie);
                    TraktLogger.LogTraktResponse(response);
                }
                else
                {
                    // no longer need movie in recommendations or watchlist
                    RemoveMovieFromRecommendations(tMovie);
                    RemoveMovieFromWatchlist(tMovie);

                    var traktMovie = new TraktSyncMovieWatched
                    {
                        Ids = new TraktMovieId { Imdb = tMovie.IMDBNumber, Tmdb = movie.TMDBNumber.ToNullableInt32() },
                        Title = tMovie.Title,
                        Year = tMovie.Year,
                        WatchedAt = DateTime.UtcNow.ToISO8601()
                    };

                    var response = TraktAPI.TraktAPI.AddMovieToWatchedHistory(traktMovie);
                    TraktLogger.LogTraktResponse(response);
                }
            })
            {
                IsBackground = true,
                Name = "ToggleWatched"
            };

            toggleWatchedThread.Start(movie);
        }

        #endregion

        #region Import Events

        private void OnImportComplete()
        {
            if (TraktSettings.AccountStatus != ConnectionState.Connected) return;

            TraktLogger.Debug("My Films import complete, initiating online sync");

            // sync again
            Thread syncThread = new Thread(delegate()
            {
                while (SyncInProgress)
                {
                    // only do one sync at a time
                    TraktLogger.Debug("My Films sync still in progress, waiting to complete. Trying again in 60 secs");
                    Thread.Sleep(60000);
                }
                SyncLibrary();
            })
            {
                IsBackground = true,
                Name = "Sync"
            };

            syncThread.Start();
        }       
      
        #endregion

        #region Other Public Methods

        public static bool FindMovie(string title, int year, string imdbid, ref int? movieid, ref string config)
        {
            // get all movies
            ArrayList myvideos = new ArrayList();
            BaseMesFilms.GetMovies(ref myvideos);

            // get all movies in local database
            List<MFMovie> movies = (from MFMovie m in myvideos select m).ToList();

            // try find a match
            MFMovie movie = movies.Find(m => BasicHandler.GetProperImdbId(m.IMDBNumber) == imdbid || (string.Compare(m.Title, title, true) == 0 && m.Year == year));
            if (movie == null) return false;

            movieid = movie.ID;
            config = movie.Config;
            return true;
        }

        #endregion

        #region Private Methods

        private bool MovieMatch(MFMovie mfMovie, TraktMovie traktMovie)
        {
            // IMDb comparison
            if (!string.IsNullOrEmpty(traktMovie.Ids.Imdb) && !string.IsNullOrEmpty(BasicHandler.GetProperImdbId(mfMovie.IMDBNumber)))
            {
                return string.Compare(BasicHandler.GetProperImdbId(mfMovie.IMDBNumber), traktMovie.Ids.Imdb, true) == 0;
            }

            // TMDb comparison
            if (!string.IsNullOrEmpty(mfMovie.TMDBNumber) && traktMovie.Ids.Tmdb.HasValue)
            {
                return string.Compare(mfMovie.TMDBNumber, traktMovie.Ids.Tmdb.ToString(), true) == 0;
            }

            // Title & Year comparison
            return string.Compare(mfMovie.Title, traktMovie.Title, true) == 0 && mfMovie.Year.ToString() == traktMovie.Year.ToString();
        }

        /// <summary>
        /// Shows the Rate Movie Dialog after playback has ended
        /// </summary>
        /// <param name="movie">The movie being rated</param>
        private void ShowRateDialog(MFMovie movie)
        {
            if (!TraktSettings.ShowRateDialogOnWatched) return;     // not enabled
            if (movie.RatingUser > 0) return;                       // already rated

            TraktLogger.Debug("Showing rate dialog for movie. Title = '{0}', Year = '{1}', IMDb ID = '{2}', TMDb ID = '{3}'", movie.Title, movie.Year, movie.IMDBNumber ?? "<empty>", movie.TMDBNumber ?? "<empty>");

            var rateThread = new Thread((objMovie) =>
            {
                MFMovie movieToRate = objMovie as MFMovie;
                if (movieToRate == null) return;

                var rateObject = new TraktSyncMovieRated
                {
                    Ids = new TraktMovieId { Imdb = movieToRate.IMDBNumber, Tmdb = movie.TMDBNumber.ToNullableInt32() },
                    Title = movieToRate.Title,
                    Year = movieToRate.Year,
                    RatedAt = DateTime.UtcNow.ToISO8601()
                };

                // get the rating submitted to trakt.tv
                int rating = GUI.GUIUtils.ShowRateDialog<TraktSyncMovieRated>(rateObject);

                if (rating > 0)
                {
                    TraktLogger.Debug("Rating {0} ({1}) as {2}/10", movieToRate.Title, movie.Year, rating.ToString());
                    movieToRate.RatingUser = (float)rating;
                    movieToRate.Username = TraktSettings.Username;
                    movieToRate.Commit();

                    // update skin properties if movie is still selected
                    if (GUICommon.GetProperty("#myfilms.user.mastertitle.value").Equals(movieToRate.Title))
                    {
                        GUICommon.SetProperty("#myfilms.user.rating.value", movieToRate.RatingUser.ToString());
                    }
                }
            })
            {
                Name = "Rate",
                IsBackground = true
            };
            
            rateThread.Start(movie);
        }

        /// <summary>
        /// Removes movie from Recommendations
        /// </summary>
        private void RemoveMovieFromRecommendations(MFMovie movie)
        {
            if (movie.CategoryTrakt == null) return;

            if (movie.CategoryTrakt.Contains(Translation.Recommendations))
            {
                TraktLogger.Info("Removing movie from trakt reommendations. Title = '{0}', Year = '{1}', IMDb ID = '{2}', TMDb ID = '{3}'", movie.Title, movie.Year, movie.IMDBNumber ?? "<empty>", movie.TMDBNumber ?? "<empty>");
                movie.CategoryTrakt.Remove(Translation.Recommendations);
                movie.Username = TraktSettings.Username;
                movie.Commit();
            }
        }

        /// <summary>
        /// Removes movie from Watchlist
        /// </summary>
        private void RemoveMovieFromWatchlist(MFMovie movie)
        {
            if (movie.CategoryTrakt == null) return;

            if (movie.CategoryTrakt.Contains(Translation.Recommendations))
            {
                TraktLogger.Info("Removing movie from trakt watchlist. Title = '{0}', Year = '{1}', IMDb ID = '{2}', TMDb ID = '{3}'", movie.Title, movie.Year, movie.IMDBNumber ?? "<empty>", movie.TMDBNumber ?? "<empty>");
                movie.CategoryTrakt.Remove(Translation.WatchList);
                movie.Username = TraktSettings.Username;
                movie.Commit();
            }
        }
        #endregion

    }
}
