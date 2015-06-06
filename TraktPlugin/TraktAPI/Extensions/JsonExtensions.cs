extern alias nsoft;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using nsoft::Newtonsoft.Json; //OnlineVideos has this namespace

namespace TraktPlugin.TraktAPI.Extensions
{
    /// <summary>
    /// Methods used to transform to and from JSON
    /// </summary>
    public static class JSONExtensions
    {
        /// <summary>
        /// Creates a list based on a JSON Array
        /// </summary>
        public static IEnumerable<T> FromJSONArray<T>(this string jsonArray)
        {
            if (string.IsNullOrEmpty(jsonArray)) return null;

            try
            {
                using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(jsonArray)))
                {
                    var ser = new DataContractJsonSerializer(typeof(IEnumerable<T>));
                    var result = (IEnumerable<T>)ser.ReadObject(ms);
                    return result;
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Creates a Dictionary based on the JSON string
        /// </summary>
        public static T FromJSONDictionary<T>(this string json)
        {
            if (string.IsNullOrEmpty(json)) return default(T);            

            try
            {
                return JsonConvert.DeserializeObject<T>(json);
            }
            catch
            {
                return default(T);
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
                using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(json.ToCharArray())))
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
            if (obj == null) return string.Empty;
            using (var ms = new MemoryStream())
            {
                var ser = new DataContractJsonSerializer(obj.GetType());
                ser.WriteObject(ms, obj);
                return Encoding.UTF8.GetString(ms.ToArray());
            }
        }
    }
}
