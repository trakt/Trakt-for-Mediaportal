using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using MediaPortal.Configuration;
using TraktPlugin.TraktAPI.DataStructures;

namespace TraktPlugin
{
    static class TraktLogger
    {
        private static string logFilename = Config.GetFile(Config.Dir.Log,"TraktPlugin.log");
        private static string backupFilename = Config.GetFile(Config.Dir.Log, "TraktPlugin.bak");
        private static Object lockObject = new object();

        static TraktLogger()
        {
            // default logging before we load settings
            TraktSettings.LogLevel = 2;

            if (File.Exists(logFilename))
            {
                if (File.Exists(backupFilename))
                {
                    try
                    {
                        File.Delete(backupFilename);
                    }
                    catch
                    {
                        Error("Failed to remove old backup log");
                    }
                }
                try
                {
                    File.Move(logFilename, backupFilename);
                }
                catch
                {
                    Error("Failed to move logfile to backup");
                }
            }

            // listen to webclient events from the TraktAPI so we can provide useful logging            
            TraktAPI.TraktAPI.OnDataSend += new TraktAPI.TraktAPI.OnDataSendDelegate(TraktAPI_OnDataSend);
            TraktAPI.TraktAPI.OnDataError += new TraktAPI.TraktAPI.OnDataErrorDelegate(TraktAPI_OnDataError);
            TraktAPI.TraktAPI.OnDataReceived += new TraktAPI.TraktAPI.OnDataReceivedDelegate(TraktAPI_OnDataReceived);
        }

        internal static void Info(String log)
        {
            if(TraktSettings.LogLevel >= 2)
                writeToFile(String.Format(createPrefix(), "INFO", log));
        }

        internal static void Info(String format, params Object[] args)
        {
            Info(String.Format(format, args));
        }

        internal static void Debug(String log)
        {
            if(TraktSettings.LogLevel >= 3)
                writeToFile(String.Format(createPrefix(), "DEBG", log));
        }

        internal static void Debug(String format, params Object[] args)
        {
            Debug(String.Format(format, args));
        }

        internal static void Error(String log)
        {
            if(TraktSettings.LogLevel >= 0)
                writeToFile(String.Format(createPrefix(), "ERR ", log));
        }

        internal static void Error(String format, params Object[] args)
        {
            Error(String.Format(format, args));
        }

        internal static void Warning(String log)
        {
            if(TraktSettings.LogLevel >= 1)
                writeToFile(String.Format(createPrefix(), "WARN", log));
        }

        internal static void Warning(String format, params Object[] args)
        {
            Warning(String.Format(format, args));
        }

        private static String createPrefix()
        {
            return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + " [{0}] " + String.Format("[{0}][{1}]", Thread.CurrentThread.Name, Thread.CurrentThread.ManagedThreadId.ToString().PadLeft(2,'0')) +  ": {1}";
        }

        private static void writeToFile(String log)
        {
            try
            {
                lock (lockObject)
                {
                    StreamWriter sw = File.AppendText(logFilename);
                    sw.WriteLine(log);
                    sw.Close();
                }
            }
            catch
            {
                Error("Failed to write out to log");
            }
        }

        private static void TraktAPI_OnDataSend(string address, string data)
        {
            if (!string.IsNullOrEmpty(data))
            {
                TraktLogger.Debug("Address: {0}, Post: {1}", address, data);
            }
            else
            {
                TraktLogger.Debug("Address: {0}", address);
            }
        }

        private static void TraktAPI_OnDataReceived(string response)
        {
            TraktLogger.Debug("Response: {0}", response ?? "null");
        }

        private static void TraktAPI_OnDataError(string error)
        {
            TraktLogger.Error("WebException: {0}", error);
        }

        /// <summary>
        /// Logs the result of Trakt api call
        /// </summary>
        /// <typeparam name="T">Response Type of message</typeparam>
        /// <param name="response">The response object holding the message to log</param>
        internal static bool LogTraktResponse<T>(T response)
        {
            if (response == null)
            {
                // we already log errors which would normally not be able to be deserialised
                // currently the return value is only being used in livetv/recordings
                return true;
            }

            try
            {
                string formatString = string.Empty;

                // success
                if (response is TraktSyncResponse)
                {
                    string itemsAdded = null;
                    string itemsRemoved = null;
                    string itemsExisting = null;
                        
                    var res = response as TraktSyncResponse;

                    if (res.Added != null)
                    {
                        itemsAdded = string.Format("Movies Added = '{0}', Shows Added = '{1}', Seasons Added = '{2}', Episodes Added = '{3}'. ", res.Added.Movies, res.Added.Shows, res.Added.Seasons, res.Added.Episodes);
                        formatString += itemsAdded;
                    }

                    if (res.Deleted != null)
                    {
                        itemsRemoved = string.Format("Movies Removed = '{0}', Shows Removed = '{1}', Seasons Removed = '{2}', Episodes Removed = '{3}'. ", res.Deleted.Movies, res.Deleted.Shows, res.Deleted.Seasons, res.Deleted.Episodes);
                        formatString += itemsRemoved;
                    }

                    if (res.Existing != null)
                    {
                        itemsExisting = string.Format("Movies Already Exist = '{0}', Shows Already Exist = '{1}', Seasons Already Exist = '{2}', Episodes Already Exist = '{3}'", res.Existing.Movies, res.Existing.Shows, res.Existing.Seasons, res.Existing.Episodes);
                        formatString += itemsExisting;
                    }

                    TraktLogger.Info("Response: {0}", formatString);
                }
                else if (response is TraktScrobbleResponse)
                {
                    var res = response as TraktScrobbleResponse;

                    if (res.Movie != null)
                    {
                        formatString = string.Format("Action = '{0}', Progress = '{1}%', Movie Title = '{2}', Year = '{3}', IMDb ID = '{4}', TMDb ID = '{5}', Trakt ID = '{6}'", res.Action, res.Progress, res.Movie.Title, res.Movie.Year.HasValue ? res.Movie.Year.ToString() : "<empty>", res.Movie.Ids.ImdbId ?? "<empty>", res.Movie.Ids.TmdbId.HasValue ? res.Movie.Ids.TmdbId.ToString() : "<empty>", res.Movie.Ids.Id.ToString());
                    }
                    else
                    {
                        formatString = string.Format("Action = '{0}', Progress = '{1}%', Episode Title = '{2} - {3}x{4} - {5}', IMDb ID = '{6}', TMDb ID = '{7}', TVDb ID = '{8}', Trakt ID = '{9}'", res.Action, res.Progress, res.Show.Title, res.Episode.Season, res.Episode.Number, res.Episode.Title ?? "<empty>", res.Episode.Ids.ImdbId ?? "<empty>", res.Episode.Ids.TmdbId.HasValue ? res.Episode.Ids.TmdbId.ToString() : "<empty>", res.Episode.Ids.TvdbId.HasValue ? res.Episode.Ids.TvdbId.ToString() : "<empty>", res.Episode.Ids.Id.ToString());
                    }

                    TraktLogger.Info("Response: {0}", formatString);
                }

                return true;
            }
            catch (Exception)
            {
                TraktLogger.Info("Response: Failed to interpret response from server");
                return false;
            }
        }
    }
}
