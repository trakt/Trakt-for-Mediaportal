using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaPortal.GUI.Library;
using TraktPlugin.GUI;
using TraktPlugin.TraktAPI.v1.DataStructures;

namespace TraktPlugin
{
    public class TraktLists
    {
        #region Variables

        static DateTime LastRequest = new DateTime();
        static Dictionary<string, IEnumerable<TraktUserList>> usersLists = new Dictionary<string, IEnumerable<TraktUserList>>();        
        static IEnumerable<TraktUserList> userLists = null;        

        #endregion

        #region Private Properties

        #endregion

        #region Public Methods

        /// <summary>
        /// Get a list of custom lists created by a user
        /// </summary>
        public static IEnumerable<TraktUserList> GetListsForUser(string username)
        {
            if (!usersLists.Keys.Contains(username) || LastRequest < DateTime.UtcNow.Subtract(new TimeSpan(0, TraktSettings.WebRequestCacheMinutes, 0)))
            {
                userLists = TraktAPI.v1.TraktAPI.GetUserLists(username);
                if (usersLists.Keys.Contains(username)) usersLists.Remove(username);
                int i = 0; // retain online sort order if we update listitems later
                foreach (var list in userLists) { list.SortOrder = i++; }
                usersLists.Add(username, userLists);
                LastRequest = DateTime.UtcNow;
            }
            return usersLists[username].OrderBy(s => s.SortOrder);
        }

        /// <summary> 
        /// Get list for user
        /// </summary>
        public static TraktUserList GetListForUser(string username, string slug)
        {
            bool getUpdates = LastRequest < DateTime.UtcNow.Subtract(new TimeSpan(0, TraktSettings.WebRequestCacheMinutes, 0));

            IEnumerable<TraktUserList> lists = GetListsForUser(username);
            TraktUserList list = lists.FirstOrDefault(l => l.Slug == slug);

            // lists api doesn't return items so check if have them yet
            if (list.Items == null || getUpdates)
            {
                // remove old list from cache
                lists = lists.Where(l => l.Slug != slug);
                
                // remember sort order
                int sortOrder = list.SortOrder;

                // get list with list items               
                list = TraktAPI.v1.TraktAPI.GetUserList(username, slug);
                list.SortOrder = sortOrder;

                // update cached result
                lists = lists.Concat(new[] { list });
                usersLists[username] = lists;
                LastRequest = DateTime.UtcNow;
            }

            return list;
        }

        /// <summary>
        /// Temporarily clears all items in a list
        /// Next time list contents will be refereshed online
        /// </summary>
        public static void ClearItemsInList(string username, string slug)
        {
            // if we are adding to the current active list, then this is invalid and we dont care
            // if we are removing from the current list, we already take care of this ourselves
            // in all other cases we should clear
            if (GUIListItems.CurrentList != null && GUIListItems.CurrentList.Slug == slug && GUIListItems.CurrentUser == username)
                return;

            IEnumerable<TraktUserList> lists = GetListsForUser(username);
            TraktUserList list = lists.FirstOrDefault(l => l.Slug == slug);
            
            // nothing to do
            if (list.Items == null) return;

            // remove old list from cache
            lists = lists.Where(l => l.Slug != slug);

            // remove items from current list
            list.Items = null;

            // update cached result
            lists = lists.Concat(new[] { list });
            usersLists[username] = lists;
        }

        /// <summary>
        /// Get the slugs for each list selected by a user in the Multi-Select dialog
        /// </summary>
        /// <param name="username">username of user</param>
        /// <param name="lists">List of lists created by user</param>
        public static List<string> GetUserListSelections(List<TraktUserList> lists)
        {
            if (lists.Count == 0)
            {
                if (!GUIUtils.ShowYesNoDialog(Translation.Lists, Translation.NoListsFound, true))
                {
                    // nothing to do, return
                    return null;
                }
                TraktList list = new TraktList();
                if (TraktLists.GetListDetailsFromUser(ref list))
                {
                    TraktLogger.Info("Creating new '{0}' list '{1}'", list.Privacy, list.Name);
                    TraktAddListResponse response = TraktAPI.v1.TraktAPI.ListAdd(list);
                    TraktLogger.LogTraktResponse<TraktResponse>(response);
                    if (response.Status == "success")
                    {
                        ClearCache(TraktSettings.Username);
                        return new List<string> { response.Slug };
                    }
                }
                  return null;
            }

            List<MultiSelectionItem> selectedItems = GUIUtils.ShowMultiSelectionDialog(Translation.SelectLists, GetMultiSelectItems(lists));
            if (selectedItems == null) return null;

            List<string> slugs = new List<string>();
            foreach (var item in selectedItems.Where(l => l.Selected == true))
            {
                slugs.Add(item.ItemID);
            }
            return slugs;
        }

        /// <summary>
        /// Get all details needed to create a new list or edit existing list
        /// </summary>
        /// <param name="list">returns list details</param>
        /// <returns>true if list details completed</returns>
        public static bool GetListDetailsFromUser(ref TraktList list)
        {
            list.UserName = TraktSettings.Username;
            list.Password = TraktSettings.Password;

            bool editing = !string.IsNullOrEmpty(list.Slug);

            // Show Keyboard for Name of list
            string name = editing ? list.Name : string.Empty;
            if (!GUIUtils.GetStringFromKeyboard(ref name)) return false;
            if ((list.Name = name) == string.Empty) return false;            

            // Skip Description and get Privacy...this requires a custom dialog as
            // virtual keyboard does not allow very much text for longer descriptions.
            // We may create a custom dialog for this in future
            List<GUIListItem> items = new List<GUIListItem>();
            GUIListItem item = new GUIListItem();
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

        public static void ClearCache(string username)
        {
            if (usersLists.ContainsKey(username)) usersLists.Remove(username);
        }

        #endregion

        #region Private\Internal Methods

        static List<MultiSelectionItem> GetMultiSelectItems(List<TraktUserList> lists)
        {
            List<MultiSelectionItem> result = new List<MultiSelectionItem>();
            
            foreach (var list in lists)
            {
                MultiSelectionItem multiSelectionItem = new MultiSelectionItem
                {
                    ItemID = list.Slug,
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
