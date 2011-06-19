using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaPortal.Plugins.MovingPictures;
using MediaPortal.Plugins.MovingPictures.Database;
using TraktPlugin.TraktAPI;
using TraktPlugin.TraktAPI.DataStructures;
using System.Timers;
using MediaPortal.Player;
using System.Reflection;
using System.ComponentModel;


namespace TraktPlugin.TraktHandlers
{
    /// <summary>
    /// Support for MovingPictures
    /// </summary>
    class MovingPictures : ITraktHandler
    {
        Timer traktTimer;
        DBMovieInfo currentMovie;
        bool SyncInProgress;

        public MovingPictures(int priority)
        {
            Priority = priority;
            TraktLogger.Debug("Adding Hooks to Moving Pictures Database");
            MovingPicturesCore.DatabaseManager.ObjectInserted += new Cornerstone.Database.DatabaseManager.ObjectAffectedDelegate(DatabaseManager_ObjectInserted);
            MovingPicturesCore.DatabaseManager.ObjectUpdated += new Cornerstone.Database.DatabaseManager.ObjectAffectedDelegate(DatabaseManager_ObjectUpdated);
            MovingPicturesCore.DatabaseManager.ObjectDeleted += new Cornerstone.Database.DatabaseManager.ObjectAffectedDelegate(DatabaseManager_ObjectDeleted);
        }

        #region ITraktHandler

        public string Name { get { return "Moving Pictures"; } }
        public int Priority { get; set; }
        
        public void SyncLibrary()
        {
            TraktLogger.Info("Moving Pictures Starting Sync");
            SyncInProgress = true;

            //Get all movies in our local database
            List<DBMovieInfo> MovieList = DBMovieInfo.GetAll();            

            //Remove any blocked movies
            MovieList.RemoveAll(movie => TraktSettings.BlockedFolders.Any(f => movie.LocalMedia[0].FullPath.Contains(f)));
            MovieList.RemoveAll(movie => TraktSettings.BlockedFilenames.Contains(movie.LocalMedia[0].FullPath));

            TraktLogger.Info("{0} movies available to sync in MovingPictures database", MovieList.Count.ToString());

            //Get the movies that we have watched
            List<DBMovieInfo> SeenList = MovieList.Where(m => m.ActiveUserSettings.WatchedCount > 0).ToList();

            TraktLogger.Info("{0} watched movies available to sync in MovingPictures database", SeenList.Count.ToString());

            //Get all movies we have in our library including movies in users collection            
            IEnumerable<TraktLibraryMovies> traktMoviesAll = TraktAPI.TraktAPI.GetAllMoviesForUser(TraktSettings.Username);
            TraktLogger.Info("{0} movies in trakt library", traktMoviesAll.Count().ToString());

            #region Movies to Sync to Collection
            //Filter out a list of movies we have already sync'd in our collection
            List<TraktLibraryMovies> NoLongerInOurCollection = new List<TraktLibraryMovies>();
            List<DBMovieInfo> moviesToSync = new List<DBMovieInfo>(MovieList);
            foreach (TraktLibraryMovies tlm in traktMoviesAll)
            {
                bool notInLocalCollection = true;
                foreach (DBMovieInfo movie in MovieList.Where(m => GetProperMovieId(m.ImdbID) == tlm.IMDBID || (m.Title == tlm.Title && m.Year.ToString() == tlm.Year)))
                {
                    //If the users IMDB ID is empty and we have matched one then set it
                    if(!String.IsNullOrEmpty(tlm.IMDBID) && (String.IsNullOrEmpty(movie.ImdbID) || movie.ImdbID.Length != 9))
                    {
                        TraktLogger.Info("Movie '{0}' inserted IMDBID '{1}'", movie.Title, tlm.IMDBID);
                        movie.ImdbID = tlm.IMDBID;
                        movie.Commit();
                    }

                    // If it is watched in Trakt but not Moving Pictures update
                    // skip if movie is watched but user wishes to have synced as unseen locally
                    if (tlm.Plays > 0 && !tlm.UnSeen && movie.ActiveUserSettings.WatchedCount == 0)
                    {
                        TraktLogger.Info("Movie '{0}' is watched on Trakt, updating database", movie.Title);
                        movie.ActiveUserSettings.WatchedCount = 1;
                        movie.Commit();
                    }

                    // mark movies as unseen if watched locally
                    if (tlm.UnSeen && movie.ActiveUserSettings.WatchedCount > 0)
                    {
                        TraktLogger.Info("Movie '{0}' is unseen on Trakt, updating database", movie.Title);
                        movie.ActiveUserSettings.WatchedCount = 0;
                        movie.ActiveUserSettings.Commit();
                    }

                    notInLocalCollection = false;

                    //filter out if its already in collection
                    if (tlm.InCollection)
                    {
                        if (!string.IsNullOrEmpty(tlm.IMDBID))
                            moviesToSync.RemoveAll(m => m.ImdbID == tlm.IMDBID);
                        else
                            moviesToSync.RemoveAll(m => m.Title == tlm.Title && m.Year.ToString() == tlm.Year);
                    }
                    break;
                }

                if (notInLocalCollection && tlm.InCollection)
                    NoLongerInOurCollection.Add(tlm);
            }
            #endregion

            #region Movies to Sync to Seen Collection
            // Filter out a list of movies already marked as watched on trakt
            // also filter out movie marked as unseen so we dont reset the unseen cache online
            List<DBMovieInfo> watchedMoviesToSync = new List<DBMovieInfo>(SeenList);
            foreach (TraktLibraryMovies tlm in traktMoviesAll.Where(t => t.Plays > 0 || t.UnSeen))
            {
                foreach (DBMovieInfo watchedMovie in SeenList.Where(m => GetProperMovieId(m.ImdbID) == tlm.IMDBID || (m.Title == tlm.Title && m.Year.ToString() == tlm.Year)))
                {
                    //filter out
                    watchedMoviesToSync.Remove(watchedMovie);
                }
            }
            #endregion

            //Send Library/Collection
            TraktLogger.Info("{0} movies need to be added to Library", moviesToSync.Count.ToString());
            foreach (DBMovieInfo m in moviesToSync)
                TraktLogger.Info("Sending movie to trakt library, Title: {0}, Year: {1}, IMDB: {2}", m.Title, m.Year.ToString(), m.ImdbID);

            if (moviesToSync.Count > 0)
            {
                TraktMovieSyncResponse response = TraktAPI.TraktAPI.SyncMovieLibrary(CreateSyncData(moviesToSync), TraktSyncModes.library);
                TraktAPI.TraktAPI.LogTraktResponse(response);
            }

            //Send Seen
            TraktLogger.Info("{0} movies need to be added to SeenList", watchedMoviesToSync.Count.ToString());
            foreach (DBMovieInfo m in watchedMoviesToSync)
                TraktLogger.Info("Sending movie to trakt as seen, Title: {0}, Year: {1}, IMDB: {2}", m.Title, m.Year.ToString(), m.ImdbID);

            if (watchedMoviesToSync.Count > 0)
            {
                TraktMovieSyncResponse response = TraktAPI.TraktAPI.SyncMovieLibrary(CreateSyncData(watchedMoviesToSync), TraktSyncModes.seen);
                TraktAPI.TraktAPI.LogTraktResponse(response);
            }

            //Dont clean library if more than one movie plugin installed
            if (TraktSettings.KeepTraktLibraryClean && TraktSettings.MoviePluginCount == 1)
            {
                //Remove movies we no longer have in our local database from Trakt
                foreach (var m in NoLongerInOurCollection)
                    TraktLogger.Info("Removing from Trakt Collection {0}", m.Title);

                TraktLogger.Info("{0} movies need to be removed from Trakt Collection", NoLongerInOurCollection.Count.ToString());

                if (NoLongerInOurCollection.Count > 0)
                {
                    //Then remove from library
                    TraktMovieSyncResponse response = TraktAPI.TraktAPI.SyncMovieLibrary(BasicHandler.CreateMovieSyncData(NoLongerInOurCollection), TraktSyncModes.unlibrary);
                    TraktAPI.TraktAPI.LogTraktResponse(response);
                }
            }

            SyncInProgress = false;
            TraktLogger.Info("Moving Pictures Sync Completed");

        }

        public bool Scrobble(String filename)
        {
            StopScrobble();
            List<DBMovieInfo> searchResults = (from m in DBMovieInfo.GetAll() where (from path in m.LocalMedia select path.FullPath).ToList().Contains(filename) select m).ToList();
            if (searchResults.Count == 1)
            {
                //Create timer
                currentMovie = searchResults[0];
                TraktLogger.Info(string.Format("Found playing movie {0}", currentMovie.Title));
                ScrobbleHandler(currentMovie, TraktScrobbleStates.watching);
                traktTimer = new Timer();
                traktTimer.Interval = 900000;
                traktTimer.Elapsed += new ElapsedEventHandler(traktTimer_Elapsed);
                traktTimer.Start();
                return true;
            }
            else if (searchResults.Count == 0)
                TraktLogger.Debug("Playback started but Movie not found");
            else
                TraktLogger.Debug("Multiple movies found for filename something is up!");
            return false;
        }

        public void StopScrobble()
        {
            if (traktTimer != null)
                traktTimer.Stop();
            
            if (currentMovie != null)
            {
                ScrobbleHandler(currentMovie, TraktScrobbleStates.cancelwatching);
                currentMovie = null;
            }
        }

        #endregion

        #region Scrobbling

        /// <summary>
        /// Ticker for Scrobbling
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void traktTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            ScrobbleHandler(currentMovie, TraktScrobbleStates.watching);
        }

        /// <summary>
        /// Scrobbles a given movie
        /// </summary>
        /// <param name="movie">Movie to Scrobble</param>
        /// <param name="state">Scrobbling mode to use</param>
        private void ScrobbleHandler(DBMovieInfo movie, TraktScrobbleStates state)
        {
            TraktLogger.Debug("Scrobbling Movie {0}", movie.Title);
            // MovingPictures stores duration in milliseconds, g_Player reports in seconds
            Double currentPosition = g_Player.CurrentPosition;
            Double duration = movie.ActualRuntime / 1000;

            Double percentageCompleted = duration != 0.0 ? (currentPosition / duration * 100) : 0.0;
            TraktLogger.Debug(string.Format("Percentage of {0} is {1}", movie.Title, percentageCompleted.ToString()));

            //Create Scrobbling Data
            TraktMovieScrobble scrobbleData = CreateScrobbleData(movie);

            if (scrobbleData != null)
            {
                // duration is reported in minutes
                scrobbleData.Duration = Convert.ToInt32(duration / 60).ToString();
                scrobbleData.Progress = Convert.ToInt32(percentageCompleted).ToString();
                BackgroundWorker scrobbler = new BackgroundWorker();
                scrobbler.DoWork += new DoWorkEventHandler(scrobbler_DoWork);
                scrobbler.RunWorkerCompleted += new RunWorkerCompletedEventHandler(scrobbler_RunWorkerCompleted);
                scrobbler.RunWorkerAsync(new MovieScrobbleAndMode { MovieScrobble = scrobbleData, ScrobbleState = state });
            }
        }

        /// <summary>
        /// BackgroundWorker code to scrobble movie state
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void scrobbler_DoWork(object sender, DoWorkEventArgs e)
        {
            MovieScrobbleAndMode data = e.Argument as MovieScrobbleAndMode;
            e.Result = TraktAPI.TraktAPI.ScrobbleMovieState(data.MovieScrobble, data.ScrobbleState);
        }

        /// <summary>
        /// End point for BackgroundWorker to send result to log
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void scrobbler_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            TraktResponse response = e.Result as TraktResponse;
            TraktAPI.TraktAPI.LogTraktResponse(response);
        }

        #endregion

        #region MovingPicturesHooks

        /// <summary>
        /// Fired when an objected is removed from the Moving Pictures Database
        /// </summary>
        /// <param name="obj"></param>
        private void DatabaseManager_ObjectDeleted(Cornerstone.Database.Tables.DatabaseTable obj)
        {
            if (TraktSettings.AccountStatus != ConnectionState.Connected) return;

            //If we have removed a movie from Moving Pictures we want to update Trakt library
            if (obj.GetType() == typeof(DBMovieInfo))
            {
                //Only remove from collection if the user wants us to
                if (TraktSettings.KeepTraktLibraryClean)
                {
                    //A Movie was removed from the database update trakt
                    DBMovieInfo insertedMovie = (DBMovieInfo)obj;
                    SyncMovie(CreateSyncData(insertedMovie), TraktSyncModes.unlibrary);
                }
            }
        }

        /// <summary>
        /// Fired when an object is updated in the Moving Pictures Database
        /// </summary>
        /// <param name="obj"></param>
        private void DatabaseManager_ObjectUpdated(Cornerstone.Database.Tables.DatabaseTable obj)
        {
            if (TraktSettings.AccountStatus != ConnectionState.Connected) return;

            //If it is user settings for a movie
            if (obj.GetType() == typeof(DBUserMovieSettings))
            {
                DBUserMovieSettings userMovieSettings = (DBUserMovieSettings)obj;
                DBMovieInfo movie = userMovieSettings.AttachedMovies[0];

                // don't do anything if movie is blocked
                if (TraktSettings.BlockedFilenames.Contains(movie.LocalMedia[0].FullPath) || TraktSettings.BlockedFolders.Any(f => movie.LocalMedia[0].FullPath.Contains(f)))
                {
                    TraktLogger.Info("Movie {0} is on the blocked list so we didn't update Trakt", movie.Title);
                    return;
                }

                // if we are syncing, we maybe manually setting state from trakt
                // in this case we dont want to resend to trakt
                if (SyncInProgress) return;

                //We check the watched flag and update Trakt respectfully
                //ignore if movie is the current movie being scrobbled, this will be set to watched automatically
                if (userMovieSettings.WatchCountChanged && movie != currentMovie)
                {
                    if (userMovieSettings.WatchedCount == 0)
                    {
                        SyncMovie(CreateSyncData(movie), TraktSyncModes.unseen);
                    }
                    else
                    {
                        SyncMovie(CreateSyncData(movie), TraktSyncModes.seen);
                    }
                }
                
                //We will update the Trakt rating of the Movie
                //TODO: Create a user setting for what they want to define as love/hate
                if (userMovieSettings.RatingChanged && userMovieSettings.UserRating > 0)
                {
                    if (userMovieSettings.UserRating >= 4)
                    {
                        RateMovie(CreateRateData(movie, TraktRateValue.love.ToString()));
                    }
                    else if (userMovieSettings.UserRating <= 2)
                    {
                        RateMovie(CreateRateData(movie, TraktRateValue.hate.ToString()));
                    }
                }

                // reset user flags for watched/ratings
                userMovieSettings.WatchCountChanged = false;
                userMovieSettings.RatingChanged = false;
            }
        }

        /// <summary>
        /// Fired when an object is inserted in the Moving Pictures Database
        /// </summary>
        /// <param name="obj"></param>
        private void DatabaseManager_ObjectInserted(Cornerstone.Database.Tables.DatabaseTable obj)
        {
            if (TraktSettings.AccountStatus != ConnectionState.Connected) return;

            if (obj.GetType() == typeof(DBWatchedHistory))
            {
                //A movie has been watched push that out.
                DBWatchedHistory watchedEvent = (DBWatchedHistory)obj;
                if (!TraktSettings.BlockedFilenames.Contains(watchedEvent.Movie.LocalMedia[0].FullPath) && !TraktSettings.BlockedFolders.Any(f => watchedEvent.Movie.LocalMedia[0].FullPath.Contains(f)))
                    ScrobbleHandler(watchedEvent.Movie, TraktScrobbleStates.scrobble);
                else
                    TraktLogger.Info("Movie {0} was found as blocked so did not scrobble", watchedEvent.Movie.Title);
            }
            else if (obj.GetType() == typeof(DBMovieInfo))
            {
                //A Movie was inserted into the database update trakt
                DBMovieInfo insertedMovie = (DBMovieInfo)obj;
                if (!TraktSettings.BlockedFilenames.Contains(insertedMovie.LocalMedia[0].FullPath) && !TraktSettings.BlockedFolders.Any(f => insertedMovie.LocalMedia[0].FullPath.Contains(f)))
                    SyncMovie(CreateSyncData(insertedMovie), TraktSyncModes.library);
                else
                    TraktLogger.Info("Newly inserted movie, {0}, was found on our block list so wasn't added to Trakt", insertedMovie.Title);
            }
        }

        #endregion
        
        #region SyncingMovieData

        /// <summary>
        /// Syncs Movie data in another thread
        /// </summary>
        /// <param name="syncData">Data to sync</param>
        /// <param name="mode">The Syncing mode to use</param>
        private void SyncMovie(TraktMovieSync syncData, TraktSyncModes mode)
        {
            BackgroundWorker moviesync = new BackgroundWorker();
            moviesync.DoWork += new DoWorkEventHandler(moviesync_DoWork);
            moviesync.RunWorkerCompleted += new RunWorkerCompletedEventHandler(moviesync_RunWorkerCompleted);
            moviesync.RunWorkerAsync(new MovieSyncAndMode { SyncData = syncData, Mode = mode });
        }

        /// <summary>
        /// Work Handler for Syncing Data in a seperate thread
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void moviesync_DoWork(object sender, DoWorkEventArgs e)
        {
            //Get the sync data
            MovieSyncAndMode data = e.Argument as MovieSyncAndMode;
            //performt the sync
            e.Result = TraktAPI.TraktAPI.SyncMovieLibrary(data.SyncData, data.Mode);
        }

        /// <summary>
        /// Records the result of the Movie Sync to the Log
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void moviesync_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            TraktResponse response = e.Result as TraktResponse;
            TraktAPI.TraktAPI.LogTraktResponse(response);
        }

        #endregion

        #region MovieRating
        private void RateMovie(TraktRateMovie rateData)
        {
            BackgroundWorker rateMovie = new BackgroundWorker();
            rateMovie.DoWork += new DoWorkEventHandler(rateMovie_DoWork);
            rateMovie.RunWorkerCompleted += new RunWorkerCompletedEventHandler(rateMovie_RunWorkerCompleted);
            rateMovie.RunWorkerAsync(rateData);
        }

        void rateMovie_DoWork(object sender, DoWorkEventArgs e)
        {
            TraktRateMovie data = (TraktRateMovie)e.Argument;
            e.Result = TraktAPI.TraktAPI.RateMovie(data);
        }

        void rateMovie_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            TraktRateResponse response = (TraktRateResponse)e.Result;
            TraktAPI.TraktAPI.LogTraktResponse(response);
        }
        #endregion

        #region DataCreators

        /// <summary>
        /// Creates Sync Data based on a List of DBMovieInfo objects
        /// </summary>
        /// <param name="Movies">The movies to base the object on</param>
        /// <returns>The Trakt Sync data to send</returns>
        public static TraktMovieSync CreateSyncData(List<DBMovieInfo> Movies)
        {
            string username = TraktSettings.Username;
            string password = TraktSettings.Password;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                return null;

            List<TraktMovieSync.Movie> moviesList = (from m in Movies
                                                select new TraktMovieSync.Movie
                                                {
                                                    IMDBID = m.ImdbID,
                                                    Title = m.Title,
                                                    Year = m.Year.ToString()
                                                }).ToList();

            TraktMovieSync syncData = new TraktMovieSync
            {
                UserName = username,
                Password = password,
                MovieList = moviesList
            };
            return syncData;
        }
               
        /// <summary>
        /// Creates Sync Data based on a single DBMovieInfo object
        /// </summary>
        /// <param name="Movie">The movie to base the object on</param>
        /// <returns>The Trakt Sync data to send</returns>
        public static TraktMovieSync CreateSyncData(DBMovieInfo Movie)
        {
            string username = TraktSettings.Username;
            string password = TraktSettings.Password;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                return null;

            List<TraktMovieSync.Movie> moviesList = new List<TraktMovieSync.Movie>();
            moviesList.Add(new TraktMovieSync.Movie
            {
                IMDBID = Movie.ImdbID,
                Title = Movie.Title,
                Year = Movie.Year.ToString()
            });

            TraktMovieSync syncData = new TraktMovieSync
            {
                UserName = username,
                Password = password,
                MovieList = moviesList
            };
            return syncData;
        }

        /// <summary>
        /// Creates Scrobble data based on a DBMovieInfo object
        /// </summary>
        /// <param name="movie">The movie to base the object on</param>
        /// <returns>The Trakt scrobble data to send</returns>
        public static TraktMovieScrobble CreateScrobbleData(DBMovieInfo movie)
        {
            string username = TraktSettings.Username;
            string password = TraktSettings.Password;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                return null;

            TraktMovieScrobble scrobbleData = new TraktMovieScrobble
            {
                Title = movie.Title,
                Year = movie.Year.ToString(),
                IMDBID = movie.ImdbID,
                PluginVersion = TraktSettings.Version,
                MediaCenter = "Mediaportal",
                MediaCenterVersion = Assembly.GetEntryAssembly().GetName().Version.ToString(),
                MediaCenterBuildDate = String.Empty,
                UserName = username,
                Password = password
            };
            return scrobbleData;
        }

        public static TraktRateMovie CreateRateData(DBMovieInfo movie, String rating)
        {
            string username = TraktSettings.Username;
            string password = TraktSettings.Password;

            if (String.IsNullOrEmpty(username) || String.IsNullOrEmpty(password))
                return null;

            TraktRateMovie rateData = new TraktRateMovie
            {
                Title = movie.Title,
                Year = movie.Year.ToString(),
                IMDBID = movie.ImdbID,
                UserName = username,
                Password = password,
                Rating = rating
            };
            return rateData;
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Gets a correctly formatted imdb id string        
        /// </summary>
        /// <param name="id">current movie imdb id</param>
        /// <returns>correctly formatted id</returns>
        static string GetProperMovieId(string id)
        {
            string imdbid = id;

            // handle invalid ids
            // return null so we dont match empty result from trakt
            if (id == null || !id.StartsWith("tt")) return null;

            // correctly format to 9 char string
            if (id.Length != 9)
            {
                imdbid = string.Format("tt{0}", id.Substring(2).PadLeft(7, '0'));
            }
            return imdbid;
        }

        #endregion

        #region Other Public Methods

        public void DisposeEvents()
        {
            TraktLogger.Debug("Removing Hooks from Moving Pictures Database");
            MovingPicturesCore.DatabaseManager.ObjectInserted -= new Cornerstone.Database.DatabaseManager.ObjectAffectedDelegate(DatabaseManager_ObjectInserted);
            MovingPicturesCore.DatabaseManager.ObjectUpdated -= new Cornerstone.Database.DatabaseManager.ObjectAffectedDelegate(DatabaseManager_ObjectUpdated);
            MovingPicturesCore.DatabaseManager.ObjectDeleted -= new Cornerstone.Database.DatabaseManager.ObjectAffectedDelegate(DatabaseManager_ObjectDeleted);
        }

        public static bool FindMovieID(string title, int year, string imdbid, ref int? movieID)
        {
            // get all movies in local database
            List<DBMovieInfo> movies = DBMovieInfo.GetAll();

            // try find a match
            DBMovieInfo movie = movies.Find(m => m.ImdbID == imdbid || (m.Title == title && m.Year == year));
            if (movie == null) return false;

            movieID = movie.ID;
            return true;
        }

        #endregion
    }
}
