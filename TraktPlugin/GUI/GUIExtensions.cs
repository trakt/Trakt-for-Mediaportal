using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaPortal.GUI.Library;
using System.Reflection;
using System.Collections;

namespace TraktPlugin.GUI
{
    public static class GUIWindowExtensions
    {
        /// <summary>
        /// Same as Children and controlList but used for backwards compatibility between mediaportal 1.1 and 1.2
        /// </summary>
        /// <param name="self"></param>
        /// <returns>IEnumerable of GUIControls</returns>
        public static IEnumerable GetControlList(this GUIWindow self)
        {
            PropertyInfo property = GetPropertyInfo<GUIWindow>("Children", null);
            return (IEnumerable)property.GetValue(self, null);
        }

        private static Dictionary<string, PropertyInfo> propertyCache = new Dictionary<string, PropertyInfo>();

        /// <summary>
        /// Gets the property info object for a property using reflection.
        /// The property info object will be cached in memory for later requests.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="newName">The name of the property in 1.2</param>
        /// <param name="oldName">The name of the property in 1.1</param>
        /// <returns>instance PropertyInfo or null if not found</returns>
        public static PropertyInfo GetPropertyInfo<T>(string newName, string oldName)
        {
            PropertyInfo property = null;
            Type type = typeof(T);
            string key = type.FullName + "|" + newName;

            if (!propertyCache.TryGetValue(key, out property))
            {
                property = type.GetProperty(newName);
                if (property == null)
                {
                    property = type.GetProperty(oldName);
                }

                propertyCache[key] = property;
            }

            return property;
        }

        /// <summary>
        /// Acts the same as the ListLayout / ListView property.
        /// </summary>
        /// <remarks>this extension method was added to allow backwards compatibility with MediaPortal 1.1</remarks>
        /// <param name="self"></param>
        /// <returns>instance of GUIListControl or null</returns>
        public static GUIListControl ListLayout(this GUIFacadeControl self)
        {
            PropertyInfo property = GetPropertyInfo<GUIFacadeControl>("ListLayout", "ListView");
            return (GUIListControl)property.GetValue(self, null);
        }

        /// <summary>
        /// Acts the same as the FilmstripLayout / FilmstripView property.
        /// </summary>
        /// <remarks>this extension method was added to allow backwards compatibility with MediaPortal 1.1</remarks>
        /// <param name="self"></param>
        /// <returns>instance of GUIListControl or null</returns>
        public static GUIFilmstripControl FilmstripLayout(this GUIFacadeControl self)
        {
            PropertyInfo property = GetPropertyInfo<GUIFacadeControl>("FilmstripLayout", "FilmstripView");
            return (GUIFilmstripControl)property.GetValue(self, null);
        }

        /// <summary>
        /// Acts the same as the ThumbnailLayout / ThumbnailView property.
        /// </summary>
        /// <remarks>this extension method was added to allow backwards compatibility with MediaPortal 1.1</remarks>
        /// <param name="self"></param>
        /// <returns>instance of GUIListControl or null</returns>
        public static GUIThumbnailPanel ThumbnailLayout(this GUIFacadeControl self)
        {
            PropertyInfo property = GetPropertyInfo<GUIFacadeControl>("ThumbnailLayout", "ThumbnailView");
            return (GUIThumbnailPanel)property.GetValue(self, null);
        }

        /// <summary>
        /// Acts the same as the AlbumListLayout / AlbumListView property.
        /// </summary>
        /// <remarks>this extension method was added to allow backwards compatibility with MediaPortal 1.1</remarks>
        /// <param name="self"></param>
        /// <returns>instance of GUIListControl or null</returns>
        public static GUIListControl AlbumListLayout(this GUIFacadeControl self)
        {
            PropertyInfo property = GetPropertyInfo<GUIFacadeControl>("AlbumListLayout", "AlbumListView");
            return (GUIListControl)property.GetValue(self, null);
        }

        /// <summary>
        /// Acts the same as the CurrentLayout / View property.
        /// </summary>
        /// <remarks>this extension method was added to allow backwards compatibility with MediaPortal 1.1</remarks>
        /// <param name="self"></param>
        /// <returns>instance of GUIListControl or null</returns>
        public static void SetCurrentLayout(this GUIFacadeControl self, string layout)
        {
            PropertyInfo property = GetPropertyInfo<GUIFacadeControl>("CurrentLayout", "View");
            property.SetValue(self, Enum.Parse(property.PropertyType, layout), null);
        }

        /// <summary>
        /// Selects the specified item in the facade
        /// </summary>
        /// <param name="self"></param>
        /// <param name="index">index of the item</param>
        public static void SelectIndex(this GUIFacadeControl self, int index)
        {
            if (index > self.Count) index = 0;
            if (index == self.Count) index--;
            GUIMessage msg = new GUIMessage(GUIMessage.MessageType.GUI_MSG_ITEM_SELECT, self.WindowId, 0, self.GetID, index, 0, null);
            GUIGraphicsContext.SendMessage(msg);
        }

        /// <summary>
        /// Sets the facade and any defined layouts visibility to the visibility defined by the skin
        /// </summary>
        /// <param name="self"></param>
        public static void SetVisibleFromSkinCondition(this GUIFacadeControl self)
        {
            self.Visible = self.VisibleFromSkinCondition;
            if (self.FilmstripLayout != null) self.FilmstripLayout.Visible = self.FilmstripLayout.VisibleFromSkinCondition;
            if (self.CoverFlowLayout != null) self.CoverFlowLayout.Visible = self.CoverFlowLayout.VisibleFromSkinCondition;
            if (self.AlbumListLayout != null) self.AlbumListLayout.Visible = self.AlbumListLayout.VisibleFromSkinCondition;
            if (self.ThumbnailLayout != null) self.ThumbnailLayout.Visible = self.ThumbnailLayout.VisibleFromSkinCondition;
            if (self.ListLayout != null) self.ListLayout.Visible = self.ListLayout.VisibleFromSkinCondition;
        }

        /// <summary>
        /// Sends a Thread message to select an item on a facade object. Will only send if itemid parameter is currently selected
        /// </summary>
        /// <param name="self">the list object</param>
        /// <param name="windowId">the window id containing list control</param>
        /// <param name="index">the item id in list to check if selected</param>
        /// <param name="controlId">the id of the list control, defaults to 50</param>
        public static void UpdateItemIfSelected(this GUIListItem self, int windowId, int index, int controlId = 50)
        {
            if (GUIWindowManager.ActiveWindow != windowId) return;

            GUIListItem selectedItem = GUIControl.GetSelectedListItem(windowId, controlId);
            
            // only send message if the current item is selected
            if (selectedItem == self)
            {
                GUIWindowManager.SendThreadMessage(new GUIMessage(GUIMessage.MessageType.GUI_MSG_ITEM_SELECT, GUIWindowManager.ActiveWindow, 0, controlId, index, 0, null));
            }
        }
    }
}