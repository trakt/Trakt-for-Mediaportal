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
using MediaPortal.GUI.Video;
using MediaPortal.Util;
using MediaPortal.Video.Database;
using TraktAPI;
using TraktAPI.DataStructures;
using TraktAPI.Enums;
using Action = MediaPortal.GUI.Library.Action;

namespace TraktPlugin.GUI
{
    public class GUISearch : GUIWindow
    {
        #region Skin Controls

        [SkinControl(50)]
        protected GUIFacadeControl Facade = null;

        [SkinControl(2)]
        protected GUIButtonControl searchButton = null;

        [SkinControl(3)]
        protected GUICheckButton movieSearchButton = null;

        [SkinControl(4)]
        protected GUICheckButton showSearchButton = null;

        [SkinControl(5)]
        protected GUICheckButton episodeSearchButton = null;

        [SkinControl(6)]
        protected GUICheckButton peopleSearchButton = null;

        [SkinControl(7)]
        protected GUICheckButton userSearchButton = null;

        [SkinControl(8)]
        protected GUICheckButton listSearchButton = null;

        #endregion

        #region Enums
        
        #endregion

        #region Constructor

        public GUISearch() { }

        #endregion

        #region Private Variables

        bool StopDownload { get; set; }
        int PreviousSelectedIndex { get; set; }
        SearchType SelectedSearchType { get; set; }
        HashSet<SearchType> SearchTypes = new HashSet<SearchType>();
        string SearchTerm = null;
        DateTime LastRequest = new DateTime();
        Dictionary<string, IEnumerable<TraktSearchResult>> searchCache = new Dictionary<string, IEnumerable<TraktSearchResult>>();

        IEnumerable<TraktSearchResult> SearchResults
        {
            get
            {
                string key = (SearchTerm ?? string.Empty) + GetSearchTypesID() + TraktSettings.MaxSearchResults;

                if (!searchCache.Keys.Contains(key) || LastRequest < DateTime.UtcNow.Subtract(new TimeSpan(0, TraktSettings.WebRequestCacheMinutes, 0)))
                {
                    _SearchResults = TraktAPI.TraktAPI.Search(SearchTerm, SearchTypes, TraktSettings.MaxSearchResults);
                    if (searchCache.Keys.Contains(key)) searchCache.Remove(key);
                    searchCache.Add(key, _SearchResults);
                    LastRequest = DateTime.UtcNow;
                    PreviousSelectedIndex = 0;
                }
                return searchCache[key];
            }
        }
        IEnumerable<TraktSearchResult> _SearchResults = null;
        #endregion

        #region Public Properties

        #endregion

        #region Base Overrides

        public override int GetID
        {
            get
            {
                return (int)TraktGUIWindows.Search;
            }
        }

        public override bool Init()
        {
            return Load(GUIGraphicsContext.Skin + @"\Trakt.Search.xml");
        }

        protected override void OnPageLoad()
        {
            base.OnPageLoad();

            // Clear GUI Properties
            ClearProperties();

            // Init Properties
            InitProperties();
        }

        protected override void OnPageDestroy(int new_windowId)
        {
            StopDownload = true;
            PreviousSelectedIndex = Facade.SelectedListItemIndex;
            ClearProperties();

            // save selected search types
            SearchType searchTypes = SearchType.none;
            if (movieSearchButton.IsSelected()) searchTypes |= SearchType.movie;
            if (showSearchButton.IsSelected()) searchTypes |= SearchType.show;
            if (episodeSearchButton.IsSelected()) searchTypes |= SearchType.episode;
            if (peopleSearchButton.IsSelected()) searchTypes |= SearchType.person;
            if (userSearchButton.IsSelected()) searchTypes |= SearchType.user;
            if (listSearchButton.IsSelected()) searchTypes |= SearchType.list;

            TraktSettings.SearchTypes = (int)searchTypes;

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
                        GUIListItem selectedItem = this.Facade.SelectedListItem;
                        if (selectedItem == null) return;

                        // Load selected search results
                        SendSearchResultsToWindow(SearchResults);
                    }
                    break;

                // Search
                case (2):
                    // check if there is any types selected
                    SetSearchTypes();
                    if (SearchTypes.Count > 0)
                    {
                        string searchTerm = SearchTerm ?? string.Empty;
                        if (GUIUtils.GetStringFromKeyboard(ref searchTerm))
                        {
                            if (!string.IsNullOrEmpty(searchTerm))
                            {
                                GUIUtils.SetProperty("#Trakt.Search.SearchTerm", searchTerm);

                                SearchTerm = searchTerm;
                                LoadSearchResults();
                            }
                        }
                    }
                    else
                    {
                        GUIUtils.ShowOKDialog(Translation.Search, Translation.NoSearchTypesSelected);
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
                    // clear search
                    SearchTerm = null;
                    base.OnAction(action);
                    break;
                default:
                    base.OnAction(action);
                    break;
            }
        }

        protected override void OnShowContextMenu()
        {
            base.OnShowContextMenu();
        }

        #endregion

        #region Private Methods
        
        private string GetSearchListName(SearchType type)
        {
            string listName = string.Empty;

            switch (type)
            {
                case SearchType.movie:
                    listName = Translation.Movies;
                    break;

                case SearchType.show:
                    listName = Translation.TVShows;
                    break;

                case SearchType.episode:
                    listName = Translation.Episodes;
                    break;

                case SearchType.person:
                    listName = Translation.People;
                    break;

                case SearchType.user:
                    listName = Translation.Users;
                    break;

                case SearchType.list:
                    listName = Translation.Lists;
                    break;
            }

            return listName;
        }

        private int GetSearchResultCount(IEnumerable<TraktSearchResult> searchResults, SearchType type)
        {
            if (searchResults == null) return 0;
            return searchResults.Where(s => s.Type == type.ToString()).Count();
        }

        private void SetSearchTypes()
        {
            SearchTypes.Clear();

            if (movieSearchButton.IsSelected()) SearchTypes.Add(SearchType.movie);
            if (showSearchButton.IsSelected()) SearchTypes.Add(SearchType.show);
            if (episodeSearchButton.IsSelected()) SearchTypes.Add(SearchType.episode);
            if (peopleSearchButton.IsSelected()) SearchTypes.Add(SearchType.person);
            if (userSearchButton.IsSelected()) SearchTypes.Add(SearchType.user);
            if (listSearchButton.IsSelected()) SearchTypes.Add(SearchType.list);
        }

        private int GetSearchTypesID()
        {
            SearchType searchTypes = SearchType.none;

            foreach (var type in SearchTypes)
            {
                searchTypes |= type;
            }

            return (int)searchTypes;
        }

        private void LoadSearchResults()
        {
            SetSearchTypes();

            GUIUtils.SetProperty("#Trakt.Items", string.Empty);

            GUIBackgroundTask.Instance.ExecuteInBackgroundAndCallback(() =>
            {
                // perform search for all types selected
                return SearchResults;
            },
            delegate(bool success, object result)
            {
                if (success)
                {
                    SendSearchResultsToFacade(result as IEnumerable<TraktSearchResult>);
                }
            }, Translation.GettingSearchResults, true);
        }

        private void SendSearchResultsToFacade(IEnumerable<TraktSearchResult> searchResults)
        {
            // clear facade
            GUIControl.ClearControl(GetID, Facade.GetID);

            if (searchResults == null || searchResults.Count() == 0)
            {
                GUIUtils.ShowNotifyDialog(GUIUtils.PluginName(), Translation.NoSearchResultsFound);
                return;
            }

            // jump directly to results
            if (!TraktSettings.ShowSearchResultsBreakdown && SearchTypes.Count == 1)
            {
                // set the selected search type as we have not clicked on a facade item
                SelectedSearchType = (SearchType)GetSearchTypesID();
                SendSearchResultsToWindow(SearchResults);

                // clear the search term so when we return (press back) we don't go in a loop.
                SearchTerm = null;
                return;
            }

            int itemId = 0;

            // Add each search type to the list
            foreach (var type in SearchTypes)
            {
                GUIListItem item = new GUIListItem(GetSearchListName(type));

                item.Label2 = GetSearchResultCount(searchResults, type).ToString();
                item.ItemId = Int32.MaxValue - itemId;
                item.TVTag = type;
                item.IconImage = "defaultFolder.png";
                item.IconImageBig = "defaultFolderBig.png";
                item.ThumbnailImage = "defaultFolderBig.png";
                item.OnItemSelected += OnItemSelected;
                Utils.SetDefaultIcons(item);
                Facade.Add(item);
                itemId++;
            }

            // Set Facade Layout
            Facade.SetCurrentLayout("List");
            GUIControl.FocusControl(GetID, Facade.GetID);

            if (PreviousSelectedIndex >= SearchTypes.Count)
                Facade.SelectIndex(PreviousSelectedIndex - 1);
            else
                Facade.SelectIndex(PreviousSelectedIndex);

            // set facade properties
            GUIUtils.SetProperty("#itemcount", SearchTypes.Count().ToString());
            GUIUtils.SetProperty("#Trakt.Items", string.Format("{0} {1}", SearchTypes.Count.ToString(), Translation.SearchTypes));
        }

        private void SendSearchResultsToWindow(IEnumerable<TraktSearchResult> searchResults)
        {
            switch (SelectedSearchType)
            {
                case SearchType.movie:
                    if (GetSearchResultCount(searchResults, SearchType.movie) == 0) break;
                    GUISearchMovies.SearchTerm = SearchTerm;
                    GUISearchMovies.Movies = SearchResults.Where(s => s.Type == SearchType.movie.ToString()).Select(m => m.Movie);
                    GUIWindowManager.ActivateWindow((int)TraktGUIWindows.SearchMovies);
                    break;

                case SearchType.show:
                    if (GetSearchResultCount(searchResults, SearchType.show) == 0) break;
                    GUISearchShows.SearchTerm = SearchTerm;
                    GUISearchShows.Shows = SearchResults.Where(s => s.Type == SearchType.show.ToString()).Select(m => m.Show);
                    GUIWindowManager.ActivateWindow((int)TraktGUIWindows.SearchShows);
                    break;

                case SearchType.episode:
                    if (GetSearchResultCount(searchResults, SearchType.episode) == 0) break;
                    GUISearchEpisodes.SearchTerm = SearchTerm;
                    GUISearchEpisodes.Episodes = SearchResults.Where(s => s.Type == SearchType.episode.ToString())
                                                              .Select(e => new TraktEpisodeSummaryEx
                                                                          {
                                                                              Episode = e.Episode,
                                                                              Show = e.Show
                                                                          });
                    GUIWindowManager.ActivateWindow((int)TraktGUIWindows.SearchEpisodes);
                    break;

                case SearchType.person:
                    if (GetSearchResultCount(searchResults, SearchType.person) == 0) break;
                    GUISearchPeople.SearchTerm = SearchTerm;
                    GUISearchPeople.People = SearchResults.Where(s => s.Type == SearchType.person.ToString()).Select(m => m.Person);
                    GUIWindowManager.ActivateWindow((int)TraktGUIWindows.SearchPeople);
                    break;

                case SearchType.user:
                    if (GetSearchResultCount(searchResults, SearchType.user) == 0) break;
                    GUISearchUsers.SearchTerm = SearchTerm;
                    GUISearchUsers.Users = SearchResults.Where(s => s.Type == SearchType.user.ToString()).Select(m => m.User);
                    GUIWindowManager.ActivateWindow((int)TraktGUIWindows.SearchUsers);
                    break;

                case SearchType.list:
                    if (GetSearchResultCount(searchResults, SearchType.list) == 0) break;
                    //TODO
                    break;
            }
        }

        private void InitProperties()
        {
            // load previous search types
            SearchTypes.Clear();
            SearchType searchTypes = (SearchType)TraktSettings.SearchTypes;

            // select all search types previously used
            if ((searchTypes & SearchType.movie) == SearchType.movie) movieSearchButton.Selected = true;
            if ((searchTypes & SearchType.show) == SearchType.show) showSearchButton.Selected = true;
            if ((searchTypes & SearchType.episode) == SearchType.episode) episodeSearchButton.Selected = true;
            if ((searchTypes & SearchType.person) == SearchType.person) peopleSearchButton.Selected = true;
            if ((searchTypes & SearchType.user) == SearchType.user) userSearchButton.Selected = true;

            GUIUtils.SetProperty("#Trakt.Search.SearchTerm", string.IsNullOrEmpty(SearchTerm) ? Translation.EnterSearchTerm : SearchTerm);

            // Load Search List if search term is populated
            if (!string.IsNullOrEmpty(SearchTerm) && searchTypes != SearchType.none)
            {
                LoadSearchResults();
            }
            else
            {
                // set focus on search button as there is nothing loaded
                GUIControl.FocusControl(GetID, searchButton.GetID);

                GUIUtils.SetProperty("#itemcount", "0");
                GUIUtils.SetProperty("#Trakt.Items", "0");
            }
        }

        private void ClearProperties()
        {
            GUIUtils.SetProperty("#Trakt.Search.SearchTerm", string.Empty);
        }
        
        private void OnItemSelected(GUIListItem item, GUIControl parent)
        {
            if (item == null) return;
            SelectedSearchType = (SearchType)item.TVTag;
        }

        #endregion
    }
}
