using System;
using System.Windows.Forms;
using System.IO;
using MediaPortal;
using MediaPortal.Configuration;
using MediaPortal.GUI.Library;
using MediaPortal.Player;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using TraktPlugin.TraktHandlers;

namespace TraktPlugin
{
    /// <summary>
    /// TraktPlugin for Mediaportal and Moving Pictures. Adds the Trakt scrobbling API to Moving Pictures
    /// Created by Luke Barnett
    /// </summary>
    [PluginIcons("TraktPlugin.Resources.Images.icon_normal.png", "TraktPlugin.Resources.Images.icon_faded.png")]
    public class TraktPlugin : ISetupForm, IPlugin
    {
        #region Private variables
        //List of all our TraktHandlers
        List<ITraktHandler> TraktHandlers = new List<ITraktHandler>();
        //Worker used for syncing libraries
        BackgroundWorker syncLibraryWorker;
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
            return true;
        }

        /// <summary>
        /// Description of the plugin
        /// </summary>
        /// <returns>The Description</returns>
        public string Description()
        {
            return "Trakt actively keeps a record of what TV shows and movies you are watching. Based on your favorites, your friends, and the community, trakt recommends other TV shows and movies.";
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
            Configuration config = new Configuration();
            config.ShowDialog();
        }

        #endregion

        #region IPlugin

        /// <summary>
        /// Starting Point
        /// </summary>
        public void Start()
        {
            TraktLogger.Info("Trakt: Starting");
            TraktSettings.loadSettings();

            if (string.IsNullOrEmpty(TraktSettings.Username) || string.IsNullOrEmpty(TraktSettings.Password))
            {
                TraktLogger.Info("Trakt: Username and/or Password is not set in configuration.");
                Stop();
            }

            TraktLogger.Debug("Trakt: Loading Handlers");
            #region Load Handlers
            if (TraktSettings.MovingPictures != -1)
            {
                try
                {
                    TraktHandlers.Add(new MovingPictures(TraktSettings.MovingPictures));
                }
                catch (IOException)
                {
                    TraktLogger.Error("Trakt: Tried to load Moving Pictures but failed");
                }
            }
            if (TraktSettings.TVSeries != -1)
            {
                try
                {
                    TraktHandlers.Add(new TVSeries(TraktSettings.TVSeries));
                }
                catch (IOException)
                {
                    TraktLogger.Error("Trakt: Tried to load TVSeries but failed");
                }
            }
            #endregion

            TraktLogger.Debug("Trakt: Sorting by Priority");
            TraktHandlers.Sort(delegate(ITraktHandler t1, ITraktHandler t2) { return t1.Priority.CompareTo(t2.Priority); });
            SyncLibrary();
                        
            TraktLogger.Debug("Trakt: Adding Mediaportal Hooks");
            g_Player.PlayBackChanged += new g_Player.ChangedHandler(g_Player_PlayBackChanged);
            g_Player.PlayBackEnded += new g_Player.EndedHandler(g_Player_PlayBackEnded);
            g_Player.PlayBackStarted += new g_Player.StartedHandler(g_Player_PlayBackStarted);
            g_Player.PlayBackStopped += new g_Player.StoppedHandler(g_Player_PlayBackStopped);
            
            if (TraktHandlers.Count == 0)
            {
                TraktLogger.Info("Trakt: We don't have any Handlers so may as well stop");
                Stop();
            }
        }

        /// <summary>
        /// End Point (Clean up)
        /// </summary>
        public void Stop()
        {
            TraktLogger.Debug("Stopping Sync if running");
            syncLibraryWorker.CancelAsync();

            TraktLogger.Debug("Trakt: Removing Mediaportal Hooks");
            g_Player.PlayBackChanged -= new g_Player.ChangedHandler(g_Player_PlayBackChanged);
            g_Player.PlayBackEnded -= new g_Player.EndedHandler(g_Player_PlayBackEnded);
            g_Player.PlayBackStarted -= new g_Player.StartedHandler(g_Player_PlayBackStarted);
            g_Player.PlayBackStopped -= new g_Player.StoppedHandler(g_Player_PlayBackStopped);

            TraktLogger.Debug("Stopping all possible Scrobblings");
            foreach (ITraktHandler traktHandler in TraktHandlers)
                traktHandler.StopScrobble();

            TraktLogger.Info("Trakt: Goodbye");
        }
                
        #endregion

        #region LibraryFunctions

        /// <summary>
        /// Sets up and starts Syncing of Libraries
        /// </summary>
        private void SyncLibrary()
        {
            if (syncLibraryWorker != null && syncLibraryWorker.IsBusy)
                return;

            syncLibraryWorker = new BackgroundWorker();
            syncLibraryWorker.DoWork += new DoWorkEventHandler(syncLibraryWorker_DoWork);
            syncLibraryWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(syncLibraryWorker_RunWorkerCompleted);
            syncLibraryWorker.WorkerSupportsCancellation = true;
            syncLibraryWorker.RunWorkerAsync();
        }

        /// <summary>
        /// End Point for Syncing of Libraries
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void syncLibraryWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //TODO: Callback to let caller know that we are done
            //Possibly stop scrobbling while we are syncing?
        }

        /// <summary>
        /// Logic for the Sync background worker
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void syncLibraryWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            foreach (ITraktHandler traktHandler in TraktHandlers)
            {
                traktHandler.SyncLibrary();
                if (syncLibraryWorker.CancellationPending)
                    return;
            }
        }

        #endregion
                
        #region MediaPortalHooks

        //Various hooks into Mediapotals Video plackback

        private void g_Player_PlayBackStarted(g_Player.MediaType type, string filename)
        {
            if (type == g_Player.MediaType.Video)
            {
                StartScrobble(filename);
            }
        }

        private void g_Player_PlayBackChanged(g_Player.MediaType type, int stoptime, string filename)
        {
            if (type == g_Player.MediaType.Video)
            {
                StartScrobble(filename);
            }
        }

        private void g_Player_PlayBackStopped(g_Player.MediaType type, int stoptime, string filename)
        {
            if (type == g_Player.MediaType.Video)
            {
                StopScrobble();
            }
        }
        
        private void g_Player_PlayBackEnded(g_Player.MediaType type, string filename)
        {
            if (type == g_Player.MediaType.Video)
            {
                StopScrobble();
            }
        }
        
        #endregion

        #region ScrobblingMethods
        /// <summary>
        /// Begins searching our supported plugins libraries to scrobble
        /// </summary>
        /// <param name="filename">The video to search for</param>
        private void StartScrobble(String filename)
        {
            TraktLogger.Debug("Trakt: Making sure that we aren't still scrobbling");
            foreach (ITraktHandler traktHandler in TraktHandlers)
                traktHandler.StopScrobble();

            if (!TraktSettings.BlockedFilenames.Contains(filename))
            {
                TraktLogger.Debug("Trakt: Checking out Libraries for the filename");
                foreach (ITraktHandler traktHandler in TraktHandlers)
                {
                    if (traktHandler.Scrobble(filename))
                    {
                        TraktLogger.Info("Trakt: File was recognised by {0} and is now scrobbling", traktHandler.Name);
                        return;
                    }
                }
                TraktLogger.Info("Trakt: File was not recognised");
            }
            else
                TraktLogger.Info("Trakt: Filename was recognised as blocked by user");
            
        }

        /// <summary>
        /// Stops all scrobbling
        /// </summary>
        private void StopScrobble()
        {
            TraktLogger.Debug("Trakt: Making sure that we aren't still scrobbling");
            foreach (ITraktHandler traktHandler in TraktHandlers)
                traktHandler.StopScrobble();
        }
        #endregion
    }
}
