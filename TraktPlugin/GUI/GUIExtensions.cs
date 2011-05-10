using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaPortal.GUI.Library;
using System.Reflection;
using System.Collections;

namespace TraktPlugin.GUI
{
    public static class GUIWindowExtensions
    {
        /// <summary>
        /// Same as Children and controlList but used for backwards compatibility between mediaportal 1.1 and 1.2
        /// </summary>
        /// <param name="self"></param>
        /// <returns>IEnumerable of GUIControls</returns>
        public static IEnumerable GetControlList(this GUIWindow self)
        {
            PropertyInfo property = GetPropertyInfo<GUIWindow>("Children", null);
            return (IEnumerable)property.GetValue(self, null);
        }

        private static Dictionary<string, PropertyInfo> propertyCache = new Dictionary<string, PropertyInfo>();

        /// <summary>
        /// Gets the property info object for a property using reflection.
        /// The property info object will be cached in memory for later requests.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="newName">The name of the property in 1.2</param>
        /// <param name="oldName">The name of the property in 1.1</param>
        /// <returns>instance PropertyInfo or null if not found</returns>
        public static PropertyInfo GetPropertyInfo<T>(string newName, string oldName)
        {
            PropertyInfo property = null;
            Type type = typeof(T);
            string key = type.FullName + "|" + newName;

            if (!propertyCache.TryGetValue(key, out property))
            {
                property = type.GetProperty(newName);
                if (property == null)
                {
                    property = type.GetProperty(oldName);
                }

                propertyCache[key] = property;
            }

            return property;
        }
    }
}