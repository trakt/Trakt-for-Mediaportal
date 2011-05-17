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

        [SkinControlAttribute(50)]
        protected GUIFacadeControl Facade = null;

        #endregion

        #region Constructor

        public GUICalendar() { }

        #endregion

        #region Private Properties

        bool StopDownload { get; set; }        

        IEnumerable<TraktCalendar> TraktCalendar
        {
            get
            {
                if (_Calendar == null)
                {
                    _Calendar = TraktAPI.TraktAPI.GetCalendarForUser(TraktSettings.Username);
                }
                return _Calendar;
            }
        }
        private IEnumerable<TraktCalendar> _Calendar = null;

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
            // clear GUI properties
            ClearProperties();

            // Load Calendar
            LoadCalendar();
        }

        protected override void OnPageDestroy(int new_windowId)
        {
            _Calendar = null;
            StopDownload = true;
            base.OnPageDestroy(new_windowId);
        }

        protected override void OnClicked(int controlId, GUIControl control, Action.ActionType actionType)
        {
            // wait for any background action to finish
            if (GUIBackgroundTask.Instance.IsBusy) return;

            switch (controlId)
            {
                case (50):
                    if (actionType == Action.ActionType.ACTION_SELECT_ITEM)
                    {
                        
                    }
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
            }
            base.OnAction(action);
        }
        #endregion

        #region Private Methods

        private void LoadCalendar()
        {
            GUIBackgroundTask.Instance.ExecuteInBackgroundAndCallback(() =>
            {
                return TraktCalendar;
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
            // clear facade
            GUIControl.ClearControl(GetID, Facade.GetID);
            
            if (calendar.Count() == 0)
            {
            //    GUIWindowManager.ShowPreviousWindow();
            //    return;
            }

            int itemCount = 0;
            List<TraktEpisode.ShowImages> showImages = new List<TraktEpisode.ShowImages>();

            // Add each days episodes to the list
            // Use Label3 of facade for Day/Group Idenitfier
            foreach (var day in calendar)
            {
                GUIListItem item = new GUIListItem();
                
                item.Label3 = DateTime.Parse(day.ToString()).ToLongDateString();
                item.IconImage = "defaultTraktCalendar.png";
                item.IconImageBig = "defaultTraktCalendarBig.png";
                item.ThumbnailImage = "defaultTraktCalendarBig.png";
                //item.OnItemSelected += OnCalendarDateSelected;
                Utils.SetDefaultIcons(item);
                Facade.Add(item);

                foreach (var episode in day.Episodes)
                {
                    GUITraktCalendarListItem episodeItem = new GUITraktCalendarListItem(episode.ToString());

                    episodeItem.Item = episode.Episode.Images;
                    episodeItem.TVTag = episode;
                    episodeItem.IconImage = "defaultTraktEpisode.png";
                    episodeItem.IconImageBig = "defaultTraktEpisodeBig.png";
                    episodeItem.ThumbnailImage = "defaultTraktEpisodeBig.png";
                    episodeItem.OnItemSelected += OnCalendarSelected;
                    Utils.SetDefaultIcons(episodeItem);
                    Facade.Add(episodeItem);
                    itemCount++;

                    // add image for download
                    showImages.Add(episode.Episode.Images);                    
                }                
            }

            Facade.SelectedListItemIndex = 1;

            // Set Facade Layout
            Facade.SetCurrentLayout("List");
            GUIControl.FocusControl(GetID, Facade.GetID);

            // set facade properties
            GUIUtils.SetProperty("#itemcount", itemCount.ToString());

            // Download episode images Async and set to facade
            GetImages(showImages);
        }

        private void OnCalendarSelected(GUIListItem item, GUIControl parent)
        {
            PublishEpisodeSkinProperties(item.TVTag as TraktCalendar.TraktEpisodes);
        }

        private void SetProperty(string property, string value)
        {
            string propertyValue = string.IsNullOrEmpty(value) ? "N/A" : value;
            GUIUtils.SetProperty(property, propertyValue);
        }

        private void ClearProperties()
        {
            SetProperty("#Trakt.Show.Imdb", string.Empty);
            SetProperty("#Trakt.Show.Tvdb", string.Empty);
            SetProperty("#Trakt.Show.TvRage", string.Empty);
            SetProperty("#Trakt.Show.Title", string.Empty);
            SetProperty("#Trakt.Show.Url", string.Empty);
            SetProperty("#Trakt.Show.AirDay", string.Empty);
            SetProperty("#Trakt.Show.AirTime", string.Empty);
            SetProperty("#Trakt.Show.Certification", string.Empty);
            SetProperty("#Trakt.Show.Country", string.Empty);
            SetProperty("#Trakt.Show.FirstAired", string.Empty);
            SetProperty("#Trakt.Show.Network", string.Empty);
            SetProperty("#Trakt.Show.Overview", string.Empty);
            SetProperty("#Trakt.Show.Runtime", string.Empty);
            SetProperty("#Trakt.Show.Year", string.Empty);
            SetProperty("#Trakt.Episode.Number", string.Empty);
            SetProperty("#Trakt.Episode.Season", string.Empty);
            SetProperty("#Trakt.Episode.FirstAired", string.Empty);
            SetProperty("#Trakt.Episode.Title", string.Empty);
            SetProperty("#Trakt.Episode.Url", string.Empty);
            SetProperty("#Trakt.Episode.Overview", string.Empty);
            SetProperty("#Trakt.Episode.Runtime", string.Empty);
            SetProperty("#Trakt.Episode.EpisodeImageFilename", string.Empty);
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
            SetProperty("#Trakt.Show.Overview", episode.Show.Overview);
            SetProperty("#Trakt.Show.Runtime", episode.Show.Runtime.ToString());
            SetProperty("#Trakt.Show.Year", episode.Show.Year.ToString());            
            SetProperty("#Trakt.Episode.Number", episode.Episode.Number.ToString());
            SetProperty("#Trakt.Episode.Season", episode.Episode.Season.ToString());
            SetProperty("#Trakt.Episode.FirstAired", episode.Episode.FirstAired.FromEpoch().ToShortDateString());
            SetProperty("#Trakt.Episode.Title", episode.Episode.Title);
            SetProperty("#Trakt.Episode.Url", episode.Episode.Url);
            SetProperty("#Trakt.Episode.Overview", string.IsNullOrEmpty(episode.Episode.Overview) ? Translation.NoEpisodeSummary : episode.Episode.Overview);
            SetProperty("#Trakt.Episode.Runtime", episode.Episode.Runtime.ToString());
            SetProperty("#Trakt.Episode.EpisodeImageFilename", episode.Episode.Images.EpisodeImageFilename);
        }

        private void GetImages(List<TraktEpisode.ShowImages> itemsWithThumbs)
        {
            StopDownload = false;

            // split the downloads in 5+ groups and do multithreaded downloading
            int groupSize = (int)Math.Max(1, Math.Floor((double)itemsWithThumbs.Count / 5));
            int groups = (int)Math.Ceiling((double)itemsWithThumbs.Count() / groupSize);

            for (int i = 0; i < groups; i++)
            {
                List<TraktEpisode.ShowImages> groupList = new List<TraktEpisode.ShowImages>();
                for (int j = groupSize * i; j < groupSize * i + (groupSize * (i + 1) > itemsWithThumbs.Count ? itemsWithThumbs.Count - groupSize * i : groupSize); j++)
                {
                    groupList.Add(itemsWithThumbs[j]);
                }

                new Thread(delegate(object o)
                {
                    List<TraktEpisode.ShowImages> items = (List<TraktEpisode.ShowImages>)o;
                    foreach (TraktEpisode.ShowImages item in items)
                    {
                        // stop download if we have exited window
                        if (StopDownload) break;

                        string remoteThumb = item.Screen;
                        if (string.IsNullOrEmpty(remoteThumb)) continue;

                        string localThumb = item.EpisodeImageFilename;
                        if (string.IsNullOrEmpty(localThumb)) continue;

                        if (GUIImageHandler.DownloadImage(remoteThumb, localThumb))
                        {
                            // notify that image has been downloaded
                            item.NotifyPropertyChanged("EpisodeImageFilename");
                        }
                    }
                })
                {
                    IsBackground = true,
                    Name = "Trakt Episode Image Downloader " + i.ToString()
                }.Start(groupList);
            }
        }

        #endregion

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
                    if (s is TraktEpisode.ShowImages && e.PropertyName == "EpisodeImageFilename")
                        SetImageToGui((s as TraktEpisode.ShowImages).EpisodeImageFilename);
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
            if (GUITextureManager.LoadFromMemory(ImageFast.FromFile(imageFilePath), texture, 0, 0, 0) > 0)
            {
                ThumbnailImage = texture;
                IconImage = texture;
                IconImageBig = texture;
            }

            // if selected and TraktFriends is current window force an update of thumbnail
            GUITraktFriends window = GUIWindowManager.GetWindow(GUIWindowManager.ActiveWindow) as GUITraktFriends;
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
