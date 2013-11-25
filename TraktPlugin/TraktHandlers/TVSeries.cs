using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaPortal.Player;
using MediaPortal.GUI.Library;
using System.ComponentModel;
using System.Reflection;
using System.Threading;
using TraktPlugin.GUI;
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
        #region Enums

        public enum SelectedType
        {
            Series,
            Season,
            Episode,
            Unknown
        }

        #endregion

        #region Variables

        Timer TraktTimer;
        bool SyncInProgress;        
        bool EpisodeWatching;
        bool MarkedFirstAsWatched;
        DBEpisode CurrentEpisode;
        static VideoHandler player = null;

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
            PlayListPlayer.EpisodeWatched +=new PlayListPlayer.EpisodeWatchedDelegate(OnPlaylistEpisodeWatched);
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
            TraktLogger.Info("MP-TVSeries Starting Sync");
            SyncInProgress = true;

            List<DBEpisode> localAllEpisodes = new List<DBEpisode>();
            List<DBEpisode> localCollectionEpisodes = new List<DBEpisode>();
            List<DBEpisode> localWatchedEpisodes = new List<DBEpisode>();

            // store list of series ids so we can update the episode counts
            // of any series that syncback watched flags
            List<int> seriesToUpdateEpisodeCounts = new List<int>();

            // optional do library sync
            if (TraktSettings.SyncLibrary)
            {
                #region Get online data
                // get all episodes on trakt that are marked as in 'collection'
                TraktLogger.Info("Getting user {0}'s 'library' episodes from trakt", TraktSettings.Username);
                IEnumerable<TraktLibraryShow> traktCollectionEpisodes = TraktAPI.TraktAPI.GetLibraryEpisodesForUser(TraktSettings.Username);
                if (traktCollectionEpisodes == null)
                {
                    TraktLogger.Error("Error getting show collection from trakt server, cancelling sync.");
                    SyncInProgress = false;
                    return;
                }
                TraktLogger.Info("{0} tvshows in trakt collection", traktCollectionEpisodes.Count().ToString());

                // get all episodes on trakt that are marked as 'seen' or 'watched'
                TraktLogger.Info("Getting user {0}'s 'watched/seen' episodes from trakt", TraktSettings.Username);
                IEnumerable<TraktLibraryShow> traktWatchedEpisodes = TraktAPI.TraktAPI.GetWatchedEpisodesForUser(TraktSettings.Username);
                if (traktWatchedEpisodes == null)
                {
                    TraktLogger.Error("Error getting shows watched from trakt server, cancelling sync.");
                    SyncInProgress = false;
                    return;
                }
                TraktLogger.Info("{0} tvshows with watched episodes in trakt library", traktWatchedEpisodes.Count().ToString());

                // get all episodes on trakt that are marked as 'unseen'
                TraktLogger.Info("Getting user {0}'s 'unseen' episodes from trakt", TraktSettings.Username);
                IEnumerable<TraktLibraryShow> traktUnSeenEpisodes = TraktAPI.TraktAPI.GetUnSeenEpisodesForUser(TraktSettings.Username);
                if (traktUnSeenEpisodes == null)
                {
                    TraktLogger.Error("Error getting shows unseen from trakt server, cancelling sync.");
                    SyncInProgress = false;
                    return;
                }
                TraktLogger.Info("{0} tvshows with unseen episodes in trakt library", traktUnSeenEpisodes.Count().ToString());
                #endregion

                #region Get local data
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
                #endregion

                #region Sync collection/library to trakt
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

                #region Sync seen to trakt
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

                #region Sync watched flags from trakt locally
                // Sync watched flags from trakt to local database
                // do not mark as watched locally if UnSeen on trakt
                foreach (DBEpisode ep in localAllEpisodes.Where(e => e[DBOnlineEpisode.cWatched] == 0))
                {
                    if (TraktEpisodeExists(traktWatchedEpisodes, ep) && !TraktEpisodeExists(traktUnSeenEpisodes, ep))
                    {
                        DBSeries series = Helper.getCorrespondingSeries(ep[DBOnlineEpisode.cSeriesID]);
                        if (series == null || series[DBOnlineSeries.cTraktIgnore]) continue;

                        // mark episode as watched
                        TraktLogger.Info("Marking episode '{0}' as watched", ep.ToString());
                        ep[DBOnlineEpisode.cWatched] = true;
                        ep.Commit();

                        if (!seriesToUpdateEpisodeCounts.Contains(ep[DBOnlineEpisode.cSeriesID]))
                            seriesToUpdateEpisodeCounts.Add(ep[DBOnlineEpisode.cSeriesID]);
                    }
                }
                #endregion

                #region Sync unseen flags from trakt locally
                foreach (DBEpisode ep in localAllEpisodes.Where(e => e[DBOnlineEpisode.cWatched] == 1))
                {
                    if (TraktEpisodeExists(traktUnSeenEpisodes, ep))
                    {
                        DBSeries series = Helper.getCorrespondingSeries(ep[DBOnlineEpisode.cSeriesID]);
                        if (series == null || series[DBOnlineSeries.cTraktIgnore]) continue;

                        // mark episode as unwatched
                        TraktLogger.Info("Marking episode '{0}' as unwatched", ep.ToString());
                        ep[DBOnlineEpisode.cWatched] = false;
                        ep.Commit();

                        if (!seriesToUpdateEpisodeCounts.Contains(ep[DBOnlineEpisode.cSeriesID]))
                            seriesToUpdateEpisodeCounts.Add(ep[DBOnlineEpisode.cSeriesID]);
                    }
                }
                #endregion

                #region Update Episode counts in Local Database
                foreach (int seriesID in seriesToUpdateEpisodeCounts)
                {
                    DBSeries series = Helper.getCorrespondingSeries(seriesID);
                    if (series == null) continue;
                    TraktLogger.Info("Updating Episode Counts for series '{0}'", series.ToString());
                    DBSeries.UpdateEpisodeCounts(series);
                }
                #endregion

                #region Ratings Sync
                // only sync ratings if we are using Advanced Ratings
                if (TraktSettings.SyncRatings)
                {
                    #region Episode Ratings
                    TraktLogger.Info("Getting rated episodes from trakt");
                    var traktRatedEpisodes = TraktAPI.TraktAPI.GetUserRatedEpisodes(TraktSettings.Username);
                    if (traktRatedEpisodes == null)
                        TraktLogger.Error("Error getting rated episodes from trakt server.");
                    else
                        TraktLogger.Info("{0} rated episodes in trakt library", traktRatedEpisodes.Count().ToString());

                    if (traktRatedEpisodes != null)
                    {
                        // get the episodes that we have rated/unrated
                        var ratedEpisodeList = localAllEpisodes.Where(e => e[DBOnlineEpisode.cMyRating] > 0).ToList();
                        var unRatedEpisodeList = localAllEpisodes.Except(ratedEpisodeList).ToList();
                        TraktLogger.Info("{0} rated episodes available to sync in tvseries database", ratedEpisodeList.Count.ToString());

                        var ratedEpisodesToSync = new List<DBEpisode>(ratedEpisodeList);

                        // note: these two foreach loops can be expensive!

                        // find out what ratings are missing locally
                        foreach (var episode in unRatedEpisodeList)
                        {
                            var traktEpisode = traktRatedEpisodes.FirstOrDefault(e => e.EpisodeDetails.TVDBID == episode[DBOnlineEpisode.cID]);
                            if (traktEpisode != null)
                            {
                                // update local collection rating
                                TraktLogger.Info("Inserting rating '{0}/10' for episode '{1}'", traktEpisode.RatingAdvanced, episode.ToString());
                                episode[DBOnlineEpisode.cMyRating] = traktEpisode.RatingAdvanced;
                                episode.Commit();
                            }
                        }

                        // find out what ratings we can send to trakt
                        foreach (var episode in ratedEpisodeList)
                        {
                            var traktEpisode = traktRatedEpisodes.FirstOrDefault(e => e.EpisodeDetails.TVDBID == episode[DBOnlineEpisode.cID]);
                            if (traktEpisode != null)
                            {
                                // already rated on trakt, remove from sync collection
                                ratedEpisodesToSync.Remove(episode);
                            }
                        }

                        TraktLogger.Info("{0} rated episodes to sync to trakt", ratedEpisodesToSync.Count);
                        if (ratedEpisodesToSync.Count > 0)
                        {
                            ratedEpisodesToSync.ForEach(a => TraktLogger.Info("Importing rating '{0}/10' for episode '{1}'", a[DBOnlineEpisode.cMyRating], a.ToString()));
                            TraktResponse response = TraktAPI.TraktAPI.RateEpisodes(CreateRatingEpisodesData(ratedEpisodesToSync));
                            TraktLogger.LogTraktResponse(response);
                        }
                    }
                    #endregion

                    #region Show Ratings
                    TraktLogger.Info("Getting rated shows from trakt");
                    var traktRatedShows = TraktAPI.TraktAPI.GetUserRatedShows(TraktSettings.Username);
                    if (traktRatedShows == null)
                        TraktLogger.Error("Error getting rated shows from trakt server.");
                    else
                        TraktLogger.Info("{0} rated shows in trakt library", traktRatedShows.Count().ToString());

                    if (traktRatedShows != null)
                    {
                        // get the shows that we have rated/unrated
                        var localAllSeries = DBSeries.Get(new SQLCondition());
                        var ratedShowList = localAllSeries.Where(s => s[DBOnlineSeries.cMyRating] > 0).ToList();
                        var unRatedShowList = localAllSeries.Except(ratedShowList).ToList();
                        TraktLogger.Info("{0} rated shows available to sync in tvseries database", ratedShowList.Count.ToString());

                        var ratedShowsToSync = new List<DBSeries>(ratedShowList);
                        foreach (var traktShow in traktRatedShows)
                        {
                            foreach (var show in unRatedShowList.Where(s => s[DBSeries.cID] == traktShow.TVDBID))
                            {
                                // update local collection rating
                                TraktLogger.Info("Inserting rating '{0}/10' for show '{1}'", traktShow.RatingAdvanced, show.ToString());
                                show[DBOnlineEpisode.cMyRating] = traktShow.RatingAdvanced;
                                show.Commit();
                            }

                            foreach (var show in ratedShowList.Where(s => s[DBSeries.cID] == traktShow.TVDBID))
                            {
                                // already rated on trakt, remove from sync collection
                                ratedShowsToSync.Remove(show);
                            }
                        }

                        TraktLogger.Info("{0} rated shows to sync to trakt", ratedShowsToSync.Count);
                        if (ratedShowsToSync.Count > 0)
                        {
                            ratedShowsToSync.ForEach(a => TraktLogger.Info("Importing rating '{0}/10' for show '{1}'", a[DBOnlineSeries.cMyRating], a.ToString()));
                            TraktResponse response = TraktAPI.TraktAPI.RateSeries(CreateRatingShowsData(ratedShowsToSync));
                            TraktLogger.LogTraktResponse(response);
                        }
                    }
                    #endregion
                }
                #endregion

                #region Clean Library
                if (TraktSettings.KeepTraktLibraryClean && TraktSettings.TvShowPluginCount == 1)
                {
                    TraktLogger.Info("Removing shows From Trakt Collection no longer in database");

                    // if we no longer have a file reference in database remove from library
                    foreach (var series in traktCollectionEpisodes)
                    {
                        TraktEpisodeSync syncData = GetEpisodesForTraktRemoval(series, localCollectionEpisodes.Where(e => e[DBOnlineEpisode.cSeriesID] == series.SeriesId).ToList());
                        if (syncData == null) continue;
                        TraktResponse response = TraktAPI.TraktAPI.SyncEpisodeLibrary(syncData, TraktSyncModes.unlibrary);
                        TraktLogger.LogTraktResponse(response);
                        Thread.Sleep(500);
                    }
                }
                #endregion

                #region Cache Shows In Library
                TraktLogger.Debug("Caching TVShows in Library");
                TraktSettings.ShowsInCollection = traktCollectionEpisodes.Select(e => e.SeriesId).ToList();
                #endregion
            }

            SyncInProgress = false;
            TraktLogger.Info("MP-TVSeries Sync Completed");
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
                Thread.CurrentThread.Name = "Scrobble";

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
                TraktLogger.LogTraktResponse(response);
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

        #region Public Methods

        public void DisposeEvents()
        {
            TraktLogger.Debug("Removing Hooks from MP-TVSeries");

            // player events
            VideoHandler.EpisodeWatched -= new VideoHandler.EpisodeWatchedDelegate(OnEpisodeWatched);
            VideoHandler.EpisodeStarted -= new VideoHandler.EpisodeStartedDelegate(OnEpisodeStarted);
            VideoHandler.EpisodeStopped -= new VideoHandler.EpisodeStoppedDelegate(OnEpisodeStopped);
            PlayListPlayer.EpisodeWatched -= new PlayListPlayer.EpisodeWatchedDelegate(OnEpisodeWatched);
            PlayListPlayer.EpisodeStarted -= new PlayListPlayer.EpisodeStartedDelegate(OnEpisodeStarted);
            PlayListPlayer.EpisodeStopped -= new PlayListPlayer.EpisodeStoppedDelegate(OnEpisodeStopped);

            // import events
            OnlineParsing.OnlineParsingCompleted -= new OnlineParsing.OnlineParsingCompletedHandler(OnImportCompleted);

            // gui events
            TVSeriesPlugin.RateItem -= new TVSeriesPlugin.RatingEventDelegate(OnRateItem);
            TVSeriesPlugin.ToggleWatched -= new TVSeriesPlugin.ToggleWatchedEventDelegate(OnToggleWatched);
        }

        /// <summary>
        /// Playback an episode using TVSeries internal Video Handler
        /// </summary>
        /// <param name="seriesid">series id of episode</param>
        /// <param name="seasonid">season index</param>
        /// <param name="episodeid">episode index</param>        
        public static bool PlayEpisode(int seriesid, int seasonid, int episodeid)
        {
            var episodes = DBEpisode.Get(seriesid, seasonid);
            var episode = episodes.FirstOrDefault(e => (e[DBEpisode.cEpisodeIndex] == episodeid || e[DBEpisode.cEpisodeIndex2] == episodeid) && !string.IsNullOrEmpty(e[DBEpisode.cFilename]));
            if (episode == null) return false;

            return PlayEpisode(episode);
        }

        /// <summary>
        /// Playback the first unwatched episode for a series using TVSeries internal Video Handler
        /// If no Unwatched episodes exists, play the Most Recently Aired
        /// </summary>
        /// <param name="seriesid">series id of episode</param>
        public static bool PlayFirstUnwatchedEpisode(int seriesid)
        {
            try
            {
                var episodes = DBEpisode.Get(seriesid);
                if (episodes == null || episodes.Count == 0)
                {
                    TraktLogger.Info("Found no episodes for TVDb {0}", seriesid.ToString());
                    return false;
                }

                // filter out anything we can't play
                episodes.RemoveAll(e => string.IsNullOrEmpty(e[DBEpisode.cFilename]));
                if (episodes.Count == 0)
                {
                    TraktLogger.Info("Found no local episodes for TVDb {0}", seriesid.ToString());
                    return false;
                }

                TraktLogger.Info("Found {0} local episodes for TVDb {1}", episodes.Count, seriesid.ToString());

                // sort episodes using DBEpisode sort comparer
                // this takes into consideration Aired/DVD order and Specials in-line sorting
                episodes.Sort();

                // get first episode unwatched, otherwise get most recently aired
                var episode = episodes.Where(e => e[DBOnlineEpisode.cWatched] == 0).FirstOrDefault();
                if (episode == null)
                {
                    TraktLogger.Info("No Unwatched episodes found, Playing most recent episode");
                    episode = episodes.LastOrDefault();
                }
                if (episode == null) return false;

                return PlayEpisode(episode);
            }
            catch (Exception e)
            {
                TraktLogger.Error("Error attempting to play first unwatched episode");
                TraktLogger.Error("Error Message: ", e.Message);
                return false;
            }
        }

        public static bool PlayEpisode(DBEpisode episode)
        {              
            if (player == null) player = new VideoHandler();
            return player.ResumeOrPlay(episode);
        }

        /// <summary>
        /// Get the current selected facade item in TVSeries
        /// </summary>
        /// <param name="obj">TVTag object</param>
        /// <returns>Returns the selected type</returns>
        public static SelectedType GetSelectedType(Object obj)
        {
            if ((obj as DBEpisode) != null) return SelectedType.Episode;
            if ((obj as DBSeries) != null) return SelectedType.Series;
            if ((obj as DBSeason) != null) return SelectedType.Season;
            return SelectedType.Unknown;
        }

        /// <summary>
        /// Get Episode Info for selected object
        /// </summary>        
        public static bool GetEpisodeInfo(Object obj, out string title, out string tvdb, out string seasonidx, out string episodeidx, out bool isWatched)
        {
            title = string.Empty;
            tvdb = string.Empty;
            seasonidx = string.Empty;
            episodeidx = string.Empty;
            isWatched = false;

            if (obj == null) return false;

            DBEpisode episode = obj as DBEpisode;
            if (episode == null) return false;

            DBSeries series = Helper.getCorrespondingSeries(episode[DBOnlineEpisode.cSeriesID]);
            if (series == null) return false;

            title = series[DBOnlineSeries.cOriginalName];
            tvdb = series[DBSeries.cID];
            seasonidx = episode[DBOnlineEpisode.cSeasonIndex];
            episodeidx = episode[DBOnlineEpisode.cEpisodeIndex];
            isWatched = episode[DBOnlineEpisode.cWatched];

            return true;
        }

        public static bool GetEpisodePersonInfo(Object obj, out SearchPeople searchPeople)
        {
            searchPeople = new SearchPeople();

            if (obj == null) return false;

            DBEpisode episode = obj as DBEpisode;
            if (episode == null) return false;

            DBSeries series = Helper.getCorrespondingSeries(episode[DBOnlineEpisode.cSeriesID]);
            if (series == null) return false;

            try
            {
                searchPeople.Actors.AddRange(series[DBOnlineSeries.cActors].ToString().Split('|').Where(s => s.Trim().Length > 0));
                searchPeople.Directors.AddRange(episode[DBOnlineEpisode.cDirector].ToString().Split('|').Where(s => s.Trim().Length > 0));
                searchPeople.Writers.AddRange(episode[DBOnlineEpisode.cWriter].ToString().Split('|').Where(s => s.Trim().Length > 0));
                searchPeople.GuestStars.AddRange(episode[DBOnlineEpisode.cGuestStars].ToString().Split('|').Where(s => s.Trim().Length > 0));
            }
            catch
            {
                TraktLogger.Error("Error getting Episode Person Info.");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Get Series Info for selected object
        /// </summary>
        public static bool GetSeriesInfo(Object obj, out string title, out string tvdb)
        {
            title = string.Empty;
            tvdb = string.Empty;
       
            if (obj == null) return false;

            DBSeries series = obj as DBSeries;
            if (series == null) return false;

            title = series[DBOnlineSeries.cOriginalName];
            tvdb = series[DBSeries.cID];
           
            return true;
        }

        public static bool GetSeriesPersonInfo(Object obj, out SearchPeople searchPeople)
        {
            searchPeople = new SearchPeople();

            if (obj == null) return false;

            DBSeries series = obj as DBSeries;
            if (series == null) return false;

            try
            {
                searchPeople.Actors.AddRange(series[DBOnlineSeries.cActors].ToString().Split('|').Where(s => s.Trim().Length > 0));
            }
            catch
            {
                TraktLogger.Error("Error getting Episode Person Info.");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Finds an episode in the current TVSeries Episode List
        /// </summary>
        /// <param name="episode"></param>
        /// <returns></returns>
        public static GUIListItem FindEpisodeInFacade(DBEpisode episode)
        {
            if (Facade == null) return null;

            for (int i = 0; i < Facade.Count; i++)
            {
                var control = Facade[i];
                GUIListItem listItem = control as GUIListItem;
                if (listItem == null) return null;

                // not in the episode level anymore
                DBEpisode ep = listItem.TVTag as DBEpisode;
                if (ep == null) return null;

                if (ep.Equals(episode))
                    return listItem;
            }

            return null;
        }

        public static bool SeriesExists(int seriesId)
        {
            var series = DBSeries.Get(seriesId);
            return (series != null);
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Get the TVTag of the selected facade item in TVseries window
        /// </summary>
        public static Object SelectedObject
        {
            get
            {
                if (Facade == null) return null;

                // Get the Selected Item
                GUIListItem currentitem = Facade.SelectedListItem;
                if (currentitem == null) return null;

                // Get the series/episode object
                return currentitem.TVTag;
            }
        }

        public static GUIFacadeControl Facade
        {
            get
            {
                // Ensure we are in TVSeries window
                GUIWindow window = GUIWindowManager.GetWindow((int)ExternalPluginWindows.TVSeries);
                if (window == null) return null;

                // Get the Facade control
                return window.GetControl(50) as GUIFacadeControl;
            }
        }

        public static void UpdateSettingAsBool(string setting, bool value)
        {
            DBOption.SetOptions(setting, value);
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
            if (series == null || series[DBOnlineSeries.cTraktIgnore]) return null;

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
            if (series == null || series[DBOnlineSeries.cTraktIgnore]) return null;

            string rating = series[DBOnlineSeries.cMyRating];

            TraktRateSeries seriesData = new TraktRateSeries()
            {
                Rating = rating,
                SeriesID = series[DBOnlineSeries.cID],
                Year = series.Year,
                Title = series[DBOnlineSeries.cOriginalName],
                UserName = TraktSettings.Username,
                Password = TraktSettings.Password,
            };

            TraktLogger.Info("Rating '{0}' as '{1}/10'", series.ToString(), rating.ToString());
            return seriesData;
        }

        private TraktRateEpisode CreateEpisodeRateData(DBEpisode episode)
        {
            DBSeries series = Helper.getCorrespondingSeries(episode[DBOnlineEpisode.cSeriesID]);

            if (series == null || series[DBOnlineSeries.cTraktIgnore]) return null;

            string rating = episode[DBOnlineEpisode.cMyRating];

            TraktRateEpisode episodeData = new TraktRateEpisode()
            {
                Episode = episode[DBOnlineEpisode.cEpisodeIndex],
                Rating = rating,
                Season = episode[DBOnlineEpisode.cSeasonIndex],
                SeriesID = episode[DBOnlineEpisode.cSeriesID],
                Year = series.Year,
                Title = series[DBOnlineSeries.cOriginalName],
                UserName = TraktSettings.Username,
                Password = TraktSettings.Password
            };

            TraktLogger.Info("Rating '{0}' as '{1}/10'", episode.ToString(), rating.ToString());
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
                EpisodeID = episode[DBOnlineEpisode.cID],
                SeriesID = series[DBSeries.cID],
                PluginVersion = TraktSettings.Version,
                MediaCenter = "Mediaportal",
                MediaCenterVersion = Assembly.GetEntryAssembly().GetName().Version.ToString(),
                MediaCenterBuildDate = String.Empty,
                UserName = TraktSettings.Username,
                Password = TraktSettings.Password
            };

            return scrobbleData;
        }

        private TraktRateEpisodes CreateRatingEpisodesData(List<DBEpisode> localEpisodes)
        {
            if (String.IsNullOrEmpty(TraktSettings.Username) || String.IsNullOrEmpty(TraktSettings.Password))
                return null;

            var traktEpisodes = from e in localEpisodes
                                select new TraktRateEpisodes.Episode
                                {
                                    TVDBID = e[DBEpisode.cSeriesID],
                                    SeasonNumber = e[DBOnlineEpisode.cSeasonIndex],
                                    EpisodeNumber = e[DBOnlineEpisode.cEpisodeIndex],
                                    Rating = e[DBOnlineEpisode.cMyRating]
                                };

            return new TraktRateEpisodes
            {
                UserName = TraktSettings.Username,
                Password = TraktSettings.Password,
                Episodes = traktEpisodes.ToList()
            };
        }

        private TraktRateShows CreateRatingShowsData(List<DBSeries> localShows)
        {
            if (String.IsNullOrEmpty(TraktSettings.Username) || String.IsNullOrEmpty(TraktSettings.Password))
                return null;

            var traktShows = from s in localShows
                             select new TraktRateShows.Show
                             {
                                TVDBID = s[DBOnlineSeries.cID],
                                Rating = s[DBOnlineSeries.cMyRating]
                             };

            return new TraktRateShows
            {
                UserName = TraktSettings.Username,
                Password = TraktSettings.Password,
                Shows = traktShows.ToList()
            };
        }

        #endregion

        #region Helpers

        private void RateEpisode(DBEpisode episode)
        {
            Thread rateThread = new Thread(delegate()
            {
                TraktLogger.Info("Received Rate Episode event from tvseries");

                TraktRateEpisode episodeRateData = CreateEpisodeRateData(episode);
                if (episodeRateData == null) return;
                TraktRateResponse response = TraktAPI.TraktAPI.RateEpisode(episodeRateData);

                // check for any error and notify
                TraktLogger.LogTraktResponse(response);
            })
            {
                IsBackground = true,
                Name = "Rate"
            };

            rateThread.Start();
        }

        private void RateSeries(DBSeries series)
        {
            Thread rateThread = new Thread(delegate()
            {
                TraktLogger.Info("Received Rate Show event from tvseries");

                TraktRateSeries seriesRateData = CreateSeriesRateData(series);
                if (seriesRateData == null) return;
                TraktRateResponse response = TraktAPI.TraktAPI.RateSeries(seriesRateData);

                // check for any error and notify
                TraktLogger.LogTraktResponse(response);
            })
            {
                IsBackground = true,
                Name = "Rate"
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
                TraktLogger.LogTraktResponse(response);

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

        /// <summary>
        /// Shows the Rate Episode Dialog after playback has ended
        /// </summary>
        /// <param name="episode">The episode being rated</param>
        private void ShowRateDialog(DBEpisode episode, bool isPlaylist)
        {
            if (DBOption.GetOptions(DBOption.cAskToRate)) return;               // tvseries dialog is enabled
            if (!TraktSettings.ShowRateDialogOnWatched) return;                 // not enabled
            if (episode[DBOnlineEpisode.cMyRating] > 0) return;                 // already rated
            if (isPlaylist && !TraktSettings.ShowRateDlgForPlaylists) return;   // disabled for playlists

            TraktLogger.Debug("Showing rate dialog for '{0}'", episode.ToString());

            new Thread((o) =>
            {
                DBEpisode epToRate = o as DBEpisode;
                if (epToRate == null) return;

                DBSeries series = Helper.getCorrespondingSeries(epToRate[DBOnlineEpisode.cSeriesID]);

                TraktRateEpisode rateObject = new TraktRateEpisode
                {
                    SeriesID = epToRate[DBOnlineEpisode.cSeriesID],
                    Title = series == null ? string.Empty : series.ToString(),
                    Episode = epToRate[DBOnlineEpisode.cEpisodeIndex],
                    Season = epToRate[DBOnlineEpisode.cSeasonIndex],
                    UserName = TraktSettings.Username,
                    Password = TraktSettings.Password
                };

                // get the rating submitted to trakt
                int rating = int.Parse(GUIUtils.ShowRateDialog<TraktRateEpisode>(rateObject));

                if (rating > 0)
                {
                    TraktLogger.Debug("Rating {0} as {1}/10", epToRate.ToString(), rating.ToString());
                     
                    epToRate[DBOnlineEpisode.cMyRating] = rating;
                    if (epToRate[DBOnlineEpisode.cRatingCount] == 0)
                    {
                        // not really needed but nice touch
                        // tvseries does not do this automatically on userrating insert
                        // we could do one step further and re-calculate rating for any vote count
                        epToRate[DBOnlineEpisode.cRatingCount] = 1;
                        epToRate[DBOnlineEpisode.cRating] = rating;
                    }
                    // ensure we force watched flag otherwise
                    // we will overwrite current state on facade with state before playback
                    epToRate[DBOnlineEpisode.cWatched] = true;
                    epToRate.Commit();

                    // update the facade holding the episode objects
                    var listItem = FindEpisodeInFacade(episode);
                    if (listItem != null)
                    {
                        listItem.TVTag = epToRate;
                    }
                }
            })
            {
                Name = "Rate",
                IsBackground = true
            }.Start(episode);
        }

        #endregion

        #region TVSeries Events

        private void OnImportCompleted(bool newEpisodeAdded)
        {
            Thread.CurrentThread.Name = "LibrarySync";

            if (TraktSettings.AccountStatus != ConnectionState.Connected) return;

            TraktLogger.Debug("MP-TVSeries import complete, checking if sync required.");

            if (newEpisodeAdded)
            {
                // sync again
                Thread syncThread = new Thread(delegate()
                {
                    TraktLogger.Info("New Episodes added in TVSeries, starting sync.");

                    while (SyncInProgress)
                    {
                        // only do one sync at a time
                        TraktLogger.Debug("MP-TVSeries sync still in progress. Trying again in 60secs.");
                        Thread.Sleep(60000);
                    }
                    SyncLibrary();
                })
                {
                    IsBackground = true,
                    Name = "LibrarySync"
                };

                syncThread.Start();
            }
            else
            {
                TraktLogger.Debug("MP-TVSeries sync is not required.");
            }
        }

        private void OnPlaylistEpisodeWatched(DBEpisode episode)
        {
            OnEpisodeWatched(episode, true);
        }
        private void OnEpisodeWatched(DBEpisode episode)
        {
            OnEpisodeWatched(episode, false);
        }
        private void OnEpisodeWatched(DBEpisode episode, bool isPlaylist)
        {
            if (TraktSettings.AccountStatus != ConnectionState.Connected) return;

            DBEpisode currentEpisode = null;
            EpisodeWatching = false;

            Thread scrobbleEpisode = new Thread(delegate(object o)
            {
                DBEpisode ep = o as DBEpisode;
                if (o == null) return;

                // submit watched state to trakt API
                // could be a double episode so mark last one as watched
                // 1st episode is set to watched during playback timer
                if (ep[DBEpisode.cEpisodeIndex2] > 0 && MarkedFirstAsWatched)
                {
                    // only set 2nd episode as watched here
                    SQLCondition condition = new SQLCondition();
                    condition.Add(new DBEpisode(), DBEpisode.cFilename, ep[DBEpisode.cFilename], SQLConditionType.Equal);
                    List<DBEpisode> episodes = DBEpisode.Get(condition, false);
                    currentEpisode = episodes[1];
                }
                else
                {
                    // single episode
                    currentEpisode = ep;
                }

                // show trakt rating dialog
                ShowRateDialog(currentEpisode, isPlaylist);

                TraktLogger.Info("MP-TVSeries episode considered watched '{0}'", currentEpisode.ToString());

                // get scrobble data to send to api
                TraktEpisodeScrobble scrobbleData = CreateScrobbleData(currentEpisode);
                if (scrobbleData == null) return;

                // set duration/progress in scrobble data
                double duration = currentEpisode[DBEpisode.cLocalPlaytime] / 60000;
                scrobbleData.Duration = Convert.ToInt32(duration).ToString();
                scrobbleData.Progress = "100";

                TraktResponse response = TraktAPI.TraktAPI.ScrobbleEpisodeState(scrobbleData, TraktScrobbleStates.scrobble);
                TraktLogger.LogTraktResponse(response);
            })
            {
                IsBackground = true,
                Name = "Scrobble"
            };

            scrobbleEpisode.Start(episode);
        }

        private void OnEpisodeStarted(DBEpisode episode)
        {
            if (TraktSettings.AccountStatus != ConnectionState.Connected) return;

            TraktLogger.Info("Starting MP-TVSeries episode playback '{0}'", episode.ToString());
            EpisodeWatching = true;
            CurrentEpisode = episode;
        }

        private void OnEpisodeStopped(DBEpisode episode)
        {
            if (TraktSettings.AccountStatus != ConnectionState.Connected) return;

            // Episode does not count as watched, we dont need to do anything.
            TraktLogger.Info("Stopped MP-TVSeries episode playback '{0}'", episode.ToString());
            EpisodeWatching = false;
            StopScrobble();

            // send cancelled watching state
            Thread cancelWatching = new Thread(delegate()
            {
                TraktEpisodeScrobble scrobbleData = new TraktEpisodeScrobble { UserName = TraktSettings.Username, Password = TraktSettings.Password };
                TraktResponse response = TraktAPI.TraktAPI.ScrobbleEpisodeState(scrobbleData, TraktScrobbleStates.cancelwatching);
                TraktLogger.LogTraktResponse(response);
            })
            {
                IsBackground = true,
                Name = "CancelWatching"
            };

            cancelWatching.Start();      
        }

        private void OnRateItem(DBTable item, string value)
        {
            if (TraktSettings.AccountStatus != ConnectionState.Connected) return;

            if (item is DBEpisode)
                RateEpisode(item as DBEpisode);
            else
                RateSeries(item as DBSeries);
        }

        private void OnToggleWatched(DBSeries series, List<DBEpisode> episodes, bool watched)
        {
            if (TraktSettings.AccountStatus != ConnectionState.Connected) return;

            Thread toggleWatched = new Thread(delegate()
            {
                TraktLogger.Info("Received togglewatched event from mp-tvseries");

                TraktEpisodeSync episodeSyncData = CreateSyncData(series, episodes);
                if (episodeSyncData == null) return;
                TraktResponse response = TraktAPI.TraktAPI.SyncEpisodeLibrary(episodeSyncData, watched ? TraktSyncModes.seen : TraktSyncModes.unseen);
                TraktLogger.LogTraktResponse(response);
            })
            {
                IsBackground = true,
                Name = "ToggleWatched"
            };

            toggleWatched.Start();
        }

        #endregion
      
    }
}
