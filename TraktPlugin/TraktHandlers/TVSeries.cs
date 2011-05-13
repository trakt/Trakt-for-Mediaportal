using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaPortal.Player;
using System.ComponentModel;
using System.Reflection;
using System.Threading;
using TraktPlugin.TraktAPI;
using TraktPlugin.TraktAPI.DataStructures;
using WindowPlugins.GUITVSeries;

namespace TraktPlugin.TraktHandlers
{
    /// <summary>
    /// Support for TVSeries
    /// </summary>
    class TVSeries : ITraktHandler
    {
        #region Variables

        Timer TraktTimer;
        bool SyncInProgress;        
        bool EpisodeWatching;
        bool MarkedFirstAsWatched;
        DBEpisode CurrentEpisode;

        #endregion

        #region Constructor

        public TVSeries(int priority)
        {
            Priority = priority;
            
            TraktLogger.Debug("Adding Hooks to MP-TVSeries");
            
            // player events
            VideoHandler.EpisodeWatched += new VideoHandler.EpisodeWatchedDelegate(OnEpisodeWatched);
            VideoHandler.EpisodeStarted += new VideoHandler.EpisodeStartedDelegate(OnEpisodeStarted);
            VideoHandler.EpisodeStopped += new VideoHandler.EpisodeStoppedDelegate(OnEpisodeStopped);
            PlayListPlayer.EpisodeWatched +=new PlayListPlayer.EpisodeWatchedDelegate(OnEpisodeWatched);
            PlayListPlayer.EpisodeStarted += new PlayListPlayer.EpisodeStartedDelegate(OnEpisodeStarted);
            PlayListPlayer.EpisodeStopped += new PlayListPlayer.EpisodeStoppedDelegate(OnEpisodeStopped);
            
            // import events
            OnlineParsing.OnlineParsingCompleted += new OnlineParsing.OnlineParsingCompletedHandler(OnImportCompleted);
            
            // gui events
            TVSeriesPlugin.RateItem += new TVSeriesPlugin.RatingEventDelegate(OnRateItem);
            TVSeriesPlugin.ToggleWatched += new TVSeriesPlugin.ToggleWatchedEventDelegate(OnToggleWatched);
        }

        #endregion

        #region ITraktHandler Members

        public string Name
        {
            get { return "MP-TVSeries"; }
        }

        public int Priority { get; set; }
        
        public void SyncLibrary()
        {
            TraktLogger.Info("TVSeries Starting Sync");

            SyncInProgress = true;

            // get all episodes on trakt that are marked as in 'collection'
            IEnumerable<TraktLibraryShow> traktCollectionEpisodes = TraktAPI.TraktAPI.GetLibraryEpisodesForUser(TraktSettings.Username);
            TraktLogger.Info("{0} tvshows in trakt collection", traktCollectionEpisodes.Count().ToString());

            // get all episodes on trakt that are marked as 'seen' or 'watched'
            IEnumerable<TraktLibraryShow> traktWatchedEpisodes = TraktAPI.TraktAPI.GetWatchedEpisodesForUser(TraktSettings.Username);
            TraktLogger.Info("{0} tvshows with watched episodes in trakt library", traktWatchedEpisodes.Count().ToString());

            // get all episodes on trakt that are marked as 'unseen'
            IEnumerable<TraktLibraryShow> traktUnSeenEpisodes = TraktAPI.TraktAPI.GetUnSeenEpisodesForUser(TraktSettings.Username);
            TraktLogger.Info("{0} tvshows with unseen episodes in trakt library", traktUnSeenEpisodes.Count().ToString());

            List<DBEpisode> localAllEpisodes = new List<DBEpisode>();
            List<DBEpisode> localCollectionEpisodes = new List<DBEpisode>();
            List<DBEpisode> localWatchedEpisodes = new List<DBEpisode>();            

            // Get all episodes in database
            SQLCondition conditions = new SQLCondition();
            conditions.Add(new DBOnlineEpisode(), DBOnlineEpisode.cSeriesID, 0, SQLConditionType.GreaterThan);
            conditions.Add(new DBOnlineEpisode(), DBOnlineEpisode.cHidden, 0, SQLConditionType.Equal);            
            localAllEpisodes = DBEpisode.Get(conditions, false);

            TraktLogger.Info("{0} total episodes in tvseries database", localAllEpisodes.Count.ToString());

            // Get episodes of files that we have locally
            localCollectionEpisodes = localAllEpisodes.Where(e => !string.IsNullOrEmpty(e[DBEpisode.cFilename].ToString())).ToList();

            TraktLogger.Info("{0} episodes with local files in tvseries database", localCollectionEpisodes.Count.ToString());

            // Get watched episodes of files that we have locally or are remote
            // user could of deleted episode from disk but still have reference to it in database           
            localWatchedEpisodes = localAllEpisodes.Where(e => e[DBOnlineEpisode.cWatched] > 0).ToList();

            TraktLogger.Info("{0} episodes watched in tvseries database", localWatchedEpisodes.Count.ToString());

            #region sync collection/library to trakt
            // get list of episodes that we have not already trakt'd
            List<DBEpisode> localEpisodesToSync = new List<DBEpisode>(localCollectionEpisodes);
            foreach (DBEpisode ep in localCollectionEpisodes)
            {
                if (TraktEpisodeExists(traktCollectionEpisodes, ep))
                {
                    // no interest in syncing, remove
                    localEpisodesToSync.Remove(ep);
                }
            }
            // sync unseen episodes
            TraktLogger.Info("{0} episodes need to be added to Library", localEpisodesToSync.Count.ToString());
            SyncLibrary(localEpisodesToSync, TraktSyncModes.library);
            #endregion

            #region sync seen to trakt
            // get list of episodes that we have not already trakt'd
            // filter out any marked as UnSeen
            List<DBEpisode> localWatchedEpisodesToSync = new List<DBEpisode>(localWatchedEpisodes);
            foreach (DBEpisode ep in localWatchedEpisodes)
            {
                if (TraktEpisodeExists(traktWatchedEpisodes, ep) || TraktEpisodeExists(traktUnSeenEpisodes, ep))
                {
                    // no interest in syncing, remove
                    localWatchedEpisodesToSync.Remove(ep);
                }
            }
            // sync seen episodes
            TraktLogger.Info("{0} episodes need to be added to SeenList", localWatchedEpisodesToSync.Count.ToString());
            SyncLibrary(localWatchedEpisodesToSync, TraktSyncModes.seen);
            #endregion

            #region sync watched flags from trakt locally
            // Sync watched flags from trakt to local database
            // do not mark as watched locally if UnSeen on trakt
            foreach (DBEpisode ep in localAllEpisodes.Where(e => e[DBOnlineEpisode.cWatched] == 0))
            {
                if (TraktEpisodeExists(traktWatchedEpisodes, ep) && !TraktEpisodeExists(traktUnSeenEpisodes, ep))
                {
                    // mark episode as watched
                    TraktLogger.Info("Marking episode '{0}' as watched", ep.ToString());
                    ep[DBOnlineEpisode.cWatched] = true;
                    ep.Commit();
                }
            }
            #endregion

            #region sync unseen flags from trakt locally
            foreach (DBEpisode ep in localAllEpisodes.Where(e => e[DBOnlineEpisode.cWatched] == 1))
            {
                if (TraktEpisodeExists(traktUnSeenEpisodes, ep))
                {
                    // mark episode as unwatched
                    TraktLogger.Info("Marking episode '{0}' as unwatched", ep.ToString());
                    ep[DBOnlineEpisode.cWatched] = false;
                    ep.Commit();
                }
            }
            #endregion

            if (TraktSettings.KeepTraktLibraryClean)
            {
                TraktLogger.Info("Removing shows From Trakt Collection no longer in database");

                // if we no longer have a file reference in database remove from library
                foreach (var series in traktCollectionEpisodes)
                {
                    TraktEpisodeSync syncData = GetEpisodesForTraktRemoval(series, localCollectionEpisodes.Where(e => e[DBOnlineEpisode.cSeriesID] == series.SeriesId).ToList());
                    if (syncData == null) continue;
                    TraktResponse response = TraktAPI.TraktAPI.SyncEpisodeLibrary(syncData, TraktSyncModes.unlibrary);
                    TraktAPI.TraktAPI.LogTraktResponse(response);
                    Thread.Sleep(500);
                }

            }

            SyncInProgress = false;

            TraktLogger.Info("TVSeries Sync Completed");
        }

        public bool Scrobble(string filename)
        {
            if (!EpisodeWatching) return false;

            StopScrobble();
            TraktLogger.Info(string.Format("Found playing episode {0}", CurrentEpisode.ToString()));

            MarkedFirstAsWatched = false;

            // create timer 15 minute timer to send watching status
            #region scrobble timer
            TraktTimer = new Timer(new TimerCallback((stateInfo) =>
            {
                // duration in minutes
                double duration = CurrentEpisode[DBEpisode.cLocalPlaytime] / 60000;
                double progress = 0.0;

                // get current progress of player (in seconds) to work out percent complete
                if (duration > 0.0)
                    progress = ((g_Player.CurrentPosition / 60.0) / duration) * 100.0;

                TraktEpisodeScrobble scrobbleData = null;

                // check if double episode has passed halfway mark and set as watched
                if (CurrentEpisode[DBEpisode.cEpisodeIndex2] > 0 && progress > 50.0)
                {
                    SQLCondition condition = new SQLCondition();
                    condition.Add(new DBEpisode(), DBEpisode.cFilename, CurrentEpisode[DBEpisode.cFilename], SQLConditionType.Equal);
                    List<DBEpisode> episodes = DBEpisode.Get(condition, false);

                    if (!MarkedFirstAsWatched)
                    {
                        // send scrobble Watched status of first episode
                        OnEpisodeWatched(episodes[0]);
                        Thread.Sleep(5000);                        
                    }

                    EpisodeWatching = true;
                    MarkedFirstAsWatched = true;

                    // we are now watching 2nd part of double episode
                    scrobbleData = CreateScrobbleData(episodes[1]);
                }
                else
                {
                    // we are watching a single episode or 1st part of double episode
                    scrobbleData = CreateScrobbleData(CurrentEpisode);
                }

                if (scrobbleData == null) return;

                // set duration/progress in scrobble data
                scrobbleData.Duration = Convert.ToInt32(duration).ToString();
                scrobbleData.Progress = Convert.ToInt32(progress).ToString();

                // set watching status on trakt
                TraktResponse response = TraktAPI.TraktAPI.ScrobbleEpisodeState(scrobbleData, TraktScrobbleStates.watching);
                TraktAPI.TraktAPI.LogTraktResponse(response);
            }), null, 3000, 900000);
            #endregion

            return true;
        }

        public void StopScrobble()
        {
            if (TraktTimer != null)
                TraktTimer.Dispose();
        }

        #endregion

        #region Data Creators

        /// <summary>
        /// Creates Sync Data based on Series object and a List of Episode objects
        /// </summary>
        /// <param name="series">The series to base the object on</param>
        /// <param name="epsiodes">The list of episodes to base the object on</param>
        /// <returns>The Trakt Sync data to send</returns>
        private TraktEpisodeSync CreateSyncData(DBSeries series, List<DBEpisode> episodes)
        {
            if (series == null) return null;

            // set series properties for episodes
            TraktEpisodeSync traktSync = new TraktEpisodeSync
            {
                Password = TraktSettings.Password,
                UserName = TraktSettings.Username,
                SeriesID = series[DBSeries.cID],
                IMDBID = series[DBOnlineSeries.cIMDBID],
                Year = series.Year,
                Title = series[DBOnlineSeries.cOriginalName]
            };

            // get list of episodes for series
            List<TraktEpisodeSync.Episode> epList = new List<TraktEpisodeSync.Episode>();

            foreach (DBEpisode ep in episodes.Where(e => e[DBEpisode.cSeriesID] == series[DBSeries.cID]))
            {
                TraktEpisodeSync.Episode episode = new TraktEpisodeSync.Episode();
                episode.SeasonIndex = ep[DBOnlineEpisode.cSeasonIndex];
                episode.EpisodeIndex = ep[DBOnlineEpisode.cEpisodeIndex];
                epList.Add(episode);
            }

            traktSync.EpisodeList = epList;
            return traktSync;
        }

        private TraktRateSeries CreateSeriesRateData(DBSeries series)
        {
            TraktRateValue loveorhate = series[DBOnlineSeries.cMyRating] >= 7.0 ? TraktRateValue.love : TraktRateValue.hate;

            TraktRateSeries seriesData = new TraktRateSeries()
            {
                Rating = loveorhate.ToString(),
                SeriesID = series[DBOnlineSeries.cID],
                Year = series.Year,
                Title = series[DBOnlineSeries.cOriginalName],
                UserName = TraktSettings.Username,
                Password = TraktSettings.Password,
            };

            TraktLogger.Info("Rating '{0}' as '{1}'", series.ToString(), loveorhate.ToString());
            return seriesData;
        }

        private TraktRateEpisode CreateEpisodeRateData(DBEpisode episode)
        {
            DBSeries series = Helper.getCorrespondingSeries(episode[DBOnlineEpisode.cSeriesID]);

            TraktRateValue loveorhate = episode[DBOnlineEpisode.cMyRating] >= 7.0 ? TraktRateValue.love : TraktRateValue.hate;

            TraktRateEpisode episodeData = new TraktRateEpisode()
            {
                Episode = episode[DBOnlineEpisode.cEpisodeIndex],
                Rating = loveorhate.ToString(),
                Season = episode[DBOnlineEpisode.cSeasonIndex],
                SeriesID = episode[DBOnlineEpisode.cSeriesID],
                Year = series.Year,
                Title = series[DBOnlineSeries.cOriginalName],
                UserName = TraktSettings.Username,
                Password = TraktSettings.Password
            };

            TraktLogger.Info("Rating '{0}' as '{1}'", episode.ToString(), loveorhate.ToString());
            return episodeData;
        }

        private TraktEpisodeScrobble CreateScrobbleData(DBEpisode episode)
        {
            DBSeries series = Helper.getCorrespondingSeries(episode[DBEpisode.cSeriesID]);
            if (series == null || series[DBOnlineSeries.cTraktIgnore]) return null;

            // create scrobble data
            TraktEpisodeScrobble scrobbleData = new TraktEpisodeScrobble
            {
                Title = series[DBOnlineSeries.cOriginalName],
                Year = series.Year,
                Season = episode[DBOnlineEpisode.cSeasonIndex],
                Episode = episode[DBOnlineEpisode.cEpisodeIndex],
                SeriesID = series[DBSeries.cID],
                PluginVersion = Assembly.GetCallingAssembly().GetName().Version.ToString(),
                MediaCenter = "Mediaportal",
                MediaCenterVersion = Assembly.GetEntryAssembly().GetName().Version.ToString(),
                MediaCenterBuildDate = String.Empty,
                UserName = TraktSettings.Username,
                Password = TraktSettings.Password
            };

            return scrobbleData;
        }

        #endregion

        #region Helpers

        private void RateEpisode(DBEpisode episode)
        {
            Thread rateThread = new Thread(delegate()
            {
                TraktRateResponse response = TraktAPI.TraktAPI.RateEpisode(CreateEpisodeRateData(episode));

                // check for any error and notify
                TraktAPI.TraktAPI.LogTraktResponse(response);
            })
            {
                IsBackground = true,
                Name = "Rate Episode"
            };

            rateThread.Start();
        }

        private void RateSeries(DBSeries series)
        {
            Thread rateThread = new Thread(delegate()
            {
                TraktRateResponse response = TraktAPI.TraktAPI.RateSeries(CreateSeriesRateData(series));

                // check for any error and notify
                TraktAPI.TraktAPI.LogTraktResponse(response);
            })
            {
                IsBackground = true,
                Name = "Rate Series"
            };

            rateThread.Start();
        }

        /// <summary>
        /// Syncronize our collection on trakt
        /// </summary>
        /// <param name="episodes">local tvseries dbepisode list</param>
        /// <param name="mode">trakt sync mode</param>
        private void SyncLibrary(List<DBEpisode> episodes, TraktSyncModes mode)
        {
            // get unique series ids
            var uniqueSeries = (from s in episodes select s[DBEpisode.cSeriesID].ToString()).Distinct().ToList();

            // go over each series, can only send one series at a time
            foreach (string seriesid in uniqueSeries)
            {
                // get series and check if we should ignore it
                DBSeries series = Helper.getCorrespondingSeries(int.Parse(seriesid));
                if (series == null || series[DBOnlineSeries.cTraktIgnore]) continue;

                TraktLogger.Info("Synchronizing '{0}' episodes for series '{1}'.", mode.ToString(), series.ToString());

                // upload to trakt
                TraktResponse response = TraktAPI.TraktAPI.SyncEpisodeLibrary(CreateSyncData(series, episodes), mode);

                // check for any error and log result
                TraktAPI.TraktAPI.LogTraktResponse(response);

                // wait a short period before uploading another series
                Thread.Sleep(2000);
            }
        }

        /// <summary>
        /// Confirm that tvseries dbepisode exists in our trakt collection
        /// </summary>
        /// <param name="traktEpisodes">trakt episode collection</param>
        /// <param name="episode">tvseries episode object</param>
        /// <returns>true if episode exists</returns>
        private bool TraktEpisodeExists(IEnumerable<TraktLibraryShow> traktEpisodes, DBEpisode episode)
        {
            var items = traktEpisodes.Where(s => s.SeriesId == episode[DBOnlineEpisode.cSeriesID] &&
                                                               s.Seasons.Where(e => e.Season == episode[DBOnlineEpisode.cSeasonIndex] &&
                                                                                    e.Episodes.Contains(episode[DBOnlineEpisode.cEpisodeIndex])).Count() == 1);
            return items.Count() == 1;
        }

        /// <summary>
        /// Removes episodes on trakt that no longer exist in users database
        /// </summary>
        /// <param name="traktShows">trakt episode collection</param>
        /// <param name="episodes">list of local episodes</param>
        /// <param name="seriesID">tvdb series id of series</param>
        /// <returns>true if episode exists</returns>
        private TraktEpisodeSync GetEpisodesForTraktRemoval(TraktLibraryShow traktShow, List<DBEpisode> episodes)
        {
            List<TraktEpisodeSync.Episode> episodeList = new List<TraktEpisodeSync.Episode>();
      
            foreach (var season in traktShow.Seasons)
            {
                foreach (var episode in season.Episodes)
                {
                    var query = episodes.Where(e => e[DBOnlineEpisode.cSeriesID] == traktShow.SeriesId &&
                                                    e[DBOnlineEpisode.cSeasonIndex] == season.Season &&
                                                    e[DBOnlineEpisode.cEpisodeIndex] == episode).ToList();

                    if (query.Count == 0)
                    {
                        // we dont have the episode
                        TraktLogger.Info("{0} - {1}x{2} does not exist in local database, marked for removal from trakt", traktShow.ToString(), season.Season.ToString(), episode.ToString());

                        TraktEpisodeSync.Episode ep = new TraktEpisodeSync.Episode
                        {
                            EpisodeIndex = episode.ToString(),
                            SeasonIndex = season.Season.ToString()
                        };
                        episodeList.Add(ep);
                    }
                }
            }
            

            if (episodeList.Count > 0)
            {
                TraktEpisodeSync syncData = new TraktEpisodeSync
                {
                    UserName = TraktSettings.Username,
                    Password = TraktSettings.Password,
                    SeriesID = traktShow.SeriesId,
                    EpisodeList = episodeList
                };
                return syncData;
            }
            return null;
        }

        #endregion

        #region TVSeries Events

        private void OnImportCompleted(bool dataUpdated)
        {
            TraktLogger.Info("TVSeries import complete, checking if sync required");

            if (dataUpdated)
            {
                // sync again
                Thread syncThread = new Thread(delegate()
                {
                    while (SyncInProgress)
                    {
                        // only do one sync at a time
                        TraktLogger.Info("TVSeries sync still in progress");
                        Thread.Sleep(60000);
                    }
                    SyncLibrary();
                })
                {
                    IsBackground = true,
                    Name = "TVSeries Sync"
                };

                syncThread.Start();
            }
            else
            {
                TraktLogger.Info("TVSeries sync is not required.");
            }
        }

        private void OnEpisodeWatched(DBEpisode episode)
        {
            DBEpisode currentEpisode = null;
            EpisodeWatching = false;

            Thread scrobbleEpisode = new Thread(delegate()
            {
                // submit watched state to trakt API
                // could be a double episode so mark last one as watched
                // 1st episode is set to watched during playback timer
                if (episode[DBEpisode.cEpisodeIndex2] > 0 && MarkedFirstAsWatched)
                {
                    // only set 2nd episode as watched here
                    SQLCondition condition = new SQLCondition();
                    condition.Add(new DBEpisode(), DBEpisode.cFilename, episode[DBEpisode.cFilename], SQLConditionType.Equal);
                    List<DBEpisode> episodes = DBEpisode.Get(condition, false);
                    currentEpisode = episodes[1];
                }
                else
                {
                    // single episode
                    currentEpisode = episode;
                }

                TraktLogger.Info("TVSeries episode considered watched '{0}'", currentEpisode.ToString());

                // get scrobble data to send to api
                TraktEpisodeScrobble scrobbleData = CreateScrobbleData(currentEpisode);
                if (scrobbleData == null) return;

                // set duration/progress in scrobble data
                double duration = currentEpisode[DBEpisode.cLocalPlaytime] / 60000;
                scrobbleData.Duration = Convert.ToInt32(duration).ToString();
                scrobbleData.Progress = "100";

                TraktResponse response = TraktAPI.TraktAPI.ScrobbleEpisodeState(scrobbleData, TraktScrobbleStates.scrobble);
                TraktAPI.TraktAPI.LogTraktResponse(response);
            })
            {
                IsBackground = true,
                Name = "Scrobble Episode"
            };

            scrobbleEpisode.Start();
        }

        private void OnEpisodeStarted(DBEpisode episode)
        {
            TraktLogger.Info("Starting TVSeries episode playback '{0}'", episode.ToString());
            EpisodeWatching = true;
            CurrentEpisode = episode;
        }

        private void OnEpisodeStopped(DBEpisode episode)
        {
            // Episode does not count as watched, we dont need to do anything.
            TraktLogger.Info("Stopped TVSeries episode playback '{0}'", episode.ToString());
            EpisodeWatching = false;
            StopScrobble();

            // send cancelled watching state
            Thread cancelWatching = new Thread(delegate()
            {
                TraktEpisodeScrobble scrobbleData = new TraktEpisodeScrobble { UserName = TraktSettings.Username, Password = TraktSettings.Password };
                TraktResponse response = TraktAPI.TraktAPI.ScrobbleEpisodeState(scrobbleData, TraktScrobbleStates.cancelwatching);
                TraktAPI.TraktAPI.LogTraktResponse(response);
            })
            {
                IsBackground = true,
                Name = "Cancel Watching Episode"
            };

            cancelWatching.Start();      
        }

        private void OnRateItem(DBTable item, string value)
        {
            TraktLogger.Info("Recieved rating event from tvseries");

            if (item is DBEpisode)
                RateEpisode(item as DBEpisode);
            else
                RateSeries(item as DBSeries);
        }

        private void OnToggleWatched(DBSeries series, List<DBEpisode> episodes, bool watched)
        {
            TraktLogger.Info("Recieved togglewatched event from tvseries");

            Thread toggleWatched = new Thread(delegate()
            {
                TraktResponse response = TraktAPI.TraktAPI.SyncEpisodeLibrary(CreateSyncData(series, episodes), watched ? TraktSyncModes.seen : TraktSyncModes.unseen);
                TraktAPI.TraktAPI.LogTraktResponse(response);
            })
            {
                IsBackground = true,
                Name = "TVSeries Toggle Watched"
            };

            toggleWatched.Start();
        }

        #endregion
      
    }
}
