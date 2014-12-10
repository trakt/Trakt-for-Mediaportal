using System;
using System.Collections.Generic;
using System.Linq;
using MediaPortal.GUI.Library;
using TraktPlugin.GUI;
using TraktPlugin.TraktAPI.DataStructures;

namespace TraktPlugin
{
    public class TraktLists
    {
        #region Variables

        static DateTime LastRequest = new DateTime();
        static Dictionary<string, IEnumerable<TraktListDetail>> UserLists = new Dictionary<string, IEnumerable<TraktListDetail>>();
        static Dictionary<string, IEnumerable<TraktListItem>> UserListItems = new Dictionary<string, IEnumerable<TraktListItem>>();

        #endregion

        #region Private Properties

        #endregion

        #region Public Methods

        /// <summary>
        /// Get custom lists created by a user
        /// </summary>
        public static IEnumerable<TraktListDetail> GetListsForUser(string username)
        {
            if (!UserLists.Keys.Contains(username) || LastRequest < DateTime.UtcNow.Subtract(new TimeSpan(0, TraktSettings.WebRequestCacheMinutes, 0)))
            {
                // get all lists for user
                var userLists = TraktAPI.TraktAPI.GetUserLists(username);
                if (userLists == null) return null;

                // remove any cached list for user
                if (UserLists.Keys.Contains(username))
                    UserLists.Remove(username);

                UserLists.Add(username, userLists);
                LastRequest = DateTime.UtcNow;
            }
            return UserLists[username];
        }

        /// <summary> 
        /// Get list items for user
        /// </summary>
        public static IEnumerable<TraktListItem> GetListItemsForUser(string username, int id)
        {
            string key = username + ":" + id;

            // use the username:id to cache items in a users list
            if (!UserListItems.Keys.Contains(key) || LastRequest < DateTime.UtcNow.Subtract(new TimeSpan(0, TraktSettings.WebRequestCacheMinutes, 0)))
            {
                // get list items               
                var listItems = TraktAPI.TraktAPI.GetUserListItems(username, id.ToString(), "full,images");
                if (listItems == null) return null;

                // remove any cached items for user
                if (UserListItems.Keys.Contains(key))
                    UserListItems.Remove(key);

                // add to list items cache
                UserListItems.Add(key, listItems);
            }
            return UserListItems[key];
        }

        /// <summary>
        /// Temporarily clears all items in a list
        /// Next time list contents will be refereshed online
        /// </summary>
        public static void ClearItemsInList(string username, int id)
        {
            // if we are adding to the current active list, then this is invalid and we dont care
            // if we are removing from the current list, we already take care of this ourselves
            // in all other cases we should clear
            if (GUIListItems.CurrentList != null && GUIListItems.CurrentList.Ids.Trakt == id && GUIListItems.CurrentUser == username)
                return;

            var listItems = GetListItemsForUser(username, id);
            if (listItems == null) return;

            // remove items
            UserListItems.Remove(username + ":" + id);
        }

        /// <summary>
        /// Get the ids for each list selected by a user in the Multi-Select dialog
        /// </summary>
        public static List<int> GetUserListSelections(List<TraktListDetail> lists)
        {
            if (lists.Count == 0)
            {
                if (!GUIUtils.ShowYesNoDialog(Translation.Lists, Translation.NoListsFound, true))
                {
                    // nothing to do, return
                    return null;
                }
                var list = new TraktListDetail();
                if (TraktLists.GetListDetailsFromUser(ref list))
                {
                    TraktLogger.Info("Creating new list for user online. Privacy = '{0}', Name = '{1}'", list.Privacy, list.Name);
                    var response = TraktAPI.TraktAPI.CreateCustomList(list, TraktSettings.Username);
                    if (response != null)
                    {
                        ClearListCache(TraktSettings.Username);
                        return new List<int> { (int)response.Ids.Trakt };
                    }
                }
                  return null;
            }

            List<MultiSelectionItem> selectedItems = GUIUtils.ShowMultiSelectionDialog(Translation.SelectLists, GetMultiSelectItems(lists));
            if (selectedItems == null) return null;

            var listIds = new List<int>();
            foreach (var item in selectedItems.Where(l => l.Selected == true))
            {
                listIds.Add(int.Parse(item.ItemID));
            }
            return listIds;
        }

        /// <summary>
        /// Get all details needed to create a new list or edit existing list
        /// </summary>
        /// <param name="list">returns list details</param>
        /// <returns>true if list details completed</returns>
        public static bool GetListDetailsFromUser(ref TraktListDetail list)
        {
            // the list will have ids if it exists online
            bool editing = list.Ids != null;

            // Show Keyboard for Name of list
            string name = editing ? list.Name : string.Empty;
            if (!GUIUtils.GetStringFromKeyboard(ref name)) return false;
            if ((list.Name = name) == string.Empty) return false;            

            // Skip Description and get Privacy...this requires a custom dialog as
            // virtual keyboard does not allow very much text for longer descriptions.
            // We may create a custom dialog for this in future
            var items = new List<GUIListItem>();
            var item = new GUIListItem();
            int selectedItem = 0;

            // Public
            item = new GUIListItem { Label = Translation.PrivacyPublic, Label2 = Translation.Public };
            if (list.Privacy == "public") { selectedItem = 0; item.Selected = true; }
            items.Add(item);
            // Private
            item = new GUIListItem { Label = Translation.PrivacyPrivate, Label2 = Translation.Private };
            if (list.Privacy == "private") { selectedItem = 1; item.Selected = true; }
            items.Add(item);
            // Friends
            item = new GUIListItem { Label = Translation.PrivacyFriends, Label2 = Translation.Friends };
            if (list.Privacy == "friends") { selectedItem = 2; item.Selected = true; }
            items.Add(item);

            selectedItem = GUIUtils.ShowMenuDialog(Translation.Privacy, items, selectedItem);
            if (selectedItem == -1) return false;

            list.Privacy = GetPrivacyLevelFromTranslation(items[selectedItem].Label2);

            // Skip 'Show Shouts' and 'Use Numbering' until we have Custom Dialog for List edits

            return true;
        }

        public static void ClearListCache(string username)
        {
            if (UserLists.ContainsKey(username))
                UserLists.Remove(username);
        }

        public static void ClearListItemCache(string username, string id)
        {
            if (UserListItems.ContainsKey(username + ":" + id))
                UserListItems.Remove(username);
        }

        #endregion

        #region Private\Internal Methods

        static List<MultiSelectionItem> GetMultiSelectItems(List<TraktListDetail> lists)
        {
            var result = new List<MultiSelectionItem>();
            
            foreach (var list in lists)
            {
                var multiSelectionItem = new MultiSelectionItem
                {
                    ItemID = list.Ids.Trakt.ToString(),
                    ItemTitle = list.Name,
                    ItemTitle2 = GetPrivacyLevelTranslation(list.Privacy),
                    Selected = false,
                    Tag = list
                };
                result.Add(multiSelectionItem);
            }

            return result;
        }

        internal static string GetPrivacyLevelFromTranslation(string translatedString)
        {
            if (translatedString == Translation.Private) return "private";
            if (translatedString == Translation.Friends) return "friends";
            return "public";
        }

        internal static string GetPrivacyLevelTranslation(string privacyLevel)
        {
            if (privacyLevel == "private") return Translation.Private;
            if (privacyLevel == "friends") return Translation.Friends;
            return Translation.Public;
        }

        #endregion

    }
}
