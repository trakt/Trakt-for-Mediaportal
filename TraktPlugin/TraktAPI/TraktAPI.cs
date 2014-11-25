using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using TraktPlugin.TraktAPI.DataStructures;
using TraktPlugin.TraktAPI.Extensions;

namespace TraktPlugin.TraktAPI
{
    public static class TraktAPI
    {
        private const string ApplicationId = "d8aed1748b971261dadabba705d85348567579f44ffcec22f8eb8cb982964c78";

        #region Web Events

        // these events can be used to log data sent / received from trakt
        internal delegate void OnDataSendDelegate(string url, string postData);
        internal delegate void OnDataReceivedDelegate(string response);
        internal delegate void OnDataErrorDelegate(string error);

        internal static event OnDataSendDelegate OnDataSend;
        internal static event OnDataReceivedDelegate OnDataReceived;
        internal static event OnDataErrorDelegate OnDataError;

        #endregion

        #region Settings

        // these settings should be set before sending data to trakt
        // exception being the UserToken which is set after logon

        internal static string Username { get; set; }
        internal static string Password { get; set; }
        internal static string UserToken { get; set; }
        internal static string UserAgent { get; set; }
        
        #endregion

        #region Trakt Methods

        #region Authentication

        /// <summary>
        /// Login to trakt and to request a user token for all subsequent requests
        /// </summary>
        /// <returns></returns>
        public static TraktUserToken Login()
        {
            var response = PostToTrakt(TraktURIs.Login, GetUserLogin(), false);
            return response.FromJSON<TraktUserToken>();
        }

        /// <summary>
        /// Gets a User Login object
        /// </summary>       
        /// <returns>The User Login json string</returns>
        private static string GetUserLogin()
        {
            return new TraktAuthentication { Username = TraktAPI.Username, Password = TraktAPI.Password }.ToJSON();
        }

        #endregion

        #region GET Methods

        #region Sync

        public static TraktLastSyncActivities GetLastSyncActivities()
        {
            var response = GetFromTrakt(TraktURIs.SyncLastActivities);
            return response.FromJSON<TraktLastSyncActivities>();
        }

        #endregion

        #region Collection

        public static IEnumerable<TraktMovieCollected> GetCollectedMovies()
        {
            var response = GetFromTrakt(TraktURIs.SyncCollectionMovies);
            
            if (response == null) return null;
            return response.FromJSONArray<TraktMovieCollected>();
        }

        public static IEnumerable<TraktEpisodeCollected> GetCollectedEpisodes()
        {
            var response = GetFromTrakt(TraktURIs.SyncCollectionEpisodes);

            if (response == null) return null;
            return response.FromJSONArray<TraktEpisodeCollected>();
        }

        #endregion

        #region Watched History

        public static IEnumerable<TraktMovieWatched> GetWatchedMovies()
        {
            var response = GetFromTrakt(TraktURIs.SyncWatchedMovies);

            if (response == null) return null;
            return response.FromJSONArray<TraktMovieWatched>();
        }

        public static IEnumerable<TraktEpisodeWatched> GetWatchedEpisodes()
        {
            var response = GetFromTrakt(TraktURIs.SyncWatchedEpisodes);

            if (response == null) return null;
            return response.FromJSONArray<TraktEpisodeWatched>();
        }

        #endregion

        #region Ratings

        public static IEnumerable<TraktMovieRated> GetRatedMovies()
        {
            var response = GetFromTrakt(TraktURIs.SyncRatedMovies);

            if (response == null) return null;
            return response.FromJSONArray<TraktMovieRated>();
        }

        public static IEnumerable<TraktEpisodeRated> GetRatedEpisodes()
        {
            var response = GetFromTrakt(TraktURIs.SyncRatedEpisodes);

            if (response == null) return null;
            return response.FromJSONArray<TraktEpisodeRated>();
        }

        public static IEnumerable<TraktShowRated> GetRatedShows()
        {
            var response = GetFromTrakt(TraktURIs.SyncRatedShows);

            if (response == null) return null;
            return response.FromJSONArray<TraktShowRated>();
        }

        #endregion

        #region Recommendations

        public static IEnumerable<TraktMovie> GetRecommendedMovies()
        {
            var response = GetFromTrakt(TraktURIs.RecommendedMovies);
            return response.FromJSONArray<TraktMovie>();
        }

        public static IEnumerable<TraktShow> GetRecommendedShows()
        {
            var response = GetFromTrakt(TraktURIs.RecommendedShows);
            return response.FromJSONArray<TraktShow>();
        }

        #endregion

        #region User

        /// <summary>
        /// Gets a list of follower requests for the current user
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<TraktFollowerRequest> GetFollowerRequests()
        {
            var response = GetFromTrakt(TraktURIs.UserFollowerRequests);
            return response.FromJSONArray<TraktFollowerRequest>();
        }

        #endregion

        #region Lists

        public static IEnumerable<TraktListDetail> GetUserLists(string username)
        {
            var response = GetFromTrakt(string.Format(TraktURIs.UserLists, username));
            return response.FromJSONArray<TraktListDetail>();
        }

        public static IEnumerable<TraktListItem> GetUserListItems(string username, string listId)
        {
            var response = GetFromTrakt(string.Format(TraktURIs.UserListItems, username, listId));
            return response.FromJSONArray<TraktListItem>();
        }

        #endregion

        #region Watchlists

        public static IEnumerable<TraktWatchListMovie> GetWatchListMovies(string username)
        {
            var response = GetFromTrakt(string.Format(TraktURIs.UserWatchlistMovies, username));
            return response.FromJSONArray<TraktWatchListMovie>();
        }

        public static IEnumerable<TraktWatchListShow> GetWatchListShows(string username)
        {
            var response = GetFromTrakt(string.Format(TraktURIs.UserWatchlistShows, username));
            return response.FromJSONArray<TraktWatchListShow>();
        }

        public static IEnumerable<TraktWatchListEpisode> GetWatchListEpisodes(string username)
        {
            var response = GetFromTrakt(string.Format(TraktURIs.UserWatchlistEpisodes, username));
            return response.FromJSONArray<TraktWatchListEpisode>();
        }

        #endregion

        #region Movies

        #region Related

        public static IEnumerable<TraktMovie> GetRelatedMovies(string id)
        {
            var response = GetFromTrakt(string.Format(TraktURIs.RelatedMovies, id));
            return response.FromJSONArray<TraktMovie>();
        }

        #endregion

        #endregion

        #region Shows

        #region Related

        public static IEnumerable<TraktShow> GetRelatedShows(string id)
        {
            var response = GetFromTrakt(string.Format(TraktURIs.RelatedShows, id));
            return response.FromJSONArray<TraktShow>();
        }

        #endregion

        #endregion

        #endregion

        #region POST Methods

        #region Collection

        public static TraktSyncResponse AddMoviesToCollecton(TraktSyncMoviesCollected movies)
        {
            var response = PostToTrakt(TraktURIs.SyncCollectionAdd, movies.ToJSON());
            return response.FromJSON<TraktSyncResponse>();
        }

        public static TraktSyncResponse RemoveMoviesFromCollecton(TraktSyncMovies movies)
        {
            var response = PostToTrakt(TraktURIs.SyncCollectionRemove, movies.ToJSON());
            return response.FromJSON<TraktSyncResponse>();
        }

        public static TraktSyncResponse AddEpisodesToCollecton(TraktSyncEpisodesCollected episodes)
        {
            var response = PostToTrakt(TraktURIs.SyncCollectionAdd, episodes.ToJSON());
            return response.FromJSON<TraktSyncResponse>();
        }

        public static TraktSyncResponse AddShowsToCollectonEx(TraktSyncShowsCollectedEx shows)
        {
            var response = PostToTrakt(TraktURIs.SyncCollectionAdd, shows.ToJSON());
            return response.FromJSON<TraktSyncResponse>();
        }

        public static TraktSyncResponse RemoveEpisodesFromCollecton(TraktSyncEpisodes episodes)
        {
            var response = PostToTrakt(TraktURIs.SyncCollectionRemove, episodes.ToJSON());
            return response.FromJSON<TraktSyncResponse>();
        }

        public static TraktSyncResponse RemoveShowsFromCollectonEx(TraktSyncShowsEx shows)
        {
            var response = PostToTrakt(TraktURIs.SyncCollectionRemove, shows.ToJSON());
            return response.FromJSON<TraktSyncResponse>();
        }

        #endregion

        #region Collection (Single)

        public static TraktSyncResponse AddMovieToCollecton(TraktSyncMovieCollected movie)
        {
            var movies = new TraktSyncMoviesCollected
            {
                Movies = new List<TraktSyncMovieCollected>() { movie }
            };

            return AddMoviesToCollecton(movies);
        }

        public static TraktSyncResponse RemoveMovieFromCollecton(TraktMovie movie)
        {
            var movies = new TraktSyncMovies
            {
                Movies = new List<TraktMovie>() { movie }
            };

            return RemoveMoviesFromCollecton(movies);
        }

        #endregion

        #region Watched History

        public static TraktSyncResponse AddMoviesToWatchedHistory(TraktSyncMoviesWatched movies)
        {
            var response = PostToTrakt(TraktURIs.SyncWatchedHistoryAdd, movies.ToJSON());
            return response.FromJSON<TraktSyncResponse>();
        }

        public static TraktSyncResponse RemoveMoviesFromWatchedHistory(TraktSyncMovies movies)
        {
            var response = PostToTrakt(TraktURIs.SyncWatchedHistoryRemove, movies.ToJSON());
            return response.FromJSON<TraktSyncResponse>();
        }

        public static TraktSyncResponse AddEpisodesToWatchedHistory(TraktSyncEpisodesWatched episodes)
        {
            var response = PostToTrakt(TraktURIs.SyncWatchedHistoryAdd, episodes.ToJSON());
            return response.FromJSON<TraktSyncResponse>();
        }

        public static TraktSyncResponse AddShowsToWatchedHistoryEx(TraktSyncShowsWatchedEx shows)
        {
            var response = PostToTrakt(TraktURIs.SyncWatchedHistoryAdd, shows.ToJSON());
            return response.FromJSON<TraktSyncResponse>();
        }

        public static TraktSyncResponse RemoveEpisodesFromWatchedHistory(TraktSyncEpisodes episodes)
        {
            var response = PostToTrakt(TraktURIs.SyncWatchedHistoryRemove, episodes.ToJSON());
            return response.FromJSON<TraktSyncResponse>();
        }

        public static TraktSyncResponse RemoveShowsFromWatchedHistoryEx(TraktSyncShowsEx shows)
        {
            var response = PostToTrakt(TraktURIs.SyncWatchedHistoryRemove, shows.ToJSON());
            return response.FromJSON<TraktSyncResponse>();
        }

        #endregion

        #region Watched History (Single)

        public static TraktSyncResponse AddMovieToWatchedHistory(TraktSyncMovieWatched movie)
        {
            var movies = new TraktSyncMoviesWatched
            {
                Movies = new List<TraktSyncMovieWatched>() { movie }
            };

            return AddMoviesToWatchedHistory(movies);
        }

        public static TraktSyncResponse RemoveMovieFromWatchedHistory(TraktMovie movie)
        {
            var movies = new TraktSyncMovies
            {
                Movies = new List<TraktMovie>() { movie }
            };

            return RemoveMoviesFromWatchedHistory(movies);
        }

        #endregion

        #region Ratings

        public static TraktSyncResponse AddMoviesToRatings(TraktSyncMoviesRated movies)
        {
            var response = PostToTrakt(TraktURIs.SyncRatingsAdd, movies.ToJSON());
            return response.FromJSON<TraktSyncResponse>();
        }

        public static TraktSyncResponse RemoveMoviesFromRatings(TraktSyncMovies movies)
        {
            var response = PostToTrakt(TraktURIs.SyncRatingsRemove, movies.ToJSON());
            return response.FromJSON<TraktSyncResponse>();
        }

        public static TraktSyncResponse AddShowsToRatings(TraktSyncShowsRated shows)
        {
            var response = PostToTrakt(TraktURIs.SyncRatingsAdd, shows.ToJSON());
            return response.FromJSON<TraktSyncResponse>();
        }

        public static TraktSyncResponse RemoveShowsFromRatings(TraktSyncShows shows)
        {
            var response = PostToTrakt(TraktURIs.SyncRatingsRemove, shows.ToJSON());
            return response.FromJSON<TraktSyncResponse>();
        }

        public static TraktSyncResponse AddEpisodesToRatings(TraktSyncEpisodesRated episodes)
        {
            var response = PostToTrakt(TraktURIs.SyncRatingsAdd, episodes.ToJSON());
            return response.FromJSON<TraktSyncResponse>();
        }

        public static TraktSyncResponse AddShowsToRatingsEx(TraktSyncShowsRatedEx shows)
        {
            var response = PostToTrakt(TraktURIs.SyncRatingsAdd, shows.ToJSON());
            return response.FromJSON<TraktSyncResponse>();
        }

        public static TraktSyncResponse RemoveEpisodesFromRatings(TraktSyncEpisodes episodes)
        {
            var response = PostToTrakt(TraktURIs.SyncRatingsRemove, episodes.ToJSON());
            return response.FromJSON<TraktSyncResponse>();
        }

        public static TraktSyncResponse RemoveShowsFromRatingsEx(TraktSyncShowsRatedEx shows)
        {
            var response = PostToTrakt(TraktURIs.SyncRatingsRemove, shows.ToJSON());
            return response.FromJSON<TraktSyncResponse>();
        }

        #endregion

        #region Ratings (Single)

        /// <summary>
        /// Rate a single episode on trakt.tv
        /// </summary>
        public static TraktSyncResponse AddEpisodeToRatings(TraktSyncEpisodeRated episode)
        {
            var episodes = new TraktSyncEpisodesRated
            {
                Episodes = new List<TraktSyncEpisodeRated>() { episode }
            };

            return AddEpisodesToRatings(episodes);
        }

        /// <summary>
        /// UnRate a single episode on trakt.tv
        /// </summary>
        public static TraktSyncResponse RemoveEpisodeFromRatings(TraktEpisode episode)
        {
            var episodes = new TraktSyncEpisodes
            {
                Episodes = new List<TraktEpisode>() { episode }
            };

            return RemoveEpisodesFromRatings(episodes);
        }

        /// <summary>
        /// Rate a single episode on trakt.tv (with show info)
        /// </summary>
        public static TraktSyncResponse AddEpisodeToRatingsEx(TraktSyncShowRatedEx item)
        {
            var episodes = new TraktSyncShowsRatedEx
            {
                Shows = new List<TraktSyncShowRatedEx>() { item }
            };

            return AddShowsToRatingsEx(episodes);
        }

        /// <summary>
        /// UnRate a single episode on trakt.tv (with show info)
        /// </summary>
        public static TraktSyncResponse RemoveEpisodeFromRatingsEx(TraktSyncShowRatedEx item)
        {
            var episodes = new TraktSyncShowsRatedEx
            {
                Shows = new List<TraktSyncShowRatedEx>() { item }
            };

            return RemoveShowsFromRatingsEx(episodes);
        }

        /// <summary>
        /// Rate a single show on trakt.tv
        /// </summary>
        public static TraktSyncResponse AddShowToRatings(TraktSyncShowRated show)
        {
            var shows = new TraktSyncShowsRated
            {
                Shows = new List<TraktSyncShowRated>() { show }
            };

            return AddShowsToRatings(shows);
        }

        /// <summary>
        /// UnRate a single show on trakt.tv
        /// </summary>
        public static TraktSyncResponse RemoveShowFromRatings(TraktShow show)
        {
            var shows = new TraktSyncShows
            {
                Shows = new List<TraktShow>() { show }
            };

            return RemoveShowsFromRatings(shows);
        }

        /// <summary>
        /// Rate a single movie on trakt.tv
        /// </summary>
        public static TraktSyncResponse AddMovieToRatings(TraktSyncMovieRated movie)
        {
            var movies = new TraktSyncMoviesRated
            {
                Movies = new List<TraktSyncMovieRated>() { movie }
            };

            return AddMoviesToRatings(movies);
        }

        /// <summary>
        /// UnRate a single movie on trakt.tv
        /// </summary>
        public static TraktSyncResponse RemoveMovieFromRatings(TraktMovie movie)
        {
            var movies = new TraktSyncMovies
            {
                Movies = new List<TraktMovie>() { movie }
            };

            return RemoveMoviesFromRatings(movies);
        }

        #endregion

        #region Scrobble

        public static TraktScrobbleResponse StartMovieScrobble(TraktScrobbleMovie movie)
        {
            var response = PostToTrakt(TraktURIs.ScrobbleStart, movie.ToJSON());
            return response.FromJSON<TraktScrobbleResponse>();
        }

        public static TraktScrobbleResponse StartEpisodeScrobble(TraktScrobbleEpisode episode)
        {
            var response = PostToTrakt(TraktURIs.ScrobbleStart, episode.ToJSON());
            return response.FromJSON<TraktScrobbleResponse>();
        }

        public static TraktScrobbleResponse PauseMovieScrobble(TraktScrobbleMovie movie)
        {
            var response = PostToTrakt(TraktURIs.ScrobblePause, movie.ToJSON());
            return response.FromJSON<TraktScrobbleResponse>();
        }

        public static TraktScrobbleResponse PauseEpisodeScrobble(TraktScrobbleEpisode episode)
        {
            var response = PostToTrakt(TraktURIs.ScrobblePause, episode.ToJSON());
            return response.FromJSON<TraktScrobbleResponse>();
        }

        public static TraktScrobbleResponse StopMovieScrobble(TraktScrobbleMovie movie)
        {
            var response = PostToTrakt(TraktURIs.ScrobbleStop, movie.ToJSON());
            return response.FromJSON<TraktScrobbleResponse>();
        }

        public static TraktScrobbleResponse StopEpisodeScrobble(TraktScrobbleEpisode episode)
        {
            var response = PostToTrakt(TraktURIs.ScrobbleStop, episode.ToJSON());
            return response.FromJSON<TraktScrobbleResponse>();
        }

        #endregion

        #region Watchlist

        public static TraktSyncResponse AddMoviesToWatchlist(TraktSyncMovies movies)
        {
            var response = PostToTrakt(TraktURIs.SyncWatchlistAdd, movies.ToJSON());
            return response.FromJSON<TraktSyncResponse>();
        }

        public static TraktSyncResponse RemoveMoviesFromWatchlist(TraktSyncMovies movies)
        {
            var response = PostToTrakt(TraktURIs.SyncWatchlistRemove, movies.ToJSON());
            return response.FromJSON<TraktSyncResponse>();
        }

        public static TraktSyncResponse AddShowsToWatchlist(TraktSyncShows shows)
        {
            var response = PostToTrakt(TraktURIs.SyncWatchlistAdd, shows.ToJSON());
            return response.FromJSON<TraktSyncResponse>();
        }

        public static TraktSyncResponse RemoveShowsFromWatchlist(TraktSyncShows shows)
        {
            var response = PostToTrakt(TraktURIs.SyncWatchlistRemove, shows.ToJSON());
            return response.FromJSON<TraktSyncResponse>();
        }

        public static TraktSyncResponse AddEpisodesToWatchlist(TraktSyncEpisodes episodes)
        {
            var response = PostToTrakt(TraktURIs.SyncWatchlistAdd, episodes.ToJSON());
            return response.FromJSON<TraktSyncResponse>();
        }

        public static TraktSyncResponse RemoveEpisodesFromWatchlist(TraktSyncEpisodes episodes)
        {
            var response = PostToTrakt(TraktURIs.SyncWatchlistRemove, episodes.ToJSON());
            return response.FromJSON<TraktSyncResponse>();
        }

        #endregion

        #region Watchlist (Single)

        public static TraktSyncResponse AddMovieToWatchlist(TraktMovie movie)
        {
            var movies = new TraktSyncMovies
            {
                Movies = new List<TraktMovie>() { movie }
            };

            return AddMoviesToWatchlist(movies);
        }

        public static TraktSyncResponse RemoveMovieFromWatchlist(TraktMovie movie)
        {
            var movies = new TraktSyncMovies
            {
                Movies = new List<TraktMovie>() { movie }
            };

            return RemoveMoviesFromWatchlist(movies);
        }

        public static TraktSyncResponse AddShowToWatchlist(TraktShow show)
        {
            var shows = new TraktSyncShows
            {
                Shows = new List<TraktShow>() { show }
            };

            return AddShowsToWatchlist(shows);
        }

        public static TraktSyncResponse RemoveShowFromWatchlist(TraktShow show)
        {
            var shows = new TraktSyncShows
            {
                Shows = new List<TraktShow>() { show }
            };

            return RemoveShowsFromWatchlist(shows);
        }

        public static TraktSyncResponse AddEpisodeToWatchlist(TraktEpisode episode)
        {
            var episodes = new TraktSyncEpisodes
            {
                Episodes = new List<TraktEpisode>() { episode }
            };

            return AddEpisodesToWatchlist(episodes);
        }

        public static TraktSyncResponse RemoveEpisodeFromWatchlist(TraktEpisode episode)
        {
            var episodes = new TraktSyncEpisodes
            {
                Episodes = new List<TraktEpisode>() { episode }
            };

            return RemoveEpisodesFromWatchlist(episodes);
        }

        #endregion

        #endregion

        #region DELETE Methods

        #region Dismiss Recommendations

        public static bool DismissRecommendedMovie(string movieId)
        {
            return DeleteFromTrakt(string.Format(TraktURIs.DismissRecommendedMovie, movieId));
        }

        public static bool DismissRecommendedShow(string showId)
        {
            return DeleteFromTrakt(string.Format(TraktURIs.DismissRecommendedShow, showId));
        }

        #endregion

        #region Delete List

        public static bool DeleteUserList(string username, string listId)
        {
            return DeleteFromTrakt(string.Format(TraktURIs.DeleteList, username, listId));
        }

        #endregion

        #endregion

        #region Web Helpers

        public static bool DeleteFromTrakt(string address)
        {
            var response = GetFromTrakt(address, "DELETE");
            return response != null;
        }

        public static string GetFromTrakt(string address, string method = "GET")
        {
            if (OnDataSend != null)
                OnDataSend(address, null);

            var request = WebRequest.Create(address) as HttpWebRequest;

            request.KeepAlive = true;
            request.Method = method;
            request.ContentLength = 0;
            request.Timeout = 120000;
            request.ContentType = "application/json";
            request.UserAgent = UserAgent;

            // add required headers for authorisation
            request.Headers.Add("trakt-api-version", "2");
            request.Headers.Add("trakt-api-key", ApplicationId);
            request.Headers.Add("trakt-user-login", Username);
            request.Headers.Add("trakt-user-token", UserToken ?? string.Empty);

            try
            {
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                if (response == null) return null;

                Stream stream = response.GetResponseStream();
                StreamReader reader = new StreamReader(stream);
                string strResponse = reader.ReadToEnd();

                if (method == "DELETE")
                {
                    strResponse = response.StatusCode.ToString();
                }
                
                if (OnDataReceived != null)
                    OnDataReceived(strResponse);

                stream.Close();
                reader.Close();
                response.Close();

                return strResponse;
            }
            catch (WebException ex)
            {
                string errorMessage = ex.Message;
                if (ex.Status == WebExceptionStatus.ProtocolError)
                {
                    var response = ex.Response as HttpWebResponse;
                    errorMessage = string.Format("The API responded to the request with the following error. Code = '{0}', Description = '{1}'", (int)response.StatusCode, response.StatusDescription);
                }

                if (OnDataError != null)
                    OnDataError(ex.Message);

                return null;
            }
        }

        public static string PostToTrakt(string address, string postData, bool logRequest = true)
        {
            if (OnDataSend != null && logRequest)
                OnDataSend(address, postData);

            byte[] data = new UTF8Encoding().GetBytes(postData);

            var request = WebRequest.Create(address) as HttpWebRequest;
            request.KeepAlive = true;

            request.Method = "POST";
            request.ContentLength = data.Length;
            request.Timeout = 120000;
            request.ContentType = "application/json";
            request.UserAgent = UserAgent;

            // add required headers for authorisation
            request.Headers.Add("trakt-api-version", "2");
            request.Headers.Add("trakt-api-key", ApplicationId);
            request.Headers.Add("trakt-user-login", Username);
            request.Headers.Add("trakt-user-token", UserToken);

            try
            {
                // post to trakt
                Stream postStream = request.GetRequestStream();
                postStream.Write(data, 0, data.Length);

                // get the response
                var response = (HttpWebResponse)request.GetResponse();
                if (response == null) return null;

                Stream responseStream = response.GetResponseStream();
                StreamReader reader = new StreamReader(responseStream);
                string strResponse = reader.ReadToEnd();

                if (OnDataReceived != null)
                    OnDataReceived(strResponse);

                // cleanup
                postStream.Close();
                responseStream.Close();
                reader.Close();
                response.Close();

                return strResponse;
            }
            catch (WebException ex)
            {
                string errorMessage = ex.Message;
                if (ex.Status == WebExceptionStatus.ProtocolError)
                {
                    var response = ex.Response as HttpWebResponse;
                    errorMessage = string.Format("The API responded to the request with the following error. Code = '{0}', Description = '{1}'", (int)response.StatusCode, response.StatusDescription);
                }

                if (OnDataError != null)
                    OnDataError(ex.Message);

                return null;
            }
        }

        #endregion

        #endregion
    }
}
