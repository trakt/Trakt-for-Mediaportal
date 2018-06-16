using MediaPortal.GUI.Library;
using MediaPortal.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using TraktAPI.DataStructures;
using TraktAPI.Enums;
using TraktAPI.Extensions;
using TraktPlugin.Cache;
using TraktPlugin.TmdbAPI.DataStructures;
using Action = MediaPortal.GUI.Library.Action;

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
            Cast,
            Crew,
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
        GUIFacadeControl.Layout CurrentLayout { get; set; }
        ImageSwapper backdrop;
        TraktItemType SelectedType { get; set; }
        int PreviousSlug { get; set; }
        int PreviousSelectedIndex = 0;
        List<TraktListItem> CurrentListItems { get; set; }

        #endregion

        #region Base Overrides

        public override int GetID
        {
            get
            {
                return (int)TraktGUIWindows.CustomListItems;
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
                        {
                            CheckAndPlayMovie(true);
                        }
                        else if (TraktSettings.EnableJumpToForTVShows || SelectedType == TraktItemType.episode)
                        {
                            CheckAndPlayEpisode(true);
                        }
                        else if (SelectedType == TraktItemType.show)
                        {
                            var selectedItem = this.Facade.SelectedListItem;
                            if (selectedItem == null) return;

                            var listItem = selectedItem.TVTag as TraktListItem;
                            if (listItem == null) return;

                            GUIWindowManager.ActivateWindow((int)TraktGUIWindows.ShowSeasons, listItem.Show.ToJSON());
                        }
                        else if (SelectedType == TraktItemType.season)
                        {
                            var selectedItem = this.Facade.SelectedListItem;
                            if (selectedItem == null) return;

                            var listItem = selectedItem.TVTag as TraktListItem;
                            if (listItem == null) return;

                            // create loading parameter for episode listing
                            var loadingParam = new SeasonLoadingParameter
                            {
                                Season = listItem.Season,
                                Show = listItem.Show
                            };
                            GUIWindowManager.ActivateWindow((int)TraktGUIWindows.SeasonEpisodes, loadingParam.ToJSON());
                        }
                        else if (SelectedType == TraktItemType.person)
                        {
                            var selectedItem = Facade.SelectedListItem;
                            if (selectedItem == null) return;

                            var listItem = selectedItem.TVTag as TraktListItem;
                            if (listItem == null) return;

                            // if we already have the person summary, parse it along to the window
                            GUIPersonSummary.CurrentPerson = listItem.Person;
                            GUIWindowManager.ActivateWindow((int)TraktGUIWindows.PersonSummary);
                        }
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
            var selectedItem = this.Facade.SelectedListItem;
            if (selectedItem == null) return;

            var selectedListItem = selectedItem.TVTag as TraktListItem;
            if (selectedListItem == null) return;

            var dlg = (IDialogbox)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
            if (dlg == null) return;

            dlg.Reset();
            dlg.SetHeading(GUIUtils.PluginName());

            GUIListItem listItem = null;

            if (SelectedType == TraktItemType.movie || SelectedType == TraktItemType.episode)
            {
                // Mark As Watched
                if (!selectedListItem.IsWatched())
                {
                    listItem = new GUIListItem(Translation.MarkAsWatched);
                    dlg.Add(listItem);
                    listItem.ItemId = (int)ContextMenuItem.MarkAsWatched;
                }

                // Mark As UnWatched
                if (selectedListItem.IsWatched())
                {
                    listItem = new GUIListItem(Translation.MarkAsUnWatched);
                    dlg.Add(listItem);
                    listItem.ItemId = (int)ContextMenuItem.MarkAsUnWatched;
                }
            }

            if (SelectedType != TraktItemType.season && SelectedType != TraktItemType.person)
            {
                // Add/Remove Watchlist
                if (!selectedListItem.IsWatchlisted())
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

            // Remove from Custom list (only if current user is the active user)
            if (TraktSettings.Username == CurrentUser)
            {
                listItem = new GUIListItem(Translation.RemoveFromList);
                dlg.Add(listItem);
                listItem.ItemId = (int)ContextMenuItem.RemoveFromList;
            }

            if (SelectedType == TraktItemType.movie || SelectedType == TraktItemType.episode)
            {
                // Add to Collection
                // Don't allow if it will be removed again on next sync
                // movie could be part of a DVD collection
                if (!selectedListItem.IsCollected() && !TraktSettings.KeepTraktLibraryClean)
                {
                    listItem = new GUIListItem(Translation.AddToLibrary);
                    dlg.Add(listItem);
                    listItem.ItemId = (int)ContextMenuItem.AddToLibrary;
                }

                if (selectedListItem.IsCollected())
                {
                    listItem = new GUIListItem(Translation.RemoveFromLibrary);
                    dlg.Add(listItem);
                    listItem.ItemId = (int)ContextMenuItem.RemoveFromLibrary;
                }
            }

            // Related Movies/Shows
            if (SelectedType != TraktItemType.person)
            {
                listItem = new GUIListItem(SelectedType == TraktItemType.movie ? Translation.RelatedMovies : Translation.RelatedShows);
                dlg.Add(listItem);
                listItem.ItemId = (int)ContextMenuItem.Related;
            }

            if (SelectedType != TraktItemType.season && SelectedType != TraktItemType.person)
            {
                // Rate
                listItem = new GUIListItem(Translation.Rate + "...");
                dlg.Add(listItem);
                listItem.ItemId = (int)ContextMenuItem.Rate;

                // Shouts
                listItem = new GUIListItem(Translation.Comments);
                dlg.Add(listItem);
                listItem.ItemId = (int)ContextMenuItem.Shouts;
            }

            // Cast and Crew
            if (SelectedType == TraktItemType.movie || SelectedType == TraktItemType.show)
            {
                listItem = new GUIListItem(Translation.Cast);
                dlg.Add(listItem);
                listItem.ItemId = (int)ContextMenuItem.Cast;

                listItem = new GUIListItem(Translation.Crew);
                dlg.Add(listItem);
                listItem.ItemId = (int)ContextMenuItem.Crew;
            }

            // Trailers
            if (SelectedType != TraktItemType.person)
            {
                if (TraktHelper.IsTrailersAvailableAndEnabled)
                {
                    listItem = new GUIListItem(Translation.Trailers);
                    dlg.Add(listItem);
                    listItem.ItemId = (int)ContextMenuItem.Trailers;
                }
            }

            // Search with mpNZB
            if (TraktHelper.IsMpNZBAvailableAndEnabled)
            {
                if ((selectedListItem.Movie != null && !selectedListItem.Movie.IsCollected()) || selectedListItem.Episode != null)
                {
                    listItem = new GUIListItem(Translation.SearchWithMpNZB);
                    dlg.Add(listItem);
                    listItem.ItemId = (int)ContextMenuItem.SearchWithMpNZB;
                }
            }

            // Search with MyTorrents
            if (TraktHelper.IsMyTorrentsAvailableAndEnabled)
            {
                if ((selectedListItem.Movie != null && !selectedListItem.Movie.IsCollected()) || selectedListItem.Episode != null)
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
                    AddItemToWatchedHistory(selectedListItem);
                    selectedItem.IsPlayed = true;
                    OnItemSelected(selectedItem, Facade);
                    if (SelectedType == TraktItemType.movie)
                        (Facade.SelectedListItem as GUICustomListItem).Images.NotifyPropertyChanged("MoviePoster");
                    else
                        (Facade.SelectedListItem as GUICustomListItem).Images.NotifyPropertyChanged("ShowPoster");
                    break;

                case ((int)ContextMenuItem.MarkAsUnWatched):
                    RemoveItemFromWatchedHistory(selectedListItem);
                    selectedItem.IsPlayed = false;
                    OnItemSelected(selectedItem, Facade);
                    if (SelectedType == TraktItemType.movie)
                        (Facade.SelectedListItem as GUICustomListItem).Images.NotifyPropertyChanged("MoviePoster");
                    else
                        (Facade.SelectedListItem as GUICustomListItem).Images.NotifyPropertyChanged("ShowPoster");
                    break;

                case ((int)ContextMenuItem.AddToWatchList):
                    AddItemToWatchList(selectedListItem);
                    OnItemSelected(selectedItem, Facade);
                    if (SelectedType == TraktItemType.movie)
                        (Facade.SelectedListItem as GUICustomListItem).Images.NotifyPropertyChanged("MoviePoster");
                    else
                        (Facade.SelectedListItem as GUICustomListItem).Images.NotifyPropertyChanged("ShowPoster");

                    GUIWatchListMovies.ClearCache(TraktSettings.Username);
                    break;

                case ((int)ContextMenuItem.RemoveFromWatchList):
                    RemoveItemFromWatchList(selectedListItem);
                    OnItemSelected(selectedItem, Facade);
                    if (SelectedType == TraktItemType.movie)
                        (Facade.SelectedListItem as GUICustomListItem).Images.NotifyPropertyChanged("MoviePoster");
                    else
                        (Facade.SelectedListItem as GUICustomListItem).Images.NotifyPropertyChanged("ShowPoster");

                    GUIWatchListMovies.ClearCache(TraktSettings.Username);
                    break;

                case ((int)ContextMenuItem.AddToList):
                    if (SelectedType == TraktItemType.movie)
                        TraktHelper.AddRemoveMovieInUserList(selectedListItem.Movie, false);
                    else if (SelectedType == TraktItemType.show)
                        TraktHelper.AddRemoveShowInUserList(selectedListItem.Show, false);
                    else if (SelectedType == TraktItemType.season)
                        TraktHelper.AddRemoveSeasonInUserList(selectedListItem.Season, false);
                    else if (SelectedType == TraktItemType.episode)
                        TraktHelper.AddRemoveEpisodeInUserList(selectedListItem.Episode, false);
                    else if (SelectedType == TraktItemType.person)
                        TraktHelper.AddRemovePersonInUserList(selectedListItem.Person, false);
                    break;

                case ((int)ContextMenuItem.RemoveFromList):
                    if (!GUIUtils.ShowYesNoDialog(Translation.DeleteListItem, Translation.ConfirmDeleteListItem)) break;

                    // Only do remove from current list
                    // We could do same as Add (ie remove from multiple lists) but typically you only remove from the current list
                    TraktHelper.AddRemoveItemInList((int)CurrentList.Ids.Trakt, GetSyncItems(selectedListItem), true);

                    // clear the list item cache
                    TraktLists.ClearListItemCache(CurrentUser, CurrentList.Ids.Trakt.ToString());

                    // remove item from collection
                    CurrentListItems.RemoveAll(l => ListItemMatch(l, selectedListItem));

                    // clear the cache
                    TraktLists.ClearListItemCache(TraktSettings.Username, CurrentList.Ids.Trakt.ToString());

                    // Remove from view
                    if (Facade.Count > 1)
                    {
                        PreviousSelectedIndex = Facade.SelectedListItemIndex;
                        SendListItemsToFacade(CurrentListItems);
                    }
                    else
                    {
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
                    AddItemToCollection(selectedListItem);
                    OnItemSelected(selectedItem, Facade);
                    if (SelectedType == TraktItemType.movie)
                        (Facade.SelectedListItem as GUICustomListItem).Images.NotifyPropertyChanged("MoviePoster");
                    else
                        (Facade.SelectedListItem as GUICustomListItem).Images.NotifyPropertyChanged("ShowPoster");
                    break;

                case ((int)ContextMenuItem.RemoveFromLibrary):
                    RemoveItemFromCollection(selectedListItem);
                    OnItemSelected(selectedItem, Facade);
                    if (SelectedType == TraktItemType.movie)
                        (Facade.SelectedListItem as GUICustomListItem).Images.NotifyPropertyChanged("MoviePoster");
                    else
                        (Facade.SelectedListItem as GUICustomListItem).Images.NotifyPropertyChanged("ShowPoster");
                    break;

                case ((int)ContextMenuItem.Related):
                    if (SelectedType == TraktItemType.movie)
                    {
                        TraktHelper.ShowRelatedMovies(selectedListItem.Movie);
                    }
                    else
                    {
                        //series, season & episode
                        TraktHelper.ShowRelatedShows(selectedListItem.Show);
                    }
                    break;

                case ((int)ContextMenuItem.Rate):
                    RateItem(selectedListItem);
                    OnItemSelected(selectedItem, Facade);
                    if (SelectedType == TraktItemType.movie)
                        (Facade.SelectedListItem as GUICustomListItem).Images.NotifyPropertyChanged("MoviePoster");
                    else
                        (Facade.SelectedListItem as GUICustomListItem).Images.NotifyPropertyChanged("ShowPoster");
                    break;

                case ((int)ContextMenuItem.Shouts):
                    if (SelectedType == TraktItemType.movie)
                        TraktHelper.ShowMovieShouts(selectedListItem.Movie);
                    else if (SelectedType == TraktItemType.show)
                        TraktHelper.ShowTVShowShouts(selectedListItem.Show);
                    else
                        TraktHelper.ShowEpisodeShouts(selectedListItem.Show, selectedListItem.Episode);
                    break;

                case ((int)ContextMenuItem.Cast):
                    if (SelectedType == TraktItemType.movie)
                    {
                        GUICreditsMovie.Movie = selectedListItem.Movie;
                        GUICreditsMovie.Type = GUICreditsMovie.CreditType.Cast;
                        GUICreditsMovie.Fanart = TmdbCache.GetMovieBackdropFilename((selectedItem as GUIMovieListItem).Images.MovieImages);
                        GUIWindowManager.ActivateWindow((int)TraktGUIWindows.CreditsMovie);
                    }
                    else if (SelectedType == TraktItemType.show)
                    {
                        GUICreditsShow.Show = selectedListItem.Show;
                        GUICreditsShow.Type = GUICreditsShow.CreditType.Cast;
                        GUICreditsShow.Fanart = TmdbCache.GetShowBackdropFilename((selectedItem as GUIShowListItem).Images.ShowImages);
                        GUIWindowManager.ActivateWindow((int)TraktGUIWindows.CreditsShow);
                    }
                    break;

                case ((int)ContextMenuItem.Crew):
                    if (SelectedType == TraktItemType.movie)
                    {
                        GUICreditsMovie.Movie = selectedListItem.Movie;
                        GUICreditsMovie.Type = GUICreditsMovie.CreditType.Crew;
                        GUICreditsMovie.Fanart = TmdbCache.GetMovieBackdropFilename((selectedItem as GUIMovieListItem).Images.MovieImages);
                        GUIWindowManager.ActivateWindow((int)TraktGUIWindows.CreditsMovie);
                    }
                    else if (SelectedType == TraktItemType.show)
                    {
                        GUICreditsShow.Show = selectedListItem.Show;
                        GUICreditsShow.Type = GUICreditsShow.CreditType.Crew;
                        GUICreditsShow.Fanart = TmdbCache.GetShowBackdropFilename((selectedItem as GUIShowListItem).Images.ShowImages);
                        GUIWindowManager.ActivateWindow((int)TraktGUIWindows.CreditsShow);
                    }
                    break;

                case ((int)ContextMenuItem.Trailers):
                    if (SelectedType == TraktItemType.movie)
                    {
                        GUICommon.ShowMovieTrailersMenu(selectedListItem.Movie);
                    }
                    else if (SelectedType == TraktItemType.episode)
                    {
                        GUICommon.ShowTVShowTrailersMenu(selectedListItem.Show, selectedListItem.Episode);
                    }
                    else if (SelectedType == TraktItemType.season && TraktHelper.IsTrailersAvailableAndEnabled)
                    {
                        GUICommon.ShowTVSeasonTrailersPluginMenu(selectedListItem.Show, selectedListItem.Season.Number);
                    }
                    break;

                case ((int)ContextMenuItem.SearchWithMpNZB):
                    string loadingParam = String.Empty;
                    if (selectedListItem.Movie != null)
                    {
                        loadingParam = string.Format("search:{0}", selectedListItem.Movie.Title);
                    }
                    else if (selectedListItem.Episode != null)
                    {
                        loadingParam = string.Format("search:{0} S{1}E{2}", selectedListItem.Show.Title, selectedListItem.Episode.Season.ToString("D2"), selectedListItem.Episode.Number.ToString("D2"));
                    }
                    GUIWindowManager.ActivateWindow((int)ExternalPluginWindows.MpNZB, loadingParam);
                    break;

                case ((int)ContextMenuItem.SearchTorrent):
                    string loadPar = String.Empty;
                    if (selectedListItem.Movie != null)
                    {
                        loadPar = selectedListItem.Movie.Title;
                    }
                    else if (selectedListItem.Episode != null)
                    {
                        loadPar = string.Format("{0} S{1}E{2}", selectedListItem.Show.Title, selectedListItem.Episode.Season.ToString("D2"), selectedListItem.Episode.Number.ToString("D2"));
                    }
                    GUIWindowManager.ActivateWindow((int)ExternalPluginWindows.MyTorrents, loadPar);
                    break;

                case ((int)ContextMenuItem.ChangeLayout):
                    CurrentLayout = GUICommon.ShowLayoutMenu(CurrentLayout, PreviousSelectedIndex);
                    break;

                default:
                    break;
            }

            base.OnShowContextMenu();
        }

        #endregion

        #region Private Methods

        private TraktSyncAll GetSyncItems(TraktListItem listItem)
        {
            var syncItems = new TraktSyncAll();

            switch (listItem.Type)
            {
                case "movie":
                    var movie = new TraktMovie
                    {
                        Ids = new TraktMovieId { Trakt = listItem.Movie.Ids.Trakt }
                    };
                    syncItems.Movies = new List<TraktMovie>();
                    syncItems.Movies.Add(movie);
                    break;

                case "show":
                    var show = new TraktShow
                    {
                        Ids = new TraktShowId { Trakt = listItem.Show.Ids.Trakt }
                    };
                    syncItems.Shows = new List<TraktShow>();
                    syncItems.Shows.Add(show);
                    break;

                case "season":
                    var season = new TraktSeason
                    {
                        Ids = new TraktSeasonId { Trakt = listItem.Season.Ids.Trakt }
                    };
                    syncItems.Seasons = new List<TraktSeason>();
                    syncItems.Seasons.Add(season);
                    break;

                case "episode":
                    var episode = new TraktEpisode
                    {
                        Ids = new TraktEpisodeId { Trakt = listItem.Episode.Ids.Trakt }
                    };
                    syncItems.Episodes = new List<TraktEpisode>();
                    syncItems.Episodes.Add(episode);
                    break;

                case "person":
                    var person = new TraktPerson
                    {
                        Ids = new TraktPersonId { Trakt = listItem.Person.Ids.Trakt }
                    };
                    syncItems.People = new List<TraktPerson>();
                    syncItems.People.Add(person);
                    break;
            }

            return syncItems;
        }

        private bool ListItemMatch(TraktListItem currentItem, TraktListItem itemToMatch)
        {
            switch (itemToMatch.Type)
            {
                case "movie":
                    if (currentItem.Movie == null) return false;
                    return currentItem.Movie.Ids.Trakt == itemToMatch.Movie.Ids.Trakt;
                
                case "show":
                    if (currentItem.Show == null) return false;
                    return currentItem.Show.Ids.Trakt == itemToMatch.Show.Ids.Trakt;

                case "season":
                    if (currentItem.Season == null) return false;
                    return currentItem.Season.Ids.Trakt == itemToMatch.Season.Ids.Trakt;

                case "episode":
                    if (currentItem.Episode == null) return false;
                    return currentItem.Episode.Ids.Trakt == itemToMatch.Episode.Ids.Trakt;

                case "person":
                    if (currentItem.Person == null) return false;
                    return currentItem.Person.Ids.Trakt == itemToMatch.Person.Ids.Trakt;
            }

            return false;
        }

        private void CheckAndPlayEpisode(bool jumpTo)
        {
            var selectedItem = this.Facade.SelectedListItem;
            if (selectedItem == null) return;

            var userListItem = selectedItem.TVTag as TraktListItem;
            if (userListItem == null) return;

            // if its a show/season, play first unwatched
            if (SelectedType == TraktItemType.season || SelectedType == TraktItemType.show)
            {
                GUICommon.CheckAndPlayFirstUnwatchedEpisode(userListItem.Show, jumpTo);
            }
            else if (SelectedType == TraktItemType.episode)
            {
                GUICommon.CheckAndPlayEpisode(userListItem.Show, userListItem.Episode);
            }
        }

        private void CheckAndPlayMovie(bool jumpTo)
        {
            var selectedItem = this.Facade.SelectedListItem;
            if (selectedItem == null) return;

            var userListItem = selectedItem.TVTag as TraktListItem;
            if (userListItem == null || userListItem.Movie == null) return;

            GUICommon.CheckAndPlayMovie(jumpTo, userListItem.Movie);
        }

        private void AddItemToWatchList(TraktListItem item)
        {
            if (SelectedType == TraktItemType.movie)
            {
                TraktHelper.AddMovieToWatchList(item.Movie, true);
            }
            else if (SelectedType == TraktItemType.show)
            {
                TraktHelper.AddShowToWatchList(item.Show);
            }
            else if (SelectedType == TraktItemType.episode)
            {
                TraktHelper.AddEpisodeToWatchList(item.Episode);
                TraktCache.AddEpisodeToWatchlist(item.Show, item.Episode);
            }
        }

        private void RemoveItemFromWatchList(TraktListItem item)
        {
            if (SelectedType == TraktItemType.movie)
            {
                TraktHelper.RemoveMovieFromWatchList(item.Movie, true);
            }
            else if (SelectedType == TraktItemType.show)
            {
                TraktHelper.RemoveShowFromWatchList(item.Show);
            }
            else if (SelectedType == TraktItemType.episode)
            {
                TraktHelper.RemoveEpisodeFromWatchList(item.Episode);
            }
        }

        private void AddItemToWatchedHistory(TraktListItem item)
        {
            if (SelectedType == TraktItemType.movie)
            {
                TraktHelper.AddMovieToWatchHistory(item.Movie);
            }
            else if (SelectedType == TraktItemType.episode)
            {
                TraktHelper.AddEpisodeToWatchHistory(item.Episode);
                TraktCache.AddEpisodeToWatchHistory(item.Show, item.Episode);
            }
        }

        private void RemoveItemFromWatchedHistory(TraktListItem item)
        {
            if (SelectedType == TraktItemType.movie)
            {
                TraktHelper.RemoveMovieFromWatchHistory(item.Movie);
            }
            else if (SelectedType == TraktItemType.episode)
            {
                TraktHelper.RemoveEpisodeFromWatchHistory(item.Episode);
                TraktCache.RemoveEpisodeFromWatchHistory(item.Show, item.Episode);
            }
        }

        private void AddItemToCollection(TraktListItem item)
        {
            if (SelectedType == TraktItemType.movie)
            {
                TraktHelper.AddMovieToCollection(item.Movie);
            }
            else if (SelectedType == TraktItemType.episode)
            {
                TraktHelper.AddEpisodeToCollection(item.Episode);
                TraktCache.AddEpisodeToCollection(item.Show, item.Episode);
            }
        }

        private void RemoveItemFromCollection(TraktListItem item)
        {
            if (SelectedType == TraktItemType.movie)
            {
                TraktHelper.RemoveMovieFromCollection(item.Movie);
            }
            else if (SelectedType == TraktItemType.episode)
            {
                TraktHelper.RemoveEpisodeFromCollection(item.Episode);
                TraktCache.RemoveEpisodeFromCollection(item.Show, item.Episode);
            }
        }

        private void RateItem(TraktListItem item)
        {
            if (SelectedType == TraktItemType.movie)
                GUICommon.RateMovie(item.Movie);
            else if (SelectedType == TraktItemType.show)
                GUICommon.RateShow(item.Show);
            else if (SelectedType == TraktItemType.episode)
                GUICommon.RateEpisode(item.Show, item.Episode);
        }

        private void LoadListItems()
        {
            GUIUtils.SetProperty("#Trakt.Items", string.Empty);

            GUIBackgroundTask.Instance.ExecuteInBackgroundAndCallback(() =>
            {
                var listItems = TraktLists.GetListItemsForUser(CurrentUser, (int)CurrentList.Ids.Trakt);
                return listItems;
            },
            delegate(bool success, object result)
            {
                if (success)
                {
                    var userListItems = result as IEnumerable<TraktListItem>;
                    SendListItemsToFacade(userListItems);
                }
            }, Translation.GettingListItems, true);
        }

        private void SendListItemsToFacade(IEnumerable<TraktListItem> listItems)
        {
            // clear facade
            GUIControl.ClearControl(GetID, Facade.GetID);

            if (listItems == null)
            {
                GUIUtils.ShowNotifyDialog(Translation.Error, Translation.ErrorGeneral);
                GUIWindowManager.ShowPreviousWindow();
                return;
            }

            CurrentListItems = listItems.ToList();

            if (listItems.Count() == 0)
            {
                GUIUtils.ShowNotifyDialog(GUIUtils.PluginName(), Translation.NoListItemsFound);
                GUIWindowManager.ShowPreviousWindow();
                return;
            }

            int itemId = 1;
            var listImages = new List<GUITmdbImage>();
            
            // Add each list item
            foreach (var listItem in listItems)
            {
                // add image for download
                var images = GetTmdbImage(listItem);
                listImages.Add(images);

                string itemName = CurrentList.DisplayNumbers ? string.Format("{0}. {1}", itemId, GetListItemLabel(listItem)) : GetListItemLabel(listItem);

                var item = new GUICustomListItem(itemName, (int)TraktGUIWindows.CustomListItems);
                
                item.Label2 = GetListItemSecondLabel(listItem);
                item.TVTag = listItem;
                item.Type = (TraktItemType)Enum.Parse(typeof(TraktItemType), listItem.Type, true);
                item.Movie = listItem.Movie;
                item.Show = listItem.Show;
                item.Episode = listItem.Episode;
                item.Season = listItem.Season;
                item.Person = listItem.Person;
                item.Images = images;
                item.IsPlayed = listItem.IsWatched();
                item.ItemId = Int32.MaxValue - itemId;
                item.IconImage = GUIImageHandler.GetDefaultPoster(false);
                item.IconImageBig = GUIImageHandler.GetDefaultPoster();
                item.ThumbnailImage = GUIImageHandler.GetDefaultPoster();
                item.OnItemSelected += OnItemSelected;
                Utils.SetDefaultIcons(item);
                Facade.Add(item);
                itemId++;
            }

            // Set Facade Layout
            Facade.CurrentLayout = CurrentLayout;
            GUIControl.FocusControl(GetID, Facade.GetID);

            Facade.SelectIndex(PreviousSelectedIndex);

            // set facade properties
            GUIUtils.SetProperty("#itemcount", listItems.Count().ToString());
            GUIUtils.SetProperty("#Trakt.Items", string.Format("{0} {1}", listItems.Count().ToString(), listItems.Count() > 1 ? Translation.Items : Translation.Item));

            // Download images Async and set to facade
            GUICustomListItem.GetImages(listImages);
        }

        private string GetListItemLabel(TraktListItem listItem)
        {
            string retValue = string.Empty;

            switch (listItem.Type)
            {
                case "movie":
                    retValue = listItem.Movie.Title;
                    break;

                case "show":
                    retValue = listItem.Show.Title;
                    break;

                case "season":
                    retValue = string.Format("{0} {1} {2}", listItem.Show.Title, GUI.Translation.Season, listItem.Season.Number);
                    break;

                case "episode":
                    retValue = string.Format("{0} - {1}x{2}{3}", listItem.Show.Title, listItem.Episode.Season, listItem.Episode.Number, string.IsNullOrEmpty(listItem.Episode.Title) ? string.Empty : " - " + listItem.Episode.Title);
                    break;

                case "person":
                    retValue = listItem.Person.Name;
                    break;
            }
            return retValue;
        }

        private string GetListItemSecondLabel(TraktListItem listItem)
        {
            string retValue = string.Empty;

            switch (listItem.Type)
            {
                case "movie":
                    retValue = listItem.Movie.Year == null ? "----" : listItem.Movie.Year.ToString();
                    break;

                case "show":
                    retValue = listItem.Show.Year == null ? "----" : listItem.Show.Year.ToString();
                    break;

                case "season":
                    retValue = string.Format("{0} {1}", listItem.Season.EpisodeCount, Translation.Episodes);
                    break;

                case "episode":
                    retValue = listItem.Episode.FirstAired.FromISO8601().ToShortDateString();
                    break;

                case "person":
                    retValue = listItem.Person.Birthday;
                    break;
            }
            return retValue;
        }

        private GUITmdbImage GetTmdbImage(TraktListItem listItem)
        {
            var images = new GUITmdbImage();

            switch (listItem.Type)
            {
                case "movie":
                    images.MovieImages = new TmdbMovieImages { Id = listItem.Movie.Ids.Tmdb };
                    break;

                case "show":
                    images.ShowImages = new TmdbShowImages { Id = listItem.Show.Ids.Tmdb };
                    break;
                case "season":
                    images.ShowImages = new TmdbShowImages { Id = listItem.Show.Ids.Tmdb };
                    images.SeasonImages = new TmdbSeasonImages
                    {
                        Id = listItem.Show.Ids.Tmdb,
                        Season = listItem.Season.Number
                    };
                    break;
                case "episode":
                    images.ShowImages = new TmdbShowImages { Id = listItem.Show.Ids.Tmdb };
                    images.SeasonImages = new TmdbSeasonImages
                    {
                        Id = listItem.Show.Ids.Tmdb,
                        Season = listItem.Episode.Season
                    };
                    break;
                case "person":
                    images.PeopleImages = new TmdbPeopleImages { Id = listItem.Person.Ids.TmdbId };
                    break;
            }

            return images;
        }

        private void InitProperties()
        {
            GUICommon.SetProperty("#Trakt.List.Username", CurrentUser);
            GUICommon.SetListProperties(CurrentList, CurrentUser);

            if (PreviousSlug != CurrentList.Ids.Trakt)
                PreviousSelectedIndex = 0;

            PreviousSlug = (int)CurrentList.Ids.Trakt;

            // Fanart
            backdrop.GUIImageOne = FanartBackground;
            backdrop.GUIImageTwo = FanartBackground2;
            backdrop.LoadingImage = loadingImage;

            // load last layout
            CurrentLayout = (GUIFacadeControl.Layout)TraktSettings.ListItemsDefaultLayout;
            // update button label
            GUIControl.SetControlLabel(GetID, layoutButton.GetID, GUICommon.GetLayoutTranslation(CurrentLayout));
        }

        private void ClearProperties()
        {
            GUICommon.ClearMovieProperties();
            GUICommon.ClearShowProperties();
            GUICommon.ClearSeasonProperties();
            GUICommon.ClearEpisodeProperties();
            GUICommon.ClearPersonProperties();
        }

        private void PublishEpisodeSkinProperties(TraktListItem item)
        {
            if (item == null || item.Episode == null) return;

            GUICommon.SetProperty("#Trakt.Season.Number", item.Episode.Season);
            GUICommon.SetEpisodeProperties(item.Show, item.Episode);
            
            PublishShowSkinProperties(item);
        }

        private void PublishSeasonSkinProperties(TraktListItem item)
        {
            if (item == null || item.Season == null) return;

            GUICommon.SetSeasonProperties(item.Show, item.Season);

            PublishShowSkinProperties(item);
        }

        private void PublishShowSkinProperties(TraktListItem item)
        {
            if (item == null || item.Show == null) return;

            GUICommon.SetShowProperties(item.Show);
        }

        private void PublishMovieSkinProperties(TraktListItem item)
        {
            if (item == null || item.Movie == null) return;

            GUICommon.SetMovieProperties(item.Movie);
        }

        private void PublishPersonSkinProperties(TraktListItem item)
        {
            if (item == null || item.Person == null) return;

            GUICommon.SetPersonProperties(item.Person);
        }

        private void OnItemSelected(GUIListItem item, GUIControl parent)
        {
            if (item == null) return;

            var listItem = item.TVTag as TraktListItem;
            if (listItem == null) return;

            PreviousSelectedIndex = Facade.SelectedListItemIndex;

            switch (listItem.Type)
            {
                case "movie":
                    SelectedType = TraktItemType.movie;
                    PublishMovieSkinProperties(listItem);
                    string fanart = TmdbCache.GetMovieBackdropFilename((item as GUICustomListItem).Images.MovieImages);
                    if (!string.IsNullOrEmpty(fanart))
                    {
                        GUIImageHandler.LoadFanart(backdrop, fanart);
                    }
                    break;

                case "show":
                    SelectedType = TraktItemType.show;
                    PublishShowSkinProperties(listItem);
                    fanart = TmdbCache.GetShowBackdropFilename((item as GUICustomListItem).Images.ShowImages);
                    if (!string.IsNullOrEmpty(fanart))
                    {
                        GUIImageHandler.LoadFanart(backdrop, fanart);
                    }
                    break;

                case "season":
                    SelectedType = TraktItemType.season;
                    PublishSeasonSkinProperties(listItem);
                    fanart = TmdbCache.GetShowBackdropFilename((item as GUICustomListItem).Images.ShowImages);
                    if (!string.IsNullOrEmpty(fanart))
                    {
                        GUIImageHandler.LoadFanart(backdrop, fanart);
                    }
                    break;

                case "episode":
                    SelectedType = TraktItemType.episode;
                    PublishEpisodeSkinProperties(listItem);
                    fanart = TmdbCache.GetShowBackdropFilename((item as GUICustomListItem).Images.ShowImages);
                    if (!string.IsNullOrEmpty(fanart))
                    {
                        GUIImageHandler.LoadFanart(backdrop, fanart);
                    }
                    break;

                case "person":
                    SelectedType = TraktItemType.person;
                    PublishPersonSkinProperties(listItem);
                    break;
            }
            GUIUtils.SetProperty("#Trakt.List.ItemType", SelectedType.ToString());
        }
        #endregion

        #region Public Properties

        public static TraktListDetail CurrentList { get; set; }
        public static string CurrentUser { get; set; }

        #endregion
    }
}