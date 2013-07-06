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
            GetFollowerRequests = 4,
            CreateMovingPicturesCategories = 5,
            CreateMovingPicturesFilters = 6,
            ShowRateDialogOnWatched = 7,
            SyncRatings = 8,
            CreateMyFilmsCategories = 9
        }

        [SkinControl((int)SkinControls.DownloadFanart)]
        protected GUICheckButton btnDownloadFanart = null;

        [SkinControl((int)SkinControls.DownloadFullSizeFanart)]
        protected GUICheckButton btnDownloadFullSizeFanart = null;

        [SkinControl((int)SkinControls.GetFollowerRequests)]
        protected GUICheckButton btnGetFollowerRequests = null;

        [SkinControl((int)SkinControls.CreateMovingPicturesCategories)]
        protected GUICheckButton btnCreateMovingPicturesCategories = null;

        [SkinControl((int)SkinControls.CreateMovingPicturesFilters)]
        protected GUICheckButton btnCreateMovingPicturesFilters = null;

        [SkinControl((int)SkinControls.CreateMyFilmsCategories)]
        protected GUICheckButton btnCreateMyFilmsCategories = null;

        [SkinControl((int)SkinControls.ShowRateDialogOnWatched)]
        protected GUICheckButton btnShowRateDialogOnWatched = null;
        
        [SkinControl((int)SkinControls.SyncRatings)]
        protected GUICheckButton btnSyncRatings = null;

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
            if (btnGetFollowerRequests != null) TraktSettings.GetFollowerRequestsOnStartup = btnGetFollowerRequests.Selected;
            if (btnShowRateDialogOnWatched != null) TraktSettings.ShowRateDialogOnWatched = btnShowRateDialogOnWatched.Selected;
            if (btnSyncRatings != null) TraktSettings.SyncRatings = btnSyncRatings.Selected;
            if (btnCreateMyFilmsCategories != null) TraktSettings.MyFilmsCategories = btnCreateMyFilmsCategories.Selected;

            // update any internal plugin settings required
            TraktSettings.UpdateInternalPluginSettings();

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
            if (btnGetFollowerRequests != null) btnGetFollowerRequests.Selected = TraktSettings.GetFollowerRequestsOnStartup;
            if (btnCreateMovingPicturesCategories != null) btnCreateMovingPicturesCategories.Selected = TraktSettings.MovingPicturesCategories;
            if (btnCreateMovingPicturesFilters != null) btnCreateMovingPicturesFilters.Selected = TraktSettings.MovingPicturesFilters;
            if (btnShowRateDialogOnWatched != null) btnShowRateDialogOnWatched.Selected = TraktSettings.ShowRateDialogOnWatched;
            if (btnSyncRatings != null) btnSyncRatings.Selected = TraktSettings.SyncRatings;
            if (btnCreateMyFilmsCategories != null) btnCreateMyFilmsCategories.Selected = TraktSettings.MyFilmsCategories;

            // Set Labels
            // Properties set by skin in Toggle Buttons do not work in MP 1.1.x!
            if (btnDownloadFanart != null) btnDownloadFanart.Label = Translation.DownloadFanart;
            if (btnDownloadFullSizeFanart != null) btnDownloadFullSizeFanart.Label = Translation.DownloadFullSizeFanart;
            if (btnGetFollowerRequests != null) btnGetFollowerRequests.Label = Translation.GetFollowerRequestsOnStartup;
            if (btnCreateMovingPicturesCategories != null) btnCreateMovingPicturesCategories.Label = Translation.CreateMovingPicturesCategories;
            if (btnCreateMovingPicturesFilters != null) btnCreateMovingPicturesFilters.Label = Translation.CreateMovingPicturesFilters;
            if (btnShowRateDialogOnWatched != null) btnShowRateDialogOnWatched.Label = Translation.ShowRateDialogOnWatched;
            if (btnSyncRatings != null) btnSyncRatings.Label = Translation.SettingSyncRatingsName;
            if (btnCreateMyFilmsCategories != null) btnCreateMyFilmsCategories.Label = Translation.CreateMyFilmsCategories;
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
