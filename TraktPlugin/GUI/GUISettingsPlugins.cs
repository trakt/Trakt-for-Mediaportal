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
using TraktPlugin.TraktAPI.v1;
using TraktPlugin.TraktAPI.v1.DataStructures;

namespace TraktPlugin.GUI
{
    public class GUISettingsPlugins : GUIWindow
    {
        #region Skin Controls

        enum SkinControls
        {
            TVSeries = 2,
            MovingPictures = 3,
            MyVideos = 4,
            MyFilms = 5,
            OnlineVideos = 6,
            MyAnime = 7,
            MyRecordedTV = 8,
            ForTheRecordRecordings = 9,
            MyLiveTV = 10,
            ForTheRecordLiveTV = 11,
            ArgusRecordings = 12,
            ArgusLiveTV = 13
        }

        [SkinControl((int)SkinControls.TVSeries)]
        protected GUICheckButton btnTVSeries = null;

        [SkinControl((int)SkinControls.MovingPictures)]
        protected GUICheckButton btnMovingPictures = null;

        [SkinControl((int)SkinControls.MyVideos)]
        protected GUICheckButton btnMyVideos = null;

        [SkinControl((int)SkinControls.MyFilms)]
        protected GUICheckButton btnMyFilms = null;

        [SkinControl((int)SkinControls.OnlineVideos)]
        protected GUICheckButton btnOnlineVideos = null;

        [SkinControl((int)SkinControls.MyAnime)]
        protected GUICheckButton btnMyAnime = null;

        [SkinControl((int)SkinControls.MyRecordedTV)]
        protected GUICheckButton btnMyRecordedTV = null;

        [SkinControl((int)SkinControls.ForTheRecordRecordings)]
        protected GUICheckButton btnForTheRecordRecordings = null;

        [SkinControl((int)SkinControls.MyLiveTV)]
        protected GUICheckButton btnMyLiveTV = null;

        [SkinControl((int)SkinControls.ForTheRecordLiveTV)]
        protected GUICheckButton btnForTheRecordLiveTV = null;

        [SkinControl((int)SkinControls.ArgusRecordings)]
        protected GUICheckButton btnArgusRecordings = null;

        [SkinControl((int)SkinControls.ArgusLiveTV)]
        protected GUICheckButton btnArgusLiveTV = null;
        #endregion

        #region Constructor

        public GUISettingsPlugins() { }

        #endregion

        #region Private Variables

        int TVSeries { get; set; }
        int MovingPictures { get; set; }
        int MyVideos { get; set; }
        int MyFilms { get; set; }
        int OnlineVideos { get; set; }
        int MyAnime { get; set; }
        int MyRecordedTV { get; set; }
        int ForTheRecordRecordings { get; set; }
        int ArgusRecordings { get; set; }
        int MyLiveTV { get; set; }
        int ForTheRecordLiveTV { get; set; }
        int ArgusLiveTV { get; set; }

        #endregion

        #region Public Variables

        public static bool PluginHandlersChanged { get; set; }
        public static bool PluginHandlersAdded { get; set; }

        #endregion

        #region Base Overrides

        public override int GetID
        {
            get
            {
                return 87273;
            }
        }

        public override bool Init()
        {
            return Load(GUIGraphicsContext.Skin + @"\Trakt.Settings.Plugins.xml");
        }

        protected override void OnPageLoad()
        {
            base.OnPageLoad();

            // Init Properties
            if (!InitProperties())
            {
                // skin has missing features
                GUIUtils.ShowOKDialog(Translation.Error, Translation.SkinPluginsOutOfDate);
                GUIWindowManager.ShowPreviousWindow();
                return;
            }
        }

        protected override void OnPageDestroy(int new_windowId)
        {
            // disable plugins
            if (!btnTVSeries.Selected) TVSeries = -1;
            if (!btnMovingPictures.Selected) MovingPictures = -1;
            if (!btnMyVideos.Selected) MyVideos = -1;
            if (!btnMyFilms.Selected) MyFilms = -1;
            if (!btnOnlineVideos.Selected) OnlineVideos = -1;
            if (!btnMyAnime.Selected) MyAnime = -1;
            if (btnMyRecordedTV != null) { if (!btnMyRecordedTV.Selected) MyRecordedTV = -1; }
            if (btnForTheRecordRecordings != null) { if (!btnForTheRecordRecordings.Selected) ForTheRecordRecordings = -1; }
            if (btnArgusRecordings != null) { if (!btnArgusRecordings.Selected) ArgusRecordings = -1; }
            if (btnMyLiveTV != null) { if (!btnMyLiveTV.Selected) MyLiveTV = -1; }
            if (btnForTheRecordLiveTV != null) { if (!btnForTheRecordLiveTV.Selected) ForTheRecordLiveTV = -1; }
            if (btnArgusLiveTV != null) { if (!btnArgusLiveTV.Selected) ArgusLiveTV = -1; }

            // enable plugins
            int i = 1;
            int[] intArray = new int[12] { TVSeries, MovingPictures, MyVideos, MyFilms, OnlineVideos,
                                          MyAnime, MyRecordedTV, ForTheRecordRecordings, MyLiveTV, 
                                          ForTheRecordLiveTV, ArgusLiveTV, ArgusRecordings };
            Array.Sort(intArray);

            // keep existing sort order
            if (btnTVSeries.Selected && TVSeries < 0) { TVSeries = intArray.Max() + i; i++; }
            if (btnMovingPictures.Selected && MovingPictures < 0) { MovingPictures = intArray.Max() + i; i++; }
            if (btnMyVideos.Selected && MyVideos < 0) { MyVideos = intArray.Max() + i; i++; }
            if (btnMyFilms.Selected && MyFilms < 0) { MyFilms = intArray.Max() + i; i++; }
            if (btnOnlineVideos.Selected && OnlineVideos < 0) { OnlineVideos = intArray.Max() + i; i++; }
            if (btnMyAnime.Selected && MyAnime < 0) { MyAnime = intArray.Max() + i; i++; }
            if (btnMyRecordedTV != null) { if (btnMyRecordedTV.Selected && MyRecordedTV < 0) { MyRecordedTV = intArray.Max() + i; i++; } }
            if (btnForTheRecordRecordings != null) { if (btnForTheRecordRecordings.Selected && ForTheRecordRecordings < 0) { ForTheRecordRecordings = intArray.Max() + i; i++; } }
            if (btnArgusRecordings != null) { if (btnArgusRecordings.Selected && ArgusRecordings < 0) { ArgusRecordings = intArray.Max() + i; i++; } }
            if (btnMyLiveTV != null) { if (btnMyLiveTV.Selected && MyLiveTV < 0) { MyLiveTV = intArray.Max() + i; i++; } }
            if (btnForTheRecordLiveTV != null) { if (btnForTheRecordLiveTV.Selected && ForTheRecordLiveTV < 0) { ForTheRecordLiveTV = intArray.Max() + i; i++; } }
            if (btnArgusLiveTV != null) { if (btnArgusLiveTV.Selected && ArgusLiveTV < 0) { ArgusLiveTV = intArray.Max() + i; i++; } }

            // save settings
            TraktSettings.TVSeries = TVSeries;
            TraktSettings.MovingPictures = MovingPictures;
            TraktSettings.MyVideos = MyVideos;
            TraktSettings.MyFilms = MyFilms;
            TraktSettings.OnlineVideos = OnlineVideos;
            if (btnMyRecordedTV != null) { TraktSettings.MyTVRecordings = MyRecordedTV; }
            if (btnArgusRecordings != null) { TraktSettings.ArgusRecordings = ArgusRecordings; }
            if (btnMyLiveTV != null) { TraktSettings.MyTVLive = MyLiveTV; }
            if (btnArgusLiveTV != null) { TraktSettings.ArgusTVLive = ArgusLiveTV; }

            TraktSettings.SaveSettings();

            base.OnPageDestroy(new_windowId);
        }

        protected override void OnClicked(int controlId, GUIControl control, Action.ActionType actionType)
        {
            // If plugin handlers change or are added, re-load when we exit.
            if (control == btnTVSeries)
            {
                PluginHandlersChanged = true;
                PluginHandlersAdded = TraktSettings.TVSeries == -1;
            }
            if (control == btnMovingPictures)
            {
                PluginHandlersChanged = true;
                PluginHandlersAdded = TraktSettings.MovingPictures == -1;
            }
            if (control == btnMyFilms)
            {
                PluginHandlersChanged = true;
                PluginHandlersAdded = TraktSettings.MyFilms == -1;
            }
            if (control == btnMyVideos)
            {
                PluginHandlersChanged = true;
                PluginHandlersAdded = TraktSettings.MyVideos == -1;
            }
            if (control == btnOnlineVideos)
            {
                PluginHandlersChanged = true;
                PluginHandlersAdded = TraktSettings.OnlineVideos == -1;
            }
            if (control == btnMyRecordedTV)
            {
                PluginHandlersChanged = true;
                PluginHandlersAdded = TraktSettings.MyTVRecordings == -1;
            }
            if (control == btnArgusRecordings)
            {
                PluginHandlersChanged = true;
                PluginHandlersAdded = TraktSettings.ArgusRecordings == -1;
            }
            if (control == btnMyLiveTV)
            {
                PluginHandlersChanged = true;
                PluginHandlersAdded = TraktSettings.MyTVLive == -1;
            }
            if (control == btnArgusLiveTV)
            {
                PluginHandlersChanged = true;
                PluginHandlersAdded = TraktSettings.ArgusTVLive == -1;
            }

            base.OnClicked(controlId, control, actionType);
        }

        #endregion

        #region Private Methods

        private bool InitProperties()
        {
            TVSeries = TraktSettings.TVSeries;
            MovingPictures = TraktSettings.MovingPictures;
            MyVideos = TraktSettings.MyVideos;
            MyFilms = TraktSettings.MyFilms;
            OnlineVideos = TraktSettings.OnlineVideos;
            MyRecordedTV = TraktSettings.MyTVRecordings;
            ArgusRecordings = TraktSettings.ArgusRecordings;
            MyLiveTV = TraktSettings.MyTVLive;
            ArgusLiveTV = TraktSettings.ArgusTVLive;

            try
            {
                if (TVSeries >= 0) btnTVSeries.Selected = true;
                if (MovingPictures >= 0) btnMovingPictures.Selected = true;
                if (MyVideos >= 0) btnMyVideos.Selected = true;
                if (MyFilms >= 0) btnMyFilms.Selected = true;
                if (OnlineVideos >= 0) btnOnlineVideos.Selected = true;
                if (MyAnime >= 0) btnMyAnime.Selected = true;
                if (btnMyRecordedTV != null) { if (MyRecordedTV >= 0) btnMyRecordedTV.Selected = true; }
                if (btnForTheRecordRecordings != null) { if (ForTheRecordRecordings >= 0) btnForTheRecordRecordings.Selected = true; }
                if (btnArgusRecordings != null) { if (ArgusRecordings >= 0) btnArgusRecordings.Selected = true; }
                if (btnMyLiveTV != null) { if (MyLiveTV >= 0) btnMyLiveTV.Selected = true; }
                if (btnForTheRecordLiveTV != null) { if (ForTheRecordLiveTV >= 0) btnForTheRecordLiveTV.Selected = true; }
                if (btnArgusLiveTV != null) { if (ArgusLiveTV >= 0) btnArgusLiveTV.Selected = true; }
            }
            catch
            {
                // Skin out of date!
                return false;
            }

            PluginHandlersChanged = false;
            PluginHandlersAdded = false;

            return true;
        }

        #endregion
    }
}
