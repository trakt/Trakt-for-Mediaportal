using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaPortal.GUI.Library;
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

        public MovingPictures(int priority)
        {
            Priority = priority;
            Log.Debug("Trakt: Adding Hooks to Moving Pictures Database");
            MovingPicturesCore.DatabaseManager.ObjectInserted += new Cornerstone.Database.DatabaseManager.ObjectAffectedDelegate(DatabaseManager_ObjectInserted);
            MovingPicturesCore.DatabaseManager.ObjectUpdated += new Cornerstone.Database.DatabaseManager.ObjectAffectedDelegate(DatabaseManager_ObjectUpdated);
            MovingPicturesCore.DatabaseManager.ObjectDeleted += new Cornerstone.Database.DatabaseManager.ObjectAffectedDelegate(DatabaseManager_ObjectDeleted);
        }

        #region ITraktHandler

        public string Name { get { return "Moving Pictures"; } }
        public int Priority { get; set; }
        
        public void SyncLibrary()
        {
            Log.Info("Trakt: Moving Pictures Starting Sync");
            List<DBMovieInfo> MovieList = DBMovieInfo.GetAll();

            //Get the movies that we have watched
            List<DBMovieInfo> SeenList = MovieList.Where(m => m.ActiveUserSettings.WatchedCount > 0).ToList();

            //Get the movies that we have yet to watch
            Log.Info("Trakt: Getting Library from Trakt");
            List<TraktLibraryMovies> NoLongerInOurLibrary = new List<TraktLibraryMovies>();
            IEnumerable<TraktLibraryMovies> movies = TraktAPI.TraktAPI.GetMoviesForUser(TraktSettings.Username);
            Log.Info("Trakt: Library from Trakt Complete");

            foreach (TraktLibraryMovies tlm in movies)
            {
                bool notInLibrary = true;
                //If it is in both libraries
                foreach (DBMovieInfo libraryMovie in MovieList.Where(m => m.ImdbID == tlm.IMDBID))
                {
                    //If it is watched in Trakt but not Moving Pictures update
                    if (tlm.Plays > 0 && libraryMovie.ActiveUserSettings.WatchedCount == 0)
                    {
                        Log.Info(String.Format("Trakt: Movie {0} is watched on Trakt updating Database", libraryMovie.Title));
                        libraryMovie.ActiveUserSettings.WatchedCount = 1;
                        libraryMovie.Commit();
                    }
                    notInLibrary = false;

                    //We want to widdle down the movies in seen and unseen if they are already on Trakt
                    //also remove any duplicates we have locally so we dont re-submit every sync
                    if (tlm.Plays > 0)
                        SeenList.RemoveAll(m => m.ImdbID == tlm.IMDBID);
                    MovieList.RemoveAll(m => m.ImdbID == tlm.IMDBID);
                    break;
                  
                }

                if (notInLibrary)
                    NoLongerInOurLibrary.Add(tlm);
            }

            Log.Info("Trakt: {0} movies need to be added to SeenList", SeenList.Count.ToString());
            foreach (DBMovieInfo m in SeenList)
                Log.Debug("Trakt: Sending from Seen to Trakt: {0}", m.Title);

            Log.Info("Trakt: {0} movies need to be added to Library", MovieList.Count.ToString());
            foreach (DBMovieInfo m in MovieList)
                Log.Debug("Trakt: Sending from UnSeen to Trakt: {0}", m.Title);

            //Send Unseen
            if (MovieList.Count > 0)
            {
                Log.Info("Trakt: Sending Library List");
                TraktResponse response = TraktAPI.TraktAPI.SyncMovieLibrary(CreateSyncData(MovieList), TraktSyncModes.library);
                TraktAPI.TraktAPI.LogTraktResponse(response);
            }
            if (SeenList.Count > 0)
            {
                Log.Info("Trakt: Sending Seen List");
                TraktResponse response = TraktAPI.TraktAPI.SyncMovieLibrary(CreateSyncData(SeenList), TraktSyncModes.seen);
                TraktAPI.TraktAPI.LogTraktResponse(response);
            }
            if (TraktSettings.KeepTraktLibraryClean)
            {
                //Remove movies we no longer have from Trakt
                Log.Info("Trakt: Removing Additional Movies From Trakt");
                foreach (var m in NoLongerInOurLibrary)
                    Log.Info(String.Format("Trakt: Removing from Trakt {0}", m.Title));

                //First need to unseen them all
                TraktResponse response = TraktAPI.TraktAPI.SyncMovieLibrary(BasicHandler.CreateMovieSyncData(NoLongerInOurLibrary), TraktSyncModes.unseen);
                TraktAPI.TraktAPI.LogTraktResponse(response);

                //Then remove form library
                response = TraktAPI.TraktAPI.SyncMovieLibrary(BasicHandler.CreateMovieSyncData(NoLongerInOurLibrary), TraktSyncModes.unlibrary);
                TraktAPI.TraktAPI.LogTraktResponse(response);
            }

            Log.Info("Trakt: Moving Pictures Sync Completed");

        }

        public bool Scrobble(String filename)
        {
            StopScrobble();
            List<DBMovieInfo> searchResults = (from m in DBMovieInfo.GetAll() where (from path in m.LocalMedia select path.FullPath).ToList().Contains(filename) select m).ToList();
            if (searchResults.Count == 1)
            {
                //Create timer
                currentMovie = searchResults[0];
                Log.Info(string.Format("Trakt: Found playing movie {0}", currentMovie.Title));
                ScrobbleHandler(currentMovie, TraktScrobbleStates.watching);
                traktTimer = new Timer();
                traktTimer.Interval = 900000;
                traktTimer.Elapsed += new ElapsedEventHandler(traktTimer_Elapsed);
                traktTimer.Start();
                return true;
            }
            else if (searchResults.Count == 0)
                Log.Debug("Trakt: Playback started but Movie not found");
            else
                Log.Debug("Trakt: Multiple movies found for filename something is up!");
            return false;
        }

        public void StopScrobble()
        {
            if (traktTimer != null)
                traktTimer.Stop();
            if (currentMovie != null)
                ScrobbleHandler(currentMovie, TraktScrobbleStates.cancelwatching);
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
            Log.Debug("Trakt: Scrobbling Movie");
            Double currentPosition = g_Player.CurrentPosition;
            Double duration = movie.ActualRuntime;

            Double percentageCompleted = currentPosition / duration * 100;
            Log.Debug(string.Format("Trakt: Percentage of {0} is {1}", movie.Title, percentageCompleted.ToString()));

            //Create Scrobbling Data
            TraktMovieScrobble scrobbleData = CreateScrobbleData(movie);

            if (scrobbleData != null)
            {
                scrobbleData.Duration = duration.ToString();
                scrobbleData.Progress = percentageCompleted.ToString();
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
            Log.Debug("Trakt: Scrobble Response: {0}", response.Message);
        }

        #endregion

        #region MovingPicturesHooks

        /// <summary>
        /// Fired when an objected is removed from the Moving Pictures Database
        /// </summary>
        /// <param name="obj"></param>
        private void DatabaseManager_ObjectDeleted(Cornerstone.Database.Tables.DatabaseTable obj)
        {
            //If DBWatchedHistory is deleted we want to check if it is still watched
            if (obj.GetType() == typeof(DBWatchedHistory))
            {
                //Unwatched?
                DBWatchedHistory watchedEvent = (DBWatchedHistory)obj;
                if (watchedEvent.Movie.ActiveUserSettings.WatchedCount == 0)
                    SyncMovie(CreateSyncData(watchedEvent.Movie), TraktSyncModes.unseen);
            }
            //If we have removed a movie from Moving Pictures we want to update Trakt library
            else if (obj.GetType() == typeof(DBMovieInfo))
            {
                //Only remove if the user wants us to
                if (TraktSettings.KeepTraktLibraryClean)
                {
                    //A Movie was removed from the database update trakt
                    DBMovieInfo insertedMovie = (DBMovieInfo)obj;
                    SyncMovie(CreateSyncData(insertedMovie), TraktSyncModes.unseen);
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
            //If it is user settings for a movie
            if (obj.GetType() == typeof(DBUserMovieSettings))
            {
                DBUserMovieSettings userMovieSettings = (DBUserMovieSettings)obj;
                DBMovieInfo movie = userMovieSettings.AttachedMovies[0];

                //We check the watched flag and update Trakt respectfully
                if (userMovieSettings.WatchedCount == 0)
                {
                    SyncMovie(CreateSyncData(movie), TraktSyncModes.unseen);
                }
                else
                {
                    SyncMovie(CreateSyncData(movie), TraktSyncModes.seen);
                }

                //We will update the Trakt rating of the Movie
                //TODO: Create a user setting for what they want to define as love/hate
                if (userMovieSettings.UserRating > 0)
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
            }
        }

        /// <summary>
        /// Fired when an object is inserted in the Moving Pictures Database
        /// </summary>
        /// <param name="obj"></param>
        private void DatabaseManager_ObjectInserted(Cornerstone.Database.Tables.DatabaseTable obj)
        {
            if (obj.GetType() == typeof(DBWatchedHistory))
            {
                //A movie has been watched push that out.
                DBWatchedHistory watchedEvent = (DBWatchedHistory)obj;
                ScrobbleHandler(watchedEvent.Movie, TraktScrobbleStates.scrobble);
            }
            else if (obj.GetType() == typeof(DBMovieInfo))
            {
                //A Movie was inserted into the database update trakt
                DBMovieInfo insertedMovie = (DBMovieInfo)obj;
                SyncMovie(CreateSyncData(insertedMovie), TraktSyncModes.library);
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
            Log.Debug("Trakt: Sync Response: {0}", response.Status);
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
            Log.Info("Trakt: Movie Rating Response: {0}", response.Status);
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
                PluginVersion = Assembly.GetCallingAssembly().GetName().Version.ToString(),
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
    }
}
