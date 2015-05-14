using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

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
    }
}
