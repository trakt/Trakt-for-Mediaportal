using System;
using System.Collections.Generic;
using System.Linq;
using MediaPortal.GUI.Library;
using MediaPortal.Util;
using TraktPlugin.Extensions;
using TraktPlugin.TraktAPI.DataStructures;
using TraktPlugin.TraktAPI.Enums;
using TraktPlugin.TraktAPI.Extensions;
using Action = MediaPortal.GUI.Library.Action;

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
        Dictionary<string, IEnumerable<TraktCommentItem>> userRecentComments = new Dictionary<string, IEnumerable<TraktCommentItem>>();

        IEnumerable<TraktCommentItem> RecentComments
        {
            get
            {
                if (!userRecentComments.Keys.Contains(CurrentUser) || LastRequest < DateTime.UtcNow.Subtract(new TimeSpan(0, TraktSettings.WebRequestCacheMinutes, 0)))
                {
                    var comments = TraktAPI.TraktAPI.GetUsersComments(CurrentUser, "all", "all", 1, TraktSettings.MaxUserCommentsRequest);

                    _RecentlyComments = comments;
                    if (userRecentComments.Keys.Contains(CurrentUser)) userRecentComments.Remove(CurrentUser);
                    userRecentComments.Add(CurrentUser, _RecentlyComments);
                    LastRequest = DateTime.UtcNow;
                    PreviousSelectedIndex = 0;
                }
                return userRecentComments[CurrentUser];
            }
        }
        private IEnumerable<TraktCommentItem> _RecentlyComments = null;

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
            LoadRecentComments();
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
                        PlayCommentItem(true);
                    }
                    break;

                // Hide Spoilers Button
                case (2):
                    TraktSettings.HideSpoilersOnShouts = !TraktSettings.HideSpoilersOnShouts;
                    PublishCommentSkinProperties(Facade.SelectedListItem.TVTag as TraktCommentItem);
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
                    PlayCommentItem(false);
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

            var selectedComment = selectedItem.TVTag as TraktCommentItem;
            if (selectedComment == null) return;

            var dlg = (IDialogbox)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
            if (dlg == null) return;

            dlg.Reset();
            dlg.SetHeading(GUIUtils.PluginName());

            GUIListItem listItem = null;

            listItem = new GUIListItem(TraktSettings.HideSpoilersOnShouts ? Translation.ShowSpoilers : Translation.HideSpoilers);
            dlg.Add(listItem);
            listItem.ItemId = (int)ContextMenuItem.Spoilers;

            // if selected activity is an episode or show, add 'Season Info'
            if (selectedComment.Show != null)
            {
                listItem = new GUIListItem(Translation.ShowSeasonInfo);
                dlg.Add(listItem);
                listItem.ItemId = (int)ActivityContextMenuItem.ShowSeasonInfo;
            }

            // get a list of common actions to perform on the selected item
            if (selectedComment.Movie != null || selectedComment.Show != null)
            {
                var listItems = GetContextMenuItemsForComment(selectedComment);
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
                    PublishCommentSkinProperties(selectedComment);
                    break;

                case ((int)ActivityContextMenuItem.ShowSeasonInfo):
                    GUIWindowManager.ActivateWindow((int)TraktGUIWindows.ShowSeasons, selectedComment.Show.ToJSON());
                    break;

                case ((int)ActivityContextMenuItem.AddToList):
                    if (selectedComment.Movie != null)
                        TraktHelper.AddRemoveMovieInUserList(selectedComment.Movie, false);
                    else if (selectedComment.Episode != null)
                        TraktHelper.AddRemoveEpisodeInUserList(selectedComment.Episode, false);
                    else
                        TraktHelper.AddRemoveShowInUserList(selectedComment.Show, false);
                    break;

                case ((int)ActivityContextMenuItem.AddToWatchList):
                    if (selectedComment.Movie != null)
                        TraktHelper.AddMovieToWatchList(selectedComment.Movie, true);
                    else if (selectedComment.Episode != null)
                        TraktHelper.AddEpisodeToWatchList(selectedComment.Episode);
                    else
                        TraktHelper.AddShowToWatchList(selectedComment.Show);
                    break;

                case ((int)ActivityContextMenuItem.Shouts):
                    if (selectedComment.Movie != null)
                        TraktHelper.ShowMovieShouts(selectedComment.Movie);
                    else if (selectedComment.Episode != null)
                        TraktHelper.ShowEpisodeShouts(selectedComment.Show, selectedComment.Episode);
                    else
                        TraktHelper.ShowTVShowShouts(selectedComment.Show);
                    break;

                case ((int)ActivityContextMenuItem.Rate):
                    if (selectedComment.Movie != null)
                        GUICommon.RateMovie(selectedComment.Movie);
                    else if (selectedComment.Episode != null)
                        GUICommon.RateEpisode(selectedComment.Show, selectedComment.Episode);
                    else
                        GUICommon.RateShow(selectedComment.Show);
                    break;

                case ((int)ActivityContextMenuItem.Trailers):
                    if (selectedComment.Movie != null)
                        GUICommon.ShowMovieTrailersMenu(selectedComment.Movie);
                    else
                        GUICommon.ShowTVShowTrailersMenu(selectedComment.Show, selectedComment.Episode);
                    break;
            }

            base.OnShowContextMenu();
        }

        #endregion

        #region Private Methods

        private List<GUIListItem> GetContextMenuItemsForComment(TraktCommentItem commentItem)
        {
            GUIListItem listItem = null;
            var listItems = new List<GUIListItem>();

            // Add Watchlist
            if (!commentItem.IsWatchlisted())
            {
                listItem = new GUIListItem(Translation.AddToWatchList);
                listItem.ItemId = (int)ActivityContextMenuItem.AddToWatchList;
                listItems.Add(listItem);
            }
            else if (commentItem.Type != ActivityType.list.ToString())
            {
                listItem = new GUIListItem(Translation.RemoveFromWatchList);
                listItem.ItemId = (int)ActivityContextMenuItem.RemoveFromWatchList;
                listItems.Add(listItem);
            }

            // Mark As Watched
            if (commentItem.Type == ActivityType.episode.ToString() || commentItem.Type == ActivityType.movie.ToString())
            {
                if (!commentItem.IsWatched())
                {
                    listItem = new GUIListItem(Translation.MarkAsWatched);
                    listItem.ItemId = (int)ActivityContextMenuItem.MarkAsWatched;
                    listItems.Add(listItem);
                }
                else
                {
                    listItem = new GUIListItem(Translation.MarkAsUnWatched);
                    listItem.ItemId = (int)ActivityContextMenuItem.MarkAsUnwatched;
                    listItems.Add(listItem);
                }
            }

            // Add To Collection
            if (commentItem.Type == ActivityType.episode.ToString() || commentItem.Type == ActivityType.movie.ToString())
            {
                if (!commentItem.IsCollected())
                {
                    listItem = new GUIListItem(Translation.AddToLibrary);
                    listItem.ItemId = (int)ActivityContextMenuItem.AddToCollection;
                    listItems.Add(listItem);
                }
                else
                {
                    listItem = new GUIListItem(Translation.RemoveFromLibrary);
                    listItem.ItemId = (int)ActivityContextMenuItem.RemoveFromCollection;
                    listItems.Add(listItem);
                }
            }

            // Add to Custom list
            listItem = new GUIListItem(Translation.AddToList);
            listItem.ItemId = (int)ActivityContextMenuItem.AddToList;
            listItems.Add(listItem);

            // Shouts
            listItem = new GUIListItem(Translation.Comments);
            listItem.ItemId = (int)ActivityContextMenuItem.Shouts;
            listItems.Add(listItem);

            // Rate
            listItem = new GUIListItem(Translation.Rate + "...");
            listItem.ItemId = (int)ActivityContextMenuItem.Rate;
            listItems.Add(listItem);

            // Trailers
            if (TraktHelper.IsTrailersAvailableAndEnabled)
            {
                listItem = new GUIListItem(Translation.Trailers);
                listItem.ItemId = (int)ActivityContextMenuItem.Trailers;
                listItems.Add(listItem);
            }

            return listItems;
        }

        private void PlayCommentItem(bool jumpTo)
        {
            var selectedItem = this.Facade.SelectedListItem;
            if (selectedItem == null) return;

            var selectedComment = selectedItem.TVTag as TraktCommentItem;
            if (selectedComment == null) return;

            switch (selectedComment.Type)
            {
                case "episode":
                    GUICommon.CheckAndPlayEpisode(selectedComment.Show, selectedComment.Episode);
                    break;

                case "show":
                case "season":                    
                    GUICommon.CheckAndPlayFirstUnwatchedEpisode(selectedComment.Show, jumpTo);
                    break;

                case "movie":
                    GUICommon.CheckAndPlayMovie(jumpTo, selectedComment.Movie);
                    break;
            }
        }

        private void LoadRecentComments()
        {
            GUIUtils.SetProperty("#Trakt.Items", string.Empty);

            GUIBackgroundTask.Instance.ExecuteInBackgroundAndCallback(() =>
            {
                return RecentComments;
            },
            delegate(bool success, object result)
            {
                if (success)
                {
                    var comments = result as IEnumerable<TraktCommentItem>;
                    SendRecentCommentsToFacade(comments);
                }
            }, Translation.GettingUserRecentShouts, true);
        }

        private void SendRecentCommentsToFacade(IEnumerable<TraktCommentItem> comments)
        {
            // clear facade
            GUIControl.ClearControl(GetID, Facade.GetID);

            // protected profiles might return null
            if (comments == null || comments.Count() == 0)
            {
                GUIUtils.ShowNotifyDialog(GUIUtils.PluginName(), Translation.UserHasNoRecentShouts);
                PreviousUser = CurrentUser;
                CurrentUser = TraktSettings.Username;
                GUIWindowManager.ShowPreviousWindow();
                return;
            }

            int itemId = 0;
            var commentImages = new List<GUITraktImage>();

            // Add each item added
            foreach (var comment in comments)
            {
                // bad api data - at least one must not be null
                if (comment.Movie == null && comment.Show == null && comment.List == null)
                    continue;

                var item = new GUICustomListItem(GetCommentItemTitle(comment), (int)TraktGUIWindows.RecentShouts);

                // add images for download
                var images = new GUITraktImage
                {
                    ShowImages = comment.Show != null ? comment.Show.Images : null,
                    MovieImages = comment.Movie != null ? comment.Movie.Images : null
                };
                commentImages.Add(images);

                // add user shout date as second label
                item.Label2 = comment.Comment.CreatedAt.ToPrettyDateTime();
                item.TVTag = comment;
                item.Episode = comment.Episode;
                item.Show = comment.Show;
                item.Movie = comment.Movie;
                item.Season = comment.Season;
                item.List = comment.List;
                item.Images = images;
                item.ItemId = Int32.MaxValue - itemId;
                item.IconImage = GUIImageHandler.GetDefaultPoster(false);
                item.IconImageBig = GUIImageHandler.GetDefaultPoster();
                item.ThumbnailImage = GUIImageHandler.GetDefaultPoster();
                item.PinImage = "traktActivityShout.png";
                item.OnItemSelected += OnCommentSelected;
                Utils.SetDefaultIcons(item);
                Facade.Add(item);
                itemId++;
            }

            // set Facade Layout
            Facade.SetCurrentLayout("List");
            GUIControl.FocusControl(GetID, Facade.GetID);

            if (PreviousSelectedIndex >= comments.Count())
                Facade.SelectIndex(PreviousSelectedIndex - 1);
            else
                Facade.SelectIndex(PreviousSelectedIndex);

            // set facade properties
            GUIUtils.SetProperty("#itemcount", comments.Count().ToString());
            GUIUtils.SetProperty("#Trakt.Items", string.Format("{0} {1}", comments.Count().ToString(), comments.Count() > 1 ? Translation.Comment : Translation.Comments));

            // Download images Async and set to facade
            GUICustomListItem.GetImages(commentImages);
        }

        private string GetCommentItemTitle(TraktCommentItem comment)
        {
            string title = string.Empty;

            switch (comment.Type)
            {
                case "movie":
                    title = comment.Movie.Title;
                    break;

                case "show":
                    title = comment.Show.Title;
                    break;
                
                case "season":
                    title = string.Format("{0} - {1} {2}", comment.Show.Title, Translation.Season, comment.Season.Number);
                    break;

                case "episode":
                    title = string.Format("{0} - {1}x{2} - {3}", comment.Show.Title, comment.Episode.Season, comment.Episode.Number, comment.Episode.Title ?? string.Format("{0} {1}", Translation.Episode, comment.Episode.Number));
                    break;

                case "list":
                    title = comment.List.Name;
                    break;
            }

            return title;
        }

        private string GetCommentText(TraktCommentItem item)
        {
            if (item.Comment.IsSpoiler && TraktSettings.HideSpoilersOnShouts) 
                return Translation.HiddenToPreventSpoilers;

            return item.Comment.Text;
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
            GUIUtils.SetProperty("#Trakt.Shout.Review", string.Empty);
            GUIUtils.SetProperty("#Trakt.Shout.Spoiler", string.Empty);
            
            GUICommon.ClearMovieProperties();
            GUICommon.ClearShowProperties();
            GUICommon.ClearSeasonProperties();
            GUICommon.ClearEpisodeProperties();
            GUICommon.ClearListProperties();
            GUICommon.ClearUserProperties();         
        }

        private void PublishCommentSkinProperties(TraktCommentItem item)
        {
            if (item == null || item.Comment == null)
                return;

            // set shout/review properties
            GUICommon.SetCommentProperties(item.Comment, item.IsWatched());
            
            // set user properties
            GUICommon.SetUserProperties(item.Comment.User);

            // set movie, show, season, episode or list properties
            // set show and episode properties for episode comments
            // set show and season for season shocommentsuts
            if (item.Movie != null)
            {
                GUICommon.SetMovieProperties(item.Movie);
            }
            else if (item.Show != null)
            {
                GUICommon.SetShowProperties(item.Show);
                if (item.Season != null)
                    GUICommon.SetSeasonProperties(item.Show, item.Season);
                if (item.Episode != null)
                    GUICommon.SetEpisodeProperties(item.Show, item.Episode);
            }
            else if (item.List != null)
            {
                GUICommon.SetListProperties(item.List, CurrentUser);
            }
        }

        private void OnCommentSelected(GUIListItem item, GUIControl parent)
        {
            var commentItem = item.TVTag as TraktCommentItem;
            if (commentItem == null) return;

            PublishCommentSkinProperties(commentItem);

            string fanartFileName = string.Empty;
            
            switch (commentItem.Type)
            {
                case "movie":
                    fanartFileName = commentItem.Movie.Images.Fanart.LocalImageFilename(ArtworkType.MovieFanart);
                    break;

                case "show":
                case "season":
                case "episode":
                    fanartFileName = commentItem.Show.Images.Fanart.LocalImageFilename(ArtworkType.ShowFanart);
                    break;

                case "list":
                    break;
            }

            if (!string.IsNullOrEmpty(fanartFileName))
            {
                GUIImageHandler.LoadFanart(backdrop, fanartFileName);
            }
        }

        #endregion
    }
}
