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
            DownloadFullSizeFanart = 3,
            GetFriendRequests = 4
        }

        [SkinControl((int)SkinControls.DownloadFanart)]
        protected GUIToggleButtonControl btnDownloadFanart = null;

        [SkinControl((int)SkinControls.DownloadFullSizeFanart)]
        protected GUIToggleButtonControl btnDownloadFullSizeFanart = null;

        [SkinControl((int)SkinControls.GetFriendRequests)]
        protected GUIToggleButtonControl btnGetFriendRequests = null;

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
            if (btnDownloadFanart != null) TraktSettings.DownloadFanart = btnDownloadFanart.Selected;
            if (btnDownloadFullSizeFanart != null) TraktSettings.DownloadFullSizeFanart = btnDownloadFullSizeFanart.Selected;
            if (btnGetFriendRequests != null) TraktSettings.GetFriendRequestsOnStartup = btnGetFriendRequests.Selected;

            TraktSettings.saveSettings();

            base.OnPageDestroy(new_windowId);
        }

        #endregion

        #region Private Methods

        private void InitProperties()
        {
            // Set States
            if (btnDownloadFanart !=null) btnDownloadFanart.Selected = TraktSettings.DownloadFanart;
            if (btnDownloadFullSizeFanart != null) btnDownloadFullSizeFanart.Selected = TraktSettings.DownloadFullSizeFanart;
            if (btnGetFriendRequests != null) btnGetFriendRequests.Selected = TraktSettings.GetFriendRequestsOnStartup;

            // Set Labels
            // Properties set by skin in Toggle Buttons do not work in MP 1.1.x!
            if (btnDownloadFanart != null) btnDownloadFanart.Label = Translation.DownloadFanart;
            if (btnDownloadFullSizeFanart != null) btnDownloadFullSizeFanart.Label = Translation.DownloadFullSizeFanart;
            if (btnGetFriendRequests != null) btnGetFriendRequests.Label = Translation.GetFriendRequestsOnStartup;
        }

        #endregion
    }
}
