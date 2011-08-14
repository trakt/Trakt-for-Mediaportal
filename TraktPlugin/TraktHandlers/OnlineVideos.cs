using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using OnlineVideos;
using OnlineVideos.MediaPortal1;
using OnlineVideos.MediaPortal1.Player;
using MediaPortal.Configuration;
using MediaPortal.GUI.Library;
using MediaPortal.Player;
using System.Threading;
using TraktPlugin.GUI;
using TraktPlugin.TraktAPI;
using TraktPlugin.TraktAPI.DataStructures;

namespace TraktPlugin.TraktHandlers
{
    class OnlineVideos : ITraktHandler
    {
        GUIOnlineVideos ovObject = null;
        ITrackingInfo currentVideo = null;
        Timer TraktTimer = null;

        #region Constructor

        public OnlineVideos(int priority)
        {
            // check if plugin exists otherwise plugin could accidently get added to list
            string pluginFilename = Path.Combine(Config.GetSubFolder(Config.Dir.Plugins, "Windows"), "OnlineVideos.MediaPortal1.dll");
            if (!File.Exists(pluginFilename))
                throw new FileNotFoundException("Plugin not found!");
            else
            {
                FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(pluginFilename);
                string version = fvi.ProductVersion;
                if (new Version(version) < new Version(0,31,0,0))
                    throw new FileLoadException("Plugin does not meet minimum requirements!");
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
            if (currentVideo == null) return false;

            if (currentVideo.VideoKind == VideoKind.TvSeries)
                TraktLogger.Info("Detected tv series '{0} - {1}x{2}' playing in OnlineVideos", currentVideo.Title, currentVideo.Season.ToString(), currentVideo.Episode.ToString());
            else
                TraktLogger.Info("Detected movie '{0}' playing in OnlineVideos", currentVideo.Title);

            #region scrobble timer
            TraktTimer = new Timer(new TimerCallback((stateInfo) =>
            {
                ITrackingInfo videoInfo = stateInfo as ITrackingInfo;

                // get duration in minutes
                double duration = g_Player.Duration / 60;
                double progress = 0.0;

                // get current progress of player
                if (g_Player.Duration > 0.0) progress = (g_Player.CurrentPosition / g_Player.Duration) * 100.0;
                    
                TraktEpisodeScrobble scrobbleEpisodeData = null;
                TraktMovieScrobble scrobbleMovieData = null;
                TraktResponse response = null;

                if (videoInfo.VideoKind == VideoKind.TvSeries)
                {
                    scrobbleEpisodeData = CreateEpisodeScrobbleData(videoInfo);
                    if (scrobbleEpisodeData == null) return;
                    scrobbleEpisodeData.Duration = Convert.ToInt32(duration).ToString();
                    scrobbleEpisodeData.Progress = Convert.ToInt32(progress).ToString();
                    response = TraktAPI.TraktAPI.ScrobbleEpisodeState(scrobbleEpisodeData, TraktScrobbleStates.watching);
                }
                else
                {
                    scrobbleMovieData = CreateMovieScrobbleData(videoInfo);
                    if (scrobbleMovieData == null) return;
                    scrobbleMovieData.Duration = Convert.ToInt32(duration).ToString();
                    scrobbleMovieData.Progress = Convert.ToInt32(progress).ToString();
                    response = TraktAPI.TraktAPI.ScrobbleMovieState(scrobbleMovieData, TraktScrobbleStates.watching);
                }

                TraktAPI.TraktAPI.LogTraktResponse(response);
            }), currentVideo, 3000, 900000);
            #endregion

            return true;
        }

        public void StopScrobble()
        {
            currentVideo = null;

            if (TraktTimer != null)
                TraktTimer.Dispose();
        }

        #endregion

        #region Player Events

        /// <summary>
        /// Event gets triggered on playback events in OnlineVideos
        /// The TrackVideoPlayback event gets fired on Playback Start, Playback Ended
        /// and Playback Stopped (if percentage watched is greater than 0.8).
        /// </summary>
        private void TrackVideoPlayback(ITrackingInfo info, double percentPlayed)
        {
            if (info.VideoKind == VideoKind.Movie || info.VideoKind == VideoKind.TvSeries)
            {
                // Started Playback
                // Bug in OnlineVideos 0.31 reports incorrect percentage
                if (percentPlayed > 1.0) percentPlayed = 1 / percentPlayed;
                if (percentPlayed < 0.8)
                {
                    currentVideo = info;
                    return;
                }

                // Playback Ended or Stopped and Considered Watched
                // TrackVideoPlayback event only gets fired on Stopped if > 80% watched
                TraktLogger.Info("Playback of '{0}' is considered watched at {1:0.00}%", info.Title, (percentPlayed * 100).ToString());

                Thread stopThread = new Thread(delegate(object o)
                {
                    ITrackingInfo videoInfo = o as ITrackingInfo;
                    
                    // duration in minutes
                    double duration = g_Player.Duration / 60;
                    double progress = 100.0;

                    TraktEpisodeScrobble scrobbleEpisodeData = null;
                    TraktMovieScrobble scrobbleMovieData = null;
                    TraktResponse response = null;

                    if (videoInfo.VideoKind == VideoKind.TvSeries)
                    {
                        scrobbleEpisodeData = CreateEpisodeScrobbleData(videoInfo);
                        if (scrobbleEpisodeData == null) return;
                        scrobbleEpisodeData.Duration = Convert.ToInt32(duration).ToString();
                        scrobbleEpisodeData.Progress = Convert.ToInt32(progress).ToString();
                        response = TraktAPI.TraktAPI.ScrobbleEpisodeState(scrobbleEpisodeData, TraktScrobbleStates.scrobble);
                    }
                    else
                    {
                        scrobbleMovieData = CreateMovieScrobbleData(videoInfo);
                        if (scrobbleMovieData == null) return;
                        scrobbleMovieData.Duration = Convert.ToInt32(duration).ToString();
                        scrobbleMovieData.Progress = Convert.ToInt32(progress).ToString();
                        response = TraktAPI.TraktAPI.ScrobbleMovieState(scrobbleMovieData, TraktScrobbleStates.scrobble);
                    }

                    TraktAPI.TraktAPI.LogTraktResponse(response);
                })
                {
                    IsBackground = true,
                    Name = "Scrobble"
                };

                stopThread.Start(info);
            }
        }
        
        #endregion

        #region Data Creators

        private TraktEpisodeScrobble CreateEpisodeScrobbleData(ITrackingInfo info)
        {
            try
            {
                // create scrobble data
                TraktEpisodeScrobble scrobbleData = new TraktEpisodeScrobble
                {
                    Title = info.Title,
                    Year = info.Year > 1900 ? info.Year.ToString() : null,
                    Season = info.Season.ToString(),
                    Episode = info.Episode.ToString(),
                    SeriesID = info.ID_TVDB,
                    IMDBID = info.ID_IMDB,
                    PluginVersion = TraktSettings.Version,
                    MediaCenter = "Mediaportal",
                    MediaCenterVersion = Assembly.GetEntryAssembly().GetName().Version.ToString(),
                    MediaCenterBuildDate = String.Empty,
                    UserName = TraktSettings.Username,
                    Password = TraktSettings.Password
                };

                return scrobbleData;
            }
            catch (Exception e)
            {
                TraktLogger.Error("Error creating scrobble data: {0}", e.Message);
                return null;
            }
        }

        private TraktMovieScrobble CreateMovieScrobbleData(ITrackingInfo info)
        {
            try
            {
                // create scrobble data
                TraktMovieScrobble scrobbleData = new TraktMovieScrobble
                {
                    Title = info.Title,
                    Year = info.Year > 1900 ? info.Year.ToString() : null,
                    IMDBID = info.ID_IMDB,
                    TMDBID = info.ID_TMDB,
                    PluginVersion = TraktSettings.Version,
                    MediaCenter = "Mediaportal",
                    MediaCenterVersion = Assembly.GetEntryAssembly().GetName().Version.ToString(),
                    MediaCenterBuildDate = String.Empty,
                    UserName = TraktSettings.Username,
                    Password = TraktSettings.Password
                };

                return scrobbleData;
            }
            catch (Exception e)
            {
                TraktLogger.Error("Error creating scrobble data: {0}", e.Message);
                return null;
            }
        }

        #endregion

        #region Other Public Methods

        public void DisposeEvents()
        {
            TraktLogger.Debug("Removing Hooks from OnlineVideos");
            ovObject.TrackVideoPlayback -= new GUIOnlineVideos.TrackVideoPlaybackHandler(TrackVideoPlayback);
            ovObject = null;
        }

        #endregion
    }
}
