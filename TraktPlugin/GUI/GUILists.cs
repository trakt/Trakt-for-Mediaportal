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
    public class GUILists : GUIWindow
    {
        #region Skin Controls

        [SkinControl(50)]
        protected GUIFacadeControl Facade = null;

        #endregion

        #region Enums

        enum ContextMenuItem
        {
            Add,
            Delete,
            Update,
            Copy
        }

        #endregion

        #region Constructor

        public GUILists() { }

        #endregion

        #region Private Variables

        bool StopDownload { get; set; }
        static int PreviousSelectedIndex { get; set; }

        #endregion

        #region Public Properties

        public static string CurrentUser { get; set; }

        #endregion

        #region Base Overrides

        public override int GetID
        {
            get
            {
                return 87275;
            }
        }

        public override bool Init()
        {
            return Load(GUIGraphicsContext.Skin + @"\Trakt.Lists.xml");
        }

        protected override void OnPageLoad()
        {
            // Requires Login
            if (!GUICommon.CheckLogin()) return;

            // Clear GUI Properties
            ClearProperties();

            // Init Properties
            InitProperties();

            // Load Lists for user
            LoadLists();
        }

        protected override void OnPageDestroy(int new_windowId)
        {
            StopDownload = true;
            PreviousSelectedIndex = Facade.SelectedListItemIndex;
            ClearProperties();

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
                        // Load current selected list
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
                    // restore current user
                    CurrentUser = TraktSettings.Username;
                    base.OnAction(action);
                    break;
                default:
                    base.OnAction(action);
                    break;
            }
        }

        protected override void OnShowContextMenu()
        {
            GUIListItem selectedItem = this.Facade.SelectedListItem;
            if (selectedItem == null) return;

            TraktUserList selectedList = (TraktUserList)selectedItem.TVTag;

            IDialogbox dlg = (IDialogbox)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
            if (dlg == null) return;

            dlg.Reset();
            dlg.SetHeading(GUIUtils.PluginName());

            GUIListItem listItem = null;

            // only allow add/delete/update if viewing your own lists
            if (CurrentUser == TraktSettings.Username)
            {
                //listItem = new GUIListItem(Translation.AddList);
                //dlg.Add(listItem);
                //listItem.ItemId = (int)ContextMenuItem.Add;

                //listItem = new GUIListItem(Translation.DeleteList);
                //dlg.Add(listItem);
                //listItem.ItemId = (int)ContextMenuItem.Delete;

                //listItem = new GUIListItem(Translation.UpdateList);
                //dlg.Add(listItem);
                //listItem.ItemId = (int)ContextMenuItem.Update;
            }
            else
            {
                //listItem = new GUIListItem(Translation.CopyList);
                //dlg.Add(listItem);
                //listItem.ItemId = (int)ContextMenuItem.Copy;
            }

            // Show Context Menu
            dlg.DoModal(GUIWindowManager.ActiveWindow);
            if (dlg.SelectedId < 0) return;

            switch (dlg.SelectedId)
            {
                case ((int)ContextMenuItem.Add):
                    break;

                case ((int)ContextMenuItem.Delete):
                    break;

                case ((int)ContextMenuItem.Update):
                    break;

                case ((int)ContextMenuItem.Copy):
                    break;

                default:
                    break;
            }

            base.OnShowContextMenu();
        }

        #endregion

        #region Private Methods

        private void LoadLists()
        {
            GUIUtils.SetProperty("#Trakt.Items", string.Empty);

            GUIBackgroundTask.Instance.ExecuteInBackgroundAndCallback(() =>
            {
                return TraktLists.GetListsForUser(CurrentUser);
            },
            delegate(bool success, object result)
            {
                if (success)
                {
                    IEnumerable<TraktUserList> lists = result as IEnumerable<TraktUserList>;
                    SendListsToFacade(lists);
                }
            }, Translation.GettingLists, true);
        }

        private void SendListsToFacade(IEnumerable<TraktUserList> lists)
        {
            // clear facade
            GUIControl.ClearControl(GetID, Facade.GetID);

            if (lists.Count() == 0 && TraktSettings.Username != CurrentUser)
            {
                GUIUtils.ShowNotifyDialog(GUIUtils.PluginName(), string.Format(Translation.NoUserLists, CurrentUser));
                CurrentUser = TraktSettings.Username;
                GUIWindowManager.ShowPreviousWindow();
                return;
            }

            if (lists.Count() == 0)
            {
                if (!GUIUtils.ShowYesNoDialog(Translation.Lists, Translation.NoListsFound, true))
                {
                    GUIWindowManager.ShowPreviousWindow();
                    return;
                }
                // TODO: Create a dialog to create a new list or edit an existing one
                GUIUtils.ShowOKDialog(Translation.Lists, "Oops, not yet implemented!");
                GUIWindowManager.ShowPreviousWindow();
                return;
            }

            int itemId = 0;

            // Add each list
            foreach (var list in lists)
            {
                GUIListItem item = new GUIListItem(list.Name);

                item.Label2 = list.Privacy;
                item.TVTag = list;
                item.ItemId = Int32.MaxValue - itemId;
                item.IconImage = "defaultFolder.png";
                item.IconImageBig = "defaultFolderBig.png";
                item.ThumbnailImage = "defaultFolderBig.png";
                item.OnItemSelected += OnItemSelected;
                Utils.SetDefaultIcons(item);
                Facade.Add(item);
                itemId++;
            }

            // Set Facade Layout
            Facade.SetCurrentLayout("List");
            GUIControl.FocusControl(GetID, Facade.GetID);

            if (PreviousSelectedIndex >= lists.Count())
                Facade.SelectIndex(PreviousSelectedIndex - 1);
            else
                Facade.SelectIndex(PreviousSelectedIndex);

            // set facade properties
            GUIUtils.SetProperty("#itemcount", lists.Count().ToString());
            GUIUtils.SetProperty("#Trakt.Items", string.Format("{0} {1}", lists.Count().ToString(), lists.Count() > 1 ? Translation.Lists : Translation.List));
        }

        private void SetProperty(string property, string value)
        {
            string propertyValue = string.IsNullOrEmpty(value) ? "N/A" : value;
            GUIUtils.SetProperty(property, propertyValue);
        }

        private void InitProperties()
        {
            // set current user to logged in user if not set
            if (string.IsNullOrEmpty(CurrentUser)) CurrentUser = TraktSettings.Username;
            SetProperty("#Trakt.Lists.CurrentUser", CurrentUser);
        }

        private void ClearProperties()
        {
            SetProperty("#Trakt.List.Name", string.Empty);
            SetProperty("#Trakt.List.Description", string.Empty);
            SetProperty("#Trakt.List.Privacy", string.Empty);
            SetProperty("#Trakt.List.Slug", string.Empty);
            SetProperty("#Trakt.List.Url", string.Empty);
        }

        private void PublishListSkinProperties(TraktUserList list)
        {
            SetProperty("#Trakt.List.Name", list.Name);
            SetProperty("#Trakt.List.Description", list.Description);
            SetProperty("#Trakt.List.Privacy", list.Privacy);
            SetProperty("#Trakt.List.Slug", list.Slug);
            SetProperty("#Trakt.List.Url", list.Url);
        }

        private void OnItemSelected(GUIListItem item, GUIControl parent)
        {
            TraktUserList list = item.TVTag as TraktUserList;
            PublishListSkinProperties(list);
        }

        #endregion
    }
}