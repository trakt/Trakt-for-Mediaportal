using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using MediaPortal.Core;
using MediaPortal.GUI.Library;

namespace TraktPlugin.Trakt
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

    /// <summary>
    /// Object that communicates with the Trakt API
    /// </summary>
    class TraktAPI
    {
        public static string Username { get; set; }

        public static string Password { get; set; }

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
            Log.Debug(string.Format("Trakt: Scrobble: {0}: {1}", status, scrobbleData.Title));
            Log.Debug(scrobbleData.ToJSON());
            // serialize Scrobble object to JSON and send to server
            string response = Transmit(string.Format(TraktURIs.ScrobbleMovie, status.ToString()), scrobbleData.ToJSON());

            // return success or failure
            return response.FromJSON<TraktResponse>();
        }

        /// <summary>
        /// Sends sync data to Trakt
        /// </summary>
        /// <param name="syncData">The sync data to send</param>
        /// <param name="mode">The sync mode to use</param>
        /// <returns>The response from trakt</returns>
        public static TraktResponse SyncMovieLibrary(TraktSync syncData, TraktSyncModes mode)
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
        /// Gets the trakt movie library for a user
        /// </summary>
        /// <param name="user">The user to get</param>
        /// <returns>The trakt movie library</returns>
        public static IEnumerable<TraktLibraryMovies> GetMoviesForUser(string user)
        {
            //Authorise otherwise we wont get much
            TraktAuth UserAuth = new TraktAuth();
            UserAuth.UserName = Username;
            UserAuth.Password = Password;
            //Get the library
            string moviesForUser = Transmit(string.Format(TraktURIs.UserLibraryMovies, user), UserAuth.ToJSON());
            Log.Debug(moviesForUser);
            //hand it on
            return moviesForUser.FromJSONArray<TraktLibraryMovies>();
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
    }
}
