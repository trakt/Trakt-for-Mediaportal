using System;
using System.Collections.Generic;
using System.Linq;
using MediaPortal.GUI.Library;
using MediaPortal.Util;
using TraktAPI.DataStructures;
using TraktAPI.Extensions;
using TraktPlugin.Cache;
using TraktPlugin.TmdbAPI.DataStructures;
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

        [SkinControl(5)]
        protected GUIButtonControl maxDaysButton = null;

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
            UserShows,
            UserSeasonPremieres,
            UserNewShows,
            AllShows,
            AllNewShows,
            AllSeasonPremieres
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
            Cast,
            Crew,
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

        Dictionary<int, Dictionary<string, List<TraktShowCalendar>>> TVShowCalendar = null;

        int CurrentPage;
        int PreviousSelectedIndex;

        ImageSwapper backdrop;
        static DateTime LastRequest = new DateTime();

        #endregion

        #region Request Methods

        private IEnumerable<TraktShowCalendar> GetCalendarShows()
        {
            IEnumerable<TraktShowCalendar> result = null;

            switch (CurrentCalendar)
            {
                case CalendarType.UserShows:
                    result = TraktAPI.TraktAPI.GetCalendarUserShows(GetStartDate().ToString("yyyy-MM-dd"), GetDaysForward());
                    break;
                case CalendarType.UserSeasonPremieres:
                    result = TraktAPI.TraktAPI.GetCalendarUserSeasonPremieresShows(GetStartDate().ToString("yyyyMMdd"), GetDaysForward());
                    break;
                case CalendarType.UserNewShows:
                    result = TraktAPI.TraktAPI.GetCalendarUserNewShows(GetStartDate().ToString("yyyy-MM-dd"), GetDaysForward());
                    break;
                case CalendarType.AllShows:
                    result = TraktAPI.TraktAPI.GetCalendarShows(GetStartDate().ToString("yyyy-MM-dd"), GetDaysForward());
                    break;
                case CalendarType.AllSeasonPremieres:
                    result = TraktAPI.TraktAPI.GetCalendarSeasonPremieresShows(GetStartDate().ToString("yyyyMMdd"), GetDaysForward());
                    break;
                case CalendarType.AllNewShows:
                    result = TraktAPI.TraktAPI.GetCalendarNewShows(GetStartDate().ToString("yyyy-MM-dd"), GetDaysForward());
                    break;
            }

            return result;
        }

        /// <summary>
        /// Returns the cached current page calendar results
        /// </summary>
        private Dictionary<string, List<TraktShowCalendar>> GetCalendarShowsFromCache()
        {
            Dictionary<string, List<TraktShowCalendar>> calendar = null;
            IEnumerable<TraktShowCalendar> result = null;

            if (TVShowCalendar == null || LastRequest < DateTime.UtcNow.Subtract(new TimeSpan(0, TraktSettings.WebRequestCacheMinutes, 0)))
            {
                CurrentPage = 1;
                PreviousSelectedIndex = 0;

                // request first page
                result = GetCalendarShows();
                
                LastRequest = DateTime.UtcNow;

                // clear the cache
                if (TVShowCalendar == null)
                    TVShowCalendar = new Dictionary<int, Dictionary<string, List<TraktShowCalendar>>>();
                else
                    TVShowCalendar.Clear();

                // create a dictionary with key being a localised day
                calendar = ConvertDaysInCalendarToLocalisedDays(result);
                
                // add page to cache
                TVShowCalendar.Add(1, calendar);
            }
            else
            {
                // get page from cache if it exists
                if (TVShowCalendar.TryGetValue(CurrentPage, out calendar))
                {
                    return calendar;
                }

                // request next page
                PreviousSelectedIndex = 0;
                result = GetCalendarShows();
                calendar = ConvertDaysInCalendarToLocalisedDays(result);

                // add to cache
                TVShowCalendar.Add(CurrentPage, calendar);
            }
            return calendar;
        }

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
                        if (item != null && item.IsFolder)
                        {
                            if (item.TVTag.ToString() == "next")
                                CurrentPage++;
                            else
                                CurrentPage--;

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

                // Max Days
                case (5):
                    ShowMaxDaysMenu();
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

            var selectedItem = Facade.SelectedListItem as GUIEpisodeListItem;
            if (selectedItem == null) return;

            var calendarItem = selectedItem.TVTag as TraktShowCalendar;
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
            listItem = new GUIListItem(Translation.RelatedShows);
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
            listItem = new GUIListItem(Translation.Comments);
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

            // Cast and Crew
            listItem = new GUIListItem(Translation.Cast);
            dlg.Add(listItem);
            listItem.ItemId = (int)ContextMenuItem.Cast;

            listItem = new GUIListItem(Translation.Crew);
            dlg.Add(listItem);
            listItem.ItemId = (int)ContextMenuItem.Crew;

            if (TraktHelper.IsTrailersAvailableAndEnabled)
            {
                listItem = new GUIListItem(Translation.Trailers);
                dlg.Add(listItem);
                listItem.ItemId = (int)ContextMenuItem.Trailers;
            }

            // Watchlist Filter
            if (CurrentCalendar == CalendarType.UserShows)
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
                    TraktHelper.RemoveEpisodeFromWatchHistory(calendarItem.Episode);
                    TraktCache.RemoveEpisodeFromWatchHistory(calendarItem.Show, calendarItem.Episode);
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

                case ((int)ContextMenuItem.Cast):
                    GUICreditsShow.Show = calendarItem.Show;
                    GUICreditsShow.Type = GUICreditsShow.CreditType.Cast;
                    GUICreditsShow.Fanart = TmdbCache.GetShowBackdropFilename(selectedItem.Images.ShowImages);
                    GUIWindowManager.ActivateWindow((int)TraktGUIWindows.CreditsShow);
                    break;

                case ((int)ContextMenuItem.Crew):
                    GUICreditsShow.Show = calendarItem.Show;
                    GUICreditsShow.Type = GUICreditsShow.CreditType.Crew;
                    GUICreditsShow.Fanart = TmdbCache.GetShowBackdropFilename(selectedItem.Images.ShowImages);
                    GUIWindowManager.ActivateWindow((int)TraktGUIWindows.CreditsShow);
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

            var calendarItem = selectedItem.TVTag as TraktShowCalendar;
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
                var listItem = new GUIListItem(label);
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
            TVShowCalendar = null;
            LoadCalendar();
        }

        private void ShowMaxDaysMenu()
        {
            var dlg = (IDialogbox)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
            if (dlg == null) return;

            dlg.Reset();
            dlg.SetHeading(Translation.MaxDays);

            for (int day = 1; day < 31; day++)
            {
                string label = string.Format("{0}", day == 1 ? Translation.Day : Translation.Days);

                // Create new item
                var listItem = new GUIListItem(label);
                listItem.ItemId = day;

                // Set selected if current
                if (day == TraktSettings.TvCalendarMaxDays) listItem.Selected = true;

                // Add new item to context menu
                dlg.Add(listItem);
            }

            dlg.DoModal(GUIWindowManager.ActiveWindow);
            if (dlg.SelectedId <= 0) return;

            // Set new Selection            
            TraktSettings.TvCalendarMaxDays = dlg.SelectedLabel;
            SetMaxDays();

            // Reset Views and Apply
            TVShowCalendar = null;
            LoadCalendar();
        }
        
        private void ShowViewMenu()
        {
            var dlg = (IDialogbox)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
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
            PreviousSelectedIndex = 0;
            TVShowCalendar = null;

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
                case CalendarType.UserShows:
                    return Translation.CalendarMyShows;

                case CalendarType.UserNewShows:
                    return Translation.CalendarMyNewShows;

                case CalendarType.UserSeasonPremieres:
                    return Translation.CalendarMyPremieres;

                case CalendarType.AllShows:
                    return Translation.CalendarAllShows;

                case CalendarType.AllNewShows:
                    return Translation.CalendarAllNewShows;

                case CalendarType.AllSeasonPremieres:
                    return Translation.CalendarAllPremieres;

                default:
                    return Translation.CalendarMyShows;
            }
        }
        
        /// <summary>
        /// Converts each day in the calendar to a localised day
        /// Some underlying episodes could be from multiple days as they span a UTC day not a local day
        /// </summary>
        private Dictionary<string, List<TraktShowCalendar>> ConvertDaysInCalendarToLocalisedDays(IEnumerable<TraktShowCalendar> calendar)
        {
            if (calendar == null)
                return null;

            var result = new Dictionary<string, List<TraktShowCalendar>>();

            // go through underlying episodes
            // and convert to localised date
            foreach (var item in calendar)
            {
                DateTime currentCalendarDate = item.AirsAt.FromISO8601().ToLocalTime().Date;
                string localDate = item.AirsAt.FromISO8601().ToLocalTime().ToString("yyyy-MM-dd");

                // remove calendar entries that are outside of scope
                if (currentCalendarDate < GetCurrentLocalStartDate())
                    continue;
                if (currentCalendarDate > GetCurrentLocalEndDate())
                    continue;

                // add new item and/or add to existing day in calendar
                if (!result.ContainsKey(localDate))
                    result.Add(localDate, new List<TraktShowCalendar>());

                // get current episodes in day
                var currentItemsInDay = result[localDate];

                // add new item to day / sort by air time
                currentItemsInDay.Add(new TraktShowCalendar { AirsAt = item.AirsAt, Episode = item.Episode, Show = item.Show });
                result[localDate] = currentItemsInDay;
            }

            return result;
        }

        private void LoadCalendar()
        {
            GUIBackgroundTask.Instance.ExecuteInBackgroundAndCallback(() =>
            {
                return GetCalendarShowsFromCache();
            },
            delegate(bool success, object result)
            {
                if (success)
                {
                    var calendar = result as Dictionary<string, List<TraktShowCalendar>>;
                    SendCalendarToFacade(calendar);
                }
            }, Translation.GettingCalendar, true);
        }

        private void SendCalendarToFacade(Dictionary<string, List<TraktShowCalendar>> calendar)
        {
            // check if we got a bad response
            if (calendar == null)
            {
                GUIUtils.ShowNotifyDialog(GUIUtils.PluginName(), Translation.ErrorCalendar);
                // set defaults
                TVShowCalendar = null;
                LastRequest = new DateTime();
                return;
            }

            // clear facade
            GUIControl.ClearControl(GetID, Facade.GetID);

            int itemCount = 0;
            var showImages = new List<GUITmdbImage>();

            // Add Previous Days Item so user can go back to previous calendar entries
            if (CurrentPage != 1)
            {
                var prevItem = new GUIListItem(string.Format(Translation.PreviousDays, TraktSettings.TvCalendarMaxDays))
                {
                    IconImage = "traktPrevWeek.png",
                    IconImageBig = "traktPrevWeek.png",
                    ThumbnailImage = "traktPrevWeek.png",
                    TVTag = "previous",
                    IsFolder = true
                };
                prevItem.OnItemSelected += OnPrevWeekSelected;
                Facade.Add(prevItem);
            }

            // Add each days episodes to the list
            // Use Label3 of facade for Day/Group Idenitfier
            foreach (var day in calendar)
            {
                // apply watchlist filter
                var episodesInDay = day.Value;
                if (TraktSettings.CalendarHideTVShowsInWatchList && !string.IsNullOrEmpty(TraktSettings.UserAccessToken))
                {
                    episodesInDay = day.Value.Where(e => !e.Show.IsWatchlisted()).ToList();
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
                        var images = new GUITmdbImage
                        {
                            EpisodeImages = new TmdbEpisodeImages
                            {
                                Id = calendarItem.Show.Ids.Tmdb,
                                Season = calendarItem.Episode.Season,
                                Episode = calendarItem.Episode.Number,
                                AirDate = calendarItem.Episode.FirstAired == null ? null : calendarItem.Episode.FirstAired.FromISO8601().ToLocalTime().ToShortDateString()
                            }
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
            if (itemCount == 0)
            {
                var item = new GUIListItem()
                {
                    Label3 = Translation.NoEpisodesThisWeek,
                    IconImage = "defaultTraktCalendar.png",
                    IconImageBig = "defaultTraktCalendarBig.png",
                    ThumbnailImage = "defaultTraktCalendarBig.png"
                };

                item.OnItemSelected += OnCalendarDateSelected;
                Utils.SetDefaultIcons(item);
                Facade.Add(item);
            }

            // Add Next Days Item so user can fetch next days calendar
            var nextItem = new GUIListItem(string.Format(Translation.NextDays, TraktSettings.TvCalendarMaxDays))
            {
                IconImage = "traktNextWeek.png",
                IconImageBig = "traktNextWeek.png",
                ThumbnailImage = "traktNextWeek.png",
                TVTag = "next",
                IsFolder = true
            };
            nextItem.OnItemSelected += OnNextWeekSelected;
            Facade.Add(nextItem);

            // Set Facade Layout
            Facade.CurrentLayout = GUIFacadeControl.Layout.List;
            GUIControl.FocusControl(GetID, Facade.GetID);

            // if we cached the last selected index then use it
            // e.g. returning from another window and the cache has not expired
            if (PreviousSelectedIndex > 0)
            {
                Facade.SelectIndex(PreviousSelectedIndex);
            }
            else
            {
                // if its the first page then there is no previous button
                // first item is a header or a folder
                Facade.SelectIndex(CurrentPage == 1 ? 1 : 2);
            }

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
            if (CurrentCalendar == CalendarType.UserShows)
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

        private void OnPrevWeekSelected(GUIListItem item, GUIControl parent)
        {
            backdrop.Filename = string.Empty;
            ClearProperties();
        }

        private void OnEpisodeSelected(GUIListItem item, GUIControl parent)
        {
            // publish extended properties, selected index excludes date headers.
            GUICommon.SetProperty("#Trakt.Calendar.Selected.Date", (item as GUIEpisodeListItem).Date);
            GUICommon.SetProperty("#selectedindex", (item as GUIEpisodeListItem).SelectedIndex);

            var calendarItem = item.TVTag as TraktShowCalendar;
            PublishCalendarSkinProperties(calendarItem);

            GUIImageHandler.LoadFanart(backdrop, TmdbCache.GetShowBackdropFilename((item as GUIEpisodeListItem).Images.ShowImages));
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

            SetMaxDays();
            SetHideWatchlisted();
        }

        /// <summary>
        /// Get number of days forward to request in calendar,
        /// </summary>
        private int GetDaysForward()
        {
            // we will always get 1 day before requested
            // and 1 day after requested to take into consideration 
            // timezone shifts...
            // return max days + 2, the maximum allowed is 31;

            if (TraktSettings.TvCalendarMaxDays >= 30)
                return 31;

            return TraktSettings.TvCalendarMaxDays + 2;
        }

        /// <summary>
        /// Get Date Time for Calendar Anchor Point
        /// </summary>
        private DateTime GetStartDate()
        {
            // we want to get 1 day before the actual date to take into 
            // consideration timezone shift

            DateTime startDate = new DateTime();

            switch (CurrentStartDate)
            {
                case StartDates.Today:
                    startDate = DateTime.UtcNow.Subtract(new TimeSpan(1, 0, 0, 0));
                    break;
                case StartDates.Yesterday:
                    startDate =  DateTime.UtcNow.Subtract(new TimeSpan(2, 0, 0, 0));
                    break;
                case StartDates.OneWeekAgo:
                    startDate = DateTime.UtcNow.Subtract(new TimeSpan(8, 0, 0, 0));
                    break;
                case StartDates.TwoWeeksAgo:
                    startDate = DateTime.UtcNow.Subtract(new TimeSpan(15, 0, 0, 0));
                    break;
                case StartDates.OneMonthAgo:
                    startDate = DateTime.UtcNow.Subtract(new TimeSpan(31, 0, 0, 0));
                    break;
            }

            if (CurrentPage == 1)
                return startDate;

            // else jump forward to start at current page
            return startDate.AddDays(TraktSettings.TvCalendarMaxDays * (CurrentPage - 1));
        }

        private DateTime GetCurrentLocalStartDate()
        {
            DateTime startDate = new DateTime();

            switch (CurrentStartDate)
            {
                case StartDates.Today:
                    startDate = DateTime.Today;
                    break;
                case StartDates.Yesterday:
                    startDate = DateTime.Today.Subtract(new TimeSpan(1, 0, 0, 0));
                    break;
                case StartDates.OneWeekAgo:
                    startDate = DateTime.Today.Subtract(new TimeSpan(7, 0, 0, 0));
                    break;
                case StartDates.TwoWeeksAgo:
                    startDate = DateTime.Today.Subtract(new TimeSpan(14, 0, 0, 0));
                    break;
                case StartDates.OneMonthAgo:
                    startDate = DateTime.Today.Subtract(new TimeSpan(30, 0, 0, 0));
                    break;
            }

            if (CurrentPage == 1)
                return startDate;
            
            return startDate.AddDays(TraktSettings.TvCalendarMaxDays * (CurrentPage - 1));
        }

        private DateTime GetCurrentLocalEndDate()
        {
            return GetCurrentLocalStartDate().AddDays(TraktSettings.TvCalendarMaxDays - 1);
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

        private void SetMaxDays()
        {
            if (maxDaysButton != null)
            {
                GUIControl.SetControlLabel(GetID, maxDaysButton.GetID, Translation.MaxDays + ": " + TraktSettings.TvCalendarMaxDays);
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
                case CalendarType.UserShows:
                    GUICommon.SetProperty("#Trakt.CurrentView", Translation.CalendarMyShows);
                    break;
                case CalendarType.UserNewShows:
                    GUICommon.SetProperty("#Trakt.CurrentView", Translation.CalendarMyNewShows);
                    break;
                case CalendarType.UserSeasonPremieres:
                    GUICommon.SetProperty("#Trakt.CurrentView", Translation.CalendarMySeasonPremieres);
                    break;
                case CalendarType.AllShows:
                    GUICommon.SetProperty("#Trakt.CurrentView", Translation.CalendarAllShows);
                    break;
                case CalendarType.AllNewShows:
                    GUICommon.SetProperty("#Trakt.CurrentView", Translation.CalendarAllNewShows);
                    break;
                case CalendarType.AllSeasonPremieres:
                    GUICommon.SetProperty("#Trakt.CurrentView", Translation.CalendarAllSeasonPremieres);
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

        private void PublishCalendarSkinProperties(TraktShowCalendar calendarItem)
        {
            GUICommon.SetShowProperties(calendarItem.Show);
            GUICommon.SetEpisodeProperties(calendarItem.Show, calendarItem.Episode);
        }
        #endregion

        #region Public Methods

        public static void ClearCache()
        {
            // this will force an update next time
            LastRequest = new DateTime();
        }

        #endregion
    }
}
