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
        string SearchTerm = string.Empty;
        DateTime LastRequest = new DateTime();
        Dictionary<string, TraktSearchResult> searchCache = new Dictionary<string, TraktSearchResult>();

        TraktSearchResult SearchResults
        {
            get
            {
                string key = SearchTerm + GetSearchTypesID();

                if (!searchCache.Keys.Contains(key) || LastRequest < DateTime.UtcNow.Subtract(new TimeSpan(0, TraktSettings.WebRequestCacheMinutes, 0)))
                {
                    _SearchResults = TraktAPI.TraktAPI.Search(SearchTerm, SearchTypes);
                    if (searchCache.Keys.Contains(key)) searchCache.Remove(key);
                    searchCache.Add(key, _SearchResults);
                    LastRequest = DateTime.UtcNow;
                    PreviousSelectedIndex = 0;
                }
                return searchCache[key];
            }
        }
        TraktSearchResult _SearchResults = null;
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
            if (movieSearchButton.Selected) searchTypes |= SearchType.movies;
            if (showSearchButton.Selected) searchTypes |= SearchType.shows;
            if (episodeSearchButton.Selected) searchTypes |= SearchType.episodes;
            if (peopleSearchButton.Selected) searchTypes |= SearchType.people;
            if (userSearchButton.Selected) searchTypes |= SearchType.users;

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
                        switch (SelectedSearchType)
                        {
                            case SearchType.movies:
                                if (SearchResults.Movies.Count() == 0) break;
                                GUISearchMovies.SearchTerm = SearchTerm;
                                GUISearchMovies.Movies = SearchResults.Movies;
                                GUIWindowManager.ActivateWindow((int)TraktGUIWindows.SearchMovies);
                                break;

                            case SearchType.shows:
                                if (SearchResults.Shows.Count() == 0) break;
                                GUISearchShows.SearchTerm = SearchTerm;
                                GUISearchShows.Shows = SearchResults.Shows;
                                GUIWindowManager.ActivateWindow((int)TraktGUIWindows.SearchShows);
                                break;

                            case SearchType.episodes:
                                if (SearchResults.Episodes.Count() == 0) break;
                                GUISearchEpisodes.SearchTerm = SearchTerm;
                                GUISearchEpisodes.Episodes = SearchResults.Episodes;
                                GUIWindowManager.ActivateWindow((int)TraktGUIWindows.SearchEpisodes);
                                break;

                            case SearchType.people:
                                if (SearchResults.People.Count() == 0) break;
                                GUISearchPeople.SearchTerm = SearchTerm;
                                GUISearchPeople.People = SearchResults.People;
                                GUIWindowManager.ActivateWindow((int)TraktGUIWindows.SearchPeople);
                                break;

                            case SearchType.users:
                                if (SearchResults.Users.Count() == 0) break;
                                GUISearchUsers.SearchTerm = SearchTerm;
                                GUISearchUsers.Users = SearchResults.Users;
                                GUIWindowManager.ActivateWindow((int)TraktGUIWindows.SearchUsers);
                                break;
                        }
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
                case SearchType.movies:
                    listName = Translation.Movies;
                    break;

                case SearchType.shows:
                    listName = Translation.TVShows;
                    break;

                case SearchType.episodes:
                    listName = Translation.Episodes;
                    break;

                case SearchType.people:
                    listName = Translation.People;
                    break;

                case SearchType.users:
                    listName = Translation.Users;
                    break;
            }

            return listName;
        }

        private int GetSearchResultCount(TraktSearchResult searchResults, SearchType type)
        {
            if (searchResults == null) return 0;

            int retValue = 0;

            switch (type)
            {
                case SearchType.movies:
                    if (searchResults.Movies != null) retValue = searchResults.Movies.Count();
                    break;

                case SearchType.shows:
                    if (searchResults.Shows != null) retValue = searchResults.Shows.Count();
                    break;

                case SearchType.episodes:
                    if (searchResults.Episodes != null) retValue = searchResults.Episodes.Count();
                    break;

                case SearchType.people:
                    if (searchResults.People != null) retValue = searchResults.People.Count();
                    break;

                case SearchType.users:
                    if (searchResults.Users != null) retValue = searchResults.Users.Count();
                    break;
            }

            return retValue;
        }

        private void SetSearchTypes()
        {
            SearchTypes.Clear();

            if (movieSearchButton.Selected) SearchTypes.Add(SearchType.movies);
            if (showSearchButton.Selected) SearchTypes.Add(SearchType.shows);
            if (episodeSearchButton.Selected) SearchTypes.Add(SearchType.episodes);
            if (peopleSearchButton.Selected) SearchTypes.Add(SearchType.people);
            if (userSearchButton.Selected) SearchTypes.Add(SearchType.users);
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
                    SendSearchResultsToFacade(result as TraktSearchResult);
                }
            }, Translation.GettingSearchResults, true);
        }

        private void SendSearchResultsToFacade(TraktSearchResult searchResults)
        {
            // clear facade
            GUIControl.ClearControl(GetID, Facade.GetID);

            if (searchResults == null || searchResults.Count == 0)
            {
                GUIUtils.ShowNotifyDialog(GUIUtils.PluginName(), Translation.NoSearchResultsFound);
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

        private void InitProperties()
        {
            // load previous search types
            SearchTypes.Clear();
            SearchType searchTypes = (SearchType)TraktSettings.SearchTypes;

            // select all search types previously used
            if ((searchTypes & SearchType.movies) == SearchType.movies) movieSearchButton.Selected = true;
            if ((searchTypes & SearchType.shows) == SearchType.shows) showSearchButton.Selected = true;
            if ((searchTypes & SearchType.episodes) == SearchType.episodes) episodeSearchButton.Selected = true;
            if ((searchTypes & SearchType.people) == SearchType.people) peopleSearchButton.Selected = true;
            if ((searchTypes & SearchType.users) == SearchType.users) userSearchButton.Selected = true;

            GUIUtils.SetProperty("#Trakt.Search.SearchTerm", SearchTerm == string.Empty ? Translation.EnterSearchTerm : SearchTerm);

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
