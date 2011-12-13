using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TraktPlugin.TraktAPI.DataStructures;

namespace TraktPlugin.TraktHandlers
{
    public class BasicHandler
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

        public static TraktMovieSync CreateMovieSyncData(string title, string year)
        {
            return CreateMovieSyncData(title, year, null);
        }

        /// <summary>
        /// Creates Sync Data based on a single movie
        /// </summary>
        /// <param name="title">Movie Title</param>
        /// <param name="year">Movie Year</param>
        /// <param name="imdb">IMDb Id of movie</param>
        /// <returns>The Trakt Sync data to send</returns>
        public static TraktMovieSync CreateMovieSyncData(string title, string year, string imdb)
        {
            if (string.IsNullOrEmpty(imdb) && (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(year))) return null;

            List<TraktMovieSync.Movie> movies = new List<TraktMovieSync.Movie>();

            TraktMovieSync.Movie syncMovie = new TraktMovieSync.Movie
            {
                Title = title,
                Year = year,
                IMDBID = imdb
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
        /// Creates Movie Rate Data object
        /// </summary>
        /// <param name="title">Title of Movie</param>
        /// <param name="year">Year of Movie</param>
        /// <returns>Rate Data Object</returns>
        public static TraktRateMovie CreateMovieRateData(string title, string year)
        {
            return CreateMovieRateData(title, year, null);
        }

        /// <summary>
        /// Creates Movie Rate Data object
        /// </summary>
        /// <param name="title">Title of Movie</param>
        /// <param name="year">Year of Movie</param>
        /// <param name="imdb">IMDB ID of movie</param>
        /// <returns>Rate Data Object</returns>
        public static TraktRateMovie CreateMovieRateData(string title, string year, string imdb)
        {
            TraktRateMovie rateObject = new TraktRateMovie
            {
                IMDBID = imdb,
                Title = title,
                Year = year,
                Rating = "love",
                UserName = TraktSettings.Username,
                Password = TraktSettings.Password
            };

            return rateObject;
        }

        public static TraktRateSeries CreateShowRateData(string title, string tvdb)
        {
            TraktRateSeries rateObject = new TraktRateSeries
            {
                Title = title,
                SeriesID = tvdb,
                Rating = "love",
                UserName = TraktSettings.Username,
                Password = TraktSettings.Password
            };

            return rateObject;
        }

        public static TraktRateEpisode CreateEpisodeRateData(string title, string tvdb, string seasonidx, string episodeidx)
        {
            TraktRateEpisode rateObject = new TraktRateEpisode
            {
                Title = title,
                SeriesID = tvdb,
                Episode = episodeidx,
                Season = seasonidx,
                Rating = "love",
                UserName = TraktSettings.Username,
                Password = TraktSettings.Password
            };

            return rateObject;
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
            return CreateShowSyncData(title, year, null);
        }

        public static TraktShowSync CreateShowSyncData(string title, string year, string imdb)
        {
            if (string.IsNullOrEmpty(imdb) && (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(year))) return null;

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

        public static bool IsValidImdb(string id)
        {
            if (id == null || !id.StartsWith("tt")) return false;
            if (id.Length != 9) return false;
            return true;
        }

        /// <summary>
        /// Gets a correctly formatted imdb id string        
        /// </summary>
        /// <param name="id">current movie imdb id</param>
        /// <returns>correctly formatted id</returns>
        public static string GetProperMovieImdbId(string id)
        {
            string imdbid = id;

            // handle invalid ids
            // return null so we dont match empty result from trakt
            if (id == null || !id.StartsWith("tt")) return null;

            // correctly format to 9 char string
            if (id.Length != 9)
            {
                imdbid = string.Format("tt{0}", id.Substring(2).PadLeft(7, '0'));
            }
            return imdbid;
        }

        /// <summary>
        /// Saves any movies that return as 'skipped' from library sync calls
        /// </summary>
        /// <param name="response">Trakt Sync Movie Response</param>
        public static void InsertSkippedMovies(TraktSyncResponse response)
        {
            if (response == null || response.SkippedMovies == null) return;

            foreach (var movie in response.SkippedMovies)
            {
                if (TraktSettings.SkippedMovies == null)
                    TraktSettings.SkippedMovies = new SyncMovieCheck();

                TraktLogger.Info("Inserting movie into skipped movie list: Title: {0}, Year: {1}, IMDb: {2}", movie.Title, movie.Year, movie.IMDBID);

                if (TraktSettings.SkippedMovies.Movies != null)
                {
                    if (!TraktSettings.SkippedMovies.Movies.Contains(movie))
                        TraktSettings.SkippedMovies.Movies.Add(movie);
                }
                else
                {
                    TraktSettings.SkippedMovies.Movies = new List<TraktMovieSync.Movie>();
                    TraktSettings.SkippedMovies.Movies.Add(movie);
                }
            }
        }

        /// <summary>
        /// Saves any movies that return as 'already_exists' from library sync calls
        /// </summary>
        /// <param name="response">Trakt Sync Movie Response</param>
        public static void InsertAlreadyExistMovies(TraktSyncResponse response)
        {
            if (response == null || response.AlreadyExistMovies == null) return;

            foreach (var movie in response.AlreadyExistMovies)
            {
                if (TraktSettings.AlreadyExistMovies == null)
                    TraktSettings.AlreadyExistMovies = new SyncMovieCheck();

                TraktLogger.Info("Inserting movie into already-exist list: Title: {0}, Year: {1}, IMDb: {2}", movie.Title, movie.Year, movie.IMDBID);

                if (TraktSettings.AlreadyExistMovies.Movies != null)
                {
                    if (!TraktSettings.AlreadyExistMovies.Movies.Contains(movie))
                        TraktSettings.AlreadyExistMovies.Movies.Add(movie);
                }
                else
                {
                    TraktSettings.AlreadyExistMovies.Movies = new List<TraktMovieSync.Movie>();
                    TraktSettings.AlreadyExistMovies.Movies.Add(movie);
                }
            }
        }
    }
}
