using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using MediaPortal.GUI.Library;
using MediaPortal.Util;
using TraktPlugin.TraktAPI.DataStructures;
using TraktPlugin.TraktAPI.Extensions;
using TraktPlugin.TraktHandlers;
using Action = MediaPortal.GUI.Library.Action;

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
        IEnumerable<TraktListDetail> Lists { get; set; }

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
            GUICommon.ClearListProperties();

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
            GUICommon.ClearListProperties();

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

                        var selectedList = selectedItem.TVTag as TraktListDetail;
                        if (selectedList == null) return;

                        // Load current selected list
                        GUIListItems.CurrentList = selectedList;
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

            var selectedList = selectedItem.TVTag as TraktListDetail;
            if (selectedItem == null) return;

            var dlg = (IDialogbox)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
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
            
            var currentList = new TraktListDetail
            {
                Ids = selectedList.Ids,
                Name = selectedList.Name,
                Description = selectedList.Description,
                Privacy = selectedList.Privacy,
                AllowComments = selectedList.AllowComments,
                DisplayNumbers = selectedList.DisplayNumbers,
                ItemCount = selectedList.ItemCount,
                Likes = selectedList.Likes,
                UpdatedAt = selectedList.UpdatedAt
            };

            switch (dlg.SelectedId)
            {
                case ((int)ContextMenuItem.Create):
                    var list = new TraktListDetail();
                    if (TraktLists.GetListDetailsFromUser(ref list))
                    {
                        if (Lists.Any(l => l.Name == list.Name))
                        {
                            // list with that name already exists
                            GUIUtils.ShowNotifyDialog(Translation.Lists, Translation.ListNameAlreadyExists);
                            return;
                        }
                        TraktLogger.Info("Creating new list for user online. Privacy = '{0}', Name = '{1}'", list.Privacy, list.Name);
                        CreateList(list);
                    }
                    break;

                case ((int)ContextMenuItem.Delete):
                    DeleteList(selectedList);
                    break;

                case ((int)ContextMenuItem.Edit):                    
                    if (TraktLists.GetListDetailsFromUser(ref currentList))
                    {
                        TraktLogger.Info("Editing list. Name = '{0}', Id = '{1}'", currentList.Name);
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

        private void CopyList(TraktListDetail sourceList, TraktListDetail newList)
        {
            var copyList = new CopyList { Username = CurrentUser, Source = sourceList, Destination = newList };
            
            var copyThread = new Thread((obj) =>
            {
                var copyParams = obj as CopyList;

                // first create new list
                TraktLogger.Info("Creating new list online. Privacy = '{0}', Name = '{1}'", copyParams.Destination.Privacy, copyParams.Destination.Name);
                var response = TraktAPI.TraktAPI.CreateCustomList(copyParams.Destination, TraktSettings.Username);
                if (response != null)
                {
                    // get items from other list
                    var userListItems = TraktAPI.TraktAPI.GetUserListItems(copyParams.Username, copyParams.Source.Ids.Trakt.ToString(), "min");

                    // copy items to new list
                    var itemsToAdd = new TraktSyncAll();
                    foreach (var item in userListItems)
                    {
                        var listItem = new TraktListItem();
                        listItem.Type = item.Type;
                        
                        switch (item.Type)
                        {
                            case "movie":
                                if (itemsToAdd.Movies == null)
                                    itemsToAdd.Movies = new List<TraktMovie>();

                                itemsToAdd.Movies.Add(new TraktMovie { Ids = item.Movie.Ids });
                                break;

                            case "show":
                                if (itemsToAdd.Shows == null)
                                    itemsToAdd.Shows = new List<TraktShow>();

                                itemsToAdd.Shows.Add(new TraktShow { Ids = item.Show.Ids });
                                break;

                            case "season":
                                if (itemsToAdd.Seasons == null)
                                    itemsToAdd.Seasons = new List<TraktSeason>();

                                itemsToAdd.Seasons.Add(new TraktSeason { Ids = item.Season.Ids });
                                break;

                            case "episode":
                                if (itemsToAdd.Episodes == null)
                                    itemsToAdd.Episodes = new List<TraktEpisode>();

                                itemsToAdd.Episodes.Add(new TraktEpisode { Ids = item.Episode.Ids });
                                break;

                            case "person":
                                if (itemsToAdd.People == null)
                                    itemsToAdd.People = new List<TraktPerson>();

                                itemsToAdd.People.Add(new TraktPerson { Ids = item.Person.Ids });
                                break;
                        }
                    }
                    
                    // add items to the list
                    var ItemsAddedResponse = TraktAPI.TraktAPI.AddItemsToList(TraktSettings.Username, response.Ids.Trakt.ToString(), itemsToAdd);

                    if (ItemsAddedResponse != null)
                    {
                        TraktLists.ClearListCache(TraktSettings.Username);
                        TraktCache.ClearCustomListCache();

                        // updated MovingPictures categories and filters menu
                        if (TraktHelper.IsMovingPicturesAvailableAndEnabled)
                        {
                            TraktHandlers.MovingPictures.UpdateCategoriesMenu(SyncListType.CustomList);
                            TraktHandlers.MovingPictures.UpdateFiltersMenu(SyncListType.CustomList);
                        }
                    }
                }
            })
            {
                Name = "CopyList",
                IsBackground = true
            };
            copyThread.Start(copyList);
        }

        private void DeleteList(TraktListDetail list)
        {
            if (!GUIUtils.ShowYesNoDialog(Translation.Lists, Translation.ConfirmDeleteList, false))
            {
                return;
            }

            GUIBackgroundTask.Instance.ExecuteInBackgroundAndCallback(() =>
            {
                TraktLogger.Info("Deleting list from online. Name = '{0}', Id = '{1}'", list.Name, list.Ids.Trakt);
                return TraktAPI.TraktAPI.DeleteUserList(TraktSettings.Username, list.Ids.Trakt.ToString());
            },
            delegate(bool success, object result)
            {
                if (success)
                {
                    if ((result as bool?) == true)
                    {
                        // remove from MovingPictures categories and filters menu
                        if (TraktHelper.IsMovingPicturesAvailableAndEnabled)
                        {
                            // not very thread safe if we tried to delete more than one before response!
                            TraktHandlers.MovingPictures.RemoveCustomListNode(list.Name);
                        }

                        // reload with new list
                        TraktLists.ClearListCache(TraktSettings.Username);
                        LoadLists();
                    }
                    else
                    {
                        GUIUtils.ShowNotifyDialog(Translation.Lists, Translation.FailedDeleteList);
                    }
                }
            }, Translation.DeletingList, true);
        }

        private void CreateList(TraktList list)
        {
            GUIBackgroundTask.Instance.ExecuteInBackgroundAndCallback(() =>
            {
                return TraktAPI.TraktAPI.CreateCustomList(list, TraktSettings.Username);
            },
            delegate(bool success, object result)
            {
                if (success)
                {
                    var response = result as TraktListDetail;
                    if (response != null)
                    {
                        // add to MovingPictures categories and filters menu
                        if (TraktHelper.IsMovingPicturesAvailableAndEnabled)
                        {
                            // not very thread safe if we tried to add more than one before response!
                            TraktHandlers.MovingPictures.AddCustomListNode(list.Name);
                        }

                        // reload with new list
                        TraktLists.ClearListCache(TraktSettings.Username);
                        LoadLists();
                    }
                    else
                    {
                        GUIUtils.ShowNotifyDialog(Translation.Lists, Translation.FailedCreateList);
                    }
                }
            }, Translation.CreatingList, true);
        }

        private void EditList(TraktListDetail list) 
        {
            GUIBackgroundTask.Instance.ExecuteInBackgroundAndCallback(() =>
            {
                return TraktAPI.TraktAPI.UpdateCustomList(list, TraktSettings.Username);
            },
            delegate(bool success, object result)
            {
                if (success)
                {
                    var response = result as TraktListDetail;
                    if (response == null)
                    {
                        // reload with new list
                        TraktLists.ClearListCache(TraktSettings.Username);
                        LoadLists();

                        var thread = new Thread((o) =>
                        {
                            TraktCache.ClearCustomListCache();

                            // updated MovingPictures categories and filters menu
                            if (TraktHelper.IsMovingPicturesAvailableAndEnabled)
                            {
                                TraktHandlers.MovingPictures.UpdateCategoriesMenu(SyncListType.CustomList);
                                TraktHandlers.MovingPictures.UpdateFiltersMenu(SyncListType.CustomList);
                            }
                        })
                        {
                            Name = "EditList",
                            IsBackground = true
                        };
                        thread.Start();
                    }
                    else
                    {
                        GUIUtils.ShowNotifyDialog(Translation.Lists, Translation.FailedUpdateList);
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
                    Lists = result as IEnumerable<TraktListDetail>;
                    SendListsToFacade(Lists);
                }
            }, Translation.GettingLists, true);
        }

        private void SendListsToFacade(IEnumerable<TraktListDetail> lists)
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

                var list = new TraktListDetail();
                if (TraktLists.GetListDetailsFromUser(ref list))
                {
                    TraktLogger.Info("Creating new list online. Privacy = '{0}', Name = '{1}'", list.Privacy, list.Name);
                    CreateList(list);
                }
                return;
            }

            int itemId = 0;

            // Add each list
            foreach (var list in lists)
            {
                var item = new GUIListItem(list.Name);

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

        private void InitProperties()
        {
            // set current user to logged in user if not set
            if (string.IsNullOrEmpty(CurrentUser))
                CurrentUser = TraktSettings.Username;
            
            GUICommon.SetProperty("#Trakt.Lists.CurrentUser", CurrentUser);
        }
                
        private void OnItemSelected(GUIListItem item, GUIControl parent)
        {
            var list = item.TVTag as TraktListDetail;
            GUICommon.SetListProperties(list, CurrentUser);
        }

        #endregion
    }

    internal class CopyList
    {
        public string Username { get; set; }
        public TraktListDetail Source { get; set; }
        public TraktListDetail Destination { get; set; }
    }
}