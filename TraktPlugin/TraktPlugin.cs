using System;
using System.Windows.Forms;
using System.IO;
using MediaPortal;
using MediaPortal.Configuration;
using MediaPortal.GUI.Library;
using MediaPortal.Player;
using Action = MediaPortal.GUI.Library.Action;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using TraktPlugin.GUI;
using TraktPlugin.TraktAPI;
using TraktPlugin.TraktHandlers;
using TraktPlugin.TraktAPI.DataStructures;

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
            TraktLogger.Info("Starting Trakt v{0}", System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString());
            TraktSettings.loadSettings();

            // check connection
            if (TraktSettings.AccountStatus == ConnectionState.Connected)
            {
                TraktLogger.Info("User {0} signed into trakt.", TraktSettings.Username);
            }
            else
                TraktLogger.Info("Username and/or Password is not set or is Invalid!");

            LoadPluginHandlers();

            #region Sync
            if (TraktHandlers.Count == 0)
            {
                TraktLogger.Info("No Plugin Handlers configured!");
            }
            else
            {
                Timer syncLibraryTimer = new Timer();
                syncLibraryTimer.Tick += new EventHandler(delegate(object o, EventArgs e)
                {
                    System.Threading.Thread.Sleep(60);
                    syncLibraryTimer.Interval = TraktSettings.SyncTimerLength;
                    SyncLibrary();
                });
                syncLibraryTimer.Enabled = true;
                syncLibraryTimer.Start();
            }
            #endregion

            TraktLogger.Debug("Adding Mediaportal Hooks");
            g_Player.PlayBackChanged += new g_Player.ChangedHandler(g_Player_PlayBackChanged);
            g_Player.PlayBackEnded += new g_Player.EndedHandler(g_Player_PlayBackEnded);
            g_Player.PlayBackStarted += new g_Player.StartedHandler(g_Player_PlayBackStarted);
            g_Player.PlayBackStopped += new g_Player.StoppedHandler(g_Player_PlayBackStopped);

            // Initialize translations
            Translation.Init();

            // Initialize skin settings
            TraktSkinSettings.Init();

            // Listen to this event to detect skin\language changes in GUI
            GUIWindowManager.OnDeActivateWindow += new GUIWindowManager.WindowActivationHandler(GUIWindowManager_OnDeActivateWindow);
            GUIWindowManager.OnActivateWindow += new GUIWindowManager.WindowActivationHandler(GUIWindowManager_OnActivateWindow);
            GUIWindowManager.OnNewAction += new OnActionHandler(GUIWindowManager_OnNewAction);            
            
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

        protected override void OnClicked(int controlId, GUIControl control, Action.ActionType actionType)
        {
            base.OnClicked(controlId, control, actionType);

            switch (controlId)
            {
                default:
                    break;
            }
        }

        #endregion

        #region Plugin Handlers

        private void LoadPluginHandlers()
        {
            TraktLogger.Debug("Loading Plugin Handlers");
            string errorMessage = "Tried to load {0} but failed, check minimum requirements are met!";
            
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
                    TraktHandlers.Add(new MyFilms(TraktSettings.MyFilms));
                else if (handlerExists && TraktSettings.MyFilms == -1)
                    TraktHandlers.RemoveAll(p => p.Name == "My Films");
            }
            catch (Exception)
            {
                TraktLogger.Error(errorMessage, "My Films");
            }
            #endregion
            
            TraktLogger.Debug("Sorting Plugin Handlers by Priority");
            TraktHandlers.Sort(delegate(ITraktHandler t1, ITraktHandler t2) { return t1.Priority.CompareTo(t2.Priority); });
        }

        #endregion

        #region LibraryFunctions

        /// <summary>
        /// Sets up and starts Syncing of Libraries
        /// </summary>
        private void SyncLibrary()
        {
            if (TraktSettings.AccountStatus != ConnectionState.Connected) return;

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
            // User could change handlers during sync from Settings so assign new list
            List<ITraktHandler> traktHandlers = new List<ITraktHandler>(TraktHandlers);
            foreach (ITraktHandler traktHandler in traktHandlers)
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
                if (TraktSettings.AccountStatus == ConnectionState.Connected) StartScrobble(filename);
            }
        }

        private void g_Player_PlayBackChanged(g_Player.MediaType type, int stoptime, string filename)
        {
            if (type == g_Player.MediaType.Video)
            {
                if (TraktSettings.AccountStatus == ConnectionState.Connected) StartScrobble(filename);
            }
        }

        private void g_Player_PlayBackStopped(g_Player.MediaType type, int stoptime, string filename)
        {
            if (type == g_Player.MediaType.Video)
            {
                if (TraktSettings.AccountStatus == ConnectionState.Connected) StopScrobble();
            }
        }
        
        private void g_Player_PlayBackEnded(g_Player.MediaType type, string filename)
        {
            if (type == g_Player.MediaType.Video)
            {
                if (TraktSettings.AccountStatus == ConnectionState.Connected) StopScrobble();
            }
        }
        
        #endregion

        #region MediaPortal Window Hooks

        int PreviousWindow = 0;
        void GUIWindowManager_OnDeActivateWindow(int windowID)
        {
            // Settings/General window
            // this is where a user can change skins\languages from GUI
            if (windowID == (int)Window.WINDOW_SETTINGS_SKIN)
            {
                // did skin change?
                if (TraktSkinSettings.CurrentSkin != TraktSkinSettings.PreviousSkin)
                {
                    TraktLogger.Info("Skin Change detected in GUI, reloading skin settings");
                    TraktSkinSettings.Init();
                }

                //did language change?
                if (Translation.CurrentLanguage != Translation.PreviousLanguage)
                {
                    TraktLogger.Info("Language Changed to '{0}' from GUI, initializing translations.", Translation.CurrentLanguage);
                    Translation.Init();
                }
            }

            PreviousWindow = windowID;
        }

        bool ConnectionChecked = false;
        bool FriendRequestsChecked = false;
        void GUIWindowManager_OnActivateWindow(int windowID)
        {
            #region Connection Check
            // We can Notify in GUI now that its initialized
            // only need this if previous connection attempt was unauthorized on Init()
            if (TraktSettings.AccountStatus == ConnectionState.Invalid && !ConnectionChecked)
            {
                System.Threading.Thread checkStatus = new System.Threading.Thread(delegate()
                {
                    TraktSettings.AccountStatus = ConnectionState.Pending;
                    // Re-Check and Notify
                    if (TraktSettings.AccountStatus == ConnectionState.Invalid)
                        GUIUtils.ShowNotifyDialog(Translation.Error, Translation.UnAuthorized, 20);
                    ConnectionChecked = true;
                })
                {
                    IsBackground = true,
                    Name = "Check Connection"
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
                LoadPluginHandlers();

                // Help user get started if no plugins enabled
                if (TraktHandlers.Count == 0)
                {
                    if (GUIUtils.ShowYesNoDialog(Translation.Plugins, Translation.NoPluginsEnabled, true))
                    {
                        GUIWindowManager.ActivateWindow((int)TraktGUIWindows.SettingsPlugins);
                    }
                    return;
                }
            }
            #endregion

            #region Friend Requests Check
            if (TraktSettings.AccountStatus == ConnectionState.Connected && TraktSettings.GetFriendRequestsOnStartup && !FriendRequestsChecked)
            {
                FriendRequestsChecked = true;
                System.Threading.Thread friendsThread = new System.Threading.Thread(delegate(object obj)
                {
                    var friendRequests = GUITraktFriends.FriendRequests;
                    TraktLogger.Info("Friend requests: {0}", friendRequests.Count().ToString());
                    if (friendRequests.Count() > 0)
                    {
                        GUIUtils.ShowNotifyDialog(Translation.FriendRequest, string.Format(Translation.FriendRequestMessage, friendRequests.Count().ToString()), 20);
                    }
                })
                {
                    IsBackground = true,
                    Name = "Getting Friend Requests"
                };

                friendsThread.Start();
            }
            #endregion

        }

        void GUIWindowManager_OnNewAction(Action action)
        {
            bool validWatchListItem = false;
            bool validRateItem = false;
            bool validShoutItem = false;
            string title = string.Empty;
            string year = string.Empty;
            string imdb = string.Empty;
            string tvdb = string.Empty;
            string season = string.Empty;
            string episode = string.Empty;
            string fanart = string.Empty;
            string type = "movie";

            GUIWindow currentWindow = GUIWindowManager.GetWindow(GUIWindowManager.ActiveWindow);
            GUIControl currentButton = null;

            switch (action.wID)
            {
                case Action.ActionType.ACTION_MOUSE_CLICK:
                case Action.ActionType.ACTION_KEY_PRESSED:
                    switch (GUIWindowManager.ActiveWindow)
                    {
                        case (int)ExternalPluginWindows.OnlineVideos:
                            #region Watch List Button
                            currentButton = currentWindow.GetControl((int)ExternalPluginControls.WatchList);
                            if (currentButton != null && currentButton.IsFocused)
                            {
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
                                        imdb = GUIPropertyManager.GetProperty("#OnlineVideos.Details.IMDbId");
                                        if (imdb == null) imdb = string.Empty;

                                        // could be a TV Show
                                        type = GUIPropertyManager.GetProperty("#OnlineVideos.Details.Type").ToLowerInvariant();
                                    }
                                    if ((!string.IsNullOrEmpty(title) && !string.IsNullOrEmpty(year)) || imdb.StartsWith("tt"))
                                        validWatchListItem = true;

                                    // Return focus to details list now so we dont go in a loop
                                    GUIControl.FocusControl((int)ExternalPluginWindows.OnlineVideos, 51);
                                }
                            }
                            #endregion
                            break;

                        case (int)ExternalPluginWindows.VideoInfo:
                            #region Rate Button
                            currentButton = currentWindow.GetControl((int)ExternalPluginControls.Rate);
                            if (currentButton != null && currentButton.IsFocused)
                            {
                                type = "movie";
                                title = GUIPropertyManager.GetProperty("#title").Trim();
                                year = GUIPropertyManager.GetProperty("#year").Trim();
                                imdb = GUIPropertyManager.GetProperty("#imdbnumber").Trim();

                                if (!string.IsNullOrEmpty(imdb) || (!string.IsNullOrEmpty(title) && !string.IsNullOrEmpty(year)))
                                    validRateItem = true;

                                // Set focus to Play Button now so we dont go in a loop
                                GUIControl.FocusControl((int)ExternalPluginWindows.VideoInfo, 2);
                            }
                            #endregion
                            #region Shouts Button
                            currentButton = currentWindow.GetControl((int)ExternalPluginControls.Shouts);
                            if (currentButton != null && currentButton.IsFocused)
                            {
                                type = "movie";
                                title = GUIPropertyManager.GetProperty("#title").Trim();
                                year = GUIPropertyManager.GetProperty("#year").Trim();
                                imdb = GUIPropertyManager.GetProperty("#imdbnumber").Trim();
                                #if !MP12
                                fanart = string.Empty;
                                #else
                                fanart = string.Empty;
                                MediaPortal.Util.FanArt.GetFanArtfilename(title, 0, out fanart);
                                #endif

                                if (!string.IsNullOrEmpty(imdb) || (!string.IsNullOrEmpty(title) && !string.IsNullOrEmpty(year)))
                                    validShoutItem = true;
                            }
                            #endregion
                            break;

                        case (int)ExternalPluginWindows.MovingPictures:
                            #region Rate Button
                            currentButton = currentWindow.GetControl((int)ExternalPluginControls.Rate);
                            if (currentButton != null && currentButton.IsFocused)
                            {
                                type = "movie";
                                title = GUIPropertyManager.GetProperty("#MovingPictures.SelectedMovie.title").Trim();
                                year = GUIPropertyManager.GetProperty("#MovingPictures.SelectedMovie.year").Trim();
                                imdb = GUIPropertyManager.GetProperty("#MovingPictures.SelectedMovie.imdb_id").Trim();

                                if (!string.IsNullOrEmpty(imdb) || (!string.IsNullOrEmpty(title) && !string.IsNullOrEmpty(year)))
                                    validRateItem = true;

                                // Set focus to Play Button now so we dont go in a loop
                                GUIControl.FocusControl((int)ExternalPluginWindows.MovingPictures, 6);
                            }
                            #endregion
                            #region Shouts Button
                            currentButton = currentWindow.GetControl((int)ExternalPluginControls.Shouts);
                            if (currentButton != null && currentButton.IsFocused)
                            {
                                type = "movie";
                                title = GUIPropertyManager.GetProperty("#MovingPictures.SelectedMovie.title").Trim();
                                year = GUIPropertyManager.GetProperty("#MovingPictures.SelectedMovie.year").Trim();
                                imdb = GUIPropertyManager.GetProperty("#MovingPictures.SelectedMovie.imdb_id").Trim();
                                fanart = GUIPropertyManager.GetProperty("#MovingPictures.SelectedMovie.backdropfullpath").Trim();

                                if (!string.IsNullOrEmpty(imdb) || (!string.IsNullOrEmpty(title) && !string.IsNullOrEmpty(year)))
                                    validShoutItem = true;
                            }
                            #endregion
                            break;

                        case (int)ExternalPluginWindows.TVSeries:
                            #region Rate Button
                            currentButton = currentWindow.GetControl((int)ExternalPluginControls.Rate);
                            if (currentButton != null && currentButton.IsFocused)
                            {
                                Object obj = TVSeries.SelectedObject;
                                if (obj != null)
                                {
                                    switch (TVSeries.GetSelectedType(obj))
                                    {
                                        case TVSeries.SelectedType.Episode:
                                            type = "episode";
                                            validRateItem = TVSeries.GetEpisodeInfo(obj, out title, out tvdb, out season, out episode);
                                            break;

                                        case TVSeries.SelectedType.Series:
                                            type = "series";
                                            validRateItem = TVSeries.GetSeriesInfo(obj, out title, out tvdb);
                                            break;

                                        default:
                                            break;
                                    }
                                    // Set focus to Facade now so we dont go in a loop
                                    GUIControl.FocusControl((int)ExternalPluginWindows.TVSeries, 50);
                                }
                            }
                            #endregion
                            #region Shouts Button
                            currentButton = currentWindow.GetControl((int)ExternalPluginControls.Shouts);
                            if (currentButton != null && currentButton.IsFocused)
                            {
                                Object obj = TVSeries.SelectedObject;
                                if (obj != null)
                                {
                                    switch (TVSeries.GetSelectedType(obj))
                                    {
                                        case TVSeries.SelectedType.Episode:
                                            type = "episode";
                                            validShoutItem = TVSeries.GetEpisodeInfo(obj, out title, out tvdb, out season, out episode);
                                            break;

                                        case TVSeries.SelectedType.Series:
                                            type = "series";
                                            validShoutItem = TVSeries.GetSeriesInfo(obj, out title, out tvdb);
                                            break;

                                        default:
                                            break;
                                    }
                                    fanart = GUIPropertyManager.GetProperty("#TVSeries.Current.Fanart").Trim();
                                }
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
                if (GUIUtils.ShowYesNoDialog(Translation.WatchList, string.Format("{0}\n{1} ({2})", Translation.AddThisItemToWatchList, title, year), true))
                {
                    TraktLogger.Info("Adding {0} '{1} ({2}) [{3}]' to Watch List", type, title, year, imdb);

                    System.Threading.Thread syncThread = new System.Threading.Thread(delegate(object obj)
                    {
                        if (type == "movie")
                        {
                            TraktAPI.TraktAPI.SyncMovieLibrary(BasicHandler.CreateMovieSyncData(title, year, imdb), TraktSyncModes.watchlist);
                            GUIWatchListMovies.ClearCache(TraktSettings.Username);
                        }
                        else
                        {
                            TraktAPI.TraktAPI.SyncShowWatchList(BasicHandler.CreateShowSyncData(title, year, imdb), TraktSyncModes.watchlist);
                            GUIWatchListShows.ClearCache(TraktSettings.Username);
                        }
                    })
                    {
                        IsBackground = true,
                        Name = "Adding to Watch List"
                    };
                    syncThread.Start();
                }
            }
            #endregion

            #region Rate
            if (validRateItem)
            {
                switch (type)
                {
                    case "movie":
                        TraktLogger.Info("Rating {0} '{1} ({2}) [{3}]'", type, title, year, imdb);
                        GUIUtils.ShowRateDialog<TraktRateMovie>(BasicHandler.CreateMovieRateData(title, year, imdb));
                        break;

                    case "series":
                        TraktLogger.Info("Rating {0} '{1} [{2}]'", type, title, tvdb);
                        GUIUtils.ShowRateDialog<TraktRateSeries>(BasicHandler.CreateShowRateData(title, tvdb));
                        break;

                    case "episode":
                        TraktLogger.Info("Rating {0} '{1} - {2}x{3} [{4}]'", type, title, season, episode, tvdb);
                        GUIUtils.ShowRateDialog<TraktRateEpisode>(BasicHandler.CreateEpisodeRateData(title, tvdb, season, episode));
                        break;
                }
            }
            #endregion

            #region Shouts
            if (validShoutItem)
            {
                // Initialize Shout window
                switch (type)
                {
                    #region movie
                    case "movie":
                        TraktLogger.Info("Searching Shouts for {0} '{1} ({2}) [{3}]'", type, title, year, imdb);
                        MovieShout movieInfo = new MovieShout
                        {
                            IMDbId = imdb,
                            Title = title,
                            Year = year
                        };
                        GUIShouts.ShoutType = GUIShouts.ShoutTypeEnum.movie;
                        GUIShouts.MovieInfo = movieInfo;
                        GUIShouts.Fanart = fanart;
                        break;
                    #endregion
                    #region episode
                    case "episode":
                        TraktLogger.Info("Searching Shouts for {0} '{1} - {2}x{3} [{4}]'", type, title, season, episode, tvdb);
                        EpisodeShout episodeInfo = new EpisodeShout
                        {
                            TVDbId = tvdb,
                            Title = title,
                            SeasonIdx = season,
                            EpisodeIdx = episode
                        };
                        GUIShouts.ShoutType = GUIShouts.ShoutTypeEnum.episode;
                        GUIShouts.EpisodeInfo = episodeInfo;
                        GUIShouts.Fanart = fanart;
                        break;
                    #endregion
                    #region series
                    case "series":
                        TraktLogger.Info("Searching Shouts for {0} '{1} [{2}]'", type, title, tvdb);
                        ShowShout seriesInfo = new ShowShout
                        {
                            TVDbId = tvdb,
                            Title = title,
                        };
                        GUIShouts.ShoutType = GUIShouts.ShoutTypeEnum.show;
                        GUIShouts.ShowInfo = seriesInfo;
                        GUIShouts.Fanart = fanart;
                        break;
                    #endregion
                }
                // Launch Shout window
                GUIWindowManager.ActivateWindow((int)TraktGUIWindows.Shouts);
            }
            #endregion
        }

        #endregion

        #region ScrobblingMethods
        /// <summary>
        /// Begins searching our supported plugins libraries to scrobble
        /// </summary>
        /// <param name="filename">The video to search for</param>
        private void StartScrobble(String filename)
        {
            StopScrobble();

            if (!TraktSettings.BlockedFilenames.Contains(filename) && !TraktSettings.BlockedFolders.Any(f => filename.Contains(f)))
            {
                TraktLogger.Debug("Checking out Libraries for the filename: {0}", filename);
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
            // User could change handlers during sync from Settings so assign new list
            List<ITraktHandler> traktHandlers = new List<ITraktHandler>(TraktHandlers);
            TraktLogger.Debug("Making sure that we aren't still scrobbling");
            foreach (ITraktHandler traktHandler in traktHandlers)
                traktHandler.StopScrobble();
        }
        #endregion
    }
}
