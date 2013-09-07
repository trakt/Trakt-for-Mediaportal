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
using TraktPlugin.TraktAPI;
using TraktPlugin.TraktAPI.DataStructures;

namespace TraktPlugin.GUI
{
    public class GUISearchUsers : GUIWindow
    {
        #region Skin Controls

        [SkinControl(2)]
        protected GUIButtonControl layoutButton = null;

        [SkinControl(50)]
        protected GUIFacadeControl Facade = null;

        #endregion

        #region Enums

        enum ContextMenuItem
        {
            ChangeLayout
        }

        #endregion

        #region Constructor

        public GUISearchUsers()
        {

        }

        #endregion

        #region Public Variables

        public static string SearchTerm { get; set; }
        public static IEnumerable<TraktUser> Users { get; set; }

        #endregion

        #region Private Variables

        bool StopDownload { get; set; }
        bool SearchTermChanged { get; set; }
        string PreviousSearchTerm { get; set; }
        Layout CurrentLayout { get; set; }
        int PreviousSelectedIndex = 0;

        #endregion

        #region Base Overrides

        public override int GetID
        {
            get
            {
                return (int)TraktGUIWindows.SearchUsers;
            }
        }

        public override bool Init()
        {
            return Load(GUIGraphicsContext.Skin + @"\Trakt.Search.Users.xml");
        }

        protected override void OnPageLoad()
        {
            base.OnPageLoad();

            if (string.IsNullOrEmpty(_loadParameter) && Users == null)
            {
                GUIWindowManager.ActivateWindow(GUIWindowManager.GetPreviousActiveWindow());
                return;
            }

            // Clear GUI Properties
            ClearProperties();

            // Init Properties
            InitProperties();

            // Load Search Results
            LoadSearchResults();
        }

        protected override void OnPageDestroy(int new_windowId)
        {
            StopDownload = true;
            ClearProperties();

            _loadParameter = null;

            // save settings
            TraktSettings.SearchUsersDefaultLayout = (int)CurrentLayout;

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
                    break;

                // Layout Button
                case (2):
                    CurrentLayout = GUICommon.ShowLayoutMenu(CurrentLayout, PreviousSelectedIndex);
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
                    base.OnAction(action);
                    break;

                case Action.ActionType.ACTION_PREVIOUS_MENU:
                    // clear search criteria if going back
                    SearchTerm = string.Empty;
                    Users = null;
                    base.OnAction(action);
                    break;

                default:
                    base.OnAction(action);
                    break;
            }
        }

        protected override void OnShowContextMenu()
        {
            if (GUIBackgroundTask.Instance.IsBusy) return;

            GUIListItem selectedItem = this.Facade.SelectedListItem;
            if (selectedItem == null) return;

            var selectedUser = selectedItem.TVTag as TraktUser;
            if (selectedUser == null) return;

            IDialogbox dlg = (IDialogbox)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
            if (dlg == null) return;

            dlg.Reset();
            dlg.SetHeading(GUIUtils.PluginName());

            GUIListItem listItem = null;

            // Change Layout
            listItem = new GUIListItem(Translation.ChangeLayout);
            dlg.Add(listItem);
            listItem.ItemId = (int)ContextMenuItem.ChangeLayout;

            // Show Context Menu
            dlg.DoModal(GUIWindowManager.ActiveWindow);
            if (dlg.SelectedId < 0) return;

            switch (dlg.SelectedId)
            {
                case ((int)ContextMenuItem.ChangeLayout):
                    CurrentLayout = GUICommon.ShowLayoutMenu(CurrentLayout, PreviousSelectedIndex);
                    break;
            }

            base.OnShowContextMenu();
        }

        #endregion

        #region Private Methods

        private void LoadSearchResults()
        {
            GUIUtils.SetProperty("#Trakt.Items", string.Empty);

            GUIBackgroundTask.Instance.ExecuteInBackgroundAndCallback(() =>
            {
                // People can be null if invoking search from loading parameters
                // Internally we set the People to load
                if (Users == null && !string.IsNullOrEmpty(SearchTerm))
                {
                    // search online
                    Users = TraktAPI.TraktAPI.SearchForUsers(SearchTerm);
                }
                return Users;
            },
            delegate(bool success, object result)
            {
                if (success)
                {
                    var users = result as IEnumerable<TraktUser>;
                    SendSearchResultsToFacade(users);
                }
            }, Translation.GettingSearchResults, true);
        }

        private void SendSearchResultsToFacade(IEnumerable<TraktUser> users)
        {
            // clear facade
            GUIControl.ClearControl(GetID, Facade.GetID);

            if (users == null || users.Count() == 0)
            {
                GUIUtils.ShowNotifyDialog(GUIUtils.PluginName(), Translation.NoSearchResultsFound);
                GUIWindowManager.ShowPreviousWindow();
                return;
            }

            int itemId = 0;

            // Add each user
            foreach (var user in users)
            {
                var item = new GUITraktSearchUserListItem(user.Username);

                item.Item = user;
                item.TVTag = user;
                item.ItemId = Int32.MaxValue - itemId;
                item.IconImage = "defaultTraktUser.png";
                item.IconImageBig = "defaultTraktUserBig.png";
                item.ThumbnailImage = "defaultTraktUserBig.png";
                item.OnItemSelected += OnUserSelected;
                Utils.SetDefaultIcons(item);
                Facade.Add(item);
                itemId++;
            }

            // Set Facade Layout
            Facade.SetCurrentLayout(Enum.GetName(typeof(Layout), CurrentLayout));
            GUIControl.FocusControl(GetID, Facade.GetID);

            if (SearchTermChanged) PreviousSelectedIndex = 0;
            Facade.SelectIndex(PreviousSelectedIndex);

            // set facade properties
            GUIUtils.SetProperty("#itemcount", users.Count().ToString());
            GUIUtils.SetProperty("#Trakt.Items", string.Format("{0} {1}", users.Count().ToString(), users.Count() > 1 ? Translation.Users : Translation.User));

            // Download images Async and set to facade
            List<TraktUser> images = new List<TraktUser>(users.ToList());
            GetImages<TraktUser>(images);
        }

        private void InitProperties()
        {
            // set search term from loading parameter
            if (!string.IsNullOrEmpty(_loadParameter))
            {
                TraktLogger.Info("User Search Loading Parameter: {0}", _loadParameter);
                SearchTerm = _loadParameter;
            }

            // remember previous search term
            SearchTermChanged = false;
            if (PreviousSearchTerm != SearchTerm) SearchTermChanged = true;
            PreviousSearchTerm = SearchTerm;

            // set context property
            GUIUtils.SetProperty("#Trakt.Search.SearchTerm", SearchTerm);
            
            // load last layout
            CurrentLayout = (Layout)TraktSettings.SearchUsersDefaultLayout;

            // update button label
            if (layoutButton != null)
                GUIControl.SetControlLabel(GetID, layoutButton.GetID, GUICommon.GetLayoutTranslation(CurrentLayout));
        }

        private void ClearProperties()
        {
            GUIUtils.SetProperty("#Trakt.Search.SearchTerm", string.Empty);
            GUICommon.ClearUserProperties();
        }

        private void PublishSkinProperties(TraktUser user)
        {
            GUICommon.SetUserProperties(user);
        }

        private void OnUserSelected(GUIListItem item, GUIControl parent)
        {
            PreviousSelectedIndex = Facade.SelectedListItemIndex;

            var user = item.TVTag as TraktUser;
            PublishSkinProperties(user);
        }

        private void GetImages<T>(List<T> itemsWithThumbs)
        {
            StopDownload = false;

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
                        #region Facade Items
                        // stop download if we have exited window
                        if (StopDownload) break;

                        string remoteThumb = string.Empty;
                        string localThumb = string.Empty;

                        if (item is TraktUser)
                        {
                            remoteThumb = (item as TraktUser).Avatar;
                            localThumb = (item as TraktUser).AvatarFilename;
                        }

                        if (!string.IsNullOrEmpty(remoteThumb) && !string.IsNullOrEmpty(localThumb))
                        {
                            if (GUIImageHandler.DownloadImage(remoteThumb, localThumb))
                            {
                                // notify that image has been downloaded
                                if (item is TraktUser)
                                {
                                    (item as TraktUser).NotifyPropertyChanged("AvatarFilename");
                                }
                            }
                        }
                        #endregion
                    }
                })
                {
                    IsBackground = true,
                    Name = "ImageDownloader" + i.ToString()
                }.Start(groupList);
            }
        }

        #endregion
    }

    public class GUITraktSearchUserListItem : GUIListItem
    {
        public GUITraktSearchUserListItem(string strLabel) : base(strLabel) { }

        public object Item
        {
            get { return _Item; }
            set
            {
                _Item = value;
                INotifyPropertyChanged notifier = value as INotifyPropertyChanged;
                if (notifier != null) notifier.PropertyChanged += (s, e) =>
                {
                    if (s is TraktUser && e.PropertyName == "AvatarFilename")
                        SetImageToGui((s as TraktUser).AvatarFilename);
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

            // if selected and is current window force an update of thumbnail
            this.UpdateItemIfSelected((int)TraktGUIWindows.SearchUsers, ItemId);
        }
    }
}