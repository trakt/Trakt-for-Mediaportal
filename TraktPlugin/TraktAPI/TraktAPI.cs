using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using TraktPlugin.TraktAPI.DataStructures;
using MediaPortal.GUI.Library;
using TraktPlugin.TraktHandlers;

namespace TraktPlugin.TraktAPI
{
    /// <summary>
    /// List of Scrobble States
    /// </summary>
    public enum TraktScrobbleStates
    {
        watching,
        scrobble
    }

    /// <summary>
    /// List of Sync Modes
    /// </summary>
    public enum TraktSyncModes
    {
        library,
        seen,
        unlibrary,
        unseen
    }

    public enum TraktClearingModes
    {
        all,
        movies,
        episodes
    }

    public enum TraktRateType
    {
        episode,
        show,
        movie
    }

    public enum TraktRateValue
    {
        love,
        hate
    }

    /// <summary>
    /// Object that communicates with the Trakt API
    /// </summary>
    class TraktAPI
    {
        public static string UserAgent { get; set; }

        /// <summary>
        /// Sends Scrobble data to Trakt
        /// </summary>
        /// <param name="scrobbleData">The Data to send</param>
        /// <param name="status">The mode to send it as</param>
        /// <returns>The response from Trakt</returns>
        public static TraktResponse ScrobbleMovieState(TraktMovieScrobble scrobbleData, TraktScrobbleStates status)
        {
            // check that we have everything we need
            // server can accept title if movie id is not supplied
            if (scrobbleData == null || string.IsNullOrEmpty(scrobbleData.Title) || string.IsNullOrEmpty(scrobbleData.Year))
            {
                TraktResponse error = new TraktResponse
                {
                    Error = "Not enough information to send to server",
                    Status = "failure"
                };
                return error;
            }
            Log.Info(string.Format("Trakt: Scrobble: {0}: {1}", status, scrobbleData.Title));
            Log.Debug(scrobbleData.ToJSON());
            // serialize Scrobble object to JSON and send to server
            string response = Transmit(string.Format(TraktURIs.ScrobbleMovie, status.ToString()), scrobbleData.ToJSON());

            // return success or failure
            return response.FromJSON<TraktResponse>();
        }

        /// <summary>
        /// Sends Scrobble data to Trakt
        /// </summary>
        /// <param name="scrobbleData">The Data to send</param>
        /// <param name="status">The mode to send it as</param>
        /// <returns>The response from Trakt</returns>
        public static TraktResponse ScrobbleEpisodeState(TraktEpisodeScrobble scrobbleData, TraktScrobbleStates status)
        {
            // check that we have everything we need
            // server can accept title if movie id is not supplied
            if (scrobbleData == null || string.IsNullOrEmpty(scrobbleData.SeriesID))
            {
                TraktResponse error = new TraktResponse
                {
                    Error = "Not enough information to send to server",
                    Status = "failure"
                };
                return error;
            }
            Log.Info(string.Format("Trakt: Scrobble: {0}: {1}", status, scrobbleData.Title));
            Log.Debug(scrobbleData.ToJSON());
            
            // serialize Scrobble object to JSON and send to server
            string response = Transmit(string.Format(TraktURIs.ScrobbleShow, status.ToString()), scrobbleData.ToJSON());

            // return success or failure
            return response.FromJSON<TraktResponse>();
        }

        /// <summary>
        /// Sends movie sync data to Trakt
        /// </summary>
        /// <param name="syncData">The sync data to send</param>
        /// <param name="mode">The sync mode to use</param>
        /// <returns>The response from trakt</returns>
        public static TraktResponse SyncMovieLibrary(TraktMovieSync syncData, TraktSyncModes mode)
        {
            // check that we have everything we need
            // server can accept title/year if imdb id is not supplied
            if (syncData == null || syncData.MovieList.Count == 0)
            {
                TraktResponse error = new TraktResponse
                {
                    Error = "Not enough information to send to server",
                    Status = "failure"
                };
                return error;
            }

            // serialize Scrobble object to JSON and send to server
            string response = Transmit(string.Format(TraktURIs.SyncMovieLibrary, mode.ToString()), syncData.ToJSON());

            // return success or failure
            return response.FromJSON<TraktResponse>();
        }

        /// <summary>
        /// Sends episode sync data to Trakt
        /// </summary>
        /// <param name="syncData">The sync data to send</param>
        /// <param name="mode">The sync mode to use</param>
        public static TraktResponse SyncEpisodeLibrary(TraktEpisodeSync syncData, TraktSyncModes mode)
        {
            // check that we have everything we need
            // server can accept title/year if imdb id is not supplied
            if (syncData == null || string.IsNullOrEmpty(syncData.SeriesID))
            {
                TraktResponse error = new TraktResponse
                {
                    Error = "Not enough information to send to server",
                    Status = "failure"
                };
                return error;
            }

            // serialize Scrobble object to JSON and send to server
            string response = Transmit(string.Format(TraktURIs.SyncEpisodeLibrary, mode.ToString()), syncData.ToJSON());

            // return success or failure
            return response.FromJSON<TraktResponse>();
        }

        /// <summary>
        /// Gets the trakt movie library for a user
        /// </summary>
        /// <param name="user">The user to get</param>
        /// <returns>The trakt movie library</returns>
        public static IEnumerable<TraktLibraryMovies> GetMoviesForUser(string user)
        {
            Log.Info("Trakt: Getting user {0}'s movies", user);
            //Get the library
            string moviesForUser = Transmit(string.Format(TraktURIs.UserLibraryMovies, user), GetUserAuthentication());
            Log.Debug(moviesForUser);
            //hand it on
            return moviesForUser.FromJSONArray<TraktLibraryMovies>();
        }

        /// <summary>
        /// Gets the trakt episode library for a user
        /// </summary>
        /// <param name="user">The user to get</param>
        /// <returns>The trakt episode library</returns>
        public static IEnumerable<TraktLibraryShows> GetLibraryEpisodesForUser(string user)
        {
            Log.Info("Trakt: Getting user {0}'s 'library' episodes", user);
            string showsForUser = Transmit(string.Format(TraktURIs.UserLibraryEpisodes, user), GetUserAuthentication());
            return showsForUser.FromJSONArray<TraktLibraryShows>();
        }

        /// <summary>
        /// Gets the trakt watched/seen episodes for a user
        /// </summary>
        /// <param name="user">The user to get</param>
        /// <returns>The trakt episode library</returns>
        public static IEnumerable<TraktLibraryShows> GetWatchedEpisodesForUser(string user)
        {
            Log.Info("Trakt: Getting user {0}'s 'watched/seen' episodes", user);
            string showsForUser = Transmit(string.Format(TraktURIs.UserWatchedEpisodes, user), GetUserAuthentication());
            return showsForUser.FromJSONArray<TraktLibraryShows>();
        }

        public static TraktRateResponse RateEpisode(TraktRateEpisode episode)
        {
            string response = Transmit(string.Format(TraktURIs.RateItem, TraktRateType.episode.ToString()), episode.ToJSON());
            return response.FromJSON<TraktRateResponse>();
        }

        public static TraktRateResponse RateSeries(TraktRateSeries series)
        {
            string response = Transmit(string.Format(TraktURIs.RateItem, TraktRateType.show.ToString()), series.ToJSON());
            return response.FromJSON<TraktRateResponse>();
        }

        /// <summary>
        /// Gets a User Authentication object
        /// </summary>       
        /// <returns>The User Authentication json string</returns>
        private static string GetUserAuthentication()
        {
            return new TraktAuthentication { Username = TraktSettings.Username, Password = TraktSettings.Password }.ToJSON();
        }

        /// <summary>
        /// Communicates to and from Trakt
        /// </summary>
        /// <param name="address">The URI to use</param>
        /// <param name="data">The Data to send</param>
        /// <returns>The response from Trakt</returns>
        private static string Transmit(string address, string data)
        {
            if (!string.IsNullOrEmpty(data))
            {
                Log.Debug("Trakt Post: " + data);
            }

            try
            {
                WebClient client = new WebClient();
                client.Headers.Add("user-agent", UserAgent);
                return client.UploadString(address, data);
            }
            catch (WebException e)
            {
                // something bad happened e.g. invalid login
                TraktResponse error = new TraktResponse
                {
                    Status = "failure",
                    Error = e.Message
                };
                return error.ToJSON();
            }
        }

        /// <summary>
        /// Clears our library on Trakt as best as the api lets us
        /// </summary>
        /// <param name="mode">What to remove from Trakt</param>
        public static void ClearLibrary(TraktClearingModes mode)
        {
            //Movies
            if (mode == TraktClearingModes.all || mode == TraktClearingModes.movies)
            {
                Log.Info("Trakt: Removing Movies from Trakt");
                Log.Info("Trakt: NOTE WILL NOT REMOVE SCROBBLED MOVIES DUE TO API LIMITATION");
                List<TraktLibraryMovies> movies = GetMoviesForUser(TraktSettings.Username).ToList();

                var syncData = BasicHandler.CreateMovieSyncData(movies);

                Log.Info("Trakt: First removing movies from seen");
                TraktResponse response = SyncMovieLibrary(syncData, TraktSyncModes.unseen);
                LogTraktResponse(response);
                
                Log.Info("Trakt: Now removing movies from library");
                response = SyncMovieLibrary(syncData, TraktSyncModes.unlibrary);
                LogTraktResponse(response);

                Log.Info("Trakt: Removed all movies possible, some manual clean up may be required");
            }

            //Episodes
            if (mode == TraktClearingModes.all || mode == TraktClearingModes.episodes)
            {
                Log.Info("Trakt: Removing Shows from Trakt");
                Log.Info("Trakt: NOTE WILL NOT REMOVE SCROBBLED SHOWS DUE TO API LIMITATION");

                Log.Info("Trakt: First removing shows from seen");
                foreach (var series in GetWatchedEpisodesForUser(TraktSettings.Username).ToList())
                {
                    Log.Info("Trakt: Removing '{0}' from seen", series.ToString());
                    TraktResponse response = SyncEpisodeLibrary(BasicHandler.CreateEpisodeSyncData(series), TraktSyncModes.unseen);
                    LogTraktResponse(response);
                    System.Threading.Thread.Sleep(500);
                }

                Log.Info("Trakt: Now removing shows from library");
                foreach(var series in GetLibraryEpisodesForUser(TraktSettings.Username).ToList())
                {
                    Log.Info("Trakt: Removing '{0}' from library", series.ToString());
                    TraktResponse response = SyncEpisodeLibrary(BasicHandler.CreateEpisodeSyncData(series), TraktSyncModes.unlibrary);
                    LogTraktResponse(response);
                    System.Threading.Thread.Sleep(500);
                }

                Log.Info("Trakt: Removed all shows possible, some manual clean up may be required");
            }
        }


        public static void LogTraktResponse<T>(T response)
        {
            var r = response as TraktResponse;

            if (r == null || r.Status == null)
            {
                Log.Info("Trakt Error: Response from server was unexpected.");
                return;
            }

            // check response error status
            if (r.Status != "success")
            {
                Log.Info("Trakt Error: {0}", r.Error);
            }
            else
            {
                // success
                Log.Info("Trakt Response: {0}", r.Message);
            }
        }
    }
}
