using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using MediaPortal.Player;
using System.Threading;
using TraktPlugin.GUI;
using TraktPlugin.TraktAPI;
using TraktPlugin.TraktAPI.DataStructures;
using TraktPlugin.TraktAPI.Extensions;

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

        #region Overrides
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
        /// Creates data object for episode scrobbling
        /// </summary>
        internal static TraktScrobbleEpisode CreateEpisodeScrobbleData(VideoInfo info)
        {
            // create scrobble data
            var scrobbleData = new TraktScrobbleEpisode
            {
                Show = new TraktShow
                {
                    Title = info.Title,
                    Year = info.Year.ToNullableInt32()
                },
                Episode = new TraktEpisode
                {
                    Number = int.Parse(info.EpisodeIdx),
                    Season = int.Parse(info.SeasonIdx)
                },
                Progress = GetPlayerProgress(info),
                AppDate = TraktSettings.BuildDate,
                AppVersion = TraktSettings.Version
            };

            return scrobbleData;
        }

        /// <summary>
        /// Creates data object for movie scrobbling
        /// </summary>
        internal static TraktScrobbleMovie CreateMovieScrobbleData(VideoInfo info)
        {
            // create scrobble data
            var scrobbleData = new TraktScrobbleMovie
            {
                Movie = new TraktMovie
                {
                    Title = info.Title,
                    Year = info.Year.ToNullableInt32()
                },
                Progress = GetPlayerProgress(info),
                AppDate = TraktSettings.BuildDate,
                AppVersion = TraktSettings.Version
            };

            return scrobbleData;
        }

        /// <summary>
        /// Gets the Progress of the current video being played
        /// </summary>
        internal static double GetPlayerProgress(VideoInfo videoInfo)
        {
            // get duration/position in minutes
            double duration = videoInfo.Runtime > 0.0 ? videoInfo.Runtime : g_Player.Duration / 60;
            double position = g_Player.CurrentPosition / 60;
            double progress = 0.0;

            if (duration > 0.0)
                progress = (position / duration) * 100.0;

            return Math.Round(progress, 2);
        }

        /// <summary>
        /// Starts scrobbling on trakt.tv
        /// </summary>
        /// <returns>returns true if successfully scrobbled</returns>
        internal static void StartScrobble(VideoInfo videoInfo)
        {
            var scrobbleThread = new Thread((objVideoInfo) =>
            {
                var info = objVideoInfo as VideoInfo;

                if (info.Type == VideoType.Series)
                {
                    StartScrobbleEpisode(info);
                }
                else
                {
                    StartScrobbleMovie(info);
                }
            })
            {
                IsBackground = true,
                Name = "Scrobble"
            };

            scrobbleThread.Start(videoInfo);
        }

        /// <summary>
        /// Stops scrobbing a movie on trakt.tv
        /// </summary>
        /// <param name="watched">Determines if we should force watched on stop</param>
        internal static void StopScrobble(VideoInfo videoInfo, bool watched = false)
        {
            var scrobbleThread = new Thread((objVideoInfo) =>
            {
                var info = objVideoInfo as VideoInfo;
                if (info == null) return;

                if (info.Type == VideoType.Series)
                {
                    StopScrobbleEpisode(info, watched);
                }
                else
                {
                    StopScrobbleMovie(info, watched);
                }
            })
            {
                IsBackground = true,
                Name = "Scrobble"
            };

            scrobbleThread.Start(videoInfo);
        }

        /// <summary>
        /// Starts scrobbling a movie from a videoInfo object
        /// </summary>
        /// <returns>returns true if successfully scrobbled</returns>
        internal static bool StartScrobbleMovie(VideoInfo videoInfo)
        {
            // get scrobble data to send to api
            var scrobbleData = CreateMovieScrobbleData(videoInfo);
            if (scrobbleData == null) return false;

            var response = TraktAPI.TraktAPI.StartMovieScrobble(scrobbleData);
            return TraktLogger.LogTraktResponse(response);
        }

        /// <summary>
        /// Stops scrobbing a movie on trakt.tv
        /// </summary>
        /// <param name="watched">Determines if we should force watched on stop</param>
        internal static bool StopScrobbleMovie(VideoInfo videoInfo, bool watched = false)
        {
            // get scrobble data to send to api
            var scrobbleData = CreateMovieScrobbleData(videoInfo);
            if (scrobbleData == null) return false;

            // force watched
            if (watched && scrobbleData.Progress < 80)
                scrobbleData.Progress = 100;

            var response = TraktAPI.TraktAPI.StopMovieScrobble(scrobbleData);
            return TraktLogger.LogTraktResponse(response);
        }

        /// <summary>
        /// Starts scrobbling an episode on trakt.tv
        /// </summary>
        /// <returns>returns true if successfully scrobbled</returns>
        internal static bool StartScrobbleEpisode(VideoInfo videoInfo)
        {
            // get scrobble data to send to api
            var scrobbleData = CreateEpisodeScrobbleData(videoInfo);
            if (scrobbleData == null) return false;

            var response = TraktAPI.TraktAPI.StartEpisodeScrobble(scrobbleData);
            return TraktLogger.LogTraktResponse(response);
        }

        /// <summary>
        /// Stops scrobbing an episode on trakt.tv
        /// </summary>
        /// <param name="watched">Determines if we should force watched on stop</param>
        internal static bool StopScrobbleEpisode(VideoInfo videoInfo, bool watched = false)
        {
            // get scrobble data to send to api
            var scrobbleData = CreateEpisodeScrobbleData(videoInfo);
            if (scrobbleData == null) return false;

            // force watched
            if (watched && scrobbleData.Progress < 80)
                scrobbleData.Progress = 100;

            var response = TraktAPI.TraktAPI.StopEpisodeScrobble(scrobbleData);
            return TraktLogger.LogTraktResponse(response);
        }

        /// <summary>
        /// Shows the Rate Dialog after playback has ended
        /// </summary>
        /// <param name="episode">The item being rated</param>
        internal static void ShowRateDialog(VideoInfo videoInfo)
        {
            if (!TraktSettings.ShowRateDialogOnWatched) return;     // not enabled            

            TraktLogger.Debug("Showing rate dialog for '{0}'", videoInfo.Title);

            var rateThread = new System.Threading.Thread((o) =>
            {
                var itemToRate = o as VideoInfo;
                if (itemToRate == null) return;

                int rating = 0;

                if (itemToRate.Type == VideoType.Series)
                {
                    var rateObject = new TraktSyncEpisodeRated
                    {
                        Title = itemToRate.Title,
                        Season = int.Parse(itemToRate.SeasonIdx),
                        Number = int.Parse(itemToRate.EpisodeIdx),
                        RatedAt = DateTime.UtcNow.ToISO8601()
                    };
                    // get the rating submitted to trakt
                    rating = GUIUtils.ShowRateDialog<TraktSyncEpisodeRated>(rateObject);
                }
                else if (itemToRate.Type == VideoType.Movie)
                {
                    var rateObject = new TraktSyncMovieRated
                    {
                        Title = itemToRate.Title,
                        Year = itemToRate.Year.ToNullableInt32(),
                        RatedAt = DateTime.UtcNow.ToISO8601()
                    };

                    // get the rating submitted to trakt
                    rating = GUIUtils.ShowRateDialog<TraktSyncMovieRated>(rateObject);
                }

                if (rating > 0)
                {
                    TraktLogger.Debug("Rating {0} as {1}/10", itemToRate.Title, rating.ToString());
                }
            })
            {
                Name = "Rate",
                IsBackground = true
            };
            
            rateThread.Start(videoInfo);
        }

        /// <summary>
        /// Compares two titles of an episode/movie, excluding any year component in the title
        /// </summary>
        /// <param name="title">Title</param>
        /// <param name="otherTitle">Title to compare</param>
        /// <param name="year">Year if included in one of the titles</param>
        /// <returns>True or False if the normalised titles match</returns>
        internal static bool IsTitleMatch(string title, string otherTitle, int? year)
        {
            if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(otherTitle))
                return false;

            if (year != null)
            {
                // remove year from title 
                title = title.Replace(string.Format("({0})", year), string.Empty).Trim();
                otherTitle = otherTitle.Replace(string.Format("({0})", year), string.Empty).Trim();
            }

            return title.ToLowerInvariant() == otherTitle.ToLowerInvariant();
        }

        /// <summary>
        /// Validates an IMDb ID
        /// </summary>
        internal static bool IsValidImdb(string id)
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
        internal static string GetProperImdbId(string id)
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
        /// Gets the Title and Year from a combined title+year string
        /// Title should be in the form 'Title (Year)' or 'Title [Year]'
        /// </summary>
        internal static void GetTitleAndYear(string inputTitle, out string title, out string year)
        {
            Match regMatch = Regex.Match(inputTitle, @"^(?<title>.+?)(?:\s*[\(\[](?<year>\d{4})[\]\)])?$");
            title = regMatch.Groups["title"].Value;
            year = regMatch.Groups["year"].Value;
        }

        /// <summary>
        /// Saves any movies that return as 'skipped' from library sync calls
        /// </summary>
        /// <param name="response">Trakt Sync Movie Response</param>
        internal static void InsertSkippedMovies(TraktSyncResponse response)
        {
            //TODO
            //if (response == null || response.SkippedMovies == null) return;

            //foreach (var movie in response.SkippedMovies)
            //{
            //    if (TraktSettings.SkippedMovies == null)
            //        TraktSettings.SkippedMovies = new SyncMovieCheck();

            //    TraktLogger.Info("Inserting movie into skipped movie list: Title: {0}, Year: {1}, IMDb: {2}", movie.Title, movie.Year, movie.IMDBID);

            //    if (TraktSettings.SkippedMovies.Movies != null)
            //    {
            //        if (!TraktSettings.SkippedMovies.Movies.Contains(movie))
            //            TraktSettings.SkippedMovies.Movies.Add(movie);
            //    }
            //    else
            //    {
            //        TraktSettings.SkippedMovies.Movies = new List<TraktMovieSync.Movie>();
            //        TraktSettings.SkippedMovies.Movies.Add(movie);
            //    }
            //}
        }

        /// <summary>
        /// Saves any movies that return as 'already_exists' from library sync calls
        /// </summary>
        /// <param name="response">Trakt Sync Movie Response</param>
        internal static void InsertAlreadyExistMovies(TraktSyncResponse response)
        {
            //TODO
            //if (response == null || response.AlreadyExistMovies == null) return;

            //foreach (var movie in response.AlreadyExistMovies)
            //{
                
            //    if (TraktSettings.AlreadyExistMovies == null)
            //        TraktSettings.AlreadyExistMovies = new SyncMovieCheck();

            //    TraktLogger.Info("Inserting movie into already-exist list: Title: {0}, Year: {1}, IMDb: {2}", movie.Title, movie.Year, movie.IMDBID);

            //    if (TraktSettings.AlreadyExistMovies.Movies != null)
            //    {
            //        if (!TraktSettings.AlreadyExistMovies.Movies.Contains(movie))
            //            TraktSettings.AlreadyExistMovies.Movies.Add(movie);
            //    }
            //    else
            //    {
            //        TraktSettings.AlreadyExistMovies.Movies = new List<TraktMovieSync.Movie>();
            //        TraktSettings.AlreadyExistMovies.Movies.Add(movie);
            //    }
            //}
        }
    }
}
