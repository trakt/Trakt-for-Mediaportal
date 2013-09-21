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
            GUISeasonListItem.StopDownload = true;
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
                    CurrentLayout = GUICommon.ShowLayoutMenu(CurrentLayout, PreviousSelectedIndex);
                    break;

                default:
                    break;
            }
            base.OnClicked(controlId, control, actionType);
        }

        protected override void OnShowContextMenu()
        {
            var selectedItem = this.Facade.SelectedListItem;
            if (selectedItem == null) return;

            var selectedSeason = selectedItem.TVTag as TraktShowSeason;

            var dlg = (IDialogbox)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
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
                    CurrentLayout = GUICommon.ShowLayoutMenu(CurrentLayout, PreviousSelectedIndex);
                    break;

                default:
                    break;
            }

            base.OnShowContextMenu();
        }

        #endregion

        #region Private Methods

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
            var seasonImages = new List<TraktImage>();

            foreach (var season in seasons)
            {
                // add image for download
                var images = new TraktImage { SeasonImages = season.Images, ShowImages = Show.Images };
                seasonImages.Add(images);

                string itemLabel = season.Season == 0 ? Translation.Specials : string.Format("{0} {1}", Translation.Season, season.Season.ToString());
                var item = new GUISeasonListItem(itemLabel, (int)TraktGUIWindows.ShowSeasons);

                item.Label2 = string.Format("{0} {1}", season.EpisodeCount, Translation.Episodes);
                item.TVTag = season;
                item.Item = images;
                item.ItemId = Int32.MaxValue - itemId;
                item.IconImage = GUIImageHandler.GetDefaultPoster(false);
                item.IconImageBig = GUIImageHandler.GetDefaultPoster();
                item.ThumbnailImage = GUIImageHandler.GetDefaultPoster();
                item.OnItemSelected += OnSeasonSelected;
                Utils.SetDefaultIcons(item);
                Facade.Add(item);
                itemId++;
            }

            // Set Facade Layout
            Facade.SetCurrentLayout(Enum.GetName(typeof(Layout), CurrentLayout));
            GUIControl.FocusControl(GetID, Facade.GetID);

            Facade.SelectIndex(PreviousSelectedIndex);

            // set facade properties
            GUIUtils.SetProperty("#itemcount", seasons.Count().ToString());
            GUIUtils.SetProperty("#Trakt.Items", string.Format("{0} {1}", seasons.Count().ToString(), seasons.Count() > 1 ? Translation.Seasons : Translation.Season));            

            // Download show images Async and set to facade
            GUISeasonListItem.GetImages(seasonImages);
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
            if (File.Exists(Show.Images.Fanart.LocalImageFilename(ArtworkType.ShowFanart)))
                GUIUtils.SetProperty("#Trakt.Show.Fanart", Show.Images.Fanart.LocalImageFilename(ArtworkType.ShowFanart));
       
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
            PreviousSelectedIndex = Facade.SelectedListItemIndex;

            var season = item.TVTag as TraktShowSeason;
            PublishSeasonSkinProperties(season);
        }
        #endregion
    }
}
