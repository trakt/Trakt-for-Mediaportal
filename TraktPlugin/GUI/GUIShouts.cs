using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using MediaPortal.GUI.Library;
using MediaPortal.Util;
using TraktPlugin.Extensions;
using TraktPlugin.TraktAPI.DataStructures;
using TraktPlugin.TraktAPI.Extensions;
using Action = MediaPortal.GUI.Library.Action;

namespace TraktPlugin.GUI
{
    public class GUIShouts : GUIWindow
    {
        #region Skin Controls

        [SkinControl(50)]
        protected GUIFacadeControl Facade = null;

        [SkinControl(2)]
        protected GUICheckButton hideSpoilersButton = null;

        [SkinControl(3)]
        protected GUIButtonControl nextEpisodeButton = null;

        [SkinControl(4)]
        protected GUIButtonControl prevEpisodeButton = null;

        #endregion

        #region Enums
         
        enum ContextMenuItem
        {
            Like,
            UnLike,
            Shout,
            Spoilers,
            NextEpisode,
            PrevEpisode,
            UserProfile
        }

        public enum ShoutTypeEnum
        {
            movie,
            show,
            episode,
            season
        }

        #endregion

        #region Constructor

        public GUIShouts() { }        

        #endregion

        #region Private Properties

        bool ExitIfNoShoutsFound { get; set; }
        int CurrentLevel { get; set; }
        Dictionary<int, int> SelectedParentItems = new Dictionary<int, int>();

        IEnumerable<TraktComment> Comments = null;
        Dictionary<int, IEnumerable<TraktComment>> CommentReplies = new Dictionary<int, IEnumerable<TraktComment>>();

        #endregion

        #region Public Properties

        public static ShoutTypeEnum ShoutType { get; set; }
        public static MovieShout MovieInfo { get; set; }
        public static ShowShout ShowInfo { get; set; }
        public static SeasonShout SeasonInfo { get; set; }
        public static EpisodeShout EpisodeInfo { get; set; }
        public static string Fanart { get; set; }
        public static string OnlineFanart { get; set; }
        public static bool IsWatched { get; set; }

        #endregion

        #region Base Overrides

        public override int GetID
        {
            get
            {
                return (int)TraktGUIWindows.Shouts;
            }
        }

        public override bool Init()
        {
            return Load(GUIGraphicsContext.Skin + @"\Trakt.Shouts.xml");
        }

        protected override void OnPageLoad()
        {
            base.OnPageLoad();

            // Clear GUI properties
            ClearProperties();

            // Requires Login
            if (!GUICommon.CheckLogin()) return;

            // Initialize
            InitProperties();
            
            // Enable/Disable GUI prev/next buttons
            EnableGUIButtons();

            // Load Shouts for Selected item
            LoadCommentsList();
        }

        protected override void OnPageDestroy(int new_windowId)
        {
            CurrentLevel = 0;
            Comments = null;
            CommentReplies.Clear();
            SelectedParentItems.Clear();

            GUIUserListItem.StopDownload = true;
            IsWatched = false;

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
                // Hide Spoilers Button
                case (2):
                    TraktSettings.HideSpoilersOnShouts = !TraktSettings.HideSpoilersOnShouts;
                    PublishCommentSkinProperties(Facade.SelectedListItem.TVTag as TraktComment);
                    break;

                // Next Episode
                case (3):
                    GetNextEpisodeComments();
                    break;

                // Previous Episode
                case (4):
                    GetPrevEpisodeComments();
                    break;

                // Comments
                case (50):
                    // re-act to comment e.g. view replies if any, like a comment etc.
                    var selectedComment = Facade.SelectedListItem.TVTag as TraktComment;
                    if (selectedComment != null)
                    {
                        if (selectedComment.Replies > 0)
                        {
                            // remember position for level
                            if (SelectedParentItems.ContainsKey(CurrentLevel))
                            {
                                SelectedParentItems[CurrentLevel] = selectedComment.Id;
                            }
                            else
                            {
                                SelectedParentItems.Add(CurrentLevel, selectedComment.Id);
                            }
                            CurrentLevel++;
                            LoadCommentReplies(selectedComment.Id);
                        }
                        else
                        {
                            // let user do something else
                            OnShowContextMenu();
                        }
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
                    // navigate back to parent if we are viewing replies
                    var selectedComment = Facade.SelectedListItem.TVTag as TraktComment;
                    if (selectedComment != null && selectedComment.ParentId > 0)
                    {
                        if (CurrentLevel > 1)
                        {
                            // load the previous list of replies                            
                            LoadCommentReplies((int)selectedComment.ParentId);
                        }
                        else
                        {
                            // return the main list of comments
                            LoadCommentsList();
                        }

                        // remove any previous selection for level returning from
                        if (SelectedParentItems.ContainsKey(CurrentLevel))
                            SelectedParentItems.Remove(CurrentLevel);

                        CurrentLevel--;
                        return;
                    }
                    break;
            }
            base.OnAction(action);
        }

        protected override void OnShowContextMenu()
        {
            if (GUIBackgroundTask.Instance.IsBusy) return;

            var selectedItem = this.Facade.SelectedListItem;
            if (selectedItem == null) return;

            var selectedComment = selectedItem.TVTag as TraktComment;
            if (selectedComment == null) return;

            var dlg = (IDialogbox)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
            if (dlg == null) return;

            dlg.Reset();
            dlg.SetHeading(GUIUtils.PluginName());

            GUIListItem listItem = null;

            // Like or Unlike Comment
            // There doesn't appear to be a way to get a users comment likes
            // so can't tell if user can like / unlike, give user the choice  
            // to do either
            if (selectedComment.User.Username != TraktSettings.Username)
            {
                // Like
                listItem = new GUIListItem(Translation.Like);
                dlg.Add(listItem);
                listItem.ItemId = (int)ContextMenuItem.Like;

                // UnLike
                listItem = new GUIListItem(Translation.UnLike);
                dlg.Add(listItem);
                listItem.ItemId = (int)ContextMenuItem.UnLike;
            }

            if (ShoutType == ShoutTypeEnum.episode && (selectedComment.ParentId == null || selectedComment.ParentId == 0))
            {
                listItem = new GUIListItem(Translation.NextEpisode);
                dlg.Add(listItem);
                listItem.ItemId = (int)ContextMenuItem.NextEpisode;

                if (EpisodeInfo.EpisodeIdx > 1)
                {
                    listItem = new GUIListItem(Translation.PreviousEpisode);
                    dlg.Add(listItem);
                    listItem.ItemId = (int)ContextMenuItem.PrevEpisode;
                }
            }

            listItem = new GUIListItem(TraktSettings.HideSpoilersOnShouts ? Translation.ShowSpoilers : Translation.HideSpoilers);
            dlg.Add(listItem);
            listItem.ItemId = (int)ContextMenuItem.Spoilers;

            // userprofile - only load for unprotected users
            if (!selectedComment.User.IsPrivate)
            {
                listItem = new GUIListItem(Translation.UserProfile);
                dlg.Add(listItem);
                listItem.ItemId = (int)ContextMenuItem.UserProfile;
            }
            // Show Context Menu
            dlg.DoModal(GUIWindowManager.ActiveWindow);
            if (dlg.SelectedId < 0) return;

            switch (dlg.SelectedId)
            {
                case (int)ContextMenuItem.Like:
                    LikeComment(selectedComment.Id);
                    selectedComment.Likes++;
                    PublishCommentSkinProperties(selectedComment);
                    break;

                case (int)ContextMenuItem.UnLike:
                    UnLikeComment(selectedComment.Id);
                    if (selectedComment.Likes > 0)
                    {
                        selectedComment.Likes--;
                        PublishCommentSkinProperties(selectedComment);
                    }
                    break;

                case ((int)ContextMenuItem.Spoilers):
                    TraktSettings.HideSpoilersOnShouts = !TraktSettings.HideSpoilersOnShouts;
                    if (hideSpoilersButton != null) hideSpoilersButton.Selected = TraktSettings.HideSpoilersOnShouts;
                    PublishCommentSkinProperties(selectedComment);
                    break;

                case ((int)ContextMenuItem.NextEpisode):
                    GetNextEpisodeComments();
                    break;

                case ((int)ContextMenuItem.PrevEpisode):
                    GetPrevEpisodeComments();
                    break;

                case ((int)ContextMenuItem.UserProfile):
                    GUIUserProfile.CurrentUser = selectedComment.User.Username;
                    GUIWindowManager.ActivateWindow((int)TraktGUIWindows.UserProfile);
                    break;

                default:
                    break;
            }

            base.OnShowContextMenu();
        }

        #endregion

        #region Private Methods

        private void LikeComment(int id)
        {
            var likeThread = new Thread((obj) =>
                {
                    TraktAPI.TraktAPI.LikeComment((int)obj);
                })
                {
                    Name = "LikeComment",
                    IsBackground = true
                };

            likeThread.Start(id);
        }

        private void UnLikeComment(int id)
        {
            var unlikeThread = new Thread((obj) =>
            {
                TraktAPI.TraktAPI.UnLikeComment((int)obj);
            })
            {
                Name = "LikeComment",
                IsBackground = true
            };

            unlikeThread.Start(id);
        }

        private void GetNextEpisodeComments()
        {
            if (ShoutType != ShoutTypeEnum.episode) return;

            Comments = null;

            var episodeIndex = EpisodeInfo.EpisodeIdx;
            var seasonIndex = EpisodeInfo.SeasonIdx;

            // increment by 1 episode
            EpisodeInfo.EpisodeIdx = episodeIndex + 1;

            // flag to indicate we dont want to exit if no shouts found
            ExitIfNoShoutsFound = false;

            LoadCommentsList();

            // set focus back to facade
            GUIControl.FocusControl(GetID, Facade.GetID);
        }

        private void GetPrevEpisodeComments()
        {
            if (ShoutType != ShoutTypeEnum.episode) return;

            var episodeIndex = EpisodeInfo.EpisodeIdx;
            var seasonIndex = EpisodeInfo.SeasonIdx;

            // there is no episode 0
            if (episodeIndex == 1) return;

            Comments = null;

            // decrement by 1 episode
            EpisodeInfo.EpisodeIdx = episodeIndex - 1;

            // flag to indicate we dont want to exit if no shouts found
            ExitIfNoShoutsFound = false;

            LoadCommentsList();

            // set focus back to facade
            GUIControl.FocusControl(GetID, Facade.GetID);
        }

        private void EnableGUIButtons()
        {
            if (nextEpisodeButton == null || prevEpisodeButton == null) return;

            // only enable episode buttons for episode shouts
            if (ShoutType != ShoutTypeEnum.episode)
            {
                GUIControl.DisableControl(GetID, nextEpisodeButton.GetID);
                GUIControl.DisableControl(GetID, prevEpisodeButton.GetID);
                return;
            }

            // we could get the max episode number and disable next button
            // on last episode, for now, lets not do the extra request and 
            // rely on notify to popup indicating no more shouts.
            GUIControl.EnableControl(GetID, nextEpisodeButton.GetID);
            GUIControl.EnableControl(GetID, prevEpisodeButton.GetID);

            // if episode one, then disable prev button
            if (EpisodeInfo.EpisodeIdx <= 1)
            {
                GUIControl.DisableControl(GetID, prevEpisodeButton.GetID);
            }
        }

        private void LoadCommentsList()
        {
            GUIUtils.SetProperty("#Trakt.Items", string.Empty);

            GUIBackgroundTask.Instance.ExecuteInBackgroundAndCallback(() =>
            {
                switch (ShoutType)
                {
                    case ShoutTypeEnum.movie:
                        if (MovieInfo == null) return null;
                        GUIUtils.SetProperty("#Trakt.Shout.CurrentItem", MovieInfo.Title);
                        return GetMovieComments();

                    case ShoutTypeEnum.show:
                        if (ShowInfo == null) return null;
                        GUIUtils.SetProperty("#Trakt.Shout.CurrentItem", ShowInfo.Title);
                        return GetShowComments();

                    case ShoutTypeEnum.season:
                        if (SeasonInfo == null) return null;
                        GUIUtils.SetProperty("#Trakt.Shout.CurrentItem", SeasonInfo.Title);
                        return GetSeasonComments();

                    case ShoutTypeEnum.episode:
                        if (EpisodeInfo == null) return null;
                        GUIUtils.SetProperty("#Trakt.Shout.CurrentItem", EpisodeInfo.ToString());
                        return GetEpisodeComments();

                    default:
                        return null;
                }
            },
            delegate(bool success, object result)
            {
                if (success)
                {
                    SendCommentsToFacade(result as IEnumerable<TraktComment>);
                }
            }, Translation.GettingShouts, true);
        }

        private void LoadCommentReplies(int id)
        {
            GUIBackgroundTask.Instance.ExecuteInBackgroundAndCallback(() =>
            {
                if (CommentReplies.ContainsKey(id))
                {
                    return CommentReplies[id];
                }
                
                var replies = TraktAPI.TraktAPI.GetCommentReplies(id.ToString());
                if (replies != null)
                {
                    CommentReplies.Add(id, replies);
                }
                return replies;
            },
            delegate(bool success, object result)
            {
                if (success)
                {
                    SendCommentsToFacade(result as IEnumerable<TraktComment>);
                }
            }, Translation.GettingShouts, true);
        }

        private IEnumerable<TraktComment> GetMovieComments()
        {
            if (Comments == null)
            {
                string title = string.Empty;
                if (MovieInfo.TraktId != null)
                    title = MovieInfo.TraktId.ToString();
                // TODO: Find trakt id from other integer IDs
                //else if (MovieInfo.TmdbId != null)
                //    title = MovieInfo.TmdbId.ToString();
                if (!string.IsNullOrEmpty(MovieInfo.ImdbId))
                    title = MovieInfo.ImdbId;
                else
                    title = string.Format("{0} {1}", MovieInfo.Title, MovieInfo.Year).ToSlug();

                Comments = TraktAPI.TraktAPI.GetMovieComments(title);
            }

            return Comments;
        }        

        private IEnumerable<TraktComment> GetShowComments()
        {
            if (Comments == null)
            {
                string title = string.Empty;

                if (ShowInfo.TraktId != null)
                {
                    title = ShowInfo.TraktId.ToString();
                }
                // TODO: Find trakt id from other integer IDs
                //else if (ShowInfo.TmdbId != null)
                //    title = ShowInfo.TmdbId.ToString();
                //else if (ShowInfo.TvdbId != null)
                //    title = ShowInfo.TvdbId.ToString();
                else if (!string.IsNullOrEmpty(ShowInfo.ImdbId))
                {
                    title = ShowInfo.ImdbId;
                }
                else
                {
                    title = ShowInfo.Title.StripYear(ShowInfo.Year).ToSlug();
                }

                Comments = TraktAPI.TraktAPI.GetShowComments(title);
            }

            return Comments;
        }

        private IEnumerable<TraktComment> GetSeasonComments()
        {
            if (Comments == null)
            {
                string title = string.Empty;

                if (SeasonInfo.TraktId != null)
                {
                    title = SeasonInfo.TraktId.ToString();
                }
                else if (!string.IsNullOrEmpty(SeasonInfo.ImdbId))
                {
                    title = SeasonInfo.ImdbId;
                }
                else
                {
                    title = SeasonInfo.Title.StripYear(SeasonInfo.Year).ToSlug();
                }

                Comments = TraktAPI.TraktAPI.GetSeasonComments(title, SeasonInfo.SeasonIdx);
            }

            return Comments;
        }

        private IEnumerable<TraktComment> GetEpisodeComments()
        {
            if (Comments == null)
            {
                string title = string.Empty;

                if (EpisodeInfo.TraktId != null)
                {
                    title = EpisodeInfo.TraktId.ToString();
                }
                // TODO: Find trakt id from other integer IDs
                //else if (EpisodeInfo.TmdbId != null)
                //    title = EpisodeInfo.TmdbId.ToString();
                //else if (EpisodeInfo.TvdbId != null)
                //    title = EpisodeInfo.TvdbId.ToString();
                else if (!string.IsNullOrEmpty(EpisodeInfo.ImdbId))
                {
                    title = EpisodeInfo.ImdbId;
                }
                else
                {
                    title = EpisodeInfo.Title.StripYear(EpisodeInfo.Year).ToSlug();
                }

                Comments = TraktAPI.TraktAPI.GetEpisodeComments(title, EpisodeInfo.SeasonIdx, EpisodeInfo.EpisodeIdx);
            }

            return Comments;
        }

        private void SendCommentsToFacade(IEnumerable<TraktComment> comments)
        {
            // clear facade
            GUIControl.ClearControl(GetID, Facade.GetID);

            if (comments == null)
            {
                GUIUtils.ShowNotifyDialog(Translation.Error, Translation.ErrorGeneral);
                GUIWindowManager.ShowPreviousWindow();
                return;
            }

            // this should not happen for replies as we only enter if more than one
            if (comments.Count() == 0)
            {
                string title = string.Empty;
                switch (ShoutType)
                {
                    case ShoutTypeEnum.movie:
                        title = MovieInfo.Title;
                        break;
                    case ShoutTypeEnum.show:
                        title = ShowInfo.Title;
                        break;
                    case ShoutTypeEnum.season:
                        title = string.Format("{0} - {1} {2}", SeasonInfo.Title, Translation.Season, SeasonInfo.SeasonIdx);
                        break;
                    case ShoutTypeEnum.episode:
                        title = EpisodeInfo.ToString();
                        break;
                }
                ClearProperties();
                GUIUtils.ShowNotifyDialog(GUIUtils.PluginName(), string.Format(Translation.NoShoutsForItem, title));
               
                if (ExitIfNoShoutsFound)
                {
                    GUIWindowManager.ShowPreviousWindow();
                    return;
                }
            }

            // filter out the duplicates!
            var distinctComments = comments.Where(s => s.Text != null && s.User != null).Distinct(new ShoutComparer());

            GUIUtils.SetProperty("#itemcount", distinctComments.Count().ToString());
            GUIUtils.SetProperty("#Trakt.Items", string.Format("{0} {1}", distinctComments.Count(), distinctComments.Count() > 1 ? Translation.Comments : Translation.Shout));

            int selectedParentItem = 0;
            int selectedItemIndex = 0;
            SelectedParentItems.TryGetValue(CurrentLevel, out selectedParentItem);

            int id = 0;
            var userImages = new List<GUITraktImage>();

            // Add each user that shouted to the list
            foreach (var comment in distinctComments)
            {
                // add image to download
                var images = new GUITraktImage { UserImages = comment.User.Images };
                userImages.Add(images);

                var shoutItem = new GUIUserListItem(comment.User.Username, (int)TraktGUIWindows.Shouts);

                shoutItem.Label2 = comment.CreatedAt.FromISO8601().ToShortDateString();
                shoutItem.Images = images;
                shoutItem.TVTag = comment;
                shoutItem.User = comment.User;
                shoutItem.ItemId = id++;
                shoutItem.IconImage = "defaultTraktUser.png";
                shoutItem.IconImageBig = "defaultTraktUserBig.png";
                shoutItem.ThumbnailImage = "defaultTraktUserBig.png";
                shoutItem.OnItemSelected += OnCommentSelected;
                Utils.SetDefaultIcons(shoutItem);
                Facade.Add(shoutItem);

                // check if we should select this comment when returning from replies
                if (selectedParentItem == (int)comment.Id)
                    selectedItemIndex = id - 1;
            }

            // Enable / Disable GUI Controls
            EnableGUIButtons();

            // Set Facade Layout
            if (Facade.Count > 0)
            {
                Facade.SetCurrentLayout("List");
                GUIControl.FocusControl(GetID, Facade.GetID);

                Facade.SelectedListItemIndex = selectedItemIndex;
            }
            else
            {
                GUIControl.FocusControl(GetID, nextEpisodeButton.GetID);
            }

            // Download avatars Async and set to facade            
            GUIUserListItem.GetImages(userImages);
        }

        private void InitProperties()
        {
            ExitIfNoShoutsFound = true;

            // only set property if file exists
            // if we set now and download later, image will not set to skin
            if (File.Exists(Fanart))
                GUIUtils.SetProperty("#Trakt.Shout.Fanart", Fanart);
            else
                DownloadFanart(Fanart, OnlineFanart);

            if (hideSpoilersButton != null)
            {
                hideSpoilersButton.Label = Translation.HideSpoilers;
                hideSpoilersButton.Selected = TraktSettings.HideSpoilersOnShouts;
            }
        }

        private void ClearProperties()
        {
            GUIUtils.SetProperty("#Trakt.Shouts.CurrentItem", string.Empty);

            GUICommon.ClearShoutProperties();
            GUICommon.ClearUserProperties();
        }

        private void DownloadFanart(string localFile, string remoteFile)
        {
            if (localFile == null || remoteFile == null)
                return;

            var getFanartthread = new Thread((o) =>
                {
                    GUIImageHandler.DownloadImage(remoteFile, localFile);
                    GUIUtils.SetProperty("#Trakt.Shout.Fanart", localFile);

                }) { Name = "ImageDownload", IsBackground = true };

            getFanartthread.Start();
        }

        private void PublishCommentSkinProperties(TraktComment comment)
        {
            if (comment == null) return;

            GUICommon.SetUserProperties(comment.User);
            GUICommon.SetCommentProperties(comment, IsWatched);
        }

        private void OnCommentSelected(GUIListItem item, GUIControl parent)
        {
            PublishCommentSkinProperties(item.TVTag as TraktComment);
        }

        #endregion
    }

    public class MovieShout
    {
        public string Title { get; set; }
        public int? Year { get; set; }
        public string ImdbId { get; set; }
        public int? TmdbId { get; set; }
        public int? TraktId { get; set; }
    }

    public class ShowShout
    {
        public string Title { get; set; }
        public int? Year { get; set; }
        public string ImdbId { get; set; }
        public int? TvdbId { get; set; }
        public int? TmdbId { get; set; }
        public int? TraktId { get; set; }
    }

    public class SeasonShout : ShowShout
    {
        public int SeasonIdx { get; set; }
    }

    public class EpisodeShout : SeasonShout
    {
        public int EpisodeIdx { get; set; }

        public override string ToString()
        {
            return string.Format("{0} - {1}x{2}", Title, SeasonIdx, EpisodeIdx);
        }
    }

    public class ShoutComparer : IEqualityComparer<TraktComment>
    {
        #region IEqualityComparer

        public bool Equals(TraktComment x, TraktComment y)
        {
            return x.User.Username == y.User.Username && x.Text.Trim() == y.Text.Trim();
        }

        public int GetHashCode(TraktComment obj)
        {
            return (obj.User.Username + obj.Text.Trim()).GetHashCode();
        }

        #endregion
    }
}