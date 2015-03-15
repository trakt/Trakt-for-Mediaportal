using System;
using System.Collections.Generic;
using System.Linq;
using MediaPortal.GUI.Library;
using MediaPortal.Util;
using TraktPlugin.TraktAPI.DataStructures;
using TraktPlugin.TraktAPI.Extensions;
using Action = MediaPortal.GUI.Library.Action;

namespace TraktPlugin.GUI
{
    public class GUITrendingShows : GUIWindow
    {
        #region Skin Controls

        [SkinControl(2)]
        protected GUIButtonControl layoutButton = null;

        [SkinControl(8)]
        protected GUISortButtonControl sortButton = null;

        [SkinControl(9)]
        protected GUICheckButton filterWatchedButton = null;

        [SkinControl(10)]
        protected GUICheckButton filterWatchListedButton = null;

        [SkinControl(11)]
        protected GUICheckButton filterCollectedButton = null;

        [SkinControl(12)]
        protected GUICheckButton filterRatedButton = null;

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

        #endregion

        #region Constructor

        public GUITrendingShows() 
        {
            backdrop = new ImageSwapper();
            backdrop.PropertyOne = "#Trakt.TrendingShows.Fanart.1";
            backdrop.PropertyTwo = "#Trakt.TrendingShows.Fanart.2";
        }

        #endregion

        #region Private Variables

        private Dictionary<int, TraktShowsTrending> TrendingShowPages = null;
        private Layout CurrentLayout { get; set; }
        private ImageSwapper backdrop;
        DateTime LastRequest = new DateTime();
        int PreviousSelectedIndex = 0;
        int CurrentPage = 1;

        #endregion

        #region Base Overrides

        public override int GetID
        {
            get
            {
                return (int)TraktGUIWindows.TrendingShows;
            }
        }

        public override bool Init()
        {
            return Load(GUIGraphicsContext.Skin + @"\Trakt.Trending.Shows.xml");
        }

        protected override void OnPageLoad()
        {
            base.OnPageLoad();

            // Clear GUI Properties
            ClearProperties();

            // Init Properties
            InitProperties();

            // Load Trending Shows
            LoadTrendingShows(CurrentPage);
        }

        protected override void OnPageDestroy(int new_windowId)
        {
            GUIShowListItem.StopDownload = true;
            PreviousSelectedIndex = Facade.SelectedListItemIndex;
            ClearProperties();

            // save current layout
            TraktSettings.TrendingShowsDefaultLayout = (int)CurrentLayout;

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
                        var item = Facade.SelectedListItem as GUIShowListItem;
                        if (item == null) return;

                        if (!item.IsFolder)
                        {
                            if (TraktSettings.EnableJumpToForTVShows)
                            {
                                CheckAndPlayEpisode(true);
                            }
                            else
                            {
                                if (item.Show == null) return;
                                GUIWindowManager.ActivateWindow((int)TraktGUIWindows.ShowSeasons, item.Show.ToJSON());
                            }
                        }
                        else
                        {
                            if (item.IsPrevPageItem)
                                CurrentPage--;
                            else
                                CurrentPage++;

                            if (CurrentPage == 1)
                                PreviousSelectedIndex = 0;
                            else
                                PreviousSelectedIndex = 1;

                            // load next / previous page
                            LoadTrendingShows(CurrentPage);
                        }
                        
                    }
                    break;

                // Layout Button
                case (2):
                    CurrentLayout = GUICommon.ShowLayoutMenu(CurrentLayout, PreviousSelectedIndex);
                    break;

                // Sort Button
                case (8):
                    var newSortBy = GUICommon.ShowSortMenu(TraktSettings.SortByTrendingShows);
                    if (newSortBy != null)
                    {
                        if (newSortBy.Field != TraktSettings.SortByTrendingShows.Field)
                        {
                            TraktSettings.SortByTrendingShows = newSortBy;
                            PreviousSelectedIndex = CurrentPage == 1 ? 0 : 1;
                            UpdateButtonState();
                            LoadTrendingShows(CurrentPage);
                        }
                    }
                    break;

                // Hide Watched
                case (9):
                    PreviousSelectedIndex = CurrentPage == 1 ? 0 : 1;
                    TraktSettings.TrendingShowsHideWatched = !TraktSettings.TrendingShowsHideWatched;
                    UpdateButtonState();
                    LoadTrendingShows(CurrentPage);
                    break;

                // Hide Watchlisted
                case (10):
                    PreviousSelectedIndex = CurrentPage == 1 ? 0 : 1;
                    TraktSettings.TrendingShowsHideWatchlisted = !TraktSettings.TrendingShowsHideWatchlisted;
                    UpdateButtonState();
                    LoadTrendingShows(CurrentPage);
                    break;

                // Hide Collected
                case (11):
                    PreviousSelectedIndex = CurrentPage == 1 ? 0 : 1;
                    TraktSettings.TrendingShowsHideCollected = !TraktSettings.TrendingShowsHideCollected;
                    UpdateButtonState();
                    LoadTrendingShows(CurrentPage);
                    break;

                // Hide Rated
                case (12):
                    PreviousSelectedIndex = CurrentPage == 1 ? 0 : 1;
                    TraktSettings.TrendingShowsHideRated = !TraktSettings.TrendingShowsHideRated;
                    UpdateButtonState();
                    LoadTrendingShows(CurrentPage);
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
                    if (!GUIBackgroundTask.Instance.IsBusy)
                    {
                        CheckAndPlayEpisode(false);
                    };
                    break;
                default:
                    base.OnAction(action);
                    break;
            }
        }

        protected override void OnShowContextMenu()
        {
            var selectedItem = this.Facade.SelectedListItem;
            if (selectedItem == null) return;

            var selectedTrendingItem = selectedItem.TVTag as TraktShowTrending;
            if (selectedTrendingItem == null) return;

            var dlg = (IDialogbox)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
            if (dlg == null) return;

            dlg.Reset();
            dlg.SetHeading(GUIUtils.PluginName());

            GUICommon.CreateTrendingShowsContextMenu(ref dlg, selectedTrendingItem.Show, false);

            // Show Context Menu
            dlg.DoModal(GUIWindowManager.ActiveWindow);
            if (dlg.SelectedId < 0) return;

            switch (dlg.SelectedId)
            {
                case ((int)TrendingContextMenuItem.AddToWatchList):
                    TraktHelper.AddShowToWatchList(selectedTrendingItem.Show);
                    OnShowSelected(selectedItem, Facade);
                    (Facade.SelectedListItem as GUIShowListItem).Images.NotifyPropertyChanged("Poster");
                    if (TraktSettings.TrendingShowsHideWatchlisted) LoadTrendingShows(CurrentPage);
                    break;

                case ((int)TrendingContextMenuItem.ShowSeasonInfo):
                    GUIWindowManager.ActivateWindow((int)TraktGUIWindows.ShowSeasons, selectedTrendingItem.Show.ToJSON());
                    break;

                case ((int)TrendingContextMenuItem.MarkAsWatched):
                    GUICommon.MarkShowAsWatched(selectedTrendingItem.Show);
                    if (TraktSettings.TrendingShowsHideWatched) LoadTrendingShows(CurrentPage);
                    break;

                case ((int)TrendingContextMenuItem.AddToLibrary):
                    GUICommon.AddShowToCollection(selectedTrendingItem.Show);
                    if (TraktSettings.TrendingShowsHideCollected) LoadTrendingShows(CurrentPage);
                    break;

                case ((int)TrendingContextMenuItem.RemoveFromWatchList):
                    TraktHelper.RemoveShowFromWatchList(selectedTrendingItem.Show);
                    OnShowSelected(selectedItem, Facade);
                    (Facade.SelectedListItem as GUIShowListItem).Images.NotifyPropertyChanged("Poster");
                    break;

                case ((int)TrendingContextMenuItem.AddToList):
                    TraktHelper.AddRemoveShowInUserList(selectedTrendingItem.Show, false);
                    break;

                case ((int)TrendingContextMenuItem.Filters):
                    if (GUICommon.ShowTVShowFiltersMenu())
                    {
                        PreviousSelectedIndex = CurrentPage == 1 ? 0 : 1;
                        UpdateButtonState();
                        LoadTrendingShows(CurrentPage);
                    }
                    break;

                case ((int)TrendingContextMenuItem.Related):
                    TraktHelper.ShowRelatedShows(selectedTrendingItem.Show);
                    break;

                case ((int)TrendingContextMenuItem.Trailers):
                    GUICommon.ShowTVShowTrailersMenu(selectedTrendingItem.Show);
                    break;

                case ((int)TrendingContextMenuItem.Shouts):
                    TraktHelper.ShowTVShowShouts(selectedTrendingItem.Show);
                    break;

                case ((int)TrendingContextMenuItem.Rate):
                    GUICommon.RateShow(selectedTrendingItem.Show);
                    OnShowSelected(selectedItem, Facade);
                    (Facade.SelectedListItem as GUIShowListItem).Images.NotifyPropertyChanged("Poster");
                    if (TraktSettings.TrendingShowsHideRated) LoadTrendingShows(CurrentPage);
                    break;

                case ((int)TrendingContextMenuItem.ChangeLayout):
                    CurrentLayout = GUICommon.ShowLayoutMenu(CurrentLayout, PreviousSelectedIndex);
                    break;

                case ((int)TrendingContextMenuItem.SearchWithMpNZB):
                    string loadingParam = string.Format("search:{0}", selectedTrendingItem.Show.Title);
                    GUIWindowManager.ActivateWindow((int)ExternalPluginWindows.MpNZB, loadingParam);
                    break;

                case ((int)TrendingContextMenuItem.SearchTorrent):
                    string loadPar = selectedTrendingItem.Show.Title;
                    GUIWindowManager.ActivateWindow((int)ExternalPluginWindows.MyTorrents, loadPar);
                    break;

                default:
                    break;
            }

            base.OnShowContextMenu();
        }

        #endregion

        #region Private Methods

        TraktShowsTrending GetTrendingShows(int page)
        {
            TraktShowsTrending trendingShows = null;

            if (TrendingShowPages == null || LastRequest < DateTime.UtcNow.Subtract(new TimeSpan(0, TraktSettings.WebRequestCacheMinutes, 0)))
            {
                // get the first page
                trendingShows = TraktAPI.TraktAPI.GetTrendingShows(1, TraktSettings.MaxTrendingShowsRequest);

                // reset to defaults
                LastRequest = DateTime.UtcNow;
                CurrentPage = 1;
                PreviousSelectedIndex = 0;

                // clear the cache
                if (TrendingShowPages == null)
                    TrendingShowPages = new Dictionary<int, TraktShowsTrending>();
                else
                    TrendingShowPages.Clear();

                // add page to cache
                TrendingShowPages.Add(1, trendingShows);
            }
            else
            {
                // get page from cache if it exists
                if (TrendingShowPages.TryGetValue(page, out trendingShows))
                {
                    return trendingShows;
                }

                // request next page
                trendingShows = TraktAPI.TraktAPI.GetTrendingShows(page, TraktSettings.MaxTrendingShowsRequest);
                if (trendingShows != null && trendingShows.Shows != null)
                {
                    // add to cache
                    TrendingShowPages.Add(page, trendingShows);
                }
            }
            return trendingShows;
        }

        private void CheckAndPlayEpisode(bool jumpTo)
        {
            var selectedItem = this.Facade.SelectedListItem;
            if (selectedItem == null) return;

            var selectedTrendingItem = selectedItem.TVTag as TraktShowTrending;
            if (selectedTrendingItem == null) return;

            GUICommon.CheckAndPlayFirstUnwatchedEpisode(selectedTrendingItem.Show, jumpTo);
        }

        private void LoadTrendingShows(int page = 1)
        {
            GUIUtils.SetProperty("#Trakt.Items", string.Empty);

            GUIBackgroundTask.Instance.ExecuteInBackgroundAndCallback(() =>
            {
                return GetTrendingShows(page);
            },
            delegate(bool success, object result)
            {
                if (success)
                {
                    var shows = result as TraktShowsTrending;
                    SendTrendingShowsToFacade(shows);
                }
            }, Translation.GettingTrendingShows, true);
        }

        private void SendTrendingShowsToFacade(TraktShowsTrending trendingItems)
        {
            // clear facade
            GUIControl.ClearControl(GetID, Facade.GetID);

            if (trendingItems == null)
            {
                GUIUtils.ShowNotifyDialog(Translation.Error, Translation.ErrorGeneral);
                GUIWindowManager.ShowPreviousWindow();
                TrendingShowPages = null;
                return;
            }

            if (trendingItems.Shows.Count() == 0)
            {
                GUIUtils.ShowNotifyDialog(GUIUtils.PluginName(), Translation.NoTrendingShows);
                GUIWindowManager.ShowPreviousWindow();
                TrendingShowPages = null;
                return;
            }

            // filter shows
            var filteredTrendingList = GUICommon.FilterTrendingShows(trendingItems.Shows).ToList();

            // sort shows
            filteredTrendingList.Sort(new GUIListItemShowSorter(TraktSettings.SortByTrendingShows.Field, TraktSettings.SortByTrendingShows.Direction));

            int itemId = 0;
            var showImages = new List<GUITraktImage>();

            // Add Previous Page Button
            if (trendingItems.CurrentPage != 1)
            {
                var prevPageItem = new GUIShowListItem(Translation.PreviousPage, (int)TraktGUIWindows.TrendingShows);
                prevPageItem.IsPrevPageItem = true;
                prevPageItem.IconImage = "traktPreviousPage.png";
                prevPageItem.IconImageBig = "traktPreviousPage.png";
                prevPageItem.ThumbnailImage = "traktPreviousPage.png";
                prevPageItem.OnItemSelected += OnPreviousPageSelected;
                prevPageItem.IsFolder = true;
                Facade.Add(prevPageItem);
                itemId++;
            }

            foreach (var trendingItem in filteredTrendingList)
            {
                var item = new GUIShowListItem(trendingItem.Show.Title, (int)TraktGUIWindows.TrendingShows);

                // add image for download
                var image = new GUITraktImage { ShowImages = trendingItem.Show.Images };
                showImages.Add(image);

                item.Label2 = trendingItem.Show.Year.ToString();
                item.TVTag = trendingItem;
                item.Show = trendingItem.Show;
                item.Images = image;
                item.ItemId = Int32.MaxValue - itemId;                
                item.IconImage = GUIImageHandler.GetDefaultPoster(false);
                item.IconImageBig = GUIImageHandler.GetDefaultPoster();
                item.ThumbnailImage = GUIImageHandler.GetDefaultPoster();
                item.OnItemSelected += OnShowSelected;
                Utils.SetDefaultIcons(item);
                Facade.Add(item);
                itemId++;
            }

            // Add Next Page Button
            if (trendingItems.CurrentPage != trendingItems.TotalPages)
            {
                var nextPageItem = new GUIShowListItem(Translation.NextPage, (int)TraktGUIWindows.TrendingShows);
                nextPageItem.IsNextPageItem = true;
                nextPageItem.IconImage = "traktNextPage.png";
                nextPageItem.IconImageBig = "traktNextPage.png";
                nextPageItem.ThumbnailImage = "traktNextPage.png";
                nextPageItem.OnItemSelected += OnNextPageSelected;
                nextPageItem.IsFolder = true;
                Facade.Add(nextPageItem);
                itemId++;
            }

            // Set Facade Layout
            Facade.SetCurrentLayout(Enum.GetName(typeof(Layout), CurrentLayout));
            GUIControl.FocusControl(GetID, Facade.GetID);

            Facade.SelectIndex(PreviousSelectedIndex);

            // set facade properties
            GUIUtils.SetProperty("#itemcount", filteredTrendingList.Count().ToString());
            GUIUtils.SetProperty("#Trakt.Items", string.Format("{0} {1}", filteredTrendingList.Count(), filteredTrendingList.Count() > 1 ? Translation.SeriesPlural : Translation.Series));

            GUIUtils.SetProperty("#Trakt.Trending.PeopleCount", trendingItems.TotalWatchers.ToString());
            GUIUtils.SetProperty("#Trakt.Trending.Description", string.Format(Translation.TrendingTVShowsPeople, trendingItems.TotalWatchers.ToString(), trendingItems.TotalItems.ToString()));

            // Page Properties
            GUIUtils.SetProperty("#Trakt.Facade.CurrentPage", trendingItems.CurrentPage.ToString());
            GUIUtils.SetProperty("#Trakt.Facade.TotalPages", trendingItems.TotalPages.ToString());
            GUIUtils.SetProperty("#Trakt.Facade.TotalItemsPerPage", TraktSettings.MaxTrendingShowsRequest.ToString());

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
            CurrentLayout = (Layout)TraktSettings.TrendingShowsDefaultLayout;

            // Update Button States
            UpdateButtonState();

            if (sortButton != null)
            {
                sortButton.SortChanged += (o, e) =>
                {
                    TraktSettings.SortByTrendingShows.Direction = (SortingDirections)(e.Order - 1);
                    PreviousSelectedIndex = CurrentPage == 1 ? 0 : 1;
                    UpdateButtonState();
                    LoadTrendingShows(CurrentPage);
                };
            }
        }

        private void UpdateButtonState()
        {
            // update layout button label
            GUIControl.SetControlLabel(GetID, layoutButton.GetID, GUICommon.GetLayoutTranslation(CurrentLayout));

            // update sortby button label
            if (sortButton != null)
            {
                sortButton.Label = GUICommon.GetSortByString(TraktSettings.SortByTrendingShows);
                sortButton.IsAscending = (TraktSettings.SortByTrendingShows.Direction == SortingDirections.Ascending);
            }
            GUIUtils.SetProperty("#Trakt.SortBy", GUICommon.GetSortByString(TraktSettings.SortByTrendingShows));

            // update filter buttons
            if (filterWatchedButton != null)
                filterWatchedButton.Selected = TraktSettings.TrendingShowsHideWatched;
            if (filterWatchListedButton != null)
                filterWatchListedButton.Selected = TraktSettings.TrendingShowsHideWatchlisted;
            if (filterCollectedButton != null)
                filterCollectedButton.Selected = TraktSettings.TrendingShowsHideCollected;
            if (filterRatedButton != null)
                filterRatedButton.Selected = TraktSettings.TrendingShowsHideRated;
        }

        private void ClearProperties(bool showsOnly = false)
        {
            if (!showsOnly)
            {
                GUIUtils.SetProperty("#Trakt.Trending.PeopleCount", string.Empty);
                GUIUtils.SetProperty("#Trakt.Trending.Description", string.Empty);
                GUIUtils.SetProperty("#Trakt.Trending.CurrentPage", string.Empty);
                GUIUtils.SetProperty("#Trakt.Trending.TotalPages", string.Empty);
            }

            GUIUtils.SetProperty("#Trakt.Show.Watchers", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.Watchers.Extra", string.Empty);

            GUICommon.ClearShowProperties();
        }

        private void PublishShowSkinProperties(TraktShowTrending trendingItem)
        {
            GUICommon.SetProperty("#Trakt.Show.Watchers", trendingItem.Watchers.ToString());
            GUICommon.SetProperty("#Trakt.Show.Watchers.Extra", trendingItem.Watchers > 1 ? string.Format(Translation.PeopleWatching, trendingItem.Watchers) : Translation.PersonWatching);

            GUICommon.SetShowProperties(trendingItem.Show);
        }

        private void OnShowSelected(GUIListItem item, GUIControl parent)
        {
            GUIUtils.SetProperty("#Trakt.Facade.IsPageItem", false.ToString());

            PreviousSelectedIndex = Facade.SelectedListItemIndex;

            var trendingItem = item.TVTag as TraktShowTrending;
            if (trendingItem == null) return;

            PublishShowSkinProperties(trendingItem);
            GUIImageHandler.LoadFanart(backdrop, trendingItem.Show.Images.Fanart.LocalImageFilename(ArtworkType.ShowFanart));
        }

        private void OnNextPageSelected(GUIListItem item, GUIControl control)
        {
            GUIUtils.SetProperty("#Trakt.Facade.IsPageItem", true.ToString());
            GUIUtils.SetProperty("#Trakt.Facade.PageToLoad", (CurrentPage + 1).ToString());

            backdrop.Filename = string.Empty;
            PreviousSelectedIndex = Facade.SelectedListItemIndex;

            // only clear the last selected show properties
            ClearProperties(true);
        }

        private void OnPreviousPageSelected(GUIListItem item, GUIControl control)
        {
            GUIUtils.SetProperty("#Trakt.Facade.IsPageItem", true.ToString());
            GUIUtils.SetProperty("#Trakt.Facade.PageToLoad", (CurrentPage - 1).ToString());

            backdrop.Filename = string.Empty;
            PreviousSelectedIndex = Facade.SelectedListItemIndex;

            // only clear the last selected show properties
            ClearProperties(true);
        }

        #endregion
    }
}
