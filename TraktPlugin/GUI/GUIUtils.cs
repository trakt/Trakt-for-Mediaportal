using System;
using System.Collections.Generic;
using MediaPortal.Configuration;
using MediaPortal.Dialogs;
using MediaPortal.GUI.Library;
using TraktPlugin.TraktAPI;
using TraktPlugin.TraktAPI.DataStructures;
using TraktPlugin.TraktAPI.Enums;
using System.Threading;

namespace TraktPlugin.GUI
{
    public static class GUIUtils
    {
        private delegate bool ShowCustomYesNoDialogDelegate(string heading, string lines, string yesLabel, string noLabel, bool defaultYes);
        private delegate void ShowOKDialogDelegate(string heading, string lines);
        private delegate void ShowNotifyDialogDelegate(string heading, string text, string image, string buttonText, int timeOut);
        private delegate int ShowMenuDialogDelegate(string heading, List<GUIListItem> items);
        private delegate List<MultiSelectionItem> ShowMultiSelectionDialogDelegate(string heading, List<MultiSelectionItem> items);
        private delegate void ShowTextDialogDelegate(string heading, string text);
        private delegate int ShowRateDialogDelegate<T>(T rateObject);
        private delegate bool GetStringFromKeyboardDelegate(ref string strLine, bool isPassword);

        public static readonly string TraktLogo = GUIGraphicsContext.Skin + "\\Media\\Logos\\trakt.png";

        public static string PluginName()
        {
            return "Trakt";
        }

        public static string GetProperty(string property)
        {
            string propertyVal = GUIPropertyManager.GetProperty(property);
            return propertyVal ?? string.Empty;
        }

        public static void SetProperty(string property, string value)
        {
            SetProperty(property, value, false);
        }

        public static void SetProperty(string property, string value, bool log)
        {
            // prevent ugly display of property names
            if (string.IsNullOrEmpty(value))
                value = " ";

            GUIPropertyManager.SetProperty(property, value);

            if (log)
            {
                if (GUIPropertyManager.Changed)
                    TraktLogger.Debug("Set property \"" + property + "\" to \"" + value + "\" successful");
                else
                    TraktLogger.Warning("Set property \"" + property + "\" to \"" + value + "\" failed");
            }
        }

        /// <summary>
        /// Displays a yes/no dialog.
        /// </summary>
        /// <returns>True if yes was clicked, False if no was clicked</returns>
        public static bool ShowYesNoDialog(string heading, string lines)
        {
            return ShowCustomYesNoDialog(heading, lines, null, null, false);
        }

        /// <summary>
        /// Displays a yes/no dialog.
        /// </summary>
        /// <returns>True if yes was clicked, False if no was clicked</returns>
        public static bool ShowYesNoDialog(string heading, string lines, bool defaultYes)
        {
            return ShowCustomYesNoDialog(heading, lines, null, null, defaultYes);
        }

        /// <summary>
        /// Displays a yes/no dialog with custom labels for the buttons.
        /// This method may become obsolete in the future if media portal adds more dialogs.
        /// </summary>
        /// <returns>True if yes was clicked, False if no was clicked</returns>
        public static bool ShowCustomYesNoDialog(string heading, string lines, string yesLabel, string noLabel)
        {
            return ShowCustomYesNoDialog(heading, lines, yesLabel, noLabel, false);
        }

        /// <summary>
        /// Displays a yes/no dialog with custom labels for the buttons.
        /// This method may become obsolete in the future if media portal adds more dialogs.
        /// </summary>
        /// <returns>True if yes was clicked, False if no was clicked</returns>
        public static bool ShowCustomYesNoDialog(string heading, string lines, string yesLabel, string noLabel, bool defaultYes)
        {
            if (GUIGraphicsContext.form.InvokeRequired)
            {
                ShowCustomYesNoDialogDelegate d = ShowCustomYesNoDialog;
                return (bool)GUIGraphicsContext.form.Invoke(d, heading, lines, yesLabel, noLabel, defaultYes);
            }

            GUIDialogYesNo dlgYesNo = (GUIDialogYesNo)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_YES_NO);

            try
            {
                dlgYesNo.Reset();
                dlgYesNo.SetHeading(heading);
                string[] linesArray = lines.Split(new string[] { "\\n", "\n" }, StringSplitOptions.None);
                if (linesArray.Length > 0) dlgYesNo.SetLine(1, linesArray[0]);
                if (linesArray.Length > 1) dlgYesNo.SetLine(2, linesArray[1]);
                if (linesArray.Length > 2) dlgYesNo.SetLine(3, linesArray[2]);
                if (linesArray.Length > 3) dlgYesNo.SetLine(4, linesArray[3]);
                dlgYesNo.SetDefaultToYes(defaultYes);

                foreach (GUIControl item in dlgYesNo.GetControlList())
                {
                    if (item is GUIButtonControl)
                    {
                        GUIButtonControl btn = (GUIButtonControl)item;
                        if (btn.GetID == 11 && !string.IsNullOrEmpty(yesLabel)) // Yes button
                            btn.Label = yesLabel;
                        else if (btn.GetID == 10 && !string.IsNullOrEmpty(noLabel)) // No button
                            btn.Label = noLabel;
                    }
                }
                dlgYesNo.DoModal(GUIWindowManager.ActiveWindow);
                return dlgYesNo.IsConfirmed;
            }
            finally
            {
                // set the standard yes/no dialog back to it's original state (yes/no buttons)
                if (dlgYesNo != null)
                {
                    dlgYesNo.ClearAll();
                }
            }
        }

        /// <summary>
        /// Displays a OK dialog with heading and up to 4 lines.
        /// </summary>
        public static void ShowOKDialog(string heading, string line1, string line2, string line3, string line4)
        {
            ShowOKDialog(heading, string.Concat(line1, line2, line3, line4));
        }

        /// <summary>
        /// Displays a OK dialog with heading and up to 4 lines split by \n in lines string.
        /// </summary>
        public static void ShowOKDialog(string heading, string lines)
        {
            if (GUIGraphicsContext.form.InvokeRequired)
            {
                ShowOKDialogDelegate d = ShowOKDialog;
                GUIGraphicsContext.form.Invoke(d, heading, lines);
                return;
            }

            GUIDialogOK dlgOK = (GUIDialogOK)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_OK);            

            dlgOK.Reset();
            dlgOK.SetHeading(heading);

            int lineid = 1;
            foreach (string line in lines.Split(new string[] { "\\n", "\n" }, StringSplitOptions.None))
            {
                dlgOK.SetLine(lineid, line);
                lineid++;
            }
            for (int i = lineid; i <= 4; i++)
                dlgOK.SetLine(i, string.Empty);

            dlgOK.DoModal(GUIWindowManager.ActiveWindow);
        }

        /// <summary>
        /// Displays a notification dialog.
        /// </summary>
        public static void ShowNotifyDialog(string heading, string text)
        {
            ShowNotifyDialog(heading, text, TraktLogo, Translation.OK, -1);
        }

        /// <summary>
        /// Displays a notification dialog.
        /// </summary>
        public static void ShowNotifyDialog(string heading, string text, int timeOut)
        {
            ShowNotifyDialog(heading, text, TraktLogo, Translation.OK, timeOut);
        }

        /// <summary>
        /// Displays a notification dialog.
        /// </summary>
        public static void ShowNotifyDialog(string heading, string text, string image)
        {
            ShowNotifyDialog(heading, text, image, Translation.OK, -1);
        }

        /// <summary>
        /// Displays a notification dialog.
        /// </summary>
        public static void ShowNotifyDialog(string heading, string text, string image, string buttonText, int timeout)
        {
            if (GUIGraphicsContext.form.InvokeRequired)
            {
                ShowNotifyDialogDelegate d = ShowNotifyDialog;
                GUIGraphicsContext.form.Invoke(d, heading, text, image, buttonText, timeout);
                return;
            }

            GUIDialogNotify pDlgNotify = (GUIDialogNotify)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_NOTIFY);
            if (pDlgNotify == null) return;

            try
            {
                pDlgNotify.Reset();
                pDlgNotify.SetHeading(heading);
                pDlgNotify.SetImage(image);
                pDlgNotify.SetText(text);
                if (timeout >= 0) pDlgNotify.TimeOut = timeout;
                    
                foreach (GUIControl item in pDlgNotify.GetControlList())
                {
                    if (item is GUIButtonControl)
                    {
                        GUIButtonControl btn = (GUIButtonControl)item;
                        if (btn.GetID == 4 && !string.IsNullOrEmpty(buttonText) && !string.IsNullOrEmpty(btn.Label))
                        {
                            // Only if ID is 4 and we have our custom text and if button already has label (in case the skin "hides" the button by emtying the label)
                            btn.Label = buttonText;
                        }
                    }
                }

                pDlgNotify.DoModal(GUIWindowManager.ActiveWindow);
            }
            finally
            {
                if (pDlgNotify != null)
                    pDlgNotify.ClearAll();
            }
        }

        /// <summary>
        /// Displays a menu dialog from list of items
        /// </summary>
        /// <returns>Selected item index, -1 if exited</returns>
        public static int ShowMenuDialog(string heading, List<GUIListItem> items)
        {
            return ShowMenuDialog(heading, items, -1);
        }

        /// <summary>
        /// Displays a menu dialog from list of items
        /// </summary>
        /// <returns>Selected item index, -1 if exited</returns>
        public static int ShowMenuDialog(string heading, List<GUIListItem> items, int selectedItemIndex)
        {
            if (GUIGraphicsContext.form.InvokeRequired)
            {
                ShowMenuDialogDelegate d = ShowMenuDialog;
                return (int)GUIGraphicsContext.form.Invoke(d, heading, items);
            }

            GUIDialogMenu dlgMenu = (GUIDialogMenu)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);            

            dlgMenu.Reset();
            dlgMenu.SetHeading(heading);           

            foreach (GUIListItem item in items)
            {
                dlgMenu.Add(item);
            }

            if (selectedItemIndex >= 0)
                dlgMenu.SelectedLabel = selectedItemIndex;

            dlgMenu.DoModal(GUIWindowManager.ActiveWindow);

            if (dlgMenu.SelectedLabel < 0)
            {
                return -1;
            }

            return dlgMenu.SelectedLabel;
        }

        /// <summary>
        /// Displays a menu dialog from list of items
        /// </summary>
        public static List<MultiSelectionItem> ShowMultiSelectionDialog(string heading, List<MultiSelectionItem> items)
        {
            List<MultiSelectionItem> result = new List<MultiSelectionItem>();
            if (items == null) return result;

            if (GUIGraphicsContext.form.InvokeRequired)
            {
                ShowMultiSelectionDialogDelegate d = ShowMultiSelectionDialog;
                return (List<MultiSelectionItem>)GUIGraphicsContext.form.Invoke(d, heading, items);
            }

            GUIWindow dlgMultiSelectOld = (GUIWindow)GUIWindowManager.GetWindow(2100);
            GUIDialogMultiSelect dlgMultiSelect = new GUIDialogMultiSelect();
            dlgMultiSelect.Init();
            GUIWindowManager.Replace(2100, dlgMultiSelect);

            try
            {
                dlgMultiSelect.Reset();
                dlgMultiSelect.SetHeading(heading);

                foreach (MultiSelectionItem multiSelectionItem in items)
                {
                    GUIListItem item = new GUIListItem();
                    item.Label = multiSelectionItem.ItemTitle;
                    item.Label2 = multiSelectionItem.ItemTitle2;
                    item.MusicTag = multiSelectionItem.Tag;
                    item.TVTag = multiSelectionItem.IsToggle;
                    item.Selected = multiSelectionItem.Selected;
                    dlgMultiSelect.Add(item);
                }

                dlgMultiSelect.DoModal(GUIWindowManager.ActiveWindow);

                if (dlgMultiSelect.DialogModalResult == ModalResult.OK)
                {
                    for (int i = 0; i < items.Count; i++)
                    {
                        MultiSelectionItem item = items[i];
                        MultiSelectionItem newMultiSelectionItem = new MultiSelectionItem();
                        newMultiSelectionItem.ItemTitle = item.ItemTitle;
                        newMultiSelectionItem.ItemTitle2 = item.ItemTitle2;
                        newMultiSelectionItem.ItemID = item.ItemID;
                        newMultiSelectionItem.Tag = item.Tag;
                        try
                        {
                            newMultiSelectionItem.Selected = dlgMultiSelect.ListItems[i].Selected;
                        }
                        catch
                        {
                            newMultiSelectionItem.Selected = item.Selected;
                        }

                        result.Add(newMultiSelectionItem);
                    }
                }
                else
                    return null;

                return result;
            }
            finally
            {
                GUIWindowManager.Replace(2100, dlgMultiSelectOld);
            }
        }

        /// <summary>
        /// Displays a text dialog.
        /// </summary>
        public static void ShowTextDialog(string heading, List<string> text)
        {
            if (text == null || text.Count == 0) return;
            ShowTextDialog(heading, string.Join("\n", text.ToArray()));
        }

        /// <summary>
        /// Displays a text dialog.
        /// </summary>
        public static void ShowTextDialog(string heading, string text)
        {
            if (GUIGraphicsContext.form.InvokeRequired)
            {
                ShowTextDialogDelegate d = ShowTextDialog;
                GUIGraphicsContext.form.Invoke(d, heading, text);
                return;
            }

            GUIDialogText dlgText = (GUIDialogText)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_TEXT);

            dlgText.Reset();
            dlgText.SetHeading(heading);
            dlgText.SetText(text);

            dlgText.DoModal(GUIWindowManager.ActiveWindow);
        }

        public static bool GetStringFromKeyboard(ref string strLine)
        {
            return GetStringFromKeyboard(ref strLine, false);
        }

        /// <summary>
        /// Gets the input from the virtual keyboard window.
        /// </summary>
        public static bool GetStringFromKeyboard(ref string strLine, bool isPassword)
        {
            if (GUIGraphicsContext.form.InvokeRequired)
            {
                GetStringFromKeyboardDelegate d = GetStringFromKeyboard;
                object[] args = { strLine, isPassword };
                bool result = (bool)GUIGraphicsContext.form.Invoke(d, args);
                strLine = (string)args[0];
                return result;
            }

            VirtualKeyboard keyboard = (VirtualKeyboard)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_VIRTUAL_KEYBOARD);
            if (keyboard == null) return false;

            keyboard.Reset();
            keyboard.Text = strLine;
            keyboard.Password = isPassword;
            keyboard.DoModal(GUIWindowManager.ActiveWindow);
            
            if (keyboard.IsConfirmed)
            {
                strLine = keyboard.Text;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Shows a Trakt Rate Dialog
        /// </summary>        
        /// <param name="rateObject">Type of object being rated</param>
        public static int ShowRateDialog<T>(T rateObject)
        {
            if (GUIGraphicsContext.form.InvokeRequired)
            {
                ShowRateDialogDelegate<T> d = ShowRateDialog<T>;
                return (int)GUIGraphicsContext.form.Invoke(d, rateObject);
            }

            TraktRateValue currentRating = TraktRateValue.unrate;

            GUIRateDialog ratingDlg = (GUIRateDialog)GUIWindowManager.GetWindow(GUIRateDialog.ID);
            ratingDlg.Reset();
            
            ratingDlg.SetHeading(Translation.RateHeading);

            // if item is not rated, it will default to seven
            if (rateObject is TraktSyncEpisodeRated)
            {
                var item = rateObject as TraktSyncEpisodeRated;
                ratingDlg.SetLine(1, string.Format("{0}x{1} - {2}", item.Season, item.Number, item.Title));
                ratingDlg.Rated = item.Rating == 0 ? TraktRateValue.seven : (TraktRateValue)Convert.ToInt32(item.Rating);
            }
            else if (rateObject is TraktSyncShowRatedEx)
            {
                // for when episode ids are not available we need to sync with both episode and show details
                var item = rateObject as TraktSyncShowRatedEx;
                ratingDlg.SetLine(1, string.Format("{0} - {1}x{2}", item.Title, item.Seasons[0].Number, item.Seasons[0].Episodes[0].Number));
                ratingDlg.Rated = item.Seasons[0].Episodes[0].Rating == 0 ? TraktRateValue.seven : (TraktRateValue)Convert.ToInt32(item.Seasons[0].Episodes[0].Rating);
            }
            else if (rateObject is TraktSyncShowRated)
            {
                var item = rateObject as TraktSyncShowRated;
                ratingDlg.SetLine(1, item.Title);
                ratingDlg.Rated = item.Rating == 0 ? TraktRateValue.seven : (TraktRateValue)Convert.ToInt32(item.Rating);

            }
            else
            {
                var item = rateObject as TraktSyncMovieRated;
                ratingDlg.SetLine(1, item.Title);
                ratingDlg.Rated = item.Rating == 0 ? TraktRateValue.seven : (TraktRateValue)Convert.ToInt32(item.Rating);
            }
            
            // show dialog
            ratingDlg.DoModal(ratingDlg.GetID);
            
            if (!ratingDlg.IsSubmitted) return -1;

            TraktSyncResponse response = null;
            if (rateObject is TraktSyncEpisodeRated)
            {
                var item = rateObject as TraktSyncEpisodeRated;
                currentRating = ratingDlg.Rated;
                item.Rating = (int)currentRating;
                Thread rateThread = new Thread(delegate(object obj)
                {
                    if ((obj as TraktSyncEpisodeRated).Rating > 0)
                    {
                        response = TraktAPI.TraktAPI.AddEpisodeToRatings(obj as TraktSyncEpisodeRated);
                    }
                    else
                    {
                        response = TraktAPI.TraktAPI.RemoveEpisodeFromRatings(obj as TraktEpisode);
                    }
                    TraktLogger.LogTraktResponse(response);
                })
                {
                    IsBackground = true,
                    Name = "Rate"
                };
                rateThread.Start(item);
            }
            else if (rateObject is TraktSyncShowRatedEx)
            {
                // for when episode ids are not available we need to sync with both episode and show details
                var item = rateObject as TraktSyncShowRatedEx;
                currentRating = ratingDlg.Rated;
                item.Seasons[0].Episodes[0].Rating = (int)currentRating;
                Thread rateThread = new Thread(delegate(object obj)
                {
                    if ((obj as TraktSyncShowRatedEx).Seasons[0].Episodes[0].Rating > 0)
                    {
                        response = TraktAPI.TraktAPI.AddEpisodeToRatingsEx(obj as TraktSyncShowRatedEx);
                    }
                    else
                    {
                        response = TraktAPI.TraktAPI.RemoveEpisodeFromRatingsEx(obj as TraktSyncShowRatedEx);
                    }
                    TraktLogger.LogTraktResponse(response);
                })
                {
                    IsBackground = true,
                    Name = "Rate"
                };
                rateThread.Start(item);
            }
            else if (rateObject is TraktSyncShowRated)
            {
                var item = rateObject as TraktSyncShowRated;
                currentRating = ratingDlg.Rated;
                item.Rating = (int)currentRating;
                Thread rateThread = new Thread(delegate(object obj)
                {
                    if ((obj as TraktSyncShowRated).Rating > 0)
                    {
                        response = TraktAPI.TraktAPI.AddShowToRatings(obj as TraktSyncShowRated);
                    }
                    else
                    {
                        response = TraktAPI.TraktAPI.RemoveShowFromRatings(obj as TraktShow);
                    }
                    TraktLogger.LogTraktResponse(response);
                })
                {
                    IsBackground = true,
                    Name = "Rate"
                };
                rateThread.Start(item);
            }
            else
            {
                var item = rateObject as TraktSyncMovieRated;
                currentRating = ratingDlg.Rated;
                item.Rating = (int)currentRating;
                Thread rateThread = new Thread(delegate(object obj)
                {
                    if ((obj as TraktSyncMovieRated).Rating > 0)
                    {
                        response = TraktAPI.TraktAPI.AddMovieToRatings(obj as TraktSyncMovieRated);
                    }
                    else
                    {
                        response = TraktAPI.TraktAPI.RemoveMovieFromRatings(obj as TraktMovie);
                    }
                    TraktLogger.LogTraktResponse(response);
                })
                {
                    IsBackground = true,
                    Name = "Rate"
                };
                rateThread.Start(item);
            }

            return (int)currentRating;
        }
    }    
}
