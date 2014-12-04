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
using TraktPlugin.TraktAPI;
using TraktPlugin.TraktAPI.DataStructures;
using TraktPlugin.TraktAPI.Extensions;
using Action = MediaPortal.GUI.Library.Action;

namespace TraktPlugin.GUI
{
    public class PersonSearch
    {
        public List<string> People { get; set; }
        public string Title { get; set; }
        public string Fanart { get; set; }
    }

    public class GUISearchPeople : GUIWindow
    {
        #region Skin Controls

        [SkinControl(2)]
        protected GUIButtonControl layoutButton = null;

        [SkinControl(50)]
        protected GUIFacadeControl Facade = null;
        
        #endregion

        #region Enums

        enum ContextMenuItem
        {
            ChangeLayout
        }

        #endregion

        #region Constructor

        public GUISearchPeople()
        {

        }

        #endregion

        #region Public Variables

        public static string SearchTerm { get; set; }
        public static IEnumerable<TraktPersonSummary> People { get; set; }

        #endregion

        #region Private Variables

        bool SearchTermChanged { get; set; }
        bool IsMultiPersonSearch { get; set; }
        string PreviousSearchTerm { get; set; }
        Layout CurrentLayout { get; set; }
        int PreviousSelectedIndex = 0;
        private readonly Object sync = new Object();

        #endregion

        #region Base Overrides

        public override int GetID
        {
            get
            {
                return (int)TraktGUIWindows.SearchPeople;
            }
        }

        public override bool Init()
        {
            return Load(GUIGraphicsContext.Skin + @"\Trakt.Search.People.xml");
        }

        protected override void OnPageLoad()
        {
            base.OnPageLoad();

            if (string.IsNullOrEmpty(_loadParameter) && People == null)
            {
                GUIWindowManager.ActivateWindow(GUIWindowManager.GetPreviousActiveWindow());
                return;
            }

            // Clear GUI Properties
            ClearProperties();

            // Init Properties
            InitProperties();

            // Load Search Results
            LoadSearchResults();
        }

        protected override void OnPageDestroy(int new_windowId)
        {
            GUIPersonListItem.StopDownload = true;
            ClearProperties();

            _loadParameter = null;
            IsMultiPersonSearch = false;
            GUIUtils.SetProperty("#Trakt.People.Fanart", string.Empty);

            // save settings
            TraktSettings.SearchPeopleDefaultLayout = (int)CurrentLayout;

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
                    base.OnAction(action);
                    break;

                case Action.ActionType.ACTION_PREVIOUS_MENU:
                    // clear search criteria if going back
                    SearchTerm = string.Empty;
                    People = null;
                    base.OnAction(action);
                    break;

                default:
                    base.OnAction(action);
                    break;
            }
        }

        protected override void OnShowContextMenu()
        {
            if (GUIBackgroundTask.Instance.IsBusy) return;

            var selectedItem = this.Facade.SelectedListItem;
            if (selectedItem == null) return;

            var selectedPerson = selectedItem.TVTag as TraktPersonSummary;
            if (selectedPerson == null) return;

            var dlg = (IDialogbox)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
            if (dlg == null) return;

            dlg.Reset();
            dlg.SetHeading(GUIUtils.PluginName());

            GUIListItem listItem = null;

            // Change Layout
            listItem = new GUIListItem(Translation.ChangeLayout);
            dlg.Add(listItem);
            listItem.ItemId = (int)ContextMenuItem.ChangeLayout;

            // Show Context Menu
            dlg.DoModal(GUIWindowManager.ActiveWindow);
            if (dlg.SelectedId < 0) return;

            switch (dlg.SelectedId)
            {
                case ((int)ContextMenuItem.ChangeLayout):
                    CurrentLayout = GUICommon.ShowLayoutMenu(CurrentLayout, PreviousSelectedIndex);
                    break;
            }

            base.OnShowContextMenu();
        }

        #endregion

        #region Private Methods

        private void LoadSearchResults()
        {
            GUIUtils.SetProperty("#Trakt.Items", string.Empty);

            GUIBackgroundTask.Instance.ExecuteInBackgroundAndCallback(() =>
            {
                // People can be null if invoking search from loading parameters
                // Internally we set the People to load
                if (People == null && !string.IsNullOrEmpty(SearchTerm))
                {
                    // search online
                    if (!IsMultiPersonSearch)
                    {
                        var searchResults = TraktAPI.TraktAPI.SearchPeople(SearchTerm);
                        if (searchResults != null)
                        {
                            People = searchResults.Select(s => s.Person);
                        }
                    }
                    else
                    {
                        // do a search for all people in parallel
                        var threads = new List<Thread>();

                        foreach (string person in SearchTerm.FromJSON<PersonSearch>().People)
                        {
                            var tPersonSearch = new Thread((obj) =>
                            {
                                var searchResults = TraktAPI.TraktAPI.SearchPeople(obj as string, 1);
                                lock (sync)
                                {
                                    if (searchResults != null)
                                    {
                                        if (People == null)
                                            People = searchResults.Select(s => s.Person);
                                        else
                                            People = People.Union(searchResults.Select(s => s.Person));
                                    }
                                }
                            });

                            tPersonSearch.Start(person);
                            tPersonSearch.Name = "Search";
                            threads.Add(tPersonSearch);
                        }

                        // wait until all search results are back
                        threads.ForEach(t => t.Join());
                    }
                }
                return People;
            },
            delegate(bool success, object result)
            {
                if (success)
                {
                    var people = result as IEnumerable<TraktPersonSummary>;
                    SendSearchResultsToFacade(people);
                }
            }, Translation.GettingSearchResults, true);
        }

        private void SendSearchResultsToFacade(IEnumerable<TraktPersonSummary> people)
        {
            // clear facade
            GUIControl.ClearControl(GetID, Facade.GetID);

            if (people == null || people.Count() == 0)
            {
                GUIUtils.ShowNotifyDialog(GUIUtils.PluginName(), Translation.NoSearchResultsFound);
                GUIWindowManager.ShowPreviousWindow();
                People = null;
                return;
            }

            int itemId = 0;
            var personImages = new List<GUITraktImage>();

            // Add each movie
            foreach (var person in people)
            {
                // add image for download
                var images = new GUITraktImage { PoepleImages = person.Images };
                personImages.Add(images);

                var item = new GUIPersonListItem(person.Name.Trim(), (int)TraktGUIWindows.SearchPeople);
                
                item.TVTag = person;
                item.Images = images;
                item.ItemId = Int32.MaxValue - itemId;
                item.IconImage = GUIImageHandler.GetDefaultPoster(false);
                item.IconImageBig = GUIImageHandler.GetDefaultPoster();
                item.ThumbnailImage = GUIImageHandler.GetDefaultPoster();
                item.OnItemSelected += OnPersonSelected;
                Utils.SetDefaultIcons(item);
                Facade.Add(item);
                itemId++;
            }

            // Set Facade Layout
            Facade.SetCurrentLayout(Enum.GetName(typeof(Layout), CurrentLayout));
            GUIControl.FocusControl(GetID, Facade.GetID);

            if (SearchTermChanged) PreviousSelectedIndex = 0;
            Facade.SelectIndex(PreviousSelectedIndex);

            // set facade properties
            GUIUtils.SetProperty("#itemcount", people.Count().ToString());
            GUIUtils.SetProperty("#Trakt.Items", string.Format("{0} {1}", people.Count().ToString(), people.Count() > 1 ? Translation.People : Translation.Person));

            // Download images Async and set to facade
            GUIPersonListItem.GetImages(personImages);
        }

        private void InitProperties()
        {
            // set search term from loading parameter
            if (!string.IsNullOrEmpty(_loadParameter))
            {
                TraktLogger.Debug("Person Search Loading Parameter: {0}", _loadParameter);
                SearchTerm = _loadParameter;

                // check if the searchterm is a list of people
                if (SearchTerm.StartsWith("{") && SearchTerm.EndsWith("}"))
                {
                    IsMultiPersonSearch = true;
                    // multi-person search will most likely have fanart as its attached
                    // to a movie, show or episode.
                    string fanart = SearchTerm.FromJSON<PersonSearch>().Fanart;
                    if (File.Exists(fanart))
                        GUIUtils.SetProperty("#Trakt.People.Fanart", fanart);
                }
            }

            // remember previous search term
            SearchTermChanged = false;
            if (PreviousSearchTerm != SearchTerm) SearchTermChanged = true;
            PreviousSearchTerm = SearchTerm;

            // set context property
            if (!IsMultiPersonSearch)
                GUIUtils.SetProperty("#Trakt.Search.SearchTerm", SearchTerm);
            else
                GUIUtils.SetProperty("#Trakt.Search.SearchTerm", SearchTerm.FromJSON<PersonSearch>().Title);

            // load last layout
            CurrentLayout = (Layout)TraktSettings.SearchPeopleDefaultLayout;

            // update button label
            if (layoutButton != null)
                GUIControl.SetControlLabel(GetID, layoutButton.GetID, GUICommon.GetLayoutTranslation(CurrentLayout));
        }

        private void ClearProperties()
        {
            GUIUtils.SetProperty("#Trakt.Search.SearchTerm", string.Empty);
            GUICommon.ClearPersonProperties();
        }

        private void PublishSkinProperties(TraktPersonSummary person)
        {
            GUICommon.SetPersonProperties(person);
        }

        private void OnPersonSelected(GUIListItem item, GUIControl parent)
        {
            PreviousSelectedIndex = Facade.SelectedListItemIndex;

            var person = item.TVTag as TraktPersonSummary;
            PublishSkinProperties(person);
        }
        #endregion
    }
}