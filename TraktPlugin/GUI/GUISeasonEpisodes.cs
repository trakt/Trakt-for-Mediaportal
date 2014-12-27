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
using TraktPlugin.TraktAPI.Extensions;

namespace TraktPlugin.GUI
{
    public class GUISeasonEpisodes : GUIWindow
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
            Trailers,
            AddToWatchList,
            RemoveFromWatchList,
            AddToLibrary,
            RemoveFromLibrary,
            MarkAsWatched,
            MarkAsUnWatched,
            Rate,
            AddToList,
            Shouts,
            ChangeLayout,
            SearchWithMpNZB,
            SearchTorrent
        }

        #endregion

        #region Constructor

        public GUISeasonEpisodes()
        {
        }

        #endregion

        #region Private Variables

        private Layout CurrentLayout { get; set; }
        DateTime LastRequest = new DateTime();
        int PreviousSelectedIndex = 0;
        TraktShowSummary Show = null;
        TraktSeasonSummary Season = null;
        Dictionary<string, IEnumerable<TraktEpisodeSummary>> Episodes = new Dictionary<string, IEnumerable<TraktEpisodeSummary>>();

        IEnumerable<TraktEpisodeSummary> SeasonEpisodes
        {
            get
            {
                string season = Season.Number.ToString();

                if (!Episodes.Keys.Contains(Show.Ids.Trakt.ToString() + "-" + season) || LastRequest < DateTime.UtcNow.Subtract(new TimeSpan(0, TraktSettings.WebRequestCacheMinutes, 0)))
                {
                    _SeasonEpisodes = TraktAPI.TraktAPI.GetSeasonEpisodes(Show.Ids.Trakt.ToString(), season);
                    if (Episodes.Keys.Contains(Show.Ids.Trakt.ToString() + "-" + season)) Episodes.Remove(Show.Ids.Trakt.ToString() + "-" + season);
                    Episodes.Add(Show.Ids.Trakt.ToString() + "-" + season, _SeasonEpisodes);
                    LastRequest = DateTime.UtcNow;
                    PreviousSelectedIndex = 0;
                }
                return Episodes[Show.Ids.Trakt.ToString() + "-" + season];
            }
        }
        private IEnumerable<TraktEpisodeSummary> _SeasonEpisodes = null;

        #endregion

        #region Base Overrides

        public override int GetID
        {
            get
            {
                return (int)TraktGUIWindows.SeasonEpisodes;
            }
        }

        public override bool Init()
        {
            return Load(GUIGraphicsContext.Skin + @"\Trakt.Season.Episodes.xml");
        }

        protected override void OnPageLoad()
        {
            base.OnPageLoad();

            // Get Loading Parameter
            if (!GetLoadingParameter())
            {
                GUIWindowManager.ActivateWindow(GUIWindowManager.GetPreviousActiveWindow());
                return;
            }

            // Clear GUI Properties
            ClearProperties();

            // Init Properties
            InitProperties();

            // Load WatchList Episodes
            LoadSeasonEpisodes();
        }

        protected override void OnPageDestroy(int new_windowId)
        {
            GUIEpisodeListItem.StopDownload = true;
            PreviousSelectedIndex = Facade.SelectedListItemIndex;
            ClearProperties();

            // save current layout
            TraktSettings.SeasonEpisodesDefaultLayout = (int)CurrentLayout;

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
                        CheckAndPlayEpisode();
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
                case Action.ActionType.ACTION_PLAY:
                case Action.ActionType.ACTION_MUSIC_PLAY:
                    CheckAndPlayEpisode();
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

            var selectedEpisode = selectedItem.TVTag as TraktEpisodeSummary;
            if (selectedEpisode == null) return;

            var dlg = (IDialogbox)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
            if (dlg == null) return;

            dlg.Reset();
            dlg.SetHeading(GUIUtils.PluginName());

            GUIListItem listItem = null;

            listItem = new GUIListItem(Translation.Trailers);
            dlg.Add(listItem);
            listItem.ItemId = (int)ContextMenuItem.Trailers;

            if (selectedEpisode.IsWatched(Show))
            {
                listItem = new GUIListItem(Translation.MarkAsUnWatched);
                dlg.Add(listItem);
                listItem.ItemId = (int)ContextMenuItem.MarkAsUnWatched;
            }
            else
            {
                listItem = new GUIListItem(Translation.MarkAsWatched);
                dlg.Add(listItem);
                listItem.ItemId = (int)ContextMenuItem.MarkAsWatched;
            }

            if (selectedEpisode.IsCollected(Show))
            {
                listItem = new GUIListItem(Translation.RemoveFromLibrary);
                dlg.Add(listItem);
                listItem.ItemId = (int)ContextMenuItem.RemoveFromLibrary;
            }
            else
            {
                listItem = new GUIListItem(Translation.AddToLibrary);
                dlg.Add(listItem);
                listItem.ItemId = (int)ContextMenuItem.AddToLibrary;
            }

            if (selectedEpisode.IsWatchlisted())
            {
                listItem = new GUIListItem(Translation.RemoveFromWatchList);
                dlg.Add(listItem);
                listItem.ItemId = (int)ContextMenuItem.RemoveFromWatchList;
            }
            else
            {
                listItem = new GUIListItem(Translation.AddEpisodeToWatchList);
                dlg.Add(listItem);
                listItem.ItemId = (int)ContextMenuItem.AddToWatchList;
            }

            // Add to Custom List
            listItem = new GUIListItem(Translation.AddToList);
            dlg.Add(listItem);
            listItem.ItemId = (int)ContextMenuItem.AddToList;

            // Rate
            listItem = new GUIListItem(Translation.Rate + "...");
            dlg.Add(listItem);
            listItem.ItemId = (int)ContextMenuItem.Rate;

            // Shouts
            listItem = new GUIListItem(Translation.Shouts);
            dlg.Add(listItem);
            listItem.ItemId = (int)ContextMenuItem.Shouts;

            // Change Layout
            listItem = new GUIListItem(Translation.ChangeLayout);
            dlg.Add(listItem);
            listItem.ItemId = (int)ContextMenuItem.ChangeLayout;

            if (TraktHelper.IsMpNZBAvailableAndEnabled)
            {
                // Search for show with mpNZB
                listItem = new GUIListItem(Translation.SearchWithMpNZB);
                dlg.Add(listItem);
                listItem.ItemId = (int)ContextMenuItem.SearchWithMpNZB;
            }

            if (TraktHelper.IsMyTorrentsAvailableAndEnabled)
            {
                // Search for show with MyTorrents
                listItem = new GUIListItem(Translation.SearchTorrent);
                dlg.Add(listItem);
                listItem.ItemId = (int)ContextMenuItem.SearchTorrent;
            }

            // Show Context Menu
            dlg.DoModal(GUIWindowManager.ActiveWindow);
            if (dlg.SelectedId < 0) return;

            switch (dlg.SelectedId)
            {
                case ((int)ContextMenuItem.Trailers):
                    GUICommon.ShowTVShowTrailersMenu(Show, selectedEpisode);
                    break;

                case ((int)ContextMenuItem.MarkAsWatched):
                    TraktHelper.AddEpisodeToWatchHistory(selectedEpisode);
                    TraktCache.AddEpisodeToWatchHistory(Show, selectedEpisode);
                    Facade.SelectedListItem.IsPlayed = true;
                    OnEpisodeSelected(Facade.SelectedListItem, Facade);
                    (Facade.SelectedListItem as GUIEpisodeListItem).Images.NotifyPropertyChanged("Screen");
                    break;

                case ((int)ContextMenuItem.MarkAsUnWatched):
                    TraktHelper.RemoveEpisodeFromWatchHistory(selectedEpisode);
                    TraktCache.RemoveEpisodeFromWatchHistory(Show, selectedEpisode);
                    Facade.SelectedListItem.IsPlayed = false;
                    OnEpisodeSelected(Facade.SelectedListItem, Facade);
                    (Facade.SelectedListItem as GUIEpisodeListItem).Images.NotifyPropertyChanged("Screen");
                    break;

                case ((int)ContextMenuItem.AddToLibrary):
                    TraktHelper.AddEpisodeToCollection(selectedEpisode);
                    TraktCache.AddEpisodeToCollection(Show, selectedEpisode);
                    OnEpisodeSelected(selectedItem, Facade);
                    (Facade.SelectedListItem as GUIEpisodeListItem).Images.NotifyPropertyChanged("Screen");
                    break;

                case ((int)ContextMenuItem.RemoveFromLibrary):
                    TraktHelper.RemoveEpisodeFromCollection(selectedEpisode);
                    TraktCache.RemoveEpisodeFromCollection(Show, selectedEpisode);
                    OnEpisodeSelected(selectedItem, Facade);
                    (Facade.SelectedListItem as GUIEpisodeListItem).Images.NotifyPropertyChanged("Screen");
                    break;

                case ((int)ContextMenuItem.AddToWatchList):
                    TraktHelper.AddEpisodeToWatchList(selectedEpisode);
                    TraktCache.AddEpisodeToWatchlist(Show, selectedEpisode);
                    OnEpisodeSelected(selectedItem, Facade);
                    (Facade.SelectedListItem as GUIEpisodeListItem).Images.NotifyPropertyChanged("Screen");
                    break;

                case ((int)ContextMenuItem.RemoveFromWatchList):
                    TraktHelper.RemoveEpisodeFromWatchList(selectedEpisode);
                    OnEpisodeSelected(selectedItem, Facade);
                    (Facade.SelectedListItem as GUIEpisodeListItem).Images.NotifyPropertyChanged("Screen");
                    break;

                case ((int)ContextMenuItem.Rate):
                    GUICommon.RateEpisode(Show, selectedEpisode);
                    OnEpisodeSelected(Facade.SelectedListItem, Facade);
                    (Facade.SelectedListItem as GUIEpisodeListItem).Images.NotifyPropertyChanged("Screen");
                    break;

                case ((int)ContextMenuItem.AddToList):
                    TraktHelper.AddRemoveEpisodeInUserList(selectedEpisode, false);
                    break;

                case ((int)ContextMenuItem.Shouts):
                    TraktHelper.ShowEpisodeShouts(Show, selectedEpisode);
                    break;

                case ((int)ContextMenuItem.ChangeLayout):
                    CurrentLayout = GUICommon.ShowLayoutMenu(CurrentLayout, PreviousSelectedIndex);
                    break;

                case ((int)ContextMenuItem.SearchWithMpNZB):
                    string loadingParam = string.Format("search:{0} S{1}E{2}", Show.Title, selectedEpisode.Season.ToString("D2"), selectedEpisode.Number.ToString("D2"));
                    GUIWindowManager.ActivateWindow((int)ExternalPluginWindows.MpNZB, loadingParam);
                    break;

                case ((int)ContextMenuItem.SearchTorrent):
                    string loadPar = string.Format("{0} S{1}E{2}", Show.Title, selectedEpisode.Season.ToString("D2"), selectedEpisode.Number.ToString("D2"));
                    GUIWindowManager.ActivateWindow((int)ExternalPluginWindows.MyTorrents, loadPar);
                    break;

                default:
                    break;
            }

            base.OnShowContextMenu();
        }

        #endregion

        #region Private Methods

        private void CheckAndPlayEpisode()
        {
            var selectedItem = this.Facade.SelectedListItem;
            if (selectedItem == null) return;

            var episode = selectedItem.TVTag as TraktEpisodeSummary;

            GUICommon.CheckAndPlayEpisode(Show, episode);
        }

        private void LoadSeasonEpisodes()
        {
            GUIUtils.SetProperty("#Trakt.Items", string.Empty);

            GUIBackgroundTask.Instance.ExecuteInBackgroundAndCallback(() =>
            {
                return SeasonEpisodes;
            },
            delegate(bool success, object result)
            {
                if (success)
                {
                    var episodes = result as IEnumerable<TraktEpisodeSummary>;
                    SendSeasonEpisodesToFacade(episodes);
                }
            }, Translation.GettingEpisodes, true);
        }

        private void SendSeasonEpisodesToFacade(IEnumerable<TraktEpisodeSummary> episodes)
        {
            // clear facade
            GUIControl.ClearControl(GetID, Facade.GetID);

            if (episodes == null || episodes.Count() == 0)
            {
                GUIUtils.ShowNotifyDialog(GUIUtils.PluginName(), Translation.NoEpisodesInSeason);
                GUIWindowManager.ShowPreviousWindow();
                return;
            }

            // Set Common Show Properties
            GUICommon.SetShowProperties(Show);

            int itemCount = 0;
            var episodeImages = new List<GUITraktImage>();

            foreach (var episode in episodes)
            {
                // use episode short string
                string itemLabel = string.Format("{0}. {1}", episode.Number.ToString(), string.IsNullOrEmpty(episode.Title) ? Translation.Episode + " " + episode.Number.ToString() : episode.Title);

                // add image for download
                var images = new GUITraktImage
                {
                    EpisodeImages = episode.Images,
                    ShowImages = Show.Images
                };

                episodeImages.Add(images);

                var item = new GUIEpisodeListItem(itemLabel, (int)TraktGUIWindows.SeasonEpisodes);

                item.Label2 = episode.FirstAired == null ? " " : episode.FirstAired.FromISO8601().ToLocalTime().ToShortDateString();
                item.TVTag = episode;
                item.Show = Show;
                item.Episode = episode;
                item.IsPlayed = episode.IsWatched(Show);
                item.Images = images;
                item.ItemId = Int32.MaxValue - itemCount;
                item.IconImage = "defaultTraktEpisode.png";
                item.IconImageBig = "defaultTraktEpisodeBig.png";
                item.ThumbnailImage = "defaultTraktEpisodeBig.png";
                item.OnItemSelected += OnEpisodeSelected;
                Utils.SetDefaultIcons(item);
                Facade.Add(item);
                itemCount++;
            }

            // Set Facade Layout
            Facade.SetCurrentLayout(Enum.GetName(typeof(Layout), CurrentLayout));
            GUIControl.FocusControl(GetID, Facade.GetID);

            if (PreviousSelectedIndex >= itemCount)
                Facade.SelectIndex(PreviousSelectedIndex - 1);
            else
                Facade.SelectIndex(PreviousSelectedIndex);

            // set facade properties
            GUIUtils.SetProperty("#itemcount", itemCount.ToString());
            GUIUtils.SetProperty("#Trakt.Items", string.Format("{0} {1}", itemCount.ToString(), itemCount > 1 ? Translation.Episodes : Translation.Episode));

            // Download episode images Async and set to facade
            GUIEpisodeListItem.GetImages(episodeImages);
        }

        private bool GetLoadingParameter()
        {
            if (_loadParameter == null)
            {
                // maybe re-loading, so check previous window id
                if (Show != null && Show.Ids.Trakt != null && Season != null)
                    return true;

                return false;
            }
            
            var loadingParam = _loadParameter.FromJSON<SeasonLoadingParameter>();
            if (loadingParam == null) return false;

            // reset previous selected index
            if (Show != null && Season != null)
            {
                if (Show.Title != loadingParam.Show.Title || Season.Number != loadingParam.Season.Number)
                    PreviousSelectedIndex = 0;
            }

            Show = loadingParam.Show;
            Season = loadingParam.Season;
            if (Show == null || Show.Ids.Trakt == null || Season == null)
                return false;
            
            return true;
        }

        private void InitProperties()
        {
            // only set property if file exists
            // if we set now and download later, image will not set to skin
            if (File.Exists(Show.Images.Fanart.LocalImageFilename(ArtworkType.ShowFanart)))
                GUIUtils.SetProperty("#Trakt.Show.Fanart", Show.Images.Fanart.LocalImageFilename(ArtworkType.ShowFanart));

            // load last layout
            CurrentLayout = (Layout)TraktSettings.SeasonEpisodesDefaultLayout;
            
            // update button label
            if (layoutButton != null)
                GUIControl.SetControlLabel(GetID, layoutButton.GetID, GUICommon.GetLayoutTranslation(CurrentLayout));
        }

        private void ClearProperties()
        {
            GUICommon.ClearShowProperties();
            GUICommon.ClearSeasonProperties();
            GUICommon.ClearEpisodeProperties();
        }

        private void OnEpisodeSelected(GUIListItem item, GUIControl parent)
        {
            var episode = item.TVTag as TraktEpisodeSummary;
            if (episode == null) return;

            PreviousSelectedIndex = Facade.SelectedListItemIndex;

            GUICommon.SetEpisodeProperties(Show, episode);

            // set season properties as well, we may show a flattened view
            // with all episodes in a show
            GUICommon.SetSeasonProperties(Show, Season);
            GUICommon.SetShowProperties(Show);
        }
        #endregion
    }

    public class SeasonLoadingParameter
    {
        public TraktShowSummary Show { get; set; }
        public TraktSeasonSummary Season { get; set; }
    }
}
