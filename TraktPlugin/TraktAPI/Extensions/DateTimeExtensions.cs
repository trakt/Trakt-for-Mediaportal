using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using TraktPlugin.Properties;

namespace TraktPlugin.TraktAPI.Extensions
{
    public static class DateTimeExtensions
    {
        /// <summary>
        /// Date Time extension method to return a unix epoch
        /// time as a long
        /// </summary>
        /// <returns> A long representing the Date Time as the number
        /// of seconds since 1/1/1970</returns>
        public static long ToEpoch(this DateTime dt)
        {
            return (long)(dt - new DateTime(1970, 1, 1)).TotalSeconds;
        }

        /// <summary>
        /// Long extension method to convert a Unix epoch
        /// time to a standard C# DateTime object.
        /// </summary>
        /// <returns>A DateTime object representing the unix
        /// time as seconds since 1/1/1970</returns>
        public static DateTime FromEpoch(this long unixTime)
        {
            return new DateTime(1970, 1, 1).AddSeconds(unixTime);
        }

        /// <summary>
        /// Converts string DateTime to ISO8601 format
        /// 2014-09-01T09:10:11.000Z
        /// </summary>
        /// <param name="dt">DateTime as string</param>
        /// <param name="hourShift">Number of hours to shift original time</param>
        /// <returns>ISO8601 Timestamp</returns>
        public static string ToISO8601(this string dt, double hourShift = 0, bool isLocal = false)
        {
            DateTime date;
            if (DateTime.TryParse(dt, out date))
            {
                if (isLocal)
                    return date.AddHours(hourShift).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
                else
                    return date.AddHours(hourShift).ToString("yyyy-MM-ddTHH:mm:ssZ");
            }

            return DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
        }
        public static string ToISO8601(this DateTime dt, double hourShift = 0)
        {
            string retValue = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

            if (dt == null)
                return retValue;

            return dt.AddHours(hourShift).ToString("yyyy-MM-ddTHH:mm:ssZ");
        }

        public static DateTime FromISO8601(this string dt)
        {
            DateTime date;
            if (DateTime.TryParse(dt, out date))
            {
                return date;
            }

            return new DateTime();
        }

        /// <summary>
        /// Returns the corresponding Olsen timezone e.g. 'Atlantic/Canary' into a Windows timezone e.g. 'GMT Standard Time'
        /// </summary>
        public static string OlsenToWindowsTimezone(this string olsenTimezone)
        {
            if (olsenTimezone == null)
                return null;

            if (_timezoneMappings == null)
            {
                _timezoneMappings = Resources.OlsenToWindows.FromJSONDictionary<Dictionary<string, string>>();
            }

            string windowsTimezone;
            _timezoneMappings.TryGetValue(olsenTimezone, out windowsTimezone);

            return windowsTimezone;
        }
        static Dictionary<string, string> _timezoneMappings = null;

        public static string ToLocalisedDayOfWeek(this DateTime date)
        {
            return DateTimeFormatInfo.CurrentInfo.GetDayName(date.DayOfWeek);
        }

        public static string ToPrettyTime(this TimeSpan span)
        {
            if (span.TotalDays >= 1)
            {
                return string.Format("{0} day{1}, {2} hour{3} and {4} minute{5}", span.Days, span.Days > 1 ? "s" : "", span.Hours, span.Hours != 1 ? "s" : "", span.Minutes, span.Minutes != 1 ? "s" : "");
            }
            else if (span.TotalHours >= 1)
            {
                return string.Format("{0} hour{1}, {2} minute{3} and {4} second{5}", span.Hours, span.Hours > 1 ? "s" : "", span.Minutes, span.Minutes != 1 ? "s" : "", span.Seconds, span.Seconds != 1 ? "s" : "");
            }
            else if (span.TotalMinutes >= 1)
            {
                return string.Format("{0} minute{1} and {2} second{3}", span.Minutes, span.Minutes > 1 ? "s" : "", span.Seconds, span.Seconds != 1 ? "s" : "");
            }
            else
            {
                return string.Format("{0} seconds", Math.Round(span.TotalSeconds, 3));
            }
        }
    }
}
