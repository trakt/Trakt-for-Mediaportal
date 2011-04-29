using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaPortal.Profile;

namespace TraktPlugin
{
    public static class TraktSettings
    {
        #region Settings
        public static string Username { get; set; }
        public static string Password { get; set; }
        public static int MovingPictures { get; set; }
        public static int TVSeries { get; set; }
        public static int MyVideos { get; set; }
        public static bool KeepTraktLibraryClean { get; set; }
        public static List<String> BlockedFilenames { get; set; }
        public static List<String> BlockedFolders { get; set; }
        public static int LogLevel { get; set; }
        public static int SyncTimerLength { get; set; }
        #endregion

        #region Constants
        private const string cTrakt = "Trakt";
        private const string cUsername = "Username";
        private const string cPassword = "Password";
        private const string cMovingPictures = "MovingPictures";
        private const string cTVSeries = "TVSeries";
        private const string cMyVideos = "MyVideos";
        private const string cKeepTraktLibraryClean = "KeepLibraryClean";
        private const string cBlockedFilenames = "BlockedFilenames";
        private const string cBlockedFolders = "BlockedFolders";
        private const string cSyncTimerLength = "SyncTimerLength";
        #endregion

        /// <summary>
        /// Get Movie Plugin Count
        /// </summary>
        public static int MoviePluginCount
        {
            get
            {
                int count = 0;
                if (MovingPictures >= 0) count++;
                if (MyVideos >= 0) count++;
                return count;
            }
        }
        

        /// <summary>
        /// Loads the Settings
        /// </summary>
        public static void loadSettings()
        {
            TraktLogger.Info("Loading Settings");
            using (Settings xmlreader = new MPSettings())
            {
                Username = xmlreader.GetValueAsString(cTrakt, cUsername, "");
                Password = xmlreader.GetValueAsString(cTrakt, cPassword, "");
                MovingPictures = xmlreader.GetValueAsInt(cTrakt, cMovingPictures, -1);
                TVSeries = xmlreader.GetValueAsInt(cTrakt, cTVSeries, -1);
                MyVideos = xmlreader.GetValueAsInt(cTrakt, cMyVideos, -1);
                KeepTraktLibraryClean = xmlreader.GetValueAsBool(cTrakt, cKeepTraktLibraryClean, false);
                BlockedFilenames = xmlreader.GetValueAsString(cTrakt, cBlockedFilenames, "").FromJSONArray<string>().ToList();
                BlockedFolders = xmlreader.GetValueAsString(cTrakt, cBlockedFolders, "").FromJSONArray<string>().ToList();
                LogLevel = xmlreader.GetValueAsInt("general", "loglevel", 1);
                SyncTimerLength = xmlreader.GetValueAsInt(cTrakt, cSyncTimerLength, 86400000);
            }
        }

        /// <summary>
        /// Saves the Settings
        /// </summary>
        public static void saveSettings()
        {
            TraktLogger.Info("Saving Settings");
            using (Settings xmlwriter = new MPSettings())
            {
                xmlwriter.SetValue(cTrakt, cUsername, Username);
                xmlwriter.SetValue(cTrakt, cPassword, Password);
                xmlwriter.SetValue(cTrakt, cMovingPictures, MovingPictures);
                xmlwriter.SetValue(cTrakt, cTVSeries, TVSeries);
                xmlwriter.SetValue(cTrakt, cMyVideos, MyVideos);
                xmlwriter.SetValueAsBool(cTrakt, cKeepTraktLibraryClean, KeepTraktLibraryClean);
                xmlwriter.SetValue(cTrakt, cBlockedFilenames, BlockedFilenames.ToJSON());
                xmlwriter.SetValue(cTrakt, cBlockedFolders, BlockedFolders.ToJSON());
                xmlwriter.SetValue(cTrakt, cSyncTimerLength, SyncTimerLength);
            }

            Settings.SaveCache();
        }
    }
}
