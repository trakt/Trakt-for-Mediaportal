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
    public class GUISettingsPlugins : GUIWindow
    {
        #region Skin Controls

        enum SkinControls
        {
            TVSeries = 2,
            MovingPictures = 3,
            MyVideos = 4,            
        }

        [SkinControl((int)SkinControls.TVSeries)]
        protected GUIToggleButtonControl btnTVSeries = null;

        [SkinControl((int)SkinControls.MovingPictures)]
        protected GUIToggleButtonControl btnMovingPictures = null;

        [SkinControl((int)SkinControls.MyVideos)]
        protected GUIToggleButtonControl btnMyVideos = null;

        #endregion

        #region Constructor

        public GUISettingsPlugins() { }

        #endregion

        #region Private Variables

        int TVSeries { get; set; }
        int MovingPictures { get; set; }
        int MyVideos { get; set; }

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
            // Init Properties
            InitProperties();
        }

        protected override void OnPageDestroy(int new_windowId)
        {
            // disable plugins
            if (!btnTVSeries.Selected && TVSeries >= 0) TVSeries = -1;
            if (!btnMovingPictures.Selected && MovingPictures >= 0) MovingPictures = -1;
            if (!btnMyVideos.Selected && MyVideos >= 0) MyVideos = -1;

            // enable plugins
            int i = 1;
            int[] intArray = new int[3] { TVSeries, MovingPictures, MyVideos };
            Array.Sort(intArray);

            // keep existing sort order
            if (btnTVSeries.Selected && TVSeries < 0) { TVSeries = intArray.Max() + i; i++; }
            if (btnMovingPictures.Selected && MovingPictures < 0) { MovingPictures = intArray.Max() + i; i++; }
            if (btnMyVideos.Selected && MyVideos < 0) { MyVideos = intArray.Max() + i; i++; }
            
            // save settings
            TraktSettings.TVSeries = TVSeries;
            TraktSettings.MovingPictures = MovingPictures;
            TraktSettings.MyVideos = MyVideos;

            TraktSettings.saveSettings();

            base.OnPageDestroy(new_windowId);
        }

        #endregion

        #region Private Methods

        private void InitProperties()
        {
            TVSeries = TraktSettings.TVSeries;
            MovingPictures = TraktSettings.MovingPictures;
            MyVideos = TraktSettings.MyVideos;

            if (TVSeries >= 0) btnTVSeries.Selected = true;
            if (MovingPictures >= 0) btnMovingPictures.Selected = true;
            if (MyVideos >= 0) btnMyVideos.Selected = true;
        }

        #endregion
    }
}
