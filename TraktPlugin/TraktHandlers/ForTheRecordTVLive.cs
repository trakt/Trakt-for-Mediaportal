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
using MediaPortal.GUI.Library;
using TraktPlugin.TraktAPI;
using TraktPlugin.TraktAPI.DataStructures;
using ForTheRecord.UI.MediaPortal;
using ForTheRecord.Entities;
using ForTheRecord.ServiceAgents;

namespace TraktPlugin.TraktHandlers
{
    class ForTheRecordTVLive : ITraktHandler
    {
        #region Variables
        Timer TraktTimer;
        VideoInfo CurrentProgram = null;
        #endregion

        #region Constructor

        public ForTheRecordTVLive(int priority)
        {
            Priority = priority;
        }

        #endregion

        #region ITraktHandler Members

        public string Name
        {
            get { return "4TR TV Live"; }
        }

        public int Priority { get; set; }

        public void SyncLibrary()
        {
            return;
        }

        public bool Scrobble(string filename)
        {
            StopScrobble();

            if (!g_Player.IsTV) return false;

            try
            {
                CurrentProgram = GetCurrentProgram();
            }
            catch (Exception e)
            {
                TraktLogger.Error(e.Message);
                return false;
            }
            if (CurrentProgram == null) return false;
            CurrentProgram.IsScrobbling = true;

            if (CurrentProgram.Type == VideoType.Series)
                TraktLogger.Info("Detected tv-series '{0}' playing in 4TR TV Live", CurrentProgram.ToString());
            else
                TraktLogger.Info("Detected movie '{0}' playing in 4TR TV Live", CurrentProgram.ToString());

            #region scrobble timer
            TraktTimer = new Timer(new TimerCallback((stateInfo) =>
            {
                Thread.CurrentThread.Name = "Scrobble";

                // get the current program airing on tv now
                // this may have changed since last status update on trakt
                VideoInfo videoInfo = GetCurrentProgram();

                if (videoInfo != null)
                {
                    // if we are watching something different, 
                    // check if we should mark previous as watched
                    if (!videoInfo.Equals(CurrentProgram))
                    {
                        TraktLogger.Info("Detected new tv program has started '{0}' -> '{1}'", CurrentProgram.ToString(), videoInfo.ToString());
                        if (IsProgramWatched(CurrentProgram) && CurrentProgram.IsScrobbling)
                        {
                            ScrobbleProgram(CurrentProgram);
                        }
                        CurrentProgram.IsScrobbling = true;
                    }

                    // continue watching new program
                    // dont try to scrobble if previous attempt failed
                    if (CurrentProgram.IsScrobbling)
                    {
                        if (videoInfo.Type == VideoType.Series)
                        {
                            videoInfo.IsScrobbling = BasicHandler.ScrobbleEpisode(videoInfo, TraktScrobbleStates.watching);
                        }
                        else
                        {
                            videoInfo.IsScrobbling = BasicHandler.ScrobbleMovie(videoInfo, TraktScrobbleStates.watching);
                        }

                        // set current program to new program
                        CurrentProgram = videoInfo;
                    }
                }
            }), null, 1000, 300000);
            #endregion

            return true;
        }

        public void StopScrobble()
        {
            if (TraktTimer != null)
                TraktTimer.Dispose();

            if (CurrentProgram == null) return;

            if (IsProgramWatched(CurrentProgram) && CurrentProgram.IsScrobbling)
            {
                ScrobbleProgram(CurrentProgram);
            }
            else
            {
                #region cancel watching
                TraktLogger.Info("Stopped playback of tv-live '{0}'", CurrentProgram.ToString());

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

                cancelWatching.Start(CurrentProgram);
                #endregion
            }

            CurrentProgram = null;
        }

        #endregion

        #region Scrobble Watched
        private void ScrobbleProgram(VideoInfo program)
        {
            Thread scrobbleProgram = new Thread(delegate(object obj)
            {
                VideoInfo videoInfo = obj as VideoInfo;
                if (videoInfo == null) return;

                TraktLogger.Info("Playback of '{0}' in 4TR tv-live is considered watched.", videoInfo.ToString());

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

            scrobbleProgram.Start(program);
        }
        #endregion

        #region Helpers

        private bool IsProgramWatched(VideoInfo program)
        {
            // check if we have watched atleast 80% of the program
            // this wont be an exact calculation +- 5mins due to the scrobble timer
            double durationPlayed = DateTime.Now.Subtract(program.StartTime).TotalMinutes;
            double percentPlayed = 0.0;
            if (program.Runtime > 0.0) percentPlayed = durationPlayed / program.Runtime;

            return percentPlayed >= 0.8;
        }

        /// <summary>
        /// Gets the current program
        /// </summary>
        /// <returns></returns>
        private VideoInfo GetCurrentProgram()
        {
            VideoInfo videoInfo = new VideoInfo();
            
            // get current program details
            GuideProgram program = ForTheRecordMain.GetProgramAt(DateTime.Now);

            if (program == null || string.IsNullOrEmpty(program.Title))
            {
                TraktLogger.Info("Unable to get current program from database.");
                return null;
            }
            else
            {
                string title = null;
                string year = null;
                GetTitleAndYear(program, out title, out year);
             
                videoInfo = new VideoInfo
                {
                    Type = program.EpisodeNumber != null || program.SeriesNumber != null ? VideoType.Series : VideoType.Movie,
                    Title = title,
                    Year = year,
                    SeasonIdx = program.SeriesNumber == null ? null : program.SeriesNumber.ToString(),
                    EpisodeIdx = program.EpisodeNumber == null ? null : program.EpisodeNumber.ToString(),
                    StartTime = program.StartTime,
                    Runtime = GetRuntime(program)
                };
            }

            return videoInfo;
        }

        private double GetRuntime(GuideProgram program)
        {
            try
            {
                DateTime startTime = program.StartTime;
                DateTime endTime = program.StopTime;

                return endTime.Subtract(startTime).TotalMinutes;
            }
            catch
            {
                return 0.0;
            }
        }

        /// <summary>
        /// Gets the title and year from a program title
        /// Title should be in the form 'title (year)' or 'title [year]'
        /// </summary>
        private void GetTitleAndYear(GuideProgram program, out string title, out string year)
        {
            Match regMatch = Regex.Match(program.Title, @"^(?<title>.+?)(?:\s*[\(\[](?<year>\d{4})[\]\)])?$");
            title = regMatch.Groups["title"].Value;
            year = regMatch.Groups["year"].Value;
        }

        #endregion
    }
}
