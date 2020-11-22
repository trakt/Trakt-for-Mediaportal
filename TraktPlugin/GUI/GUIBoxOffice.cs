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
    public class GUIBoxOffice : GUIWindow
    {
        #region Skin Controls

        [SkinControl(2)]
        protected GUIButtonControl layoutButton = null;

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
            HideShowWatched,
            MarkAsWatched,
            MarkAsUnWatched,
            AddToWatchList,
            RemoveFromWatchList,
            AddToList,
            AddToLibrary,
            RemoveFromLibrary,
            Related,
            Rate,
            Shouts,
            Cast,
            Crew,
            ChangeLayout,
            Trailers,
            SearchWithMpNZB,
            SearchTorrent
        }

        #endregion

        #region Constructor

        public GUIBoxOffice()
        {
            backdrop = new ImageSwapper();
            backdrop.PropertyOne = "#Trakt.BoxOffice.Fanart.1";
            backdrop.PropertyTwo = "#Trakt.BoxOffice.Fanart.2";
        }

        #endregion

        #region Private Variables

        private GUIFacadeControl.Layout CurrentLayout { get; set; }
        private ImageSwapper backdrop;
        DateTime LastRequest = new DateTime();
        int PreviousSelectedIndex = 0;

        IEnumerable<TraktMovieBoxOffice> BoxOffice
        {
            get
            {
                if (_BoxOffice == null || LastRequest < DateTime.UtcNow.Subtract(new TimeSpan(0, TraktSettings.WebRequestCacheMinutes, 0)))
                {
                    _BoxOffice = TraktAPI.TraktAPI.GetBoxOffice();                    
                    LastRequest = DateTime.UtcNow;
                    PreviousSelectedIndex = 0;
                }
                return _BoxOffice;
            }
        }
        private IEnumerable<TraktMovieBoxOffice> _BoxOffice = null;

        #endregion

        #region Base Overrides

        public override int GetID
        {
            get
            {
                return (int)TraktGUIWindows.BoxOffice;
            }
        }

        public override bool Init()
        {
            return Load(GUIGraphicsContext.Skin + @"\Trakt.BoxOffice.xml");
        }

        protected override void OnPageLoad()
        {
            base.OnPageLoad();

            // Clear GUI Properties
            ClearProperties();

            // Init Properties
            InitProperties();

            // Load Box Office 
            LoadBoxOffice();
        }

        protected override void OnPageDestroy(int new_windowId)
        {
            GUIMovieListItem.StopDownload = true;
            ClearProperties();

            // save settings
            TraktSettings.BoxOfficeDefaultLayout = (int)CurrentLayout;

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
                        CheckAndPlayMovie(false);
                    }
                    break;
                default:
                    base.OnAction(action);
                    break;
            }
        }

        protected override void OnShowContextMenu()
        {
            if (GUIBackgroundTask.Instance.IsBusy) return;

            var selectedItem = this.Facade.SelectedListItem as GUIMovieListItem;
            if (selectedItem == null) return;

            var selectedMovie = selectedItem.Movie;
            if (selectedMovie == null) return;

            var dlg = (IDialogbox)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
            if (dlg == null) return;

            dlg.Reset();
            dlg.SetHeading(GUIUtils.PluginName());

            GUIListItem listItem = null;

            // Mark As Watched
            if (!selectedMovie.IsWatched())
            {
                listItem = new GUIListItem(Translation.MarkAsWatched);
                dlg.Add(listItem);
                listItem.ItemId = (int)ContextMenuItem.MarkAsWatched;
            }

            // Mark As UnWatched
            if (selectedMovie.IsWatched())
            {
                listItem = new GUIListItem(Translation.MarkAsUnWatched);
                dlg.Add(listItem);
                listItem.ItemId = (int)ContextMenuItem.MarkAsUnWatched;
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

            // Add to Custom list
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

            // Cast and Crew
            listItem = new GUIListItem(Translation.Cast);
            dlg.Add(listItem);
            listItem.ItemId = (int)ContextMenuItem.Cast;

            listItem = new GUIListItem(Translation.Crew);
            dlg.Add(listItem);
            listItem.ItemId = (int)ContextMenuItem.Crew;

            // Trailers
            if (TraktHelper.IsTrailersAvailableAndEnabled)
            {
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
                case ((int)ContextMenuItem.MarkAsWatched):
                    TraktHelper.AddMovieToWatchHistory(selectedMovie);
                    selectedItem.IsPlayed = true;
                    OnMovieSelected(selectedItem, Facade);
                    (Facade.SelectedListItem as GUIMovieListItem).Images.NotifyPropertyChanged("Poster");
                    break;

                case ((int)ContextMenuItem.MarkAsUnWatched):
                    TraktHelper.RemoveMovieFromWatchHistory(selectedMovie);
                    selectedItem.IsPlayed = false;
                    OnMovieSelected(selectedItem, Facade);
                    (Facade.SelectedListItem as GUIMovieListItem).Images.NotifyPropertyChanged("Poster");
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
                    GUICommon.RateMovie(selectedMovie);
                    OnMovieSelected(selectedItem, Facade);
                    (Facade.SelectedListItem as GUIMovieListItem).Images.NotifyPropertyChanged("Poster");
                    break;

                case ((int)ContextMenuItem.Shouts):
                    TraktHelper.ShowMovieShouts(selectedMovie);
                    break;

                case ((int)ContextMenuItem.Cast):
                    GUICreditsMovie.Movie = selectedMovie;
                    GUICreditsMovie.Type = GUICreditsMovie.CreditType.Cast;
                    GUICreditsMovie.Fanart = TmdbCache.GetMovieBackdropFilename(selectedItem.Images.MovieImages);
                    GUIWindowManager.ActivateWindow((int)TraktGUIWindows.CreditsMovie);
                    break;

                case ((int)ContextMenuItem.Crew):
                    GUICreditsMovie.Movie = selectedMovie;
                    GUICreditsMovie.Type = GUICreditsMovie.CreditType.Crew;
                    GUICreditsMovie.Fanart = TmdbCache.GetMovieBackdropFilename(selectedItem.Images.MovieImages);
                    GUIWindowManager.ActivateWindow((int)TraktGUIWindows.CreditsMovie);
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

        private void CheckAndPlayMovie(bool jumpTo)
        {
            var selectedItem = this.Facade.SelectedListItem as GUIMovieListItem;
            if (selectedItem == null) return;

            var selectedMovie = selectedItem.Movie;
            GUICommon.CheckAndPlayMovie(jumpTo, selectedMovie);
        }

        private void LoadBoxOffice()
        {
            GUIUtils.SetProperty("#Trakt.Items", string.Empty);

            GUIBackgroundTask.Instance.ExecuteInBackgroundAndCallback(() =>
            {
                return BoxOffice;
            },
            delegate(bool success, object result)
            {
                if (success)
                {
                    var boxOffice = result as IEnumerable<TraktMovieBoxOffice>;
                    SendBoxOfficeToFacade(boxOffice);
                }
            }, Translation.GettingBoxOffice, true);
        }

        private void SendBoxOfficeToFacade(IEnumerable<TraktMovieBoxOffice> boxOffice)
        {
            // clear facade
            GUIControl.ClearControl(GetID, Facade.GetID);

            if (boxOffice == null)
            {
                GUIUtils.ShowNotifyDialog(Translation.Error, Translation.ErrorGeneral);
                GUIWindowManager.ShowPreviousWindow();
                return;
            }

            if (boxOffice.Count() == 0)
            {
                GUIUtils.ShowNotifyDialog(GUIUtils.PluginName(), Translation.NoBoxOffice);
                GUIWindowManager.ShowPreviousWindow();
                return;
            }

            int itemId = 0;
            var movieImages = new List<GUITmdbImage>();

            // Add each movie
            foreach (var box in boxOffice.Where(b => !string.IsNullOrEmpty(b.Movie.Title)))
            {
                var item = new GUIMovieListItem(box.Movie.Title, (int)TraktGUIWindows.BoxOffice);

                // add image for download
                var image = new GUITmdbImage { MovieImages = new TmdbMovieImages { Id = box.Movie.Ids.Tmdb } };
                movieImages.Add(image);

                item.Label2 = box.Movie.Year == null ? "----" : box.Movie.Year.ToString();
                item.TVTag = box;
                item.Movie = box.Movie;
                item.Images = image;
                item.IsPlayed = box.Movie.IsWatched();
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
            Facade.CurrentLayout = CurrentLayout;
            GUIControl.FocusControl(GetID, Facade.GetID);

            Facade.SelectIndex(PreviousSelectedIndex);

            // set facade properties
            GUIUtils.SetProperty("#itemcount", boxOffice.Count().ToString());
            GUIUtils.SetProperty("#Trakt.Items", string.Format("{0} {1}", boxOffice.Count().ToString(), boxOffice.Count() > 1 ? Translation.Movies : Translation.Movie));

            uint lTotalRevenue = 0;
            foreach (var movie in boxOffice)
            {
                lTotalRevenue += (uint)movie.Revenue;
            }            

            GUIUtils.SetProperty("#Trakt.BoxOffice.Total.Revenue", string.Format("{0:C0}", lTotalRevenue));
            GUIUtils.SetProperty("#Trakt.BoxOffice.Total.Revenue.Raw", lTotalRevenue.ToString());

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
            CurrentLayout = (GUIFacadeControl.Layout)TraktSettings.BoxOfficeDefaultLayout;
            // update button label
            GUIControl.SetControlLabel(GetID, layoutButton.GetID, GUICommon.GetLayoutTranslation(CurrentLayout));
        }

        private void ClearProperties()
        {
            GUIUtils.SetProperty("#Trakt.Movie.Revenue", string.Empty);
            GUICommon.ClearMovieProperties();
        }

        private void PublishMovieSkinProperties(TraktMovieBoxOffice boxOfficeMovie)
        {
            GUIUtils.SetProperty("#Trakt.Movie.Revenue", string.Format("{0:C0}", boxOfficeMovie.Revenue));
            GUIUtils.SetProperty("#Trakt.Movie.Revenue.Raw", boxOfficeMovie.Revenue.ToString());
            GUICommon.SetMovieProperties(boxOfficeMovie.Movie);
        }

        private void OnMovieSelected(GUIListItem item, GUIControl parent)
        {
            PreviousSelectedIndex = Facade.SelectedListItemIndex;

            var movie = item.TVTag as TraktMovieBoxOffice;
            PublishMovieSkinProperties(movie);
            GUIImageHandler.LoadFanart(backdrop, TmdbCache.GetMovieBackdropFilename((item as GUIMovieListItem).Images.MovieImages));
        }

        #endregion
    }
}