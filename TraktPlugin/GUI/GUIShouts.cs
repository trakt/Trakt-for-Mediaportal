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
            episode
        }

        #endregion

        #region Constructor

        public GUIShouts() { }        

        #endregion

        #region Private Properties

        bool ExitIfNoShoutsFound { get; set; }

        #endregion

        #region Public Properties

        public static ShoutTypeEnum ShoutType { get; set; }
        public static MovieShout MovieInfo { get; set; }
        public static ShowShout ShowInfo { get; set; }
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
            LoadShoutsList();
        }

        protected override void OnPageDestroy(int new_windowId)
        {
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
                    PublishShoutSkinProperties(Facade.SelectedListItem.TVTag as TraktComment);
                    break;

                // Next Episode
                case (3):
                    GetNextEpisodeShouts();
                    break;

                // Previous Episode
                case (4):
                    GetPrevEpisodeShouts();
                    break;

                default:
                    break;
            }
            base.OnClicked(controlId, control, actionType);
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

            if (ShoutType == ShoutTypeEnum.episode)
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
                    PublishShoutSkinProperties(selectedComment);
                    break;

                case (int)ContextMenuItem.UnLike:
                    UnLikeComment(selectedComment.Id);
                    if (selectedComment.Likes > 0)
                    {
                        selectedComment.Likes--;
                        PublishShoutSkinProperties(selectedComment);
                    }
                    break;

                case ((int)ContextMenuItem.Spoilers):
                    TraktSettings.HideSpoilersOnShouts = !TraktSettings.HideSpoilersOnShouts;
                    if (hideSpoilersButton != null) hideSpoilersButton.Selected = TraktSettings.HideSpoilersOnShouts;
                    PublishShoutSkinProperties(selectedComment);
                    break;

                case ((int)ContextMenuItem.NextEpisode):
                    GetNextEpisodeShouts();
                    break;

                case ((int)ContextMenuItem.PrevEpisode):
                    GetPrevEpisodeShouts();
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

        private void GetNextEpisodeShouts()
        {
            if (ShoutType != ShoutTypeEnum.episode) return;

            var episodeIndex = EpisodeInfo.EpisodeIdx;
            var seasonIndex = EpisodeInfo.SeasonIdx;

            // increment by 1 episode
            EpisodeInfo.EpisodeIdx = episodeIndex + 1;

            // flag to indicate we dont want to exit if no shouts found
            ExitIfNoShoutsFound = false;

            LoadShoutsList();

            // set focus back to facade
            GUIControl.FocusControl(GetID, Facade.GetID);
        }

        private void GetPrevEpisodeShouts()
        {
            if (ShoutType != ShoutTypeEnum.episode) return;

            var episodeIndex = EpisodeInfo.EpisodeIdx;
            var seasonIndex = EpisodeInfo.SeasonIdx;

            // there is no episode 0
            if (episodeIndex == 1) return;

            // decrement by 1 episode
            EpisodeInfo.EpisodeIdx = episodeIndex - 1;

            // flag to indicate we dont want to exit if no shouts found
            ExitIfNoShoutsFound = false;

            LoadShoutsList();

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

        private void LoadShoutsList()
        {
            GUIUtils.SetProperty("#Trakt.Items", string.Empty);

            GUIBackgroundTask.Instance.ExecuteInBackgroundAndCallback(() =>
            {
                switch (ShoutType)
                {
                    case ShoutTypeEnum.movie:
                        if (MovieInfo == null) return null;
                        GUIUtils.SetProperty("#Trakt.Shout.CurrentItem", MovieInfo.Title);
                        return GetMovieShouts();

                    case ShoutTypeEnum.show:
                        if (ShowInfo == null) return null;
                        GUIUtils.SetProperty("#Trakt.Shout.CurrentItem", ShowInfo.Title);
                        return GetShowShouts();

                    case ShoutTypeEnum.episode:
                        if (EpisodeInfo == null) return null;
                        GUIUtils.SetProperty("#Trakt.Shout.CurrentItem", EpisodeInfo.ToString());
                        return GetEpisodeShouts();

                    default:
                        return null;
                }
            },
            delegate(bool success, object result)
            {
                if (success)
                {
                    SendShoutsToFacade(result as IEnumerable<TraktComment>);
                }
            }, Translation.GettingShouts, true);
        }

        private IEnumerable<TraktComment> GetMovieShouts()
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

            return TraktAPI.TraktAPI.GetMovieComments(title);
        }

        private IEnumerable<TraktComment> GetShowShouts()
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

            return TraktAPI.TraktAPI.GetShowComments(title);
        }

        private IEnumerable<TraktComment> GetEpisodeShouts()
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

            return TraktAPI.TraktAPI.GetEpisodeComments(title, EpisodeInfo.SeasonIdx, EpisodeInfo.EpisodeIdx);
        }

        private void SendShoutsToFacade(IEnumerable<TraktComment> shouts)
        {
            // clear facade
            GUIControl.ClearControl(GetID, Facade.GetID);

            if (shouts == null)
            {
                GUIUtils.ShowNotifyDialog(Translation.Error, Translation.ErrorGeneral);
                GUIWindowManager.ShowPreviousWindow();
                return;
            }

            if (shouts.Count() == 0)
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
            var distinctShouts = shouts.Where(s => s.Comment != null && s.User != null).Distinct(new ShoutComparer());

            GUIUtils.SetProperty("#itemcount", distinctShouts.Count().ToString());
            GUIUtils.SetProperty("#Trakt.Items", string.Format("{0} {1}", distinctShouts.Count(), distinctShouts.Count() > 1 ? Translation.Comments : Translation.Shout));            

            int id = 0;
            var userImages = new List<GUITraktImage>();

            // Add each user that shouted to the list
            foreach (var shout in distinctShouts)
            {
                // add image to download
                var images = new GUITraktImage { UserImages = shout.User.Images };
                userImages.Add(images);

                var shoutItem = new GUIUserListItem(shout.User.Username, (int)TraktGUIWindows.Shouts);

                shoutItem.Label2 = shout.CreatedAt.FromISO8601().ToShortDateString();
                shoutItem.Images = images;
                shoutItem.TVTag = shout;
                shoutItem.User = shout.User;
                shoutItem.ItemId = id++;
                shoutItem.IconImage = "defaultTraktUser.png";
                shoutItem.IconImageBig = "defaultTraktUserBig.png";
                shoutItem.ThumbnailImage = "defaultTraktUserBig.png";
                shoutItem.OnItemSelected += OnShoutSelected;
                Utils.SetDefaultIcons(shoutItem);
                Facade.Add(shoutItem);
            }

            // Enable / Disable GUI Controls
            EnableGUIButtons();

            // Set Facade Layout
            if (Facade.Count > 0)
            {
                Facade.SetCurrentLayout("List");
                GUIControl.FocusControl(GetID, Facade.GetID);

                Facade.SelectedListItemIndex = 0;
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
            var getFanartthread = new Thread((o) =>
                {
                    GUIImageHandler.DownloadImage(remoteFile, localFile);
                    GUIUtils.SetProperty("#Trakt.Shout.Fanart", localFile);

                }) { Name = "ImageDownload", IsBackground = true };

            getFanartthread.Start();
        }

        private void PublishShoutSkinProperties(TraktComment shout)
        {
            if (shout == null) return;

            GUICommon.SetUserProperties(shout.User);
            GUICommon.SetShoutProperties(shout, IsWatched);
        }

        private void OnShoutSelected(GUIListItem item, GUIControl parent)
        {
            PublishShoutSkinProperties(item.TVTag as TraktComment);
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

    public class EpisodeShout
    {
        public string Title { get; set; }
        public int? Year { get; set; }
        public string ImdbId { get; set; }
        public int? TvdbId { get; set; }
        public int? TmdbId { get; set; }
        public int? TraktId { get; set; }
        public int SeasonIdx { get; set; }
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
            return x.User.Username == y.User.Username && x.Comment.Trim() == y.Comment.Trim();
        }

        public int GetHashCode(TraktComment obj)
        {
            return (obj.User.Username + obj.Comment.Trim()).GetHashCode();
        }

        #endregion
    }
}