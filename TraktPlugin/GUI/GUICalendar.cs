using System;
using System.Collections.Generic;
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
    public class GUICalendar : GUIWindow
    {
        #region Skin Controls

        [SkinControl(2)]
        protected GUIButtonControl viewButton = null;

        [SkinControl(3)]
        protected GUIButtonControl startDateButton = null;

        [SkinControl(4)]
        protected GUICheckButton hideWatchlistedButton = null;

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

        enum FacadeMovement
        {
            Up,
            Down
        }

        enum CalendarType
        {
            MyShows,
            Premieres,
            AllShows
        }

        enum ContextMenuItem
        {
            View,
            StartDate,
            ShowSeasonInfo,
            MarkAsWatched,
            MarkAsUnWatched,
            AddShowToList,
            AddEpisodeToList,
            AddShowToWatchList,
            AddEpisodeToWatchList,
            RemoveShowFromWatchList,
            RemoveEpisodeFromWatchList,
            Related,
            AddToLibrary,
            RemoveFromLibrary,
            Rate,
            Shouts,
            Trailers,
            WatchlistFilter
        }

        enum StartDates
        {
            Today,
            Yesterday,
            OneWeekAgo,
            TwoWeeksAgo,
            OneMonthAgo
        }

        #endregion

        #region Constructor

        public GUICalendar()
        {
            backdrop = new ImageSwapper();
            backdrop.PropertyOne = "#Trakt.Calendar.Fanart.1";
            backdrop.PropertyTwo = "#Trakt.Calendar.Fanart.2";
        }

        #endregion

        #region Private Variables

        FacadeMovement LastMoved { get; set; }
        CalendarType CurrentCalendar { get; set; }
        StartDates CurrentStartDate { get; set; }
        int CurrentWeekDays = 7;
        int PreviousSelectedIndex;
        int PreviousCalendarDayCount;
        bool IsCached = false;
        ImageSwapper backdrop;
        DateTime LastRequest = new DateTime();        

        #endregion

        #region Private Properties

        IEnumerable<TraktCalendar> TraktCalendarMyShows
        {
            get
            {
                if (_CalendarMyShows == null || LastRequest < DateTime.UtcNow.Subtract(new TimeSpan(0, TraktSettings.WebRequestCacheMinutes, 0)))
                {
                    _CalendarMyShows = TraktAPI.TraktAPI.GetCalendarForUser(TraktSettings.Username, GetStartDate().ToString("yyyyMMdd"), GetDaysForward());
                    LastRequest = DateTime.UtcNow;
                    IsCached = false;
                }
                return _CalendarMyShows;
            }
        }
        static IEnumerable<TraktCalendar> _CalendarMyShows = null;

        IEnumerable<TraktCalendar> TraktCalendarPremieres
        {
            get
            {
                if (_CalendarPremieres == null || LastRequest < DateTime.UtcNow.Subtract(new TimeSpan(0, TraktSettings.WebRequestCacheMinutes, 0)))
                {
                    _CalendarPremieres = TraktAPI.TraktAPI.GetCalendarPremieres(GetStartDate().ToString("yyyyMMdd"), GetDaysForward());
                    LastRequest = DateTime.UtcNow;
                    IsCached = false;
                }
                return _CalendarPremieres;
            }
        }
        private IEnumerable<TraktCalendar> _CalendarPremieres = null;

        IEnumerable<TraktCalendar> TraktCalendarAllShows
        {
            get
            {
                if (_CalendarAllShows == null || LastRequest < DateTime.UtcNow.Subtract(new TimeSpan(0, TraktSettings.WebRequestCacheMinutes, 0)))
                {
                    _CalendarAllShows = TraktAPI.TraktAPI.GetCalendarShows(GetStartDate().ToString("yyyyMMdd"), GetDaysForward());
                    LastRequest = DateTime.UtcNow;
                    IsCached = false;
                }
                return _CalendarAllShows;
            }
        }
        private IEnumerable<TraktCalendar> _CalendarAllShows = null;

        #endregion

        #region Base Overrides

        public override int GetID
        {
            get
            {
                return (int)TraktGUIWindows.Calendar;
            }
        }

        public override bool Init()
        {
            return Load(GUIGraphicsContext.Skin + @"\Trakt.Calendar.xml");
        }

        protected override void OnPageLoad()
        {
            base.OnPageLoad();

            // Clear GUI properties
            ClearProperties();

            // Requires Login
            if (!GUICommon.CheckLogin()) return;

            // Init Properties
            InitProperties();

            // Load Calendar
            LoadCalendar();
        }

        protected override void OnPageDestroy(int new_windowId)
        {
            PreviousSelectedIndex = Facade.SelectedListItemIndex;
            switch (CurrentCalendar)
            {
                case CalendarType.MyShows:
                    if (_CalendarMyShows != null) PreviousCalendarDayCount = _CalendarMyShows.Count();
                    break;

                case CalendarType.Premieres:
                    if (_CalendarPremieres != null) PreviousCalendarDayCount = _CalendarPremieres.Count();
                    break;

                case CalendarType.AllShows:
                    if (_CalendarAllShows != null) PreviousCalendarDayCount = _CalendarAllShows.Count();
                    break;
            }

            GUIEpisodeListItem.StopDownload = true;
            ClearProperties();

            // save current view
            TraktSettings.DefaultCalendarView = (int)CurrentCalendar;
            TraktSettings.DefaultCalendarStartDate = (int)CurrentStartDate;

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
                        GUIListItem item = Facade.SelectedListItem as GUIListItem;

                        // Is a group header
                        if (item !=null && item.IsFolder)
                        {
                            switch (CurrentCalendar)
                            {
                                case CalendarType.MyShows:
                                    // previous call may have timedout
                                    if (_CalendarMyShows != null)
                                    {
                                        PreviousCalendarDayCount = _CalendarMyShows.Count();
                                        PreviousSelectedIndex = Facade.SelectedListItemIndex;
                                        CurrentWeekDays += 7;
                                        _CalendarMyShows = null;
                                    }
                                    break;

                                case CalendarType.Premieres:
                                    if (_CalendarPremieres != null)
                                    {
                                        PreviousCalendarDayCount = _CalendarPremieres.Count();
                                        PreviousSelectedIndex = Facade.SelectedListItemIndex;
                                        CurrentWeekDays += 7;
                                        _CalendarPremieres = null;
                                    }
                                    break;

                                case CalendarType.AllShows:
                                    if (_CalendarAllShows != null)
                                    {
                                        PreviousCalendarDayCount = _CalendarAllShows.Count();
                                        PreviousSelectedIndex = Facade.SelectedListItemIndex;
                                        CurrentWeekDays += 7;
                                        _CalendarAllShows = null;
                                    }
                                    break;
                            }

                            // load next 7 days in calendar
                            LoadCalendar();
                        }

                        // Is an episode
                        if (item != null && !item.IsFolder)
                        {
                            CheckAndPlayEpisode();
                        }
                    }
                    break;

                // View Button
                case (2):
                    ShowViewMenu();
                    break;

                // Start Date Button
                case (3):
                    ShowStartDateMenu();
                    break;

                // Hide Watchlisted
                case (4):
                    TraktSettings.CalendarHideTVShowsInWatchList = !TraktSettings.CalendarHideTVShowsInWatchList;
                    LoadCalendar();
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

                case Action.ActionType.ACTION_PREVIOUS_MENU:
                    break;

                case Action.ActionType.ACTION_MOVE_UP:
                case Action.ActionType.ACTION_PAGE_UP:
                    LastMoved = FacadeMovement.Up;                    
                    break;

                case Action.ActionType.ACTION_MOVE_DOWN:
                case Action.ActionType.ACTION_PAGE_DOWN:
                    LastMoved = FacadeMovement.Down;
                    break;
            }
            base.OnAction(action);
        }

        protected override void OnShowContextMenu()
        {
            IDialogbox dlg = (IDialogbox)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
            if (dlg == null) return;

            dlg.Reset();
            dlg.SetHeading(GUIUtils.PluginName());

            var episodeItem = Facade.SelectedListItem.TVTag as TraktEpisodeSummary;
            if (episodeItem == null) return;

            // Create Views Menu Item
            GUIListItem listItem = new GUIListItem(Translation.ChangeView);
            dlg.Add(listItem);
            listItem.ItemId = (int)ContextMenuItem.View;

            // Start Date
            listItem = new GUIListItem(Translation.StartDate + "...");
            dlg.Add(listItem);
            listItem.ItemId = (int)ContextMenuItem.StartDate;

            // Show Season Information
            listItem = new GUIListItem(Translation.ShowSeasonInfo);
            dlg.Add(listItem);
            listItem.ItemId = (int)ContextMenuItem.ShowSeasonInfo;

            // Related Shows
            listItem = new GUIListItem(Translation.RelatedShows + "...");
            dlg.Add(listItem);
            listItem.ItemId = (int)ContextMenuItem.Related;

            // Rate
            listItem = new GUIListItem(Translation.Rate + "...");
            dlg.Add(listItem);
            listItem.ItemId = (int)ContextMenuItem.Rate;

            // Mark As Watched
            if (!episodeItem.Episode.Watched)
            {
                listItem = new GUIListItem(Translation.MarkAsWatched);
                dlg.Add(listItem);
                listItem.ItemId = (int)ContextMenuItem.MarkAsWatched;
            }

            // Mark As UnWatched
            if (episodeItem.Episode.Watched)
            {
                listItem = new GUIListItem(Translation.MarkAsUnWatched);
                dlg.Add(listItem);
                listItem.ItemId = (int)ContextMenuItem.MarkAsUnWatched;
            }

            // Add/Remove Show Watch List
            if (!episodeItem.Show.InWatchList)
            {
                listItem = new GUIListItem(Translation.AddShowToWatchList);
                dlg.Add(listItem);
                listItem.ItemId = (int)ContextMenuItem.AddShowToWatchList;
            }
            else
            {
                listItem = new GUIListItem(Translation.RemoveShowFromWatchList);
                dlg.Add(listItem);
                listItem.ItemId = (int)ContextMenuItem.RemoveShowFromWatchList;
            }

            // Add/Remove Episode Watch List
            if (!episodeItem.Episode.InWatchList)
            {
                listItem = new GUIListItem(Translation.AddEpisodeToWatchList);
                dlg.Add(listItem);
                listItem.ItemId = (int)ContextMenuItem.AddEpisodeToWatchList;
            }
            else
            {
                listItem = new GUIListItem(Translation.RemoveEpisodeFromWatchList);
                dlg.Add(listItem);
                listItem.ItemId = (int)ContextMenuItem.RemoveEpisodeFromWatchList;
            }

            // Add Show to Custom List
            listItem = new GUIListItem(Translation.AddShowToList + "...");
            dlg.Add(listItem);
            listItem.ItemId = (int)ContextMenuItem.AddShowToList;

            // Add Episode to Custom List
            listItem = new GUIListItem(Translation.AddEpisodeToList + "...");
            dlg.Add(listItem);
            listItem.ItemId = (int)ContextMenuItem.AddEpisodeToList;

            // Shouts
            listItem = new GUIListItem(Translation.Shouts + "...");
            dlg.Add(listItem);
            listItem.ItemId = (int)ContextMenuItem.Shouts;

            // Add/Remove Libary
            if (!episodeItem.Episode.InCollection)
            {
                listItem = new GUIListItem(Translation.AddToLibrary);
                dlg.Add(listItem);
                listItem.ItemId = (int)ContextMenuItem.AddToLibrary;
            }
            else
            {
                listItem = new GUIListItem(Translation.RemoveFromLibrary);
                dlg.Add(listItem);
                listItem.ItemId = (int)ContextMenuItem.RemoveFromLibrary;
            }

            if (TraktHelper.IsTrailersAvailableAndEnabled)
            {
                listItem = new GUIListItem(Translation.Trailers);
                dlg.Add(listItem);
                listItem.ItemId = (int)ContextMenuItem.Trailers;
            }

            // Watch List Filter
            if (CurrentCalendar == CalendarType.MyShows)
            {
                if (TraktSettings.CalendarHideTVShowsInWatchList)
                    listItem = new GUIListItem(Translation.ShowTVShowsInWatchlist);
                else
                    listItem = new GUIListItem(Translation.HideTVShowsInWatchlist);
                
                dlg.Add(listItem);
                listItem.ItemId = (int)ContextMenuItem.WatchlistFilter;
            }

            // Show Context Menu
            dlg.DoModal(GUIWindowManager.ActiveWindow);
            if (dlg.SelectedId < 0) return;

            switch (dlg.SelectedId)
            {
                case ((int)ContextMenuItem.View):
                    ShowViewMenu();
                    break;

                case ((int)ContextMenuItem.StartDate):
                    ShowStartDateMenu();
                    break;

                case ((int)ContextMenuItem.ShowSeasonInfo):
                    GUIWindowManager.ActivateWindow((int)TraktGUIWindows.ShowSeasons, episodeItem.Show.ToJSON());
                    break;

                case ((int)ContextMenuItem.Related):
                    TraktHelper.ShowRelatedShows(episodeItem.Show);
                    break;

                case ((int)ContextMenuItem.Shouts):
                    TraktHelper.ShowEpisodeShouts(episodeItem.Show, episodeItem.Episode);
                    break;

                case ((int)ContextMenuItem.Rate):
                    GUICommon.RateEpisode(episodeItem.Show, episodeItem.Episode);
                    OnEpisodeSelected(Facade.SelectedListItem, Facade);
                    (Facade.SelectedListItem as GUIEpisodeListItem).Images.NotifyPropertyChanged("Screen");
                    break;

                case ((int)ContextMenuItem.MarkAsWatched):
                    TraktHelper.MarkEpisodeAsWatched(episodeItem.Show, episodeItem.Episode);
                    episodeItem.Episode.Watched = true;
                    Facade.SelectedListItem.IsPlayed = true;
                    if (episodeItem.Episode.Plays == 0) episodeItem.Episode.Plays = 1;
                    OnEpisodeSelected(Facade.SelectedListItem, Facade);
                    (Facade.SelectedListItem as GUIEpisodeListItem).Images.NotifyPropertyChanged("Screen");
                    break;

                case ((int)ContextMenuItem.MarkAsUnWatched):
                    TraktHelper.MarkEpisodeAsUnWatched(episodeItem.Show, episodeItem.Episode);
                    episodeItem.Episode.Watched = false;
                    Facade.SelectedListItem.IsPlayed = false;
                    OnEpisodeSelected(Facade.SelectedListItem, Facade);
                    (Facade.SelectedListItem as GUIEpisodeListItem).Images.NotifyPropertyChanged("Screen");
                    break;

                case ((int)ContextMenuItem.AddShowToWatchList):
                    TraktHelper.AddShowToWatchList(episodeItem.Show);
                    episodeItem.Show.InWatchList = true;
                    OnEpisodeSelected(Facade.SelectedListItem, Facade);
                    GUIWatchListShows.ClearCache(TraktSettings.Username);
                    break;

                case ((int)ContextMenuItem.AddEpisodeToWatchList):
                    TraktHelper.AddEpisodeToWatchList(episodeItem.Show, episodeItem.Episode);
                    episodeItem.Episode.InWatchList = true;
                    OnEpisodeSelected(Facade.SelectedListItem, Facade);
                    (Facade.SelectedListItem as GUIEpisodeListItem).Images.NotifyPropertyChanged("Screen");
                    GUIWatchListShows.ClearCache(TraktSettings.Username);
                    break;

                case ((int)ContextMenuItem.RemoveShowFromWatchList):
                    TraktHelper.RemoveShowFromWatchList(episodeItem.Show);
                    episodeItem.Show.InWatchList = false;
                    OnEpisodeSelected(Facade.SelectedListItem, Facade);
                    GUIWatchListShows.ClearCache(TraktSettings.Username);
                    break;

                case ((int)ContextMenuItem.RemoveEpisodeFromWatchList):
                    TraktHelper.RemoveEpisodeFromWatchList(episodeItem.Show, episodeItem.Episode);
                    episodeItem.Episode.InWatchList = false;
                    OnEpisodeSelected(Facade.SelectedListItem, Facade);
                    (Facade.SelectedListItem as GUIEpisodeListItem).Images.NotifyPropertyChanged("Screen");
                    GUIWatchListShows.ClearCache(TraktSettings.Username);
                    break;

                case ((int)ContextMenuItem.AddEpisodeToList):
                    TraktHelper.AddRemoveEpisodeInUserList(episodeItem.Show.Title, episodeItem.Show.Year.ToString(), episodeItem.Episode.Season.ToString(), episodeItem.Episode.Number.ToString(), episodeItem.Show.Tvdb, false);                    
                    break;

                case ((int)ContextMenuItem.AddShowToList):
                    TraktHelper.AddRemoveShowInUserList(episodeItem.Show.Title, episodeItem.Show.Year.ToString(), episodeItem.Show.Tvdb, false);
                    break;

                case ((int)ContextMenuItem.AddToLibrary):
                    TraktHelper.AddEpisodeToLibrary(episodeItem.Show, episodeItem.Episode);
                    episodeItem.Episode.InCollection = true;
                    OnEpisodeSelected(Facade.SelectedListItem, Facade);
                    (Facade.SelectedListItem as GUIEpisodeListItem).Images.NotifyPropertyChanged("Screen");
                    break;

                case ((int)ContextMenuItem.RemoveFromLibrary):
                    TraktHelper.RemoveEpisodeFromLibrary(episodeItem.Show, episodeItem.Episode);
                    episodeItem.Episode.InCollection = false;
                    OnEpisodeSelected(Facade.SelectedListItem, Facade);
                    (Facade.SelectedListItem as GUIEpisodeListItem).Images.NotifyPropertyChanged("Screen");
                    break;

                case ((int)ContextMenuItem.Trailers):
                    if (episodeItem != null) GUICommon.ShowTVShowTrailersMenu(episodeItem.Show, episodeItem.Episode);
                    break;

                case ((int)ContextMenuItem.WatchlistFilter):
                    TraktSettings.CalendarHideTVShowsInWatchList = !TraktSettings.CalendarHideTVShowsInWatchList;
                    SetHideWatchlisted();
                    LoadCalendar();
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
            GUIListItem selectedItem = Facade.SelectedListItem as GUIListItem;
            if (selectedItem == null) return;

            var episode = selectedItem.TVTag as TraktEpisodeSummary;
            if (episode == null) return;

            GUICommon.CheckAndPlayEpisode(episode.Show, episode.Episode);
        }

        private void ShowStartDateMenu()
        {
            IDialogbox dlg = (IDialogbox)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
            if (dlg == null) return;

            dlg.Reset();
            dlg.SetHeading(Translation.StartDate);

            foreach (int value in Enum.GetValues(typeof(StartDates)))
            {
                StartDates type = (StartDates)Enum.Parse(typeof(StartDates), value.ToString());
                string label = GetStartDateName(type);

                // Create new item
                GUIListItem listItem = new GUIListItem(label);
                listItem.ItemId = value;

                // Set selected if current
                if (type == CurrentStartDate) listItem.Selected = true;

                // Add new item to context menu
                dlg.Add(listItem);
            }

            dlg.DoModal(GUIWindowManager.ActiveWindow);
            if (dlg.SelectedId <= 0) return;

            // Set new Selection            
            CurrentStartDate = (StartDates)Enum.GetValues(typeof(StartDates)).GetValue(dlg.SelectedLabel);
            SetCurrentStartDate();

            // Reset Views and Apply
            CurrentWeekDays = 7;
            PreviousSelectedIndex = 0;
            PreviousCalendarDayCount = 0;
            _CalendarMyShows = null;
            _CalendarPremieres = null;
            _CalendarAllShows = null;

            LoadCalendar();
        }

        private void ShowViewMenu()
        {
            IDialogbox dlg = (IDialogbox)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
            if (dlg == null) return;

            dlg.Reset();
            dlg.SetHeading(Translation.Calendar);

            foreach (int value in Enum.GetValues(typeof(CalendarType)))
            {
                CalendarType type = (CalendarType)Enum.Parse(typeof(CalendarType), value.ToString());
                string label = GetCalendarTypeName(type);

                // Create new item
                GUIListItem listItem = new GUIListItem(label);
                listItem.ItemId = value;

                // Set selected if current
                if (type == CurrentCalendar) listItem.Selected = true;

                // Add new item to context menu
                dlg.Add(listItem);
            }

            dlg.DoModal(GUIWindowManager.ActiveWindow);
            if (dlg.SelectedId <= 0) return;

            // Set new Selection            
            CurrentCalendar = (CalendarType)Enum.GetValues(typeof(CalendarType)).GetValue(dlg.SelectedLabel);
            SetCurrentView();

            // Reset Views and Apply
            CurrentWeekDays = 7;
            PreviousSelectedIndex = 0;
            PreviousCalendarDayCount = 0;
            _CalendarMyShows = null;
            _CalendarPremieres = null;
            _CalendarAllShows = null;

            LoadCalendar();
        }

        private string GetStartDateName(StartDates date)
        {
            switch (date)
            {
                case StartDates.Today:
                    return Translation.DateToday;

                case StartDates.Yesterday:
                    return Translation.DateYesterday;

                case StartDates.OneWeekAgo:
                    return Translation.DateOneWeekAgo;

                case StartDates.TwoWeeksAgo:
                    return Translation.DateTwoWeeksAgo;

                case StartDates.OneMonthAgo:
                    return Translation.DateOneMonthAgo;

                default:
                    return Translation.DateToday;
            }
        }

        private string GetCalendarTypeName(CalendarType type)
        {
            switch (type)
            {
                case CalendarType.MyShows:
                    return Translation.CalendarMyShows;

                case CalendarType.Premieres:
                    return Translation.CalendarPremieres;

                case CalendarType.AllShows:
                    return Translation.CalendarAllShows;

                default:
                    return Translation.CalendarMyShows;
            }
        }

        private IEnumerable<TraktCalendar> GetCalendar()
        {
            GUIUtils.SetProperty("#Trakt.Items", string.Empty);

            switch (CurrentCalendar)
            {
                case CalendarType.MyShows:
                    return TraktCalendarMyShows;

                case CalendarType.Premieres:
                    return TraktCalendarPremieres;
     
                case CalendarType.AllShows:
                    return TraktCalendarAllShows;

                default:
                    return TraktCalendarMyShows;
            }
        }

        private void LoadCalendar()
        {
            GUIBackgroundTask.Instance.ExecuteInBackgroundAndCallback(() =>
            {
                return GetCalendar();
            },
            delegate(bool success, object result)
            {
                if (success)
                {
                    IEnumerable<TraktCalendar> calendar = result as IEnumerable<TraktCalendar>;
                    SendCalendarToFacade(calendar);
                }
            }, Translation.GettingCalendar, true);
        }

        private void SendCalendarToFacade(IEnumerable<TraktCalendar> calendar)
        {
            // check if we got a bad response
            if (calendar.Count() < PreviousCalendarDayCount)
            {
                GUIUtils.ShowNotifyDialog(GUIUtils.PluginName(), Translation.ErrorCalendar);
                // set defaults
                _CalendarMyShows = null;
                _CalendarPremieres = null;
                _CalendarAllShows = null;
                LastRequest = new DateTime();
                return;
            }

            // clear facade
            GUIControl.ClearControl(GetID, Facade.GetID);
           
            int itemCount = 0;
            List<TraktImage> showImages = new List<TraktImage>();

            // Add each days episodes to the list
            // Use Label3 of facade for Day/Group Idenitfier
            foreach (var day in calendar)
            {                 
                // apply watch list filter
                var episodes = day.Episodes;
                if (CurrentCalendar == CalendarType.MyShows)
                {
                    if (TraktSettings.CalendarHideTVShowsInWatchList)
                    {
                        episodes = day.Episodes.Where(e => !e.Show.InWatchList).ToList();
                    }
                }

                if (episodes.Count > 0)
                {
                    // add day header
                    GUIListItem item = new GUIListItem();
                    item.Label3 = GetDayHeader(DateTime.Parse(day.Date));
                    item.IconImage = "defaultTraktCalendar.png";
                    item.IconImageBig = "defaultTraktCalendarBig.png";
                    item.ThumbnailImage = "defaultTraktCalendarBig.png";
                    item.OnItemSelected += OnCalendarDateSelected;
                    Utils.SetDefaultIcons(item);
                    Facade.Add(item);
                 
                    foreach (var episode in episodes)
                    {
                        GUIEpisodeListItem episodeItem = new GUIEpisodeListItem(episode.ToString(), (int)TraktGUIWindows.Calendar);

                        // add image for download
                        TraktImage images = new TraktImage
                        {
                            EpisodeImages = episode.Episode.Images,
                            ShowImages = episode.Show.Images
                        };
                        showImages.Add(images);

                        // extended skin properties
                        episodeItem.Date = DateTime.Parse(day.Date).ToLongDateString();
                        episodeItem.SelectedIndex = (itemCount + 1).ToString();
                        
                        episodeItem.Images = images;
                        episodeItem.TVTag = episode;
                        episodeItem.ItemId = Int32.MaxValue - itemCount;
                        episodeItem.IsPlayed = episode.Episode.Watched;
                        episodeItem.IconImage = "defaultTraktEpisode.png";
                        episodeItem.IconImageBig = "defaultTraktEpisodeBig.png";
                        episodeItem.ThumbnailImage = "defaultTraktEpisodeBig.png";
                        episodeItem.OnItemSelected += OnEpisodeSelected;
                        Utils.SetDefaultIcons(episodeItem);
                        Facade.Add(episodeItem);
                        itemCount++;
                    }
                }
            }

            // if nothing airing this week, then indicate to user
            if (!IsCached && (calendar.Count() == PreviousCalendarDayCount))
            {
                GUIListItem item = new GUIListItem();

                item.Label3 = Translation.NoEpisodesThisWeek;
                item.IconImage = "defaultTraktCalendar.png";
                item.IconImageBig = "defaultTraktCalendarBig.png";
                item.ThumbnailImage = "defaultTraktCalendarBig.png";
                item.OnItemSelected += OnCalendarDateSelected;
                Utils.SetDefaultIcons(item);
                Facade.Add(item);

                // Stay on Next Week Item
                if (PreviousSelectedIndex > 0)
                    PreviousSelectedIndex--;
            }

            // Add Next Week Item so user can fetch next weeks calendar
            GUIListItem nextItem = new GUIListItem(Translation.NextWeek);
            
            nextItem.IconImage = "traktNextWeek.png";
            nextItem.IconImageBig = "traktNextWeek.png";
            nextItem.ThumbnailImage = "traktNextWeek.png";
            nextItem.OnItemSelected += OnNextWeekSelected;
            nextItem.IsFolder = true;
            Facade.Add(nextItem);

            // Set Facade Layout
            Facade.SetCurrentLayout("List");
            GUIControl.FocusControl(GetID, Facade.GetID);

            // Set the first episode on calendar on initial request (Index 0 is a day header), 
            // Set last position if paging to next week
            if (!IsCached)
                Facade.SelectIndex(PreviousSelectedIndex + 1);
            else // If cached just set to last position
                Facade.SelectIndex(PreviousSelectedIndex);

            // set facade properties
            GUIUtils.SetProperty("#itemcount", itemCount.ToString());
            GUIUtils.SetProperty("#Trakt.Items", string.Format("{0} {1}", itemCount.ToString(), itemCount > 1 ? Translation.Episodes : Translation.Episode));

            // Download episode images Async and set to facade
            GUIEpisodeListItem.GetImages(showImages);
        }

        private string GetDayHeader(DateTime dateTime)
        {
            // only set this for My Shows as the other types are not localized
            // Today, Tomorrow and Yesterday will not make much sense in that case.
            if (CurrentCalendar == CalendarType.MyShows)
            {
                if (dateTime.Date.DayOfYear.Equals(DateTime.Now.DayOfYear))
                    return Translation.Today;
                if (dateTime.Date.DayOfYear.Equals(DateTime.Now.DayOfYear + 1))
                    return Translation.Tomorrow;
                if (dateTime.Date.DayOfYear.Equals(DateTime.Now.DayOfYear - 1))
                    return Translation.Yesterday;
            }
            return dateTime.ToLongDateString();
        }

        private void OnCalendarDateSelected(GUIListItem item, GUIControl parent)
        {
            // Skip over date to next/prev episode if a header (Date)
            if (LastMoved == FacadeMovement.Down)
            {
                Facade.OnAction(new Action(Action.ActionType.ACTION_MOVE_DOWN, 0, 0));
            }
            else
            {
                Facade.OnAction(new Action(Action.ActionType.ACTION_MOVE_UP, 0, 0));
                
                // if the current item is now the first item which is a header, then skip to end
                // we need to bypass the scroll delay so we are not stuck on the first item
                if (Facade.SelectedListItemIndex == 0)
                {
                    Facade.SelectedListItemIndex = Facade.Count - 1;
                }
            }
        }

        private void OnNextWeekSelected(GUIListItem item, GUIControl parent)
        {
            backdrop.Filename = string.Empty;
            ClearProperties();
        }

        private void OnEpisodeSelected(GUIListItem item, GUIControl parent)
        {
            // publish extended properties, selected index excludes date headers.
            GUICommon.SetProperty("#Trakt.Calendar.Selected.Date", (item as GUIEpisodeListItem).Date);
            GUICommon.SetProperty("#selectedindex", (item as GUIEpisodeListItem).SelectedIndex);

            var episode = item.TVTag as TraktEpisodeSummary;
            PublishEpisodeSkinProperties(episode);

            GUIImageHandler.LoadFanart(backdrop, episode.Show.Images.Fanart.LocalImageFilename(ArtworkType.ShowFanart));
        }

        private void InitProperties()
        {
            // Fanart
            backdrop.GUIImageOne = FanartBackground;
            backdrop.GUIImageTwo = FanartBackground2;
            backdrop.LoadingImage = loadingImage;

            CurrentCalendar = (CalendarType)TraktSettings.DefaultCalendarView;
            SetCurrentView();

            CurrentStartDate = (StartDates)TraktSettings.DefaultCalendarStartDate;
            SetCurrentStartDate();

            SetHideWatchlisted();

            // clear properties only if we need to
            if (LastRequest < DateTime.UtcNow.Subtract(new TimeSpan(0, TraktSettings.WebRequestCacheMinutes, 0)))
            {
                CurrentWeekDays = 7;
                PreviousSelectedIndex = 0;
                PreviousCalendarDayCount = 0;
                IsCached = false;
            }
            else // restore previous position on load
                IsCached = true;
        }

        /// <summary>
        /// Get number of days forward to request in calendar,
        /// takes into consideration the current anchor point
        /// </summary>
        private string GetDaysForward()
        {
            switch (CurrentStartDate)
            {
                case StartDates.Today:
                    return CurrentWeekDays.ToString();
                case StartDates.Yesterday:
                    return (CurrentWeekDays + 1).ToString();
                case StartDates.OneWeekAgo:
                    return (CurrentWeekDays + 7).ToString();
                case StartDates.TwoWeeksAgo:
                    return (CurrentWeekDays + 14).ToString();
                case StartDates.OneMonthAgo:
                    return (CurrentWeekDays + 31).ToString();
                default:
                    return CurrentWeekDays.ToString();
            }
        }

        /// <summary>
        /// Get Date Time for Calendar Anchor Point
        /// </summary>
        private DateTime GetStartDate()
        {
            switch (CurrentStartDate)
            {
                case StartDates.Today:
                    return DateTime.Now.Subtract(new TimeSpan(0, 0, 0, 0));
                case StartDates.Yesterday:
                    return DateTime.Now.Subtract(new TimeSpan(1, 0, 0, 0));
                case StartDates.OneWeekAgo:
                    return DateTime.Now.Subtract(new TimeSpan(7, 0, 0, 0));
                case StartDates.TwoWeeksAgo:
                    return DateTime.Now.Subtract(new TimeSpan(14, 0, 0, 0));
                case StartDates.OneMonthAgo:
                    return DateTime.Now.Subtract(new TimeSpan(31, 0, 0, 0));
                default:
                    return DateTime.Now.Subtract(new TimeSpan(0, 0, 0, 0));
            }
        }

        private void SetCurrentStartDate()
        {
            // Set current start date in button label
            if (startDateButton != null)
                startDateButton.Label = Translation.StartDate + ": " + GetStartDateName(CurrentStartDate);
        }

        private void SetHideWatchlisted()
        {
            if (hideWatchlistedButton != null)
            {
                hideWatchlistedButton.Selected = TraktSettings.CalendarHideTVShowsInWatchList;
                GUIControl.SetControlLabel(GetID, hideWatchlistedButton.GetID, Translation.HideWatchlisted);
            }
        }

        private void SetCurrentView()
        {
            // Set current view in button label
            if (viewButton != null)
                viewButton.Label = Translation.View + ": " + GetCalendarTypeName(CurrentCalendar);

            GUICommon.SetProperty("#Trakt.Calendar.Type", CurrentCalendar.ToString());
            switch (CurrentCalendar)
            {
                case CalendarType.MyShows:
                    GUICommon.SetProperty("#Trakt.CurrentView", Translation.CalendarMyShows);
                    break;
                case CalendarType.Premieres:
                    GUICommon.SetProperty("#Trakt.CurrentView", Translation.CalendarPremieres);
                    break;
                case CalendarType.AllShows:
                    GUICommon.SetProperty("#Trakt.CurrentView", Translation.CalendarAllShows);
                    break;
            }            
        }

        private void ClearProperties()
        {
            GUIUtils.SetProperty("#Trakt.Calendar.Selected.Date", string.Empty);
            GUIUtils.SetProperty("#selectedindex", string.Empty);

            GUICommon.ClearShowProperties();
            GUICommon.ClearEpisodeProperties();
        }

        private void PublishEpisodeSkinProperties(TraktEpisodeSummary episode)
        {
            GUICommon.SetShowProperties(episode.Show);
            GUICommon.SetEpisodeProperties(episode.Episode);
        }
        #endregion

        #region Public Methods

        public static void ClearCache()
        {
            _CalendarMyShows = null;
        }

        #endregion
    }
}
