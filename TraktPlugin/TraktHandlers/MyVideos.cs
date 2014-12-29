using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using TraktPlugin.GUI;
using TraktPlugin.TraktAPI;
using TraktPlugin.TraktAPI.DataStructures;
using TraktPlugin.TraktAPI.Enums;
using TraktPlugin.TraktAPI.Extensions;
using MediaPortal.Player;
using MediaPortal.Playlists;
using System.Reflection;
using System.ComponentModel;
using MediaPortal.Configuration;
using MediaPortal.Video.Database;
using System.Threading;
using System.IO;

namespace TraktPlugin.TraktHandlers
{
    class MyVideos : ITraktHandler
    {
        #region Variables
     
        IMDBMovie CurrentMovie = null;
        int WatchedPercent = 95;

        #endregion

        #region Constructor

        public MyVideos(int priority)
        {
            // check that we are running MediaPortal 1.7 Pre-Release or greater
            string libFilename = Path.Combine(Config.GetSubFolder(Config.Dir.Plugins, "Windows"), "GUIVideos.dll");

            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(libFilename);
            string version = fvi.ProductVersion;
            if (new Version(version) < new Version(1, 6, 100, 0))
            {
                throw new FileLoadException("MediaPortal does not meet minimum requirements!");
            }

            // get video watched percentage to count as scrobble
            using (MediaPortal.Profile.Settings xmlreader = new MediaPortal.Profile.MPSettings())
            {
                WatchedPercent = xmlreader.GetValueAsInt("movies", "playedpercentagewatched", 95);
            }

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

            #region Get online data from trakt.tv

            #region Get unwatched movies from trakt.tv
            TraktLogger.Info("Getting user {0}'s unwatched movies from trakt", TraktSettings.Username);
            var traktUnWatchedMovies = TraktCache.GetUnWatchedMoviesFromTrakt();
            TraktLogger.Info("There are {0} unwatched movies since the last sync with trakt.tv", (traktUnWatchedMovies ?? new List<TraktMovie>()).Count());
            #endregion

            #region Get watched movies from trakt.tv
            TraktLogger.Info("Getting user {0}'s watched movies from trakt", TraktSettings.Username);
            var traktWatchedMovies = TraktCache.GetWatchedMoviesFromTrakt();
            if (traktWatchedMovies == null)
            {
                TraktLogger.Error("Error getting watched movies from trakt server");
            }
            else
            {
                TraktLogger.Info("There are {0} watched movies in trakt.tv library", traktWatchedMovies.Count());
            }
            #endregion

            #region Get collected movies from trakt.tv
            TraktLogger.Info("Getting user {0}'s collected movies from trakt", TraktSettings.Username);
            var traktCollectedMovies = TraktCache.GetCollectedMoviesFromTrakt();
            if (traktCollectedMovies == null)
            {
                TraktLogger.Error("Error getting collected movies from trakt server");
            }
            else
            {
                TraktLogger.Info("There are {0} collected movies in trakt.tv library", traktCollectedMovies.Count());
            }
            #endregion

            #region Get rated movies from trakt.tv
            // not used in MyVideos sync but could be used externally
            TraktLogger.Info("Getting user {0}'s rated movies from trakt", TraktSettings.Username);
            var traktRatedMovies = TraktCache.GetRatedMoviesFromTrakt();
            if (traktRatedMovies == null)
            {
                TraktLogger.Error("Error getting rated movies from trakt server");
            }
            else
            {
                TraktLogger.Info("There are {0} rated movies in trakt.tv library", traktRatedMovies.Count());
            }
            #endregion

            #region Get watchlisted movies from trakt.tv
            // not used in MyVideos sync but could be used externally
            TraktLogger.Info("Getting user {0}'s watchlisted movies from trakt", TraktSettings.Username);
            var traktWatchListMovies = TraktCache.GetWatchlistedMoviesFromTrakt();
            if (traktWatchListMovies == null)
            {
                TraktLogger.Error("Error getting watchlisted movies from trakt server");
            }
            else
            {
                TraktLogger.Info("There are {0} watchlisted movies in trakt.tv library", traktWatchListMovies.Count());
            }
            #endregion

            #endregion

            // optionally do library sync
            if (TraktSettings.SyncLibrary)
            {
                // get all movies
                ArrayList myvideos = new ArrayList();
                VideoDatabase.GetMovies(ref myvideos);

                var collectedMovies = (from IMDBMovie movie in myvideos select movie).ToList();

                #region Remove Blocked Movies
                collectedMovies.RemoveAll(m => TraktSettings.BlockedFolders.Any(f => m.Path.ToLowerInvariant().Contains(f.ToLowerInvariant())));

                List<int> blockedMovieIds = new List<int>();
                foreach (string file in TraktSettings.BlockedFilenames)
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
                collectedMovies.RemoveAll(m => blockedMovieIds.Contains(m.ID));
                #endregion

                #region Skipped Movies Check
                // Remove Skipped Movies from previous Sync
                //TODO
                //if (TraktSettings.SkippedMovies != null)
                //{
                //    // allow movies to re-sync again after 7-days in the case user has addressed issue ie. edited movie or added to themoviedb.org
                //    if (TraktSettings.SkippedMovies.LastSkippedSync.FromEpoch() > DateTime.UtcNow.Subtract(new TimeSpan(7, 0, 0, 0)))
                //    {
                //        if (TraktSettings.SkippedMovies.Movies != null && TraktSettings.SkippedMovies.Movies.Count > 0)
                //        {
                //            TraktLogger.Info("Skipping {0} movies due to invalid data or movies don't exist on http://themoviedb.org. Next check will be {1}.", TraktSettings.SkippedMovies.Movies.Count, TraktSettings.SkippedMovies.LastSkippedSync.FromEpoch().Add(new TimeSpan(7, 0, 0, 0)));
                //            foreach (var movie in TraktSettings.SkippedMovies.Movies)
                //            {
                //                TraktLogger.Info("Skipping movie, Title: {0}, Year: {1}, IMDb: {2}", movie.Title, movie.Year, movie.IMDBID);
                //                MovieList.RemoveAll(m => (m.Title == movie.Title) && (m.Year.ToString() == movie.Year) && (m.IMDBNumber == movie.IMDBID));
                //            }
                //        }
                //    }
                //    else
                //    {
                //        if (TraktSettings.SkippedMovies.Movies != null) TraktSettings.SkippedMovies.Movies.Clear();
                //        TraktSettings.SkippedMovies.LastSkippedSync = DateTime.UtcNow.ToEpoch();
                //    }
                //}
                #endregion

                #region Already Exists Movie Check
                // Remove Already-Exists Movies, these are typically movies that are using aka names and no IMDb/TMDb set
                // When we compare our local collection with trakt collection we have english only titles, so if no imdb/tmdb exists
                // we need to fallback to title matching. When we sync aka names are sometimes accepted if defined on themoviedb.org so we need to 
                // do this to revent syncing these movies every sync interval.
                //TODO
                //if (TraktSettings.AlreadyExistMovies != null && TraktSettings.AlreadyExistMovies.Movies != null && TraktSettings.AlreadyExistMovies.Movies.Count > 0)
                //{
                //    TraktLogger.Debug("Skipping {0} movies as they already exist in trakt library but failed local match previously.", TraktSettings.AlreadyExistMovies.Movies.Count.ToString());
                //    var movies = new List<TraktMovieSync.Movie>(TraktSettings.AlreadyExistMovies.Movies);
                //    foreach (var movie in movies)
                //    {
                //        Predicate<IMDBMovie> criteria = m => (m.Title == movie.Title) && (m.Year.ToString() == movie.Year) && (m.IMDBNumber == movie.IMDBID);
                //        if (MovieList.Exists(criteria))
                //        {
                //            TraktLogger.Debug("Skipping movie, Title: {0}, Year: {1}, IMDb: {2}", movie.Title, movie.Year, movie.IMDBID);
                //            MovieList.RemoveAll(criteria);
                //        }
                //        else
                //        {
                //            // remove as we have now removed from our local collection or updated movie signature
                //            if (TraktSettings.MoviePluginCount == 1)
                //            {
                //                TraktLogger.Debug("Removing 'AlreadyExists' movie, Title: {0}, Year: {1}, IMDb: {2}", movie.Title, movie.Year, movie.IMDBID);
                //                TraktSettings.AlreadyExistMovies.Movies.Remove(movie);
                //            }
                //        }
                //    }
                //}
                #endregion

                TraktLogger.Info("Found {0} movies available to sync in My Videos database", collectedMovies.Count);

                // get the movies that we have watched
                var watchedMovies = collectedMovies.Where(m => m.Watched > 0).ToList();

                TraktLogger.Info("Found {0} watched movies available to sync in My Videos database", watchedMovies.Count);

                // clear the last time(s) we did anything online
                TraktCache.ClearLastActivityCache();

                #region Mark movies as unwatched in local database
                if (traktUnWatchedMovies != null && traktUnWatchedMovies.Count() > 0)
                {
                    foreach (var movie in traktUnWatchedMovies)
                    {
                        var localMovie = watchedMovies.FirstOrDefault(m => MovieMatch(m, movie));
                        if (localMovie == null) continue;

                        TraktLogger.Info("Marking movie as unwatched in local database, movie is not watched on trakt.tv. Title = '{0}', Year = '{1}', IMDb ID = '{2}', TMDb ID = '{3}'",
                                          movie.Title, movie.Year.HasValue ? movie.Year.ToString() : "<empty>", movie.Ids.Imdb ?? "<empty>", movie.Ids.Tmdb.HasValue ? movie.Ids.Tmdb.ToString() : "<empty>");

                        localMovie.Watched = 0;
                        IMDBMovie details = localMovie;
                        VideoDatabase.SetMovieInfoById(localMovie.ID, ref details);
                        VideoDatabase.SetMovieWatchedStatus(localMovie.ID, false, 0);
                    }

                    // update watched set
                    watchedMovies = collectedMovies.Where(m => m.Watched > 0).ToList();
                }
                #endregion
                
                #region Mark movies as watched in local database
                if (traktWatchedMovies != null && traktWatchedMovies.Count() > 0)
                {
                    foreach (var twm in traktWatchedMovies)
                    {
                        var localMovie = collectedMovies.FirstOrDefault(m => MovieMatch(m, twm.Movie));
                        if (localMovie == null) continue;

                        int iPercent;
                        int iWatchedCount;
                        bool localIsWatched = VideoDatabase.GetmovieWatchedStatus(localMovie.ID, out iPercent, out iWatchedCount);

                        if (!localIsWatched || iWatchedCount < twm.Plays)
                        {
                            TraktLogger.Info("Updating local movie watched state / play count to match trakt.tv. Plays = '{0}', Title = '{1}', Year = '{2}', IMDb ID = '{3}', TMDb ID = '{4}'",
                                              twm.Plays, twm.Movie.Title, twm.Movie.Year.HasValue ? twm.Movie.Year.ToString() : "<empty>", twm.Movie.Ids.Imdb ?? "<empty>", twm.Movie.Ids.Tmdb.HasValue ? twm.Movie.Ids.Tmdb.ToString() : "<empty>");

                            if (localMovie.DateWatched == "0001-01-01 00:00:00")
                            {
                                DateTime dateWatched;
                                if (DateTime.TryParse(twm.LastWatchedAt, out dateWatched))
                                {
                                    localMovie.DateWatched = dateWatched.ToString("yyyy-MM-dd HH:mm:ss");
                                }
                            }

                            localMovie.Watched = 1;
                            localMovie.WatchedCount = twm.Plays;
                            localMovie.WatchedPercent = iPercent;

                            VideoDatabase.SetMovieWatchedCount(localMovie.ID, twm.Plays);
                            VideoDatabase.SetMovieWatchedStatus(localMovie.ID, true, iPercent);
                            
                            IMDBMovie details = localMovie;
                            VideoDatabase.SetMovieInfoById(localMovie.ID, ref details);
                        }
                    }
                }
                #endregion

                #region Add movies to watched history at trakt.tv
                if (traktWatchedMovies != null)
                {
                    var syncWatchedMovies = new List<TraktSyncMovieWatched>();
                    TraktLogger.Info("Finding movies to add to trakt.tv watched history");

                    syncWatchedMovies = (from movie in watchedMovies
                                         where !traktWatchedMovies.ToList().Exists(c => MovieMatch(movie, c.Movie))
                                         select new TraktSyncMovieWatched
                                         {
                                             Ids = new TraktMovieId { Imdb = movie.IMDBNumber },
                                             Title = movie.Title,
                                             Year = movie.Year,
                                             WatchedAt = GetLastDateWatched(movie),
                                         }).ToList();

                    TraktLogger.Info("Adding {0} movies to trakt.tv watched history", syncWatchedMovies.Count);

                    if (syncWatchedMovies.Count > 0)
                    {
                        // update local cache
                        TraktCache.AddMoviesToWatchHistory(syncWatchedMovies);

                        int pageSize = TraktSettings.SyncBatchSize;
                        int pages = (int)Math.Ceiling((double)syncWatchedMovies.Count / pageSize);
                        for (int i = 0; i < pages; i++)
                        {
                            TraktLogger.Info("Adding movies [{0}/{1}] to trakt.tv watched history", i + 1, pages);

                            var pagedMovies = syncWatchedMovies.Skip(i * pageSize).Take(pageSize).ToList();

                            pagedMovies.ForEach(s => TraktLogger.Info("Adding movie to trakt.tv watched history. Title = '{0}', Year = '{1}', IMDb ID = '{2}', TMDb ID = '{3}', Date Watched = '{4}'",
                                                                             s.Title, s.Year.HasValue ? s.Year.ToString() : "<empty>", s.Ids.Imdb ?? "<empty>", s.Ids.Tmdb.HasValue ? s.Ids.Tmdb.ToString() : "<empty>", s.WatchedAt));

                            var response = TraktAPI.TraktAPI.AddMoviesToWatchedHistory(new TraktSyncMoviesWatched { Movies = pagedMovies });
                            TraktLogger.LogTraktResponse<TraktSyncResponse>(response);
                        }
                    }
                }
                #endregion

                #region Add movies to collection at trakt.tv
                if (traktCollectedMovies != null)
                {
                    var syncCollectedMovies = new List<TraktSyncMovieCollected>();
                    TraktLogger.Info("Finding movies to add to trakt.tv collection");

                    syncCollectedMovies = (from movie in collectedMovies
                                           where !traktCollectedMovies.ToList().Exists(c => MovieMatch(movie, c.Movie))
                                           select new TraktSyncMovieCollected
                                           {
                                               Ids = new TraktMovieId { Imdb = movie.IMDBNumber },
                                               Title = movie.Title,
                                               Year = movie.Year,
                                               CollectedAt = movie.DateAdded.ToISO8601(),
                                               MediaType = GetMovieMediaType(movie),
                                               Resolution = GetMovieResolution(movie),
                                               AudioCodec = GetMovieAudioCodec(movie),
                                               AudioChannels = GetMovieAudioChannels(movie),
                                               Is3D = IsMovie3D(movie)
                                           }).ToList();

                    TraktLogger.Info("Adding {0} movies to trakt.tv collection", syncCollectedMovies.Count);

                    if (syncCollectedMovies.Count > 0)
                    {
                        // update internal cache
                        TraktCache.AddMoviesToCollection(syncCollectedMovies);

                        int pageSize = TraktSettings.SyncBatchSize;
                        int pages = (int)Math.Ceiling((double)syncCollectedMovies.Count / pageSize);
                        for (int i = 0; i < pages; i++)
                        {
                            TraktLogger.Info("Adding movies [{0}/{1}] to trakt.tv collection", i + 1, pages);

                            var pagedMovies = syncCollectedMovies.Skip(i * pageSize).Take(pageSize).ToList();

                            pagedMovies.ForEach(s => TraktLogger.Info("Adding movie to trakt.tv collection. Title = '{0}', Year = '{1}', IMDb ID = '{2}', TMDb ID = '{3}', Date Added = '{4}', MediaType = '{5}', Resolution = '{6}', Audio Codec = '{7}', Audio Channels = '{8}'",
                                                                        s.Title, s.Year.HasValue ? s.Year.ToString() : "<empty>", s.Ids.Imdb ?? "<empty>", s.Ids.Tmdb.HasValue ? s.Ids.Tmdb.ToString() : "<empty>",
                                                                        s.CollectedAt, s.MediaType ?? "<empty>", s.Resolution ?? "<empty>", s.AudioCodec ?? "<empty>", s.AudioChannels ?? "<empty>"));

                            var response = TraktAPI.TraktAPI.AddMoviesToCollecton(new TraktSyncMoviesCollected { Movies = pagedMovies });
                            TraktLogger.LogTraktResponse(response);
                        }
                    }
                }
                #endregion

                #region Remove movies no longer in collection from trakt.tv
                if (TraktSettings.KeepTraktLibraryClean && TraktSettings.MoviePluginCount == 1 && traktCollectedMovies != null)
                {
                    var syncUnCollectedMovies = new List<TraktMovie>();
                    TraktLogger.Info("Finding movies to remove from trakt.tv collection");

                    // workout what movies that are in trakt collection that are not in local collection
                    syncUnCollectedMovies = (from tcm in traktCollectedMovies
                                             where !collectedMovies.Exists(c => MovieMatch(c, tcm.Movie))
                                             select new TraktMovie
                                             {
                                                 Ids = tcm.Movie.Ids,
                                                 Title = tcm.Movie.Title,
                                                 Year = tcm.Movie.Year
                                             }).ToList();

                    TraktLogger.Info("Removing {0} movies from trakt.tv collection", syncUnCollectedMovies.Count);

                    if (syncUnCollectedMovies.Count > 0)
                    {
                        // update local cache
                        TraktCache.RemoveMoviesFromCollection(syncUnCollectedMovies);

                        int pageSize = TraktSettings.SyncBatchSize;
                        int pages = (int)Math.Ceiling((double)syncUnCollectedMovies.Count / pageSize);
                        for (int i = 0; i < pages; i++)
                        {
                            TraktLogger.Info("Removing movies [{0}/{1}] from trakt.tv collection", i + 1, pages);

                            var pagedMovies = syncUnCollectedMovies.Skip(i * pageSize).Take(pageSize).ToList();

                            pagedMovies.ForEach(s => TraktLogger.Info("Removing movie from trakt.tv collection, movie no longer exists locally. Title = '{0}', Year = '{1}', IMDb ID = '{2}', TMDb ID = '{3}'",
                                                                        s.Title, s.Year.HasValue ? s.Year.ToString() : "<empty>", s.Ids.Imdb ?? "<empty>", s.Ids.Tmdb.HasValue ? s.Ids.Tmdb.ToString() : "<empty>"));

                            var response = TraktAPI.TraktAPI.RemoveMoviesFromCollecton(new TraktSyncMovies { Movies = pagedMovies });
                            TraktLogger.LogTraktResponse(response);
                        }
                    }
                }
                #endregion

                myvideos.Clear();
            }

            TraktLogger.Info("My Videos Sync Completed");
        }

        public bool Scrobble(string filename)
        {
            StopScrobble();

            // stop check if not valid player type for plugin handler
            if (g_Player.IsTV || g_Player.IsTVRecording) return false;

            // lookup movie by filename
            IMDBMovie movie = new IMDBMovie();
            int result = VideoDatabase.GetMovieInfo(filename, ref movie);
            if (result == -1) return false;

            CurrentMovie = movie;

            var scrobbleData = CreateScrobbleData(CurrentMovie);
            var scrobbleMovie = new Thread((objScrobble) =>
            {
                var tScrobbleData = objScrobble as TraktScrobbleMovie;
                if (tScrobbleData == null) return;

                TraktLogger.Info("Sending start scrobble of movie to trakt.tv. Title = '{0}', Year = '{1}', IMDb ID = '{2}'", tScrobbleData.Movie.Title, tScrobbleData.Movie.Year, tScrobbleData.Movie.Ids.Imdb ?? "<empty>");
                var response = TraktAPI.TraktAPI.StartMovieScrobble(tScrobbleData);
                TraktLogger.LogTraktResponse(response);
            })
            {
                IsBackground = true,
                Name = "Scrobble"
            };

            scrobbleMovie.Start(scrobbleData);

            return true;
        }

        public void StopScrobble()
        {
            if (CurrentMovie == null)
                return;

            var scrobbleData = CreateScrobbleData(CurrentMovie);

            // check if movie is considered 'watched'
            if (scrobbleData.Progress >= WatchedPercent)
            {
                ShowRateDialog(CurrentMovie);
            }

            var scrobbleMovie = new Thread((objScrobble) =>
            {
                var tScrobbleData = objScrobble as TraktScrobbleMovie;
                if (tScrobbleData == null) return;

                // update local cache
                if (tScrobbleData.Progress >= WatchedPercent)
                {
                    TraktCache.AddMovieToWatchHistory(tScrobbleData.Movie);
                }

                TraktLogger.Info("Sending stop scrobble of movie to trakt.tv. Title = '{0}', Year = '{1}', IMDb ID = '{2}'", tScrobbleData.Movie.Title, tScrobbleData.Movie.Year, tScrobbleData.Movie.Ids.Imdb ?? "<empty>");
                var response = TraktAPI.TraktAPI.StopMovieScrobble(tScrobbleData);
                TraktLogger.LogTraktResponse(response);
            })
            {
                IsBackground = true,
                Name = "Scrobble"
            };

            scrobbleMovie.Start(scrobbleData);

            CurrentMovie = null;            
        }

        #endregion

        #region Data Creators

        /// <summary>
        /// Checks if the movie is 3D or not
        /// </summary>
        private bool IsMovie3D(IMDBMovie movie)
        {
            if (movie.MediaInfo == null) return false;

            // My Videos does not expose this info via the API
            return false;
        }

        /// <summary>
        /// Gets the trakt compatible string for the movies Media Type
        /// </summary>
        private string GetMovieMediaType(IMDBMovie movie)
        {            
            if (movie.MediaInfo == null) return null;

            // My Videos does not expose this info via the API
            return null;
        }

        /// <summary>
        /// Gets the trakt compatible string for the movies Resolution
        /// </summary>
        private string GetMovieResolution(IMDBMovie movie)
        {
            if (movie.MediaInfo == null) return null;

            // try to match 1:1 with what we know
            switch (movie.MediaInfo.VideoResolution)
            {
                case "1080p":
                    return TraktResolution.hd_1080p.ToString();
                case "1080i":
                    return TraktResolution.hd_1080i.ToString();
                case "720p":
                    return TraktResolution.hd_720p.ToString();
                case "576p":
                    return TraktResolution.sd_576p.ToString();
                case "576i":
                    return TraktResolution.sd_576i.ToString();
                case "480p":
                    return TraktResolution.sd_480p.ToString();
                case "480i":
                    return TraktResolution.sd_480i.ToString();
                case "4K UHD":
                    return TraktResolution.uhd_4k.ToString();
            }

            return null;
        }

        /// <summary>
        /// Gets the trakt compatible string for the movies Audio
        /// </summary>
        private string GetMovieAudioCodec(IMDBMovie movie)
        {
            if (movie.MediaInfo == null) return null;

            switch (movie.MediaInfo.AudioCodec)
            {
                case "TrueHD":
                    return TraktAudio.dolby_truehd.ToString();
                case "DTS":
                    return TraktAudio.dts.ToString();
                case "DTS ES":
                case "DTSHD":
                    return TraktAudio.dts_ma.ToString();
                case "AC-3 DOLBY DIGITAL":
                case "AC-3":
                case "AC3":
                    return TraktAudio.dolby_digital.ToString();
                case "AAC LC":
                case "AAC":
                    return TraktAudio.aac.ToString();
                case "MPEG AUDIO VERSION 1 LAYER 3":
                case "MP3":
                    return TraktAudio.mp3.ToString();
                case "PCM":
                    return TraktAudio.lpcm.ToString();
                case "OGG":
                    return TraktAudio.ogg.ToString();
                case "WMA":
                    return TraktAudio.wma.ToString();
                default:
                    return null;
            }
        }

        /// <summary>
        /// Gets the trakt compatible string for the movies Audio Channels
        /// </summary>
        private string GetMovieAudioChannels(IMDBMovie movie)
        {
            if (movie.MediaInfo == null) return null;

            switch (movie.MediaInfo.AudioChannels)
            {
                case "7.1":
                case "6.1":
                case "5.1":
                    return movie.MediaInfo.AudioChannels;
                case "7":
                    return "6.1";
                case "stereo":
                    return "2.0";
                case "mono":
                    return "1.0";
                default:
                    return null;
            }
        }

        /// <summary>
        /// Get the date watched for the movie
        /// </summary>        
        private static string GetLastDateWatched(IMDBMovie movie)
        {
            string dateLastPlayed = DateTime.UtcNow.ToISO8601();

            if (!string.IsNullOrEmpty(movie.DateWatched) && movie.DateWatched != "0001-01-01 00:00:00" )
            {
                try
                {
                    DateTime dateResult;
                    if (DateTime.TryParse(movie.DateWatched, out dateResult))
                    {
                        dateLastPlayed = dateResult.ToUniversalTime().ToISO8601();
                    }
                }
                catch (Exception e)
                {
                    TraktLogger.Error("Failed to get last watched date from movie. Title = '{0}', Error = '{1}'", movie.Title, e.Message);
                }
            }

            return dateLastPlayed;
        }

        /// <summary>
        /// Creates Scrobble data based on a IMDBMovie object
        /// </summary>
        /// <param name="movie">The movie to base the object on</param>
        /// <returns>The Trakt scrobble data to send</returns>
        private static TraktScrobbleMovie CreateScrobbleData(IMDBMovie movie)
        {
            double progress = (g_Player.CurrentPosition / (g_Player.Duration == 0.0 ? movie.RunTime * 60.0 : g_Player.Duration)) * 100.0;

            var scrobbleData = new TraktScrobbleMovie
            {
                Movie = new TraktMovie
                {
                    Ids = new TraktMovieId { Imdb = movie.IMDBNumber },
                    Title = movie.Title,
                    Year = movie.Year
                },
                AppDate = TraktSettings.BuildDate,
                AppVersion = TraktSettings.Version,
                Progress = Math.Round(progress, 2)
            };

            return scrobbleData;
        }

        #endregion

        #region Public Methods

        internal static bool FindMovieID(string title, int year, string imdbid, ref IMDBMovie imdbMovie)
        {
            // get all movies
            ArrayList myvideos = new ArrayList();
            VideoDatabase.GetMovies(ref myvideos);

            // get all movies in local database
            List<IMDBMovie> movies = (from IMDBMovie m in myvideos select m).ToList();

            // try find a match
            IMDBMovie movie = movies.Find(m => BasicHandler.GetProperImdbId(m.IMDBNumber) == imdbid || (string.Compare(m.Title, title, true) == 0 && m.Year == year));
            if (movie == null) return false;

            imdbMovie = movie;
            return true;
        }

        #endregion

        #region Private Methods

        private bool MovieMatch(IMDBMovie localMovie, TraktMovie traktMovie)
        {
            // IMDb comparison
            if (!string.IsNullOrEmpty(traktMovie.Ids.Imdb) && !string.IsNullOrEmpty(BasicHandler.GetProperImdbId(localMovie.IMDBNumber)))
            {
                return string.Compare(BasicHandler.GetProperImdbId(localMovie.IMDBNumber), traktMovie.Ids.Imdb, true) == 0;
            }

            // Title & Year comparison
            return string.Compare(localMovie.Title, traktMovie.Title, true) == 0 && localMovie.Year.ToString() == traktMovie.Year.ToString();
        }

        /// <summary>
        /// Shows the Rate Movie Dialog after playback has ended
        /// </summary>
        /// <param name="movie">The movie being rated</param>
        private void ShowRateDialog(IMDBMovie movie)
        {
            if (!TraktSettings.ShowRateDialogOnWatched) return;
            if (!TraktSettings.ShowRateDlgForPlaylists && PlayListPlayer.SingletonPlayer.CurrentPlaylistType == PlayListType.PLAYLIST_VIDEO) return;

            TraktLogger.Debug("Showing rate dialog for movie. Title = '{0}', Year = '{1}', IMDb ID = '{2}'", movie.Title, movie.Year, movie.IMDBNumber ?? "<empty>");

            var rateThread = new Thread((o) =>
            {
                var movieToRate = o as IMDBMovie;
                if (movieToRate == null) return;

                var rateObject = new TraktSyncMovieRated
                {
                    Ids = new TraktMovieId { Imdb = movieToRate.IMDBNumber },
                    Title = movieToRate.Title,
                    Year = movieToRate.Year,
                    RatedAt = DateTime.UtcNow.ToISO8601(),
                };

                // get the rating submitted to trakt.tv
                int rating = GUI.GUIUtils.ShowRateDialog<TraktSyncMovieRated>(rateObject);

                if (rating > 0)
                {
                    // update local cache
                    TraktCache.AddMovieToRatings(rateObject, rating);

                    TraktLogger.Debug("Rating {0} ({1}) as {2}/10", movieToRate.Title, movie.Year, rating.ToString());
                }
                else if (rating == 0)
                {
                    TraktCache.RemoveMovieFromRatings(rateObject);
                }
            })
            {
                Name = "Rate",
                IsBackground = true
            };

            rateThread.Start(movie);
        }

        #endregion

    }
}
