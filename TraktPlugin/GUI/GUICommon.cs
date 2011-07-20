using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaPortal.GUI.Library;
using MediaPortal.GUI.Video;
using MediaPortal.Video.Database;

namespace TraktPlugin.GUI
{
    using MyFilmsPlugin.MyFilms;

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
        SettingsGeneral = 87274,
        Shouts = 87280
    }

    enum ExternalPluginWindows
    {
        OnlineVideos = 4755,
        VideoInfo = 2003,
        MovingPictures = 96742,
        TVSeries = 9811,
        MyFilms = 7986
    }

    enum ExternalPluginControls
    {
        WatchList = 97258,
        Rate = 97259,
        Shouts = 97260
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

        /// <summary>
        /// Checks if a selected movie exists locally and plays movie or
        /// jumps to corresponding plugin details view
        /// </summary>
        /// <param name="jumpTo">false if movie should be played directly</param>
        public static void CheckAndPlayMovie(bool jumpTo, string title, int year, string imdbid)
        {
            bool handled = false;

            if (TraktHelper.IsMovingPicturesAvailableAndEnabled)
            {
                int? movieid = null;

                // Find Movie ID in MovingPictures
                // Movie List is now cached internally in MovingPictures so it will be fast
                bool movieExists = TraktHandlers.MovingPictures.FindMovieID(title, year, imdbid, ref movieid);

                if (movieExists)
                {
                    // Loading Parameter only works in MediaPortal 1.2
                    // Load MovingPictures Details view else, directly play movie if using MP 1.1
                    #if MP12
                    if (jumpTo)
                    {
                        string loadingParameter = string.Format("movieid:{0}", movieid);
                        // Open MovingPictures Details view so user can play movie
                        GUIWindowManager.ActivateWindow((int)ExternalPluginWindows.MovingPictures, loadingParameter);
                    }
                    else
                        TraktHandlers.MovingPictures.PlayMovie(movieid);
                    #else
                    TraktHandlers.MovingPictures.PlayMovie(movieid);
                    #endif
                    handled = true;
                }
            }

            // check if its in My Videos database
            if (TraktSettings.MyVideos >= 0 && handled == false)
            {
                IMDBMovie movie = null;
                if (TraktHandlers.MyVideos.FindMovieID(title, year, imdbid, ref movie))
                {
                    // Open My Videos Video Info view so user can play movie
                    if (jumpTo)
                    {
                        GUIVideoInfo videoInfo = (GUIVideoInfo)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_VIDEO_INFO);
                        videoInfo.Movie = movie;
                        GUIWindowManager.ActivateWindow((int)GUIWindow.Window.WINDOW_VIDEO_INFO);
                    }
                    else
                    {
                        GUIVideoFiles.PlayMovie(movie.ID);
                    }
                    handled = true;
                }
            }

            // check if its in My Films database
            if (TraktSettings.MyFilms >= 0 && handled == false)
            {
                MFMovie movie = null;
                if (TraktHandlers.MyFilms.FindMovieID(title, year, imdbid, ref movie))
                {
                    // Open My Videos Video Info view so user can play movie
                    if (jumpTo)
                    {
                        // ToDo: add load param support in MF and add proper string here
                        string loadingParameter = string.Format("movieid:{0}", movie.ID);
                        GUIWindowManager.ActivateWindow((int)ExternalPluginWindows.MyFilms, loadingParameter);
                    }
                    else
                        // ToDo: Add player to MyFilms handler
                        // TraktHandlers.MyFilms.PlayMovie(movieid);
                    handled = true;
                }
            }

        }

        /// <summary>
        /// Checks if a selected episode exists locally and plays episode
        /// </summary>
        /// <param name="seriesidx">the series tvdb id of episode</param>
        /// <param name="seasonidx">the season index of episode</param>
        /// <param name="episodeidx">the episode index of episode</param>
        public static void CheckAndPlayEpisode(int seriesid, int seasonidx, int episodeidx)
        {
            bool handled = false;

            // check if plugin is installed and enabled
            if (TraktHelper.IsMPTVSeriesAvailableAndEnabled)
            {
                // Play episode if it exists
                handled = TraktHandlers.TVSeries.PlayEpisode(seriesid, seasonidx, episodeidx);
            }
        }
    }
}
