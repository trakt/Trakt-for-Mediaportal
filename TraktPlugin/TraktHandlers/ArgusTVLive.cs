using System;
using System.Threading;
using ArgusTV.DataContracts;
using ArgusTV.UI.MediaPortal;
using MediaPortal.Player;
using TraktPlugin.Extensions;

namespace TraktPlugin.TraktHandlers
{
    class ArgusTVLive : ITraktHandler
    {
        #region Variables
        Timer TraktTimer;
        VideoInfo CurrentProgram = null;
        #endregion

        #region Constructor

        public ArgusTVLive(int priority)
        {
            Priority = priority;
        }

        #endregion

        #region ITraktHandler Members

        public string Name
        {
            get { return "Argus TV Live"; }
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
            {
                TraktLogger.Info("Detected tv show playing on Argus Live TV. Title = '{0}'", CurrentProgram.ToString());
            }
            else
            {
                TraktLogger.Info("Detected movie playing on Argus Live TV. Title = '{0}'", CurrentProgram.ToString());
            }

            #region Scrobble Timer

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
                        TraktLogger.Info("Detected new tv program has started. Previous Program =  '{0}', New Program = '{1}'", CurrentProgram.ToString(), videoInfo.ToString());
                        if (IsProgramWatched(CurrentProgram) && CurrentProgram.IsScrobbling)
                        {
                            TraktLogger.Info("Playback of program on Live TV is considered watched. Title = '{0}'", CurrentProgram.ToString());
                            BasicHandler.StopScrobble(CurrentProgram, true);
                        }
                        CurrentProgram.IsScrobbling = true;
                    }

                    // continue watching new program
                    // dont try to scrobble if previous attempt failed
                    if (CurrentProgram.IsScrobbling)
                    {
                        if (videoInfo.Type == VideoType.Series)
                        {
                            videoInfo.IsScrobbling = BasicHandler.StartScrobbleEpisode(videoInfo);
                        }
                        else
                        {
                            videoInfo.IsScrobbling = BasicHandler.StartScrobbleMovie(videoInfo);
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
                TraktLogger.Info("Playback of program on Live TV is considered watched. Title = '{0}'", CurrentProgram.ToString());
                BasicHandler.StopScrobble(CurrentProgram, true);
            }
            else
            {
                BasicHandler.StopScrobble(CurrentProgram);
            }

            CurrentProgram = null;
        }

        public void SyncProgress()
        {
            return;
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Checks if the current program is considered watched
        /// </summary>
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
        private VideoInfo GetCurrentProgram()
        {
            VideoInfo videoInfo = new VideoInfo();

            // get current program details
            GuideProgram program = PluginMain.GetProgramAt(DateTime.Now);

            if (program == null || string.IsNullOrEmpty(program.Title))
            {
                TraktLogger.Info("Unable to get current program from database");
                return null;
            }
            else
            {
                string title = null;
                string year = null;
                BasicHandler.GetTitleAndYear(program.Title, out title, out year);

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

                TraktLogger.Info("Current program details. Title='{0}', Year='{1}', Season='{2}', Episode='{3}', StartTime='{4}', Runtime='{5}'", videoInfo.Title, videoInfo.Year.ToLogString(), videoInfo.SeasonIdx.ToLogString(), videoInfo.EpisodeIdx.ToLogString(), videoInfo.StartTime == null ? "<empty>" : videoInfo.StartTime.ToString(), videoInfo.Runtime);
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
        
        #endregion
    }
}
