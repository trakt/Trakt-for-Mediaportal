using System;
using System.Windows.Forms;
using System.IO;
using MediaPortal;
using MediaPortal.GUI.Library;
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
            //Start Looking for Playback
        }
        
        public void Stop()
        {
            //Remove any event hooks
            Log.Debug("Trakt: Goodbye");
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
            List<DBMovieInfo> SeenList = MovieList.Where(m => m.WatchedHistory.Count > 0).ToList();
            List<DBMovieInfo> UnSeenList = MovieList.Where(m => m.WatchedHistory.Count == 0).ToList();

            Log.Debug("Trakt: Sending Seen List");
            TraktResponse response = TraktAPI.SyncMovieLibrary(TraktHandler.CreateSyncData(SeenList), TraktSyncModes.seen);
            Log.Debug(String.Format("Trakt: Response from Trakt, {0} {1} {2}", response.Status, response.Message, response.Error));
            bgTrakySync.ReportProgress(50);

            Log.Debug("Trakt: Sending UnSeen List");
            response = TraktAPI.SyncMovieLibrary(TraktHandler.CreateSyncData(UnSeenList), TraktSyncModes.library);
            Log.Debug(String.Format("Trakt: Response from Trakt, {0} {1} {2}", response.Status, response.Message, response.Error));
            bgTrakySync.ReportProgress(75);

            //Get the Movie Library from Trakt

            List<DBMovieInfo> TraktLibrary = new List<DBMovieInfo>();            
            //Convert TraktMovies to DBMovieInfo
            foreach (TraktLibraryMovies tlm in TraktAPI.GetMoviesForUser(TraktAPI.Username).ToList())
            {
                Log.Debug(String.Format("Trakt: From Trakt {0}",tlm.Title));
                List<DBMovieInfo> temp = MovieList.Where(m => m.ImdbID == tlm.IMDBID).ToList();
                TraktLibrary.AddRange(temp);
            }

            List<DBMovieInfo> RemoveFromTraktLibrary = TraktLibrary.Where(m => MovieList.Contains(m) == false).ToList();

            //TODO!!! FIX THIS SO IT WORKS
            Log.Debug("Trakt: Removing Additional Movies From Trakt");
            response = TraktAPI.SyncMovieLibrary(TraktHandler.CreateSyncData(RemoveFromTraktLibrary), TraktSyncModes.unlibrary);
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
    }
}
