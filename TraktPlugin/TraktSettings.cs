using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaPortal.GUI.Library;
using MediaPortal.Profile;

namespace TraktPlugin
{
    public static class TraktSettings
    {
        #region Settings
        public static string Username { get; set; }
        public static string Password { get; set; }
        public static int MovingPictures { get; set; }
        public static bool KeepTraktLibraryClean { get; set; }
        #endregion

        #region Constants
        private const string cTrakt = "Trakt";
        private const string cUsername = "Username";
        private const string cPassword = "Password";
        private const string cMovingPictures = "MovingPictures";
        private const string cKeepTraktLibraryClean = "KeepLibraryClean";
        #endregion

        /// <summary>
        /// Loads the Settings
        /// </summary>
        public static void loadSettings()
        {
            Log.Info("Trakt: Loading Settings");
            using (Settings xmlreader = new MPSettings())
            {
                Username = xmlreader.GetValueAsString(cTrakt, cUsername, "");
                Password = xmlreader.GetValueAsString(cTrakt, cPassword, "");
                MovingPictures = xmlreader.GetValueAsInt(cTrakt, cMovingPictures, -1);
                KeepTraktLibraryClean = xmlreader.GetValueAsBool(cTrakt, cKeepTraktLibraryClean, false);
            }
        }

        /// <summary>
        /// Saves the Settings
        /// </summary>
        public static void saveSettings()
        {
            Log.Info("Trakt: Saving Settings");
            using (Settings xmlwriter = new MPSettings())
            {
                xmlwriter.SetValue(cTrakt, cUsername, Username);
                xmlwriter.SetValue(cTrakt, cPassword, Password);
                xmlwriter.SetValue(cTrakt, cMovingPictures, MovingPictures);
                xmlwriter.SetValueAsBool(cTrakt, cKeepTraktLibraryClean, KeepTraktLibraryClean);
            }
        }
    }
}
