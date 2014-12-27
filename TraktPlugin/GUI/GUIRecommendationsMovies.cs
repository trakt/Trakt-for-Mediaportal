using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using MediaPortal.GUI.Library;
using MediaPortal.Util;
using TraktPlugin.TraktAPI.DataStructures;
using Action = MediaPortal.GUI.Library.Action;

namespace TraktPlugin.GUI
{
    public class GUIRecommendationsMovies : GUIWindow
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
            MarkAsWatched,
            DismissRecommendation,
            AddToWatchList,
            RemoveFromWatchList,
            AddToList,
            AddToLibrary,
            RemoveFromLibrary,
            Related,
            Rate,
            Shouts,
            ChangeLayout,
            Trailers,
            SearchWithMpNZB,
            SearchTorrent
        }

        #endregion

        #region Constructor

        public GUIRecommendationsMovies()
        {
            backdrop = new ImageSwapper();
            backdrop.PropertyOne = "#Trakt.RecommendedMovies.Fanart.1";
            backdrop.PropertyTwo = "#Trakt.RecommendedMovies.Fanart.2";
        }

        #endregion

        #region Private Variables

        Layout CurrentLayout { get; set; }
        string CurrentGenre { get; set; }
        bool HideCollected { get; set; }
        bool HideWatchlisted { get; set; }
        int StartYear { get; set; }
        int EndYear { get; set; }
        int PreviousSelectedIndex { get; set; }
        ImageSwapper backdrop;
        DateTime LastRequest = new DateTime();

        IEnumerable<TraktMovieSummary> RecommendedMovies
        {
            get
            {
                if (_RecommendedMovies == null || LastRequest < DateTime.UtcNow.Subtract(new TimeSpan(0, TraktSettings.WebRequestCacheMinutes, 0)))
                {
                    SetRecommendationProperties();
                    if ((StartYear > EndYear) && EndYear != 0) StartYear = 0;
                    //TODO - Filtering not yet implemented on API
                    //_RecommendedMovies = TraktAPI.TraktAPI.GetRecommendedMovies(TraktGenres.MovieGenres[CurrentGenre], HideCollected, HideWatchlisted, StartYear, EndYear);
                    _RecommendedMovies = TraktAPI.TraktAPI.GetRecommendedMovies("full,images");
                    LastRequest = DateTime.UtcNow;
                    PreviousSelectedIndex = 0;
                }
                return _RecommendedMovies;
            }
        }
        static IEnumerable<TraktMovieSummary> _RecommendedMovies = null;

        #endregion

        #region Base Overrides

        public override int GetID
        {
            get
            {
                return (int)TraktGUIWindows.RecommendationsMovies;
            }
        }

        public override bool Init()
        {
            return Load(GUIGraphicsContext.Skin + @"\Trakt.Recommendations.Movies.xml");
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

            // Load Trending Movies
            LoadRecommendedMovies();
        }

        protected override void OnPageDestroy(int new_windowId)
        {
            GUIMovieListItem.StopDownload = true;
            PreviousSelectedIndex = Facade.SelectedListItemIndex;
            ClearProperties();

            // save current layout
            TraktSettings.RecommendedMoviesDefaultLayout = (int)CurrentLayout;

            // genre
            TraktSettings.MovieRecommendationGenre = CurrentGenre;

            // hide collected/watchlisted
            TraktSettings.MovieRecommendationHideCollected = HideCollected;
            TraktSettings.MovieRecommendationHideWatchlisted = HideWatchlisted;

            // start/end year
            TraktSettings.MovieRecommendationStartYear = StartYear;
            TraktSettings.MovieRecommendationEndYear = EndYear;

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
                        CheckAndPlayMovie(true);
                    }
                    break;

                // Layout Button
                case (2):
                    CurrentLayout = GUICommon.ShowLayoutMenu(CurrentLayout, PreviousSelectedIndex);
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
                    if (startYear == "0") startYear = "1888";
                    if (GUIUtils.GetStringFromKeyboard(ref startYear))
                    {
                        int result;
                        if (startYear.Length == 4 && int.TryParse(startYear, out result) && !startYear.Equals(StartYear.ToString()))
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
                        if (endYear.Length == 4 && int.TryParse(endYear, out result) && !endYear.Equals(EndYear.ToString()))
                        {
                            EndYear = result;
                            GUIControl.SetControlLabel(GetID, endYearButton.GetID, GetEndYearTitle(EndYear));
                            ReloadRecommendations();
                        }
                    }
                    break;

                // Sort Button
                case (8):
                    var newSortBy = GUICommon.ShowSortMenu(TraktSettings.SortByRecommendedMovies);
                    if (newSortBy != null)
                    {
                        if (newSortBy.Field != TraktSettings.SortByRecommendedMovies.Field)
                        {
                            TraktSettings.SortByRecommendedMovies = newSortBy;
                            PreviousSelectedIndex = 0;
                            UpdateButtonState();
                            LoadRecommendedMovies();
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
                    CheckAndPlayMovie(false);
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

            var selectedMovie = selectedItem.TVTag as TraktMovieSummary;
            if (selectedMovie == null) return;

            IDialogbox dlg = (IDialogbox)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
            if (dlg == null) return;

            dlg.Reset();
            dlg.SetHeading(GUIUtils.PluginName());

            GUIListItem listItem = null;

            // Dismiss Recommendation
            listItem = new GUIListItem(Translation.DismissRecommendation);
            dlg.Add(listItem);
            listItem.ItemId = (int)ContextMenuItem.DismissRecommendation;

            // Mark As Watched
            // This should remove item from recommendations if executed
            if (!selectedMovie.IsWatched())
            {
                listItem = new GUIListItem(Translation.MarkAsWatched);
                dlg.Add(listItem);
                listItem.ItemId = (int)ContextMenuItem.MarkAsWatched;
            }

            // Add/Remove Watchlist            
            if (!selectedMovie.IsWatchlisted())
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

            // Add to Custom List
            listItem = new GUIListItem(Translation.AddToList);
            dlg.Add(listItem);
            listItem.ItemId = (int)ContextMenuItem.AddToList;

            // Add to Library
            // Don't allow if it will be removed again on next sync
            // movie could be part of a DVD collection
            if (!selectedMovie.IsCollected() && !TraktSettings.KeepTraktLibraryClean)
            {
                listItem = new GUIListItem(Translation.AddToLibrary);
                dlg.Add(listItem);
                listItem.ItemId = (int)ContextMenuItem.AddToLibrary;
            }

            if (selectedMovie.IsCollected())
            {
                listItem = new GUIListItem(Translation.RemoveFromLibrary);
                dlg.Add(listItem);
                listItem.ItemId = (int)ContextMenuItem.RemoveFromLibrary;
            }

            // Related Movies
            listItem = new GUIListItem(Translation.RelatedMovies);
            dlg.Add(listItem);
            listItem.ItemId = (int)ContextMenuItem.Related;

            // Rate Movie
            listItem = new GUIListItem(Translation.RateMovie);
            dlg.Add(listItem);
            listItem.ItemId = (int)ContextMenuItem.Rate;

            // Shouts
            listItem = new GUIListItem(Translation.Comments);
            dlg.Add(listItem);
            listItem.ItemId = (int)ContextMenuItem.Shouts;

            if (TraktHelper.IsTrailersAvailableAndEnabled)
            {
                // Trailers
                listItem = new GUIListItem(Translation.Trailers);
                dlg.Add(listItem);
                listItem.ItemId = (int)ContextMenuItem.Trailers;
            }

            // Change Layout
            listItem = new GUIListItem(Translation.ChangeLayout);
            dlg.Add(listItem);
            listItem.ItemId = (int)ContextMenuItem.ChangeLayout;

            if (!selectedMovie.IsCollected() && TraktHelper.IsMpNZBAvailableAndEnabled)
            {
                // Search for movie with mpNZB
                listItem = new GUIListItem(Translation.SearchWithMpNZB);
                dlg.Add(listItem);
                listItem.ItemId = (int)ContextMenuItem.SearchWithMpNZB;
            }

            if (!selectedMovie.IsCollected() && TraktHelper.IsMyTorrentsAvailableAndEnabled)
            {
                // Search for movie with MyTorrents
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
                    DismissRecommendation(selectedMovie);
                    if (_RecommendedMovies.Count() > 1)
                    {
                        var moviesToExcept = new List<TraktMovieSummary>();
                        moviesToExcept.Add(selectedMovie);
                        _RecommendedMovies = RecommendedMovies.Except(moviesToExcept);
                    }
                    else
                    {
                        // reload, none left
                        ClearProperties();
                        GUIControl.ClearControl(GetID, Facade.GetID);
                        _RecommendedMovies = null;
                    }
                    LoadRecommendedMovies();
                    break;

                case ((int)ContextMenuItem.MarkAsWatched):
                    PreviousSelectedIndex = this.Facade.SelectedListItemIndex;
                    TraktHelper.AddMovieToWatchHistory(selectedMovie);
                    if (_RecommendedMovies.Count() > 1)
                    {
                        var moviesToExcept = new List<TraktMovieSummary>();
                        moviesToExcept.Add(selectedMovie);
                        _RecommendedMovies = RecommendedMovies.Except(moviesToExcept);
                    }
                    else
                    {
                        // reload, none left
                        ClearProperties();
                        GUIControl.ClearControl(GetID, Facade.GetID);
                        _RecommendedMovies = null;
                    }
                    LoadRecommendedMovies();
                    break;

                case ((int)ContextMenuItem.AddToWatchList):
                    TraktHelper.AddMovieToWatchList(selectedMovie, true);
                    OnMovieSelected(selectedItem, Facade);
                    (Facade.SelectedListItem as GUIMovieListItem).Images.NotifyPropertyChanged("Poster");
                    break;

                case ((int)ContextMenuItem.RemoveFromWatchList):
                    TraktHelper.RemoveMovieFromWatchList(selectedMovie, true);
                    OnMovieSelected(selectedItem, Facade);
                    (Facade.SelectedListItem as GUIMovieListItem).Images.NotifyPropertyChanged("Poster");
                    break;

                case ((int)ContextMenuItem.AddToList):
                    TraktHelper.AddRemoveMovieInUserList(selectedMovie, false);
                    break;

                case ((int)ContextMenuItem.AddToLibrary):
                    TraktHelper.AddMovieToCollection(selectedMovie);
                    OnMovieSelected(selectedItem, Facade);
                    (Facade.SelectedListItem as GUIMovieListItem).Images.NotifyPropertyChanged("Poster");
                    break;

                case ((int)ContextMenuItem.RemoveFromLibrary):
                    TraktHelper.RemoveMovieFromCollection(selectedMovie);
                    OnMovieSelected(selectedItem, Facade);
                    (Facade.SelectedListItem as GUIMovieListItem).Images.NotifyPropertyChanged("Poster");
                    break;

                case ((int)ContextMenuItem.Related):
                    TraktHelper.ShowRelatedMovies(selectedMovie);
                    break;

                case ((int)ContextMenuItem.Rate):
                    PreviousSelectedIndex = this.Facade.SelectedListItemIndex;
                    if (GUICommon.RateMovie(selectedMovie))
                    {
                        // also mark as watched
                        TraktHelper.AddMovieToWatchHistory(selectedMovie);
                        // remove from recommendations
                        if (_RecommendedMovies.Count() > 1)
                        {
                            var moviesToExcept = new List<TraktMovieSummary>();
                            moviesToExcept.Add(selectedMovie);
                            _RecommendedMovies = RecommendedMovies.Except(moviesToExcept);
                        }
                        else
                        {
                            // reload, none left
                            ClearProperties();
                            GUIControl.ClearControl(GetID, Facade.GetID);
                            _RecommendedMovies = null;
                        }
                        LoadRecommendedMovies();
                    }
                    break;

                case ((int)ContextMenuItem.Shouts):
                    TraktHelper.ShowMovieShouts(selectedMovie);
                    break;

                case ((int)ContextMenuItem.Trailers):
                    GUICommon.ShowMovieTrailersMenu(selectedMovie);
                    break;
              
                case ((int)ContextMenuItem.ChangeLayout):
                    CurrentLayout = GUICommon.ShowLayoutMenu(CurrentLayout, PreviousSelectedIndex);
                    break;

                case ((int)ContextMenuItem.SearchWithMpNZB):
                    string loadingParam = string.Format("search:{0}", selectedMovie.Title);
                    GUIWindowManager.ActivateWindow((int)ExternalPluginWindows.MpNZB, loadingParam);
                    break;

                case ((int)ContextMenuItem.SearchTorrent):
                    string loadPar = selectedMovie.Title;
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
            _RecommendedMovies = null;
            LoadRecommendedMovies();
        }

        private void CheckAndPlayMovie(bool jumpTo)
        {
            GUIListItem selectedItem = this.Facade.SelectedListItem;
            if (selectedItem == null) return;

            var selectedMovie = selectedItem.TVTag as TraktMovieSummary;
            if (selectedMovie == null) return;

            GUICommon.CheckAndPlayMovie(jumpTo, selectedMovie);
        }

        private void DismissRecommendation(TraktMovie movie)
        {
            var syncThread = new Thread(obj =>
            {
                TraktMovie dismissMovie = obj as TraktMovie;

                var response = TraktAPI.TraktAPI.DismissRecommendedMovie(dismissMovie.Ids.Trakt.ToString());
                if (response)
                {
                    TraktHandlers.MovingPictures.RemoveMovieCriteriaFromRecommendationsNode(movie.Ids.Imdb);
                }
            })
            {
                IsBackground = true,
                Name = "DismissRecommendation"
            };

            syncThread.Start(movie);
        }

        private void ShowGenreMenu()
        {
            IDialogbox dlg = (IDialogbox)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
            dlg.Reset();
            dlg.SetHeading(TraktGenres.ItemName(CurrentGenre));

            foreach (string genre in TraktGenres.MovieGenres.Keys)
            {
                string menuItem = TraktGenres.ItemName(genre);
                GUIListItem pItem = new GUIListItem(menuItem);
                if (genre == CurrentGenre) pItem.Selected = true;
                dlg.Add(pItem);
            }

            dlg.DoModal(GUIWindowManager.ActiveWindow);

            if (dlg.SelectedLabel >= 0)
            {
                var genre = TraktGenres.MovieGenres.ElementAt(dlg.SelectedLabel).Key;
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
                return string.Format(Translation.StartYear, 1888);
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

        private void LoadRecommendedMovies()
        {
            GUIUtils.SetProperty("#Trakt.Items", string.Empty);

            GUIBackgroundTask.Instance.ExecuteInBackgroundAndCallback(() =>
            {
                return RecommendedMovies;
            },
            delegate(bool success, object result)
            {
                if (success)
                {
                    var movies = result as IEnumerable<TraktMovieSummary>;
                    SendRecommendedMoviesToFacade(movies);
                }
            }, Translation.GettingRecommendedMovies, true);
        }

        private void SendRecommendedMoviesToFacade(IEnumerable<TraktMovieSummary> movies)
        {
            // clear facade
            GUIControl.ClearControl(GetID, Facade.GetID);

            if (movies.Count() == 0)
            {
                // try again
                if (genreButton == null)
                {
                    // restore defaults for next time so we can get recommendations
                    if (CurrentGenre != "All")
                        CurrentGenre = "All";

                    _RecommendedMovies = null;
                    GUIWindowManager.ShowPreviousWindow();
                    
                }
                else
                {
                    GUIUtils.ShowNotifyDialog(GUIUtils.PluginName(), Translation.NoMovieRecommendations);
                    GUIControl.FocusControl(GetID, genreButton.GetID);
                }
                return;
            }

            // sort movies
            var movieList = movies.ToList();
            movieList.Sort(new GUIListItemMovieSorter(TraktSettings.SortByRecommendedMovies.Field, TraktSettings.SortByRecommendedMovies.Direction));

            int itemId = 0;
            var movieImages = new List<GUITraktImage>();

            // Add each movie
            foreach (var movie in movieList)
            {
                var item = new GUIMovieListItem(movie.Title, (int)TraktGUIWindows.RecommendationsMovies);

                // add image for download
                var image = new GUITraktImage { MovieImages = movie.Images };
                movieImages.Add(image);

                item.Label2 = movie.Year == null ? "----" : movie.Year.ToString();
                item.TVTag = movie;
                item.Movie = movie;
                item.Images = image;
                item.ItemId = Int32.MaxValue - itemId;
                // movie in collection doesnt nessararily mean
                // that the movie is locally available on this computer
                // as 'keep library clean' might not be enabled
                //item.IsRemote = !movie.InCollection;
                item.IconImage = GUIImageHandler.GetDefaultPoster(false);
                item.IconImageBig = GUIImageHandler.GetDefaultPoster();
                item.ThumbnailImage = GUIImageHandler.GetDefaultPoster();
                item.OnItemSelected += OnMovieSelected;
                Utils.SetDefaultIcons(item);
                Facade.Add(item);
                itemId++;
            }

            // Set Facade Layout
            Facade.SetCurrentLayout(Enum.GetName(typeof(Layout), CurrentLayout));
            GUIControl.FocusControl(GetID, Facade.GetID);

            if (PreviousSelectedIndex >= movies.Count())
                Facade.SelectIndex(PreviousSelectedIndex - 1);
            else
                Facade.SelectIndex(PreviousSelectedIndex);

            // set facade properties
            GUIUtils.SetProperty("#itemcount", movies.Count().ToString());
            GUIUtils.SetProperty("#Trakt.Items", string.Format("{0} {1}", movies.Count().ToString(), movies.Count() > 1 ? Translation.Movies : Translation.Movie));

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
            CurrentLayout = (Layout)TraktSettings.RecommendedMoviesDefaultLayout;
            // update button label
            GUIControl.SetControlLabel(GetID, layoutButton.GetID, GUICommon.GetLayoutTranslation(CurrentLayout));

            // genres
            CurrentGenre = TraktSettings.MovieRecommendationGenre;
            if (genreButton != null) GUIControl.SetControlLabel(GetID, genreButton.GetID, TraktGenres.ItemName(CurrentGenre));

            // toggles for hide collected/watchlisted
            HideCollected = TraktSettings.MovieRecommendationHideCollected;
            HideWatchlisted = TraktSettings.MovieRecommendationHideWatchlisted;
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
            StartYear = TraktSettings.MovieRecommendationStartYear;
            EndYear = TraktSettings.MovieRecommendationEndYear;
            if (startYearButton != null) GUIControl.SetControlLabel(GetID, startYearButton.GetID, GetStartYearTitle(StartYear));
            if (endYearButton != null) GUIControl.SetControlLabel(GetID, endYearButton.GetID, GetEndYearTitle(EndYear));

            SetRecommendationProperties();

            if (sortButton != null)
            {
                UpdateButtonState();
                sortButton.SortChanged += (o, e) =>
                {
                    TraktSettings.SortByRecommendedMovies.Direction = (SortingDirections)(e.Order - 1);
                    PreviousSelectedIndex = 0;
                    UpdateButtonState();
                    LoadRecommendedMovies();
                };
            }
        }

        private void UpdateButtonState()
        {
            // update sortby button label
            if (sortButton != null)
            {
                sortButton.Label = GUICommon.GetSortByString(TraktSettings.SortByRecommendedMovies);
                sortButton.IsAscending = (TraktSettings.SortByRecommendedMovies.Direction == SortingDirections.Ascending);
            }
            GUIUtils.SetProperty("#Trakt.SortBy", GUICommon.GetSortByString(TraktSettings.SortByRecommendedMovies));
        }

        private void SetRecommendationProperties()
        {
            GUIUtils.SetProperty("#Trakt.Recommendations.Genre", TraktGenres.Translate(CurrentGenre));
            GUIUtils.SetProperty("#Trakt.Recommendations.HideCollected", HideCollected.ToString());
            GUIUtils.SetProperty("#Trakt.Recommendations.HideWatchlisted", HideWatchlisted.ToString());
            GUIUtils.SetProperty("#Trakt.Recommendations.StartYear", StartYear == 0 ? "1888" : StartYear.ToString());
            GUIUtils.SetProperty("#Trakt.Recommendations.EndYear", EndYear == 0 ? DateTime.Now.AddYears(3).Year.ToString() : EndYear.ToString());
        }

        private void ClearProperties()
        {
            GUICommon.ClearMovieProperties();
        }

        private void PublishMovieSkinProperties(TraktMovieSummary movie)
        {
            GUICommon.SetMovieProperties(movie);
        }

        private void OnMovieSelected(GUIListItem item, GUIControl parent)
        {
            PreviousSelectedIndex = Facade.SelectedListItemIndex;

            var movie = item.TVTag as TraktMovieSummary;
            if (movie == null) return;

            PublishMovieSkinProperties(movie);
            GUIImageHandler.LoadFanart(backdrop, movie.Images.Fanart.LocalImageFilename(ArtworkType.MovieFanart));
        }

        #endregion

        #region Public Methods

        public static void ClearCache()
        {
            _RecommendedMovies = null;
        }

        #endregion
    }
}