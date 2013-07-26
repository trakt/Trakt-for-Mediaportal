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
    public class RelatedMovie
    {
        public string Title { get; set; }
        public int Year { get; set; }
        public string IMDbId { get; set; }

        public string Slug
        {
            get
            {
                if (!string.IsNullOrEmpty(TraktHandlers.BasicHandler.GetProperMovieImdbId(IMDbId))) return IMDbId;
                if (string.IsNullOrEmpty(Title)) return string.Empty;
                return string.Format("{0} {1}", Title, Year).ToSlug();
            }
        }
    }

    public class GUIRelatedMovies : GUIWindow
    {
        #region Skin Controls

        [SkinControl(2)]
        protected GUIButtonControl layoutButton = null;

        [SkinControl(3)]
        protected GUICheckButton hideWatchedButton = null;

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
            ChangeLayout,
            Trailers,
            SearchWithMpNZB,
            SearchTorrent
        }

        #endregion

        #region Constructor

        public GUIRelatedMovies()
        {
            backdrop = new ImageSwapper();
            backdrop.PropertyOne = "#Trakt.RelatedMovies.Fanart.1";
            backdrop.PropertyTwo = "#Trakt.RelatedMovies.Fanart.2";
        }

        #endregion

        #region Public Variables

        public static RelatedMovie relatedMovie { get; set; }

        #endregion

        #region Private Variables

        bool StopDownload { get; set; }
        private Layout CurrentLayout { get; set; }
        private ImageSwapper backdrop;
        DateTime LastRequest = new DateTime();
        int PreviousSelectedIndex = 0;
        Dictionary<string, IEnumerable<TraktMovie>> dictRelatedMovies = new Dictionary<string, IEnumerable<TraktMovie>>();
        bool HideWatched = false;
        bool SendingWatchedToTrakt = false;
        bool RelationChanged = false;

        IEnumerable<TraktMovie> RelatedMovies
        {
            get
            {
                if (!dictRelatedMovies.Keys.Contains(relatedMovie.Slug) || LastRequest < DateTime.UtcNow.Subtract(new TimeSpan(0, TraktSettings.WebRequestCacheMinutes, 0)))
                {
                    _RelatedMovies = TraktAPI.TraktAPI.GetRelatedMovies(relatedMovie.Slug, HideWatched);
                    if (dictRelatedMovies.Keys.Contains(relatedMovie.Slug)) dictRelatedMovies.Remove(relatedMovie.Slug);
                    dictRelatedMovies.Add(relatedMovie.Slug, _RelatedMovies);
                    LastRequest = DateTime.UtcNow;
                    PreviousSelectedIndex = 0;
                }
                return dictRelatedMovies[relatedMovie.Slug];
            }
        }
        private IEnumerable<TraktMovie> _RelatedMovies = null;

        #endregion

        #region Base Overrides

        public override int GetID
        {
            get
            {
                return (int)TraktGUIWindows.RelatedMovies;
            }
        }

        public override bool Init()
        {
            return Load(GUIGraphicsContext.Skin + @"\Trakt.Related.Movies.xml");
        }

        protected override void OnPageLoad()
        {
            base.OnPageLoad();

            if (relatedMovie == null)
            {
                GUIWindowManager.ActivateWindow(GUIWindowManager.GetPreviousActiveWindow());
                return;
            }

            // Clear GUI Properties
            ClearProperties();

            // Init Properties
            InitProperties();

            // Load Related Movies
            LoadRelatedMovies();
        }

        protected override void OnPageDestroy(int new_windowId)
        {
            StopDownload = true;            
            ClearProperties();

            if (RelationChanged)
                PreviousSelectedIndex = 0;
            else 
                PreviousSelectedIndex = Facade.SelectedListItemIndex;

            // save settings
            TraktSettings.RelatedMoviesDefaultLayout = (int)CurrentLayout;
            TraktSettings.HideWatchedRelatedMovies = HideWatched;

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
                
                // Hide Watched Button
                case (3):
                    HideWatched = hideWatchedButton.Selected;
                    dictRelatedMovies.Remove(relatedMovie.Slug);
                    LoadRelatedMovies();
                    GUIControl.FocusControl((int)TraktGUIWindows.RelatedMovies, Facade.GetID);
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

            GUIListItem selectedItem = this.Facade.SelectedListItem;
            if (selectedItem == null) return;

            TraktMovie selectedMovie = (TraktMovie)selectedItem.TVTag;

            IDialogbox dlg = (IDialogbox)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
            if (dlg == null) return;

            dlg.Reset();
            dlg.SetHeading(GUIUtils.PluginName());

            GUIListItem listItem = null;

            // Hide/Show Watched items
            listItem = new GUIListItem(HideWatched ? Translation.ShowWatched : Translation.HideWatched);
            dlg.Add(listItem);
            listItem.ItemId = (int)ContextMenuItem.HideShowWatched;

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

            // Add/Remove Watch List            
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

            // Add to Custom list
            listItem = new GUIListItem(Translation.AddToList + "...");
            dlg.Add(listItem);
            listItem.ItemId = (int)ContextMenuItem.AddToList;

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

            // Trailers
            if (TraktHelper.IsOnlineVideosAvailableAndEnabled)
            {
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
                case ((int)ContextMenuItem.HideShowWatched):
                    HideWatched = !HideWatched;
                    if (hideWatchedButton != null) hideWatchedButton.Selected = HideWatched;
                    dictRelatedMovies.Remove(relatedMovie.Slug);
                    LoadRelatedMovies();
                    break;

                case ((int)ContextMenuItem.MarkAsWatched):
                    TraktHelper.MarkMovieAsWatched(selectedMovie);
                    if (!HideWatched)
                    {
                        if (selectedMovie.Plays == 0) selectedMovie.Plays = 1;
                        selectedMovie.Watched = true;
                        selectedItem.IsPlayed = true;
                        OnMovieSelected(selectedItem, Facade);
                        selectedMovie.Images.NotifyPropertyChanged("PosterImageFilename");
                    }
                    else
                    {
                        dictRelatedMovies.Remove(relatedMovie.Slug);
                        LoadRelatedMovies();
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
                    TraktHelper.RemoveMovieFromWatchList(selectedMovie, true);
                    selectedMovie.InWatchList = false;
                    OnMovieSelected(selectedItem, Facade);
                    selectedMovie.Images.NotifyPropertyChanged("PosterImageFilename");
                    break;

                case ((int)ContextMenuItem.AddToList):
                    TraktHelper.AddRemoveMovieInUserList(selectedMovie.Title, selectedMovie.Year, selectedMovie.IMDBID, false);
                    break;

                case ((int)ContextMenuItem.AddToLibrary):
                    TraktHelper.AddMovieToLibrary(selectedMovie);
                    selectedMovie.InCollection = true;
                    OnMovieSelected(selectedItem, Facade);
                    selectedMovie.Images.NotifyPropertyChanged("PosterImageFilename");
                    break;

                case ((int)ContextMenuItem.RemoveFromLibrary):
                    TraktHelper.RemoveMovieFromLibrary(selectedMovie);
                    selectedMovie.InCollection = false;
                    OnMovieSelected(selectedItem, Facade);
                    selectedMovie.Images.NotifyPropertyChanged("PosterImageFilename");
                    break;

                case ((int)ContextMenuItem.Related):
                    RelatedMovie relMovie = new RelatedMovie
                    {
                        Title = selectedMovie.Title,
                        IMDbId = selectedMovie.IMDBID,
                        Year = Convert.ToInt32(selectedMovie.Year)
                    };
                    relatedMovie = relMovie;
                    GUIUtils.SetProperty("#Trakt.Related.Movie", relMovie.Title);
                    LoadRelatedMovies();
                    RelationChanged = true;
                    break;

                case ((int)ContextMenuItem.Rate):
                    GUICommon.RateMovie(selectedMovie);
                    OnMovieSelected(selectedItem, Facade);
                    selectedMovie.Images.NotifyPropertyChanged("PosterImageFilename");
                    break;

                case ((int)ContextMenuItem.Shouts):
                    TraktHelper.ShowMovieShouts(selectedMovie);
                    break;

                case ((int)ContextMenuItem.Trailers):
                    GUICommon.ShowMovieTrailersMenu(selectedMovie);
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

            TraktMovie selectedMovie = selectedItem.TVTag as TraktMovie;
            if (selectedMovie == null) return;

            GUICommon.CheckAndPlayMovie(jumpTo, selectedMovie);
        }

        private void LoadRelatedMovies()
        {
            GUIUtils.SetProperty("#Trakt.Items", string.Empty);

            GUIBackgroundTask.Instance.ExecuteInBackgroundAndCallback(() =>
            {
                if (hideWatchedButton != null)
                {
                    GUIControl.DisableControl((int)TraktGUIWindows.RelatedMovies, hideWatchedButton.GetID);
                }

                if (HideWatched)
                {
                    // wait until watched item has been sent to trakt or timesout (10secs)
                    while (SendingWatchedToTrakt) Thread.Sleep(500);
                }
                return RelatedMovies;
            },
            delegate(bool success, object result)
            {
                if (hideWatchedButton != null)
                {
                    GUIControl.EnableControl((int)TraktGUIWindows.RelatedMovies, hideWatchedButton.GetID);
                }

                if (success)
                {
                    IEnumerable<TraktMovie> movies = result as IEnumerable<TraktMovie>;
                    SendRelatedMoviesToFacade(movies);
                }
            }, Translation.GettingRelatedMovies, true);
        }

        private void SendRelatedMoviesToFacade(IEnumerable<TraktMovie> movies)
        {
            // clear facade
            GUIControl.ClearControl(GetID, Facade.GetID);

            if (movies.Count() == 0)
            {
                string title = string.IsNullOrEmpty(relatedMovie.Title) ? relatedMovie.IMDbId : relatedMovie.Title;
                GUIUtils.ShowNotifyDialog(GUIUtils.PluginName(), string.Format(Translation.NoRelatedMovies, title));
                GUIWindowManager.ShowPreviousWindow();
                return;
            }

            int itemId = 0;
            List<TraktMovie.MovieImages> movieImages = new List<TraktMovie.MovieImages>();

            // Add each movie mark remote if not in collection            
            foreach (var movie in movies)
            {
                GUITraktRelatedMovieListItem item = new GUITraktRelatedMovieListItem(movie.Title);

                item.Label2 = movie.Year;
                item.TVTag = movie;
                item.Item = movie.Images;
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

                // add image for download
                movieImages.Add(movie.Images);
            }

            // Set Facade Layout
            Facade.SetCurrentLayout(Enum.GetName(typeof(Layout), CurrentLayout));
            GUIControl.FocusControl(GetID, Facade.GetID);

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

            // set context property
            string title = string.IsNullOrEmpty(relatedMovie.Title) ? relatedMovie.IMDbId : relatedMovie.Title;
            GUIUtils.SetProperty("#Trakt.Related.Movie", title);

            // hide watched
            HideWatched = TraktSettings.HideWatchedRelatedMovies;            
            SendingWatchedToTrakt = false;
            if (hideWatchedButton != null)
            {
                GUIControl.SetControlLabel((int)TraktGUIWindows.RelatedMovies, hideWatchedButton.GetID, Translation.HideWatched);
                hideWatchedButton.Selected = HideWatched;
            }

            // no changes yet
            RelationChanged = false;

            // load last layout
            CurrentLayout = (Layout)TraktSettings.RelatedMoviesDefaultLayout;
            // update button label
            GUIControl.SetControlLabel(GetID, layoutButton.GetID, GUICommon.GetLayoutTranslation(CurrentLayout));
        }

        private void ClearProperties()
        {
            GUIUtils.SetProperty("#Trakt.Related.Movie", string.Empty);
            GUICommon.ClearMovieProperties();
        }

        private void PublishMovieSkinProperties(TraktMovie movie)
        {
            GUICommon.SetMovieProperties(movie);
        }

        private void OnMovieSelected(GUIListItem item, GUIControl parent)
        {
            TraktMovie movie = item.TVTag as TraktMovie;
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

                // sort images so that images that already exist are displayed first
                groupList.Sort((m1, m2) =>
                {
                    int x = Convert.ToInt32(File.Exists(m1.PosterImageFilename)) + Convert.ToInt32(File.Exists(m1.FanartImageFilename));
                    int y = Convert.ToInt32(File.Exists(m2.PosterImageFilename)) + Convert.ToInt32(File.Exists(m2.FanartImageFilename));
                    return y.CompareTo(x);
                });

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
    }

    public class GUITraktRelatedMovieListItem : GUIListItem
    {
        public GUITraktRelatedMovieListItem(string strLabel) : base(strLabel) { }

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
                        this.UpdateItemIfSelected((int)TraktGUIWindows.RelatedMovies, ItemId);
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
            TraktMovie movie = TVTag as TraktMovie;
            MainOverlayImage mainOverlay = MainOverlayImage.None;

            if (movie.InWatchList)
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
            this.UpdateItemIfSelected((int)TraktGUIWindows.RelatedMovies, ItemId);
        }
    }
}