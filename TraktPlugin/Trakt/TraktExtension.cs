using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.Serialization.Json;

namespace TraktPlugin.Trakt
{
    public static class JSONExtensions
    {
        public static IEnumerable<T> FromJSONArray<T>(this string jsonArray)
        {
            if (string.IsNullOrEmpty(jsonArray)) return new List<T>();

            try
            {
                using (var ms = new MemoryStream(Encoding.Default.GetBytes(jsonArray)))
                {
                    var ser = new DataContractJsonSerializer(typeof(IEnumerable<T>));
                    var result = (IEnumerable<T>)ser.ReadObject(ms);

                    if (result == null)
                    {
                        return new List<T>();
                    }
                    else
                    {
                        return result;
                    }
                }
            }
            catch (Exception)
            {
                return new List<T>();
            }
        }

        public static T FromJSON<T>(this string json)
        {
            if (string.IsNullOrEmpty(json)) return default(T);

            try
            {
                using (var ms = new MemoryStream(Encoding.Default.GetBytes(json.ToCharArray())))
                {
                    var ser = new DataContractJsonSerializer(typeof(T));
                    return (T)ser.ReadObject(ms);
                }
            }
            catch (Exception)
            {
                return default(T);
            }
        }

        public static string ToJSON(this object obj)
        {
            using (var ms = new MemoryStream())
            {
                var ser = new DataContractJsonSerializer(obj.GetType());
                ser.WriteObject(ms, obj);
                return Encoding.Default.GetString(ms.ToArray());
            }
        }
    }
}
