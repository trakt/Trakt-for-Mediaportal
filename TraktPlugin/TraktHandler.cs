using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TraktPlugin.Trakt;
using MediaPortal.Plugins.MovingPictures.Database;
using MediaPortal.GUI.Library;
using System.Reflection;

namespace TraktPlugin
{
    public static class TraktHandler
    {
        public static bool SyncInProgress = false;

        public static TraktMovieScrobble CreateScrobbleData(DBMovieInfo movie)
        {
            string username = TraktAPI.Username;
            string password = TraktAPI.Password;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                return null;

            TraktMovieScrobble scrobbleData = new TraktMovieScrobble
            {
                Title = movie.Title,
                Year = movie.Year.ToString(),
                IMDBID = movie.ImdbID,
                PluginVersion = Assembly.GetCallingAssembly().GetName().Version.ToString(),
                MediaCenter = "Mediaportal",
                MediaCenterVersion = Assembly.GetEntryAssembly().GetName().Version.ToString(),
                MediaCenterBuildDate = String.Empty,
                UserName = username,
                Password = password
            };
            return scrobbleData;
        }

        public static TraktSync CreateSyncData(List<DBMovieInfo> Movies)
        {
            string username = TraktAPI.Username;
            string password = TraktAPI.Password;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                return null;

            List<TraktSync.Movie> moviesList = (from m in Movies
                                                select new TraktSync.Movie
                                                {
                                                    IMDBID = m.ImdbID,
                                                    Title = m.Title,
                                                    Year = m.Year.ToString()
                                                }).ToList();

            TraktSync syncData = new TraktSync
            {
                UserName = username,
                Password = password,
                MovieList = moviesList
            };
            return syncData;
        }

        public static TraktSync CreateSyncData(List<TraktLibraryMovies> Movies)
        {
            string username = TraktAPI.Username;
            string password = TraktAPI.Password;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                return null;

            List<TraktSync.Movie> moviesList = (from m in Movies
                                                select new TraktSync.Movie
                                                {
                                                    IMDBID = m.IMDBID,
                                                    Title = m.Title,
                                                    Year = m.Year.ToString()
                                                }).ToList();

            TraktSync syncData = new TraktSync
            {
                UserName = username,
                Password = password,
                MovieList = moviesList
            };
            return syncData;
        }

        public static TraktSync CreateSyncData(DBMovieInfo Movie)
        {
            string username = TraktAPI.Username;
            string password = TraktAPI.Password;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                return null;

            List<TraktSync.Movie> moviesList = new List<TraktSync.Movie>();
            moviesList.Add(new TraktSync.Movie { IMDBID = Movie.ImdbID, Title = Movie.Title, Year = Movie.Year.ToString() });

            TraktSync syncData = new TraktSync
            {
                UserName = username,
                Password = password,
                MovieList = moviesList
            };
            return syncData;
        }

        #region Helpers
        private static DateTime getLinkerTimeStamp(string filePath)
        {
            const int PeHeaderOffset = 60;
            const int LinkerTimestampOffset = 8;

            byte[] b = new byte[2047];
            using (System.IO.Stream s = new System.IO.FileStream(filePath, System.IO.FileMode.Open, System.IO.FileAccess.Read))
            {
                s.Read(b, 0, 2047);
            }

            int secondsSince1970 = BitConverter.ToInt32(b, BitConverter.ToInt32(b, PeHeaderOffset) + LinkerTimestampOffset);

            return new DateTime(1970, 1, 1, 0, 0, 0).AddSeconds(secondsSince1970);
        }
        #endregion
    }
}
