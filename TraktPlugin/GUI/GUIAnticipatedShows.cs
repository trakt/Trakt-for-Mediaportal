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
    public class GUIAnticipatedShows : GUIWindow
    {
        #region Skin Controls

        [SkinControl(2)]
        protected GUIButtonControl layoutButton = null;

        [SkinControl(8)]
        protected GUISortButtonControl sortButton = null;

        [SkinControl(10)]
        protected GUICheckButton filterWatchListedButton = null;

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

        public GUIAnticipatedShows()
        {
            backdrop = new ImageSwapper();
            backdrop.PropertyOne = "#Trakt.AnticipatedShows.Fanart.1";
            backdrop.PropertyTwo = "#Trakt.AnticipatedShows.Fanart.2";
        }

        #endregion

        #region Private Variables

        private Dictionary<int, TraktShowsAnticipated> AnticipatedShowPages = null;
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
                return (int)TraktGUIWindows.AnticipatedShows;
            }
        }

        public override bool Init()
        {
            return Load(GUIGraphicsContext.Skin + @"\Trakt.Anticipated.Shows.xml");
        }

        protected override void OnPageLoad()
        {
            base.OnPageLoad();

            // Clear GUI Properties
            ClearProperties();

            // Init Properties
            InitProperties();

            // Load Anticipated Shows
            LoadAnticipatedShows(CurrentPage);
        }

        protected override void OnPageDestroy(int new_windowId)
        {
            GUIShowListItem.StopDownload = true;
            PreviousSelectedIndex = Facade.SelectedListItemIndex;
            ClearProperties();

            // save current layout
            TraktSettings.AnticipatedShowsDefaultLayout = (int)CurrentLayout;

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
                            LoadAnticipatedShows(CurrentPage);
                        }

                    }
                    break;

                // Layout Button
                case (2):
                    CurrentLayout = GUICommon.ShowLayoutMenu(CurrentLayout, PreviousSelectedIndex);
                    break;

                // Sort Button
                case (8):
                    var newSortBy = GUICommon.ShowSortMenu(TraktSettings.SortByAnticipatedShows);
                    if (newSortBy != null)
                    {
                        if (newSortBy.Field != TraktSettings.SortByAnticipatedShows.Field)
                        {
                            TraktSettings.SortByAnticipatedShows = newSortBy;
                            PreviousSelectedIndex = CurrentPage == 1 ? 0 : 1;
                            UpdateButtonState();
                            LoadAnticipatedShows(CurrentPage);
                        }
                    }
                    break;

                // Hide Watchlisted
                case (10):
                    PreviousSelectedIndex = CurrentPage == 1 ? 0 : 1;
                    TraktSettings.AnticipatedShowsHideWatchlisted = !TraktSettings.AnticipatedShowsHideWatchlisted;
                    UpdateButtonState();
                    LoadAnticipatedShows(CurrentPage);
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

            var selectedAnticipatedItem = selectedItem.TVTag as TraktShowAnticipated;
            if (selectedAnticipatedItem == null) return;

            var dlg = (IDialogbox)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
            if (dlg == null) return;

            dlg.Reset();
            dlg.SetHeading(GUIUtils.PluginName());

            GUICommon.CreateShowsContextMenu(ref dlg, selectedAnticipatedItem.Show, false);

            // Show Context Menu
            dlg.DoModal(GUIWindowManager.ActiveWindow);
            if (dlg.SelectedId < 0) return;

            switch (dlg.SelectedId)
            {
                case ((int)MediaContextMenuItem.AddToWatchList):
                    TraktHelper.AddShowToWatchList(selectedAnticipatedItem.Show);
                    OnShowSelected(selectedItem, Facade);
                    (Facade.SelectedListItem as GUIShowListItem).Images.NotifyPropertyChanged("Poster");
                    if (TraktSettings.AnticipatedShowsHideWatchlisted) LoadAnticipatedShows(CurrentPage);
                    break;

                case ((int)MediaContextMenuItem.ShowSeasonInfo):
                    GUIWindowManager.ActivateWindow((int)TraktGUIWindows.ShowSeasons, selectedAnticipatedItem.Show.ToJSON());
                    break;

                case ((int)MediaContextMenuItem.MarkAsWatched):
                    GUICommon.MarkShowAsWatched(selectedAnticipatedItem.Show);
                    LoadAnticipatedShows(CurrentPage);
                    break;

                case ((int)MediaContextMenuItem.AddToLibrary):
                    GUICommon.AddShowToCollection(selectedAnticipatedItem.Show);
                    LoadAnticipatedShows(CurrentPage);
                    break;

                case ((int)MediaContextMenuItem.RemoveFromWatchList):
                    TraktHelper.RemoveShowFromWatchList(selectedAnticipatedItem.Show);
                    OnShowSelected(selectedItem, Facade);
                    (Facade.SelectedListItem as GUIShowListItem).Images.NotifyPropertyChanged("Poster");
                    break;

                case ((int)MediaContextMenuItem.AddToList):
                    TraktHelper.AddRemoveShowInUserList(selectedAnticipatedItem.Show, false);
                    break;

                case ((int)MediaContextMenuItem.Filters):
                    if (GUICommon.ShowTVShowFiltersMenu())
                    {
                        PreviousSelectedIndex = CurrentPage == 1 ? 0 : 1;
                        UpdateButtonState();
                        LoadAnticipatedShows(CurrentPage);
                    }
                    break;

                case ((int)MediaContextMenuItem.Related):
                    TraktHelper.ShowRelatedShows(selectedAnticipatedItem.Show);
                    break;

                case ((int)MediaContextMenuItem.Cast):
                    GUICreditsShow.Show = selectedAnticipatedItem.Show;
                    GUICreditsShow.Type = GUICreditsShow.CreditType.Cast;
                    GUICreditsShow.Fanart = TmdbCache.GetShowBackdropFilename(selectedItem.Images.ShowImages);
                    GUIWindowManager.ActivateWindow((int)TraktGUIWindows.CreditsShow);
                    break;

                case ((int)MediaContextMenuItem.Crew):
                    GUICreditsShow.Show = selectedAnticipatedItem.Show;
                    GUICreditsShow.Type = GUICreditsShow.CreditType.Crew;
                    GUICreditsShow.Fanart = TmdbCache.GetShowBackdropFilename(selectedItem.Images.ShowImages);
                    GUIWindowManager.ActivateWindow((int)TraktGUIWindows.CreditsShow);
                    break;

                case ((int)MediaContextMenuItem.Trailers):
                    GUICommon.ShowTVShowTrailersMenu(selectedAnticipatedItem.Show);
                    break;

                case ((int)MediaContextMenuItem.Shouts):
                    TraktHelper.ShowTVShowShouts(selectedAnticipatedItem.Show);
                    break;

                case ((int)MediaContextMenuItem.Rate):
                    GUICommon.RateShow(selectedAnticipatedItem.Show);
                    OnShowSelected(selectedItem, Facade);
                    (Facade.SelectedListItem as GUIShowListItem).Images.NotifyPropertyChanged("Poster");
                    LoadAnticipatedShows(CurrentPage);
                    break;

                case ((int)MediaContextMenuItem.ChangeLayout):
                    CurrentLayout = GUICommon.ShowLayoutMenu(CurrentLayout, PreviousSelectedIndex);
                    break;

                case ((int)MediaContextMenuItem.SearchWithMpNZB):
                    string loadingParam = string.Format("search:{0}", selectedAnticipatedItem.Show.Title);
                    GUIWindowManager.ActivateWindow((int)ExternalPluginWindows.MpNZB, loadingParam);
                    break;

                case ((int)MediaContextMenuItem.SearchTorrent):
                    string loadPar = selectedAnticipatedItem.Show.Title;
                    GUIWindowManager.ActivateWindow((int)ExternalPluginWindows.MyTorrents, loadPar);
                    break;

                default:
                    break;
            }

            base.OnShowContextMenu();
        }

        #endregion

        #region Private Methods

        TraktShowsAnticipated GetAnticipatedShows(int page)
        {
            TraktShowsAnticipated AnticipatedShows = null;

            if (AnticipatedShowPages == null || LastRequest < DateTime.UtcNow.Subtract(new TimeSpan(0, TraktSettings.WebRequestCacheMinutes, 0)))
            {
                // get the first page
                AnticipatedShows = TraktAPI.TraktAPI.GetAnticipatedShows(1, TraktSettings.MaxAnticipatedShowsRequest);

                // reset to defaults
                LastRequest = DateTime.UtcNow;
                CurrentPage = 1;
                PreviousSelectedIndex = 0;

                // clear the cache
                if (AnticipatedShowPages == null)
                    AnticipatedShowPages = new Dictionary<int, TraktShowsAnticipated>();
                else
                    AnticipatedShowPages.Clear();

                // add page to cache
                AnticipatedShowPages.Add(1, AnticipatedShows);
            }
            else
            {
                // get page from cache if it exists
                if (AnticipatedShowPages.TryGetValue(page, out AnticipatedShows))
                {
                    return AnticipatedShows;
                }

                // request next page
                AnticipatedShows = TraktAPI.TraktAPI.GetAnticipatedShows(page, TraktSettings.MaxAnticipatedShowsRequest);
                if (AnticipatedShows != null && AnticipatedShows.Shows != null)
                {
                    // add to cache
                    AnticipatedShowPages.Add(page, AnticipatedShows);
                }
            }
            return AnticipatedShows;
        }

        private void CheckAndPlayEpisode(bool jumpTo)
        {
            var selectedItem = this.Facade.SelectedListItem;
            if (selectedItem == null) return;

            var selectedAnticipatedItem = selectedItem.TVTag as TraktShowAnticipated;
            if (selectedAnticipatedItem == null) return;

            GUICommon.CheckAndPlayFirstUnwatchedEpisode(selectedAnticipatedItem.Show, jumpTo);
        }

        private void LoadAnticipatedShows(int page = 1)
        {
            GUIUtils.SetProperty("#Trakt.Items", string.Empty);

            GUIBackgroundTask.Instance.ExecuteInBackgroundAndCallback(() =>
            {
                return GetAnticipatedShows(page);
            },
            delegate(bool success, object result)
            {
                if (success)
                {
                    var shows = result as TraktShowsAnticipated;
                    SendAnticipatedShowsToFacade(shows);
                }
            }, Translation.GettingAnticipatedShows, true);
        }

        private void SendAnticipatedShowsToFacade(TraktShowsAnticipated anticipatedItems)
        {
            // clear facade
            GUIControl.ClearControl(GetID, Facade.GetID);

            if (anticipatedItems == null)
            {
                GUIUtils.ShowNotifyDialog(Translation.Error, Translation.ErrorGeneral);
                GUIWindowManager.ShowPreviousWindow();
                AnticipatedShowPages = null;
                return;
            }
            
            // filter shows
            var filteredAnticipatedList = FilterAnticipatedShows(anticipatedItems.Shows).Where(s => !string.IsNullOrEmpty(s.Show.Title)).ToList();

            // sort shows
            filteredAnticipatedList.Sort(new GUIListItemShowSorter(TraktSettings.SortByAnticipatedShows.Field, TraktSettings.SortByAnticipatedShows.Direction));

            int itemId = 0;
            var showImages = new List<GUITmdbImage>();

            // Add Previous Page Button
            if (anticipatedItems.CurrentPage != 1)
            {
                var prevPageItem = new GUIShowListItem(Translation.PreviousPage, (int)TraktGUIWindows.AnticipatedShows);
                prevPageItem.IsPrevPageItem = true;
                prevPageItem.IconImage = "traktPreviousPage.png";
                prevPageItem.IconImageBig = "traktPreviousPage.png";
                prevPageItem.ThumbnailImage = "traktPreviousPage.png";
                prevPageItem.OnItemSelected += OnPreviousPageSelected;
                prevPageItem.IsFolder = true;
                Facade.Add(prevPageItem);
                itemId++;
            }

            foreach (var anticipatedItem in filteredAnticipatedList)
            {
                var item = new GUIShowListItem(anticipatedItem.Show.Title, (int)TraktGUIWindows.AnticipatedShows);

                // add image for download
                var image = new GUITmdbImage { ShowImages = new TmdbShowImages { Id = anticipatedItem.Show.Ids.Tmdb } };
                showImages.Add(image);

                item.Label2 = anticipatedItem.Show.Year.ToString();
                item.TVTag = anticipatedItem;
                item.Show = anticipatedItem.Show;
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
            if (anticipatedItems.CurrentPage != anticipatedItems.TotalPages)
            {
                var nextPageItem = new GUIShowListItem(Translation.NextPage, (int)TraktGUIWindows.AnticipatedShows);
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
            GUIUtils.SetProperty("#itemcount", filteredAnticipatedList.Count().ToString());
            GUIUtils.SetProperty("#Trakt.Items", string.Format("{0} {1}", filteredAnticipatedList.Count(), filteredAnticipatedList.Count() > 1 ? Translation.SeriesPlural : Translation.Series));

            // Page Properties
            GUIUtils.SetProperty("#Trakt.Facade.CurrentPage", anticipatedItems.CurrentPage.ToString());
            GUIUtils.SetProperty("#Trakt.Facade.TotalPages", anticipatedItems.TotalPages.ToString());
            GUIUtils.SetProperty("#Trakt.Facade.TotalItemsPerPage", TraktSettings.MaxAnticipatedShowsRequest.ToString());

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
            CurrentLayout = (Layout)TraktSettings.AnticipatedShowsDefaultLayout;

            // Update Button States
            UpdateButtonState();

            if (sortButton != null)
            {
                sortButton.SortChanged += (o, e) =>
                {
                    TraktSettings.SortByAnticipatedShows.Direction = (SortingDirections)(e.Order - 1);
                    PreviousSelectedIndex = CurrentPage == 1 ? 0 : 1;
                    UpdateButtonState();
                    LoadAnticipatedShows(CurrentPage);
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
                sortButton.Label = GUICommon.GetSortByString(TraktSettings.SortByAnticipatedShows);
                sortButton.IsAscending = (TraktSettings.SortByAnticipatedShows.Direction == SortingDirections.Ascending);
            }
            GUIUtils.SetProperty("#Trakt.SortBy", GUICommon.GetSortByString(TraktSettings.SortByAnticipatedShows));

            // update filter buttons
            if (filterWatchListedButton != null)
                filterWatchListedButton.Selected = TraktSettings.AnticipatedShowsHideWatchlisted;
        }

        private void ClearProperties(bool showsOnly = false)
        {
            if (!showsOnly)
            {
                GUIUtils.SetProperty("#Trakt.Anticipated.CurrentPage", string.Empty);
                GUIUtils.SetProperty("#Trakt.Anticipated.TotalPages", string.Empty);
            }

            GUIUtils.SetProperty("#Trakt.Show.ListCount", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.ListCount.Extra", string.Empty);

            GUICommon.ClearShowProperties();
        }

        private void PublishShowSkinProperties(TraktShowAnticipated anticipatedItem)
        {
            GUICommon.SetProperty("#Trakt.Show.ListCount", anticipatedItem.ListCount.ToString());
            GUICommon.SetProperty("#Trakt.Show.ListCount.Extra", string.Format(Translation.AppearsInList, anticipatedItem.ListCount));

            GUICommon.SetShowProperties(anticipatedItem.Show);
        }

        private void OnShowSelected(GUIListItem item, GUIControl parent)
        {
            GUIUtils.SetProperty("#Trakt.Facade.IsPageItem", false.ToString());

            PreviousSelectedIndex = Facade.SelectedListItemIndex;

            var anticipatedItem = item.TVTag as TraktShowAnticipated;
            if (anticipatedItem == null) return;

            PublishShowSkinProperties(anticipatedItem);
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

        private IEnumerable<TraktShowAnticipated> FilterAnticipatedShows(IEnumerable<TraktShowAnticipated> showsToFilter)
        {
            if (TraktSettings.AnticipatedShowsHideWatchlisted)
                showsToFilter = showsToFilter.Where(a => !a.Show.IsWatchlisted());

            return showsToFilter;
        }

        #endregion
    }
}
