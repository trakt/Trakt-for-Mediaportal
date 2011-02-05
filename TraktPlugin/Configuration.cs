using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Security.Cryptography;
using System.Xml;
using System.IO;
using TraktPlugin.Trakt;
using MediaPortal.GUI.Library;
using MediaPortal.Configuration;
using MediaPortal.UserInterface.Controls;
using MediaPortal.Profile;

namespace TraktPlugin
{
    /// <summary>
    /// Windows form to configure the plugin and complete a full sync
    /// </summary>
    public partial class Configuration : MPConfigForm
    {
        #region Variables
        private TraktPlugin _owner;
        private const string cUsername = "Username";
        private const string cPassword = "Password";
        private const string cStartSync = "Sync Library";
        private const string cStopSync = "Stop";
        private const string cTrakt = "trakt";
        private const string cCompleteSync = "completeSync";
        private string _username = String.Empty;
        private string _password = String.Empty;
        private bool _completeSync = false;
        #endregion

        public Configuration(TraktPlugin owner)
        {
            InitializeComponent();
            _owner = owner;
            LoadConfig();
            tbUsername.Text = _username;
            cbKeepTraktInSync.Checked = _completeSync;
            btnSync.Text = cStartSync;
            ToolTip toolTip = new ToolTip();
            toolTip.SetToolTip(cbKeepTraktInSync, "Will remove items from Trakt that aren't found in or are no longer in your library");
        }

        #region PrivateVoids
        private void btnSync_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_username) || string.IsNullOrEmpty(_password))
            {
                MessageBox.Show("Please enter your username and password first");
                return;
            }
            if (btnSync.Text.CompareTo(cStartSync) == 0)
            {
                MessageBox.Show("Starting Manual Sync");
                _owner.SyncLibrary(pbSync);
                btnSync.Text = cStopSync;
            }
            else
            {
                _owner.StopSync();
                btnSync.Text = cStartSync;
            }
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void tbUsername_TextChanged(object sender, EventArgs e)
        {
            _username = tbUsername.Text;
            UpdateConfig();
        }

        private void tbPassword_TextChanged(object sender, EventArgs e)
        {
            _password = tbPassword.Text.GetSha1();
            UpdateConfig();
        }

        private void cbKeepTraktInSync_CheckedChanged(object sender, EventArgs e)
        {
            _completeSync = cbKeepTraktInSync.Checked;
            UpdateConfig();
        }

        private void LoadToAPI()
        {
            TraktAPI.Username = _username;
            TraktAPI.Password = _password;
            TraktAPI.CompleteSync = _completeSync;
        }

        private void UpdateConfig()
        {
            Log.Debug("Trakt: Saving Configuration");
            using (Settings xmlwriter = new MPSettings())
            {
                xmlwriter.SetValue(cTrakt, cUsername, _username);
                xmlwriter.SetValue(cTrakt, cPassword, _password);
                xmlwriter.SetValueAsBool(cTrakt, cCompleteSync, _completeSync);
            }
            LoadToAPI();
        }
        #endregion

        #region PublicVoids
        public void LoadConfig()
        {
            Log.Debug("Trakt: Loading Configuration");
            using (Settings xmlreader = new MPSettings())
            {
                _username = xmlreader.GetValueAsString(cTrakt, cUsername, "");
                _password = xmlreader.GetValueAsString(cTrakt, cPassword, "");
                _completeSync = xmlreader.GetValueAsBool(cTrakt, cCompleteSync, false);
            }
            LoadToAPI();            
        }
                
        public void SyncCompleted()
        {
            btnSync.Text = cStartSync;
        }
        #endregion
    }

    #region String Extension

    /// <summary>
    /// Creats a SHA1 Hash of a string
    /// </summary>
    public static class SHA1StringExtension
    {
        public static string GetSha1(this string value)
        {
            var data = Encoding.ASCII.GetBytes(value);
            var hashData = new SHA1Managed().ComputeHash(data);

            var hash = string.Empty;

            foreach (var b in hashData)
                hash += b.ToString("X2");

            return hash;
        }
    }
    #endregion
}
