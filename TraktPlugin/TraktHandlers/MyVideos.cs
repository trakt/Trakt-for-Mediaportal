using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TraktPlugin.TraktAPI;
using TraktPlugin.TraktAPI.DataStructures;
using MediaPortal.Player;
using System.Reflection;
using System.ComponentModel;
using MediaPortal.Video.Database;
using System.Threading;

namespace TraktPlugin.TraktHandlers
{
    class MyVideos : ITraktHandler
    {
        #region Variables

        Timer TraktTimer;        
        IMDBMovie CurrentMovie = null;

        #endregion

        #region Constructor

        public MyVideos(int priority)
        {
            Priority = priority;
        }

        #endregion

        #region ITraktHandler

        public string Name
        {
            get { return "My Videos"; }
        }

        public int Priority { get; set; }
       
        public void SyncLibrary()
        {
            TraktLogger.Info("My Videos Starting Sync");

            // get all movies
            ArrayList myvideos = new ArrayList();
            VideoDatabase.GetMovies(ref myvideos);

            List<IMDBMovie> MovieList = (from IMDBMovie movie in myvideos select movie).ToList();

            // Remove any blocked movies
            MovieList.RemoveAll(m => TraktSettings.BlockedFolders.Any(f => m.Path.Contains(f)));

            List<int> blockedMovieIds = new List<int>();
            foreach(string file in TraktSettings.BlockedFilenames)
            {
                int pathId = 0;
                int movieId = 0;

                // get a list of ids for blocked filenames
                // filename seems to always be empty for an IMDBMovie object!
                if (VideoDatabase.GetFile(file, out pathId, out movieId, false) > 0)
                {
                    blockedMovieIds.Add(movieId);
                }
            }
            MovieList.RemoveAll(m => blockedMovieIds.Contains(m.ID));

            TraktLogger.Info("{0} movies available to sync in My Videos database", MovieList.Count.ToString());

            // get the movies that we have watched
            List<IMDBMovie> SeenList = MovieList.Where(m => m.Watched > 0).ToList();

            TraktLogger.Info("{0} watched movies available to sync in My Videos database", SeenList.Count.ToString());

            // get the movies that we have yet to watch                        
            IEnumerable<TraktLibraryMovies> traktMoviesAll = TraktAPI.TraktAPI.GetAllMoviesForUser(TraktSettings.Username);
            TraktLogger.Info("{0} movies in trakt library", traktMoviesAll.Count().ToString());

            #region Movies to Sync to Collection
            List<IMDBMovie> moviesToSync = new List<IMDBMovie>(MovieList);
            List<TraktLibraryMovies> NoLongerInOurCollection = new List<TraktLibraryMovies>();            
            //Filter out a list of movies we have already sync'd in our collection
            foreach (TraktLibraryMovies tlm in traktMoviesAll)
            {
                bool notInLocalCollection = true;
                // if it is in both libraries
                foreach (IMDBMovie libraryMovie in MovieList.Where(m => m.IMDBNumber == tlm.IMDBID || (m.Title == tlm.Title && m.Year.ToString() == tlm.Year)))
                {
                    // if the users IMDB ID is empty and we have matched one then set it
                    if (!String.IsNullOrEmpty(tlm.IMDBID) && (String.IsNullOrEmpty(libraryMovie.IMDBNumber) || libraryMovie.IMDBNumber.Length != 9))
                    {
                        TraktLogger.Info("Movie {0} inserted IMDBID {1}", libraryMovie.Title, tlm.IMDBID);
                        libraryMovie.IMDBNumber = tlm.IMDBID;
                        IMDBMovie details = libraryMovie;
                        VideoDatabase.SetMovieInfoById(libraryMovie.ID, ref details);
                    }

                    // if it is watched in Trakt but not My Videos update
                    if (tlm.Plays > 0 && libraryMovie.Watched == 0)
                    {
                        TraktLogger.Info("Movie {0} is watched on Trakt updating Database", libraryMovie.Title);
                        libraryMovie.Watched = 1;
                        IMDBMovie details = libraryMovie;
                        VideoDatabase.SetMovieInfoById(libraryMovie.ID, ref details);
                    }

                    notInLocalCollection = false;

                    //filter out if its already in collection
                    if (tlm.InCollection)
                    {
                        if (!string.IsNullOrEmpty(tlm.IMDBID))
                            moviesToSync.RemoveAll(m => m.IMDBNumber == tlm.IMDBID);
                        else
                            moviesToSync.RemoveAll(m => m.Title == tlm.Title && m.Year.ToString() == tlm.Year);
                    }
                    break;
                }

                if (notInLocalCollection && tlm.InCollection)
                    NoLongerInOurCollection.Add(tlm);
            }
            #endregion

            #region Movies to Sync to Seen Collection
            //Filter out a list of movies already marked as watched on trakt
            List<IMDBMovie> watchedMoviesToSync = new List<IMDBMovie>(SeenList);
            foreach (TraktLibraryMovies tlm in traktMoviesAll.Where(t => t.Plays > 0))
            {
                foreach (IMDBMovie watchedMovie in SeenList.Where(m => m.IMDBNumber == tlm.IMDBID || (m.Title == tlm.Title && m.Year.ToString() == tlm.Year)))
                {
                    //filter out
                    watchedMoviesToSync.Remove(watchedMovie);
                }
            }
            #endregion

            //Send Library/Collection
            TraktLogger.Info("{0} movies need to be added to Library", moviesToSync.Count.ToString());
            foreach (IMDBMovie m in moviesToSync)
                TraktLogger.Info("Sending movie to trakt library, Title: {0}, Year: {1}, IMDB: {2}", m.Title, m.Year.ToString(), m.IMDBNumber);

            if (moviesToSync.Count > 0)
            {
                TraktMovieSyncResponse response = TraktAPI.TraktAPI.SyncMovieLibrary(CreateSyncData(moviesToSync), TraktSyncModes.library);
                TraktAPI.TraktAPI.LogTraktResponse(response);
            }

            //Send Seen
            TraktLogger.Info("{0} movies need to be added to SeenList", watchedMoviesToSync.Count.ToString());
            foreach (IMDBMovie m in watchedMoviesToSync)
                TraktLogger.Info("Sending movie to trakt as seen, Title: {0}, Year: {1}, IMDB: {2}", m.Title, m.Year.ToString(), m.IMDBNumber);

            if (watchedMoviesToSync.Count > 0)
            {
                TraktMovieSyncResponse response = TraktAPI.TraktAPI.SyncMovieLibrary(CreateSyncData(watchedMoviesToSync), TraktSyncModes.seen);
                TraktAPI.TraktAPI.LogTraktResponse(response);
            }

            //Dont clean library if more than one movie plugin installed
            if (TraktSettings.KeepTraktLibraryClean && TraktSettings.MoviePluginCount == 1)
            {
                //Remove movies we no longer have in our local database from Trakt
                foreach (var m in NoLongerInOurCollection)
                    TraktLogger.Info("Removing from Trakt Collection {0}", m.Title);

                TraktLogger.Info("{0} movies need to be removed from Trakt Collection", NoLongerInOurCollection.Count.ToString());

                if (NoLongerInOurCollection.Count > 0)
                {
                    //Then remove from library
                    TraktMovieSyncResponse response = TraktAPI.TraktAPI.SyncMovieLibrary(BasicHandler.CreateMovieSyncData(NoLongerInOurCollection), TraktSyncModes.unlibrary);
                    TraktAPI.TraktAPI.LogTraktResponse(response);
                }                
            }

            TraktLogger.Info("My Videos Sync Completed");
        }

        public bool Scrobble(string filename)
        {
            StopScrobble();

            // lookup movie by filename
            IMDBMovie movie = new IMDBMovie();
            int result = VideoDatabase.GetMovieInfo(filename, ref movie);
            if (result == -1) return false;

            CurrentMovie = movie;

            // create timer 15 minute timer to send watching status
            #region scrobble timer
            TraktTimer = new Timer(new TimerCallback((stateInfo) =>
            {
                IMDBMovie currentMovie = stateInfo as IMDBMovie;

                TraktLogger.Info("Scrobbling Movie {0}", movie.Title);
                
                double duration = g_Player.Duration;
                double progress = 0.0;

                // get current progress of player (in seconds) to work out percent complete
                if (duration > 0.0)
                    progress = (g_Player.CurrentPosition / duration) * 100.0;

                // create Scrobbling Data
                TraktMovieScrobble scrobbleData = CreateScrobbleData(currentMovie);
                if (scrobbleData == null) return;

                // set duration/progress in scrobble data
                scrobbleData.Duration = Convert.ToInt32(duration / 60).ToString();
                scrobbleData.Progress = Convert.ToInt32(progress).ToString();

                // set watching status on trakt
                TraktResponse response = TraktAPI.TraktAPI.ScrobbleMovieState(scrobbleData, TraktScrobbleStates.watching);
                TraktAPI.TraktAPI.LogTraktResponse(response);
            }), movie, 3000, 900000);
            #endregion

            return true;
        }

        public void StopScrobble()
        {
            if (TraktTimer != null)
                TraktTimer.Dispose();

            if (CurrentMovie == null) return;

            Thread scrobbleMovie = new Thread(delegate(object o)
            {
                IMDBMovie movie = o as IMDBMovie;
                if (movie == null) return;

                TraktLogger.Info("MyVideos movie considered watched '{0}'", movie.Title);

                // get scrobble data to send to api
                TraktMovieScrobble scrobbleData = CreateScrobbleData(movie);
                if (scrobbleData == null) return;

                // set duration/progress in scrobble data                
                scrobbleData.Duration = Convert.ToInt32(g_Player.Duration / 60).ToString();
                scrobbleData.Progress = "100";

                TraktResponse response = TraktAPI.TraktAPI.ScrobbleMovieState(scrobbleData, TraktScrobbleStates.scrobble);
                TraktAPI.TraktAPI.LogTraktResponse(response);
            })
            {
                IsBackground = true,
                Name = "Scrobble Movie"
            };

            // if movie is atleat 90% complete, consider watched
            if ((g_Player.CurrentPosition / g_Player.Duration) >= 0.9)
            {
                scrobbleMovie.Start(CurrentMovie);
            }
            else
            {
                TraktLogger.Info("Stopped MyVideos movies playback '{0}'", CurrentMovie.Title);

                // stop scrobbling
                Thread cancelWatching = new Thread(delegate()
                {
                    TraktMovieScrobble scrobbleData = new TraktMovieScrobble { UserName = TraktSettings.Username, Password = TraktSettings.Password };
                    TraktResponse response = TraktAPI.TraktAPI.ScrobbleMovieState(scrobbleData, TraktScrobbleStates.cancelwatching);
                    TraktAPI.TraktAPI.LogTraktResponse(response);
                })
                {
                    IsBackground = true,
                    Name = "Cancel Watching Movie"
                };

                cancelWatching.Start();
            }

            CurrentMovie = null;            
        }

        #endregion

        #region DataCreators

        /// <summary>
        /// Creates Sync Data based on a List of IMDBMovie objects
        /// </summary>
        /// <param name="Movies">The movies to base the object on</param>
        /// <returns>The Trakt Sync data to send</returns>
        public static TraktMovieSync CreateSyncData(List<IMDBMovie> Movies)
        {
            string username = TraktSettings.Username;
            string password = TraktSettings.Password;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                return null;

            List<TraktMovieSync.Movie> moviesList = (from m in Movies
                                                     select new TraktMovieSync.Movie
                                                     {
                                                         IMDBID = m.IMDBNumber,
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
        /// Creates Sync Data based on a single IMDBMovie object
        /// </summary>
        /// <param name="Movie">The movie to base the object on</param>
        /// <returns>The Trakt Sync data to send</returns>
        public static TraktMovieSync CreateSyncData(IMDBMovie Movie)
        {
            string username = TraktSettings.Username;
            string password = TraktSettings.Password;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                return null;

            List<TraktMovieSync.Movie> moviesList = new List<TraktMovieSync.Movie>();
            moviesList.Add(new TraktMovieSync.Movie
            {
                IMDBID = Movie.IMDBNumber,
                Title = Movie.Title,
                Year = Movie.Year.ToString()
            });

            TraktMovieSync syncData = new TraktMovieSync
            {
                UserName = username,
                Password = password,
                MovieList = moviesList
            };
            return syncData;
        }

        /// <summary>
        /// Creates Scrobble data based on a IMDBMovie object
        /// </summary>
        /// <param name="movie">The movie to base the object on</param>
        /// <returns>The Trakt scrobble data to send</returns>
        public static TraktMovieScrobble CreateScrobbleData(IMDBMovie movie)
        {
            string username = TraktSettings.Username;
            string password = TraktSettings.Password;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                return null;

            TraktMovieScrobble scrobbleData = new TraktMovieScrobble
            {
                Title = movie.Title,
                Year = movie.Year.ToString(),
                IMDBID = movie.IMDBNumber,
                PluginVersion = Assembly.GetCallingAssembly().GetName().Version.ToString(),
                MediaCenter = "Mediaportal",
                MediaCenterVersion = Assembly.GetEntryAssembly().GetName().Version.ToString(),
                MediaCenterBuildDate = String.Empty,
                UserName = username,
                Password = password
            };
            return scrobbleData;
        }

        #endregion

    }
}
