using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.Serialization.Json;

namespace TraktPlugin.TraktAPI
{
    /// <summary>
    /// Methods used to transform to and from JSON
    /// </summary>
    public static class JSONExtensions
    {
        /// <summary>
        /// Creates a list based on a JSON Array
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="jsonArray"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Creates an object from JSON
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="json"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Turns an object into JSON
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
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
