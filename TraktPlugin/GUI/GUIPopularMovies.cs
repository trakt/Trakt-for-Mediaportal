using System;
using System.Collections.Generic;
using System.Linq;
using MediaPortal.GUI.Library;
using MediaPortal.Util;
using TraktPlugin.Cache;
using TraktPlugin.TmdbAPI.DataStructures;
using TraktAPI.DataStructures;
using Action = MediaPortal.GUI.Library.Action;

namespace TraktPlugin.GUI
{
    public class GUIPopularMovies : GUIWindow
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

        public GUIPopularMovies()
        {
            backdrop = new ImageSwapper();
            backdrop.PropertyOne = "#Trakt.PopularMovies.Fanart.1";
            backdrop.PropertyTwo = "#Trakt.PopularMovies.Fanart.2";
        }

        #endregion

        #region Private Variables

        private Dictionary<int, TraktMoviesPopular> PopularMoviePages = null;
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
                return (int)TraktGUIWindows.PopularMovies;
            }
        }

        public override bool Init()
        {
            return Load(GUIGraphicsContext.Skin + @"\Trakt.Popular.Movies.xml");
        }

        protected override void OnPageLoad()
        {
            base.OnPageLoad();

            // Clear GUI Properties
            ClearProperties();

            // Init Properties
            InitProperties();

            // Load Popular Movies
            LoadPopularMovies(CurrentPage);
        }

        protected override void OnPageDestroy(int new_windowId)
        {
            GUIMovieListItem.StopDownload = true;
            PreviousSelectedIndex = Facade.SelectedListItemIndex;
            ClearProperties();

            // save current layout
            TraktSettings.PopularMoviesDefaultLayout = (int)CurrentLayout;

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
                        var item = Facade.SelectedListItem as GUIMovieListItem;
                        if (item == null) return;

                        if (!item.IsFolder)
                        {
                            CheckAndPlayMovie(true);
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
                            LoadPopularMovies(CurrentPage);
                        }
                    }
                    break;

                // Layout Button
                case (2):
                    CurrentLayout = GUICommon.ShowLayoutMenu(CurrentLayout, PreviousSelectedIndex);
                    break;

                // Sort Button
                case (8):
                    var newSortBy = GUICommon.ShowSortMenu(TraktSettings.SortByPopularMovies);
                    if (newSortBy != null)
                    {
                        if (newSortBy.Field != TraktSettings.SortByPopularMovies.Field)
                        {
                            TraktSettings.SortByPopularMovies = newSortBy;
                            PreviousSelectedIndex = CurrentPage == 1 ? 0 : 1;
                            UpdateButtonState();
                            LoadPopularMovies(CurrentPage);
                        }
                    }
                    break;

                // Hide Watched
                case (9):
                    PreviousSelectedIndex = CurrentPage == 1 ? 0 : 1;
                    TraktSettings.PopularMoviesHideWatched = !TraktSettings.PopularMoviesHideWatched;
                    UpdateButtonState();
                    LoadPopularMovies(CurrentPage);
                    break;

                // Hide Watchlisted
                case (10):
                    PreviousSelectedIndex = CurrentPage == 1 ? 0 : 1;
                    TraktSettings.PopularMoviesHideWatchlisted = !TraktSettings.PopularMoviesHideWatchlisted;
                    UpdateButtonState();
                    LoadPopularMovies(CurrentPage);
                    break;

                // Hide Collected
                case (11):
                    PreviousSelectedIndex = CurrentPage == 1 ? 0 : 1;
                    TraktSettings.PopularMoviesHideCollected = !TraktSettings.PopularMoviesHideCollected;
                    UpdateButtonState();
                    LoadPopularMovies(CurrentPage);
                    break;

                // Hide Rated
                case (12):
                    PreviousSelectedIndex = CurrentPage == 1 ? 0 : 1;
                    TraktSettings.PopularMoviesHideRated = !TraktSettings.PopularMoviesHideRated;
                    UpdateButtonState();
                    LoadPopularMovies(CurrentPage);
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
                    CheckAndPlayMovie(false);
                    break;
                default:
                    base.OnAction(action);
                    break;
            }
        }

        protected override void OnShowContextMenu()
        {
            var selectedItem = this.Facade.SelectedListItem as GUIMovieListItem;
            if (selectedItem == null) return;

            var selectedPopularMovie = selectedItem.TVTag as TraktMovieSummary;
            if (selectedPopularMovie == null) return;

            var dlg = (IDialogbox)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
            if (dlg == null) return;

            dlg.Reset();
            dlg.SetHeading(GUIUtils.PluginName());

            GUICommon.CreateMoviesContextMenu(ref dlg, selectedPopularMovie, false);

            // Show Context Menu
            dlg.DoModal(GUIWindowManager.ActiveWindow);
            if (dlg.SelectedId < 0) return;

            switch (dlg.SelectedId)
            {
                case ((int)MediaContextMenuItem.MarkAsWatched):
                    TraktHelper.AddMovieToWatchHistory(selectedPopularMovie);
                    selectedItem.IsPlayed = true;
                    OnMovieSelected(selectedItem, Facade);
                    (Facade.SelectedListItem as GUIMovieListItem).Images.NotifyPropertyChanged("Poster");
                    if (TraktSettings.PopularMoviesHideWatched) LoadPopularMovies(CurrentPage);
                    break;

                case ((int)MediaContextMenuItem.MarkAsUnWatched):
                    TraktHelper.RemoveMovieFromWatchHistory(selectedPopularMovie);
                    selectedItem.IsPlayed = false;
                    OnMovieSelected(selectedItem, Facade);
                    (Facade.SelectedListItem as GUIMovieListItem).Images.NotifyPropertyChanged("Poster");
                    break;

                case ((int)MediaContextMenuItem.AddToWatchList):
                    TraktHelper.AddMovieToWatchList(selectedPopularMovie, true);
                    OnMovieSelected(selectedItem, Facade);
                    (Facade.SelectedListItem as GUIMovieListItem).Images.NotifyPropertyChanged("Poster");
                    if (TraktSettings.PopularMoviesHideWatchlisted) LoadPopularMovies(CurrentPage);
                    break;

                case ((int)MediaContextMenuItem.RemoveFromWatchList):
                    TraktHelper.RemoveMovieFromWatchList(selectedPopularMovie, true);
                    OnMovieSelected(selectedItem, Facade);
                    (Facade.SelectedListItem as GUIMovieListItem).Images.NotifyPropertyChanged("Poster");
                    break;

                case ((int)MediaContextMenuItem.AddToList):
                    TraktHelper.AddRemoveMovieInUserList(selectedPopularMovie, false);
                    break;

                case ((int)MediaContextMenuItem.AddToLibrary):
                    TraktHelper.AddMovieToCollection(selectedPopularMovie);
                    OnMovieSelected(selectedItem, Facade);
                    (Facade.SelectedListItem as GUIMovieListItem).Images.NotifyPropertyChanged("Poster");
                    if (TraktSettings.PopularMoviesHideCollected) LoadPopularMovies(CurrentPage);
                    break;

                case ((int)MediaContextMenuItem.RemoveFromLibrary):
                    TraktHelper.RemoveMovieFromCollection(selectedPopularMovie);
                    OnMovieSelected(selectedItem, Facade);
                    (Facade.SelectedListItem as GUIMovieListItem).Images.NotifyPropertyChanged("Poster");
                    break;

                case ((int)MediaContextMenuItem.Related):
                    TraktHelper.ShowRelatedMovies(selectedPopularMovie);
                    break;

                case ((int)MediaContextMenuItem.Rate):
                    GUICommon.RateMovie(selectedPopularMovie);
                    OnMovieSelected(selectedItem, Facade);
                    (Facade.SelectedListItem as GUIMovieListItem).Images.NotifyPropertyChanged("Poster");
                    if (TraktSettings.PopularMoviesHideRated) LoadPopularMovies(CurrentPage);
                    break;

                case ((int)MediaContextMenuItem.Filters):
                    if (ShowMovieFiltersMenu())
                    {
                        PreviousSelectedIndex = CurrentPage == 1 ? 0 : 1;
                        UpdateButtonState();
                        LoadPopularMovies(CurrentPage);
                    }
                    break;

                case ((int)MediaContextMenuItem.Shouts):
                    TraktHelper.ShowMovieShouts(selectedPopularMovie);
                    break;

                case ((int)MediaContextMenuItem.Cast):
                    GUICreditsMovie.Movie = selectedPopularMovie;
                    GUICreditsMovie.Type = GUICreditsMovie.CreditType.Cast;
                    GUICreditsMovie.Fanart = TmdbCache.GetMovieBackdropFilename(selectedItem.Images.MovieImages);
                    GUIWindowManager.ActivateWindow((int)TraktGUIWindows.CreditsMovie);
                    break;

                case ((int)MediaContextMenuItem.Crew):
                    GUICreditsMovie.Movie = selectedPopularMovie;
                    GUICreditsMovie.Type = GUICreditsMovie.CreditType.Crew;
                    GUICreditsMovie.Fanart = TmdbCache.GetMovieBackdropFilename(selectedItem.Images.MovieImages);
                    GUIWindowManager.ActivateWindow((int)TraktGUIWindows.CreditsMovie);
                    break;

                case ((int)MediaContextMenuItem.Trailers):
                    GUICommon.ShowMovieTrailersMenu(selectedPopularMovie);
                    break;

                case ((int)MediaContextMenuItem.ChangeLayout):
                    CurrentLayout = GUICommon.ShowLayoutMenu(CurrentLayout, PreviousSelectedIndex);
                    break;

                case ((int)MediaContextMenuItem.SearchWithMpNZB):
                    string loadingParam = string.Format("search:{0}", selectedPopularMovie.Title);
                    GUIWindowManager.ActivateWindow((int)ExternalPluginWindows.MpNZB, loadingParam);
                    break;

                case ((int)MediaContextMenuItem.SearchTorrent):
                    string loadParm = selectedPopularMovie.Title;
                    GUIWindowManager.ActivateWindow((int)ExternalPluginWindows.MyTorrents, loadParm);
                    break;

                default:
                    break;
            }

            base.OnShowContextMenu();
        }

        #endregion

        #region Private Methods

        TraktMoviesPopular GetPopularMovies(int page)
        {
            TraktMoviesPopular PopularMovies = null;

            if (PopularMoviePages == null || LastRequest < DateTime.UtcNow.Subtract(new TimeSpan(0, TraktSettings.WebRequestCacheMinutes, 0)))
            {
                // get the first page
                PopularMovies = TraktAPI.TraktAPI.GetPopularMovies(1, TraktSettings.MaxPopularMoviesRequest);

                // reset to defaults
                LastRequest = DateTime.UtcNow;
                CurrentPage = 1;
                PreviousSelectedIndex = 0;

                // clear the cache
                if (PopularMoviePages == null)
                    PopularMoviePages = new Dictionary<int, TraktMoviesPopular>();
                else
                    PopularMoviePages.Clear();

                // add page to cache
                PopularMoviePages.Add(1, PopularMovies);
            }
            else
            {
                // get page from cache if it exists
                if (PopularMoviePages.TryGetValue(page, out PopularMovies))
                {
                    return PopularMovies;
                }

                // request next page
                PopularMovies = TraktAPI.TraktAPI.GetPopularMovies(page, TraktSettings.MaxPopularMoviesRequest);
                if (PopularMovies != null && PopularMovies.Movies != null)
                {
                    // add to cache
                    PopularMoviePages.Add(page, PopularMovies);
                }
            }
            return PopularMovies;
        }

        private void CheckAndPlayMovie(bool jumpTo)
        {
            var selectedItem = this.Facade.SelectedListItem;
            if (selectedItem == null) return;

            var selectedPopularItem = selectedItem.TVTag as TraktMovieSummary;
            if (selectedPopularItem == null) return;

            GUICommon.CheckAndPlayMovie(jumpTo, selectedPopularItem);
        }

        private void LoadPopularMovies(int page = 1)
        {
            GUIUtils.SetProperty("#Trakt.Items", string.Empty);

            GUIBackgroundTask.Instance.ExecuteInBackgroundAndCallback(() =>
            {
                return GetPopularMovies(page);
            },
            delegate(bool success, object result)
            {
                if (success)
                {
                    var movies = result as TraktMoviesPopular;
                    SendPopularMoviesToFacade(movies);
                }
            }, Translation.GettingPopularMovies, true);
        }

        private void SendPopularMoviesToFacade(TraktMoviesPopular PopularItems)
        {
            // clear facade
            GUIControl.ClearControl(GetID, Facade.GetID);

            if (PopularItems == null)
            {
                GUIUtils.ShowNotifyDialog(Translation.Error, Translation.ErrorGeneral);
                GUIWindowManager.ShowPreviousWindow();
                PopularMoviePages = null;
                return;
            }
            
            // filter movies
            var filteredPopularList = FilterPopularMovies(PopularItems.Movies).Where(m => !string.IsNullOrEmpty(m.Title)).ToList();

            // sort movies
            filteredPopularList.Sort(new GUIListItemMovieSorter(TraktSettings.SortByPopularMovies.Field, TraktSettings.SortByPopularMovies.Direction));

            int itemId = 0;
            var movieImages = new List<GUITmdbImage>();

            // Add Previous Page Button
            if (PopularItems.CurrentPage != 1)
            {
                var prevPageItem = new GUIMovieListItem(Translation.PreviousPage, (int)TraktGUIWindows.PopularMovies);
                prevPageItem.IsPrevPageItem = true;
                prevPageItem.IconImage = "traktPreviousPage.png";
                prevPageItem.IconImageBig = "traktPreviousPage.png";
                prevPageItem.ThumbnailImage = "traktPreviousPage.png";
                prevPageItem.OnItemSelected += OnPreviousPageSelected;
                prevPageItem.IsFolder = true;
                Facade.Add(prevPageItem);
                itemId++;
            }

            // Add each movie mark remote if not in collection            
            foreach (var PopularItem in filteredPopularList)
            {
                // add image for download
                var images = new GUITmdbImage { MovieImages = new TmdbMovieImages { Id = PopularItem.Ids.Tmdb } };
                movieImages.Add(images);

                var item = new GUIMovieListItem(PopularItem.Title, (int)TraktGUIWindows.PopularMovies);

                item.Label2 = PopularItem.Year == null ? "----" : PopularItem.Year.ToString();
                item.TVTag = PopularItem;
                item.Movie = PopularItem;
                item.Images = images;
                item.IsPlayed = PopularItem.IsWatched();
                item.ItemId = Int32.MaxValue - itemId;
                item.IconImage = GUIImageHandler.GetDefaultPoster(false);
                item.IconImageBig = GUIImageHandler.GetDefaultPoster();
                item.ThumbnailImage = GUIImageHandler.GetDefaultPoster();
                item.OnItemSelected += OnMovieSelected;
                Utils.SetDefaultIcons(item);
                Facade.Add(item);
                itemId++;
            }

            // Add Next Page Button
            if (PopularItems.CurrentPage != PopularItems.TotalPages)
            {
                var nextPageItem = new GUIMovieListItem(Translation.NextPage, (int)TraktGUIWindows.PopularMovies);
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
            GUIUtils.SetProperty("#Trakt.Items", string.Format("{0} {1}", filteredPopularList.Count(), filteredPopularList.Count() > 1 ? Translation.Movies : Translation.Movie));

            // Page Properties
            GUIUtils.SetProperty("#Trakt.Facade.CurrentPage", PopularItems.CurrentPage.ToString());
            GUIUtils.SetProperty("#Trakt.Facade.TotalPages", PopularItems.TotalPages.ToString());
            GUIUtils.SetProperty("#Trakt.Facade.TotalItemsPerPage", TraktSettings.MaxPopularMoviesRequest.ToString());

            // Download movie images Async and set to facade
            GUIMovieListItem.GetImages(movieImages);
        }

        private void InitProperties()
        {
            // Fanart
            backdrop.GUIImageOne = FanartBackground;
            backdrop.GUIImageTwo = FanartBackground2;
            backdrop.LoadingImage = loadingImage;

            // load last layout
            CurrentLayout = (GUIFacadeControl.Layout)TraktSettings.PopularMoviesDefaultLayout;

            // Update Button States
            UpdateButtonState();

            if (sortButton != null)
            {
                UpdateButtonState();
                sortButton.SortChanged += (o, e) =>
                {
                    TraktSettings.SortByPopularMovies.Direction = (SortingDirections)(e.Order - 1);
                    PreviousSelectedIndex = CurrentPage == 1 ? 0 : 1;
                    UpdateButtonState();
                    LoadPopularMovies(CurrentPage);
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
                sortButton.Label = GUICommon.GetSortByString(TraktSettings.SortByPopularMovies);
                sortButton.IsAscending = (TraktSettings.SortByPopularMovies.Direction == SortingDirections.Ascending);
            }
            GUIUtils.SetProperty("#Trakt.SortBy", GUICommon.GetSortByString(TraktSettings.SortByPopularMovies));

            // update filter buttons
            if (filterWatchedButton != null)
                filterWatchedButton.Selected = TraktSettings.PopularMoviesHideWatched;
            if (filterWatchListedButton != null)
                filterWatchListedButton.Selected = TraktSettings.PopularMoviesHideWatchlisted;
            if (filterCollectedButton != null)
                filterCollectedButton.Selected = TraktSettings.PopularMoviesHideCollected;
            if (filterRatedButton != null)
                filterRatedButton.Selected = TraktSettings.PopularMoviesHideRated;
        }

        private void ClearProperties(bool moviesOnly = false)
        {
            if (!moviesOnly)
            {
                GUIUtils.SetProperty("#Trakt.Popular.CurrentPage", string.Empty);
                GUIUtils.SetProperty("#Trakt.Popular.TotalPages", string.Empty);
            }

            GUICommon.ClearMovieProperties();
        }

        private void PublishMovieSkinProperties(TraktMovieSummary PopularItem)
        {
            GUICommon.SetMovieProperties(PopularItem);
        }

        private void OnMovieSelected(GUIListItem item, GUIControl control)
        {
            GUIUtils.SetProperty("#Trakt.Facade.IsPageItem", false.ToString());

            PreviousSelectedIndex = Facade.SelectedListItemIndex;

            var PopularItem = item.TVTag as TraktMovieSummary;
            if (PopularItem == null) return;

            PublishMovieSkinProperties(PopularItem);
            GUIImageHandler.LoadFanart(backdrop, TmdbCache.GetMovieBackdropFilename((item as GUIMovieListItem).Images.MovieImages));
        }

        private void OnNextPageSelected(GUIListItem item, GUIControl control)
        {
            GUIUtils.SetProperty("#Trakt.Facade.IsPageItem", true.ToString());
            GUIUtils.SetProperty("#Trakt.Facade.PageToLoad", (CurrentPage + 1).ToString());

            backdrop.Filename = string.Empty;
            PreviousSelectedIndex = Facade.SelectedListItemIndex;

            // only clear the last selected movie properties
            ClearProperties(true);
        }

        private void OnPreviousPageSelected(GUIListItem item, GUIControl control)
        {
            GUIUtils.SetProperty("#Trakt.Facade.IsPageItem", true.ToString());
            GUIUtils.SetProperty("#Trakt.Facade.PageToLoad", (CurrentPage - 1).ToString());

            backdrop.Filename = string.Empty;
            PreviousSelectedIndex = Facade.SelectedListItemIndex;

            // only clear the last selected movie properties
            ClearProperties(true);
        }

        #region Filters
        
        private bool ShowMovieFiltersMenu()
        {
            var filters = new Dictionary<Filters, bool>();

            filters.Add(Filters.Watched, TraktSettings.PopularMoviesHideWatched);
            filters.Add(Filters.Watchlisted, TraktSettings.PopularMoviesHideWatchlisted);
            filters.Add(Filters.Collected, TraktSettings.PopularMoviesHideCollected);
            filters.Add(Filters.Rated, TraktSettings.PopularMoviesHideRated);

            var selectedItems = GUIUtils.ShowMultiSelectionDialog(Translation.Filters, GUICommon.GetFilterListItems(filters));
            if (selectedItems == null) return false;

            foreach (var item in selectedItems.Where(l => l.Selected == true))
            {
                // toggle state of all selected items
                switch ((Filters)Enum.Parse(typeof(Filters), item.ItemID, true))
                {
                    case Filters.Watched:
                        TraktSettings.PopularMoviesHideWatched = !TraktSettings.PopularMoviesHideWatched;
                        break;
                    case Filters.Watchlisted:
                        TraktSettings.PopularMoviesHideWatchlisted = !TraktSettings.PopularMoviesHideWatchlisted;
                        break;
                    case Filters.Collected:
                        TraktSettings.PopularMoviesHideCollected = !TraktSettings.PopularMoviesHideCollected;
                        break;
                    case Filters.Rated:
                        TraktSettings.PopularMoviesHideRated = !TraktSettings.PopularMoviesHideRated;
                        break;
                }
            }

            return true;
        }

        private IEnumerable<TraktMovieSummary> FilterPopularMovies(IEnumerable<TraktMovieSummary> moviesToFilter)
        {
            if (TraktSettings.PopularMoviesHideWatched)
                moviesToFilter = moviesToFilter.Where(m => !m.IsWatched());

            if (TraktSettings.PopularMoviesHideWatchlisted)
                moviesToFilter = moviesToFilter.Where(m => !m.IsWatchlisted());

            if (TraktSettings.PopularMoviesHideCollected)
                moviesToFilter = moviesToFilter.Where(m => !m.IsCollected());

            if (TraktSettings.PopularMoviesHideRated)
                moviesToFilter = moviesToFilter.Where(m => m.UserRating() == null);

            return moviesToFilter;
        }

        #endregion

        #endregion
    }
}
