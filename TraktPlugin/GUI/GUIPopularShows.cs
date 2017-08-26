using System;
using System.Collections.Generic;
using System.Linq;
using MediaPortal.GUI.Library;
using MediaPortal.Util;
using TraktAPI.DataStructures;
using TraktAPI.Extensions;
using TraktPlugin.Cache;
using TraktPlugin.TmdbAPI.DataStructures;
using Action = MediaPortal.GUI.Library.Action;

namespace TraktPlugin.GUI
{
    public class GUIPopularShows : GUIWindow
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

        public GUIPopularShows()
        {
            backdrop = new ImageSwapper();
            backdrop.PropertyOne = "#Trakt.PopularShows.Fanart.1";
            backdrop.PropertyTwo = "#Trakt.PopularShows.Fanart.2";
        }

        #endregion

        #region Private Variables

        private Dictionary<int, TraktShowsPopular> PopularShowPages = null;
        private GUIFacadeControl.Layout CurrentLayout { get; set; }
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
                return (int)TraktGUIWindows.PopularShows;
            }
        }

        public override bool Init()
        {
            return Load(GUIGraphicsContext.Skin + @"\Trakt.Popular.Shows.xml");
        }

        protected override void OnPageLoad()
        {
            base.OnPageLoad();

            // Clear GUI Properties
            ClearProperties();

            // Init Properties
            InitProperties();

            // Load Popular Shows
            LoadPopularShows(CurrentPage);
        }

        protected override void OnPageDestroy(int new_windowId)
        {
            GUIShowListItem.StopDownload = true;
            PreviousSelectedIndex = Facade.SelectedListItemIndex;
            ClearProperties();

            // save current layout
            TraktSettings.PopularShowsDefaultLayout = (int)CurrentLayout;

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
                            LoadPopularShows(CurrentPage);
                        }

                    }
                    break;

                // Layout Button
                case (2):
                    CurrentLayout = GUICommon.ShowLayoutMenu(CurrentLayout, PreviousSelectedIndex);
                    break;

                // Sort Button
                case (8):
                    var newSortBy = GUICommon.ShowSortMenu(TraktSettings.SortByPopularShows);
                    if (newSortBy != null)
                    {
                        if (newSortBy.Field != TraktSettings.SortByPopularShows.Field)
                        {
                            TraktSettings.SortByPopularShows = newSortBy;
                            PreviousSelectedIndex = CurrentPage == 1 ? 0 : 1;
                            UpdateButtonState();
                            LoadPopularShows(CurrentPage);
                        }
                    }
                    break;

                // Hide Watched
                case (9):
                    PreviousSelectedIndex = CurrentPage == 1 ? 0 : 1;
                    TraktSettings.PopularShowsHideWatched = !TraktSettings.PopularShowsHideWatched;
                    UpdateButtonState();
                    LoadPopularShows(CurrentPage);
                    break;

                // Hide Watchlisted
                case (10):
                    PreviousSelectedIndex = CurrentPage == 1 ? 0 : 1;
                    TraktSettings.PopularShowsHideWatchlisted = !TraktSettings.PopularShowsHideWatchlisted;
                    UpdateButtonState();
                    LoadPopularShows(CurrentPage);
                    break;

                // Hide Collected
                case (11):
                    PreviousSelectedIndex = CurrentPage == 1 ? 0 : 1;
                    TraktSettings.PopularShowsHideCollected = !TraktSettings.PopularShowsHideCollected;
                    UpdateButtonState();
                    LoadPopularShows(CurrentPage);
                    break;

                // Hide Rated
                case (12):
                    PreviousSelectedIndex = CurrentPage == 1 ? 0 : 1;
                    TraktSettings.PopularShowsHideRated = !TraktSettings.PopularShowsHideRated;
                    UpdateButtonState();
                    LoadPopularShows(CurrentPage);
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
            var selectedItem = this.Facade.SelectedListItem as GUIShowListItem;
            if (selectedItem == null) return;

            var selectedPopularItem = selectedItem.TVTag as TraktShowSummary;
            if (selectedPopularItem == null) return;

            var dlg = (IDialogbox)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
            if (dlg == null) return;

            dlg.Reset();
            dlg.SetHeading(GUIUtils.PluginName());

            GUICommon.CreateShowsContextMenu(ref dlg, selectedPopularItem, false);

            // Show Context Menu
            dlg.DoModal(GUIWindowManager.ActiveWindow);
            if (dlg.SelectedId < 0) return;

            switch (dlg.SelectedId)
            {
                case ((int)MediaContextMenuItem.AddToWatchList):
                    TraktHelper.AddShowToWatchList(selectedPopularItem);
                    OnShowSelected(selectedItem, Facade);
                    (Facade.SelectedListItem as GUIShowListItem).Images.NotifyPropertyChanged("Poster");
                    if (TraktSettings.PopularShowsHideWatchlisted) LoadPopularShows(CurrentPage);
                    break;

                case ((int)MediaContextMenuItem.ShowSeasonInfo):
                    GUIWindowManager.ActivateWindow((int)TraktGUIWindows.ShowSeasons, selectedPopularItem.ToJSON());
                    break;

                case ((int)MediaContextMenuItem.MarkAsWatched):
                    GUICommon.MarkShowAsWatched(selectedPopularItem);
                    if (TraktSettings.PopularShowsHideWatched) LoadPopularShows(CurrentPage);
                    break;

                case ((int)MediaContextMenuItem.AddToLibrary):
                    GUICommon.AddShowToCollection(selectedPopularItem);
                    if (TraktSettings.PopularShowsHideCollected) LoadPopularShows(CurrentPage);
                    break;

                case ((int)MediaContextMenuItem.RemoveFromWatchList):
                    TraktHelper.RemoveShowFromWatchList(selectedPopularItem);
                    OnShowSelected(selectedItem, Facade);
                    (Facade.SelectedListItem as GUIShowListItem).Images.NotifyPropertyChanged("Poster");
                    break;

                case ((int)MediaContextMenuItem.AddToList):
                    TraktHelper.AddRemoveShowInUserList(selectedPopularItem, false);
                    break;

                case ((int)MediaContextMenuItem.Filters):
                    if (ShowTVShowFiltersMenu())
                    {
                        PreviousSelectedIndex = CurrentPage == 1 ? 0 : 1;
                        UpdateButtonState();
                        LoadPopularShows(CurrentPage);
                    }
                    break;

                case ((int)MediaContextMenuItem.Related):
                    TraktHelper.ShowRelatedShows(selectedPopularItem);
                    break;

                case ((int)MediaContextMenuItem.Trailers):
                    GUICommon.ShowTVShowTrailersMenu(selectedPopularItem);
                    break;

                case ((int)MediaContextMenuItem.Shouts):
                    TraktHelper.ShowTVShowShouts(selectedPopularItem);
                    break;

                case ((int)MediaContextMenuItem.Rate):
                    GUICommon.RateShow(selectedPopularItem);
                    OnShowSelected(selectedItem, Facade);
                    (Facade.SelectedListItem as GUIShowListItem).Images.NotifyPropertyChanged("Poster");
                    if (TraktSettings.PopularShowsHideRated) LoadPopularShows(CurrentPage);
                    break;

                case ((int)MediaContextMenuItem.Cast):
                    GUICreditsShow.Show = selectedPopularItem;
                    GUICreditsShow.Type = GUICreditsShow.CreditType.Cast;
                    GUICreditsShow.Fanart = TmdbCache.GetShowBackdropFilename(selectedItem.Images.ShowImages);
                    GUIWindowManager.ActivateWindow((int)TraktGUIWindows.CreditsShow);
                    break;

                case ((int)MediaContextMenuItem.Crew):
                    GUICreditsShow.Show = selectedPopularItem;
                    GUICreditsShow.Type = GUICreditsShow.CreditType.Crew;
                    GUICreditsShow.Fanart = TmdbCache.GetShowBackdropFilename(selectedItem.Images.ShowImages);
                    GUIWindowManager.ActivateWindow((int)TraktGUIWindows.CreditsShow);
                    break;

                case ((int)MediaContextMenuItem.ChangeLayout):
                    CurrentLayout = GUICommon.ShowLayoutMenu(CurrentLayout, PreviousSelectedIndex);
                    break;

                case ((int)MediaContextMenuItem.SearchWithMpNZB):
                    string loadingParam = string.Format("search:{0}", selectedPopularItem.Title);
                    GUIWindowManager.ActivateWindow((int)ExternalPluginWindows.MpNZB, loadingParam);
                    break;

                case ((int)MediaContextMenuItem.SearchTorrent):
                    string loadPar = selectedPopularItem.Title;
                    GUIWindowManager.ActivateWindow((int)ExternalPluginWindows.MyTorrents, loadPar);
                    break;

                default:
                    break;
            }

            base.OnShowContextMenu();
        }

        #endregion

        #region Private Methods

        TraktShowsPopular GetPopularShows(int page)
        {
            TraktShowsPopular PopularShows = null;

            if (PopularShowPages == null || LastRequest < DateTime.UtcNow.Subtract(new TimeSpan(0, TraktSettings.WebRequestCacheMinutes, 0)))
            {
                // get the first page
                PopularShows = TraktAPI.TraktAPI.GetPopularShows(1, TraktSettings.MaxPopularShowsRequest);

                // reset to defaults
                LastRequest = DateTime.UtcNow;
                CurrentPage = 1;
                PreviousSelectedIndex = 0;

                // clear the cache
                if (PopularShowPages == null)
                    PopularShowPages = new Dictionary<int, TraktShowsPopular>();
                else
                    PopularShowPages.Clear();

                // add page to cache
                PopularShowPages.Add(1, PopularShows);
            }
            else
            {
                // get page from cache if it exists
                if (PopularShowPages.TryGetValue(page, out PopularShows))
                {
                    return PopularShows;
                }

                // request next page
                PopularShows = TraktAPI.TraktAPI.GetPopularShows(page, TraktSettings.MaxPopularShowsRequest);
                if (PopularShows != null && PopularShows.Shows != null)
                {
                    // add to cache
                    PopularShowPages.Add(page, PopularShows);
                }
            }
            return PopularShows;
        }

        private void CheckAndPlayEpisode(bool jumpTo)
        {
            var selectedItem = this.Facade.SelectedListItem;
            if (selectedItem == null) return;

            var selectedPopularItem = selectedItem.TVTag as TraktShowSummary;
            if (selectedPopularItem == null) return;

            GUICommon.CheckAndPlayFirstUnwatchedEpisode(selectedPopularItem, jumpTo);
        }

        private void LoadPopularShows(int page = 1)
        {
            GUIUtils.SetProperty("#Trakt.Items", string.Empty);

            GUIBackgroundTask.Instance.ExecuteInBackgroundAndCallback(() =>
            {
                return GetPopularShows(page);
            },
            delegate(bool success, object result)
            {
                if (success)
                {
                    var shows = result as TraktShowsPopular;
                    SendPopularShowsToFacade(shows);
                }
            }, Translation.GettingPopularShows, true);
        }

        private void SendPopularShowsToFacade(TraktShowsPopular PopularItems)
        {
            // clear facade
            GUIControl.ClearControl(GetID, Facade.GetID);

            if (PopularItems == null)
            {
                GUIUtils.ShowNotifyDialog(Translation.Error, Translation.ErrorGeneral);
                GUIWindowManager.ShowPreviousWindow();
                PopularShowPages = null;
                return;
            }
            
            // filter shows
            var filteredPopularList = FilterPopularShows(PopularItems.Shows).Where(s => !string.IsNullOrEmpty(s.Title)).ToList();

            // sort shows
            filteredPopularList.Sort(new GUIListItemShowSorter(TraktSettings.SortByPopularShows.Field, TraktSettings.SortByPopularShows.Direction));

            int itemId = 0;
            var showImages = new List<GUITmdbImage>();

            // Add Previous Page Button
            if (PopularItems.CurrentPage != 1)
            {
                var prevPageItem = new GUIShowListItem(Translation.PreviousPage, (int)TraktGUIWindows.PopularShows);
                prevPageItem.IsPrevPageItem = true;
                prevPageItem.IconImage = "traktPreviousPage.png";
                prevPageItem.IconImageBig = "traktPreviousPage.png";
                prevPageItem.ThumbnailImage = "traktPreviousPage.png";
                prevPageItem.OnItemSelected += OnPreviousPageSelected;
                prevPageItem.IsFolder = true;
                Facade.Add(prevPageItem);
                itemId++;
            }

            foreach (var popularItem in filteredPopularList)
            {
                var item = new GUIShowListItem(popularItem.Title, (int)TraktGUIWindows.PopularShows);

                // add image for download
                var image = new GUITmdbImage { ShowImages = new TmdbShowImages { Id = popularItem.Ids.Tmdb } };
                showImages.Add(image);

                item.Label2 = popularItem.Year.ToString();
                item.TVTag = popularItem;
                item.Show = popularItem;
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
            if (PopularItems.CurrentPage != PopularItems.TotalPages)
            {
                var nextPageItem = new GUIShowListItem(Translation.NextPage, (int)TraktGUIWindows.PopularShows);
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
            Facade.CurrentLayout = CurrentLayout;
            GUIControl.FocusControl(GetID, Facade.GetID);

            Facade.SelectIndex(PreviousSelectedIndex);

            // set facade properties
            GUIUtils.SetProperty("#itemcount", filteredPopularList.Count().ToString());
            GUIUtils.SetProperty("#Trakt.Items", string.Format("{0} {1}", filteredPopularList.Count(), filteredPopularList.Count() > 1 ? Translation.SeriesPlural : Translation.Series));

            // Page Properties
            GUIUtils.SetProperty("#Trakt.Facade.CurrentPage", PopularItems.CurrentPage.ToString());
            GUIUtils.SetProperty("#Trakt.Facade.TotalPages", PopularItems.TotalPages.ToString());
            GUIUtils.SetProperty("#Trakt.Facade.TotalItemsPerPage", TraktSettings.MaxPopularShowsRequest.ToString());

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
            CurrentLayout = (GUIFacadeControl.Layout)TraktSettings.PopularShowsDefaultLayout;

            // Update Button States
            UpdateButtonState();

            if (sortButton != null)
            {
                sortButton.SortChanged += (o, e) =>
                {
                    TraktSettings.SortByPopularShows.Direction = (SortingDirections)(e.Order - 1);
                    PreviousSelectedIndex = CurrentPage == 1 ? 0 : 1;
                    UpdateButtonState();
                    LoadPopularShows(CurrentPage);
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
                sortButton.Label = GUICommon.GetSortByString(TraktSettings.SortByPopularShows);
                sortButton.IsAscending = (TraktSettings.SortByPopularShows.Direction == SortingDirections.Ascending);
            }
            GUIUtils.SetProperty("#Trakt.SortBy", GUICommon.GetSortByString(TraktSettings.SortByPopularShows));

            // update filter buttons
            if (filterWatchedButton != null)
                filterWatchedButton.Selected = TraktSettings.PopularShowsHideWatched;
            if (filterWatchListedButton != null)
                filterWatchListedButton.Selected = TraktSettings.PopularShowsHideWatchlisted;
            if (filterCollectedButton != null)
                filterCollectedButton.Selected = TraktSettings.PopularShowsHideCollected;
            if (filterRatedButton != null)
                filterRatedButton.Selected = TraktSettings.PopularShowsHideRated;
        }

        private void ClearProperties(bool showsOnly = false)
        {
            if (!showsOnly)
            {
                GUIUtils.SetProperty("#Trakt.Popular.PeopleCount", string.Empty);
                GUIUtils.SetProperty("#Trakt.Popular.Description", string.Empty);
                GUIUtils.SetProperty("#Trakt.Popular.CurrentPage", string.Empty);
                GUIUtils.SetProperty("#Trakt.Popular.TotalPages", string.Empty);
            }

            GUIUtils.SetProperty("#Trakt.Show.Watchers", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.Watchers.Extra", string.Empty);

            GUICommon.ClearShowProperties();
        }

        private void PublishShowSkinProperties(TraktShowSummary PopularItem)
        {
            GUICommon.SetShowProperties(PopularItem);
        }

        private void OnShowSelected(GUIListItem item, GUIControl parent)
        {
            GUIUtils.SetProperty("#Trakt.Facade.IsPageItem", false.ToString());

            PreviousSelectedIndex = Facade.SelectedListItemIndex;

            var PopularItem = item.TVTag as TraktShowSummary;
            if (PopularItem == null) return;

            PublishShowSkinProperties(PopularItem);
            GUIImageHandler.LoadFanart(backdrop, TmdbCache.GetShowBackdropFilename((item as GUIShowListItem).Images.ShowImages));
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

        private bool ShowTVShowFiltersMenu()
        {
            var filters = new Dictionary<Filters, bool>();

            filters.Add(Filters.Watched, TraktSettings.PopularShowsHideWatched);
            filters.Add(Filters.Watchlisted, TraktSettings.PopularShowsHideWatchlisted);
            filters.Add(Filters.Collected, TraktSettings.PopularShowsHideCollected);
            filters.Add(Filters.Rated, TraktSettings.PopularShowsHideRated);

            var selectedItems = GUIUtils.ShowMultiSelectionDialog(Translation.Filters, GUICommon.GetFilterListItems(filters));
            if (selectedItems == null) return false;

            foreach (var item in selectedItems.Where(l => l.Selected == true))
            {
                // toggle state of all selected items
                switch ((Filters)Enum.Parse(typeof(Filters), item.ItemID, true))
                {
                    case Filters.Watched:
                        TraktSettings.PopularShowsHideWatched = !TraktSettings.PopularShowsHideWatched;
                        break;
                    case Filters.Watchlisted:
                        TraktSettings.PopularShowsHideWatchlisted = !TraktSettings.PopularShowsHideWatchlisted;
                        break;
                    case Filters.Collected:
                        TraktSettings.PopularShowsHideCollected = !TraktSettings.PopularShowsHideCollected;
                        break;
                    case Filters.Rated:
                        TraktSettings.PopularShowsHideRated = !TraktSettings.PopularShowsHideRated;
                        break;
                }
            }

            return true;
        }

        private IEnumerable<TraktShowSummary> FilterPopularShows(IEnumerable<TraktShowSummary> showsToFilter)
        {
            if (TraktSettings.PopularShowsHideWatched)
                showsToFilter = showsToFilter.Where(s => !s.IsWatched());

            if (TraktSettings.PopularShowsHideWatchlisted)
                showsToFilter = showsToFilter.Where(s => !s.IsWatchlisted());

            if (TraktSettings.PopularShowsHideCollected)
                showsToFilter = showsToFilter.Where(s => !s.IsCollected());

            if (TraktSettings.PopularShowsHideRated)
                showsToFilter = showsToFilter.Where(s => s.UserRating() == null);

            return showsToFilter;
        }

        #endregion
    }
}
