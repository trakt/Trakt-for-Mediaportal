using System;
using System.Collections.Generic;
using System.Linq;
using MediaPortal.GUI.Library;
using MediaPortal.Util;
using TraktPlugin.Extensions;
using TraktPlugin.TraktAPI.DataStructures;
using TraktPlugin.TraktAPI.Enums;
using TraktPlugin.TraktAPI.Extensions;
using Action = MediaPortal.GUI.Library.Action;

namespace TraktPlugin.GUI
{
    public class GUIRecentWatchedEpisodes : GUIWindow
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

        public GUIRecentWatchedEpisodes()
        {
            backdrop = new ImageSwapper();
            backdrop.PropertyOne = "#Trakt.RecentWatchedEpisodes.Fanart.1";
            backdrop.PropertyTwo = "#Trakt.RecentWatchedEpisodes.Fanart.2";
        }

        #endregion

        #region Private Variables

        static int PreviousSelectedIndex { get; set; }
        static DateTime LastRequest = new DateTime();
        string PreviousUser = null;
        Layout CurrentLayout { get; set; }
        ImageSwapper backdrop;
        Dictionary<string, IEnumerable<TraktEpisodeHistory>> userRecentlyWatchedEpisodes = new Dictionary<string, IEnumerable<TraktEpisodeHistory>>();

        IEnumerable<TraktEpisodeHistory> RecentlyWatchedEpisodes
        {
            get
            {
                if (!userRecentlyWatchedEpisodes.Keys.Contains(CurrentUser) || LastRequest < DateTime.UtcNow.Subtract(new TimeSpan(0, TraktSettings.WebRequestCacheMinutes, 0)))
                {
                    var recentlyWatched = TraktAPI.TraktAPI.GetUsersEpisodeWatchedHistory(CurrentUser, 1, TraktSettings.MaxUserWatchedEpisodesRequest);

                    _RecentlyWatchedEpisodes = recentlyWatched;
                    if (userRecentlyWatchedEpisodes.Keys.Contains(CurrentUser)) userRecentlyWatchedEpisodes.Remove(CurrentUser);
                    userRecentlyWatchedEpisodes.Add(CurrentUser, _RecentlyWatchedEpisodes);
                    LastRequest = DateTime.UtcNow;
                    PreviousSelectedIndex = 0;
                }
                return userRecentlyWatchedEpisodes[CurrentUser];
            }
        }
        private IEnumerable<TraktEpisodeHistory> _RecentlyWatchedEpisodes = null;

        #endregion

        #region Public Properties

        public static string CurrentUser { get; set; }

        #endregion

        #region Base Overrides

        public override int GetID
        {
            get
            {
                return (int)TraktGUIWindows.RecentWatchedEpisodes;
            }
        }

        public override bool Init()
        {
            return Load(GUIGraphicsContext.Skin + @"\Trakt.RecentWatched.Episodes.xml");
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

            // Load Watched History
            LoadRecentlyWatchedEpisodes();
        }

        protected override void OnPageDestroy(int new_windowId)
        {
            GUIEpisodeListItem.StopDownload = true;
            PreviousSelectedIndex = Facade.SelectedListItemIndex;
            ClearProperties();

            // save current layout
            TraktSettings.RecentWatchedEpisodesDefaultLayout = (int)CurrentLayout;

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

            var selectedRecent = selectedItem.TVTag as TraktEpisodeHistory;
            if (selectedRecent == null) return;

            var selectedEpisode = selectedRecent.Episode;
            if (selectedEpisode == null) return;

            var selectedShow = selectedRecent.Show;
            if (selectedShow == null) return;

            var dlg = (IDialogbox)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
            if (dlg == null) return;

            dlg.Reset();
            dlg.SetHeading(GUIUtils.PluginName());

            GUIListItem listItem = null;

            // Show Season Information
            listItem = new GUIListItem(Translation.ShowSeasonInfo);
            dlg.Add(listItem);
            listItem.ItemId = (int)ContextMenuItem.ShowSeasonInfo;

            if (!selectedEpisode.IsWatchlisted())
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
            if (!selectedEpisode.IsWatched(selectedShow))
            {
                listItem = new GUIListItem(Translation.MarkAsWatched);
                dlg.Add(listItem);
                listItem.ItemId = (int)ContextMenuItem.MarkAsWatched;
            }

            // Mark As UnWatched
            if (selectedEpisode.IsWatched(selectedShow))
            {
                listItem = new GUIListItem(Translation.MarkAsUnWatched);
                dlg.Add(listItem);
                listItem.ItemId = (int)ContextMenuItem.MarkAsUnWatched;
            }

            // Add to Collection
            if (!selectedEpisode.IsCollected(selectedShow) && !TraktSettings.KeepTraktLibraryClean)
            {
                listItem = new GUIListItem(Translation.AddToLibrary);
                dlg.Add(listItem);
                listItem.ItemId = (int)ContextMenuItem.AddToLibrary;
            }

            if (selectedEpisode.IsCollected(selectedShow))
            {
                listItem = new GUIListItem(Translation.RemoveFromLibrary);
                dlg.Add(listItem);
                listItem.ItemId = (int)ContextMenuItem.RemoveFromLibrary;
            }

            // Related Shows
            listItem = new GUIListItem(Translation.RelatedShows);
            dlg.Add(listItem);
            listItem.ItemId = (int)ContextMenuItem.Related;

            // Rate Episode
            listItem = new GUIListItem(Translation.RateEpisode);
            dlg.Add(listItem);
            listItem.ItemId = (int)ContextMenuItem.Rate;

            // Shouts
            listItem = new GUIListItem(Translation.Comments);
            dlg.Add(listItem);
            listItem.ItemId = (int)ContextMenuItem.Shouts;

            if (TraktHelper.IsTrailersAvailableAndEnabled)
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

            if (!selectedEpisode.IsCollected(selectedShow) && TraktHelper.IsMpNZBAvailableAndEnabled)
            {
                // Search for movie with mpNZB
                listItem = new GUIListItem(Translation.SearchWithMpNZB);
                dlg.Add(listItem);
                listItem.ItemId = (int)ContextMenuItem.SearchWithMpNZB;
            }

            if (!selectedEpisode.IsCollected(selectedShow) && TraktHelper.IsMyTorrentsAvailableAndEnabled)
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
                    TraktHelper.AddEpisodeToWatchHistory(selectedEpisode);
                    TraktCache.AddEpisodeToWatchHistory(selectedShow, selectedEpisode);
                    selectedItem.IsPlayed = true;
                    OnEpisodeSelected(selectedItem, Facade);
                    (Facade.SelectedListItem as GUIEpisodeListItem).Images.NotifyPropertyChanged("Screen");
                    break;

                case ((int)ContextMenuItem.MarkAsUnWatched):
                    TraktHelper.RemoveEpisodeFromWatchHistory(selectedEpisode);
                    TraktCache.RemoveEpisodeFromWatchHistory(selectedShow, selectedEpisode);
                    selectedItem.IsPlayed = false;
                    OnEpisodeSelected(selectedItem, Facade);
                    (Facade.SelectedListItem as GUIEpisodeListItem).Images.NotifyPropertyChanged("Screen");
                    break;

                case ((int)ContextMenuItem.AddToWatchList):
                    TraktHelper.AddEpisodeToWatchList(selectedEpisode);
                    TraktCache.AddEpisodeToWatchlist(selectedShow, selectedEpisode);
                    OnEpisodeSelected(selectedItem, Facade);
                    (Facade.SelectedListItem as GUIEpisodeListItem).Images.NotifyPropertyChanged("Screen");
                    break;

                case ((int)ContextMenuItem.RemoveFromWatchList):
                    TraktHelper.RemoveEpisodeFromWatchList(selectedEpisode);
                    OnEpisodeSelected(selectedItem, Facade);
                    (Facade.SelectedListItem as GUIEpisodeListItem).Images.NotifyPropertyChanged("Screen");
                    break;

                case ((int)ContextMenuItem.AddToList):
                    TraktHelper.AddRemoveEpisodeInUserList(selectedEpisode, false);
                    break;

                case ((int)ContextMenuItem.Trailers):
                    PreviousUser = CurrentUser;
                    GUICommon.ShowTVShowTrailersMenu(selectedShow, selectedEpisode);
                    break;

                case ((int)ContextMenuItem.AddToLibrary):
                    TraktHelper.AddEpisodeToCollection(selectedEpisode);
                    TraktCache.AddEpisodeToCollection(selectedShow, selectedEpisode);
                    OnEpisodeSelected(selectedItem, Facade);
                    (Facade.SelectedListItem as GUIEpisodeListItem).Images.NotifyPropertyChanged("Screen");
                    break;

                case ((int)ContextMenuItem.RemoveFromLibrary):
                    TraktHelper.RemoveEpisodeFromCollection(selectedEpisode);
                    TraktCache.RemoveEpisodeFromCollection(selectedShow, selectedEpisode);
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

            var selectedRecent = selectedItem.TVTag as TraktEpisodeHistory;
            if (selectedRecent == null) return;

            GUICommon.CheckAndPlayEpisode(selectedRecent.Show, selectedRecent.Episode);
        }

        private void LoadRecentlyWatchedEpisodes()
        {
            GUIUtils.SetProperty("#Trakt.Items", string.Empty);

            GUIBackgroundTask.Instance.ExecuteInBackgroundAndCallback(() =>
            {
                return RecentlyWatchedEpisodes;
            },
            delegate(bool success, object result)
            {
                if (success)
                {
                    var recentlyWatched = result as IEnumerable<TraktEpisodeHistory>;
                    SendRecentlyWatchedToFacade(recentlyWatched);
                }
            }, Translation.GettingUserWatchedHistory, true);
        }

        private void SendRecentlyWatchedToFacade(IEnumerable<TraktEpisodeHistory> recentlyWatched)
        {
            // clear facade
            GUIControl.ClearControl(GetID, Facade.GetID);

            // protected profiles might return null
            if (recentlyWatched == null || recentlyWatched.Count() == 0)
            {
                GUIUtils.ShowNotifyDialog(GUIUtils.PluginName(), Translation.UserHasNotWatchedEpisodes);
                PreviousUser = CurrentUser;
                CurrentUser = TraktSettings.Username;
                GUIWindowManager.ShowPreviousWindow();
                return;
            }

            int itemId = 0;
            var showImages = new List<GUITraktImage>();

            // Add each item watched
            foreach (var recent in recentlyWatched)
            {
                // bad data in API
                if (recent.Show == null || recent.Episode == null)
                    continue;

                // skip invalid episodes
                if (recent.Episode.Number == 0)
                    continue;

                string episodeName = string.Format("{0} - {1}x{2} - {3}", recent.Show.Title, recent.Episode.Season, recent.Episode.Number, recent.Episode.Title ?? string.Format("{0} {1}", Translation.Episode, recent.Episode.Number));

                var item = new GUIEpisodeListItem(episodeName, (int)TraktGUIWindows.RecentWatchedEpisodes);

                // add images for download
                var images = new GUITraktImage
                {
                    EpisodeImages = recent.Episode.Images,
                    ShowImages = recent.Show.Images
                };
                showImages.Add(images);

                // add user watched date as second label
                item.Label2 = recent.WatchedAt.ToPrettyDateTime();
                item.Date = recent.WatchedAt.FromISO8601().ToLongDateString();
                item.TVTag = recent;
                item.Episode = recent.Episode;
                item.Show = recent.Show;
                item.Images = images;
                item.ItemId = Int32.MaxValue - itemId;     
                item.IsPlayed = CurrentUser != TraktSettings.Username ? recent.Episode.IsWatched(recent.Show) : false;
                item.IconImage = "defaultTraktEpisode.png";
                item.IconImageBig = "defaultTraktEpisodeBig.png";
                item.ThumbnailImage = "defaultTraktEpisodeBig.png";
                item.OnItemSelected += OnEpisodeSelected;
                Utils.SetDefaultIcons(item);
                Facade.Add(item);
                itemId++;
            }

            // set Facade Layout
            Facade.SetCurrentLayout(Enum.GetName(typeof(Layout), CurrentLayout));
            GUIControl.FocusControl(GetID, Facade.GetID);

            if (PreviousSelectedIndex >= recentlyWatched.Count())
                Facade.SelectIndex(PreviousSelectedIndex - 1);
            else
                Facade.SelectIndex(PreviousSelectedIndex);

            // set facade properties
            GUIUtils.SetProperty("#itemcount", recentlyWatched.Count().ToString());
            GUIUtils.SetProperty("#Trakt.Items", string.Format("{0} {1}", recentlyWatched.Count().ToString(), recentlyWatched.Count() > 1 ? Translation.Episodes : Translation.Episode));

            // Download show images Async and set to facade
            GUIEpisodeListItem.GetImages(showImages);
        }

        private void InitProperties()
        {
            // Fanart
            backdrop.GUIImageOne = FanartBackground;
            backdrop.GUIImageTwo = FanartBackground2;
            backdrop.LoadingImage = loadingImage;

            // load watched history for user
            if (string.IsNullOrEmpty(CurrentUser)) CurrentUser = TraktSettings.Username;
            GUICommon.SetProperty("#Trakt.RecentWatched.CurrentUser", CurrentUser);

            // load last layout
            CurrentLayout = (Layout)TraktSettings.RecentWatchedEpisodesDefaultLayout;

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
            GUIUtils.SetProperty("#Trakt.Episode.WatchedDate", string.Empty);
            GUIUtils.SetProperty("#Trakt.Episode.Action", string.Empty);
            GUICommon.ClearEpisodeProperties();
            GUICommon.ClearShowProperties();
        }

        private void OnEpisodeSelected(GUIListItem item, GUIControl parent)
        {
            var recent = item.TVTag as TraktEpisodeHistory;
            if (recent == null) return;

            PreviousSelectedIndex = Facade.SelectedListItemIndex;

            GUICommon.SetProperty("#Trakt.Episode.WatchedDate", (item as GUIEpisodeListItem).Date);
            GUICommon.SetProperty("#Trakt.Episode.Action", recent.Action);
            GUICommon.SetShowProperties(recent.Show);
            GUICommon.SetEpisodeProperties(recent.Show, recent.Episode);

            GUIImageHandler.LoadFanart(backdrop, recent.Show.Images.Fanart.LocalImageFilename(ArtworkType.ShowFanart));
        }
        #endregion
    }
}
