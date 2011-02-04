using System;
using System.Windows.Forms;
using System.IO;
using MediaPortal;
using MediaPortal.GUI.Library;
using MediaPortal.Player;
using TraktPlugin.Trakt;
using MediaPortal.Plugins.MovingPictures;
using MediaPortal.Plugins.MovingPictures.Database;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace TraktPlugin
{
    /// <summary>
    /// TraktPlugin for Mediaportal and Moving Pictures. Adds the Trakt scrobbling API to Moving Pictures
    /// Created by Luke Barnett
    /// </summary>
    public class TraktPlugin : ISetupForm, IPlugin
    {
        #region Private variables
        private BackgroundWorker bgTrakySync = null;
        private Configuration config = null;
        private ProgressBar pbProgress = null;
        private Timer traktTimer = null;
        private DBMovieInfo currentMovie = null;
        #endregion

        #region ISetupFrom

        /// <summary>
        /// Returns the Author of the Plugin to Mediaportal
        /// </summary>
        /// <returns>The Author of the Plugin</returns>
        public string Author()
        {
            return "Technicolour";
        }

        /// <summary>
        /// Boolean that decides whether the plugin can be enabled or not
        /// </summary>
        /// <returns>The boolean answer</returns>
        public bool CanEnable()
        {
            return true;
        }

        /// <summary>
        /// Decides if the plugin is enabled by default
        /// </summary>
        /// <returns>The boolean answer</returns>
        public bool DefaultEnabled()
        {
            return false;
        }

        /// <summary>
        /// Description of the plugin
        /// </summary>
        /// <returns>The Description</returns>
        public string Description()
        {
            return "Adds Trakt scrobbling to Mediaportal";
        }

        /// <summary>
        /// Returns the items for the plugin
        /// </summary>
        /// <param name="strButtonText">The Buttons Text</param>
        /// <param name="strButtonImage">The Buttons Image</param>
        /// <param name="strButtonImageFocus">The Buttons Focused Image</param>
        /// <param name="strPictureImage">The Picture Image</param>
        /// <returns></returns>
        public bool GetHome(out string strButtonText, out string strButtonImage, out string strButtonImageFocus, out string strPictureImage)
        {
            strButtonImage = null;
            strButtonText = null;
            strButtonImageFocus = null;
            strPictureImage = null;
            return false;
        }

        /// <summary>
        /// Gets the Window id accociated with the plugin
        /// </summary>
        /// <returns>The window id</returns>
        public int GetWindowId()
        {
            return -1;
        }

        /// <summary>
        /// Boolean asking if the plugin has a setup setting
        /// </summary>
        /// <returns>The Boolean answer</returns>
        public bool HasSetup()
        {
            return true;
        }

        /// <summary>
        /// The Name of the Plugin
        /// </summary>
        /// <returns>The Name of the Plugin</returns>
        public string PluginName()
        {
            return "Trakt";
        }

        /// <summary>
        /// Shows the Plugins configuration window
        /// </summary>
        public void ShowPlugin()
        {
            if(config == null)
                config = new Configuration(this);
            config.ShowDialog();
        }

        #endregion

        #region IPlugin

        /// <summary>
        /// Starting Point
        /// </summary>
        public void Start()
        {
            Log.Debug("Trakt: Plugin Started");
            //Load the configuration details
            if (config == null)
                config = new Configuration(this);
            config.LoadConfig();
            Log.Debug("Trakt: Configuration Loaded");
            //If we load the config and the username or password is not set we should stop before we break something.
            if (string.IsNullOrEmpty(TraktAPI.Username) || string.IsNullOrEmpty(TraktAPI.Password))
            {
                Log.Debug("Trakt: Fatal Error: No Username and or password");
                Log.Debug("Dying gracefully");
                return;
            }
            Log.Debug(String.Format("Trakt: Username: {0} Password: {1}", TraktAPI.Username, TraktAPI.Password));
            //clearTraktLibrary();
            //Sync Library do we want to do this every startup? Or just on config?
            SyncLibrary(null);
            //Start Looking for Playback
            g_Player.PlayBackStarted += new g_Player.StartedHandler(g_Player_PlayBackStarted);
            g_Player.PlayBackStopped += new g_Player.StoppedHandler(g_Player_PlayBackStopped);
            g_Player.PlayBackChanged += new g_Player.ChangedHandler(g_Player_PlayBackChanged);
            g_Player.PlayBackEnded += new g_Player.EndedHandler(g_Player_PlayBackEnded);
            //Look for Database Updates
            MovingPicturesCore.DatabaseManager.ObjectInserted += new Cornerstone.Database.DatabaseManager.ObjectAffectedDelegate(DatabaseManager_ObjectInserted);
            MovingPicturesCore.DatabaseManager.ObjectDeleted += new Cornerstone.Database.DatabaseManager.ObjectAffectedDelegate(DatabaseManager_ObjectDeleted);
            MovingPicturesCore.DatabaseManager.ObjectUpdated += new Cornerstone.Database.DatabaseManager.ObjectAffectedDelegate(DatabaseManager_ObjectUpdated);
        }


        /// <summary>
        /// End Point (Clean up)
        /// </summary>
        public void Stop()
        {
            //Remove any event hooks
            g_Player.PlayBackStarted -= new g_Player.StartedHandler(g_Player_PlayBackStarted);
            g_Player.PlayBackStopped -= new g_Player.StoppedHandler(g_Player_PlayBackStopped);
            g_Player.PlayBackChanged -= new g_Player.ChangedHandler(g_Player_PlayBackChanged);
            g_Player.PlayBackEnded -= new g_Player.EndedHandler(g_Player_PlayBackEnded);

            MovingPicturesCore.DatabaseManager.ObjectInserted -= new Cornerstone.Database.DatabaseManager.ObjectAffectedDelegate(DatabaseManager_ObjectInserted);
            MovingPicturesCore.DatabaseManager.ObjectDeleted -= new Cornerstone.Database.DatabaseManager.ObjectAffectedDelegate(DatabaseManager_ObjectDeleted);
            MovingPicturesCore.DatabaseManager.ObjectUpdated -= new Cornerstone.Database.DatabaseManager.ObjectAffectedDelegate(DatabaseManager_ObjectUpdated);

            //Stop the scrobbler
            if (traktTimer != null)
                traktTimer.Stop();

            Log.Debug("Trakt: Goodbye");
        }
                
        #endregion
        
        #region FullLibrarySync
        /// <summary>
        /// Does a full sync between the Trakt and Moving Pictures Library
        /// </summary>
        /// <param name="bar">The Progress bar to send progress to</param>
        public void SyncLibrary(ProgressBar bar)
        {
            //If we are already running a sync don't start another one
            if (bgTrakySync != null && bgTrakySync.IsBusy)
            {
                return;
            }

            //Set the progress bar
            pbProgress = bar;
            if (pbProgress != null)
                pbProgress.Value = 0;

            //Setup the worker
            bgTrakySync = new BackgroundWorker();
            bgTrakySync.DoWork += new DoWorkEventHandler(bgTrakySync_DoWork);
            bgTrakySync.ProgressChanged += new ProgressChangedEventHandler(bgTrakySync_ProgressChanged);
            bgTrakySync.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bgTrakySync_RunWorkerCompleted);
            bgTrakySync.WorkerReportsProgress = true;
            bgTrakySync.WorkerSupportsCancellation = true;

            //Start sync
            bgTrakySync.RunWorkerAsync();
        }

        /// <summary>
        /// Clean up for the Completion of the full sync
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void bgTrakySync_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //Alert that we have finished
            if (pbProgress != null)
                pbProgress.Value = 100;
            else
                Log.Debug("Trakt: Sync Completed");

            if (config != null)
                config.SyncCompleted();
        }

        /// <summary>
        /// Updates the progress to those that are listening
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void bgTrakySync_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (pbProgress != null)
                pbProgress.Value = e.ProgressPercentage;
            else
                Log.Debug(String.Format("Trakt: TraktSync Progress {0}", e.ProgressPercentage.ToString()));
        }

        /// <summary>
        /// Main logic for performing a full library sync
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void bgTrakySync_DoWork(object sender, DoWorkEventArgs e)
        {
            Log.Debug("Trakt: Starting Sync");
            //Get the Movies from Moving Pictures
            List<DBMovieInfo> MovieList = DBMovieInfo.GetAll();
            //Get the movies that we have watched
            List<DBMovieInfo> SeenList = MovieList.Where(m => m.ActiveUserSettings.WatchedCount > 0).ToList();
            //Get the movies that we have yet to watch
            List<DBMovieInfo> UnSeenList = MovieList.Where(m => m.ActiveUserSettings.WatchedCount == 0).ToList();
            Log.Debug("Trakt: SeenList Count {0}", SeenList.Count.ToString());
            Log.Debug("Trakt: UnSeenList Count {0}", UnSeenList.Count.ToString());
            bgTrakySync.ReportProgress(20);
            //Get the Movie Library from Trakt
            Log.Debug("Getting Library from Trakt");
            List<TraktLibraryMovies> NoLongerInOurLibrary = new List<TraktLibraryMovies>();
            foreach (TraktLibraryMovies tlm in TraktAPI.GetMoviesForUser(TraktAPI.Username))
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
                        else if(UnSeenList.Contains(libraryMovie))
                            UnSeenList.Remove(libraryMovie);
                        break;
                    }
                }

                if (notInLibrary)
                    NoLongerInOurLibrary.Add(tlm);
            }

            foreach (var m in NoLongerInOurLibrary)
                Log.Debug(String.Format("Trakt: Removing from Trakt {0}", m.Title));

            foreach (DBMovieInfo m in SeenList)
                Log.Debug("Trakt: Sending from Seen to Trakt: {0}", m.Title);

            foreach (DBMovieInfo m in UnSeenList)
                Log.Debug("Trakt: Sending from UnSeen to Trakt: {0}", m.Title);

            bgTrakySync.ReportProgress(60);
            Log.Debug("Trakt2: SeenList Count {0}", SeenList.Count.ToString());
            Log.Debug("Trakt2: UnSeenList Count {0}", UnSeenList.Count.ToString());

            //Send Unseen
            Log.Debug("Trakt: Sending UnSeen List");
            TraktResponse response = TraktAPI.SyncMovieLibrary(TraktHandler.CreateSyncData(UnSeenList), TraktSyncModes.library);
            Log.Debug(String.Format("Trakt: Response from Trakt, {0} {1} {2}", response.Status, response.Message, response.Error));
            bgTrakySync.ReportProgress(70);
            //Send the Seen
            Log.Debug("Trakt: Sending Seen List");
            response = TraktAPI.SyncMovieLibrary(TraktHandler.CreateSyncData(SeenList), TraktSyncModes.seen);
            Log.Debug(String.Format("Trakt: Response from Trakt, {0} {1} {2}", response.Status, response.Message, response.Error));
            bgTrakySync.ReportProgress(80);
            //Remove movies we no longer have from Trakt
            Log.Debug("Trakt: Removing Additional Movies From Trakt");
            //First need to unseen them all
            response = TraktAPI.SyncMovieLibrary(TraktHandler.CreateSyncData(NoLongerInOurLibrary), TraktSyncModes.unseen);
            Log.Debug(String.Format("Trakt: Response from Trakt, {0} {1} {2}", response.Status, response.Message, response.Error));
            bgTrakySync.ReportProgress(95);
            //Then remove form library
            response = TraktAPI.SyncMovieLibrary(TraktHandler.CreateSyncData(NoLongerInOurLibrary), TraktSyncModes.unlibrary);
            Log.Debug(String.Format("Trakt: Response from Trakt, {0} {1} {2}", response.Status, response.Message, response.Error));

            Log.Debug("Trakt: Sync Completed");
        }

        /// <summary>
        /// Stops the full sync
        /// </summary>
        public void StopSync()
        {
            if (bgTrakySync != null && bgTrakySync.IsBusy)
            {
                bgTrakySync.CancelAsync();
            }
        }
        #endregion

        #region MediaportalPlayerEventHandlers
        /// <summary>
        /// Fired when Mediaportal starts playing media
        /// </summary>
        /// <param name="type">The type of Media it is</param>
        /// <param name="filename">The filename of the media</param>
        private void g_Player_PlayBackStarted(g_Player.MediaType type, string filename)
        {
            //We only care if it is video
            if(type == g_Player.MediaType.Video)
                checkPlayback(filename);
        }
        
        /// <summary>
        /// Fired when Mediaportal stops playing media
        /// </summary>
        /// <param name="type">The type of Media it is</param>
        /// <param name="filename">The filename of the media</param>
        private void g_Player_PlayBackEnded(g_Player.MediaType type, string filename)
        {
            //We only care if it is video
            if (type == g_Player.MediaType.Video)
            {
                //We pull watched events from Moving Pictures so we just tidy up here
                if (traktTimer != null)
                    traktTimer.Stop();
                traktTimer = null;
                currentMovie = null;
            }
        }

        /// <summary>
        /// Fired when Mediaportal changes the media that it is playing
        /// </summary>
        /// <param name="type">The type of media it is</param>
        /// <param name="stoptime">The time that the old media was stopped</param>
        /// <param name="filename">The filename of the media</param>
        private void g_Player_PlayBackChanged(g_Player.MediaType type, int stoptime, string filename)
        {
            //Clean Up
            if (traktTimer != null)
                traktTimer.Stop();
            traktTimer = null;
            currentMovie = null;

            //We only care if it is video
            if (type == g_Player.MediaType.Video)
                checkPlayback(filename);
        }

        /// <summary>
        /// Fired when Mediaportal stops playback
        /// </summary>
        /// <param name="type">The type of media it is</param>
        /// <param name="stoptime">The time that the old media was stopped</param>
        /// <param name="filename">The filename of the media</param>
        private void g_Player_PlayBackStopped(g_Player.MediaType type, int stoptime, string filename)
        {
            //Check if we have watched enough of the movie to lock it in or to clear trakt
            //Clear trakt
        }

        #endregion

        #region ScrobblingMethods
        /// <summary>
        /// Checks the filename against the moving pictures database and starts scrobbling if found
        /// </summary>
        /// <param name="filename"></param>
        private void checkPlayback(string filename)
        {
            //Check if it is one of the movies
            //If so watch it on trakt
            if (traktTimer != null)
                traktTimer.Stop();
            List<DBMovieInfo> searchResults = (from m in DBMovieInfo.GetAll() where (from path in m.LocalMedia select path.FullPath).ToList().Contains(filename) select m).ToList();
            if (searchResults.Count == 1)
            {
                //Create timer
                currentMovie = searchResults[0];
                Log.Debug(string.Format("Trakt: Found playing movie {0}", currentMovie.Title));
                playBackHandler(currentMovie, TraktScrobbleStates.watching);
                traktTimer = new Timer();
                traktTimer.Interval = 900000;
                traktTimer.Tick += new EventHandler(traktTimer_Tick);
                traktTimer.Start();
            }
            else if (searchResults.Count == 0)
                Log.Debug("Trakt: Playback started but Movie not found");
            else
                Log.Debug("Trakt: Multiple movies found for filename something is up!");
        }

        /// <summary>
        /// Logic that updates Trakt on the required interval
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void traktTimer_Tick(object sender, EventArgs e)
        {
            playBackHandler(currentMovie, TraktScrobbleStates.watching);
        }


        /// <summary>
        /// Creates and sends the scrobbling or watched data to Trakt
        /// </summary>
        /// <param name="movie">The Movie to send</param>
        /// <param name="state">The state to send</param>
        private void playBackHandler(DBMovieInfo movie, TraktScrobbleStates state)
        {
            Log.Debug("Trakt: Scrobbling Movie");
            Double currentPosition = g_Player.CurrentPosition;
            Double duration = movie.ActualRuntime;

            Double percentageCompleted = currentPosition / duration * 100;
            Log.Debug(string.Format("Trakt: Percentage of {0} is {1}", movie.Title, percentageCompleted.ToString()));

            //Create Scrobbling Data
            TraktMovieScrobble scrobbleData = TraktHandler.CreateScrobbleData(movie);

            if (scrobbleData != null)
            {
                scrobbleData.Duration = duration.ToString();
                scrobbleData.Progress = percentageCompleted.ToString();
                TraktResponse response = TraktAPI.ScrobbleMovieState(scrobbleData, state);
                Log.Debug(string.Format("Trakt: Response: {0}", response.Message));
            }

        }
        #endregion

        #region MovingPicturesDatabaseEventHandlers
        /// <summary>
        /// Fired when an object is update in the database
        /// </summary>
        /// <param name="obj"></param>
        void DatabaseManager_ObjectUpdated(Cornerstone.Database.Tables.DatabaseTable obj)
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
                    Log.Debug("Trakt: {0}", TraktAPI.SyncMovieLibrary(TraktHandler.CreateSyncData(movie), TraktSyncModes.unseen).Message);
                }
                else
                {
                    Log.Debug("Trakt: Movie {0} is watched updating Trakt", movie.Title);
                    Log.Debug("Trakt: {0}", TraktAPI.SyncMovieLibrary(TraktHandler.CreateSyncData(movie), TraktSyncModes.seen).Message);
                }
            }
        }
        
        /// <summary>
        /// Fired when an object is deleted
        /// </summary>
        /// <param name="obj"></param>
        void DatabaseManager_ObjectDeleted(Cornerstone.Database.Tables.DatabaseTable obj)
        {
            //If DBWatchedHistory is deleted we want to check if it is still watched
            if (obj.GetType() == typeof(DBWatchedHistory))
            {
                //Unwatched?
                DBWatchedHistory watchedEvent = (DBWatchedHistory)obj;
                if (watchedEvent.Movie.ActiveUserSettings.WatchedCount == 0)
                    TraktAPI.SyncMovieLibrary(TraktHandler.CreateSyncData(watchedEvent.Movie), TraktSyncModes.unseen);
            }
            //If we have removed a movie from Moving Pictures we want to update Trakt library
            else if (obj.GetType() == typeof(DBMovieInfo))
            {
                //A Movie was removed from the database update trakt
                DBMovieInfo insertedMovie = (DBMovieInfo)obj;
                TraktAPI.SyncMovieLibrary(TraktHandler.CreateSyncData(insertedMovie), TraktSyncModes.unlibrary);
            }
        }
        
        /// <summary>
        /// Fired when an object is added
        /// </summary>
        /// <param name="obj"></param>
        void DatabaseManager_ObjectInserted(Cornerstone.Database.Tables.DatabaseTable obj)
        {
            if (obj.GetType() == typeof(DBWatchedHistory))
            {
                //A movie has been watched push that out.
                DBWatchedHistory watchedEvent = (DBWatchedHistory)obj;
                playBackHandler(watchedEvent.Movie, TraktScrobbleStates.scrobble);
            }
            else if (obj.GetType() == typeof(DBMovieInfo))
            {
                //A Movie was inserted into the database update trakt
                DBMovieInfo insertedMovie = (DBMovieInfo)obj;
                TraktAPI.SyncMovieLibrary(TraktHandler.CreateSyncData(insertedMovie), TraktSyncModes.library);
            }
        }
        #endregion

        /// <summary>
        /// Clears the users Trakt Library (Note doesn't remove scrobbled movies this has to be done manually)
        /// </summary>
        private void clearTraktLibrary()
        {
            Log.Debug("Trakt: Clearing Trakt Library");
            var moviesInTrakt = TraktAPI.GetMoviesForUser(TraktAPI.Username).ToList();
            Log.Debug("Trakt: Movies Found on Trakt");
            foreach (var movie in moviesInTrakt)
                Log.Debug(string.Format("Trakt: Movie {0}", movie.Title));
            var response = TraktAPI.SyncMovieLibrary(TraktHandler.CreateSyncData(moviesInTrakt), TraktSyncModes.unseen);
            Log.Debug(string.Format("Trakt: Response from removing seen flag {0} {1} {2}", response.Error, response.Status, response.Message));
            response = TraktAPI.SyncMovieLibrary(TraktHandler.CreateSyncData(moviesInTrakt), TraktSyncModes.unlibrary);
            Log.Debug(string.Format("Trakt: Response from removing from library {0} {1} {2}", response.Error, response.Status, response.Message));
        }
    }
}
