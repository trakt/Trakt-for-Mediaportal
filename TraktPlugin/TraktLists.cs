using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TraktPlugin.GUI;
using TraktPlugin.TraktAPI.DataStructures;

namespace TraktPlugin
{
    public class TraktLists
    {
        #region Variables

        static DateTime LastRequest = new DateTime();
        static Dictionary<string, IEnumerable<TraktUserList>> userLists = new Dictionary<string, IEnumerable<TraktUserList>>();
        static IEnumerable<TraktUserList> userList = null;

        #endregion

        #region Private Properties

        #endregion

        #region Public Methods

        /// <summary>
        /// Get a list of custom lists created by a user
        /// </summary>
        public static IEnumerable<TraktUserList> GetListsForUser(string username)
        {
            if (!userLists.Keys.Contains(username) || LastRequest < DateTime.UtcNow.Subtract(new TimeSpan(0, TraktSettings.WebRequestCacheMinutes, 0)))
            {
                userList = TraktAPI.TraktAPI.GetUserLists(username);
                if (userLists.Keys.Contains(username)) userLists.Remove(username);
                userLists.Add(username, userList);
                LastRequest = DateTime.UtcNow;
            }
            return userLists[username];
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
                    return null;
                }
                // TODO: Create a dialog to create a new list or edit an existing one
                GUIUtils.ShowOKDialog(Translation.Lists, "Oops, not yet implemented!");
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

        #endregion

        #region Private Methods

        static List<MultiSelectionItem> GetMultiSelectItems(List<TraktUserList> lists)
        {
            List<MultiSelectionItem> result = new List<MultiSelectionItem>();
            
            foreach (var list in lists)
            {
                MultiSelectionItem multiSelectionItem = new MultiSelectionItem
                {
                    ItemID = list.Slug,
                    ItemTitle = list.Name,
                    ItemTitle2 = list.Privacy,
                    Selected = false,
                    Tag = list
                };
                result.Add(multiSelectionItem);
            }

            return result;
        }

        #endregion

    }
}
