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
    public class GUICalendarMovies : GUIWindow
    {
        #region Skin Controls

        [SkinControl(2)]
        protected GUIButtonControl viewButton = null;

        [SkinControl(3)]
        protected GUIButtonControl startDateButton = null;

        [SkinControl(5)]
        protected GUIButtonControl maxDaysButton = null;
        
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
            UserMovies,
            UserDVDs,
            AllMovies,
            AllDVDs
        }

        enum ContextMenuItem
        {
            View,
            StartDate,
            MaxDays,
            HideMovie,
            MarkAsWatched,
            MarkAsUnWatched,
            AddMovieToList,
            AddMovieToWatchlist,
            RemoveMovieFromWatchlist,
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

        public GUICalendarMovies()
        {
            backdrop = new ImageSwapper();
            backdrop.PropertyOne = "#Trakt.CalendarMovies.Fanart.1";
            backdrop.PropertyTwo = "#Trakt.CalendarMovies.Fanart.2";
        }

        #endregion

        #region Private Variables

        FacadeMovement LastMoved { get; set; }
        CalendarType CurrentCalendar { get; set; }
        StartDates CurrentStartDate { get; set; }

        Dictionary<int, Dictionary<string, List<TraktMovieCalendar>>> MovieCalendar = null;

        int CurrentPage;
        int PreviousSelectedIndex;

        ImageSwapper backdrop;
        static DateTime LastRequest = new DateTime();

        bool FilterHiddenMovies;
        #endregion

        #region Request Methods

        private IEnumerable<TraktMovieCalendar> GetCalendarMovies()
        {
            IEnumerable<TraktMovieCalendar> result = null;

            switch (CurrentCalendar)
            {
                case CalendarType.UserMovies:
                    result = TraktAPI.TraktAPI.GetCalendarUserMovies(GetStartDate().ToString("yyyy-MM-dd"), GetDaysForward());
                    break;
                case CalendarType.UserDVDs:
                    result = TraktAPI.TraktAPI.GetCalendarUserDVDs(GetStartDate().ToString("yyyy-MM-dd"), GetDaysForward());
                    break;
                case CalendarType.AllMovies:
                    result = TraktAPI.TraktAPI.GetCalendarMovies(GetStartDate().ToString("yyyy-MM-dd"), GetDaysForward());
                    break;
                case CalendarType.AllDVDs:
                    result = TraktAPI.TraktAPI.GetCalendarDVDs(GetStartDate().ToString("yyyy-MM-dd"), GetDaysForward());
                    break;
            }

            return result;
        }

        /// <summary>
        /// Returns the cached current page calendar results
        /// </summary>
        private Dictionary<string, List<TraktMovieCalendar>> GetCalendarMoviesFromCache()
        {
            Dictionary<string, List<TraktMovieCalendar>> calendar = null;
            IEnumerable<TraktMovieCalendar> result = null;

            if (MovieCalendar == null || LastRequest < DateTime.UtcNow.Subtract(new TimeSpan(0, TraktSettings.WebRequestCacheMinutes, 0)))
            {
                CurrentPage = 1;
                FilterHiddenMovies = false;
                PreviousSelectedIndex = 0;

                // request first page
                result = GetCalendarMovies();

                LastRequest = DateTime.UtcNow;

                // clear the cache
                if (MovieCalendar == null)
                    MovieCalendar = new Dictionary<int, Dictionary<string, List<TraktMovieCalendar>>>();
                else
                    MovieCalendar.Clear();

                // create a dictionary with key being a localised day
                calendar = ConvertDaysInCalendarToLocalisedDays(result);

                // add page to cache
                MovieCalendar.Add(CurrentPage, calendar);
            }
            else
            {
                // get page from cache if it exists
                if (MovieCalendar.TryGetValue(CurrentPage, out calendar))
                {
                    return calendar;
                }

                // request next page
                PreviousSelectedIndex = 0;
                FilterHiddenMovies = false;
                result = GetCalendarMovies();
                calendar = ConvertDaysInCalendarToLocalisedDays(result);

                // add to cache
                MovieCalendar.Add(CurrentPage, calendar);
            }
            return calendar;
        }

        #endregion

        #region Base Overrides

        public override int GetID
        {
            get
            {
                return (int)TraktGUIWindows.CalendarMovies;
            }
        }

        public override bool Init()
        {
            return Load(GUIGraphicsContext.Skin + @"\Trakt.Calendar.Movies.xml");
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

            GUIMovieListItem.StopDownload = true;
            ClearProperties();

            // save current view
            TraktSettings.DefaultMovieCalendarView = (int)CurrentCalendar;
            TraktSettings.DefaultMovieCalendarStartDate = (int)CurrentStartDate;

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
                        var item = Facade.SelectedListItem as GUIListItem;

                        // Is a group header
                        if (item != null && item.IsFolder)
                        {
                            if (item.TVTag.ToString() == "next")
                            {
                                CurrentPage++;
                                if (CurrentPage == 0) CurrentPage = 1;
                            }
                            else
                            {
                                CurrentPage--;
                                if (CurrentPage == 0) CurrentPage = -1;
                            }

                            // load next 7 days in calendar
                            LoadCalendar();
                        }

                        // Is a movie
                        if (item != null && !item.IsFolder)
                        {
                            CheckAndPlayMovie(true);
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
                    CheckAndPlayMovie(false);
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

            var selectedItem = Facade.SelectedListItem as GUIMovieListItem;
            if (selectedItem == null) return;

            var calendarItem = selectedItem.TVTag as TraktMovieCalendar;
            if (calendarItem == null) return;

            // Create Views Menu Item
            var listItem = new GUIListItem(Translation.ChangeView);
            dlg.Add(listItem);
            listItem.ItemId = (int)ContextMenuItem.View;

            // Start Date
            listItem = new GUIListItem(Translation.StartDate + "...");
            dlg.Add(listItem);
            listItem.ItemId = (int)ContextMenuItem.StartDate;

            // Max Days
            listItem = new GUIListItem(Translation.MaxDays + "...");
            dlg.Add(listItem);
            listItem.ItemId = (int)ContextMenuItem.MaxDays;
            
            // Hide Movie
            listItem = new GUIListItem(Translation.HideMovie);
            dlg.Add(listItem);
            listItem.ItemId = (int)ContextMenuItem.HideMovie;
            
            // Related Movies
            listItem = new GUIListItem(Translation.RelatedMovies);
            dlg.Add(listItem);
            listItem.ItemId = (int)ContextMenuItem.Related;

            // Rate
            listItem = new GUIListItem(Translation.Rate + "...");
            dlg.Add(listItem);
            listItem.ItemId = (int)ContextMenuItem.Rate;

            // Mark As Watched
            if (!calendarItem.Movie.IsWatched())
            {
                listItem = new GUIListItem(Translation.MarkAsWatched);
                dlg.Add(listItem);
                listItem.ItemId = (int)ContextMenuItem.MarkAsWatched;
            }
            else
            {
                listItem = new GUIListItem(Translation.MarkAsUnWatched);
                dlg.Add(listItem);
                listItem.ItemId = (int)ContextMenuItem.MarkAsUnWatched;
            }

            // Add/Remove Movie Watchlist
            if (!calendarItem.Movie.IsWatchlisted())
            {
                listItem = new GUIListItem(Translation.AddToWatchList);
                dlg.Add(listItem);
                listItem.ItemId = (int)ContextMenuItem.AddMovieToWatchlist;
            }
            else
            {
                listItem = new GUIListItem(Translation.RemoveFromWatchList);
                dlg.Add(listItem);
                listItem.ItemId = (int)ContextMenuItem.RemoveMovieFromWatchlist;
            }

            // Add Movie to Custom List
            listItem = new GUIListItem(Translation.AddToList);
            dlg.Add(listItem);
            listItem.ItemId = (int)ContextMenuItem.AddMovieToList;
            
            // Shouts
            listItem = new GUIListItem(Translation.Comments);
            dlg.Add(listItem);
            listItem.ItemId = (int)ContextMenuItem.Shouts;

            // Add/Remove Libary
            if (!calendarItem.Movie.IsCollected())
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

                case ((int)ContextMenuItem.MaxDays):
                    ShowMaxDaysMenu();
                    break;

                case ((int)ContextMenuItem.HideMovie):
                    TraktHelper.AddHiddenMovie(calendarItem.Movie, "calendar");
                    FilterHiddenMovies = true;
                    LoadCalendar();
                    break;
                    
                case ((int)ContextMenuItem.Related):
                    TraktHelper.ShowRelatedMovies(calendarItem.Movie);
                    break;

                case ((int)ContextMenuItem.Shouts):
                    TraktHelper.ShowMovieShouts(calendarItem.Movie);
                    break;

                case ((int)ContextMenuItem.Rate):
                    GUICommon.RateMovie(calendarItem.Movie);
                    OnMovieSelected(Facade.SelectedListItem, Facade);
                    (Facade.SelectedListItem as GUIMovieListItem).Images.NotifyPropertyChanged("Poster");
                    break;

                case ((int)ContextMenuItem.MarkAsWatched):
                    TraktHelper.AddMovieToWatchHistory(calendarItem.Movie);
                    Facade.SelectedListItem.IsPlayed = true;
                    OnMovieSelected(Facade.SelectedListItem, Facade);
                    (Facade.SelectedListItem as GUIMovieListItem).Images.NotifyPropertyChanged("Poster");
                    break;

                case ((int)ContextMenuItem.MarkAsUnWatched):
                    TraktHelper.RemoveMovieFromWatchHistory(calendarItem.Movie);
                    Facade.SelectedListItem.IsPlayed = false;
                    OnMovieSelected(Facade.SelectedListItem, Facade);
                    (Facade.SelectedListItem as GUIMovieListItem).Images.NotifyPropertyChanged("Poster");
                    break;

                case ((int)ContextMenuItem.AddMovieToWatchlist):
                    TraktHelper.AddMovieToWatchList(calendarItem.Movie, true);
                    OnMovieSelected(Facade.SelectedListItem, Facade);
                    GUIWatchListMovies.ClearCache(TraktSettings.Username);
                    break;
                    
                case ((int)ContextMenuItem.RemoveMovieFromWatchlist):
                    TraktHelper.RemoveMovieFromWatchList(calendarItem.Movie, true);
                    OnMovieSelected(Facade.SelectedListItem, Facade);
                    GUIWatchListMovies.ClearCache(TraktSettings.Username);
                    break;

                case ((int)ContextMenuItem.AddMovieToList):
                    TraktHelper.AddRemoveMovieInUserList(calendarItem.Movie, false);
                    break;
                    
                case ((int)ContextMenuItem.AddToLibrary):
                    TraktHelper.AddMovieToCollection(calendarItem.Movie);
                    OnMovieSelected(Facade.SelectedListItem, Facade);
                    (Facade.SelectedListItem as GUIMovieListItem).Images.NotifyPropertyChanged("Poster");
                    break;

                case ((int)ContextMenuItem.RemoveFromLibrary):
                    TraktHelper.RemoveMovieFromCollection(calendarItem.Movie);
                    OnMovieSelected(Facade.SelectedListItem, Facade);
                    (Facade.SelectedListItem as GUIMovieListItem).Images.NotifyPropertyChanged("Poster");
                    break;

                case ((int)ContextMenuItem.Cast):
                    GUICreditsMovie.Movie = calendarItem.Movie;
                    GUICreditsMovie.Type = GUICreditsMovie.CreditType.Cast;
                    GUICreditsMovie.Fanart = TmdbCache.GetMovieBackdropFilename(selectedItem.Images.MovieImages);
                    GUIWindowManager.ActivateWindow((int)TraktGUIWindows.CreditsMovie);
                    break;

                case ((int)ContextMenuItem.Crew):
                    GUICreditsMovie.Movie = calendarItem.Movie;
                    GUICreditsMovie.Type = GUICreditsMovie.CreditType.Crew;
                    GUICreditsMovie.Fanart = TmdbCache.GetMovieBackdropFilename(selectedItem.Images.MovieImages);
                    GUIWindowManager.ActivateWindow((int)TraktGUIWindows.CreditsMovie);
                    break;

                case ((int)ContextMenuItem.Trailers):
                    if (calendarItem != null) GUICommon.ShowMovieTrailersMenu(calendarItem.Movie);
                    break;
                    
                default:
                    break;
            }

            base.OnShowContextMenu();
        }

        #endregion

        #region Private Methods

        private void CheckAndPlayMovie(bool jumpTo)
        {
            var selectedItem = Facade.SelectedListItem as GUIListItem;
            if (selectedItem == null) return;

            var calendarItem = selectedItem.TVTag as TraktMovieCalendar;
            if (calendarItem == null) return;

            GUICommon.CheckAndPlayMovie(jumpTo, calendarItem.Movie);
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
            MovieCalendar = null;
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
                if (day == TraktSettings.MovieCalendarMaxDays) listItem.Selected = true;

                // Add new item to context menu
                dlg.Add(listItem);
            }

            dlg.DoModal(GUIWindowManager.ActiveWindow);
            if (dlg.SelectedId <= 0) return;

            // Set new Selection            
            TraktSettings.MovieCalendarMaxDays = dlg.SelectedLabel + 1;
            SetMaxDays();

            // Reset Views and Apply
            MovieCalendar = null;
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
                var listItem = new GUIListItem(label);
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
            MovieCalendar = null;

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
                case CalendarType.UserMovies:
                    return Translation.CalendarMyMovies;

                case CalendarType.UserDVDs:
                    return Translation.CalendarMyDVDs;
                    
                case CalendarType.AllMovies:
                    return Translation.CalendarAllMovies;

                case CalendarType.AllDVDs:
                    return Translation.CalendarAllDVDs;
                    
                default:
                    return Translation.CalendarMyMovies;
            }
        }

        /// <summary>
        /// Converts each day in the calendar to a localised day
        /// Some underlying movies could be from multiple days as they span a UTC day not a local day
        /// </summary>
        private Dictionary<string, List<TraktMovieCalendar>> ConvertDaysInCalendarToLocalisedDays(IEnumerable<TraktMovieCalendar> calendar)
        {
            if (calendar == null)
                return null;

            var result = new Dictionary<string, List<TraktMovieCalendar>>();

            // go through underlying episodes
            // and convert to localised date
            foreach (var item in calendar)
            {
                DateTime currentCalendarDate = item.Released.FromISO8601().ToLocalTime().Date;
                string localDate = item.Released.FromISO8601().ToLocalTime().ToString("yyyy-MM-dd");

                // remove calendar entries that are outside of scope
                if (currentCalendarDate < GetCurrentLocalStartDate())
                    continue;
                if (currentCalendarDate > GetCurrentLocalEndDate())
                    continue;

                // add new item and/or add to existing day in calendar
                if (!result.ContainsKey(localDate))
                    result.Add(localDate, new List<TraktMovieCalendar>());

                // get current episodes in day
                var currentItemsInDay = result[localDate];

                // add new item to day / sort by air time
                currentItemsInDay.Add(new TraktMovieCalendar { Released = item.Released, Movie = item.Movie });
                result[localDate] = currentItemsInDay;
            }

            return result;
        }

        private void LoadCalendar()
        {
            GUIBackgroundTask.Instance.ExecuteInBackgroundAndCallback(() =>
            {
                return GetCalendarMoviesFromCache();
            },
            delegate (bool success, object result)
            {
                if (success)
                {
                    var calendar = result as Dictionary<string, List<TraktMovieCalendar>>;
                    SendCalendarToFacade(calendar);
                }
            }, Translation.GettingCalendar, true);
        }

        private void SendCalendarToFacade(Dictionary<string, List<TraktMovieCalendar>> calendar)
        {
            // check if we got a bad response
            if (calendar == null)
            {
                GUIUtils.ShowNotifyDialog(GUIUtils.PluginName(), Translation.ErrorCalendar);
                // set defaults
                MovieCalendar = null;
                LastRequest = new DateTime();
                return;
            }

            // clear facade
            GUIControl.ClearControl(GetID, Facade.GetID);

            int itemCount = 0;
            var movieImages = new List<GUITmdbImage>();

            // Add Previous Days Item so user can go back to previous calendar entries
            var prevItem = new GUIListItem(string.Format(Translation.PreviousDays, TraktSettings.TvCalendarMaxDays))
            {
                IconImage = "traktPreviousPage.png",
                IconImageBig = "traktPreviousPage.png",
                ThumbnailImage = "traktPreviousPage.png",
                TVTag = "previous",
                IsFolder = true
            };
            prevItem.OnItemSelected += OnPrevWeekSelected;
            Facade.Add(prevItem);

            // Add each days episodes to the list
            // Use Label3 of facade for Day/Group Idenitfier
            foreach (var day in calendar)
            {
                var moviesInDay = day.Value;

                // filter hidden shows
                if (FilterHiddenMovies && !string.IsNullOrEmpty(TraktSettings.UserAccessToken))
                {
                    // for each hidden trakt show in the calendar, remove from our request
                    // this only needs to be done if we have manually hidden a movie whilst using a cached calendar
                    moviesInDay.RemoveAll(m => m.Movie.IsHidden("calendar"));
                }

                if (moviesInDay.Count() > 0)
                {
                    // add day header
                    var item = new GUIListItem();
                    item.Label3 = GetDayHeader(day.Key.FromISO8601());
                    item.IconImage = "defaultTraktPoster.png";
                    item.IconImageBig = "defaultTraktPosterBig.png";
                    item.ThumbnailImage = "defaultTraktPosterBig.png";
                    item.OnItemSelected += OnCalendarDateSelected;
                    Utils.SetDefaultIcons(item);
                    Facade.Add(item);

                    foreach (var calendarItem in moviesInDay)
                    {
                        var movieItem = new GUIMovieListItem(calendarItem.Movie.Title, (int)TraktGUIWindows.CalendarMovies);

                        // add image for download
                        var images = new GUITmdbImage
                        {
                            MovieImages = new TmdbMovieImages { Id = calendarItem.Movie.Ids.Tmdb }
                        };
                        movieImages.Add(images);

                        // extended skin properties
                        movieItem.Date = DateTime.Parse(day.Key).ToLongDateString();
                        movieItem.SelectedIndex = (itemCount + 1).ToString();

                        movieItem.Images = images;
                        movieItem.TVTag = calendarItem;
                        movieItem.Movie = calendarItem.Movie;
                        movieItem.ItemId = Int32.MaxValue - itemCount;
                        movieItem.IsPlayed = calendarItem.Movie.IsWatched();
                        movieItem.IconImage = "defaultTraktPoster.png";
                        movieItem.IconImageBig = "defaultTraktPosterBig.png";
                        movieItem.ThumbnailImage = "defaultTraktPosterBig.png";
                        movieItem.OnItemSelected += OnMovieSelected;
                        Utils.SetDefaultIcons(movieItem);
                        Facade.Add(movieItem);
                        itemCount++;
                    }
                }
            }

            // if nothing airing this week, then indicate to user
            if (itemCount == 0)
            {
                var item = new GUIListItem()
                {
                    Label3 = Translation.NoMoviesThisWeek,
                    IconImage = "defaultTraktPoster.png",
                    IconImageBig = "defaultTraktPosterBig.png",
                    ThumbnailImage = "defaultTraktPosterBig.png"
                };

                item.OnItemSelected += OnCalendarDateSelected;
                Utils.SetDefaultIcons(item);
                Facade.Add(item);
            }

            // Add Next Days Item so user can fetch next days calendar
            var nextItem = new GUIListItem(string.Format(Translation.NextDays, TraktSettings.TvCalendarMaxDays))
            {
                IconImage = "traktNextPage.png",
                IconImageBig = "traktNextPage.png",
                ThumbnailImage = "traktNextPage.png",
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
                Facade.SelectedListItemIndex = PreviousSelectedIndex;
            }
            else
            {
                // beginning of a page has a previous button 
                // and a date header, so skip 2 items
                Facade.SelectedListItemIndex = 2;
            }

            // set facade properties
            GUIUtils.SetProperty("#itemcount", itemCount.ToString());
            GUIUtils.SetProperty("#Trakt.Items", string.Format("{0} {1}", itemCount.ToString(), itemCount > 1 ? Translation.Movies : Translation.Movie));

            // Download movie images Async and set to facade
            GUIMovieListItem.GetImages(movieImages);
        }

        private string GetDayHeader(DateTime dateTime)
        {
            if (dateTime.Date.DayOfYear.Equals(DateTime.Now.DayOfYear))
                return Translation.Today;
            if (dateTime.Date.DayOfYear.Equals(DateTime.Now.DayOfYear + 1))
                return Translation.Tomorrow;
            if (dateTime.Date.DayOfYear.Equals(DateTime.Now.DayOfYear - 1))
                return Translation.Yesterday;
         
            return dateTime.ToLongDateString();
        }
        
        private void OnCalendarDateSelected(GUIListItem item, GUIControl parent)
        {
            // Skip over date to next/prev movie if a header (Date)
            if (LastMoved == FacadeMovement.Down)
            {
                Facade.OnAction(new Action(Action.ActionType.ACTION_MOVE_DOWN, 0, 0));
            }
            else
            {
                Facade.OnAction(new Action(Action.ActionType.ACTION_MOVE_UP, 0, 0));
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

        private void OnMovieSelected(GUIListItem item, GUIControl parent)
        {
            // publish extended properties, selected index excludes date headers.
            GUICommon.SetProperty("#Trakt.Calendar.Selected.Date", (item as GUIMovieListItem).Date);
            GUICommon.SetProperty("#selectedindex", (item as GUIMovieListItem).SelectedIndex);

            var calendarItem = item.TVTag as TraktMovieCalendar;
            PublishCalendarSkinProperties(calendarItem);

            GUIImageHandler.LoadFanart(backdrop, TmdbCache.GetMovieBackdropFilename((item as GUIMovieListItem).Images.MovieImages));
        }

        private void InitProperties()
        {
            // Fanart
            backdrop.GUIImageOne = FanartBackground;
            backdrop.GUIImageTwo = FanartBackground2;
            backdrop.LoadingImage = loadingImage;

            CurrentCalendar = (CalendarType)TraktSettings.DefaultMovieCalendarView;
            SetCurrentView();

            CurrentStartDate = (StartDates)TraktSettings.DefaultMovieCalendarStartDate;
            SetCurrentStartDate();

            SetMaxDays();
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

            if (TraktSettings.MovieCalendarMaxDays >= 30)
                return 31;

            return TraktSettings.MovieCalendarMaxDays + 2;
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
                    startDate = DateTime.UtcNow.Subtract(new TimeSpan(2, 0, 0, 0));
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

            // else jump to start at current page
            if (CurrentPage > 0)
                return startDate.AddDays(TraktSettings.MovieCalendarMaxDays * (CurrentPage - 1));
            else
                return startDate.AddDays(TraktSettings.MovieCalendarMaxDays * CurrentPage);
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

            if (CurrentPage > 0)
                return startDate.AddDays(TraktSettings.MovieCalendarMaxDays * (CurrentPage - 1));
            else
                return startDate.AddDays(TraktSettings.MovieCalendarMaxDays * CurrentPage);
        }

        private DateTime GetCurrentLocalEndDate()
        {
            return GetCurrentLocalStartDate().AddDays(TraktSettings.MovieCalendarMaxDays - 1);
        }

        private void SetCurrentStartDate()
        {
            // Set current start date in button label
            if (startDateButton != null)
                startDateButton.Label = Translation.StartDate + ": " + GetStartDateName(CurrentStartDate);
        }
        
        private void SetMaxDays()
        {
            if (maxDaysButton != null)
            {
                GUIControl.SetControlLabel(GetID, maxDaysButton.GetID, Translation.MaxDays + ": " + TraktSettings.MovieCalendarMaxDays);
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
                case CalendarType.UserMovies:
                    GUICommon.SetProperty("#Trakt.CurrentView", Translation.CalendarMyMovies);
                    break;
                case CalendarType.UserDVDs:
                    GUICommon.SetProperty("#Trakt.CurrentView", Translation.CalendarMyDVDs);
                    break;
                case CalendarType.AllMovies:
                    GUICommon.SetProperty("#Trakt.CurrentView", Translation.CalendarAllMovies);
                    break;
                case CalendarType.AllDVDs:
                    GUICommon.SetProperty("#Trakt.CurrentView", Translation.CalendarAllDVDs);
                    break;
            }
        }

        private void ClearProperties()
        {
            GUIUtils.SetProperty("#Trakt.Calendar.Selected.Date", string.Empty);
            GUIUtils.SetProperty("#selectedindex", string.Empty);

            GUICommon.ClearMovieProperties();
        }

        private void PublishCalendarSkinProperties(TraktMovieCalendar calendarItem)
        {
            GUICommon.SetMovieProperties(calendarItem.Movie);
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
