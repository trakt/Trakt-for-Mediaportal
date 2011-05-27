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
using TraktPlugin.GUI;
using TraktPlugin.TraktHandlers;

namespace TraktPlugin
{
    /// <summary>
    /// TraktPlugin for Mediaportal and Moving Pictures. Adds the Trakt scrobbling API to Moving Pictures
    /// Created by Luke Barnett
    /// </summary>
    [PluginIcons("TraktPlugin.Resources.Images.icon_normal.png", "TraktPlugin.Resources.Images.icon_faded.png")]
    public class TraktPlugin : GUIWindow, ISetupForm
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
            return "Technicolour, ltfearme";
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
            strButtonText = PluginName();
            strButtonImage = string.Empty;
            strButtonImageFocus = string.Empty;
            strPictureImage = "hover_trakt.png";
            // dont display on home screen if skin doesn't exist.
            return File.Exists(GUIGraphicsContext.Skin + @"\Trakt.xml");
        }

        /// <summary>
        /// Gets the Window id accociated with the plugin
        /// </summary>
        /// <returns>The window id</returns>
        public int GetWindowId()
        {
            return (int)GUIWindows.Main;
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
            return GUIUtils.PluginName();
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

        #region GUI Windows

        enum GUIWindows
        {
            Main = 87258,
            Calendar = 87259,
            Friends = 87260,
            Recommendations = 87261,
            RecommendationsShows = 87262,
            RecommendationsMovies = 87263,
            Trending = 87264,
            TrendingShows = 87265,
            TrendingMovies = 87266,
            WatchedList = 87267,
            WatchedListShows = 87268,
            WatchedListEpisodes = 87269,
            WatchedListMovies = 87270,
            Settings = 87271
        }

        #endregion

        #region GUIWindow Overrides

        public override int GetID
        {
            get
            {
                return (int)GUIWindows.Main;
            }
        }

        /// <summary>
        /// Starting Point
        /// </summary>
        public override bool Init()
        {
            TraktLogger.Info("Starting Trakt v{0}", System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString());
            TraktSettings.loadSettings();

            if (string.IsNullOrEmpty(TraktSettings.Username) || string.IsNullOrEmpty(TraktSettings.Password))
            {
                TraktLogger.Info("Username and/or Password is not set in configuration, stopping plugin load.");
                DeInit();
                return true;
            }

            TraktLogger.Debug("Loading Handlers");
            #region Load Handlers
            string errorMessage = "Tried to load {0} but failed, check minimum requirements are met!";
            if (TraktSettings.MovingPictures != -1)
            {
                try
                {
                    TraktHandlers.Add(new MovingPictures(TraktSettings.MovingPictures));
                }
                catch (Exception)
                {
                    TraktLogger.Error(errorMessage, "Moving Pictures");
                }
            }
            if (TraktSettings.TVSeries != -1)
            {
                try
                {
                    TraktHandlers.Add(new TVSeries(TraktSettings.TVSeries));
                }
                catch (Exception)
                {
                    TraktLogger.Error(errorMessage, "MP-TVSeries");
                }
            }
            if (TraktSettings.MyVideos != -1)
            {
                try
                {
                    TraktHandlers.Add(new MyVideos(TraktSettings.MyVideos));
                }
                catch (Exception)
                {
                    TraktLogger.Error(errorMessage, "My Videos");
                }
            }
            #endregion

            TraktLogger.Debug("Sorting by Priority");
            TraktHandlers.Sort(delegate(ITraktHandler t1, ITraktHandler t2) { return t1.Priority.CompareTo(t2.Priority); });
            SyncLibrary();
                        
            TraktLogger.Debug("Adding Mediaportal Hooks");
            g_Player.PlayBackChanged += new g_Player.ChangedHandler(g_Player_PlayBackChanged);
            g_Player.PlayBackEnded += new g_Player.EndedHandler(g_Player_PlayBackEnded);
            g_Player.PlayBackStarted += new g_Player.StartedHandler(g_Player_PlayBackStarted);
            g_Player.PlayBackStopped += new g_Player.StoppedHandler(g_Player_PlayBackStopped);

            if (TraktHandlers.Count == 0)
            {
                TraktLogger.Info("We don't have any Handlers so may as well stop");
                DeInit();
            }
            else
            {
                Timer syncLibraryTimer = new Timer();
                syncLibraryTimer.Tick += new EventHandler(delegate(object o, EventArgs e)
                {
                    System.Threading.Thread.Sleep(60);
                    SyncLibrary();
                });
                syncLibraryTimer.Interval = TraktSettings.SyncTimerLength;
                syncLibraryTimer.Enabled = true;
            }

            // Load all available translation strings
            // so skins have access to them
            foreach (string name in Translation.Strings.Keys)
            {
                GUIUtils.SetProperty("#Trakt.Translation." + name + ".Label", Translation.Strings[name]);
            }

            // Initialize skin settings
            TraktSkinSettings.Init();
            // Listen to this event to detect skin changes in GUI
            GUIWindowManager.OnDeActivateWindow += new GUIWindowManager.WindowActivationHandler(GUIWindowManager_OnDeActivateWindow);

            // Load main skin window
            // this is a launching pad to all other windows
            string xmlSkin = GUIGraphicsContext.Skin + @"\Trakt.xml";
            TraktLogger.Info("Loading main skin window: " + xmlSkin);
            return Load(xmlSkin);            
        }

        /// <summary>
        /// End Point (Clean up)
        /// </summary>
        public override void DeInit()
        {
            if (syncLibraryWorker != null)
            {
                TraktLogger.Debug("Stopping Sync if running");
                syncLibraryWorker.CancelAsync();
            }

            TraktLogger.Debug("Removing Mediaportal Hooks");
            g_Player.PlayBackChanged -= new g_Player.ChangedHandler(g_Player_PlayBackChanged);
            g_Player.PlayBackEnded -= new g_Player.EndedHandler(g_Player_PlayBackEnded);
            g_Player.PlayBackStarted -= new g_Player.StartedHandler(g_Player_PlayBackStarted);
            g_Player.PlayBackStopped -= new g_Player.StoppedHandler(g_Player_PlayBackStopped);

            TraktLogger.Debug("Stopping all possible Scrobblings");
            foreach (ITraktHandler traktHandler in TraktHandlers)
                traktHandler.StopScrobble();

            // save settings
            TraktSettings.saveSettings();

            TraktLogger.Info("Goodbye");
            base.DeInit();
        }

        protected override void OnPageLoad()
        {
            base.OnPageLoad();
        }

        protected override void OnClicked(int controlId, GUIControl control, MediaPortal.GUI.Library.Action.ActionType actionType)
        {
            base.OnClicked(controlId, control, actionType);

            switch (controlId)
            {
                default:
                    break;
            }
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
                
        #region MediaPortal Playback Hooks

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

        #region MediaPortal Window Hooks

        void GUIWindowManager_OnDeActivateWindow(int windowID)
        {
            // Settings/General window
            // this is where a user can change skins from GUI
            if (windowID == (int)Window.WINDOW_SETTINGS_SKIN)
            {
                // did skin change?
                if (TraktSkinSettings.CurrentSkin != TraktSkinSettings.PreviousSkin)
                {
                    TraktLogger.Info("Skin Change detected in GUI, reloading skin settings");
                    TraktSkinSettings.Init();
                }
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
            TraktLogger.Debug("Making sure that we aren't still scrobbling");
            foreach (ITraktHandler traktHandler in TraktHandlers)
                traktHandler.StopScrobble();

            if (!TraktSettings.BlockedFilenames.Contains(filename) && !TraktSettings.BlockedFolders.Any(f => filename.Contains(f)))
            {
                TraktLogger.Debug("Checking out Libraries for the filename");
                foreach (ITraktHandler traktHandler in TraktHandlers)
                {
                    if (traktHandler.Scrobble(filename))
                    {
                        TraktLogger.Info("File was recognised by {0} and is now scrobbling", traktHandler.Name);
                        return;
                    }
                }
                TraktLogger.Info("File was not recognised");
            }
            else
                TraktLogger.Info("Filename was recognised as blocked by user");
            
        }

        /// <summary>
        /// Stops all scrobbling
        /// </summary>
        private void StopScrobble()
        {
            TraktLogger.Debug("Making sure that we aren't still scrobbling");
            foreach (ITraktHandler traktHandler in TraktHandlers)
                traktHandler.StopScrobble();
        }
        #endregion
    }
}
