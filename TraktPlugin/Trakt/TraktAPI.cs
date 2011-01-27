using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using MediaPortal.Core;
using MediaPortal.GUI.Library;

namespace TraktPlugin.Trakt
{

    public enum TraktScrobbleStates
    {
        watching,
        scrobble
    }

    public enum TraktSyncModes
    {
        library,
        seen,
        unlibrary,
        unseen
    }

    class TraktAPI
    {
        public static string Username { get; set; }

        public static string Password { get; set; }

        public static string UserAgent { get; set; }

        public static TraktResponse ScrobbleMovieState(TraktMovieScrobble scrobbleData, TraktScrobbleStates status)
        {
            // check that we have everything we need
            // server can accept title if movie id is not supplied
            if (string.IsNullOrEmpty(scrobbleData.Title) || string.IsNullOrEmpty(scrobbleData.Year))
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

        public static TraktResponse SyncMovieLibrary(TraktSync syncData, TraktSyncModes mode)
        {
            // check that we have everything we need
            // server can accept title/year if imdb id is not supplied
            if (syncData.MovieList.Count == 0)
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

        public static IEnumerable<TraktLibraryMovies> GetMoviesForUser(string user)
        {
            string moviesForUser = Transmit(string.Format(TraktURIs.UserLibraryMovies, user), string.Empty);
            return moviesForUser.FromJSONArray<TraktLibraryMovies>();
        }


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
