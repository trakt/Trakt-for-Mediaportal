using System;
using System.Collections.Generic;
using MediaPortal.Configuration;
using MediaPortal.Dialogs;
using MediaPortal.GUI.Library;

namespace TraktPlugin.GUI
{
    public static class GUIUtils
    {
        private delegate bool ShowCustomYesNoDialogDelegate(string heading, string lines, string yesLabel, string noLabel, bool defaultYes);
        private delegate void ShowOKDialogDelegate(string heading, string lines);
        private delegate void ShowNotifyDialogDelegate(string heading, string text, string image, string buttonText);
        private delegate int ShowMenuDialogDelegate(string heading, List<GUIListItem> items);
        private delegate void ShowTextDialogDelegate(string heading, string text);

        public static readonly string TraktLogo = GUIGraphicsContext.Skin + "\\Media\\Logos\\trakt.png";

        public static string PluginName()
        {
            return "Trakt";
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
            ShowNotifyDialog(heading, text, TraktLogo, Translation.OK);
        }

        /// <summary>
        /// Displays a notification dialog.
        /// </summary>
        public static void ShowNotifyDialog(string heading, string text, string image)
        {
            ShowNotifyDialog(heading, text, image, Translation.OK);
        }

        /// <summary>
        /// Displays a notification dialog.
        /// </summary>
        public static void ShowNotifyDialog(string heading, string text, string image, string buttonText)
        {
            if (GUIGraphicsContext.form.InvokeRequired)
            {
                ShowNotifyDialogDelegate d = ShowNotifyDialog;
                GUIGraphicsContext.form.Invoke(d, heading, text, image, buttonText);
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

            GUIDialogMenu dlgMenu = (GUIDialogMenu)GUIWindowManager.GetWindow((int)MediaPortal.GUI.Library.GUIWindow.Window.WINDOW_DIALOG_MENU);            

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

        /// <summary>
        /// Gets the input from the virtual keyboard window.
        /// </summary>
        public static bool GetStringFromKeyboard(ref string strLine)
        {
            VirtualKeyboard keyboard = (VirtualKeyboard)GUIWindowManager.GetWindow((int)MediaPortal.GUI.Library.GUIWindow.Window.WINDOW_VIRTUAL_KEYBOARD);
            if (keyboard == null) return false;

            keyboard.Reset();
            keyboard.Text = strLine;
            keyboard.DoModal(GUIWindowManager.ActiveWindow);

            if (keyboard.IsConfirmed)
            {
                strLine = keyboard.Text;
                return true;
            }

            return false;
        }
    }
}
