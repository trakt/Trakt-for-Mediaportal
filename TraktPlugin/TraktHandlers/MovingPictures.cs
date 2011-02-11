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
            Log.Debug("Adding Hooks to Moving Pictures Database");
            MovingPicturesCore.DatabaseManager.ObjectInserted += new Cornerstone.Database.DatabaseManager.ObjectAffectedDelegate(DatabaseManager_ObjectInserted);
            MovingPicturesCore.DatabaseManager.ObjectUpdated += new Cornerstone.Database.DatabaseManager.ObjectAffectedDelegate(DatabaseManager_ObjectUpdated);
            MovingPicturesCore.DatabaseManager.ObjectDeleted += new Cornerstone.Database.DatabaseManager.ObjectAffectedDelegate(DatabaseManager_ObjectDeleted);
        }

        #region ITraktHandler

        public string Name { get { return "Moving Pictures"; } }
        public int Priority { get; set; }
        
        public void SyncLibrary()
        {
            Log.Debug("Trakt: Moving Pictures Staring Sync");
            List<DBMovieInfo> MovieList = DBMovieInfo.GetAll();
            //Get the movies that we have watched
            List<DBMovieInfo> SeenList = MovieList.Where(m => m.ActiveUserSettings.WatchedCount > 0).ToList();
            //Get the movies that we have yet to watch
            List<DBMovieInfo> UnSeenList = MovieList.Where(m => m.ActiveUserSettings.WatchedCount == 0).ToList();
            Log.Debug("Getting Library from Trakt");
            List<TraktLibraryMovies> NoLongerInOurLibrary = new List<TraktLibraryMovies>();
            foreach (TraktLibraryMovies tlm in TraktAPI.TraktAPI.GetMoviesForUser(TraktSettings.Username))
            {
                bool notInLibrary = true;
                foreach (DBMovieInfo libraryMovie in MovieList)
                {
                    //If it is in both libraries
                    if (libraryMovie.ImdbID == tlm.IMDBID)
                    {
                        //If it is watched in Trakt but not Moving Pictures update Moving Pictures
                        if (tlm.Watched && libraryMovie.ActiveUserSettings.WatchedCount == 0)
                        {
                            Log.Debug(String.Format("Movie {0} is watched on Trakt updating Database", libraryMovie.Title));
                            libraryMovie.ActiveUserSettings.WatchedCount = 1;
                            libraryMovie.Commit();
                        }
                        notInLibrary = false;

                        //We want to widdle down the movies in seen and unseen if they are already on Trakt
                        if (SeenList.Contains(libraryMovie))
                            SeenList.Remove(libraryMovie);
                        else if (UnSeenList.Contains(libraryMovie))
                            UnSeenList.Remove(libraryMovie);
                        break;
                    }
                }

                if (notInLibrary)
                    NoLongerInOurLibrary.Add(tlm);
            }

            Log.Debug("Trakt: SeenList Count {0}", SeenList.Count.ToString());
            foreach (DBMovieInfo m in SeenList)
                Log.Debug("Trakt: Sending from Seen to Trakt: {0}", m.Title);
            Log.Debug("Trakt: UnSeenList Count {0}", UnSeenList.Count.ToString());
            foreach (DBMovieInfo m in UnSeenList)
                Log.Debug("Trakt: Sending from UnSeen to Trakt: {0}", m.Title);

            //Send Unseen
            Log.Debug("Trakt: Sending UnSeen List");
            TraktResponse response = TraktAPI.TraktAPI.SyncMovieLibrary(CreateSyncData(UnSeenList), TraktSyncModes.library);
            Log.Debug(String.Format("Trakt: Response from Trakt, {0} {1} {2}", response.Status, response.Message, response.Error));

            Log.Debug("Trakt: Sending Seen List");
            response = TraktAPI.TraktAPI.SyncMovieLibrary(CreateSyncData(SeenList), TraktSyncModes.seen);
            Log.Debug(String.Format("Trakt: Response from Trakt, {0} {1} {2}", response.Status, response.Message, response.Error));

            if (TraktSettings.KeepTraktLibraryClean)
            {
                //Remove movies we no longer have from Trakt
                Log.Debug("Trakt: Removing Additional Movies From Trakt");
                foreach (var m in NoLongerInOurLibrary)
                    Log.Debug(String.Format("Trakt: Removing from Trakt {0}", m.Title));
                //First need to unseen them all
                response = TraktAPI.TraktAPI.SyncMovieLibrary(BasicHandler.CreateSyncData(NoLongerInOurLibrary), TraktSyncModes.unseen);
                Log.Debug(String.Format("Trakt: Response from Trakt, {0} {1} {2}", response.Status, response.Message, response.Error));
                //Then remove form library
                response = TraktAPI.TraktAPI.SyncMovieLibrary(BasicHandler.CreateSyncData(NoLongerInOurLibrary), TraktSyncModes.unlibrary);
                Log.Debug(String.Format("Trakt: Response from Trakt, {0} {1} {2}", response.Status, response.Message, response.Error));
            }

            Log.Debug("Trakt: Sync Completed");

        }

        public bool Scrobble(String filename)
        {
            StopScrobble();
            List<DBMovieInfo> searchResults = (from m in DBMovieInfo.GetAll() where (from path in m.LocalMedia select path.FullPath).ToList().Contains(filename) select m).ToList();
            if (searchResults.Count == 1)
            {
                //Create timer
                currentMovie = searchResults[0];
                Log.Debug(string.Format("Trakt: Found playing movie {0}", currentMovie.Title));
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
                TraktResponse response = TraktAPI.TraktAPI.ScrobbleMovieState(scrobbleData, state);
                Log.Debug(string.Format("Trakt: Response: {0}", response.Message));
            }
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
                    TraktAPI.TraktAPI.SyncMovieLibrary(CreateSyncData(watchedEvent.Movie), TraktSyncModes.unseen);
            }
            //If we have removed a movie from Moving Pictures we want to update Trakt library
            else if (obj.GetType() == typeof(DBMovieInfo))
            {
                //A Movie was removed from the database update trakt
                DBMovieInfo insertedMovie = (DBMovieInfo)obj;
                TraktAPI.TraktAPI.SyncMovieLibrary(CreateSyncData(insertedMovie), TraktSyncModes.unlibrary);
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
                    Log.Debug("Trakt: Movie {0} is not watched updating Trakt", movie.Title);
                    Log.Debug("Trakt: {0}", TraktAPI.TraktAPI.SyncMovieLibrary(CreateSyncData(movie), TraktSyncModes.unseen).Message);
                }
                else
                {
                    Log.Debug("Trakt: Movie {0} is watched updating Trakt", movie.Title);
                    Log.Debug("Trakt: {0}", TraktAPI.TraktAPI.SyncMovieLibrary(CreateSyncData(movie), TraktSyncModes.seen).Message);
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
                TraktAPI.TraktAPI.SyncMovieLibrary(CreateSyncData(insertedMovie), TraktSyncModes.library);
            }
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

        #endregion
    }
}
