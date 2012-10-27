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
    public class GUIRecommendationsShows : GUIWindow
    {
        #region Skin Controls

        [SkinControl(2)]
        protected GUIButtonControl layoutButton = null;

        [SkinControl(3)]
        protected GUIButtonControl genreButton = null;

        [SkinControl(4)]
        protected GUICheckButton hideCollectedButton = null;

        [SkinControl(5)]
        protected GUICheckButton hideWatchlistedButton = null;

        [SkinControl(6)]
        protected GUIButtonControl startYearButton = null;

        [SkinControl(7)]
        protected GUIButtonControl endYearButton = null;

        [SkinControl(8)]
        protected GUISortButtonControl sortButton = null;

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

        enum ContextMenuItem
        {
            ShowSeasonInfo,
            AddToWatchList,
            DismissRecommendation,
            RemoveFromWatchList,
            AddToList,
            Trailers,
            Related,
            Rate,
            Shouts,
            ChangeLayout,
            SearchWithMpNZB,
            SearchTorrent
        }

        #endregion

        #region Constructor

        public GUIRecommendationsShows()
        {
            backdrop = new ImageSwapper();
            backdrop.PropertyOne = "#Trakt.RecommendedShows.Fanart.1";
            backdrop.PropertyTwo = "#Trakt.RecommendedShows.Fanart.2";
        }

        #endregion

        #region Private Variables

        bool StopDownload { get; set; }
        Layout CurrentLayout { get; set; }
        string CurrentGenre { get; set; }
        bool HideCollected { get; set; }
        bool HideWatchlisted { get; set; }
        int StartYear { get; set; }
        int EndYear { get; set; }
        int PreviousSelectedIndex { get; set; }
        ImageSwapper backdrop;
        DateTime LastRequest = new DateTime();

        IEnumerable<TraktShow> RecommendedShows
        {
            get
            {
                if (_RecommendedShows == null || LastRequest < DateTime.UtcNow.Subtract(new TimeSpan(0, TraktSettings.WebRequestCacheMinutes, 0)))
                {
                    SetRecommendationProperties();
                    if ((StartYear > EndYear) && EndYear != 0) StartYear = 0;
                    _RecommendedShows = TraktAPI.TraktAPI.GetRecommendedShows(TraktGenres.ShowGenres[CurrentGenre], HideCollected, HideWatchlisted, StartYear, EndYear);
                    LastRequest = DateTime.UtcNow;
                    PreviousSelectedIndex = 0;
                }
                return _RecommendedShows;
            }
        }
        static IEnumerable<TraktShow> _RecommendedShows = null;

        #endregion

        #region Base Overrides

        public override int GetID
        {
            get
            {
                return 87262;
            }
        }

        public override bool Init()
        {
            return Load(GUIGraphicsContext.Skin + @"\Trakt.Recommendations.Shows.xml");
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

            // Load Recommended Shows
            LoadRecommendedShows();
        }

        protected override void OnPageDestroy(int new_windowId)
        {
            StopDownload = true;
            PreviousSelectedIndex = Facade.SelectedListItemIndex;
            ClearProperties();

            // save current layout
            TraktSettings.RecommendedShowsDefaultLayout = (int)CurrentLayout;

            // genre
            TraktSettings.ShowRecommendationGenre = CurrentGenre;

            // hide collected/watchlisted
            TraktSettings.ShowRecommendationHideCollected = HideCollected;
            TraktSettings.ShowRecommendationHideWatchlisted = HideWatchlisted;

            // start/end year
            TraktSettings.ShowRecommendationStartYear = StartYear;
            TraktSettings.ShowRecommendationEndYear = EndYear;

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
                        if (TraktSettings.EnableJumpToForTVShows)
                        {
                            CheckAndPlayEpisode(true);
                        }
                        else
                        {
                            GUIListItem selectedItem = this.Facade.SelectedListItem;
                            if (selectedItem == null) return;
                            TraktShow selectedShow = (TraktShow)selectedItem.TVTag;
                            GUIWindowManager.ActivateWindow((int)TraktGUIWindows.ShowSeasons, selectedShow.ToJSON());
                        }
                    }
                    break;

                // Layout Button
                case (2):
                    CurrentLayout = GUICommon.ShowLayoutMenu(CurrentLayout);
                    break;

                // Genre Button
                case (3):
                    ShowGenreMenu();
                    break;

                // Hide Collected Toggle Button
                case (4):
                    HideCollected = hideCollectedButton.Selected;
                    ReloadRecommendations();
                    break;

                // Hide Watchlisted Toggle Button
                case (5):
                    HideWatchlisted = hideWatchlistedButton.Selected;
                    ReloadRecommendations();
                    break;

                // Start Year Button
                case (6):
                    string startYear = StartYear.ToString();
                    if (startYear == "0") startYear = "1919";
                    if (GUIUtils.GetStringFromKeyboard(ref startYear))
                    {
                        int result;
                        if (startYear.Length == 4 && int.TryParse(startYear, out result))
                        {
                            StartYear = result;
                            GUIControl.SetControlLabel(GetID, startYearButton.GetID, GetStartYearTitle(StartYear));
                            ReloadRecommendations();
                        }
                    }
                    break;

                // End Year Button
                case (7):
                    string endYear = EndYear.ToString();
                    if (endYear == "0") endYear = DateTime.Now.AddYears(3).Year.ToString();
                    if (GUIUtils.GetStringFromKeyboard(ref endYear))
                    {
                        int result;
                        if (endYear.Length == 4 && int.TryParse(endYear, out result))
                        {
                            EndYear = result;
                            GUIControl.SetControlLabel(GetID, endYearButton.GetID, GetEndYearTitle(EndYear));
                            ReloadRecommendations();
                        }
                    }
                    break;

                // Sort Button
                case (8):
                    var newSortBy = GUICommon.ShowSortMenu(TraktSettings.SortByRecommendedShows);
                    if (newSortBy != null)
                    {
                        if (newSortBy.Field != TraktSettings.SortByRecommendedShows.Field)
                        {
                            TraktSettings.SortByRecommendedShows = newSortBy;
                            PreviousSelectedIndex = 0;
                            UpdateButtonState();
                            LoadRecommendedShows();
                        }
                    }
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

            TraktShow selectedShow = (TraktShow)selectedItem.TVTag;

            IDialogbox dlg = (IDialogbox)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
            if (dlg == null) return;

            dlg.Reset();
            dlg.SetHeading(GUIUtils.PluginName());

            GUIListItem listItem = null;

            listItem = new GUIListItem(Translation.DismissRecommendation);
            dlg.Add(listItem);
            listItem.ItemId = (int)ContextMenuItem.DismissRecommendation;

            // Show Season Information
            listItem = new GUIListItem(Translation.ShowSeasonInfo);
            dlg.Add(listItem);
            listItem.ItemId = (int)ContextMenuItem.ShowSeasonInfo;


            // Add/Remove Watch List            
            if (!selectedShow.InWatchList)
            {
                listItem = new GUIListItem(Translation.AddToWatchList);
                dlg.Add(listItem);
                listItem.ItemId = (int)ContextMenuItem.AddToWatchList;
            }
            else
            {
                listItem = new GUIListItem(Translation.RemoveFromWatchList);
                dlg.Add(listItem);
                listItem.ItemId = (int)ContextMenuItem.RemoveFromWatchList;
            }

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
                case ((int)ContextMenuItem.DismissRecommendation):
                    PreviousSelectedIndex = this.Facade.SelectedListItemIndex;
                    DismissRecommendation(selectedShow);
                    if (_RecommendedShows.Count() > 1)
                    {
                        var showsToExcept = new List<TraktShow>();
                        showsToExcept.Add(selectedShow);
                        _RecommendedShows = RecommendedShows.Except(showsToExcept);
                    }
                    else
                    {
                        // reload, none left
                        ClearProperties();
                        GUIControl.ClearControl(GetID, Facade.GetID);
                        _RecommendedShows = null;
                    }
                    LoadRecommendedShows();
                    break;

                case ((int)ContextMenuItem.ShowSeasonInfo):
                    GUIWindowManager.ActivateWindow((int)TraktGUIWindows.ShowSeasons, selectedShow.ToJSON());
                    break;

                case ((int)ContextMenuItem.AddToWatchList):
                    AddShowToWatchList(selectedShow);
                    selectedShow.InWatchList = true;
                    OnShowSelected(selectedItem, Facade);
                    selectedShow.Images.NotifyPropertyChanged("PosterImageFilename");
                    GUIWatchListShows.ClearCache(TraktSettings.Username);
                    break;

                case ((int)ContextMenuItem.RemoveFromWatchList):
                    RemoveShowFromWatchList(selectedShow);
                    selectedShow.InWatchList = false;
                    OnShowSelected(selectedItem, Facade);
                    selectedShow.Images.NotifyPropertyChanged("PosterImageFilename");
                    GUIWatchListShows.ClearCache(TraktSettings.Username);
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

                case ((int)ContextMenuItem.Trailers):
                    GUICommon.ShowTVShowTrailersMenu(selectedShow);
                    break;

                case ((int)ContextMenuItem.Shouts):
                    GUIShouts.ShoutType = GUIShouts.ShoutTypeEnum.show;
                    GUIShouts.ShowInfo = new ShowShout { IMDbId = selectedShow.Imdb, TVDbId = selectedShow.Tvdb, Title = selectedShow.Title };
                    GUIShouts.Fanart = selectedShow.Images.FanartImageFilename;
                    GUIWindowManager.ActivateWindow((int)TraktGUIWindows.Shouts);
                    break;

                case ((int)ContextMenuItem.Rate):
                    PreviousSelectedIndex = this.Facade.SelectedListItemIndex;
                    if (GUICommon.RateShow(selectedShow))
                    {
                        // remove from recommendations
                        if (_RecommendedShows.Count() > 1)
                        {
                            var showsToExcept = new List<TraktShow>();
                            showsToExcept.Add(selectedShow);
                            _RecommendedShows = RecommendedShows.Except(showsToExcept);
                        }
                        else
                        {
                            // reload, none left
                            ClearProperties();
                            GUIControl.ClearControl(GetID, Facade.GetID);
                            _RecommendedShows = null;
                        }
                        LoadRecommendedShows();
                    }
                    break;

                case ((int)ContextMenuItem.ChangeLayout):
                    CurrentLayout = GUICommon.ShowLayoutMenu(CurrentLayout);
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

        private void ReloadRecommendations()
        {
            PreviousSelectedIndex = this.Facade.SelectedListItemIndex;
            ClearProperties();
            GUIControl.ClearControl(GetID, Facade.GetID);
            _RecommendedShows = null;
            LoadRecommendedShows();
        }

        private void CheckAndPlayEpisode(bool jumpTo)
        {
            GUIListItem selectedItem = this.Facade.SelectedListItem;
            if (selectedItem == null) return;

            TraktShow selectedShow = (TraktShow)selectedItem.TVTag;
            GUICommon.CheckAndPlayFirstUnwatched(Convert.ToInt32(selectedShow.Tvdb), string.IsNullOrEmpty(selectedShow.Imdb) ? selectedShow.Title : selectedShow.Imdb, jumpTo);
        }

        private TraktShowSync CreateSyncData(TraktShow show)
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

        private void DismissRecommendation(TraktShow show)
        {
            Thread syncThread = new Thread(delegate(object obj)
            {
                TraktShow dismissShow = obj as TraktShow;

                TraktShowSlug syncShow = new TraktShowSlug
                {
                    UserName = TraktSettings.Username,
                    Password = TraktSettings.Password,
                    IMDbId = dismissShow.Imdb,
                    TVDbId = dismissShow.Tvdb,
                    Title = dismissShow.Title,
                    Year = dismissShow.Year.ToString()
                };

                TraktResponse response = TraktAPI.TraktAPI.DismissShowRecommendation(syncShow);
                TraktAPI.TraktAPI.LogTraktResponse<TraktResponse>(response);
            })
            {
                IsBackground = true,
                Name = "Dismiss Recommendation"
            };

            syncThread.Start(show);
        }

        private void AddShowToWatchList(TraktShow show)
        {
            Thread syncThread = new Thread(delegate(object obj)
            {
                TraktAPI.TraktAPI.SyncShowWatchList(CreateSyncData(obj as TraktShow), TraktSyncModes.watchlist);
            })
            {
                IsBackground = true,
                Name = "Adding Show to Watch List"
            };

            syncThread.Start(show);
        }

        private void RemoveShowFromWatchList(TraktShow show)
        {
            Thread syncThread = new Thread(delegate(object obj)
            {
                TraktAPI.TraktAPI.SyncShowWatchList(CreateSyncData(obj as TraktShow), TraktSyncModes.unwatchlist);
            })
            {
                IsBackground = true,
                Name = "Removing Show from Watch List"
            };

            syncThread.Start(show);
        }

        private void ShowGenreMenu()
        {
            IDialogbox dlg = (IDialogbox)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
            dlg.Reset();
            dlg.SetHeading(TraktGenres.ItemName(CurrentGenre));

            foreach (string genre in TraktGenres.ShowGenres.Keys)
            {
                string menuItem = TraktGenres.ItemName(genre);
                GUIListItem pItem = new GUIListItem(menuItem);
                if (genre == CurrentGenre) pItem.Selected = true;
                dlg.Add(pItem);
            }

            dlg.DoModal(GUIWindowManager.ActiveWindow);

            if (dlg.SelectedLabel >= 0)
            {
                var genre = TraktGenres.ShowGenres.ElementAt(dlg.SelectedLabel).Key;
                if (genre != CurrentGenre)
                {
                    CurrentGenre = genre;
                    GUIControl.SetControlLabel(GetID, genreButton.GetID, TraktGenres.ItemName(CurrentGenre));
                    ReloadRecommendations();
                }
            }
        }

        private string GetStartYearTitle(int startYear)
        {
            if (startYear == 0)
                return string.Format(Translation.StartYear, 1919);
            else
                return string.Format(Translation.StartYear, startYear);
        }

        private string GetEndYearTitle(int endYear)
        {
            if (endYear == 0)
                return string.Format(Translation.EndYear, DateTime.Now.AddYears(3).Year);
            else
                return string.Format(Translation.EndYear, endYear);
        }

        private void LoadRecommendedShows()
        {
            GUIUtils.SetProperty("#Trakt.Items", string.Empty);

            GUIBackgroundTask.Instance.ExecuteInBackgroundAndCallback(() =>
            {
                return RecommendedShows;
            },
            delegate(bool success, object result)
            {
                if (success)
                {
                    IEnumerable<TraktShow> shows = result as IEnumerable<TraktShow>;
                    SendRecommendedShowsToFacade(shows);
                }
            }, Translation.GettingRecommendedShows, true);
        }

        private void SendRecommendedShowsToFacade(IEnumerable<TraktShow> shows)
        {
            // clear facade
            GUIControl.ClearControl(GetID, Facade.GetID);

            if (shows.Count() == 0)
            {
                GUIUtils.ShowNotifyDialog(GUIUtils.PluginName(), Translation.NoShowRecommendations);
                GUIWindowManager.ShowPreviousWindow();
                return;
            }

            // sort shows
            var showList = shows.ToList();
            showList.Sort(new GUIListItemShowSorter(TraktSettings.SortByRecommendedShows.Field, TraktSettings.SortByRecommendedShows.Direction));

            int itemId = 0;
            List<TraktShow.ShowImages> showImages = new List<TraktShow.ShowImages>();

            foreach (var show in showList)
            {
                GUITraktRecommendedShowListItem item = new GUITraktRecommendedShowListItem(show.Title);

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

            // load last layout
            CurrentLayout = (Layout)TraktSettings.RecommendedShowsDefaultLayout;
            // update button label
            GUIControl.SetControlLabel(GetID, layoutButton.GetID, GUICommon.GetLayoutTranslation(CurrentLayout));

            // genres
            CurrentGenre = TraktSettings.ShowRecommendationGenre;
            if (genreButton != null) GUIControl.SetControlLabel(GetID, genreButton.GetID, TraktGenres.ItemName(CurrentGenre));

            // toggles for hide collected/watchlisted
            HideCollected = TraktSettings.ShowRecommendationHideCollected;
            HideWatchlisted = TraktSettings.ShowRecommendationHideWatchlisted;
            if (hideCollectedButton != null) hideCollectedButton.Selected = HideCollected;
            if (hideWatchlistedButton != null) hideWatchlistedButton.Selected = HideWatchlisted;

            // start/end year
            StartYear = TraktSettings.ShowRecommendationStartYear;
            EndYear = TraktSettings.ShowRecommendationEndYear;
            if (startYearButton != null) GUIControl.SetControlLabel(GetID, startYearButton.GetID, GetStartYearTitle(StartYear));
            if (endYearButton != null) GUIControl.SetControlLabel(GetID, endYearButton.GetID, GetEndYearTitle(EndYear));

            SetRecommendationProperties();

            if (sortButton != null)
            {
                UpdateButtonState();
                sortButton.SortChanged += (o, e) =>
                {
                    TraktSettings.SortByRecommendedShows.Direction = (SortingDirections)(e.Order - 1);
                    PreviousSelectedIndex = 0;
                    UpdateButtonState();
                    LoadRecommendedShows();
                };
            }
        }

        private void UpdateButtonState()
        {
            // update sortby button label
            if (sortButton != null)
            {
                sortButton.Label = GUICommon.GetSortByString(TraktSettings.SortByRecommendedShows);
                sortButton.IsAscending = (TraktSettings.SortByRecommendedShows.Direction == SortingDirections.Ascending);
            }
            GUIUtils.SetProperty("#Trakt.SortBy", GUICommon.GetSortByString(TraktSettings.SortByRecommendedShows));
        }

        private void SetRecommendationProperties()
        {
            GUIUtils.SetProperty("#Trakt.Recommendations.Genre", TraktGenres.Translate(CurrentGenre));
            GUIUtils.SetProperty("#Trakt.Recommendations.HideCollected", HideCollected.ToString());
            GUIUtils.SetProperty("#Trakt.Recommendations.HideWatchlisted", HideWatchlisted.ToString());
            GUIUtils.SetProperty("#Trakt.Recommendations.StartYear", StartYear == 0 ? "1919" : StartYear.ToString());
            GUIUtils.SetProperty("#Trakt.Recommendations.EndYear", EndYear == 0 ? DateTime.Now.AddYears(3).Year.ToString() : EndYear.ToString());
        }

        private void ClearProperties()
        {
            GUICommon.ClearShowProperties();
        }

        private void PublishShowSkinProperties(TraktShow show)
        {
            GUICommon.SetShowProperties(show);
        }

        private void OnShowSelected(GUIListItem item, GUIControl parent)
        {
            TraktShow show = item.TVTag as TraktShow;
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

        public static void ClearCache()
        {
            _RecommendedShows = null;
        }

        #endregion
    }

    public class GUITraktRecommendedShowListItem : GUIListItem
    {
        public GUITraktRecommendedShowListItem(string strLabel) : base(strLabel) { }

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

            // determine the overlay to add to poster
            TraktShow show = TVTag as TraktShow;
            MainOverlayImage mainOverlay = MainOverlayImage.None;

            if (show.InWatchList)
                mainOverlay = MainOverlayImage.Watchlist;

            // we never show rating movies in Recommendations
            RatingOverlayImage ratingOverlay = RatingOverlayImage.None;

            // get a reference to a MediaPortal Texture Identifier
            string suffix = Enum.GetName(typeof(MainOverlayImage), mainOverlay) + Enum.GetName(typeof(RatingOverlayImage), ratingOverlay);
            string texture = GUIImageHandler.GetTextureIdentFromFile(imageFilePath, suffix);

            // build memory image
            Image memoryImage = null;
            if (mainOverlay != MainOverlayImage.None)
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
            GUIRecommendationsShows window = GUIWindowManager.GetWindow(GUIWindowManager.ActiveWindow) as GUIRecommendationsShows;
            if (window != null)
            {
                GUIListItem selectedItem = GUIControl.GetSelectedListItem((int)TraktGUIWindows.RecommendationsShows, (int)TraktGUIControls.Facade);
                if (selectedItem == this)
                {
                    GUIWindowManager.SendThreadMessage(new GUIMessage(GUIMessage.MessageType.GUI_MSG_ITEM_SELECT, GUIWindowManager.ActiveWindow, 0, (int)TraktGUIControls.Facade, ItemId, 0, null));
                }
            }
        }
    }
}
