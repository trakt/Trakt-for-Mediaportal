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
    public class GUIShowSeasons : GUIWindow
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
            MarkAsWatched,
            AddToLibrary,
            AddToList,
            Sort,
            ChangeLayout
        }

        #endregion

        #region Constructor

        public GUIShowSeasons() 
        {
        }

        #endregion

        #region Private Variables

        bool StopDownload { get; set; }
        private Layout CurrentLayout { get; set; }
        DateTime LastRequest = new DateTime();
        int PreviousSelectedIndex = 0;
        TraktShow Show = null;
        Dictionary<string, IEnumerable<TraktShowSeason>> Shows = new Dictionary<string, IEnumerable<TraktShowSeason>>();

        IEnumerable<TraktShowSeason> ShowSeasons
        {
            get
            {
                if (!Shows.Keys.Contains(Show.Tvdb) || LastRequest < DateTime.UtcNow.Subtract(new TimeSpan(0, TraktSettings.WebRequestCacheMinutes, 0)))
                {
                    _ShowSeasons = TraktAPI.TraktAPI.GetShowSeasons(Show.Tvdb);
                    if (Shows.Keys.Contains(Show.Tvdb)) Shows.Remove(Show.Tvdb);
                    Shows.Add(Show.Tvdb, _ShowSeasons);
                    LastRequest = DateTime.UtcNow;
                    PreviousSelectedIndex = 0;
                }
                return Shows[Show.Tvdb];
            }
        }
        private IEnumerable<TraktShowSeason> _ShowSeasons = null;

        #endregion

        #region Base Overrides

        public override int GetID
        {
            get
            {
                return (int)TraktGUIWindows.ShowSeasons;
            }
        }

        public override bool Init()
        {
            return Load(GUIGraphicsContext.Skin + @"\Trakt.Show.Seasons.xml");
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

            // Load Show Seasons
            LoadShowSeasons();
        }

        protected override void OnPageDestroy(int new_windowId)
        {
            StopDownload = true;
            PreviousSelectedIndex = Facade.SelectedListItemIndex;
            ClearProperties();

            // save current layout
            TraktSettings.ShowSeasonsDefaultLayout = (int)CurrentLayout;

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
                        var selectedItem = this.Facade.SelectedListItem;
                        if (selectedItem == null) return;

                        var selectedSeason = selectedItem.TVTag as TraktShowSeason;
                        if (selectedSeason == null) return;

                        // create loading parameter for episode listing
                        var loadingParam = new SeasonLoadingParameter
                        {
                            Season = selectedSeason,
                            Show = Show
                        };
                        GUIWindowManager.ActivateWindow((int)TraktGUIWindows.SeasonEpisodes, loadingParam.ToJSON());
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

        protected override void OnShowContextMenu()
        {
            GUIListItem selectedItem = this.Facade.SelectedListItem;
            if (selectedItem == null) return;

            TraktShowSeason selectedSeason = (TraktShowSeason)selectedItem.TVTag;

            IDialogbox dlg = (IDialogbox)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
            if (dlg == null) return;

            dlg.Reset();
            dlg.SetHeading(GUIUtils.PluginName());

            GUIListItem listItem = null;

            // Mark season as watched
            listItem = new GUIListItem(Translation.MarkAsWatched);
            dlg.Add(listItem);
            listItem.ItemId = (int)ContextMenuItem.MarkAsWatched;

            // Add season to Library
            listItem = new GUIListItem(Translation.AddToLibrary);
            dlg.Add(listItem);
            listItem.ItemId = (int)ContextMenuItem.AddToLibrary;

            // Add to Custom List
            listItem = new GUIListItem(Translation.AddToList + "...");
            dlg.Add(listItem);
            listItem.ItemId = (int)ContextMenuItem.AddToList;

            // Sort By
            listItem = new GUIListItem(TraktSettings.SortSeasonsAscending ? Translation.SortSeasonsDescending : Translation.SortSeasonsAscending);
            dlg.Add(listItem);
            listItem.ItemId = (int)ContextMenuItem.Sort;
          
            // Change Layout
            listItem = new GUIListItem(Translation.ChangeLayout);
            dlg.Add(listItem);
            listItem.ItemId = (int)ContextMenuItem.ChangeLayout;

            // Show Context Menu
            dlg.DoModal(GUIWindowManager.ActiveWindow);
            if (dlg.SelectedId < 0) return;

            switch (dlg.SelectedId)
            {
                case ((int)ContextMenuItem.MarkAsWatched):
                    GUICommon.MarkSeasonAsSeen(Show, selectedSeason.Season);
                    break;

                case ((int)ContextMenuItem.AddToLibrary):
                    GUICommon.AddSeasonToLibrary(Show, selectedSeason.Season);
                    break;

                case ((int)ContextMenuItem.AddToList):
                    TraktHelper.AddRemoveSeasonInUserList(Show.Title, Show.Year.ToString(), selectedSeason.Season.ToString() ,Show.Tvdb, false);
                    break;

                case ((int)ContextMenuItem.Sort):
                    TraktSettings.SortSeasonsAscending = !TraktSettings.SortSeasonsAscending;
                    LoadShowSeasons();
                    break;

                case ((int)ContextMenuItem.ChangeLayout):
                    CurrentLayout = GUICommon.ShowLayoutMenu(CurrentLayout);
                    break;

                default:
                    break;
            }

            base.OnShowContextMenu();
        }

        #endregion

        #region Private Methods

        private TraktShowSync CreateSyncData(TraktTrendingShow show)
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

        private void LoadShowSeasons()
        {
            GUIUtils.SetProperty("#Trakt.Items", string.Empty);

            GUIBackgroundTask.Instance.ExecuteInBackgroundAndCallback(() =>
            {
                return ShowSeasons;
            },
            delegate(bool success, object result)
            {
                if (success)
                {
                    IEnumerable<TraktShowSeason> seasons = result as IEnumerable<TraktShowSeason>;
                    SendShowSeasonsToFacade(seasons);
                }
            }, Translation.GettingShowSeasons, true);
        }

        private void SendShowSeasonsToFacade(IEnumerable<TraktShowSeason> seasons)
        {
            // clear facade
            GUIControl.ClearControl(GetID, Facade.GetID);

            if (seasons == null || seasons.Count() == 0)
            {
                GUIUtils.ShowNotifyDialog(GUIUtils.PluginName(), Translation.NoSeasonsForShow);
                GUIWindowManager.ShowPreviousWindow();
                return;
            }

            // sort ascending or descending order
            if (TraktSettings.SortSeasonsAscending)
            {
                seasons = seasons.OrderBy(s => s.Season);
            }

            int itemId = 0;
            List<TraktShowSeason.SeasonImages> seasonImages = new List<TraktShowSeason.SeasonImages>();

            foreach (var season in seasons)
            {
                string itemLabel = season.Season == 0 ? Translation.Specials : string.Format("{0} {1}", Translation.Season, season.Season.ToString());
                GUITraktSeasonListItem item = new GUITraktSeasonListItem(itemLabel);

                item.Label2 = string.Format("{0} {1}", season.EpisodeCount, Translation.Episodes);
                item.TVTag = season;
                item.Item = season.Images;
                item.ItemId = Int32.MaxValue - itemId;
                item.IconImage = "defaultVideo.png";
                item.IconImageBig = "defaultVideoBig.png";
                item.ThumbnailImage = "defaultVideoBig.png";
                item.OnItemSelected += OnSeasonSelected;
                Utils.SetDefaultIcons(item);
                Facade.Add(item);
                itemId++;

                // add image for download
                seasonImages.Add(season.Images);
            }

            // Set Facade Layout
            Facade.SetCurrentLayout(Enum.GetName(typeof(Layout), CurrentLayout));
            GUIControl.FocusControl(GetID, Facade.GetID);

            Facade.SelectIndex(PreviousSelectedIndex);

            // set facade properties
            GUIUtils.SetProperty("#itemcount", seasons.Count().ToString());
            GUIUtils.SetProperty("#Trakt.Items", string.Format("{0} {1}", seasons.Count().ToString(), seasons.Count() > 1 ? Translation.Seasons : Translation.Season));            

            // Download show images Async and set to facade
            GetImages(seasonImages);
        }

        private bool GetLoadingParameter()
        {
            if (_loadParameter == null)
            {
                // maybe re-loading, so check previous window id
                if (Show != null && !string.IsNullOrEmpty(Show.Tvdb))
                    return true;

                return false;
            }
            
            Show = _loadParameter.FromJSON<TraktShow>();
            if (Show == null || string.IsNullOrEmpty(Show.Tvdb)) return false;
            
            return true;
        }

        private void InitProperties()
        {
            // only set property if file exists
            // if we set now and download later, image will not set to skin
            if (File.Exists(Show.Images.FanartImageFilename))
                GUIUtils.SetProperty("#Trakt.Show.Fanart", Show.Images.FanartImageFilename);
       
            // Load Show Properties
            PublishShowSkinProperties(Show);

            // load last layout
            CurrentLayout = (Layout)TraktSettings.ShowSeasonsDefaultLayout;

            // update layout button label
            GUIControl.SetControlLabel(GetID, layoutButton.GetID, GUICommon.GetLayoutTranslation(CurrentLayout));
        }

        private void ClearProperties()
        {
            GUICommon.ClearShowProperties();
            GUICommon.ClearSeasonProperties();
        }

        private void PublishShowSkinProperties(TraktShow show)
        {
            GUICommon.SetShowProperties(show);
        }

        private void PublishSeasonSkinProperties(TraktShowSeason season)
        {
            GUICommon.SetSeasonProperties(season);
        }

        private void OnSeasonSelected(GUIListItem item, GUIControl parent)
        {
            TraktShowSeason season = item.TVTag as TraktShowSeason;
            PublishSeasonSkinProperties(season);
        }

        private void GetImages(List<TraktShowSeason.SeasonImages> itemsWithThumbs)
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
                List<TraktShowSeason.SeasonImages> groupList = new List<TraktShowSeason.SeasonImages>();
                for (int j = groupSize * i; j < groupSize * i + (groupSize * (i + 1) > itemsWithThumbs.Count ? itemsWithThumbs.Count - groupSize * i : groupSize); j++)
                {
                    groupList.Add(itemsWithThumbs[j]);
                }

                new Thread(delegate(object o)
                {
                    List<TraktShowSeason.SeasonImages> items = (List<TraktShowSeason.SeasonImages>)o;
                    foreach (var item in items)
                    {
                        #region Poster
                        // stop download if we have exited window
                        if (StopDownload) break;

                        string remoteThumb = item.Poster;
                        string localThumb = item.PosterImageFilename;

                        if (!string.IsNullOrEmpty(remoteThumb) && !string.IsNullOrEmpty(localThumb))
                        {
                            if (GUIImageHandler.DownloadImage(remoteThumb, localThumb))
                            {
                                // notify that image has been downloaded
                                item.NotifyPropertyChanged("PosterImageFilename");
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

    public class GUITraktSeasonListItem : GUIListItem
    {
        public GUITraktSeasonListItem(string strLabel) : base(strLabel) { }

        public object Item
        {
            get { return _Item; }
            set
            {
                _Item = value;
                INotifyPropertyChanged notifier = value as INotifyPropertyChanged;
                if (notifier != null) notifier.PropertyChanged += (s, e) =>
                {
                    if (s is TraktShowSeason.SeasonImages && e.PropertyName == "PosterImageFilename")
                        SetImageToGui((s as TraktShowSeason.SeasonImages).PosterImageFilename);
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

            ThumbnailImage = imageFilePath;
            IconImage = imageFilePath;
            IconImageBig = imageFilePath;

            // if selected and is current window force an update of thumbnail
            this.UpdateItemIfSelected((int)TraktGUIWindows.ShowSeasons, ItemId);
        }
    }
}
