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
    public class GUIRecentAddedEpisodes : GUIWindow
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
            ShowSeasonInfo,
            RemoveFromWatchList,
            AddToWatchList,
            AddToList,
            ChangeLayout,
            MarkAsWatched,
            MarkAsUnWatched,
            AddToLibrary,
            RemoveFromLibrary,
            Related,
            Rate,
            Shouts,
            Trailers,
            SearchWithMpNZB,
            SearchTorrent
        }

        #endregion

        #region Constructor

        public GUIRecentAddedEpisodes()
        {
            backdrop = new ImageSwapper();
            backdrop.PropertyOne = "#Trakt.RecentAddedEpisodes.Fanart.1";
            backdrop.PropertyTwo = "#Trakt.RecentAddedEpisodes.Fanart.2";
        }

        #endregion

        #region Private Variables

        static int PreviousSelectedIndex { get; set; }
        static DateTime LastRequest = new DateTime();
        string PreviousUser = null;
        Layout CurrentLayout { get; set; }
        ImageSwapper backdrop;
        Dictionary<string, IEnumerable<TraktActivity.Activity>> userRecentlyAddedEpisodes = new Dictionary<string, IEnumerable<TraktActivity.Activity>>();

        IEnumerable<TraktActivity.Activity> RecentlyAddedEpisodes
        {
            get
            {
                if (!userRecentlyAddedEpisodes.Keys.Contains(CurrentUser) || LastRequest < DateTime.UtcNow.Subtract(new TimeSpan(0, TraktSettings.WebRequestCacheMinutes, 0)))
                {
                    TraktActivity activity = TraktAPI.TraktAPI.GetUserActivity
                    (
                        CurrentUser,
                        new List<TraktAPI.ActivityType>() { TraktAPI.ActivityType.episode },
                        new List<TraktAPI.ActivityAction>() { TraktAPI.ActivityAction.collection }
                    );

                    _RecentlyAddedEpisodes = activity.Activities;
                    if (userRecentlyAddedEpisodes.Keys.Contains(CurrentUser)) userRecentlyAddedEpisodes.Remove(CurrentUser);
                    userRecentlyAddedEpisodes.Add(CurrentUser, _RecentlyAddedEpisodes);
                    LastRequest = DateTime.UtcNow;
                    PreviousSelectedIndex = 0;
                }
                return userRecentlyAddedEpisodes[CurrentUser];
            }
        }
        private IEnumerable<TraktActivity.Activity> _RecentlyAddedEpisodes = null;

        #endregion

        #region Public Properties

        public static string CurrentUser { get; set; }

        #endregion

        #region Base Overrides

        public override int GetID
        {
            get
            {
                return (int)TraktGUIWindows.RecentAddedEpisodes;
            }
        }

        public override bool Init()
        {
            return Load(GUIGraphicsContext.Skin + @"\Trakt.RecentAdded.Episodes.xml");
        }

        protected override void OnPageLoad()
        {
            base.OnPageLoad();

            // Clear GUI Properties
            ClearProperties();

            // Requires Login
            if (!GUICommon.CheckLogin()) return;

            // Init Properties
            InitProperties();

            // Load Recently Added
            LoadRecentlyAddedEpisodes();
        }

        protected override void OnPageDestroy(int new_windowId)
        {
            GUIEpisodeListItem.StopDownload = true;
            PreviousSelectedIndex = Facade.SelectedListItemIndex;
            ClearProperties();

            // save current layout
            TraktSettings.RecentAddedEpisodesDefaultLayout = (int)CurrentLayout;

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
                        PreviousUser = CurrentUser;
                        CheckAndPlayEpisode(true);
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
                    // restore current user
                    PreviousUser = CurrentUser;
                    CurrentUser = TraktSettings.Username;
                    base.OnAction(action);
                    break;
                case Action.ActionType.ACTION_PLAY:
                case Action.ActionType.ACTION_MUSIC_PLAY:
                    PreviousUser = CurrentUser;
                    CheckAndPlayEpisode(false);
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

            var selectedEpisodeSummary = selectedItem.TVTag as TraktEpisodeSummary;
            if (selectedEpisodeSummary == null) return;

            var selectedEpisode = selectedEpisodeSummary.Episode;
            if (selectedEpisode == null) return;

            var selectedShow = selectedEpisodeSummary.Show;
            if (selectedShow == null) return;

            IDialogbox dlg = (IDialogbox)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
            if (dlg == null) return;

            dlg.Reset();
            dlg.SetHeading(GUIUtils.PluginName());

            GUIListItem listItem = null;

            // Show Season Information
            listItem = new GUIListItem(Translation.ShowSeasonInfo);
            dlg.Add(listItem);
            listItem.ItemId = (int)ContextMenuItem.ShowSeasonInfo;

            if (!selectedEpisode.InWatchList)
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

            // Add to Custom List
            listItem = new GUIListItem(Translation.AddToList);
            dlg.Add(listItem);
            listItem.ItemId = (int)ContextMenuItem.AddToList;

            // Mark As Watched
            if (!selectedEpisode.Watched)
            {
                listItem = new GUIListItem(Translation.MarkAsWatched);
                dlg.Add(listItem);
                listItem.ItemId = (int)ContextMenuItem.MarkAsWatched;
            }

            // Mark As UnWatched
            if (selectedEpisode.Watched)
            {
                listItem = new GUIListItem(Translation.MarkAsUnWatched);
                dlg.Add(listItem);
                listItem.ItemId = (int)ContextMenuItem.MarkAsUnWatched;
            }

            // Add to Library
            // Don't allow if it will be removed again on next sync
            // movie could be part of a DVD collection
            if (!selectedEpisode.InCollection && !TraktSettings.KeepTraktLibraryClean)
            {
                listItem = new GUIListItem(Translation.AddToLibrary);
                dlg.Add(listItem);
                listItem.ItemId = (int)ContextMenuItem.AddToLibrary;
            }

            if (selectedEpisode.InCollection)
            {
                listItem = new GUIListItem(Translation.RemoveFromLibrary);
                dlg.Add(listItem);
                listItem.ItemId = (int)ContextMenuItem.RemoveFromLibrary;
            }

            // Related Shows
            listItem = new GUIListItem(Translation.RelatedShows + "...");
            dlg.Add(listItem);
            listItem.ItemId = (int)ContextMenuItem.Related;

            // Rate Episode
            listItem = new GUIListItem(Translation.RateEpisode);
            dlg.Add(listItem);
            listItem.ItemId = (int)ContextMenuItem.Rate;

            // Shouts
            listItem = new GUIListItem(Translation.Shouts + "...");
            dlg.Add(listItem);
            listItem.ItemId = (int)ContextMenuItem.Shouts;

            if (TraktHelper.IsOnlineVideosAvailableAndEnabled)
            {
                // Trailers
                listItem = new GUIListItem(Translation.Trailers);
                dlg.Add(listItem);
                listItem.ItemId = (int)ContextMenuItem.Trailers;
            }

            // Change Layout
            listItem = new GUIListItem(Translation.ChangeLayout);
            dlg.Add(listItem);
            listItem.ItemId = (int)ContextMenuItem.ChangeLayout;

            if (!selectedEpisode.InCollection && TraktHelper.IsMpNZBAvailableAndEnabled)
            {
                // Search for movie with mpNZB
                listItem = new GUIListItem(Translation.SearchWithMpNZB);
                dlg.Add(listItem);
                listItem.ItemId = (int)ContextMenuItem.SearchWithMpNZB;
            }

            if (!selectedEpisode.InCollection && TraktHelper.IsMyTorrentsAvailableAndEnabled)
            {
                // Search for movie with MyTorrents
                listItem = new GUIListItem(Translation.SearchTorrent);
                dlg.Add(listItem);
                listItem.ItemId = (int)ContextMenuItem.SearchTorrent;
            }

            // Show Context Menu
            dlg.DoModal(GUIWindowManager.ActiveWindow);
            if (dlg.SelectedId < 0) return;

            switch (dlg.SelectedId)
            {
                case ((int)ContextMenuItem.ShowSeasonInfo):
                    GUIWindowManager.ActivateWindow((int)TraktGUIWindows.ShowSeasons, selectedShow.ToJSON());
                    break;

                case ((int)ContextMenuItem.MarkAsWatched):
                    TraktHelper.MarkEpisodeAsWatched(selectedShow, selectedEpisode);
                    if (selectedEpisode.Plays == 0) selectedEpisode.Plays = 1;
                    selectedEpisode.Watched = true;
                    selectedItem.IsPlayed = true;
                    OnEpisodeSelected(selectedItem, Facade);
                    (Facade.SelectedListItem as GUIEpisodeListItem).Images.NotifyPropertyChanged("Screen");
                    break;

                case ((int)ContextMenuItem.MarkAsUnWatched):
                    TraktHelper.MarkEpisodeAsUnWatched(selectedShow, selectedEpisode);
                    selectedEpisode.Watched = false;
                    selectedItem.IsPlayed = false;
                    OnEpisodeSelected(selectedItem, Facade);
                    (Facade.SelectedListItem as GUIEpisodeListItem).Images.NotifyPropertyChanged("Screen");
                    break;

                case ((int)ContextMenuItem.AddToWatchList):
                    TraktHelper.AddEpisodeToWatchList(selectedShow, selectedEpisode);
                    selectedEpisode.InWatchList = true;
                    OnEpisodeSelected(selectedItem, Facade);
                    (Facade.SelectedListItem as GUIEpisodeListItem).Images.NotifyPropertyChanged("Screen");
                    break;

                case ((int)ContextMenuItem.RemoveFromWatchList):
                    TraktHelper.RemoveEpisodeFromWatchList(selectedShow, selectedEpisode);
                    selectedEpisode.InWatchList = false;
                    OnEpisodeSelected(selectedItem, Facade);
                    (Facade.SelectedListItem as GUIEpisodeListItem).Images.NotifyPropertyChanged("Screen");
                    break;

                case ((int)ContextMenuItem.AddToList):
                    TraktHelper.AddRemoveEpisodeInUserList(selectedShow, selectedEpisode, false);
                    break;

                case ((int)ContextMenuItem.Trailers):
                    PreviousUser = CurrentUser;
                    GUICommon.ShowTVShowTrailersMenu(selectedShow, selectedEpisode);
                    break;

                case ((int)ContextMenuItem.AddToLibrary):
                    TraktHelper.AddEpisodeToLibrary(selectedShow, selectedEpisode);
                    selectedEpisode.InCollection = true;
                    OnEpisodeSelected(selectedItem, Facade);
                    (Facade.SelectedListItem as GUIEpisodeListItem).Images.NotifyPropertyChanged("Screen");
                    break;

                case ((int)ContextMenuItem.RemoveFromLibrary):
                    TraktHelper.RemoveEpisodeFromLibrary(selectedShow, selectedEpisode);
                    selectedEpisode.InCollection = false;
                    OnEpisodeSelected(selectedItem, Facade);
                    (Facade.SelectedListItem as GUIEpisodeListItem).Images.NotifyPropertyChanged("Screen");
                    break;

                case ((int)ContextMenuItem.Related):
                    TraktHelper.ShowRelatedShows(selectedShow);
                    break;

                case ((int)ContextMenuItem.Rate):
                    GUICommon.RateEpisode(selectedShow, selectedEpisode);
                    OnEpisodeSelected(selectedItem, Facade);
                    (Facade.SelectedListItem as GUIEpisodeListItem).Images.NotifyPropertyChanged("Screen");
                    break;

                case ((int)ContextMenuItem.Shouts):
                    TraktHelper.ShowEpisodeShouts(selectedShow, selectedEpisode);
                    break;

                case ((int)ContextMenuItem.ChangeLayout):
                    CurrentLayout = GUICommon.ShowLayoutMenu(CurrentLayout, PreviousSelectedIndex);
                    break;

                case ((int)ContextMenuItem.SearchWithMpNZB):
                    string loadingParam = string.Format("search:{0} S{1}E{2}", selectedShow.Title, selectedEpisode.Season.ToString("D2"), selectedEpisode.Number.ToString("D2"));
                    GUIWindowManager.ActivateWindow((int)ExternalPluginWindows.MpNZB, loadingParam);
                    break;

                case ((int)ContextMenuItem.SearchTorrent):
                    string loadPar = string.Format("{0} S{1}E{2}", selectedShow.Title, selectedEpisode.Season.ToString("D2"), selectedEpisode.Number.ToString("D2"));
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
            GUIListItem selectedItem = this.Facade.SelectedListItem;
            if (selectedItem == null) return;

            var episodeSummary = (TraktEpisodeSummary)selectedItem.TVTag;

            GUICommon.CheckAndPlayEpisode(episodeSummary.Show, episodeSummary.Episode);
        }

        private void LoadRecentlyAddedEpisodes()
        {
            GUIUtils.SetProperty("#Trakt.Items", string.Empty);

            GUIBackgroundTask.Instance.ExecuteInBackgroundAndCallback(() =>
            {
                return RecentlyAddedEpisodes;
            },
            delegate(bool success, object result)
            {
                if (success)
                {
                    IEnumerable<TraktActivity.Activity> activities = result as IEnumerable<TraktActivity.Activity>;
                    SendRecentlyAddedToFacade(activities);
                }
            }, Translation.GettingUserRecentAdded, true);
        }

        private void SendRecentlyAddedToFacade(IEnumerable<TraktActivity.Activity> activities)
        {
            // clear facade
            GUIControl.ClearControl(GetID, Facade.GetID);

            // protected profiles might return null
            if (activities == null || activities.Count() == 0)
            {
                GUIUtils.ShowNotifyDialog(GUIUtils.PluginName(), Translation.UserHasNoRecentAddedEpisodes);
                PreviousUser = CurrentUser;
                CurrentUser = TraktSettings.Username;
                GUIWindowManager.ShowPreviousWindow();
                return;
            }

            int itemId = 0;
            int episodeCount = 0;
            var showImages = new List<TraktImage>();

            // Add each item added
            foreach (var activity in activities)
            {
                // bad data in API
                if (activity.Show == null || activity.Episodes == null)
                    continue;

                // trakt returns an episode array per activity 
                // you may add more than one in bulk
                foreach (var episode in activity.Episodes)
                {
                    // prevent too many episodes loading in facade
                    // its possible that 1 activity item can represent many episodes
                    // e.g. user could of added 400 episodes of The Simpsons
                    if (episodeCount >= 100) continue;

                    var episodeSummary = new TraktEpisodeSummary { Episode = episode, Show = activity.Show };

                    // skip invalid episodes
                    if (episodeSummary.Episode.Number == 0) continue;

                    var item = new GUIEpisodeListItem(episodeSummary.ToString(), (int)TraktGUIWindows.RecentAddedEpisodes);

                    // add images for download
                    var images = new TraktImage
                    {
                        EpisodeImages = episode.Images,
                        ShowImages = activity.Show.Images
                    };
                    showImages.Add(images);

                    // add user added date as second label
                    item.Label2 = activity.Timestamp.FromEpoch().ToShortDateString();
                    item.TVTag = episodeSummary;
                    item.Images = images;
                    item.Date = activity.Timestamp.FromEpoch().ToLongDateString();
                    item.ItemId = Int32.MaxValue - itemId++;
                    item.IsPlayed = episode.Watched;
                    item.IconImage = "defaultTraktEpisode.png";
                    item.IconImageBig = "defaultTraktEpisodeBig.png";
                    item.ThumbnailImage = "defaultTraktEpisodeBig.png";
                    item.OnItemSelected += OnEpisodeSelected;
                    Utils.SetDefaultIcons(item);
                    Facade.Add(item);
                    episodeCount++;
                }
            }

            // set Facade Layout
            Facade.SetCurrentLayout(Enum.GetName(typeof(Layout), CurrentLayout));
            GUIControl.FocusControl(GetID, Facade.GetID);

            if (PreviousSelectedIndex >= episodeCount)
                Facade.SelectIndex(PreviousSelectedIndex - 1);
            else
                Facade.SelectIndex(PreviousSelectedIndex);

            // set facade properties
            GUIUtils.SetProperty("#itemcount", episodeCount.ToString());
            GUIUtils.SetProperty("#Trakt.Items", string.Format("{0} {1}", episodeCount.ToString(), episodeCount > 1 ? Translation.Episodes : Translation.Episode));

            // Download show images Async and set to facade
            GUIEpisodeListItem.GetImages(showImages);
        }

        private void InitProperties()
        {
            // Fanart
            backdrop.GUIImageOne = FanartBackground;
            backdrop.GUIImageTwo = FanartBackground2;
            backdrop.LoadingImage = loadingImage;

            // load recently added for user
            if (string.IsNullOrEmpty(CurrentUser)) CurrentUser = TraktSettings.Username;
            GUICommon.SetProperty("#Trakt.RecentAdded.CurrentUser", CurrentUser);

            // load last layout
            CurrentLayout = (Layout)TraktSettings.RecentAddedEpisodesDefaultLayout;

            // Update Button States
            UpdateButtonState();

            // don't remember previous selected if a different user
            if (PreviousUser != CurrentUser)
                PreviousSelectedIndex = 0;
        }

        private void UpdateButtonState()
        {
            // update layout button label
            GUIControl.SetControlLabel(GetID, layoutButton.GetID, GUICommon.GetLayoutTranslation(CurrentLayout));
        }

        private void ClearProperties()
        {
            GUIUtils.SetProperty("#Trakt.Episode.AddedDate", string.Empty);
            GUICommon.ClearEpisodeProperties();
            GUICommon.ClearShowProperties();
        }

        private void OnEpisodeSelected(GUIListItem item, GUIControl parent)
        {
            PreviousSelectedIndex = Facade.SelectedListItemIndex;

            GUICommon.SetProperty("#Trakt.Episode.AddedDate", (item as GUIEpisodeListItem).Date);

            var episodeSummary = (TraktEpisodeSummary)item.TVTag;

            GUICommon.SetShowProperties(episodeSummary.Show);
            GUICommon.SetEpisodeProperties(episodeSummary.Episode);

            GUIImageHandler.LoadFanart(backdrop, episodeSummary.Show.Images.Fanart.LocalImageFilename(ArtworkType.ShowFanart));
        }
        #endregion
    }
}
