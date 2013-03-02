using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Net;
using System.Web;
using TraktPlugin.TraktAPI.DataStructures;
using TraktPlugin.TraktHandlers;

namespace TraktPlugin.TraktAPI
{
    #region Enumerables
    /// <summary>
    /// List of Scrobble States
    /// </summary>
    public enum TraktScrobbleStates
    {
        watching,
        scrobble,
        cancelwatching
    }

    /// <summary>
    /// List of Sync Modes
    /// </summary>
    public enum TraktSyncModes
    {
        library,
        seen,
        unlibrary,
        unseen,
        watchlist,
        unwatchlist
    }

    /// <summary>
    /// List of Clearing Modes
    /// </summary>
    public enum TraktClearingModes
    {
        all,
        movies,
        episodes
    }

    /// <summary>
    /// List of Item Types
    /// </summary>
    public enum TraktItemType
    {
        episode,
        season,
        show,
        movie
    }

    /// <summary>
    /// List of Rate Values
    /// </summary>
    public enum TraktRateValue
    {
        unrate,
        one,
        two,
        three,
        four,
        five,
        six,
        seven,
        eight,
        nine,
        ten,
        love, //deprecated - ten
        hate  //deprecated - one
    }

    /// <summary>
    /// Trakt Connection States
    /// </summary>
    public enum ConnectionState
    {
        Connected,
        Connecting,
        Disconnected,
        Invalid,
        Pending
    }

    /// <summary>
    /// Privacy Level for Lists
    /// </summary>
    public enum ListPrivacyLevel
    {
        Public,
        Private,
        Friends
    }

    /// <summary>
    /// Defaults to all, but you can instead send a comma delimited list of actions. 
    /// For example, /all or /watching,scrobble,seen or /rating.
    /// </summary>
    public enum ActivityAction
    {
        all,
        watching,
        scrobble,
        checkin,
        seen,
        collection,
        rating,
        watchlist,
        review,
        shout,
        created,
        item_added
    }

    /// <summary>
    /// Defaults to all, but you can instead send a comma delimited list of types.
    /// For example, /all or /movie,show or /list.
    /// </summary>
    public enum ActivityType
    {
        all,
        episode,
        show,
        movie,
        list
    }

    #endregion

    /// <summary>
    /// Object that communicates with the Trakt API
    /// </summary>
    public class TraktAPI
    {
        #region Scrobbling

        /// <summary>
        /// Sends Scrobble data to Trakt
        /// </summary>
        /// <param name="scrobbleData">The Data to send</param>
        /// <param name="status">The mode to send it as</param>
        /// <returns>The response from Trakt</returns>
        public static TraktResponse ScrobbleMovieState(TraktMovieScrobble scrobbleData, TraktScrobbleStates status)
        {
            //If we are cancelling a scrobble we don't need data
            if (status != TraktScrobbleStates.cancelwatching)
            {
                // check that we have everything we need
                // server can accept title if movie id is not supplied
                if (scrobbleData == null)
                {
                    TraktResponse error = new TraktResponse
                    {
                        Error = "Not enough information to send to server",
                        Status = "failure"
                    };
                    return error;
                }
            }

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
            if (status != TraktScrobbleStates.cancelwatching)
            {
                if (scrobbleData == null)
                {
                    TraktResponse error = new TraktResponse
                    {
                        Error = "Not enough information to send to server",
                        Status = "failure"
                    };
                    return error;
                }
            }
            
            // serialize Scrobble object to JSON and send to server
            string response = Transmit(string.Format(TraktURIs.ScrobbleShow, status.ToString()), scrobbleData.ToJSON());

            // return success or failure
            return response.FromJSON<TraktResponse>();
        }

        #endregion

        #region Syncing

        /// <summary>
        /// Sends movie sync data to Trakt
        /// </summary>
        /// <param name="syncData">The sync data to send</param>
        /// <param name="mode">The sync mode to use</param>
        /// <returns>The response from trakt</returns>
        public static TraktSyncResponse SyncMovieLibrary(TraktMovieSync syncData, TraktSyncModes mode)
        {
            // check that we have everything we need
            // server can accept title/year if imdb id is not supplied
            if (syncData == null || syncData.MovieList.Count == 0)
            {
                TraktSyncResponse error = new TraktSyncResponse
                {
                    Error = "Not enough information to send to server",
                    Status = "failure"
                };
                return error;
            }

            // serialize Scrobble object to JSON and send to server
            string response = Transmit(string.Format(TraktURIs.SyncMovieLibrary, mode.ToString()), syncData.ToJSON());

            // Log how many movies were inserted, skipped, already exist and movie list of failures
            TraktLogger.Debug("Response: {0}", response);

            // return success or failure
            return response.FromJSON<TraktSyncResponse>();
        }

        /// <summary>
        /// Add/Remove show to/from watchlist
        /// </summary>
        /// <param name="syncData">The sync data to send</param>
        /// <param name="mode">The sync mode to use</param>
        /// <returns>The response from trakt</returns>
        public static TraktResponse SyncShowWatchList(TraktShowSync syncData, TraktSyncModes mode)
        {
            // check that we have everything we need            
            if (syncData == null || syncData.Shows.Count == 0)
            {
                TraktResponse error = new TraktResponse
                {
                    Error = "Not enough information to send to server",
                    Status = "failure"
                };
                return error;
            }

            // serialize Scrobble object to JSON and send to server
            string response = Transmit(string.Format(TraktURIs.SyncShowWatchList, mode.ToString()), syncData.ToJSON());           
            TraktLogger.Debug("Response: {0}", response);

            // return success or failure
            return response.FromJSON<TraktResponse>();
        }

        /// <summary>
        /// Add/Remove episode to/from watchlist
        /// </summary>
        /// <param name="syncData">The sync data to send</param>
        /// <param name="mode">The sync mode to use</param>
        /// <returns>The response from trakt</returns>
        public static TraktResponse SyncEpisodeWatchList(TraktEpisodeSync syncData, TraktSyncModes mode)
        {
            // check that we have everything we need            
            if (syncData == null || syncData.EpisodeList.Count == 0)
            {
                TraktResponse error = new TraktResponse
                {
                    Error = "Not enough information to send to server",
                    Status = "failure"
                };
                return error;
            }

            // serialize Scrobble object to JSON and send to server
            string response = Transmit(string.Format(TraktURIs.SyncEpisodeWatchList, mode.ToString()), syncData.ToJSON());
            TraktLogger.Debug("Response: {0}", response);

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

        public static TraktResponse SyncShowAsSeen(TraktShowSeen show)
        {
            string response = Transmit(TraktURIs.ShowSeen, show.ToJSON());
            return response.FromJSON<TraktResponse>();
        }

        public static TraktResponse SyncSeasonAsSeen(TraktSeasonSeen showSeason)
        {
            string response = Transmit(TraktURIs.SeasonSeen, showSeason.ToJSON());
            return response.FromJSON<TraktResponse>();
        }

        public static TraktResponse SyncShowAsLibrary(TraktShowLibrary show)
        {
            string response = Transmit(TraktURIs.ShowLibrary, show.ToJSON());
            return response.FromJSON<TraktResponse>();
        }

        public static TraktResponse SyncSeasonAsLibrary(TraktSeasonLibrary showSeason)
        {
            string response = Transmit(TraktURIs.SeasonLibrary, showSeason.ToJSON());
            return response.FromJSON<TraktResponse>();
        }

        #endregion

        #region Trakt Library Calls

        /// <summary>
        /// Gets the trakt movie library for a user
        /// </summary>
        /// <param name="user">The user to get</param>
        /// <returns>The trakt movie library</returns>
        public static IEnumerable<TraktLibraryMovies> GetMovieCollectionForUser(string user)
        {
            TraktLogger.Info("Getting user {0}'s movie collection from trakt", user);
            //Get the library
            string moviesForUser = Transmit(string.Format(TraktURIs.UserMoviesCollection, user), GetUserAuthentication());
            TraktLogger.Debug("Response: {0}", moviesForUser);
            //hand it on
            // if we timeout we will return an error response
            TraktResponse response = moviesForUser.FromJSON<TraktResponse>();
            if (response == null || response.Error != null) return null;
            return moviesForUser.FromJSONArray<TraktLibraryMovies>();
        }

        public static IEnumerable<TraktLibraryMovies> GetAllMoviesForUser(string user)
        {
            return GetAllMoviesForUser(user, true);
        }

        /// <summary>
        /// Gets all movies for a user from trakt, including movies not in collection
        /// </summary>
        /// <param name="user">The user to get</param>
        /// <param name="syncDataOnly">set this to true (default) if you want the absolute minimum data returned nessacary for syncing</param>
        /// <returns>The trakt movie library</returns>
        public static IEnumerable<TraktLibraryMovies> GetAllMoviesForUser(string user, bool syncDataOnly)
        {
            TraktLogger.Info("Getting user {0}'s movies from trakt", user);
            //Get the library
            string moviesForUser = Transmit(string.Format(TraktURIs.UserMoviesAll, user, syncDataOnly ? @"/min" : string.Empty), GetUserAuthentication());
            TraktLogger.Debug("Response: {0}", moviesForUser);
            //hand it on
            // if we timeout we will return an error response
            TraktResponse response = moviesForUser.FromJSON<TraktResponse>();
            if (response == null || response.Error != null) return null;
            return moviesForUser.FromJSONArray<TraktLibraryMovies>();
        }

        public static IEnumerable<TraktLibraryShow> GetLibraryEpisodesForUser(string user)
        {
            return GetLibraryEpisodesForUser(user, true);
        }

        /// <summary>
        /// Gets the trakt episode library for a user
        /// </summary>
        /// <param name="user">The user to get</param>
        /// <param name="syncDataOnly">set this to true (default) if you want the absolute minimum data returned nessacary for syncing</param>
        /// <returns>The trakt episode library</returns>
        public static IEnumerable<TraktLibraryShow> GetLibraryEpisodesForUser(string user, bool syncDataOnly)
        {
            TraktLogger.Info("Getting user {0}'s 'library' episodes from trakt", user);
            string showsForUser = Transmit(string.Format(TraktURIs.UserEpisodesCollection, user, syncDataOnly ? @"/min" : string.Empty), GetUserAuthentication());
            TraktLogger.Debug("Response: {0}", showsForUser);            
            // if we timeout we will return an error response
            TraktResponse response = showsForUser.FromJSON<TraktResponse>();
            if (response == null || response.Error != null) return null;
            return showsForUser.FromJSONArray<TraktLibraryShow>();
        }

        public static IEnumerable<TraktLibraryShow> GetWatchedEpisodesForUser(string user)
        {
            return GetWatchedEpisodesForUser(user, true);
        }

        /// <summary>
        /// Gets the trakt watched/seen episodes for a user
        /// </summary>
        /// <param name="user">The user to get</param>
        /// <param name="syncDataOnly">set this to true (default) if you want the absolute minimum data returned nessacary for syncing</param>
        /// <returns>The trakt episode library</returns>
        public static IEnumerable<TraktLibraryShow> GetWatchedEpisodesForUser(string user, bool syncDataOnly)
        {
            TraktLogger.Info("Getting user {0}'s 'watched/seen' episodes from trakt", user);
            string showsForUser = Transmit(string.Format(TraktURIs.UserWatchedEpisodes, user, syncDataOnly ? @"/min" : string.Empty), GetUserAuthentication());
            TraktLogger.Debug("Response: {0}", showsForUser);
            // if we timeout we will return an error response
            TraktResponse response = showsForUser.FromJSON<TraktResponse>();
            if (response == null || response.Error != null) return null;
            return showsForUser.FromJSONArray<TraktLibraryShow>();
        }

        public static IEnumerable<TraktLibraryShow> GetUnSeenEpisodesForUser(string user)
        {
            return GetUnSeenEpisodesForUser(user, true);
        }

        public static IEnumerable<TraktLibraryShow> GetUnSeenEpisodesForUser(string user, bool syncDataOnly)
        {
            TraktLogger.Info("Getting user {0}'s 'unseen' episodes from trakt", user);
            string showsForUser = Transmit(string.Format(TraktURIs.UserEpisodesUnSeen, user, syncDataOnly ? @"/min" : string.Empty), GetUserAuthentication());
            TraktLogger.Debug("Response: {0}", showsForUser);
            // if we timeout we will return an error response
            TraktResponse response = showsForUser.FromJSON<TraktResponse>();
            if (response == null || response.Error != null) return null;
            return showsForUser.FromJSONArray<TraktLibraryShow>();
        }

        #endregion

        #region Rating

        /// <summary>
        /// Sends episode rate data to Trakt
        /// </summary>
        /// <param name="episode">The Trakt rate data to send</param>
        /// <returns>The response from Trakt</returns>
        public static TraktRateResponse RateEpisode(TraktRateEpisode episode)
        {
            if (episode == null) return null;
            string response = Transmit(string.Format(TraktURIs.RateItem, TraktItemType.episode.ToString()), episode.ToJSON());
            return response.FromJSON<TraktRateResponse>();
        }

        /// <summary>
        /// Sends episodes rate data to Trakt
        /// </summary>
        /// <param name="episode">The Trakt rate data to send</param>
        /// <returns>The response from Trakt</returns>
        public static TraktRateResponse RateEpisodes(TraktRateEpisodes episodes)
        {
            if (episodes == null) return null;
            string response = Transmit(TraktURIs.RateEpisodes, episodes.ToJSON());
            return response.FromJSON<TraktRateResponse>();
        }

        /// <summary>
        /// Sends series rate data to Trakt
        /// </summary>
        /// <param name="episode">The Trakt rate data to send</param>
        /// <returns>The response from Trakt</returns>
        public static TraktRateResponse RateSeries(TraktRateSeries series)
        {
            if (series == null) return null;
            string response = Transmit(string.Format(TraktURIs.RateItem, TraktItemType.show.ToString()), series.ToJSON());
            return response.FromJSON<TraktRateResponse>();
        }

        /// <summary>
        /// Sends multiple series rate data to Trakt
        /// </summary>
        /// <param name="episode">The Trakt rate data to send</param>
        /// <returns>The response from Trakt</returns>
        public static TraktRateResponse RateSeries(TraktRateShows shows)
        {
            if (shows == null) return null;
            string response = Transmit(TraktURIs.RateShows, shows.ToJSON());
            return response.FromJSON<TraktRateResponse>();
        }

        /// <summary>
        /// Sends movie rate data to Trakt
        /// </summary>
        /// <param name="episode">The Trakt rate data to send</param>
        /// <returns>The response from Trakt</returns>
        public static TraktRateResponse RateMovie(TraktRateMovie movie)
        {
            if (movie == null) return null;
            string response = Transmit(string.Format(TraktURIs.RateItem, TraktItemType.movie.ToString()), movie.ToJSON());
            return response.FromJSON<TraktRateResponse>();
        }

        /// <summary>
        /// Sends movies rate data to Trakt
        /// </summary>
        /// <param name="episode">The Trakt rate data to send</param>
        /// <returns>The response from Trakt</returns>
        public static TraktRateResponse RateMovies(TraktRateMovies movies)
        {
            if (movies == null) return null;
            string response = Transmit(TraktURIs.RateMovies, movies.ToJSON());
            return response.FromJSON<TraktRateResponse>();
        }

        #endregion

        #region User

        public static TraktUserProfile GetUserProfile(string user)
        {
            string response = Transmit(string.Format(TraktURIs.UserProfile, user), GetUserAuthentication());
            return response.FromJSON<TraktUserProfile>();
        }

        /// <summary>
        /// Returns a list of Friends and their user profiles
        /// </summary>
        /// <param name="user">username of person to retrieve friend s list</param>
        public static IEnumerable<TraktUserProfile> GetUserFriends(string user)
        {
            string response = Transmit(string.Format(TraktURIs.UserFriends, user), GetUserAuthentication());
            return response.FromJSONArray<TraktUserProfile>();
        }
        
        /// <summary>
        /// Returns list of episodes in Users Calendar
        /// </summary>
        /// <param name="user">username of person to get Calendar</param>
        public static IEnumerable<TraktCalendar> GetCalendarForUser(string user)
        {
            // 7-Days from Today
            // All Dates should be in PST (GMT-8)
            DateTime dateNow = DateTime.UtcNow.Subtract(new TimeSpan(8, 0, 0));
            return GetCalendarForUser(user, dateNow.ToString("yyyyMMdd"), "7");
        }

        /// <summary>
        /// Returns list of episodes in Users Calendar
        /// </summary>
        /// <param name="user">username of person to get Calendar</param>
        /// <param name="startDate">Start Date of calendar in form yyyyMMdd (GMT-8hrs)</param>
        /// <param name="days">Number of days to return in calendar</param>
        public static IEnumerable<TraktCalendar> GetCalendarForUser(string user, string startDate, string days)
        {
            string userCalendar = Transmit(string.Format(TraktURIs.UserCalendarShows, user, startDate, days), GetUserAuthentication());
            return userCalendar.FromJSONArray<TraktCalendar>();
        }

        public static IEnumerable<TraktCalendar> GetCalendarPremieres()
        {
            // 7-Days from Today
            // All Dates should be in PST (GMT-8)
            DateTime dateNow = DateTime.UtcNow.Subtract(new TimeSpan(8, 0, 0));
            return GetCalendarPremieres(dateNow.ToString("yyyyMMdd"), "7");
        }

        /// <summary>
        /// Returns list of episodes in the Premieres Calendar
        /// </summary>        
        /// <param name="startDate">Start Date of calendar in form yyyyMMdd (GMT-8hrs)</param>
        /// <param name="days">Number of days to return in calendar</param>
        public static IEnumerable<TraktCalendar> GetCalendarPremieres(string startDate, string days)
        {
            string premieres = Transmit(string.Format(TraktURIs.CalendarPremieres, startDate, days), GetUserAuthentication());
            return premieres.FromJSONArray<TraktCalendar>();
        }

        public static IEnumerable<TraktCalendar> GetCalendarShows()
        {
            DateTime dateNow = DateTime.UtcNow.Subtract(new TimeSpan(8, 0, 0));
            return GetCalendarShows(dateNow.ToString("yyyyMMdd"), "7");
        }

        public static IEnumerable<TraktCalendar> GetCalendarShows(string startDate, string days)
        {
            string premieres = Transmit(string.Format(TraktURIs.CalendarAllShows, startDate, days), GetUserAuthentication());
            return premieres.FromJSONArray<TraktCalendar>();
        }

        /// <summary>
        /// Returns list of the 100 last watched episodes by a user
        /// </summary>
        /// <param name="user">username of person to get watched history</param>
        [Obsolete("This method is deprecated and has been replaced by GetUserActivity", false)]
        public static IEnumerable<TraktWatchedEpisode> GetUserEpisodeWatchedHistory(string user)
        {
            string watchedEpisodes = Transmit(string.Format(TraktURIs.UserEpisodeWatchedHistory, user), GetUserAuthentication());
            return watchedEpisodes.FromJSONArray<TraktWatchedEpisode>();
        }

        /// <summary>
        /// Returns list of the 100 last watched movies by a user
        /// </summary>
        /// <param name="user">username of person to get watched history</param>
        [Obsolete("This method is deprecated and has been replaced by GetUserActivity", false)]
        public static IEnumerable<TraktWatchedMovie> GetUserMovieWatchedHistory(string user)
        {
            string watchedMovies = Transmit(string.Format(TraktURIs.UserMovieWatchedHistory, user), GetUserAuthentication());
            return watchedMovies.FromJSONArray<TraktWatchedMovie>();
        }

        /// <summary>
        /// Returns a list of lists created by user
        /// </summary>
        /// <param name="user">username of person to get lists</param>
        public static IEnumerable<TraktUserList> GetUserLists(string user)
        {
            string userLists = Transmit(string.Format(TraktURIs.UserLists, user), GetUserAuthentication());
            return userLists.FromJSONArray<TraktUserList>();
        }

        /// <summary>
        /// Returns the contents of a lists for a user
        /// </summary>
        /// <param name="user">username of person</param>
        /// <param name="slug">slug (id) of list item e.g. "star-wars-collection"</param>
        public static TraktUserList GetUserList(string user, string slug)
        {
            string userList = Transmit(string.Format(TraktURIs.UserList, user, slug), GetUserAuthentication());
            return userList.FromJSON<TraktUserList>();
        }

        /// <summary>
        /// Returns the users Rated Movies
        /// </summary>
        /// <param name="user">username of person</param>
        public static IEnumerable<TraktUserMovieRating> GetUserRatedMovies(string user)
        {
            string ratedMovies = Transmit(string.Format(TraktURIs.UserRatedMoviesList, user), GetUserAuthentication());
            return ratedMovies.FromJSONArray<TraktUserMovieRating>();
        }

        /// <summary>
        /// Returns the users Rated Shows
        /// </summary>
        /// <param name="user">username of person</param>
        public static IEnumerable<TraktUserShowRating> GetUserRatedShows(string user)
        {
            string ratedShows = Transmit(string.Format(TraktURIs.UserRatedShowsList, user), GetUserAuthentication());
            return ratedShows.FromJSONArray<TraktUserShowRating>();
        }

        /// <summary>
        /// Returns the users Rated Episodes
        /// </summary>
        /// <param name="user">username of person</param>
        public static IEnumerable<TraktUserEpisodeRating> GetUserRatedEpisodes(string user)
        {
            string ratedEpisodes = Transmit(string.Format(TraktURIs.UserRatedEpisodesList, user), GetUserAuthentication());
            return ratedEpisodes.FromJSONArray<TraktUserEpisodeRating>();
        }

        #endregion

        #region Activity

        public static TraktActivity GetFriendActivity()
        {
            return GetFriendActivity(null, null, false);
        }

        public static TraktActivity GetFriendActivity(bool includeMe)
        {
            return GetFriendActivity(null, null, includeMe);
        }

        public static TraktActivity GetFriendActivity(List<ActivityType> types, List<ActivityAction> actions, bool includeMe)
        {
            return GetFriendActivity(types, actions, 0, 0, includeMe);
        }

        public static TraktActivity GetFriendActivity(List<ActivityType> types, List<ActivityAction> actions, long start, long end, bool includeMe)
        {
            // get comma seperated list of types and actions (if more than one)
            string activityTypes = types == null ? "all" : string.Join(",", types.Select(t => t.ToString()).ToArray());
            string activityActions = actions == null ? "all" : string.Join(",", actions.Select(a => a.ToString()).ToArray());

            string startEnd = (start == 0 || end == 0) ? string.Empty : string.Format("/{0}/{1}", start, end);
            string apiUrl = includeMe ? TraktURIs.ActivityFriendsMe : TraktURIs.ActivityFriends;

            string activity = Transmit(string.Format(apiUrl, activityTypes, activityActions, startEnd), GetUserAuthentication());
            return activity.FromJSON<TraktActivity>();
        }

        public static TraktActivity GetCommunityActivity()
        {
            return GetCommunityActivity(null, null);
        }

        public static TraktActivity GetCommunityActivity(List<ActivityType> types, List<ActivityAction> actions)
        {
            return GetCommunityActivity(types, actions, 0, 0);
        }

        public static TraktActivity GetCommunityActivity(List<ActivityType> types, List<ActivityAction> actions, long start, long end)
        {
            // get comma seperated list of types and actions (if more than one)
            string activityTypes = types == null ? "all" : string.Join(",", types.Select(t => t.ToString()).ToArray());
            string activityActions = actions == null ? "all" : string.Join(",", actions.Select(a => a.ToString()).ToArray());

            string startEnd = (start == 0 || end == 0) ? string.Empty : string.Format("/{0}/{1}", start, end);

            string activity = Transmit(string.Format(TraktURIs.ActivityCommunity, activityTypes, activityActions, startEnd), GetUserAuthentication());
            return activity.FromJSON<TraktActivity>();
        }

        public static TraktActivity GetUserActivity(string username, List<ActivityType> types, List<ActivityAction> actions)
        {
            // get comma seperated list of types and actions (if more than one)
            string activityTypes = string.Join(",", types.Select(t => t.ToString()).ToArray());
            string activityActions = string.Join(",", actions.Select(a => a.ToString()).ToArray());

            string activity = Transmit(string.Format(TraktURIs.ActivityUser, username, activityTypes, activityActions), GetUserAuthentication());
            return activity.FromJSON<TraktActivity>();
        }

        #endregion

        #region Friends

        /// <summary>
        /// Returns a list of Friends for current user
        /// </summary>        
        public static IEnumerable<TraktUserProfile> GetFriends()
        {
            string response = Transmit(TraktURIs.Friends, GetUserAuthentication());
            return response.FromJSONArray<TraktUserProfile>();
        }

        /// <summary>
        /// Returns a list of Friend requests for current user
        /// </summary>        
        public static IEnumerable<TraktUserProfile> GetFriendRequests()
        {
            string response = Transmit(TraktURIs.FriendRequests, GetUserAuthentication());
            return response.FromJSONArray<TraktUserProfile>();
        }

        public static TraktResponse FriendApprove(TraktFriend friend)
        {
            string response = Transmit(TraktURIs.FriendApprove, friend.ToJSON());
            return response.FromJSON<TraktResponse>();
        }

        public static TraktResponse FriendAdd(TraktFriend friend)
        {
            string response = Transmit(TraktURIs.FriendAdd, friend.ToJSON());
            return response.FromJSON<TraktResponse>();
        }

        public static TraktResponse FriendDeny(TraktFriend friend)
        {
            string response = Transmit(TraktURIs.FriendDeny, friend.ToJSON());
            return response.FromJSON<TraktResponse>();
        }

        public static TraktResponse FriendDelete(TraktFriend friend)
        {
            string response = Transmit(TraktURIs.FriendDelete, friend.ToJSON());
            return response.FromJSON<TraktResponse>();
        }

        #endregion

        #region Trending

        public static IEnumerable<TraktTrendingMovie> GetTrendingMovies()
        {
            string response = Transmit(TraktURIs.TrendingMovies, GetUserAuthentication());
            return response.FromJSONArray<TraktTrendingMovie>();
        }

        public static IEnumerable<TraktTrendingShow> GetTrendingShows()
        {
            string response = Transmit(TraktURIs.TrendingShows, GetUserAuthentication());
            return response.FromJSONArray<TraktTrendingShow>();
        }

        #endregion

        #region Recommendations

        public static TraktResponse DismissMovieRecommendation(TraktMovieSlug movie)
        {
            string response = Transmit(TraktURIs.DismissMovieRecommendation, movie.ToJSON());
            return response.FromJSON<TraktResponse>();
        }

        public static TraktResponse DismissShowRecommendation(TraktShowSlug show)
        {
            string response = Transmit(TraktURIs.DismissShowRecommendation, show.ToJSON());
            return response.FromJSON<TraktResponse>();
        }


        /// <summary>
        /// Get Recommendations with out any filtering
        /// </summary>
        public static IEnumerable<TraktMovie> GetRecommendedMovies()
        {
            string response = Transmit(TraktURIs.UserMovieRecommendations, GetUserAuthentication());
            return response.FromJSONArray<TraktMovie>();
        }

        public static IEnumerable<TraktMovie> GetRecommendedMovies(string genre, bool hidecollected, bool hidewatchlisted, int startyear, int endyear)
        {
            var traktRecommendationPost = new TraktRecommendations
            {
                Username = TraktSettings.Username,
                Password = TraktSettings.Password,
                Genre = genre,
                HideCollected = hidecollected,
                HideWatchlisted = hidewatchlisted,
                StartYear = startyear,
                EndYear = endyear
            };

            string response = Transmit(TraktURIs.UserMovieRecommendations, traktRecommendationPost.ToJSON());
            return response.FromJSONArray<TraktMovie>();
        }

        /// <summary>
        /// Get Recommendations with out any filtering
        /// </summary>        
        public static IEnumerable<TraktShow> GetRecommendedShows()
        {
            string response = Transmit(TraktURIs.UserShowsRecommendations, GetUserAuthentication());
            return response.FromJSONArray<TraktShow>();
        }

        public static IEnumerable<TraktShow> GetRecommendedShows(string genre, bool hidecollected, bool hidewatchlisted, int startyear, int endyear)
        {
            var traktRecommendationPost = new TraktRecommendations
            {
                Username = TraktSettings.Username,
                Password = TraktSettings.Password,
                Genre = genre,
                HideCollected = hidecollected,
                HideWatchlisted = hidewatchlisted,
                StartYear = startyear,
                EndYear = endyear
            };

            string response = Transmit(TraktURIs.UserShowsRecommendations, traktRecommendationPost.ToJSON());
            return response.FromJSONArray<TraktShow>();
        }

        #endregion

        #region Watch List

        public static IEnumerable<TraktWatchListMovie> GetWatchListMovies(string user)
        {
            string response = Transmit(string.Format(TraktURIs.UserMovieWatchList, user), GetUserAuthentication());
            return response.FromJSONArray<TraktWatchListMovie>();
        }

        public static IEnumerable<TraktWatchListShow> GetWatchListShows(string user)
        {
            string response = Transmit(string.Format(TraktURIs.UserShowsWatchList, user), GetUserAuthentication());
            return response.FromJSONArray<TraktWatchListShow>();
        }

        public static IEnumerable<TraktWatchListEpisode> GetWatchListEpisodes(string user)
        {
            string response = Transmit(string.Format(TraktURIs.UserEpisodesWatchList, user), GetUserAuthentication());
            return response.FromJSONArray<TraktWatchListEpisode>();
        }

        #endregion

        #region Lists

        public static TraktAddListResponse ListAdd(TraktList list)
        {
            string response = Transmit(TraktURIs.ListAdd, list.ToJSON());
            return response.FromJSON<TraktAddListResponse>();
        }

        public static TraktResponse ListDelete(TraktList list)
        {
            string response = Transmit(TraktURIs.ListDelete, list.ToJSON());
            return response.FromJSON<TraktResponse>();
        }

        public static TraktResponse ListUpdate(TraktList list)
        {
            string response = Transmit(TraktURIs.ListUpdate, list.ToJSON());
            return response.FromJSON<TraktResponse>();
        }

        public static TraktSyncResponse ListAddItems(TraktList list)
        {
            string response = Transmit(TraktURIs.ListItemsAdd, list.ToJSON());
            return response.FromJSON<TraktSyncResponse>();
        }

        public static TraktSyncResponse ListDeleteItems(TraktList list)
        {
            string response = Transmit(TraktURIs.ListItemsDelete, list.ToJSON());
            return response.FromJSON<TraktSyncResponse>();
        }

        #endregion

        #region Account

        public static TraktResponse CreateAccount(TraktAccount account)
        {
            string response = Transmit(TraktURIs.CreateAccount, account.ToJSON());
            return response.FromJSON<TraktResponse>();
        }

        public static TraktResponse TestAccount(TraktAccount account)
        {
            string response = Transmit(TraktURIs.TestAccount, account.ToJSON());
            return response.FromJSON<TraktResponse>();
        }

        public static TraktAccountSettings GetAccountSettings()
        {
            string response = Transmit(TraktURIs.AccountSettings, GetUserAuthentication());
            return response.FromJSON<TraktAccountSettings>();
        }

        #endregion

        #region Search

        /// <summary>
        /// Returns a list of users found using search term
        /// </summary>        
        public static IEnumerable<TraktUserProfile> SearchForFriends(string searchTerm)
        {
            string response = Transmit(string.Format(TraktURIs.SearchUsers, HttpUtility.UrlEncode(searchTerm)), GetUserAuthentication());
            return response.FromJSONArray<TraktUserProfile>();
        }

        /// <summary>
        /// Returns a list of movies found using search term
        /// </summary>        
        public static IEnumerable<TraktMovie> SearchMovies(string searchTerm)
        {
            string response = Transmit(string.Format(TraktURIs.SearchMovies, HttpUtility.UrlEncode(searchTerm)), string.Empty);
            return response.FromJSONArray<TraktMovie>();
        }

        /// <summary>
        /// Returns a list of shows found using search term
        /// </summary>        
        public static IEnumerable<TraktShow> SearchShows(string searchTerm)
        {
            string response = Transmit(string.Format(TraktURIs.SearchShows, HttpUtility.UrlEncode(searchTerm)), string.Empty);
            return response.FromJSONArray<TraktShow>();
        }

        /// <summary>
        /// Returns a list of episodes found using search term
        /// </summary>        
        public static IEnumerable<TraktSearchEpisode> SearchEpisodes(string searchTerm)
        {
            string response = Transmit(string.Format(TraktURIs.SearchEpisodes, HttpUtility.UrlEncode(searchTerm)), string.Empty);
            return response.FromJSONArray<TraktSearchEpisode>();
        }

        /// <summary>
        /// Returns a list of actors found using search term
        /// </summary>        
        public static IEnumerable<TraktActor> SearchActor(string searchTerm)
        {
            string response = Transmit(string.Format(TraktURIs.SearchActor, HttpUtility.UrlEncode(searchTerm)), string.Empty);
            return response.FromJSONArray<TraktActor>();
        }

        #endregion

        #region Shouts

        /// <summary>
        /// Return a list of shouts for a movie
        /// </summary>
        /// <param name="title">The movie search term, either (title-year seperate spaces with '-'), imdbid, tmdbid</param>    
        public static IEnumerable<TraktShout> GetMovieShouts(string title)
        {
            string response = Transmit(string.Format(TraktURIs.MovieShouts, title), GetUserAuthentication());
            return response.FromJSONArray<TraktShout>();
        }

        /// <summary>
        /// Return a list of shouts for a show
        /// </summary>
        /// <param name="title">The show search term, either (title seperate spaces with '-'), imdbid, tvdbid</param>    
        public static IEnumerable<TraktShout> GetShowShouts(string title)
        {
            string response = Transmit(string.Format(TraktURIs.ShowShouts, title), GetUserAuthentication());
            return response.FromJSONArray<TraktShout>();
        }

        /// <summary>
        /// Return a list of shouts for a episode
        /// </summary>
        /// <param name="title">The episode search term, either (title seperate spaces with '-'), imdbid, tmdbid</param>
        /// <param name="season">The episode index</param>
        /// <param name="indexc">The season index</param>
        public static IEnumerable<TraktShout> GetEpisodeShouts(string title, string season, string episode)
        {
            string response = Transmit(string.Format(TraktURIs.EpisodeShouts, title, season, episode), GetUserAuthentication());
            return response.FromJSONArray<TraktShout>();
        }

        #endregion

        #region Related

        public static IEnumerable<TraktMovie> GetRelatedMovies(string title)
        {
            return GetRelatedMovies(title, false);
        }

        /// <summary>
        /// Return a list of related movies for a movie
        /// </summary>
        /// <param name="title">The movie search term, either (title-year seperate spaces with '-'), imdbid, tmdbid</param>
        /// <param name="hidewatched">Hide watched movies</param>
        public static IEnumerable<TraktMovie> GetRelatedMovies(string title, bool hidewatched)
        {
            string response = Transmit(string.Format(TraktURIs.RelatedMovies, title, hidewatched ? "/hidewatched" : string.Empty), GetUserAuthentication());
            return response.FromJSONArray<TraktMovie>();
        }

        public static IEnumerable<TraktShow> GetRelatedShows(string title)
        {
            return GetRelatedShows(title, false);
        }

        /// <summary>
        /// Return a list of related shows for a show
        /// </summary>
        /// <param name="title">The show search term, either (title-year seperate spaces with '-'), imdbid, tvdbid</param>
        /// <param name="hidewatched">Hide watched movies</param>
        public static IEnumerable<TraktShow> GetRelatedShows(string title, bool hidewatched)
        {
            string response = Transmit(string.Format(TraktURIs.RelatedShows, title, hidewatched ? "/hidewatched" : string.Empty), GetUserAuthentication());
            return response.FromJSONArray<TraktShow>();
        }

        #endregion

        #region Show Seasons

        /// <summary>
        /// Return a list of seasons for a tv show
        /// </summary>
        /// <param name="title">The show search term, either (title-year seperate spaces with '-'), imdbid, tvdbid</param>
        public static IEnumerable<TraktShowSeason> GetShowSeasons(string title)
        {
            string response = Transmit(string.Format(TraktURIs.ShowSeasons, title), string.Empty);
            return response.FromJSONArray<TraktShowSeason>();
        }

        #endregion

        #region Season Episodes

        /// <summary>
        /// Return a list of episodes for a tv show season
        /// </summary>
        /// <param name="title">The show search term, either (title-year seperate spaces with '-'), imdbid, tvdbid</param>
        /// <param name="season">The season, 0 for specials</param>
        public static IEnumerable<TraktEpisode> GetSeasonEpisodes(string title, string season)
        {
            string response = Transmit(string.Format(TraktURIs.SeasonEpisodes, title, season), GetUserAuthentication());
            return response.FromJSONArray<TraktEpisode>();
        }

        #endregion

        #region Helpers

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
                TraktLogger.Debug("Post: {0} Address: {1}", data, address);
            }

            try
            {
                ServicePointManager.Expect100Continue = false;
                WebClient client = new WebClient();
                client.Encoding = Encoding.UTF8;
                client.Headers.Add("user-agent", TraktSettings.UserAgent);
                return client.UploadString(address, data);
            }
            catch (WebException e)
            {
                // something bad happened
                TraktLogger.Debug("WebException: {0}", e.Message);

                if(e.Status == WebExceptionStatus.ProtocolError)
                {
                    var response = ((HttpWebResponse)e.Response);
                    try
                    {
                        using (var stream = response.GetResponseStream())
                        {
                            using (var reader = new StreamReader(stream))
                            {
                                return reader.ReadToEnd();
                            }
                        }
                    }
                    catch { } 
                }

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
        internal static void ClearLibrary(TraktClearingModes mode, ProgressDialog progressDialog, bool clearSeen)
        {
            progressDialog.Title = "Clearing Library";
            progressDialog.CancelMessage = "Attempting to Cancel";
            progressDialog.Maximum = 100;
            progressDialog.Value = 0;
            progressDialog.Line1 = "Clearing your Trakt Library";
            progressDialog.ShowDialog(ProgressDialog.PROGDLG.Modal, ProgressDialog.PROGDLG.NoMinimize, ProgressDialog.PROGDLG.NoTime);

            //Movies
            if (mode == TraktClearingModes.all || mode == TraktClearingModes.movies)
            {
                TraktLogger.Info("Removing Movies from Trakt");
                TraktLogger.Info("NOTE: WILL NOT REMOVE SCROBBLED MOVIES DUE TO API LIMITATION");
                progressDialog.Line2 = "Getting movies for user";
                List<TraktLibraryMovies> movies = GetAllMoviesForUser(TraktSettings.Username).ToList();

                var syncData = BasicHandler.CreateMovieSyncData(movies);
                TraktResponse response = null;

                if (clearSeen)
                {
                    TraktLogger.Info("First removing movies from seen");
                    progressDialog.Line2 = "Setting seen movies as unseen";
                    response = SyncMovieLibrary(syncData, TraktSyncModes.unseen);
                    LogTraktResponse(response);
                }

                TraktLogger.Info("Now removing movies from library");
                progressDialog.Line2 = "Removing movies from library";
                response = SyncMovieLibrary(syncData, TraktSyncModes.unlibrary);
                LogTraktResponse(response);

                TraktLogger.Info("Removed all movies possible, some manual clean up may be required");
            }
            if(mode == TraktClearingModes.all)
                progressDialog.Value = 15;
            if (progressDialog.HasUserCancelled)
            {
                TraktLogger.Info("Cancelling Library Clearing");
                progressDialog.CloseDialog();
                return;
            }
            //Episodes
            if (mode == TraktClearingModes.all || mode == TraktClearingModes.episodes)
            {
                TraktLogger.Info("Removing Shows from Trakt");
                TraktLogger.Info("NOTE: WILL NOT REMOVE SCROBBLED SHOWS DUE TO API LIMITATION");

                if (clearSeen)
                {
                    TraktLogger.Info("First removing shows from seen");
                    progressDialog.Line2 = "Getting Watched Episodes from Trakt";
                    var watchedEpisodes = GetWatchedEpisodesForUser(TraktSettings.Username);
                    if (watchedEpisodes != null)
                    {
                        foreach (var series in watchedEpisodes.ToList())
                        {
                            TraktLogger.Info("Removing '{0}' from seen", series.ToString());
                            progressDialog.Line2 = string.Format("Setting {0} as unseen", series.ToString());
                            TraktResponse response = SyncEpisodeLibrary(BasicHandler.CreateEpisodeSyncData(series), TraktSyncModes.unseen);
                            LogTraktResponse(response);
                            System.Threading.Thread.Sleep(500);
                            if (progressDialog.HasUserCancelled)
                            {
                                TraktLogger.Info("Cancelling Library Clearing");
                                progressDialog.CloseDialog();
                                return;
                            }
                        }
                    }
                }
                progressDialog.Value = 85;
                TraktLogger.Info("Now removing shows from library");
                progressDialog.Line2 = "Getting Library Episodes from Trakt";
                var libraryEpisodes = GetLibraryEpisodesForUser(TraktSettings.Username);
                if (libraryEpisodes != null)
                {
                    foreach (var series in libraryEpisodes.ToList())
                    {
                        TraktLogger.Info("Removing '{0}' from library", series.ToString());
                        progressDialog.Line2 = string.Format("Removing {0} from library", series.ToString());
                        TraktResponse response = SyncEpisodeLibrary(BasicHandler.CreateEpisodeSyncData(series), TraktSyncModes.unlibrary);
                        LogTraktResponse(response);
                        System.Threading.Thread.Sleep(500);
                        if (progressDialog.HasUserCancelled)
                        {
                            TraktLogger.Info("Cancelling Library Clearing");
                            progressDialog.CloseDialog();
                            return;
                        }
                    }
                }
                TraktLogger.Info("Removed all shows possible, some manual clean up may be required");
            }
            progressDialog.Value = 100;
            progressDialog.CloseDialog();
        }

        /// <summary>
        /// Logs the result of Trakt api call
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="response"></param>
        public static bool LogTraktResponse<T>(T response)
        {
            try
            {
                if (response == null || (response as TraktResponse).Status == null)
                {
                    // server is probably temporarily unavailable
                    // return true even though it failed, so we can try again
                    // currently the return value is only being used in livetv/recordings
                    TraktLogger.Error("Response from server was unexpected.");
                    return true;
                }

                // check response error status
                if ((response as TraktResponse).Status != "success")
                {
                    if ((response as TraktResponse).Error == "The remote server returned an error: (401) Unauthorized.")
                    {
                        TraktLogger.Error("401 Unauthorized, Please check your Username and Password");
                    }
                    else
                        TraktLogger.Error((response as TraktResponse).Error);

                    return false;
                }
                else
                {
                    // success
                    if (!string.IsNullOrEmpty((response as TraktResponse).Message))
                    {
                        TraktLogger.Info("Response: {0}", (response as TraktResponse).Message);
                    }
                    else
                    {
                        // no message returned on movie sync success
                        if ((response is TraktSyncResponse))
                        {
                            string message = "Response: Items Inserted: {0}, Items Already Exist: {1}, Items Skipped: {2}";
                            TraktLogger.Info(message, (response as TraktSyncResponse).Inserted, (response as TraktSyncResponse).AlreadyExist, (response as TraktSyncResponse).Skipped);
                        }
                    }
                    return true;
                }
            }
            catch (Exception)
            {
                TraktLogger.Info("Response: {0}", "Failed to interpret response from server");
                return false;
            }
        }

        #endregion
    }
}
