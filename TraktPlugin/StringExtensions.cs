using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace TraktPlugin
{
    public static class StringExtensions
    {
        public static string ToSlug(this string item)
        {
            string invalidChars = Regex.Escape(new string(Path.GetInvalidFileNameChars()));
            string invalidReStr = string.Format(@"[{0}]+", invalidChars);
            return Regex.Replace(item, invalidReStr, string.Empty).ToLower().Replace(" ", "-");
        }

        public static bool IsNumber(this string number)
        {
            double retValue;
            return double.TryParse(number, out retValue);
        }

        public static string StripHTML(this string htmlString)
        {
            if (string.IsNullOrEmpty(htmlString)) return string.Empty;

            string pattern = @"<(.|\n)*?>";
            return Regex.Replace(htmlString, pattern, string.Empty);
        }

        public static string RemapHighOrderChars(this string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;

            // hack to remap high order unicode characters with a low order equivalents
            // for now, this allows better usage of clipping. This can be removed, once the skin engine can properly render unicode without falling back to sprites
            // as unicode is more widely used, this will hit us more with existing font rendering only allowing cached font textures with clipping
            
            input = input.Replace(((char)8211).ToString(), "--");  //	–
            input = input.Replace(((char)8212).ToString(), "---");  //	—
            input = input.Replace((char)8216, '\''); //	‘
            input = input.Replace((char)8217, '\''); //	’
            input = input.Replace((char)8220, '"');  //	“
            input = input.Replace((char)8221, '"');  //	”
            input = input.Replace((char)8223, '"');  // ‟
            input = input.Replace((char)8226, '*');  //	•
            input = input.Replace(((char)8230).ToString(), "...");  // …
            input = input.Replace(((char)8482).ToString(), string.Empty);  // ™
            
            return input;
        }

        public static string SurroundWithDoubleQuotes(this string text)
        {
            return SurroundWith(text, "\"");
        }

        public static string SurroundWith(this string text, string ends)
        {
            return ends + text + ends;
        }
    }
}
