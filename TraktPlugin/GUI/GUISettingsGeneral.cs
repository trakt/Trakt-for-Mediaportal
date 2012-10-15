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
using MediaPortal.Dialogs;

namespace TraktPlugin.GUI
{
    public class GUISettingsGeneral : GUIWindow
    {
        #region Skin Controls

        enum SkinControls
        {
            DownloadFanart = 2,
            DownloadFullSizeFanart = 3,
            GetFriendRequests = 4,
            CreateMovingPicturesCategories = 5,
            CreateMovingPicturesFilters = 6,
            ShowRateDialogOnWatched = 7,
            SyncRatings = 8
        }

        [SkinControl((int)SkinControls.DownloadFanart)]
        protected GUIToggleButtonControl btnDownloadFanart = null;

        [SkinControl((int)SkinControls.DownloadFullSizeFanart)]
        protected GUIToggleButtonControl btnDownloadFullSizeFanart = null;

        [SkinControl((int)SkinControls.GetFriendRequests)]
        protected GUIToggleButtonControl btnGetFriendRequests = null;

        [SkinControl((int)SkinControls.CreateMovingPicturesCategories)]
        protected GUIToggleButtonControl btnCreateMovingPicturesCategories = null;

        [SkinControl((int)SkinControls.CreateMovingPicturesFilters)]
        protected GUIToggleButtonControl btnCreateMovingPicturesFilters = null;

        [SkinControl((int)SkinControls.ShowRateDialogOnWatched)]
        protected GUIToggleButtonControl btnShowRateDialogOnWatched = null;
        
        [SkinControl((int)SkinControls.SyncRatings)]
        protected GUIToggleButtonControl btnSyncRatings = null;

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
            base.OnPageLoad();

            // Init Properties
            InitProperties();
        }

        protected override void OnPageDestroy(int new_windowId)
        {
            // save settings
            if (btnDownloadFanart != null) TraktSettings.DownloadFanart = btnDownloadFanart.Selected;
            if (btnDownloadFullSizeFanart != null) TraktSettings.DownloadFullSizeFanart = btnDownloadFullSizeFanart.Selected;
            if (btnGetFriendRequests != null) TraktSettings.GetFriendRequestsOnStartup = btnGetFriendRequests.Selected;
            if (btnShowRateDialogOnWatched != null) TraktSettings.ShowRateDialogOnWatched = btnShowRateDialogOnWatched.Selected;
            if (btnSyncRatings != null) TraktSettings.SyncRatings = btnSyncRatings.Selected;

            TraktSettings.saveSettings();

            base.OnPageDestroy(new_windowId);
        }

        protected override void OnClicked(int controlId, GUIControl control, Action.ActionType actionType)
        {
            if (control == btnCreateMovingPicturesCategories)
                CreateMovingPicturesCategoriesClicked();
            if (control == btnCreateMovingPicturesFilters)
                CreateMovingPicturesFiltersClicked();
            base.OnClicked(controlId, control, actionType);
        }

        #endregion

        #region Private Methods

        private void InitProperties()
        {
            // Set States
            if (btnDownloadFanart !=null) btnDownloadFanart.Selected = TraktSettings.DownloadFanart;
            if (btnDownloadFullSizeFanart != null) btnDownloadFullSizeFanart.Selected = TraktSettings.DownloadFullSizeFanart;
            if (btnGetFriendRequests != null) btnGetFriendRequests.Selected = TraktSettings.GetFriendRequestsOnStartup;
            if (btnCreateMovingPicturesCategories != null) btnCreateMovingPicturesCategories.Selected = TraktSettings.MovingPicturesCategories;
            if (btnCreateMovingPicturesFilters != null) btnCreateMovingPicturesFilters.Selected = TraktSettings.MovingPicturesFilters;
            if (btnShowRateDialogOnWatched != null) btnShowRateDialogOnWatched.Selected = TraktSettings.ShowRateDialogOnWatched;
            if (btnSyncRatings != null) btnSyncRatings.Selected = TraktSettings.SyncRatings;

            // Set Labels
            // Properties set by skin in Toggle Buttons do not work in MP 1.1.x!
            if (btnDownloadFanart != null) btnDownloadFanart.Label = Translation.DownloadFanart;
            if (btnDownloadFullSizeFanart != null) btnDownloadFullSizeFanart.Label = Translation.DownloadFullSizeFanart;
            if (btnGetFriendRequests != null) btnGetFriendRequests.Label = Translation.GetFriendRequestsOnStartup;
            if (btnCreateMovingPicturesCategories != null) btnCreateMovingPicturesCategories.Label = Translation.CreateMovingPicturesCategories;
            if (btnCreateMovingPicturesFilters != null) btnCreateMovingPicturesFilters.Label = Translation.CreateMovingPicturesFilters;
            if (btnShowRateDialogOnWatched != null) btnShowRateDialogOnWatched.Label = Translation.ShowRateDialogOnWatched;
            if (btnSyncRatings != null) btnSyncRatings.Label = Translation.SettingSyncRatingsName;
        }

        private void CreateMovingPicturesCategoriesClicked()
        {
            if (TraktHelper.IsMovingPicturesAvailableAndEnabled)
            {
                if (TraktSettings.MovingPicturesCategories)
                {
                    //Remove
                    TraktSettings.MovingPicturesCategories = false;
                    TraktHandlers.MovingPictures.RemoveMovingPicturesCategories();
                }
                else
                {
                    //Add
                    TraktSettings.MovingPicturesCategories = true;
                    BackgroundWorker categoriesCreator = new BackgroundWorker();
                    categoriesCreator.DoWork += new DoWorkEventHandler(categoriesCreator_DoWork);
                    categoriesCreator.RunWorkerAsync();
                }
                btnCreateMovingPicturesCategories.Selected = TraktSettings.MovingPicturesCategories;
            }
            else
            {
                GUIUtils.ShowOKDialog(GUIUtils.PluginName(), Translation.NoMovingPictures);
            }
        }

        void categoriesCreator_DoWork(object sender, DoWorkEventArgs e)
        {
            GUIDialogProgress progressDialog = (GUIDialogProgress)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_PROGRESS);

            progressDialog.Reset();
            progressDialog.ShowWaitCursor = true;
            progressDialog.SetHeading(Translation.CreatingCategories);
            progressDialog.Percentage = 0;
            progressDialog.SetLine(1, string.Empty);
            progressDialog.SetLine(2, string.Empty);
            progressDialog.StartModal(this.GetID);

            GUIWindowManager.Process();

            TraktHandlers.MovingPictures.CreateMovingPicturesCategories();

            progressDialog.SetHeading(Translation.UpdatingCategories);
            GUIWindowManager.Process();

            TraktHandlers.MovingPictures.UpdateMovingPicturesCategories();
            progressDialog.ShowWaitCursor = false;
            progressDialog.Close();
            GUIWindowManager.Process();
        }

        private void CreateMovingPicturesFiltersClicked()
        {
            if (TraktHelper.IsMovingPicturesAvailableAndEnabled)
            {
                if (TraktSettings.MovingPicturesFilters)
                {
                    //Remove
                    TraktSettings.MovingPicturesFilters = false;
                    TraktHandlers.MovingPictures.RemoveMovingPicturesFilters();
                }
                else
                {
                    //Add
                    TraktSettings.MovingPicturesFilters = true;
                    BackgroundWorker filtersCreator = new BackgroundWorker();
                    filtersCreator.DoWork += new DoWorkEventHandler(filtersCreator_DoWork);
                    filtersCreator.RunWorkerAsync();
                }
                btnCreateMovingPicturesFilters.Selected = TraktSettings.MovingPicturesFilters;
            }
            else
            {
                GUIUtils.ShowOKDialog(GUIUtils.PluginName(), Translation.NoMovingPictures);
            }
        }

        void filtersCreator_DoWork(object sender, DoWorkEventArgs e)
        {
            GUIDialogProgress progressDialog = (GUIDialogProgress)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_PROGRESS);

            progressDialog.Reset();
            progressDialog.ShowWaitCursor = true;
            progressDialog.SetHeading(Translation.CreatingFilters);
            progressDialog.Percentage = 0;
            progressDialog.SetLine(1, string.Empty);
            progressDialog.SetLine(2, string.Empty);
            progressDialog.StartModal(this.GetID);

            GUIWindowManager.Process();

            TraktHandlers.MovingPictures.CreateMovingPicturesFilters();

            progressDialog.SetHeading(Translation.UpdatingFilters);
            GUIWindowManager.Process();

            TraktHandlers.MovingPictures.UpdateMovingPicturesFilters();
            progressDialog.ShowWaitCursor = false;
            progressDialog.Close();
            GUIWindowManager.Process();
        }

        #endregion
    }
}
