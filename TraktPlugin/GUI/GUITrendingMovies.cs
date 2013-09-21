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
using MediaPortal.Video.Database;
using MediaPortal.GUI.Video;
using Action = MediaPortal.GUI.Library.Action;
using MediaPortal.Util;
using TraktPlugin.TraktAPI;
using TraktPlugin.TraktAPI.DataStructures;

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

        private Layout CurrentLayout { get; set; }
        private ImageSwapper backdrop;
        DateTime LastRequest = new DateTime();
        int PreviousSelectedIndex = 0;

        IEnumerable<TraktTrendingMovie> TrendingMovies
        {
            get
            {
                if (_TrendingMovies == null || LastRequest < DateTime.UtcNow.Subtract(new TimeSpan(0, TraktSettings.WebRequestCacheMinutes, 0)))
                {
                    _TrendingMovies = TraktAPI.TraktAPI.GetTrendingMovies();
                    LastRequest = DateTime.UtcNow;
                    PreviousSelectedIndex = 0;
                }
                return _TrendingMovies;
            }
        }
        private IEnumerable<TraktTrendingMovie> _TrendingMovies = null;

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
            LoadTrendingMovies();
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
                        CheckAndPlayMovie(true);
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
                            PreviousSelectedIndex = 0;
                            UpdateButtonState();
                            LoadTrendingMovies();
                        }
                    }
                    break;

                // Hide Watched
                case (9):
                    TraktSettings.TrendingMoviesHideWatched = !TraktSettings.TrendingMoviesHideWatched;
                    UpdateButtonState();
                    LoadTrendingMovies();
                    break;

                // Hide Watchlisted
                case (10):
                    TraktSettings.TrendingMoviesHideWatchlisted = !TraktSettings.TrendingMoviesHideWatchlisted;
                    UpdateButtonState();
                    LoadTrendingMovies();
                    break;

                // Hide Collected
                case (11):
                    TraktSettings.TrendingMoviesHideCollected = !TraktSettings.TrendingMoviesHideCollected;
                    UpdateButtonState();
                    LoadTrendingMovies();
                    break;

                // Hide Rated
                case (12):
                    TraktSettings.TrendingMoviesHideRated = !TraktSettings.TrendingMoviesHideRated;
                    UpdateButtonState();
                    LoadTrendingMovies();
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
            var selectedItem = this.Facade.SelectedListItem;
            if (selectedItem == null) return;
            
            var selectedMovie = selectedItem.TVTag as TraktTrendingMovie;
            if (selectedMovie == null) return;

            var dlg = (IDialogbox)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
            if (dlg == null) return;

            dlg.Reset();
            dlg.SetHeading(GUIUtils.PluginName());

            GUICommon.CreateTrendingMoviesContextMenu(ref dlg, selectedMovie, false);

            // Show Context Menu
            dlg.DoModal(GUIWindowManager.ActiveWindow);
            if (dlg.SelectedId < 0) return;

            switch (dlg.SelectedId)
            {
                case ((int)TrendingContextMenuItem.MarkAsWatched):
                    TraktHelper.MarkMovieAsWatched(selectedMovie);
                    if (selectedMovie.Plays == 0) selectedMovie.Plays = 1;
                    selectedMovie.Watched = true;
                    selectedItem.IsPlayed = true;
                    OnMovieSelected(selectedItem, Facade);
                    ((Facade.SelectedListItem as GUIMovieListItem).Item as TraktImage).NotifyPropertyChanged("Poster");
                    if (TraktSettings.TrendingMoviesHideWatched) LoadTrendingMovies();
                    break;

                case ((int)TrendingContextMenuItem.MarkAsUnWatched):
                    TraktHelper.MarkMovieAsUnWatched(selectedMovie);
                    selectedMovie.Watched = false;
                    selectedItem.IsPlayed = false;
                    OnMovieSelected(selectedItem, Facade);
                    ((Facade.SelectedListItem as GUIMovieListItem).Item as TraktImage).NotifyPropertyChanged("Poster");
                    break;

                case ((int)TrendingContextMenuItem.AddToWatchList):
                    TraktHelper.AddMovieToWatchList(selectedMovie, true);
                    selectedMovie.InWatchList = true;
                    OnMovieSelected(selectedItem, Facade);
                    ((Facade.SelectedListItem as GUIMovieListItem).Item as TraktImage).NotifyPropertyChanged("Poster");
                    if (TraktSettings.TrendingMoviesHideWatchlisted) LoadTrendingMovies();
                    break;

                case ((int)TrendingContextMenuItem.RemoveFromWatchList):
                    TraktHelper.RemoveMovieFromWatchList(selectedMovie, true);
                    selectedMovie.InWatchList = false;
                    OnMovieSelected(selectedItem, Facade);
                    ((Facade.SelectedListItem as GUIMovieListItem).Item as TraktImage).NotifyPropertyChanged("Poster");
                    break;

                case ((int)TrendingContextMenuItem.AddToList):
                    TraktHelper.AddRemoveMovieInUserList(selectedMovie, false);                    
                    break;

                case ((int)TrendingContextMenuItem.AddToLibrary):
                    TraktHelper.AddMovieToLibrary(selectedMovie);
                    selectedMovie.InCollection = true;
                    OnMovieSelected(selectedItem, Facade);
                    ((Facade.SelectedListItem as GUIMovieListItem).Item as TraktImage).NotifyPropertyChanged("Poster");
                    if (TraktSettings.TrendingMoviesHideCollected) LoadTrendingMovies();
                    break;

                case ((int)TrendingContextMenuItem.RemoveFromLibrary):
                    TraktHelper.RemoveMovieFromLibrary(selectedMovie);
                    selectedMovie.InCollection = false;
                    OnMovieSelected(selectedItem, Facade);
                    ((Facade.SelectedListItem as GUIMovieListItem).Item as TraktImage).NotifyPropertyChanged("Poster");
                    break;

                case ((int)TrendingContextMenuItem.Related):
                    TraktHelper.ShowRelatedMovies(selectedMovie);
                    break;

                case ((int)TrendingContextMenuItem.Rate):
                    GUICommon.RateMovie(selectedMovie);
                    OnMovieSelected(selectedItem, Facade);
                    ((Facade.SelectedListItem as GUIMovieListItem).Item as TraktImage).NotifyPropertyChanged("Poster");
                    if (TraktSettings.TrendingMoviesHideRated) LoadTrendingMovies();
                    break;

                case ((int)TrendingContextMenuItem.Filters):
                    if (GUICommon.ShowMovieFiltersMenu())
                    {
                        UpdateButtonState();
                        LoadTrendingMovies();
                    }
                    break;

                case ((int)TrendingContextMenuItem.Shouts):
                    TraktHelper.ShowMovieShouts(selectedMovie);
                    break;

                case ((int)TrendingContextMenuItem.Trailers):
                    GUICommon.ShowMovieTrailersMenu(selectedMovie);
                    break;

                case ((int)TrendingContextMenuItem.ChangeLayout):
                    CurrentLayout = GUICommon.ShowLayoutMenu(CurrentLayout, PreviousSelectedIndex);
                    break;

                case ((int)TrendingContextMenuItem.SearchWithMpNZB):
                    string loadingParam = string.Format("search:{0}", selectedMovie.Title);
                    GUIWindowManager.ActivateWindow((int)ExternalPluginWindows.MpNZB, loadingParam);
                    break;

                case ((int)TrendingContextMenuItem.SearchTorrent):
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

        private void CheckAndPlayMovie(bool jumpTo)
        {
            var selectedItem = this.Facade.SelectedListItem;
            if (selectedItem == null) return;

            var selectedMovie = selectedItem.TVTag as TraktMovie;
            if (selectedMovie == null) return;

            GUICommon.CheckAndPlayMovie(jumpTo, selectedMovie);
        }

        private void LoadTrendingMovies()
        {
            GUIUtils.SetProperty("#Trakt.Items", string.Empty);

            GUIBackgroundTask.Instance.ExecuteInBackgroundAndCallback(() =>
            {
                return TrendingMovies;
            },
            delegate(bool success, object result)
            {
                if (success)
                {
                    IEnumerable<TraktTrendingMovie> movies = result as IEnumerable<TraktTrendingMovie>;
                    SendTrendingMoviesToFacade(movies);
                }
            }, Translation.GettingTrendingMovies, true);
        }

        private void SendTrendingMoviesToFacade(IEnumerable<TraktTrendingMovie> movies)
        {
            // clear facade
            GUIControl.ClearControl(GetID, Facade.GetID);

            if (movies.Count() == 0)
            {
                GUIUtils.ShowNotifyDialog(GUIUtils.PluginName(), Translation.NoTrendingMovies);
                GUIWindowManager.ShowPreviousWindow();
                return;
            }

            // filter movies
            movies = GUICommon.FilterTrendingMovies(movies);

            // sort movies
            var movieList = movies.Where(m => !string.IsNullOrEmpty(m.Title)).ToList();
            movieList.Sort(new GUIListItemMovieSorter(TraktSettings.SortByTrendingMovies.Field, TraktSettings.SortByTrendingMovies.Direction));

            int itemId = 0;
            var movieImages = new List<TraktImage>();

            // Add each movie mark remote if not in collection            
            foreach (var movie in movieList)
            {
                // add image for download
                var images = new TraktImage { MovieImages = movie.Images };
                movieImages.Add(images);

                var item = new GUIMovieListItem(movie.Title, (int)TraktGUIWindows.TrendingMovies);

                item.Label2 = movie.Year == "0" ? "----" : movie.Year;
                item.TVTag = movie;
                item.Item = images;
                item.IsPlayed = movie.Watched;
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

            Facade.SelectIndex(PreviousSelectedIndex);

            // set facade properties
            GUIUtils.SetProperty("#itemcount", movies.Count().ToString());
            GUIUtils.SetProperty("#Trakt.Items", string.Format("{0} {1}", movies.Count().ToString(), movies.Count() > 1 ? Translation.Movies : Translation.Movie));
            GUIUtils.SetProperty("#Trakt.Trending.PeopleCount", movies.Sum(m => m.Watchers).ToString());
            GUIUtils.SetProperty("#Trakt.Trending.Description", string.Format(Translation.TrendingMoviePeople, movies.Sum(m => m.Watchers).ToString(), movies.Count().ToString()));

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
            CurrentLayout = (Layout)TraktSettings.TrendingMoviesDefaultLayout;
            
            // Update Button States
            UpdateButtonState();

            if (sortButton != null)
            {
                UpdateButtonState();
                sortButton.SortChanged += (o, e) =>
                {
                    TraktSettings.SortByTrendingMovies.Direction = (SortingDirections)(e.Order - 1);
                    PreviousSelectedIndex = 0;
                    UpdateButtonState();
                    LoadTrendingMovies();
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

        private void ClearProperties()
        {
            GUIUtils.SetProperty("#Trakt.Trending.PeopleCount", string.Empty);
            GUIUtils.SetProperty("#Trakt.Trending.Description", string.Empty);
            
            GUIUtils.SetProperty("#Trakt.Movie.Watchers", string.Empty);
            GUIUtils.SetProperty("#Trakt.Movie.Watchers.Extra", string.Empty);
            GUICommon.ClearMovieProperties();
        }

        private void PublishMovieSkinProperties(TraktTrendingMovie movie)
        {
            GUICommon.SetProperty("#Trakt.Movie.Watchers", movie.Watchers.ToString());
            GUICommon.SetProperty("#Trakt.Movie.Watchers.Extra", movie.Watchers > 1 ? string.Format(Translation.PeopleWatching, movie.Watchers) : Translation.PersonWatching);
            GUICommon.SetMovieProperties(movie);
        }

        private void OnMovieSelected(GUIListItem item, GUIControl parent)
        {
            PreviousSelectedIndex = Facade.SelectedListItemIndex;
            
            var movie = item.TVTag as TraktTrendingMovie;
            PublishMovieSkinProperties(movie);
            GUIImageHandler.LoadFanart(backdrop, movie.Images.Fanart.LocalImageFilename(ArtworkType.MovieFanart));
        }
        #endregion
    }
}
