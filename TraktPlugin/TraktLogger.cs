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
            try
            {
                if (response == null || (response as TraktResponse).Status == null)
                {
                    // server is probably temporarily unavailable
                    // return true even though it failed, so we can try again
                    // currently the return value is only being used in livetv/recordings
                    TraktLogger.Error("Response from server was unexpected.");
                    return true;
                }

                // check response error status
                if ((response as TraktResponse).Status != "success")
                {
                    if ((response as TraktResponse).Error == "The remote server returned an error: (401) Unauthorized.")
                    {
                        TraktLogger.Error("401 Unauthorized, Please check your Username and Password");
                    }
                    else
                        TraktLogger.Error((response as TraktResponse).Error);

                    return false;
                }
                else
                {
                    // success
                    if (!string.IsNullOrEmpty((response as TraktResponse).Message))
                    {
                        TraktLogger.Info("Response: {0}", (response as TraktResponse).Message);
                    }
                    else
                    {
                        // no message returned on movie sync success
                        if ((response is TraktSyncResponse))
                        {
                            string message = "Response: Items Inserted: {0}, Items Already Exist: {1}, Items Skipped: {2}";
                            TraktLogger.Info(message, (response as TraktSyncResponse).Inserted, (response as TraktSyncResponse).AlreadyExist, (response as TraktSyncResponse).Skipped);
                        }
                    }
                    return true;
                }
            }
            catch (Exception)
            {
                TraktLogger.Info("Response: {0}", "Failed to interpret response from server");
                return false;
            }
        }
    }
}
