using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using MediaPortal.Configuration;

namespace TraktPlugin
{
    public static class TraktLogger
    {
        private static string logFilename = Config.GetFile(Config.Dir.Log,"TraktPlugin.log");
        private static string backupFilename = Config.GetFile(Config.Dir.Log, "TraktPlugin.bak");

        static TraktLogger()
        {
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
        }

        public static void Info(String log)
        {
            writeToFile(String.Format(createPrefix(), "Info", log));
        }

        public static void Info(String format, params String[] args)
        {
            Info(String.Format(format, args));
        }

        public static void Debug(String log)
        {
            writeToFile(String.Format(createPrefix(), "Debug", log));
        }

        public static void Debug(String format, params String[] args)
        {
            Debug(String.Format(format, args));
        }

        public static void Error(String log)
        {
            writeToFile(String.Format(createPrefix(), "Error", log));
        }

        public static void Error(String format, params String[] args)
        {
            Error(String.Format(format, args));
        }

        private static String createPrefix()
        {
            return DateTime.Now + String.Format("[{0}][{1}]", Thread.CurrentThread.Name, Thread.CurrentThread.ManagedThreadId) +  "[{0}] {1}";
        }

        private static void writeToFile(String log)
        {
            try
            {
                StreamWriter sw = File.AppendText(logFilename);
                sw.WriteLine(log);
                sw.Close();
            }
            catch
            {
                Error("Failed to write out to log");
            }
        }
    }
}
