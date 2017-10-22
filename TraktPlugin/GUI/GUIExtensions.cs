using MediaPortal.GUI.Library;
using System;

namespace TraktPlugin.GUI
{
    public static class GUIWindowExtensions
    {
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

            try
            {
                GUIListItem selectedItem = GUIControl.GetSelectedListItem(windowId, controlId);

                // only send message if the current item is selected
                if (selectedItem == self)
                {
                    GUIWindowManager.SendThreadMessage(new GUIMessage(GUIMessage.MessageType.GUI_MSG_ITEM_SELECT, GUIWindowManager.ActiveWindow, 0, controlId, index, 0, null));
                }
            }
            catch (Exception)
            {
                TraktLogger.Warning("Unable to update selected facade item, MediaPortal could not get a reference");
            }
        }

        /// <summary>
        /// Checks if a GUICheckButton control is selected (checked)
        /// This is also safe if control is not implemented by skin in which case the default value is returned
        /// </summary>
        /// <param name="self">the control to check</param>
        /// <returns>true if selected/checked</returns>
        public static bool IsSelected(this GUICheckButton self, bool defaultValue = false)
        {
            // check if skin implements control
            if (self == null) return defaultValue;
            return self.Selected;
        }

        /// <summary>
        /// Sets a GUICheckButton to selected or not selected
        /// </summary>
        /// <param name="self">the control to set</param>
        /// <param name="state">checked or unchecked</param>
        public static void Select(this GUICheckButton self, bool state)
        {
            // check if skin implements control
            if (self == null) return;
            
            self.Selected = state;
            return;
        }
    }
}