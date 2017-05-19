using System;
using System.Globalization;
using TraktAPI.Extensions;

namespace TraktPlugin.Extensions
{
    public static class DateExtensions
    {
        static string[] AbbreviatedDaysOfWeek = CultureInfo.CurrentUICulture.DateTimeFormat.AbbreviatedDayNames;

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

        public static string ToShortDayName(this DayOfWeek dayOfWeek)
        {
            return AbbreviatedDaysOfWeek[(int)dayOfWeek];
        }

        public static string ToPrettyDateTime(this string timestamp)
        {
            DateTime dateTimestamp = timestamp.FromISO8601();

            if (dateTimestamp.ToLocalTime().Date == DateTime.Today)
            {
                return dateTimestamp.ToShortTimeString();
            }
            else if (dateTimestamp.ToLocalTime().Date >= DateTime.Today.AddDays(-7))
            {
                return dateTimestamp.ToLocalTime().DayOfWeek.ToShortDayName() + ", " + dateTimestamp.ToLocalTime().ToShortTimeString();
            }
            else if (dateTimestamp.ToLocalTime().Date < DateTime.Today.AddDays(-7))
            {
                return dateTimestamp.ToLocalTime().ToShortDateString();
            }

            return string.Empty;
        }
    }
}
