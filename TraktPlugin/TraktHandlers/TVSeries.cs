using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using MediaPortal.GUI.Library;
using MediaPortal.Player;
using TraktPlugin.Extensions;
using TraktPlugin.GUI;
using TraktPlugin.TraktAPI.DataStructures;
using TraktPlugin.TraktAPI.Enums;
using TraktPlugin.TraktAPI.Extensions;
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

        bool SyncLibraryInProgress;
        bool SyncPlaybackInProgress;
        bool EpisodeWatching;
        bool FirstEpisodeWatched;
        DBEpisode CurrentEpisode;
        DBEpisode SecondEpisode;
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
            TraktLogger.Info("MP-TVSeries Library Starting Sync");
            SyncLibraryInProgress = true;

            // store list of series ids so we can update the episode counts
            // of any series that syncback watched flags
            var seriesToUpdateEpisodeCounts = new HashSet<int>();

            #region Get online data from trakt.tv

            // clear the last time(s) we did anything online
            TraktCache.ClearLastActivityCache();

            #region UnWatched / Watched

            // get all episodes on trakt that are marked as 'unseen'
            TraktLogger.Info("Getting user {0}'s unwatched episodes from trakt.tv", TraktSettings.Username);
            var traktUnWatchedEpisodes = TraktCache.GetUnWatchedEpisodesFromTrakt().ToNullableList();
            if (traktUnWatchedEpisodes == null)
            {
                TraktLogger.Error("Error getting tv shows unwatched from trakt.tv server");
            }
            else
            {
                TraktLogger.Info("Found {0} unwatched tv episodes in trakt.tv library", traktUnWatchedEpisodes.Count());
            }

            // get all episodes on trakt that are marked as 'seen' or 'watched'
            TraktLogger.Info("Getting user {0}'s watched episodes from trakt.tv", TraktSettings.Username);
            var traktWatchedEpisodes = TraktCache.GetWatchedEpisodesFromTrakt().ToNullableList();
            if (traktWatchedEpisodes == null)
            {
                TraktLogger.Error("Error getting tv shows watched from trakt.tv server");
            }
            else
            {
                TraktLogger.Info("Found {0} watched tv episodes in trakt.tv library", traktWatchedEpisodes.Count());
            }

            #endregion

            #region Collection

            // get all episodes on trakt that are marked as in 'collection'
            TraktLogger.Info("Getting user {0}'s collected tv episodes from trakt.tv", TraktSettings.Username);
            var traktCollectedEpisodes = TraktCache.GetCollectedEpisodesFromTrakt().ToNullableList();
            if (traktCollectedEpisodes == null)
            {
                TraktLogger.Error("Error getting tv episode collection from trakt.tv server");
            }
            else
            {
                TraktLogger.Info("Found {0} tv episodes in trakt.tv collection", traktCollectedEpisodes.Count());
            }
            #endregion

            #region Ratings

            #region Episodes

            TraktLogger.Info("Getting user {0}'s rated episodes from trakt.tv", TraktSettings.Username);
            var traktRatedEpisodes = TraktCache.GetRatedEpisodesFromTrakt().ToNullableList();
            if (traktRatedEpisodes == null)
            {
                TraktLogger.Error("Error getting rated episodes from trakt.tv server");
            }
            else
            {
                TraktLogger.Info("Found {0} rated tv episodes in trakt.tv library", traktRatedEpisodes.Count());
            }

            #endregion

            #region Shows

            TraktLogger.Info("Getting user {0}'s rated shows from trakt.tv", TraktSettings.Username);
            var traktRatedShows = TraktCache.GetRatedShowsFromTrakt().ToNullableList();
            if (traktRatedShows == null)
            {
                TraktLogger.Error("Error getting rated shows from trakt.tv server");
            }
            else
            {
                TraktLogger.Info("Found {0} rated tv shows in trakt.tv library", traktRatedShows.Count());
            }

            #endregion

            #endregion

            #region Watchlist

            #region Shows

            TraktLogger.Info("Getting user {0}'s watchlisted shows from trakt.tv", TraktSettings.Username);
            var traktWatchlistedShows = TraktCache.GetWatchlistedShowsFromTrakt();
            if (traktWatchlistedShows == null)
            {
                TraktLogger.Error("Error getting watchlisted shows from trakt.tv server");
            }
            else
            {
                TraktLogger.Info("Found {0} watchlisted tv shows in trakt.tv library", traktWatchlistedShows.Count());
            }

            #endregion

            #region Episodes

            TraktLogger.Info("Getting user {0}'s watchlisted episodes from trakt.tv", TraktSettings.Username);
            var traktWatchlistedEpisodes = TraktCache.GetWatchlistedEpisodesFromTrakt();
            if (traktWatchlistedEpisodes == null)
            {
                TraktLogger.Error("Error getting watchlisted episodes from trakt.tv server");
            }
            else
            {
                TraktLogger.Info("Found {0} watchlisted tv episodes in trakt.tv library", traktWatchlistedEpisodes.Count());
            }

            #endregion

            #endregion

            #endregion

            // optionally do library sync
            if (TraktSettings.SyncLibrary)
            {
                #region Get data from local database

                TraktLogger.Info("Getting local episodes from tvseries database, Ignoring {0} tv show(s) set by user", IgnoredSeries.Count);

                // Get all episodes in database
                SQLCondition conditions = new SQLCondition();
                conditions.Add(new DBOnlineEpisode(), DBOnlineEpisode.cSeriesID, 0, SQLConditionType.GreaterThan);
                conditions.Add(new DBOnlineEpisode(), DBOnlineEpisode.cHidden, 0, SQLConditionType.Equal);
                var localEpisodes = DBEpisode.Get(conditions, false);

                int episodeCount = localEpisodes.Count;

                // filter out the ignored shows
                localEpisodes.RemoveAll(e => IgnoredSeries.Contains(e[DBOnlineEpisode.cSeriesID]));

                TraktLogger.Info("Found {0} total episodes in tvseries database{1}", episodeCount, IgnoredSeries.Count > 0 ? string.Format(" and {0} ignored episodes", episodeCount - localEpisodes.Count) : "");

                // Get episodes of files that we have locally
                var localCollectedEpisodes = localEpisodes.Where(e => !string.IsNullOrEmpty(e[DBEpisode.cFilename].ToString())).ToList();

                TraktLogger.Info("Found {0} episodes with local files in tvseries database", localCollectedEpisodes.Count);

                // Get watched episodes of files that we have locally or are remote
                // user could of deleted episode from disk but still have reference to it in database           
                var localWatchedEpisodes = localEpisodes.Where(e => e[DBOnlineEpisode.cWatched] > 0).ToList();
                var localUnWatchedEpisodes = localEpisodes.Except(localWatchedEpisodes).ToList();

                TraktLogger.Info("Found {0} episodes watched in tvseries database", localWatchedEpisodes.Count);

                var localRatedEpisodes = new List<DBEpisode>();
                var localNonRatedEpisodes = new List<DBEpisode>();
                var localRatedShows = new List<DBSeries>();
                var localNonRatedShows = new List<DBSeries>();
                if (TraktSettings.SyncRatings)
                {
                    // get the episodes that we have rated/unrated
                    localRatedEpisodes.AddRange(localEpisodes.Where(e => e[DBOnlineEpisode.cMyRating] > 0));
                    localNonRatedEpisodes = localEpisodes.Except(localRatedEpisodes).ToList();
                    TraktLogger.Info("Found {0} episodes rated in tvseries database", localRatedEpisodes.Count);

                    // get the shows that we have rated/unrated
                    var shows = DBSeries.Get(new SQLCondition());
                    localRatedShows.AddRange(shows.Where(s => s[DBOnlineSeries.cMyRating] > 0 && !IgnoredSeries.Contains(s[DBOnlineSeries.cID])));
                    localNonRatedShows = shows.Except(localRatedShows).ToList();
                    TraktLogger.Info("Found {0} shows rated in tvseries database", localRatedShows.Count);
                }

                #endregion

                #region Mark episodes as unwatched in local database

                TraktLogger.Info("Start sync of tv episode unwatched state to local database");
                if (traktUnWatchedEpisodes != null && traktUnWatchedEpisodes.Count() > 0)
                {
                    // create a unique key to lookup and search for faster
                    var localLookupEpisodes = localWatchedEpisodes.ToLookup(twe => CreateLookupKey(twe), twe => twe);

                    foreach (var episode in traktUnWatchedEpisodes)
                    {
                        if (IgnoredSeries.Exists(tvdbid => tvdbid == episode.ShowTvdbId))
                            continue;

                        string tvdbKey = CreateLookupKey(episode);

                        var watchedEpisode = localLookupEpisodes[tvdbKey].FirstOrDefault();
                        if (watchedEpisode != null)
                        {
                            TraktLogger.Info("Marking episode as unwatched in local database, episode is not watched on trakt.tv. Title = '{0}', Year = '{1}', Season = '{2}', Episode = '{3}', Show TVDb ID = '{4}', Show IMDb ID = '{5}'",
                                episode.ShowTitle, episode.ShowYear.HasValue ? episode.ShowYear.ToString() : "<empty>", episode.Season, episode.Number, episode.ShowTvdbId.HasValue ? episode.ShowTvdbId.ToString() : "<empty>", episode.ShowImdbId ?? "<empty>");

                            watchedEpisode[DBOnlineEpisode.cWatched] = false;
                            watchedEpisode.Commit();

                            // update watched/unwatched counter later
                            seriesToUpdateEpisodeCounts.Add(watchedEpisode[DBOnlineEpisode.cSeriesID]);

                            // update watched episodes
                            localWatchedEpisodes.Remove(watchedEpisode);
                        }
                    }
                }
                #endregion

                #region Mark episodes as watched in local database

                TraktLogger.Info("Start sync of tv episode watched state to local database");
                if (traktWatchedEpisodes != null && traktWatchedEpisodes.Count() > 0)
                {
                    // create a unique key to lookup and search for faster
                    var onlineEpisodes = traktWatchedEpisodes.ToLookup(twe => CreateLookupKey(twe), twe => twe);

                    foreach (var episode in localUnWatchedEpisodes)
                    {
                        string tvdbKey = CreateLookupKey(episode);

                        var traktEpisode = onlineEpisodes[tvdbKey].FirstOrDefault();
                        if (traktEpisode != null)
                        {
                            TraktLogger.Info("Marking episode as watched in local database, episode is watched on trakt.tv. Plays = '{0}', Title = '{1}', Year = '{2}', Season = '{3}', Episode = '{4}', Show TVDb ID = '{5}', Show IMDb ID = '{6}'",
                                traktEpisode.Plays, traktEpisode.ShowTitle, traktEpisode.ShowYear.HasValue ? traktEpisode.ShowYear.ToString() : "<empty>", traktEpisode.Season, traktEpisode.Number, traktEpisode.ShowTvdbId.HasValue ? traktEpisode.ShowTvdbId.ToString() : "<empty>", traktEpisode.ShowImdbId ?? "<empty>");

                            episode[DBOnlineEpisode.cWatched] = true;
                            episode[DBOnlineEpisode.cPlayCount] = traktEpisode.Plays;

                            if (string.IsNullOrEmpty(episode["LastWatchedDate"]))
                                episode["LastWatchedDate"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                            if (string.IsNullOrEmpty(episode["FirstWatchedDate"]))
                                episode["FirstWatchedDate"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                            if (!string.IsNullOrEmpty(episode[DBEpisode.cFilename]) && string.IsNullOrEmpty(episode[DBEpisode.cDateWatched]))
                                episode[DBEpisode.cDateWatched] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                            episode.Commit();

                            // update watched/unwatched counter later
                            seriesToUpdateEpisodeCounts.Add(episode[DBOnlineEpisode.cSeriesID]);
                        }
                    }
                }

                #endregion

                #region Rate episodes in local database

                if (TraktSettings.SyncRatings)
                {
                    #region Episodes
                    TraktLogger.Info("Start sync of tv episode ratings to local database");
                    if (traktRatedEpisodes != null && traktRatedEpisodes.Count() > 0)
                    {
                        // create a unique key to lookup and search for faster
                        var onlineEpisodes = traktRatedEpisodes.ToLookup(tre => CreateLookupKey(tre), tre => tre);

                        foreach (var episode in localNonRatedEpisodes)
                        {
                            string tvdbKey = CreateLookupKey(episode);

                            var traktEpisode = onlineEpisodes[tvdbKey].FirstOrDefault();
                            if (traktEpisode != null)
                            {
                                // update local collection rating
                                TraktLogger.Info("Inserting rating for tv episode in local database, episode is rated on trakt.tv. Rating = '{0}/10', Title = '{1}', Year = '{2}' Season = '{3}', Episode = '{4}', Show TVDb ID = '{5}', Show IMDb ID = '{6}', Episode TVDb ID = '{7}'",
                                    traktEpisode.Rating, traktEpisode.Show.Title, traktEpisode.Show.Year.HasValue ? traktEpisode.Show.Year.ToString() : "<empty>", traktEpisode.Episode.Season, traktEpisode.Episode.Number, traktEpisode.Show.Ids.Tvdb.HasValue ? traktEpisode.Show.Ids.Tvdb.ToString() : "<empty>", traktEpisode.Show.Ids.Imdb ?? "<empty>", traktEpisode.Episode.Ids.Tvdb.HasValue ? traktEpisode.Episode.Ids.Tvdb.ToString() : "<empty>");

                                // we could potentially use the RatedAt date to insert a DateWatched if empty or less than
                                episode[DBOnlineEpisode.cMyRating] = traktEpisode.Rating;
                                episode.Commit();
                            }
                        }
                    }
                    #endregion

                    #region Shows
                    TraktLogger.Info("Start sync of tv show ratings to local database");
                    if (traktRatedShows != null && traktRatedShows.Count() > 0)
                    {
                        foreach (var show in localNonRatedShows) 
                        {
                            if (IgnoredSeries.Exists(tvdbid => tvdbid == show[DBSeries.cID]))
                                continue;

                            // if we have the episode unrated, rate it
                            var traktShow = traktRatedShows.FirstOrDefault(trs => ShowMatch(show, trs.Show));
                            if (traktShow == null)
                                continue;

                            // update local collection rating
                            TraktLogger.Info("Inserting rating for tv show in local database, show is rated on trakt.tv. Rating = '{0}/10', Title = '{1}', Year = '{1}', Show TVDb ID = '{2}'",
                                traktShow.Rating, traktShow.Show.Title, traktShow.Show.Year.HasValue ? traktShow.Show.Year.ToString() : "<empty>" , traktShow.Show.Ids.Tvdb.HasValue ? traktShow.Show.Ids.Tvdb.ToString() : "<empty>");

                            show[DBOnlineSeries.cMyRating] = traktShow.Rating;
                            show.Commit();
                        }
                    }
                    #endregion
                }

                #endregion

                #region Add episodes to watched history at trakt.tv
                int showCount = 0;
                int iSyncCounter = 0;
                if (traktWatchedEpisodes != null)
                {
                    var syncWatchedShows = GetWatchedShowsForSyncEx(localWatchedEpisodes, traktWatchedEpisodes);

                    TraktLogger.Info("Found {0} local tv show(s) with {1} watched episode(s) to add to trakt.tv watched history", syncWatchedShows.Shows.Count, syncWatchedShows.Shows.Sum(sh => sh.Seasons.Sum(se => se.Episodes.Count())));

                    showCount = syncWatchedShows.Shows.Count;
                    foreach (var show in syncWatchedShows.Shows)
                    {
                        int showEpisodeCount = show.Seasons.Sum(s => s.Episodes.Count());
                        TraktLogger.Info("Adding tv show [{0}/{1}] to trakt.tv episode watched history, Episode Count = '{2}', Show Title = '{3}', Show Year = '{4}', Show TVDb ID = '{5}', Show IMDb ID = '{6}'",
                                            ++iSyncCounter, showCount, showEpisodeCount, show.Title, show.Year.HasValue ? show.Year.ToString() : "<empty>", show.Ids.Tvdb, show.Ids.Imdb ?? "<empty>");

                        show.Seasons.ForEach(s => s.Episodes.ForEach(e =>
                        {
                            TraktLogger.Info("Adding episode to trakt.tv watched history, Title = '{0} - {1}x{2}', Watched At = '{3}'", show.Title, s.Number, e.Number, e.WatchedAt.ToLogString());
                        }));

                        // only sync one show at a time regardless of batch size in settings
                        var pagedShows = new List<TraktSyncShowWatchedEx>();
                        pagedShows.Add(show);
                
                        // update local cache
                        TraktCache.AddEpisodesToWatchHistory(show);

                        var response = TraktAPI.TraktAPI.AddShowsToWatchedHistoryEx(new TraktSyncShowsWatchedEx { Shows = pagedShows });
                        TraktLogger.LogTraktResponse<TraktSyncResponse>(response);
                    }
                }
                #endregion

                #region Add episodes to collection at trakt.tv
                if (traktCollectedEpisodes != null)
                {
                    var syncCollectedShows = GetCollectedShowsForSyncEx(localCollectedEpisodes, traktCollectedEpisodes);

                    TraktLogger.Info("Found {0} local tv show(s) with {1} collected episode(s) to add to trakt.tv collection", syncCollectedShows.Shows.Count, syncCollectedShows.Shows.Sum(sh => sh.Seasons.Sum(se => se.Episodes.Count())));

                    iSyncCounter = 0;
                    showCount = syncCollectedShows.Shows.Count;
                    foreach (var show in syncCollectedShows.Shows)
                    {
                        int showEpisodeCount = show.Seasons.Sum(s => s.Episodes.Count());
                        TraktLogger.Info("Adding tv show [{0}/{1}] to trakt.tv episode collection, Episode Count = '{2}', Show Title = '{3}', Show Year = '{4}', Show TVDb ID = '{5}', Show IMDb ID = '{6}'",
                                            ++iSyncCounter, showCount, showEpisodeCount, show.Title, show.Year.HasValue ? show.Year.ToString() : "<empty>", show.Ids.Tvdb, show.Ids.Imdb ?? "<empty>");

                        show.Seasons.ForEach(s => s.Episodes.ForEach(e =>
                        {
                            TraktLogger.Info("Adding episode to trakt.tv collection, Title = '{0} - {1}x{2}', Collected At = '{3}', Audio Channels = '{4}', Audio Codec = '{5}', Resolution = '{6}', Media Type = '{7}', Is 3D = '{8}'", show.Title, s.Number, e.Number, e.CollectedAt.ToLogString(), e.AudioChannels.ToLogString(), e.AudioCodec.ToLogString(), e.Resolution.ToLogString(), e.MediaType.ToLogString(), e.Is3D);
                        }));

                        // only sync one show at a time regardless of batch size in settings
                        var pagedShows = new List<TraktSyncShowCollectedEx>();
                        pagedShows.Add(show);

                        // update local cache
                        TraktCache.AddEpisodesToCollection(show);

                        var response = TraktAPI.TraktAPI.AddShowsToCollectonEx(new TraktSyncShowsCollectedEx { Shows = pagedShows });
                        TraktLogger.LogTraktResponse<TraktSyncResponse>(response);
                    }
                }
                #endregion

                #region Add episode/show ratings to trakt.tv

                if (TraktSettings.SyncRatings)
                {
                    #region Episodes
                    if (traktRatedEpisodes != null)
                    {
                        var syncRatedShowsEx = GetRatedEpisodesForSyncEx(localRatedEpisodes, traktRatedEpisodes);

                        TraktLogger.Info("Found {0} local tv show(s) with {1} rated episode(s) to add to trakt.tv ratings", syncRatedShowsEx.Shows.Count, syncRatedShowsEx.Shows.Sum(sh => sh.Seasons.Sum(se => se.Episodes.Count())));

                        iSyncCounter = 0;
                        showCount = syncRatedShowsEx.Shows.Count;
                        foreach (var show in syncRatedShowsEx.Shows)
                        {
                            int showEpisodeCount = show.Seasons.Sum(s => s.Episodes.Count());
                            TraktLogger.Info("Adding tv show [{0}/{1}] to trakt.tv episode ratings, Episode Count = '{2}', Show Title = '{3}', Show Year = '{4}', Show TVDb ID = '{5}', Show IMDb ID = '{6}'",
                                                ++iSyncCounter, showCount, showEpisodeCount, show.Title, show.Year.HasValue ? show.Year.ToString() : "<empty>", show.Ids.Tvdb, show.Ids.Imdb ?? "<empty>");

                            show.Seasons.ForEach(s => s.Episodes.ForEach(e =>
                            {
                                TraktLogger.Info("Adding episode to trakt.tv ratings, Title = '{0} - {1}x{2}', Rating = '{3}', Rated At = '{4}'", show.Title, s.Number, e.Number, e.Rating, e.RatedAt.ToLogString());
                            }));

                            // only sync one show at a time regardless of batch size in settings
                            var pagedShows = new List<TraktSyncShowRatedEx>();
                            pagedShows.Add(show);

                            // update local cache
                            TraktCache.AddEpisodesToRatings(show);

                            var response = TraktAPI.TraktAPI.AddShowsToRatingsEx(new TraktSyncShowsRatedEx { Shows = pagedShows });
                            TraktLogger.LogTraktResponse<TraktSyncResponse>(response);
                        }
                    }
                    #endregion

                    #region Shows
                    if (traktRatedShows != null)
                    {
                        var syncRatedShows = new List<TraktSyncShowRated>();
                        TraktLogger.Info("Finding local tv shows to add to trakt.tv ratings");

                        syncRatedShows = (from show in localRatedShows
                                          where !traktRatedShows.ToList().Exists(trs => ShowMatch(show, trs.Show))
                                          select new TraktSyncShowRated
                                          {
                                              Ids = new TraktShowId
                                              {
                                                  Tvdb = show[DBSeries.cID],
                                                  Imdb = BasicHandler.GetProperImdbId(show[DBOnlineSeries.cIMDBID])
                                              },
                                              Title = show[DBOnlineSeries.cOriginalName],
                                              Year = show.Year.ToNullableInt32(),
                                              Rating = show[DBOnlineSeries.cMyRating],
                                              RatedAt = DateTime.UtcNow.ToISO8601(),
                                          }).ToList();

                        TraktLogger.Info("Found {0} local tv show(s) rated to add to trakt.tv ratings", syncRatedShows.Count);

                        if (syncRatedShows.Count > 0)
                        {
                            // update local cache
                            TraktCache.AddShowsToRatings(syncRatedShows);

                            int pageSize = TraktSettings.SyncBatchSize;
                            int pages = (int)Math.Ceiling((double)syncRatedShows.Count / pageSize);
                            for (int i = 0; i < pages; i++)
                            {
                                TraktLogger.Info("Adding tv shows [{0}/{1}] to trakt.tv ratings", i + 1, pages);

                                var pagedShows = syncRatedShows.Skip(i * pageSize).Take(pageSize).ToList();

                                pagedShows.ForEach(s =>
                                {
                                    TraktLogger.Info("Adding tv show to trakt.tv ratings, Title = '{0}', Year = '{1}', TVDb ID = '{2}', IMDb ID = '{3}', Rating = '{4}', Rated At = '{5}'", s.Title, s.Year.ToLogString(), s.Ids.Tvdb.ToLogString(), s.Ids.Imdb.ToLogString(), s.Rating, s.RatedAt.ToLogString());
                                });

                                var response = TraktAPI.TraktAPI.AddShowsToRatings(new TraktSyncShowsRated { Shows = pagedShows });
                                TraktLogger.LogTraktResponse(response);
                            }
                        }
                    }
                    #endregion
                }
                
                #endregion

                #region Remove episodes no longer in collection from trakt.tv

                if (TraktSettings.KeepTraktLibraryClean && TraktSettings.TvShowPluginCount == 1 && traktCollectedEpisodes != null)
                {
                    var syncRemovedShows = GetRemovedShowsForSyncEx(localCollectedEpisodes, traktCollectedEpisodes);

                    TraktLogger.Info("Found {0} local tv show(s) with {1} episode(s) to remove from trakt.tv collection", syncRemovedShows.Shows.Count, syncRemovedShows.Shows.Sum(sh => sh.Seasons.Sum(se => se.Episodes.Count())));

                    iSyncCounter = 0;
                    showCount = syncRemovedShows.Shows.Count;
                    foreach (var show in syncRemovedShows.Shows)
                    {
                        int showEpisodeCount = show.Seasons.Sum(s => s.Episodes.Count());
                        TraktLogger.Info("Removing tv show [{0}/{1}] from trakt.tv episode collection, Episode Count = '{2}', Show Title = '{3}', Show Year = '{4}', Show TVDb ID = '{5}', Show IMDb ID = '{6}'",
                                            ++iSyncCounter, showCount, showEpisodeCount, show.Title, show.Year.HasValue ? show.Year.ToString() : "<empty>", show.Ids.Tvdb, show.Ids.Imdb ?? "<empty>");

                        show.Seasons.ForEach(s => s.Episodes.ForEach(e =>
                        {
                            TraktLogger.Info("Removing episode from trakt.tv collection, Title = '{0} - {1}x{2}'", show.Title, s.Number, e.Number);
                        }));

                        // only sync one show at a time regardless of batch size in settings
                        var pagedShows = new List<TraktSyncShowEx>();
                        pagedShows.Add(show);

                        // update local cache
                        TraktCache.RemoveEpisodesFromCollection(show);

                        var response = TraktAPI.TraktAPI.RemoveShowsFromCollectonEx(new TraktSyncShowsEx { Shows = pagedShows });
                        TraktLogger.LogTraktResponse<TraktSyncResponse>(response);
                    }
                }

                #endregion

                #region Update episode counts in local database
                foreach (int seriesID in seriesToUpdateEpisodeCounts)
                {
                    var series = Helper.getCorrespondingSeries(seriesID);
                    if (series == null) continue;

                    TraktLogger.Info("Updating episode counts in local database for series. Title = '{0}', Year = '{1}', Show TVDb ID = '{2}'", series.ToString(), series.Year ?? "<empty>", series[DBSeries.cID]);
                    DBSeries.UpdateEpisodeCounts(series);
                }
                #endregion
            }

            SyncLibraryInProgress = false;
            TraktLogger.Info("MP-TVSeries Library Sync Completed");
        }

        public bool Scrobble(string filename)
        {
            if (!EpisodeWatching) return false;
           
            FirstEpisodeWatched = false;

            var scrobbleThread = new Thread((episodeObj) =>
            {
                var scrobbleEpisode = episodeObj as DBEpisode;
                if (scrobbleEpisode == null) return;

                var show = Helper.getCorrespondingSeries(scrobbleEpisode[DBEpisode.cSeriesID]);
                if (show == null || show[DBOnlineSeries.cTraktIgnore]) return;

                // get the current player progress
                double progress = GetPlayerProgress(scrobbleEpisode);

                // check if it's a double episode and handle accordingly based on start time
                TraktScrobbleEpisode scrobbleData = null;
                if (scrobbleEpisode.IsDoubleEpisode)
                {
                    // get both episodes from filename query
                    var condition = new SQLCondition();
                    condition.Add(new DBEpisode(), DBEpisode.cFilename, scrobbleEpisode[DBEpisode.cFilename], SQLConditionType.Equal);
                    var episodes = DBEpisode.Get(condition, false);
                    if (episodes == null || episodes.Count != 2)
                    {
                        TraktLogger.Error("Unable to retrieve double episode information from tvseries database for current playing episode. Title = '{0}'", scrobbleEpisode.ToString());
                        return;
                    }

                    // store the second episode so we can use seperately
                    SecondEpisode = episodes[1];

                    // if we're already past the half way mark scrobble the second part only
                    if (progress > 50)
                    {
                        // don't scrobble the first part when we stop
                        FirstEpisodeWatched = true;

                        TraktLogger.Info("Sending start scrobble of second part of episode to trakt.tv. Show Title = '{0}', Season = '{1}', Episode = '{2}', Episode Title = '{3}', Show TVDb ID = '{4}', Episode TVDb ID = '{5}'",
                                    show[DBOnlineSeries.cOriginalName], episodes[1][DBOnlineEpisode.cSeasonIndex], episodes[1][DBOnlineEpisode.cEpisodeIndex], episodes[1][DBOnlineEpisode.cEpisodeName], episodes[1][DBOnlineEpisode.cSeriesID], episodes[1][DBOnlineEpisode.cID]);

                        scrobbleData = CreateScrobbleData(episodes[1], progress);
                        if (scrobbleData == null) return;

                        var response = TraktAPI.TraktAPI.StartEpisodeScrobble(scrobbleData);
                        TraktLogger.LogTraktResponse(response);

                        return;
                    }
                }

                TraktLogger.Info("Sending start scrobble of episode to trakt.tv. Show Title = '{0}', Season = '{1}', Episode = '{2}', Episode Title = '{3}', Show TVDb ID = '{4}', Episode TVDb ID = '{5}'",
                                    show[DBOnlineSeries.cOriginalName], scrobbleEpisode[DBOnlineEpisode.cSeasonIndex], scrobbleEpisode[DBOnlineEpisode.cEpisodeIndex], scrobbleEpisode[DBOnlineEpisode.cEpisodeName], scrobbleEpisode[DBOnlineEpisode.cSeriesID], scrobbleEpisode[DBOnlineEpisode.cID]);

                scrobbleData = CreateScrobbleData(scrobbleEpisode, progress);
                if (scrobbleData == null) return;

                TraktLogger.LogTraktResponse(TraktAPI.TraktAPI.StartEpisodeScrobble(scrobbleData));
            })
            {
                Name = "Scrobble",
                IsBackground = true
            };

            scrobbleThread.Start(CurrentEpisode);

            return true;
        }

        public void StopScrobble()
        {
            return;
        }

        public void SyncProgress()
        {
            if (!TraktSettings.SyncPlayback || SyncPlaybackInProgress)
                return;

            SyncPlaybackInProgress = true;

            TraktLogger.Info("MP-TVSeries Starting Playback Sync");

            // get playback data from trakt
            var playbackData = TraktCache.PlaybackData;
            if (playbackData == null)
            {
                TraktLogger.Warning("Failed to get plackback data from trakt.tv");
                SyncPlaybackInProgress = false;
                return;
            }

            TraktLogger.Info("Found {0} tv episodes on trakt.tv with resume data", playbackData.Where(p => p.Type == "episode").Count());

            foreach (var item in playbackData.Where(p => p.Type == "episode"))
            {
                if (!item.Show.Ids.Tvdb.HasValue || item.Show.Ids.Tvdb <= 0)
                {
                    TraktLogger.Warning("Skipping item with invalid TVDb ID, TV Show = '{0}', Season='{1}', Episode='{2}'", item.Show.Title, item.Episode.Season, item.Episode.Number);
                    continue;
                }

                // get episode from local database if it exists
                var episode = DBEpisode.Get(item.Show.Ids.Tvdb.Value, item.Episode.Season, item.Episode.Number);
                if (episode == null || string.IsNullOrEmpty(episode[DBEpisode.cFilename]))
                    continue;

                // if the local playtime is not known then skip
                if (episode[DBEpisode.cLocalPlaytime] <= 0)
                {
                    TraktLogger.Warning("Skipping item with invalid runtime in database, TV Show = '{0}', Season='{1}', Episode='{2}'", item.Show.Title, item.Episode.Season, item.Episode.Number);
                    continue;
                }

                // update the stop time based on percentage watched
                // tvseries stores localplaytime in milliseconds and stoptime in secs
                var resumeData = Convert.ToInt32((episode[DBEpisode.cLocalPlaytime] / 1000.0) * (item.Progress / 100.0)) - TraktSettings.SyncResumeDelta;
                if (resumeData < 0) resumeData = 0;

                if (episode[DBEpisode.cStopTime] < resumeData)
                {
                    TraktLogger.Info("Setting resume time '{0}' for episode, Title = '{1} - {2}x{3}'", new TimeSpan(0, 0, 0, resumeData), item.Show.Title, item.Episode.Season, item.Episode.Number);
                    episode[DBEpisode.cStopTime] = resumeData;
                    episode.Commit();
                }
            }

            TraktLogger.Info("MP-TVSeries Playback Sync Completed");
            SyncPlaybackInProgress = false;
            return;
        }

        #endregion

        /// <summary>
        /// Stores a list of Series ignored by user for scrobble / sync
        /// </summary>
        public static List<int?> IgnoredSeries
        {
            get
            {
                if (_IgnoredSeries == null)
                {
                    var ignoredSeries = DBSeries.Get(new SQLCondition(new DBOnlineSeries(), DBOnlineSeries.cTraktIgnore, true, SQLConditionType.Equal));
                    if (ignoredSeries == null)
                    {
                        // return a empty list
                        _IgnoredSeries = new List<int?>();
                    }
                    else
                    {
                        _IgnoredSeries = ignoredSeries.Select(s => (int?)s[DBSeries.cID]).ToList();
                    }
                }
                return _IgnoredSeries;
            }
        }
        static List<int?> _IgnoredSeries = null;

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
        public static bool GetEpisodeInfo(Object obj, out string title, out string year, out string showTvdbId, out string epTvdbId, out string seasonidx, out string episodeidx, out bool isWatched)
        {
            title = string.Empty;
            year = string.Empty;
            showTvdbId = string.Empty;
            epTvdbId = string.Empty;
            seasonidx = string.Empty;
            episodeidx = string.Empty;
            isWatched = false;

            if (obj == null) return false;

            var episode = obj as DBEpisode;
            if (episode == null) return false;

            var series = Helper.getCorrespondingSeries(episode[DBOnlineEpisode.cSeriesID]);
            if (series == null) return false;

            title = series[DBOnlineSeries.cOriginalName];
            year = series.Year;
            showTvdbId = series[DBSeries.cID];
            epTvdbId = episode[DBOnlineEpisode.cID];
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
        public static bool GetSeriesInfo(Object obj, out string title, out string year, out string tvdb)
        {
            title = string.Empty;
            year = string.Empty;
            tvdb = string.Empty;
       
            if (obj == null) return false;

            var series = obj as DBSeries;
            if (series == null) return false;

            title = series[DBOnlineSeries.cOriginalName];
            year = series.Year;
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

        /// <summary>
        /// Checks if the series id exists in the local collection
        /// </summary>
        public static bool SeriesExists(int seriesId)
        {
            var series = Helper.getCorrespondingSeries(seriesId);
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
        /// Returns a list of episodes for watched history sync (uses episode ids)
        /// This may not be ideal as it depends on trakt knowing the episode tvdb ids since there is no show data to fallback on.
        /// </summary>        
        private List<TraktSyncEpisodeWatched> GetWatchedEpisodesForSync(List<DBEpisode> localWatchedEpisodes, List<TraktCache.EpisodeWatched> traktWatchedEpisodes)
        {
            TraktLogger.Info("Finding local episodes to add to trakt.tv watched history");

            var syncWatchedEpisodes = new List<TraktSyncEpisodeWatched>();
            syncWatchedEpisodes.AddRange(from episode in localWatchedEpisodes
                                         where !traktWatchedEpisodes.Any(twe => EpisodeMatch(episode, twe))
                                         select new TraktSyncEpisodeWatched
                                         {
                                             Ids = new TraktEpisodeId
                                             {
                                                 Tvdb = episode[DBOnlineEpisode.cID],
                                                 Imdb = BasicHandler.GetProperImdbId(episode[DBOnlineEpisode.cIMDBID])
                                             },
                                             Number = episode[DBOnlineEpisode.cEpisodeIndex],
                                             Season = episode[DBOnlineEpisode.cSeasonIndex],
                                             Title = episode[DBOnlineEpisode.cEpisodeName],
                                             WatchedAt = GetLastPlayedDate(episode)
                                         });

            return syncWatchedEpisodes;
        }

        /// <summary>
        /// Returns a list of shows for watched history sync as show objects with season / episode hierarchy
        /// </summary>
        private TraktSyncShowsWatchedEx GetWatchedShowsForSyncEx(List<DBEpisode> localWatchedEpisodes, List<TraktCache.EpisodeWatched> traktEpisodesWatched)
        {
            TraktLogger.Info("Finding local episodes to add to trakt.tv watched history");

            // prepare new sync object
            var syncWatchedEpisodes = new TraktSyncShowsWatchedEx();
            syncWatchedEpisodes.Shows = new List<TraktSyncShowWatchedEx>();
           
            // filter out any invalid episodes by user
            var episodes = localWatchedEpisodes.Where(lwe => lwe[DBOnlineEpisode.cEpisodeIndex] != "" && 
                                                             lwe[DBOnlineEpisode.cEpisodeIndex] != "0").ToList();

            // create a unique key to lookup and search for faster
            var onlineEpisodes = traktEpisodesWatched.ToLookup(twe => CreateLookupKey(twe), twe => twe);

            foreach (var episode in episodes)
            {
                string tvdbKey = CreateLookupKey(episode);

                var traktEpisode = onlineEpisodes[tvdbKey].FirstOrDefault();

                // check if not watched on trakt and add it to sync list
                if (traktEpisode == null)
                {
                    // check if we already have the show added to our sync object
                    var syncShow = syncWatchedEpisodes.Shows.FirstOrDefault(swe => swe.Ids != null && swe.Ids.Tvdb == episode[DBOnlineEpisode.cSeriesID]);
                    if (syncShow == null)
                    {
                        // get show data from episode
                        var show = Helper.getCorrespondingSeries(episode[DBOnlineEpisode.cSeriesID]);
                        if (show == null) continue;

                        // create new show
                        syncShow = new TraktSyncShowWatchedEx
                        {
                            Ids = new TraktShowId
                            {
                                Tvdb = show[DBSeries.cID],
                                Imdb = BasicHandler.GetProperImdbId(show[DBOnlineSeries.cIMDBID])
                            },
                            Title = show[DBOnlineSeries.cOriginalName],
                            Year = show.Year.ToNullableInt32()
                        };
                        
                        // add a new season collection to show object
                        syncShow.Seasons = new List<TraktSyncShowWatchedEx.Season>();

                        // add show to the collection
                        syncWatchedEpisodes.Shows.Add(syncShow);
                    }

                    // check if season exists in show sync object
                    var syncSeason = syncShow.Seasons.FirstOrDefault(ss => ss.Number == episode[DBOnlineEpisode.cSeasonIndex]);
                    if (syncSeason == null)
                    {
                        // create new season
                        syncSeason = new TraktSyncShowWatchedEx.Season
                        {
                            Number = episode[DBOnlineEpisode.cSeasonIndex]
                        };

                        // add a new episode collection to season object
                        syncSeason.Episodes = new List<TraktSyncShowWatchedEx.Season.Episode>();

                        // add season to the show
                        syncShow.Seasons.Add(syncSeason);
                    }

                    // add episode to season
                    syncSeason.Episodes.Add(new TraktSyncShowWatchedEx.Season.Episode
                    {
                        Number = episode[DBOnlineEpisode.cEpisodeIndex],
                        WatchedAt = GetLastPlayedDate(episode)
                    });
                }
            }

            return syncWatchedEpisodes;
        }

        /// <summary>
        /// Returns a list of episodes for collection sync (uses episode ids)
        /// This may not be ideal as it depends on trakt knowing the episode tvdb ids since there is no show data to fallback on.
        /// </summary>        
        private List<TraktSyncEpisodeCollected> GetCollectedEpisodesForSync(List<DBEpisode> localCollectedEpisodes, List<TraktCache.EpisodeCollected> traktCollectedEpisodes)
        {
            TraktLogger.Info("Finding local episodes to add to trakt.tv collection");

            var syncCollectedEpisodes = new List<TraktSyncEpisodeCollected>();
            syncCollectedEpisodes.AddRange(from episode in localCollectedEpisodes
                                           where !traktCollectedEpisodes.Any(tce => EpisodeMatch(episode, tce))
                                           select new TraktSyncEpisodeCollected
                                           {
                                               Ids = new TraktEpisodeId
                                               {
                                                   Tvdb = episode[DBOnlineEpisode.cID],
                                                   Imdb = BasicHandler.GetProperImdbId(episode[DBOnlineEpisode.cIMDBID])
                                               },
                                               Number = episode[DBOnlineEpisode.cEpisodeIndex],
                                               Season = episode[DBOnlineEpisode.cSeasonIndex],
                                               Title = episode[DBOnlineEpisode.cEpisodeName],
                                               CollectedAt = episode[DBEpisode.cFileDateAdded].ToString().ToISO8601(0, true),
                                               MediaType = GetEpisodeMediaType(episode),
                                               Resolution = GetEpisodeResolution(episode),
                                               AudioCodec = GetEpisodeAudioCodec(episode),
                                               AudioChannels = GetEpisodeAudioChannels(episode),
                                               Is3D = false
                                           });

            return syncCollectedEpisodes;
        }

        /// <summary>
        /// Returns a list of shows for collection sync as show objects with season / episode hierarchy
        /// </summary>
        private TraktSyncShowsCollectedEx GetCollectedShowsForSyncEx(List<DBEpisode> localCollectedEpisodes, List<TraktCache.EpisodeCollected> traktEpisodesCollected)
        {
            TraktLogger.Info("Finding local episodes to add to trakt.tv collection");

            // prepare new sync object
            var syncCollectedEpisodes = new TraktSyncShowsCollectedEx();
            syncCollectedEpisodes.Shows = new List<TraktSyncShowCollectedEx>();

            // filter out any invalid episodes or ignored series by user
            var episodes = localCollectedEpisodes.Where(lce => lce[DBOnlineEpisode.cEpisodeIndex] != "" &&
                                                               lce[DBOnlineEpisode.cEpisodeIndex] != "0").ToList();

            // create a unique key to lookup and search for faster
            var onlineEpisodes = traktEpisodesCollected.ToLookup(tce => CreateLookupKey(tce), tce => tce);

            foreach (var episode in episodes)
            {
                string tvdbKey = CreateLookupKey(episode);

                var traktEpisode = onlineEpisodes[tvdbKey].FirstOrDefault();

                // check if not collected on trakt and add it to sync list
                if (traktEpisode == null)
                {
                    // check if we already have the show added to our sync object
                    var syncShow = syncCollectedEpisodes.Shows.FirstOrDefault(sce => sce.Ids != null && sce.Ids.Tvdb == episode[DBOnlineEpisode.cSeriesID]);
                    if (syncShow == null)
                    {
                        // get show data from episode
                        var show = Helper.getCorrespondingSeries(episode[DBOnlineEpisode.cSeriesID]);
                        if (show == null) continue;

                        // create new show
                        syncShow = new TraktSyncShowCollectedEx
                        {
                            Ids = new TraktShowId
                            {
                                Tvdb = show[DBSeries.cID],
                                Imdb = BasicHandler.GetProperImdbId(show[DBOnlineSeries.cIMDBID])
                            },
                            Title = show[DBOnlineSeries.cOriginalName],
                            Year = show.Year.ToNullableInt32()
                        };

                        // add a new season collection to show object
                        syncShow.Seasons = new List<TraktSyncShowCollectedEx.Season>();

                        // add show to the collection
                        syncCollectedEpisodes.Shows.Add(syncShow);
                    }

                    // check if season exists in show sync object
                    var syncSeason = syncShow.Seasons.FirstOrDefault(ss => ss.Number == episode[DBOnlineEpisode.cSeasonIndex]);
                    if (syncSeason == null)
                    {
                        // create new season
                        syncSeason = new TraktSyncShowCollectedEx.Season
                        {
                            Number = episode[DBOnlineEpisode.cSeasonIndex]
                        };

                        // add a new episode collection to season object
                        syncSeason.Episodes = new List<TraktSyncShowCollectedEx.Season.Episode>();

                        // add season to the show
                        syncShow.Seasons.Add(syncSeason);
                    }

                    // add episode to season
                    syncSeason.Episodes.Add(new TraktSyncShowCollectedEx.Season.Episode
                    {
                        Number = episode[DBOnlineEpisode.cEpisodeIndex],
                        CollectedAt = episode[DBEpisode.cFileDateAdded].ToString().ToISO8601(0, true),
                        MediaType = GetEpisodeMediaType(episode),
                        Resolution = GetEpisodeResolution(episode),
                        AudioCodec = GetEpisodeAudioCodec(episode),
                        AudioChannels = GetEpisodeAudioChannels(episode),
                        Is3D = false
                    });
                }
            }

            return syncCollectedEpisodes;
        }

        /// <summary>
        /// Returns a list of episodes for ratings sync (uses episode ids)
        /// This may not be ideal as it depends on trakt knowing the episode tvdb ids since there is no show data to fallback on.
        /// </summary>        
        private List<TraktSyncEpisodeRated> GetRatedEpisodesForSync(List<DBEpisode> localRatedEpisodes, List<TraktEpisodeRated> traktRatedEpisodes)
        {
            TraktLogger.Info("Finding local episodes to add to trakt.tv ratings");

            var syncRatedEpisodes = new List<TraktSyncEpisodeRated>();
            syncRatedEpisodes.AddRange(from episode in localRatedEpisodes
                                       where !traktRatedEpisodes.Any(tre => EpisodeMatch(episode, tre.Episode, tre.Show))
                                       select new TraktSyncEpisodeRated
                                       {
                                           Ids = new TraktEpisodeId
                                           {
                                               Tvdb = episode[DBOnlineEpisode.cID],
                                               Imdb = BasicHandler.GetProperImdbId(episode[DBOnlineEpisode.cIMDBID])
                                           },
                                           Number = episode[DBOnlineEpisode.cEpisodeIndex],
                                           Season = episode[DBOnlineEpisode.cSeasonIndex],
                                           Title = episode[DBOnlineEpisode.cEpisodeName],
                                           Rating = episode[DBOnlineEpisode.cMyRating],
                                           RatedAt = DateTime.UtcNow.ToISO8601(),
                                       });

            return syncRatedEpisodes;
        }

        /// <summary>
        /// Returns a list of shows for rating sync as show objects with season / episode hierarchy
        /// </summary>
        private TraktSyncShowsRatedEx GetRatedEpisodesForSyncEx(List<DBEpisode> localRatedEpisodes, List<TraktEpisodeRated> traktEpisodesRated)
        {
            TraktLogger.Info("Finding local episodes to add to trakt.tv ratings");

            // prepare new sync object
            var syncRatedEpisodes = new TraktSyncShowsRatedEx();
            syncRatedEpisodes.Shows = new List<TraktSyncShowRatedEx>();

            // filter out any invalid episodes or ignored series by user
            var episodes = localRatedEpisodes.Where(lre => lre[DBOnlineEpisode.cEpisodeIndex] != "" &&
                                                           lre[DBOnlineEpisode.cEpisodeIndex] != "0").ToList();

            // create a unique key to lookup and search for faster
            var onlineEpisodes = traktEpisodesRated.ToLookup(tre => CreateLookupKey(tre), tre => tre);

            foreach (var episode in episodes)
            {
                string tvdbKey = CreateLookupKey(episode);

                var traktEpisode = onlineEpisodes[tvdbKey].FirstOrDefault();

                // check if not rated on trakt and add it to sync list
                if (traktEpisode == null)
                {
                    // check if we already have the show added to our sync object
                    var syncShow = syncRatedEpisodes.Shows.FirstOrDefault(sre => sre.Ids != null && sre.Ids.Tvdb == episode[DBOnlineEpisode.cSeriesID]);
                    if (syncShow == null)
                    {
                        // get show data from episode
                        var show = Helper.getCorrespondingSeries(episode[DBOnlineEpisode.cSeriesID]);
                        if (show == null) continue;

                        // create new show
                        syncShow = new TraktSyncShowRatedEx
                        {
                            Ids = new TraktShowId
                            {
                                Tvdb = show[DBSeries.cID],
                                Imdb = BasicHandler.GetProperImdbId(show[DBOnlineSeries.cIMDBID])
                            },
                            Title = show[DBOnlineSeries.cOriginalName],
                            Year = show.Year.ToNullableInt32()
                        };

                        // add a new season collection to show object
                        syncShow.Seasons = new List<TraktSyncShowRatedEx.Season>();

                        // add show to the collection
                        syncRatedEpisodes.Shows.Add(syncShow);
                    }

                    // check if season exists in show sync object
                    var syncSeason = syncShow.Seasons.FirstOrDefault(ss => ss.Number == episode[DBOnlineEpisode.cSeasonIndex]);
                    if (syncSeason == null)
                    {
                        // create new season
                        syncSeason = new TraktSyncShowRatedEx.Season
                        {
                            Number = episode[DBOnlineEpisode.cSeasonIndex]
                        };

                        // add a new episode collection to season object
                        syncSeason.Episodes = new List<TraktSyncShowRatedEx.Season.Episode>();

                        // add season to the show
                        syncShow.Seasons.Add(syncSeason);
                    }

                    // add episode to season
                    syncSeason.Episodes.Add(new TraktSyncShowRatedEx.Season.Episode
                    {
                        Number = episode[DBOnlineEpisode.cEpisodeIndex],
                        Rating = episode[DBOnlineEpisode.cMyRating],
                        RatedAt = DateTime.UtcNow.ToISO8601()
                    });
                }
            }

            return syncRatedEpisodes;
        }

        // <summary>
        /// Returns a list of episodes for removal sync (uses episode ids)
        /// This may not be ideal as it depends on trakt knowing the episode tvdb ids since there is no show data to fallback on.
        /// </summary>        
        private List<TraktEpisode> GetRemovedEpisodesForSync(List<DBEpisode> localCollectedEpisodes, List<TraktCache.EpisodeCollected> traktCollectedEpisodes)
        {
            TraktLogger.Info("Finding local episodes to remove from trakt.tv collection");

            var syncUnCollectedEpisodes = new List<TraktEpisode>();

            // workout what episodes that are in trakt collection that are not in local collection
            syncUnCollectedEpisodes.AddRange(from episode in traktCollectedEpisodes
                                             where !localCollectedEpisodes.Exists(lce => EpisodeMatch(lce, episode))
                                             select new TraktEpisode
                                             {
                                                 Ids = new TraktEpisodeId
                                                 {
                                                     Tvdb = localCollectedEpisodes.First(lce => EpisodeMatch(lce, episode))[DBOnlineEpisode.cID]
                                                 }
                                             });                                            

            return syncUnCollectedEpisodes;
        }

        /// <summary>
        /// Returns a list of shows for removal sync as show objects with season / episode hierarchy
        /// </summary>
        private TraktSyncShowsEx GetRemovedShowsForSyncEx(List<DBEpisode> localCollectedEpisodes, List<TraktCache.EpisodeCollected> traktEpisodesCollected)
        {
            TraktLogger.Info("Finding local episodes to remove from trakt.tv collection");

            // prepare new sync object
            var syncUnCollectedEpisodes = new TraktSyncShowsEx();
            syncUnCollectedEpisodes.Shows = new List<TraktSyncShowEx>();

            // create a unique key to lookup and search for faster
            var localEpisodes = localCollectedEpisodes.ToLookup(lce => CreateLookupKey(lce), lce => lce);

            foreach (var episode in traktEpisodesCollected)
            {
                string tvdbKey = CreateLookupKey(episode);

                var localEpisode = localEpisodes[tvdbKey].FirstOrDefault();

                // check if not collected locally
                if (localEpisode == null)
                {
                    // check if we already have the show added to our sync object
                    var syncShow = syncUnCollectedEpisodes.Shows.FirstOrDefault(suce => suce.Ids != null && suce.Ids.Trakt == episode.ShowId);
                    if (syncShow == null)
                    {
                        // get show data from episode and create new show
                        syncShow = new TraktSyncShowEx
                        {
                            Ids = new TraktShowId
                            {
                                Trakt = episode.ShowId,
                                Imdb = episode.ShowImdbId.ToNullIfEmpty(),
                                Tvdb = episode.ShowTvdbId
                            },
                            Title = episode.ShowTitle,
                            Year = episode.ShowYear
                        };

                        // add a new season collection to show object
                        syncShow.Seasons = new List<TraktSyncShowEx.Season>();

                        // add show to the collection
                        syncUnCollectedEpisodes.Shows.Add(syncShow);
                    }

                    // check if season exists in show sync object
                    var syncSeason = syncShow.Seasons.FirstOrDefault(ss => ss.Number == episode.Season);
                    if (syncSeason == null)
                    {
                        // create new season
                        syncSeason = new TraktSyncShowEx.Season
                        {
                            Number = episode.Season
                        };

                        // add a new episode collection to season object
                        syncSeason.Episodes = new List<TraktSyncShowEx.Season.Episode>();

                        // add season to the show
                        syncShow.Seasons.Add(syncSeason);
                    }

                    // add episode to season
                    syncSeason.Episodes.Add(new TraktSyncShowEx.Season.Episode
                    {
                        Number = episode.Number
                    });
                }
            }

            return syncUnCollectedEpisodes;
        }

        /// <summary>
        /// Creates Scrobble data based on a DBEpisode object
        /// </summary>
        private TraktScrobbleEpisode CreateScrobbleData(DBEpisode episode, double progress)
        {
            var show = Helper.getCorrespondingSeries(episode[DBOnlineEpisode.cSeriesID]);
            if (show == null || show[DBOnlineSeries.cTraktIgnore]) return null;

            // check if its a valid episode, tvdb is notorious for episode 0's.
            if (episode[DBOnlineEpisode.cEpisodeIndex] < 1)
            {
                TraktLogger.Info("Ignoring scrobble of invalid episode with episode number zero. Title = '{0}'", episode.ToString());
                return null;
            }

            // bad progress reading from g_player
            if (progress > 100) progress = 100;

            var scrobbleData = new TraktScrobbleEpisode
            {
                Episode = new TraktEpisode
                {
                    Ids = new TraktEpisodeId
                    { 
                        Tvdb = episode[DBOnlineEpisode.cID],
                        Imdb = BasicHandler.GetProperImdbId(episode[DBOnlineEpisode.cIMDBID])
                    },
                    Title = episode[DBOnlineEpisode.cEpisodeName],
                    Season = episode[DBOnlineEpisode.cSeasonIndex],
                    Number = episode[DBOnlineEpisode.cEpisodeIndex]
                },
                Show = new TraktShow
                {
                    Ids = new TraktShowId
                    {
                        Tvdb = show[DBSeries.cID],
                        Imdb = BasicHandler.GetProperImdbId(show[DBOnlineSeries.cIMDBID])
                    },
                    Title = show[DBOnlineSeries.cOriginalName],
                    Year = show.Year.ToNullableInt32()
                },
                Progress = Math.Round(progress, 2),
                AppVersion = TraktSettings.Version,
                AppDate = TraktSettings.BuildDate
            };

            return scrobbleData;
        }

        /// <summary>
        /// Get the current g_Player progress of the playing episode
        /// </summary>
        private double GetPlayerProgress(DBEpisode episode)
        {
            // duration in minutes
            double duration = episode[DBEpisode.cLocalPlaytime] / 60000;
            double progress = 0.0;

            // get current progress of player (in seconds) to work out percent complete
            if (duration > 0.0)
                progress = ((g_Player.CurrentPosition / 60.0) / duration) * 100.0;
            
            if (progress > 100.0)
                progress = 100;

            return Math.Round(progress, 2);
        }

        /// <summary>
        /// Gets the trakt compatible string for the episodes Media Type
        /// </summary>
        private string GetEpisodeMediaType(DBEpisode episode)
        {
            // tvseries doesn't really support bluray, dvd etc
            // so just return Digital for now
            return TraktMediaType.digital.ToString();
        }

        /// <summary>
        /// Gets the trakt compatible string for the episodes Resolution
        /// </summary>
        private string GetEpisodeResolution(DBEpisode episode)
        {
            // note: we don't store interlaced flag

            int videoWidth = episode[DBEpisode.cVideoWidth];
            int videoHeight = episode[DBEpisode.cVideoHeight];

            if ((videoWidth <= 3840 && videoWidth > 3000) || videoHeight == 2160)
                return TraktResolution.uhd_4k.ToString();

            if ((videoWidth <= 1920 && videoWidth > 1800) || videoHeight == 1080)
                return TraktResolution.hd_1080p.ToString();

            if ((videoWidth <= 1280 && videoWidth > 1100 ) || videoHeight == 720)
                return TraktResolution.hd_720p.ToString();

            if (videoWidth == 704 || videoHeight == 576)
                return TraktResolution.sd_576p.ToString();

            if (videoWidth == 704 || videoHeight == 480)
                return TraktResolution.sd_480p.ToString();
            
            return null;
        }

        /// <summary>
        /// Gets the trakt compatible string for the episodes Audio
        /// </summary>
        private string GetEpisodeAudioCodec(DBEpisode episode)
        {
            string audioCodec = episode[DBEpisode.cAudioFormat].ToString();

            switch (audioCodec.ToLowerInvariant())
            {
                case "truehd":
                    return TraktAudio.dolby_truehd.ToString();
                case "dts":
                    return TraktAudio.dts.ToString();
                case "dtshd":
                    return TraktAudio.dts_ma.ToString();
                case "ac3":
                case "ac-3":
                    return TraktAudio.dolby_digital.ToString();
                case "aac":
                    return TraktAudio.aac.ToString();
                case "mpeg audio":
                case "mp3":
                    return TraktAudio.mp3.ToString();
                case "pcm":
                    return TraktAudio.lpcm.ToString();
                case "ogg":
                    return TraktAudio.ogg.ToString();
                case "wma":
                    return TraktAudio.wma.ToString();
                case "flac":
                    return TraktAudio.flac.ToString();
                default:
                    return null;
            }
        }

        /// <summary>
        /// Gets the trakt compatible string for the episodes Audio Channels
        /// </summary>
        private string GetEpisodeAudioChannels(DBEpisode episode)
        {
            switch (episode[DBEpisode.cAudioChannels].ToString())
            {
                case "8":
                    return "7.1";
                case "7":
                    return "6.1";
                case "6":
                    return "5.1";
                case "5":
                    return "5.0";
                case "4":
                    return "4.0";
                case "3":
                    return "2.1";
                case "2":
                    return "2.0";
                case "1":
                    return "1.0";
                default:
                    return null;
            }
        }

        /// <summary>
        /// Gets the last time the episode
        /// </summary>
        private string GetLastPlayedDate(DBEpisode episode)
        {
            string slastPlayedDate = string.Empty;
            string retValue = DateTime.UtcNow.ToISO8601();

            // note: use string rather than constant as we can then support 
            // older version of tvseries plugin which doesn't have this field
            // FirstWatchedDate is best for syncing if available
            if (!string.IsNullOrEmpty(episode["FirstWatchedDate"]))
            {
                slastPlayedDate = episode["FirstWatchedDate"];
            }
            else if (!string.IsNullOrEmpty(episode[DBEpisode.cDateWatched]))
            {
                slastPlayedDate = episode[DBEpisode.cDateWatched];
            }

            if (!string.IsNullOrEmpty(slastPlayedDate))
            {
                DateTime result;
                if (DateTime.TryParse(slastPlayedDate, out result))
                {
                    return result.ToUniversalTime().ToISO8601();
                }
            }

            return retValue;
        }

        #endregion

        #region Helpers

        private string CreateLookupKey(TraktEpisodeRated item)
        {
            string show = null;

            if (item.Show.Ids != null && item.Show.Ids.Tvdb != null)
            {
                show = item.Show.Ids.Tvdb.Value.ToString();
            }
            else if (item.Show.Ids != null && item.Show.Ids.Imdb != null)
            {
                show = item.Show.Ids.Imdb;
            }
            else
            {
                if (item.Show.Title == null)
                    return item.GetHashCode().ToString();

                show = item.Show.Title + "_" + item.Show.Year ?? string.Empty;
            }

            return string.Format("{0}_{1}_{2}", show, item.Episode.Season, item.Episode.Number);
        }

        private string CreateLookupKey(TraktCache.Episode episode)
        {
            string show = null;

            if (episode.ShowTvdbId != null)
            {
                show = episode.ShowTvdbId.Value.ToString();
            }
            else if (episode.ShowImdbId != null)
            {
                show = episode.ShowImdbId;
            }
            else
            {
                if (episode.ShowTitle == null)
                    return episode.GetHashCode().ToString();

                show = episode.ShowTitle + "_" + episode.ShowYear ?? string.Empty;
            }

            return string.Format("{0}_{1}_{2}", show, episode.Season, episode.Number);
        }

        private string CreateLookupKey(DBEpisode episode)
        {
            return string.Format("{0}_{1}_{2}", episode[DBOnlineEpisode.cSeriesID], episode[DBOnlineEpisode.cSeasonIndex], episode[DBOnlineEpisode.cEpisodeIndex]);
        }

        private bool EpisodeMatch(DBEpisode localEpisode, TraktCache.Episode onlineEpisode)
        {
            // check if we can match by TVDb ID, this is the best and logical first choice
            if (onlineEpisode.ShowTvdbId != null && onlineEpisode.ShowTvdbId > 0)
            {
                return localEpisode[DBOnlineEpisode.cSeriesID] == onlineEpisode.ShowTvdbId &&
                       localEpisode[DBOnlineEpisode.cSeasonIndex] == onlineEpisode.Season &&
                       localEpisode[DBOnlineEpisode.cEpisodeIndex] == onlineEpisode.Number;
            }
            // next try show IMDb ID
            else if (BasicHandler.IsValidImdb(onlineEpisode.ShowImdbId))
            {
                var show = Helper.getCorrespondingSeries(localEpisode[DBOnlineEpisode.cSeriesID]);
                if (show == null) return false;

                return BasicHandler.GetProperImdbId(show[DBOnlineSeries.cIMDBID]) == onlineEpisode.ShowImdbId &&
                       localEpisode[DBOnlineEpisode.cSeasonIndex] == onlineEpisode.Season &&
                       localEpisode[DBOnlineEpisode.cEpisodeIndex] == onlineEpisode.Number;
            }
            // finially lookup by Title / Year
            else
            {
                var show = Helper.getCorrespondingSeries(localEpisode[DBOnlineEpisode.cSeriesID]);
                if (show == null) return false;

                return BasicHandler.IsTitleMatch(show[DBOnlineSeries.cOriginalName], onlineEpisode.ShowTitle, show.Year.ToNullableInt32()) &&
                       show.Year.ToNullableInt32() == onlineEpisode.ShowYear &&
                       localEpisode[DBOnlineEpisode.cSeasonIndex] == onlineEpisode.Season &&
                       localEpisode[DBOnlineEpisode.cEpisodeIndex] == onlineEpisode.Number;
            }
        }

        private bool EpisodeMatch(DBEpisode localEpisode, TraktEpisode onlineEpisode, TraktShow onlineShow)
        {
            // episode ids are unreliable on themoviedb.org, they seem to be wrong for quite a few episodes!

            //if (onlineEpisode.Ids.TvdbId != null && onlineEpisode.Ids.TvdbId > 0)
            //{
            //    return localEpisode[DBOnlineEpisode.cID] == onlineEpisode.Ids.TvdbId;
            //}

            //else if (BasicHandler.IsValidImdb(onlineEpisode.Ids.ImdbId) && BasicHandler.IsValidImdb(localEpisode[DBOnlineEpisode.cIMDBID]))
            //{
            //    return BasicHandler.GetProperImdbId(localEpisode[DBOnlineEpisode.cIMDBID]) == onlineEpisode.Ids.ImdbId;
            //}

            if (onlineShow.Ids.Tvdb != null && onlineShow.Ids.Tvdb > 0)
            {
                return localEpisode[DBOnlineEpisode.cSeriesID] == onlineShow.Ids.Tvdb &&
                       localEpisode[DBOnlineEpisode.cSeasonIndex] == onlineEpisode.Season &&
                       localEpisode[DBOnlineEpisode.cEpisodeIndex] == onlineEpisode.Number;
            }
            else if (BasicHandler.IsValidImdb(onlineShow.Ids.Imdb))
            {
                var show = Helper.getCorrespondingSeries(localEpisode[DBOnlineEpisode.cSeriesID]);
                if (show == null) return false;

                return BasicHandler.GetProperImdbId(show[DBOnlineSeries.cIMDBID]) == onlineShow.Ids.Imdb &&
                       localEpisode[DBOnlineEpisode.cSeasonIndex] == onlineEpisode.Season &&
                       localEpisode[DBOnlineEpisode.cEpisodeIndex] == onlineEpisode.Number;
            }
            else
            {
                var show = Helper.getCorrespondingSeries(localEpisode[DBOnlineEpisode.cSeriesID]);
                if (show == null) return false;

                return BasicHandler.IsTitleMatch(show[DBOnlineSeries.cOriginalName], onlineShow.Title, onlineShow.Year) &&
                       show.Year.ToNullableInt32() == onlineShow.Year &&
                       localEpisode[DBOnlineEpisode.cSeasonIndex] == onlineEpisode.Season &&
                       localEpisode[DBOnlineEpisode.cEpisodeIndex] == onlineEpisode.Number;
            }
        }

        private bool ShowMatch(DBSeries localShow, TraktShow onlineShow)
        {
            if (onlineShow.Ids.Tvdb != null && onlineShow.Ids.Tvdb > 0)
            {
                return localShow[DBSeries.cID] == onlineShow.Ids.Tvdb;
            }
            else if (BasicHandler.IsValidImdb(onlineShow.Ids.Imdb) && BasicHandler.IsValidImdb(localShow[DBOnlineSeries.cIMDBID]))
            {
                return localShow[DBOnlineSeries.cIMDBID] == onlineShow.Ids.Imdb;
            }
            else
            {
                return BasicHandler.IsTitleMatch(localShow[DBOnlineSeries.cOriginalName], onlineShow.Title, onlineShow.Year) &&
                       localShow.Year.ToNullableInt32() == onlineShow.Year;
            }
        }

        private void RateEpisode(DBEpisode episode)
        {
            var rateThread = new Thread((objEpisode) =>
            {
                var rateEpisode = objEpisode as DBEpisode;
                if (rateEpisode == null) return;

                var show = Helper.getCorrespondingSeries(rateEpisode[DBOnlineEpisode.cSeriesID]);
                if (show == null || show[DBOnlineSeries.cTraktIgnore]) return;

                TraktLogger.Info("Received a Rate Episode event from tvseries. Show Title = '{0}', Show Year = '{1}', Season = '{2}', Episode = '{3}', Episode Title = '{4}', Show TVDb ID = '{5}', Episode TVDb ID = '{6}'",
                                    show[DBOnlineSeries.cOriginalName], show.Year ?? "<empty>", episode[DBOnlineEpisode.cSeasonIndex], episode[DBOnlineEpisode.cEpisodeIndex], episode[DBOnlineEpisode.cEpisodeName], episode[DBOnlineEpisode.cSeriesID], episode[DBOnlineEpisode.cID]);

                // send show data as well in case tvdb ids are not available on trakt server
                // TraktSyncEpisodeRated object is good if we could trust trakt having the tvdb ids.
                // trakt is more likely to have a show tvdb id than a episode tvdb id
                var episodeRateData = new TraktSyncShowRatedEx
                {
                    Title = show[DBOnlineSeries.cOriginalName],
                    Year = show.Year.ToNullableInt32(),
                    Ids = new TraktShowId
                    { 
                        Tvdb = show[DBSeries.cID],
                        Imdb = BasicHandler.GetProperImdbId(show[DBOnlineSeries.cIMDBID])
                    },
                    Seasons = new List<TraktSyncShowRatedEx.Season>
                    {
                        new TraktSyncShowRatedEx.Season
                        {
                            Number = episode[DBOnlineEpisode.cSeasonIndex],
                            Episodes = new List<TraktSyncShowRatedEx.Season.Episode>
                            {
                                new TraktSyncShowRatedEx.Season.Episode
                                {
                                    Number = episode[DBOnlineEpisode.cEpisodeIndex],
                                    Rating = episode[DBOnlineEpisode.cMyRating],
                                    RatedAt = DateTime.UtcNow.ToISO8601()
                                }
                            }
                        }
                    }
                };

                // update local cache
                TraktCache.AddEpisodeToRatings
                    (
                        new TraktShow
                        {
                            Ids = new TraktShowId
                            {
                                Tvdb = show[DBSeries.cID],
                                Imdb = BasicHandler.GetProperImdbId(show[DBOnlineSeries.cIMDBID])
                            }
                        },
                        new TraktEpisode
                        {
                            Ids = new TraktEpisodeId
                            {
                                Tvdb = episode[DBOnlineEpisode.cID],
                                Imdb = episode[DBOnlineEpisode.cIMDBID].ToString().ToNullIfEmpty()
                            },
                            Number = episode[DBOnlineEpisode.cEpisodeIndex],
                            Season = episode[DBOnlineEpisode.cSeasonIndex]
                        },
                        episode[DBOnlineEpisode.cMyRating]
                    );

                var response = TraktAPI.TraktAPI.AddEpisodeToRatingsEx(episodeRateData);
                TraktLogger.LogTraktResponse(response);
            })
            {
                IsBackground = true,
                Name = "Rate"
            };

            rateThread.Start(episode);
        }

        private void RateShow(DBSeries show)
        {
            var rateThread = new Thread((objShow) =>
            {
                if (show[DBOnlineSeries.cTraktIgnore]) return;

                var rateShow = objShow as DBSeries;
                if (rateShow == null) return;

                TraktLogger.Info("Received a Rate Show event from tvseries. Show Title = '{0}', Show Year = '{1}', Show TVDb ID = '{2}'", show[DBOnlineSeries.cOriginalName], show.Year, show[DBSeries.cID]);

                var showRateData = new TraktSyncShowRated
                {
                    Ids = new TraktShowId
                    {
                        Tvdb = show[DBSeries.cID],
                        Imdb = BasicHandler.GetProperImdbId(show[DBOnlineSeries.cIMDBID])
                    },
                    Title = show[DBOnlineSeries.cOriginalName],
                    Year = show.Year.ToNullableInt32(),
                    Rating = show[DBOnlineSeries.cMyRating],
                    RatedAt = DateTime.UtcNow.ToISO8601()
                };

                // update local cache
                TraktCache.AddShowToRatings(showRateData, showRateData.Rating);

                var response = TraktAPI.TraktAPI.AddShowToRatings(showRateData);
                TraktLogger.LogTraktResponse(response);
            })
            {
                IsBackground = true,
                Name = "Rate"
            };

            rateThread.Start(show);
        }

        private void MarkEpisodesAsWatched(DBSeries show, List<DBEpisode> episodes)
        {
            var syncThread = new Thread((o) =>
            {
                // send show data as well in case tvdb ids are not available on trakt server
                // TraktSyncEpisodeRated object is good if we could trust trakt having the tvdb ids.
                // trakt is more likely to have a show tvdb id than a episode tvdb id
                var showEpisodes = new TraktSyncShowWatchedEx
                {
                    Title = show[DBOnlineSeries.cOriginalName],
                    Year = show.Year.ToNullableInt32(),
                    Ids = new TraktShowId
                    {
                        Tvdb = show[DBSeries.cID],
                        Imdb = BasicHandler.GetProperImdbId(show[DBOnlineSeries.cIMDBID])
                    }
                };

                var seasons = new List<TraktSyncShowWatchedEx.Season>();

                foreach (var episode in episodes)
                {
                    if (seasons.Exists(s => s.Number == episode[DBOnlineEpisode.cSeasonIndex]))
                    {
                        // add the episode to the season collection
                        seasons.First(s => s.Number == episode[DBOnlineEpisode.cSeasonIndex])
                               .Episodes.Add(new TraktSyncShowWatchedEx.Season.Episode
                               {
                                   Number = episode[DBOnlineEpisode.cEpisodeIndex],
                                   WatchedAt = DateTime.UtcNow.ToISO8601()
                               });

                    }
                    else
                    {
                        // create season and add episode to it's episode collection
                        seasons.Add(new TraktSyncShowWatchedEx.Season
                        {
                            Number = episode[DBOnlineEpisode.cSeasonIndex],
                            Episodes = new List<TraktSyncShowWatchedEx.Season.Episode>
                            {
                                new TraktSyncShowWatchedEx.Season.Episode
                                {
                                    Number = episode[DBOnlineEpisode.cEpisodeIndex],
                                    WatchedAt = DateTime.UtcNow.ToISO8601()
                                }
                            }
                        });
                    }
                }
                showEpisodes.Seasons = seasons;

                var showSync = new TraktSyncShowsWatchedEx
                {
                    Shows = new List<TraktSyncShowWatchedEx> { showEpisodes }
                };

                // update local cache
                TraktCache.AddEpisodesToWatchHistory(showEpisodes);

                var response = TraktAPI.TraktAPI.AddShowsToWatchedHistoryEx(showSync);
                TraktLogger.LogTraktResponse(response);
            })
            {
                IsBackground = true,
                Name = "ToggleWatched"
            };

            syncThread.Start();
        }

        private void MarkEpisodesAsUnWatched(DBSeries show, List<DBEpisode> episodes)
        {
            var syncThread = new Thread((o) =>
            {
                // send show data as well in case tvdb ids are not available on trakt server
                // TraktSyncEpisodeRated object is good if we could trust trakt having the tvdb ids.
                // trakt is more likely to have a show tvdb id than a episode tvdb id
                var showEpisodes = new TraktSyncShowEx
                {
                    Title = show[DBOnlineSeries.cOriginalName],
                    Year = show.Year.ToNullableInt32(),
                    Ids = new TraktShowId
                    {
                        Tvdb = show[DBSeries.cID],
                        Imdb = BasicHandler.GetProperImdbId(show[DBOnlineSeries.cIMDBID])
                    }
                };

                var seasons = new List<TraktSyncShowEx.Season>();

                foreach (var episode in episodes)
                {
                    if (seasons.Exists(s => s.Number == episode[DBOnlineEpisode.cSeasonIndex]))
                    {
                        // add the episode to the season collection
                        seasons.First(s => s.Number == episode[DBOnlineEpisode.cSeasonIndex])
                               .Episodes.Add(new TraktSyncShowEx.Season.Episode
                               {
                                   Number = episode[DBOnlineEpisode.cEpisodeIndex]
                               });

                    }
                    else
                    {
                        // create season and add episode to it's episode collection
                        seasons.Add(new TraktSyncShowEx.Season
                        {
                            Number = episode[DBOnlineEpisode.cSeasonIndex],
                            Episodes = new List<TraktSyncShowEx.Season.Episode>
                            {
                                new TraktSyncShowEx.Season.Episode
                                {
                                    Number = episode[DBOnlineEpisode.cEpisodeIndex]
                                }
                            }
                        });
                    }
                }
                showEpisodes.Seasons = seasons;

                var showSync = new TraktSyncShowsEx
                {
                    Shows = new List<TraktSyncShowEx> { showEpisodes }
                };

                // update local cache
                TraktCache.RemoveEpisodesFromWatchHistory(showEpisodes);

                var response = TraktAPI.TraktAPI.RemoveShowsFromWatchedHistoryEx(showSync);
                TraktLogger.LogTraktResponse(response);
            })
            {
                IsBackground = true,
                Name = "ToggleWatched"
            };

            syncThread.Start();
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

            TraktLogger.Debug("Showing rate dialog for episode. Title = '{0}'", episode.ToString());

            var rateThread = new Thread((o) =>
            {
                var epToRate = o as DBEpisode;
                if (epToRate == null) return;

                var show = Helper.getCorrespondingSeries(epToRate[DBOnlineEpisode.cSeriesID]);
                if (show == null || show[DBOnlineSeries.cTraktIgnore]) return;

                var episodeRateData = new TraktSyncShowRatedEx
                {
                    Title = show[DBOnlineSeries.cOriginalName],
                    Year = show.Year.ToNullableInt32(),
                    Ids = new TraktShowId
                    {
                        Tvdb = show[DBSeries.cID],
                        Imdb = BasicHandler.GetProperImdbId(show[DBOnlineSeries.cIMDBID])
                    },
                    Seasons = new List<TraktSyncShowRatedEx.Season>
                    {
                        new TraktSyncShowRatedEx.Season
                        {
                            Number = episode[DBOnlineEpisode.cSeasonIndex],
                            Episodes = new List<TraktSyncShowRatedEx.Season.Episode>
                            {
                                new TraktSyncShowRatedEx.Season.Episode
                                {
                                    Number = episode[DBOnlineEpisode.cEpisodeIndex],
                                    Rating = episode[DBOnlineEpisode.cMyRating],
                                    RatedAt = DateTime.UtcNow.ToISO8601()
                                }
                            }
                        }
                    }
                };

                // get the rating submitted to trakt
                int rating = GUIUtils.ShowRateDialog<TraktSyncShowRatedEx>(episodeRateData);

                if (rating > 0)
                {
                    TraktLogger.Debug("Rating {0} as {1}/10", epToRate.ToString(), rating.ToString());

                    // update local cache
                    TraktCache.AddEpisodeToRatings
                    (
                        new TraktShow
                        {
                            Ids = new TraktShowId
                            {
                                Tvdb = show[DBSeries.cID],
                                Imdb = BasicHandler.GetProperImdbId(show[DBOnlineSeries.cIMDBID])
                            }
                        },
                        new TraktEpisode
                        {
                            Ids = new TraktEpisodeId
                            {
                                Tvdb = episode[DBOnlineEpisode.cID],
                                Imdb = episode[DBOnlineEpisode.cIMDBID].ToString().ToNullIfEmpty()
                            },
                            Number = episode[DBOnlineEpisode.cEpisodeIndex],
                            Season = episode[DBOnlineEpisode.cSeasonIndex]
                        }, rating
                    );

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
                else if(rating == 0)
                {
                    TraktCache.RemoveEpisodeFromRatings
                    (
                        new TraktEpisode
                        {
                            Ids = new TraktEpisodeId
                            {
                                Tvdb = episode[DBOnlineEpisode.cID],
                                Imdb = episode[DBOnlineEpisode.cIMDBID].ToString().ToNullIfEmpty()
                            }
                        }
                    );
                }
            })
            {
                Name = "Rate",
                IsBackground = true
            };          
            
            rateThread.Start(episode);
        }

        #endregion

        #region TVSeries Events

        private void OnImportCompleted(bool newEpisodeAdded)
        {
            Thread.CurrentThread.Name = "Sync";

            if (TraktSettings.AccountStatus != ConnectionState.Connected) return;

            TraktLogger.Debug("MP-TVSeries import complete, checking if sync required");

            if (newEpisodeAdded)
            {
                // sync again
                var syncThread = new Thread(obj =>
                {
                    TraktLogger.Info("New episodes added in MP-TVSeries, starting sync");

                    while (SyncLibraryInProgress)
                    {
                        // only do one sync at a time
                        TraktLogger.Debug("MP-TVSeries sync still in progress, trying again in 60 secs");
                        Thread.Sleep(60000);
                    }
                    try
                    {
                        SyncLibrary();
                    }
                    catch (Exception ex)
                    {
                        TraktLogger.Error("MP-TVSeries sync failed, Reason = '{0}', StackTrace = {1}", ex.Message, ex.StackTrace);
                    }
                })
                {
                    IsBackground = true,
                    Name = "Sync"
                };

                syncThread.Start();
            }
            else
            {
                TraktLogger.Debug("MP-TVSeries sync is not required");
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

            EpisodeWatching = false;

            TraktLogger.Info("Playback of MP-TVSeries episode stopped and considered watched. Title = '{0}', PlayList Item = '{1}'", episode.ToString(), isPlaylist);
            double progress = GetPlayerProgress(episode);

            // purely defensive check against bad progress reading
            // for an episode that counts as watched.
            if (progress < 80) progress = 100;

            var stopWatching = new Thread((objEpisode) =>
            {
                var stoppedEpisode = objEpisode as DBEpisode;
                if (stoppedEpisode == null) return;

                TraktScrobbleEpisode scrobbleData = null;

                #region Double Episode Handling

                // if its a double episode we may need to mark two episodes as watched
                if (stoppedEpisode.IsDoubleEpisode)
                {
                    // check if we should mark the first episode as watched
                    if (!FirstEpisodeWatched)
                    {
                        scrobbleData = CreateScrobbleData(stoppedEpisode, 100);
                        if (scrobbleData == null) return;

                        TraktLogger.LogTraktResponse(TraktAPI.TraktAPI.StopEpisodeScrobble(scrobbleData));
                    }

                    // scrobble the second 
                    scrobbleData = CreateScrobbleData(SecondEpisode, progress);
                    if (scrobbleData == null) return;

                    // prompt to rate second episode
                    ShowRateDialog(SecondEpisode, isPlaylist);

                    TraktLogger.LogTraktResponse(TraktAPI.TraktAPI.StopEpisodeScrobble(scrobbleData));
                    return;
                }

                #endregion

                scrobbleData = CreateScrobbleData(stoppedEpisode, progress);
                if (scrobbleData == null) return;

                // prompt to rate episode
                ShowRateDialog(stoppedEpisode, isPlaylist);

                // update local cache
                TraktCache.AddEpisodeToWatchHistory(scrobbleData.Show, scrobbleData.Episode);

                var response = TraktAPI.TraktAPI.StopEpisodeScrobble(scrobbleData);
                TraktLogger.LogTraktResponse(response);
            })
            {
                IsBackground = true,
                Name = "Scrobble"
            };

            stopWatching.Start(episode);
        }

        private void OnEpisodeStarted(DBEpisode episode)
        {
            if (TraktSettings.AccountStatus != ConnectionState.Connected) return;

            TraktLogger.Info("Playback of MP-TVSeries episode started. Title = '{0}'", episode.ToString());
            EpisodeWatching = true;
            CurrentEpisode = episode;
        }

        private void OnEpisodeStopped(DBEpisode episode)
        {
            if (TraktSettings.AccountStatus != ConnectionState.Connected) return;

            // episode does not count as watched
            // if it's a double episode we need to double check the progress to determine
            // if the first episode should be marked as watched
            TraktLogger.Info("Playback of MP-TVSeries episode stopped. Title = '{0}'", episode.ToString());
            EpisodeWatching = false;

            double progress = GetPlayerProgress(episode);

            var stopWatching = new Thread((objEpisode) =>
            {
                TraktScrobbleResponse response = null;
                TraktScrobbleEpisode scrobbleData = null;

                var stoppedEpisode = objEpisode as DBEpisode;
                if (stoppedEpisode == null) return;

                #region Double Episode Handling

                if (stoppedEpisode.IsDoubleEpisode && progress > 50.0)
                {
                    // first episode can be marked as watched
                    if (!FirstEpisodeWatched)
                    {
                        TraktLogger.Info("Marking first episode of double episode as watched");

                        // fake progress so it's marked as watched online
                        scrobbleData = CreateScrobbleData(stoppedEpisode, 100);
                        if (scrobbleData == null) return;

                        // prompt to rate episode
                        ShowRateDialog(stoppedEpisode, false);

                        // update local cache
                        TraktCache.AddEpisodeToWatchHistory(scrobbleData.Show, scrobbleData.Episode);
                        TraktLogger.LogTraktResponse(TraktAPI.TraktAPI.StopEpisodeScrobble(scrobbleData));

                        return;
                    }
                    else
                    {
                        // stop the second 
                        scrobbleData = CreateScrobbleData(SecondEpisode, progress);
                        if (scrobbleData == null) return;

                        // only mark as watched online if percentage watched is greater than user setting
                        response = TraktAPI.TraktAPI.PauseEpisodeScrobble(scrobbleData);
                        TraktLogger.LogTraktResponse(response);

                        return;
                    }
                }

                #endregion

                scrobbleData = CreateScrobbleData(stoppedEpisode, progress);
                TraktAPI.TraktAPI.PauseEpisodeScrobble(scrobbleData);

                TraktLogger.LogTraktResponse(response);
            })
            {
                IsBackground = true,
                Name = "Scrobble"
            };

            stopWatching.Start(episode);
        }

        private void OnRateItem(DBTable item, string value)
        {
            if (TraktSettings.AccountStatus != ConnectionState.Connected) return;
            
            if (item is DBEpisode)
                RateEpisode(item as DBEpisode);
            else
                RateShow(item as DBSeries);
        }

        private void OnToggleWatched(DBSeries show, List<DBEpisode> episodes, bool watched)
        {
            if (TraktSettings.AccountStatus != ConnectionState.Connected) return;

            TraktLogger.Info("Received a Toggle Watched event from tvseries. Show Title = '{0}', Episodes = '{1}', Watched = '{2}'", show[DBOnlineSeries.cOriginalName], episodes.Count, watched.ToString());
            
            if (show[DBOnlineSeries.cTraktIgnore]) return;

            if (watched)
                MarkEpisodesAsWatched(show, episodes);
            else
                MarkEpisodesAsUnWatched(show, episodes);
            
        }

        #endregion
      
    } 
}
