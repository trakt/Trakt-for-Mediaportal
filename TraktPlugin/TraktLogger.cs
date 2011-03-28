using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using MediaPortal.Configuration;

namespace TraktPlugin
{
    public static class TraktLogger
    {
        private static string logFilename = Config.GetFile(Config.Dir.Log,"TraktPlugin.log");

        static TraktLogger()
        {
            //Delete our old log file
            if (File.Exists(logFilename))
                File.Delete(logFilename);
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
            return DateTime.Now + " [{0}] {1}";
        }

        private static void writeToFile(String log)
        {
            StreamWriter sw = File.AppendText(logFilename);
            sw.WriteLine(log);
            sw.Close();
        }
    }
}
