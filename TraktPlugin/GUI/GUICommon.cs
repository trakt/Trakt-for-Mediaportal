using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaPortal.GUI.Library;

namespace TraktPlugin.GUI
{
    enum TraktGUIWindows
    {
        Main = 87258,
        Calendar = 87259,
        Friends = 87260,
        Recommendations = 87261,
        RecommendationsShows = 87262,
        RecommendationsMovies = 87263,
        Trending = 87264,
        TrendingShows = 87265,
        TrendingMovies = 87266,
        WatchedList = 87267,
        WatchedListShows = 87268,
        WatchedListEpisodes = 87269,
        WatchedListMovies = 87270,
        Settings = 87271,
        SettingsAccount = 87272,
        SettingsPlugins = 87273,
        SettingsGeneral = 87274
    }

    enum ExternalPluginWindows
    {
        OnlineVideos = 4755
    }

    enum ExternalPluginControls
    {
        WatchList = 97258
    }

    public class GUICommon
    {
        /// <summary>
        /// Checks if user is logged in, if not the user is presented with
        /// a choice to jump to Account settings and signup/login.
        /// </summary>
        public static bool CheckLogin()
        {
            if (TraktSettings.AccountStatus != TraktAPI.ConnectionState.Connected)
            {
                if (GUIUtils.ShowYesNoDialog(Translation.Login, Translation.NotLoggedIn, true))
                {
                    GUIWindowManager.ActivateWindow((int)TraktGUIWindows.SettingsAccount);                    
                    return false;
                }
                GUIWindowManager.ShowPreviousWindow();
                return false;
            }
            return true;
        }
    }
}
