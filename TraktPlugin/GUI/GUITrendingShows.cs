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
using Action = MediaPortal.GUI.Library.Action;
using MediaPortal.Util;
using TraktPlugin.TraktAPI;
using TraktPlugin.TraktAPI.DataStructures;

namespace TraktPlugin.GUI
{
    public class GUITrendingShows : GUIWindow
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

        #endregion

        #region Constructor

        public GUITrendingShows() 
        {
            backdrop = new ImageSwapper();
            backdrop.PropertyOne = "#Trakt.TrendingShows.Fanart.1";
            backdrop.PropertyTwo = "#Trakt.TrendingShows.Fanart.2";
        }

        #endregion

        #region Private Variables

        private Layout CurrentLayout { get; set; }
        private ImageSwapper backdrop;
        DateTime LastRequest = new DateTime();
        int PreviousSelectedIndex = 0;

        IEnumerable<TraktTrendingShow> TrendingShows
        {
            get
            {
                if (_TrendingShows == null || LastRequest < DateTime.UtcNow.Subtract(new TimeSpan(0, TraktSettings.WebRequestCacheMinutes, 0)))
                {
                    _TrendingShows = TraktAPI.TraktAPI.GetTrendingShows();
                    LastRequest = DateTime.UtcNow;
                    PreviousSelectedIndex = 0;
                }
                return _TrendingShows;
            }
        }
        private IEnumerable<TraktTrendingShow> _TrendingShows = null;
        
        #endregion

        #region Base Overrides

        public override int GetID
        {
            get
            {
                return (int)TraktGUIWindows.TrendingShows;
            }
        }

        public override bool Init()
        {
            return Load(GUIGraphicsContext.Skin + @"\Trakt.Trending.Shows.xml");
        }

        protected override void OnPageLoad()
        {
            base.OnPageLoad();

            // Clear GUI Properties
            ClearProperties();

            // Init Properties
            InitProperties();

            // Load Trending Shows
            LoadTrendingShows();
        }

        protected override void OnPageDestroy(int new_windowId)
        {
            GUIShowListItem.StopDownload = true;
            PreviousSelectedIndex = Facade.SelectedListItemIndex;
            ClearProperties();

            // save current layout
            TraktSettings.TrendingShowsDefaultLayout = (int)CurrentLayout;

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
                        if (TraktSettings.EnableJumpToForTVShows)
                        {
                            CheckAndPlayEpisode(true);
                        }
                        else
                        {
                            GUIListItem selectedItem = this.Facade.SelectedListItem;
                            if (selectedItem == null) return;
                            TraktTrendingShow selectedShow = (TraktTrendingShow)selectedItem.TVTag;
                            GUIWindowManager.ActivateWindow((int)TraktGUIWindows.ShowSeasons, selectedShow.ToJSON());
                        }
                    }
                    break;

                // Layout Button
                case (2):
                    CurrentLayout = GUICommon.ShowLayoutMenu(CurrentLayout, PreviousSelectedIndex);
                    break;

                // Sort Button
                case (8):
                    var newSortBy = GUICommon.ShowSortMenu(TraktSettings.SortByTrendingShows);
                    if (newSortBy != null)
                    {
                        if (newSortBy.Field != TraktSettings.SortByTrendingShows.Field)
                        {
                            TraktSettings.SortByTrendingShows = newSortBy;
                            PreviousSelectedIndex = 0;
                            UpdateButtonState();
                            LoadTrendingShows();
                        }
                    }
                    break;

                // Hide Watched
                case (9):
                    TraktSettings.TrendingShowsHideWatched = !TraktSettings.TrendingShowsHideWatched;
                    UpdateButtonState();
                    LoadTrendingShows();
                    break;

                // Hide Watchlisted
                case (10):
                    TraktSettings.TrendingShowsHideWatchlisted = !TraktSettings.TrendingShowsHideWatchlisted;
                    UpdateButtonState();
                    LoadTrendingShows();
                    break;

                // Hide Collected
                case (11):
                    TraktSettings.TrendingShowsHideCollected = !TraktSettings.TrendingShowsHideCollected;
                    UpdateButtonState();
                    LoadTrendingShows();
                    break;

                // Hide Rated
                case (12):
                    TraktSettings.TrendingShowsHideRated = !TraktSettings.TrendingShowsHideRated;
                    UpdateButtonState();
                    LoadTrendingShows();
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
                    if (!GUIBackgroundTask.Instance.IsBusy)
                    {
                        CheckAndPlayEpisode(false);
                    };
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

            var selectedShow = selectedItem.TVTag as TraktTrendingShow;
            if (selectedShow == null) return;

            var dlg = (IDialogbox)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
            if (dlg == null) return;

            dlg.Reset();
            dlg.SetHeading(GUIUtils.PluginName());

            GUICommon.CreateTrendingShowsContextMenu(ref dlg, selectedShow, false);

            // Show Context Menu
            dlg.DoModal(GUIWindowManager.ActiveWindow);
            if (dlg.SelectedId < 0) return;

            switch (dlg.SelectedId)
            {
                case ((int)TrendingContextMenuItem.AddToWatchList):
                    TraktHelper.AddShowToWatchList(selectedShow);
                    selectedShow.InWatchList = true;
                    OnShowSelected(selectedItem, Facade);
                    (Facade.SelectedListItem as GUIShowListItem).Images.NotifyPropertyChanged("Poster");
                    if (TraktSettings.TrendingShowsHideWatchlisted) LoadTrendingShows();
                    break;

                case ((int)TrendingContextMenuItem.ShowSeasonInfo):
                    GUIWindowManager.ActivateWindow((int)TraktGUIWindows.ShowSeasons, selectedShow.ToJSON());
                    break;

                case ((int)TrendingContextMenuItem.MarkAsWatched):
                    GUICommon.MarkShowAsSeen(selectedShow);
                    if (TraktSettings.TrendingShowsHideWatched) LoadTrendingShows();
                    break;

                case ((int)TrendingContextMenuItem.AddToLibrary):
                    GUICommon.AddShowToLibrary(selectedShow);
                    if (TraktSettings.TrendingShowsHideCollected) LoadTrendingShows();
                    break;

                case ((int)TrendingContextMenuItem.RemoveFromWatchList):
                    TraktHelper.RemoveShowFromWatchList(selectedShow);
                    selectedShow.InWatchList = false;
                    OnShowSelected(selectedItem, Facade);
                    (Facade.SelectedListItem as GUIShowListItem).Images.NotifyPropertyChanged("Poster");
                    break;

                case ((int)TrendingContextMenuItem.AddToList):
                    TraktHelper.AddRemoveShowInUserList(selectedShow.Title, selectedShow.Year.ToString(), selectedShow.Tvdb, false);
                    break;

                case ((int)TrendingContextMenuItem.Filters):
                    if (GUICommon.ShowTVShowFiltersMenu())
                    {
                        UpdateButtonState();
                        LoadTrendingShows();
                    }
                    break;

                case ((int)TrendingContextMenuItem.Related):
                    TraktHelper.ShowRelatedShows(selectedShow);
                    break;

                case ((int)TrendingContextMenuItem.Trailers):
                    GUICommon.ShowTVShowTrailersMenu(selectedShow);
                    break;

                case ((int)TrendingContextMenuItem.Shouts):
                    TraktHelper.ShowTVShowShouts(selectedShow);
                    break;

                case ((int)TrendingContextMenuItem.Rate):
                    GUICommon.RateShow(selectedShow);
                    OnShowSelected(selectedItem, Facade);
                    (Facade.SelectedListItem as GUIShowListItem).Images.NotifyPropertyChanged("Poster");
                    if (TraktSettings.TrendingShowsHideRated) LoadTrendingShows();
                    break;

                case ((int)TrendingContextMenuItem.ChangeLayout):
                    CurrentLayout = GUICommon.ShowLayoutMenu(CurrentLayout, PreviousSelectedIndex);
                    break;

                case ((int)TrendingContextMenuItem.SearchWithMpNZB):
                    string loadingParam = string.Format("search:{0}", selectedShow.Title);
                    GUIWindowManager.ActivateWindow((int)ExternalPluginWindows.MpNZB, loadingParam);
                    break;

                case ((int)TrendingContextMenuItem.SearchTorrent):
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
            var selectedItem = this.Facade.SelectedListItem;
            if (selectedItem == null) return;

            var selectedShow = selectedItem.TVTag as TraktShow;
            GUICommon.CheckAndPlayFirstUnwatched(selectedShow, jumpTo);
        }

        private void LoadTrendingShows()
        {
            GUIUtils.SetProperty("#Trakt.Items", string.Empty);

            GUIBackgroundTask.Instance.ExecuteInBackgroundAndCallback(() =>
            {
                return TrendingShows;
            },
            delegate(bool success, object result)
            {
                if (success)
                {
                    IEnumerable<TraktTrendingShow> shows = result as IEnumerable<TraktTrendingShow>;
                    SendTrendingShowsToFacade(shows);
                }
            }, Translation.GettingTrendingShows, true);
        }

        private void SendTrendingShowsToFacade(IEnumerable<TraktTrendingShow> shows)
        {
            // clear facade
            GUIControl.ClearControl(GetID, Facade.GetID);

            if (shows.Count() == 0)
            {
                GUIUtils.ShowNotifyDialog(GUIUtils.PluginName(), Translation.NoTrendingShows);
                GUIWindowManager.ShowPreviousWindow();
                return;
            }

            // filter shows
            shows = GUICommon.FilterTrendingShows(shows);

            // sort shows
            var showList = shows.ToList();
            showList.Sort(new GUIListItemShowSorter(TraktSettings.SortByTrendingShows.Field, TraktSettings.SortByTrendingShows.Direction));

            int itemId = 0;
            var showImages = new List<TraktImage>();

            foreach (var show in showList)
            {
                var item = new GUIShowListItem(show.Title, (int)TraktGUIWindows.TrendingShows);

                // add image for download
                var image = new TraktImage { ShowImages = show.Images };
                showImages.Add(image);

                item.Label2 = show.Year.ToString();
                item.TVTag = show;
                item.Images = image;
                item.ItemId = Int32.MaxValue - itemId;                
                item.IconImage = GUIImageHandler.GetDefaultPoster(false);
                item.IconImageBig = GUIImageHandler.GetDefaultPoster();
                item.ThumbnailImage = GUIImageHandler.GetDefaultPoster();
                item.OnItemSelected += OnShowSelected;
                Utils.SetDefaultIcons(item);
                Facade.Add(item);
                itemId++;
            }

            // Set Facade Layout
            Facade.SetCurrentLayout(Enum.GetName(typeof(Layout), CurrentLayout));
            GUIControl.FocusControl(GetID, Facade.GetID);

            Facade.SelectIndex(PreviousSelectedIndex);

            // set facade properties
            GUIUtils.SetProperty("#itemcount", shows.Count().ToString());
            GUIUtils.SetProperty("#Trakt.Items", string.Format("{0} {1}", shows.Count().ToString(), shows.Count() > 1 ? Translation.SeriesPlural : Translation.Series));
            GUIUtils.SetProperty("#Trakt.Trending.PeopleCount", shows.Sum(s => s.Watchers).ToString());
            GUIUtils.SetProperty("#Trakt.Trending.Description", string.Format(Translation.TrendingTVShowsPeople, shows.Sum(s => s.Watchers).ToString(), shows.Count().ToString()));

            // Download show images Async and set to facade
            GUIShowListItem.GetImages(showImages);
        }

        private void InitProperties()
        {
            // Fanart
            backdrop.GUIImageOne = FanartBackground;
            backdrop.GUIImageTwo = FanartBackground2;
            backdrop.LoadingImage = loadingImage;            

            // load last layout
            CurrentLayout = (Layout)TraktSettings.TrendingShowsDefaultLayout;

            // Update Button States
            UpdateButtonState();

            if (sortButton != null)
            {
                sortButton.SortChanged += (o, e) =>
                {
                    TraktSettings.SortByTrendingShows.Direction = (SortingDirections)(e.Order - 1);
                    PreviousSelectedIndex = 0;
                    UpdateButtonState();
                    LoadTrendingShows();
                };
            }
        }

        private void UpdateButtonState()
        {
            // update layout button label
            GUIControl.SetControlLabel(GetID, layoutButton.GetID, GUICommon.GetLayoutTranslation(CurrentLayout));

            // update sortby button label
            if (sortButton != null)
            {
                sortButton.Label = GUICommon.GetSortByString(TraktSettings.SortByTrendingShows);
                sortButton.IsAscending = (TraktSettings.SortByTrendingShows.Direction == SortingDirections.Ascending);
            }
            GUIUtils.SetProperty("#Trakt.SortBy", GUICommon.GetSortByString(TraktSettings.SortByTrendingShows));

            // update filter buttons
            if (filterWatchedButton != null)
                filterWatchedButton.Selected = TraktSettings.TrendingShowsHideWatched;
            if (filterWatchListedButton != null)
                filterWatchListedButton.Selected = TraktSettings.TrendingShowsHideWatchlisted;
            if (filterCollectedButton != null)
                filterCollectedButton.Selected = TraktSettings.TrendingShowsHideCollected;
            if (filterRatedButton != null)
                filterRatedButton.Selected = TraktSettings.TrendingShowsHideRated;
        }

        private void ClearProperties()
        {
            GUIUtils.SetProperty("#Trakt.Trending.PeopleCount", string.Empty);
            GUIUtils.SetProperty("#Trakt.Trending.Description", string.Empty);

            GUIUtils.SetProperty("#Trakt.Show.Watchers", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.Watchers.Extra", string.Empty);
            GUICommon.ClearShowProperties();
        }

        private void PublishShowSkinProperties(TraktTrendingShow show)
        {
            GUICommon.SetProperty("#Trakt.Show.Watchers", show.Watchers.ToString());
            GUICommon.SetProperty("#Trakt.Show.Watchers.Extra", show.Watchers > 1 ? string.Format(Translation.PeopleWatching, show.Watchers) : Translation.PersonWatching);
            GUICommon.SetShowProperties(show);
        }

        private void OnShowSelected(GUIListItem item, GUIControl parent)
        {
            PreviousSelectedIndex = Facade.SelectedListItemIndex;

            var show = item.TVTag as TraktTrendingShow;
            PublishShowSkinProperties(show);
            GUIImageHandler.LoadFanart(backdrop, show.Images.Fanart.LocalImageFilename(ArtworkType.ShowFanart));
        }
        #endregion
    }
}
