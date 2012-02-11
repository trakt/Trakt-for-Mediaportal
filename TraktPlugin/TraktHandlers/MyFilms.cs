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
using MyFilmsPlugin.MyFilms.MyFilmsGUI;
using System.Threading;

namespace TraktPlugin.TraktHandlers
{
    class MyFilmsHandler : ITraktHandler
    {
        #region Variables

        Timer TraktTimer;
        MFMovie CurrentMovie = null;
        bool SyncInProgress = false;

        #endregion

        #region Constructor

        public MyFilmsHandler(int priority)
        {
            // check if plugin exists otherwise plugin could accidently get added to list
            string pluginFilename = Path.Combine(Config.GetSubFolder(Config.Dir.Plugins, "Windows"), "MyFilms.dll");
            if (!File.Exists(pluginFilename))
                throw new FileNotFoundException("Plugin not found!");
            else
            {
                FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(pluginFilename);
                string version = fvi.ProductVersion;
                if (new Version(version) < new Version(5,0,3,1488))
                    throw new FileLoadException("Plugin does not meet minimum requirements!");
            }

            // Subscribe to Events
            TraktLogger.Debug("Adding Hooks to My Films");
            MyFilmsDetail.RateItem += new MyFilmsDetail.RatingEventDelegate(OnRateItem);
            MyFilmsDetail.WatchedItem += new MyFilmsDetail.WatchedEventDelegate(OnToggleWatched);
            MyFilmsDetail.MovieStarted += new MyFilmsDetail.MovieStartedEventDelegate(OnStartedMovie);
            MyFilmsDetail.MovieStopped += new MyFilmsDetail.MovieStoppedEventDelegate(OnStoppedMovie);
            MyFilmsDetail.MovieWatched += new MyFilmsDetail.MovieWatchedEventDelegate(OnWatchedMovie);
            MyFilms.ImportComplete += new MyFilms.ImportCompleteEventDelegate(OnImportComplete);

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
            TraktLogger.Info("My Films Starting Sync");
            SyncInProgress = true;

            // get all movies
            ArrayList myvideos = new ArrayList();
            BaseMesFilms.GetMovies(ref myvideos);
            TraktLogger.Info("BaseMesFilms.GetMovies: returning " + myvideos.Count + " movies");

            List<MFMovie> MovieList = (from MFMovie movie in myvideos select movie).ToList();

            // Remove any blocked movies
            MovieList.RemoveAll(movie => TraktSettings.BlockedFolders.Any(f => movie.File.ToLowerInvariant().Contains(f.ToLowerInvariant())));
            MovieList.RemoveAll(movie => TraktSettings.BlockedFilenames.Contains(movie.File));

            #region Skipped Movies Check
            // Remove Skipped Movies from previous Sync
            if (TraktSettings.SkippedMovies != null)
            {
                // allow movies to re-sync again after 7-days in the case user has addressed issue ie. edited movie or added to themoviedb.org
                if (TraktSettings.SkippedMovies.LastSkippedSync.FromEpoch() > DateTime.UtcNow.Subtract(new TimeSpan(7, 0, 0, 0)))
                {
                    if (TraktSettings.SkippedMovies.Movies != null && TraktSettings.SkippedMovies.Movies.Count > 0)
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

            #region Already Exists Movie Check
            // Remove Already-Exists Movies, these are typically movies that are using aka names and no IMDb/TMDb set
            // When we compare our local collection with trakt collection we have english only titles, so if no imdb/tmdb exists
            // we need to fallback to title matching. When we sync aka names are sometimes accepted if defined on themoviedb.org so we need to 
            // do this to revent syncing these movies every sync interval.
            if (TraktSettings.AlreadyExistMovies != null && TraktSettings.AlreadyExistMovies.Movies != null && TraktSettings.AlreadyExistMovies.Movies.Count > 0)
            {
                TraktLogger.Debug("Skipping {0} movies as they already exist in trakt library but failed local match previously.", TraktSettings.AlreadyExistMovies.Movies.Count.ToString());
                var movies = new List<TraktMovieSync.Movie>(TraktSettings.AlreadyExistMovies.Movies);
                foreach (var movie in movies)
                {
                    Predicate<MFMovie> criteria = m => (m.Title == movie.Title) && (m.Year.ToString() == movie.Year) && (m.IMDBNumber == movie.IMDBID);
                    if (MovieList.Exists(criteria))
                    {
                        TraktLogger.Debug("Skipping movie, Title: {0}, Year: {1}, IMDb: {2}", movie.Title, movie.Year, movie.IMDBID);
                        MovieList.RemoveAll(criteria);
                    }
                    else
                    {
                        // remove as we have now removed from our local collection or updated movie signature
                        if (TraktSettings.MoviePluginCount == 1)
                        {
                            TraktLogger.Debug("Removing 'AlreadyExists' movie, Title: {0}, Year: {1}, IMDb: {2}", movie.Title, movie.Year, movie.IMDBID);
                            TraktSettings.AlreadyExistMovies.Movies.Remove(movie);
                        }
                    }
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
                SyncInProgress = false;
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
                    // If the users IMDb Id is empty/invalid and we have matched one then set it
                    if (BasicHandler.IsValidImdb(tlm.IMDBID) && !BasicHandler.IsValidImdb(libraryMovie.IMDBNumber))
                    {
                        TraktLogger.Info("Movie '{0}' inserted IMDb Id '{1}'", libraryMovie.Title, tlm.IMDBID);
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
                TraktSyncResponse response = TraktAPI.TraktAPI.SyncMovieLibrary(CreateSyncData(moviesToSync), TraktSyncModes.library);
                BasicHandler.InsertSkippedMovies(response);
                BasicHandler.InsertAlreadyExistMovies(response);
                TraktAPI.TraktAPI.LogTraktResponse(response);
            }

            //Send Seen
            TraktLogger.Info("{0} movies need to be added to SeenList", watchedMoviesToSync.Count.ToString());
            foreach (MFMovie m in watchedMoviesToSync)
                TraktLogger.Info("Sending movie to trakt as seen, Title: {0}, Year: {1}, IMDb: {2}", m.Title, m.Year.ToString(), m.IMDBNumber);

            if (watchedMoviesToSync.Count > 0)
            {
                TraktSyncResponse response = TraktAPI.TraktAPI.SyncMovieLibrary(CreateSyncData(watchedMoviesToSync), TraktSyncModes.seen);
                BasicHandler.InsertSkippedMovies(response);
                BasicHandler.InsertAlreadyExistMovies(response);
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
                    if (TraktSettings.AlreadyExistMovies != null && TraktSettings.AlreadyExistMovies.Movies != null && TraktSettings.AlreadyExistMovies.Movies.Count > 0)
                    {
                        TraktLogger.Warning("DISABLING CLEAN LIBRARY!!!, there are trakt library movies that can't be determined to be local in collection.");
                        TraktLogger.Warning("To fix this, check the 'already exist' entries in log, then check movies in local collection against this list and ensure IMDb id is set then run sync again.");
                    }
                    else
                    {
                        //Then remove from library
                        TraktSyncResponse response = TraktAPI.TraktAPI.SyncMovieLibrary(BasicHandler.CreateMovieSyncData(NoLongerInOurCollection), TraktSyncModes.unlibrary);
                        TraktAPI.TraktAPI.LogTraktResponse(response);
                    }
                }                
            }

            SyncInProgress = false;
            TraktLogger.Info("My Films Sync Completed");
        }

        public bool Scrobble(string filename)
        {
            // check movie is from my films
            if (CurrentMovie == null) return false;

            StopScrobble();

            // create 15 minute timer to send watching status
            #region scrobble timer
            TraktTimer = new Timer(new TimerCallback((stateInfo) =>
            {
                Thread.CurrentThread.Name = "Scrobble Movie";

                MFMovie currentMovie = stateInfo as MFMovie;

                TraktLogger.Info("Scrobbling Movie {0}", currentMovie.Title);
                
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
            }), CurrentMovie, 3000, 900000);
            #endregion

            return true;
        }

        public void StopScrobble()
        {
            if (TraktTimer != null)
                TraktTimer.Dispose();
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
            
            // unsubscribe from events
            MyFilmsDetail.RateItem -= new MyFilmsDetail.RatingEventDelegate(OnRateItem);
            MyFilmsDetail.WatchedItem -= new MyFilmsDetail.WatchedEventDelegate(OnToggleWatched);
            MyFilmsDetail.MovieStarted -= new MyFilmsDetail.MovieStartedEventDelegate(OnStartedMovie);
            MyFilmsDetail.MovieStopped -= new MyFilmsDetail.MovieStoppedEventDelegate(OnStoppedMovie);
            MyFilmsDetail.MovieWatched -= new MyFilmsDetail.MovieWatchedEventDelegate(OnWatchedMovie);
            MyFilms.ImportComplete -= new MyFilms.ImportCompleteEventDelegate(OnImportComplete);
        }

        #endregion

        #region Player Events

        private void OnStartedMovie(MFMovie movie)
        {
            if (TraktSettings.AccountStatus != ConnectionState.Connected) return;

            if (!TraktSettings.BlockedFilenames.Contains(movie.File) && !TraktSettings.BlockedFolders.Any(f => movie.File.ToLowerInvariant().Contains(f.ToLowerInvariant())))
            {
                TraktLogger.Info("Starting My Films movie playback: '{0}'", movie.Title);
                CurrentMovie = movie;
            }
        }

        private void OnStoppedMovie(MFMovie movie)
        {
            if (TraktSettings.AccountStatus != ConnectionState.Connected) return;

            if (!TraktSettings.BlockedFilenames.Contains(movie.File) && !TraktSettings.BlockedFolders.Any(f => movie.File.ToLowerInvariant().Contains(f.ToLowerInvariant())))
            {
                TraktLogger.Info("Stopped My Films movie playback: '{0}'", movie.Title);

                CurrentMovie = null;
                StopScrobble();

                // send cancelled watching state
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
        }

        private void OnWatchedMovie(MFMovie movie)
        {
            if (TraktSettings.AccountStatus != ConnectionState.Connected) return;
            
            CurrentMovie = null;

            if (!TraktSettings.BlockedFilenames.Contains(movie.File) && !TraktSettings.BlockedFolders.Any(f => movie.File.ToLowerInvariant().Contains(f.ToLowerInvariant())))
            {
                Thread scrobbleMovie = new Thread(delegate(object obj)
                {
                    MFMovie watchedMovie = obj as MFMovie;
                    if (watchedMovie == null) return;

                    TraktLogger.Info("My Films movie considered watched: '{0}'", watchedMovie.Title);

                    // get scrobble data to send to api
                    TraktMovieScrobble scrobbleData = CreateScrobbleData(watchedMovie);
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

                scrobbleMovie.Start(movie);
            }
        }

        #endregion

        #region GUI Events

        private void OnRateItem(MFMovie movie, string value)
        {
            TraktLogger.Info("Recieved rating event from MyFilms");

            if (TraktSettings.AccountStatus != ConnectionState.Connected) return;

            // don't do anything if movie is blocked
            if (TraktSettings.BlockedFilenames.Contains(movie.File) || TraktSettings.BlockedFolders.Any(f => movie.File.ToLowerInvariant().Contains(f.ToLowerInvariant())))
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

                response = TraktAPI.TraktAPI.RateMovie(CreateRateData(tMovie, TraktHelper.GetRateValue(rating).ToString()));

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
            if (TraktSettings.BlockedFilenames.Contains(movie.File) || TraktSettings.BlockedFolders.Any(f => movie.File.ToLowerInvariant().Contains(f.ToLowerInvariant())))
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

        #region Import Events

        private void OnImportComplete()
        {
            if (TraktSettings.AccountStatus != ConnectionState.Connected) return;

            TraktLogger.Debug("My Films import complete, initiating online sync");

            // sync again
            Thread syncThread = new Thread(delegate()
            {
                while (SyncInProgress)
                {
                    // only do one sync at a time
                    TraktLogger.Debug("My Films sync still in progress, waiting to complete...");
                    Thread.Sleep(60000);
                }
                SyncLibrary();
            })
            {
                IsBackground = true,
                Name = "My Films Sync"
            };

            syncThread.Start();
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
