using System;
using System.Collections.Generic;
using System.Linq;
using MediaPortal.GUI.Library;
using MediaPortal.Util;
using TraktPlugin.TraktAPI.DataStructures;
using Action = MediaPortal.GUI.Library.Action;

namespace TraktPlugin.GUI
{
    public class GUIPersonMovieCredits : GUIWindow
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

        public GUIPersonMovieCredits()
        {
            backdrop = new ImageSwapper();
            backdrop.PropertyOne = "#Trakt.PersonCreditMovies.Fanart.1";
            backdrop.PropertyTwo = "#Trakt.PersonCreditMovies.Fanart.2";
        }

        #endregion

        #region Private Variables

        int PreviousSelectedIndex = 0;
        private Layout CurrentLayout { get; set; }
        private ImageSwapper backdrop;
                
        #endregion

        #region Base Overrides

        public override int GetID
        {
            get
            {
                return (int)TraktGUIWindows.PersonCreditMovies;
            }
        }

        public override bool Init()
        {
            return Load(GUIGraphicsContext.Skin + @"\Trakt.Person.Credits.Movies.xml");
        }

        protected override void OnPageLoad()
        {
            base.OnPageLoad();

            if (CurrentPerson == null || CurrentCredits == null)
            {
                TraktLogger.Info("Exiting Person Movie Credits as there is no current person or credits set");
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
            GUIMovieListItem.StopDownload = true;
            ClearProperties();

            // save current layout
            TraktSettings.PersonMovieCreditsDefaultLayout = (int)CurrentLayout;

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
                    var item = Facade.SelectedListItem as GUIMovieListItem;
                    if (item == null) return;

                    CheckAndPlayMovie(true);
                    break;

                // Layout Button
                case (2):
                    CurrentLayout = GUICommon.ShowLayoutMenu(CurrentLayout, PreviousSelectedIndex);
                    break;

                // Sort Button
                case (8):
                    var newSortBy = GUICommon.ShowSortMenu(TraktSettings.SortByCreditMovies);
                    if (newSortBy != null)
                    {
                        if (newSortBy.Field != TraktSettings.SortByCreditMovies.Field)
                        {
                            TraktSettings.SortByCreditMovies = newSortBy;
                            PreviousSelectedIndex = 0;
                            UpdateButtonState();
                            LoadCredits();
                        }
                    }
                    break;

                // Hide Watched
                case (9):
                    PreviousSelectedIndex = 0;
                    TraktSettings.CreditMoviesHideWatched = !TraktSettings.CreditMoviesHideWatched;
                    UpdateButtonState();
                    LoadCredits();
                    break;

                // Hide Watchlisted
                case (10):
                    PreviousSelectedIndex = 0;
                    TraktSettings.CreditMoviesHideWatchlisted = !TraktSettings.CreditMoviesHideWatchlisted;
                    UpdateButtonState();
                    LoadCredits();
                    break;

                // Hide Collected
                case (11):
                    PreviousSelectedIndex = 0;
                    TraktSettings.CreditMoviesHideCollected = !TraktSettings.CreditMoviesHideCollected;
                    UpdateButtonState();
                    LoadCredits();
                    break;

                // Hide Rated
                case (12):
                    PreviousSelectedIndex = 0;
                    TraktSettings.CreditMoviesHideRated = !TraktSettings.CreditMoviesHideRated;
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
            var selectedItem = this.Facade.SelectedListItem as GUIMovieListItem;
            if (selectedItem == null) return;

            var selectedMovie = selectedItem.Movie;

            var dlg = (IDialogbox)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
            if (dlg == null) return;

            dlg.Reset();
            dlg.SetHeading(GUIUtils.PluginName());

            GUICommon.CreateMoviesContextMenu(ref dlg, selectedMovie, false);

            // Show Context Menu
            dlg.DoModal(GUIWindowManager.ActiveWindow);
            if (dlg.SelectedId < 0) return;

            switch (dlg.SelectedId)
            {
                case ((int)MediaContextMenuItem.MarkAsWatched):
                    TraktHelper.AddMovieToWatchHistory(selectedMovie);
                    selectedItem.IsPlayed = true;
                    OnItemSelected(selectedItem, Facade);
                    selectedItem.Images.NotifyPropertyChanged("Poster");
                    if (TraktSettings.CreditMoviesHideWatched) LoadCredits();
                    break;

                case ((int)MediaContextMenuItem.MarkAsUnWatched):
                    TraktHelper.RemoveMovieFromWatchHistory(selectedMovie);
                    selectedItem.IsPlayed = false;
                    OnItemSelected(selectedItem, Facade);
                    selectedItem.Images.NotifyPropertyChanged("Poster");
                    break;

                case ((int)MediaContextMenuItem.AddToWatchList):
                    TraktHelper.AddMovieToWatchList(selectedMovie, true);
                    OnItemSelected(selectedItem, Facade);
                    selectedItem.Images.NotifyPropertyChanged("Poster");
                    if (TraktSettings.CreditMoviesHideWatchlisted) LoadCredits();
                    break;

                case ((int)MediaContextMenuItem.RemoveFromWatchList):
                    TraktHelper.RemoveMovieFromWatchList(selectedMovie, true);
                    OnItemSelected(selectedItem, Facade);
                    selectedItem.Images.NotifyPropertyChanged("Poster");
                    break;

                case ((int)MediaContextMenuItem.AddToList):
                    TraktHelper.AddRemoveMovieInUserList(selectedMovie, false);
                    break;

                case ((int)MediaContextMenuItem.AddToLibrary):
                    TraktHelper.AddMovieToCollection(selectedMovie);
                    OnItemSelected(selectedItem, Facade);
                    selectedItem.Images.NotifyPropertyChanged("Poster");
                    if (TraktSettings.CreditMoviesHideCollected) LoadCredits();
                    break;

                case ((int)MediaContextMenuItem.RemoveFromLibrary):
                    TraktHelper.RemoveMovieFromCollection(selectedMovie);
                    OnItemSelected(selectedItem, Facade);
                    selectedItem.Images.NotifyPropertyChanged("Poster");
                    break;

                case ((int)MediaContextMenuItem.Related):
                    TraktHelper.ShowRelatedMovies(selectedMovie);
                    break;

                case ((int)MediaContextMenuItem.Rate):
                    GUICommon.RateMovie(selectedMovie);
                    OnItemSelected(selectedItem, Facade);
                    selectedItem.Images.NotifyPropertyChanged("Poster");
                    if (TraktSettings.CreditMoviesHideRated) LoadCredits();
                    break;

                case ((int)MediaContextMenuItem.Filters):
                    if (ShowMovieFiltersMenu())
                    {
                        PreviousSelectedIndex = 0;
                        UpdateButtonState();
                        LoadCredits();
                    }
                    break;

                case ((int)MediaContextMenuItem.Shouts):
                    TraktHelper.ShowMovieShouts(selectedMovie);
                    break;

                case ((int)MediaContextMenuItem.Cast):
                    GUICreditsMovie.Movie = selectedMovie;
                    GUICreditsMovie.Type = GUICreditsMovie.CreditType.Cast;
                    GUICreditsMovie.Fanart = selectedMovie.Images.Fanart.LocalImageFilename(ArtworkType.MovieFanart);
                    GUIWindowManager.ActivateWindow((int)TraktGUIWindows.CreditsMovie);
                    break;

                case ((int)MediaContextMenuItem.Crew):
                    GUICreditsMovie.Movie = selectedMovie;
                    GUICreditsMovie.Type = GUICreditsMovie.CreditType.Crew;
                    GUICreditsMovie.Fanart = selectedMovie.Images.Fanart.LocalImageFilename(ArtworkType.MovieFanart);
                    GUIWindowManager.ActivateWindow((int)TraktGUIWindows.CreditsMovie);
                    break;

                case ((int)MediaContextMenuItem.Trailers):
                    GUICommon.ShowMovieTrailersMenu(selectedMovie);
                    break;

                case ((int)MediaContextMenuItem.ChangeLayout):
                    CurrentLayout = GUICommon.ShowLayoutMenu(CurrentLayout, PreviousSelectedIndex);
                    break;

                case ((int)MediaContextMenuItem.SearchWithMpNZB):
                    string loadingParam = string.Format("search:{0}", selectedMovie.Title);
                    GUIWindowManager.ActivateWindow((int)ExternalPluginWindows.MpNZB, loadingParam);
                    break;

                case ((int)MediaContextMenuItem.SearchTorrent):
                    string loadParm = selectedMovie.Title;
                    GUIWindowManager.ActivateWindow((int)ExternalPluginWindows.MyTorrents, loadParm);
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
            var selectedItem = this.Facade.SelectedListItem as GUIMovieListItem;
            if (selectedItem == null) return;

            GUICommon.CheckAndPlayMovie(jumpTo, selectedItem.Movie);
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
                        var credits = result as TraktPersonMovieCredits;
                        SendCastToFacade(credits.Cast);
                    }
                    else
                    {
                        var credits = result as TraktPersonMovieCredits;

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
            }, Translation.GettingMovieCredits, true);
        }

        private void SendCrewToFacade(List<TraktPersonMovieJob> crew)
        {
            // clear facade
            GUIControl.ClearControl(GetID, Facade.GetID);

            if (crew == null)
            {
                GUIUtils.ShowNotifyDialog(Translation.Error, Translation.ErrorGeneral);
                GUIWindowManager.ShowPreviousWindow();
                return;
            }

            // filter movies
            var filteredCrew = FilterCrewMovies(crew).Where(m => !string.IsNullOrEmpty(m.Movie.Title)).ToList();

            // sort movies
            filteredCrew.Sort(new GUIListItemMovieSorter(TraktSettings.SortByCreditMovies.Field, TraktSettings.SortByCreditMovies.Direction));

            int itemId = 0;
            GUIMovieListItem item = null;
            var movieImages = new List<GUITraktImage>();

            foreach (var job in filteredCrew)
            {
                // add image for download
                var images = new GUITraktImage { MovieImages = job.Movie.Images };
                movieImages.Add(images);

                item = new GUIMovieListItem(job.Movie.Title, (int)TraktGUIWindows.PersonCreditMovies);
                item.Label2 = job.Movie.Year == null ? "----" : job.Movie.Year.ToString();
                item.Movie = job.Movie;
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
            Facade.SetCurrentLayout(Enum.GetName(typeof(Layout), CurrentLayout));
            GUIControl.FocusControl(GetID, Facade.GetID);

            Facade.SelectIndex(PreviousSelectedIndex);

            // set facade properties
            GUIUtils.SetProperty("#itemcount", Facade.Count.ToString());
            GUIUtils.SetProperty("#Trakt.Items", string.Format("{0} {1}", filteredCrew.Count, filteredCrew.Count > 1 ? Translation.Movies : Translation.Movie));

            // Download movie images Async and set to facade
            GUIMovieListItem.GetImages(movieImages);
        }

        private void SendCastToFacade(List<TraktPersonMovieCast> cast)
        {
            // clear facade
            GUIControl.ClearControl(GetID, Facade.GetID);

            if (cast == null)
            {
                GUIUtils.ShowNotifyDialog(Translation.Error, Translation.ErrorGeneral);
                GUIWindowManager.ShowPreviousWindow();
                return;
            }

            // filter movies
            var filteredCast = FilterCastMovies(cast).Where(m => !string.IsNullOrEmpty(m.Movie.Title)).ToList();

            // sort movies
            filteredCast.Sort(new GUIListItemMovieSorter(TraktSettings.SortByCreditMovies.Field, TraktSettings.SortByCreditMovies.Direction));

            int itemId = 0;
            GUIMovieListItem item = null;
            var movieImages = new List<GUITraktImage>();

            foreach (var credit in filteredCast)
            {
                // add image for download
                var images = new GUITraktImage { MovieImages = credit.Movie.Images };
                movieImages.Add(images);

                item = new GUIMovieListItem(credit.Movie.Title, (int)TraktGUIWindows.PersonCreditMovies);
                item.Label2 = credit.Movie.Year == null ? "----" : credit.Movie.Year.ToString();
                item.Movie = credit.Movie;
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
            Facade.SetCurrentLayout(Enum.GetName(typeof(Layout), CurrentLayout));
            GUIControl.FocusControl(GetID, Facade.GetID);

            Facade.SelectIndex(PreviousSelectedIndex);

            // set facade properties
            GUIUtils.SetProperty("#itemcount", Facade.Count.ToString());
            GUIUtils.SetProperty("#Trakt.Items", string.Format("{0} {1}", filteredCast.Count, filteredCast.Count > 1 ? Translation.Movies : Translation.Movie));

            // Download movie images Async and set to facade
            GUIMovieListItem.GetImages(movieImages);
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
            CurrentLayout = (Layout)TraktSettings.PersonMovieCreditsDefaultLayout;

            // Update Button States
            UpdateButtonState();

            if (sortButton != null)
            {
                UpdateButtonState();
                sortButton.SortChanged += (o, e) =>
                {
                    TraktSettings.SortByCreditMovies.Direction = (SortingDirections)(e.Order - 1);
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
                sortButton.Label = GUICommon.GetSortByString(TraktSettings.SortByCreditMovies);
                sortButton.IsAscending = (TraktSettings.SortByCreditMovies.Direction == SortingDirections.Ascending);
            }
            GUIUtils.SetProperty("#Trakt.SortBy", GUICommon.GetSortByString(TraktSettings.SortByCreditMovies));

            // update filter buttons
            if (filterWatchedButton != null)
                filterWatchedButton.Selected = TraktSettings.CreditMoviesHideWatched;
            if (filterWatchListedButton != null)
                filterWatchListedButton.Selected = TraktSettings.CreditMoviesHideWatchlisted;
            if (filterCollectedButton != null)
                filterCollectedButton.Selected = TraktSettings.CreditMoviesHideCollected;
            if (filterRatedButton != null)
                filterRatedButton.Selected = TraktSettings.CreditMoviesHideRated;
        }

        private void ClearProperties()
        {
            GUIUtils.SetProperty("#Trakt.Person.Movie.CreditValue", string.Empty);
            GUIUtils.SetProperty("#Trakt.Person.Movie.CreditType", string.Empty);

            GUICommon.ClearPersonProperties();
            GUICommon.ClearMovieProperties();
        }

        private void PublishCrewSkinProperties(TraktPersonMovieJob creditItem)
        {
            GUICommon.SetProperty("#Trakt.Person.Movie.CreditValue", GUICommon.GetTranslatedCreditJob(creditItem.Job));
            GUICommon.SetProperty("#Trakt.Person.Movie.CreditType", Translation.Job);

            GUICommon.SetMovieProperties(creditItem.Movie);
        }

        private void PublishCastSkinProperties(TraktPersonMovieCast creditItem)
        {
            GUICommon.SetProperty("#Trakt.Person.Movie.CreditValue", creditItem.Character);
            GUICommon.SetProperty("#Trakt.Person.Movie.CreditType", Translation.Character);

            GUICommon.SetMovieProperties(creditItem.Movie);
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

            var creditItem = item.TVTag as TraktPersonMovieJob;
            if (creditItem == null) return;

            PublishCrewSkinProperties(creditItem);
            GUIImageHandler.LoadFanart(backdrop, creditItem.Movie.Images.Fanart.LocalImageFilename(ArtworkType.MovieFanart));
        }

        private void OnCastSelected(GUIListItem item, GUIControl control)
        {
            PreviousSelectedIndex = Facade.SelectedListItemIndex;

            var creditItem = item.TVTag as TraktPersonMovieCast;
            if (creditItem == null) return;

            PublishCastSkinProperties(creditItem);
            GUIImageHandler.LoadFanart(backdrop, creditItem.Movie.Images.Fanart.LocalImageFilename(ArtworkType.MovieFanart));
        }

        #endregion

        #region Filters

        private bool ShowMovieFiltersMenu()
        {
            var filters = new Dictionary<Filters, bool>();

            filters.Add(Filters.Watched, TraktSettings.CreditMoviesHideWatched);
            filters.Add(Filters.Watchlisted, TraktSettings.CreditMoviesHideWatchlisted);
            filters.Add(Filters.Collected, TraktSettings.CreditMoviesHideCollected);
            filters.Add(Filters.Rated, TraktSettings.CreditMoviesHideRated);

            var selectedItems = GUIUtils.ShowMultiSelectionDialog(Translation.Filters, GUICommon.GetFilterListItems(filters));
            if (selectedItems == null) return false;

            foreach (var item in selectedItems.Where(l => l.Selected == true))
            {
                // toggle state of all selected items
                switch ((Filters)Enum.Parse(typeof(Filters), item.ItemID, true))
                {
                    case Filters.Watched:
                        TraktSettings.CreditMoviesHideWatched = !TraktSettings.CreditMoviesHideWatched;
                        break;
                    case Filters.Watchlisted:
                        TraktSettings.CreditMoviesHideWatchlisted = !TraktSettings.CreditMoviesHideWatchlisted;
                        break;
                    case Filters.Collected:
                        TraktSettings.CreditMoviesHideCollected = !TraktSettings.CreditMoviesHideCollected;
                        break;
                    case Filters.Rated:
                        TraktSettings.CreditMoviesHideRated = !TraktSettings.CreditMoviesHideRated;
                        break;
                }
            }

            return true;
        }

        private IEnumerable<TraktPersonMovieJob> FilterCrewMovies(IEnumerable<TraktPersonMovieJob> jobs)
        {
            if (TraktSettings.CreditMoviesHideWatched)
                jobs = jobs.Where(m => !m.Movie.IsWatched());

            if (TraktSettings.CreditMoviesHideWatchlisted)
                jobs = jobs.Where(m => !m.Movie.IsWatchlisted());

            if (TraktSettings.CreditMoviesHideCollected)
                jobs = jobs.Where(m => !m.Movie.IsCollected());

            if (TraktSettings.CreditMoviesHideRated)
                jobs = jobs.Where(m => m.Movie.UserRating() == null);

            return jobs;
        }

        private IEnumerable<TraktPersonMovieCast> FilterCastMovies(IEnumerable<TraktPersonMovieCast> characters)
        {
            if (TraktSettings.CreditMoviesHideWatched)
                characters = characters.Where(m => !m.Movie.IsWatched());

            if (TraktSettings.CreditMoviesHideWatchlisted)
                characters = characters.Where(m => !m.Movie.IsWatchlisted());

            if (TraktSettings.CreditMoviesHideCollected)
                characters = characters.Where(m => !m.Movie.IsCollected());

            if (TraktSettings.CreditMoviesHideRated)
                characters = characters.Where(m => m.Movie.UserRating() == null);

            return characters;
        }

        #endregion

        #region Public Static Properties

        public static TraktPersonSummary CurrentPerson { get; set; } 
        public static TraktPersonMovieCredits CurrentCredits { get; set; }
        public static Credit CurrentCreditType { get; set; }

        #endregion
    }
}