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
        public static TraktMovieSync CreateMovieSyncData(List<TraktLibraryMovies> Movies)
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

        /// <summary>
        /// Creates Sync Data based on a single movie
        /// </summary>
        /// <param name="title">Movie Title</param>
        /// <param name="year">Movie Year</param>
        /// <returns>The Trakt Sync data to send</returns>
        public static TraktMovieSync CreateMovieSyncData(string title, string year)
        {
            if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(year)) return null;

            List<TraktMovieSync.Movie> movies = new List<TraktMovieSync.Movie>();

            TraktMovieSync.Movie syncMovie = new TraktMovieSync.Movie
            {
                Title = title,
                Year = year
            };
            movies.Add(syncMovie);

            TraktMovieSync syncData = new TraktMovieSync
            {
                UserName = TraktSettings.Username,
                Password = TraktSettings.Password,
                MovieList = movies
            };

            return syncData;
        }

        /// <summary>
        /// Creates Sync Data based on a TraktLibraryShows object
        /// </summary>
        /// <param name="show">The show to base the object on</param>
        /// <returns>The Trakt Sync data to send</returns>
        public static TraktEpisodeSync CreateEpisodeSyncData(TraktLibraryShow show)
        {
            TraktEpisodeSync syncData = new TraktEpisodeSync
            {
                SeriesID = show.SeriesId,
                Title = show.Title,
                UserName = TraktSettings.Username,
                Password = TraktSettings.Password
            };

            var episodes = new List<TraktEpisodeSync.Episode>();

            foreach(var season in show.Seasons)
            {
                foreach (var episode in season.Episodes)
                {
                    episodes.Add(new TraktEpisodeSync.Episode
                                     {
                                         EpisodeIndex = episode.ToString(),
                                         SeasonIndex = season.Season.ToString()
                                     });
                }
            }

            syncData.EpisodeList = episodes;

            return syncData;
        }

        public static TraktShowSync CreateShowSyncData(string title, string year)
        {
            if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(year)) return null;

            List<TraktShowSync.Show> shows = new List<TraktShowSync.Show>();

            TraktShowSync.Show syncShow = new TraktShowSync.Show
            {
                Title = title,
                Year = Convert.ToInt32(year)
            };
            shows.Add(syncShow);

            TraktShowSync syncData = new TraktShowSync
            {
                UserName = TraktSettings.Username,
                Password = TraktSettings.Password,
                Shows = shows
            };

            return syncData;
        }
    }
}
