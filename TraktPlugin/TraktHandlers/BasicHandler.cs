using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TraktPlugin.TraktAPI.DataStructures;

namespace TraktPlugin.TraktHandlers
{
    class BasicHandler
    {
        /// <summary>
        /// Creates Sync Data based on a List of TraktLibraryMovies objects
        /// </summary>
        /// <param name="Movies">The movies to base the object on</param>
        /// <returns>The Trakt Sync data to send</returns>
        public static TraktMovieSync CreateSyncData(List<TraktLibraryMovies> Movies)
        {
            string username = TraktSettings.Username;
            string password = TraktSettings.Password;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                return null;

            List<TraktMovieSync.Movie> moviesList = (from m in Movies
                                                     select new TraktMovieSync.Movie
                                                     {
                                                         IMDBID = m.IMDBID,
                                                         Title = m.Title,
                                                         Year = m.Year.ToString()
                                                     }).ToList();

            TraktMovieSync syncData = new TraktMovieSync
            {
                UserName = username,
                Password = password,
                MovieList = moviesList
            };
            return syncData;
        }
    }
}
