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

        enum Layout
        {
            List = 0,
            SmallIcons = 1,
            LargeIcons = 2,
            Filmstrip = 3,
        }

        enum TrailerSite
        {
            IMDb,
            iTunes,
            YouTube
        }

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
            Trailers
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
                            CheckAndPlayEpisode();
                    }
                    break;

                // Layout Button
                case (2):
                    ShowLayoutMenu();
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
                        CheckAndPlayEpisode();
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
            listItem = new GUIListItem(Translation.AddToList + "...");
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

            #if MP12
            // Trailers
            if (TraktHelper.IsOnlineVideosAvailableAndEnabled)
            {
                listItem = new GUIListItem(Translation.Trailers);
                dlg.Add(listItem);
                listItem.ItemId = (int)ContextMenuItem.Trailers;
            }
            #endif

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
                        relatedMovie.IMDbId = userListItem.Movie.Imdb;
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
                        GUIShouts.MovieInfo = new MovieShout { IMDbId = userListItem.ImdbId, TMDbId = userListItem.Movie.Tmdb, Title = userListItem.Title, Year = userListItem.Year };
                    else if (SelectedType == TraktItemType.show)
                        GUIShouts.ShowInfo = new ShowShout { IMDbId = userListItem.ImdbId, TVDbId = userListItem.Show.Tvdb, Title = userListItem.Title };
                    else
                        GUIShouts.EpisodeInfo = new EpisodeShout { IMDbId = userListItem.ImdbId, TVDbId = userListItem.Show.Tvdb, Title = userListItem.Title, SeasonIdx = userListItem.SeasonNumber, EpisodeIdx = userListItem.EpisodeNumber };
                    GUIShouts.Fanart = SelectedType == TraktItemType.movie ? (userListItem.Images as TraktMovie.MovieImages).FanartImageFilename : (userListItem.Images as TraktShow.ShowImages).FanartImageFilename;
                    GUIWindowManager.ActivateWindow((int)TraktGUIWindows.Shouts);
                    break;

                #if MP12
                case ((int)ContextMenuItem.Trailers):
                    ShowTrailersMenu(userListItem);
                    break;
                #endif

                case ((int)ContextMenuItem.ChangeLayout):
                    ShowLayoutMenu();
                    break;

                default:
                    break;
            }

            base.OnShowContextMenu();
        }

        #endregion

        #region Private Methods

        private void CheckAndPlayEpisode()
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
                GUICommon.CheckAndPlayFirstUnwatched(seriesid, searchterm);
            }
            else
            {
                int seasonidx = Convert.ToInt32(userListItem.SeasonNumber);
                int episodeidx = Convert.ToInt32(userListItem.EpisodeNumber);
                GUICommon.CheckAndPlayEpisode(seriesid, searchterm, seasonidx, episodeidx);
            }
        }

        private void CheckAndPlayMovie(bool jumpTo)
        {
            GUIListItem selectedItem = this.Facade.SelectedListItem;
            if (selectedItem == null) return;

            TraktUserListItem userListItem = selectedItem.TVTag as TraktUserListItem;
            if (userListItem == null || userListItem.Movie == null) return;

            string title = userListItem.Movie.Title;
            string imdbid = userListItem.Movie.Imdb;
            int year = Convert.ToInt32(userListItem.Year);

            GUICommon.CheckAndPlayMovie(jumpTo, title, year, imdbid);
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
                IMDBID = movie.Imdb,
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
                Name = "Adding Item to Watch List"
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
                Name = "Removing Item from Watch List"
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
                Name = "Mark Item as Watched"
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
                Name = "Mark Item as UnWatched"
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
                Name = "Add Item to Library"
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
                Name = "Remove Item From Library"
            };

            syncThread.Start(item);
        }

        private void RateItem(TraktUserListItem item)
        {
            string prevRating = string.IsNullOrEmpty(item.Rating) ? "false" : item.Rating;

            if (SelectedType == TraktItemType.movie)
            {
                // default rating to love if not already set
                TraktRateMovie rateObject = new TraktRateMovie
                {
                    IMDBID = item.Movie.Imdb,
                    Title = item.Movie.Title,
                    Year = item.Movie.Year,
                    Rating = prevRating,
                    UserName = TraktSettings.Username,
                    Password = TraktSettings.Password
                };
                item.Rating = GUIUtils.ShowRateDialog<TraktRateMovie>(rateObject);
            }
            else if (SelectedType == TraktItemType.show)
            {
                TraktRateSeries rateObject = new TraktRateSeries
                {
                    SeriesID = item.Show.Tvdb,
                    Title = item.Show.Title,
                    Year = item.Show.Year.ToString(),
                    Rating = prevRating,
                    UserName = TraktSettings.Username,
                    Password = TraktSettings.Password
                };
                item.Rating = GUIUtils.ShowRateDialog<TraktRateSeries>(rateObject);
            }
            else if (SelectedType == TraktItemType.episode)
            {
                TraktRateEpisode rateObject = new TraktRateEpisode
                {
                    SeriesID = item.Show.Tvdb,
                    Title = item.Show.Title,
                    Year = item.Show.Year.ToString(),
                    Rating = prevRating,
                    Episode = item.EpisodeNumber.ToString(),
                    Season = item.SeasonNumber.ToString(),
                    UserName = TraktSettings.Username,
                    Password = TraktSettings.Password
                };
                item.Rating = GUIUtils.ShowRateDialog<TraktRateEpisode>(rateObject);
            }

            // if previous rating not equal to current rating then 
            // update skin properties to reflect changes so we dont
            // need to re-request from server
            if (prevRating != item.Rating)
            {
                if (prevRating == "false")
                {
                    item.Ratings.Votes++;
                    if (item.Rating == "love")
                        item.Ratings.LovedCount++;
                    else
                        item.Ratings.HatedCount++;
                }

                if (prevRating == "love")
                {
                    item.Ratings.LovedCount--;
                    item.Ratings.HatedCount++;
                }

                if (prevRating == "hate")
                {
                    item.Ratings.LovedCount++;
                    item.Ratings.HatedCount--;
                }

                item.Ratings.Percentage = (int)Math.Round(100 * (item.Ratings.LovedCount / (float)item.Ratings.Votes));
            }
        }

        #if MP12
        private void ShowTrailersMenu(TraktUserListItem item)
        {
            IDialogbox dlg = (IDialogbox)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
            dlg.Reset();
            dlg.SetHeading(Translation.Trailer);

            foreach (TrailerSite site in Enum.GetValues(typeof(TrailerSite)))
            {
                string menuItem = Enum.GetName(typeof(TrailerSite), site);
                if (SelectedType != TraktItemType.movie && menuItem == "iTunes") continue;
                GUIListItem pItem = new GUIListItem(menuItem);
                dlg.Add(pItem);                
            }

            dlg.DoModal(GUIWindowManager.ActiveWindow);

            if (dlg.SelectedLabel >= 0)
            {
                string siteUtil = string.Empty;
                string searchParam = string.Empty;

                switch (dlg.SelectedLabelText)
                {
                    case ("IMDb"):
                        siteUtil = "IMDb Trailers";
                        if (!string.IsNullOrEmpty(item.ImdbId))
                            // Exact search
                            searchParam = item.ImdbId;
                        else
                            searchParam = item.Title;
                        break;

                    case ("iTunes"):
                        siteUtil = "iTunes Movie Trailers";
                        searchParam = item.Movie.Title;
                        break;

                    case ("YouTube"):
                        siteUtil = "YouTube";
                        searchParam = item.Title;
                        break;
                }

                string loadingParam = string.Format("site:{0}|search:{1}|return:Locked", siteUtil, searchParam);
                // Launch OnlineVideos Trailer search
                GUIWindowManager.ActivateWindow((int)ExternalPluginWindows.OnlineVideos, loadingParam);
            }
        }
        #endif

        private void ShowLayoutMenu()
        {
            IDialogbox dlg = (IDialogbox)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
            dlg.Reset();
            dlg.SetHeading(GetLayoutTranslation(CurrentLayout));

            foreach (Layout layout in Enum.GetValues(typeof(Layout)))
            {
                string menuItem = GetLayoutTranslation(layout);
                GUIListItem pItem = new GUIListItem(menuItem);
                if (layout == CurrentLayout) pItem.Selected = true;
                dlg.Add(pItem);
            }

            dlg.DoModal(GUIWindowManager.ActiveWindow);

            if (dlg.SelectedLabel >= 0)
            {
                CurrentLayout = (Layout)dlg.SelectedLabel;
                Facade.SetCurrentLayout(Enum.GetName(typeof(Layout), CurrentLayout));
                GUIControl.SetControlLabel(GetID, layoutButton.GetID, GetLayoutTranslation(CurrentLayout));
            }
        }

        private string GetLayoutTranslation(Layout layout)
        {
            string strLine = string.Empty;
            switch (layout)
            {
                case Layout.List:
                    strLine = GUILocalizeStrings.Get(101);
                    break;
                case Layout.SmallIcons:
                    strLine = GUILocalizeStrings.Get(100);
                    break;
                case Layout.LargeIcons:
                    strLine = GUILocalizeStrings.Get(417);
                    break;
                case Layout.Filmstrip:
                    strLine = GUILocalizeStrings.Get(733);
                    break;
            }
            return strLine;
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

            int itemId = 0;
            List<object> images = new List<object>();
            
            // Add each list item
            foreach (var listItem in list.Items)
            {
                GUITraktCustomListItem item = new GUITraktCustomListItem(listItem.ToString());

                item.Label2 = listItem.Year;
                item.TVTag = listItem;
                item.Item = listItem.Images;
                item.IsPlayed = listItem.Watched;
                item.ItemId = Int32.MaxValue - itemId;
                item.IconImage = "defaultVideo.png";
                item.IconImageBig = "defaultVideoBig.png";
                item.ThumbnailImage = "defaultVideoBig.png";
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

        private void SetProperty(string property, string value)
        {
            string propertyValue = string.IsNullOrEmpty(value) ? "N/A" : value;
            GUIUtils.SetProperty(property, propertyValue);
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
            GUIControl.SetControlLabel(GetID, layoutButton.GetID, GetLayoutTranslation(CurrentLayout));
        }

        private void ClearProperties()
        {
            #region Movie
            GUIUtils.SetProperty("#Trakt.Movie.Imdb", string.Empty);
            GUIUtils.SetProperty("#Trakt.Movie.Certification", string.Empty);
            GUIUtils.SetProperty("#Trakt.Movie.Overview", string.Empty);
            GUIUtils.SetProperty("#Trakt.Movie.Released", string.Empty);
            GUIUtils.SetProperty("#Trakt.Movie.Runtime", string.Empty);
            GUIUtils.SetProperty("#Trakt.Movie.Tagline", string.Empty);
            GUIUtils.SetProperty("#Trakt.Movie.Title", string.Empty);
            GUIUtils.SetProperty("#Trakt.Movie.Tmdb", string.Empty);
            GUIUtils.SetProperty("#Trakt.Movie.Trailer", string.Empty);
            GUIUtils.SetProperty("#Trakt.Movie.Url", string.Empty);
            GUIUtils.SetProperty("#Trakt.Movie.Year", string.Empty);
            GUIUtils.SetProperty("#Trakt.Movie.PosterImageFilename", string.Empty);
            GUIUtils.SetProperty("#Trakt.Movie.FanartImageFilename", string.Empty);
            GUIUtils.SetProperty("#Trakt.Movie.InCollection", string.Empty);
            GUIUtils.SetProperty("#Trakt.Movie.InWatchList", string.Empty);
            GUIUtils.SetProperty("#Trakt.Movie.Plays", string.Empty);
            GUIUtils.SetProperty("#Trakt.Movie.Watched", string.Empty);
            GUIUtils.SetProperty("#Trakt.Movie.Rating", string.Empty);
            GUIUtils.SetProperty("#Trakt.Movie.Ratings.Icon", string.Empty);
            GUIUtils.SetProperty("#Trakt.Movie.Ratings.HatedCount", string.Empty);
            GUIUtils.SetProperty("#Trakt.Movie.Ratings.LovedCount", string.Empty);
            GUIUtils.SetProperty("#Trakt.Movie.Ratings.Percentage", string.Empty);
            GUIUtils.SetProperty("#Trakt.Movie.Ratings.Votes", string.Empty);
            #endregion

            #region Show
            GUIUtils.SetProperty("#Trakt.Show.AirDay", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.AirTime", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.Country", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.Network", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.TvRage", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.Imdb", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.Certification", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.Overview", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.FirstAired", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.Runtime", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.Title", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.Tvdb", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.Trailer", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.Url", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.Year", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.PosterImageFilename", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.FanartImageFilename", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.InWatchList", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.Rating", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.Ratings.Icon", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.Ratings.HatedCount", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.Ratings.LovedCount", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.Ratings.Percentage", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.Ratings.Votes", string.Empty);
            #endregion

            #region Season
            GUIUtils.SetProperty("#Trakt.Season.Number", string.Empty);
            #endregion

            #region Episode
            GUIUtils.SetProperty("#Trakt.Episode.Number", string.Empty);
            GUIUtils.SetProperty("#Trakt.Episode.Season", string.Empty);
            GUIUtils.SetProperty("#Trakt.Episode.FirstAired", string.Empty);
            GUIUtils.SetProperty("#Trakt.Episode.Title", string.Empty);
            GUIUtils.SetProperty("#Trakt.Episode.Url", string.Empty);
            GUIUtils.SetProperty("#Trakt.Episode.Overview", string.Empty);
            GUIUtils.SetProperty("#Trakt.Episode.Runtime", string.Empty);
            GUIUtils.SetProperty("#Trakt.Episode.InWatchList", string.Empty);
            GUIUtils.SetProperty("#Trakt.Episode.InCollection", string.Empty);
            GUIUtils.SetProperty("#Trakt.Episode.Plays", string.Empty);
            GUIUtils.SetProperty("#Trakt.Episode.Watched", string.Empty);
            GUIUtils.SetProperty("#Trakt.Episode.Rating", string.Empty);
            GUIUtils.SetProperty("#Trakt.Episode.Ratings.Icon", string.Empty);
            GUIUtils.SetProperty("#Trakt.Episode.Ratings.HatedCount", string.Empty);
            GUIUtils.SetProperty("#Trakt.Episode.Ratings.LovedCount", string.Empty);
            GUIUtils.SetProperty("#Trakt.Episode.Ratings.Percentage", string.Empty);
            GUIUtils.SetProperty("#Trakt.Episode.Ratings.Votes", string.Empty);
            GUIUtils.SetProperty("#Trakt.Episode.EpisodeImageFilename", string.Empty);
            #endregion
        }

        private void PublishEpisodeSkinProperties(TraktUserListItem item)
        {
            if (item == null) return;

            TraktEpisode episode = item.Episode;
            if (episode == null) return;

            SetProperty("#Trakt.Episode.Number", item.EpisodeNumber.ToString());
            SetProperty("#Trakt.Episode.Season", episode.Season.ToString());
            SetProperty("#Trakt.Episode.FirstAired", episode.FirstAired.FromEpoch().ToShortDateString());
            SetProperty("#Trakt.Episode.Title", string.IsNullOrEmpty(episode.Title) ? string.Format("{0} {1}", Translation.Episode, episode.Number.ToString()) : episode.Title);
            SetProperty("#Trakt.Episode.Url", episode.Url);
            SetProperty("#Trakt.Episode.Overview", string.IsNullOrEmpty(episode.Overview) ? Translation.NoEpisodeSummary : episode.Overview);
            SetProperty("#Trakt.Episode.Runtime", episode.Runtime.ToString());
            SetProperty("#Trakt.Episode.InWatchList", item.InWatchList.ToString());
            SetProperty("#Trakt.Episode.InCollection", item.InCollection.ToString());
            SetProperty("#Trakt.Episode.Plays", item.Plays.ToString());
            SetProperty("#Trakt.Episode.Watched", item.Watched.ToString());
            SetProperty("#Trakt.Episode.Rating", item.Rating);
            SetProperty("#Trakt.Episode.Ratings.Icon", (item.Ratings.LovedCount > item.Ratings.HatedCount) ? "love" : "hate");
            SetProperty("#Trakt.Episode.Ratings.HatedCount", item.Ratings.HatedCount.ToString());
            SetProperty("#Trakt.Episode.Ratings.LovedCount", item.Ratings.LovedCount.ToString());
            SetProperty("#Trakt.Episode.Ratings.Percentage", item.Ratings.Percentage.ToString());
            SetProperty("#Trakt.Episode.Ratings.Votes", item.Ratings.Votes.ToString());
            SetProperty("#Trakt.Episode.EpisodeImageFilename", episode.Images.EpisodeImageFilename);

            PublishSeasonSkinProperties(item);
        }

        private void PublishSeasonSkinProperties(TraktUserListItem item)
        {
            if (item == null) return;
            SetProperty("#Trakt.Season.Number", item.SeasonNumber);
            PublishShowSkinProperties(item);
        }

        private void PublishShowSkinProperties(TraktUserListItem item)
        {
            if (item == null) return;

            TraktShow show = item.Show;
            if (show == null) return;

            SetProperty("#Trakt.Show.AirDay", show.AirDay);
            SetProperty("#Trakt.Show.AirTime", show.AirTime);
            SetProperty("#Trakt.Show.Country", show.Country);
            SetProperty("#Trakt.Show.Network", show.Network);
            SetProperty("#Trakt.Show.TvRage", show.TvRage);
            SetProperty("#Trakt.Show.Imdb", show.Imdb);
            SetProperty("#Trakt.Show.Certification", show.Certification);
            SetProperty("#Trakt.Show.Overview", string.IsNullOrEmpty(show.Overview) ? Translation.NoShowSummary : show.Overview);
            SetProperty("#Trakt.Show.FirstAired", show.FirstAired.FromEpoch().ToShortDateString());
            SetProperty("#Trakt.Show.Runtime", show.Runtime.ToString());
            SetProperty("#Trakt.Show.Title", show.Title);
            SetProperty("#Trakt.Show.Url", show.Url);
            SetProperty("#Trakt.Show.Year", show.Year.ToString());
            SetProperty("#Trakt.Show.PosterImageFilename", show.Images.PosterImageFilename);
            SetProperty("#Trakt.Show.FanartImageFilename", show.Images.FanartImageFilename);
            SetProperty("#Trakt.Show.InWatchList", item.InWatchList.ToString());
            SetProperty("#Trakt.Show.Rating", item.Rating);
            SetProperty("#Trakt.Show.Ratings.Icon", (item.Ratings.LovedCount > item.Ratings.HatedCount) ? "love" : "hate");
            SetProperty("#Trakt.Show.Ratings.HatedCount", item.Ratings.HatedCount.ToString());
            SetProperty("#Trakt.Show.Ratings.LovedCount", item.Ratings.LovedCount.ToString());
            SetProperty("#Trakt.Show.Ratings.Percentage", item.Ratings.Percentage.ToString());
            SetProperty("#Trakt.Show.Ratings.Votes", item.Ratings.Votes.ToString());
        }

        private void PublishMovieSkinProperties(TraktUserListItem item)
        {
            if (item == null || item.Movie == null) return;

            SetProperty("#Trakt.Movie.Imdb", item.Movie.Imdb);
            SetProperty("#Trakt.Movie.Certification", item.Movie.Certification);
            SetProperty("#Trakt.Movie.Overview", string.IsNullOrEmpty(item.Movie.Overview) ? Translation.NoMovieSummary : item.Movie.Overview);
            SetProperty("#Trakt.Movie.Released", item.Movie.Released.FromEpoch().ToShortDateString());
            SetProperty("#Trakt.Movie.Runtime", item.Movie.Runtime.ToString());
            SetProperty("#Trakt.Movie.Tagline", item.Movie.Tagline);
            SetProperty("#Trakt.Movie.Title", item.Movie.Title);
            SetProperty("#Trakt.Movie.Tmdb", item.Movie.Tmdb);
            SetProperty("#Trakt.Movie.Trailer", item.Movie.Trailer);
            SetProperty("#Trakt.Movie.Url", item.Movie.Url);
            SetProperty("#Trakt.Movie.Year", item.Movie.Year);
            SetProperty("#Trakt.Movie.PosterImageFilename", item.Movie.Images.PosterImageFilename);
            SetProperty("#Trakt.Movie.FanartImageFilename", item.Movie.Images.FanartImageFilename);
            SetProperty("#Trakt.Movie.InCollection", item.InCollection.ToString());
            SetProperty("#Trakt.Movie.InWatchList", item.InWatchList.ToString());
            SetProperty("#Trakt.Movie.Plays", item.Plays.ToString());
            SetProperty("#Trakt.Movie.Watched", item.Watched.ToString());
            SetProperty("#Trakt.Movie.Rating", item.Rating);
            SetProperty("#Trakt.Movie.Ratings.Icon", (item.Ratings.LovedCount > item.Ratings.HatedCount) ? "love" : "hate");
            SetProperty("#Trakt.Movie.Ratings.HatedCount", item.Ratings.HatedCount.ToString());
            SetProperty("#Trakt.Movie.Ratings.LovedCount", item.Ratings.LovedCount.ToString());
            SetProperty("#Trakt.Movie.Ratings.Percentage", item.Ratings.Percentage.ToString());
            SetProperty("#Trakt.Movie.Ratings.Votes", item.Ratings.Votes.ToString());
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
                    #if !MP12
                    // refresh the facade so thumbnails get displayed
                    // this is not needed in MP 1.2 Beta
                    GUIWindowManager.SendThreadMessage(new GUIMessage(GUIMessage.MessageType.GUI_MSG_REFRESH, GUIWindowManager.ActiveWindow, 0, 50, 0, 0, null));
                    #endif
                })
                {
                    IsBackground = true,
                    Name = "Trakt Image Downloader " + i.ToString()
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
                        UpdateCurrentSelection();
                    if (s is TraktShow.ShowImages && e.PropertyName == "PosterImageFilename")
                        SetImageToGui((s as TraktShow.ShowImages).PosterImageFilename);
                    // re-size season posters to same as series/movie posters
                    if (s is TraktShow.ShowImages && e.PropertyName == "SeasonImageFilename")
                        SetImageToGui((s as TraktShow.ShowImages).SeasonImageFilename, new Size(300, 434));
                    if (s is TraktShow.ShowImages && e.PropertyName == "FanartImageFilename")
                        UpdateCurrentSelection();
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

            RatingOverlayImage ratingOverlay = RatingOverlayImage.None;

            if (listItem.Rating == "love")
                ratingOverlay = RatingOverlayImage.Love;
            else if (listItem.Rating == "hate")
                ratingOverlay = RatingOverlayImage.Hate;

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
            UpdateCurrentSelection();
        }

        protected void UpdateCurrentSelection()
        {
            GUIListItems window = GUIWindowManager.GetWindow(GUIWindowManager.ActiveWindow) as GUIListItems;
            if (window != null)
            {
                GUIListItem selectedItem = GUIControl.GetSelectedListItem((int)TraktGUIWindows.ListItems, 50);
                if (selectedItem == this)
                {
                    GUIWindowManager.SendThreadMessage(new GUIMessage(GUIMessage.MessageType.GUI_MSG_ITEM_SELECT, GUIWindowManager.ActiveWindow, 0, 50, ItemId, 0, null));
                }
            }
        }
    }
}