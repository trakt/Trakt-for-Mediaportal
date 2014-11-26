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
using TraktPlugin.TraktAPI.v1;
using TraktPlugin.TraktAPI.v1.DataStructures;
using TraktPlugin.TraktAPI.v1.Extensions;

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
        Layout CurrentLayout { get; set; }
        ImageSwapper backdrop;        
        Dictionary<string, IEnumerable<TraktActivity.Activity>> userRecentlyWatchedMovies = new Dictionary<string, IEnumerable<TraktActivity.Activity>>();

        IEnumerable<TraktActivity.Activity> RecentlyWatchedMovies
        {
            get
            {
                if (!userRecentlyWatchedMovies.Keys.Contains(CurrentUser) || LastRequest < DateTime.UtcNow.Subtract(new TimeSpan(0, TraktSettings.WebRequestCacheMinutes, 0)))
                {
                    TraktActivity activity = TraktAPI.v1.TraktAPI.GetUserActivity
                    (
                        CurrentUser,
                        new List<TraktAPI.v1.ActivityType>() { TraktAPI.v1.ActivityType.movie },
                        new List<TraktAPI.v1.ActivityAction>() { TraktAPI.v1.ActivityAction.checkin, TraktAPI.v1.ActivityAction.scrobble }
                    );

                    _RecentlyWatchedMovies = activity.Activities;
                    if (userRecentlyWatchedMovies.Keys.Contains(CurrentUser)) userRecentlyWatchedMovies.Remove(CurrentUser);
                    userRecentlyWatchedMovies.Add(CurrentUser, _RecentlyWatchedMovies);
                    LastRequest = DateTime.UtcNow;
                    PreviousSelectedIndex = 0;
                }
                return userRecentlyWatchedMovies[CurrentUser];
            }
        }
        private IEnumerable<TraktActivity.Activity> _RecentlyWatchedMovies = null;

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
            GUIListItem selectedItem = this.Facade.SelectedListItem;
            if (selectedItem == null) return;

            var selectedMovie = selectedItem.TVTag as TraktMovie;
            if (selectedMovie == null) return;

            IDialogbox dlg = (IDialogbox)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
            if (dlg == null) return;

            dlg.Reset();
            dlg.SetHeading(GUIUtils.PluginName());

            GUIListItem listItem = null;

            if (!selectedMovie.InWatchList)
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
            if (!selectedMovie.Watched)
            {
                listItem = new GUIListItem(Translation.MarkAsWatched);
                dlg.Add(listItem);
                listItem.ItemId = (int)ContextMenuItem.MarkAsWatched;
            }

            // Mark As UnWatched
            if (selectedMovie.Watched)
            {
                listItem = new GUIListItem(Translation.MarkAsUnWatched);
                dlg.Add(listItem);
                listItem.ItemId = (int)ContextMenuItem.MarkAsUnWatched;
            }

            // Add to Library
            // Don't allow if it will be removed again on next sync
            // movie could be part of a DVD collection
            if (!selectedMovie.InCollection && !TraktSettings.KeepTraktLibraryClean)
            {
                listItem = new GUIListItem(Translation.AddToLibrary);
                dlg.Add(listItem);
                listItem.ItemId = (int)ContextMenuItem.AddToLibrary;
            }

            if (selectedMovie.InCollection)
            {
                listItem = new GUIListItem(Translation.RemoveFromLibrary);
                dlg.Add(listItem);
                listItem.ItemId = (int)ContextMenuItem.RemoveFromLibrary;
            }

            // Related Movies
            listItem = new GUIListItem(Translation.RelatedMovies + "...");
            dlg.Add(listItem);
            listItem.ItemId = (int)ContextMenuItem.Related;

            // Rate Movie
            listItem = new GUIListItem(Translation.RateMovie);
            dlg.Add(listItem);
            listItem.ItemId = (int)ContextMenuItem.Rate;

            // Shouts
            listItem = new GUIListItem(Translation.Shouts + "...");
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

            if (!selectedMovie.InCollection && TraktHelper.IsMpNZBAvailableAndEnabled)
            {
                // Search for movie with mpNZB
                listItem = new GUIListItem(Translation.SearchWithMpNZB);
                dlg.Add(listItem);
                listItem.ItemId = (int)ContextMenuItem.SearchWithMpNZB;
            }

            if (!selectedMovie.InCollection && TraktHelper.IsMyTorrentsAvailableAndEnabled)
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
                    TraktHelper.MarkMovieAsWatched(selectedMovie);
                    if (selectedMovie.Plays == 0) selectedMovie.Plays = 1;
                    selectedMovie.Watched = true;
                    selectedItem.IsPlayed = true;
                    OnMovieSelected(selectedItem, Facade);
                    (Facade.SelectedListItem as GUIMovieListItem).Images.NotifyPropertyChanged("Poster");
                    break;

                case ((int)ContextMenuItem.MarkAsUnWatched):
                    TraktHelper.MarkMovieAsUnWatched(selectedMovie);
                    selectedMovie.Watched = false;
                    selectedItem.IsPlayed = false;
                    OnMovieSelected(selectedItem, Facade);
                    (Facade.SelectedListItem as GUIMovieListItem).Images.NotifyPropertyChanged("Poster");
                    break;

                case ((int)ContextMenuItem.AddToWatchList):
                    TraktHelper.AddMovieToWatchList(selectedMovie, true);
                    selectedMovie.InWatchList = true;
                    OnMovieSelected(selectedItem, Facade);
                    (Facade.SelectedListItem as GUIMovieListItem).Images.NotifyPropertyChanged("Poster");
                    break;

                case ((int)ContextMenuItem.RemoveFromWatchList):
                    TraktHelper.RemoveMovieFromWatchList(selectedMovie, true);
                    selectedMovie.InWatchList = false;
                    OnMovieSelected(selectedItem, Facade);
                    (Facade.SelectedListItem as GUIMovieListItem).Images.NotifyPropertyChanged("Poster");
                    break;

                case ((int)ContextMenuItem.AddToList):
                    TraktHelper.AddRemoveMovieInUserList(selectedMovie.Title, selectedMovie.Year, selectedMovie.IMDBID, false);
                    break;

                case ((int)ContextMenuItem.Trailers):
                    PreviousUser = CurrentUser;
                    GUICommon.ShowMovieTrailersMenu(selectedMovie);
                    break;

                case ((int)ContextMenuItem.AddToLibrary):
                    TraktHelper.AddMovieToLibrary(selectedMovie);
                    selectedMovie.InCollection = true;
                    OnMovieSelected(selectedItem, Facade);
                    (Facade.SelectedListItem as GUIMovieListItem).Images.NotifyPropertyChanged("Poster");
                    break;

                case ((int)ContextMenuItem.RemoveFromLibrary):
                    TraktHelper.RemoveMovieFromLibrary(selectedMovie);
                    selectedMovie.InCollection = false;
                    OnMovieSelected(selectedItem, Facade);
                    (Facade.SelectedListItem as GUIMovieListItem).Images.NotifyPropertyChanged("Poster");
                    break;

                case ((int)ContextMenuItem.Related):
                    RelatedMovie relatedMovie = new RelatedMovie();
                    relatedMovie.ImdbId = selectedMovie.IMDBID;
                    relatedMovie.Title = selectedMovie.Title;
                    GUIRelatedMovies.relatedMovie = relatedMovie;
                    GUIWindowManager.ActivateWindow((int)TraktGUIWindows.RelatedMovies);
                    break;

                case ((int)ContextMenuItem.Rate):
                    GUICommon.RateMovie(selectedMovie);
                    OnMovieSelected(selectedItem, Facade);
                    (Facade.SelectedListItem as GUIMovieListItem).Images.NotifyPropertyChanged("Poster");
                    break;

                case ((int)ContextMenuItem.Shouts):
                    TraktHelper.ShowMovieShouts(selectedMovie);
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

            var selectedMovie = selectedItem.TVTag as TraktMovie;
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
                    IEnumerable<TraktActivity.Activity> activities = result as IEnumerable<TraktActivity.Activity>;
                    SendRecentlyWatchedToFacade(activities);
                }
            }, Translation.GettingUserWatchedHistory, true);
        }

        private void SendRecentlyWatchedToFacade(IEnumerable<TraktActivity.Activity> activities)
        {
            // clear facade
            GUIControl.ClearControl(GetID, Facade.GetID);

            // protected profiles might return null
            if (activities == null || activities.Count() == 0)
            {
                GUIUtils.ShowNotifyDialog(GUIUtils.PluginName(), Translation.UserHasNotWatchedMovies);
                PreviousUser = CurrentUser;
                CurrentUser = TraktSettings.Username;
                GUIWindowManager.ShowPreviousWindow();
                return;
            }

            int itemId = 0;
            var movieImages = new List<GUIImage>();

            // Add each item watched
            foreach (var activity in activities)
            {
                // bad data in API
                if (activity.Movie == null)
                    continue;

                // add image for download
                var images = new GUIImage { MovieImages = activity.Movie.Images };
                movieImages.Add(images);

                var item = new GUIMovieListItem(activity.Movie.Title, (int)TraktGUIWindows.RecentWatchedMovies);

                // add user watched date as second label
                item.Label2 = activity.Timestamp.FromEpoch().ToShortDateString();
                item.Date = activity.Timestamp.FromEpoch().ToLongDateString();
                item.TVTag = activity.Movie;
                item.Images = images;
                item.ItemId = Int32.MaxValue - itemId;
                item.IsPlayed = activity.Movie.Watched;
                item.IconImage = GUIImageHandler.GetDefaultPoster(false);
                item.IconImageBig = GUIImageHandler.GetDefaultPoster();
                item.ThumbnailImage = GUIImageHandler.GetDefaultPoster();
                item.OnItemSelected += OnMovieSelected;
                Utils.SetDefaultIcons(item);
                Facade.Add(item);
                itemId++;
            }

            // set Facade Layout
            Facade.SetCurrentLayout(Enum.GetName(typeof(Layout), CurrentLayout));
            GUIControl.FocusControl(GetID, Facade.GetID);

            if (PreviousSelectedIndex >= activities.Count())
                Facade.SelectIndex(PreviousSelectedIndex - 1);
            else
                Facade.SelectIndex(PreviousSelectedIndex);

            // set facade properties
            GUIUtils.SetProperty("#itemcount", activities.Count().ToString());
            GUIUtils.SetProperty("#Trakt.Items", string.Format("{0} {1}", activities.Count().ToString(), activities.Count() > 1 ? Translation.Movies : Translation.Movie));

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
            CurrentLayout = (Layout)TraktSettings.RecentWatchedMoviesDefaultLayout;

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
            GUICommon.ClearMovieProperties();
        }

        private void OnMovieSelected(GUIListItem item, GUIControl parent)
        {
            var selectedMovie = item.TVTag as TraktMovie;
            if (selectedMovie == null) return;

            PreviousSelectedIndex = Facade.SelectedListItemIndex;

            GUICommon.SetProperty("#Trakt.Movie.WatchedDate", (item as GUIMovieListItem).Date);
            GUICommon.SetMovieProperties(selectedMovie);

            GUIImageHandler.LoadFanart(backdrop, selectedMovie.Images.Fanart.LocalImageFilename(ArtworkType.MovieFanart));
        }
        #endregion
    }
}
