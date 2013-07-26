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
    public class GUIListItems : GUIWindow
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
            MarkAsWatched,
            MarkAsUnWatched,
            AddToWatchList,
            RemoveFromWatchList,
            AddToList,
            RemoveFromList,
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

        public GUIListItems()
        {
            backdrop = new ImageSwapper();
            backdrop.PropertyOne = "#Trakt.List.Fanart.1";
            backdrop.PropertyTwo = "#Trakt.List.Fanart.2";
        }

        #endregion

        #region Private Variables

        bool StopDownload { get; set; }
        Layout CurrentLayout { get; set; }
        ImageSwapper backdrop;
        TraktItemType SelectedType { get; set; }
        string PreviousSlug { get; set; }
        int PreviousSelectedIndex = 0;

        #endregion

        #region Base Overrides

        public override int GetID
        {
            get
            {
                return (int)TraktGUIWindows.ListItems;
            }
        }

        public override bool Init()
        {
            return Load(GUIGraphicsContext.Skin + @"\Trakt.List.Items.xml");
        }

        protected override void OnPageLoad()
        {
            base.OnPageLoad();

            if (CurrentUser == null || CurrentList == null)
            {
                GUIWindowManager.ActivateWindow(GUIWindowManager.GetPreviousActiveWindow());
                return;
            }

            // Clear GUI Properties
            ClearProperties();

            // Init Properties
            InitProperties();

            // Load List Items
            LoadListItems();
        }

        protected override void OnPageDestroy(int new_windowId)
        {
            StopDownload = true;
            PreviousSelectedIndex = Facade.SelectedListItemIndex;
            ClearProperties();

            // save current layout
            TraktSettings.ListItemsDefaultLayout = (int)CurrentLayout;

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
                        if (SelectedType == TraktItemType.movie)
                            CheckAndPlayMovie(true);
                        else
                            CheckAndPlayEpisode(true);
                    }
                    break;

                // Layout Button
                case (2):
                    CurrentLayout = GUICommon.ShowLayoutMenu(CurrentLayout);
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
                    CurrentUser = null;
                    base.OnAction(action);
                    break;
                case Action.ActionType.ACTION_PLAY:
                case Action.ActionType.ACTION_MUSIC_PLAY:
                    if (SelectedType == TraktItemType.movie)
                        CheckAndPlayMovie(false);
                    else
                        CheckAndPlayEpisode(false);
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

            TraktUserListItem userListItem = (TraktUserListItem)selectedItem.TVTag;

            IDialogbox dlg = (IDialogbox)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
            if (dlg == null) return;

            dlg.Reset();
            dlg.SetHeading(GUIUtils.PluginName());

            GUIListItem listItem = null;

            if (SelectedType == TraktItemType.movie || SelectedType == TraktItemType.episode)
            {
                // Mark As Watched
                if (!userListItem.Watched)
                {
                    listItem = new GUIListItem(Translation.MarkAsWatched);
                    dlg.Add(listItem);
                    listItem.ItemId = (int)ContextMenuItem.MarkAsWatched;
                }

                // Mark As UnWatched
                if (userListItem.Watched)
                {
                    listItem = new GUIListItem(Translation.MarkAsUnWatched);
                    dlg.Add(listItem);
                    listItem.ItemId = (int)ContextMenuItem.MarkAsUnWatched;
                }
            }

            if (SelectedType != TraktItemType.season)
            {
                // Add/Remove Watch List
                if (!userListItem.InWatchList)
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
            }

            // Add to Custom list
            listItem = new GUIListItem(Translation.AddToList);
            dlg.Add(listItem);
            listItem.ItemId = (int)ContextMenuItem.AddToList;

            // Remove from Custom list (Only if current user is the active user)
            if (TraktSettings.Username == CurrentUser)
            {
                listItem = new GUIListItem(Translation.RemoveFromList);
                dlg.Add(listItem);
                listItem.ItemId = (int)ContextMenuItem.RemoveFromList;
            }

            if (SelectedType == TraktItemType.movie || SelectedType == TraktItemType.episode)
            {
                // Add to Library
                // Don't allow if it will be removed again on next sync
                // movie could be part of a DVD collection
                if (!userListItem.InCollection && !TraktSettings.KeepTraktLibraryClean)
                {
                    listItem = new GUIListItem(Translation.AddToLibrary);
                    dlg.Add(listItem);
                    listItem.ItemId = (int)ContextMenuItem.AddToLibrary;
                }

                if (userListItem.InCollection)
                {
                    listItem = new GUIListItem(Translation.RemoveFromLibrary);
                    dlg.Add(listItem);
                    listItem.ItemId = (int)ContextMenuItem.RemoveFromLibrary;
                }
            }

            // Related Movies/Shows
            listItem = new GUIListItem(SelectedType == TraktItemType.movie ? Translation.RelatedMovies : Translation.RelatedShows + "...");
            dlg.Add(listItem);
            listItem.ItemId = (int)ContextMenuItem.Related;

            if (SelectedType != TraktItemType.season)
            {
                // Rate
                listItem = new GUIListItem(Translation.Rate + "...");
                dlg.Add(listItem);
                listItem.ItemId = (int)ContextMenuItem.Rate;

                // Shouts
                listItem = new GUIListItem(Translation.Shouts + "...");
                dlg.Add(listItem);
                listItem.ItemId = (int)ContextMenuItem.Shouts;
            }
            
            // Trailers
            if (TraktHelper.IsOnlineVideosAvailableAndEnabled)
            {
                listItem = new GUIListItem(Translation.Trailers);
                dlg.Add(listItem);
                listItem.ItemId = (int)ContextMenuItem.Trailers;
            }

            // Search with mpNZB
            if (TraktHelper.IsMpNZBAvailableAndEnabled)
            {
                if ((userListItem.Movie != null && !userListItem.Movie.InCollection) || userListItem.Episode != null)
                {
                    listItem = new GUIListItem(Translation.SearchWithMpNZB);
                    dlg.Add(listItem);
                    listItem.ItemId = (int)ContextMenuItem.SearchWithMpNZB;
                }
            }

            // Search with MyTorrents
            if (TraktHelper.IsMyTorrentsAvailableAndEnabled)
            {
                if ((userListItem.Movie != null && !userListItem.Movie.InCollection) || userListItem.Episode != null)
                {
                    listItem = new GUIListItem(Translation.SearchTorrent);
                    dlg.Add(listItem);
                    listItem.ItemId = (int)ContextMenuItem.SearchTorrent;
                }
            }

            // Change Layout
            listItem = new GUIListItem(Translation.ChangeLayout);
            dlg.Add(listItem);
            listItem.ItemId = (int)ContextMenuItem.ChangeLayout;

            // Show Context Menu
            dlg.DoModal(GUIWindowManager.ActiveWindow);
            if (dlg.SelectedId < 0) return;

            switch (dlg.SelectedId)
            {
                case ((int)ContextMenuItem.MarkAsWatched):
                    MarkItemAsWatched(userListItem);
                    if (userListItem.Plays == 0) userListItem.Plays = 1;
                    userListItem.Watched = true;
                    selectedItem.IsPlayed = true;
                    OnItemSelected(selectedItem, Facade);
                    if (SelectedType == TraktItemType.movie)
                        userListItem.Movie.Images.NotifyPropertyChanged("PosterImageFilename");
                    else
                        userListItem.Show.Images.NotifyPropertyChanged("PosterImageFilename");
                    break;

                case ((int)ContextMenuItem.MarkAsUnWatched):
                    MarkItemAsUnWatched(userListItem);
                    userListItem.Watched = false;
                    selectedItem.IsPlayed = false;
                    OnItemSelected(selectedItem, Facade);
                    if (SelectedType == TraktItemType.movie)
                        userListItem.Movie.Images.NotifyPropertyChanged("PosterImageFilename");
                    else
                        userListItem.Show.Images.NotifyPropertyChanged("PosterImageFilename");
                    break;

                case ((int)ContextMenuItem.AddToWatchList):
                    AddItemToWatchList(userListItem);
                    userListItem.InWatchList = true;
                    OnItemSelected(selectedItem, Facade);
                    if (SelectedType == TraktItemType.movie)
                        userListItem.Movie.Images.NotifyPropertyChanged("PosterImageFilename");
                    else
                        userListItem.Show.Images.NotifyPropertyChanged("PosterImageFilename");
                    GUIWatchListMovies.ClearCache(TraktSettings.Username);
                    break;

                case ((int)ContextMenuItem.RemoveFromWatchList):
                    RemoveItemFromWatchList(userListItem);
                    userListItem.InWatchList = false;
                    OnItemSelected(selectedItem, Facade);
                    if (SelectedType == TraktItemType.movie)
                        userListItem.Movie.Images.NotifyPropertyChanged("PosterImageFilename");
                    else
                        userListItem.Show.Images.NotifyPropertyChanged("PosterImageFilename");
                    GUIWatchListMovies.ClearCache(TraktSettings.Username);
                    break;

                case ((int)ContextMenuItem.AddToList):
                    if (SelectedType == TraktItemType.movie)
                        TraktHelper.AddRemoveMovieInUserList(userListItem.Title, userListItem.Year, userListItem.ImdbId, false);
                    else if (SelectedType == TraktItemType.show)
                        TraktHelper.AddRemoveShowInUserList(userListItem.Title, userListItem.Year, userListItem.Show.Tvdb, false);
                    else if (SelectedType == TraktItemType.season)
                        TraktHelper.AddRemoveSeasonInUserList(userListItem.Title, userListItem.Year, userListItem.SeasonNumber, userListItem.Show.Tvdb, false);
                    else if (SelectedType == TraktItemType.episode)
                        TraktHelper.AddRemoveEpisodeInUserList(userListItem.Title, userListItem.Year, userListItem.SeasonNumber, userListItem.EpisodeNumber, userListItem.Show.Tvdb, false);
                    break;

                case ((int)ContextMenuItem.RemoveFromList):
                    if (!GUIUtils.ShowYesNoDialog(Translation.DeleteListItem, Translation.ConfirmDeleteListItem)) break;

                    // Only do remove from current list
                    // We could do same as Add (ie remove from multile lists) but typically you only remove from the current list
                    if (SelectedType == TraktItemType.movie)
                    {
                        TraktListItem item = new TraktListItem { Type = "movie", ImdbId = userListItem.ImdbId, Title = userListItem.Title, Year = Convert.ToInt32(userListItem.Year) };
                        TraktHelper.AddRemoveItemInList(CurrentList.Slug, item, true);
                    }
                    else if (SelectedType == TraktItemType.show)
                    {
                        TraktListItem item = new TraktListItem { Type = "show", TvdbId = userListItem.Show.Tvdb, Title = userListItem.Title, Year = Convert.ToInt32(userListItem.Year) };
                        TraktHelper.AddRemoveItemInList(CurrentList.Slug, item, true);
                    }
                    else if (SelectedType == TraktItemType.season)
                    {
                        TraktListItem item = new TraktListItem { Type = "season", TvdbId = userListItem.Show.Tvdb, Title = userListItem.Title, Year = Convert.ToInt32(userListItem.Year), Season = Convert.ToInt32(userListItem.SeasonNumber) };
                        TraktHelper.AddRemoveItemInList(CurrentList.Slug, item, true);
                    }
                    else if (SelectedType == TraktItemType.episode)
                    {
                        TraktListItem item = new TraktListItem { Type = "episode", TvdbId = userListItem.Show.Tvdb, Title = userListItem.Title, Year = Convert.ToInt32(userListItem.Year), Season = Convert.ToInt32(userListItem.SeasonNumber), Episode = Convert.ToInt32(userListItem.EpisodeNumber) };
                        TraktHelper.AddRemoveItemInList(CurrentList.Slug, item, true);
                    }

                    // Remove from view
                    if (Facade.Count > 1)
                    {
                        PreviousSelectedIndex = Facade.SelectedListItemIndex;
                        CurrentList.Items.Remove(userListItem);
                        SendListItemsToFacade(CurrentList);
                    }
                    else
                    {
                        CurrentList.Items.Remove(userListItem);

                        // no more items left
                        GUIControl.ClearControl(GetID, Facade.GetID);
                        ClearProperties();
                        GUIWindowManager.Process();

                        // nothing left, exit
                        GUIWindowManager.ShowPreviousWindow();
                        return;
                    }
                    break;

                case ((int)ContextMenuItem.AddToLibrary):
                    AddItemToLibrary(userListItem);
                    userListItem.InCollection = true;
                    OnItemSelected(selectedItem, Facade);
                    if (SelectedType == TraktItemType.movie)
                        userListItem.Movie.Images.NotifyPropertyChanged("PosterImageFilename");
                    else
                        userListItem.Show.Images.NotifyPropertyChanged("PosterImageFilename");
                    break;

                case ((int)ContextMenuItem.RemoveFromLibrary):
                    RemoveItemFromLibrary(userListItem);
                    userListItem.InCollection = false;
                    OnItemSelected(selectedItem, Facade);
                    if (SelectedType == TraktItemType.movie)
                        userListItem.Movie.Images.NotifyPropertyChanged("PosterImageFilename");
                    else
                        userListItem.Show.Images.NotifyPropertyChanged("PosterImageFilename");
                    break;

                case ((int)ContextMenuItem.Related):
                    if (SelectedType == TraktItemType.movie)
                    {
                        RelatedMovie relatedMovie = new RelatedMovie();
                        relatedMovie.IMDbId = userListItem.Movie.IMDBID;
                        relatedMovie.Title = userListItem.Movie.Title;
                        GUIRelatedMovies.relatedMovie = relatedMovie;
                        GUIWindowManager.ActivateWindow((int)TraktGUIWindows.RelatedMovies);
                    }
                    else
                    {
                        //series, season & episode
                        RelatedShow relatedShow = new RelatedShow();
                        relatedShow.Title = userListItem.Show.Title;
                        relatedShow.TVDbId = userListItem.Show.Tvdb;
                        GUIRelatedShows.relatedShow = relatedShow;
                        GUIWindowManager.ActivateWindow((int)TraktGUIWindows.RelatedShows);
                    }
                    break;

                case ((int)ContextMenuItem.Rate):
                    RateItem(userListItem);
                    OnItemSelected(selectedItem, Facade);
                    if (SelectedType == TraktItemType.movie)
                        userListItem.Movie.Images.NotifyPropertyChanged("PosterImageFilename");
                    else
                        userListItem.Show.Images.NotifyPropertyChanged("PosterImageFilename");
                    break;

                case ((int)ContextMenuItem.Shouts):
                    GUIShouts.ShoutType = (GUIShouts.ShoutTypeEnum)Enum.Parse(typeof(GUIShouts.ShoutTypeEnum), SelectedType.ToString(), true);
                    if (SelectedType == TraktItemType.movie)
                        GUIShouts.MovieInfo = new MovieShout { IMDbId = userListItem.ImdbId, TMDbId = userListItem.Movie.TMDBID, Title = userListItem.Title, Year = userListItem.Year };
                    else if (SelectedType == TraktItemType.show)
                        GUIShouts.ShowInfo = new ShowShout { IMDbId = userListItem.ImdbId, TVDbId = userListItem.Show.Tvdb, Title = userListItem.Title };
                    else
                        GUIShouts.EpisodeInfo = new EpisodeShout { IMDbId = userListItem.ImdbId, TVDbId = userListItem.Show.Tvdb, Title = userListItem.Title, SeasonIdx = userListItem.SeasonNumber, EpisodeIdx = userListItem.EpisodeNumber };
                    GUIShouts.Fanart = SelectedType == TraktItemType.movie ? (userListItem.Images as TraktMovie.MovieImages).FanartImageFilename : (userListItem.Images as TraktShow.ShowImages).FanartImageFilename;
                    GUIWindowManager.ActivateWindow((int)TraktGUIWindows.Shouts);
                    break;
                
                case ((int)ContextMenuItem.Trailers):
                    if (SelectedType == TraktItemType.movie)
                        GUICommon.ShowMovieTrailersMenu(userListItem.Movie);
                    else
                        GUICommon.ShowTVShowTrailersMenu(userListItem.Show, userListItem.Episode);
                    break;

                case ((int)ContextMenuItem.SearchWithMpNZB):
                    string loadingParam = String.Empty;
                    if (userListItem.Movie != null)
                    {
                        loadingParam = string.Format("search:{0}", userListItem.Movie.Title);
                    }
                    else if (userListItem.Episode != null)
                    {
                        loadingParam = string.Format("search:{0} S{1}E{2}", userListItem.Show.Title, userListItem.Episode.Season.ToString("D2"), userListItem.Episode.Number.ToString("D2"));
                    }
                    GUIWindowManager.ActivateWindow((int)ExternalPluginWindows.MpNZB, loadingParam);
                    break;

                case ((int)ContextMenuItem.SearchTorrent):
                    string loadPar = String.Empty;
                    if (userListItem.Movie != null)
                    {
                        loadPar = userListItem.Movie.Title;
                    }
                    else if (userListItem.Episode != null)
                    {
                        loadPar = string.Format("{0} S{1}E{2}", userListItem.Show.Title, userListItem.Episode.Season.ToString("D2"), userListItem.Episode.Number.ToString("D2"));
                    }
                    GUIWindowManager.ActivateWindow((int)ExternalPluginWindows.MyTorrents, loadPar);
                    break;

                case ((int)ContextMenuItem.ChangeLayout):
                    CurrentLayout = GUICommon.ShowLayoutMenu(CurrentLayout);
                    break;

                default:
                    break;
            }

            base.OnShowContextMenu();
        }

        #endregion

        #region Private Methods

        private void CheckAndPlayEpisode(bool jumpTo)
        {
            GUIListItem selectedItem = this.Facade.SelectedListItem;
            if (selectedItem == null) return;

            TraktUserListItem userListItem = selectedItem.TVTag as TraktUserListItem;
            if (userListItem == null) return;

            int seriesid = Convert.ToInt32(userListItem.Show.Tvdb);
            string searchterm = string.IsNullOrEmpty(userListItem.Show.Imdb) ? userListItem.Show.Title : userListItem.Show.Imdb;

            // if its a show/season, play first unwatched
            if (SelectedType != TraktItemType.episode)
            {
                GUICommon.CheckAndPlayFirstUnwatched(seriesid, searchterm, jumpTo, userListItem.Title);
            }
            else
            {
                int seasonidx = Convert.ToInt32(userListItem.SeasonNumber);
                int episodeidx = Convert.ToInt32(userListItem.EpisodeNumber);
                GUICommon.CheckAndPlayEpisode(seriesid, searchterm, seasonidx, episodeidx, userListItem.Title);
            }
        }

        private void CheckAndPlayMovie(bool jumpTo)
        {
            GUIListItem selectedItem = this.Facade.SelectedListItem;
            if (selectedItem == null) return;

            TraktUserListItem userListItem = selectedItem.TVTag as TraktUserListItem;
            if (userListItem == null || userListItem.Movie == null) return;

            GUICommon.CheckAndPlayMovie(jumpTo, userListItem.Movie);
        }

        private TraktShowSync CreateShowSyncData(TraktShow show)
        {
            TraktShowSync.Show showToSync = new TraktShowSync.Show
            {
                Title = show.Title,
                TVDBID = show.Tvdb,
                Year = show.Year
            };

            TraktShowSync syncData = new TraktShowSync
            {
                UserName = TraktSettings.Username,
                Password = TraktSettings.Password,
                Shows = new List<TraktShowSync.Show> { showToSync }
            };
            return syncData;
        }

        private TraktMovieSync CreateMovieSyncData(TraktMovie movie)
        {
            if (movie == null) return null;

            TraktMovieSync.Movie syncMovie = new TraktMovieSync.Movie
            {
                IMDBID = movie.IMDBID,
                Title = movie.Title,
                Year = movie.Year
            };            

            TraktMovieSync syncData = new TraktMovieSync
            {
                UserName = TraktSettings.Username,
                Password = TraktSettings.Password,
                MovieList = new List<TraktMovieSync.Movie> { syncMovie }
            };

            return syncData;
        }

        private TraktEpisodeSync CreateEpisodeSyncData(TraktUserListItem item)
        {
            if (item == null) return null;

            TraktEpisodeSync.Episode syncEpisode = new TraktEpisodeSync.Episode
            {
                EpisodeIndex = item.EpisodeNumber.ToString(),
                SeasonIndex = item.SeasonNumber.ToString()
            };

            TraktEpisodeSync syncData = new TraktEpisodeSync
            {
                UserName = TraktSettings.Username,
                Password = TraktSettings.Password,
                SeriesID = item.Show.Tvdb,
                Title = item.Show.Title,
                Year = item.Year,
                EpisodeList = new List<TraktEpisodeSync.Episode> { syncEpisode }
            };

            return syncData;
        }

        private void AddItemToWatchList(TraktUserListItem item)
        {
            Thread syncThread = new Thread(delegate(object obj)
            {
                if (SelectedType == TraktItemType.movie)
                    TraktAPI.TraktAPI.SyncMovieLibrary(CreateMovieSyncData((obj as TraktUserListItem).Movie), TraktSyncModes.watchlist);
                else if (SelectedType == TraktItemType.show)
                    TraktAPI.TraktAPI.SyncShowWatchList(CreateShowSyncData((obj as TraktUserListItem).Show), TraktSyncModes.watchlist);
                else
                    TraktAPI.TraktAPI.SyncEpisodeLibrary(CreateEpisodeSyncData(obj as TraktUserListItem), TraktSyncModes.watchlist);
            })
            {
                IsBackground = true,
                Name = "AddWatchList"
            };

            syncThread.Start(item);
        }

        private void RemoveItemFromWatchList(TraktUserListItem item)
        {
            Thread syncThread = new Thread(delegate(object obj)
            {
                if (SelectedType == TraktItemType.movie)
                    TraktAPI.TraktAPI.SyncMovieLibrary(CreateMovieSyncData((obj as TraktUserListItem).Movie), TraktSyncModes.unwatchlist);
                else if (SelectedType == TraktItemType.show)
                    TraktAPI.TraktAPI.SyncShowWatchList(CreateShowSyncData((obj as TraktUserListItem).Show), TraktSyncModes.unwatchlist);
                else
                    TraktAPI.TraktAPI.SyncEpisodeLibrary(CreateEpisodeSyncData(obj as TraktUserListItem), TraktSyncModes.unwatchlist);
            })
            {
                IsBackground = true,
                Name = "RemoveWatchList"
            };

            syncThread.Start(item);
        }

        private void MarkItemAsWatched(TraktUserListItem item)
        {
            Thread syncThread = new Thread(delegate(object obj)
            {
                if (SelectedType == TraktItemType.movie)
                    TraktAPI.TraktAPI.SyncMovieLibrary(CreateMovieSyncData((obj as TraktUserListItem).Movie), TraktSyncModes.seen);
                else
                    TraktAPI.TraktAPI.SyncEpisodeLibrary(CreateEpisodeSyncData(obj as TraktUserListItem), TraktSyncModes.seen);
            })
            {
                IsBackground = true,
                Name = "MarkWatched"
            };

            syncThread.Start(item);
        }

        private void MarkItemAsUnWatched(TraktUserListItem item)
        {
            Thread syncThread = new Thread(delegate(object obj)
            {
                if (SelectedType == TraktItemType.movie)
                    TraktAPI.TraktAPI.SyncMovieLibrary(CreateMovieSyncData((obj as TraktUserListItem).Movie), TraktSyncModes.unseen);
                else
                    TraktAPI.TraktAPI.SyncEpisodeLibrary(CreateEpisodeSyncData(obj as TraktUserListItem), TraktSyncModes.unseen);
            })
            {
                IsBackground = true,
                Name = "MarkUnWatched"
            };

            syncThread.Start(item);
        }

        private void AddItemToLibrary(TraktUserListItem item)
        {
            Thread syncThread = new Thread(delegate(object obj)
            {
                if (SelectedType == TraktItemType.movie)
                    TraktAPI.TraktAPI.SyncMovieLibrary(CreateMovieSyncData((obj as TraktUserListItem).Movie), TraktSyncModes.library);
                else
                    TraktAPI.TraktAPI.SyncEpisodeLibrary(CreateEpisodeSyncData(obj as TraktUserListItem), TraktSyncModes.library);
            })
            {
                IsBackground = true,
                Name = "AddLibrary"
            };

            syncThread.Start(item);
        }

        private void RemoveItemFromLibrary(TraktUserListItem item)
        {
            Thread syncThread = new Thread(delegate(object obj)
            {
                if (SelectedType == TraktItemType.movie)
                    TraktAPI.TraktAPI.SyncMovieLibrary(CreateMovieSyncData((obj as TraktUserListItem).Movie), TraktSyncModes.unlibrary);
                else
                    TraktAPI.TraktAPI.SyncEpisodeLibrary(CreateEpisodeSyncData(obj as TraktUserListItem), TraktSyncModes.unlibrary);
            })
            {
                IsBackground = true,
                Name = "RemoveLibrary"
            };

            syncThread.Start(item);
        }

        private void RateItem(TraktUserListItem item)
        {
            if (SelectedType == TraktItemType.movie)
            {
                GUICommon.RateMovie(item.Movie);
            }
            else if (SelectedType == TraktItemType.show)
            {
                GUICommon.RateShow(item.Show);
            }
            else if (SelectedType == TraktItemType.episode)
            {
                GUICommon.RateEpisode(item.Show, item.Episode);
            }
        }

        private void LoadListItems()
        {
            GUIUtils.SetProperty("#Trakt.Items", string.Empty);

            GUIBackgroundTask.Instance.ExecuteInBackgroundAndCallback(() =>
            {
                return TraktLists.GetListForUser(CurrentUser, CurrentList.Slug);
            },
            delegate(bool success, object result)
            {
                if (success)
                {
                    CurrentList = result as TraktUserList;
                    SendListItemsToFacade(CurrentList);
                }
            }, Translation.GettingListItems, true);
        }

        private void SendListItemsToFacade(TraktUserList list)
        {
            // clear facade
            GUIControl.ClearControl(GetID, Facade.GetID);

            if (list.Items == null || list.Items.Count() == 0)
            {
                GUIUtils.ShowNotifyDialog(GUIUtils.PluginName(), Translation.NoListItemsFound);
                GUIWindowManager.ShowPreviousWindow();
                return;
            }

            int itemId = 1;
            List<object> images = new List<object>();
            
            // Add each list item
            foreach (var listItem in list.Items)
            {
                string itemName = list.ShowNumbers ? string.Format("{0}. {1}", itemId, listItem.ToString()) : listItem.ToString();

                GUITraktCustomListItem item = new GUITraktCustomListItem(itemName);

                item.Label2 = listItem.Year;
                item.TVTag = listItem;
                item.Item = listItem.Images;
                item.IsPlayed = listItem.Watched;
                item.ItemId = Int32.MaxValue - itemId;
                item.IconImage = GUIImageHandler.GetDefaultPoster(false);
                item.IconImageBig = GUIImageHandler.GetDefaultPoster();
                item.ThumbnailImage = GUIImageHandler.GetDefaultPoster();
                item.OnItemSelected += OnItemSelected;
                Utils.SetDefaultIcons(item);
                Facade.Add(item);
                itemId++;

                // add image for download
                images.Add(listItem.Images);
            }

            // Set Facade Layout
            Facade.SetCurrentLayout(Enum.GetName(typeof(Layout), CurrentLayout));
            GUIControl.FocusControl(GetID, Facade.GetID);

            Facade.SelectIndex(PreviousSelectedIndex);

            // set facade properties
            GUIUtils.SetProperty("#itemcount", list.Items.Count().ToString());
            GUIUtils.SetProperty("#Trakt.Items", string.Format("{0} {1}", list.Items.Count().ToString(), list.Items.Count() > 1 ? Translation.Items : Translation.Item));

            // Download images Async and set to facade
            GetImages(images);
        }

        private void InitProperties()
        {
            GUIUtils.SetProperty("#Trakt.List.Username", CurrentUser);
            GUIUtils.SetProperty("#Trakt.List.Slug", CurrentList.Slug);
            GUIUtils.SetProperty("#Trakt.List.Name", CurrentList.Name);
            GUIUtils.SetProperty("#Trakt.List.Description", CurrentList.Description);
            GUIUtils.SetProperty("#Trakt.List.Privacy", CurrentList.Privacy);
            GUIUtils.SetProperty("#Trakt.List.Url", CurrentList.Url);

            if (PreviousSlug != CurrentList.Slug)
                PreviousSelectedIndex = 0;
            PreviousSlug = CurrentList.Slug;

            // Fanart
            backdrop.GUIImageOne = FanartBackground;
            backdrop.GUIImageTwo = FanartBackground2;
            backdrop.LoadingImage = loadingImage;

            // load last layout
            CurrentLayout = (Layout)TraktSettings.ListItemsDefaultLayout;
            // update button label
            GUIControl.SetControlLabel(GetID, layoutButton.GetID, GUICommon.GetLayoutTranslation(CurrentLayout));
        }

        private void ClearProperties()
        {
            GUICommon.ClearMovieProperties();
            GUICommon.ClearShowProperties();
            GUICommon.ClearSeasonProperties();
            GUICommon.ClearEpisodeProperties();
        }

        private void PublishEpisodeSkinProperties(TraktUserListItem item)
        {
            if (item == null || item.Episode == null) return;
            GUICommon.SetEpisodeProperties(item.Episode);

            // workaround API not having episode number set
            // can be removed later when fixed
            GUICommon.SetProperty("#Trakt.Episode.Number", item.EpisodeNumber);

            PublishSeasonSkinProperties(item);
        }

        private void PublishSeasonSkinProperties(TraktUserListItem item)
        {
            if (item == null) return;
            GUICommon.SetProperty("#Trakt.Season.Number", item.SeasonNumber);
            PublishShowSkinProperties(item);
        }

        private void PublishShowSkinProperties(TraktUserListItem item)
        {
            if (item == null || item.Show == null) return;
            GUICommon.SetShowProperties(item.Show);
        }

        private void PublishMovieSkinProperties(TraktUserListItem item)
        {
            if (item == null || item.Movie == null) return;
            GUICommon.SetMovieProperties(item.Movie);
        }

        private void OnItemSelected(GUIListItem item, GUIControl parent)
        {
            if (item == null) return;

            TraktUserListItem listItem = item.TVTag as TraktUserListItem;
            if (listItem == null) return;

            switch (listItem.Type)
            {
                case "movie":
                    SelectedType = TraktItemType.movie;
                    PublishMovieSkinProperties(listItem);
                    GUIImageHandler.LoadFanart(backdrop, listItem.Movie.Images.FanartImageFilename);
                    break;
                case "show":
                    SelectedType = TraktItemType.show;
                    PublishShowSkinProperties(listItem);
                    GUIImageHandler.LoadFanart(backdrop, listItem.Show.Images.FanartImageFilename);
                    break;
                case "season":
                    SelectedType = TraktItemType.season;
                    PublishSeasonSkinProperties(listItem);
                    GUIImageHandler.LoadFanart(backdrop, listItem.Show.Images.FanartImageFilename);
                    break;
                case "episode":
                    SelectedType = TraktItemType.episode;
                    PublishEpisodeSkinProperties(listItem);
                    GUIImageHandler.LoadFanart(backdrop, listItem.Show.Images.FanartImageFilename);
                    break;
            }
            GUIUtils.SetProperty("#Trakt.List.ItemType", SelectedType.ToString());
        }

        private void GetImages(List<object> itemsWithThumbs)
        {
            StopDownload = false;

            // split the downloads in 5+ groups and do multithreaded downloading
            int groupSize = (int)Math.Max(1, Math.Floor((double)itemsWithThumbs.Count / 5));
            int groups = (int)Math.Ceiling((double)itemsWithThumbs.Count() / groupSize);

            for (int i = 0; i < groups; i++)
            {
                List<object> groupList = new List<object>();
                for (int j = groupSize * i; j < groupSize * i + (groupSize * (i + 1) > itemsWithThumbs.Count ? itemsWithThumbs.Count - groupSize * i : groupSize); j++)
                {
                    groupList.Add(itemsWithThumbs[j]);
                }

                new Thread(delegate(object o)
                {
                    List<object> items = (List<object>)o;
                    foreach (object item in items)
                    {
                        #region Poster
                        // stop download if we have exited window
                        if (StopDownload) break;

                        string remoteThumb = string.Empty;
                        string localThumb = string.Empty;
                        bool seasonPoster = false;

                        if (item is TraktMovie.MovieImages)
                        {
                            remoteThumb = ((TraktMovie.MovieImages)item).Poster;
                            localThumb = ((TraktMovie.MovieImages)item).PosterImageFilename;
                        }
                        else
                        {
                            // check if season poster should be downloaded instead of series poster
                            if (string.IsNullOrEmpty(((TraktShow.ShowImages)item).SeasonImageFilename))
                            {
                                seasonPoster = false;
                                remoteThumb = ((TraktShow.ShowImages)item).Poster;
                                localThumb = ((TraktShow.ShowImages)item).PosterImageFilename;
                            }
                            else
                            {
                                seasonPoster = true;
                                remoteThumb = ((TraktShow.ShowImages)item).Season;
                                localThumb = ((TraktShow.ShowImages)item).SeasonImageFilename;
                            }
                        }

                        if (!string.IsNullOrEmpty(remoteThumb) && !string.IsNullOrEmpty(localThumb))
                        {
                            if (GUIImageHandler.DownloadImage(remoteThumb, localThumb))
                            {
                                // notify that image has been downloaded
                                if (item is TraktMovie.MovieImages)
                                    ((TraktMovie.MovieImages)item).NotifyPropertyChanged("PosterImageFilename");
                                else
                                    ((TraktShow.ShowImages)item).NotifyPropertyChanged(seasonPoster ? "SeasonImageFilename" : "PosterImageFilename");
                            }
                        }
                        #endregion

                        #region Fanart
                        // stop download if we have exited window
                        if (StopDownload) break;
                        if (!TraktSettings.DownloadFanart) continue;

                        string remoteFanart = string.Empty;
                        string localFanart = string.Empty;

                        remoteFanart = item is TraktMovie.MovieImages ? ((TraktMovie.MovieImages)item).Fanart : ((TraktShow.ShowImages)item).Fanart;
                        localFanart = item is TraktMovie.MovieImages ? ((TraktMovie.MovieImages)item).FanartImageFilename : ((TraktShow.ShowImages)item).FanartImageFilename;

                        if (!string.IsNullOrEmpty(remoteFanart) && !string.IsNullOrEmpty(localFanart))
                        {
                            if (GUIImageHandler.DownloadImage(remoteFanart, localFanart))
                            {
                                // notify that image has been downloaded
                                if (item is TraktMovie.MovieImages)
                                    ((TraktMovie.MovieImages)item).NotifyPropertyChanged("FanartImageFilename");
                                else
                                    ((TraktShow.ShowImages)item).NotifyPropertyChanged("FanartImageFilename");
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

        #region Public Properties

        public static TraktUserList CurrentList { get; set; }
        public static string CurrentUser { get; set; }

        #endregion
    }

    public class GUITraktCustomListItem : GUIListItem
    {
        public GUITraktCustomListItem(string strLabel) : base(strLabel) { }

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
                        this.UpdateItemIfSelected((int)TraktGUIWindows.ListItems, ItemId);
                    if (s is TraktShow.ShowImages && e.PropertyName == "PosterImageFilename")
                        SetImageToGui((s as TraktShow.ShowImages).PosterImageFilename);
                    // re-size season posters to same as series/movie posters
                    if (s is TraktShow.ShowImages && e.PropertyName == "SeasonImageFilename")
                        SetImageToGui((s as TraktShow.ShowImages).SeasonImageFilename, new Size(300, 434));
                    if (s is TraktShow.ShowImages && e.PropertyName == "FanartImageFilename")
                        this.UpdateItemIfSelected((int)TraktGUIWindows.ListItems, ItemId);
                };
            }
        } protected object _Item;

        /// <summary>
        /// Loads an Image from memory into a facade item
        /// </summary>
        /// <param name="imageFilePath">Filename of image</param>
        protected void SetImageToGui(string imageFilePath)
        {
            SetImageToGui(imageFilePath, new Size());
        }

        protected void SetImageToGui(string imageFilePath, Size size)
        {
            if (string.IsNullOrEmpty(imageFilePath)) return;

            // determine the overlay to add to poster
            TraktUserListItem listItem = TVTag as TraktUserListItem;
            MainOverlayImage mainOverlay = MainOverlayImage.None;

            if (listItem.InWatchList)
                mainOverlay = MainOverlayImage.Watchlist;
            else if (listItem.Watched)
                mainOverlay = MainOverlayImage.Seenit;

            // add additional overlay if applicable
            if (listItem.InCollection)
                mainOverlay |= MainOverlayImage.Library;

            RatingOverlayImage ratingOverlay = GUIImageHandler.GetRatingOverlay(listItem.RatingAdvanced);

            // get a reference to a MediaPortal Texture Identifier
            string suffix = mainOverlay.ToString().Replace(", ", string.Empty) + Enum.GetName(typeof(RatingOverlayImage), ratingOverlay);
            string texture = GUIImageHandler.GetTextureIdentFromFile(imageFilePath, suffix);

            // build memory image
            Image memoryImage = null;
            if (mainOverlay != MainOverlayImage.None || ratingOverlay != RatingOverlayImage.None)
            {
                memoryImage = GUIImageHandler.DrawOverlayOnPoster(imageFilePath, mainOverlay, ratingOverlay, size);
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
            this.UpdateItemIfSelected((int)TraktGUIWindows.ListItems, ItemId);
        }
    }
}