using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using MediaPortal.GUI.Library;
using MediaPortal.Util;
using TraktPlugin.TraktAPI.DataStructures;
using Action = MediaPortal.GUI.Library.Action;

namespace TraktPlugin.GUI
{
    public class GUICreditsShow : GUIWindow
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
            AddToList,
            ChangeLayout
        }

        public enum CreditType
        {
            Cast,
            Crew
        }

        #endregion

        #region Constructor

        public GUICreditsShow()
        {

        }

        #endregion

        #region Public Properties

        public static string Fanart { get; set; }
        public static TraktShowSummary Show { get; set; }
        public static CreditType Type { get; set; }

        #endregion

        #region Privates

        Layout CurrentLayout { get; set; }
        int PreviousSelectedIndex = 0;
        DateTime LastRequest = new DateTime();
        Dictionary<string, TraktCredits> showCredits = new Dictionary<string, TraktCredits>();

        TraktCredits ShowCredits
        {
            get
            {
                var slug = Show.Ids.Trakt.ToString();

                if (!showCredits.ContainsKey(slug) || LastRequest < DateTime.UtcNow.Subtract(new TimeSpan(0, TraktSettings.WebRequestCacheMinutes, 0)))
                {
                    var credits = TraktAPI.TraktAPI.GetShowPeople(slug);
                    if (credits == null) return null;

                    if (showCredits.ContainsKey(slug))
                        showCredits.Remove(slug);

                    showCredits.Add(slug, credits);

                    LastRequest = DateTime.UtcNow;
                }
                return showCredits[slug];
            }
        }

        #endregion

        #region Base Overrides

        public override int GetID
        {
            get
            {
                return (int)TraktGUIWindows.CreditsShow;
            }
        }

        public override bool Init()
        {
            return Load(GUIGraphicsContext.Skin + @"\Trakt.Credits.Show.xml");
        }

        protected override void OnPageLoad()
        {
            base.OnPageLoad();

            if (string.IsNullOrEmpty(_loadParameter) && Show == null)
            {
                GUIWindowManager.ActivateWindow(GUIWindowManager.GetPreviousActiveWindow());
                return;
            }

            // Clear GUI Properties
            ClearProperties();

            // Init Properties
            InitProperties();

            // Load Credits
            LoadCredits();
        }

        protected override void OnPageDestroy(int new_windowId)
        {
            GUIPersonListItem.StopDownload = true;
            ClearProperties();

            // save settings
            TraktSettings.CreditsShowDefaultLayout = (int)CurrentLayout;

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
                    var selectedItem = Facade.SelectedListItem as GUIPersonListItem;
                    if (selectedItem == null) return;

                    GUIWindowManager.ActivateWindow((int)TraktGUIWindows.PersonSummary, selectedItem.Person.Ids.Trakt.ToString());
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
                    _loadParameter = null;
                    PreviousSelectedIndex = 0;
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

            var selectedItem = this.Facade.SelectedListItem as GUIPersonListItem;
            if (selectedItem == null) return;

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

        private TraktShowSummary GetShowSummary(string id)
        {
            var showSummary = TraktAPI.TraktAPI.GetShowSummary(id);
            if (showSummary != null)
            {
                // Publish Show Properties
                GUICommon.SetShowProperties(showSummary);
            }
            return showSummary;
        }

        private void LoadCredits()
        {
            GUIUtils.SetProperty("#Trakt.Items", string.Empty);

            GUIBackgroundTask.Instance.ExecuteInBackgroundAndCallback(() =>
            {
                // get the show summary if we need it
                if (Show == null)
                {
                    Show = GetShowSummary(_loadParameter);
                }

                return ShowCredits;
            },
            delegate(bool success, object result)
            {
                if (success)
                {
                    var people = result as TraktCredits;
                    SendCreditResultsToFacade(people);
                }
            }, Translation.GettingSearchResults, true);
        }

        private void SendCreditResultsToFacade(TraktCredits credits)
        {
            // clear facade
            GUIControl.ClearControl(GetID, Facade.GetID);

            if (credits == null)
            {
                GUIUtils.ShowNotifyDialog(Translation.Error, Translation.ErrorGeneral);
                GUIWindowManager.ShowPreviousWindow();
                Show = null;
                return;
            }

            int itemId = 0;
            var personImages = new List<GUITraktImage>();

            // Add each character
            if (Type == CreditType.Cast && credits.Cast != null)
            {
                foreach (var person in credits.Cast)
                {
                    // add image for download
                    var images = new GUITraktImage { PeopleImages = person.Person.Images };
                    personImages.Add(images);

                    var item = new GUIPersonListItem(string.IsNullOrEmpty(person.Character) ? person.Person.Name : string.Format(Translation.ActorAndRole, person.Person.Name, person.Character), (int)TraktGUIWindows.CreditsShow);

                    item.Person = person.Person;
                    item.CreditType = Credit.Cast;
                    item.Character = person;
                    item.Images = images;
                    item.ItemId = Int32.MaxValue - itemId;
                    item.IconImage = GUIImageHandler.GetDefaultPoster(false);
                    item.IconImageBig = GUIImageHandler.GetDefaultPoster();
                    item.ThumbnailImage = GUIImageHandler.GetDefaultPoster();
                    item.OnItemSelected += OnCastSelected;
                    Utils.SetDefaultIcons(item);
                    Facade.Add(item);
                    itemId++;
                }
            }

            // add each crew member
            if (Type == CreditType.Crew && credits.Crew != null)
            {
                if (credits.Crew.Directing != null)
                {
                    foreach (var person in credits.Crew.Directing)
                    {
                        // add image for download
                        var images = new GUITraktImage { PeopleImages = person.Person.Images };
                        personImages.Add(images);

                        var item = new GUIPersonListItem(person.Person.Name, (int)TraktGUIWindows.CreditsShow);
                        item.Label2 = GUICommon.GetTranslatedCreditJob(person.Job);
                        item.Person = person.Person;
                        item.CreditType = Credit.Directing;
                        item.Job = person;
                        item.Images = images;
                        item.ItemId = Int32.MaxValue - itemId;
                        item.IconImage = GUIImageHandler.GetDefaultPoster(false);
                        item.IconImageBig = GUIImageHandler.GetDefaultPoster();
                        item.ThumbnailImage = GUIImageHandler.GetDefaultPoster();
                        item.OnItemSelected += OnCrewSelected;
                        Utils.SetDefaultIcons(item);
                        Facade.Add(item);
                        itemId++;
                    }
                }

                if (credits.Crew.Writing != null)
                {
                    foreach (var person in credits.Crew.Writing)
                    {
                        // add image for download
                        var images = new GUITraktImage { PeopleImages = person.Person.Images };
                        personImages.Add(images);

                        var item = new GUIPersonListItem(person.Person.Name, (int)TraktGUIWindows.CreditsShow);
                        item.Label2 = GUICommon.GetTranslatedCreditJob(person.Job);
                        item.Person = person.Person;
                        item.CreditType = Credit.Writing;
                        item.Job = person;
                        item.Images = images;
                        item.ItemId = Int32.MaxValue - itemId;
                        item.IconImage = GUIImageHandler.GetDefaultPoster(false);
                        item.IconImageBig = GUIImageHandler.GetDefaultPoster();
                        item.ThumbnailImage = GUIImageHandler.GetDefaultPoster();
                        item.OnItemSelected += OnCrewSelected;
                        Utils.SetDefaultIcons(item);
                        Facade.Add(item);
                        itemId++;
                    }
                }

                if (credits.Crew.Production != null)
                {
                    foreach (var person in credits.Crew.Production)
                    {
                        // add image for download
                        var images = new GUITraktImage { PeopleImages = person.Person.Images };
                        personImages.Add(images);

                        var item = new GUIPersonListItem(person.Person.Name, (int)TraktGUIWindows.CreditsShow);
                        item.Label2 = GUICommon.GetTranslatedCreditJob(person.Job);
                        item.Person = person.Person;
                        item.CreditType = Credit.Production;
                        item.Job = person;
                        item.Images = images;
                        item.ItemId = Int32.MaxValue - itemId;
                        item.IconImage = GUIImageHandler.GetDefaultPoster(false);
                        item.IconImageBig = GUIImageHandler.GetDefaultPoster();
                        item.ThumbnailImage = GUIImageHandler.GetDefaultPoster();
                        item.OnItemSelected += OnCrewSelected;
                        Utils.SetDefaultIcons(item);
                        Facade.Add(item);
                        itemId++;
                    }
                }

                if (credits.Crew.Art != null)
                {
                    foreach (var person in credits.Crew.Art)
                    {
                        // add image for download
                        var images = new GUITraktImage { PeopleImages = person.Person.Images };
                        personImages.Add(images);

                        var item = new GUIPersonListItem(person.Person.Name, (int)TraktGUIWindows.CreditsShow);
                        item.Label2 = GUICommon.GetTranslatedCreditJob(person.Job);
                        item.Person = person.Person;
                        item.CreditType = Credit.Art;
                        item.Job = person;
                        item.Images = images;
                        item.ItemId = Int32.MaxValue - itemId;
                        item.IconImage = GUIImageHandler.GetDefaultPoster(false);
                        item.IconImageBig = GUIImageHandler.GetDefaultPoster();
                        item.ThumbnailImage = GUIImageHandler.GetDefaultPoster();
                        item.OnItemSelected += OnCrewSelected;
                        Utils.SetDefaultIcons(item);
                        Facade.Add(item);
                        itemId++;
                    }
                }

                if (credits.Crew.Camera != null)
                {
                    foreach (var person in credits.Crew.Camera)
                    {
                        // add image for download
                        var images = new GUITraktImage { PeopleImages = person.Person.Images };
                        personImages.Add(images);

                        var item = new GUIPersonListItem(person.Person.Name, (int)TraktGUIWindows.CreditsShow);
                        item.Label2 = GUICommon.GetTranslatedCreditJob(person.Job);
                        item.Person = person.Person;
                        item.CreditType = Credit.Camera;
                        item.Job = person;
                        item.Images = images;
                        item.ItemId = Int32.MaxValue - itemId;
                        item.IconImage = GUIImageHandler.GetDefaultPoster(false);
                        item.IconImageBig = GUIImageHandler.GetDefaultPoster();
                        item.ThumbnailImage = GUIImageHandler.GetDefaultPoster();
                        item.OnItemSelected += OnCrewSelected;
                        Utils.SetDefaultIcons(item);
                        Facade.Add(item);
                        itemId++;
                    }
                }

                if (credits.Crew.CostumeAndMakeUp != null)
                {
                    foreach (var person in credits.Crew.CostumeAndMakeUp)
                    {
                        // add image for download
                        var images = new GUITraktImage { PeopleImages = person.Person.Images };
                        personImages.Add(images);

                        var item = new GUIPersonListItem(person.Person.Name, (int)TraktGUIWindows.CreditsShow);
                        item.Label2 = GUICommon.GetTranslatedCreditJob(person.Job);
                        item.Person = person.Person;
                        item.CreditType = Credit.CostumeAndMakeUp;
                        item.Job = person;
                        item.Images = images;
                        item.ItemId = Int32.MaxValue - itemId;
                        item.IconImage = GUIImageHandler.GetDefaultPoster(false);
                        item.IconImageBig = GUIImageHandler.GetDefaultPoster();
                        item.ThumbnailImage = GUIImageHandler.GetDefaultPoster();
                        item.OnItemSelected += OnCrewSelected;
                        Utils.SetDefaultIcons(item);
                        Facade.Add(item);
                        itemId++;
                    }
                }

                if (credits.Crew.Sound != null)
                {
                    foreach (var person in credits.Crew.Sound)
                    {
                        // add image for download
                        var images = new GUITraktImage { PeopleImages = person.Person.Images };
                        personImages.Add(images);

                        var item = new GUIPersonListItem(person.Person.Name, (int)TraktGUIWindows.CreditsShow);
                        item.Label2 = GUICommon.GetTranslatedCreditJob(person.Job);
                        item.Person = person.Person;
                        item.CreditType = Credit.Sound;
                        item.Job = person;
                        item.Images = images;
                        item.ItemId = Int32.MaxValue - itemId;
                        item.IconImage = GUIImageHandler.GetDefaultPoster(false);
                        item.IconImageBig = GUIImageHandler.GetDefaultPoster();
                        item.ThumbnailImage = GUIImageHandler.GetDefaultPoster();
                        item.OnItemSelected += OnCrewSelected;
                        Utils.SetDefaultIcons(item);
                        Facade.Add(item);
                        itemId++;
                    }
                }
            }

            // if nothing to display exit
            if (Facade.Count == 0)
            {
                GUIUtils.ShowNotifyDialog(Type == CreditType.Crew ? Translation.Crew : Translation.Cast, Translation.NoCreditsFound);
                GUIWindowManager.ShowPreviousWindow();
                Show = null;
                return;
            }

            // Set Facade Layout
            Facade.SetCurrentLayout(Enum.GetName(typeof(Layout), CurrentLayout));
            GUIControl.FocusControl(GetID, Facade.GetID);

            Facade.SelectIndex(PreviousSelectedIndex);

            // set facade properties
            GUIUtils.SetProperty("#itemcount", Facade.Count.ToString());
            GUIUtils.SetProperty("#Trakt.Items", string.Format("{0} {1}", Facade.Count.ToString(), Facade.Count > 1 ? Translation.People : Translation.Person));

            // Download images Async and set to facade
            GUIPersonListItem.GetImages(personImages, false);
        }

        private void InitProperties()
        {
            // only set property if file exists
            // if we set now and download later, image will not set to skin
            if (File.Exists(Fanart))
                GUIUtils.SetProperty("#Trakt.Show.FanartImageFilename", Fanart);
            else
                DownloadFanart();

            // set credit type property
            GUIUtils.SetProperty("#Trakt.Credit.Type", Type == CreditType.Cast ? Translation.Cast : Translation.Crew);

            // Set current Show properties
            if (Show != null)
                GUICommon.SetShowProperties(Show);

            // load last layout
            CurrentLayout = (Layout)TraktSettings.CreditsShowDefaultLayout;

            // update button label
            if (layoutButton != null)
                GUIControl.SetControlLabel(GetID, layoutButton.GetID, GUICommon.GetLayoutTranslation(CurrentLayout));
        }

        private void ClearProperties()
        {
            GUIUtils.SetProperty("#Trakt.Person.CreditType", string.Empty);
            GUIUtils.SetProperty("#Trakt.Person.CreditValue", string.Empty);

            GUICommon.ClearPersonProperties();
        }

        private void OnCastSelected(GUIListItem item, GUIControl parent)
        {
            PreviousSelectedIndex = Facade.SelectedListItemIndex;

            var selectedItem = item as GUIPersonListItem;
            if (item == null)
                return;

            GUIUtils.SetProperty("#Trakt.Person.CreditType", Translation.Character);
            GUIUtils.SetProperty("#Trakt.Person.CreditValue", selectedItem.Character.Character);
            GUICommon.SetPersonProperties(selectedItem.Person);
        }

        private void OnCrewSelected(GUIListItem item, GUIControl parent)
        {
            PreviousSelectedIndex = Facade.SelectedListItemIndex;

            var selectedItem = item as GUIPersonListItem;
            if (item == null)
                return;

            GUIUtils.SetProperty("#Trakt.Person.CreditType", Translation.Job);
            GUIUtils.SetProperty("#Trakt.Person.CreditValue", GUICommon.GetTranslatedCreditJob(selectedItem.Job.Job));
            GUICommon.SetPersonProperties(selectedItem.Person);
        }

        private void DownloadFanart()
        {
            if (Show == null || Show.Images == null || Show.Images.Fanart == null)
                return;

            var getFanartthread = new Thread((o) =>
            {
                var show = o as TraktShowSummary;
                string localFile = show.Images.Fanart.LocalImageFilename(ArtworkType.ShowFanart);
                string remoteFile = TraktSettings.DownloadFullSizeFanart ? show.Images.Fanart.FullSize : show.Images.Fanart.MediumSize;

                if (localFile == null || remoteFile == null)
                    return;

                GUIImageHandler.DownloadImage(remoteFile, localFile);
                GUIUtils.SetProperty("#Trakt.Show.FanartImageFilename", localFile);

            }) { Name = "ImageDownload", IsBackground = true };

            getFanartthread.Start(Show);
        }

        #endregion
    }
}