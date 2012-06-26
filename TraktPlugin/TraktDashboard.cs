using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using MediaPortal.Util;
using MediaPortal.GUI.Library;
using TraktPlugin.GUI;
using TraktPlugin.TraktAPI;
using TraktPlugin.TraktAPI.DataStructures;

namespace TraktPlugin
{
    internal class TraktDashboard
    {
        #region Private Variables

        private TraktActivity PreviousActivity = null;
        private long ActivityStartTime = 0;

        private Timer ActivityTimer = null;

        bool StopAvatarDownload = false;

        #endregion

        #region Constructor

        public TraktDashboard() { }

        #endregion

        #region Private Methods

        private GUIFacadeControl GetActivityFacade()
        {
            // get current window
            var window = GUIWindowManager.GetWindow(GUIWindowManager.ActiveWindow);

            // get activity facade control
            var control = window.GetControl((int)TraktDashboardControls.ActivityFacade);
            return control as GUIFacadeControl;
        }

        private void ClearActivityProperties()
        {
            GUIUtils.SetProperty("#Trakt.Activity.Count", "0");
            GUIUtils.SetProperty("#Trakt.Activity.Items", string.Format("0 {0}", Translation.Activities));
            GUIUtils.SetProperty("#Trakt.Activity.Description", TraktSettings.ShowCommunityActivity ? Translation.ActivityCommunityDesc : Translation.ActivityFriendsDesc);
        }

        private void LoadActivity()
        {
            GUIFacadeControl facade = null;
            int i = 0;

            // get the facade, may need to wait until
            // window has completely loaded
            do
            {
                facade = GetActivityFacade();
                if (facade == null) Thread.Sleep(250);
                i++;
            }
            while (i < 10 && facade == null);

            // no luck, possible skinning error
            if (facade == null) return;

            // load facade if empty and we have activity already
            // facade is empty on re-load of window
            if (facade.Count == 0 && PreviousActivity != null && PreviousActivity.Activities.Count > 0)
            {
                LoadActivityFacade(PreviousActivity, facade);
            }

            // get latest activity
            var activities = GetActivity(TraktSettings.ShowCommunityActivity);

            // load activity into list
            LoadActivityFacade(activities, facade);
        }

        private void LoadActivityFacade(TraktActivity activities, GUIFacadeControl facade)
        {
            if (!TraktSkinSettings.DashBoardActivityWindows.Contains(GUIWindowManager.ActiveWindow.ToString()))
                return;

            // if no activities report to user
            if (activities == null || activities.Activities.Count == 0)
            {
                GUIListItem item = new GUIListItem(Translation.NoActivities);
                facade.Add(item);
                facade.SetCurrentLayout("List");
                ClearActivityProperties();
                return;
            }

            // if no new activities then nothing to do
            if (facade.Count > 0)
            {
                var mostRecentActivity = facade[0].TVTag as TraktActivity.Activity;
                if (mostRecentActivity != null)
                {
                    if (mostRecentActivity.Timestamp == activities.Activities.First().Timestamp &&
                        mostRecentActivity.User.Username == activities.Activities.First().User.Username)
                    {
                        return;
                    }
                }
            }

            // stop any existing image downloads
            StopAvatarDownload = true;

            // clear facade
            GUIControl.ClearControl(GUIWindowManager.ActiveWindow, facade.GetID);

            int itemId = 0;
            var avatarImages = new List<TraktUserProfile>();

            // Add each activity item to the facade
            foreach (var activity in activities.Activities)
            {
                GUITraktDashboardListItem item = new GUITraktDashboardListItem(GetListItemTitle(activity));

                string activityImage = GetActivityImage(activity);
                string avatarImage = GetAvatarImage(activity);

                item.Label2 = string.Format("{0}", activity.Timestamp.FromEpoch().ToLocalTime().ToShortTimeString());
                item.TVTag = activity;
                item.Item = activity.User;
                item.ItemId = Int32.MaxValue - itemId;
                item.IconImage = avatarImage;
                item.IconImageBig = avatarImage;
                item.ThumbnailImage = avatarImage;
                item.PinImage = activityImage;
                //item.OnItemSelected += OnActivitySelected;
                facade.Add(item);
                itemId++;

                // add image for download
                if (avatarImage == "defaultTraktUser.png")
                    avatarImages.Add(activity.User);
            }

            // Set Facade Layout
            facade.SetCurrentLayout("List");

            // set facade properties
            GUIUtils.SetProperty("#Trakt.Activity.Count", activities.Activities.Count().ToString());
            GUIUtils.SetProperty("#Trakt.Activity.Items", string.Format("{0} {1}", activities.Activities.Count().ToString(), activities.Activities.Count() > 1 ? Translation.Activities : Translation.Activity));
            GUIUtils.SetProperty("#Trakt.Activity.Description", TraktSettings.ShowCommunityActivity ? Translation.ActivityCommunityDesc : Translation.ActivityFriendsDesc);

            // Download avatar images Async and set to facade
            StopAvatarDownload = false;
            GetImages<TraktUserProfile>(avatarImages);
        }

        private string GetActivityImage(TraktActivity.Activity activity)
        {
            string imageFilename = string.Empty;

            switch (activity.Action)
            {
                case "checkin":
                case "watching":
                    imageFilename = "traktActivityWatching.png";
                    break;

                case "seen":
                case "scrobble":
                    imageFilename = "traktActivityWatched.png";
                    break;

                case "collection":
                    imageFilename = "traktActivityCollected.png";
                    break;

                case "rating":
                    imageFilename = int.Parse(activity.RatingAdvanced) > 5 ? "traktActivityLove.png" : "traktActivityHate.png";
                    break;

                case "watchlist":
                    imageFilename = "traktActivityWatchlist.png";
                    break;

                case "shout":
                    imageFilename = "traktActivityShout.png";
                    break;

                case "item_added":
                case "created":
                    imageFilename = "traktActivityList.png";
                    break;
            }

            return imageFilename;
        }

        private string GetAvatarImage(TraktActivity.Activity activity)
        {
            string filename = activity.User.AvatarFilename;
            if (string.IsNullOrEmpty(filename) || !System.IO.File.Exists(filename))
            {
                filename = "defaultTraktUser.png";
            }
            return filename;
        }

        private string GetListItemTitle(TraktActivity.Activity activity)
        {
            string itemName = GetActivityItemName(activity);
            string userName = activity.User.Username;
            string title = string.Empty;

            switch (activity.Action)
            {
                case "watching":
                    title = string.Format(Translation.ActivityWatching, userName, itemName);
                    break;

                case "scrobble":
                    title = string.Format(Translation.ActivityWatched, userName, itemName);
                    break;

                case "checkin":
                    title = string.Format(Translation.ActivityCheckedIn, userName, itemName);
                    break;

                case "seen":
                    if (activity.Type == "episode" && activity.Episodes.Count > 1)
                    {
                        title = string.Format(Translation.ActivitySeenEpisodes, userName, activity.Episodes.Count, itemName);
                    }
                    else
                    {
                        title = string.Format(Translation.ActivitySeen, userName, itemName);
                    }
                    break;

                case "collection":
                    if (activity.Type == "episode" && activity.Episodes.Count > 1)
                    {
                        title = string.Format(Translation.ActivityCollectedEpisodes, userName, activity.Episodes.Count, itemName);
                    }
                    else
                    {
                        title = string.Format(Translation.ActivityCollected, userName, itemName);
                    }
                    break;

                case "rating":
                    if (activity.UseRatingAdvanced)
                    {
                        title = string.Format(Translation.ActivityRatingAdvanced, userName, itemName, activity.RatingAdvanced);
                    }
                    else
                    {
                        title = string.Format(Translation.ActivityRating, userName, itemName);
                    }
                    break;

                case "watchlist":
                    title = string.Format(Translation.ActivityWatchlist, userName, itemName);
                    break;

                case "shout":
                    title = string.Format(Translation.ActivityShouts, userName, itemName);
                    break;

                case "created": // created list
                    title = string.Format(Translation.ActivityCreatedList, userName, itemName);
                    break;

                case "item_added": // added item to list
                    title = string.Format(Translation.ActivityAddToList, userName, itemName, activity.List.Name);
                    break;
            }

            return title;
        }

        private string GetActivityItemName(TraktActivity.Activity activity)
        {
            string name = string.Empty;

            switch (activity.Type)
            {
                case "episode":
                    if (activity.Action == "seen" || activity.Action == "collection")
                    {
                        if (activity.Episodes.Count > 1)
                        {
                            // just return show name
                            name = activity.Show.Title;
                        }
                        else
                        {
                            //  get the first and only item in collection of episodes
                            string episodeIndex = activity.Episodes.First().Number.ToString();
                            string seasonIndex = activity.Episodes.First().Season.ToString();
                            string episodeName = activity.Episodes.First().Title;

                            if (string.IsNullOrEmpty(episodeName))
                                episodeName = string.Format("{0} {1}", Translation.Episode, episodeIndex);

                            name = string.Format("{0} - {1}x{2} - {3}", activity.Show.Title, seasonIndex, episodeIndex, episodeName);
                        }
                    }
                    else
                    {
                        string episodeName = activity.Episode.Title;

                        if (string.IsNullOrEmpty(episodeName))
                            episodeName = string.Format("{0} {1}", Translation.Episode, activity.Episode.Number.ToString());

                        name = string.Format("{0} - {1}x{2} - {3}", activity.Show.Title, activity.Episode.Season.ToString(), activity.Episode.Number.ToString(), episodeName);
                    }
                    break;

                case "show":
                    name = activity.Show.Title;
                    break;

                case "movie":
                    name = string.Format("{0} ({1})", activity.Movie.Title, activity.Movie.Year);
                    break;

                case "list":
                    if (activity.Action == "item_added")
                    {
                        // return the name of the item added to the list
                        switch (activity.ListItem.Type)
                        {
                            case "show":
                                name = activity.ListItem.Show.Title;
                                break;

                            case "episode":
                                string episodeIndex = activity.ListItem.Episode.Number.ToString();
                                string seasonIndex = activity.ListItem.Episode.Season.ToString();
                                string episodeName = activity.ListItem.Episode.Title;

                                if (string.IsNullOrEmpty(episodeName))
                                    episodeName = string.Format("{0} {1}", Translation.Episode, episodeIndex);

                                name = string.Format("{0} - {1}x{2} - {3}", activity.ListItem.Show.Title, seasonIndex, episodeIndex, episodeName);
                                break;

                            case "movie":
                                name = activity.ListItem.Movie.Title;
                                break;
                        }
                    }
                    else if (activity.Action == "created")
                    {
                        // return the list name
                        name = activity.List.Name;
                    }
                    break;
            }

            return name;
        }

        private TraktActivity GetActivity(bool community)
        {
            if (PreviousActivity == null || ActivityStartTime <= 0)
            {
                PreviousActivity = community ? TraktAPI.TraktAPI.GetCommunityActivity() : TraktAPI.TraktAPI.GetFriendActivity();
            }
            else
            {
                TraktActivity incrementalActivity = null;

                // get latest incremental change using last current timestamp as start point
                if (community)
                {
                    incrementalActivity = TraktAPI.TraktAPI.GetCommunityActivity(null, null, ActivityStartTime, DateTime.UtcNow.ToEpoch());
                }
                else
                {
                    incrementalActivity = TraktAPI.TraktAPI.GetFriendActivity(null, null, ActivityStartTime, DateTime.UtcNow.ToEpoch());
                }

                // join the Previous request with the current
                if (incrementalActivity != null)
                {
                    TraktLogger.Debug("Response: {0}", incrementalActivity.ToJSON());
                    PreviousActivity.Activities = incrementalActivity.Activities.Union(PreviousActivity.Activities).Take(100).ToList();
                    PreviousActivity.Timestamps = incrementalActivity.Timestamps;
                }
            }

            // store current timestamp and only request incremental change next time
            if (PreviousActivity != null)
            {
                ActivityStartTime = PreviousActivity.Timestamps.Current;
            }

            return PreviousActivity;
        }

        private void GetImages<T>(List<T> itemsWithThumbs)
        {
            // split the downloads in 5+ groups and do multithreaded downloading
            int groupSize = (int)Math.Max(1, Math.Floor((double)itemsWithThumbs.Count / 5));
            int groups = (int)Math.Ceiling((double)itemsWithThumbs.Count() / groupSize);

            for (int i = 0; i < groups; i++)
            {
                List<T> groupList = new List<T>();
                for (int j = groupSize * i; j < groupSize * i + (groupSize * (i + 1) > itemsWithThumbs.Count ? itemsWithThumbs.Count - groupSize * i : groupSize); j++)
                {
                    groupList.Add(itemsWithThumbs[j]);
                }

                new Thread(delegate(object o)
                {
                    List<T> items = (List<T>)o;
                    foreach (T item in items)
                    {
                        string remoteThumb = string.Empty;
                        string localThumb = string.Empty;

                        if (item is TraktUserProfile)
                        {
                            if (StopAvatarDownload) return;
                            remoteThumb = (item as TraktUserProfile).Avatar;
                            localThumb = (item as TraktUserProfile).AvatarFilename;
                        }
                        else if (item is TraktMovie.MovieImages)
                        {
                            if ((item as TraktMovie.MovieImages) != null)
                            {
                                remoteThumb = (item as TraktMovie.MovieImages).Poster;
                                localThumb = (item as TraktMovie.MovieImages).PosterImageFilename;
                            }
                        }
                        else if (item is TraktShow.ShowImages)
                        {
                            if ((item as TraktShow.ShowImages) != null)
                            {
                                remoteThumb = (item as TraktShow.ShowImages).Poster;
                                localThumb = (item as TraktShow.ShowImages).PosterImageFilename;
                            }
                        }

                        if (!string.IsNullOrEmpty(remoteThumb) && !string.IsNullOrEmpty(localThumb))
                        {
                            if (GUIImageHandler.DownloadImage(remoteThumb, localThumb))
                            {
                                // notify that image has been downloaded
                                if (item is TraktUserProfile)
                                {
                                    if (StopAvatarDownload) return;
                                    (item as TraktUserProfile).NotifyPropertyChanged("AvatarFilename");
                                }
                                else if (item is TraktMovie.MovieImages)
                                {
                                    (item as TraktMovie.MovieImages).NotifyPropertyChanged("PosterImageFilename");
                                }
                                else if (item is TraktShow.ShowImages)
                                {
                                    (item as TraktShow.ShowImages).NotifyPropertyChanged("PosterImageFilename");
                                }
                            }
                        }
                    }
                })
                {
                    IsBackground = true,
                    Name = "Trakt Dashboard Image Downloader " + i.ToString()
                }.Start(groupList);
            }
        }

        #endregion

        #region Public Properties

        #endregion

        #region Public Methods

        public void Init()
        {
            // initalize timercallbacks
            if (TraktSkinSettings.DashBoardActivityWindows.Count > 0)
            {
                ClearActivityProperties();
                ActivityTimer = new Timer(new TimerCallback((o) => { LoadActivity(); }), null, Timeout.Infinite, Timeout.Infinite);
            }
        }

        public void StartActivityPolling()
        {
            if (ActivityTimer != null)
            {
                ActivityTimer.Change(0, 15000);
            }
        }

        public void StopActivityPolling()
        {
            if (ActivityTimer != null)
            {
                ActivityTimer.Change(Timeout.Infinite, Timeout.Infinite);
            }
        }

        #endregion
    }

    public class GUITraktDashboardListItem : GUIListItem
    {
        public GUITraktDashboardListItem(string strLabel) : base(strLabel) { }

        public object Item
        {
            get { return _Item; }
            set
            {
                _Item = value;
                INotifyPropertyChanged notifier = value as INotifyPropertyChanged;
                if (notifier != null) notifier.PropertyChanged += (s, e) =>
                {
                    if (s is TraktUserProfile && e.PropertyName == "AvatarFilename")
                        SetImageToGui((s as TraktUserProfile).AvatarFilename);
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
        }
    }

}
