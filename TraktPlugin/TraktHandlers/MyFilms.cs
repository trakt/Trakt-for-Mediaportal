using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using TraktPlugin.TraktAPI;
using TraktPlugin.TraktAPI.DataStructures;
using MediaPortal.Player;
using MediaPortal.Configuration;
using System.Reflection;
using System.ComponentModel;
using MyFilmsPlugin.MyFilms;
using System.Threading;

namespace TraktPlugin.TraktHandlers
{
    class MyFilms : ITraktHandler
    {
        #region Variables

        Timer TraktTimer;        
        MFMovie CurrentMovie = null;

        #endregion

        #region Constructor

        public MyFilms(int priority)
        {
            // check if plugin exists otherwise plugin could accidently get added to list
            string pluginFilename = Path.Combine(Config.GetSubFolder(Config.Dir.Plugins, "Windows"), "MyFilms.dll");
            if (!File.Exists(pluginFilename))
                throw new FileNotFoundException("Plugin not found!");
            else
            {
                FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(pluginFilename);
                string version = fvi.ProductVersion;
                if (new Version(version) < new Version(5,0,1,1173))
                    throw new FileLoadException("Plugin does not meet minimum requirements!");
            }
            
            // Subscribe to GUI Events
            TraktLogger.Debug("Adding Hooks to My Films");
            MyFilmsPlugin.MyFilms.MyFilmsGUI.MyFilmsDetail.RateItem += new MyFilmsPlugin.MyFilms.MyFilmsGUI.MyFilmsDetail.RatingEventDelegate(OnRateItem);
            MyFilmsPlugin.MyFilms.MyFilmsGUI.MyFilmsDetail.WatchedItem += new MyFilmsPlugin.MyFilms.MyFilmsGUI.MyFilmsDetail.WatchedEventDelegate(OnToggleWatched);

            Priority = priority;
        }

        #endregion

        #region ITraktHandler

        public string Name
        {
            get { return "My Films"; }
        }

        public int Priority { get; set; }
       
        public void SyncLibrary()
        {
            TraktLogger.Info("MyFilms Starting Sync");

            // get all movies
            ArrayList myvideos = new ArrayList();
            BaseMesFilms.GetMovies(ref myvideos);
            TraktLogger.Info("BaseMesFilms.GetMovies: returning " + myvideos.Count + " movies");

            List<MFMovie> MovieList = (from MFMovie movie in myvideos select movie).ToList();

            // Remove any blocked movies
            MovieList.RemoveAll(movie => TraktSettings.BlockedFolders.Any(f => movie.Path.Contains(f)));
            MovieList.RemoveAll(movie => TraktSettings.BlockedFilenames.Contains(movie.File));

            #region Skipped Movies Check
            // Remove Skipped Movies from previous Sync
            if (TraktSettings.SkippedMovies != null)
            {
                // allow movies to re-sync again after 7-days in the case user has addressed issue ie. edited movie or added to themoviedb.org
                if (TraktSettings.SkippedMovies.LastSkippedSync.FromEpoch() > DateTime.UtcNow.Subtract(new TimeSpan(7, 0, 0, 0)))
                {
                    if (TraktSettings.SkippedMovies.Movies != null)
                    {
                        TraktLogger.Info("Skipping {0} movies due to invalid data or movies don't exist on http://themoviedb.org. Next check will be {1}.", TraktSettings.SkippedMovies.Movies.Count, TraktSettings.SkippedMovies.LastSkippedSync.FromEpoch().Add(new TimeSpan(7,0,0,0)));
                        foreach (var movie in TraktSettings.SkippedMovies.Movies)
                        {
                            TraktLogger.Info("Skipping movie, Title: {0}, Year: {1}, IMDb: {2}", movie.Title, movie.Year, movie.IMDBID);
                            MovieList.RemoveAll(m => (m.Title == movie.Title) && (m.Year.ToString() == movie.Year) && (m.IMDBNumber == movie.IMDBID));
                        }
                    }
                }
                else
                {
                    if (TraktSettings.SkippedMovies.Movies != null) TraktSettings.SkippedMovies.Movies.Clear();
                    TraktSettings.SkippedMovies.LastSkippedSync = DateTime.UtcNow.ToEpoch();
                }
            }
            #endregion

            TraktLogger.Info("{0} movies available to sync in MyFilms database(s)", MovieList.Count.ToString());

            // get the movies that we have watched
            List<MFMovie> SeenList = MovieList.Where(m => m.Watched == true).ToList();

            TraktLogger.Info("{0} watched movies available to sync in MyFilms database(s)", SeenList.Count.ToString());

            // get the movies that we have yet to watch                        
            IEnumerable<TraktLibraryMovies> traktMoviesAll = TraktAPI.TraktAPI.GetAllMoviesForUser(TraktSettings.Username);
            if (traktMoviesAll == null)
            {
                TraktLogger.Error("Error getting movies from trakt server, cancelling sync.");
                return;
            }
            TraktLogger.Info("{0} movies in trakt library", traktMoviesAll.Count().ToString());

            #region Movies to Sync to Collection
            List<MFMovie> moviesToSync = new List<MFMovie>(MovieList);
            List<TraktLibraryMovies> NoLongerInOurCollection = new List<TraktLibraryMovies>();            
            //Filter out a list of movies we have already sync'd in our collection
            foreach (TraktLibraryMovies tlm in traktMoviesAll)
            {
                bool notInLocalCollection = true;
                // if it is in both libraries
                foreach (MFMovie libraryMovie in MovieList.Where(m => BasicHandler.GetProperMovieImdbId(m.IMDBNumber) == tlm.IMDBID || (string.Compare(m.Title, tlm.Title, true) == 0 && m.Year.ToString() == tlm.Year)))
                {
                    // if the users IMDb ID is empty and we have matched one then set it
                    if (!String.IsNullOrEmpty(tlm.IMDBID) && (String.IsNullOrEmpty(libraryMovie.IMDBNumber) || libraryMovie.IMDBNumber.Length != 9))
                    {
                        TraktLogger.Info("Movie '{0}' inserted IMDb ID '{1}'", libraryMovie.Title, tlm.IMDBID);
                        libraryMovie.IMDBNumber = tlm.IMDBID;
                        libraryMovie.Username = TraktSettings.Username;
                        libraryMovie.Commit();
                    }

                    // if it is watched in Trakt but not My Films update
                    // skip if movie is watched but user wishes to have synced as unseen locally
                    if (tlm.Plays > 0 && !tlm.UnSeen && libraryMovie.Watched == false)
                    {
                        TraktLogger.Info("Movie '{0}' is watched on Trakt updating Database", libraryMovie.Title);
                        libraryMovie.Watched = true;
                        libraryMovie.WatchedCount = tlm.Plays;
                        libraryMovie.Username = TraktSettings.Username; 
                        libraryMovie.Commit();
                    }

                    // mark movies as unseen if watched locally
                    if (tlm.UnSeen && libraryMovie.Watched == true)
                    {
                        TraktLogger.Info("Movie '{0}' is unseen on Trakt, updating database", libraryMovie.Title);
                        libraryMovie.Watched = false;
                        libraryMovie.WatchedCount = tlm.Plays;
                        libraryMovie.Username = TraktSettings.Username; 
                        libraryMovie.Commit();
                    }

                    notInLocalCollection = false;

                    //filter out if its already in collection
                    if (tlm.InCollection)
                    {
                        moviesToSync.RemoveAll(m => (BasicHandler.GetProperMovieImdbId(m.IMDBNumber) == tlm.IMDBID) || (string.Compare(m.Title, tlm.Title, true) == 0 && m.Year.ToString() == tlm.Year));
                    }
                    break;
                }

                if (notInLocalCollection && tlm.InCollection)
                    NoLongerInOurCollection.Add(tlm);
            }
            #endregion

            #region Movies to Sync to Seen Collection
            // filter out a list of movies already marked as watched on trakt
            // also filter out movie marked as unseen so we dont reset the unseen cache online
            List<MFMovie> watchedMoviesToSync = new List<MFMovie>(SeenList);
            foreach (TraktLibraryMovies tlm in traktMoviesAll.Where(t => t.Plays > 0 || t.UnSeen))
            {
                foreach (MFMovie watchedMovie in SeenList.Where(m => BasicHandler.GetProperMovieImdbId(m.IMDBNumber) == tlm.IMDBID || (string.Compare(m.Title, tlm.Title, true) == 0 && m.Year.ToString() == tlm.Year)))
                {
                    //filter out
                    watchedMoviesToSync.Remove(watchedMovie);
                }
            }
            #endregion

            //Send Library/Collection
            TraktLogger.Info("{0} movies need to be added to Library", moviesToSync.Count.ToString());
            foreach (MFMovie m in moviesToSync)
                TraktLogger.Info("Sending movie to trakt library, Title: {0}, Year: {1}, IMDb: {2}", m.Title, m.Year.ToString(), m.IMDBNumber);

            if (moviesToSync.Count > 0)
            {
                TraktMovieSyncResponse response = TraktAPI.TraktAPI.SyncMovieLibrary(CreateSyncData(moviesToSync), TraktSyncModes.library);
                BasicHandler.InsertSkippedMovies(response);
                TraktAPI.TraktAPI.LogTraktResponse(response);
            }

            //Send Seen
            TraktLogger.Info("{0} movies need to be added to SeenList", watchedMoviesToSync.Count.ToString());
            foreach (MFMovie m in watchedMoviesToSync)
                TraktLogger.Info("Sending movie to trakt as seen, Title: {0}, Year: {1}, IMDb: {2}", m.Title, m.Year.ToString(), m.IMDBNumber);

            if (watchedMoviesToSync.Count > 0)
            {
                TraktMovieSyncResponse response = TraktAPI.TraktAPI.SyncMovieLibrary(CreateSyncData(watchedMoviesToSync), TraktSyncModes.seen);
                BasicHandler.InsertSkippedMovies(response);
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

            TraktLogger.Info("MyFilms Sync Completed");
        }

        public bool Scrobble(string filename)
        {
            StopScrobble();

            // lookup movie by filename
            ArrayList myvideos = new ArrayList();
            BaseMesFilms.GetMovies(ref myvideos);

            MFMovie movie = (from MFMovie m in myvideos select m).ToList().Find(m => m.File == filename);
            if (movie == null) return false;

            CurrentMovie = movie;

            // create 15 minute timer to send watching status
            #region scrobble timer
            TraktTimer = new Timer(new TimerCallback((stateInfo) =>
            {
                MFMovie currentMovie = stateInfo as MFMovie;

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
                MFMovie movie = o as MFMovie;
                if (movie == null) return;

                TraktLogger.Info("MyFilms movie considered watched '{0}'", movie.Title);

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

            // if movie is atleast 90% complete, consider watched
            if ((g_Player.CurrentPosition / g_Player.Duration) >= 0.9)
            {
                scrobbleMovie.Start(CurrentMovie);
            }
            else
            {
                TraktLogger.Info("Stopped MyFilms movie playback '{0}'", CurrentMovie.Title);

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
        public static TraktMovieSync CreateSyncData(List<MFMovie> Movies)
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
        public static TraktMovieSync CreateSyncData(MFMovie Movie)
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
        public static TraktMovieScrobble CreateScrobbleData(MFMovie movie)
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
                PluginVersion = TraktSettings.Version,
                MediaCenter = "Mediaportal",
                MediaCenterVersion = Assembly.GetEntryAssembly().GetName().Version.ToString(),
                MediaCenterBuildDate = String.Empty,
                UserName = username,
                Password = password
            };
            return scrobbleData;
        }

        public static TraktRateMovie CreateRateData(MFMovie movie, String rating)
        {
            string username = TraktSettings.Username;
            string password = TraktSettings.Password;

            if (String.IsNullOrEmpty(username) || String.IsNullOrEmpty(password))
                return null;

            TraktRateMovie rateData = new TraktRateMovie
            {
                Title = movie.Title,
                Year = movie.Year.ToString(),
                IMDBID = movie.IMDBNumber,
                TMDBID = null,
                UserName = username,
                Password = password,
                Rating = rating
            };
            return rateData;
        }

        #endregion

        #region Public Methods
        
        public void DisposeEvents()
        {
            TraktLogger.Debug("Removing Hooks from My Films");
            
            // gui events
            MyFilmsPlugin.MyFilms.MyFilmsGUI.MyFilmsDetail.RateItem -= new MyFilmsPlugin.MyFilms.MyFilmsGUI.MyFilmsDetail.RatingEventDelegate(OnRateItem);
            MyFilmsPlugin.MyFilms.MyFilmsGUI.MyFilmsDetail.WatchedItem -= new MyFilmsPlugin.MyFilms.MyFilmsGUI.MyFilmsDetail.WatchedEventDelegate(OnToggleWatched);
        }

        #endregion

        #region GUI Events

        private void OnRateItem(MFMovie movie, string value)
        {
            TraktLogger.Info("Recieved rating event from MyFilms");

            if (TraktSettings.AccountStatus != ConnectionState.Connected) return;

            // don't do anything if movie is blocked
            if (TraktSettings.BlockedFilenames.Contains(movie.File) || TraktSettings.BlockedFolders.Any(f => movie.File.Contains(f)))
            {
                TraktLogger.Info("Movie {0} is on the blocked list so we didn't update Trakt", movie.Title);
                return;
            }

            // Add setting for this later to control love/hate value
            double rating = Convert.ToDouble(value);
            TraktRateResponse response = null;

            Thread rateThread = new Thread((o) =>
            {
                MFMovie tMovie = o as MFMovie;

                if (rating >= 7.0)
                    response = TraktAPI.TraktAPI.RateMovie(CreateRateData(tMovie, TraktRateValue.love.ToString()));
                else
                    response = TraktAPI.TraktAPI.RateMovie(CreateRateData(tMovie, TraktRateValue.hate.ToString()));

                TraktAPI.TraktAPI.LogTraktResponse(response);
            })
            {
                IsBackground = true,
                Name = "My Films Rate"
            };

            rateThread.Start(movie);
        }

        private void OnToggleWatched(MFMovie movie, bool watched, int count)
        {
            TraktLogger.Info("Received togglewatched event from MyFilms");

            if (TraktSettings.AccountStatus != ConnectionState.Connected) return;

            // don't do anything if movie is blocked
            if (TraktSettings.BlockedFilenames.Contains(movie.File) || TraktSettings.BlockedFolders.Any(f => movie.File.Contains(f)))
            {
                TraktLogger.Info("Movie {0} is on the blocked list so we didn't update Trakt", movie.Title);
                return;
            }

            Thread toggleWatchedThread = new Thread((o) =>
            {
                MFMovie tMovie = o as MFMovie;
                TraktResponse response = TraktAPI.TraktAPI.SyncMovieLibrary(CreateSyncData(tMovie), watched ? TraktSyncModes.seen : TraktSyncModes.unseen);
                TraktAPI.TraktAPI.LogTraktResponse(response);
            })
            {
                IsBackground = true,
                Name = "My Films Toggle Watched"
            };

            toggleWatchedThread.Start(movie);
        }

        #endregion

        #region Other Public Methods
        public static bool FindMovie(string title, int year, string imdbid, ref int? movieid, ref string config)
        {
            // get all movies
            ArrayList myvideos = new ArrayList();
            BaseMesFilms.GetMovies(ref myvideos);

            // get all movies in local database
            List<MFMovie> movies = (from MFMovie m in myvideos select m).ToList();

            // try find a match
            MFMovie movie = movies.Find(m => BasicHandler.GetProperMovieImdbId(m.IMDBNumber) == imdbid || (string.Compare(m.Title, title, true) == 0 && m.Year == year));
            if (movie == null) return false;

            movieid = movie.ID;
            config = movie.Config;
            return true;
        }
        #endregion

    }
}
