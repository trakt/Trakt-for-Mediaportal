using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    public class GUICalendar : GUIWindow
    {
        #region Skin Controls

        [SkinControl(2)]
        protected GUIButtonControl viewButton = null;

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
            Premieres
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

        bool StopDownload { get; set; }
        FacadeMovement LastMoved { get; set; }
        CalendarType CurrentCalendar { get; set; }
        int CurrentWeekDays = 7;
        int PreviousSelectedIndex;
        int PreviousCalendarDayCount;
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
                    _CalendarMyShows = TraktAPI.TraktAPI.GetCalendarForUser(TraktSettings.Username, DateTime.Now.ToString("yyyyMMdd"), CurrentWeekDays.ToString());
                    LastRequest = DateTime.UtcNow;
                }
                return _CalendarMyShows;
            }
        }
        private IEnumerable<TraktCalendar> _CalendarMyShows = null;

        IEnumerable<TraktCalendar> TraktCalendarPremieres
        {
            get
            {
                if (_CalendarPremieres == null || LastRequest < DateTime.UtcNow.Subtract(new TimeSpan(0, TraktSettings.WebRequestCacheMinutes, 0)))
                {
                    _CalendarPremieres = TraktAPI.TraktAPI.GetCalendarPremieres(DateTime.Now.ToString("yyyyMMdd"), CurrentWeekDays.ToString());
                    LastRequest = DateTime.UtcNow;
                }
                return _CalendarPremieres;
            }
        }
        private IEnumerable<TraktCalendar> _CalendarPremieres = null;

        #endregion

        #region Base Overrides

        public override int GetID
        {
            get
            {
                return 87259;
            }
        }

        public override bool Init()
        {
            return Load(GUIGraphicsContext.Skin + @"\Trakt.Calendar.xml");
        }

        protected override void OnPageLoad()
        {
            // Requires Login
            if (!GUICommon.CheckLogin()) return;

            // Clear GUI properties
            ClearProperties();

            // Init Properties
            InitProperties();

            // Load Calendar
            LoadCalendar();
        }

        protected override void OnPageDestroy(int new_windowId)
        {
            CurrentWeekDays = 7;
            PreviousSelectedIndex = 0;
            PreviousCalendarDayCount = 0;
            StopDownload = true;
            ClearProperties();

            // save current view
            TraktSettings.DefaultCalendarView = (int)CurrentCalendar;

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
                        if (item !=null && item.IsFolder)
                        {
                            // previous call may have timedout
                            if (CurrentCalendar == CalendarType.MyShows && _CalendarMyShows != null)
                            {
                                PreviousCalendarDayCount = _CalendarMyShows.Count();
                                PreviousSelectedIndex = Facade.SelectedListItemIndex;
                                CurrentWeekDays += 7;
                                _CalendarMyShows = null;
                            }

                            if (CurrentCalendar == CalendarType.Premieres && _CalendarPremieres != null)
                            {
                                PreviousCalendarDayCount = _CalendarPremieres.Count();
                                PreviousSelectedIndex = Facade.SelectedListItemIndex;
                                CurrentWeekDays += 7;
                                _CalendarPremieres = null;
                            }

                            // load next 7 days in calendar
                            LoadCalendar();
                        }
                    }
                    break;

                // View Button
                case (2):
                    ShowViewMenu();                    
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
            
            // Create Views Menu Item
            GUIListItem listItem = new GUIListItem(Translation.ChangeView);   

            // Add new item to context menu
            dlg.Add(listItem);

            // Show Context Menu
            dlg.DoModal(GUIWindowManager.ActiveWindow);
            if (dlg.SelectedId <= 0) return;

            switch (dlg.SelectedLabel)
            {
                case (0):
                    ShowViewMenu();
                    break;

                default:
                    break;
            }

            base.OnShowContextMenu();
        }

        #endregion

        #region Private Methods

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

            LoadCalendar();
        }

        private string GetCalendarTypeName(CalendarType type)
        {
            switch (type)
            {
                case CalendarType.MyShows:
                    return Translation.CalendarMyShows;

                case CalendarType.Premieres:
                    return Translation.CalendarPremieres;

                default:
                    return Translation.CalendarMyShows;
            }
        }

        private IEnumerable<TraktCalendar> GetCalendar()
        {
            GUIUtils.SetProperty("#Trakt.Items", string.Empty);

            if (CurrentCalendar == CalendarType.MyShows)            
                return TraktCalendarMyShows;
            else
                return TraktCalendarPremieres;
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
                _CalendarPremieres = null;
                _CalendarMyShows = null;
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
                GUIListItem item = new GUIListItem();
                
                item.Label3 = DateTime.Parse(day.ToString()).ToLongDateString();
                item.IconImage = "defaultTraktCalendar.png";
                item.IconImageBig = "defaultTraktCalendarBig.png";
                item.ThumbnailImage = "defaultTraktCalendarBig.png";
                item.OnItemSelected += OnCalendarDateSelected;
                Utils.SetDefaultIcons(item);
                Facade.Add(item);

                foreach (var episode in day.Episodes)
                {
                    GUITraktCalendarListItem episodeItem = new GUITraktCalendarListItem(episode.ToString());

                    // add image for download
                    TraktImage images = new TraktImage
                    {
                        EpisodeImages = episode.Episode.Images,
                        ShowImages = episode.Show.Images
                    };
                    showImages.Add(images);

                    episodeItem.Item = images;
                    episodeItem.TVTag = episode;
                    episodeItem.ItemId = Int32.MaxValue - itemCount;
                    episodeItem.IconImage = "defaultTraktEpisode.png";
                    episodeItem.IconImageBig = "defaultTraktEpisodeBig.png";
                    episodeItem.ThumbnailImage = "defaultTraktEpisodeBig.png";
                    episodeItem.OnItemSelected += OnEpisodeSelected;
                    Utils.SetDefaultIcons(episodeItem);
                    Facade.Add(episodeItem);
                    itemCount++;
                }
            }

            // if nothing airing this week, then indicate to user
            if (calendar.Count() == PreviousCalendarDayCount)
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

            // Select the first episode on calendar, 
            // Set last position if paging to next week
            Facade.SelectedListItemIndex = PreviousSelectedIndex + 1;

            // set facade properties
            GUIUtils.SetProperty("#itemcount", itemCount.ToString());
            GUIUtils.SetProperty("#Trakt.Items", string.Format("{0} {1}", itemCount.ToString(), itemCount > 1 ? Translation.Episodes : Translation.Episode));

            // Download episode images Async and set to facade
            GetImages(showImages);
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
            TraktCalendar.TraktEpisodes episode = item.TVTag as TraktCalendar.TraktEpisodes;
            PublishEpisodeSkinProperties(episode);
            GUIImageHandler.LoadFanart(backdrop, episode.Show.Images.FanartImageFilename);
        }

        private void InitProperties()
        {
            // Fanart
            backdrop.GUIImageOne = FanartBackground;
            backdrop.GUIImageTwo = FanartBackground2;
            backdrop.LoadingImage = loadingImage;            

            CurrentCalendar = (CalendarType)TraktSettings.DefaultCalendarView;
            SetCurrentView();
        }

        private void SetCurrentView()
        {
            // Set current view in button label
            if (viewButton != null)
                viewButton.Label = Translation.View + ": " + GetCalendarTypeName(CurrentCalendar);

            SetProperty("#Trakt.Calendar.Type", CurrentCalendar.ToString());
            SetProperty("#Trakt.CurrentView", CurrentCalendar == CalendarType.MyShows ? Translation.CalendarMyShows : Translation.CalendarPremieres);
        }

        private void SetProperty(string property, string value)
        {
            string propertyValue = string.IsNullOrEmpty(value) ? "N/A" : value;
            GUIUtils.SetProperty(property, propertyValue);
        }

        private void ClearProperties()
        {
            GUIUtils.SetProperty("#Trakt.Show.Imdb", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.Tvdb", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.TvRage", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.Title", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.Url", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.AirDay", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.AirTime", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.Certification", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.Country", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.FirstAired", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.Network", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.Overview", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.Runtime", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.Year", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.FanartImageFilename", string.Empty);
            GUIUtils.SetProperty("#Trakt.Episode.Number", string.Empty);
            GUIUtils.SetProperty("#Trakt.Episode.Season", string.Empty);
            GUIUtils.SetProperty("#Trakt.Episode.FirstAired", string.Empty);
            GUIUtils.SetProperty("#Trakt.Episode.Title", string.Empty);
            GUIUtils.SetProperty("#Trakt.Episode.Url", string.Empty);
            GUIUtils.SetProperty("#Trakt.Episode.Overview", string.Empty);
            GUIUtils.SetProperty("#Trakt.Episode.Runtime", string.Empty);
            GUIUtils.SetProperty("#Trakt.Episode.EpisodeImageFilename", string.Empty);
        }

        private void PublishEpisodeSkinProperties(TraktCalendar.TraktEpisodes episode)
        {
            SetProperty("#Trakt.Show.Imdb", episode.Show.Imdb);
            SetProperty("#Trakt.Show.Tvdb", episode.Show.Tvdb);
            SetProperty("#Trakt.Show.TvRage", episode.Show.TvRage);
            SetProperty("#Trakt.Show.Title", episode.Show.Title);
            SetProperty("#Trakt.Show.Url", episode.Show.Url);
            SetProperty("#Trakt.Show.AirDay", episode.Show.AirDay);
            SetProperty("#Trakt.Show.AirTime", episode.Show.AirTime);
            SetProperty("#Trakt.Show.Certification", episode.Show.Certification);
            SetProperty("#Trakt.Show.Country", episode.Show.Country);
            SetProperty("#Trakt.Show.FirstAired", episode.Show.FirstAired.FromEpoch().ToShortDateString());
            SetProperty("#Trakt.Show.Network", episode.Show.Network);
            SetProperty("#Trakt.Show.Overview", string.IsNullOrEmpty(episode.Show.Overview) ? Translation.NoShowSummary : episode.Show.Overview);
            SetProperty("#Trakt.Show.Runtime", episode.Show.Runtime.ToString());
            SetProperty("#Trakt.Show.Year", episode.Show.Year.ToString());
            SetProperty("#Trakt.Show.FanartImageFilename", episode.Show.Images.FanartImageFilename);
            SetProperty("#Trakt.Episode.Number", episode.Episode.Number.ToString());
            SetProperty("#Trakt.Episode.Season", episode.Episode.Season.ToString());
            SetProperty("#Trakt.Episode.FirstAired", episode.Episode.FirstAired.FromEpoch().ToShortDateString());
            SetProperty("#Trakt.Episode.Title", string.IsNullOrEmpty(episode.Episode.Title) ? string.Format("{0} {1}", Translation.Episode, episode.Episode.Number.ToString()) : episode.Episode.Title);
            SetProperty("#Trakt.Episode.Url", episode.Episode.Url);
            SetProperty("#Trakt.Episode.Overview", string.IsNullOrEmpty(episode.Episode.Overview) ? Translation.NoEpisodeSummary : episode.Episode.Overview);
            SetProperty("#Trakt.Episode.Runtime", episode.Episode.Runtime.ToString());
            SetProperty("#Trakt.Episode.EpisodeImageFilename", episode.Episode.Images.EpisodeImageFilename);
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
                            else
                            {
                                // possibly failed to generate cache image on server
                                if (!remoteFanart.Contains("fanart-summary"))
                                {
                                    TraktLogger.Info("Cached Image download failed, attempting to download full size fanart");
                                    remoteFanart = item.ShowImages.Fanart.Replace("-940", string.Empty);
                                    if (GUIImageHandler.DownloadImage(remoteFanart, localFanart))
                                    {
                                        // notify that image has been downloaded
                                        item.NotifyPropertyChanged("ShowImages");
                                    }
                                }
                            }
                        }
                        #endregion
                    }
                    #if !MP12
                    // refresh the facade so thumbnails get displayed
                    // this is not needed in MP 1.2 Beta
                    GUIWindowManager.SendThreadMessage(new GUIMessage(GUIMessage.MessageType.GUI_MSG_REFRESH, GUIWindowManager.ActiveWindow, 0, 50, 0, 0, null));
                    #endif
                })
                {
                    IsBackground = true,
                    Name = "Trakt Episode Image Downloader " + i.ToString()
                }.Start(groupList);
            }
        }

        #endregion

    }

    public class TraktImage : INotifyPropertyChanged
    {
        public TraktEpisode.ShowImages EpisodeImages { get; set; }
        public TraktShow.ShowImages ShowImages { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

    }

    public class GUITraktCalendarListItem : GUIListItem
    {
        public GUITraktCalendarListItem(string strLabel) : base(strLabel) { }

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

            // Get a reference to a MdiaPortal Texture Identifier
            string texture = GUIImageHandler.GetTextureIdentFromFile(imageFilePath);

            // load texture into facade item
            if (GUITextureManager.LoadFromMemory(GUIImageHandler.LoadImage(imageFilePath), texture, 0, 0, 0) > 0)
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
            GUICalendar window = GUIWindowManager.GetWindow(GUIWindowManager.ActiveWindow) as GUICalendar;
            if (window != null)
            {
                GUIListItem selectedItem = GUIControl.GetSelectedListItem(87259, 50);
                if (selectedItem == this)
                {
                    GUIWindowManager.SendThreadMessage(new GUIMessage(GUIMessage.MessageType.GUI_MSG_ITEM_SELECT, GUIWindowManager.ActiveWindow, 0, 50, ItemId, 0, null));
                }
            }
        }
    }
}
