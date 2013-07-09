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
            Create,
            Delete,
            Edit,
            Copy
        }

        #endregion

        #region Constructor

        public GUILists() { }

        #endregion

        #region Private Variables

        bool StopDownload { get; set; }
        static int PreviousSelectedIndex { get; set; }
        IEnumerable<TraktUserList> Lists { get; set; }

        #endregion

        #region Public Properties

        public static string CurrentUser { get; set; }

        #endregion

        #region Base Overrides

        public override int GetID
        {
            get
            {
                return (int)TraktGUIWindows.Lists;
            }
        }

        public override bool Init()
        {
            return Load(GUIGraphicsContext.Skin + @"\Trakt.Lists.xml");
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
                        GUIListItem selectedItem = this.Facade.SelectedListItem;
                        if (selectedItem == null) return;                                                

                        // Load current selected list
                        GUIListItems.CurrentList = (TraktUserList)selectedItem.TVTag;
                        GUIListItems.CurrentUser = CurrentUser;
                        GUIWindowManager.ActivateWindow((int)TraktGUIWindows.ListItems);
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
            if (GUIBackgroundTask.Instance.IsBusy) return;

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
                listItem = new GUIListItem(Translation.CreateList);
                dlg.Add(listItem);
                listItem.ItemId = (int)ContextMenuItem.Create;

                listItem = new GUIListItem(Translation.EditList);
                dlg.Add(listItem);
                listItem.ItemId = (int)ContextMenuItem.Edit;

                listItem = new GUIListItem(Translation.DeleteList);
                dlg.Add(listItem);
                listItem.ItemId = (int)ContextMenuItem.Delete;
            }
            else
            {
                // copy a friends list
                listItem = new GUIListItem(Translation.CopyList);
                dlg.Add(listItem);
                listItem.ItemId = (int)ContextMenuItem.Copy;
            }

            // Show Context Menu
            dlg.DoModal(GUIWindowManager.ActiveWindow);
            if (dlg.SelectedId < 0) return;

            TraktList currentList = new TraktList
            {
                Name = selectedList.Name,
                Description = selectedList.Description,
                Privacy = selectedList.Privacy,
                Slug = selectedList.Slug,
                ShowNumbers = selectedList.ShowNumbers,
                AllowShouts = selectedList.AllowShouts
            };

            switch (dlg.SelectedId)
            {
                case ((int)ContextMenuItem.Create):
                    TraktList list = new TraktList();
                    if (TraktLists.GetListDetailsFromUser(ref list))
                    {
                        if (Lists.ToList().Exists(l => l.Name == list.Name))
                        {
                            // list with that name already exists
                            GUIUtils.ShowNotifyDialog(Translation.Lists, Translation.ListNameAlreadyExists);
                            return;
                        }
                        TraktLogger.Info("Creating new '{0}' list '{1}'", list.Privacy, list.Name);
                        CreateList(list);
                    }
                    break;

                case ((int)ContextMenuItem.Delete):
                    DeleteList(selectedList);
                    break;

                case ((int)ContextMenuItem.Edit):                    
                    if (TraktLists.GetListDetailsFromUser(ref currentList))
                    {
                        TraktLogger.Info("Editing list '{0}'", currentList.Slug);
                        EditList(currentList);
                    }
                    break;

                case ((int)ContextMenuItem.Copy):
                    if (TraktLists.GetListDetailsFromUser(ref currentList))
                    {
                        CopyList(selectedList, currentList);
                    }
                    break;

                default:
                    break;
            }

            base.OnShowContextMenu();
        }

        #endregion

        #region Private Methods

        private void CopyList(TraktUserList sourceList, TraktList newList)
        {
            CopyList copyList = new CopyList { Username = CurrentUser, Source = sourceList, Destination = newList };
            
            Thread copyThread = new Thread(delegate(object obj)
            {
                CopyList copyParams = obj as CopyList;

                // first create new list
                TraktLogger.Info("Creating new '{0}' list '{1}'", copyParams.Destination.Privacy, copyParams.Destination.Name);
                TraktAddListResponse response = TraktAPI.TraktAPI.ListAdd(copyParams.Destination);
                TraktAPI.TraktAPI.LogTraktResponse<TraktResponse>(response);
                if (response.Status == "success")
                {
                    // update with offical slug
                    copyParams.Destination.Slug = response.Slug;

                    // get items from other list
                    TraktUserList userList = TraktAPI.TraktAPI.GetUserList(copyParams.Username, copyParams.Source.Slug);
                    // copy items to new list
                    List<TraktListItem> items = new List<TraktListItem>();
                    foreach (var item in userList.Items)
                    {
                        TraktListItem listItem = new TraktListItem();
                        listItem.Type = item.Type;

                        switch (item.Type)
                        {
                            case "movie":
                                listItem.Title = item.Movie.Title;
                                listItem.Year = Convert.ToInt32(item.Movie.Year);
                                listItem.ImdbId = item.Movie.IMDBID;
                                break;
                            case "show":
                                listItem.Title = item.Show.Title;
                                listItem.Year = item.Show.Year;
                                listItem.TvdbId = item.Show.Tvdb;
                                break;
                            case "season":
                                listItem.Title = item.Show.Title;
                                listItem.Year = item.Show.Year;
                                listItem.TvdbId = item.Show.Tvdb;
                                listItem.Season = Convert.ToInt32(item.SeasonNumber);
                                break;
                            case "episode":
                                listItem.Title = item.Show.Title;
                                listItem.Year = item.Show.Year;
                                listItem.TvdbId = item.Show.Tvdb;
                                listItem.Season = Convert.ToInt32(item.SeasonNumber);
                                listItem.Episode = Convert.ToInt32(item.EpisodeNumber);
                                break;
                        }
                        items.Add(listItem);
                    }
                    copyParams.Destination.Items = items;
                    
                    // add items to the list
                    TraktAPI.TraktAPI.LogTraktResponse<TraktSyncResponse>(TraktAPI.TraktAPI.ListAddItems(copyParams.Destination));
                    if (response.Status == "success") TraktLists.ClearCache(TraktSettings.Username);
                }
            })
            {
                Name = "CopyList",
                IsBackground = true
            };
            copyThread.Start(copyList);
        }

        private void DeleteList(TraktUserList list)
        {
            if (!GUIUtils.ShowYesNoDialog(Translation.Lists, Translation.ConfirmDeleteList, false))
            {
                return;
            }

            GUIBackgroundTask.Instance.ExecuteInBackgroundAndCallback(() =>
            {
                TraktLogger.Info("Deleting list '{0}'", list.Name);
                TraktList deleteList = new TraktList{ UserName = TraktSettings.Username, Password = TraktSettings.Password, Slug = list.Slug };
                return TraktAPI.TraktAPI.ListDelete(deleteList);
            },
            delegate(bool success, object result)
            {
                if (success)
                {
                    TraktResponse response = result as TraktResponse;
                    TraktAPI.TraktAPI.LogTraktResponse<TraktResponse>(response);
                    if (response.Status == "success")
                    {
                        // reload with new list
                        TraktLists.ClearCache(TraktSettings.Username);
                        LoadLists();
                    }
                    else
                    {
                        GUIUtils.ShowNotifyDialog(Translation.Lists, response.Error);
                    }
                }
            }, Translation.DeletingList, true);
        }

        private void CreateList(TraktList list)
        {
            GUIBackgroundTask.Instance.ExecuteInBackgroundAndCallback(() =>
            {
                return TraktAPI.TraktAPI.ListAdd(list);
            },
            delegate(bool success, object result)
            {
                if (success)
                {
                    TraktResponse response = result as TraktResponse;
                    TraktAPI.TraktAPI.LogTraktResponse<TraktResponse>(response);
                    if (response.Status == "success")
                    {
                        // reload with new list
                        TraktLists.ClearCache(TraktSettings.Username);
                        LoadLists();
                    }
                    else
                    {
                        GUIUtils.ShowNotifyDialog(Translation.Lists, response.Error);
                    }
                }
            }, Translation.CreatingList, true);
        }

        private void EditList(TraktList list)
        {
            GUIBackgroundTask.Instance.ExecuteInBackgroundAndCallback(() =>
            {
                return TraktAPI.TraktAPI.ListUpdate(list);
            },
            delegate(bool success, object result)
            {
                if (success)
                {
                    TraktResponse response = result as TraktResponse;
                    TraktAPI.TraktAPI.LogTraktResponse<TraktResponse>(response);
                    if (response.Status == "success")
                    {
                        // reload with new list
                        TraktLists.ClearCache(TraktSettings.Username);
                        LoadLists();
                    }
                    else
                    {
                        GUIUtils.ShowNotifyDialog(Translation.Lists, response.Error);
                    }
                }
            }, Translation.EditingList, true);
        }

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
                    Lists = result as IEnumerable<TraktUserList>;
                    SendListsToFacade(Lists);
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
                    // nothing to do, exit
                    GUIWindowManager.ShowPreviousWindow();
                    return;
                }
                TraktList list = new TraktList();
                if (TraktLists.GetListDetailsFromUser(ref list))
                {
                    TraktLogger.Info("Creating new '{0}' list '{1}'", list.Privacy, list.Name);
                    CreateList(list);
                }
                return;
            }

            int itemId = 0;

            // Add each list
            foreach (var list in lists)
            {
                GUIListItem item = new GUIListItem(list.Name);

                item.Label2 = TraktLists.GetPrivacyLevelTranslation(list.Privacy);
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
            SetProperty("#Trakt.List.AllowShouts", string.Empty);
            SetProperty("#Trakt.List.ShowNumbers", string.Empty);
        }

        private void PublishListSkinProperties(TraktUserList list)
        {
            SetProperty("#Trakt.List.Name", list.Name);
            SetProperty("#Trakt.List.Description", list.Description);
            SetProperty("#Trakt.List.Privacy", list.Privacy);
            SetProperty("#Trakt.List.Slug", list.Slug);
            SetProperty("#Trakt.List.Url", list.Url);
            SetProperty("#Trakt.List.AllowShouts", list.AllowShouts.ToString());
            SetProperty("#Trakt.List.ShowNumbers", list.ShowNumbers.ToString());
        }

        private void OnItemSelected(GUIListItem item, GUIControl parent)
        {
            TraktUserList list = item.TVTag as TraktUserList;
            PublishListSkinProperties(list);
        }

        #endregion
    }

    internal class CopyList
    {
        public string Username { get; set; }
        public TraktUserList Source { get; set; }
        public TraktList Destination { get; set; }
    }
}