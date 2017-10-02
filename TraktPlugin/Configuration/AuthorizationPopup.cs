using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using TraktAPI.DataStructures;
using System.Threading;

namespace TraktPlugin
{
    public partial class AuthorizationPopup : Form
    {
        BackgroundWorker AuthWorker = null;

        public AuthorizationPopup()
        {
            InitializeComponent();
        }

        private void AuthorizationPopup_Load(object sender, EventArgs e)
        {
            TraktAPI.TraktAPI.AuthorisationCancelled = false;

            AuthWorker = new BackgroundWorker()
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true
            };

            AuthWorker.RunWorkerCompleted += AuthWorker_RunWorkerCompleted;
            AuthWorker.DoWork += AuthWorker_DoWork;
            
            // properties accessed in worker will never change
            // so dont need to worry about passing them in as an argument
            AuthWorker.RunWorkerAsync();
        }

        private void AuthWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            #region Get Device Code
            TraktLogger.Info("Getting device code from trakt.tv");
            
            var code = TraktAPI.TraktAPI.GetDeviceCode();
            if (code == null || string.IsNullOrEmpty(code.DeviceCode))
            {
                AuthWorker.CancelAsync();
                Close();
            }
            else if (AuthWorker.CancellationPending)
            {
                return;
            }

            lblUserCode.Text = code.UserCode;
            #endregion

            #region Poll for Access Token
            TraktLogger.Info("Successfully got device code from trakt.tv, presenting code '{0}' to user for activation at '{1}'. Code expires in '{2}' secs", code.UserCode, code.VerificationUrl, code.ExpiresIn);
            TraktLogger.Info("Polling trakt.tv for authorization token every '{0}' secs until user enters code", code.Interval);

            var authToken = TraktAPI.TraktAPI.GetAuthenticationToken(code);
            if (authToken != null && !AuthWorker.CancellationPending)
            {
                TraktSettings.UserRefreshToken = authToken.RefreshToken;
                TraktSettings.UserAccessToken = authToken.AccessToken;
                TraktSettings.UserAccessTokenExpiry = DateTime.UtcNow.AddSeconds(authToken.ExpiresIn).ToString();

                TraktLogger.Info("Authorization to use trakt.tv account has been successfull");

                lblUserCode.Text = "SUCCESS";
                lblUserCode.ForeColor = Color.Green;
                Thread.Sleep(3000);
            }
            else
            {
                lblUserCode.Text = "ERROR";
                lblUserCode.ForeColor = Color.Red;
                Thread.Sleep(10000);
            }
            #endregion
        }
        
        private void AuthWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                TraktAPI.TraktAPI.AuthorisationCancelled = true;
                DialogResult = DialogResult.Abort;
                Close();
            }
            else if (e.Error != null)
            {
                TraktAPI.TraktAPI.AuthorisationCancelled = true;

                lblUserCode.Text = "ERROR";
                lblUserCode.ForeColor = Color.Red;
                Thread.Sleep(5000);

                DialogResult = DialogResult.Abort;
                Close();
            }
            else
            {
                DialogResult = DialogResult.OK;
                Close();
            }
        }

        private void AuthorizationPopup_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Check if background worker is doing anything and send a cancellation if it is
            if (AuthWorker.IsBusy)
            {
                TraktLogger.Info("Authorization process cancelled");
                TraktAPI.TraktAPI.AuthorisationCancelled = true;
                AuthWorker.CancelAsync();
            }
        }

        private void lnkActivate_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start(@"https://trakt.tv/activate");
        }
    }
}
