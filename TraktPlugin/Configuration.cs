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
        TraktPlugin _owner;
        private const string cUsername = "Username";
        private const string cPassword = "Password";
        private const string cStartSync = "Sync Library";
        private const string cStopSync = "Stop";
        private string _username = String.Empty;
        private string _password = String.Empty;

        public Configuration(TraktPlugin owner)
        {
            InitializeComponent();
            _owner = owner;
            LoadConfig();
            tbUsername.Text = _username;
            btnSync.Text = cStartSync;
        }

        private void btnSync_Click(object sender, EventArgs e)
        {
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

        private void UpdateConfig()
        {
            Log.Debug("Trakt: Saving Configuration");
            using (Settings xmlwriter = new MPSettings())
            {
                xmlwriter.SetValue("trakt", cUsername, _username);
                xmlwriter.SetValue("trakt", cPassword, _password);
            }
        }

        public void LoadConfig()
        {
            Log.Debug("Trakt: Loading Configuration");
            using (Settings xmlreader = new MPSettings())
            {
                _username = xmlreader.GetValueAsString("trakt", cUsername, "");
                _password = xmlreader.GetValueAsString("trakt", cPassword, "");
            }

            TraktAPI.Username = _username;
            TraktAPI.Password = _password;
        }

        public void SyncCompleted()
        {
            btnSync.Text = cStartSync;
        }
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
