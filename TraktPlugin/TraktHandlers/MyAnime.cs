using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaPortal.Player;
using MediaPortal.GUI.Library;
using MediaPortal.Configuration;
using System.ComponentModel;
using System.Reflection;
using System.Threading;
using System.IO;
using TraktPlugin.TraktAPI;
using TraktPlugin.TraktAPI.DataStructures;
using AniDBAPI;
using MyAnimePlugin2;
using MyAnimePlugin2.Persistence;

namespace TraktPlugin.TraktHandlers
{
    /// <summary>
    /// Support for My Anime
    /// </summary>
    class MyAnime : ITraktHandler
    {
        #region Enums

        public enum SelectedType
        {
            Series,
            EpisodeTypes,
            Episode,
            Group,
            Unknown
        }

        #endregion

        #region Variables

        Timer TraktTimer;
        static VideoHandler player = null;
        FileLocal CurrentEpisode = null;

        #endregion

        #region Constructor

        public MyAnime(int priority)
        {
            // check if plugin exists otherwise plugin could accidently get added to list
            string pluginFilename = Path.Combine(Config.GetSubFolder(Config.Dir.Plugins, "Windows"), "Anime2.dll");
            if (!File.Exists(pluginFilename)) throw new FileNotFoundException("Plugin not found!");
           
            Priority = priority;
        }

        #endregion

        #region ITraktHandler Members

        public string Name
        {
            get { return "My Anime"; }
        }

        public int Priority { get; set; }

        public void SyncLibrary()
        {
            TraktLogger.Info("My Anime Starting Sync");

            #region Get online data
            // get all episodes on trakt that are marked as in 'collection'
            IEnumerable<TraktLibraryShow> traktCollectionEpisodes = TraktAPI.TraktAPI.GetLibraryEpisodesForUser(TraktSettings.Username);
            if (traktCollectionEpisodes == null)
            {
                TraktLogger.Error("Error getting show collection from trakt server, cancelling sync.");
                return;
            }
            TraktLogger.Info("{0} tvshows in trakt collection", traktCollectionEpisodes.Count().ToString());

            // get all episodes on trakt that are marked as 'seen' or 'watched'
            IEnumerable<TraktLibraryShow> traktWatchedEpisodes = TraktAPI.TraktAPI.GetWatchedEpisodesForUser(TraktSettings.Username);
            if (traktWatchedEpisodes == null)
            {
                TraktLogger.Error("Error getting shows watched from trakt server, cancelling sync.");
                return;
            }
            TraktLogger.Info("{0} tvshows with watched episodes in trakt library", traktWatchedEpisodes.Count().ToString());

            // get all episodes on trakt that are marked as 'unseen'
            IEnumerable<TraktLibraryShow> traktUnSeenEpisodes = TraktAPI.TraktAPI.GetUnSeenEpisodesForUser(TraktSettings.Username);
            if (traktUnSeenEpisodes == null)
            {
                TraktLogger.Error("Error getting shows unseen from trakt server, cancelling sync.");
                return;
            }
            TraktLogger.Info("{0} tvshows with unseen episodes in trakt library", traktUnSeenEpisodes.Count().ToString());
            #endregion

            #region Get local data
            List<FileLocal> localCollectionEpisodes = new List<FileLocal>();
            List<FileLocal> localWatchedEpisodes = new List<FileLocal>();

            // Get all local episodes in database
            localCollectionEpisodes = FileLocal.GetAll().Where(f => !string.IsNullOrEmpty(f.FileNameFull) && f.AnimeEpisodes.Count > 0).ToList();

            TraktLogger.Info("{0} episodes with local files in my anime database", localCollectionEpisodes.Count.ToString());

            // Get only Valid Episodes types
            localCollectionEpisodes.RemoveAll(lc => lc.AnimeEpisodes.Where(e => (e.EpisodeTypeEnum != enEpisodeType.Normal && e.EpisodeTypeEnum != enEpisodeType.Special)).Count() > 0);

            TraktLogger.Info("{0} episodes with valid episode types in my anime database", localCollectionEpisodes.Count.ToString());

            // Get watched episodes
            localWatchedEpisodes = localCollectionEpisodes.Where(f => (f.AniDB_File != null && f.AniDB_File.IsWatched > 0) || (f.AnimeEpisodes != null && f.AnimeEpisodes[0].IsWatched > 0)).ToList();

            TraktLogger.Info("{0} episodes watched in my anime database", localWatchedEpisodes.Count.ToString());
            #endregion

            #region Sync collection/library to trakt
            // get list of episodes that we have not already trakt'd
            List<FileLocal> localEpisodesToSync = new List<FileLocal>(localCollectionEpisodes);
            foreach (FileLocal ep in localCollectionEpisodes)
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
            List<FileLocal> localWatchedEpisodesToSync = new List<FileLocal>(localWatchedEpisodes);
            foreach (FileLocal ep in localWatchedEpisodes)
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
            foreach (FileLocal ep in localCollectionEpisodes.Where(e => e.AnimeEpisodes[0].IsWatched == 0))
            {
                if (TraktEpisodeExists(traktWatchedEpisodes, ep) && !TraktEpisodeExists(traktUnSeenEpisodes, ep))
                {
                    // mark episode as watched
                    TraktLogger.Info("Marking episode '{0}' as watched", ep.ToString());
                    ep.AnimeEpisodes[0].ToggleWatchedStatus(true, false);
                }
            }
            #endregion

            #region Sync unseen flags from trakt locally
            foreach (FileLocal ep in localCollectionEpisodes.Where(e => e.AnimeEpisodes[0].IsWatched > 1))
            {
                if (TraktEpisodeExists(traktUnSeenEpisodes, ep))
                {
                    // mark episode as unwatched
                    TraktLogger.Info("Marking episode '{0}' as unwatched", ep.ToString());
                    ep.AnimeEpisodes[0].ToggleWatchedStatus(false, false);
                }
            }
            #endregion

            #region Clean Library
            if (TraktSettings.KeepTraktLibraryClean && TraktSettings.TvShowPluginCount > 1)
            {
                TraktLogger.Info("Removing shows From Trakt Collection no longer in database");

                // if we no longer have a file reference in database remove from library
                foreach (var series in traktCollectionEpisodes)
                {
                    TraktEpisodeSync syncData = GetEpisodesForTraktRemoval(series, localCollectionEpisodes.Where(e => e.AniDB_File.AnimeSeries.TvDB_ID.ToString() == series.SeriesId).ToList());
                    if (syncData == null) continue;
                    TraktResponse response = TraktAPI.TraktAPI.SyncEpisodeLibrary(syncData, TraktSyncModes.unlibrary);
                    TraktAPI.TraktAPI.LogTraktResponse(response);
                    Thread.Sleep(500);
                }
            }
            #endregion

            TraktLogger.Info("My Anime Sync Completed");
        }

        public bool Scrobble(string filename)
        {
            StopScrobble();

            // lookup episode by filename
            List<FileLocal> files = FileLocal.GetAll();
            FileLocal file = files.FirstOrDefault(f => f.FileNameFull == filename);
            if (file == null) return false;

            CurrentEpisode = file;
            TraktLogger.Info("Detected episode playing in My Anime: '{0}'", CurrentEpisode.ToString());

            // create 15 minute timer to send watching status
            #region scrobble timer
            TraktTimer = new Timer(new TimerCallback((stateInfo) =>
            {
                FileLocal episode = stateInfo as FileLocal;
                if (episode == null) return;

                // duration in minutes
                double duration = g_Player.Duration / 60;
                double progress = 0.0;

                // get current progress of player (in seconds) to work out percent complete
                if (g_Player.Duration > 0.0) progress = (g_Player.CurrentPosition / g_Player.Duration) * 100.0;

                TraktEpisodeScrobble scrobbleData = CreateScrobbleData(CurrentEpisode);
                if (scrobbleData == null) return;

                // set duration/progress in scrobble data
                scrobbleData.Duration = Convert.ToInt32(duration).ToString();
                scrobbleData.Progress = Convert.ToInt32(progress).ToString();

                // set watching status on trakt
                TraktResponse response = TraktAPI.TraktAPI.ScrobbleEpisodeState(scrobbleData, TraktScrobbleStates.watching);
                TraktAPI.TraktAPI.LogTraktResponse(response);
            }), CurrentEpisode, 3000, 900000);
            #endregion

            return true;
        }

        public void StopScrobble()
        {
            if (TraktTimer != null)
                TraktTimer.Dispose();

            if (CurrentEpisode == null) return;

            #region Scrobble
            Thread scrobbleEpisode = new Thread(delegate(object o)
            {
                FileLocal episode = o as FileLocal;
                if (episode == null) return;

                TraktLogger.Info("My Anime episode considered watched '{0}'", episode.ToString());

                // get scrobble data to send to api
                TraktEpisodeScrobble scrobbleData = CreateScrobbleData(episode);
                if (scrobbleData == null) return;

                // set duration/progress in scrobble data                
                scrobbleData.Duration = Convert.ToInt32(g_Player.Duration / 60).ToString();
                scrobbleData.Progress = "100";

                TraktResponse response = TraktAPI.TraktAPI.ScrobbleEpisodeState(scrobbleData, TraktScrobbleStates.scrobble);
                TraktAPI.TraktAPI.LogTraktResponse(response);
            })
            {
                IsBackground = true,
                Name = "Scrobble Episode"
            };
            #endregion

            // if episode is atleast 90% complete, consider watched
            if ((g_Player.CurrentPosition / g_Player.Duration) >= 0.9)
            {
                scrobbleEpisode.Start(CurrentEpisode);
            }
            else
            {
                #region Cancel Watching
                TraktLogger.Info("Stopped My Anime episode playback '{0}'", CurrentEpisode.ToString());

                // stop scrobbling
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
                #endregion

                cancelWatching.Start();
            }

            CurrentEpisode = null;
        }

        #endregion

        #region Public Methods

        public void DisposeEvents()
        {

        }

        /// <summary>
        /// Playback an episode using My Anime internal Video Handler
        /// </summary>
        /// <param name="seriesid">series id of episode</param>
        /// <param name="seasonid">season index</param>
        /// <param name="episodeid">episode index</param>
        public static bool PlayEpisode(int seriesid, int seasonid, int episodeid)
        {
            var episodes = FileLocal.GetAll();
            var episode = episodes.FirstOrDefault(e => e.AnimeEpisodes.Where(ae => ae.Series.TvDB_Episodes.Where(te => te.SeriesID == seriesid && te.SeasonNumber == seasonid && te.EpisodeNumber == episodeid).Count() == 1).Count() == 1);

            if (episode == null || string.IsNullOrEmpty(episode.FileNameFull)) return false;
            return PlayEpisode(episode);
        }

        /// <summary>
        /// Playback the first unwatched episode for a series using TVSeries internal Video Handler
        /// If no Unwatched episodes exists, play the Most Recently Aired
        /// </summary>
        /// <param name="seriesid">series id of episode</param>
        public static bool PlayFirstUnwatchedEpisode(int seriesid)
        {
            var episodes = FileLocal.GetAll();
            if (episodes == null || episodes.Count == 0) return false;

            // filter out anything we can't play
            episodes.RemoveAll(e => string.IsNullOrEmpty(e.FileNameFull));
            if (episodes.Count == 0) return false;

            // filter by tvdb series id
            episodes.RemoveAll(e => e.AniDB_File == null || e.AniDB_File.AnimeSeries.TvDB_ID != seriesid);

            TraktLogger.Info("Found {0} local episodes for TVDb {1}", episodes.Count, seriesid.ToString());
            if (episodes.Count == 0) return false;

            // sort by air date
            episodes.Sort(new Comparison<FileLocal>((x, y) => { return x.AnimeEpisodes.First().AniDB_Episode.AirDate.CompareTo(y.AnimeEpisodes.First().AniDB_Episode.AirDate); }));

            // filter out watched
            var episode = episodes.Where(e => e.AnimeEpisodes.First().IsWatched == 0).FirstOrDefault();
            if (episode == null)
            {
                TraktLogger.Info("No Unwatched episodes found, Playing most recent episode");
                episode = episodes.LastOrDefault();
            }
            if (episode == null) return false;

            return PlayEpisode(episode);
        }

        public static bool PlayEpisode(FileLocal episode)
        {
            if (player == null) player = new VideoHandler();
            return player.ResumeOrPlay(episode);
        }

        /// <summary>
        /// Get the current selected facade item in My Anime
        /// </summary>
        /// <param name="obj">TVTag object</param>
        /// <returns>Returns the selected type</returns>
        public static SelectedType GetSelectedType(Object obj)
        {
            if ((obj as AnimeEpisode) != null) return SelectedType.Episode;
            if ((obj as AnimeSeries) != null) return SelectedType.Series;
            return SelectedType.Unknown;
        }

        /// <summary>
        /// Get Episode Info for selected object
        /// </summary>        
        public static bool GetEpisodeInfo(Object obj, out string title, out string tvdb, out string seasonidx, out string episodeidx)
        {
            title = string.Empty;
            tvdb = string.Empty;
            seasonidx = string.Empty;
            episodeidx = string.Empty;            

            if (obj == null) return false;

            AnimeEpisode episode = obj as AnimeEpisode;
            if (episode == null) return false;

            AnimeSeries series = episode.Series;
            if (series == null) return false;

            title = series.SeriesName;
            tvdb = series.TvDB_ID.HasValue ? series.TvDB_ID.Value.ToString() : null;
            int iSeasonidx = 0;
            int iEpisodeidx = 0;

            if (GetTVDBEpisodeInfo(episode, out tvdb, out iSeasonidx, out iEpisodeidx))
            {
                seasonidx = iSeasonidx.ToString();
                episodeidx = iEpisodeidx.ToString();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Get Series Info for selected object
        /// </summary>
        public static bool GetSeriesInfo(Object obj, out string title, out string tvdb)
        {
            title = string.Empty;
            tvdb = string.Empty;

            if (obj == null) return false;

            AnimeSeries series = obj as AnimeSeries;
            if (series == null) return false;

            title = series.SeriesName;
            tvdb = series.TvDB_ID.HasValue ? series.TvDB_ID.Value.ToString() : null;

            return true;
        }

        /// <summary>
        /// Get TVDB info from Anime Episode
        /// </summary>
        /// <param name="episode">Anime Episode reference</param>
        /// <param name="seriesid">TVDB Series ID</param>
        /// <param name="seasonidx">TVDB Season Number</param>
        /// <param name="episodeidx">TVDB Episode Number</param>
        /// <returns></returns>
        public static bool GetTVDBEpisodeInfo(AnimeEpisode episode, out string seriesid, out int seasonidx, out int episodeidx)
        {
            seriesid = null;
            seasonidx = 0;
            episodeidx = 0;

            if (episode.Series == null) return false;

            seriesid = episode.Series.TvDB_ID.HasValue ? episode.Series.TvDB_ID.ToString() : null;
            if (seriesid == null) return false;
            
            // get air date in valid tvdb form
            string episodeAirDate = episode.AniDB_Episode.AirDateAsDate.ToString("yyyy-MM-dd");

            TvDB_Episode tvdbEpisode = null;
            List<TvDB_Episode> tvdbEpisodes = episode.Series.TvDB_Episodes;

            // episode Number is not absolute in some case e.g. multiple animeseries mapped to the same tvdb series
            if (episode.Series.TvDB_SeasonNumber == null || episode.Series.TvDB_SeasonNumber == 1)
            {
                // first try absolute episode order
                tvdbEpisode = tvdbEpisodes.FirstOrDefault(e => e.Absolute_number == episode.EpisodeNumber);
            }

            // try title / airdate matching, this should support specials
            if (tvdbEpisode == null)
            {
                tvdbEpisode = tvdbEpisodes.FirstOrDefault(e => e.EpisodeName == episode.EpisodeName || e.FirstAired == episodeAirDate);

                // try My Anime's helper, doesn't support specials
                if (tvdbEpisode == null) tvdbEpisode = episode.GetTvDBEpisode();
            }

            if (tvdbEpisode == null) return false;

            seasonidx = tvdbEpisode.SeasonNumber;
            episodeidx = tvdbEpisode.EpisodeNumber;
            return true;
        }

        /// <summary>
        /// Anime Year is in form StartYear-LastYear
        /// </summary>
        public static string GetStartYear(AnimeSeries series)
        {
            if (series.Year.Contains('-'))
                return series.Year.Split('-')[0];
            return series.Year;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Get the TVTag of the selected facade item in My Anime window
        /// </summary>
        public static Object SelectedObject
        {
            get
            {
                // Ensure we are in My Anime window
                GUIWindow window = GUIWindowManager.GetWindow((int)GUI.ExternalPluginWindows.MyAnime);
                if (window == null) return null;

                // Get the Facade control
                GUIFacadeControl facade = window.GetControl(50) as GUIFacadeControl;
                if (facade == null) return null;

                // Get the Selected Item
                GUIListItem currentitem = facade.SelectedListItem;
                if (currentitem == null) return null;

                // Get the series/episode object
                return currentitem.TVTag;
            }
        }

        #endregion

        #region Data Creators

        /// <summary>
        /// Creates Sync Data based on Series object and a List of Episode objects
        /// </summary>
        /// <param name="series">The series to base the object on</param>
        /// <param name="epsiodes">The list of episodes to base the object on</param>
        /// <returns>The Trakt Sync data to send</returns>
        private TraktEpisodeSync CreateSyncData(AnimeSeries series, List<FileLocal> episodes)
        {
            if (series == null || series.TvDB_ID == null) return null;

            // set series properties for episodes
            TraktEpisodeSync traktSync = new TraktEpisodeSync
            {
                Password = TraktSettings.Password,
                UserName = TraktSettings.Username,
                SeriesID = series.TvDB_ID.ToString(),
                Year = GetStartYear(series),
                Title = series.SeriesName
            };

            // get list of episodes for series
            List<TraktEpisodeSync.Episode> epList = new List<TraktEpisodeSync.Episode>();

            foreach (FileLocal file in episodes.Where(e => (e.AniDB_File != null && e.AniDB_File.AnimeSeries.TvDB_ID == series.TvDB_ID)))
            {
                TraktEpisodeSync.Episode episode = new TraktEpisodeSync.Episode();

                // can have multiple episodes linked to a file?
                foreach (var ep in file.AnimeEpisodes)
                {
                    string seriesid = series.TvDB_ID.ToString();
                    int seasonidx = 0;
                    int episodeidx = 0;

                    if (GetTVDBEpisodeInfo(ep, out seriesid, out seasonidx, out episodeidx))
                    {
                        episode.SeasonIndex = seasonidx.ToString();
                        episode.EpisodeIndex = episodeidx.ToString();
                        epList.Add(episode);
                    }
                    else
                    {
                        TraktLogger.Info("Unable to find match for episode: '{0} | airDate: {1}'", ep.ToString(), ep.AniDB_Episode.AirDateAsDate.ToString("yyyy-MM-dd"));
                    }
                }
            }

            if (epList.Count == 0)
            {
                TraktLogger.Warning("Unable to find any matching TVDb episodes for series '{0}', confirm Absolute Order and/or Episode Names and/or AirDates for episodes are correct on http://theTVDb.com and your database.", series.SeriesName);
                return null;
            }

            traktSync.EpisodeList = epList;
            return traktSync;
        }

        private TraktEpisodeScrobble CreateScrobbleData(FileLocal episode)
        {
            string seriesid = null;
            int seasonidx = 0;
            int episodeidx = 0;

            if (!GetTVDBEpisodeInfo(episode.AnimeEpisodes[0], out seriesid, out seasonidx, out episodeidx)) return null;

            // create scrobble data
            try
            {
                TraktEpisodeScrobble scrobbleData = new TraktEpisodeScrobble
                {
                    Title = episode.AniDB_File.AnimeSeries.SeriesName,
                    Year = GetStartYear(episode.AniDB_File.AnimeSeries),
                    Season = seasonidx.ToString(),
                    Episode = episodeidx.ToString(),
                    SeriesID = seriesid,
                    PluginVersion = TraktSettings.Version,
                    MediaCenter = "Mediaportal",
                    MediaCenterVersion = Assembly.GetEntryAssembly().GetName().Version.ToString(),
                    MediaCenterBuildDate = String.Empty,
                    UserName = TraktSettings.Username,
                    Password = TraktSettings.Password
                };

                return scrobbleData;
            }
            catch
            {
                TraktLogger.Error("Failed to create scrobble data for '{0}'", episode.ToString());
                return null;
            }
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Syncronize our collection on trakt
        /// </summary>
        /// <param name="episodes">local tvseries dbepisode list</param>
        /// <param name="mode">trakt sync mode</param>
        private void SyncLibrary(List<FileLocal> episodes, TraktSyncModes mode)
        {
            if (episodes.Count == 0) return;

            // get unique series ids
            var uniqueSeries = (from s in episodes where (s.AniDB_File != null && s.AniDB_File.AnimeSeries.TvDB_ID > 0) select s.AniDB_File.AnimeSeries.TvDB_ID).Distinct().ToList();

            if (uniqueSeries.Count == 0)
            {
                TraktLogger.Info("TVDb info not available for series, can not sync '{0}' with trakt.", mode.ToString());
            }

            // go over each series, can only send one series at a time
            foreach (int seriesid in uniqueSeries)
            {
                // There should only be one series
                List<AnimeSeries> series = AnimeSeries.GetSeriesWithSpecificTvDB(seriesid);
                if (series == null) continue;
                
                TraktLogger.Info("Synchronizing '{0}' episodes for series '{1}'.", mode.ToString(), series[0].ToString());

                // upload to trakt
                TraktEpisodeSync episodeSync = CreateSyncData(series[0], episodes);
                if (episodeSync != null)
                {
                    TraktResponse response = TraktAPI.TraktAPI.SyncEpisodeLibrary(episodeSync, mode);

                    // check for any error and log result
                    TraktAPI.TraktAPI.LogTraktResponse(response);

                    // wait a short period before uploading another series
                    Thread.Sleep(2000);
                }
            }
        }

        /// <summary>
        /// Confirm that my anime episode exists in our trakt collection
        /// </summary>
        /// <param name="traktEpisodes">trakt episode collection</param>
        /// <param name="episode">tvseries episode object</param>
        /// <returns>true if episode exists</returns>
        private bool TraktEpisodeExists(IEnumerable<TraktLibraryShow> traktEpisodes, FileLocal episode)
        {
            string seriesid = null;
            int seasonidx = 0;
            int episodeidx = 0;

            if (GetTVDBEpisodeInfo(episode.AnimeEpisodes[0], out seriesid, out seasonidx, out episodeidx))
            {
                var items = traktEpisodes.Where(s => s.SeriesId == seriesid &&
                                                                   s.Seasons.Where(e => e.Season == seasonidx &&
                                                                                        e.Episodes.Contains(episodeidx)).Count() == 1);
                return items.Count() == 1;
            }
            return false;
        }        

        /// <summary>
        /// Removes episodes on trakt that no longer exist in users database
        /// </summary>
        /// <param name="traktShows">trakt episode collection</param>
        /// <param name="episodes">list of local episodes</param>
        /// <param name="seriesID">tvdb series id of series</param>
        /// <returns>true if episode exists</returns>
        private TraktEpisodeSync GetEpisodesForTraktRemoval(TraktLibraryShow traktShow, List<FileLocal> episodes)
        {
            List<TraktEpisodeSync.Episode> episodeList = new List<TraktEpisodeSync.Episode>();

            foreach (var season in traktShow.Seasons)
            {
                foreach (var episode in season.Episodes)
                {


                    var query = episodes.Where(e => e.AniDB_File != null && e.AniDB_File.AnimeSeries.TvDB_ID.ToString() == traktShow.SeriesId &&
                                                    e.AniDB_File.AnimeSeries.TvDB_Episodes.Where(t => !string.IsNullOrEmpty(t.Filename) && t.SeasonNumber == season.Season && t.EpisodeNumber == episode).Count() == 1).ToList();

                    if (query.Count == 0)
                    {
                        // we dont have the episode anymore
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

        #region My Anime Events

        #endregion

    }
}
