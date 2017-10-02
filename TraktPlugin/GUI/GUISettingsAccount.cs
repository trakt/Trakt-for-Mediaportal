using MediaPortal.GUI.Library;
using System;
using TraktAPI.DataStructures;
using TraktAPI.Enums;
using Action = MediaPortal.GUI.Library.Action;

namespace TraktPlugin.GUI
{
    public class GUISettingsAccount : GUIWindow
    {
        #region Skin Controls

        enum SkinControls
        {
            AuthoriseOrDisconnect = 2
        }
        
        [SkinControl((int)SkinControls.AuthoriseOrDisconnect)]
        protected GUIButtonControl btnAuthoriseOrDisconnect = null;
                
        #endregion

        #region Constructor

        public GUISettingsAccount() { }

        #endregion

        #region Private Variables

        #endregion

        #region Base Overrides

        public override int GetID
        {
            get
            {
                return (int)TraktGUIWindows.SettingsAccount;
            }
        }

        public override bool Init()
        {
            return Load(GUIGraphicsContext.Skin + @"\Trakt.Settings.AccountAuth.xml");
        }

        protected override void OnPageLoad()
        {
            base.OnPageLoad();

            // Init Properties
            InitProperties();
        }

        protected override void OnClicked(int controlId, GUIControl control, Action.ActionType actionType)
        {
            // wait for any background action to finish
            if (GUIBackgroundTask.Instance.IsBusy) return;

            switch (controlId)
            {
                // Disconnect
                case ((int)SkinControls.AuthoriseOrDisconnect):
                    if (string.IsNullOrEmpty(TraktSettings.UserAccessToken))
                    {
                        GUIBackgroundTask.Instance.ExecuteInBackgroundAndCallback(() =>
                        {
                            TraktAPI.TraktAPI.AuthorisationCancelled = false;

                            #region Get Device Code
                            TraktLogger.Info("Getting device code from trakt.tv");

                            TraktSettings.AccountStatus = ConnectionState.Connecting;

                            var code = TraktAPI.TraktAPI.GetDeviceCode();
                            if (code == null || string.IsNullOrEmpty(code.DeviceCode))
                            {
                                return null;
                            }
                            #endregion

                            #region Poll for Access Token
                            TraktLogger.Info("Successfully got device code from trakt.tv, presenting code '{0}' to user for activation at '{1}'. Code expires in '{2}' secs", code.UserCode, code.VerificationUrl, code.ExpiresIn);
                            
                            // show the user their user code
                            GUIUtils.SetProperty("#Trakt.Settings.Account.Authorise", "true");
                            GUIUtils.SetProperty("#Trakt.Settings.Account.UserCode", code.UserCode);
                            GUIUtils.SetProperty("#Trakt.Settings.Account.ActivateUrl", code.VerificationUrl);
                            GUIUtils.SetProperty("#Trakt.Settings.Account.UserCode", code.UserCode);
                            GUIUtils.SetProperty("#Trakt.Settings.Account.ScanQRCode", string.Format(Translation.ScanQRCode, code.VerificationUrl, code.UserCode));

                            TraktLogger.Info("Polling trakt.tv for authorisation token every '{0}' secs until user enters code", code.Interval);

                            var authToken = TraktAPI.TraktAPI.GetAuthenticationToken(code);
                            if (authToken != null)
                            {
                                TraktSettings.UserRefreshToken = authToken.RefreshToken;
                                TraktSettings.UserAccessToken = authToken.AccessToken;
                                TraktSettings.UserAccessTokenExpiry = DateTime.UtcNow.AddSeconds(authToken.ExpiresIn).ToString();

                                if (!TestAccount(authToken))
                                    return null;
                            }
                            #endregion

                            return authToken;
                        },
                        delegate (bool success, object result)
                        {
                            GUIUtils.SetProperty("#Trakt.Settings.Account.Authorise", "false");

                            if (success)
                            {
                                var authToken = result as TraktAuthenticationToken;
                                if (authToken == null)
                                {
                                    TraktSettings.AccountStatus = ConnectionState.Disconnected;
                                    GUIUtils.ShowNotifyDialog(Translation.Error, Translation.FailedApplicationAuthorization);
                                }
                                else
                                {
                                    GUIUtils.ShowNotifyDialog(Translation.Login, string.Format(Translation.UserLoginSuccess, TraktSettings.Username));
                                }
                            }
                            
                        }, Translation.AuthorizingApplication, false);
                    }
                    else
                    {
                        DisconnectAccount();
                    }
                    break;
                    
                default:
                    break;
            }
            base.OnClicked(controlId, control, actionType);
        }

        public override void OnAction(Action action)
        {   
            switch (action.wID)
            {
                case Action.ActionType.ACTION_PREVIOUS_MENU:
                    if (GUIBackgroundTask.Instance.IsBusy)
                    {
                        TraktAPI.TraktAPI.AuthorisationCancelled = true;
                        GUIBackgroundTask.Instance.StopBackgroundTask();
                    }
                    break;
            }
            base.OnAction(action);
        }

        #endregion

        #region Private Methods
                
        private bool TestAccount(TraktAuthenticationToken token)
        {
            // test account by requesting the user settings
            var response = TraktAPI.TraktAPI.GetUserSettings();
            
            if (response == null || response.User == null)
            {
                GUIUtils.ShowNotifyDialog(Translation.Error, Translation.FailedOnlineSettings);
                return false;
            }
            else
            {
                // Save New Account Settings
                TraktSettings.Username = response.User.Username;                
                TraktSettings.OnlineSettings = response;

                TraktSettings.AccountStatus = ConnectionState.Connected;
                InitProperties();

                // clear caches
                // watchlists are stored by user so dont need clearing.
                GUINetwork.ClearCache();
                GUICalendar.ClearCache();
                GUIRecommendationsMovies.ClearCache();
                GUIRecommendationsShows.ClearCache();

                // clear any stored user data
                TraktCache.ClearSyncCache();

                // persist settings
                TraktSettings.SaveSettings(false);

                return true;
            }
        }
        
        private void DisconnectAccount()
        {
            TraktLogger.Info("Revoking application access to trakt.tv account");

            GUIBackgroundTask.Instance.ExecuteInBackgroundAndCallback(() =>
            {
                TraktAPI.TraktAPI.RevokeToken();
                return true;
            },
            delegate (bool success, object result)
            {
                if (success)
                {
                    // clear account settings
                    TraktSettings.Username = string.Empty;
                    TraktSettings.UserAccessToken = string.Empty;
                    TraktSettings.UserAccessTokenExpiry = string.Empty;
                    TraktSettings.UserRefreshToken = string.Empty;
                    TraktSettings.AccountStatus = ConnectionState.Disconnected;

                    InitProperties();

                    // clear caches
                    // watchlists are stored by user so dont need clearing.
                    GUINetwork.ClearCache();
                    GUICalendar.ClearCache();
                    GUIRecommendationsMovies.ClearCache();
                    GUIRecommendationsShows.ClearCache();

                    // clear any stored user data
                    TraktCache.ClearSyncCache();

                    // persist settings
                    TraktSettings.SaveSettings(false);
                }

            }, Translation.AuthorizingApplication, false);
        }

        private void InitProperties()
        {
            GUIWindowManager.Process();

            // Show Authorise caption in button or disconnect depending on state
            if (string.IsNullOrEmpty(TraktSettings.UserAccessToken))
            {
                GUIControl.SetControlLabel(GetID, btnAuthoriseOrDisconnect.GetID, Translation.AuthorizeApplication);
            }
            else if (TraktSettings.AccountStatus == ConnectionState.Connected)
            {
                GUIControl.SetControlLabel(GetID, btnAuthoriseOrDisconnect.GetID, string.Format(Translation.DisconnectAccount, TraktSettings.Username));
            }
        }

        #endregion
    }
}
