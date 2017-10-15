using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using MediaPortal.GUI.Library;
using MediaPortal.Util;
using TraktPlugin.Extensions;
using TraktAPI.DataStructures;
using TraktAPI.Extensions;
using TraktPlugin.Cache;
using TraktPlugin.TmdbAPI.DataStructures;
using Action = MediaPortal.GUI.Library.Action;

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
            Trailers,
            Comments,
            Rate,
            MarkAsWatched,
            AddToLibrary,
            AddToList,
            AddToWatchlist,
            Sort,
            ChangeLayout
        }

        #endregion

        #region Constructor

        public GUIShowSeasons() 
        {
        }

        #endregion

        #region Public Properties

        public static string Fanart { get; set; }

        #endregion

        #region Private Variables

        private GUIFacadeControl.Layout CurrentLayout { get; set; }
        DateTime LastRequest = new DateTime();
        int PreviousSelectedIndex = 0;
        TraktShowSummary Show = null;
        Dictionary<string, IEnumerable<TraktSeasonSummary>> Shows = new Dictionary<string, IEnumerable<TraktSeasonSummary>>();

        IEnumerable<TraktSeason> ShowSeasons
        {
            get
            {
                string slug = Show.Ids.Trakt.ToString();
                if (string.IsNullOrEmpty(slug))
                {
                    if (Show.Ids.Imdb != null)
                    {
                        slug = Show.Ids.Imdb;
                    }
                    else
                    {
                        slug = Show.Title.StripYear(Show.Year).ToSlug();
                    }
                }

                if (!Shows.Keys.Contains(slug) || LastRequest < DateTime.UtcNow.Subtract(new TimeSpan(0, TraktSettings.WebRequestCacheMinutes, 0)))
                {
                    _ShowSeasons = TraktAPI.TraktAPI.GetShowSeasons(slug);

                    if (Shows.Keys.Contains(slug))
                        Shows.Remove(slug);

                    Shows.Add(slug, _ShowSeasons);
                    LastRequest = DateTime.UtcNow;
                    PreviousSelectedIndex = 0;
                }
                return Shows[slug];
            }
        }
        private IEnumerable<TraktSeasonSummary> _ShowSeasons = null;

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
            Fanart = null;
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

                        var selectedSeason = selectedItem.TVTag as TraktSeasonSummary;
                        if (selectedSeason == null) return;

                        // don't bother loading seasons view if there is no episodes to display
                        if (selectedSeason.EpisodeCount > 0)
                        {
                            // create loading parameter for episode listing
                            var loadingParam = new SeasonLoadingParameter
                            {
                                Season = selectedSeason,
                                Show = Show
                            };
                            GUIWindowManager.ActivateWindow((int)TraktGUIWindows.SeasonEpisodes, loadingParam.ToJSON());
                        }
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

            var selectedSeason = selectedItem.TVTag as TraktSeasonSummary;

            var dlg = (IDialogbox)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
            if (dlg == null) return;

            dlg.Reset();
            dlg.SetHeading(GUIUtils.PluginName());

            GUIListItem listItem = null;

            // Trailers
            if (TraktHelper.IsTrailersAvailableAndEnabled)
            {
                listItem = new GUIListItem(Translation.Trailers);
                dlg.Add(listItem);
                listItem.ItemId = (int)ContextMenuItem.Trailers;
            }

            // Comments
            listItem = new GUIListItem(Translation.Comments);
            dlg.Add(listItem);
            listItem.ItemId = (int)ContextMenuItem.Comments;

            // Rate
            listItem = new GUIListItem(Translation.Rate + "...");
            dlg.Add(listItem);
            listItem.ItemId = (int)ContextMenuItem.Rate;

            // Mark season as watched
            listItem = new GUIListItem(Translation.MarkAsWatched);
            dlg.Add(listItem);
            listItem.ItemId = (int)ContextMenuItem.MarkAsWatched;

            // Add season to Library
            listItem = new GUIListItem(Translation.AddToLibrary);
            dlg.Add(listItem);
            listItem.ItemId = (int)ContextMenuItem.AddToLibrary;

            // Add to Custom List
            listItem = new GUIListItem(Translation.AddToList);
            dlg.Add(listItem);
            listItem.ItemId = (int)ContextMenuItem.AddToList;

            // Add to Watchlist
            listItem = new GUIListItem(Translation.AddToWatchList);
            dlg.Add(listItem);
            listItem.ItemId = (int)ContextMenuItem.AddToWatchlist;

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
                case ((int)ContextMenuItem.Trailers):
                    GUICommon.ShowTVSeasonTrailersPluginMenu(Show, selectedSeason.Number);
                    break;

                case ((int)ContextMenuItem.Comments):
                    TraktHelper.ShowTVSeasonShouts(Show, selectedSeason);
                    break;

                case ((int)ContextMenuItem.Rate):
                    GUICommon.RateSeason(Show, selectedSeason);
                    OnSeasonSelected(selectedItem, Facade);
                    (Facade.SelectedListItem as GUISeasonListItem).Images.NotifyPropertyChanged("Poster");
                    break;

                case ((int)ContextMenuItem.MarkAsWatched):
                    GUICommon.MarkSeasonAsWatched(Show, selectedSeason.Number);
                    OnSeasonSelected(selectedItem, Facade);
                    (Facade.SelectedListItem as GUISeasonListItem).Images.NotifyPropertyChanged("Poster");
                    break;

                case ((int)ContextMenuItem.AddToLibrary):
                    GUICommon.AddSeasonToLibrary(Show, selectedSeason.Number);
                    OnSeasonSelected(selectedItem, Facade);
                    (Facade.SelectedListItem as GUISeasonListItem).Images.NotifyPropertyChanged("Poster");
                    break;

                case ((int)ContextMenuItem.AddToList):
                    TraktHelper.AddRemoveSeasonInUserList(selectedSeason, false);
                    break;

                case ((int)ContextMenuItem.AddToWatchlist):
                    TraktHelper.AddSeasonToWatchList(Show, selectedSeason.Number);
                    OnSeasonSelected(selectedItem, Facade);
                    (Facade.SelectedListItem as GUISeasonListItem).Images.NotifyPropertyChanged("Poster");
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
                IEnumerable<TraktSeason> seasons = null;
                var threads = new List<Thread>();

                var t1 = new Thread(() => { seasons = ShowSeasons; });
                var t2 = new Thread(() => GetShowDetail());

                t1.Start();
                t1.Name = "Seasons";

                t2.Start();
                t2.Name = "Summary";

                threads.Add(t1);
                threads.Add(t2);

                // wait until all results are back
                threads.ForEach(t => t.Join());

                return seasons;
            },
            delegate(bool success, object result)
            {
                if (success)
                {
                    var seasons = result as IEnumerable<TraktSeasonSummary>;
                    SendShowSeasonsToFacade(seasons);
                }
            }, Translation.GettingShowSeasons, true);
        }

        private void GetShowDetail()
        {
            // check if we need to fetch the show detail as well
            // currently the season API does not give back any show 
            // summary info except for images
            if (Show.UpdatedAt == null)
            {
                string slug = Show.Ids.Trakt.ToString();
                if (string.IsNullOrEmpty(slug))
                {
                    if (Show.Ids.Imdb != null)
                    {
                        slug = Show.Ids.Imdb;
                    }
                    else
                    {
                        slug = Show.Title.StripYear(Show.Year).ToSlug();
                    }
                }

                var showSummary = TraktAPI.TraktAPI.GetShowSummary(slug);
                if (showSummary != null)
                {
                    Show = showSummary;

                    // Load Show Properties
                    PublishShowSkinProperties(Show);

                    // Publish Fanart
                    var showImages = TmdbCache.GetShowImages(showSummary.Ids.Tmdb);
                    var showBackdropFilename = TmdbCache.GetShowBackdropFilename(showImages);
                    if (showBackdropFilename == null)
                        return;

                    if (File.Exists(showBackdropFilename))
                    {
                        GUIUtils.SetProperty("#Trakt.Show.Fanart", showBackdropFilename);
                    }
                }
            }
        }

        private void SendShowSeasonsToFacade(IEnumerable<TraktSeasonSummary> seasons)
        {
            // clear facade
            GUIControl.ClearControl(GetID, Facade.GetID);

            if (seasons == null)
            {
                GUIUtils.ShowNotifyDialog(Translation.Error, Translation.ErrorGeneral);
                GUIWindowManager.ShowPreviousWindow();
                return;
            }

            if (seasons.Count() == 0)
            {
                GUIUtils.ShowNotifyDialog(GUIUtils.PluginName(), Translation.NoSeasonsForShow);
                GUIWindowManager.ShowPreviousWindow();
                return;
            }

            // sort ascending or descending order
            if (!TraktSettings.SortSeasonsAscending)
            {
                seasons = seasons.OrderByDescending(s => s.Number).ToList();
            }
            else
            {
                seasons = seasons.OrderBy(s => s.Number).ToList();
            }

            int itemId = 0;
            var seasonImages = new List<GUITmdbImage>();

            // skip over any seasons with no episodes
            foreach (var season in seasons.Where(s => s.EpisodeCount > 0))
            {
                // add image for download
                var images = new GUITmdbImage { SeasonImages = new TmdbSeasonImages { Id = Show.Ids.Tmdb, Season = season.Number } };
                seasonImages.Add(images);
                
                // if a custom title is set for the season, use that, otherwise use a translated string
                string itemLabel = season.Number == 0 ? Translation.Specials : string.Format("{0} {1}", Translation.Season, season.Number.ToString());
                if (!string.IsNullOrEmpty(season.Title) && itemLabel != season.Title)
                    itemLabel = string.Format("{0}. {1}", season.Number, season.Title);

                var item = new GUISeasonListItem(itemLabel, (int)TraktGUIWindows.ShowSeasons);

                item.Label2 = string.Format("{0} {1}", season.EpisodeCount, Translation.Episodes);
                item.TVTag = season;
                item.Show = Show;
                item.Season = season;
                item.Images = images;
                item.ItemId = Int32.MaxValue - itemId;
                item.IsPlayed = season.IsWatched(Show);
                item.IconImage = GUIImageHandler.GetDefaultPoster(false);
                item.IconImageBig = GUIImageHandler.GetDefaultPoster();
                item.ThumbnailImage = GUIImageHandler.GetDefaultPoster();
                item.OnItemSelected += OnSeasonSelected;
                Utils.SetDefaultIcons(item);
                Facade.Add(item);
                itemId++;
            }

            // Set Facade Layout
            Facade.CurrentLayout = CurrentLayout;
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
                if (Show != null && (Show.Ids.Trakt != null || Show.Ids.Imdb != null || Show.Title != null))
                    return true;

                return false;
            }
            
            Show = _loadParameter.FromJSON<TraktShowSummary>();
            if (Show == null || Show.Ids == null)
                return false;
            
            return true;
        }

        private void InitProperties()
        {
            // only set property if file exists
            // if we set now and download later, image will not set to skin
            var showImages = TmdbCache.GetShowImages(Show.Ids.Tmdb, true);
            var backdropFilename = TmdbCache.GetShowBackdropFilename(showImages);

            if (backdropFilename != null)
            {
                if (File.Exists(backdropFilename))
                {
                    GUIUtils.SetProperty("#Trakt.Show.Fanart", backdropFilename);
                }
            }
            else if (Fanart != null)
            {
                if (File.Exists(Fanart))
                {
                    GUIUtils.SetProperty("#Trakt.Show.Fanart", Fanart);
                }
            }
            
            // Load Show Properties
            PublishShowSkinProperties(Show);

            // load last layout
            CurrentLayout = (GUIFacadeControl.Layout)TraktSettings.ShowSeasonsDefaultLayout;

            // update layout button label
            GUIControl.SetControlLabel(GetID, layoutButton.GetID, GUICommon.GetLayoutTranslation(CurrentLayout));
        }

        private void ClearProperties()
        {
            GUICommon.ClearShowProperties();
            GUICommon.ClearSeasonProperties();
        }

        private void PublishShowSkinProperties(TraktShowSummary show)
        {
            GUICommon.SetShowProperties(show);
        }

        private void PublishSeasonSkinProperties(TraktSeasonSummary season)
        {
            GUICommon.SetSeasonProperties(Show, season);
        }

        private void OnSeasonSelected(GUIListItem item, GUIControl parent)
        {
            PreviousSelectedIndex = Facade.SelectedListItemIndex;

            var season = item.TVTag as TraktSeasonSummary;
            PublishSeasonSkinProperties(season);
        }
        #endregion
    }
}
