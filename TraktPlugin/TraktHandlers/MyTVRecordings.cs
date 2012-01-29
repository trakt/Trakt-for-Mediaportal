using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Reflection;
using System.ComponentModel;
using System.Threading;
using MediaPortal.Player;
using TraktPlugin.TraktAPI;
using TraktPlugin.TraktAPI.DataStructures;
using TvDatabase;

namespace TraktPlugin.TraktHandlers
{
    class MyTVRecordings : ITraktHandler
    {
        #region Variables
        Timer TraktTimer;
        VideoInfo CurrentRecording = null;
        #endregion

        #region Constructor

        public MyTVRecordings(int priority)
        {
            Priority = priority;
        }

        #endregion

        #region ITraktHandler Members

        public string Name
        {
            get { return "My TV Recordings"; }
        }

        public int Priority { get; set; }

        public void SyncLibrary()
        {
            return;
        }

        public bool Scrobble(string filename)
        {
            StopScrobble();

            if (!g_Player.IsTVRecording) return false;

            // get recording details from tv database
            TvBusinessLayer layer = new TvBusinessLayer();
            Recording recording = layer.GetRecordingByFileName(filename);
            if (recording == null || string.IsNullOrEmpty(recording.Title))
            {
                TraktLogger.Info("Unable to get recording details from database.");
                return false;
            }

            // get year from title if available, some EPG entries contain this
            string title = null;
            string year = null;
            GetTitleAndYear(recording, out title, out year);

            CurrentRecording = new VideoInfo
            {
                Type = !string.IsNullOrEmpty(recording.EpisodeNum) || !string.IsNullOrEmpty(recording.SeriesNum) ? VideoType.Series : VideoType.Movie,
                Title = title,
                Year = year,
                SeasonIdx = recording.SeriesNum,
                EpisodeIdx = recording.EpisodeNum
            };

            if (CurrentRecording.Type == VideoType.Series)
                TraktLogger.Info("Detected tv-series '{0}' playing in TV Recordings", CurrentRecording.ToString());
            else
                TraktLogger.Info("Detected movie '{0}' playing in TV Recordings", CurrentRecording.ToString());

            #region scrobble timer
            TraktTimer = new Timer(new TimerCallback((stateInfo) =>
            {
                Thread.CurrentThread.Name = "Scrobble";

                VideoInfo videoInfo = stateInfo as VideoInfo;

                if (videoInfo.Type == VideoType.Series)
                {
                    ScrobbleEpisode(videoInfo, TraktScrobbleStates.watching);
                }
                else
                {
                    ScrobbleMovie(videoInfo, TraktScrobbleStates.watching);
                }
            }), CurrentRecording, 3000, 900000);
            #endregion

            return true;
        }

        public void StopScrobble()
        {
            if (TraktTimer != null)
                TraktTimer.Dispose();

            if (CurrentRecording == null) return;

            // get current progress of player
            double progress = 0.0;
            if (g_Player.Duration > 0.0) progress = (g_Player.CurrentPosition / g_Player.Duration) * 100.0;

            //TraktLogger.Debug(string.Format("Percentage of '{0}' watched is {1}%", CurrentRecording.Title, progress.ToString("N2")));

            // if recording is at least 80% complete, consider watched
            if (progress >= 80.0)
            {
                #region scrobble
                Thread scrobbleRecording = new Thread(delegate(object obj)
                {
                    VideoInfo videoInfo = obj as VideoInfo;
                    if (videoInfo == null) return;

                    TraktLogger.Info("Playback of tv-recording is considered watched '{0}'", videoInfo.ToString());
                 
                    if (videoInfo.Type == VideoType.Series)
                    {
                        ScrobbleEpisode(videoInfo, TraktScrobbleStates.scrobble);
                    }
                    else
                    {
                        ScrobbleMovie(videoInfo, TraktScrobbleStates.scrobble);
                    }
                })
                {
                    IsBackground = true,
                    Name = "Scrobble"
                };

                scrobbleRecording.Start(CurrentRecording);
                #endregion
            }
            else
            {
                #region cancel watching
                TraktLogger.Info("Stopped playback of tv-recording '{0}'", CurrentRecording.ToString());
                
                Thread cancelWatching = new Thread(delegate(object obj)
                {
                    VideoInfo videoInfo = obj as VideoInfo;
                    if (videoInfo == null) return;

                    if (videoInfo.Type == VideoType.Series)
                    {
                        TraktEpisodeScrobble scrobbleData = new TraktEpisodeScrobble { UserName = TraktSettings.Username, Password = TraktSettings.Password };
                        TraktResponse response = TraktAPI.TraktAPI.ScrobbleEpisodeState(scrobbleData, TraktScrobbleStates.cancelwatching);
                        TraktAPI.TraktAPI.LogTraktResponse(response);
                    }
                    else
                    {
                        TraktMovieScrobble scrobbleData = new TraktMovieScrobble { UserName = TraktSettings.Username, Password = TraktSettings.Password };
                        TraktResponse response = TraktAPI.TraktAPI.ScrobbleMovieState(scrobbleData, TraktScrobbleStates.cancelwatching);
                        TraktAPI.TraktAPI.LogTraktResponse(response);
                    }
                })
                {
                    IsBackground = true,
                    Name = "Cancel Watching"
                };

                cancelWatching.Start(CurrentRecording);
                #endregion
            }

            CurrentRecording = null;
        }

        #endregion

        #region Scrobble

        private void ScrobbleMovie(VideoInfo videoInfo, TraktScrobbleStates state)
        {
            TraktMovieScrobble scrobbleData = BasicHandler.CreateMovieScrobbleData(videoInfo);
            if (scrobbleData == null) return;
         
            // get duration in minutes
            double duration = g_Player.Duration / 60;
            double progress = 0.0;

            if (g_Player.Duration > 0.0) progress = (g_Player.CurrentPosition / g_Player.Duration) * 100.0;
            scrobbleData.Duration = Convert.ToInt32(duration).ToString();
            scrobbleData.Progress = Convert.ToInt32(progress).ToString();

            TraktResponse response = TraktAPI.TraktAPI.ScrobbleMovieState(scrobbleData, state);
            TraktAPI.TraktAPI.LogTraktResponse(response);
        }

        private void ScrobbleEpisode(VideoInfo videoInfo, TraktScrobbleStates state)
        {
            // get scrobble data to send to api
            TraktEpisodeScrobble scrobbleData = BasicHandler.CreateEpisodeScrobbleData(videoInfo);
            if (scrobbleData == null) return;

            // get duration in minutes
            double duration = g_Player.Duration / 60;
            double progress = 0.0;

            if (g_Player.Duration > 0.0) progress = (g_Player.CurrentPosition / g_Player.Duration) * 100.0;
            scrobbleData.Duration = Convert.ToInt32(duration).ToString();
            scrobbleData.Progress = Convert.ToInt32(progress).ToString();

            TraktResponse response = TraktAPI.TraktAPI.ScrobbleEpisodeState(scrobbleData, state);
            TraktAPI.TraktAPI.LogTraktResponse(response);
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Gets the title and year from a recording title
        /// Title should be in the form 'title (year)' or 'title [year]'
        /// </summary>
        private void GetTitleAndYear(Recording info, out string title, out string year)
        {
            Match regMatch = Regex.Match(info.Title, @"^(?<title>.+?)(?:\s*[\(\[](?<year>\d{4})[\]\)])?$");
            title = regMatch.Groups["title"].Value;
            year = regMatch.Groups["year"].Value;
        }

        #endregion
    }
}
