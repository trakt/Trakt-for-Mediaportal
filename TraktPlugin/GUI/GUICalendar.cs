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

        Dictionary<string, IEnumerable<TraktCalendar>> TraktCalendarShows
        {
            get
            {
                if (_CalendarShows == null || LastRequest < DateTime.UtcNow.Subtract(new TimeSpan(0, TraktSettings.WebRequestCacheMinutes, 0)))
                {
                    _CalendarShows = TraktAPI.TraktAPI.GetCalendarShows(GetStartDate().ToString("yyyyMMdd"), GetDaysForward(), true);
                    LastRequest = DateTime.UtcNow;
                    IsCached = false;
                }
                return _CalendarShows;
            }
        }
        static Dictionary<string, IEnumerable<TraktCalendar>> _CalendarShows = null;

        Dictionary<string, IEnumerable<TraktCalendar>> TraktCalendarPremieres
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
        private Dictionary<string, IEnumerable<TraktCalendar>> _CalendarPremieres = null;

        Dictionary<string, IEnumerable<TraktCalendar>> TraktCalendarAllShows
        {
            get
            {
                if (_CalendarAllShows == null || LastRequest < DateTime.UtcNow.Subtract(new TimeSpan(0, TraktSettings.WebRequestCacheMinutes, 0)))
                {
                    // Dont send OAuth so as it does not filter by 'own' shows
                    _CalendarAllShows = TraktAPI.TraktAPI.GetCalendarShows(GetStartDate().ToString("yyyyMMdd"), GetDaysForward(), false);
                    LastRequest = DateTime.UtcNow;
                    IsCached = false;
                }
                return _CalendarAllShows;
            }
        }
        private Dictionary<string, IEnumerable<TraktCalendar>> _CalendarAllShows = null;

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
                    if (_CalendarShows != null) PreviousCalendarDayCount = _CalendarShows.Count();
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
                                    if (_CalendarShows != null)
                                    {
                                        PreviousCalendarDayCount = _CalendarShows.Count();
                                        PreviousSelectedIndex = Facade.SelectedListItemIndex;
                                        CurrentWeekDays += 7;
                                        _CalendarShows = null;
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
            var dlg = (IDialogbox)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
            if (dlg == null) return;

            dlg.Reset();
            dlg.SetHeading(GUIUtils.PluginName());

            var calendarItem = Facade.SelectedListItem.TVTag as TraktCalendar;
            if (calendarItem == null) return;

            // Create Views Menu Item
            var listItem = new GUIListItem(Translation.ChangeView);
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
            if (!calendarItem.Episode.IsWatched(calendarItem.Show))
            {
                listItem = new GUIListItem(Translation.MarkAsWatched);
                dlg.Add(listItem);
                listItem.ItemId = (int)ContextMenuItem.MarkAsWatched;
            }

            // Mark As UnWatched
            if (calendarItem.Episode.IsWatched(calendarItem.Show))
            {
                listItem = new GUIListItem(Translation.MarkAsUnWatched);
                dlg.Add(listItem);
                listItem.ItemId = (int)ContextMenuItem.MarkAsUnWatched;
            }

            // Add/Remove Show Watchlist
            if (!calendarItem.Show.IsWatchlisted())
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

            // Add/Remove Episode Watchlist
            if (!calendarItem.Episode.IsWatchlisted())
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
            if (!calendarItem.Episode.IsCollected(calendarItem.Show))
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

            // Watchlist Filter
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
                    GUIWindowManager.ActivateWindow((int)TraktGUIWindows.ShowSeasons, calendarItem.Show.ToJSON());
                    break;

                case ((int)ContextMenuItem.Related):
                    TraktHelper.ShowRelatedShows(calendarItem.Show);
                    break;

                case ((int)ContextMenuItem.Shouts):
                    TraktHelper.ShowEpisodeShouts(calendarItem.Show, calendarItem.Episode);
                    break;

                case ((int)ContextMenuItem.Rate):
                    GUICommon.RateEpisode(calendarItem.Show, calendarItem.Episode);
                    OnEpisodeSelected(Facade.SelectedListItem, Facade);
                    (Facade.SelectedListItem as GUIEpisodeListItem).Images.NotifyPropertyChanged("Screen");
                    break;

                case ((int)ContextMenuItem.MarkAsWatched):
                    TraktHelper.AddEpisodeToWatchHistory(calendarItem.Episode);
                    TraktCache.AddEpisodeToWatchHistory(calendarItem.Show, calendarItem.Episode);
                    Facade.SelectedListItem.IsPlayed = true;
                    OnEpisodeSelected(Facade.SelectedListItem, Facade);
                    (Facade.SelectedListItem as GUIEpisodeListItem).Images.NotifyPropertyChanged("Screen");
                    break;

                case ((int)ContextMenuItem.MarkAsUnWatched):
                    TraktHelper.RemoveEpisodeFromCollection(calendarItem.Episode);
                    TraktCache.RemoveEpisodeFromCollection(calendarItem.Show, calendarItem.Episode);
                    Facade.SelectedListItem.IsPlayed = false;
                    OnEpisodeSelected(Facade.SelectedListItem, Facade);
                    (Facade.SelectedListItem as GUIEpisodeListItem).Images.NotifyPropertyChanged("Screen");
                    break;

                case ((int)ContextMenuItem.AddShowToWatchList):
                    TraktHelper.AddShowToWatchList(calendarItem.Show);
                    OnEpisodeSelected(Facade.SelectedListItem, Facade);
                    GUIWatchListShows.ClearCache(TraktSettings.Username);
                    break;

                case ((int)ContextMenuItem.AddEpisodeToWatchList):
                    TraktHelper.AddEpisodeToWatchList(calendarItem.Episode);
                    TraktCache.AddEpisodeToWatchlist(calendarItem.Show, calendarItem.Episode);
                    OnEpisodeSelected(Facade.SelectedListItem, Facade);
                    (Facade.SelectedListItem as GUIEpisodeListItem).Images.NotifyPropertyChanged("Screen");
                    GUIWatchListShows.ClearCache(TraktSettings.Username);
                    break;

                case ((int)ContextMenuItem.RemoveShowFromWatchList):
                    TraktHelper.RemoveShowFromWatchList(calendarItem.Show);                    
                    OnEpisodeSelected(Facade.SelectedListItem, Facade);
                    GUIWatchListShows.ClearCache(TraktSettings.Username);
                    break;

                case ((int)ContextMenuItem.RemoveEpisodeFromWatchList):
                    TraktHelper.RemoveEpisodeFromWatchList(calendarItem.Episode);
                    OnEpisodeSelected(Facade.SelectedListItem, Facade);
                    (Facade.SelectedListItem as GUIEpisodeListItem).Images.NotifyPropertyChanged("Screen");
                    GUIWatchListShows.ClearCache(TraktSettings.Username);
                    break;

                case ((int)ContextMenuItem.AddEpisodeToList):
                    TraktHelper.AddRemoveEpisodeInUserList(calendarItem.Episode, false);                    
                    break;

                case ((int)ContextMenuItem.AddShowToList):
                    TraktHelper.AddRemoveShowInUserList(calendarItem.Show, false);
                    break;

                case ((int)ContextMenuItem.AddToLibrary):
                    TraktHelper.AddEpisodeToCollection(calendarItem.Episode);
                    TraktCache.AddEpisodeToCollection(calendarItem.Show, calendarItem.Episode);
                    OnEpisodeSelected(Facade.SelectedListItem, Facade);
                    (Facade.SelectedListItem as GUIEpisodeListItem).Images.NotifyPropertyChanged("Screen");
                    break;

                case ((int)ContextMenuItem.RemoveFromLibrary):
                    TraktHelper.RemoveEpisodeFromCollection(calendarItem.Episode);
                    TraktCache.RemoveEpisodeFromCollection(calendarItem.Show, calendarItem.Episode);
                    OnEpisodeSelected(Facade.SelectedListItem, Facade);
                    (Facade.SelectedListItem as GUIEpisodeListItem).Images.NotifyPropertyChanged("Screen");
                    break;

                case ((int)ContextMenuItem.Trailers):
                    if (calendarItem != null) GUICommon.ShowTVShowTrailersMenu(calendarItem.Show, calendarItem.Episode);
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
            var selectedItem = Facade.SelectedListItem as GUIListItem;
            if (selectedItem == null) return;

            var calendarItem = selectedItem.TVTag as TraktCalendar;
            if (calendarItem == null) return;

            GUICommon.CheckAndPlayEpisode(calendarItem.Show, calendarItem.Episode);
        }

        private void ShowStartDateMenu()
        {
            var dlg = (IDialogbox)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
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
            _CalendarShows = null;
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
            _CalendarShows = null;
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

        private Dictionary<string, IEnumerable<TraktCalendar>> GetCalendar()
        {
            GUIUtils.SetProperty("#Trakt.Items", string.Empty);

            switch (CurrentCalendar)
            {
                case CalendarType.MyShows:
                    return TraktCalendarShows;

                case CalendarType.Premieres:
                    return TraktCalendarPremieres;
     
                case CalendarType.AllShows:
                    return TraktCalendarAllShows;

                default:
                    return TraktCalendarShows;
            }
        }

        private void LoadCalendar()
        {
            GUIBackgroundTask.Instance.ExecuteInBackgroundAndCallback(() =>
            {
                var calendar = GetCalendar();

                // convert the days (dictionary 'key') of the result to a localised day
                return ConvertDaysInCalendarToLocalisedDays(calendar);
            },
            delegate(bool success, object result)
            {
                if (success)
                {
                    var calendar = result as Dictionary<string, List<TraktCalendar>>;
                    SendCalendarToFacade(calendar);
                }
            }, Translation.GettingCalendar, true);
        }

        private void SendCalendarToFacade(Dictionary<string, List<TraktCalendar>> calendar)
        {
            // check if we got a bad response
            if (calendar.Count() < PreviousCalendarDayCount)
            {
                GUIUtils.ShowNotifyDialog(GUIUtils.PluginName(), Translation.ErrorCalendar);
                // set defaults
                _CalendarShows = null;
                _CalendarPremieres = null;
                _CalendarAllShows = null;
                LastRequest = new DateTime();
                return;
            }

            // clear facade
            GUIControl.ClearControl(GetID, Facade.GetID);
           
            int itemCount = 0;
            var showImages = new List<GUITraktImage>();

            // Add each days episodes to the list
            // Use Label3 of facade for Day/Group Idenitfier
            foreach (var day in calendar)
            {                 
                // apply watchlist filter
                var episodesInDay = day.Value;
                if (CurrentCalendar == CalendarType.MyShows)
                {
                    if (TraktSettings.CalendarHideTVShowsInWatchList)
                    {
                        episodesInDay = day.Value.Where(e => !e.Show.IsWatchlisted()).ToList();
                    }
                }

                if (episodesInDay.Count() > 0)
                {
                    // add day header
                    var item = new GUIListItem();
                    item.Label3 = GetDayHeader(day.Key.FromISO8601());
                    item.IconImage = "defaultTraktCalendar.png";
                    item.IconImageBig = "defaultTraktCalendarBig.png";
                    item.ThumbnailImage = "defaultTraktCalendarBig.png";
                    item.OnItemSelected += OnCalendarDateSelected;
                    Utils.SetDefaultIcons(item);
                    Facade.Add(item);
                 
                    foreach (var calendarItem in episodesInDay)
                    {
                        var episodeItem = new GUIEpisodeListItem(calendarItem.ToString(), (int)TraktGUIWindows.Calendar);

                        // add image for download
                        var images = new GUITraktImage
                        {
                            EpisodeImages = calendarItem.Episode.Images,
                            ShowImages = calendarItem.Show.Images
                        };
                        showImages.Add(images);

                        // extended skin properties
                        episodeItem.Date = DateTime.Parse(day.Key).ToLongDateString();
                        episodeItem.SelectedIndex = (itemCount + 1).ToString();
                        
                        episodeItem.Images = images;
                        episodeItem.TVTag = calendarItem;
                        episodeItem.Episode = calendarItem.Episode;
                        episodeItem.Show = calendarItem.Show;
                        episodeItem.ItemId = Int32.MaxValue - itemCount;
                        episodeItem.IsPlayed = calendarItem.Episode.IsWatched(calendarItem.Show);
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

        /// <summary>
        /// Converts each day in the calendar dictionary key to a localised day
        /// Some underlying episodes could be from multiple days as they span a UTC day not a local day
        /// </summary>
        private Dictionary<string, List<TraktCalendar>> ConvertDaysInCalendarToLocalisedDays(Dictionary<string, IEnumerable<TraktCalendar>> calendar)
        {
            var result = new Dictionary<string, List<TraktCalendar>>();

            if (calendar == null)
                return result;

            foreach (var day in calendar)
            {
                // go through underlying episodes
                // and convert to localised date
                var episodesInDay = day.Value;
                foreach (var calendarItem in episodesInDay.OrderBy(e => e.AirsAt.FromISO8601()))
                {
                    string localDate = calendarItem.AirsAt.FromISO8601().ToLocalTime().ToString("yyyy-MM-dd");

                    // add new item and/or add to existing day in calendar
                    if (!result.ContainsKey(localDate))
                        result.Add(localDate, new List<TraktCalendar>());

                    // get current episodes in day
                    var currentItemsInDay = result[localDate];

                    // add new item to day / sort by air time
                    currentItemsInDay.Add(calendarItem);
                    result[localDate] = currentItemsInDay;
                }
            }

            return result;
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

            var calendarItem = item.TVTag as TraktCalendar;
            PublishCalendarSkinProperties(calendarItem);

            GUIImageHandler.LoadFanart(backdrop, calendarItem.Show.Images.Fanart.LocalImageFilename(ArtworkType.ShowFanart));
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
                    return DateTime.UtcNow.Subtract(new TimeSpan(0, 0, 0, 0));
                case StartDates.Yesterday:
                    return DateTime.UtcNow.Subtract(new TimeSpan(1, 0, 0, 0));
                case StartDates.OneWeekAgo:
                    return DateTime.UtcNow.Subtract(new TimeSpan(7, 0, 0, 0));
                case StartDates.TwoWeeksAgo:
                    return DateTime.UtcNow.Subtract(new TimeSpan(14, 0, 0, 0));
                case StartDates.OneMonthAgo:
                    return DateTime.UtcNow.Subtract(new TimeSpan(31, 0, 0, 0));
                default:
                    return DateTime.UtcNow.Subtract(new TimeSpan(0, 0, 0, 0));
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

        private void PublishCalendarSkinProperties(TraktCalendar calendarItem)
        {
            GUICommon.SetShowProperties(calendarItem.Show);
            GUICommon.SetEpisodeProperties(calendarItem.Show, calendarItem.Episode);
        }
        #endregion

        #region Public Methods

        public static void ClearCache()
        {
            _CalendarShows = null;
        }

        #endregion
    }
}
