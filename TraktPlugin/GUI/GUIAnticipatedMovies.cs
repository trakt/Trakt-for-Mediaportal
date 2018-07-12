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
    public class GUIAnticipatedMovies : GUIWindow
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

        public GUIAnticipatedMovies()
        {
            backdrop = new ImageSwapper();
            backdrop.PropertyOne = "#Trakt.AnticipatedMovies.Fanart.1";
            backdrop.PropertyTwo = "#Trakt.AnticipatedMovies.Fanart.2";
        }

        #endregion

        #region Private Variables

        private Dictionary<int, TraktMoviesAnticipated> AnticipatedMoviePages = null;
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
                return (int)TraktGUIWindows.AnticipatedMovies;
            }
        }

        public override bool Init()
        {
            return Load(GUIGraphicsContext.Skin + @"\Trakt.Anticipated.Movies.xml");
        }

        protected override void OnPageLoad()
        {
            base.OnPageLoad();

            // Clear GUI Properties
            ClearProperties();

            // Init Properties
            InitProperties();

            // Load Anticipated Movies
            LoadAnticipatedMovies(CurrentPage);
        }

        protected override void OnPageDestroy(int new_windowId)
        {
            GUIMovieListItem.StopDownload = true;
            PreviousSelectedIndex = Facade.SelectedListItemIndex;
            ClearProperties();

            // save current layout
            TraktSettings.AnticipatedMoviesDefaultLayout = (int)CurrentLayout;

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
                            LoadAnticipatedMovies(CurrentPage);
                        }
                    }
                    break;

                // Layout Button
                case (2):
                    CurrentLayout = GUICommon.ShowLayoutMenu(CurrentLayout, PreviousSelectedIndex);
                    break;

                // Sort Button
                case (8):
                    var newSortBy = GUICommon.ShowSortMenu(TraktSettings.SortByAnticipatedMovies);
                    if (newSortBy != null)
                    {
                        if (newSortBy.Field != TraktSettings.SortByAnticipatedMovies.Field)
                        {
                            TraktSettings.SortByAnticipatedMovies = newSortBy;
                            PreviousSelectedIndex = CurrentPage == 1 ? 0 : 1;
                            UpdateButtonState();
                            LoadAnticipatedMovies(CurrentPage);
                        }
                    }
                    break;

                // Hide Watchlisted
                case (10):
                    PreviousSelectedIndex = CurrentPage == 1 ? 0 : 1;
                    TraktSettings.AnticipatedMoviesHideWatchlisted = !TraktSettings.AnticipatedMoviesHideWatchlisted;
                    UpdateButtonState();
                    LoadAnticipatedMovies(CurrentPage);
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

            var selectedAnticipatedItem = selectedItem.TVTag as TraktMovieAnticipated;
            if (selectedAnticipatedItem == null) return;

            var dlg = (IDialogbox)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
            if (dlg == null) return;

            dlg.Reset();
            dlg.SetHeading(GUIUtils.PluginName());

            GUICommon.CreateMoviesContextMenu(ref dlg, selectedAnticipatedItem.Movie, false);

            // Show Context Menu
            dlg.DoModal(GUIWindowManager.ActiveWindow);
            if (dlg.SelectedId < 0) return;

            switch (dlg.SelectedId)
            {
                case ((int)MediaContextMenuItem.MarkAsWatched):
                    TraktHelper.AddMovieToWatchHistory(selectedAnticipatedItem.Movie);
                    selectedItem.IsPlayed = true;
                    OnMovieSelected(selectedItem, Facade);
                    (Facade.SelectedListItem as GUIMovieListItem).Images.NotifyPropertyChanged("Poster");
                    LoadAnticipatedMovies(CurrentPage);
                    break;

                case ((int)MediaContextMenuItem.MarkAsUnWatched):
                    TraktHelper.RemoveMovieFromWatchHistory(selectedAnticipatedItem.Movie);
                    selectedItem.IsPlayed = false;
                    OnMovieSelected(selectedItem, Facade);
                    (Facade.SelectedListItem as GUIMovieListItem).Images.NotifyPropertyChanged("Poster");
                    break;

                case ((int)MediaContextMenuItem.AddToWatchList):
                    TraktHelper.AddMovieToWatchList(selectedAnticipatedItem.Movie, true);
                    OnMovieSelected(selectedItem, Facade);
                    (Facade.SelectedListItem as GUIMovieListItem).Images.NotifyPropertyChanged("Poster");
                    if (TraktSettings.AnticipatedMoviesHideWatchlisted) LoadAnticipatedMovies(CurrentPage);
                    break;

                case ((int)MediaContextMenuItem.RemoveFromWatchList):
                    TraktHelper.RemoveMovieFromWatchList(selectedAnticipatedItem.Movie, true);
                    OnMovieSelected(selectedItem, Facade);
                    (Facade.SelectedListItem as GUIMovieListItem).Images.NotifyPropertyChanged("Poster");
                    break;

                case ((int)MediaContextMenuItem.AddToList):
                    TraktHelper.AddRemoveMovieInUserList(selectedAnticipatedItem.Movie, false);
                    break;

                case ((int)MediaContextMenuItem.AddToLibrary):
                    TraktHelper.AddMovieToCollection(selectedAnticipatedItem.Movie);
                    OnMovieSelected(selectedItem, Facade);
                    (Facade.SelectedListItem as GUIMovieListItem).Images.NotifyPropertyChanged("Poster");
                    LoadAnticipatedMovies(CurrentPage);
                    break;

                case ((int)MediaContextMenuItem.RemoveFromLibrary):
                    TraktHelper.RemoveMovieFromCollection(selectedAnticipatedItem.Movie);
                    OnMovieSelected(selectedItem, Facade);
                    (Facade.SelectedListItem as GUIMovieListItem).Images.NotifyPropertyChanged("Poster");
                    break;

                case ((int)MediaContextMenuItem.Related):
                    TraktHelper.ShowRelatedMovies(selectedAnticipatedItem.Movie);
                    break;

                case ((int)MediaContextMenuItem.Rate):
                    GUICommon.RateMovie(selectedAnticipatedItem.Movie);
                    OnMovieSelected(selectedItem, Facade);
                    (Facade.SelectedListItem as GUIMovieListItem).Images.NotifyPropertyChanged("Poster");
                    LoadAnticipatedMovies(CurrentPage);
                    break;

                case ((int)MediaContextMenuItem.Filters):
                    if (GUICommon.ShowMovieFiltersMenu())
                    {
                        PreviousSelectedIndex = CurrentPage == 1 ? 0 : 1;
                        UpdateButtonState();
                        LoadAnticipatedMovies(CurrentPage);
                    }
                    break;

                case ((int)MediaContextMenuItem.Shouts):
                    TraktHelper.ShowMovieShouts(selectedAnticipatedItem.Movie);
                    break;

                case ((int)MediaContextMenuItem.Cast):
                    GUICreditsMovie.Movie = selectedAnticipatedItem.Movie;
                    GUICreditsMovie.Type = GUICreditsMovie.CreditType.Cast;
                    GUICreditsMovie.Fanart = TmdbCache.GetMovieBackdropFilename(selectedItem.Images.MovieImages);
                    GUIWindowManager.ActivateWindow((int)TraktGUIWindows.CreditsMovie);
                    break;

                case ((int)MediaContextMenuItem.Crew):
                    GUICreditsMovie.Movie = selectedAnticipatedItem.Movie;
                    GUICreditsMovie.Type = GUICreditsMovie.CreditType.Crew;
                    GUICreditsMovie.Fanart = TmdbCache.GetMovieBackdropFilename(selectedItem.Images.MovieImages);
                    GUIWindowManager.ActivateWindow((int)TraktGUIWindows.CreditsMovie);
                    break;

                case ((int)MediaContextMenuItem.Trailers):
                    GUICommon.ShowMovieTrailersMenu(selectedAnticipatedItem.Movie);
                    break;

                case ((int)MediaContextMenuItem.ChangeLayout):
                    CurrentLayout = GUICommon.ShowLayoutMenu(CurrentLayout, PreviousSelectedIndex);
                    break;

                case ((int)MediaContextMenuItem.SearchWithMpNZB):
                    string loadingParam = string.Format("search:{0}", selectedAnticipatedItem.Movie.Title);
                    GUIWindowManager.ActivateWindow((int)ExternalPluginWindows.MpNZB, loadingParam);
                    break;

                case ((int)MediaContextMenuItem.SearchTorrent):
                    string loadPar = selectedAnticipatedItem.Movie.Title;
                    GUIWindowManager.ActivateWindow((int)ExternalPluginWindows.MyTorrents, loadPar);
                    break;

                default:
                    break;
            }

            base.OnShowContextMenu();
        }

        #endregion

        #region Private Methods

        TraktMoviesAnticipated GetAnticipatedMovies(int page)
        {
            TraktMoviesAnticipated trendingMovies = null;

            if (AnticipatedMoviePages == null || LastRequest < DateTime.UtcNow.Subtract(new TimeSpan(0, TraktSettings.WebRequestCacheMinutes, 0)))
            {
                // get the first page
                trendingMovies = TraktAPI.TraktAPI.GetAnticipatedMovies(1, TraktSettings.MaxAnticipatedMoviesRequest);

                // reset to defaults
                LastRequest = DateTime.UtcNow;
                CurrentPage = 1;
                PreviousSelectedIndex = 0;

                // clear the cache
                if (AnticipatedMoviePages == null)
                    AnticipatedMoviePages = new Dictionary<int, TraktMoviesAnticipated>();
                else
                    AnticipatedMoviePages.Clear();

                // add page to cache
                AnticipatedMoviePages.Add(1, trendingMovies);
            }
            else
            {
                // get page from cache if it exists
                if (AnticipatedMoviePages.TryGetValue(page, out trendingMovies))
                {
                    return trendingMovies;
                }

                // request next page
                trendingMovies = TraktAPI.TraktAPI.GetAnticipatedMovies(page, TraktSettings.MaxAnticipatedMoviesRequest);
                if (trendingMovies != null && trendingMovies.Movies != null)
                {
                    // add to cache
                    AnticipatedMoviePages.Add(page, trendingMovies);
                }
            }
            return trendingMovies;
        }

        private void CheckAndPlayMovie(bool jumpTo)
        {
            var selectedItem = this.Facade.SelectedListItem;
            if (selectedItem == null) return;

            var selectedAnticipatedItem = selectedItem.TVTag as TraktMovieAnticipated;
            if (selectedAnticipatedItem == null) return;

            GUICommon.CheckAndPlayMovie(jumpTo, selectedAnticipatedItem.Movie);
        }

        private void LoadAnticipatedMovies(int page = 1)
        {
            GUIUtils.SetProperty("#Trakt.Items", string.Empty);

            GUIBackgroundTask.Instance.ExecuteInBackgroundAndCallback(() =>
            {
                return GetAnticipatedMovies(page);
            },
            delegate(bool success, object result)
            {
                if (success)
                {
                    var movies = result as TraktMoviesAnticipated;
                    SendAnticipatedMoviesToFacade(movies);
                }
            }, Translation.GettingAnticipatedMovies, true);
        }

        private void SendAnticipatedMoviesToFacade(TraktMoviesAnticipated anticipatedItems)
        {
            // clear facade
            GUIControl.ClearControl(GetID, Facade.GetID);

            if (anticipatedItems == null)
            {
                GUIUtils.ShowNotifyDialog(Translation.Error, Translation.ErrorGeneral);
                GUIWindowManager.ShowPreviousWindow();
                AnticipatedMoviePages = null;
                return;
            }
            
            // filter movies
            var filteredAnticipatedList = FilterAnticipatedMovies(anticipatedItems.Movies).Where(m => !string.IsNullOrEmpty(m.Movie.Title)).ToList();

            // sort movies
            filteredAnticipatedList.Sort(new GUIListItemMovieSorter(TraktSettings.SortByAnticipatedMovies.Field, TraktSettings.SortByAnticipatedMovies.Direction));

            int itemId = 0;
            var movieImages = new List<GUITmdbImage>();

            // Add Previous Page Button
            if (anticipatedItems.CurrentPage != 1)
            {
                var prevPageItem = new GUIMovieListItem(Translation.PreviousPage, (int)TraktGUIWindows.AnticipatedMovies);
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
            foreach (var anticipatedItem in filteredAnticipatedList)
            {
                // add image for download
                var images = new GUITmdbImage { MovieImages = new TmdbMovieImages { Id = anticipatedItem.Movie.Ids.Tmdb } };
                movieImages.Add(images);

                var item = new GUIMovieListItem(anticipatedItem.Movie.Title, (int)TraktGUIWindows.AnticipatedMovies);

                item.Label2 = anticipatedItem.Movie.Year == null ? "----" : anticipatedItem.Movie.Year.ToString();
                item.TVTag = anticipatedItem;
                item.Movie = anticipatedItem.Movie;
                item.Images = images;
                item.IsPlayed = anticipatedItem.Movie.IsWatched();
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
            if (anticipatedItems.CurrentPage != anticipatedItems.TotalPages)
            {
                var nextPageItem = new GUIMovieListItem(Translation.NextPage, (int)TraktGUIWindows.AnticipatedMovies);
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
            GUIUtils.SetProperty("#itemcount", filteredAnticipatedList.Count().ToString());
            GUIUtils.SetProperty("#Trakt.Items", string.Format("{0} {1}", filteredAnticipatedList.Count(), filteredAnticipatedList.Count() > 1 ? Translation.Movies : Translation.Movie));

            // Page Properties
            GUIUtils.SetProperty("#Trakt.Facade.CurrentPage", anticipatedItems.CurrentPage.ToString());
            GUIUtils.SetProperty("#Trakt.Facade.TotalPages", anticipatedItems.TotalPages.ToString());
            GUIUtils.SetProperty("#Trakt.Facade.TotalItemsPerPage", TraktSettings.MaxAnticipatedMoviesRequest.ToString());

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
            CurrentLayout = (GUIFacadeControl.Layout)TraktSettings.AnticipatedMoviesDefaultLayout;

            // Update Button States
            UpdateButtonState();

            if (sortButton != null)
            {
                UpdateButtonState();
                sortButton.SortChanged += (o, e) =>
                {
                    TraktSettings.SortByAnticipatedMovies.Direction = (SortingDirections)(e.Order - 1);
                    PreviousSelectedIndex = CurrentPage == 1 ? 0 : 1;
                    UpdateButtonState();
                    LoadAnticipatedMovies(CurrentPage);
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
                sortButton.Label = GUICommon.GetSortByString(TraktSettings.SortByAnticipatedMovies);
                sortButton.IsAscending = (TraktSettings.SortByAnticipatedMovies.Direction == SortingDirections.Ascending);
            }
            GUIUtils.SetProperty("#Trakt.SortBy", GUICommon.GetSortByString(TraktSettings.SortByAnticipatedMovies));

            // update filter buttons
            if (filterWatchListedButton != null)
                filterWatchListedButton.Selected = TraktSettings.AnticipatedMoviesHideWatchlisted;
        }

        private void ClearProperties(bool moviesOnly = false)
        {
            if (!moviesOnly)
            {
                GUIUtils.SetProperty("#Trakt.Anticipated.CurrentPage", string.Empty);
                GUIUtils.SetProperty("#Trakt.Anticipated.TotalPages", string.Empty);
                GUIUtils.SetProperty("#Trakt.Facade.IsPageItem", string.Empty);
            }

            GUIUtils.SetProperty("#Trakt.Movie.ListCount", string.Empty);
            GUIUtils.SetProperty("#Trakt.Movie.ListCount.Extra", string.Empty);

            GUICommon.ClearMovieProperties();
        }

        private void PublishMovieSkinProperties(TraktMovieAnticipated anticipatedItem)
        {
            GUICommon.SetProperty("#Trakt.Movie.ListCount", anticipatedItem.ListCount.ToString());
            GUICommon.SetProperty("#Trakt.Movie.ListCount.Extra", string.Format(Translation.AppearsInList, anticipatedItem.ListCount));

            GUICommon.SetMovieProperties(anticipatedItem.Movie);
        }

        private void OnMovieSelected(GUIListItem item, GUIControl control)
        {
            GUIUtils.SetProperty("#Trakt.Facade.IsPageItem", false.ToString());

            PreviousSelectedIndex = Facade.SelectedListItemIndex;

            var anticipatedItem = item.TVTag as TraktMovieAnticipated;
            if (anticipatedItem == null) return;

            PublishMovieSkinProperties(anticipatedItem);
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

        private IEnumerable<TraktMovieAnticipated> FilterAnticipatedMovies(IEnumerable<TraktMovieAnticipated> moviesToFilter)
        {
            if (TraktSettings.AnticipatedMoviesHideWatchlisted)
                moviesToFilter = moviesToFilter.Where(a => !a.Movie.IsWatchlisted());

            return moviesToFilter;

        }
        #endregion
    }
}
