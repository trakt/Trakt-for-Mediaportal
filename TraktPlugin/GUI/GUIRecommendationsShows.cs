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
    public class GUIRecommendationsShows : GUIWindow
    {
        #region Skin Controls

        [SkinControl(50)]
        protected GUIFacadeControl Facade = null;

        [SkinControl(2)]
        protected GUIButtonControl layoutButton = null;

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
            AddToWatchList,
            RemoveFromWatchList,
            Rate,
            ChangeLayout
        }

        #endregion

        #region Constructor

        public GUIRecommendationsShows() { }

        #endregion

        #region Private Variables

        bool StopDownload { get; set; }
        private Layout CurrentLayout { get; set; }
        int PreviousSelectedIndex { get; set; }

        IEnumerable<TraktShow> RecommendedShows
        {
            get
            {
                if (_RecommendedShows == null)
                {
                    _RecommendedShows = TraktAPI.TraktAPI.GetRecommendedShows();
                }
                return _RecommendedShows;
            }
        }
        private IEnumerable<TraktShow> _RecommendedShows = null;

        #endregion

        #region Base Overrides

        public override int GetID
        {
            get
            {
                return 87262;
            }
        }

        public override bool Init()
        {
            return Load(GUIGraphicsContext.Skin + @"\Trakt.Recommendations.Shows.xml");
        }

        protected override void OnPageLoad()
        {
            // Requires Login
            if (!GUICommon.CheckLogin()) return;

            // Clear GUI Properties
            ClearProperties();

            // Init Properties
            InitProperties();

            // Load Recommended Shows
            LoadRecommendedShows();
        }

        protected override void OnPageDestroy(int new_windowId)
        {
            StopDownload = true;
            _RecommendedShows = null;
            ClearProperties();

            // save current layout
            TraktSettings.RecommendedShowsDefaultLayout = (int)CurrentLayout;

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

            TraktShow selectedShow = (TraktShow)selectedItem.TVTag;

            IDialogbox dlg = (IDialogbox)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
            if (dlg == null) return;

            dlg.Reset();
            dlg.SetHeading(GUIUtils.PluginName());

            GUIListItem listItem = null;

            // Add/Remove Watch List            
            if (!selectedShow.InWatchList)
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

            // Rate Show
            listItem = new GUIListItem(Translation.RateShow);
            dlg.Add(listItem);
            listItem.ItemId = (int)ContextMenuItem.Rate;

            // Change Layout
            listItem = new GUIListItem(Translation.ChangeLayout);
            dlg.Add(listItem);
            listItem.ItemId = (int)ContextMenuItem.ChangeLayout;

            // Show Context Menu
            dlg.DoModal(GUIWindowManager.ActiveWindow);
            if (dlg.SelectedId < 0) return;

            switch (dlg.SelectedId)
            {
                case ((int)ContextMenuItem.AddToWatchList):
                    AddShowToWatchList(selectedShow);
                    selectedShow.InWatchList = true;
                    OnShowSelected(selectedItem, Facade);
                    selectedShow.Images.NotifyPropertyChanged("PosterImageFilename");
                    break;

                case ((int)ContextMenuItem.RemoveFromWatchList):
                    RemoveShowFromWatchList(selectedShow);
                    selectedShow.InWatchList = false;
                    OnShowSelected(selectedItem, Facade);
                    selectedShow.Images.NotifyPropertyChanged("PosterImageFilename");
                    break;

                case ((int)ContextMenuItem.Rate):
                    PreviousSelectedIndex = this.Facade.SelectedListItemIndex;
                    if (RateShow(selectedShow))
                    {
                        // remove from recommendations
                        if (_RecommendedShows.Count() > 1)
                        {
                            var showsToExcept = new List<TraktShow>();
                            showsToExcept.Add(selectedShow);
                            _RecommendedShows = RecommendedShows.Except(showsToExcept);
                        }
                        else
                        {
                            // reload, none left
                            ClearProperties();
                            GUIControl.ClearControl(GetID, Facade.GetID);
                            _RecommendedShows = null;
                        }
                        LoadRecommendedShows();
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

        private TraktShowSync CreateSyncData(TraktShow show)
        {
            if (show == null) return null;

            List<TraktShowSync.Show> shows = new List<TraktShowSync.Show>();

            TraktShowSync.Show syncShow = new TraktShowSync.Show
            {
                TVDBID = show.Tvdb,
                Title = show.Title,
                Year = show.Year
            };
            shows.Add(syncShow);

            TraktShowSync syncData = new TraktShowSync
            {
                UserName = TraktSettings.Username,
                Password = TraktSettings.Password,
                Shows = shows
            };

            return syncData;
        }

        private void AddShowToWatchList(TraktShow show)
        {
            Thread syncThread = new Thread(delegate(object obj)
            {
                TraktAPI.TraktAPI.SyncShowWatchList(CreateSyncData(obj as TraktShow), TraktSyncModes.watchlist);
            })
            {
                IsBackground = true,
                Name = "Adding Show to Watch List"
            };

            syncThread.Start(show);
        }

        private void RemoveShowFromWatchList(TraktShow show)
        {
            Thread syncThread = new Thread(delegate(object obj)
            {
                TraktAPI.TraktAPI.SyncShowWatchList(CreateSyncData(obj as TraktShow), TraktSyncModes.unwatchlist);
            })
            {
                IsBackground = true,
                Name = "Removing Show from Watch List"
            };

            syncThread.Start(show);
        }

        private bool RateShow(TraktShow show)
        {
            // init since its not part of the API
            show.Rating = "false";
            bool ratingChanged = false;

            TraktRateSeries rateObject = new TraktRateSeries
            {
                SeriesID = show.Tvdb,
                Title = show.Title,
                Year = show.Year.ToString(),
                Rating = show.Rating,
                UserName = TraktSettings.Username,
                Password = TraktSettings.Password
            };

            string prevRating = show.Rating;
            show.Rating = GUIUtils.ShowRateDialog<TraktRateSeries>(rateObject);

            if (prevRating != show.Rating)
            {
                ratingChanged = true;
            }

            return ratingChanged;
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

        private void LoadRecommendedShows()
        {
            GUIUtils.SetProperty("#Trakt.Items", string.Empty);

            GUIBackgroundTask.Instance.ExecuteInBackgroundAndCallback(() =>
            {
                return RecommendedShows;
            },
            delegate(bool success, object result)
            {
                if (success)
                {
                    IEnumerable<TraktShow> shows = result as IEnumerable<TraktShow>;
                    SendRecommendedShowsToFacade(shows);
                }
            }, Translation.GettingRecommendedShows, true);
        }

        private void SendRecommendedShowsToFacade(IEnumerable<TraktShow> shows)
        {
            // clear facade
            GUIControl.ClearControl(GetID, Facade.GetID);

            if (shows.Count() == 0)
            {
                GUIUtils.ShowNotifyDialog(GUIUtils.PluginName(), Translation.NoShowRecommendations);
                GUIWindowManager.ShowPreviousWindow();
                return;
            }

            int itemId = 0;
            List<TraktShow.ShowImages> showImages = new List<TraktShow.ShowImages>();

            foreach (var show in shows)
            {
                GUITraktRecommendedShowListItem item = new GUITraktRecommendedShowListItem(show.Title);

                item.Label2 = show.Year.ToString();
                item.TVTag = show;
                item.Item = show.Images;
                item.ItemId = Int32.MaxValue - itemId;
                item.IconImage = "defaultVideo.png";
                item.IconImageBig = "defaultVideoBig.png";
                item.ThumbnailImage = "defaultVideoBig.png";
                item.OnItemSelected += OnShowSelected;
                Utils.SetDefaultIcons(item);
                Facade.Add(item);
                itemId++;

                // add image for download
                showImages.Add(show.Images);
            }

            // Set Facade Layout
            Facade.SetCurrentLayout(Enum.GetName(typeof(Layout), CurrentLayout));
            GUIControl.FocusControl(GetID, Facade.GetID);

            if (PreviousSelectedIndex >= shows.Count())
                Facade.SelectedListItemIndex = PreviousSelectedIndex - 1;
            else
                Facade.SelectedListItemIndex = PreviousSelectedIndex;

            // set facade properties
            GUIUtils.SetProperty("#itemcount", shows.Count().ToString());
            GUIUtils.SetProperty("#Trakt.Items", string.Format("{0} {1}", shows.Count().ToString(), shows.Count() > 1 ? Translation.SeriesPlural : Translation.Series));

            // Download show images Async and set to facade
            GetImages(showImages);
        }

        private void SetProperty(string property, string value)
        {
            string propertyValue = string.IsNullOrEmpty(value) ? "N/A" : value;
            GUIUtils.SetProperty(property, propertyValue);
        }

        private void InitProperties()
        {
            // load last layout
            CurrentLayout = (Layout)TraktSettings.RecommendedShowsDefaultLayout;
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
            GUIUtils.SetProperty("#Trakt.Show.PosterImageFilename", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.InWatchList", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.Rating", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.Ratings.Icon", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.Ratings.HatedCount", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.Ratings.LovedCount", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.Ratings.Percentage", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.Ratings.Votes", string.Empty);
        }

        private void PublishShowSkinProperties(TraktShow show)
        {
            SetProperty("#Trakt.Show.AirDay", show.AirDay);
            SetProperty("#Trakt.Show.AirTime", show.AirTime);
            SetProperty("#Trakt.Show.Country", show.Country);
            SetProperty("#Trakt.Show.Network", show.Network);
            SetProperty("#Trakt.Show.TvRage", show.TvRage);
            SetProperty("#Trakt.Show.Imdb", show.Imdb);
            SetProperty("#Trakt.Show.Certification", show.Certification);
            SetProperty("#Trakt.Show.Overview", show.Overview);
            SetProperty("#Trakt.Show.FirstAired", show.FirstAired.FromEpoch().ToShortDateString());
            SetProperty("#Trakt.Show.Runtime", show.Runtime.ToString());
            SetProperty("#Trakt.Show.Title", show.Title);
            SetProperty("#Trakt.Show.Url", show.Url);
            SetProperty("#Trakt.Show.Year", show.Year.ToString());
            SetProperty("#Trakt.Show.PosterImageFilename", show.Images.PosterImageFilename);
            SetProperty("#Trakt.Show.InWatchList", show.InWatchList.ToString());
            SetProperty("#Trakt.Show.Rating", show.Rating);
            SetProperty("#Trakt.Show.Ratings.Icon", (show.Ratings.LovedCount > show.Ratings.HatedCount) ? "love" : "hate");
            SetProperty("#Trakt.Show.Ratings.HatedCount", show.Ratings.HatedCount.ToString());
            SetProperty("#Trakt.Show.Ratings.LovedCount", show.Ratings.LovedCount.ToString());
            SetProperty("#Trakt.Show.Ratings.Percentage", show.Ratings.Percentage.ToString());
            SetProperty("#Trakt.Show.Ratings.Votes", show.Ratings.Votes.ToString());
        }

        private void OnShowSelected(GUIListItem item, GUIControl parent)
        {
            PublishShowSkinProperties(item.TVTag as TraktShow);
        }

        private void GetImages(List<TraktShow.ShowImages> itemsWithThumbs)
        {
            StopDownload = false;

            // split the downloads in 5+ groups and do multithreaded downloading
            int groupSize = (int)Math.Max(1, Math.Floor((double)itemsWithThumbs.Count / 5));
            int groups = (int)Math.Ceiling((double)itemsWithThumbs.Count() / groupSize);

            for (int i = 0; i < groups; i++)
            {
                List<TraktShow.ShowImages> groupList = new List<TraktShow.ShowImages>();
                for (int j = groupSize * i; j < groupSize * i + (groupSize * (i + 1) > itemsWithThumbs.Count ? itemsWithThumbs.Count - groupSize * i : groupSize); j++)
                {
                    groupList.Add(itemsWithThumbs[j]);
                }

                new Thread(delegate(object o)
                {
                    List<TraktShow.ShowImages> items = (List<TraktShow.ShowImages>)o;
                    foreach (TraktShow.ShowImages item in items)
                    {
                        // stop download if we have exited window
                        if (StopDownload) break;

                        string remoteThumb = item.Poster;
                        if (string.IsNullOrEmpty(remoteThumb)) continue;

                        string localThumb = item.PosterImageFilename;
                        if (string.IsNullOrEmpty(localThumb)) continue;

                        if (GUIImageHandler.DownloadImage(remoteThumb, localThumb))
                        {
                            // notify that image has been downloaded
                            item.NotifyPropertyChanged("PosterImageFilename");
                        }
                    }
                })
                {
                    IsBackground = true,
                    Name = "Trakt Show Image Downloader " + i.ToString()
                }.Start(groupList);
            }
        }

        #endregion
    }

    public class GUITraktRecommendedShowListItem : GUIListItem
    {
        public GUITraktRecommendedShowListItem(string strLabel) : base(strLabel) { }

        public object Item
        {
            get { return _Item; }
            set
            {
                _Item = value;
                INotifyPropertyChanged notifier = value as INotifyPropertyChanged;
                if (notifier != null) notifier.PropertyChanged += (s, e) =>
                {
                    if (s is TraktShow.ShowImages && e.PropertyName == "PosterImageFilename")
                        SetImageToGui((s as TraktShow.ShowImages).PosterImageFilename);
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
            TraktShow show = TVTag as TraktShow;
            MainOverlayImage mainOverlay = MainOverlayImage.None;

            if (show.InWatchList)
                mainOverlay = MainOverlayImage.Watchlist;

            // we never show rating movies in Recommendations
            RatingOverlayImage ratingOverlay = RatingOverlayImage.None;

            // get a reference to a MediaPortal Texture Identifier
            string suffix = Enum.GetName(typeof(MainOverlayImage), mainOverlay) + Enum.GetName(typeof(RatingOverlayImage), ratingOverlay);
            string texture = GUIImageHandler.GetTextureIdentFromFile(imageFilePath, suffix);

            // build memory image
            Image memoryImage = null;
            if (mainOverlay != MainOverlayImage.None)
                memoryImage = GUIImageHandler.DrawOverlayOnPoster(imageFilePath, mainOverlay, ratingOverlay);
            else
                memoryImage = ImageFast.FromFile(imageFilePath);

            // load texture into facade item
            if (GUITextureManager.LoadFromMemory(memoryImage, texture, 0, 0, 0) > 0)
            {
                ThumbnailImage = texture;
                IconImage = texture;
                IconImageBig = texture;
            }

            // if selected and TraktFriends is current window force an update of thumbnail
            GUIRecommendationsShows window = GUIWindowManager.GetWindow(GUIWindowManager.ActiveWindow) as GUIRecommendationsShows;
            if (window != null)
            {
                GUIListItem selectedItem = GUIControl.GetSelectedListItem(87262, 50);
                if (selectedItem == this)
                {
                    GUIWindowManager.SendThreadMessage(new GUIMessage(GUIMessage.MessageType.GUI_MSG_ITEM_SELECT, GUIWindowManager.ActiveWindow, 0, 50, ItemId, 0, null));
                }
            }
        }
    }
}
