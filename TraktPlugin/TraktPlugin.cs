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
    public class TraktPlugin : ISetupForm, IPlugin
    {
        #region Private variables
        private BackgroundWorker bgTrakySync = null;
        private Configuration config = null;
        private ProgressBar pbProgress = null;
        private Timer traktTimer = null;
        private DBMovieInfo currentMovie = null;
        private Timer syncTimer = null;
        #endregion

        #region ISetupFrom

        public string Author()
        {
            return "Technicolour";
        }

        public bool CanEnable()
        {
            return true;
        }

        public bool DefaultEnabled()
        {
            return false;
        }

        public string Description()
        {
            return "Adds Trakt scrobbling to Mediaportal";
        }

        public bool GetHome(out string strButtonText, out string strButtonImage, out string strButtonImageFocus, out string strPictureImage)
        {
            strButtonImage = null;
            strButtonText = null;
            strButtonImageFocus = null;
            strPictureImage = null;
            return false;
        }

        public int GetWindowId()
        {
            return -1;
        }

        public bool HasSetup()
        {
            return true;
        }

        public string PluginName()
        {
            return "Trakt";
        }

        public void ShowPlugin()
        {
            if(config == null)
                config = new Configuration(this);
            config.ShowDialog();
        }

        #endregion

        #region IPlugin

        public void Start()
        {
            Log.Debug("Trakt: Plugin Started");
            if (config == null)
                config = new Configuration(this);
            config.LoadConfig();
            Log.Debug("Trakt: Configuration Loaded");
            Log.Debug(String.Format("Trakt: Username: {0} Password: {1}", TraktAPI.Username, TraktAPI.Password));
            //Start Auto Sync
            SyncLibrary(null);
            syncTimer = new Timer();
            syncTimer.Interval = 43200000;
            syncTimer.Tick += new EventHandler(syncTimer_Tick);
            syncTimer.Start();
            //Start Looking for Playback
            g_Player.PlayBackStarted += new g_Player.StartedHandler(g_Player_PlayBackStarted);
            g_Player.PlayBackStopped += new g_Player.StoppedHandler(g_Player_PlayBackStopped);
            g_Player.PlayBackChanged += new g_Player.ChangedHandler(g_Player_PlayBackChanged);
            g_Player.PlayBackEnded += new g_Player.EndedHandler(g_Player_PlayBackEnded);
        }

        void syncTimer_Tick(object sender, EventArgs e)
        {
            SyncLibrary(null);
        }

        
        public void Stop()
        {
            //Remove any event hooks
            Log.Debug("Trakt: Goodbye");
            g_Player.PlayBackStarted -= new g_Player.StartedHandler(g_Player_PlayBackStarted);
            g_Player.PlayBackStopped -= new g_Player.StoppedHandler(g_Player_PlayBackStopped);
            g_Player.PlayBackChanged -= new g_Player.ChangedHandler(g_Player_PlayBackChanged);
            g_Player.PlayBackEnded -= new g_Player.EndedHandler(g_Player_PlayBackEnded);

            if (syncTimer != null)
                syncTimer.Stop();
            if (traktTimer != null)
                traktTimer.Stop();
        }
                
        #endregion

        public void SyncLibrary(ProgressBar bar)
        {
            if (bgTrakySync != null && bgTrakySync.IsBusy)
            {
                return;
            }

            pbProgress = bar;

            bgTrakySync = new BackgroundWorker();
            bgTrakySync.DoWork += new DoWorkEventHandler(bgTrakySync_DoWork);
            bgTrakySync.ProgressChanged += new ProgressChangedEventHandler(bgTrakySync_ProgressChanged);
            bgTrakySync.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bgTrakySync_RunWorkerCompleted);
            bgTrakySync.WorkerReportsProgress = true;
            bgTrakySync.WorkerSupportsCancellation = true;
            bgTrakySync.RunWorkerAsync();
        }

        private void bgTrakySync_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (pbProgress != null)
                pbProgress.Value = 100;
            else
                Log.Debug("Trakt: Sync Completed");
            if (config != null)
                config.SyncCompleted();
        }

        private void bgTrakySync_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (pbProgress != null)
                pbProgress.Value = e.ProgressPercentage;
            else
                Log.Debug(String.Format("Trakt: TraktSync Progress {0}", e.ProgressPercentage.ToString()));
        }

        private void bgTrakySync_DoWork(object sender, DoWorkEventArgs e)
        {
            Log.Debug("Trakt: Starting Sync");

            List<DBMovieInfo> MovieList = DBMovieInfo.GetAll();
            List<DBUser> UserList = DBUser.GetAll();
            List<DBMovieInfo> SeenList = MovieList.Where(m => m.ActiveUserSettings.WatchedCount > 0).ToList();
            List<DBMovieInfo> UnSeenList = MovieList.Where(m => m.ActiveUserSettings.WatchedCount == 0).ToList();

            foreach(DBMovieInfo m in MovieList)
            {
                Log.Debug(string.Format("Trakt: Database: Movie: {0} WatchedHistoryCount {1}", m.Title, m.WatchedHistory.Count.ToString()));
            }

            foreach (DBMovieInfo m in SeenList)
            {
                Log.Debug(string.Format("Trakt: Seen: {0}", m.Title));
            }

            foreach (DBMovieInfo m in UnSeenList)
            {
                Log.Debug(string.Format("Trakt: UnSeen: {0}", m.Title));
            }

            Log.Debug("Trakt: Sending Seen List");
            TraktResponse response = TraktAPI.SyncMovieLibrary(TraktHandler.CreateSyncData(SeenList), TraktSyncModes.seen);
            Log.Debug(String.Format("Trakt: Response from Trakt, {0} {1} {2}", response.Status, response.Message, response.Error));
            bgTrakySync.ReportProgress(50);

            Log.Debug("Trakt: Sending UnSeen List");
            response = TraktAPI.SyncMovieLibrary(TraktHandler.CreateSyncData(UnSeenList), TraktSyncModes.library);
            Log.Debug(String.Format("Trakt: Response from Trakt, {0} {1} {2}", response.Status, response.Message, response.Error));
            bgTrakySync.ReportProgress(75);


            Log.Debug("Getting Library from Trakt");
            List<TraktLibraryMovies> NoLongerInOurLibrary = new List<TraktLibraryMovies>();

            //Get the Movie Library from Trakt
            foreach (TraktLibraryMovies tlm in TraktAPI.GetMoviesForUser(TraktAPI.Username))
            {
                bool notInLibrary = true;
                foreach (DBMovieInfo libraryMovie in MovieList)
                {
                    if (libraryMovie.ImdbID == tlm.IMDBID)
                    {
                        if (tlm.Watched && libraryMovie.ActiveUserSettings.WatchedCount == 0)
                        {
                            //TODO: Update Moving Pictures watched flag.
                            Log.Debug(String.Format("Movie {0} is watched on Trakt updating Database",libraryMovie.Title));
                            libraryMovie.ActiveUserSettings.WatchedCount = 1;
                            libraryMovie.Commit();
                        }
                        notInLibrary = false;
                        break;
                    }
                }
                if (notInLibrary)
                    NoLongerInOurLibrary.Add(tlm);
            }

            Log.Debug("Trakt: Removing Additional Movies From Trakt");
            response = TraktAPI.SyncMovieLibrary(TraktHandler.CreateSyncData(NoLongerInOurLibrary), TraktSyncModes.unlibrary);
            Log.Debug(String.Format("Trakt: Response from Trakt, {0} {1} {2}", response.Status, response.Message, response.Error));


            Log.Debug("Trakt: Sync Completed");
        }

        public void StopSync()
        {
            if (bgTrakySync != null && bgTrakySync.IsBusy)
            {
                bgTrakySync.CancelAsync();
            }
        }

        void g_Player_PlayBackStarted(g_Player.MediaType type, string filename)
        {
            checkPlayback(filename);
        }

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

        void traktTimer_Tick(object sender, EventArgs e)
        {
            playBackHandler(currentMovie, TraktScrobbleStates.watching);
        }

        void g_Player_PlayBackEnded(g_Player.MediaType type, string filename)
        {
            if (currentMovie != null)
                playBackHandler(currentMovie, TraktScrobbleStates.scrobble);
            if (traktTimer != null)
                traktTimer.Stop();
            traktTimer = null;
            currentMovie = null;
        }

        void g_Player_PlayBackChanged(g_Player.MediaType type, int stoptime, string filename)
        {
            checkPlayback(filename);
        }

        void g_Player_PlayBackStopped(g_Player.MediaType type, int stoptime, string filename)
        {
            //Check if we have watched enough of the movie to lock it in or to clear trakt
            //Clear trakt
        }

        void playBackHandler(DBMovieInfo movie, TraktScrobbleStates state)
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
    }
}
