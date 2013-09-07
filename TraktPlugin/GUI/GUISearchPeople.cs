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

        bool StopDownload { get; set; }
        bool SearchTermChanged { get; set; }
        string PreviousSearchTerm { get; set; }
        Layout CurrentLayout { get; set; }
        int PreviousSelectedIndex = 0;

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
            StopDownload = true;
            ClearProperties();

            _loadParameter = null;

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

            GUIListItem selectedItem = this.Facade.SelectedListItem;
            if (selectedItem == null) return;

            var selectedPerson = selectedItem.TVTag as TraktPersonSummary;
            if (selectedPerson == null) return;

            IDialogbox dlg = (IDialogbox)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
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
                    People = TraktAPI.TraktAPI.SearchPeople(SearchTerm);
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
                return;
            }

            int itemId = 0;
            var personImages = new List<TraktPerson.PersonImages>();

            // Add each movie
            foreach (var person in people)
            {
                var item = new GUITraktSearchPeopleListItem(person.Name);
                
                item.TVTag = person;
                item.Item = person.Images;
                item.ItemId = Int32.MaxValue - itemId;
                item.IconImage = GUIImageHandler.GetDefaultPoster(false);
                item.IconImageBig = GUIImageHandler.GetDefaultPoster();
                item.ThumbnailImage = GUIImageHandler.GetDefaultPoster();
                item.OnItemSelected += OnPersonSelected;
                Utils.SetDefaultIcons(item);
                Facade.Add(item);
                itemId++;

                // add image for download
                personImages.Add(person.Images);
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
            GetImages(personImages);
        }

        private void InitProperties()
        {
            // set search term from loading parameter
            if (!string.IsNullOrEmpty(_loadParameter))
            {
                TraktLogger.Info("Person Search Loading Parameter: {0}", _loadParameter);
                SearchTerm = _loadParameter;
            }

            // remember previous search term
            SearchTermChanged = false;
            if (PreviousSearchTerm != SearchTerm) SearchTermChanged = true;
            PreviousSearchTerm = SearchTerm;

            // set context property
            GUIUtils.SetProperty("#Trakt.Search.SearchTerm", SearchTerm);

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

        private void GetImages(List<TraktPerson.PersonImages> itemsWithThumbs)
        {
            StopDownload = false;

            // split the downloads in 5+ groups and do multithreaded downloading
            int groupSize = (int)Math.Max(1, Math.Floor((double)itemsWithThumbs.Count / 5));
            int groups = (int)Math.Ceiling((double)itemsWithThumbs.Count() / groupSize);

            for (int i = 0; i < groups; i++)
            {
                var groupList = new List<TraktPerson.PersonImages>();
                for (int j = groupSize * i; j < groupSize * i + (groupSize * (i + 1) > itemsWithThumbs.Count ? itemsWithThumbs.Count - groupSize * i : groupSize); j++)
                {
                    groupList.Add(itemsWithThumbs[j]);
                }

                new Thread(delegate(object o)
                {
                    var items = (List<TraktPerson.PersonImages>)o;
                    foreach (var item in items)
                    {
                        #region Headshot
                        // stop download if we have exited window
                        if (StopDownload) break;

                        string remoteThumb = item.Headshot;
                        string localThumb = item.HeadshotImageFilename;

                        if (!string.IsNullOrEmpty(remoteThumb) && !string.IsNullOrEmpty(localThumb))
                        {
                            if (GUIImageHandler.DownloadImage(remoteThumb, localThumb))
                            {
                                // notify that image has been downloaded
                                item.NotifyPropertyChanged("HeadshotImageFilename");
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

    public class GUITraktSearchPeopleListItem : GUIListItem
    {
        public GUITraktSearchPeopleListItem(string strLabel) : base(strLabel) { }

        public object Item
        {
            get { return _Item; }
            set
            {
                _Item = value;
                INotifyPropertyChanged notifier = value as INotifyPropertyChanged;
                if (notifier != null) notifier.PropertyChanged += (s, e) =>
                {
                    if (s is TraktPerson.PersonImages && e.PropertyName == "HeadshotImageFilename")
                        SetImageToGui((s as TraktPerson.PersonImages).HeadshotImageFilename);
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

            ThumbnailImage = imageFilePath;
            IconImage = imageFilePath;
            IconImageBig = imageFilePath;

            // if selected and is current window force an update of thumbnail
            this.UpdateItemIfSelected((int)TraktGUIWindows.SearchPeople, ItemId);
        }
    }
}