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
    public class GUISettingsAccount : GUIWindow
    {
        #region Skin Controls

        enum SkinControls
        {
            Create = 2,
            Login = 3,
            Disconnect = 4,            
            Title = 10,
            Username = 11,
            Password = 12,
            EmailButton = 13,            
            Ok = 15,
            Cancel = 16,
            TestConnect = 17            
        }

        [SkinControl((int)SkinControls.Create)]
        protected GUIButtonControl btnCreateNewAccount = null;

        [SkinControl((int)SkinControls.Login)]
        protected GUIButtonControl btnLoginExistingAccount = null;

        [SkinControl((int)SkinControls.Disconnect)]
        protected GUIButtonControl btnDisconnectAccount = null;

        [SkinControlAttribute((int)SkinControls.Title)]
        protected GUILabelControl lblTitle = null;

        [SkinControlAttribute((int)SkinControls.TestConnect)]
        protected GUILabelControl lblTestConnect = null;

        [SkinControlAttribute((int)SkinControls.Username)]
        protected GUIButtonControl btnUsername = null;

        [SkinControlAttribute((int)SkinControls.Password)]
        protected GUIButtonControl btnPassword = null;

        [SkinControlAttribute((int)SkinControls.EmailButton)]
        protected GUIButtonControl btnEmail = null;

        [SkinControlAttribute((int)SkinControls.Ok)]
        protected GUIButtonControl btnOk = null;

        [SkinControlAttribute((int)SkinControls.Cancel)]
        protected GUIButtonControl btnCancel = null;

        #endregion

        #region Constructor

        public GUISettingsAccount() { }

        #endregion

        #region Private Variables

        private string Username { get; set; }
        private string Password { get; set; }
        private string Email { get; set; }
        private bool NewAccount { get; set; }

        #endregion

        #region Base Overrides

        public override int GetID
        {
            get
            {
                return 87272;
            }
        }

        public override bool Init()
        {
            return Load(GUIGraphicsContext.Skin + @"\Trakt.Settings.Account.xml");
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
                case ((int)SkinControls.Disconnect):
                    DisconnectAccount();
                    break;

                case ((int)SkinControls.Create):
                    ShowAccountControls(true);
                    break;

                case ((int)SkinControls.Login):
                    bool autoLogin = ShowLoginMenu();                    
                    ShowAccountControls(false);
                    if (autoLogin)
                    {
                        GUIControl.SetControlLabel(GetID, btnUsername.GetID, this.Username);
                        GUIControl.SetControlLabel(GetID, btnPassword.GetID, GetMaskedPassword(this.Password));
                        TraktAccount account = new TraktAccount
                        {
                            Username = this.Username,
                            Password = this.Password
                        };
                        TestAccount(account);
                    }
                    break;

                case ((int)SkinControls.Username):
                    string username = Username;
                    if (GUIUtils.GetStringFromKeyboard(ref username))
                    {
                        Username = username;
                        GUIControl.SetControlLabel(GetID, btnUsername.GetID, username);
                    }
                    break;

                case ((int)SkinControls.Password):
                    string password = Password;
                    if (GUIUtils.GetStringFromKeyboard(ref password, true))
                    {
                        Password = password;
                        GUIControl.SetControlLabel(GetID, btnPassword.GetID, GetMaskedPassword(password));
                    }
                    break;

                case ((int)SkinControls.EmailButton):
                    string email = Email;
                    if (GUIUtils.GetStringFromKeyboard(ref email))
                    {
                        Email = email;
                        GUIControl.SetControlLabel(GetID, btnEmail.GetID, email);
                    }
                    break;

                case ((int)SkinControls.Ok):
                    if (ValidateFields())
                    {
                        TraktAccount account = new TraktAccount
                        {
                            Username = this.Username,
                            Password = this.Password.GetSha1(),
                            Email = this.Email
                        };
                        TestAccount(account);
                    }
                    break;

                case ((int)SkinControls.Cancel):
                    HideAccountControls();
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
                    if (GUIPropertyManager.GetProperty("#Trakt.Settings.Account.Dialog.Visible") == "true")
                    {
                        HideAccountControls();
                        return;
                    }
                    break;
            }
            base.OnAction(action);
        }

        #endregion

        #region Private Methods

        private bool ShowLoginMenu()
        {
            if (TraktSettings.UserLogins.Count == 0) return false;

            // Show List of users to login as
            List<GUIListItem> items = new List<GUIListItem>();

            foreach (var userlogin in TraktSettings.UserLogins)
            {
                items.Add(new GUIListItem { Label = userlogin.Username, Selected = TraktSettings.Username == userlogin.Username });
            }

            // Login new user manually
            items.Add(new GUIListItem { Label = Translation.LoginExistingAccount });            

            int selectedItem = GUIUtils.ShowMenuDialog(Translation.SelectUser, items);
            if (selectedItem == -1 || selectedItem == TraktSettings.UserLogins.Count) return false;

            this.Username = TraktSettings.UserLogins[selectedItem].Username;
            this.Password = TraktSettings.UserLogins[selectedItem].Password;

            return true;
        }

        private string GetMaskedPassword(string password)
        {
            int i = 0;
            string maskedPassword = string.Empty;

            for (i = 0; i < password.Length; i++)
            {
                maskedPassword += "*";
            }

            return maskedPassword;
        }

        private void TestAccount(TraktAccount account)
        {
            TraktResponse response = null;
            if (NewAccount)
            {
                if (lblTestConnect != null)
                    GUIControl.SetControlLabel(GetID, lblTestConnect.GetID, Translation.CreatingAccount);

                GUIWindowManager.Process();
                response = TraktAPI.TraktAPI.CreateAccount(account);
            }
            else
            {
                if (lblTestConnect != null)
                    GUIControl.SetControlLabel(GetID, lblTestConnect.GetID, Translation.SigningIntoAccount);

                GUIWindowManager.Process();
                response = TraktAPI.TraktAPI.TestAccount(account);
            }

            if (response.Status == "failure")
            {
                string errorMessage = string.IsNullOrEmpty(response.Error) ? response.Message : response.Error;
                GUIUtils.ShowNotifyDialog(Translation.Error, errorMessage);
                if (lblTestConnect != null)
                    GUIControl.SetControlLabel(GetID, lblTestConnect.GetID, string.Empty);
            }
            else
            {
                // Save New Account Settings
                TraktSettings.Username = account.Username;
                TraktSettings.Password = account.Password;
                if (!TraktSettings.UserLogins.Exists(u => u.Username == TraktSettings.Username))
                {
                    TraktSettings.UserLogins.Add(new TraktAuthentication { Username = TraktSettings.Username, Password = TraktSettings.Password });
                }
                TraktSettings.AccountStatus = ConnectionState.Connected;
                HideAccountControls();
                InitProperties();

                // clear caches
                // watch lists are stored by user so dont need clearing.
                GUITraktFriends.ClearCache();
                GUICalendar.ClearCache();
                GUIRecommendationsMovies.ClearCache();
                GUIRecommendationsShows.ClearCache();
            }
        }

        private bool ValidateFields()
        {
            bool invalid = false;
            string errorMessage = string.Empty;

            if (string.IsNullOrEmpty(Username))
            {
                errorMessage = Translation.ValidUsername;
                invalid = true;
            }
            else if (string.IsNullOrEmpty(Password))
            {
                errorMessage = Translation.ValidPassword;
                invalid = true;
            }
            else if (NewAccount && !IsValidEmail(Email))
            {
                errorMessage = Translation.ValidEmail;
                invalid = true;
            }

            if (invalid)
            {
                GUIUtils.ShowOKDialog(Translation.Error, errorMessage);
                return false;
            }

            return true;
        }

        private bool IsValidEmail(string emailAddress)
        {
            return Regex.IsMatch(emailAddress,
                   @"^(?("")("".+?""@)|(([0-9a-zA-Z]((\.(?!\.))|[-!#\$%&'\*\+/=\?\^`\{\}\|~\w])*)(?<=[0-9a-zA-Z])@))" +
                   @"(?(\[)(\[(\d{1,3}\.){3}\d{1,3}\])|(([0-9a-zA-Z][-\w]*[0-9a-zA-Z]\.)+[a-zA-Z]{2,6}))$");
        }

        private void HideAccountControls()
        {
            Username = string.Empty;
            Password = string.Empty;
            Email = string.Empty;

            if (btnUsername != null)
                GUIControl.SetControlLabel(GetID, btnUsername.GetID, string.Empty);
            if (btnPassword != null)
                GUIControl.SetControlLabel(GetID, btnPassword.GetID, string.Empty);
            if (btnEmail != null)
                GUIControl.SetControlLabel(GetID, btnEmail.GetID, string.Empty);
            if (lblTestConnect != null)
                GUIControl.SetControlLabel(GetID, lblTestConnect.GetID, string.Empty);
          
            GUIUtils.SetProperty("#Trakt.Settings.Account.Dialog.Visible", "false");
            GUIWindowManager.Process();

            // Set Focus back to main window controls
            if (btnCreateNewAccount != null)
                GUIControl.FocusControl(GetID, btnCreateNewAccount.GetID);
        }

        private void ShowAccountControls(bool newUser)
        {
            // set conditions so skins can show controls for account login/creation
            // there were issues when trying to invoke a virtual keyboard from a dialog
            // ie. (dialog from with-in another dialog) hence the reason why 
            // we are re-using existing window to show controls.
            GUIUtils.SetProperty("#Trakt.Settings.Account.Dialog.Visible", "true");
            GUIUtils.SetProperty("#Trakt.Settings.Account.Dialog.NewUser", newUser.ToString().ToLowerInvariant());
            GUIWindowManager.Process();

            if (btnUsername != null)
                GUIControl.FocusControl(GetID, btnUsername.GetID);
            if (btnOk != null)
                GUIControl.SetControlLabel(GetID, btnOk.GetID, newUser ? Translation.Create : Translation.Login);
            if (lblTitle != null)
                GUIControl.SetControlLabel(GetID, lblTitle.GetID, newUser ? Translation.CreateAccount : Translation.Login);

            NewAccount = newUser;
        }

        private void DisconnectAccount()
        {
            TraktLogger.Info("Disconnecting Account: {0}", TraktSettings.Username);

            // clear account settings
            TraktSettings.Username = string.Empty;
            TraktSettings.Password = string.Empty;
            TraktSettings.AccountStatus = ConnectionState.Disconnected;

            InitProperties();
        }

        private void InitProperties()
        {
            Username = string.Empty;
            Password = string.Empty;
            Email = string.Empty;

            GUIUtils.SetProperty("#Trakt.Settings.Account.Dialog.Visible", "false");
            GUIWindowManager.Process();

            // Set Disconnect button Label / or Hide it
            if (btnDisconnectAccount != null)
            {
                if (TraktSettings.AccountStatus == ConnectionState.Connected)
                {
                    GUIControl.SetControlLabel(GetID, btnDisconnectAccount.GetID, string.Format(Translation.DisconnectAccount, TraktSettings.Username));
                    btnDisconnectAccount.Visible = true;
                }
                else
                {
                    // Hide Control, no account to disconnect
                    btnDisconnectAccount.Visible = false;
                }
            }
        }

        #endregion
    }
}
