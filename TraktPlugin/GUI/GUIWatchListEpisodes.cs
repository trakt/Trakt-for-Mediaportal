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
    public class GUIWatchListEpisodes : GUIWindow
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

        enum Layout
        {
            List = 0,
            SmallIcons = 1,
            LargeIcons = 2,
            Filmstrip = 3,
        }

        enum ContextMenuItem
        {
            RemoveFromWatchList,
            ChangeLayout
        }

        #endregion

        #region Constructor

        public GUIWatchListEpisodes()
        {
            backdrop = new ImageSwapper();
            backdrop.PropertyOne = "#Trakt.WatchListEpisodes.Fanart.1";
            backdrop.PropertyTwo = "#Trakt.WatchListEpisodes.Fanart.2";
        }

        #endregion

        #region Private Variables

        bool StopDownload { get; set; }
        bool RemovingWatchListItem { get; set; }
        private Layout CurrentLayout { get; set; }
        int PreviousSelectedIndex { get; set; }
        ImageSwapper backdrop;

        IEnumerable<TraktWatchListEpisode> WatchListEpisodes
        {
            get
            {
                if (_WatchListEpisodes == null)
                {
                    _WatchListEpisodes = TraktAPI.TraktAPI.GetWatchListEpisodes(TraktSettings.Username);
                }
                return _WatchListEpisodes;
            }
        }
        private IEnumerable<TraktWatchListEpisode> _WatchListEpisodes = null;

        #endregion

        #region Base Overrides

        public override int GetID
        {
            get
            {
                return 87269;
            }
        }

        public override bool Init()
        {
            return Load(GUIGraphicsContext.Skin + @"\Trakt.WatchList.Episodes.xml");
        }

        protected override void OnPageLoad()
        {
            // Requires Login
            if (!GUICommon.CheckLogin()) return;
          
            // Clear GUI Properties
            ClearProperties();

            // Init Properties
            InitProperties();

            // Load WatchList Episodes
            LoadWatchListEpisodes();
        }

        protected override void OnPageDestroy(int new_windowId)
        {
            StopDownload = true;
            _WatchListEpisodes = null;
            PreviousSelectedIndex = 0;
            ClearProperties();

            // save current layout
            TraktSettings.WatchListEpisodesDefaultLayout = (int)CurrentLayout;

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

                    }
                    break;

                // Layout Button
                case (2):
                    ShowLayoutMenu();
                    break;

                default:
                    break;
            }
            base.OnClicked(controlId, control, actionType);
        }

        protected override void OnShowContextMenu()
        {
            GUIListItem selectedItem = this.Facade.SelectedListItem;
            if (selectedItem == null) return;

            var item = (KeyValuePair<TraktShow, TraktWatchListEpisode.Episode>)selectedItem.TVTag;
            var selectedSeries = item.Key;
            var selectedEpisode = item.Value;

            IDialogbox dlg = (IDialogbox)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
            if (dlg == null) return;

            dlg.Reset();
            dlg.SetHeading(GUIUtils.PluginName());

            GUIListItem listItem = null;

            listItem = new GUIListItem(Translation.RemoveFromWatchList);
            dlg.Add(listItem);
            listItem.ItemId = (int)ContextMenuItem.RemoveFromWatchList;

            // Change Layout
            listItem = new GUIListItem(Translation.ChangeLayout);
            dlg.Add(listItem);
            listItem.ItemId = (int)ContextMenuItem.ChangeLayout;

            // Show Context Menu
            dlg.DoModal(GUIWindowManager.ActiveWindow);
            if (dlg.SelectedId < 0) return;

            switch (dlg.SelectedId)
            {
                case ((int)ContextMenuItem.RemoveFromWatchList):
                    RemovingWatchListItem = true;
                    PreviousSelectedIndex = this.Facade.SelectedListItemIndex;
                    RemoveEpisodeFromWatchList(item);
                    if (this.Facade.Count >= 1)
                    {
                        // remove from list
                        _WatchListEpisodes = null;
                        LoadWatchListEpisodes();
                    }
                    else
                    {
                        // no more shows left
                        ClearProperties();
                        GUIControl.ClearControl(GetID, Facade.GetID);
                        _WatchListEpisodes = null;
                        // notify and exit
                        GUIUtils.ShowNotifyDialog(GUIUtils.PluginName(), Translation.NoShowWatchList);
                        GUIWindowManager.ShowPreviousWindow();
                        return;
                    }
                    break;

                case ((int)ContextMenuItem.ChangeLayout):
                    ShowLayoutMenu();
                    break;

                default:
                    break;
            }

            base.OnShowContextMenu();
        }

        #endregion

        #region Private Methods

        private TraktEpisodeSync CreateSyncData(KeyValuePair<TraktShow, TraktWatchListEpisode.Episode> item)
        {
            var series = item.Key;
            var episode = item.Value;

            List<TraktEpisodeSync.Episode> episodes = new List<TraktEpisodeSync.Episode>();

            TraktEpisodeSync.Episode ep = new TraktEpisodeSync.Episode
            {
                EpisodeIndex = episode.Number.ToString(),
                SeasonIndex = episode.Season.ToString()                
            };
            episodes.Add(ep);

            TraktEpisodeSync syncData = new TraktEpisodeSync
            {
                UserName = TraktSettings.Username,
                Password = TraktSettings.Password,
                SeriesID = series.Tvdb,
                Title = series.Title,
                Year = series.Year.ToString(),
                EpisodeList = episodes
            };

            return syncData;
        }

        private void RemoveEpisodeFromWatchList(KeyValuePair<TraktShow, TraktWatchListEpisode.Episode> item)
        {
            Thread syncThread = new Thread(delegate(object obj)
            {
                TraktAPI.TraktAPI.SyncEpisodeWatchList(CreateSyncData((KeyValuePair<TraktShow, TraktWatchListEpisode.Episode>)obj), TraktSyncModes.unwatchlist);
                RemovingWatchListItem = false;
            })
            {
                IsBackground = true,
                Name = "Removing Episode from Watch List"
            };

            syncThread.Start(item);
        }


        private void ShowLayoutMenu()
        {
            IDialogbox dlg = (IDialogbox)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
            dlg.Reset();
            dlg.SetHeading(GetLayoutTranslation(CurrentLayout));

            foreach (Layout layout in Enum.GetValues(typeof(Layout)))
            {
                string menuItem = GetLayoutTranslation(layout);
                GUIListItem pItem = new GUIListItem(menuItem);
                if (layout == CurrentLayout) pItem.Selected = true;
                dlg.Add(pItem);
            }

            dlg.DoModal(GUIWindowManager.ActiveWindow);

            if (dlg.SelectedLabel >= 0)
            {
                CurrentLayout = (Layout)dlg.SelectedLabel;
                Facade.SetCurrentLayout(Enum.GetName(typeof(Layout), CurrentLayout));
                GUIControl.SetControlLabel(GetID, layoutButton.GetID, GetLayoutTranslation(CurrentLayout));
            }
        }

        private string GetLayoutTranslation(Layout layout)
        {
            string strLine = string.Empty;
            switch (layout)
            {
                case Layout.List:
                    strLine = GUILocalizeStrings.Get(101);
                    break;
                case Layout.SmallIcons:
                    strLine = GUILocalizeStrings.Get(100);
                    break;
                case Layout.LargeIcons:
                    strLine = GUILocalizeStrings.Get(417);
                    break;
                case Layout.Filmstrip:
                    strLine = GUILocalizeStrings.Get(733);
                    break;
            }
            return strLine;
        }

        private void LoadWatchListEpisodes()
        {
            GUIUtils.SetProperty("#Trakt.Items", string.Empty);

            GUIBackgroundTask.Instance.ExecuteInBackgroundAndCallback(() =>
            {
                // wait until watched item has been removed or timesout (10secs)
                while (RemovingWatchListItem) Thread.Sleep(500);
                return WatchListEpisodes;
            },
            delegate(bool success, object result)
            {
                if (success)
                {
                    IEnumerable<TraktWatchListEpisode> shows = result as IEnumerable<TraktWatchListEpisode>;
                    SendWatchListEpisodesToFacade(shows);
                }
            }, Translation.GettingWatchListEpisodes, true);
        }

        private void SendWatchListEpisodesToFacade(IEnumerable<TraktWatchListEpisode> shows)
        {
            // clear facade
            GUIControl.ClearControl(GetID, Facade.GetID);

            if (shows.Count() == 0)
            {
                GUIUtils.ShowNotifyDialog(GUIUtils.PluginName(), Translation.NoEpisodeWatchList);
                GUIWindowManager.ShowPreviousWindow();
                return;
            }

            int itemCount = 0;
            List<TraktImage> showImages = new List<TraktImage>();

            // Add each show and underlying episodes
            // Should we do facade levels (Series,Season,Episodes)?
            foreach (var show in shows)
            {
                foreach (var episode in show.Episodes)
                {
                    string itemLabel = string.Format("{0} - {1}x{2}{3}", show.Title, episode.Season.ToString(), episode.Number.ToString(), string.IsNullOrEmpty(episode.Title) ? string.Empty : " - " + episode.Title);

                    GUITraktWatchListEpisodeListItem item = new GUITraktWatchListEpisodeListItem(itemLabel);

                    // add image for download
                    TraktImage images = new TraktImage
                    {
                        EpisodeImages = episode.Images,
                        ShowImages = show.Images
                    };
                    showImages.Add(images);

                    item.Label2 = episode.FirstAired.FromEpoch().ToShortDateString();
                    item.TVTag = new KeyValuePair<TraktShow, TraktWatchListEpisode.Episode>(show, episode);
                    item.Item = images;
                    item.ItemId = Int32.MaxValue - itemCount;
                    item.IconImage = "defaultTraktEpisode.png";
                    item.IconImageBig = "defaultTraktEpisodeBig.png";
                    item.ThumbnailImage = "defaultTraktEpisodeBig.png";
                    item.OnItemSelected += OnEpisodeSelected;
                    Utils.SetDefaultIcons(item);
                    Facade.Add(item);
                    itemCount++;
                }
            }

            // Set Facade Layout
            Facade.SetCurrentLayout(Enum.GetName(typeof(Layout), CurrentLayout));
            GUIControl.FocusControl(GetID, Facade.GetID);

            if (PreviousSelectedIndex >= shows.Count())
                Facade.SelectedListItemIndex = PreviousSelectedIndex - 1;
            else
                Facade.SelectedListItemIndex = PreviousSelectedIndex;

            // set facade properties
            GUIUtils.SetProperty("#itemcount", itemCount.ToString());
            GUIUtils.SetProperty("#Trakt.Items", string.Format("{0} {1}", itemCount.ToString(), itemCount > 1 ? Translation.Episodes : Translation.Episode));

            // Download episode images Async and set to facade
            GetImages(showImages);
        }

        private void SetProperty(string property, string value)
        {
            string propertyValue = string.IsNullOrEmpty(value) ? "N/A" : value;
            GUIUtils.SetProperty(property, propertyValue);
        }

        private void InitProperties()
        {
            // Fanart
            backdrop.GUIImageOne = FanartBackground;
            backdrop.GUIImageTwo = FanartBackground2;
            backdrop.LoadingImage = loadingImage;

            RemovingWatchListItem = false;

            // load last layout
            CurrentLayout = (Layout)TraktSettings.WatchListEpisodesDefaultLayout;
            // update button label
            GUIControl.SetControlLabel(GetID, layoutButton.GetID, GetLayoutTranslation(CurrentLayout));
        }

        private void ClearProperties()
        {
            GUIUtils.SetProperty("#Trakt.Show.AirDay", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.AirTime", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.Country", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.Network", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.TvRage", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.Imdb", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.Certification", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.Overview", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.FirstAired", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.Runtime", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.Title", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.Tvdb", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.Trailer", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.Url", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.Year", string.Empty);            
            GUIUtils.SetProperty("#Trakt.Show.FanartImageFilename", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.Ratings.Icon", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.Ratings.HatedCount", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.Ratings.LovedCount", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.Ratings.Percentage", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.Ratings.Votes", string.Empty);
            GUIUtils.SetProperty("#Trakt.Episode.Number", string.Empty);
            GUIUtils.SetProperty("#Trakt.Episode.Season", string.Empty);
            GUIUtils.SetProperty("#Trakt.Episode.FirstAired", string.Empty);
            GUIUtils.SetProperty("#Trakt.Episode.WatchList.Inserted", string.Empty);
            GUIUtils.SetProperty("#Trakt.Episode.Title", string.Empty);
            GUIUtils.SetProperty("#Trakt.Episode.Url", string.Empty);
            GUIUtils.SetProperty("#Trakt.Episode.Overview", string.Empty);
            GUIUtils.SetProperty("#Trakt.Episode.Runtime", string.Empty);
            GUIUtils.SetProperty("#Trakt.Episode.EpisodeImageFilename", string.Empty);
        }

        private void PublishEpisodeSkinProperties(KeyValuePair<TraktShow, TraktWatchListEpisode.Episode> e)
        {
            var show = e.Key;
            var episode = e.Value;

            SetProperty("#Trakt.Show.Imdb", show.Imdb);
            SetProperty("#Trakt.Show.Tvdb", show.Tvdb);
            SetProperty("#Trakt.Show.TvRage", show.TvRage);
            SetProperty("#Trakt.Show.Title", show.Title);
            SetProperty("#Trakt.Show.Url", show.Url);
            SetProperty("#Trakt.Show.AirDay", show.AirDay);
            SetProperty("#Trakt.Show.AirTime", show.AirTime);
            SetProperty("#Trakt.Show.Certification", show.Certification);
            SetProperty("#Trakt.Show.Country", show.Country);
            SetProperty("#Trakt.Show.FirstAired", show.FirstAired.FromEpoch().ToShortDateString());
            SetProperty("#Trakt.Show.Network", show.Network);
            SetProperty("#Trakt.Show.Overview", show.Overview);
            SetProperty("#Trakt.Show.Runtime", show.Runtime.ToString());
            SetProperty("#Trakt.Show.Year", show.Year.ToString());
            SetProperty("#Trakt.Show.FanartImageFilename", show.Images.FanartImageFilename);
            SetProperty("#Trakt.Show.Ratings.Icon", (show.Ratings.LovedCount > show.Ratings.HatedCount) ? "love" : "hate");
            SetProperty("#Trakt.Show.Ratings.HatedCount", show.Ratings.HatedCount.ToString());
            SetProperty("#Trakt.Show.Ratings.LovedCount", show.Ratings.LovedCount.ToString());
            SetProperty("#Trakt.Show.Ratings.Percentage", show.Ratings.Percentage.ToString());
            SetProperty("#Trakt.Show.Ratings.Votes", show.Ratings.Votes.ToString());
            SetProperty("#Trakt.Episode.Number", episode.Number.ToString());
            SetProperty("#Trakt.Episode.Season", episode.Season.ToString());
            SetProperty("#Trakt.Episode.FirstAired", episode.FirstAired.FromEpoch().ToShortDateString());
            SetProperty("#Trakt.Episode.WatchList.Inserted", episode.Inserted.FromEpoch().ToShortDateString());
            SetProperty("#Trakt.Episode.Title", string.IsNullOrEmpty(episode.Title) ? string.Format("{0} {1}", Translation.Episode, episode.Number.ToString()) : episode.Title);
            SetProperty("#Trakt.Episode.Url", episode.Url);
            SetProperty("#Trakt.Episode.Overview", string.IsNullOrEmpty(episode.Overview) ? Translation.NoEpisodeSummary : episode.Overview);
            SetProperty("#Trakt.Episode.Runtime", episode.Runtime.ToString());
            SetProperty("#Trakt.Episode.EpisodeImageFilename", episode.Images.EpisodeImageFilename);
        }

        private void OnEpisodeSelected(GUIListItem item, GUIControl parent)
        {
            var episode = (KeyValuePair<TraktShow, TraktWatchListEpisode.Episode>)item.TVTag;
            PublishEpisodeSkinProperties(episode);
            GUIImageHandler.LoadFanart(backdrop, episode.Key.Images.FanartImageFilename);
        }

        private void GetImages(List<TraktImage> itemsWithThumbs)
        {
            StopDownload = false;

            // split the downloads in 5+ groups and do multithreaded downloading
            int groupSize = (int)Math.Max(1, Math.Floor((double)itemsWithThumbs.Count / 5));
            int groups = (int)Math.Ceiling((double)itemsWithThumbs.Count() / groupSize);

            for (int i = 0; i < groups; i++)
            {
                List<TraktImage> groupList = new List<TraktImage>();
                for (int j = groupSize * i; j < groupSize * i + (groupSize * (i + 1) > itemsWithThumbs.Count ? itemsWithThumbs.Count - groupSize * i : groupSize); j++)
                {
                    groupList.Add(itemsWithThumbs[j]);
                }

                new Thread(delegate(object o)
                {
                    List<TraktImage> items = (List<TraktImage>)o;
                    foreach (TraktImage item in items)
                    {
                        #region Episode Image
                        // stop download if we have exited window
                        if (StopDownload) break;

                        string remoteThumb = item.EpisodeImages.Screen;
                        string localThumb = item.EpisodeImages.EpisodeImageFilename;

                        if (!string.IsNullOrEmpty(remoteThumb) && !string.IsNullOrEmpty(localThumb))
                        {
                            if (GUIImageHandler.DownloadImage(remoteThumb, localThumb))
                            {
                                // notify that image has been downloaded
                                item.NotifyPropertyChanged("EpisodeImages");
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
                                item.NotifyPropertyChanged("ShowImages");
                            }
                        }
                        #endregion
                    }
                })
                {
                    IsBackground = true,
                    Name = "Trakt Episode Image Downloader " + i.ToString()
                }.Start(groupList);
            }
        }

        #endregion
    }

    public class GUITraktWatchListEpisodeListItem : GUIListItem
    {
        public GUITraktWatchListEpisodeListItem(string strLabel) : base(strLabel) { }

        public object Item
        {
            get { return _Item; }
            set
            {
                _Item = value;
                INotifyPropertyChanged notifier = value as INotifyPropertyChanged;
                if (notifier != null) notifier.PropertyChanged += (s, e) =>
                {
                    if (s is TraktImage && e.PropertyName == "EpisodeImages")
                        SetImageToGui((s as TraktImage).EpisodeImages.EpisodeImageFilename);
                    if (s is TraktImage && e.PropertyName == "ShowImages")
                        UpdateCurrentSelection();
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

            // get a reference to a MediaPortal Texture Identifier
            string texture = GUIImageHandler.GetTextureIdentFromFile(imageFilePath);

            // build memory image
            Image memoryImage = GUIImageHandler.LoadImage(imageFilePath);

            // load texture into facade item
            if (GUITextureManager.LoadFromMemory(memoryImage, texture, 0, 0, 0) > 0)
            {
                ThumbnailImage = texture;
                IconImage = texture;
                IconImageBig = texture;
            }

            // if selected and is current window force an update of thumbnail
            UpdateCurrentSelection();            
        }

        protected void UpdateCurrentSelection()
        {
            GUIWatchListEpisodes window = GUIWindowManager.GetWindow(GUIWindowManager.ActiveWindow) as GUIWatchListEpisodes;
            if (window != null)
            {
                GUIListItem selectedItem = GUIControl.GetSelectedListItem(87269, 50);
                if (selectedItem == this)
                {
                    GUIWindowManager.SendThreadMessage(new GUIMessage(GUIMessage.MessageType.GUI_MSG_ITEM_SELECT, GUIWindowManager.ActiveWindow, 0, 50, ItemId, 0, null));
                }
            }
        }
    }
}