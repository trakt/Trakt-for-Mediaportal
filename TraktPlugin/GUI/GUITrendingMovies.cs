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
    public class GUITrendingMovies : GUIWindow
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

        public GUITrendingMovies() 
        {
            backdrop = new ImageSwapper();
            backdrop.PropertyOne = "#Trakt.TrendingMovies.Fanart.1";
            backdrop.PropertyTwo = "#Trakt.TrendingMovies.Fanart.2";
        }

        #endregion

        #region Private Variables

        private Dictionary<int, TraktMoviesTrending> TrendingMoviePages = null;
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
                return (int)TraktGUIWindows.TrendingMovies;
            }
        }

        public override bool Init()
        {
            return Load(GUIGraphicsContext.Skin + @"\Trakt.Trending.Movies.xml");
        }

        protected override void OnPageLoad()
        {
            base.OnPageLoad();

            // Clear GUI Properties
            ClearProperties();

            // Init Properties
            InitProperties();

            // Load Trending Movies
            LoadTrendingMovies(CurrentPage);
        }

        protected override void OnPageDestroy(int new_windowId)
        {
            GUIMovieListItem.StopDownload = true;
            PreviousSelectedIndex = Facade.SelectedListItemIndex;
            ClearProperties();

            // save current layout
            TraktSettings.TrendingMoviesDefaultLayout = (int)CurrentLayout;            

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
                            LoadTrendingMovies(CurrentPage);
                        }
                    }
                    break;

                // Layout Button
                case (2):
                    CurrentLayout = GUICommon.ShowLayoutMenu(CurrentLayout, PreviousSelectedIndex);
                    break;

                // Sort Button
                case (8):
                    var newSortBy = GUICommon.ShowSortMenu(TraktSettings.SortByTrendingMovies);
                    if (newSortBy != null)
                    {
                        if (newSortBy.Field != TraktSettings.SortByTrendingMovies.Field)
                        {
                            TraktSettings.SortByTrendingMovies = newSortBy;
                            PreviousSelectedIndex = CurrentPage == 1 ? 0 : 1;
                            UpdateButtonState();
                            LoadTrendingMovies(CurrentPage);
                        }
                    }
                    break;

                // Hide Watched
                case (9):
                    PreviousSelectedIndex = CurrentPage == 1 ? 0 : 1;
                    TraktSettings.TrendingMoviesHideWatched = !TraktSettings.TrendingMoviesHideWatched;
                    UpdateButtonState();
                    LoadTrendingMovies(CurrentPage);
                    break;

                // Hide Watchlisted
                case (10):
                    PreviousSelectedIndex = CurrentPage == 1 ? 0 : 1;
                    TraktSettings.TrendingMoviesHideWatchlisted = !TraktSettings.TrendingMoviesHideWatchlisted;
                    UpdateButtonState();
                    LoadTrendingMovies(CurrentPage);
                    break;

                // Hide Collected
                case (11):
                    PreviousSelectedIndex = CurrentPage == 1 ? 0 : 1;
                    TraktSettings.TrendingMoviesHideCollected = !TraktSettings.TrendingMoviesHideCollected;
                    UpdateButtonState();
                    LoadTrendingMovies(CurrentPage);
                    break;

                // Hide Rated
                case (12):
                    PreviousSelectedIndex = CurrentPage == 1 ? 0 : 1;
                    TraktSettings.TrendingMoviesHideRated = !TraktSettings.TrendingMoviesHideRated;
                    UpdateButtonState();
                    LoadTrendingMovies(CurrentPage);
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
            
            var selectedTrendingItem = selectedItem.TVTag as TraktMovieTrending;
            if (selectedTrendingItem == null) return;

            var dlg = (IDialogbox)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
            if (dlg == null) return;

            dlg.Reset();
            dlg.SetHeading(GUIUtils.PluginName());

            GUICommon.CreateMoviesContextMenu(ref dlg, selectedTrendingItem.Movie, false);

            // Show Context Menu
            dlg.DoModal(GUIWindowManager.ActiveWindow);
            if (dlg.SelectedId < 0) return;

            switch (dlg.SelectedId)
            {
                case ((int)MediaContextMenuItem.MarkAsWatched):
                    TraktHelper.AddMovieToWatchHistory(selectedTrendingItem.Movie);
                    selectedItem.IsPlayed = true;
                    OnMovieSelected(selectedItem, Facade);
                    (Facade.SelectedListItem as GUIMovieListItem).Images.NotifyPropertyChanged("Poster");
                    if (TraktSettings.TrendingMoviesHideWatched) LoadTrendingMovies(CurrentPage);
                    break;

                case ((int)MediaContextMenuItem.MarkAsUnWatched):
                    TraktHelper.RemoveMovieFromWatchHistory(selectedTrendingItem.Movie);
                    selectedItem.IsPlayed = false;
                    OnMovieSelected(selectedItem, Facade);
                    (Facade.SelectedListItem as GUIMovieListItem).Images.NotifyPropertyChanged("Poster");
                    break;

                case ((int)MediaContextMenuItem.AddToWatchList):
                    TraktHelper.AddMovieToWatchList(selectedTrendingItem.Movie, true);
                    OnMovieSelected(selectedItem, Facade);
                    (Facade.SelectedListItem as GUIMovieListItem).Images.NotifyPropertyChanged("Poster");
                    if (TraktSettings.TrendingMoviesHideWatchlisted) LoadTrendingMovies(CurrentPage);
                    break;

                case ((int)MediaContextMenuItem.RemoveFromWatchList):
                    TraktHelper.RemoveMovieFromWatchList(selectedTrendingItem.Movie, true);
                    OnMovieSelected(selectedItem, Facade);
                    (Facade.SelectedListItem as GUIMovieListItem).Images.NotifyPropertyChanged("Poster");
                    break;

                case ((int)MediaContextMenuItem.AddToList):
                    TraktHelper.AddRemoveMovieInUserList(selectedTrendingItem.Movie, false);                    
                    break;

                case ((int)MediaContextMenuItem.AddToLibrary):
                    TraktHelper.AddMovieToCollection(selectedTrendingItem.Movie);
                    OnMovieSelected(selectedItem, Facade);
                    (Facade.SelectedListItem as GUIMovieListItem).Images.NotifyPropertyChanged("Poster");
                    if (TraktSettings.TrendingMoviesHideCollected) LoadTrendingMovies(CurrentPage);
                    break;

                case ((int)MediaContextMenuItem.RemoveFromLibrary):
                    TraktHelper.RemoveMovieFromCollection(selectedTrendingItem.Movie);
                    OnMovieSelected(selectedItem, Facade);
                    (Facade.SelectedListItem as GUIMovieListItem).Images.NotifyPropertyChanged("Poster");
                    break;

                case ((int)MediaContextMenuItem.Related):
                    TraktHelper.ShowRelatedMovies(selectedTrendingItem.Movie);
                    break;

                case ((int)MediaContextMenuItem.Rate):
                    GUICommon.RateMovie(selectedTrendingItem.Movie);
                    OnMovieSelected(selectedItem, Facade);
                    (Facade.SelectedListItem as GUIMovieListItem).Images.NotifyPropertyChanged("Poster");
                    if (TraktSettings.TrendingMoviesHideRated) LoadTrendingMovies(CurrentPage);
                    break;

                case ((int)MediaContextMenuItem.Filters):
                    if (GUICommon.ShowMovieFiltersMenu())
                    {
                        PreviousSelectedIndex = CurrentPage == 1 ? 0 : 1;
                        UpdateButtonState();
                        LoadTrendingMovies(CurrentPage);
                    }
                    break;

                case ((int)MediaContextMenuItem.Shouts):
                    TraktHelper.ShowMovieShouts(selectedTrendingItem.Movie);
                    break;

                case ((int)MediaContextMenuItem.Cast):
                    GUICreditsMovie.Movie = selectedTrendingItem.Movie;
                    GUICreditsMovie.Type = GUICreditsMovie.CreditType.Cast;
                    GUICreditsMovie.Fanart = TmdbCache.GetMovieBackdropFilename(selectedItem.Images.MovieImages);
                    GUIWindowManager.ActivateWindow((int)TraktGUIWindows.CreditsMovie);
                    break;

                case ((int)MediaContextMenuItem.Crew):
                    GUICreditsMovie.Movie = selectedTrendingItem.Movie;
                    GUICreditsMovie.Type = GUICreditsMovie.CreditType.Crew;
                    GUICreditsMovie.Fanart = TmdbCache.GetMovieBackdropFilename(selectedItem.Images.MovieImages);
                    GUIWindowManager.ActivateWindow((int)TraktGUIWindows.CreditsMovie);
                    break;

                case ((int)MediaContextMenuItem.Trailers):
                    GUICommon.ShowMovieTrailersMenu(selectedTrendingItem.Movie);
                    break;

                case ((int)MediaContextMenuItem.ChangeLayout):
                    CurrentLayout = GUICommon.ShowLayoutMenu(CurrentLayout, PreviousSelectedIndex);
                    break;

                case ((int)MediaContextMenuItem.SearchWithMpNZB):
                    string loadingParam = string.Format("search:{0}", selectedTrendingItem.Movie.Title);
                    GUIWindowManager.ActivateWindow((int)ExternalPluginWindows.MpNZB, loadingParam);
                    break;

                case ((int)MediaContextMenuItem.SearchTorrent):
                    string loadPar = selectedTrendingItem.Movie.Title;
                    GUIWindowManager.ActivateWindow((int)ExternalPluginWindows.MyTorrents, loadPar);
                    break;

                default:
                    break;
            }
            
            base.OnShowContextMenu();
        }

        #endregion

        #region Private Methods

        TraktMoviesTrending GetTrendingMovies(int page)
        {
            TraktMoviesTrending trendingMovies = null;

            if (TrendingMoviePages == null || LastRequest < DateTime.UtcNow.Subtract(new TimeSpan(0, TraktSettings.WebRequestCacheMinutes, 0)))
            {
                // get the first page
                trendingMovies = TraktAPI.TraktAPI.GetTrendingMovies(1, TraktSettings.MaxTrendingMoviesRequest);
                
                // reset to defaults
                LastRequest = DateTime.UtcNow;
                CurrentPage = 1;
                PreviousSelectedIndex = 0;

                // clear the cache
                if (TrendingMoviePages == null)
                    TrendingMoviePages = new Dictionary<int, TraktMoviesTrending>();
                else
                    TrendingMoviePages.Clear();

                // add page to cache
                TrendingMoviePages.Add(1, trendingMovies);
            }
            else
            {
                // get page from cache if it exists
                if (TrendingMoviePages.TryGetValue(page, out trendingMovies))
                {
                    return trendingMovies;
                }

                // request next page
                trendingMovies = TraktAPI.TraktAPI.GetTrendingMovies(page, TraktSettings.MaxTrendingMoviesRequest);
                if (trendingMovies != null && trendingMovies.Movies != null)
                {
                    // add to cache
                    TrendingMoviePages.Add(page, trendingMovies);
                }
            }
            return trendingMovies;
        }

        private void CheckAndPlayMovie(bool jumpTo)
        {
            var selectedItem = this.Facade.SelectedListItem;
            if (selectedItem == null) return;

            var selectedTrendingItem = selectedItem.TVTag as TraktMovieTrending;
            if (selectedTrendingItem == null) return;

            GUICommon.CheckAndPlayMovie(jumpTo, selectedTrendingItem.Movie);
        }

        private void LoadTrendingMovies(int page = 1)
        {
            GUIUtils.SetProperty("#Trakt.Items", string.Empty);

            GUIBackgroundTask.Instance.ExecuteInBackgroundAndCallback(() =>
            {
                return GetTrendingMovies(page);
            },
            delegate(bool success, object result)
            {
                if (success)
                {
                    var movies = result as TraktMoviesTrending;
                    SendTrendingMoviesToFacade(movies);
                }
            }, Translation.GettingTrendingMovies, true);
        }

        private void SendTrendingMoviesToFacade(TraktMoviesTrending trendingItems)
        {
            // clear facade
            GUIControl.ClearControl(GetID, Facade.GetID);

            if (trendingItems == null)
            {
                GUIUtils.ShowNotifyDialog(Translation.Error, Translation.ErrorGeneral);
                GUIWindowManager.ShowPreviousWindow();
                TrendingMoviePages = null;
                return;
            }

            if (trendingItems.Movies.Count() == 0)
            {
                GUIUtils.ShowNotifyDialog(GUIUtils.PluginName(), Translation.NoTrendingMovies);
                GUIWindowManager.ShowPreviousWindow();
                TrendingMoviePages = null;
                return;
            }

            // filter movies
            var filteredTrendingList = GUICommon.FilterTrendingMovies(trendingItems.Movies).Where(m => !string.IsNullOrEmpty(m.Movie.Title)).ToList();

            // sort movies
            filteredTrendingList.Sort(new GUIListItemMovieSorter(TraktSettings.SortByTrendingMovies.Field, TraktSettings.SortByTrendingMovies.Direction));

            int itemId = 0;
            var movieImages = new List<GUITmdbImage>();

            // Add Previous Page Button
            if (trendingItems.CurrentPage != 1)
            {
                var prevPageItem = new GUIMovieListItem(Translation.PreviousPage, (int)TraktGUIWindows.TrendingMovies);
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
            foreach (var trendingItem in filteredTrendingList)
            {
                // add image for download
                var images = new GUITmdbImage { MovieImages = new TmdbMovieImages { Id = trendingItem.Movie.Ids.Tmdb } };
                movieImages.Add(images);

                var item = new GUIMovieListItem(trendingItem.Movie.Title, (int)TraktGUIWindows.TrendingMovies);

                item.Label2 = trendingItem.Movie.Year == null ? "----" : trendingItem.Movie.Year.ToString();
                item.TVTag = trendingItem;
                item.Movie = trendingItem.Movie;
                item.Images = images;
                item.IsPlayed = trendingItem.Movie.IsWatched();
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
            if (trendingItems.CurrentPage != trendingItems.TotalPages)
            {
                var nextPageItem = new GUIMovieListItem(Translation.NextPage, (int)TraktGUIWindows.TrendingMovies);
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
            GUIUtils.SetProperty("#itemcount", filteredTrendingList.Count().ToString());
            GUIUtils.SetProperty("#Trakt.Items", string.Format("{0} {1}", filteredTrendingList.Count(), filteredTrendingList.Count() > 1 ? Translation.Movies : Translation.Movie));
            
            // set global trending properties
            GUIUtils.SetProperty("#Trakt.Trending.PeopleCount", trendingItems.TotalWatchers.ToString());
            GUIUtils.SetProperty("#Trakt.Trending.Description", string.Format(Translation.TrendingMoviePeople, trendingItems.TotalWatchers.ToString(), trendingItems.TotalItems.ToString()));

            // Page Properties
            GUIUtils.SetProperty("#Trakt.Facade.CurrentPage", trendingItems.CurrentPage.ToString());
            GUIUtils.SetProperty("#Trakt.Facade.TotalPages", trendingItems.TotalPages.ToString());
            GUIUtils.SetProperty("#Trakt.Facade.TotalItemsPerPage", TraktSettings.MaxTrendingMoviesRequest.ToString());

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
            CurrentLayout = (GUIFacadeControl.Layout)TraktSettings.TrendingMoviesDefaultLayout;
            
            // Update Button States
            UpdateButtonState();

            if (sortButton != null)
            {
                UpdateButtonState();
                sortButton.SortChanged += (o, e) =>
                {
                    TraktSettings.SortByTrendingMovies.Direction = (SortingDirections)(e.Order - 1);
                    PreviousSelectedIndex = CurrentPage == 1 ? 0 : 1;
                    UpdateButtonState();
                    LoadTrendingMovies(CurrentPage);
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
                sortButton.Label = GUICommon.GetSortByString(TraktSettings.SortByTrendingMovies);
                sortButton.IsAscending = (TraktSettings.SortByTrendingMovies.Direction == SortingDirections.Ascending);
            }
            GUIUtils.SetProperty("#Trakt.SortBy", GUICommon.GetSortByString(TraktSettings.SortByTrendingMovies));

            // update filter buttons
            if (filterWatchedButton != null)
                filterWatchedButton.Selected = TraktSettings.TrendingMoviesHideWatched;
            if (filterWatchListedButton != null)
                filterWatchListedButton.Selected = TraktSettings.TrendingMoviesHideWatchlisted;
            if (filterCollectedButton != null)
                filterCollectedButton.Selected = TraktSettings.TrendingMoviesHideCollected;
            if (filterRatedButton != null)
                filterRatedButton.Selected = TraktSettings.TrendingMoviesHideRated;
        }

        private void ClearProperties(bool moviesOnly = false)
        {
            if (!moviesOnly)
            {
                GUIUtils.SetProperty("#Trakt.Trending.PeopleCount", string.Empty);
                GUIUtils.SetProperty("#Trakt.Trending.Description", string.Empty);
                GUIUtils.SetProperty("#Trakt.Trending.CurrentPage", string.Empty);
                GUIUtils.SetProperty("#Trakt.Trending.TotalPages", string.Empty);
                GUIUtils.SetProperty("#Trakt.Facade.IsPageItem", string.Empty);
            }

            GUIUtils.SetProperty("#Trakt.Movie.Watchers", string.Empty);
            GUIUtils.SetProperty("#Trakt.Movie.Watchers.Extra", string.Empty);

            GUICommon.ClearMovieProperties();
        }

        private void PublishMovieSkinProperties(TraktMovieTrending trendingItem)
        {
            GUICommon.SetProperty("#Trakt.Movie.Watchers", trendingItem.Watchers.ToString());
            GUICommon.SetProperty("#Trakt.Movie.Watchers.Extra", trendingItem.Watchers > 1 ? string.Format(Translation.PeopleWatching, trendingItem.Watchers) : Translation.PersonWatching);

            GUICommon.SetMovieProperties(trendingItem.Movie);
        }

        private void OnMovieSelected(GUIListItem item, GUIControl control)
        {
            GUIUtils.SetProperty("#Trakt.Facade.IsPageItem", false.ToString());

            PreviousSelectedIndex = Facade.SelectedListItemIndex;

            var trendingItem = item.TVTag as TraktMovieTrending;
            if (trendingItem == null) return;

            PublishMovieSkinProperties(trendingItem);
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

        #endregion
    }
}
