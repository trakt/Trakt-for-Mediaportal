using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using MediaPortal.GUI.Library;
using MediaPortal.Util;
using TraktAPI.DataStructures;
using TraktAPI.Extensions;
using TraktPlugin.TmdbAPI.DataStructures;
using TraktPlugin.Cache;
using Action = MediaPortal.GUI.Library.Action;

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
            Cast,
            Crew,
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

        GUIFacadeControl.Layout CurrentLayout { get; set; }
        string CurrentGenre { get; set; }
        bool HideCollected { get; set; }
        bool HideWatchlisted { get; set; }
        int StartYear { get; set; }
        int EndYear { get; set; }
        int PreviousSelectedIndex { get; set; }
        ImageSwapper backdrop;
        DateTime LastRequest = new DateTime();

        IEnumerable<TraktShowSummary> RecommendedShows
        {
            get
            {
                if (_RecommendedShows == null || LastRequest < DateTime.UtcNow.Subtract(new TimeSpan(0, TraktSettings.WebRequestCacheMinutes, 0)))
                {
                    SetRecommendationProperties();
                    if ((StartYear > EndYear) && EndYear != 0) StartYear = 0;
                    //TODO_RecommendedShows = TraktAPI.TraktAPI.GetRecommendedShows(TraktGenres.ShowGenres[CurrentGenre], HideCollected, HideWatchlisted, StartYear, EndYear);
                    _RecommendedShows = TraktAPI.TraktAPI.GetRecommendedShows();
                    LastRequest = DateTime.UtcNow;
                    PreviousSelectedIndex = 0;
                }
                return _RecommendedShows;
            }
        }
        static IEnumerable<TraktShowSummary> _RecommendedShows = null;

        #endregion

        #region Base Overrides

        public override int GetID
        {
            get
            {
                return (int)TraktGUIWindows.RecommendationsShows;
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
            GUIShowListItem.StopDownload = true;
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

                            var selectedShow = selectedItem.TVTag as TraktShowSummary;
                            if (selectedShow == null) return;

                            GUIWindowManager.ActivateWindow((int)TraktGUIWindows.ShowSeasons, selectedShow.ToJSON());
                        }
                    }
                    break;

                // Layout Button
                case (2):
                    CurrentLayout = GUICommon.ShowLayoutMenu(CurrentLayout, PreviousSelectedIndex);
                    break;

                // Genre Button
                case (3):
                    GUIUtils.ShowNotifyDialog("Trakt", Translation.FeatureNotAvailable);
                    break;
                    //TODO
                    //ShowGenreMenu();
                    //break;

                // Hide Collected Toggle Button
                case (4):
                    GUIUtils.ShowNotifyDialog("Trakt", Translation.FeatureNotAvailable);
                    break;
                    //TODO
                    //HideCollected = hideCollectedButton.Selected;
                    //ReloadRecommendations();
                    //break;

                // Hide Watchlisted Toggle Button
                case (5):
                    GUIUtils.ShowNotifyDialog("Trakt", Translation.FeatureNotAvailable);
                    break;
                    //TODO
                    //HideWatchlisted = hideWatchlistedButton.Selected;
                    //ReloadRecommendations();
                    //break;

                // Start Year Button
                case (6):
                    GUIUtils.ShowNotifyDialog("Trakt", Translation.FeatureNotAvailable);
                    break;
                    //TODO
                    //string startYear = StartYear.ToString();
                    //if (startYear == "0") startYear = "1919";
                    //if (GUIUtils.GetStringFromKeyboard(ref startYear))
                    //{
                    //    int result;
                    //    if (startYear.Length == 4 && int.TryParse(startYear, out result) && !startYear.Equals(StartYear.ToString()))
                    //    {
                    //        StartYear = result;
                    //        GUIControl.SetControlLabel(GetID, startYearButton.GetID, GetStartYearTitle(StartYear));
                    //        ReloadRecommendations();
                    //    }
                    //}
                    //break;

                // End Year Button
                case (7):
                    GUIUtils.ShowNotifyDialog("Trakt", Translation.FeatureNotAvailable);
                    break;
                    //TODO
                    //string endYear = EndYear.ToString();
                    //if (endYear == "0") endYear = DateTime.Now.AddYears(3).Year.ToString();
                    //if (GUIUtils.GetStringFromKeyboard(ref endYear))
                    //{
                    //    int result;
                    //    if (endYear.Length == 4 && int.TryParse(endYear, out result) && !endYear.Equals(EndYear.ToString()))
                    //    {
                    //        EndYear = result;
                    //        GUIControl.SetControlLabel(GetID, endYearButton.GetID, GetEndYearTitle(EndYear));
                    //        ReloadRecommendations();
                    //    }
                    //}
                    //break;

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
            var selectedItem = this.Facade.SelectedListItem as GUIShowListItem;
            if (selectedItem == null) return;

            var selectedShow = selectedItem.TVTag as TraktShowSummary;
            if (selectedShow == null) return;

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

            // Add/Remove Watchlist            
            if (!selectedShow.IsWatchlisted())
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

            listItem = new GUIListItem(Translation.AddToList);
            dlg.Add(listItem);
            listItem.ItemId = (int)ContextMenuItem.AddToList;

            if (TraktHelper.IsTrailersAvailableAndEnabled)
            {
                listItem = new GUIListItem(Translation.Trailers);
                dlg.Add(listItem);
                listItem.ItemId = (int)ContextMenuItem.Trailers;
            }

            // Related Shows
            listItem = new GUIListItem(Translation.RelatedShows);
            dlg.Add(listItem);
            listItem.ItemId = (int)ContextMenuItem.Related;

            // Rate Show
            listItem = new GUIListItem(Translation.RateShow);
            dlg.Add(listItem);
            listItem.ItemId = (int)ContextMenuItem.Rate;

            // Shouts
            listItem = new GUIListItem(Translation.Comments);
            dlg.Add(listItem);
            listItem.ItemId = (int)ContextMenuItem.Shouts;

            // Cast and Crew
            listItem = new GUIListItem(Translation.Cast);
            dlg.Add(listItem);
            listItem.ItemId = (int)ContextMenuItem.Cast;

            listItem = new GUIListItem(Translation.Crew);
            dlg.Add(listItem);
            listItem.ItemId = (int)ContextMenuItem.Crew;

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
                        var showsToExcept = new List<TraktShowSummary>();
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
                    TraktHelper.AddShowToWatchList(selectedShow);
                    OnShowSelected(selectedItem, Facade);
                    (Facade.SelectedListItem as GUIShowListItem).Images.NotifyPropertyChanged("Poster");
                    break;

                case ((int)ContextMenuItem.RemoveFromWatchList):
                    TraktHelper.RemoveShowFromWatchList(selectedShow);
                    OnShowSelected(selectedItem, Facade);
                    (Facade.SelectedListItem as GUIShowListItem).Images.NotifyPropertyChanged("Poster");
                    break;

                case ((int)ContextMenuItem.AddToList):
                    TraktHelper.AddRemoveShowInUserList(selectedShow, false);
                    break;

                case ((int)ContextMenuItem.Related):
                    TraktHelper.ShowRelatedShows(selectedShow);
                    break;

                case ((int)ContextMenuItem.Trailers):
                    GUICommon.ShowTVShowTrailersMenu(selectedShow);
                    break;

                case ((int)ContextMenuItem.Shouts):
                    TraktHelper.ShowTVShowShouts(selectedShow);
                    break;

                case ((int)ContextMenuItem.Rate):
                    PreviousSelectedIndex = this.Facade.SelectedListItemIndex;
                    if (GUICommon.RateShow(selectedShow))
                    {
                        // remove from recommendations
                        if (_RecommendedShows.Count() > 1)
                        {
                            var showsToExcept = new List<TraktShowSummary>();
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

                case ((int)ContextMenuItem.Cast):
                    GUICreditsShow.Show = selectedShow;
                    GUICreditsShow.Type = GUICreditsShow.CreditType.Cast;
                    GUICreditsShow.Fanart = TmdbCache.GetShowBackdropFilename(selectedItem.Images.ShowImages);
                    GUIWindowManager.ActivateWindow((int)TraktGUIWindows.CreditsShow);
                    break;

                case ((int)ContextMenuItem.Crew):
                    GUICreditsShow.Show = selectedShow;
                    GUICreditsShow.Type = GUICreditsShow.CreditType.Crew;
                    GUICreditsShow.Fanart = TmdbCache.GetShowBackdropFilename(selectedItem.Images.ShowImages);
                    GUIWindowManager.ActivateWindow((int)TraktGUIWindows.CreditsShow);
                    break;

                case ((int)ContextMenuItem.ChangeLayout):
                    CurrentLayout = GUICommon.ShowLayoutMenu(CurrentLayout, PreviousSelectedIndex);
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

            var selectedShow = selectedItem.TVTag as TraktShowSummary;
            if (selectedShow == null) return;

            GUICommon.CheckAndPlayFirstUnwatchedEpisode(selectedShow, jumpTo);
        }

        private void DismissRecommendation(TraktShow show)
        {
            Thread syncThread = new Thread(delegate(object obj)
            {
                TraktShow dismissShow = obj as TraktShow;

                var response = TraktAPI.TraktAPI.DismissRecommendedShow(dismissShow.Ids.Trakt.ToString());
            })
            {
                IsBackground = true,
                Name = "DismissRecommendation"
            };

            syncThread.Start(show);
        }
        
        private void ShowGenreMenu()
        {
            var dlg = (IDialogbox)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
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
                    var shows = result as IEnumerable<TraktShowSummary>;
                    SendRecommendedShowsToFacade(shows);
                }
            }, Translation.GettingRecommendedShows, true);
        }

        private void SendRecommendedShowsToFacade(IEnumerable<TraktShowSummary> shows)
        {
            // clear facade
            GUIControl.ClearControl(GetID, Facade.GetID);

            if (shows == null)
            {
                GUIUtils.ShowNotifyDialog(Translation.Error, Translation.ErrorGeneral);
                GUIWindowManager.ShowPreviousWindow();
                return;
            }

            if (shows.Count() == 0)
            {
                // try again
                if (genreButton == null)
                {
                    // restore defaults for next time so we can get recommendations
                    if (CurrentGenre != "All")
                        CurrentGenre = "All";

                    _RecommendedShows = null;
                    GUIWindowManager.ShowPreviousWindow();
                }
                else
                {
                    GUIUtils.ShowNotifyDialog(GUIUtils.PluginName(), Translation.NoShowRecommendations);
                    GUIControl.FocusControl(GetID, genreButton.GetID);
                }
                return;
            }

            // sort shows
            var showList = shows.ToList();
            showList.Sort(new GUIListItemShowSorter(TraktSettings.SortByRecommendedShows.Field, TraktSettings.SortByRecommendedShows.Direction));

            int itemId = 0;
            var showImages = new List<GUITmdbImage>();

            foreach (var show in showList)
            {
                var item = new GUIShowListItem(show.Title, (int)TraktGUIWindows.RecommendationsShows);

                // add image for download
                var images = new GUITmdbImage { ShowImages = new TmdbShowImages { Id = show.Ids.Tmdb } };
                showImages.Add(images);

                item.Label2 = show.Year.ToString();
                item.TVTag = show;
                item.Show = show;
                item.Images = images;
                item.ItemId = Int32.MaxValue - itemId;
                item.IconImage = GUIImageHandler.GetDefaultPoster(false);
                item.IconImageBig = GUIImageHandler.GetDefaultPoster();
                item.ThumbnailImage = GUIImageHandler.GetDefaultPoster();
                item.OnItemSelected += OnShowSelected;
                Utils.SetDefaultIcons(item);
                Facade.Add(item);
                itemId++;
            }

            // Set Facade Layout
            Facade.CurrentLayout = CurrentLayout;
            GUIControl.FocusControl(GetID, Facade.GetID);

            if (PreviousSelectedIndex >= shows.Count())
                Facade.SelectIndex(PreviousSelectedIndex - 1);
            else
                Facade.SelectIndex(PreviousSelectedIndex);

            // set facade properties
            GUIUtils.SetProperty("#itemcount", shows.Count().ToString());
            GUIUtils.SetProperty("#Trakt.Items", string.Format("{0} {1}", shows.Count().ToString(), shows.Count() > 1 ? Translation.SeriesPlural : Translation.Series));

            // Download show images Async and set to facade
            GUIShowListItem.GetImages(showImages);
        }

        private void InitProperties()
        {
            // Fanart
            backdrop.GUIImageOne = FanartBackground;
            backdrop.GUIImageTwo = FanartBackground2;
            backdrop.LoadingImage = loadingImage;            

            // load last layout
            CurrentLayout = (GUIFacadeControl.Layout)TraktSettings.RecommendedShowsDefaultLayout;
            // update button label
            GUIControl.SetControlLabel(GetID, layoutButton.GetID, GUICommon.GetLayoutTranslation(CurrentLayout));

            // genres
            CurrentGenre = TraktSettings.ShowRecommendationGenre;
            if (genreButton != null) GUIControl.SetControlLabel(GetID, genreButton.GetID, TraktGenres.ItemName(CurrentGenre));

            // toggles for hide collected/watchlisted
            HideCollected = TraktSettings.ShowRecommendationHideCollected;
            HideWatchlisted = TraktSettings.ShowRecommendationHideWatchlisted;
            if (hideCollectedButton != null)
            {
                hideCollectedButton.Selected = HideCollected;
                GUIControl.SetControlLabel(GetID, hideCollectedButton.GetID, Translation.HideCollected);
            }
            if (hideWatchlistedButton != null)
            {
                hideWatchlistedButton.Selected = HideWatchlisted;
                GUIControl.SetControlLabel(GetID, hideWatchlistedButton.GetID, Translation.HideWatchlisted);
            }

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

        private void PublishShowSkinProperties(TraktShowSummary show)
        {
            GUICommon.SetShowProperties(show);
        }

        private void OnShowSelected(GUIListItem item, GUIControl parent)
        {
            PreviousSelectedIndex = Facade.SelectedListItemIndex;

            var show = item.TVTag as TraktShowSummary;
            if (show == null) return;

            PublishShowSkinProperties(show);
            GUIImageHandler.LoadFanart(backdrop, TmdbCache.GetShowBackdropFilename((item as GUIShowListItem).Images.ShowImages));
        }
        #endregion

        #region Public Methods

        public static void ClearCache()
        {
            _RecommendedShows = null;
        }

        #endregion
    }
}
