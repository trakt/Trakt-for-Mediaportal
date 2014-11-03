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
using TraktPlugin.GUI;
using TraktPlugin.TraktAPI.v1;
using TraktPlugin.TraktAPI.v1.DataStructures;
using ArgusTV.DataContracts;
using ArgusTV.ServiceAgents;

namespace TraktPlugin.TraktHandlers
{
    class ArgusTVRecordings : ITraktHandler
    {
        #region Variables
        Timer TraktTimer;
        VideoInfo CurrentRecording = null;
        #endregion

        #region Constructor

        public ArgusTVRecordings(int priority)
        {
            Priority = priority;
        }

        #endregion

        #region ITraktHandler Members

        public string Name
        {
            get { return "Argus TV Recordings"; }
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
            ControlServiceAgent layer = new ControlServiceAgent();
            Recording recording = layer.GetRecordingByFileName(filename);
            if (recording == null || string.IsNullOrEmpty(recording.Title))
            {
                TraktLogger.Info("Unable to get recording details from argus database.");
                return false;
            }

            // get year from title if available, some EPG entries contain this
            string title = null;
            string year = null;
            GetTitleAndYear(recording, out title, out year);

            CurrentRecording = new VideoInfo
            {
                Type = recording.EpisodeNumber != null || recording.SeriesNumber != null ? VideoType.Series : VideoType.Movie,
                Title = title,
                Year = year,
                SeasonIdx = recording.SeriesNumber == null ? null : recording.SeriesNumber.ToString(),
                EpisodeIdx = recording.EpisodeNumber == null ? null : recording.EpisodeNumber.ToString(),
                IsScrobbling = true
            };

            if (CurrentRecording.Type == VideoType.Series)
                TraktLogger.Info("Detected tv-series '{0}' playing in Argus TV-Recordings", CurrentRecording.ToString());
            else
                TraktLogger.Info("Detected movie '{0}' playing in Argus TV-Recordings", CurrentRecording.ToString());

            #region scrobble timer
            TraktTimer = new Timer(new TimerCallback((stateInfo) =>
            {
                Thread.CurrentThread.Name = "Scrobble";

                VideoInfo videoInfo = stateInfo as VideoInfo;

                // maybe the program does not exist on trakt
                // ignore in future if it failed previously
                if (videoInfo.IsScrobbling)
                {
                    if (videoInfo.Type == VideoType.Series)
                    {
                        videoInfo.IsScrobbling = BasicHandler.ScrobbleEpisode(videoInfo, TraktScrobbleStates.watching);
                    }
                    else
                    {
                        videoInfo.IsScrobbling = BasicHandler.ScrobbleMovie(videoInfo, TraktScrobbleStates.watching);
                    }

                    if (videoInfo.Equals(CurrentRecording))
                        CurrentRecording.IsScrobbling = videoInfo.IsScrobbling;
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

            TraktLogger.Debug("Current Position: {0}, Duration: {1}", g_Player.CurrentPosition.ToString(), g_Player.Duration.ToString());
            TraktLogger.Debug(string.Format("Percentage of '{0}' watched is {1}%", CurrentRecording.Title, progress > 100.0 ? "100" : progress.ToString("N2")));

            // if recording is at least 80% complete, consider watched
            // consider watched with invalid progress as well, we should never be exactly 0.0
            if ((progress == 0.0 || progress >= 80.0) && CurrentRecording.IsScrobbling)
            {
                // Show rate dialog
                ShowRateDialog(CurrentRecording);

                #region scrobble
                Thread scrobbleRecording = new Thread(delegate(object obj)
                {
                    VideoInfo videoInfo = obj as VideoInfo;
                    if (videoInfo == null) return;

                    TraktLogger.Info("Playback of '{0}' in Argus tv-recording is considered watched.", videoInfo.ToString());

                    if (videoInfo.Type == VideoType.Series)
                    {
                        BasicHandler.ScrobbleEpisode(videoInfo, TraktScrobbleStates.scrobble);
                    }
                    else
                    {
                        BasicHandler.ScrobbleMovie(videoInfo, TraktScrobbleStates.scrobble);
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
                TraktLogger.Info("Stopped playback of Argus tv-recording '{0}'", CurrentRecording.ToString());

                Thread cancelWatching = new Thread(delegate(object obj)
                {
                    VideoInfo videoInfo = obj as VideoInfo;
                    if (videoInfo == null) return;

                    if (videoInfo.Type == VideoType.Series)
                    {
                        TraktEpisodeScrobble scrobbleData = new TraktEpisodeScrobble { UserName = TraktSettings.Username, Password = TraktSettings.Password };
                        TraktResponse response = TraktAPI.v1.TraktAPI.ScrobbleEpisodeState(scrobbleData, TraktScrobbleStates.cancelwatching);
                        TraktLogger.LogTraktResponse(response);
                    }
                    else
                    {
                        TraktMovieScrobble scrobbleData = new TraktMovieScrobble { UserName = TraktSettings.Username, Password = TraktSettings.Password };
                        TraktResponse response = TraktAPI.v1.TraktAPI.ScrobbleMovieState(scrobbleData, TraktScrobbleStates.cancelwatching);
                        TraktLogger.LogTraktResponse(response);
                    }
                })
                {
                    IsBackground = true,
                    Name = "CancelWatching"
                };

                cancelWatching.Start(CurrentRecording);
                #endregion
            }

            CurrentRecording = null;
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

        /// <summary>
        /// Shows the Rate Dialog after playback has ended
        /// </summary>
        /// <param name="episode">The item being rated</param>
        private void ShowRateDialog(VideoInfo videoInfo)
        {
            if (!TraktSettings.ShowRateDialogOnWatched) return;     // not enabled            

            TraktLogger.Debug("Showing rate dialog for '{0}'", videoInfo.Title);

            new Thread((o) =>
            {
                VideoInfo itemToRate = o as VideoInfo;
                if (itemToRate == null) return;

                int rating = 0;

                if (itemToRate.Type == VideoType.Series)
                {
                    TraktRateEpisode rateObject = new TraktRateEpisode
                    {
                        Title = itemToRate.Title,
                        Year = itemToRate.Year,
                        Episode = itemToRate.EpisodeIdx,
                        Season = itemToRate.SeasonIdx,
                        UserName = TraktSettings.Username,
                        Password = TraktSettings.Password
                    };
                    // get the rating submitted to trakt
                    //TODOrating = int.Parse(GUIUtils.ShowRateDialog<TraktRateEpisode>(rateObject));
                }
                else if (itemToRate.Type == VideoType.Movie)
                {
                    TraktRateMovie rateObject = new TraktRateMovie
                    {
                        Title = itemToRate.Title,
                        Year = itemToRate.Year,
                        UserName = TraktSettings.Username,
                        Password = TraktSettings.Password
                    };
                    // get the rating submitted to trakt
                    //TODOrating = int.Parse(GUIUtils.ShowRateDialog<TraktRateMovie>(rateObject));
                }

                if (rating > 0)
                {
                    TraktLogger.Debug("Rating {0} as {1}/10", itemToRate.Title, rating.ToString());
                    // note: no user rating field to set
                }
            })
            {
                Name = "Rate",
                IsBackground = true
            }.Start(videoInfo);
        }
        #endregion
    }
}