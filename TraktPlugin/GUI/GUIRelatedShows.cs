using System;
using System.Collections.Generic;
using System.Linq;
using MediaPortal.GUI.Library;
using MediaPortal.Util;
using TraktPlugin.TraktAPI.DataStructures;
using TraktPlugin.TraktAPI.Extensions;
using Action = MediaPortal.GUI.Library.Action;

namespace TraktPlugin.GUI
{
    public class RelatedShow
    {
        public string Title { get; set; }
        public int? Year { get; set; }
        public int? TvdbId { get; set; }
        public int? TmdbId { get; set; }
        public int? TraktId { get; set; }
        public string ImdbId { get; set; }

        public string Slug
        {
            get
            {
                if (TraktId != null) return TraktId.ToString();
                if (ImdbId != null) return ImdbId;
                // TODO: Find trakt id from other IDs
                //if (TmdbId != null) return TmdbId.ToString();
                //if (TvdbId != null) return TvdbId.ToString();

                if (!string.IsNullOrEmpty(TraktHandlers.BasicHandler.GetProperImdbId(ImdbId)))
                    return ImdbId;

                return Title.StripYear(Year).ToSlug();
            }
        }
    }

    public class GUIRelatedShows : GUIWindow
    {
        #region Skin Controls

        [SkinControl(2)]
        protected GUIButtonControl layoutButton = null;

        [SkinControl(3)]
        protected GUICheckButton hideWatchedButton = null;

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
            HideShowWatched,
            MarkAsWatched,
            AddToLibrary,
            AddToWatchList,
            RemoveFromWatchList,
            AddToList,
            Trailers,
            Shouts,
            Related,
            Rate,
            ChangeLayout,
            SearchWithMpNZB,
            SearchTorrent
        }

        #endregion

        #region Constructor

        public GUIRelatedShows()
        {
            backdrop = new ImageSwapper();
            backdrop.PropertyOne = "#Trakt.RelatedShows.Fanart.1";
            backdrop.PropertyTwo = "#Trakt.RelatedShows.Fanart.2";
        }

        #endregion

        #region Public Variables

        public static RelatedShow relatedShow { get; set; }

        #endregion

        #region Private Variables

        private Layout CurrentLayout { get; set; }
        private ImageSwapper backdrop;
        DateTime LastRequest = new DateTime();
        int PreviousSelectedIndex = 0;
        Dictionary<string, IEnumerable<TraktShowSummary>> dictRelatedShows = new Dictionary<string, IEnumerable<TraktShowSummary>>();
        bool HideWatched = false;
        bool RelationChanged = false;

        IEnumerable<TraktShowSummary> RelatedShows
        {
            get
            {
                if (!dictRelatedShows.Keys.Contains(relatedShow.Slug) || LastRequest < DateTime.UtcNow.Subtract(new TimeSpan(0, TraktSettings.WebRequestCacheMinutes, 0)))
                {
                    // TODO Hide Watched (Not Implemented on Server)
                    _RelatedShows = TraktAPI.TraktAPI.GetRelatedShows(relatedShow.Slug, HideWatched);
                    if (dictRelatedShows.Keys.Contains(relatedShow.Slug)) dictRelatedShows.Remove(relatedShow.Slug);
                    dictRelatedShows.Add(relatedShow.Slug, _RelatedShows);
                    LastRequest = DateTime.UtcNow;
                    PreviousSelectedIndex = 0;
                }
                return _RelatedShows;
            }
        }
        private IEnumerable<TraktShowSummary> _RelatedShows = null;

        #endregion

        #region Base Overrides

        public override int GetID
        {
            get
            {
                return (int)TraktGUIWindows.RelatedShows;
            }
        }

        public override bool Init()
        {
            return Load(GUIGraphicsContext.Skin + @"\Trakt.Related.Shows.xml");
        }

        protected override void OnPageLoad()
        {
            base.OnPageLoad();

            if (relatedShow == null)
            {
                GUIWindowManager.ActivateWindow(GUIWindowManager.GetPreviousActiveWindow());
                return;
            }

            // Clear GUI Properties
            ClearProperties();

            // Init Properties
            InitProperties();

            // Load Related Shows
            LoadRelatedShows();
        }

        protected override void OnPageDestroy(int new_windowId)
        {
            GUIShowListItem.StopDownload = true;
            ClearProperties();

            if (RelationChanged)
                PreviousSelectedIndex = 0;
            else
                PreviousSelectedIndex = Facade.SelectedListItemIndex;

            // save settings
            TraktSettings.RelatedShowsDefaultLayout = (int)CurrentLayout;
            TraktSettings.HideWatchedRelatedShows = HideWatched;

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

                            var selectedShow = (TraktShowSummary)selectedItem.TVTag;
                            GUIWindowManager.ActivateWindow((int)TraktGUIWindows.ShowSeasons, selectedShow.ToJSON());
                        }
                    }
                    break;

                // Layout Button
                case (2):
                    CurrentLayout = GUICommon.ShowLayoutMenu(CurrentLayout, PreviousSelectedIndex);
                    break;

                // Hide Watched Button
                case (3):
                    GUIUtils.ShowNotifyDialog("Trakt", Translation.FeatureNotAvailable);
                    break;
                    //TODO
                    //HideWatched = hideWatchedButton.Selected;
                    //dictRelatedShows.Remove(relatedShow.Slug);
                    //LoadRelatedShows();
                    //GUIControl.FocusControl((int)TraktGUIWindows.RelatedShows, Facade.GetID);
                    //break;

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
                default:
                    base.OnAction(action);
                    break;
            }
        }

        protected override void OnShowContextMenu()
        {
            if (GUIBackgroundTask.Instance.IsBusy) return;

            var selectedItem = this.Facade.SelectedListItem;
            if (selectedItem == null) return;

            var selectedShow = selectedItem.TVTag as TraktShowSummary;
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

            // Hide/Show Watched items
            listItem = new GUIListItem(HideWatched ? Translation.ShowWatched : Translation.HideWatched);
            dlg.Add(listItem);
            listItem.ItemId = (int)ContextMenuItem.HideShowWatched;

            // Mark Show As Watched
            listItem = new GUIListItem(Translation.MarkAsWatched);
            dlg.Add(listItem);
            listItem.ItemId = (int)ContextMenuItem.MarkAsWatched;

            // Add Show to Library
            listItem = new GUIListItem(Translation.AddToLibrary);
            dlg.Add(listItem);
            listItem.ItemId = (int)ContextMenuItem.AddToLibrary;

            // Add/Remove Watchlist            
            if (!selectedShow.IsWatchlisted())
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

            if (TraktHelper.IsTrailersAvailableAndEnabled)
            {
                listItem = new GUIListItem(Translation.Trailers);
                dlg.Add(listItem);
                listItem.ItemId = (int)ContextMenuItem.Trailers;
            }

            // Related Shows
            listItem = new GUIListItem(Translation.RelatedShows);
            dlg.Add(listItem);
            listItem.ItemId = (int)ContextMenuItem.Related;

            // Rate Show
            listItem = new GUIListItem(Translation.RateShow);
            dlg.Add(listItem);
            listItem.ItemId = (int)ContextMenuItem.Rate;

            // Shouts
            listItem = new GUIListItem(Translation.Comments);
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
                case ((int)ContextMenuItem.ShowSeasonInfo):
                    GUIWindowManager.ActivateWindow((int)TraktGUIWindows.ShowSeasons, selectedShow.ToJSON());
                    break;

                case ((int)ContextMenuItem.HideShowWatched):
                    HideWatched = !HideWatched;
                    if (hideWatchedButton != null) hideWatchedButton.Selected = HideWatched;
                    dictRelatedShows.Remove(relatedShow.Slug);
                    LoadRelatedShows();
                    break;

                case ((int)ContextMenuItem.MarkAsWatched):
                    GUICommon.MarkShowAsWatched(selectedShow);
                    break;

                case ((int)ContextMenuItem.AddToLibrary):
                    GUICommon.AddShowToCollection(selectedShow);
                    break;

                case ((int)ContextMenuItem.AddToWatchList):
                    TraktHelper.AddShowToWatchList(selectedShow);
                    OnShowSelected(selectedItem, Facade);
                    (Facade.SelectedListItem as GUIShowListItem).Images.NotifyPropertyChanged("Poster");
                    break;

                case ((int)ContextMenuItem.RemoveFromWatchList):
                    TraktHelper.RemoveShowFromWatchList(selectedShow);
                    OnShowSelected(selectedItem, Facade);
                    (Facade.SelectedListItem as GUIShowListItem).Images.NotifyPropertyChanged("Poster");
                    break;

                case ((int)ContextMenuItem.AddToList):
                    TraktHelper.AddRemoveShowInUserList(selectedShow, false);
                    break;

                case ((int)ContextMenuItem.Trailers):
                    GUICommon.ShowTVShowTrailersMenu(selectedShow);
                    break;

                case ((int)ContextMenuItem.Shouts):
                    TraktHelper.ShowTVShowShouts(selectedShow);
                    break;

                case ((int)ContextMenuItem.Related):
                    var relShow = new RelatedShow
                    {
                        TraktId = selectedShow.Ids.Trakt,
                        TmdbId = selectedShow.Ids.Tmdb,
                        TvdbId = selectedShow.Ids.Tvdb,
                        Title = selectedShow.Title,
                        Year = selectedShow.Year
                        
                    };
                    relatedShow = relShow;
                    LoadRelatedShows();
                    GUIUtils.SetProperty("#Trakt.Related.Show", relatedShow.Title);
                    RelationChanged = true;
                    break;

                case ((int)ContextMenuItem.Rate):
                    GUICommon.RateShow(selectedShow);
                    OnShowSelected(selectedItem, Facade);
                    (Facade.SelectedListItem as GUIShowListItem).Images.NotifyPropertyChanged("Poster");
                    break;

                case ((int)ContextMenuItem.ChangeLayout):
                    CurrentLayout = GUICommon.ShowLayoutMenu(CurrentLayout, PreviousSelectedIndex);
                    break;

                case ((int)ContextMenuItem.SearchWithMpNZB):
                    string loadingParam = string.Format("search:{0}", selectedShow.Title);
                    GUIWindowManager.ActivateWindow((int)ExternalPluginWindows.MpNZB, loadingParam);
                    break;

                case ((int)ContextMenuItem.SearchTorrent):
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

            var selectedShow = selectedItem.TVTag as TraktShowSummary;
            GUICommon.CheckAndPlayFirstUnwatchedEpisode(selectedShow, jumpTo);
        }

        private void LoadRelatedShows()
        {
            GUIUtils.SetProperty("#Trakt.Items", string.Empty);

            GUIBackgroundTask.Instance.ExecuteInBackgroundAndCallback(() =>
            {
                if (hideWatchedButton != null)
                {
                    GUIControl.DisableControl((int)TraktGUIWindows.RelatedShows, hideWatchedButton.GetID);
                }
                return RelatedShows;
            },
            delegate(bool success, object result)
            {
                if (hideWatchedButton != null)
                {
                    GUIControl.EnableControl((int)TraktGUIWindows.RelatedShows, hideWatchedButton.GetID);
                }

                if (success)
                {
                    IEnumerable<TraktShowSummary> shows = result as IEnumerable<TraktShowSummary>;
                    SendRelatedShowsToFacade(shows);
                }
            }, Translation.GettingRelatedShows, true);
        }

        private void SendRelatedShowsToFacade(IEnumerable<TraktShowSummary> shows)
        {
            // clear facade
            GUIControl.ClearControl(GetID, Facade.GetID);

            if (shows.Count() == 0)
            {
                string title = string.IsNullOrEmpty(relatedShow.Title) ? relatedShow.Slug : relatedShow.Title;
                GUIUtils.ShowNotifyDialog(GUIUtils.PluginName(), string.Format(Translation.NoRelatedShows, title));
                GUIWindowManager.ShowPreviousWindow();
                return;
            }

            int itemId = 0;
            var showImages = new List<GUITraktImage>();

            foreach (var show in shows)
            {
                var item = new GUIShowListItem(show.Title, (int)TraktGUIWindows.RelatedShows);

                // add image for download
                var images = new GUITraktImage { ShowImages = show.Images };
                showImages.Add(images);
                
                item.Label2 = show.Year.ToString();
                item.TVTag = show;
                item.Show = show;
                item.Show = show;
                item.Images = images;
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
            
            // Download show images Async and set to facade
            GUIShowListItem.GetImages(showImages);
        }

        private void InitProperties()
        {
            // Fanart
            backdrop.GUIImageOne = FanartBackground;
            backdrop.GUIImageTwo = FanartBackground2;
            backdrop.LoadingImage = loadingImage;

            // set context property
            string title = string.IsNullOrEmpty(relatedShow.Title) ? relatedShow.TvdbId.ToString() : relatedShow.Title;
            GUIUtils.SetProperty("#Trakt.Related.Show", title);

            // hide watched
            HideWatched = TraktSettings.HideWatchedRelatedShows;
            // hide watched            
            if (hideWatchedButton != null)
            {
                GUIControl.SetControlLabel((int)TraktGUIWindows.RelatedShows, hideWatchedButton.GetID, Translation.HideWatched);
                hideWatchedButton.Selected = HideWatched;
            }

            // no changes yet
            RelationChanged = false;

            // load last layout
            CurrentLayout = (Layout)TraktSettings.RelatedShowsDefaultLayout;
            // update button label
            GUIControl.SetControlLabel(GetID, layoutButton.GetID, GUICommon.GetLayoutTranslation(CurrentLayout));
        }

        private void ClearProperties()
        {
            GUIUtils.SetProperty("#Trakt.Related.Show", string.Empty);
            GUICommon.ClearShowProperties();            
        }

        private void OnShowSelected(GUIListItem item, GUIControl parent)
        {
            PreviousSelectedIndex = Facade.SelectedListItemIndex;

            var selectedShow = item.TVTag as TraktShowSummary;
            GUICommon.SetShowProperties(selectedShow);

            GUIImageHandler.LoadFanart(backdrop, selectedShow.Images.Fanart.LocalImageFilename(ArtworkType.ShowFanart));
        }
        #endregion
    }
}
