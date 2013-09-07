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
    public class GUISearchEpisodes : GUIWindow
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

        public GUISearchEpisodes()
        {
            backdrop = new ImageSwapper();
            backdrop.PropertyOne = "#Trakt.SearchEpisodes.Fanart.1";
            backdrop.PropertyTwo = "#Trakt.SearchEpisodes.Fanart.2";
        }

        #endregion

        #region Public Variables

        public static string SearchTerm { get; set; }
        public static IEnumerable<TraktEpisodeSummary> Episodes { get; set; }

        #endregion

        #region Private Variables

        bool StopDownload { get; set; }
        bool SearchTermChanged { get; set; }
        string PreviousSearchTerm { get; set; }
        Layout CurrentLayout { get; set; }
        ImageSwapper backdrop;
        int PreviousSelectedIndex = 0;

        #endregion

        #region Base Overrides

        public override int GetID
        {
            get
            {
                return (int)TraktGUIWindows.SearchEpisodes;
            }
        }

        public override bool Init()
        {
            return Load(GUIGraphicsContext.Skin + @"\Trakt.Search.Episodes.xml");
        }

        protected override void OnPageLoad()
        {
            base.OnPageLoad();

            if (string.IsNullOrEmpty(_loadParameter) && Episodes == null)
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
            TraktSettings.SearchEpisodesDefaultLayout = (int)CurrentLayout;

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
                case Action.ActionType.ACTION_PLAY:
                case Action.ActionType.ACTION_MUSIC_PLAY:
                    if (!GUIBackgroundTask.Instance.IsBusy)
                    {
                        CheckAndPlayEpisode(false);
                    }
                    break;

                case Action.ActionType.ACTION_PREVIOUS_MENU:
                    // clear search criteria if going back
                    SearchTerm = string.Empty;
                    Episodes = null;
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
                    selectedEpisode.Images.NotifyPropertyChanged("EpisodeImageFilename");
                    break;

                case ((int)ContextMenuItem.MarkAsUnWatched):
                    TraktHelper.MarkEpisodeAsUnWatched(selectedShow, selectedEpisode);
                    selectedEpisode.Watched = false;
                    selectedItem.IsPlayed = false;
                    OnEpisodeSelected(selectedItem, Facade);
                    selectedEpisode.Images.NotifyPropertyChanged("EpisodeImageFilename");
                    break;

                case ((int)ContextMenuItem.AddToWatchList):
                    TraktHelper.AddEpisodeToWatchList(selectedShow, selectedEpisode);
                    selectedEpisode.InWatchList = true;
                    OnEpisodeSelected(selectedItem, Facade);
                    selectedEpisode.Images.NotifyPropertyChanged("EpisodeImageFilename");
                    break;

                case ((int)ContextMenuItem.RemoveFromWatchList):
                    TraktHelper.RemoveEpisodeFromWatchList(selectedShow, selectedEpisode);
                    selectedEpisode.InWatchList = false;
                    OnEpisodeSelected(selectedItem, Facade);
                    selectedEpisode.Images.NotifyPropertyChanged("EpisodeImageFilename");
                    break;

                case ((int)ContextMenuItem.AddToList):
                    TraktHelper.AddRemoveEpisodeInUserList(selectedShow, selectedEpisode, false);
                    break;

                case ((int)ContextMenuItem.Trailers):
                    GUICommon.ShowTVShowTrailersMenu(selectedShow, selectedEpisode);
                    break;

                case ((int)ContextMenuItem.AddToLibrary):
                    TraktHelper.AddEpisodeToLibrary(selectedShow, selectedEpisode);
                    selectedEpisode.InCollection = true;
                    OnEpisodeSelected(selectedItem, Facade);
                    selectedEpisode.Images.NotifyPropertyChanged("EpisodeImageFilename");
                    break;

                case ((int)ContextMenuItem.RemoveFromLibrary):
                    TraktHelper.RemoveEpisodeFromLibrary(selectedShow, selectedEpisode);
                    selectedEpisode.InCollection = false;
                    OnEpisodeSelected(selectedItem, Facade);
                    selectedEpisode.Images.NotifyPropertyChanged("EpisodeImageFilename");
                    break;

                case ((int)ContextMenuItem.Related):
                    TraktHelper.ShowRelatedShows(selectedShow);
                    break;

                case ((int)ContextMenuItem.Rate):
                    GUICommon.RateEpisode(selectedShow, selectedEpisode);
                    OnEpisodeSelected(selectedItem, Facade);
                    selectedEpisode.Images.NotifyPropertyChanged("EpisodeImageFilename");
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
            var selectedItem = this.Facade.SelectedListItem;
            if (selectedItem == null) return;

            var selectedEpisodeSummary = selectedItem.TVTag as TraktEpisodeSummary;
            GUICommon.CheckAndPlayEpisode(selectedEpisodeSummary.Show, selectedEpisodeSummary.Episode);
        }

        private void LoadSearchResults()
        {
            GUIUtils.SetProperty("#Trakt.Items", string.Empty);

            GUIBackgroundTask.Instance.ExecuteInBackgroundAndCallback(() =>
            {
                // Episodes can be null if invoking search from loading parameters
                // Internally we set the Episodes on load
                if (Episodes == null && !string.IsNullOrEmpty(SearchTerm))
                {
                    // search online
                    Episodes = TraktAPI.TraktAPI.SearchEpisodes(SearchTerm);
                }
                return Episodes;
            },
            delegate(bool success, object result)
            {
                if (success)
                {
                    var episodes = result as IEnumerable<TraktEpisodeSummary>;
                    SendSearchResultsToFacade(episodes);
                }
            }, Translation.GettingSearchResults, true);
        }

        private void SendSearchResultsToFacade(IEnumerable<TraktEpisodeSummary> episodes)
        {
            // clear facade
            GUIControl.ClearControl(GetID, Facade.GetID);

            if (episodes == null || episodes.Count() == 0)
            {
                GUIUtils.ShowNotifyDialog(GUIUtils.PluginName(), Translation.NoSearchResultsFound);
                GUIWindowManager.ShowPreviousWindow();
                return;
            }

            int itemId = 0;
            var showImages = new List<TraktImage>();

            // Add each show
            foreach (var episodeSummary in episodes)
            {
                // add images for download
                var images = new TraktImage
                {
                    EpisodeImages = episodeSummary.Episode.Images,
                    ShowImages = episodeSummary.Show.Images
                };
                showImages.Add(images);

                string itemName = string.Format("{0} - {1}x{2}{3}", episodeSummary.Show.Title, episodeSummary.Episode.Season.ToString(), episodeSummary.Episode.Number.ToString(), string.IsNullOrEmpty(episodeSummary.Episode.Title) ? string.Empty : " - " + episodeSummary.Episode.Title);
                GUITraktSearchEpisodeListItem item = new GUITraktSearchEpisodeListItem(itemName);

                item.Label2 = episodeSummary.Show.Year.ToString();
                item.TVTag = episodeSummary;
                item.Item = images;
                item.IsPlayed = episodeSummary.Episode.Watched;
                item.ItemId = Int32.MaxValue - itemId;
                item.IconImage = "defaultTraktEpisode.png";
                item.IconImageBig = "defaultTraktEpisodeBig.png";
                item.ThumbnailImage = "defaultTraktEpisodeBig.png";
                item.OnItemSelected += OnEpisodeSelected;
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
            GUIUtils.SetProperty("#itemcount", episodes.Count().ToString());
            GUIUtils.SetProperty("#Trakt.Items", string.Format("{0} {1}", episodes.Count().ToString(), episodes.Count() > 1 ? Translation.Episodes : Translation.Episode));

            // Download images Async and set to facade
            GetImages(showImages);
        }

        private void InitProperties()
        {
            // Fanart
            backdrop.GUIImageOne = FanartBackground;
            backdrop.GUIImageTwo = FanartBackground2;
            backdrop.LoadingImage = loadingImage;

            // set search term from loading parameter
            if (!string.IsNullOrEmpty(_loadParameter))
            {
                TraktLogger.Info("Episode Search Loading Parameter: {0}", _loadParameter);
                SearchTerm = _loadParameter;
            }

            // remember previous search term
            SearchTermChanged = false;
            if (PreviousSearchTerm != SearchTerm) SearchTermChanged = true;
            PreviousSearchTerm = SearchTerm;

            // set context property
            GUIUtils.SetProperty("#Trakt.Search.SearchTerm", SearchTerm);

            // load last layout
            CurrentLayout = (Layout)TraktSettings.SearchShowsDefaultLayout;

            // update button label
            GUIControl.SetControlLabel(GetID, layoutButton.GetID, GUICommon.GetLayoutTranslation(CurrentLayout));
        }

        private void ClearProperties()
        {
            GUIUtils.SetProperty("#Trakt.Search.SearchTerm", string.Empty);
            GUICommon.ClearShowProperties();
            GUICommon.ClearEpisodeProperties();
        }

        private void PublishSkinProperties(TraktEpisodeSummary episodeSummary)
        {
            GUICommon.SetShowProperties(episodeSummary.Show);
            GUICommon.SetEpisodeProperties(episodeSummary.Episode);
        }

        private void OnEpisodeSelected(GUIListItem item, GUIControl parent)
        {
            PreviousSelectedIndex = Facade.SelectedListItemIndex;

            var episodeSummary = item.TVTag as TraktEpisodeSummary;
            PublishSkinProperties(episodeSummary);
            GUIImageHandler.LoadFanart(backdrop, episodeSummary.Show.Images.FanartImageFilename);
        }

        private void GetImages(List<TraktImage> itemsWithThumbs)
        {
            StopDownload = false;

            // split the downloads in 5+ groups and do multithreaded downloading
            int groupSize = (int)Math.Max(1, Math.Floor((double)itemsWithThumbs.Count / 5));
            int groups = (int)Math.Ceiling((double)itemsWithThumbs.Count() / groupSize);

            for (int i = 0; i < groups; i++)
            {
                var groupList = new List<TraktImage>();
                for (int j = groupSize * i; j < groupSize * i + (groupSize * (i + 1) > itemsWithThumbs.Count ? itemsWithThumbs.Count - groupSize * i : groupSize); j++)
                {
                    groupList.Add(itemsWithThumbs[j]);
                }

                new Thread(delegate(object o)
                {
                    var items = (List<TraktImage>)o;
                    foreach (var item in items)
                    {
                        #region Episode Thumbnail
                        // stop download if we have exited window
                        if (StopDownload) break;

                        string remoteThumb = item.EpisodeImages.Screen;
                        string localThumb = item.EpisodeImages.EpisodeImageFilename;

                        if (!string.IsNullOrEmpty(remoteThumb) && !string.IsNullOrEmpty(localThumb))
                        {
                            if (GUIImageHandler.DownloadImage(remoteThumb, localThumb))
                            {
                                // notify that image has been downloaded
                                item.NotifyPropertyChanged("EpisodeImageFilename");
                            }
                        }
                        #endregion

                        #region Fanart
                        // stop download if we have exited window
                        if (StopDownload) break;
                        if (!TraktSettings.DownloadFanart) continue;

                        string remoteFanart = item.ShowImages.Fanart;
                        string localFanart = item.ShowImages.FanartImageFilename;

                        if (!string.IsNullOrEmpty(remoteFanart) && !string.IsNullOrEmpty(localFanart))
                        {
                            if (GUIImageHandler.DownloadImage(remoteFanart, localFanart))
                            {
                                // notify that image has been downloaded
                                item.NotifyPropertyChanged("FanartImageFilename");
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

    public class GUITraktSearchEpisodeListItem : GUIListItem
    {
        public GUITraktSearchEpisodeListItem(string strLabel) : base(strLabel) { }

        public object Item
        {
            get { return _Item; }
            set
            {
                _Item = value;
                INotifyPropertyChanged notifier = value as INotifyPropertyChanged;
                if (notifier != null) notifier.PropertyChanged += (s, e) =>
                {
                    if (s is TraktImage && e.PropertyName == "EpisodeImageFilename")
                        SetImageToGui((s as TraktImage).EpisodeImages.EpisodeImageFilename);
                    if (s is TraktImage && e.PropertyName == "FanartImageFilename")
                        this.UpdateItemIfSelected((int)TraktGUIWindows.SearchEpisodes, ItemId);
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

            // determine the overlay to add to thumbnail
            var episodeSummary = TVTag as TraktEpisodeSummary;
            if (episodeSummary == null) return;

            var show = episodeSummary.Show;
            var episode = episodeSummary.Episode;

            MainOverlayImage mainOverlay = MainOverlayImage.None;

            if (episode.InWatchList)
                mainOverlay = MainOverlayImage.Watchlist;
            else if (episode.Watched)
                mainOverlay = MainOverlayImage.Seenit;

            // add additional overlay if applicable
            if (episode.InCollection)
                mainOverlay |= MainOverlayImage.Library;

            RatingOverlayImage ratingOverlay = GUIImageHandler.GetRatingOverlay(episode.RatingAdvanced);

            // get a reference to a MediaPortal Texture Identifier
            string suffix = mainOverlay.ToString().Replace(", ", string.Empty) + Enum.GetName(typeof(RatingOverlayImage), ratingOverlay);
            string texture = GUIImageHandler.GetTextureIdentFromFile(imageFilePath, suffix);

            // build memory image
            Image memoryImage = null;
            if (mainOverlay != MainOverlayImage.None || ratingOverlay != RatingOverlayImage.None)
            {
                memoryImage = GUIImageHandler.DrawOverlayOnEpisodeThumb(imageFilePath, mainOverlay, ratingOverlay, new Size(400, 225));
                if (memoryImage == null) return;

                // load texture into facade item
                if (GUITextureManager.LoadFromMemory(memoryImage, texture, 0, 0, 0) > 0)
                {
                    ThumbnailImage = texture;
                    IconImage = texture;
                    IconImageBig = texture;
                }
            }
            else
            {
                ThumbnailImage = imageFilePath;
                IconImage = imageFilePath;
                IconImageBig = imageFilePath;
            }

            // if selected and is current window force an update of thumbnail
            this.UpdateItemIfSelected((int)TraktGUIWindows.SearchEpisodes, ItemId);
        }
    }
}