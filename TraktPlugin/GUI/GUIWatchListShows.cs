using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using MediaPortal.Configuration;
using MediaPortal.GUI.Library;
using Action = MediaPortal.GUI.Library.Action;
using MediaPortal.Util;
using TraktPlugin.TraktAPI;
using TraktPlugin.TraktAPI.DataStructures;

namespace TraktPlugin.GUI
{
    public class GUIWatchListShows : GUIWindow
    {
        #region Skin Controls

        [SkinControl(2)]
        protected GUIButtonControl layoutButton = null;

        [SkinControl(50)]
        protected GUIFacadeControl Facade = null;

        [SkinControlAttribute(60)]
        protected GUIImage FanartBackground = null;

        [SkinControlAttribute(61)]
        protected GUIImage FanartBackground2 = null;

        [SkinControlAttribute(62)]
        protected GUIImage loadingImage = null;

        #endregion

        #region Enums

        enum Layout
        {
            List = 0,
            SmallIcons = 1,
            LargeIcons = 2,
            Filmstrip = 3,
        }

        enum ContextMenuItem
        {
            RemoveFromWatchList,
            AddToWatchList,
            AddToList,
            Trailers,
            Related,
            Rate,
            Shouts,
            ChangeLayout,
            SearchWithMpNZB,
            SearchTorrent
        }

        enum TrailerSite
        {
            IMDb,
            YouTube
        }

        #endregion

        #region Constructor

        public GUIWatchListShows()
        {
            backdrop = new ImageSwapper();
            backdrop.PropertyOne = "#Trakt.WatchListShows.Fanart.1";
            backdrop.PropertyTwo = "#Trakt.WatchListShows.Fanart.2";
        }

        #endregion

        #region Private Variables

        bool StopDownload { get; set; }
        private Layout CurrentLayout { get; set; }
        static int PreviousSelectedIndex { get; set; }
        private ImageSwapper backdrop;
        static DateTime LastRequest = new DateTime();
        static Dictionary<string, IEnumerable<TraktWatchListShow>> userWatchList = new Dictionary<string, IEnumerable<TraktWatchListShow>>();

        static IEnumerable<TraktWatchListShow> WatchListShows
        {
            get
            {
                if (!userWatchList.Keys.Contains(CurrentUser) || LastRequest < DateTime.UtcNow.Subtract(new TimeSpan(0, TraktSettings.WebRequestCacheMinutes, 0)))
                {
                    _WatchListShows = TraktAPI.TraktAPI.GetWatchListShows(CurrentUser);
                    if (userWatchList.Keys.Contains(CurrentUser)) userWatchList.Remove(CurrentUser);
                    userWatchList.Add(CurrentUser, _WatchListShows);
                    LastRequest = DateTime.UtcNow;
                    PreviousSelectedIndex = 0;
                }
                return userWatchList[CurrentUser];
            }
        }
        static IEnumerable<TraktWatchListShow> _WatchListShows = null;        

        #endregion

        #region Public Properties

        public static string CurrentUser { get; set; }

        #endregion

        #region Base Overrides

        public override int GetID
        {
            get
            {
                return 87268;
            }
        }

        public override bool Init()
        {
            return Load(GUIGraphicsContext.Skin + @"\Trakt.WatchList.Shows.xml");
        }

        protected override void OnPageLoad()
        {
            base.OnPageLoad();

            // Clear GUI Properties
            ClearProperties();

            // Requires Login
            if (!GUICommon.CheckLogin()) return;

            // Init Properties
            InitProperties();

            // Load WatchList Shows
            LoadWatchListShows();
        }

        protected override void OnPageDestroy(int new_windowId)
        {
            StopDownload = true;
            PreviousSelectedIndex = Facade.SelectedListItemIndex;
            ClearProperties();

            // save current layout
            TraktSettings.WatchListShowsDefaultLayout = (int)CurrentLayout;

            base.OnPageDestroy(new_windowId);
        }

        protected override void OnClicked(int controlId, GUIControl control, Action.ActionType actionType)
        {
            // wait for any background action to finish
            if (GUIBackgroundTask.Instance.IsBusy) return;

            switch (controlId)
            {
                // Facade
                case (50):
                    if (actionType == Action.ActionType.ACTION_SELECT_ITEM)
                    {
                        CheckAndPlayEpisode(true);
                    }
                    break;

                // Layout Button
                case (2):
                    ShowLayoutMenu();
                    break;

                default:
                    break;
            }
            base.OnClicked(controlId, control, actionType);
        }

        public override void OnAction(Action action)
        {
            switch (action.wID)
            {
                case Action.ActionType.ACTION_PREVIOUS_MENU:
                    // restore current user
                    CurrentUser = TraktSettings.Username;
                    base.OnAction(action);
                    break;
                case Action.ActionType.ACTION_PLAY:
                case Action.ActionType.ACTION_MUSIC_PLAY:
                    CheckAndPlayEpisode(false);
                    break;
                default:
                    base.OnAction(action);
                    break;
            }
        }

        protected override void OnShowContextMenu()
        {
            GUIListItem selectedItem = this.Facade.SelectedListItem;
            if (selectedItem == null) return;

            TraktWatchListShow selectedShow = (TraktWatchListShow)selectedItem.TVTag;

            IDialogbox dlg = (IDialogbox)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
            if (dlg == null) return;

            dlg.Reset();
            dlg.SetHeading(GUIUtils.PluginName());

            GUIListItem listItem = null;

            if (CurrentUser == TraktSettings.Username)
            {
                listItem = new GUIListItem(Translation.RemoveFromWatchList);
                dlg.Add(listItem);
                listItem.ItemId = (int)ContextMenuItem.RemoveFromWatchList;
            }
            else if (!selectedShow.InWatchList)
            {
                // viewing someone else's watch list and not in yours
                listItem = new GUIListItem(Translation.AddToWatchList);
                dlg.Add(listItem);
                listItem.ItemId = (int)ContextMenuItem.AddToWatchList;
            }

            // Add to Custom List
            listItem = new GUIListItem(Translation.AddToList + "...");
            dlg.Add(listItem);
            listItem.ItemId = (int)ContextMenuItem.AddToList;

            if (TraktHelper.IsOnlineVideosAvailableAndEnabled)
            {
                listItem = new GUIListItem(Translation.Trailers);
                dlg.Add(listItem);
                listItem.ItemId = (int)ContextMenuItem.Trailers;
            }

            // Related Shows
            listItem = new GUIListItem(Translation.RelatedShows + "...");
            dlg.Add(listItem);
            listItem.ItemId = (int)ContextMenuItem.Related;

            // Rate Show
            listItem = new GUIListItem(Translation.RateShow);
            dlg.Add(listItem);
            listItem.ItemId = (int)ContextMenuItem.Rate;

            // Shouts
            listItem = new GUIListItem(Translation.Shouts + "...");
            dlg.Add(listItem);
            listItem.ItemId = (int)ContextMenuItem.Shouts;

            // Change Layout
            listItem = new GUIListItem(Translation.ChangeLayout);
            dlg.Add(listItem);
            listItem.ItemId = (int)ContextMenuItem.ChangeLayout;

            if (TraktHelper.IsMpNZBAvailableAndEnabled)
            {
                // Search for show with mpNZB
                listItem = new GUIListItem(Translation.SearchWithMpNZB);
                dlg.Add(listItem);
                listItem.ItemId = (int)ContextMenuItem.SearchWithMpNZB;
            }

            if (TraktHelper.IsMyTorrentsAvailableAndEnabled)
            {
                // Search for show with MyTorrents
                listItem = new GUIListItem(Translation.SearchTorrent);
                dlg.Add(listItem);
                listItem.ItemId = (int)ContextMenuItem.SearchTorrent;
            }

            // Show Context Menu
            dlg.DoModal(GUIWindowManager.ActiveWindow);
            if (dlg.SelectedId < 0) return;

            switch (dlg.SelectedId)
            {
                case ((int)ContextMenuItem.AddToWatchList):
                    AddShowToWatchList(selectedShow);
                    selectedShow.InWatchList = true;
                    OnShowSelected(selectedItem, Facade);
                    selectedShow.Images.NotifyPropertyChanged("PosterImageFilename");
                    GUIWatchListShows.ClearCache(TraktSettings.Username);
                    break;

                case ((int)ContextMenuItem.RemoveFromWatchList):
                    PreviousSelectedIndex = this.Facade.SelectedListItemIndex;
                    RemoveShowFromWatchList(selectedShow);
                    if (_WatchListShows.Count() >= 1)
                    {
                        // remove from list
                        var showsToExcept = new List<TraktWatchListShow>();
                        showsToExcept.Add(selectedShow);
                        _WatchListShows = WatchListShows.Except(showsToExcept);
                        userWatchList[CurrentUser] = _WatchListShows;
                        LoadWatchListShows();
                    }
                    else
                    {
                        // no more shows left
                        ClearProperties();
                        GUIControl.ClearControl(GetID, Facade.GetID);
                        _WatchListShows = null;
                        userWatchList.Remove(CurrentUser);
                        // notify and exit
                        GUIUtils.ShowNotifyDialog(GUIUtils.PluginName(), Translation.NoShowWatchList);
                        GUIWindowManager.ShowPreviousWindow();
                        return;
                    }
                    break;

                case ((int)ContextMenuItem.AddToList):
                    TraktHelper.AddRemoveShowInUserList(selectedShow.Title, selectedShow.Year.ToString(), selectedShow.Tvdb, false);
                    break;

                case ((int)ContextMenuItem.Related):
                    RelatedShow relatedShow = new RelatedShow();
                    relatedShow.Title = selectedShow.Title;
                    relatedShow.TVDbId = selectedShow.Tvdb;
                    GUIRelatedShows.relatedShow = relatedShow;
                    GUIWindowManager.ActivateWindow((int)TraktGUIWindows.RelatedShows);
                    break;

                case ((int)ContextMenuItem.Rate):
                    GUICommon.RateShow(selectedShow);
                    OnShowSelected(selectedItem, Facade);
                    selectedShow.Images.NotifyPropertyChanged("PosterImageFilename");
                    if (CurrentUser != TraktSettings.Username) GUIWatchListShows.ClearCache(TraktSettings.Username);
                    break;

                case ((int)ContextMenuItem.Shouts):
                    GUIShouts.ShoutType = GUIShouts.ShoutTypeEnum.show;
                    GUIShouts.ShowInfo = new ShowShout { IMDbId = selectedShow.Imdb, TVDbId = selectedShow.Tvdb, Title = selectedShow.Title };
                    GUIShouts.Fanart = selectedShow.Images.FanartImageFilename;
                    GUIWindowManager.ActivateWindow((int)TraktGUIWindows.Shouts);
                    break;

                case ((int)ContextMenuItem.Trailers):
                    ShowTrailersMenu(selectedShow);
                    break;

                case ((int)ContextMenuItem.ChangeLayout):
                    ShowLayoutMenu();
                    break;

                case ((int)ContextMenuItem.SearchWithMpNZB):
                    string loadingParam = string.Format("search:{0}", selectedShow.Title);
                    GUIWindowManager.ActivateWindow((int)ExternalPluginWindows.MpNZB, loadingParam);
                    break;

                case ((int)ContextMenuItem.SearchTorrent):
                    string loadPar = selectedShow.Title;
                    GUIWindowManager.ActivateWindow((int)ExternalPluginWindows.MyTorrents, loadPar);
                    break;

                default:
                    break;
            }

            base.OnShowContextMenu();
        }

        #endregion

        #region Private Methods

        private void CheckAndPlayEpisode(bool jumpTo)
        {
            GUIListItem selectedItem = this.Facade.SelectedListItem;
            if (selectedItem == null) return;

            TraktWatchListShow selectedShow = (TraktWatchListShow)selectedItem.TVTag;
            GUICommon.CheckAndPlayFirstUnwatched(Convert.ToInt32(selectedShow.Tvdb), string.IsNullOrEmpty(selectedShow.Imdb) ? selectedShow.Title : selectedShow.Imdb, jumpTo);
        }

        private void ShowTrailersMenu(TraktWatchListShow show)
        {
            IDialogbox dlg = (IDialogbox)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
            dlg.Reset();
            dlg.SetHeading(Translation.Trailer);

            foreach (TrailerSite site in Enum.GetValues(typeof(TrailerSite)))
            {
                string menuItem = Enum.GetName(typeof(TrailerSite), site);
                GUIListItem pItem = new GUIListItem(menuItem);
                dlg.Add(pItem);
            }

            dlg.DoModal(GUIWindowManager.ActiveWindow);

            if (dlg.SelectedLabel >= 0)
            {
                string siteUtil = string.Empty;
                string searchParam = string.Empty;

                switch (dlg.SelectedLabelText)
                {
                    case ("IMDb"):
                        siteUtil = "IMDb Movie Trailers";
                        if (!string.IsNullOrEmpty(show.Imdb))
                            // Exact search
                            searchParam = show.Imdb;
                        else
                            searchParam = show.Title;
                        break;

                    case ("YouTube"):
                        siteUtil = "YouTube";
                        searchParam = show.Title;
                        break;
                }

                string loadingParam = string.Format("site:{0}|search:{1}|return:Locked", siteUtil, searchParam);
                // Launch OnlineVideos Trailer search
                GUIWindowManager.ActivateWindow((int)ExternalPluginWindows.OnlineVideos, loadingParam);
            }
        }

        private TraktShowSync CreateSyncData(TraktWatchListShow show)
        {
            if (show == null) return null;

            List<TraktShowSync.Show> shows = new List<TraktShowSync.Show>();

            TraktShowSync.Show syncShow = new TraktShowSync.Show
            {
                TVDBID = show.Tvdb,
                Title = show.Title,
                Year = show.Year
            };
            shows.Add(syncShow);

            TraktShowSync syncData = new TraktShowSync
            {
                UserName = TraktSettings.Username,
                Password = TraktSettings.Password,
                Shows = shows
            };

            return syncData;
        }

        private void AddShowToWatchList(TraktWatchListShow show)
        {
            Thread syncThread = new Thread(delegate(object obj)
            {
                TraktAPI.TraktAPI.SyncShowWatchList(CreateSyncData(obj as TraktWatchListShow), TraktSyncModes.watchlist);
            })
            {
                IsBackground = true,
                Name = "Adding Show to Watch List"
            };

            syncThread.Start(show);
        }

        private void RemoveShowFromWatchList(TraktWatchListShow show)
        {
            Thread syncThread = new Thread(delegate(object obj)
            {
                TraktAPI.TraktAPI.SyncShowWatchList(CreateSyncData(obj as TraktWatchListShow), TraktSyncModes.unwatchlist);
            })
            {
                IsBackground = true,
                Name = "Removing Show from Watch List"
            };

            syncThread.Start(show);
        }


        private void ShowLayoutMenu()
        {
            IDialogbox dlg = (IDialogbox)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
            dlg.Reset();
            dlg.SetHeading(GetLayoutTranslation(CurrentLayout));

            foreach (Layout layout in Enum.GetValues(typeof(Layout)))
            {
                string menuItem = GetLayoutTranslation(layout);
                GUIListItem pItem = new GUIListItem(menuItem);
                if (layout == CurrentLayout) pItem.Selected = true;
                dlg.Add(pItem);
            }

            dlg.DoModal(GUIWindowManager.ActiveWindow);

            if (dlg.SelectedLabel >= 0)
            {
                CurrentLayout = (Layout)dlg.SelectedLabel;
                Facade.SetCurrentLayout(Enum.GetName(typeof(Layout), CurrentLayout));
                GUIControl.SetControlLabel(GetID, layoutButton.GetID, GetLayoutTranslation(CurrentLayout));
            }
        }

        private string GetLayoutTranslation(Layout layout)
        {
            string strLine = string.Empty;
            switch (layout)
            {
                case Layout.List:
                    strLine = GUILocalizeStrings.Get(101);
                    break;
                case Layout.SmallIcons:
                    strLine = GUILocalizeStrings.Get(100);
                    break;
                case Layout.LargeIcons:
                    strLine = GUILocalizeStrings.Get(417);
                    break;
                case Layout.Filmstrip:
                    strLine = GUILocalizeStrings.Get(733);
                    break;
            }
            return strLine;
        }

        private void LoadWatchListShows()
        {
            GUIUtils.SetProperty("#Trakt.Items", string.Empty);

            GUIBackgroundTask.Instance.ExecuteInBackgroundAndCallback(() =>
            {
                return WatchListShows;
            },
            delegate(bool success, object result)
            {
                if (success)
                {
                    IEnumerable<TraktWatchListShow> shows = result as IEnumerable<TraktWatchListShow>;
                    SendWatchListShowsToFacade(shows);
                }
            }, Translation.GettingWatchListMovies, true);
        }

        private void SendWatchListShowsToFacade(IEnumerable<TraktWatchListShow> shows)
        {
            // clear facade
            GUIControl.ClearControl(GetID, Facade.GetID);

            if (shows.Count() == 0)
            {
                GUIUtils.ShowNotifyDialog(GUIUtils.PluginName(), string.Format(Translation.NoShowWatchList, CurrentUser));
                CurrentUser = TraktSettings.Username;
                GUIWindowManager.ShowPreviousWindow();
                return;
            }

            int itemId = 0;
            List<TraktShow.ShowImages> showImages = new List<TraktShow.ShowImages>();

            // Add each show
            foreach (var show in shows)
            {
                GUITraktWatchListShowListItem item = new GUITraktWatchListShowListItem(show.Title);

                item.Label2 = show.Year.ToString();
                item.TVTag = show;
                item.Item = show.Images;
                item.ItemId = Int32.MaxValue - itemId;
                item.IconImage = "defaultVideo.png";
                item.IconImageBig = "defaultVideoBig.png";
                item.ThumbnailImage = "defaultVideoBig.png";
                item.OnItemSelected += OnShowSelected;
                Utils.SetDefaultIcons(item);
                Facade.Add(item);
                itemId++;

                // add image for download
                showImages.Add(show.Images);
            }

            // Set Facade Layout
            Facade.SetCurrentLayout(Enum.GetName(typeof(Layout), CurrentLayout));
            GUIControl.FocusControl(GetID, Facade.GetID);

            if (PreviousSelectedIndex >= shows.Count())
                Facade.SelectIndex(PreviousSelectedIndex - 1);
            else
                Facade.SelectIndex(PreviousSelectedIndex);

            // set facade properties
            GUIUtils.SetProperty("#itemcount", shows.Count().ToString());
            GUIUtils.SetProperty("#Trakt.Items", string.Format("{0} {1}", shows.Count().ToString(), shows.Count() > 1 ? Translation.SeriesPlural : Translation.Series));

            // Download show images Async and set to facade
            GetImages(showImages);
        }

        private void InitProperties()
        {
            // Fanart
            backdrop.GUIImageOne = FanartBackground;
            backdrop.GUIImageTwo = FanartBackground2;
            backdrop.LoadingImage = loadingImage;

            // load Watch list for user
            if (string.IsNullOrEmpty(CurrentUser)) CurrentUser = TraktSettings.Username;
            GUICommon.SetProperty("#Trakt.WatchList.CurrentUser", CurrentUser);

            // load last layout
            CurrentLayout = (Layout)TraktSettings.WatchListShowsDefaultLayout;
            // update button label
            GUIControl.SetControlLabel(GetID, layoutButton.GetID, GetLayoutTranslation(CurrentLayout));
        }

        private void ClearProperties()
        {
            GUIUtils.SetProperty("#Trakt.Show.WatchList.Inserted", string.Empty);
            GUICommon.ClearShowProperties();
        }

        private void PublishShowSkinProperties(TraktWatchListShow show)
        {
            GUICommon.SetProperty("#Trakt.Show.WatchList.Inserted", show.Inserted.FromEpoch().ToShortDateString());
            GUICommon.SetShowProperties(show);
        }

        private void OnShowSelected(GUIListItem item, GUIControl parent)
        {
            TraktWatchListShow show = item.TVTag as TraktWatchListShow;
            PublishShowSkinProperties(show);
            GUIImageHandler.LoadFanart(backdrop, show.Images.FanartImageFilename);
        }

        private void GetImages(List<TraktShow.ShowImages> itemsWithThumbs)
        {
            StopDownload = false;

            // split the downloads in 5+ groups and do multithreaded downloading
            int groupSize = (int)Math.Max(1, Math.Floor((double)itemsWithThumbs.Count / 5));
            int groups = (int)Math.Ceiling((double)itemsWithThumbs.Count() / groupSize);

            for (int i = 0; i < groups; i++)
            {
                List<TraktShow.ShowImages> groupList = new List<TraktShow.ShowImages>();
                for (int j = groupSize * i; j < groupSize * i + (groupSize * (i + 1) > itemsWithThumbs.Count ? itemsWithThumbs.Count - groupSize * i : groupSize); j++)
                {
                    groupList.Add(itemsWithThumbs[j]);
                }

                new Thread(delegate(object o)
                {
                    List<TraktShow.ShowImages> items = (List<TraktShow.ShowImages>)o;
                    foreach (TraktShow.ShowImages item in items)
                    {
                        #region Poster
                        // stop download if we have exited window
                        if (StopDownload) break;

                        string remoteThumb = item.Poster;
                        string localThumb = item.PosterImageFilename;

                        if (!string.IsNullOrEmpty(remoteThumb) && !string.IsNullOrEmpty(localThumb))
                        {
                            if (GUIImageHandler.DownloadImage(remoteThumb, localThumb))
                            {
                                // notify that image has been downloaded
                                item.NotifyPropertyChanged("PosterImageFilename");
                            }
                        }
                        #endregion

                        #region Fanart
                        // stop download if we have exited window
                        if (StopDownload) break;
                        if (!TraktSettings.DownloadFanart) continue;

                        string remoteFanart = item.Fanart;
                        string localFanart = item.FanartImageFilename;

                        if (!string.IsNullOrEmpty(remoteFanart) && !string.IsNullOrEmpty(localFanart))
                        {
                            if (GUIImageHandler.DownloadImage(remoteFanart, localFanart))
                            {
                                // notify that image has been downloaded
                                item.NotifyPropertyChanged("FanartImageFilename");
                            }
                        }
                        #endregion
                    }
                })
                {
                    IsBackground = true,
                    Name = "Trakt Show Image Downloader " + i.ToString()
                }.Start(groupList);
            }
        }

        #endregion

        #region Public Methods

        public static void ClearCache(string username)
        {
            if (userWatchList.Keys.Contains(username)) userWatchList.Remove(username);
        }

        #endregion
    }

    public class GUITraktWatchListShowListItem : GUIListItem
    {
        public GUITraktWatchListShowListItem(string strLabel) : base(strLabel) { }

        public object Item
        {
            get { return _Item; }
            set
            {
                _Item = value;
                INotifyPropertyChanged notifier = value as INotifyPropertyChanged;
                if (notifier != null) notifier.PropertyChanged += (s, e) =>
                {
                    if (s is TraktShow.ShowImages && e.PropertyName == "PosterImageFilename")
                        SetImageToGui((s as TraktShow.ShowImages).PosterImageFilename);
                    if (s is TraktShow.ShowImages && e.PropertyName == "FanartImageFilename")
                        UpdateCurrentSelection();
                };
            }
        } protected object _Item;

        /// <summary>
        /// Loads an Image from memory into a facade item
        /// </summary>
        /// <param name="imageFilePath">Filename of image</param>
        protected void SetImageToGui(string imageFilePath)
        {
            if (string.IsNullOrEmpty(imageFilePath)) return;

            // determine the overlays to add to poster
            TraktWatchListShow show = TVTag as TraktWatchListShow;
            MainOverlayImage mainOverlay = MainOverlayImage.None;

            // only show watch list icon if viewing someone elses watch list
            if ((GUIWatchListShows.CurrentUser != TraktSettings.Username) && show.InWatchList)
                mainOverlay = MainOverlayImage.Watchlist;

            RatingOverlayImage ratingOverlay = GUIImageHandler.GetRatingOverlay(show.RatingAdvanced);

            // get a reference to a MediaPortal Texture Identifier
            string suffix = mainOverlay.ToString().Replace(", ", string.Empty) + Enum.GetName(typeof(RatingOverlayImage), ratingOverlay);
            string texture = GUIImageHandler.GetTextureIdentFromFile(imageFilePath, suffix);

            // build memory image
            Image memoryImage = null;
            if (mainOverlay != MainOverlayImage.None || ratingOverlay != RatingOverlayImage.None)
            {
                memoryImage = GUIImageHandler.DrawOverlayOnPoster(imageFilePath, mainOverlay, ratingOverlay);
                if (memoryImage == null) return;

                // load texture into facade item
                if (GUITextureManager.LoadFromMemory(memoryImage, texture, 0, 0, 0) > 0)
                {
                    ThumbnailImage = texture;
                    IconImage = texture;
                    IconImageBig = texture;
                }
            }
            else
            {
                ThumbnailImage = imageFilePath;
                IconImage = imageFilePath;
                IconImageBig = imageFilePath;
            }

            // if selected and is current window force an update of thumbnail
            UpdateCurrentSelection();
        }

        protected void UpdateCurrentSelection()
        {
            GUIWatchListShows window = GUIWindowManager.GetWindow(GUIWindowManager.ActiveWindow) as GUIWatchListShows;
            if (window != null)
            {
                GUIListItem selectedItem = GUIControl.GetSelectedListItem(87268, 50);
                if (selectedItem == this)
                {
                    GUIWindowManager.SendThreadMessage(new GUIMessage(GUIMessage.MessageType.GUI_MSG_ITEM_SELECT, GUIWindowManager.ActiveWindow, 0, 50, ItemId, 0, null));
                }
            }
        }
    }
}