using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using MediaPortal.Configuration;
using MediaPortal.GUI.Library;
using MediaPortal.Player;
using OnlineVideos;
using OnlineVideos.MediaPortal1;
using TraktPlugin.Extensions;
using TraktPlugin.GUI;
using TraktAPI.DataStructures;
using TraktAPI.Extensions;

namespace TraktPlugin.TraktHandlers
{
    class OnlineVideos : ITraktHandler
    {
        GUIOnlineVideos ovObject = null;
        ITrackingInfo CurrentVideo = null;

        #region Constructor

        public OnlineVideos(int priority)
        {
            TraktLogger.Info("Initialising OnlineVideos plugin handler");

            // check if plugin exists otherwise plugin could accidently get added to list
            string pluginFilename = Path.Combine(Config.GetSubFolder(Config.Dir.Plugins, "Windows"), "OnlineVideos.MediaPortal1.dll");
            if (!File.Exists(pluginFilename))
            {
                throw new FileNotFoundException("Plugin not found!");
            }
            else
            {
                var fvi = FileVersionInfo.GetVersionInfo(pluginFilename);
                if (new Version(fvi.FileVersion) < new Version(1, 9, 0, 3341))
                {
                    throw new FileLoadException("Plugin does not meet the minimum requirements, check you have the latest version installed!");
                }
            }

            TraktLogger.Debug("Adding Hooks to OnlineVideos");
            
            // Subscribe to Player Events            
            ovObject = (GUIOnlineVideos)GUIWindowManager.GetWindow((int)ExternalPluginWindows.OnlineVideos);
            ovObject.TrackVideoPlayback += new GUIOnlineVideos.TrackVideoPlaybackHandler(TrackVideoPlayback);
            
            Priority = priority;
        }

        #endregion

        #region ITraktHandler Members

        public string Name { get { return "OnlineVideos"; } }
        public int Priority { get; set; }

        public void SyncLibrary()
        {
            // OnlineVideos does not have a library feature
            return;
        }

        public bool Scrobble(string filename)
        {
            if (CurrentVideo == null || !filename.StartsWith("http://"))
                return false;

            if (CurrentVideo.VideoKind == VideoKind.TvSeries)
            {
                TraktLogger.Info("Detected tv series playing in OnlineVideos. Title = '{0} - {1}x{2}', Year = '{3}', IMDb ID = '{4}', TMDb ID = '{5}', TVDb ID = '{6}'", CurrentVideo.Title, CurrentVideo.Season, CurrentVideo.Episode, CurrentVideo.Year == 0 ? "<empty>" : CurrentVideo.Year.ToString(), CurrentVideo.ID_IMDB.ToLogString(), CurrentVideo.ID_TMDB.ToLogString(), CurrentVideo.ID_TVDB.ToLogString());
            }
            else
            {
                TraktLogger.Info("Detected movie playing in OnlineVideos. Title = '{0}', Year = '{1}', IMDb ID = '{2}', TMDb ID = '{3}'", CurrentVideo.Title, CurrentVideo.Year, CurrentVideo.ID_IMDB.ToLogString(), CurrentVideo.ID_TMDB.ToLogString());
            }

            // scrobble from plugin event handler
            return true;
        }

        public void StopScrobble()
        {
            if (CurrentVideo != null)
            {
                // onlinevideos can split videos into multiple parts
                // we only need to handle a pause event if the progress is less than 80%

                double position = g_Player.CurrentPosition;
                double duration = g_Player.Duration;

                if (duration != 0)
                {
                    double progress = position / duration;
                    if (progress < 0.8)
                    {
                        // send pause event to trakt.tv
                        TraktLogger.Info("Playback stopped in OnlineVideos but video is not considered watched, Progress = '{0}%', Duration = '{1}', Current Position = '{2}'", Math.Round(progress * 100, 2), g_Player.Duration, g_Player.CurrentPosition);

                        if (CurrentVideo.VideoKind == VideoKind.TvSeries)
                        {
                            var scrobbleEpisodeData = CreateEpisodeScrobbleData(CurrentVideo, Math.Round(progress * 100, 2));

                            var scrobbleThread = new Thread((objInfo) =>
                            {
                                var response = TraktAPI.TraktAPI.PauseEpisodeScrobble(objInfo as TraktScrobbleEpisode);
                                TraktLogger.LogTraktResponse(response);

                                if (response != null && response.Code == 0)
                                {
                                    //  add episode paused to cache
                                    if (response.Action == "pause")
                                    {
                                        TraktCache.AddEpisodeToPausedData(response.Show, response.Episode, response.Progress);
                                    }
                                }
                            })
                            {
                                IsBackground = true,
                                Name = "Scrobble"
                            };

                            scrobbleThread.Start(scrobbleEpisodeData);
                        }
                        else
                        {
                            var scrobbleMovieData = CreateMovieScrobbleData(CurrentVideo, Math.Round(progress * 100, 2));

                            var scrobbleThread = new Thread((objInfo) =>
                            {
                                var response = TraktAPI.TraktAPI.PauseMovieScrobble(objInfo as TraktScrobbleMovie);
                                TraktLogger.LogTraktResponse(response);

                                if (response != null && response.Code == 0)
                                {
                                    //  add movie paused to cache
                                    if (response.Action == "pause")
                                    {
                                        TraktCache.AddMovieToPausedData(response.Movie, response.Progress);
                                    }
                                }
                            })
                            {
                                IsBackground = true,
                                Name = "Scrobble"
                            };

                            scrobbleThread.Start(scrobbleMovieData);
                        }
                    }
                    else
                    {
                        // either completely watched or finished watching part of a mult-part file
                        TraktLogger.Info("Playback stopped in OnlineVideos, awaiting next playback event.");
                    }
                }

                CurrentVideo = null;
            }

            return;
        }

        public void SyncProgress()
        {
            return;
        }

        #endregion

        #region Player Events

        /// <summary>
        /// Event gets triggered on playback events in OnlineVideos
        /// The TrackVideoPlayback event gets fired on Playback Start, Playback Ended (100%)
        /// and Playback Stopped (if percentage watched is greater than 0.8).      
        /// </summary>
        private void TrackVideoPlayback(ITrackingInfo info, double percentPlayed)
        {
            CurrentVideo = null;

            TraktLogger.Debug("Received Video Playback event from OnlineVideos");

            if (info.VideoKind == VideoKind.Movie || info.VideoKind == VideoKind.TvSeries)
            {
                // Started Playback
                if (percentPlayed < 0.8)
                {
                    CurrentVideo = info;

                    // start scrobble
                    if (info.VideoKind == VideoKind.TvSeries)
                    {
                        var scrobbleEpisodeData = CreateEpisodeScrobbleData(info, Math.Round(percentPlayed * 100, 2));

                        var scrobbleThread = new Thread((objInfo) =>
                        {
                            var response = TraktAPI.TraktAPI.StartEpisodeScrobble(objInfo as TraktScrobbleEpisode);
                            TraktLogger.LogTraktResponse(response);
                        })
                        {
                            IsBackground = true,
                            Name = "Scrobble"
                        };

                        scrobbleThread.Start(scrobbleEpisodeData);
                    }
                    else
                    {
                        var scrobbleMovieData = CreateMovieScrobbleData(info, Math.Round(percentPlayed * 100, 2));

                        var scrobbleThread = new Thread((objInfo) =>
                        {
                            var response = TraktAPI.TraktAPI.StartMovieScrobble(objInfo as TraktScrobbleMovie);
                            TraktLogger.LogTraktResponse(response);
                        })
                        {
                            IsBackground = true,
                            Name = "Scrobble"
                        };

                        scrobbleThread.Start(scrobbleMovieData);
                    }
                    
                    return;
                }

                // Playback Ended or Stopped and Considered Watched
                // TrackVideoPlayback event only gets fired on Stopped if > 80% watched
                if (info.VideoKind == VideoKind.TvSeries)
                {
                    TraktLogger.Info("Playback of episode has ended and is considered watched. Progress = '{0}%', Title = '{1} - {2}x{3}', Year = '{4}', IMDb ID = '{5}', TMDb ID = '{6}', TVDb ID = '{7}'", Math.Round(percentPlayed * 100, 2), info.Title, info.Season, info.Episode, info.Year == 0 ? "<empty>" : info.Year.ToString(), info.ID_IMDB.ToLogString(), info.ID_TMDB.ToLogString(), info.ID_TVDB.ToLogString());
                }
                else
                {
                    TraktLogger.Info("Playback of movie has ended and is considered watched. Progress = '{0}%', Title = '{1}', Year = '{2}', IMDb ID = '{3}', TMDb ID = '{4}'", Math.Round(percentPlayed * 100, 2), info.Title, info.Year, info.ID_IMDB.ToLogString(), info.ID_TMDB.ToLogString());
                }

                // Show Rating Dialog after watched
                ShowRateDialog(info);

                // stop scrobble
                if (info.VideoKind == VideoKind.TvSeries)
                {
                    var scrobbleEpisodeData = CreateEpisodeScrobbleData(info, Math.Round(percentPlayed * 100, 2));

                    var scrobbleThread = new Thread((objInfo) =>
                    {
                        var threadParam = objInfo as TraktScrobbleEpisode;

                        var response = TraktAPI.TraktAPI.StopEpisodeScrobble(threadParam);
                        TraktLogger.LogTraktResponse(response);

                        if (response != null && response.Code == 0)
                        {
                            //  add episode to cache
                            if (response.Action == "scrobble")
                            {
                                TraktCache.AddEpisodeToWatchHistory(response.Show, response.Episode);
                            }
                            else if (response.Action == "pause")
                            {
                                TraktCache.AddEpisodeToPausedData(response.Show, response.Episode, response.Progress);
                            }
                        }
                    })
                    {
                        IsBackground = true,
                        Name = "Scrobble"
                    };

                    scrobbleThread.Start(scrobbleEpisodeData);
                }
                else
                {
                    var scrobbleMovieData = CreateMovieScrobbleData(info, Math.Round(percentPlayed * 100, 2));

                    var scrobbleThread = new Thread((objInfo) =>
                    {
                        var threadParam = objInfo as TraktScrobbleMovie;

                        var response = TraktAPI.TraktAPI.StopMovieScrobble(threadParam);
                        TraktLogger.LogTraktResponse(response);

                        if (response != null && response.Code == 0)
                        {
                            // add movie to cache
                            if (response.Action == "scrobble")
                            {
                                TraktCache.AddMovieToWatchHistory(response.Movie);
                            }
                            else if (response.Action == "pause")
                            {
                                TraktCache.AddMovieToPausedData(response.Movie, response.Progress);
                            }
                        }
                    })
                    {
                        IsBackground = true,
                        Name = "Scrobble"
                    };

                    scrobbleThread.Start(scrobbleMovieData);
                }
            }
        }
        
        #endregion

        #region Data Creators

        private TraktScrobbleEpisode CreateEpisodeScrobbleData(ITrackingInfo info, double progress = 0)
        {
            var scrobbleData = new TraktScrobbleEpisode
            {
                Episode = new TraktEpisode
                {
                    Ids = new TraktEpisodeId(),
                    Number = (int)info.Episode,
                    Season = (int)info.Season
                },
                Show = new TraktShow
                {
                    Ids = new TraktShowId { Imdb = info.ID_IMDB, Tmdb = info.ID_TMDB.ToNullableInt32(), Tvdb = info.ID_TVDB.ToNullableInt32() },
                    Title = info.Title,
                    Year = info.Year > 0 ? (int?)info.Year : null
                },
                AppDate = TraktSettings.BuildDate,
                AppVersion = TraktSettings.Version,
                Progress = progress
            };

            return scrobbleData;
        }

        private TraktScrobbleMovie CreateMovieScrobbleData(ITrackingInfo info, double progress = 0)
        {
            var scrobbleData = new TraktScrobbleMovie
            {
                Movie = new TraktMovie
                {
                    Ids = new TraktMovieId { Imdb = info.ID_IMDB, Tmdb = info.ID_TMDB.ToNullableInt32() },
                    Title = info.Title,
                    Year = (int)info.Year
                },
                AppDate = TraktSettings.BuildDate,
                AppVersion = TraktSettings.Version,
                Progress = progress
            };
            return scrobbleData;
        }

        #endregion
        
        #region Public Methods

        public void DisposeEvents()
        {
            TraktLogger.Debug("Removing Hooks from OnlineVideos");
            ovObject.TrackVideoPlayback -= new GUIOnlineVideos.TrackVideoPlaybackHandler(TrackVideoPlayback);
            ovObject = null;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Shows the Rate Dialog after playback has ended
        /// </summary>
        /// <param name="episode">The item being rated</param>
        private void ShowRateDialog(ITrackingInfo videoInfo)
        {            
            if (!TraktSettings.ShowRateDialogOnWatched)
                return;

            var rateThread = new Thread((objInfo) =>
            {
                var itemToRate = objInfo as ITrackingInfo;
                if (itemToRate == null) return;

                int rating = -1;

                if (itemToRate.VideoKind == VideoKind.TvSeries)
                {
                    TraktLogger.Info("Showing rate dialog for episode. Title = '{0}', Year = '{1}', IMDb ID = '{2}', TMDb ID = '{3}', Season = '{4}', Episode = '{5}'", itemToRate.Title, itemToRate.Year == 0 ? "<empty>" : itemToRate.Year.ToString(), itemToRate.ID_IMDB.ToLogString(), itemToRate.ID_TMDB.ToLogString(), itemToRate.Episode, itemToRate.Season);

                    // this gets complicated when the episode IDs are not available!
                    var rateObject = new TraktSyncShowRatedEx
                    {
                        Ids = new TraktShowId { Tvdb = itemToRate.ID_TVDB.ToNullableInt32(), Tmdb = itemToRate.ID_TMDB.ToNullableInt32() },
                        Title = itemToRate.Title,
                        Year = itemToRate.Year > 0 ? (int?)itemToRate.Year : null,
                        Seasons = new List<TraktSyncShowRatedEx.Season>
                        {   
                            new TraktSyncShowRatedEx.Season 
                            {
                                Number = (int)itemToRate.Season,
                                Episodes = new List<TraktSyncShowRatedEx.Season.Episode>
                                {
                                    new TraktSyncShowRatedEx.Season.Episode
                                    {
                                        Number = (int)itemToRate.Episode,
                                        RatedAt = DateTime.UtcNow.ToISO8601()   
                                    }
                                }
                            }
                        }
                    };
                    // get the rating submitted to trakt
                    rating = GUIUtils.ShowRateDialog<TraktSyncShowRatedEx>(rateObject);

                    // add episode rated to cache
                    if (rating > 0)
                    {
                        TraktCache.AddEpisodeToRatings(
                            new TraktShow
                            {
                                Ids = rateObject.Ids,
                                Title = rateObject.Title,
                                Year = rateObject.Year,
                            },
                            new TraktEpisode
                            {
                                Ids = new TraktEpisodeId(),
                                Season = rateObject.Seasons[0].Number,
                                Number = rateObject.Seasons[0].Episodes[0].Number
                            },
                            rating
                        );
                    }
                }
                else if (itemToRate.VideoKind == VideoKind.Movie)
                {
                    TraktLogger.Info("Showing rate dialog for movie. Title = '{0}', Year = '{1}', IMDb Id = '{2}', TMDb ID = '{3}'", itemToRate.Title, itemToRate.Year, itemToRate.ID_IMDB.ToLogString(), itemToRate.ID_TMDB.ToLogString());

                    var rateObject = new TraktSyncMovieRated
                    {
                        Ids = new TraktMovieId { Imdb = itemToRate.ID_IMDB, Tmdb = itemToRate.ID_TMDB.ToNullableInt32() },
                        Title = itemToRate.Title,
                        Year = (int)itemToRate.Year,
                        RatedAt = DateTime.UtcNow.ToISO8601()
                    };
                    // get the rating submitted to trakt
                    rating = GUIUtils.ShowRateDialog<TraktSyncMovieRated>(rateObject);

                    // add movie rated to cache
                    if (rating > 0)
                    {
                        TraktCache.AddMovieToRatings(rateObject, rating);
                    }
                    else
                    {
                        TraktCache.RemoveMovieFromRatings(rateObject);
                    }
                }
            })
            {
                Name = "Rate",
                IsBackground = true
            };
            
            rateThread.Start(videoInfo);
        }

        #endregion
    }
}
