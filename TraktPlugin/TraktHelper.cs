using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using MediaPortal.Configuration;
using MediaPortal.Profile;
using TraktPlugin.TraktHandlers;
using TraktPlugin.TraktAPI;
using TraktPlugin.TraktAPI.DataStructures;

namespace TraktPlugin
{
    public class TraktHelper
    {
        #region Plugin Helpers
        public static bool IsPluginEnabled(string name)
        {
            using (Settings xmlreader = new MPSettings())
            {
                return xmlreader.GetValueAsBool("plugins", name, false);
            }
        }

        public static bool IsOnlineVideosAvailableAndEnabled
        {
            get
            {
                return File.Exists(Path.Combine(Config.GetSubFolder(Config.Dir.Plugins, "Windows"), "OnlineVideos.MediaPortal1.dll")) && IsPluginEnabled("Online Videos");
            }
        }

        public static bool IsMovingPicturesAvailableAndEnabled
        {
            get
            {
                return File.Exists(Path.Combine(Config.GetSubFolder(Config.Dir.Plugins, "Windows"), "MovingPictures.dll")) && IsPluginEnabled("Moving Pictures");
            }
        }

        public static bool IsMPTVSeriesAvailableAndEnabled
        {
            get
            {
                return File.Exists(Path.Combine(Config.GetSubFolder(Config.Dir.Plugins, "Windows"), "MP-TVSeries.dll")) && IsPluginEnabled("MP-TV Series");
            }
        }

        public static bool IsMyFilmsAvailableAndEnabled
        {
            get
            {
                return File.Exists(Path.Combine(Config.GetSubFolder(Config.Dir.Plugins, "Windows"), "MyFilms.dll")) && IsPluginEnabled("MyFilms");
            }
        }

        public static bool IsMyAnimeAvailableAndEnabled
        {
            get
            {
                return File.Exists(Path.Combine(Config.GetSubFolder(Config.Dir.Plugins, "Windows"), "Anime2.dll")) && IsPluginEnabled("My Anime");
            }
        }
        #endregion

        #region API Helpers

        public static void AddMovieToWatchList(string title, string year)
        {
            AddMovieToWatchList(title, year, null);
        }

        public static void AddMovieToWatchList(string title, string year, string imdbid)
        {
            AddMovieToWatchList(title, year, imdbid, false);
        }

        public static void AddMovieToWatchList(string title, string year, bool updateMovingPicturesFilters)
        {
            AddMovieToWatchList(title, year, null, updateMovingPicturesFilters);
        }

        /// <summary>
        /// Adds a movie to the current users Watch List
        /// </summary>
        /// <param name="title">title of movie</param>
        /// <param name="year">year of movie</param>
        /// <param name="imdbid">imdbid of movie</param>
        /// <param name="updateMovingPicturesFilters">set to true if movingpictures categories/filters should also be updated</param>
        public static void AddMovieToWatchList(string title, string year, string imdbid, bool updateMovingPicturesFilters)
        {
            if (TraktSettings.AccountStatus != ConnectionState.Connected) return;

            TraktMovieSync syncObject = BasicHandler.CreateMovieSyncData(title, year, imdbid);
            if (syncObject == null) return;

            Thread syncThread = new Thread(delegate(object obj)
            {
                TraktMovieSyncResponse response = TraktAPI.TraktAPI.SyncMovieLibrary(obj as TraktMovieSync, TraktSyncModes.watchlist);
                if (response == null || response.Status != "success") return;
                if (updateMovingPicturesFilters && IsMovingPicturesAvailableAndEnabled)
                {
                    // Update Categories & Filters
                    MovingPictures.ClearWatchListCache();
                    MovingPictures.UpdateCategoriesAndFilters();
                }
                GUI.GUIWatchListMovies.ClearCache(TraktSettings.Username);
            })
            {
                IsBackground = true,
                Name = "Adding Movie to Watch List"
            };

            syncThread.Start(syncObject);
        }

        #endregion
    }
}
