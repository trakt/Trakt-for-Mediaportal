using System;
using System.Collections.Generic;
using System.Linq;
using MediaPortal.GUI.Library;
using MediaPortal.Util;
using TraktAPI.DataStructures;
using TraktAPI.Extensions;
using TraktPlugin.TmdbAPI.DataStructures;
using TraktPlugin.Cache;
using Action = MediaPortal.GUI.Library.Action;

namespace TraktPlugin.GUI
{
    public class GUIPersonShowCredits : GUIWindow
    {
        #region Skin Controls

        [SkinControl(2)]
        protected GUIButtonControl layoutButton = null;

        [SkinControl(8)]
        protected GUISortButtonControl sortButton = null;

        [SkinControl(9)]
        protected GUICheckButton filterWatchedButton = null;

        [SkinControl(10)]
        protected GUICheckButton filterWatchListedButton = null;

        [SkinControl(11)]
        protected GUICheckButton filterCollectedButton = null;

        [SkinControl(12)]
        protected GUICheckButton filterRatedButton = null;

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

        public enum View
        {
            Movies,
            Shows
        }

        #endregion

        #region Constructor

        public GUIPersonShowCredits()
        {
            backdrop = new ImageSwapper();
            backdrop.PropertyOne = "#Trakt.PersonCreditShows.Fanart.1";
            backdrop.PropertyTwo = "#Trakt.PersonCreditShows.Fanart.2";
        }

        #endregion

        #region Private Variables

        int PreviousSelectedIndex = 0;
        private GUIFacadeControl.Layout CurrentLayout { get; set; }
        private ImageSwapper backdrop;

        #endregion

        #region Base Overrides

        public override int GetID
        {
            get
            {
                return (int)TraktGUIWindows.PersonCreditShows;
            }
        }

        public override bool Init()
        {
            return Load(GUIGraphicsContext.Skin + @"\Trakt.Person.Credits.Shows.xml");
        }

        protected override void OnPageLoad()
        {
            base.OnPageLoad();

            if (CurrentPerson == null || CurrentCredits == null)
            {
                TraktLogger.Info("Exiting Person Show Credits as there is no current person or credits set");
                GUIWindowManager.ActivateWindow(GUIWindowManager.GetPreviousActiveWindow());
                return;
            }

            // Init Properties
            InitProperties();

            // Load Credits
            LoadCredits();
        }

        protected override void OnPageDestroy(int new_windowId)
        {
            GUIShowListItem.StopDownload = true;
            ClearProperties();

            // save current layout
            TraktSettings.PersonShowCreditsDefaultLayout = (int)CurrentLayout;

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
                    var item = Facade.SelectedListItem as GUIShowListItem;
                    if (item == null) return;

                    if (TraktSettings.EnableJumpToForTVShows)
                    {
                        CheckAndPlayEpisode(true);
                    }
                    else
                    {
                        if (item.Show == null) return;
                        GUIWindowManager.ActivateWindow((int)TraktGUIWindows.ShowSeasons, item.Show.ToJSON());
                    }
                    break;

                // Layout Button
                case (2):
                    CurrentLayout = GUICommon.ShowLayoutMenu(CurrentLayout, PreviousSelectedIndex);
                    break;

                // Sort Button
                case (8):
                    var newSortBy = GUICommon.ShowSortMenu(TraktSettings.SortByCreditShows);
                    if (newSortBy != null)
                    {
                        if (newSortBy.Field != TraktSettings.SortByCreditShows.Field)
                        {
                            TraktSettings.SortByCreditShows = newSortBy;
                            PreviousSelectedIndex = 0;
                            UpdateButtonState();
                            LoadCredits();
                        }
                    }
                    break;

                // Hide Watched
                case (9):
                    PreviousSelectedIndex = 0;
                    TraktSettings.CreditShowsHideWatched = !TraktSettings.CreditShowsHideWatched;
                    UpdateButtonState();
                    LoadCredits();
                    break;

                // Hide Watchlisted
                case (10):
                    PreviousSelectedIndex = 0;
                    TraktSettings.CreditShowsHideWatchlisted = !TraktSettings.CreditShowsHideWatchlisted;
                    UpdateButtonState();
                    LoadCredits();
                    break;

                // Hide Collected
                case (11):
                    PreviousSelectedIndex = 0;
                    TraktSettings.CreditShowsHideCollected = !TraktSettings.CreditShowsHideCollected;
                    UpdateButtonState();
                    LoadCredits();
                    break;

                // Hide Rated
                case (12):
                    PreviousSelectedIndex = 0;
                    TraktSettings.CreditShowsHideRated = !TraktSettings.CreditShowsHideRated;
                    UpdateButtonState();
                    LoadCredits();
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
                    CurrentPerson = null;
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
            var selectedItem = this.Facade.SelectedListItem as GUIShowListItem;
            if (selectedItem == null) return;

            var selectedShow = selectedItem.Show;

            var dlg = (IDialogbox)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
            if (dlg == null) return;

            dlg.Reset();
            dlg.SetHeading(GUIUtils.PluginName());

            GUICommon.CreateShowsContextMenu(ref dlg, selectedShow, false);

            // Show Context Menu
            dlg.DoModal(GUIWindowManager.ActiveWindow);
            if (dlg.SelectedId < 0) return;

            switch (dlg.SelectedId)
            {
                case ((int)MediaContextMenuItem.AddToWatchList):
                    TraktHelper.AddShowToWatchList(selectedShow);
                    OnItemSelected(selectedItem, Facade);
                    (Facade.SelectedListItem as GUIShowListItem).Images.NotifyPropertyChanged("Poster");
                    if (TraktSettings.CreditShowsHideWatchlisted) LoadCredits();
                    break;

                case ((int)MediaContextMenuItem.ShowSeasonInfo):
                    GUIWindowManager.ActivateWindow((int)TraktGUIWindows.ShowSeasons, selectedShow.ToJSON());
                    break;

                case ((int)MediaContextMenuItem.MarkAsWatched):
                    GUICommon.MarkShowAsWatched(selectedShow);
                    if (TraktSettings.CreditShowsHideWatched) LoadCredits();
                    break;

                case ((int)MediaContextMenuItem.AddToLibrary):
                    GUICommon.AddShowToCollection(selectedShow);
                    if (TraktSettings.CreditShowsHideCollected) LoadCredits();
                    break;

                case ((int)MediaContextMenuItem.RemoveFromWatchList):
                    TraktHelper.RemoveShowFromWatchList(selectedShow);
                    OnItemSelected(selectedItem, Facade);
                    (Facade.SelectedListItem as GUIShowListItem).Images.NotifyPropertyChanged("Poster");
                    break;

                case ((int)MediaContextMenuItem.AddToList):
                    TraktHelper.AddRemoveShowInUserList(selectedShow, false);
                    break;

                case ((int)MediaContextMenuItem.Filters):
                    if (ShowTVShowFiltersMenu())
                    {
                        PreviousSelectedIndex = 0;
                        UpdateButtonState();
                        LoadCredits();
                    }
                    break;

                case ((int)MediaContextMenuItem.Related):
                    TraktHelper.ShowRelatedShows(selectedShow);
                    break;

                case ((int)MediaContextMenuItem.Cast):
                    GUICreditsShow.Show = selectedShow;
                    GUICreditsShow.Type = GUICreditsShow.CreditType.Cast;
                    GUICreditsShow.Fanart = TmdbCache.GetShowBackdropFilename(selectedItem.Images.ShowImages);
                    GUIWindowManager.ActivateWindow((int)TraktGUIWindows.CreditsShow);
                    break;

                case ((int)MediaContextMenuItem.Crew):
                    GUICreditsShow.Show = selectedShow;
                    GUICreditsShow.Type = GUICreditsShow.CreditType.Crew;
                    GUICreditsShow.Fanart = TmdbCache.GetShowBackdropFilename(selectedItem.Images.ShowImages);
                    GUIWindowManager.ActivateWindow((int)TraktGUIWindows.CreditsShow);
                    break;

                case ((int)MediaContextMenuItem.Trailers):
                    GUICommon.ShowTVShowTrailersMenu(selectedShow);
                    break;

                case ((int)MediaContextMenuItem.Shouts):
                    TraktHelper.ShowTVShowShouts(selectedShow);
                    break;

                case ((int)MediaContextMenuItem.Rate):
                    GUICommon.RateShow(selectedShow);
                    OnItemSelected(selectedItem, Facade);
                    (Facade.SelectedListItem as GUIShowListItem).Images.NotifyPropertyChanged("Poster");
                    if (TraktSettings.CreditShowsHideRated) LoadCredits();
                    break;

                case ((int)MediaContextMenuItem.ChangeLayout):
                    CurrentLayout = GUICommon.ShowLayoutMenu(CurrentLayout, PreviousSelectedIndex);
                    break;

                case ((int)MediaContextMenuItem.SearchWithMpNZB):
                    string loadingParam = string.Format("search:{0}", selectedShow.Title);
                    GUIWindowManager.ActivateWindow((int)ExternalPluginWindows.MpNZB, loadingParam);
                    break;

                case ((int)MediaContextMenuItem.SearchTorrent):
                    string loadPar = selectedShow.Title;
                    GUIWindowManager.ActivateWindow((int)ExternalPluginWindows.MyTorrents, loadPar);
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
            var selectedItem = this.Facade.SelectedListItem as GUIShowListItem;
            if (selectedItem == null) return;

            GUICommon.CheckAndPlayFirstUnwatchedEpisode(selectedItem.Show, jumpTo);
        }

        private void LoadCredits()
        {
            GUIBackgroundTask.Instance.ExecuteInBackgroundAndCallback(() =>
            {
                return CurrentCredits;
            },
            delegate(bool success, object result)
            {
                if (success)
                {
                    if (CurrentCreditType == Credit.Cast)
                    {
                        var credits = result as TraktPersonShowCredits;
                        SendCastToFacade(credits.Cast);
                    }
                    else
                    {
                        var credits = result as TraktPersonShowCredits;

                        switch (CurrentCreditType)
                        {
                            case Credit.Art:
                                SendCrewToFacade(credits.Crew.Art);
                                break;
                            case Credit.Camera:
                                SendCrewToFacade(credits.Crew.Camera);
                                break;
                            case Credit.CostumeAndMakeUp:
                                SendCrewToFacade(credits.Crew.CostumeAndMakeUp);
                                break;
                            case Credit.Directing:
                                SendCrewToFacade(credits.Crew.Directing);
                                break;
                            case Credit.Production:
                                SendCrewToFacade(credits.Crew.Production);
                                break;
                            case Credit.Sound:
                                SendCrewToFacade(credits.Crew.Sound);
                                break;
                            case Credit.Writing:
                                SendCrewToFacade(credits.Crew.Writing);
                                break;
                        }
                    }
                }
            }, Translation.GettingShowCredits, true);
        }

        private void SendCrewToFacade(List<TraktPersonShowJob> crew)
        {
            // clear facade
            GUIControl.ClearControl(GetID, Facade.GetID);

            if (crew == null)
            {
                GUIUtils.ShowNotifyDialog(Translation.Error, Translation.ErrorGeneral);
                GUIWindowManager.ShowPreviousWindow();
                return;
            }

            // filter Shows
            var filteredCrew = FilterCrewShows(crew).Where(m => !string.IsNullOrEmpty(m.Show.Title)).ToList();

            // sort Shows
            filteredCrew.Sort(new GUIListItemShowSorter(TraktSettings.SortByCreditShows.Field, TraktSettings.SortByCreditShows.Direction));

            int itemId = 0;
            GUIShowListItem item = null;
            var ShowImages = new List<GUITmdbImage>();

            foreach (var job in filteredCrew)
            {
                // add image for download
                var images = new GUITmdbImage { ShowImages = new TmdbShowImages { Id = job.Show.Ids.Tmdb } };
                ShowImages.Add(images);

                item = new GUIShowListItem(job.Show.Title, (int)TraktGUIWindows.PersonCreditShows);
                item.Label2 = job.Show.Year == null ? "----" : job.Show.Year.ToString();
                item.Show = job.Show;
                item.TVTag = job;
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

            // Set Facade Layout
            Facade.CurrentLayout = CurrentLayout;
            GUIControl.FocusControl(GetID, Facade.GetID);

            Facade.SelectIndex(PreviousSelectedIndex);

            // set facade properties
            GUIUtils.SetProperty("#itemcount", Facade.Count.ToString());
            GUIUtils.SetProperty("#Trakt.Items", string.Format("{0} {1}", filteredCrew.Count, filteredCrew.Count > 1 ? Translation.Shows : Translation.Show));

            // Download Show images Async and set to facade
            GUIShowListItem.GetImages(ShowImages);
        }

        private void SendCastToFacade(List<TraktPersonShowCast> cast)
        {
            // clear facade
            GUIControl.ClearControl(GetID, Facade.GetID);

            if (cast == null)
            {
                GUIUtils.ShowNotifyDialog(Translation.Error, Translation.ErrorGeneral);
                GUIWindowManager.ShowPreviousWindow();
                return;
            }

            // filter Shows
            var filteredCast = FilterCastShows(cast).Where(m => !string.IsNullOrEmpty(m.Show.Title)).ToList();

            // sort Shows
            filteredCast.Sort(new GUIListItemShowSorter(TraktSettings.SortByCreditShows.Field, TraktSettings.SortByCreditShows.Direction));

            int itemId = 0;
            GUIShowListItem item = null;
            var ShowImages = new List<GUITmdbImage>();

            foreach (var credit in filteredCast)
            {
                // add image for download
                var images = new GUITmdbImage { ShowImages = new TmdbShowImages { Id = credit.Show.Ids.Tmdb } };
                ShowImages.Add(images);

                item = new GUIShowListItem(credit.Show.Title, (int)TraktGUIWindows.PersonCreditShows);
                item.Label2 = credit.Show.Year == null ? "----" : credit.Show.Year.ToString();
                item.Show = credit.Show;
                item.TVTag = credit;
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

            // Set Facade Layout
            Facade.CurrentLayout = CurrentLayout;
            GUIControl.FocusControl(GetID, Facade.GetID);

            Facade.SelectIndex(PreviousSelectedIndex);

            // set facade properties
            GUIUtils.SetProperty("#itemcount", Facade.Count.ToString());
            GUIUtils.SetProperty("#Trakt.Items", string.Format("{0} {1}", filteredCast.Count, filteredCast.Count > 1 ? Translation.Shows : Translation.Show));

            // Download Show images Async and set to facade
            GUIShowListItem.GetImages(ShowImages);
        }

        private void InitProperties()
        {
            // Fanart
            backdrop.GUIImageOne = FanartBackground;
            backdrop.GUIImageTwo = FanartBackground2;
            backdrop.LoadingImage = loadingImage;

            // publish person properties
            GUICommon.SetPersonProperties(CurrentPerson);

            // publish credit type
            GUICommon.SetProperty("#Trakt.Person.CreditTypeRaw", CurrentCreditType.ToString());
            GUICommon.SetProperty("#Trakt.Person.CreditType", GUICommon.GetTranslatedCreditType(CurrentCreditType));

            // load last layout
            CurrentLayout = (GUIFacadeControl.Layout)TraktSettings.PersonShowCreditsDefaultLayout;

            // Update Button States
            UpdateButtonState();

            if (sortButton != null)
            {
                UpdateButtonState();
                sortButton.SortChanged += (o, e) =>
                {
                    TraktSettings.SortByCreditShows.Direction = (SortingDirections)(e.Order - 1);
                    PreviousSelectedIndex = 0;
                    UpdateButtonState();
                    LoadCredits();
                };
            }

            return;
        }

        private void UpdateButtonState()
        {
            // update layout button label
            GUIControl.SetControlLabel(GetID, layoutButton.GetID, GUICommon.GetLayoutTranslation(CurrentLayout));

            // update sortby button label
            if (sortButton != null)
            {
                sortButton.Label = GUICommon.GetSortByString(TraktSettings.SortByCreditShows);
                sortButton.IsAscending = (TraktSettings.SortByCreditShows.Direction == SortingDirections.Ascending);
            }
            GUIUtils.SetProperty("#Trakt.SortBy", GUICommon.GetSortByString(TraktSettings.SortByCreditShows));

            // update filter buttons
            if (filterWatchedButton != null)
                filterWatchedButton.Selected = TraktSettings.CreditShowsHideWatched;
            if (filterWatchListedButton != null)
                filterWatchListedButton.Selected = TraktSettings.CreditShowsHideWatchlisted;
            if (filterCollectedButton != null)
                filterCollectedButton.Selected = TraktSettings.CreditShowsHideCollected;
            if (filterRatedButton != null)
                filterRatedButton.Selected = TraktSettings.CreditShowsHideRated;
        }

        private void ClearProperties()
        {
            GUIUtils.SetProperty("#Trakt.Person.Show.CreditValue", string.Empty);
            GUIUtils.SetProperty("#Trakt.Person.Show.CreditType", string.Empty);
            GUICommon.SetProperty("#Trakt.Person.Show.EpisodeCount", string.Empty);
            GUICommon.SetProperty("#Trakt.Person.Show.IsSeriesRegular", string.Empty);

            GUICommon.ClearPersonProperties();
            GUICommon.ClearShowProperties();
        }

        private void PublishCrewSkinProperties(TraktPersonShowJob creditItem)
        {
            GUICommon.SetProperty("#Trakt.Person.Show.CreditValue", GUICommon.GetTranslatedCreditJob(creditItem.Jobs.FirstOrDefault()));
            GUICommon.SetProperty("#Trakt.Person.Show.CreditType", Translation.Job);

            GUICommon.SetShowProperties(creditItem.Show);
        }

        private void PublishCastSkinProperties(TraktPersonShowCast creditItem)
        {
            GUICommon.SetProperty("#Trakt.Person.Show.CreditValue", creditItem.Characters.FirstOrDefault());
            GUICommon.SetProperty("#Trakt.Person.Show.CreditType", Translation.Character);
            GUICommon.SetProperty("#Trakt.Person.Show.EpisodeCount", creditItem.EpisodeCount);
            GUICommon.SetProperty("#Trakt.Person.Show.IsSeriesRegular", creditItem.IsSeriesRegular);

            GUICommon.SetShowProperties(creditItem.Show);
        }

        private void OnItemSelected(GUIListItem item, GUIControl control)
        {
            if (CurrentCreditType == Credit.Cast)
                OnCastSelected(item, Facade);
            else
                OnCrewSelected(item, Facade);
        }

        private void OnCrewSelected(GUIListItem item, GUIControl control)
        {
            PreviousSelectedIndex = Facade.SelectedListItemIndex;

            var creditItem = item.TVTag as TraktPersonShowJob;
            if (creditItem == null) return;

            PublishCrewSkinProperties(creditItem);
            GUIImageHandler.LoadFanart(backdrop, TmdbCache.GetShowBackdropFilename((item as GUIShowListItem).Images.ShowImages));
        }

        private void OnCastSelected(GUIListItem item, GUIControl control)
        {
            PreviousSelectedIndex = Facade.SelectedListItemIndex;

            var creditItem = item.TVTag as TraktPersonShowCast;
            if (creditItem == null) return;

            PublishCastSkinProperties(creditItem);
            GUIImageHandler.LoadFanart(backdrop, TmdbCache.GetShowBackdropFilename((item as GUIShowListItem).Images.ShowImages));
        }

        #endregion

        #region Filters

        private bool ShowTVShowFiltersMenu()
        {
            var filters = new Dictionary<Filters, bool>();

            filters.Add(Filters.Watched, TraktSettings.CreditShowsHideWatched);
            filters.Add(Filters.Watchlisted, TraktSettings.CreditShowsHideWatchlisted);
            filters.Add(Filters.Collected, TraktSettings.CreditShowsHideCollected);
            filters.Add(Filters.Rated, TraktSettings.CreditShowsHideRated);

            var selectedItems = GUIUtils.ShowMultiSelectionDialog(Translation.Filters, GUICommon.GetFilterListItems(filters));
            if (selectedItems == null) return false;

            foreach (var item in selectedItems.Where(l => l.Selected == true))
            {
                // toggle state of all selected items
                switch ((Filters)Enum.Parse(typeof(Filters), item.ItemID, true))
                {
                    case Filters.Watched:
                        TraktSettings.CreditShowsHideWatched = !TraktSettings.CreditShowsHideWatched;
                        break;
                    case Filters.Watchlisted:
                        TraktSettings.CreditShowsHideWatchlisted = !TraktSettings.CreditShowsHideWatchlisted;
                        break;
                    case Filters.Collected:
                        TraktSettings.CreditShowsHideCollected = !TraktSettings.CreditShowsHideCollected;
                        break;
                    case Filters.Rated:
                        TraktSettings.CreditShowsHideRated = !TraktSettings.CreditShowsHideRated;
                        break;
                }
            }

            return true;
        }

        private IEnumerable<TraktPersonShowJob> FilterCrewShows(IEnumerable<TraktPersonShowJob> jobs)
        {
            if (TraktSettings.CreditShowsHideWatched)
                jobs = jobs.Where(m => !m.Show.IsWatched());

            if (TraktSettings.CreditShowsHideWatchlisted)
                jobs = jobs.Where(m => !m.Show.IsWatchlisted());

            if (TraktSettings.CreditShowsHideCollected)
                jobs = jobs.Where(m => !m.Show.IsCollected());

            if (TraktSettings.CreditShowsHideRated)
                jobs = jobs.Where(m => m.Show.UserRating() == null);

            return jobs;
        }

        private IEnumerable<TraktPersonShowCast> FilterCastShows(IEnumerable<TraktPersonShowCast> characters)
        {
            if (TraktSettings.CreditShowsHideWatched)
                characters = characters.Where(m => !m.Show.IsWatched());

            if (TraktSettings.CreditShowsHideWatchlisted)
                characters = characters.Where(m => !m.Show.IsWatchlisted());

            if (TraktSettings.CreditShowsHideCollected)
                characters = characters.Where(m => !m.Show.IsCollected());

            if (TraktSettings.CreditShowsHideRated)
                characters = characters.Where(m => m.Show.UserRating() == null);

            return characters;
        }

        #endregion

        #region Public Static Properties

        public static TraktPersonSummary CurrentPerson { get; set; }
        public static TraktPersonShowCredits CurrentCredits { get; set; }
        public static Credit CurrentCreditType { get; set; }

        #endregion
    }
}