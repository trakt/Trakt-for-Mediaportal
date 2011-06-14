using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Text.RegularExpressions;
using MediaPortal.Configuration;
using MediaPortal.GUI.Library;
using Action = MediaPortal.GUI.Library.Action;
using MediaPortal.Util;
using TraktPlugin.TraktAPI;
using TraktPlugin.TraktAPI.DataStructures;

namespace TraktPlugin.GUI
{
    public class GUISettingsGeneral : GUIWindow
    {
        #region Skin Controls

        enum SkinControls
        {
            DownloadFanart = 2,
            DownloadFullSizeFanart = 3
        }

        [SkinControl((int)SkinControls.DownloadFanart)]
        protected GUIToggleButtonControl btnDownloadFanart = null;

        [SkinControl((int)SkinControls.DownloadFullSizeFanart)]
        protected GUIToggleButtonControl btnDownloadFullSizeFanart = null;

        #endregion

        #region Constructor

        public GUISettingsGeneral() { }

        #endregion

        #region Base Overrides

        public override int GetID
        {
            get
            {
                return 87274;
            }
        }

        public override bool Init()
        {
            return Load(GUIGraphicsContext.Skin + @"\Trakt.Settings.General.xml");
        }

        protected override void OnPageLoad()
        {
            // Init Properties
            InitProperties();
        }

        protected override void OnPageDestroy(int new_windowId)
        {
            // save settings
            TraktSettings.DownloadFanart = btnDownloadFanart.Selected;
            TraktSettings.DownloadFullSizeFanart = btnDownloadFullSizeFanart.Selected;
            
            TraktSettings.saveSettings();

            base.OnPageDestroy(new_windowId);
        }

        #endregion

        #region Private Methods

        private void InitProperties()
        {
            // Set States
            btnDownloadFanart.Selected = TraktSettings.DownloadFanart;
            btnDownloadFullSizeFanart.Selected = TraktSettings.DownloadFullSizeFanart;

            // Set Labels
            // Properties set by skin in Toggle Buttons do not work in MP 1.1.x!
            btnDownloadFanart.Label = Translation.DownloadFanart;
            btnDownloadFullSizeFanart.Label = Translation.DownloadFullSizeFanart;
        }

        #endregion
    }
}
