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
using MediaPortal.Video.Database;
using MediaPortal.GUI.Video;
using Action = MediaPortal.GUI.Library.Action;
using MediaPortal.Util;
using TraktPlugin.TraktAPI.v1;
using TraktPlugin.TraktAPI.v1.DataStructures;
using TraktPlugin.TraktAPI.v1.Extensions;

namespace TraktPlugin.GUI
{
    public class GUIRecentShouts : GUIWindow
    {
        #region Skin Controls

        [SkinControl(50)]
        protected GUIFacadeControl Facade = null;

        [SkinControl(2)]
        protected GUICheckButton hideSpoilersButton = null;

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
            Spoilers,
            RemoveFromWatchList,
            AddToWatchList,
            AddToList,
            MarkAsWatched,
            MarkAsUnWatched,
            AddToLibrary,
            RemoveFromLibrary,
            Related,
            Rate,
            Shouts,
            Trailers,
            SearchWithMpNZB,
            SearchTorrent
        }

        #endregion

        #region Constructor

        public GUIRecentShouts()
        {
            backdrop = new ImageSwapper();
            backdrop.PropertyOne = "#Trakt.RecentShouts.Fanart.1";
            backdrop.PropertyTwo = "#Trakt.RecentShouts.Fanart.2";
        }

        #endregion

        #region Private Variables

        static int PreviousSelectedIndex { get; set; }
        static DateTime LastRequest = new DateTime();
        string PreviousUser = null;
        Layout CurrentLayout { get; set; }
        ImageSwapper backdrop;
        Dictionary<string, IEnumerable<TraktActivity.Activity>> userRecentShouts = new Dictionary<string, IEnumerable<TraktActivity.Activity>>();

        IEnumerable<TraktActivity.Activity> RecentShouts
        {
            get
            {
                if (!userRecentShouts.Keys.Contains(CurrentUser) || LastRequest < DateTime.UtcNow.Subtract(new TimeSpan(0, TraktSettings.WebRequestCacheMinutes, 0)))
                {
                    TraktActivity activity = TraktAPI.v1.TraktAPI.GetUserActivity
                    (
                        CurrentUser,
                        new List<TraktAPI.v1.ActivityType>() { TraktAPI.v1.ActivityType.all },
                        new List<TraktAPI.v1.ActivityAction>() { TraktAPI.v1.ActivityAction.review, TraktAPI.v1.ActivityAction.shout }
                    );

                    _RecentlyShouts = activity.Activities;
                    if (userRecentShouts.Keys.Contains(CurrentUser)) userRecentShouts.Remove(CurrentUser);
                    userRecentShouts.Add(CurrentUser, _RecentlyShouts);
                    LastRequest = DateTime.UtcNow;
                    PreviousSelectedIndex = 0;
                }
                return userRecentShouts[CurrentUser];
            }
        }
        private IEnumerable<TraktActivity.Activity> _RecentlyShouts = null;

        #endregion

        #region Public Properties

        public static string CurrentUser { get; set; }

        #endregion

        #region Base Overrides

        public override int GetID
        {
            get
            {
                return (int)TraktGUIWindows.RecentShouts;
            }
        }

        public override bool Init()
        {
            return Load(GUIGraphicsContext.Skin + @"\Trakt.RecentShouts.xml");
        }

        protected override void OnPageLoad()
        {
            base.OnPageLoad();

            // Clear GUI Properties
            ClearProperties();

            // Requires Login
            if (!GUICommon.CheckLogin()) return;

            // Init Properties
            InitProperties();

            // Load Recently Added
            LoadRecentShouts();
        }

        protected override void OnPageDestroy(int new_windowId)
        {
            GUICustomListItem.StopDownload = true;
            PreviousSelectedIndex = Facade.SelectedListItemIndex;
            ClearProperties();

            if (hideSpoilersButton != null)
            {
                TraktSettings.HideSpoilersOnShouts = hideSpoilersButton.Selected;
            }

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
                        PreviousUser = CurrentUser;
                        PlayActivityItem(true);
                    }
                    break;

                // Hide Spoilers Button
                case (2):
                    TraktSettings.HideSpoilersOnShouts = !TraktSettings.HideSpoilersOnShouts;
                    PublishShoutSkinProperties(Facade.SelectedListItem.TVTag as TraktActivity.Activity);
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
                    // restore current user
                    PreviousUser = CurrentUser;
                    CurrentUser = TraktSettings.Username;
                    base.OnAction(action);
                    break;
                case Action.ActionType.ACTION_PLAY:
                case Action.ActionType.ACTION_MUSIC_PLAY:
                    PreviousUser = CurrentUser;
                    PlayActivityItem(false);
                    break;
                default:
                    base.OnAction(action);
                    break;
            }
        }

        protected override void OnShowContextMenu()
        {
            var selectedItem = this.Facade.SelectedListItem;
            if (selectedItem == null) return;

            var selectedActivity = selectedItem.TVTag as TraktActivity.Activity;
            if (selectedActivity == null) return;

            var type = (ActivityType)Enum.Parse(typeof(ActivityType), selectedActivity.Type);

            var dlg = (IDialogbox)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
            if (dlg == null) return;

            dlg.Reset();
            dlg.SetHeading(GUIUtils.PluginName());

            GUIListItem listItem = null;

            listItem = new GUIListItem(TraktSettings.HideSpoilersOnShouts ? Translation.ShowSpoilers : Translation.HideSpoilers);
            dlg.Add(listItem);
            listItem.ItemId = (int)ContextMenuItem.Spoilers;

            // if selected activity is an episode or show, add 'Season Info'
            if (selectedActivity.Show != null)
            {
                listItem = new GUIListItem(Translation.ShowSeasonInfo);
                dlg.Add(listItem);
                listItem.ItemId = (int)ActivityContextMenuItem.ShowSeasonInfo;
            }

            // get a list of common actions to perform on the selected item
            if (selectedActivity.Movie != null || selectedActivity.Show != null)
            {
                var listItems = GUICommon.GetContextMenuItemsForActivity();
                foreach (var item in listItems)
                {
                    int itemId = item.ItemId;
                    dlg.Add(item);
                    item.ItemId = itemId;
                }
            }

            // Show Context Menu
            dlg.DoModal(GUIWindowManager.ActiveWindow);
            if (dlg.SelectedId < 0) return;

            switch (dlg.SelectedId)
            {
                case ((int)ContextMenuItem.Spoilers):
                    TraktSettings.HideSpoilersOnShouts = !TraktSettings.HideSpoilersOnShouts;
                    if (hideSpoilersButton != null) hideSpoilersButton.Selected = TraktSettings.HideSpoilersOnShouts;
                    PublishShoutSkinProperties(selectedActivity);
                    break;

                case ((int)ActivityContextMenuItem.ShowSeasonInfo):
                    GUIWindowManager.ActivateWindow((int)TraktGUIWindows.ShowSeasons, selectedActivity.Show.ToJSON());
                    break;

                case ((int)ActivityContextMenuItem.AddToList):
                    if (selectedActivity.Movie != null)
                        TraktHelper.AddRemoveMovieInUserList(selectedActivity.Movie, false);
                    else if (selectedActivity.Episode != null)
                        TraktHelper.AddRemoveEpisodeInUserList(selectedActivity.Show, selectedActivity.Episode, false);
                    else
                        TraktHelper.AddRemoveShowInUserList(selectedActivity.Show, false);
                    break;

                case ((int)ActivityContextMenuItem.AddToWatchList):
                    if (selectedActivity.Movie != null)
                        TraktHelper.AddMovieToWatchList(selectedActivity.Movie, true);
                    else if (selectedActivity.Episode != null)
                        TraktHelper.AddEpisodeToWatchList(selectedActivity.Show, selectedActivity.Episode);
                    else
                        TraktHelper.AddShowToWatchList(selectedActivity.Show);
                    break;

                case ((int)ActivityContextMenuItem.Shouts):
                    if (selectedActivity.Movie != null)
                        TraktHelper.ShowMovieShouts(selectedActivity.Movie);
                    else if (selectedActivity.Episode != null)
                        TraktHelper.ShowEpisodeShouts(selectedActivity.Show, selectedActivity.Episode);
                    else
                        TraktHelper.ShowTVShowShouts(selectedActivity.Show);
                    break;

                case ((int)ActivityContextMenuItem.Rate):
                    if (selectedActivity.Movie != null)
                        GUICommon.RateMovie(selectedActivity.Movie);
                    else if (selectedActivity.Episode != null)
                        GUICommon.RateEpisode(selectedActivity.Show, selectedActivity.Episode);
                    else
                        GUICommon.RateShow(selectedActivity.Show);
                    break;

                case ((int)ActivityContextMenuItem.Trailers):
                    if (selectedActivity.Movie != null)
                        GUICommon.ShowMovieTrailersMenu(selectedActivity.Movie);
                    else
                        GUICommon.ShowTVShowTrailersMenu(selectedActivity.Show, selectedActivity.Episode);
                    break;
            }

            base.OnShowContextMenu();
        }

        #endregion

        #region Private Methods

        private void PlayActivityItem(bool jumpTo)
        {
            var selectedItem = this.Facade.SelectedListItem;
            if (selectedItem == null) return;

            var selectedActivity = selectedItem.TVTag as TraktActivity.Activity;
            if (selectedActivity == null) return;

            var type = (ActivityType)Enum.Parse(typeof(ActivityType), selectedActivity.Type);

            switch (type)
            {
                case ActivityType.episode:
                    GUICommon.CheckAndPlayEpisode(selectedActivity.Show, selectedActivity.Episode);
                    break;

                case ActivityType.show:
                    GUICommon.CheckAndPlayFirstUnwatchedEpisode(selectedActivity.Show, jumpTo);
                    break;

                case ActivityType.movie:
                    GUICommon.CheckAndPlayMovie(jumpTo, selectedActivity.Movie);
                    break;
            }
        }

        private void LoadRecentShouts()
        {
            GUIUtils.SetProperty("#Trakt.Items", string.Empty);

            GUIBackgroundTask.Instance.ExecuteInBackgroundAndCallback(() =>
            {
                return RecentShouts;
            },
            delegate(bool success, object result)
            {
                if (success)
                {
                    IEnumerable<TraktActivity.Activity> activities = result as IEnumerable<TraktActivity.Activity>;
                    SendRecentShoutsToFacade(activities);
                }
            }, Translation.GettingUserRecentShouts, true);
        }

        private void SendRecentShoutsToFacade(IEnumerable<TraktActivity.Activity> activities)
        {
            // clear facade
            GUIControl.ClearControl(GetID, Facade.GetID);

            // protected profiles might return null
            if (activities == null || activities.Count() == 0)
            {
                GUIUtils.ShowNotifyDialog(GUIUtils.PluginName(), Translation.UserHasNoRecentShouts);
                PreviousUser = CurrentUser;
                CurrentUser = TraktSettings.Username;
                GUIWindowManager.ShowPreviousWindow();
                return;
            }

            int itemId = 0;
            var shoutImages = new List<TraktImage>();

            // Add each item added
            foreach (var activity in activities)
            {
                // bad api data
                if (activity.Movie == null && activity.Show == null) continue;

                var item = new GUICustomListItem(GUICommon.GetActivityListItemTitle(activity), (int)TraktGUIWindows.RecentShouts);

                // add images for download
                var images = new TraktImage
                {
                    ShowImages = activity.Show != null ? activity.Show.Images : null,
                    MovieImages = activity.Movie != null ? activity.Movie.Images : null
                };
                shoutImages.Add(images);

                // add user shout date as second label
                item.Label2 = activity.Timestamp.FromEpoch().ToShortDateString();
                item.TVTag = activity;
                item.Images = images;
                item.ItemId = Int32.MaxValue - itemId;
                item.IconImage = GUIImageHandler.GetDefaultPoster(false);
                item.IconImageBig = GUIImageHandler.GetDefaultPoster();
                item.ThumbnailImage = GUIImageHandler.GetDefaultPoster();
                item.PinImage = "traktActivityShout.png";
                item.OnItemSelected += OnShoutSelected;
                Utils.SetDefaultIcons(item);
                Facade.Add(item);
                itemId++;
            }

            // set Facade Layout
            Facade.SetCurrentLayout("List");
            GUIControl.FocusControl(GetID, Facade.GetID);

            if (PreviousSelectedIndex >= activities.Count())
                Facade.SelectIndex(PreviousSelectedIndex - 1);
            else
                Facade.SelectIndex(PreviousSelectedIndex);

            // set facade properties
            GUIUtils.SetProperty("#itemcount", activities.Count().ToString());
            GUIUtils.SetProperty("#Trakt.Items", string.Format("{0} {1}", activities.Count().ToString(), activities.Count() > 1 ? Translation.Shout : Translation.Shouts));

            // Download images Async and set to facade
            GUICustomListItem.GetImages(shoutImages);
        }

        private string GetActivityShoutText(TraktActivity.Activity activity)
        {
            if (activity.Shout.Spoiler && TraktSettings.HideSpoilersOnShouts) 
                return Translation.HiddenToPreventSpoilers;

            return activity.Shout.Text;
        }

        private string GetActivityReviewText(TraktActivity.Activity activity)
        {
            if (activity.Review.Spoiler &&  TraktSettings.HideSpoilersOnShouts) 
                return Translation.HiddenToPreventSpoilers;

            return activity.Review.Text;
        }

        private void InitProperties()
        {
            // Fanart
            backdrop.GUIImageOne = FanartBackground;
            backdrop.GUIImageTwo = FanartBackground2;
            backdrop.LoadingImage = loadingImage;

            // load recently added for user
            if (string.IsNullOrEmpty(CurrentUser)) CurrentUser = TraktSettings.Username;
            GUICommon.SetProperty("#Trakt.RecentShouts.CurrentUser", CurrentUser);

            if (hideSpoilersButton != null)
            {
                hideSpoilersButton.Label = Translation.HideSpoilers;
                hideSpoilersButton.Selected = TraktSettings.HideSpoilersOnShouts;
            }

            // don't remember previous selected if a different user
            if (PreviousUser != CurrentUser)
                PreviousSelectedIndex = 0;
        }

        private void ClearProperties()
        {
            GUIUtils.SetProperty("#Trakt.Shout.Type", string.Empty);
            GUIUtils.SetProperty("#Trakt.Shout.Date", string.Empty);

            GUIUtils.SetProperty("#Trakt.Shout.Text", string.Empty);
            GUIUtils.SetProperty("#Trakt.Shout.Spoiler", string.Empty);
            GUIUtils.SetProperty("#Trakt.Shout.Review", string.Empty);

            GUICommon.ClearMovieProperties();
            GUICommon.ClearShowProperties();
            GUICommon.ClearEpisodeProperties();
            GUICommon.ClearUserProperties();                
        }

        private void PublishShoutSkinProperties(TraktActivity.Activity activity)
        {
            if (activity == null) return;

            if (activity.Shout == null && activity.Review == null) return;

            GUIUtils.SetProperty("#Trakt.Shout.Type", activity.Type);
            GUICommon.SetProperty("#Trakt.Shout.Date", activity.Timestamp.FromEpoch().ToShortDateString());

            // set shout/review properties
            GUIUtils.SetProperty("#Trakt.Shout.Text", activity.Shout != null ? GetActivityShoutText(activity) : GetActivityReviewText(activity));
            GUIUtils.SetProperty("#Trakt.Shout.Spoiler", activity.Shout != null ? activity.Shout.Spoiler.ToString() : activity.Review.Spoiler.ToString());
            GUIUtils.SetProperty("#Trakt.Shout.Review", (activity.Review != null).ToString());

            // set user properties
            GUICommon.SetUserProperties(activity.User);

            // set movie, show or episode properties
            // set show and episode properties for episode shouts
            if (activity.Movie != null)
            {
                GUICommon.SetMovieProperties(activity.Movie);
            }
            else
            {
                GUICommon.SetShowProperties(activity.Show);
                if (activity.Episode != null) 
                    GUICommon.SetEpisodeProperties(activity.Episode);
            }
        }

        private void OnShoutSelected(GUIListItem item, GUIControl parent)
        {
            var activity = item.TVTag as TraktActivity.Activity;
            if (activity == null) return;

            PublishShoutSkinProperties(activity);

            string fanartFileName = activity.Movie != null ? activity.Movie.Images.Fanart.LocalImageFilename(ArtworkType.MovieFanart) : activity.Show.Images.Fanart.LocalImageFilename(ArtworkType.ShowFanart);
            GUIImageHandler.LoadFanart(backdrop, fanartFileName);
        }
        #endregion
    }
}
