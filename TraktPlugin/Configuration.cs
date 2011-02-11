using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Security.Cryptography;

namespace TraktPlugin
{
    public partial class Configuration : Form
    {

        public Configuration()
        {
            InitializeComponent();
            TraktSettings.loadSettings();
            tbUsername.Text = TraktSettings.Username;
            if (TraktSettings.MovingPictures != -1)
                cbMovingPictures.Checked = true;
            nudMovingPictures.Value = TraktSettings.MovingPictures;

            cbKeepInSync.Checked = TraktSettings.KeepTraktLibraryClean;
        }

        private void tbUsername_TextChanged(object sender, EventArgs e)
        {
            TraktSettings.Username = tbUsername.Text;
            TraktSettings.saveSettings();
        }

        private void tbPassword_TextChanged(object sender, EventArgs e)
        {
            TraktSettings.Password = tbPassword.Text.GetSha1();
            TraktSettings.saveSettings();
        }

        private void cbMovingPictures_CheckedChanged(object sender, EventArgs e)
        {
            if (cbMovingPictures.Checked)
            {
                //Get next highest value for priority

                //We currently only have one plugin supported so it will always be zero (highest)
                nudMovingPictures.Value = 0;
                TraktSettings.MovingPictures = (int)nudMovingPictures.Value;
            }
            else
            {
                //If disabled it is always -1
                nudMovingPictures.Value = -1;
                TraktSettings.MovingPictures = (int)nudMovingPictures.Value;
            }
            TraktSettings.saveSettings();
        }

        private void nudMovingPictures_ValueChanged(object sender, EventArgs e)
        {
            if (nudMovingPictures.Value == -1)
                cbMovingPictures.Checked = false;
            else
                cbMovingPictures.Checked = true;
            TraktSettings.MovingPictures = (int)nudMovingPictures.Value;
            TraktSettings.saveSettings();
        }

        private void cbKeepInSync_CheckedChanged(object sender, EventArgs e)
        {
            //IMPORTANT NOTE on support for more than one library backend for the same video type (i.e movies) we shouldn't keep in sync ever.
            TraktSettings.KeepTraktLibraryClean = cbKeepInSync.Checked;
            TraktSettings.saveSettings();
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            this.Close();
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
