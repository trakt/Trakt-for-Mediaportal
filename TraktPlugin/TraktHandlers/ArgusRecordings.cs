using System;
using ArgusTV.DataContracts;
using ArgusTV.ServiceAgents;
using MediaPortal.Player;
using TraktPlugin.Extensions;

namespace TraktPlugin.TraktHandlers
{
    class ArgusTVRecordings : ITraktHandler
    {
        #region Variables

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
            BasicHandler.GetTitleAndYear(recording.Title, out title, out year);

            CurrentRecording = new VideoInfo
            {
                Type = recording.EpisodeNumber != null || recording.SeriesNumber != null ? VideoType.Series : VideoType.Movie,
                Title = title,
                Year = year,
                SeasonIdx = recording.SeriesNumber == null ? null : recording.SeriesNumber.ToString(),
                EpisodeIdx = recording.EpisodeNumber == null ? null : recording.EpisodeNumber.ToString(),
                IsScrobbling = true
            };

            TraktLogger.Info("Current program details. Title='{0}', Year='{1}', Season='{2}', Episode='{3}', StartTime='{4}', Runtime='{5}'", CurrentRecording.Title, CurrentRecording.Year.ToLogString(), CurrentRecording.SeasonIdx.ToLogString(), CurrentRecording.EpisodeIdx.ToLogString(), CurrentRecording.StartTime == null ? "<empty>" : CurrentRecording.StartTime.ToString(), CurrentRecording.Runtime);

            if (CurrentRecording.Type == VideoType.Series)
            {
                TraktLogger.Info("Detected tv show playing in Argus TV Recordings. Title = '{0}'", CurrentRecording.ToString());
            }
            else
            {
                TraktLogger.Info("Detected movie playing in Argus TV Recordings. Title = '{0}'", CurrentRecording.ToString());
            }

            BasicHandler.StartScrobble(CurrentRecording);

            return true;
        }

        public void StopScrobble()
        {
            if (CurrentRecording == null) return;

            // get current progress of player
            bool watched = false;
            double progress = 0.0;
            if (g_Player.Duration > 0.0)
                progress = Math.Round((g_Player.CurrentPosition / g_Player.Duration) * 100.0, 2);

            TraktLogger.Info("Video recording has stopped, checking progress. Title = '{0}', Current Position = '{1}', Duration = '{2}', Progress = '{3}%'",
                               CurrentRecording.Title, g_Player.CurrentPosition.ToString(), g_Player.Duration.ToString(), progress > 100.0 ? "100" : progress.ToString());

            // if recording is at least 80% complete, consider watched
            // consider watched with invalid progress as well, we should never be exactly 0.0
            if (progress == 0.0 || progress >= 80.0)
            {
                watched = true;

                // Show rate dialog
                BasicHandler.ShowRateDialog(CurrentRecording);
            }

            BasicHandler.StopScrobble(CurrentRecording, watched);

            CurrentRecording = null;
        }

        public void SyncProgress()
        {
            return;
        }

        #endregion
    }
}