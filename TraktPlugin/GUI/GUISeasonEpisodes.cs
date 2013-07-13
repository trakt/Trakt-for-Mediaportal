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

        bool StopDownload { get; set; }
        private Layout CurrentLayout { get; set; }
        DateTime LastRequest = new DateTime();
        int PreviousSelectedIndex = 0;
        TraktShow Show = null;
        TraktShowSeason Season = null;
        Dictionary<string, IEnumerable<TraktEpisode>> Episodes = new Dictionary<string, IEnumerable<TraktEpisode>>();

        IEnumerable<TraktEpisode> SeasonEpisodes
        {
            get
            {
                string season = Season.Season.ToString();

                if (!Episodes.Keys.Contains(Show.Tvdb + "-" + season) || LastRequest < DateTime.UtcNow.Subtract(new TimeSpan(0, TraktSettings.WebRequestCacheMinutes, 0)))
                {
                    _SeasonEpisodes = TraktAPI.TraktAPI.GetSeasonEpisodes(Show.Tvdb, season);
                    if (Episodes.Keys.Contains(Show.Tvdb + "-" + season)) Episodes.Remove(Show.Tvdb + "-" + season);
                    Episodes.Add(Show.Tvdb + "-" + season, _SeasonEpisodes);
                    LastRequest = DateTime.UtcNow;
                    PreviousSelectedIndex = 0;
                }
                return Episodes[Show.Tvdb + "-" + season];
            }
        }
        private IEnumerable<TraktEpisode> _SeasonEpisodes = null;

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
            StopDownload = true;
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
                    CurrentLayout = GUICommon.ShowLayoutMenu(CurrentLayout);
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
            GUIListItem selectedItem = this.Facade.SelectedListItem;
            if (selectedItem == null) return;

            var selectedEpisode = selectedItem.TVTag as TraktEpisode;
            if (selectedEpisode == null) return;

            IDialogbox dlg = (IDialogbox)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
            if (dlg == null) return;

            dlg.Reset();
            dlg.SetHeading(GUIUtils.PluginName());

            GUIListItem listItem = null;

            listItem = new GUIListItem(Translation.Trailers);
            dlg.Add(listItem);
            listItem.ItemId = (int)ContextMenuItem.Trailers;

            if (selectedEpisode.Watched)
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

            if (selectedEpisode.InCollection)
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

            if (selectedEpisode.InWatchList)
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
            listItem = new GUIListItem(Translation.AddToList + "...");
            dlg.Add(listItem);
            listItem.ItemId = (int)ContextMenuItem.AddToList;

            // Rate
            listItem = new GUIListItem(Translation.Rate + "...");
            dlg.Add(listItem);
            listItem.ItemId = (int)ContextMenuItem.Rate;

            // Shouts
            listItem = new GUIListItem(Translation.Shouts + "...");
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
                    TraktHelper.MarkEpisodeAsWatched(Show, selectedEpisode);
                    selectedEpisode.Watched = true;
                    Facade.SelectedListItem.IsPlayed = true;
                    if (selectedEpisode.Plays == 0) selectedEpisode.Plays = 1;
                    OnEpisodeSelected(Facade.SelectedListItem, Facade);
                    selectedEpisode.Images.NotifyPropertyChanged("EpisodeImageFilename");
                    break;

                case ((int)ContextMenuItem.MarkAsUnWatched):
                    TraktHelper.MarkEpisodeAsUnWatched(Show, selectedEpisode);
                    selectedEpisode.Watched = false;
                    Facade.SelectedListItem.IsPlayed = false;
                    OnEpisodeSelected(Facade.SelectedListItem, Facade);
                    selectedEpisode.Images.NotifyPropertyChanged("EpisodeImageFilename");
                    break;

                case ((int)ContextMenuItem.AddToLibrary):
                    TraktHelper.AddEpisodeToLibrary(Show, selectedEpisode);
                    selectedEpisode.InCollection = true;
                    OnEpisodeSelected(selectedItem, Facade);
                    selectedEpisode.Images.NotifyPropertyChanged("EpisodeImageFilename");
                    break;

                case ((int)ContextMenuItem.RemoveFromLibrary):
                    TraktHelper.RemoveEpisodeFromLibrary(Show, selectedEpisode);
                    selectedEpisode.InCollection = false;
                    OnEpisodeSelected(selectedItem, Facade);
                    selectedEpisode.Images.NotifyPropertyChanged("EpisodeImageFilename");
                    break;

                case ((int)ContextMenuItem.AddToWatchList):
                    TraktHelper.AddEpisodeToWatchList(Show, selectedEpisode);
                    selectedEpisode.InWatchList = true;
                    OnEpisodeSelected(selectedItem, Facade);
                    selectedEpisode.Images.NotifyPropertyChanged("EpisodeImageFilename");
                    break;

                case ((int)ContextMenuItem.RemoveFromWatchList):
                    TraktHelper.RemoveEpisodeFromWatchList(Show, selectedEpisode);
                    selectedEpisode.InWatchList = false;
                    OnEpisodeSelected(selectedItem, Facade);
                    selectedEpisode.Images.NotifyPropertyChanged("EpisodeImageFilename");
                    break;

                case ((int)ContextMenuItem.Rate):
                    GUICommon.RateEpisode(Show, selectedEpisode);
                    OnEpisodeSelected(Facade.SelectedListItem, Facade);
                    selectedEpisode.Images.NotifyPropertyChanged("EpisodeImageFilename");
                    break;

                case ((int)ContextMenuItem.AddToList):
                    TraktHelper.AddRemoveEpisodeInUserList(Show.Title, Show.Year.ToString(), selectedEpisode.Season.ToString(), selectedEpisode.Number.ToString(), Show.Tvdb, false);
                    break;

                case ((int)ContextMenuItem.Shouts):
                    TraktHelper.ShowTVShowShouts(Show);
                    break;

                case ((int)ContextMenuItem.ChangeLayout):
                    CurrentLayout = GUICommon.ShowLayoutMenu(CurrentLayout);
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
            GUIListItem selectedItem = this.Facade.SelectedListItem;
            if (selectedItem == null) return;

            var selectedEpisode = selectedItem.TVTag as TraktEpisode;

            GUICommon.CheckAndPlayEpisode(Show, selectedEpisode);
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
                    IEnumerable<TraktEpisode> episodes = result as IEnumerable<TraktEpisode>;
                    SendSeasonEpisodesToFacade(episodes);
                }
            }, Translation.GettingWatchListEpisodes, true);
        }

        private void SendSeasonEpisodesToFacade(IEnumerable<TraktEpisode> episodes)
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
            List<TraktEpisode.ShowImages> episodeImages = new List<TraktEpisode.ShowImages>();

            foreach (var episode in episodes)
            {
                string itemLabel = string.Format("{0}. {1}", episode.Number.ToString(), string.IsNullOrEmpty(episode.Title) ? Translation.Episode + " " + episode.Number.ToString() : episode.Title);

                GUITraktSeasonEpisodeListItem item = new GUITraktSeasonEpisodeListItem(itemLabel);

                // add image for download
                episodeImages.Add(episode.Images);

                item.Label2 = episode.FirstAired == 0 ? " " : episode.FirstAired.FromEpoch().ToShortDateString();
                item.TVTag = episode;
                item.IsPlayed = episode.Watched;
                item.Item = episode.Images;
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
            GetImages(episodeImages);
        }

        private bool GetLoadingParameter()
        {
            if (_loadParameter == null)
            {
                // maybe re-loading, so check previous window id
                if (Show != null && !string.IsNullOrEmpty(Show.Tvdb) && Season != null)
                    return true;

                return false;
            }
            
            var loadingParam = _loadParameter.FromJSON<SeasonLoadingParameter>();
            if (loadingParam == null) return false;

            // reset previous selected index
            if (Show != null && Season != null)
            {
                if (Show.Title != loadingParam.Show.Title || Season.Season != loadingParam.Season.Season)
                    PreviousSelectedIndex = 0;
            }

            Show = loadingParam.Show;
            Season = loadingParam.Season;
            if (Show == null || string.IsNullOrEmpty(Show.Tvdb) || Season == null) return false;
            
            return true;
        }

        private void InitProperties()
        {
            // only set property if file exists
            // if we set now and download later, image will not set to skin
            if (File.Exists(Show.Images.FanartImageFilename))
                GUIUtils.SetProperty("#Trakt.Show.Fanart", Show.Images.FanartImageFilename);

            // load last layout
            CurrentLayout = (Layout)TraktSettings.SeasonEpisodesDefaultLayout;
            // update button label
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
            var episode = item.TVTag as TraktEpisode;
            if (episode == null) return;

            GUICommon.SetEpisodeProperties(episode);

            // set season properties as well, we may show a flattened view
            // with all episodes in season
            GUICommon.SetSeasonProperties(Season);
        }

        private void GetImages(List<TraktEpisode.ShowImages> itemsWithThumbs)
        {
            StopDownload = false;

            new Thread((o) =>
            {
                // download fanart if we need to
                if (!File.Exists(Show.Images.FanartImageFilename) && !string.IsNullOrEmpty(Show.Images.Fanart) && TraktSettings.DownloadFanart)
                {
                    if (GUIImageHandler.DownloadImage(Show.Images.Fanart, Show.Images.FanartImageFilename))
                    {
                        // notify that image has been downloaded
                        GUIUtils.SetProperty("#Trakt.Show.Fanart", Show.Images.FanartImageFilename);
                    }
                }
            })
            {
                IsBackground = true,
                Name = "ImageDownloader"
            }.Start();

            // split the downloads in 5+ groups and do multithreaded downloading
            int groupSize = (int)Math.Max(1, Math.Floor((double)itemsWithThumbs.Count / 5));
            int groups = (int)Math.Ceiling((double)itemsWithThumbs.Count() / groupSize);

            for (int i = 0; i < groups; i++)
            {
                List<TraktEpisode.ShowImages> groupList = new List<TraktEpisode.ShowImages>();
                for (int j = groupSize * i; j < groupSize * i + (groupSize * (i + 1) > itemsWithThumbs.Count ? itemsWithThumbs.Count - groupSize * i : groupSize); j++)
                {
                    groupList.Add(itemsWithThumbs[j]);
                }

                new Thread(delegate(object o)
                {
                    List<TraktEpisode.ShowImages> items = (List<TraktEpisode.ShowImages>)o;
                    foreach (TraktEpisode.ShowImages item in items)
                    {
                        #region Episode Image
                        // stop download if we have exited window
                        if (StopDownload) break;

                        string remoteThumb = item.Screen;
                        string localThumb = item.EpisodeImageFilename;

                        if (!string.IsNullOrEmpty(remoteThumb) && !string.IsNullOrEmpty(localThumb))
                        {
                            if (GUIImageHandler.DownloadImage(remoteThumb, localThumb))
                            {
                                // notify that image has been downloaded
                                item.NotifyPropertyChanged("EpisodeImageFilename");
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

    public class GUITraktSeasonEpisodeListItem : GUIListItem
    {
        public GUITraktSeasonEpisodeListItem(string strLabel) : base(strLabel) { }

        public object Item
        {
            get { return _Item; }
            set
            {
                _Item = value;
                INotifyPropertyChanged notifier = value as INotifyPropertyChanged;
                if (notifier != null) notifier.PropertyChanged += (s, e) =>
                {
                    if (s is TraktEpisode.ShowImages && e.PropertyName == "EpisodeImageFilename")
                        SetImageToGui((s as TraktEpisode.ShowImages).EpisodeImageFilename);
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

            // determine the overlay to add to poster
            TraktEpisode episode = TVTag as TraktEpisode;
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

            // build memory image, resize thumbnail incase its a fanart
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
            this.UpdateItemIfSelected((int)TraktGUIWindows.SeasonEpisodes, ItemId);
        }
    }

    public class SeasonLoadingParameter
    {
        public TraktShow Show { get; set; }
        public TraktShowSeason Season { get; set; }
    }
}
