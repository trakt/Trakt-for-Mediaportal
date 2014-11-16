using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaPortal.Player;
using TraktPlugin.TraktAPI.v1;
using TraktPlugin.TraktAPI.v1.DataStructures;

namespace TraktPlugin.TraktHandlers
{
    public enum VideoType
    {
        Movie,
        Series
    }

    public class VideoInfo
    {
        public VideoType Type { get; set; }
        public DateTime StartTime { get; set; }
        public string Title { get; set; }
        public string Year { get; set; }
        public string SeasonIdx { get; set; }
        public string EpisodeIdx { get; set; }
        public double Runtime { get; set; }
        public bool IsScrobbling { get; set; }

        #region overrides
        public override string ToString()
        {
            if (this.Type == VideoType.Series)
                return string.Format("{0} - {1}x{2}", this.Title, this.SeasonIdx, this.EpisodeIdx);
            else
                return string.Format("{0}{1}", this.Title, string.IsNullOrEmpty(this.Year) ? string.Empty : " (" + this.Year + ")");
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            return (this.ToString().Equals(((VideoInfo)obj).ToString()));
        }

        public override int GetHashCode()
        {
            return this.ToString().GetHashCode();
        }
        #endregion
    }

    [Flags]
    internal enum SyncListType
    {
        CustomList = 1,
        Recommendations = 2,
        Watchlist = 4,
        All = 1 | 2 | 4
    }

    /// <summary>
    /// Common code that can be shared between all the plugin handlers for library syncing
    /// </summary>
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

        public static TraktMovieSync CreateMovieSyncData(string title, string year, string imdb)
        {
            return CreateMovieSyncData(title, year, imdb, null);
        }

        /// <summary>
        /// Creates Sync Data based on a single movie
        /// </summary>
        /// <param name="title">Movie Title</param>
        /// <param name="year">Movie Year</param>
        /// <param name="imdb">IMDb Id of movie</param>
        /// <returns>The Trakt Sync data to send</returns>
        public static TraktMovieSync CreateMovieSyncData(string title, string year, string imdb, string tmdb)
        {
            List<TraktMovieSync.Movie> movies = new List<TraktMovieSync.Movie>();

            TraktMovieSync.Movie syncMovie = new TraktMovieSync.Movie
            {
                Title = title,
                Year = year,
                IMDBID = imdb,
                TMDBID = tmdb
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
        /// <param name="imdb">IMDb ID of movie</param>
        /// <returns>Rate Data Object</returns>
        public static TraktRateMovie CreateMovieRateData(string title, string year, string imdb)
        {
            TraktRateMovie rateObject = new TraktRateMovie
            {
                IMDBID = imdb,
                Title = title,
                Year = year,
                Rating = "7",
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
                Rating = "7",
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
                Rating = "7",
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

        public static TraktEpisodeSync CreateEpisodeSyncData(string title, string year, string tvdbid, string seasonidx, string episodeidx)
        {
            TraktEpisodeSync syncData = new TraktEpisodeSync
            {
                SeriesID = tvdbid,
                Title = title,
                EpisodeList = new List<TraktEpisodeSync.Episode>{ new TraktEpisodeSync.Episode { EpisodeIndex = episodeidx, SeasonIndex = seasonidx } },
                UserName = TraktSettings.Username,
                Password = TraktSettings.Password
            };

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
                Year = string.IsNullOrEmpty(year) ? 0 : Convert.ToInt32(year),
                TVDBID = imdb.StartsWith("tt") ? null : imdb
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

        public static TraktEpisodeScrobble CreateEpisodeScrobbleData(VideoInfo info)
        {
            try
            {
                // create scrobble data
                TraktEpisodeScrobble scrobbleData = new TraktEpisodeScrobble
                {
                    Title = info.Title,
                    Year = info.Year,
                    Season = info.SeasonIdx,
                    Episode = info.EpisodeIdx,
                    PluginVersion = TraktSettings.Version,
                    MediaCenter = "Mediaportal",
                    MediaCenterVersion = System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString(),
                    MediaCenterBuildDate = String.Empty,
                    UserName = TraktSettings.Username,
                    Password = TraktSettings.Password
                };

                return scrobbleData;
            }
            catch (Exception e)
            {
                TraktLogger.Error("Error creating scrobble data: {0}", e.Message);
                return null;
            }
        }

        public static TraktMovieScrobble CreateMovieScrobbleData(VideoInfo info)
        {
            try
            {
                // create scrobble data
                TraktMovieScrobble scrobbleData = new TraktMovieScrobble
                {
                    Title = info.Title,
                    Year = info.Year,
                    PluginVersion = TraktSettings.Version,
                    MediaCenter = "Mediaportal",
                    MediaCenterVersion = System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString(),
                    MediaCenterBuildDate = String.Empty,
                    UserName = TraktSettings.Username,
                    Password = TraktSettings.Password
                };

                return scrobbleData;
            }
            catch (Exception e)
            {
                TraktLogger.Error("Error creating scrobble data: {0}", e.Message);
                return null;
            }
        }

        /// <summary>
        /// Scrobbles a movie from a videoInfo object
        /// </summary>
        /// <returns>returns true if successfully scrobbled</returns>
        public static bool ScrobbleMovie(VideoInfo videoInfo, TraktScrobbleStates state)
        {
            TraktMovieScrobble scrobbleData = CreateMovieScrobbleData(videoInfo);
            if (scrobbleData == null) return false;

            // get duration/position in minutes
            double duration = videoInfo.Runtime > 0.0 ? videoInfo.Runtime : g_Player.Duration / 60;
            double position = g_Player.CurrentPosition / 60;
            double progress = 0.0;

            if (duration > 0.0) progress = (position / duration) * 100.0;

            // sometimes with recordings/timeshifting we can get inaccurate player properties
            // adjust if duration is less than a typical movie
            scrobbleData.Duration = (duration < 15.0) ? "60" : Convert.ToInt32(duration).ToString();
            scrobbleData.Progress = (state == TraktScrobbleStates.scrobble) ? "100" : Convert.ToInt32(progress).ToString();

            TraktResponse response = TraktAPI.v1.TraktAPI.ScrobbleMovieState(scrobbleData, state);
            return TraktLogger.LogTraktResponse(response);
        }

        /// <summary>
        /// Scrobbles a episode from a videoInfo object
        /// </summary>
        /// <returns>returns true if successfully scrobbled</returns>
        public static bool ScrobbleEpisode(VideoInfo videoInfo, TraktScrobbleStates state)
        {
            // get scrobble data to send to api
            TraktEpisodeScrobble scrobbleData = CreateEpisodeScrobbleData(videoInfo);
            if (scrobbleData == null) return false;

            // get duration/position in minutes
            double duration = videoInfo.Runtime > 0.0 ? videoInfo.Runtime : g_Player.Duration / 60;
            double position = g_Player.CurrentPosition / 60;
            double progress = 0.0;

            if (duration > 0.0) progress = (position / duration) * 100.0;

            // sometimes with recordings/timeshifting we can get invalid player properties
            // adjust if duration is less than a typical episode
            scrobbleData.Duration = (duration < 10.0) ? "30" : Convert.ToInt32(duration).ToString();
            scrobbleData.Progress = (state == TraktScrobbleStates.scrobble) ? "100" : Convert.ToInt32(progress).ToString();

            TraktResponse response = TraktAPI.v1.TraktAPI.ScrobbleEpisodeState(scrobbleData, state);
            return TraktLogger.LogTraktResponse(response);
        }

        /// <summary>
        /// Validates an IMDb ID
        /// </summary>
        public static bool IsValidImdb(string id)
        {
            if (id == null || !id.StartsWith("tt", StringComparison.InvariantCultureIgnoreCase)) return false;
            if (id.Length != 9) return false;
            return true;
        }

        /// <summary>
        /// Gets a correctly formatted imdb id string        
        /// </summary>
        /// <param name="id">current movie imdb id</param>
        /// <returns>correctly formatted id</returns>
        public static string GetProperImdbId(string id)
        {
            string imdbid = id;

            // handle invalid ids
            // return null so we dont match empty result from trakt
            if (id == null || !id.StartsWith("tt", StringComparison.InvariantCultureIgnoreCase)) return null;

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
                //TODO
                //if (TraktSettings.SkippedMovies == null)
                //    TraktSettings.SkippedMovies = new SyncMovieCheck();

                //TraktLogger.Info("Inserting movie into skipped movie list: Title: {0}, Year: {1}, IMDb: {2}", movie.Title, movie.Year, movie.IMDBID);

                //if (TraktSettings.SkippedMovies.Movies != null)
                //{
                //    if (!TraktSettings.SkippedMovies.Movies.Contains(movie))
                //        TraktSettings.SkippedMovies.Movies.Add(movie);
                //}
                //else
                //{
                //    TraktSettings.SkippedMovies.Movies = new List<TraktMovieSync.Movie>();
                //    TraktSettings.SkippedMovies.Movies.Add(movie);
                //}
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
                //TODO
                //if (TraktSettings.AlreadyExistMovies == null)
                //    TraktSettings.AlreadyExistMovies = new SyncMovieCheck();

                //TraktLogger.Info("Inserting movie into already-exist list: Title: {0}, Year: {1}, IMDb: {2}", movie.Title, movie.Year, movie.IMDBID);

                //if (TraktSettings.AlreadyExistMovies.Movies != null)
                //{
                //    if (!TraktSettings.AlreadyExistMovies.Movies.Contains(movie))
                //        TraktSettings.AlreadyExistMovies.Movies.Add(movie);
                //}
                //else
                //{
                //    TraktSettings.AlreadyExistMovies.Movies = new List<TraktMovieSync.Movie>();
                //    TraktSettings.AlreadyExistMovies.Movies.Add(movie);
                //}
            }
        }
    }
}
