using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cornerstone.Database;
using Cornerstone.Database.CustomTypes;
using MediaPortal.Plugins.MovingPictures;
using MediaPortal.Plugins.MovingPictures.LocalMediaManagement;
using MediaPortal.Plugins.MovingPictures.Database;
using MediaPortal.Plugins.MovingPictures.MainUI;
using TraktPlugin.GUI;
using TraktPlugin.TraktAPI;
using TraktPlugin.TraktAPI.DataStructures;
using TraktPlugin.TraktAPI.Enums;
using TraktPlugin.TraktAPI.Extensions;
using System.Timers;
using MediaPortal.Player;
using System.Reflection;
using System.ComponentModel;
using System.Threading;
using Cornerstone.Database.Tables;

namespace TraktPlugin.TraktHandlers
{
    /// <summary>
    /// Support for MovingPictures
    /// </summary>
    class MovingPictures : ITraktHandler
    {
        DBMovieInfo currentMovie;
        bool SyncInProgress;
        bool TraktRateSent;
        bool IsDVDPlaying;
        public static MoviePlayer player = null;

        public static DBSourceInfo tmdbSource;

        public MovingPictures(int priority)
        {
            Priority = priority;
            TraktLogger.Debug("Adding Hooks to Moving Pictures Database");
            MovingPicturesCore.DatabaseManager.ObjectInserted += new DatabaseManager.ObjectAffectedDelegate(DatabaseManager_ObjectInserted);
            MovingPicturesCore.DatabaseManager.ObjectUpdatedEx += new DatabaseManager.ObjectUpdatedDelegate(DatabaseManager_ObjectUpdatedEx);
            MovingPicturesCore.DatabaseManager.ObjectDeleted += new DatabaseManager.ObjectAffectedDelegate(DatabaseManager_ObjectDeleted);
        }

        #region ITraktHandler

        public string Name { get { return "Moving Pictures"; } }
        public int Priority { get; set; }

        public void SyncLibrary()
        {
            TraktLogger.Info("Moving Pictures Starting Sync");
            SyncInProgress = true;

            // Get all movies in the local database
            var collectedMovies = DBMovieInfo.GetAll();            

            // Get TMDb Data Provider
            tmdbSource = DBSourceInfo.GetAll().Find(s => s.ToString() == "themoviedb.org");
            
            // Remove any blocked movies
            TraktLogger.Info("Removing blocked files and folders from sync movie list");
            collectedMovies.RemoveAll(movie => TraktSettings.BlockedFolders.Any(f => movie.LocalMedia[0].FullPath.ToLowerInvariant().Contains(f.ToLowerInvariant())));
            collectedMovies.RemoveAll(movie => TraktSettings.BlockedFilenames.Contains(movie.LocalMedia[0].FullPath));

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
            //            TraktLogger.Info("Skipping {0} movies due to invalid data or movies don't exist on http://themoviedb.org. Next check will be {1}", TraktSettings.SkippedMovies.Movies.Count, TraktSettings.SkippedMovies.LastSkippedSync.FromEpoch().Add(new TimeSpan(7, 0, 0, 0)));
            //            foreach (var movie in TraktSettings.SkippedMovies.Movies)
            //            {
            //                TraktLogger.Info("Skipping movie, Title: {0}, Year: {1}, IMDb: {2}", movie.Title, movie.Year, movie.IMDBID);
            //                collectedMovies.RemoveAll(m => (m.Title == movie.Title) && (m.Year.ToString() == movie.Year) && (m.ImdbID == movie.IMDBID));
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
            // we need to fallback to title matching. When we sync aka names, they're sometimes accepted if defined on themoviedb.org so we need to 
            // do this to prevent syncing these movies every sync interval.
            //TODO
            //if (TraktSettings.AlreadyExistMovies != null && TraktSettings.AlreadyExistMovies.Movies != null && TraktSettings.AlreadyExistMovies.Movies.Count > 0)
            //{
            //    TraktLogger.Debug("Skipping {0} movies as they already exist in trakt library but failed local match previously", TraktSettings.AlreadyExistMovies.Movies.Count.ToString());
            //    var movies = new List<TraktMovieSync.Movie>(TraktSettings.AlreadyExistMovies.Movies);
            //    foreach (var movie in movies)
            //    {
            //        Predicate<DBMovieInfo> criteria = m => (m.Title == movie.Title) && (m.Year.ToString() == movie.Year) && (m.ImdbID == movie.IMDBID);
            //        if (collectedMovies.Exists(criteria))
            //        {
            //            TraktLogger.Debug("Skipping movie, Title: {0}, Year: {1}, IMDb: {2}", movie.Title, movie.Year, movie.IMDBID);
            //            collectedMovies.RemoveAll(criteria);
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

            #region Get online data from trakt.tv

            #region Get unwatched movies from trakt.tv
            // clear the last time(s) we did anything online
            TraktCache.ClearLastActivityCache();

            TraktLogger.Info("Getting user {0}'s unwatched movies from trakt", TraktSettings.Username);
            var traktUnWatchedMovies = TraktCache.GetUnWatchedMoviesFromTrakt();
            TraktLogger.Info("There are {0} unwatched movies since the last sync with trakt.tv", traktUnWatchedMovies.Count());
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
                #region Get local database info
                
                TraktLogger.Info("Found {0} movies available to sync in MovingPictures database", collectedMovies.Count);

                // get the movies that we have watched
                var watchedMovies = collectedMovies.Where(m => m.ActiveUserSettings.WatchedCount > 0).ToList();
                TraktLogger.Info("Found {0} watched movies available to sync in MovingPictures database", watchedMovies.Count);

                // get the movies that we have rated/unrated
                var ratedMovies = collectedMovies.Where(m => m.ActiveUserSettings.UserRating > 0).ToList();
                TraktLogger.Info("Found {0} rated movies available to sync in MovingPictures database", ratedMovies.Count);
                
                #endregion

                #region Mark movies as unwatched in local database
                if (traktUnWatchedMovies.Count() > 0)
                {
                    foreach (var movie in traktUnWatchedMovies)
                    {
                        var localMovie = watchedMovies.FirstOrDefault(m => MovieMatch(m, movie));
                        if (localMovie == null) continue;

                        TraktLogger.Info("Marking movie as unwatched in local database, movie is not watched on trakt.tv. Title = '{0}', Year = '{1}', IMDb ID = '{2}', TMDb ID = '{3}'",
                                          movie.Title, movie.Year.HasValue ? movie.Year.ToString() : "<empty>", movie.Ids.Imdb ?? "<empty>", movie.Ids.Tmdb.HasValue ? movie.Ids.Tmdb.ToString() : "<empty>" );

                        localMovie.ActiveUserSettings.WatchedCount = 0;
                        localMovie.ActiveUserSettings.Commit();
                    }

                    // update watched set
                    watchedMovies = collectedMovies.Where(m => m.ActiveUserSettings.WatchedCount > 0).ToList();
                }
                #endregion

                #region Mark movies as watched in local database
                if (traktWatchedMovies.Count() > 0)
                {
                    foreach (var twm in traktWatchedMovies)
                    {
                        var localMovie = collectedMovies.FirstOrDefault(m => MovieMatch(m, twm.Movie));
                        if (localMovie == null) continue;

                        if (localMovie.ActiveUserSettings.WatchedCount < twm.Plays)
                        {
                            TraktLogger.Info("Updating local movie watched state / play count to match trakt.tv. Plays = '{0}', Title = '{1}', Year = '{2}', IMDb ID = '{3}', TMDb ID = '{4}'",
                                              twm.Plays, twm.Movie.Title, twm.Movie.Year.HasValue ? twm.Movie.Year.ToString() : "<empty>", twm.Movie.Ids.Imdb ?? "<empty>", twm.Movie.Ids.Tmdb.HasValue ? twm.Movie.Ids.Tmdb.ToString() : "<empty>");

                            localMovie.ActiveUserSettings.WatchedCount = twm.Plays;
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
                                        Ids = new TraktMovieId { Imdb = movie.ImdbID, Tmdb = GetTmdbID(movie).ToNullableInt32() },
                                        Title = movie.Title,
                                        Year = movie.Year,
                                        WatchedAt = GetFirstWatchedDate(movie),
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
                                           Ids = new TraktMovieId { Imdb = movie.ImdbID, Tmdb = GetTmdbID(movie).ToNullableInt32() },
                                           Title = movie.Title,
                                           Year = movie.Year,
                                           CollectedAt = movie.DateAdded.ToUniversalTime().ToISO8601(),
                                           MediaType = GetMovieMediaType(movie),
                                           Resolution = GetMovieResolution(movie),
                                           AudioCodec = GetMovieAudioCodec(movie),
                                           AudioChannels = GetMovieAudioChannels(movie),
                                           Is3D = IsMovie3D(movie)
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
                                            Ids = new TraktMovieId { Imdb = movie.ImdbID, Tmdb = GetTmdbID(movie).ToNullableInt32() },
                                            Title = movie.Title,
                                            Year = movie.Year,
                                            Rating = (int)movie.UserSettings.First().UserRating * 2,
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

                        if (localMovie.UserSettings.First().UserRating == 0)
                        {
                            // update local collection rating (5 Point Scale)
                            int rating = (int)(Math.Round(trm.Rating / 2.0, MidpointRounding.AwayFromZero));

                            TraktLogger.Info("Adding movie rating to match trakt.tv. Rated = '{0}/10', Title = '{1}', Year = '{2}', IMDb ID = '{3}', TMDb ID = '{4}'",
                                              trm.Rating, trm.Movie.Title, trm.Movie.Year.HasValue ? trm.Movie.Year.ToString() : "<empty>", trm.Movie.Ids.Imdb ?? "<empty>", trm.Movie.Ids.Tmdb.HasValue ? trm.Movie.Ids.Tmdb.ToString() : "<empty>");

                            localMovie.ActiveUserSettings.UserRating = rating;
                            localMovie.Commit();
                        }
                    }
                }
                #endregion
            }

            #region Filters and Categories Menu
                        
            // Moving Pictures Categories Menu
            if (TraktSettings.MovingPicturesCategories)
                UpdateCategoriesMenu();
            else
                RemoveTraktFromCategoryMenu();

            // Moving Pictures Filters Menu
            if (TraktSettings.MovingPicturesFilters)
                UpdateFiltersMenu();
            else
                RemoveTraktFromFiltersMenu();

            #endregion

            SyncInProgress = false;
            TraktLogger.Info("Moving Pictures Sync Completed");
        }

        public bool Scrobble(String filename)
        {
            StopScrobble();
            
            // stop check if not valid player type for plugin handler
            if (g_Player.IsTV || g_Player.IsTVRecording)
                return false;

            bool matchFound = false;
            List<DBMovieInfo> searchResults = (from m in DBMovieInfo.GetAll() 
                                               where (from path in m.LocalMedia select path.FullPath).ToList().Contains(filename)
                                               select m).ToList();

            if (searchResults.Count == 1)
            {
                matchFound = true;
                currentMovie = searchResults[0];
                IsDVDPlaying = false;
            }
            else
            {
                #region Check if movie playing is a DVD
                IsDVDPlaying = false;

                // check if filename is DVD/Bluray format
                if (VideoUtility.GetVideoFormat(filename) == VideoFormat.DVD || VideoUtility.GetVideoFormat(filename) == VideoFormat.Bluray)
                {
                    // use the player skin properties to determine movie playing
                    // note: movingpictures sets this 2secs after playback
                    TraktLogger.Info("Getting DVD/Bluray movie info from player skin properties");
                    System.Threading.Thread.Sleep(3000);

                    string title = GUI.GUIUtils.GetProperty("#Play.Current.Title");
                    string year = GUI.GUIUtils.GetProperty("#Play.Current.Year");
                    string imdb = GUI.GUIUtils.GetProperty("#Play.Current.IMDBNumber");

                    // we should always have title/year
                    if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(year))
                    {
                        TraktLogger.Info("Not enough information from MediaPortal play skin properties to get a movie match, missing Title and/or Year!");
                        return false;
                    }

                    // Check IMDb first
                    if (!string.IsNullOrEmpty(imdb))
                    {
                        TraktLogger.Info("Searching MovingPictures library for movie. IMDb ID = '{0}'", imdb);
                        currentMovie = DBMovieInfo.GetAll().FirstOrDefault(m => m.ImdbID == imdb);
                    }
                    else
                    {
                        TraktLogger.Info("Searching MovingPictures library for movie. Title = '{0}', Year = '{1}'", title, year);
                        currentMovie = DBMovieInfo.GetAll().FirstOrDefault(m => m.Title == title && m.Year == Convert.ToInt32(year));
                    }

                    if (currentMovie != null)
                    {
                        matchFound = true;
                        IsDVDPlaying = true;
                    }
                    else
                    {
                        TraktLogger.Info("Could not find movie in MovingPictures library. Filename = '{0}', Title = '{1}', Year = '{2}'", filename, title, year);
                    }
                }
                else
                {
                    TraktLogger.Debug("Filename could not be matched to a movie in MovingPictures");
                }
                #endregion
            }

            if (matchFound)
            {
                StartMovieScrobble(currentMovie);
                return true;
            }

            return false;
        }

        public void StopScrobble()
        {
            if (currentMovie != null)
            {
                Double watchPercent = MovingPicturesCore.Settings.MinimumWatchPercentage / 100.0;

                #region DVD Workaround

                // MovingPictures does not fire off a watched event for completed DVDs
                if (IsDVDPlaying)
                {
                    IsDVDPlaying = false;

                    TraktLogger.Info("DVD/Bluray stopped, checking if considered watched. Movie: '{0}', Current Position: '{1}', Duration: '{2}'", currentMovie.Title, g_Player.CurrentPosition, g_Player.Duration);

                    // Ignore watched percentage of video and scrobble anyway
                    // MovingPictures also doesn't appear to store the mediainfo correct for DVDs
                    // It appears to add up all videos on the DVD structure
                    if (TraktSettings.IgnoreWatchedPercentOnDVD)
                    {
                        TraktLogger.Info("Ignoring watched percent on DVD, sending watched state to trakt.tv");

                        ShowRateDialog(currentMovie);
                        StopMovieScrobble(currentMovie, true);
                        
                        RemoveMovieCriteriaFromRecommendationsNode(currentMovie.ImdbID);
                        RemoveMovieCriteriaFromWatchlistNode(currentMovie.ImdbID);

                        currentMovie = null;
                        return;
                    }
                    
                    // check percentage watched, if duration is '0' scrobble anyway as it could be back 
                    // at the menu after main feature has completed.
                    if (g_Player.Duration == 0 || (g_Player.CurrentPosition / g_Player.Duration) >= watchPercent)
                    {
                        ShowRateDialog(currentMovie);
                        StopMovieScrobble(currentMovie, true);

                        RemoveMovieCriteriaFromRecommendationsNode(currentMovie.ImdbID);
                        RemoveMovieCriteriaFromWatchlistNode(currentMovie.ImdbID);

                        currentMovie = null;
                        return;
                    }
                }
                #endregion

                if (g_Player.Duration != 0)
                {
                    // no point sending stop scrobble if we receive movie watched event from movpics
                    if ((g_Player.CurrentPosition / g_Player.Duration) >= watchPercent)
                    {
                        currentMovie = null;
                        return;
                    }
                }
                StopMovieScrobble(currentMovie);
                currentMovie = null;
            }
        }

        #endregion

        #region Scrobbling

        /// <summary>
        /// Starts or unpauses a movie scrobble
        /// </summary>
        private void StartMovieScrobble(DBMovieInfo movie)
        {
            var scrobbleThread = new Thread(movieObj =>
            {
                var scrobbleMovie = movieObj as DBMovieInfo;
                if (scrobbleMovie == null) return;

                TraktLogger.Info("Sending start scrobble of movie to trakt.tv. Title = '{0}', Year = '{1}', IMDb ID = '{2}', TMDb ID = '{3}'", scrobbleMovie.Title, scrobbleMovie.Year, scrobbleMovie.ImdbID ?? "<empty>", GetTmdbID(scrobbleMovie) ?? "<empty>");
                var response = TraktAPI.TraktAPI.StartMovieScrobble(CreateScrobbleData(scrobbleMovie));
                TraktLogger.LogTraktResponse(response);
            })
            {
                Name = "Scrobble",
                IsBackground = true
            };

            scrobbleThread.Start(movie);
        }

        /// <summary>
        /// Stops a movie scrobble
        /// </summary>
        private void StopMovieScrobble(DBMovieInfo movie, bool forceWatched = false)
        {
            var scrobbleThread = new Thread(movieObj =>
            {
                var scrobbleMovie = movieObj as DBMovieInfo;
                if (scrobbleMovie == null) return;

                var scrobbleData = CreateScrobbleData(scrobbleMovie);
                if (forceWatched)
                {
                    // override the percentage so it's marked as watched
                    scrobbleData.Progress = 100;
                }

                TraktLogger.Info("Sending stop scrobble of movie to trakt.tv. Progress = '{0}%', Title = '{1}', Year = '{2}', IMDb ID = '{3}', TMDb ID = '{4}'", scrobbleData.Progress, scrobbleMovie.Title, movie.Year, scrobbleMovie.ImdbID ?? "<empty>", GetTmdbID(scrobbleMovie) ?? "<empty>");
                var response = TraktAPI.TraktAPI.StopMovieScrobble(scrobbleData);
                TraktLogger.LogTraktResponse(response);
            })
            {
                Name = "Scrobble",
                IsBackground = true
            };

            scrobbleThread.Start(movie);
        }

        #endregion

        #region MovingPictures Events

        /// <summary>
        /// Fired when an objected is removed from the MovingPictures Database
        /// </summary>
        private void DatabaseManager_ObjectDeleted(DatabaseTable obj)
        {
            // only remove from collection if the user wants us to
            if (!TraktSettings.KeepTraktLibraryClean || !TraktSettings.SyncLibrary)
                return;

            // check connection state
            if (TraktSettings.AccountStatus != ConnectionState.Connected)
                return;

            // if we have removed a movie from MovingPictures we want to update Trakt library
            if (obj.GetType() != typeof(DBMovieInfo))
                return;

            var syncThread = new Thread((objMovie) =>
            {
                var movie = objMovie as DBMovieInfo;

                var traktMovie = new TraktMovie
                {
                    Ids = new TraktMovieId { Imdb = movie.ImdbID, Tmdb = GetTmdbID(movie).ToNullableInt32() },
                    Title = movie.Title,
                    Year = movie.Year
                };

                TraktLogger.Info("Removing movie from trakt.tv collection, Title = '{0}', Year = '{1}', IMDB ID = '{2}', TMDb ID = '{3}'", movie.Title, movie.Year, movie.ImdbID ?? "<empty>", GetTmdbID(movie) ?? "<empty>");

                var response = TraktAPI.TraktAPI.RemoveMovieFromCollection(traktMovie);
                TraktLogger.LogTraktResponse(response);
            })
            {
                IsBackground = true,
                Name = "Sync"
            };

            syncThread.Start(obj);
        }

        /// <summary>
        /// Fired when an object is updated in the Moving Pictures Database
        /// </summary>
        /// <param name="obj"></param>
        private void DatabaseManager_ObjectUpdatedEx(DatabaseTable dbObject, TableUpdateInfo ui)
        {
            // check connection state
            if (TraktSettings.AccountStatus != ConnectionState.Connected)
                return;

            // If it is user settings for a movie
            if (dbObject.GetType() != typeof(DBUserMovieSettings))
                return;

            // if we are syncing, we maybe manually setting state from trakt
            // in this case we dont want to resend to trakt
            if (SyncInProgress) return;

            DBUserMovieSettings userMovieSettings = (DBUserMovieSettings)dbObject;
            DBMovieInfo movie = userMovieSettings.AttachedMovies[0];

            // don't do anything if movie is blocked
            if (TraktSettings.BlockedFilenames.Contains(movie.LocalMedia[0].FullPath) || TraktSettings.BlockedFolders.Any(f => movie.LocalMedia[0].FullPath.ToLowerInvariant().Contains(f.ToLowerInvariant())))
            {
                TraktLogger.Info("Movie is on the blocked list so we didn't update trakt.tv. Title = '{0}', Year = '{1}', IMDB ID = '{2}', TMDb ID = '{3}'", movie.Title, movie.Year, movie.ImdbID ?? "<empty>", GetTmdbID(movie) ?? "<empty>");
                return;
            }

            // we check the watched flag and update Trakt respectfully
            // ignore if movie is the current movie being scrobbled, this will be set to watched automatically
            if (ui.WatchedCountChanged() && movie != currentMovie)
            {
                if (userMovieSettings.WatchedCount == 0)
                {
                    TraktLogger.Info("Received Un-Watched event in MovingPictures for movie. Title = '{0}', Year = '{1}', IMDB ID = '{2}', TMDb ID = '{3}'", movie.Title, movie.Year, movie.ImdbID ?? "<empty>", GetTmdbID(movie) ?? "<empty>");

                    var syncThread = new Thread((objMovie) =>
                    {
                        var tMovie = objMovie as DBMovieInfo;

                        var traktMovie = new TraktMovie
                        {
                            Ids = new TraktMovieId { Imdb = tMovie.ImdbID, Tmdb = GetTmdbID(tMovie).ToNullableInt32() },
                            Title = tMovie.Title,
                            Year = tMovie.Year
                        };

                        var response = TraktAPI.TraktAPI.RemoveMovieFromWatchedHistory(traktMovie);
                        TraktLogger.LogTraktResponse(response);
                    })
                    {
                        IsBackground = true,
                        Name = "Sync"
                    };

                    syncThread.Start(movie);
                }
                else
                {
                    TraktLogger.Info("Received Watched event in MovingPictures for movie. Title = '{0}', Year = '{1}', IMDB ID = '{2}', TMDb ID = '{3}'", movie.Title, movie.Year, movie.ImdbID ?? "<empty>", GetTmdbID(movie) ?? "<empty>");
                    if (!g_Player.IsVideo)
                    {
                        var syncThread = new Thread((objMovie) =>
                        {
                            var tMovie = objMovie as DBMovieInfo;

                            var traktMovie = new TraktSyncMovieWatched
                            {
                                Ids = new TraktMovieId { Imdb = tMovie.ImdbID, Tmdb = GetTmdbID(tMovie).ToNullableInt32() },
                                Title = tMovie.Title,
                                Year = tMovie.Year,
                                WatchedAt = DateTime.UtcNow.ToISO8601()
                            };

                            var response = TraktAPI.TraktAPI.AddMovieToWatchedHistory(traktMovie);
                            TraktLogger.LogTraktResponse(response);

                            // don't need to keep this movie anymore in categories/filter menu if it's watched
                            RemoveMovieCriteriaFromRecommendationsNode(tMovie.ImdbID);
                            RemoveMovieCriteriaFromWatchlistNode(tMovie.ImdbID);
                        })
                        {
                            IsBackground = true,
                            Name = "Sync"
                        };

                        syncThread.Start(movie);
                    }
                }
            }

            // we will update the Trakt rating of the Movie
            // ignore if we rated using trakt rate dialog
            if (ui.RatingChanged() && userMovieSettings.UserRating > 0 && !TraktRateSent)
            {
                TraktLogger.Info("Received Rate event in MovingPictures for movie. Rating = '{0}/5', Title = '{1}', Year = '{2}', IMDB ID = '{3}', TMDb ID = '{4}'", userMovieSettings.UserRating, movie.Title, movie.Year, movie.ImdbID ?? "<empty>", GetTmdbID(movie) ?? "<empty>");
                
                var syncThread = new Thread((objMovie) =>
                {
                    var tMovie = objMovie as DBMovieInfo;

                    var traktMovie = new TraktSyncMovieRated
                    {
                        Ids = new TraktMovieId { Imdb = tMovie.ImdbID, Tmdb = GetTmdbID(tMovie).ToNullableInt32() },
                        Title = tMovie.Title,
                        Year = tMovie.Year,
                        RatedAt = DateTime.UtcNow.ToISO8601(),
                        Rating = (int)userMovieSettings.UserRating * 2
                    };

                    var response = TraktAPI.TraktAPI.AddMovieToRatings(traktMovie);
                    TraktLogger.LogTraktResponse(response);
                })
                {
                    IsBackground = true,
                    Name = "Sync"
                };
                syncThread.Start(movie);
            }
        }

        /// <summary>
        /// Fired when an object is inserted in the Moving Pictures Database
        /// </summary>
        private void DatabaseManager_ObjectInserted(Cornerstone.Database.Tables.DatabaseTable obj)
        {
            // check connection state
            if (TraktSettings.AccountStatus != ConnectionState.Connected)
                return;

            if (obj.GetType() == typeof(DBWatchedHistory))
            {
                // movie has just been watched
                DBWatchedHistory watchedEvent = (DBWatchedHistory)obj;
                if (!TraktSettings.BlockedFilenames.Contains(watchedEvent.Movie.LocalMedia[0].FullPath) && !TraktSettings.BlockedFolders.Any(f => watchedEvent.Movie.LocalMedia[0].FullPath.ToLowerInvariant().Contains(f.ToLowerInvariant())))
                {
                    TraktLogger.Info("Watched History updated in MovingPictures. Title = '{0}', Year = '{1}', IMDB ID = '{2}', TMDb ID = '{3}'", watchedEvent.Movie.Title, watchedEvent.Movie.Year, watchedEvent.Movie.ImdbID ?? "<empty>", GetTmdbID(watchedEvent.Movie) ?? "<empty>");
                    ShowRateDialog(watchedEvent.Movie);
                    StopMovieScrobble(watchedEvent.Movie);

                    // remove from watchlist and recommendation categories and filters menu
                    // watched items are auto-removed online for these lists so we can do this now locally
                    RemoveMovieCriteriaFromRecommendationsNode(watchedEvent.Movie.ImdbID);
                    RemoveMovieCriteriaFromWatchlistNode(watchedEvent.Movie.ImdbID);
                }
                else
                {
                    TraktLogger.Info("Movie was blocked and not added to watched history on trakt.tv. Title = '{0}', Year = '{1}', IMDB ID = '{2}', TMDb ID = '{3}'", watchedEvent.Movie.Title, watchedEvent.Movie.Year, watchedEvent.Movie.ImdbID ?? "<empty>", GetTmdbID(watchedEvent.Movie) ?? "<empty>");
                }
            }
            else if (obj.GetType() == typeof(DBMovieInfo) && TraktSettings.SyncLibrary)
            {
                // movie was inserted into the database
                var insertedMovie = obj as DBMovieInfo;
                if (!TraktSettings.BlockedFilenames.Contains(insertedMovie.LocalMedia[0].FullPath) && !TraktSettings.BlockedFolders.Any(f => insertedMovie.LocalMedia[0].FullPath.ToLowerInvariant().Contains(f.ToLowerInvariant())))
                {
                    var syncThread = new Thread((objMovie) =>
                    {
                        // wait for import to be 100% complete including MediaInfo
                        Thread.Sleep(30000);

                        var tMovie = objMovie as DBMovieInfo;

                        var traktMovie = new TraktSyncMovieCollected
                        {
                            Ids = new TraktMovieId { Imdb = tMovie.ImdbID, Tmdb = GetTmdbID(tMovie).ToNullableInt32() },
                            Title = tMovie.Title,
                            Year = tMovie.Year,
                            CollectedAt = DateTime.UtcNow.ToISO8601(),
                            MediaType = GetMovieMediaType(tMovie),
                            Resolution = GetMovieResolution(tMovie),
                            AudioCodec = GetMovieAudioCodec(tMovie),
                            AudioChannels = GetMovieAudioChannels(tMovie),
                            Is3D = IsMovie3D(tMovie)
                        };

                        TraktLogger.Info("New movie added into MovingPictures, adding to trakt.tv collection. Title = '{0}', Year = '{1}', IMDb ID = '{2}', TMDb ID = '{3}', Date Added = '{4}', MediaType = '{5}', Resolution = '{6}', Audio Codec = '{7}', Audio Channels = '{8}'",
                                            traktMovie.Title, traktMovie.Year.HasValue ? traktMovie.Year.ToString() : "<empty>", traktMovie.Ids.Imdb ?? "<empty>", traktMovie.Ids.Tmdb.HasValue ? traktMovie.Ids.Tmdb.ToString() : "<empty>",
                                            traktMovie.CollectedAt, traktMovie.MediaType ?? "<empty>", traktMovie.Resolution ?? "<empty>", traktMovie.AudioCodec ?? "<empty>", traktMovie.AudioChannels ?? "<empty>");

                        var response = TraktAPI.TraktAPI.AddMovieToCollection(traktMovie);
                        TraktLogger.LogTraktResponse(response);
                    })
                    {
                        IsBackground = true,
                        Name = "Sync"
                    };

                    syncThread.Start(insertedMovie);
                }
                else
                {
                    TraktLogger.Info("Movie was blocked and not added to collection on trakt.tv. Title = '{0}', Year = '{1}', IMDB ID = '{2}', TMDb ID = '{3}'", insertedMovie.Title, insertedMovie.Year, insertedMovie.ImdbID ?? "<empty>", GetTmdbID(insertedMovie) ?? "<empty>");
                }
            }
        }

        #endregion
        
        #region Data Creators

        /// <summary>
        /// Checks if the movie is 3D or not
        /// </summary>
        private bool IsMovie3D(DBMovieInfo movie)
        {
            // get the first movie if stacked
            var firstMovie = movie.LocalMedia.FirstOrDefault();
            if (firstMovie == null) return false;

            // if mediainfo not available don't do anything
            if (!firstMovie.HasMediaInfo) return false;

            return firstMovie.Is3D;
        }

        /// <summary>
        /// Gets the trakt compatible string for the movies Media Type
        /// </summary>
        private string GetMovieMediaType(DBMovieInfo movie)
        {
            // get the first movie if stacked
            var firstMovie = movie.LocalMedia.FirstOrDefault();
            if (firstMovie == null) return null;

            // if mediainfo not available don't do anything
            if (!firstMovie.HasMediaInfo) return null;

            if (firstMovie.IsVideo)
                return TraktMediaType.digital.ToString();
            else if (firstMovie.IsBluray)
                return TraktMediaType.bluray.ToString();
            else if (firstMovie.IsDVD)
                return TraktMediaType.dvd.ToString();
            else if (firstMovie.IsHDDVD)
                return TraktMediaType.hddvd.ToString();
            else
                return null;
        }

        /// <summary>
        /// Gets the trakt compatible string for the movies Resolution
        /// </summary>
        private string GetMovieResolution(DBMovieInfo movie)
        {
            // get the first movie if stacked
            var firstMovie = movie.LocalMedia.FirstOrDefault();
            if (firstMovie == null) return null;

            // if mediainfo not available don't do anything
            if (!firstMovie.HasMediaInfo) return null;

            // try to match 1:1 with what we know
            switch (firstMovie.VideoResolution)
            {
                case "1080p":
                    return TraktResolution.hd_1080p.ToString();
                case "1080i":
                    return TraktResolution.hd_1080i.ToString();
                case "720p":
                    return TraktResolution.hd_720p.ToString();
                case "576p":
                    return TraktResolution.sd_576p.ToString();
                case "576i":
                    return TraktResolution.sd_576i.ToString();
                case "480p":
                    return TraktResolution.sd_480p.ToString();
                case "480i":
                    return TraktResolution.sd_480i.ToString();
                case "4K UHD":
                    return TraktResolution.uhd_4k.ToString();
            }

            return null;
        }

        /// <summary>
        /// Gets the trakt compatible string for the movies Audio
        /// </summary>
        private string GetMovieAudioCodec(DBMovieInfo movie)
        {
            // get the first movie if stacked
            var firstMovie = movie.LocalMedia.FirstOrDefault();
            if (firstMovie == null) return null;

            // if mediainfo not available don't do anything
            if (!firstMovie.HasMediaInfo) return null;

            switch (firstMovie.AudioCodec.ToLowerInvariant())
            {
                case "truehd":
                    return TraktAudio.dolby_truehd.ToString();
                case "dts":
                    return TraktAudio.dts.ToString();
                case "dtshd":
                    return TraktAudio.dts_ma.ToString();
                case "ac3":
                    return TraktAudio.dolby_digital.ToString();
                case "aac":
                    return TraktAudio.aac.ToString();
                case "mp2":
                    return TraktAudio.mp3.ToString();
                case "pcm":
                    return TraktAudio.lpcm.ToString();
                case "ogg":
                    return TraktAudio.ogg.ToString();
                 case "wma":
                    return TraktAudio.wma.ToString();
                case "flac":
                    return TraktAudio.flac.ToString();
                default:
                    return null;
            }
        }

        /// <summary>
        /// Gets the trakt compatible string for the movies Audio Channels
        /// </summary>
        private string GetMovieAudioChannels(DBMovieInfo movie)
        {
            // get the first movie if stacked
            var firstMovie = movie.LocalMedia.FirstOrDefault();
            if (firstMovie == null) return null;

            // if mediainfo not available don't do anything
            if (!firstMovie.HasMediaInfo) return null;

            switch (firstMovie.AudioChannels)
            {
                case "7.1":                    
                case "6.1":
                case "5.1":
                case "4.1":
                case "3.1":
                case "2.1":
                    return firstMovie.AudioChannels;
                case "7":
                    return "6.1";
                case "6":
                    return "5.1";
                case "5":
                    return "5.0";
                case "4":
                    return "4.0";
                case "3":
                    return "2.1";
                case "stereo":
                    return "2.0";
                case "mono":
                    return "1.0";
                default:
                    return null;
            }
        }

        /// <summary>
        /// returns the movie duration in seconds
        /// </summary>
        private double GetMovieDuration(DBMovieInfo movie, bool isDVD)
        {
            double duration = 0.0;

            // first try to get from MediaInfo
            if (movie.ActualRuntime != 0)
            {
                // MovingPictures stores duration in milliseconds
                duration = movie.ActualRuntime / 1000.0;
            }
            else if (g_Player.Duration != 0.0)
            {
                // g_Player reports in seconds
                duration = g_Player.Duration;
            }
            else
            {
                // MovingPictures stores scraped runtime in minutes
                duration = movie.Runtime * 60.0;
            }
            
            // MediaInfo runtime from MovingPictures is wrong
            // it sums up all videos on the DVD structure!
            // check if more than 4hrs will suffice
            if (isDVD && duration > (4 * 60 * 60)) duration = movie.Runtime * 60.0;

            // sometimes we could be finishing a DVD in an featurette
            // come up with an arbitrary runtime to avoid scrobbling as a trailer,
            // and be rejected, only do this on DVDs
            if (isDVD && duration < 900.0) duration = 120 * 60;

            return duration;
        }

        /// <summary>
        /// gets the first watched date of a movie
        /// </summary>
        private static string GetFirstWatchedDate(DBMovieInfo movie)
        {
            string dateFirstPlayed = DateTime.UtcNow.ToISO8601();

            if (movie.WatchedHistory != null && movie.WatchedHistory.Count > 0)
            {
                try
                {
                    // get the first time played, MovingPictures stores a history of watched dates
                    // not the last time played as the API would lead you to believe as the best value to use
                    dateFirstPlayed = movie.WatchedHistory.First().DateWatched.ToUniversalTime().ToISO8601();
                }
                catch (Exception e)
                {
                    TraktLogger.Error("Failed to get first watched date from watched movie. Title = '{0}', Year = '{1}', Error = '{2}'", movie.Title, movie.Year, e.Message);
                }
            }

            return dateFirstPlayed;
        }

        /// <summary>
        /// Creates Scrobble data based on a DBMovieInfo object
        /// </summary>
        private TraktScrobbleMovie CreateScrobbleData(DBMovieInfo movie)
        {
            // MovingPictures stores duration in milliseconds, g_Player reports in seconds
            double currentPosition = g_Player.CurrentPosition;
            double duration = GetMovieDuration(movie, IsDVDPlaying);

            // g_Player reports in seconds
            double progress = duration != 0.0 ? (currentPosition / duration * 100.0) : 0.0;

            var scrobbleData = new TraktScrobbleMovie
            {
                Movie = new TraktMovie
                {
                    Ids = new TraktMovieId { Imdb = movie.ImdbID, Tmdb = GetTmdbID(movie).ToNullableInt32() },
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

        #region Other Private Methods

        /// <summary>
        /// Checks if a local movie is the same as an online movie
        /// </summary>
        private bool MovieMatch(DBMovieInfo movPicsMovie, TraktMovie traktMovie)
        {
            // IMDb comparison
            if (!string.IsNullOrEmpty(traktMovie.Ids.Imdb) && !string.IsNullOrEmpty(BasicHandler.GetProperImdbId(movPicsMovie.ImdbID)))
            {
                return string.Compare(BasicHandler.GetProperImdbId(movPicsMovie.ImdbID), traktMovie.Ids.Imdb, true) == 0;
            }

            // TMDb comparison
            if (!string.IsNullOrEmpty(GetTmdbID(movPicsMovie)) && traktMovie.Ids.Tmdb.HasValue)
            {
                return string.Compare(GetTmdbID(movPicsMovie), traktMovie.Ids.Tmdb.ToString(), true) == 0;
            }

            // Title & Year comparison
            return string.Compare(movPicsMovie.Title, traktMovie.Title, true) == 0 && movPicsMovie.Year.ToString() == traktMovie.Year.ToString();
        }

        /// <summary>
        /// Shows the Rate Movie Dialog after playback has ended
        /// </summary>
        /// <param name="movie">The movie being rated</param>
        private void ShowRateDialog(DBMovieInfo movie)
        {
            if (MovingPicturesCore.Settings.AutoPromptForRating) return;    // movpics dialog is enabled
            if (!TraktSettings.ShowRateDialogOnWatched) return;             // not enabled
            if (movie.ActiveUserSettings.UserRating > 0) return;            // already rated

            var rateThread = new Thread((objMovie) =>
            {
                TraktLogger.Info("Showing rate dialog for movie. Title = '{0}', Year = '{1}', IMDB ID = '{2}', TMDb ID = '{3}'", movie.Title, movie.Year, movie.ImdbID ?? "<empty>", GetTmdbID(movie) ?? "<empty>");

                // added a delay due to bug in MovingPictures blocking OnPageLoad()
                // a call to GUIWindowManager.Process() causes MediaPortal to wait until any modal dialog is closed.
                // the value may need to be tweaked on some systems
                // visible symtoms of issue is wrong backdrop / progress in background whilst dialog is modal
                Thread.Sleep(TraktSettings.MovPicsRatingDlgDelay);

                var movieToRate = objMovie as DBMovieInfo;
                if (movieToRate == null) return;

                var rateObject = new TraktSyncMovieRated
                {
                    Ids = new TraktMovieId { Imdb = movieToRate.ImdbID, Tmdb = GetTmdbID(movieToRate).ToNullableInt32() },
                    Title = movieToRate.Title,
                    Year = movieToRate.Year,
                    RatedAt = DateTime.UtcNow.ToISO8601()
                };

                // get the rating submitted to trakt
                int rating = GUIUtils.ShowRateDialog<TraktSyncMovieRated>(rateObject);
                if (rating == -1) return;

                // flag to ignore event handler
                TraktRateSent = true;

                if (rating > 0)
                {
                    TraktLogger.Info("Applying rating for movie. Rating = '{0}/10', Title = '{1}', Year = '{2}', IMDB ID = '{3}', TMDb ID = '{4}'", rating,  movie.Title, movie.Year, movie.ImdbID ?? "<empty>", GetTmdbID(movie) ?? "<empty>");
                    movieToRate.ActiveUserSettings.UserRating = (int)(Math.Round(rating / 2.0, MidpointRounding.AwayFromZero));

                    // Publish to skin - same as how MovingPictures does it i.e. lose precision due to rounding
                    // Make sure we're still showing the active movie
                    if (GUIUtils.GetProperty("#MovingPictures.SelectedMovie.title").Equals(movieToRate.Title))
                    {
                        GUICommon.SetProperty("#MovingPictures.UserMovieSettings.user_rating", movieToRate.ActiveUserSettings.UserRating.ToString());
                        GUICommon.SetProperty("#MovingPictures.UserMovieSettings.10point_user_rating", (movieToRate.ActiveUserSettings.UserRating * 2).ToString());
                    }

                    if (movieToRate.Popularity == 0 && movieToRate.Score == 0)
                    {
                        movieToRate.Score = rating;
                        movieToRate.Popularity = 1;
                    }
                }
                else
                {
                    // unrate
                    TraktLogger.Info("Removing rating for movie. Title = '{0}', Year = '{1}', IMDB ID = '{2}', TMDb ID = '{3}'", movie.Title, movie.Year, movie.ImdbID ?? "<empty>", GetTmdbID(movie) ?? "<empty>");
                    movieToRate.ActiveUserSettings.UserRating = 0;

                    // Make sure we're still showing the active movie
                    if (GUIUtils.GetProperty("#MovingPictures.SelectedMovie.title").Equals(movieToRate.Title))
                    {
                        GUICommon.SetProperty("#MovingPictures.UserMovieSettings.user_rating", " ");
                        GUICommon.SetProperty("#MovingPictures.UserMovieSettings.10point_user_rating", " ");
                    }

                    if (movieToRate.Popularity == 1)
                    {
                        movieToRate.Score = 0;
                        movieToRate.Popularity = 0;
                    }
                }

                movieToRate.Commit();
                TraktRateSent = false;
            })
            {
                Name = "Rate",
                IsBackground = true
            };

            rateThread.Start(movie);
        }

        #endregion

        #region Other Public Methods

        public void DisposeEvents()
        {
            TraktLogger.Debug("Removing Hooks from Moving Pictures Database");
            MovingPicturesCore.DatabaseManager.ObjectInserted -= new DatabaseManager.ObjectAffectedDelegate(DatabaseManager_ObjectInserted);
            MovingPicturesCore.DatabaseManager.ObjectUpdatedEx -= new DatabaseManager.ObjectUpdatedDelegate(DatabaseManager_ObjectUpdatedEx);
            MovingPicturesCore.DatabaseManager.ObjectDeleted -= new DatabaseManager.ObjectAffectedDelegate(DatabaseManager_ObjectDeleted);
        }

        public static string GetTmdbID(DBMovieInfo movie)
        {
            if (tmdbSource == null) return null;

            string id = movie.GetSourceMovieInfo(tmdbSource).Identifier;
            if (id == null || id.Trim() == string.Empty) return null;

            // TMDb ID must be an integer greater than zero
            int result = 0;
            if (!int.TryParse(id, out result))
                return null;

            if (result <= 0) return null;

            return id;
        }

        public static bool FindMovieID(string title, int year, string imdbid, ref int? movieID)
        {
            // get all movies in local database
            List<DBMovieInfo> movies = DBMovieInfo.GetAll();

            // try find a match
            DBMovieInfo movie = movies.Find(m => BasicHandler.GetProperImdbId(m.ImdbID) == imdbid || (string.Compare(m.Title, title, true) == 0 && m.Year == year));
            if (movie == null)
            {
                TraktLogger.Info("Found no movies for search criteria. Title = '{0}', Year = '{1}", title, year);
                return false;
            }

            movieID = movie.ID;
            return true;
        }

        public static bool GetMoviePersonInfo(int? movieID, out SearchPeople searchPeople)
        {
            searchPeople = new SearchPeople();

            if (movieID == null) return false;

            var movies = DBMovieInfo.GetAll();
            var selectedMovie = movies.Find(m => m.ID == movieID);
            if (selectedMovie == null) return false;

            foreach (var actor in selectedMovie.Actors)
            {
                searchPeople.Actors.Add(actor);
            }
            foreach (var writer in selectedMovie.Writers)
            {
                searchPeople.Writers.Add(writer);
            }
            foreach (var director in selectedMovie.Directors)
            {
                searchPeople.Directors.Add(director);
            }

            return true;
        }
        
        public static void PlayMovie(int? movieID)
        {
            if (movieID == null) return;

            // get all movies in local database
            List<DBMovieInfo> movies = DBMovieInfo.GetAll();

            // try find a match
            DBMovieInfo movie = movies.Find(m => m.ID == movieID);

            if (movie == null) return;
            PlayMovie(movie);
        }

        public static void PlayMovie(DBMovieInfo movie)
        {
            if (player == null) player = new MoviePlayer(new MovingPicturesGUI());
            player.Play(movie);
        }

        public static void UpdateSettingAsBool(string setting, bool value)
        {
            if (MovingPicturesCore.Settings.ContainsKey(setting))
            {
                MovingPicturesCore.Settings[setting].Value = value;
            }
        }

        #endregion

        #region Categories and Filters menu

        /// <summary>
        /// Returns whether the Trakt node exists in the Categories menu
        /// </summary>
        static bool TraktCategoriesMenuExists
        {
            get
            {
                if (MovingPicturesCore.Settings.CategoriesMenu == null || MovingPicturesCore.Settings.CategoriesMenu.RootNodes == null)
                    return false;

                return TraktCategoriesMenuRootNode != null;
            }
        }

        /// <summary>
        /// Returns whether the Trakt node exists in the Filters menu
        /// </summary>
        static bool TraktFiltersMenuExists
        {
            get
            {
                if (MovingPicturesCore.Settings.FilterMenu == null || MovingPicturesCore.Settings.FilterMenu.RootNodes == null)
                    return false;
                
                return TraktFiltersMenuRootNode != null;
            }
        }

        /// <summary>
        /// Gets the Trakt parent node from the Categories menu
        /// </summary>
        static DBNode<DBMovieInfo> TraktCategoriesMenuRootNode
        {
            get
            {
                return MovingPicturesCore.Settings.CategoriesMenu.RootNodes.FirstOrDefault(n => n.Name == "${Trakt}");
            }
        }

        /// <summary>
        /// Gets the Trakt parent node from the Filters menu
        /// </summary>
        static DBNode<DBMovieInfo> TraktFiltersMenuRootNode
        {
            get
            {
                return MovingPicturesCore.Settings.FilterMenu.RootNodes.FirstOrDefault(n => n.Name == "${Trakt}");
            }
        }

        /// <summary>
        /// Removes top level trakt node from MovingPictures categories
        /// </summary>
        internal static void RemoveTraktFromCategoryMenu()
        {
            // nothing to delete
            if (!TraktCategoriesMenuExists)
                return;

            TraktLogger.Info("Removing trakt node from categories menu.");

            var traktNode = TraktCategoriesMenuRootNode;

            // remove the node from the categories menu
            MovingPicturesCore.Settings.CategoriesMenu.RootNodes.Remove(traktNode);
            MovingPicturesCore.Settings.CategoriesMenu.RootNodes.Commit();

            TraktLogger.Info("Finished removing trakt node from categories menu.");

            // delete the trakt node
            traktNode.Delete();
        }

        /// <summary>  
        /// Removes top level trakt node from MovingPictures filters
        /// </summary>
        internal static void RemoveTraktFromFiltersMenu()
        {
            // nothing to delete
            if (!TraktFiltersMenuExists)
                return;

            TraktLogger.Info("Removing trakt node from filters menu.");

            var traktNode = TraktFiltersMenuRootNode;

            // remove the node from the filters menu
            MovingPicturesCore.Settings.FilterMenu.RootNodes.Remove(traktNode);
            MovingPicturesCore.Settings.FilterMenu.RootNodes.Commit();

            TraktLogger.Info("Finished removing trakt node from filters menu.");

            // delete the trakt node
            traktNode.Delete();
        }

        /// <summary>
        /// Creates a node in a menu
        /// </summary>
        /// <param name="menu">Categories or Filters menu</param>
        /// <param name="name">Name of node to create</param>
        static void CreateNodeInMenu(DBMenu<DBMovieInfo> menu, string name)
        {
            var node = new DBNode<DBMovieInfo>();
            node.Name = name;

            var nodeSettings = new DBMovieNodeSettings();
            node.AdditionalSettings = nodeSettings;

            // Add node to the end
            node.SortPosition = menu.RootNodes.Count + 1;

            // Add to root node
            menu.RootNodes.Add(node);

            // Committing node to menu
            menu.Commit();
        }

        /// <summary>
        /// Created a child node
        /// </summary>
        /// <param name="rootNode">The parent node to create a node under</param>
        /// <param name="name">Name of new child node</param>
        /// <returns>Returns the existing child node if it exists otherwise creates a new one</returns>
        static DBNode<DBMovieInfo> CreateNode(DBNode<DBMovieInfo> rootNode, string name, bool createBlacklist = false)
        {
            if (rootNode == null) return null;

            string nodeName = name;
            if (!nodeName.StartsWith("$"))
                nodeName = string.Format("${{{0}}}", name);

            // check if the node exists, if not create it
            var node = rootNode.Children.FirstOrDefault(n => n.Name == nodeName);
            
            // we have it, nothing else to do
            if (node != null)
                return node;

            TraktLogger.Info("Creating child node '{0}' under Trakt", name);

            // create the node with default settings
            node = new DBNode<DBMovieInfo> { Name = nodeName };

            var nodeSettings = new DBMovieNodeSettings();
            node.AdditionalSettings = nodeSettings;

            if (createBlacklist)
            {
                TraktLogger.Info("Adding dummy blacklist criteria to the '{0}' node filter", name);
                node.Filter = new DBFilter<DBMovieInfo>();
                node.Filter.CriteriaGrouping = DBFilter<DBMovieInfo>.CriteriaGroupingEnum.ONE;
                node.Filter.Name = string.Format("{0} Filter", nodeName);
                AddDummyBlacklistToFilter(node.Filter);
            }

            // add as a child to the root node
            node.Parent = rootNode;
            rootNode.Children.Add(node);
            rootNode.Commit();

            return node;
        }

        /// <summary>
        /// Removes a child node
        /// </summary>
        /// <param name="rootNode">Parent node to remove node from</param>
        /// <param name="name">The name of the child node to remove</param>
        static void RemoveNode(DBNode<DBMovieInfo> rootNode, string name)
        {
            if (rootNode == null) return;

            string nodeName = name;
            if (!nodeName.StartsWith("$"))
                nodeName = string.Format("${{{0}}}", name);

            // check if the node exists before removing
            var node = rootNode.Children.FirstOrDefault(n => n.Name == nodeName);
            if (node == null) return;

            rootNode.Children.Remove(node);
            rootNode.Children.Commit();            

            node.Delete();
        }

        /// <summary>
        /// Removes a custom list from the Trakt Categories and Filters menu
        /// Only should remove a node if it no longer exists online at trakt.tv
        /// </summary>
        /// <param name="listName">Name of the list to remove</param>
        internal static void RemoveCustomListNode(string listName)
        {
            if (TraktSettings.MovingPicturesCategories && TraktCategoriesMenuExists)
            {
                TraktLogger.Info("Removing custom list '{0}' from trakt categories menu.", listName);
                RemoveNode(TraktCategoriesMenuRootNode, listName);
            }

            if (TraktSettings.MovingPicturesFilters && TraktFiltersMenuExists)
            {
                TraktLogger.Info("Removing custom list '{0}' from trakt filters menu.", listName);
                RemoveNode(TraktFiltersMenuRootNode, listName);
            }

            // clear cached list
            TraktCache.ClearCustomListCache(listName);
        }

        /// <summary>
        /// Add a new node for a Custom list e.g. new list created
        /// A blacklist filter will be created until items are adding to the list
        /// </summary>
        /// <param name="listName">Name of list to create</param>
        internal static void AddCustomListNode(string listName)
        {
            if (TraktSettings.MovingPicturesCategories && TraktCategoriesMenuExists)
            {
                TraktLogger.Info("Adding custom list '{0}' to trakt categories menu.", listName);
                CreateNode(TraktCategoriesMenuRootNode, listName, true);
            }

            if (TraktSettings.MovingPicturesFilters && TraktFiltersMenuExists)
            {
                TraktLogger.Info("Adding custom list '{0}' to trakt filters menu.", listName);
                CreateNode(TraktFiltersMenuRootNode, listName, true);
            }

            // clear cache
            TraktCache.ClearCustomListCache();
        }

        /// <summary>
        /// Creates a dummy blacklist list constraint as opposed to a real Blacklist.
        /// A real blacklist can add a huge amount of records to the filters table.
        /// The purpose of the blacklist in this context is to simply ensure that there is 
        /// always at least one criteria added to a filter so that it doesn't show All Movies.
        /// </summary>
        /// <param name="filter">The Filter that requires a dummy blacklist criteria</param>
        static void AddDummyBlacklistToFilter(DBFilter<DBMovieInfo> filter)
        {
            if (filter == null) return;

            // clear any legitimate blacklist if set
            filter.BlackList.Clear();

            // when we're managing a blacklist for a nodes filter
            // we typically need to add a record for each movie in the users database
            // this can result in a large amount of records inserted into the Filters table
            // instead, let's create a dummy criteria and add it the filter e.g. a movie that will never match
            var criteria = new DBCriteria<DBMovieInfo>();
            criteria.Field = DBField.GetFieldByDBName(typeof(DBMovieInfo), "imdb_id");
            criteria.Operator = DBCriteria<DBMovieInfo>.OperatorEnum.EQUAL;
            criteria.Value = "ttDummyBlacklist";

            // add the criteria to the filter
            filter.Criteria.Add(criteria);
        }

        /// <summary>
        /// Add criteria (IMDb ID's) for a movie list to a nodes filter criteria
        /// We don't need to worry about movies that don't exist as they will simply not be visible
        /// </summary>
        /// <param name="name">Name of child node to add filters to</param>
        /// <param name="movieIMDbList">List of IMDb ID's to add </param>
        static void AddMoviesCriteriaToNode(DBNode<DBMovieInfo> node, IEnumerable<string> movieIMDbList)
        {
            if (node == null) return;

            // clear existing filter
            if (node.HasFilter)
            {
                node.Filter.WhiteList.Clear();
                node.Filter.BlackList.Clear();
                node.Filter.Delete();
            }

            // create a new filter, such that any criteria will match
            node.Filter = new DBFilter<DBMovieInfo>();
            node.Filter.CriteriaGrouping = DBFilter<DBMovieInfo>.CriteriaGroupingEnum.ONE;
            node.Filter.Name = string.Format("{0} Filter", node.Name);

            // add criteria for each movie
            foreach (var movieId in movieIMDbList)
            {
                if (string.IsNullOrEmpty(movieId)) continue;

                TraktLogger.Debug("Adding criteria to the '{0}' node filter, Field = 'imdb_id', Value = '{1}'", node.Name, movieId);

                var criteria = new DBCriteria<DBMovieInfo>();
                criteria.Field = DBField.GetFieldByDBName(typeof(DBMovieInfo), "imdb_id");
                criteria.Operator = DBCriteria<DBMovieInfo>.OperatorEnum.EQUAL;
                criteria.Value = movieId;

                // add the criteria to the filter
                node.Filter.Criteria.Add(criteria);
            }

            if (node.Filter.Criteria.Count == 0)
            {
                TraktLogger.Info("Adding dummy blacklist criteria to the '{0}' node filter.", node.Name);
                AddDummyBlacklistToFilter(node.Filter);
            }

            node.Commit();
        }

        /// <summary>
        /// Adds a movie criteria to a node
        /// </summary>
        /// <param name="node">Node to add movie criteria to</param>
        /// <param name="movieId">IMDb movie ID used for the criteria</param>
        static void AddMovieCriteriaToNode(DBNode<DBMovieInfo> node, string movieId)
        {
            if (node == null || !BasicHandler.IsValidImdb(movieId)) return;

            // check existing filter exists
            if (!node.HasFilter) return;

            // add movie id as a criteria
            var criteria = new DBCriteria<DBMovieInfo>();
            criteria.Field = DBField.GetFieldByDBName(typeof(DBMovieInfo), "imdb_id");
            criteria.Operator = DBCriteria<DBMovieInfo>.OperatorEnum.EQUAL;
            criteria.Value = movieId;

            // add the criteria to the filter
            node.Filter.Criteria.Add(criteria);
            node.Commit();
        }

        /// <summary>
        /// Removes a movie criteria from a node
        /// </summary>
        /// <param name="movie">The movie to remove from the nodes criteria filter</param>
        /// <param name="rootNode">The node that the criteria filter belongs to</param>
        static void RemoveMovieCriteriaFromNode(DBNode<DBMovieInfo> node, string movieId)
        {
            if (!BasicHandler.IsValidImdb(movieId))
                return;

            if (!node.HasFilter || string.IsNullOrEmpty(movieId)) return;

            // find critera match in the nodes filter and then remove
            var criteria = node.Filter.Criteria.FirstOrDefault(c => c.Value.ToString() == movieId);
            if (criteria != null)
                node.Filter.Criteria.Remove(criteria);

            if (node.Filter.Criteria.Count == 0)
            {
                TraktLogger.Info("Adding dummy blacklist criteria to the child '{0}' node filter.", node.Name);
                AddDummyBlacklistToFilter(node.Filter);
            }

            node.Commit();
        }

        /// <summary>
        /// Removes a movie criteria from the Watchlist node in the Categories and Filters menu(s)
        /// </summary>
        /// <param name="movie">IMDb movie ID used for the criteria</param>
        internal static void RemoveMovieCriteriaFromWatchlistNode(string movieId)
        {
            if (!BasicHandler.IsValidImdb(movieId))
                return;

            // remove from categories menu
            if (TraktSettings.MovingPicturesCategories && TraktCategoriesMenuExists)
            {
                TraktLogger.Debug("Removing movie from the watchlist node in the categories menu. Criteria = '{0}'", movieId);
                var node = GetNodeByName(TraktCategoriesMenuRootNode ,GUI.Translation.WatchList);
                if (node != null)
                {
                    RemoveMovieCriteriaFromNode(node, movieId);
                }
            }

            // remove from filters menu
            if (TraktSettings.MovingPicturesFilters && TraktFiltersMenuExists)
            {
                TraktLogger.Debug("Removing movie from the watchlist node in the filters menu. Criteria = '{0}'", movieId);
                var node = GetNodeByName(TraktFiltersMenuRootNode, GUI.Translation.WatchList);
                if (node != null)
                {
                    RemoveMovieCriteriaFromNode(node, movieId);
                }
            }

            // clear the watchlist cache as it's now out of sync
            TraktCache.ClearWatchlistMoviesCache();
        }

        /// <summary>
        /// Adds a movie criteria to the Watchlist node in the Categories and Filters menu(s)
        /// </summary>
        /// <param name="movieId">IMDb movie ID used for the criteria</param>
        internal static void AddMovieCriteriaToWatchlistNode(string movieId)
        {
            if (!BasicHandler.IsValidImdb(movieId))
                return;

            // add to categories menu
            if (TraktSettings.MovingPicturesCategories && TraktCategoriesMenuExists)
            {
                TraktLogger.Info("Added movie '{0}' to the watchlist node in the categories menu.", movieId);
                var node = GetNodeByName(TraktCategoriesMenuRootNode, GUI.Translation.WatchList);
                if (node != null)
                {
                    AddMovieCriteriaToNode(node, movieId);
                }
            }

            // add to filters menu
            if (TraktSettings.MovingPicturesFilters && TraktFiltersMenuExists)
            {
                TraktLogger.Info("Adding movie '{0}' to the watchlist node in the filters menu.", movieId);
                var node = GetNodeByName(TraktFiltersMenuRootNode, GUI.Translation.WatchList);
                if (node != null)
                {
                    AddMovieCriteriaToNode(node, movieId);
                }
            }

            // clear the watchlist cache as it's now out of sync
            TraktCache.ClearCustomListCache();
        }

        /// <summary>
        /// Removes a movie criteria from the Recommendations node in the Categories and Filters menu(s)
        /// </summary>
        /// <param name="movie">IMDb movie ID used for the criteria</param>
        internal static void RemoveMovieCriteriaFromRecommendationsNode(string movieId)
        {
            if (!BasicHandler.IsValidImdb(movieId))
                return;

            // remove from categories menu
            if (TraktSettings.MovingPicturesCategories && TraktCategoriesMenuExists)
            {
                TraktLogger.Debug("Removing movie from the recommendations node in the categories menu. Criteria = '{0}'", movieId);
                var node = GetNodeByName(TraktCategoriesMenuRootNode, GUI.Translation.Recommendations);
                if (node != null)
                {
                    RemoveMovieCriteriaFromNode(node, movieId);
                }
            }

            // remove from filters menu
            if (TraktSettings.MovingPicturesFilters && TraktFiltersMenuExists)
            {
                TraktLogger.Debug("Removing movie from the recommendations node in the filters menu. Criteria = '{0}'", movieId);
                var node = GetNodeByName(TraktFiltersMenuRootNode, GUI.Translation.Recommendations);
                if (node != null)
                {
                    RemoveMovieCriteriaFromNode(node, movieId);
                }
            }

            // clear the recommendations cache as it's now out of sync
            TraktCache.ClearRecommendationsCache();
        }

        /// <summary>
        /// Adds a movie criteria to a custom list node in the Categories and Filters menu(s)
        /// </summary>
        /// <param name="movieId">IMDb movie ID used for the criteria</param>
        /// <param name="listName">Name of list to add to</param>
        internal static void AddMovieCriteriaToCustomlistNode(string listName, string movieId)
        {
            if (!BasicHandler.IsValidImdb(movieId))
                return;

            // add to categories menu
            if (TraktSettings.MovingPicturesCategories && TraktCategoriesMenuExists)
            {
                TraktLogger.Info("Added movie '{0}' to the custom list '{1}' node in the categories menu.", movieId, listName);
                var node = GetNodeByName(TraktCategoriesMenuRootNode, listName);
                if (node != null)
                {
                    AddMovieCriteriaToNode(node, movieId);
                }
            }

            // add to filters menu
            if (TraktSettings.MovingPicturesFilters && TraktFiltersMenuExists)
            {
                TraktLogger.Info("Added movie '{0}' to the custom list '{1}' node in the filters menu.", movieId, listName);
                var node = GetNodeByName(TraktFiltersMenuRootNode, listName);
                if (node != null)
                {
                    AddMovieCriteriaToNode(node, movieId);
                }
            }

            // clear the custom list cache as it's now out of sync
            TraktCache.ClearCustomListCache(listName);
        }

        /// <summary>
        /// Removes a movie criteria from a custom list node in the Categories and Filters menu(s)
        /// </summary>
        /// <param name="movie">IMDb movie ID used for the criteria</param>
        /// <param name="listName">Name of list to remove from</param>
        internal static void RemoveMovieCriteriaFromCustomlistNode(string listName, string movieId)
        {
            if (!BasicHandler.IsValidImdb(movieId))
                return;

            // remove from categories menu
            if (TraktSettings.MovingPicturesCategories && TraktCategoriesMenuExists)
            {
                TraktLogger.Debug("Removing movie from the custom list in the categories menu. Node = '{0}', Criteria = '{1}'", listName, movieId);
                var node = GetNodeByName(TraktCategoriesMenuRootNode, listName);
                if (node != null)
                {
                    RemoveMovieCriteriaFromNode(node, movieId);
                }
            }

            // remove from filters menu
            if (TraktSettings.MovingPicturesFilters && TraktFiltersMenuExists)
            {
                TraktLogger.Debug("Removing movie from the custom list in the filters menu. Node = '{0}', Criteria = '{1}'", listName, movieId);
                var node = GetNodeByName(TraktFiltersMenuRootNode, listName);
                if (node != null)
                {
                    RemoveMovieCriteriaFromNode(node, movieId);
                }
            }

            // clear the customlist cache as it's now out of sync
            TraktCache.ClearCustomListCache();
        }

        /// <summary>
        /// Gets a child node by name from a parent node
        /// </summary>
        /// <param name="rootNode">The parent node the child node belongs to</param>
        /// <param name="name">The name of the node to retrieve</param>
        static DBNode<DBMovieInfo> GetNodeByName(DBNode<DBMovieInfo> rootNode, string name)
        {
            if (!rootNode.HasChildren) return null;

            return rootNode.Children.FirstOrDefault(n => n.Name == string.Format("${{{0}}}", name));
        }

        /// <summary>
        /// Creates/Updates Trakt categories menu
        /// </summary>
        /// <param name="syncLists">Specify which lists to sync</param>
        internal static void UpdateCategoriesMenu(SyncListType syncLists = SyncListType.All)
        {
            // create root node if it doesn't exist
            if (!TraktCategoriesMenuExists)
            {
                TraktLogger.Info("Trakt node does not exist in categories menu, creating now");
                CreateNodeInMenu(MovingPicturesCore.Settings.CategoriesMenu, "${Trakt}");
                MovingPicturesCore.Settings.CategoriesMenu.Commit();
            }

            // now we're ready to create all the child-nodes for the recommendations, watchlist and custom lists
            var recommendations = TraktCache.GetRecommendedMoviesFromTrakt();
            if (recommendations != null && (syncLists & SyncListType.Recommendations) != 0)
            {
                TraktLogger.Info("Found {0} recommended movies on trakt.tv", recommendations.Count());
                var recommendationsNode = CreateNode(TraktCategoriesMenuRootNode, GUI.Translation.Recommendations);

                // add criteria to the nodes filter
                TraktLogger.Info("Adding recommendations from trakt.tv to the categories menu");
                AddMoviesCriteriaToNode(recommendationsNode, recommendations.Select(m => m.Ids.Imdb));
            }

            var watchlist = TraktCache.GetWatchlistedMoviesFromTrakt();
            if (watchlist != null && (syncLists & SyncListType.Watchlist) != 0)
            {
                TraktLogger.Info("Found {0} watchlist movies on trakt.tv", watchlist.Count());
                var watchListNode = CreateNode(TraktCategoriesMenuRootNode, GUI.Translation.WatchList);

                // add criteria to the nodes filter
                TraktLogger.Info("Adding users watchlist from trakt.tv to the categories menu");
                AddMoviesCriteriaToNode(watchListNode, watchlist.Select(m => m.Movie.Ids.Imdb));
            }

            var customLists = TraktCache.CustomLists;
            if (customLists != null && (syncLists & SyncListType.CustomList) != 0)
            {
                TraktLogger.Info("Found {0} custom lists on trakt.tv", customLists.Count());

                foreach (var list in customLists)
                {
                    string listName = list.Key.Name;
                    List<TraktListItem> listItems = list.Value;

                    var listNode = CreateNode(TraktCategoriesMenuRootNode, listName);

                    // add criteria to the nodes filter, only add criteria for movie items
                    TraktLogger.Info("Adding custom list from trakt.tv to the categories menu. Name = '{0}' Total Movie Items = '{1}'", listName, listItems.Where(i => i.Movie != null).Count());
                    AddMoviesCriteriaToNode(listNode, listItems.Where(i => i.Movie != null).Select(m => m.Movie.Ids.Imdb));
                }

                // Remove any menu items that no longer have associated lists on trakt
                var currentNodes = new List<DBNode<DBMovieInfo>>(TraktCategoriesMenuRootNode.Children);
                foreach (var node in currentNodes)
                {
                    if (node.Name == string.Format("${{{0}}}", GUI.Translation.WatchList) ||
                        node.Name == string.Format("${{{0}}}", GUI.Translation.Recommendations))
                        continue;

                    if (!TraktCache.CustomLists.Keys.Any(key => string.Format("${{{0}}}", key.Name) == node.Name))
                    {
                        TraktLogger.Info("Removing node '{0}' from categories menu as custom list no longer exists online", node.Name);
                        RemoveNode(TraktCategoriesMenuRootNode, node.Name);
                    }
                }
            }
        }

        /// <summary>
        /// Creates/Updates Trakt filters menu
        /// </summary>
        /// <param name="syncLists">Specify which lists to sync</param>
        internal static void UpdateFiltersMenu(SyncListType syncLists = SyncListType.All)
        {
            // create root node if it doesn't exist
            if (!TraktFiltersMenuExists)
            {
                TraktLogger.Info("Trakt node does not exist in filters menu, creating now");
                CreateNodeInMenu(MovingPicturesCore.Settings.FilterMenu, "${Trakt}");
                MovingPicturesCore.Settings.FilterMenu.Commit();
            }

            // now we're ready to create all the sub-nodes for the recommendations, watchlist and custom lists
            var recommendations = TraktCache.GetRecommendedMoviesFromTrakt();
            if (recommendations != null && (syncLists & SyncListType.Recommendations) != 0)
            {
                TraktLogger.Info("Found {0} recommended movies on trakt.tv", recommendations.Count());
                var recommendationsNode = CreateNode(TraktFiltersMenuRootNode, GUI.Translation.Recommendations);

                // add criteria to the nodes filter
                TraktLogger.Info("Adding recommendations from trakt.tv to the categories menu");
                AddMoviesCriteriaToNode(recommendationsNode, recommendations.Select(m => m.Ids.Imdb));
            }

            var watchlist = TraktCache.GetWatchlistedMoviesFromTrakt();
            if (watchlist != null && (syncLists & SyncListType.Watchlist) != 0)
            {
                TraktLogger.Info("Found {0} watchlist movies on trakt.tv", watchlist.Count());
                var watchListNode = CreateNode(TraktFiltersMenuRootNode, GUI.Translation.WatchList);

                // add criteria to the nodes filter
                TraktLogger.Info("Adding users watchlist from trakt.tv to the categories menu");
                AddMoviesCriteriaToNode(watchListNode, watchlist.Select(m => m.Movie.Ids.Imdb));
            }

            var customLists = TraktCache.CustomLists;
            if (customLists != null && (syncLists & SyncListType.CustomList) != 0)
            {
                TraktLogger.Info("Found {0} custom lists on trakt.tv", customLists.Count());

                foreach (var list in TraktCache.CustomLists)
                {
                    string listName = list.Key.Name;
                    List<TraktListItem> listItems = list.Value;

                    var listNode = CreateNode(TraktFiltersMenuRootNode, listName);

                    // add criteria to the nodes filter, only add criteria for movie items
                    TraktLogger.Info("Adding custom list from trakt.tv to the filters menu. Name = '{0}' Total Movie Items = '{1}'", listName, listItems.Where(i => i.Movie != null).Count());
                    AddMoviesCriteriaToNode(listNode, listItems.Where(i => i.Movie != null).Select(m => m.Movie.Ids.Imdb));
                }

                // Remove any menu items that no longer have associated lists on trakt
                var currentNodes = new List<DBNode<DBMovieInfo>>(TraktFiltersMenuRootNode.Children);
                foreach (var node in currentNodes)
                {
                    if (node.Name == string.Format("${{{0}}}", GUI.Translation.WatchList) ||
                        node.Name == string.Format("${{{0}}}", GUI.Translation.Recommendations))
                        continue;

                    if (!TraktCache.CustomLists.Keys.Any(key => string.Format("${{{0}}}", key.Name) == node.Name))
                    {
                        TraktLogger.Info("Removing node '{0}' from filters menu as custom list no longer exists online", node.Name);
                        RemoveNode(TraktFiltersMenuRootNode, node.Name);
                    }
                }
            }
        }

        #endregion
    }
}
