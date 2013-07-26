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
    public class GUIWatchListMovies : GUIWindow
    {
        #region Skin Controls

        [SkinControl(2)]
        protected GUIButtonControl layoutButton = null;

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

        public GUIWatchListMovies()
        {
            backdrop = new ImageSwapper();
            backdrop.PropertyOne = "#Trakt.WatchListMovies.Fanart.1";
            backdrop.PropertyTwo = "#Trakt.WatchListMovies.Fanart.2";
        }

        #endregion

        #region Private Variables

        bool StopDownload { get; set; }
        private Layout CurrentLayout { get; set; }
        static int PreviousSelectedIndex { get; set; }
        private ImageSwapper backdrop;
        static DateTime LastRequest = new DateTime();
        static Dictionary<string, IEnumerable<TraktWatchListMovie>> userWatchList = new Dictionary<string, IEnumerable<TraktWatchListMovie>>();

        static IEnumerable<TraktWatchListMovie> WatchListMovies
        {
            get
            {
                if (!userWatchList.Keys.Contains(CurrentUser) || LastRequest < DateTime.UtcNow.Subtract(new TimeSpan(0, TraktSettings.WebRequestCacheMinutes, 0)))
                {
                    _WatchListMovies = TraktAPI.TraktAPI.GetWatchListMovies(CurrentUser);
                    if (userWatchList.Keys.Contains(CurrentUser)) userWatchList.Remove(CurrentUser);
                    userWatchList.Add(CurrentUser, _WatchListMovies);
                    LastRequest = DateTime.UtcNow;
                    PreviousSelectedIndex = 0;
                }
                return userWatchList[CurrentUser];
            }
        }
        static IEnumerable<TraktWatchListMovie> _WatchListMovies = null;

        #endregion

        #region Public Properties

        public static string CurrentUser { get; set; }

        #endregion

        #region Base Overrides

        public override int GetID
        {
            get
            {
                return (int)TraktGUIWindows.WatchedListMovies;
            }
        }

        public override bool Init()
        {
            return Load(GUIGraphicsContext.Skin + @"\Trakt.WatchList.Movies.xml");
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

            // Load WatchList Movies
            LoadWatchListMovies();
        }

        protected override void OnPageDestroy(int new_windowId)
        {
            StopDownload = true;
            PreviousSelectedIndex = Facade.SelectedListItemIndex;
            ClearProperties();

            // save current layout
            TraktSettings.WatchListMoviesDefaultLayout = (int)CurrentLayout;

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
                    CurrentLayout = GUICommon.ShowLayoutMenu(CurrentLayout);
                    break;

                // Sort Button
                case (8):
                    var newSortBy = GUICommon.ShowSortMenu(TraktSettings.SortByWatchListMovies);
                    if (newSortBy != null)
                    {
                        if (newSortBy.Field != TraktSettings.SortByWatchListMovies.Field)
                        {
                            TraktSettings.SortByWatchListMovies = newSortBy;
                            PreviousSelectedIndex = 0;
                            UpdateButtonState();
                            LoadWatchListMovies();
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
                case Action.ActionType.ACTION_PREVIOUS_MENU:
                    // restore current user
                    CurrentUser = TraktSettings.Username;
                    base.OnAction(action);
                    break;
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

            TraktWatchListMovie selectedMovie = (TraktWatchListMovie)selectedItem.TVTag;

            IDialogbox dlg = (IDialogbox)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
            if (dlg == null) return;

            dlg.Reset();
            dlg.SetHeading(GUIUtils.PluginName());

            GUIListItem listItem = null;

            // only allow removal if viewing your own watch list
            if (CurrentUser == TraktSettings.Username)
            {
                listItem = new GUIListItem(Translation.RemoveFromWatchList);
                dlg.Add(listItem);
                listItem.ItemId = (int)ContextMenuItem.RemoveFromWatchList;
            }
            else if (!selectedMovie.InWatchList)
            {
                // viewing someone else's watch list and not in yours
                listItem = new GUIListItem(Translation.AddToWatchList);
                dlg.Add(listItem);
                listItem.ItemId = (int)ContextMenuItem.AddToWatchList;
            }

            // Add to Custom List
            listItem = new GUIListItem(Translation.AddToList + "...");
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

            if (TraktHelper.IsOnlineVideosAvailableAndEnabled)
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
                    if (CurrentUser != TraktSettings.Username)
                    {
                        if (selectedMovie.Plays == 0) selectedMovie.Plays = 1;
                        selectedMovie.Watched = true;
                        selectedItem.IsPlayed = true;
                        OnMovieSelected(selectedItem, Facade);
                        selectedMovie.Images.NotifyPropertyChanged("PosterImageFilename");
                        GUIWatchListMovies.ClearCache(TraktSettings.Username);
                    }
                    else
                    {
                        // when marking a movie as seen via API, it will remove from watch list
                        // we should do the same in GUI
                        PreviousSelectedIndex = this.Facade.SelectedListItemIndex;
                        if (_WatchListMovies.Count() >= 1)
                        {
                            // remove from list
                            var moviesToExcept = new List<TraktWatchListMovie>();
                            moviesToExcept.Add(selectedMovie);
                            _WatchListMovies = WatchListMovies.Except(moviesToExcept);
                            userWatchList[CurrentUser] = _WatchListMovies;
                            LoadWatchListMovies();
                        }
                        else
                        {
                            // no more movies left
                            ClearProperties();
                            GUIControl.ClearControl(GetID, Facade.GetID);
                            _WatchListMovies = null;
                            userWatchList.Remove(CurrentUser);
                            // notify and exit
                            GUIUtils.ShowNotifyDialog(GUIUtils.PluginName(), Translation.NoMovieWatchList);
                            GUIWindowManager.ShowPreviousWindow();
                            return;
                        }
                    }
                    break;

                case ((int)ContextMenuItem.MarkAsUnWatched):
                    TraktHelper.MarkMovieAsUnWatched(selectedMovie);
                    selectedMovie.Watched = false;
                    selectedItem.IsPlayed = false;
                    OnMovieSelected(selectedItem, Facade);
                    selectedMovie.Images.NotifyPropertyChanged("PosterImageFilename");
                    break;

                case ((int)ContextMenuItem.AddToWatchList):
                    TraktHelper.AddMovieToWatchList(selectedMovie, true);
                    selectedMovie.InWatchList = true;
                    OnMovieSelected(selectedItem, Facade);
                    selectedMovie.Images.NotifyPropertyChanged("PosterImageFilename");
                    break;

                case ((int)ContextMenuItem.RemoveFromWatchList):
                    PreviousSelectedIndex = this.Facade.SelectedListItemIndex;
                    TraktHelper.RemoveMovieFromWatchList(selectedMovie, true);
                    if (_WatchListMovies.Count() >= 1)
                    {
                        // remove from list
                        var moviesToExcept = new List<TraktWatchListMovie>();
                        moviesToExcept.Add(selectedMovie);
                        _WatchListMovies = WatchListMovies.Except(moviesToExcept);
                        userWatchList[CurrentUser] = _WatchListMovies;
                        LoadWatchListMovies();
                    }
                    else
                    {
                        // no more movies left
                        ClearProperties();
                        GUIControl.ClearControl(GetID, Facade.GetID);
                        _WatchListMovies = null;
                        userWatchList.Remove(CurrentUser);
                        // notify and exit
                        GUIUtils.ShowNotifyDialog(GUIUtils.PluginName(), Translation.NoMovieWatchList);
                        GUIWindowManager.ShowPreviousWindow();
                        return;                    
                    }
                    break;

                case ((int)ContextMenuItem.AddToList):
                    TraktHelper.AddRemoveMovieInUserList(selectedMovie.Title, selectedMovie.Year, selectedMovie.IMDBID, false);
                    break;

                case ((int)ContextMenuItem.Trailers):
                    GUICommon.ShowMovieTrailersMenu(selectedMovie);
                    break;

                case ((int)ContextMenuItem.AddToLibrary):
                    TraktHelper.AddMovieToLibrary(selectedMovie);
                    selectedMovie.InCollection = true;
                    OnMovieSelected(selectedItem, Facade);
                    selectedMovie.Images.NotifyPropertyChanged("PosterImageFilename");
                    if (CurrentUser != TraktSettings.Username) GUIWatchListMovies.ClearCache(TraktSettings.Username);
                    break;

                case ((int)ContextMenuItem.RemoveFromLibrary):
                    TraktHelper.RemoveMovieFromLibrary(selectedMovie);
                    selectedMovie.InCollection = false;
                    OnMovieSelected(selectedItem, Facade);
                    selectedMovie.Images.NotifyPropertyChanged("PosterImageFilename");
                    if (CurrentUser != TraktSettings.Username) GUIWatchListMovies.ClearCache(TraktSettings.Username);
                    break;

                case ((int)ContextMenuItem.Related):
                    TraktHelper.ShowRelatedMovies(selectedMovie);
                    break;

                case ((int)ContextMenuItem.Rate):
                    GUICommon.RateMovie(selectedMovie);
                    OnMovieSelected(selectedItem, Facade);
                    selectedMovie.Images.NotifyPropertyChanged("PosterImageFilename");
                    if (CurrentUser != TraktSettings.Username) GUIWatchListMovies.ClearCache(TraktSettings.Username);
                    break;

                case ((int)ContextMenuItem.Shouts):
                    TraktHelper.ShowMovieShouts(selectedMovie);
                    break;

                case ((int)ContextMenuItem.ChangeLayout):
                    CurrentLayout = GUICommon.ShowLayoutMenu(CurrentLayout);
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

            TraktWatchListMovie selectedMovie = selectedItem.TVTag as TraktWatchListMovie;
            if (selectedMovie == null) return;

            GUICommon.CheckAndPlayMovie(jumpTo, selectedMovie);
        }

        private void LoadWatchListMovies()
        {
            GUIUtils.SetProperty("#Trakt.Items", string.Empty);

            GUIBackgroundTask.Instance.ExecuteInBackgroundAndCallback(() =>
            {
                return WatchListMovies;
            },
            delegate(bool success, object result)
            {
                if (success)
                {
                    IEnumerable<TraktWatchListMovie> movies = result as IEnumerable<TraktWatchListMovie>;
                    SendWatchListMoviesToFacade(movies);
                }
            }, Translation.GettingWatchListMovies, true);
        }

        private void SendWatchListMoviesToFacade(IEnumerable<TraktWatchListMovie> movies)
        {
            // clear facade
            GUIControl.ClearControl(GetID, Facade.GetID);

            if (movies.Count() == 0)
            {
                GUIUtils.ShowNotifyDialog(GUIUtils.PluginName(), string.Format(Translation.NoMovieWatchList, CurrentUser));
                CurrentUser = TraktSettings.Username;
                GUIWindowManager.ShowPreviousWindow();
                return;
            }

            // sort movies
            var movieList = movies.ToList();
            movieList.Sort(new GUIListItemMovieSorter(TraktSettings.SortByWatchListMovies.Field, TraktSettings.SortByWatchListMovies.Direction));

            int itemId = 0;
            List<TraktMovie.MovieImages> movieImages = new List<TraktMovie.MovieImages>();

            // Add each movie
            foreach (var movie in movieList)
            {
                GUITraktWatchListMovieListItem item = new GUITraktWatchListMovieListItem(movie.Title);

                item.Label2 = movie.Year;
                item.TVTag = movie;
                item.Item = movie.Images;
                item.ItemId = Int32.MaxValue - itemId;
                item.IsPlayed = movie.Watched;
                item.IconImage = GUIImageHandler.GetDefaultPoster(false);
                item.IconImageBig = GUIImageHandler.GetDefaultPoster();
                item.ThumbnailImage = GUIImageHandler.GetDefaultPoster();
                item.OnItemSelected += OnMovieSelected;
                Utils.SetDefaultIcons(item);
                Facade.Add(item);
                itemId++;

                // add image for download
                movieImages.Add(movie.Images);
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
            GetImages(movieImages);
        }

        private void InitProperties()
        {
            // Fanart
            backdrop.GUIImageOne = FanartBackground;
            backdrop.GUIImageTwo = FanartBackground2;
            backdrop.LoadingImage = loadingImage;

            // load Watch list for user
            if (string.IsNullOrEmpty(CurrentUser)) CurrentUser = TraktSettings.Username;
            GUICommon.SetProperty("#Trakt.WatchList.CurrentUser", CurrentUser);

            // load last layout
            CurrentLayout = (Layout)TraktSettings.WatchListMoviesDefaultLayout;

            // Update Button States
            UpdateButtonState();

            if (sortButton != null)
            {
                sortButton.SortChanged += (o, e) =>
                {
                    TraktSettings.SortByWatchListMovies.Direction = (SortingDirections)(e.Order - 1);
                    PreviousSelectedIndex = 0;
                    UpdateButtonState();
                    LoadWatchListMovies();
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
                sortButton.Label = GUICommon.GetSortByString(TraktSettings.SortByWatchListMovies);
                sortButton.IsAscending = (TraktSettings.SortByWatchListMovies.Direction == SortingDirections.Ascending);
            }
            GUIUtils.SetProperty("#Trakt.SortBy", GUICommon.GetSortByString(TraktSettings.SortByWatchListMovies));
        }

        private void ClearProperties()
        {
            GUIUtils.SetProperty("#Trakt.Movie.WatchList.Inserted", string.Empty);
            GUICommon.ClearMovieProperties();
        }

        private void PublishMovieSkinProperties(TraktWatchListMovie movie)
        {
            GUICommon.SetProperty("#Trakt.Movie.WatchList.Inserted", movie.Inserted.FromEpoch().ToShortDateString());
            GUICommon.SetMovieProperties(movie);
        }

        private void OnMovieSelected(GUIListItem item, GUIControl parent)
        {
            TraktWatchListMovie movie = item.TVTag as TraktWatchListMovie;
            PublishMovieSkinProperties(movie);
            GUIImageHandler.LoadFanart(backdrop, movie.Images.FanartImageFilename);
        }

        private void GetImages(List<TraktMovie.MovieImages> itemsWithThumbs)
        {
            StopDownload = false;

            // split the downloads in 5+ groups and do multithreaded downloading
            int groupSize = (int)Math.Max(1, Math.Floor((double)itemsWithThumbs.Count / 5));
            int groups = (int)Math.Ceiling((double)itemsWithThumbs.Count() / groupSize);

            for (int i = 0; i < groups; i++)
            {
                List<TraktMovie.MovieImages> groupList = new List<TraktMovie.MovieImages>();
                for (int j = groupSize * i; j < groupSize * i + (groupSize * (i + 1) > itemsWithThumbs.Count ? itemsWithThumbs.Count - groupSize * i : groupSize); j++)
                {
                    groupList.Add(itemsWithThumbs[j]);
                }

                new Thread(delegate(object o)
                {
                    List<TraktMovie.MovieImages> items = (List<TraktMovie.MovieImages>)o;
                    foreach (TraktMovie.MovieImages item in items)
                    {
                        #region Poster
                        // stop download if we have exited window
                        if (StopDownload) break;

                        string remoteThumb = item.Poster;
                        string localThumb = item.PosterImageFilename;

                        if (!string.IsNullOrEmpty(remoteThumb) && !string.IsNullOrEmpty(localThumb))
                        {
                            if (GUIImageHandler.DownloadImage(remoteThumb, localThumb))
                            {
                                // notify that image has been downloaded
                                item.NotifyPropertyChanged("PosterImageFilename");
                            }
                        }
                        #endregion

                        #region Fanart
                        // stop download if we have exited window
                        if (StopDownload) break;
                        if (!TraktSettings.DownloadFanart) continue;

                        string remoteFanart = item.Fanart;
                        string localFanart = item.FanartImageFilename;

                        if (!string.IsNullOrEmpty(remoteFanart) && !string.IsNullOrEmpty(localFanart))
                        {
                            if (GUIImageHandler.DownloadImage(remoteFanart, localFanart))
                            {
                                // notify that image has been downloaded
                                item.NotifyPropertyChanged("FanartImageFilename");
                            }
                        }
                        #endregion
                    }
                })
                {
                    IsBackground = true,
                    Name = "ImageDownloader" + i.ToString()
                }.Start(groupList);
            }
        }

        #endregion

        #region Public Methods

        public static void ClearCache(string username)
        {
            if (userWatchList.Keys.Contains(username)) userWatchList.Remove(username);
        }

        #endregion
    }

    public class GUITraktWatchListMovieListItem : GUIListItem
    {
        public GUITraktWatchListMovieListItem(string strLabel) : base(strLabel) { }

        public object Item
        {
            get { return _Item; }
            set
            {
                _Item = value;
                INotifyPropertyChanged notifier = value as INotifyPropertyChanged;
                if (notifier != null) notifier.PropertyChanged += (s, e) =>
                {
                    if (s is TraktMovie.MovieImages && e.PropertyName == "PosterImageFilename")
                        SetImageToGui((s as TraktMovie.MovieImages).PosterImageFilename);
                    if (s is TraktMovie.MovieImages && e.PropertyName == "FanartImageFilename")
                        this.UpdateItemIfSelected((int)TraktGUIWindows.WatchedListMovies, ItemId);
                };
            }
        } protected object _Item;

        /// <summary>
        /// Loads an Image from memory into a facade item
        /// </summary>
        /// <param name="imageFilePath">Filename of image</param>
        protected void SetImageToGui(string imageFilePath)
        {
            if (string.IsNullOrEmpty(imageFilePath)) return;

            // determine the overlay to add to poster
            TraktWatchListMovie movie = TVTag as TraktWatchListMovie;
            MainOverlayImage mainOverlay = MainOverlayImage.None;

            // only show watch list icon if viewing someone elses watch list
            if ((GUIWatchListMovies.CurrentUser != TraktSettings.Username) && movie.InWatchList)
                mainOverlay = MainOverlayImage.Watchlist;
            else if (movie.Watched)
                mainOverlay = MainOverlayImage.Seenit;

            // add additional overlay if applicable
            if (movie.InCollection)
                mainOverlay |= MainOverlayImage.Library;

            RatingOverlayImage ratingOverlay = GUIImageHandler.GetRatingOverlay(movie.RatingAdvanced);

            // get a reference to a MediaPortal Texture Identifier
            string suffix = mainOverlay.ToString().Replace(", ", string.Empty) + Enum.GetName(typeof(RatingOverlayImage), ratingOverlay);
            string texture = GUIImageHandler.GetTextureIdentFromFile(imageFilePath, suffix);

            // build memory image
            Image memoryImage = null;
            if (mainOverlay != MainOverlayImage.None || ratingOverlay != RatingOverlayImage.None)
            {
                memoryImage = GUIImageHandler.DrawOverlayOnPoster(imageFilePath, mainOverlay, ratingOverlay);
                if (memoryImage == null) return;

                // load texture into facade item
                if (GUITextureManager.LoadFromMemory(memoryImage, texture, 0, 0, 0) > 0)
                {
                    ThumbnailImage = texture;
                    IconImage = texture;
                    IconImageBig = texture;
                }
            }
            else
            {
                ThumbnailImage = imageFilePath;
                IconImage = imageFilePath;
                IconImageBig = imageFilePath;
            }

            // if selected and is current window force an update of thumbnail
            this.UpdateItemIfSelected((int)TraktGUIWindows.WatchedListMovies, ItemId);
        }
    }
}