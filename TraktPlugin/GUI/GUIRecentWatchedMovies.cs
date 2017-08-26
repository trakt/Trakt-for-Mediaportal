using System;
using System.Collections.Generic;
using System.Linq;
using MediaPortal.GUI.Library;
using MediaPortal.Util;
using TraktPlugin.Cache;
using TraktPlugin.Extensions;
using TraktPlugin.TmdbAPI.DataStructures;
using TraktAPI.DataStructures;
using TraktAPI.Extensions;
using Action = MediaPortal.GUI.Library.Action;

namespace TraktPlugin.GUI
{
    public class GUIRecentWatchedMovies : GUIWindow
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
            RemoveFromWatchList,
            AddToWatchList,
            AddToList,
            ChangeLayout,
            MarkAsWatched,
            MarkAsUnWatched,
            AddToLibrary,
            RemoveFromLibrary,
            Related,
            Rate,
            Shouts,
            Cast,
            Crew,
            Trailers,
            SearchWithMpNZB,
            SearchTorrent
        }

        #endregion

        #region Constructor

        public GUIRecentWatchedMovies()
        {
            backdrop = new ImageSwapper();
            backdrop.PropertyOne = "#Trakt.RecentWatchedMovies.Fanart.1";
            backdrop.PropertyTwo = "#Trakt.RecentWatchedMovies.Fanart.2";
        }

        #endregion

        #region Private Variables

        static int PreviousSelectedIndex { get; set; }
        static DateTime LastRequest = new DateTime();
        string PreviousUser = null;
        GUIFacadeControl.Layout CurrentLayout { get; set; }
        ImageSwapper backdrop;
        Dictionary<string, IEnumerable<TraktMovieHistory>> userRecentlyWatchedMovies = new Dictionary<string, IEnumerable<TraktMovieHistory>>();

        IEnumerable<TraktMovieHistory> RecentlyWatchedMovies
        {
            get
            {
                if (!userRecentlyWatchedMovies.Keys.Contains(CurrentUser) || LastRequest < DateTime.UtcNow.Subtract(new TimeSpan(0, TraktSettings.WebRequestCacheMinutes, 0)))
                {
                    var recentlyWatched = TraktAPI.TraktAPI.GetUsersMovieWatchedHistory(CurrentUser == TraktSettings.Username ? "me" : CurrentUser, 1, TraktSettings.MaxUserWatchedMoviesRequest);

                    _RecentlyWatchedMovies = recentlyWatched;
                    if (userRecentlyWatchedMovies.Keys.Contains(CurrentUser)) userRecentlyWatchedMovies.Remove(CurrentUser);
                    userRecentlyWatchedMovies.Add(CurrentUser, _RecentlyWatchedMovies);
                    LastRequest = DateTime.UtcNow;
                    PreviousSelectedIndex = 0;
                }
                return userRecentlyWatchedMovies[CurrentUser];
            }
        }
        private IEnumerable<TraktMovieHistory> _RecentlyWatchedMovies = null;

        #endregion

        #region Public Properties

        public static string CurrentUser { get; set; }

        #endregion

        #region Base Overrides

        public override int GetID
        {
            get
            {
                return (int)TraktGUIWindows.RecentWatchedMovies;
            }
        }

        public override bool Init()
        {
            return Load(GUIGraphicsContext.Skin + @"\Trakt.RecentWatched.Movies.xml");
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

            // Load Watched History
            LoadRecentlyWatched();
        }

        protected override void OnPageDestroy(int new_windowId)
        {
            GUIMovieListItem.StopDownload = true;
            PreviousSelectedIndex = Facade.SelectedListItemIndex;
            ClearProperties();

            // save current layout
            TraktSettings.RecentWatchedMoviesDefaultLayout = (int)CurrentLayout;

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
                        PreviousUser = CurrentUser;
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
                case Action.ActionType.ACTION_PREVIOUS_MENU:
                    // restore current user
                    PreviousUser = CurrentUser;
                    CurrentUser = TraktSettings.Username;
                    base.OnAction(action);
                    break;
                case Action.ActionType.ACTION_PLAY:
                case Action.ActionType.ACTION_MUSIC_PLAY:
                    PreviousUser = CurrentUser;
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

            var selectedMovie = selectedItem.TVTag as TraktMovieSummary;
            if (selectedMovie == null) return;

            var dlg = (IDialogbox)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
            if (dlg == null) return;

            dlg.Reset();
            dlg.SetHeading(GUIUtils.PluginName());

            GUIListItem listItem = null;

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

            // Add to Collection
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

                case ((int)ContextMenuItem.Trailers):
                    PreviousUser = CurrentUser;
                    GUICommon.ShowMovieTrailersMenu(selectedMovie);
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
            GUIListItem selectedItem = this.Facade.SelectedListItem;
            if (selectedItem == null) return;

            var selectedMovie = selectedItem.TVTag as TraktMovieSummary;
            if (selectedMovie == null) return;

            GUICommon.CheckAndPlayMovie(jumpTo, selectedMovie);
        }

        private void LoadRecentlyWatched()
        {
            GUIUtils.SetProperty("#Trakt.Items", string.Empty);

            GUIBackgroundTask.Instance.ExecuteInBackgroundAndCallback(() =>
            {
                return RecentlyWatchedMovies;
            },
            delegate(bool success, object result)
            {
                if (success)
                {
                    var recentlyWatched = result as IEnumerable<TraktMovieHistory>;
                    SendRecentlyWatchedToFacade(recentlyWatched);
                }
            }, Translation.GettingUserWatchedHistory, true);
        }

        private void SendRecentlyWatchedToFacade(IEnumerable<TraktMovieHistory> recentlyWatched)
        {
            // clear facade
            GUIControl.ClearControl(GetID, Facade.GetID);

            // protected profiles might return null
            if (recentlyWatched == null || recentlyWatched.Count() == 0)
            {
                GUIUtils.ShowNotifyDialog(GUIUtils.PluginName(), Translation.UserHasNotWatchedMovies);
                PreviousUser = CurrentUser;
                CurrentUser = TraktSettings.Username;
                GUIWindowManager.ShowPreviousWindow();
                return;
            }

            int itemId = 0;
            var movieImages = new List<GUITmdbImage>();

            // Add each item watched
            foreach (var recent in recentlyWatched)
            {
                // bad data in API
                if (recent.Movie == null)
                    continue;

                // add image for download
                var images = new GUITmdbImage { MovieImages = new TmdbMovieImages { Id = recent.Movie.Ids.Tmdb } };
                movieImages.Add(images);

                var item = new GUIMovieListItem(recent.Movie.Title, (int)TraktGUIWindows.RecentWatchedMovies);

                // add user watched date as second label
                item.Label2 = recent.WatchedAt.ToPrettyDateTime();
                item.Date = recent.WatchedAt.FromISO8601().ToLongDateString();
                item.Action = recent.Action;
                item.TVTag = recent.Movie;
                item.Movie = recent.Movie;
                item.Images = images;
                item.ItemId = Int32.MaxValue - itemId;
                item.IsPlayed = CurrentUser != TraktSettings.Username ? recent.Movie.IsWatched() : false;
                item.IconImage = GUIImageHandler.GetDefaultPoster(false);
                item.IconImageBig = GUIImageHandler.GetDefaultPoster();
                item.ThumbnailImage = GUIImageHandler.GetDefaultPoster();
                item.OnItemSelected += OnMovieSelected;
                Utils.SetDefaultIcons(item);
                Facade.Add(item);
                itemId++;
            }

            // set Facade Layout
            Facade.CurrentLayout = CurrentLayout;
            GUIControl.FocusControl(GetID, Facade.GetID);

            if (PreviousSelectedIndex >= recentlyWatched.Count())
                Facade.SelectIndex(PreviousSelectedIndex - 1);
            else
                Facade.SelectIndex(PreviousSelectedIndex);

            // set facade properties
            GUIUtils.SetProperty("#itemcount", recentlyWatched.Count().ToString());
            GUIUtils.SetProperty("#Trakt.Items", string.Format("{0} {1}", recentlyWatched.Count().ToString(), recentlyWatched.Count() > 1 ? Translation.Movies : Translation.Movie));

            // Download movie images Async and set to facade
            GUIMovieListItem.GetImages(movieImages);
        }

        private void InitProperties()
        {
            // Fanart
            backdrop.GUIImageOne = FanartBackground;
            backdrop.GUIImageTwo = FanartBackground2;
            backdrop.LoadingImage = loadingImage;

            // load watched history for user
            if (string.IsNullOrEmpty(CurrentUser)) CurrentUser = TraktSettings.Username;
            GUICommon.SetProperty("#Trakt.RecentWatched.CurrentUser", CurrentUser);

            // load last layout
            CurrentLayout = (GUIFacadeControl.Layout)TraktSettings.RecentWatchedMoviesDefaultLayout;

            // Update Button States
            UpdateButtonState();

            // don't remember previous selected if a different user
            if (PreviousUser != CurrentUser)
                PreviousSelectedIndex = 0;
        }

        private void UpdateButtonState()
        {
            // update layout button label
            GUIControl.SetControlLabel(GetID, layoutButton.GetID, GUICommon.GetLayoutTranslation(CurrentLayout));
        }

        private void ClearProperties()
        {
            GUIUtils.SetProperty("#Trakt.Movie.WatchedDate", string.Empty);
            GUIUtils.SetProperty("#Trakt.Movie.Action", string.Empty);
            GUICommon.ClearMovieProperties();
        }

        private void OnMovieSelected(GUIListItem item, GUIControl parent)
        {
            var selectedMovie = item.TVTag as TraktMovieSummary;
            if (selectedMovie == null) return;

            PreviousSelectedIndex = Facade.SelectedListItemIndex;

            GUICommon.SetProperty("#Trakt.Movie.WatchedDate", (item as GUIMovieListItem).Date);
            GUICommon.SetProperty("#Trakt.Movie.Action", (item as GUIMovieListItem).Action);
            GUICommon.SetMovieProperties(selectedMovie);

            GUIImageHandler.LoadFanart(backdrop, TmdbCache.GetMovieBackdropFilename((item as GUIMovieListItem).Images.MovieImages));
        }
        #endregion
    }
}
