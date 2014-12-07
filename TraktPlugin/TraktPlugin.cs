using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using MediaPortal.Configuration;
using MediaPortal.GUI.Library;
using MediaPortal.Player;
using TraktPlugin.GUI;
using TraktPlugin.TraktAPI.DataStructures;
using TraktPlugin.TraktAPI.Enums;
using TraktPlugin.TraktAPI.Extensions;
using TraktPlugin.TraktHandlers;

namespace TraktPlugin
{
    /// <summary>
    /// TraktPlugin for Mediaportal.
    /// Created by Luke Barnett, Damien Haynes
    /// </summary>
    [PluginIcons("TraktPlugin.Resources.Images.icon_normal.png", "TraktPlugin.Resources.Images.icon_faded.png")]
    public class TraktPlugin : GUIWindow, ISetupForm
    {
        #region Private Variables
        // List of all our TraktHandlers
        List<ITraktHandler> TraktHandlers = new List<ITraktHandler>();
        // Worker used for syncing libraries
        BackgroundWorker syncLibraryWorker;
        static Timer syncLibraryTimer;
        // Settings Management from MPEI
        ExtensionSettings extensionSettings = new ExtensionSettings();
        // Dashboard - Activity / Trending Items
        TraktDashboard dashBoard = new TraktDashboard();
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
            return "Trakt actively keeps a record of what TV shows and movies you are watching. Based on your favourites, your friends, and the community, trakt recommends other TV shows and movies.";
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
            return (int)TraktGUIWindows.Main;
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

        #region GUIWindow Overrides

        /// <summary>
        /// will set #currentmodule property on pageload
        /// </summary>
        /// <returns></returns>
        public override string GetModuleName()
        {
            return GUIUtils.PluginName();
        }

        public override int GetID
        {
            get
            {
                return (int)TraktGUIWindows.Main;
            }
        }

        /// <summary>
        /// Starting Point
        /// </summary>
        public override bool Init()
        {
            TraktLogger.Info("Starting Trakt plugin. Version = '{0}', Date = '{1}'", TraktSettings.Version, TraktSettings.BuildDate);

            TraktSettings.PerformMaintenance();
            TraktSettings.LoadSettings();

            // Load plugins we want to sync
            LoadPluginHandlers();

            // Sync Libaries now and periodically
            syncLibraryTimer = new Timer(new TimerCallback((o) => { SyncLibrary(); }), null, TraktSettings.SyncStartDelay, TraktSettings.SyncTimerLength);
            
            TraktLogger.Debug("Adding MediaPortal event handlers");
            g_Player.PlayBackChanged += new g_Player.ChangedHandler(g_Player_PlayBackChanged);
            g_Player.PlayBackEnded += new g_Player.EndedHandler(g_Player_PlayBackEnded);
            g_Player.PlayBackStarted += new g_Player.StartedHandler(g_Player_PlayBackStarted);
            g_Player.PlayBackStopped += new g_Player.StoppedHandler(g_Player_PlayBackStopped);

            // Listen to this event to detect skin\language changes in GUI
            GUIWindowManager.OnDeActivateWindow += new GUIWindowManager.WindowActivationHandler(GUIWindowManager_OnDeActivateWindow);
            GUIWindowManager.OnActivateWindow += new GUIWindowManager.WindowActivationHandler(GUIWindowManager_OnActivateWindow);
            GUIWindowManager.Receivers += new SendMessageHandler(GUIWindowManager_Receivers);
            
            // Initialize translations
            Translation.Init();

            // Initialize skin settings
            TraktSkinSettings.Init();

            // Initialize genres
            TraktGenres.Init();

            // Initialize Extension Settings
            extensionSettings.Init();

            // Initialize Skin Dashboard
            dashBoard.Init();

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
                TraktLogger.Debug("Stopping any plugins currently syncing library");
                syncLibraryWorker.CancelAsync();
            }

            TraktLogger.Debug("Removing MediaPortal event handlers");
            g_Player.PlayBackChanged -= g_Player_PlayBackChanged;
            g_Player.PlayBackEnded -= g_Player_PlayBackEnded;
            g_Player.PlayBackStarted -= g_Player_PlayBackStarted;
            g_Player.PlayBackStopped -= g_Player_PlayBackStopped;

            GUIWindowManager.OnDeActivateWindow -= GUIWindowManager_OnDeActivateWindow;
            GUIWindowManager.OnActivateWindow -= GUIWindowManager_OnActivateWindow;
            GUIWindowManager.Receivers -= GUIWindowManager_Receivers;

            TraktLogger.Debug("Stopping any plugins currently scrobbling");
            foreach (ITraktHandler traktHandler in TraktHandlers)
                traktHandler.StopScrobble();

            // Unload Plugin Handlers
            UnLoadPluginHandlers();

            // save dashboard data
            //TODOTraktSettings.LastActivityLoad = dashBoard.PreviousActivity;
            //TODOTraktSettings.LastTrendingMovies = dashBoard.PreviousTrendingMovies;
            //TODOTraktSettings.LastTrendingShows = dashBoard.PreviousTrendingShows;
            //TODOTraktSettings.LastStatistics = dashBoard.PreviousStatistics;

            // save settings
            TraktSettings.SaveSettings();

            TraktLogger.Info("Plugin has successfully unloaded");
            base.DeInit();
        }

        protected override void OnPageLoad()
        {
            base.OnPageLoad();
        }

        #endregion

        #region Plugin Handlers

        private void UnLoadPluginHandlers()
        {
            TraktLogger.Debug("Un-Loading Plugin Handlers");

            #region MovingPictures
            if (TraktHandlers.Exists(p => p.Name == "Moving Pictures"))
            {
                ITraktHandler item = TraktHandlers.FirstOrDefault(p => p.Name == "Moving Pictures");
                (item as MovingPictures).DisposeEvents();
                TraktHandlers.Remove(item);
            }
            #endregion

            #region MP-TVSeries
            if (TraktHandlers.Exists(p => p.Name == "MP-TVSeries"))
            {
                ITraktHandler item = TraktHandlers.FirstOrDefault(p => p.Name == "MP-TVSeries");
                (item as TVSeries).DisposeEvents();
                TraktHandlers.Remove(item);

            }
            #endregion

            #region My Videos
            if (TraktHandlers.Exists(p => p.Name == "My Videos"))
            {
                TraktHandlers.RemoveAll(p => p.Name == "My Videos");
            }
            #endregion

            #region My Films
            if (TraktHandlers.Exists(p => p.Name == "My Films"))
            {
                ITraktHandler item = TraktHandlers.FirstOrDefault(p => p.Name == "My Films");
                (item as MyFilmsHandler).DisposeEvents();
                TraktHandlers.Remove(item);
            }
            #endregion

            #region OnlineVideos
            if (TraktHandlers.Exists(p => p.Name == "OnlineVideos"))
            {
                ITraktHandler item = TraktHandlers.FirstOrDefault(p => p.Name == "OnlineVideos");
                (item as TraktHandlers.OnlineVideos).DisposeEvents();
                TraktHandlers.Remove(item);
            }
            #endregion

            #region My TV Recordings
            if (TraktHandlers.Exists(p => p.Name == "My TV Recordings"))
            {
                TraktHandlers.RemoveAll(p => p.Name == "My TV Recordings");
            }
            #endregion

            #region My TV Live
            if (TraktHandlers.Exists(p => p.Name == "My TV Live"))
            {
                TraktHandlers.RemoveAll(p => p.Name == "My TV Live");
            }
            #endregion

            #region 4TR TV Recordings
            if (TraktHandlers.Exists(p => p.Name == "4TR TV Recordings"))
            {
                TraktHandlers.RemoveAll(p => p.Name == "4TR TV Recordings");
            }
            #endregion

            #region 4TR TV Live
            if (TraktHandlers.Exists(p => p.Name == "4TR TV Live"))
            {
                TraktHandlers.RemoveAll(p => p.Name == "4TR TV Live");
            }
            #endregion

            #region Argus TV Recordings
            if (TraktHandlers.Exists(p => p.Name == "Argus TV Recordings"))
            {
                TraktHandlers.RemoveAll(p => p.Name == "Argus TV Recordings");
            }
            #endregion

            #region Argus TV Live
            if (TraktHandlers.Exists(p => p.Name == "Argus TV Live"))
            {
                TraktHandlers.RemoveAll(p => p.Name == "Argus TV Live");
            }
            #endregion
        }

        private void LoadPluginHandlers()
        {
            TraktLogger.Debug("Loading Plugin Handlers");
            string errorMessage = "Tried to load plugin handler '{0}' but failed, check minimum requirements have been met!";
            
            #region MovingPictures
            try
            {
                bool handlerExists = TraktHandlers.Exists(p => p.Name == "Moving Pictures");
                if (!handlerExists && TraktSettings.MovingPictures != -1)
                    TraktHandlers.Add(new MovingPictures(TraktSettings.MovingPictures));
                else if (handlerExists && TraktSettings.MovingPictures == -1)
                {
                    ITraktHandler item = TraktHandlers.FirstOrDefault(p => p.Name == "Moving Pictures");
                    (item as MovingPictures).DisposeEvents();
                    TraktHandlers.Remove(item);
                }
            }
            catch (Exception)
            {
                TraktLogger.Error(errorMessage, "Moving Pictures");
            }
            #endregion

            #region MP-TVSeries
            try
            {
                bool handlerExists = TraktHandlers.Exists(p => p.Name == "MP-TVSeries");
                if (!handlerExists && TraktSettings.TVSeries != -1)
                    TraktHandlers.Add(new TVSeries(TraktSettings.TVSeries));
                else if (handlerExists && TraktSettings.TVSeries == -1)
                {
                    ITraktHandler item = TraktHandlers.FirstOrDefault(p => p.Name == "MP-TVSeries");
                    (item as TVSeries).DisposeEvents();
                    TraktHandlers.Remove(item);

                }
            }
            catch (Exception)
            {
                TraktLogger.Error(errorMessage, "MP-TVSeries");
            }
            #endregion

            #region My Videos
            try
            {
                bool handlerExists = TraktHandlers.Exists(p => p.Name == "My Videos");
                if (!handlerExists && TraktSettings.MyVideos != -1)
                    TraktHandlers.Add(new MyVideos(TraktSettings.MyVideos));
                else if (handlerExists && TraktSettings.MyVideos == -1)
                    TraktHandlers.RemoveAll(p => p.Name == "My Videos");
            }
            catch (Exception)
            {
                TraktLogger.Error(errorMessage, "My Videos");
            }
            #endregion

            #region My Films
            try
            {
                bool handlerExists = TraktHandlers.Exists(p => p.Name == "My Films");
                if (!handlerExists && TraktSettings.MyFilms != -1)
                    TraktHandlers.Add(new MyFilmsHandler(TraktSettings.MyFilms));
                else if (handlerExists && TraktSettings.MyFilms == -1)
                {
                    ITraktHandler item = TraktHandlers.FirstOrDefault(p => p.Name == "My Films");
                    (item as MyFilmsHandler).DisposeEvents();
                    TraktHandlers.Remove(item);
                }
            }
            catch (Exception)
            {
                TraktLogger.Error(errorMessage, "My Films");
            }
            #endregion

            #region OnlineVideos
            try
            {
                bool handlerExists = TraktHandlers.Exists(p => p.Name == "OnlineVideos");
                if (!handlerExists && TraktSettings.OnlineVideos != -1)
                    TraktHandlers.Add(new TraktHandlers.OnlineVideos(TraktSettings.OnlineVideos));
                else if (handlerExists && TraktSettings.OnlineVideos == -1)
                {
                    ITraktHandler item = TraktHandlers.FirstOrDefault(p => p.Name == "OnlineVideos");
                    (item as TraktHandlers.OnlineVideos).DisposeEvents();
                    TraktHandlers.Remove(item);
                }
            }
            catch (Exception)
            {
                TraktLogger.Error(errorMessage, "OnlineVideos");
            }
            #endregion

            #region My TV Recordings
            try
            {
                bool handlerExists = TraktHandlers.Exists(p => p.Name == "My TV Recordings");
                if (!handlerExists && TraktSettings.MyTVRecordings != -1)
                    TraktHandlers.Add(new MyTVRecordings(TraktSettings.MyTVRecordings));
                else if (handlerExists && TraktSettings.MyTVRecordings == -1)
                    TraktHandlers.RemoveAll(p => p.Name == "My TV Recordings");
            }
            catch (Exception)
            {
                TraktLogger.Error(errorMessage, "My TV Recordings");
            }
            #endregion

            #region My TV Live
            try
            {
                bool handlerExists = TraktHandlers.Exists(p => p.Name == "My TV Live");
                if (!handlerExists && TraktSettings.MyTVLive != -1)
                    TraktHandlers.Add(new MyTVLive(TraktSettings.MyTVLive));
                else if (handlerExists && TraktSettings.MyTVRecordings == -1)
                    TraktHandlers.RemoveAll(p => p.Name == "My TV Live");
            }
            catch (Exception)
            {
                TraktLogger.Error(errorMessage, "My TV Live");
            }
            #endregion

            #region Argus TV Recordings
            try
            {
                bool handlerExists = TraktHandlers.Exists(p => p.Name == "Argus TV Recordings");
                if (!handlerExists && TraktSettings.ArgusRecordings != -1)
                    TraktHandlers.Add(new ArgusTVRecordings(TraktSettings.ArgusRecordings));
                else if (handlerExists && TraktSettings.ArgusRecordings == -1)
                    TraktHandlers.RemoveAll(p => p.Name == "Argus TV Recordings");
            }
            catch (Exception)
            {
                TraktLogger.Error(errorMessage, "Argus TV Recordings");
            }
            #endregion

            #region Argus TV Live
            try
            {
                bool handlerExists = TraktHandlers.Exists(p => p.Name == "Argus TV Live");
                if (!handlerExists && TraktSettings.ArgusTVLive != -1)
                    TraktHandlers.Add(new ArgusTVLive(TraktSettings.ArgusTVLive));
                else if (handlerExists && TraktSettings.ArgusTVLive == -1)
                    TraktHandlers.RemoveAll(p => p.Name == "Argus TV Live");
            }
            catch (Exception)
            {
                TraktLogger.Error(errorMessage, "Argus TV Live");
            }
            #endregion

            if (TraktHandlers.Count == 0)
            {
                TraktLogger.Info("There are no Plugin Handlers configured!");
            }
            else
            {
                TraktLogger.Debug("Sorting Plugin Handlers by priority");
                TraktHandlers.Sort(delegate(ITraktHandler t1, ITraktHandler t2) { return t1.Priority.CompareTo(t2.Priority); });
            }
        }

        #endregion

        #region Library Functions

        /// <summary>
        /// Changes the period and start time of the Library Sync
        /// </summary>
        /// <param name="dueTime">initial time in milliseconds to wait before starting sync</param>
        /// <param name="period">period of time to wait inbetween sync's</param>
        public static void ChangeSyncTimer(int dueTime, int period)
        {
            syncLibraryTimer.Change(dueTime, period);
        }

        /// <summary>
        /// Sets up and starts Syncing of Libraries
        /// </summary>
        private void SyncLibrary()
        {
            // no plugins to sync, abort
            if (TraktHandlers.Count == 0) return;

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
            if (string.IsNullOrEmpty(Thread.CurrentThread.Name))
                Thread.CurrentThread.Name = "Sync";

            TraktLogger.Info("Library Sync complete for all enabled plugins");

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
            Thread.CurrentThread.Name = "Sync";

            if (TraktSettings.AccountStatus != ConnectionState.Connected)
                return;

            TraktLogger.Info("Library Sync started for all enabled plugins");

            // User could change handlers during sync from Settings so assign new list
            List<ITraktHandler> traktHandlers = new List<ITraktHandler>(TraktHandlers);
            foreach (ITraktHandler traktHandler in traktHandlers)
            {
                try
                {
                    traktHandler.SyncLibrary();
                }
                catch (Exception ex)
                {
                    TraktLogger.Error("Error synchronising library. Plugin = '{0}', Error = '{1}'", traktHandler.Name, ex.Message);
                }

                if (syncLibraryWorker.CancellationPending)
                    return;
            }    
        }

        #endregion
                
        #region MediaPortal Playback Hooks

        private void g_Player_PlayBackStarted(g_Player.MediaType type, string filename)
        {
            if (IsValidScrobbleType(type))
            {
                StartScrobble(filename);
            }
        }

        private void g_Player_PlayBackChanged(g_Player.MediaType type, int stoptime, string filename)
        {
            if (IsValidScrobbleType(type))
            {
                StartScrobble(filename);
            }
        }

        private void g_Player_PlayBackStopped(g_Player.MediaType type, int stoptime, string filename)
        {
            if (IsValidScrobbleType(type))
            {
                StopScrobble();
            }
        }
        
        private void g_Player_PlayBackEnded(g_Player.MediaType type, string filename)
        {
            if (IsValidScrobbleType(type))
            {
                StopScrobble();
            }
        }

        private bool IsValidScrobbleType(g_Player.MediaType type)
        {
            return type == g_Player.MediaType.Video || type == g_Player.MediaType.Recording || type == g_Player.MediaType.TV;
        }

        #endregion

        #region MediaPortal Window Hooks

        int PreviousWindow = 0;
        void GUIWindowManager_OnDeActivateWindow(int windowID)
        {
            // Settings/General window
            // this is where a user can change skins\languages from GUI
            if (windowID == (int)ExternalPluginWindows.MPSkinSettings)
            {
                // did skin change?
                if (TraktSkinSettings.CurrentSkin != TraktSkinSettings.PreviousSkin)
                {
                    TraktLogger.Info("Skin change detected in GUI, reloading skin settings");
                    TraktSkinSettings.Init();
                }

                // did language change?
                if (Translation.CurrentLanguage != Translation.PreviousLanguage)
                {
                    TraktLogger.Info("Language Changed to '{0}' from GUI, re-initializing translations", Translation.CurrentLanguage);
                    Translation.Init();
                }
            }

            PreviousWindow = windowID;
        }

        bool ConnectionChecked = false;
        bool FollowerRequestsChecked = false;
        void GUIWindowManager_OnActivateWindow(int windowID)
        {
            #region Connection Check
            // We can Notify in GUI now that its initialized
            // only need this if previous connection attempt was unauthorized on Init()
            if (!ConnectionChecked)
            {
                ConnectionChecked = true;
                Thread checkStatus = new Thread(delegate()
                {
                    if (TraktSettings.AccountStatus == ConnectionState.Invalid)
                    {
                        TraktSettings.AccountStatus = ConnectionState.Pending;
                        // Re-Check and Notify
                        if (TraktSettings.AccountStatus == ConnectionState.Invalid)
                        {
                            Thread.Sleep(10000);
                            GUIUtils.ShowNotifyDialog(Translation.Error, Translation.UnAuthorized);
                        }
                    }
                })
                {
                    IsBackground = true,
                    Name = "CheckConnection"
                };
                checkStatus.Start();
            }
            #endregion

            #region Plugin Handler Check
            // If we exit settings, we may need to reload plugin handlers
            // Also Prompt to Sync / Warn users if no plugin handlers are defined
            if ((windowID < (int)TraktGUIWindows.Settings || windowID > (int)TraktGUIWindows.SettingsGeneral) &&
                (PreviousWindow >= (int)TraktGUIWindows.Settings && PreviousWindow <= (int)TraktGUIWindows.SettingsGeneral))
            {
              Thread pluginHandlerCheckThread = new Thread(delegate(object obj)
              {
                if (GUISettingsPlugins.PluginHandlersChanged)
                {
                  LoadPluginHandlers();
                }

                // Help user get started if no plugins enabled
                if (TraktHandlers.Count == 0)
                {
                  if (GUIUtils.ShowYesNoDialog(Translation.Plugins, Translation.NoPluginsEnabled, true))
                  {
                    GUIWindowManager.ActivateWindow((int)TraktGUIWindows.SettingsPlugins);
                  }
                  return;
                }

                if (GUISettingsPlugins.PluginHandlersAdded)
                {
                  if (GUIUtils.ShowYesNoDialog(Translation.Synchronize, Translation.SynchronizeNow, true))
                      ChangeSyncTimer(0, TraktSettings.SyncTimerLength);
                }

                GUISettingsPlugins.PluginHandlersAdded = false;
                GUISettingsPlugins.PluginHandlersChanged = false;
              })
              {
                IsBackground = true,
                Name = "PluginCheck"
              };
              pluginHandlerCheckThread.Start();
            }
            #endregion

            #region Follower Requests Check
            if (TraktSettings.GetFollowerRequestsOnStartup && !FollowerRequestsChecked)
            {
                FollowerRequestsChecked = true;
                var followerReqThread = new Thread((o) =>
                {
                    if (TraktSettings.AccountStatus == ConnectionState.Connected)
                    {
                        var followerRequests = TraktCache.FollowerRequests;
                        TraktLogger.Info("Found {0} follower requests for user", followerRequests.Count());
                        if (followerRequests.Count() > 0)
                        {
                            Thread.Sleep(10000);
                            GUIUtils.ShowNotifyDialog(Translation.FollowerRequests, string.Format(Translation.FollowerRequestMessage, followerRequests.Count().ToString()));
                        }
                    }
                })
                {
                    IsBackground = true,
                    Name = "FollowReq"
                };

                followerReqThread.Start();
            }
            #endregion

            #region Dashboard Start

            if (TraktSkinSettings.DashBoardActivityWindows != null && TraktSkinSettings.DashBoardActivityWindows.Contains(windowID.ToString()))
            {
                dashBoard.StartActivityPolling();
            }
            else
            {
                dashBoard.StopActivityPolling();
            }

            if (TraktSkinSettings.DashboardTrendingCollection != null && TraktSkinSettings.DashboardTrendingCollection.Exists(d => d.MovieWindows.Contains(windowID.ToString())))
            {
                dashBoard.StartTrendingMoviesPolling();
            }
            else
            {
                dashBoard.StopTrendingMoviesPolling();
            }

            if (TraktSkinSettings.DashboardTrendingCollection != null && TraktSkinSettings.DashboardTrendingCollection.Exists(d => d.TVShowWindows.Contains(windowID.ToString())))
            {
                dashBoard.StartTrendingShowsPolling();
            }
            else
            {
                dashBoard.StopTrendingShowsPolling();
            }

            #endregion

            if (windowID == (int)ExternalPluginWindows.MPEISettings)
            {
                // save our settings now so we dont get out of sync
                // with extension settings
                TraktLogger.Debug("Entering Extension Settings window");
                TraktSettings.SaveSettings();
            }
        }

        void GUIWindowManager_Receivers(GUIMessage message)
        {
            bool validWatchListItem = false;
            bool validCustomListItem = false;
            bool validRateItem = false;
            bool validShoutItem = false;
            bool validRelatedItem = false;
            bool validTraktMenuItem = false;
            bool validSearchItem = false;
            bool updatePluginFilters = false;
            string title = string.Empty;
            string year = string.Empty;
            string imdbid = string.Empty;
            string tmdbid = string.Empty;
            string showtvdbid = string.Empty;
            string epTvdbId = string.Empty;
            string season = string.Empty;
            string episode = string.Empty;
            string fanart = string.Empty;
            bool isWatched = false;
            SearchPeople searchPeople = null;
            string type = "movie";            

            switch (message.Message)
            {
                case GUIMessage.MessageType.GUI_MSG_CLICKED:
                    switch (GUIWindowManager.ActiveWindow)
                    {
                        case (int)ExternalPluginWindows.OnlineVideos:
                            #region WatchList/CustomList Button
                            switch (message.SenderControlId)
                            {
                                case ((int)ExternalPluginControls.WatchList):
                                case ((int)ExternalPluginControls.CustomList):
                                    // Confirm we are in IMDB/iTunes Trailer Details view
                                    // This will give us enough information to send to trakt
                                    bool isDetails = GUIPropertyManager.GetProperty("#OnlineVideos.state").ToLowerInvariant() == "details";
                                    string siteUtil = GUIPropertyManager.GetProperty("#OnlineVideos.selectedSiteUtil").ToLowerInvariant();
                                    if (isDetails && (siteUtil == "imdb" || siteUtil == "itmovietrailers"))
                                    {
                                        title = GUIPropertyManager.GetProperty("#OnlineVideos.Details.Title").Trim();
                                        year = GUIPropertyManager.GetProperty("#OnlineVideos.Details.Year").Trim();
                                        if (siteUtil == "imdb")
                                        {
                                            // IMDb site exposes IMDb ID, use this to get a better match on trakt
                                            // this property is new, check for null in case user hasn't updated site
                                            imdbid = GUIPropertyManager.GetProperty("#OnlineVideos.Details.IMDbId");
                                            if (imdbid == null) imdbid = string.Empty;

                                            // could be a TV Show
                                            type = GUIPropertyManager.GetProperty("#OnlineVideos.Details.Type").ToLowerInvariant();
                                        }
                                        if ((!string.IsNullOrEmpty(title) && !string.IsNullOrEmpty(year)) || imdbid.StartsWith("tt"))
                                        {
                                            if (message.SenderControlId == (int)ExternalPluginControls.WatchList) validWatchListItem = true;
                                            if (message.SenderControlId == (int)ExternalPluginControls.CustomList) validCustomListItem = true;
                                        }
                                        // Return focus to details list now so we dont go in a loop
                                        GUIControl.FocusControl((int)ExternalPluginWindows.OnlineVideos, 51);
                                    }
                                    break;
                            }
                            #endregion
                            break;
                        case (int)ExternalPluginWindows.Showtimes:
                            #region WatchList/CustomList Button
                            switch (message.SenderControlId)
                            {
                                case ((int)ExternalPluginControls.WatchList):
                                case ((int)ExternalPluginControls.CustomList):
                                    // Confirm we are in Showtimes Details view
                                    // This will give us enough information to send to trakt
                                    bool isDetails = GUIWindowManager.GetWindow(GUIWindowManager.ActiveWindow).GetControl(24).Visible;
                                    if (isDetails)
                                    {
                                        title = GUIPropertyManager.GetProperty("#st_title").Trim();
                                        DateTime releaseDate = DateTime.MinValue;

                                        if (DateTime.TryParse(GUIPropertyManager.GetProperty("#st_releasedate").Trim(), out releaseDate))
                                        {
                                            year = releaseDate.Year.ToString();
                                        }
                                                                               
                                        imdbid = GUIPropertyManager.GetProperty("#st_imdb");
                                        if (imdbid == null) imdbid = string.Empty;

                                        tmdbid = GUIPropertyManager.GetProperty("#st_tmdb");
                                        if (tmdbid == null) imdbid = string.Empty;

                                        if ((!string.IsNullOrEmpty(title) && !string.IsNullOrEmpty(year)) || imdbid.StartsWith("tt") || !string.IsNullOrEmpty(tmdbid))
                                        {
                                            if (message.SenderControlId == (int)ExternalPluginControls.WatchList) validWatchListItem = true;
                                            if (message.SenderControlId == (int)ExternalPluginControls.CustomList) validCustomListItem = true;
                                        }
                                        // set focus to next button so we dont go in a loop
                                        GUIControl.FocusControl((int)ExternalPluginWindows.Showtimes, 42);
                                    }
                                    break;
                            }
                            #endregion
                            break;
                        case (int)ExternalPluginWindows.VideoInfo:
                            #region Watchlist/CustomList/Rate/Shouts/RelatedItem/SearchBy
                            switch (message.SenderControlId)
                            {
                                case ((int)ExternalPluginControls.WatchList):
                                case ((int)ExternalPluginControls.CustomList):
                                case ((int)ExternalPluginControls.Rate):
                                case ((int)ExternalPluginControls.Shouts):
                                case ((int)ExternalPluginControls.RelatedItems):
                                case ((int)ExternalPluginControls.SearchBy):
                                case ((int)ExternalPluginControls.TraktMenu):
                                    type = "movie";
                                    title = GUIPropertyManager.GetProperty("#title").Trim();
                                    year = GUIPropertyManager.GetProperty("#year").Trim();
                                    imdbid = GUIPropertyManager.GetProperty("#imdbnumber").Trim();
                                  
                                    MediaPortal.Util.FanArt.GetFanArtfilename(title, 0, out fanart);
                                    if (fanart.ToLowerInvariant().Equals("unknown"))
                                    {
                                        string movieid = GUIPropertyManager.GetProperty("#movieid").Trim();
                                        MediaPortal.Util.FanArt.GetFanArtfilename(movieid, 0, out fanart);
                                    }
                                    
                                    searchPeople = new SearchPeople();
                                    string people = GUIPropertyManager.GetProperty("#cast").Trim();
                                    if (people != string.Empty && people != "unknown")
                                    {
                                        // actors seperated by newlines
                                        var peopleAndRoles = people.Split('\n').Select(s => s.Trim());

                                        // each actor string also includes the role: {0} as {1} &#10;
                                        // get the seperator from the localised string and then reverse the formatted string
                                        string roleSepString = GUILocalizeStrings.Get(1320).Split(' ')[1].Trim();

                                        foreach (var personAndRole in peopleAndRoles)
                                        {
                                            var personAndRoleStrings = personAndRole.Split(new string[] { string.Format(" {0} ", roleSepString) }, StringSplitOptions.None);
                                            searchPeople.Actors.Add(personAndRoleStrings.First());
                                        }
                                    }

                                    people = GUIPropertyManager.GetProperty("#director").Trim();
                                    if (people != string.Empty && people != "unknown") searchPeople.Directors.AddRange(people.Split(',').Select(s => s.Trim()));

                                    people = GUIPropertyManager.GetProperty("#credits").Trim();
                                    if (people != string.Empty && people != "unknown")
                                    {
                                        var writers = people.Split(',').Select(s => s.Trim());
                                        foreach(var writer in writers)
                                        {
                                            // remove the writer type e.g. (Story), (Screenplay)
                                            searchPeople.Writers.Add(writer.Split('(').First().Trim());
                                        }
                                    }

                                    if (!string.IsNullOrEmpty(imdbid) || (!string.IsNullOrEmpty(title) && !string.IsNullOrEmpty(year)))
                                    {
                                        if (message.SenderControlId == (int)ExternalPluginControls.WatchList) validWatchListItem = true;
                                        if (message.SenderControlId == (int)ExternalPluginControls.CustomList) validCustomListItem = true;
                                        if (message.SenderControlId == (int)ExternalPluginControls.Rate) validRateItem = true;
                                        if (message.SenderControlId == (int)ExternalPluginControls.Shouts) validShoutItem = true;
                                        if (message.SenderControlId == (int)ExternalPluginControls.RelatedItems) validRelatedItem = true;
                                        if (message.SenderControlId == (int)ExternalPluginControls.TraktMenu) validTraktMenuItem = true;
                                    }

                                    // Set focus to Play Button now so we dont go in a loop
                                    GUIControl.FocusControl((int)ExternalPluginWindows.VideoInfo, 2);
                                    break;
                            }
                            #endregion
                            break;
                        case (int)ExternalPluginWindows.MovingPictures:
                            #region WatchList/CustomList/Rate/Shouts/RelatedItem/Search
                            switch (message.SenderControlId)
                            {
                                case ((int)ExternalPluginControls.WatchList):
                                case ((int)ExternalPluginControls.CustomList):
                                case ((int)ExternalPluginControls.Rate):
                                case ((int)ExternalPluginControls.Shouts):
                                case ((int)ExternalPluginControls.RelatedItems):
                                case ((int)ExternalPluginControls.SearchBy):
                                case ((int)ExternalPluginControls.TraktMenu):
                                    type = "movie";
                                    updatePluginFilters = true;
                                    title = GUIPropertyManager.GetProperty("#MovingPictures.SelectedMovie.title").Trim();
                                    year = GUIPropertyManager.GetProperty("#MovingPictures.SelectedMovie.year").Trim();
                                    imdbid = GUIPropertyManager.GetProperty("#MovingPictures.SelectedMovie.imdb_id").Trim();
                                    fanart = GUIPropertyManager.GetProperty("#MovingPictures.SelectedMovie.backdropfullpath").Trim();
                                    isWatched = GUIPropertyManager.GetProperty("#MovingPictures.UserMovieSettings.watched").Trim() != "0";

                                    // get movie people from database
                                    searchPeople = new SearchPeople();
                                    if (TraktHelper.IsMovingPicturesAvailableAndEnabled)
                                    {
                                        int? movieID = null;
                                        int iYear = 0; int.TryParse(year, out iYear);
                                        if (MovingPictures.FindMovieID(title, iYear, imdbid, ref movieID))
                                            MovingPictures.GetMoviePersonInfo(movieID, out searchPeople);
                                    }
                                    
                                    if (!string.IsNullOrEmpty(imdbid) || (!string.IsNullOrEmpty(title) && !string.IsNullOrEmpty(year)))
                                    {
                                        if (message.SenderControlId == (int)ExternalPluginControls.WatchList) validWatchListItem = true;
                                        if (message.SenderControlId == (int)ExternalPluginControls.CustomList) validCustomListItem = true;
                                        if (message.SenderControlId == (int)ExternalPluginControls.Rate) validRateItem = true;
                                        if (message.SenderControlId == (int)ExternalPluginControls.Shouts) validShoutItem = true;
                                        if (message.SenderControlId == (int)ExternalPluginControls.RelatedItems) validRelatedItem = true;
                                        if (message.SenderControlId == (int)ExternalPluginControls.TraktMenu) validTraktMenuItem = true;
                                        if (message.SenderControlId == (int)ExternalPluginControls.SearchBy) validSearchItem = true;
                                    }

                                    // Set focus to Play Button now so we dont go in a loop
                                    GUIControl.FocusControl((int)ExternalPluginWindows.MovingPictures, 6);
                                    break;
                            }
                            #endregion
                            break;
                        case (int)ExternalPluginWindows.TVSeries:
                            #region WatchList/CustomList/Rate/Shouts/Related
                            switch (message.SenderControlId)
                            {
                                case ((int)ExternalPluginControls.WatchList):
                                case ((int)ExternalPluginControls.CustomList):
                                case ((int)ExternalPluginControls.Rate):
                                case ((int)ExternalPluginControls.Shouts):
                                case ((int)ExternalPluginControls.RelatedItems):
                                case ((int)ExternalPluginControls.SearchBy):
                                case ((int)ExternalPluginControls.TraktMenu):
                                    Object obj = TVSeries.SelectedObject;
                                    bool validItem = false;
                                    if (obj != null)
                                    {
                                        searchPeople = new SearchPeople();

                                        switch (TVSeries.GetSelectedType(obj))
                                        {
                                            case TVSeries.SelectedType.Episode:
                                                type = "episode";
                                                validItem = TVSeries.GetEpisodeInfo(obj, out title, out year, out showtvdbid, out epTvdbId, out season, out episode, out isWatched);
                                                validItem |= TVSeries.GetEpisodePersonInfo(obj, out searchPeople);
                                                break;

                                            case TVSeries.SelectedType.Series:
                                                type = "series";
                                                validItem =  TVSeries.GetSeriesInfo(obj, out title, out year, out showtvdbid);
                                                validItem |= TVSeries.GetSeriesPersonInfo(obj, out searchPeople);
                                                break;

                                            default:
                                                break;
                                        }

                                        fanart = GUIPropertyManager.GetProperty("#TVSeries.Current.Fanart").Trim();

                                        if (validItem)
                                        {
                                            if (message.SenderControlId == (int)ExternalPluginControls.WatchList) validWatchListItem = true;
                                            if (message.SenderControlId == (int)ExternalPluginControls.CustomList) validCustomListItem = true;
                                            if (message.SenderControlId == (int)ExternalPluginControls.Rate) validRateItem = true;
                                            if (message.SenderControlId == (int)ExternalPluginControls.Shouts) validShoutItem = true;
                                            if (message.SenderControlId == (int)ExternalPluginControls.RelatedItems) validRelatedItem = true;
                                            if (message.SenderControlId == (int)ExternalPluginControls.SearchBy) validSearchItem = true;
                                            if (message.SenderControlId == (int)ExternalPluginControls.TraktMenu) validTraktMenuItem = true;
                                        }
                                    }

                                    // Set focus to Facade now so we dont go in a loop
                                    GUIControl.FocusControl((int)ExternalPluginWindows.TVSeries, 50);
                                    break;
                            }
                            #endregion                            
                            break;
                    }
                    break;
                
                default:
                    break;
            }

            #region Add To Watch List
            if (validWatchListItem)
            {
                if (type == "movie")
                {
                    if (GUIUtils.ShowYesNoDialog(Translation.WatchList, string.Format("{0}\n{1} ({2})", Translation.AddThisItemToWatchList, title, year), true))
                    {
                        TraktLogger.Info("Adding movie to Watchlist. Title = '{0}', Year = '{1}', IMDb ID = '{2}'", title, year, imdbid);
                        TraktHelper.AddMovieToWatchList(title, year.ToNullableInt32(), imdbid.ToNullIfEmpty(), tmdbid.ToNullableInt32(), updatePluginFilters);
                    }
                }
                else if (type == "show")
                {
                    if (GUIUtils.ShowYesNoDialog(Translation.WatchList, Translation.AddShowToWatchList, true))
                    {
                        TraktLogger.Info("Adding show to Watchlist. Title = '{0}', Year = '{1}', TVDb ID = '{2}'", title, year, showtvdbid);
                        TraktHelper.AddShowToWatchList(title, year.ToNullableInt32(), showtvdbid.ToNullableInt32(), imdbid.ToNullIfEmpty(), tmdbid.ToNullableInt32(), null);
                    }
                }
                else if (type == "episode")
                {
                    if (GUIUtils.ShowYesNoDialog(Translation.WatchList, Translation.AddEpisodeToWatchList, true))
                    {
                        TraktLogger.Info("Adding episode to Watchlist. Title = '{0}', Year = '{1}', Season = '{2}', Episode = '{3}', Episode TVDb ID = '{4}'", title, year, season, episode, epTvdbId);
                        TraktHelper.AddEpisodeToWatchList(null, season.ToInt(), episode.ToInt(), epTvdbId.ToNullableInt32(), null, null, null);
                    }
                }
            }
            #endregion

            #region Add To Custom List
            if (validCustomListItem)
            {
                if (type == "movie")
                {
                    TraktLogger.Info("Adding movie to Custom List. Title = '{0}', Year = '{1}', IMDb ID = '{2}'", title, year, imdbid);
                    TraktHelper.AddRemoveMovieInUserList(title, year, imdbid, false);
                }
                else if (type == "show")
                {
                    TraktLogger.Info("Adding show to Custom List. Title = '{0}', Year = '{1}', TVDb ID = '{2}'", title, year, showtvdbid);
                    TraktHelper.AddRemoveShowInUserList(title, year, showtvdbid, false);
                }
                else if (type == "episode")
                {
                    TraktLogger.Info("Adding episode to Custom List. Title = '{0}', Year = '{1}', Season = '{2}', Episode = '{3}', Episode TVDb ID = '{4}'", title, year, season, episode, epTvdbId);
                    TraktHelper.AddRemoveEpisodeInUserList(new TraktEpisode
                    {
                        Ids = new TraktEpisodeId
                        {
                            TvdbId = epTvdbId.ToNullableInt32()
                        },
                        Number = episode.ToInt(),
                        Season = season.ToInt()
                    }, false);
                }
            }
            #endregion

            #region Rate
            if (validRateItem)
            {
                if (!GUICommon.CheckLogin(false)) return;

                switch (type)
                {
                    case "movie":
                        TraktLogger.Info("Showing rate dialog for movie. Title = '{0}', Year = '{1}', IMDb ID = '{2}'", title, year, imdbid);
                        GUIUtils.ShowRateDialog<TraktSyncMovieRated>(new TraktSyncMovieRated
                        {
                            Ids = new TraktMovieId { ImdbId = imdbid.ToNullIfEmpty(), TmdbId = tmdbid.ToNullableInt32() },
                            Title = title,
                            Year = year.ToNullableInt32()
                        });
                        break;

                    case "series":
                        TraktLogger.Info("Showing rate dialog for tv show. Title = '{0}', Year = '{1}', TVDb ID = '{2}'", title, year, showtvdbid);
                        GUIUtils.ShowRateDialog<TraktSyncShowRated>(new TraktSyncShowRated
                        {
                            Ids = new TraktShowId { TvdbId = showtvdbid.ToNullableInt32(), ImdbId = imdbid.ToNullIfEmpty() },
                            Title = title,
                            Year = year.ToNullableInt32()
                        });
                        break;

                    case "episode":
                        TraktLogger.Info("Showing rate dialog for tv episode. Title = '{0}', Year = '{1}', Season = '{2}', Episode = '{3}', Episode TVDb ID = '{4}'", title, year, season, episode, epTvdbId);
                        GUIUtils.ShowRateDialog<TraktSyncEpisodeRated>(new TraktSyncEpisodeRated
                        {
                            Ids = new TraktEpisodeId { TvdbId = showtvdbid.ToNullableInt32() },
                            Number = episode.ToInt(),
                            Season = season.ToInt(),
                            RatedAt = DateTime.UtcNow.ToISO8601()
                        });
                        break;
                }
            }
            #endregion

            #region Shouts
            if (validShoutItem)
            {
                if (!GUICommon.CheckLogin(false)) return;

                // Initialize Shout window
                switch (type)
                {
                    #region movie
                    case "movie":
                        TraktLogger.Info("Displaying Shouts for {0}. Title = '{0}', Year = '{1}', IMDb ID = '{2}'", title, year, imdbid);
                        TraktHelper.ShowMovieShouts(imdbid, title, year, isWatched, fanart);
                        break;
                    #endregion
                    #region episode
                    case "episode":
                        TraktLogger.Info("Displaying Shouts for {0}. Title = '{0}', Year = '{1}', TVDb ID = '{2}'", title, year, showtvdbid);
                        TraktHelper.ShowEpisodeShouts(title, showtvdbid, season, episode, isWatched, fanart);
                        break;
                    #endregion
                    #region series
                    case "series":
                        TraktLogger.Info("Displaying Shouts for {0}. Title = '{0}', Year = '{1}', TVDb ID = '{2}'", title, year, showtvdbid);
                        TraktHelper.ShowTVShowShouts(title, showtvdbid.ToNullableInt32(), null, isWatched, fanart);
                        break;
                    #endregion
                }
            }
            #endregion

            #region Related Movies/Shows
            if (validRelatedItem)
            {
                // Initialize Shout window
                switch (type)
                {
                    #region movie
                    case "movie":
                        TraktLogger.Info("Displaying Related Movies for {0}. Title = '{0}', Year = '{1}', IMDb ID = '{2}'", title, year, imdbid);
                        TraktHelper.ShowRelatedMovies(imdbid, title, year);
                        break;
                    #endregion
                    #region series
                    case "series":
                        TraktLogger.Info("Displaying Related Shows for {0}. Title = '{0}', Year = '{1}', TVDb ID = '{2}'", title, year, showtvdbid);
                        TraktHelper.ShowRelatedShows(showtvdbid, title);
                        break;
                    #endregion
                }
            }
            #endregion

            #region Trakt Menu
            if (validTraktMenuItem)
            {
                if (!GUICommon.CheckLogin(false)) return;

                switch (type)
                {
                    case "movie":
                        GUICommon.ShowTraktExtMovieMenu(title, year, imdbid, isWatched, fanart, searchPeople, false);
                        break;

                    case "series":
                        GUICommon.ShowTraktExtTVShowMenu(title, year, showtvdbid, fanart, searchPeople, false);
                        break;

                    case "episode":
                        GUICommon.ShowTraktExtEpisodeMenu(title, year, season, episode, showtvdbid, isWatched, fanart, searchPeople, false);
                        break;
                }
            }
            #endregion

            #region Search Menu
            if (validSearchItem)
            {
                if (searchPeople.Count == 0)
                {
                    GUIUtils.ShowOKDialog(Translation.SearchBy, Translation.NoPeopleToSearch);
                }
                else
                {
                    GUICommon.ShowSearchByMenu(searchPeople, title, fanart);
                }
            }
            #endregion
        }

        #endregion

        #region Scrobbling Methods
        /// <summary>
        /// Begins searching our supported plugins libraries to scrobble
        /// </summary>
        /// <param name="filename">The video to search for</param>
        private void StartScrobble(String filename)
        {
            Thread scrobbleThread = new Thread(delegate()
            {
                if (TraktSettings.AccountStatus != ConnectionState.Connected) return;

                StopScrobble();

                // Workaround for any Race Conditions from custom event handled plugin handlers
                // Sleep to ensure that custom event gets fired first e.g. OnlineVideos VideoTracking Info.
                Thread.Sleep(500);

                if (!TraktSettings.BlockedFilenames.Contains(filename) && !TraktSettings.BlockedFolders.Any(f => filename.ToLowerInvariant().Contains(f.ToLowerInvariant())))
                {
                    TraktLogger.Debug("Searching through enabled plugin handlers for now playing media. Filename = '{0}'", filename);
                    foreach (ITraktHandler traktHandler in TraktHandlers)
                    {
                        try
                        {
                            if (traktHandler.Scrobble(filename))
                            {
                                TraktLogger.Debug("File was recognised by an enabled plugin handler and is now scrobbling. Plugin = '{0}'", traktHandler.Name);
                                return;
                            }
                        }
                        catch
                        {
                            TraktLogger.Error("Error starting scrobble from plugin handler. Plugin = '{0}'", traktHandler.Name);
                        }
                    }
                    TraktLogger.Info("File was not recognised in any enabled plugin handlers, no scrobble will occur");
                }
                else
                    TraktLogger.Info("Filename was blocked by user, no scrobble will occur");
            })
            {
                IsBackground = true,
                Name = "Scrobble"
            };
            scrobbleThread.Start();
        }

        /// <summary>
        /// Stops all scrobbling
        /// </summary>
        private void StopScrobble()
        {
            if (TraktSettings.AccountStatus != ConnectionState.Connected) return;

            // user could change handlers during sync from Settings so assign new list
            List<ITraktHandler> traktHandlers = new List<ITraktHandler>(TraktHandlers);
            foreach (ITraktHandler traktHandler in traktHandlers)
                traktHandler.StopScrobble();
        }
        #endregion
    }
}
