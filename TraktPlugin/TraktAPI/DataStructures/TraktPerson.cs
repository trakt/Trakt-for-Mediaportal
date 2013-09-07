using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace TraktPlugin.TraktAPI.DataStructures
{
    [DataContract]
    public class TraktPerson
    {
        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "images")]
        public PersonImages Images { get; set; }

        [DataContract]
        public class PersonImages : INotifyPropertyChanged
        {
            [DataMember(Name = "headshot")]
            public string Headshot { get; set; }

            #region INotifyPropertyChanged

            /// <summary>
            /// Path to local headhot image
            /// </summary>
            public string HeadshotImageFilename
            {
                get
                {
                    string filename = string.Empty;
                    if (!string.IsNullOrEmpty(Headshot))
                    {
                        string folder = MediaPortal.Configuration.Config.GetSubFolder(MediaPortal.Configuration.Config.Dir.Thumbs, @"Trakt\People");
                        string headShotUrl = Headshot;
                        if (headShotUrl.Contains("jpg?"))
                        {
                            headShotUrl = headShotUrl.Replace("jpg?", string.Empty) + ".jpg";
                        }
                        var uri = new Uri(headShotUrl);
                        filename = System.IO.Path.Combine(folder, System.IO.Path.GetFileName(uri.LocalPath));
                    }
                    return filename;
                }
                set
                {
                    _HeadshotImageFilename = value;
                }
            }
            string _HeadshotImageFilename = string.Empty;

            /// <summary>
            /// Notify image property change during async image downloading
            /// Sends messages to facade to update image
            /// </summary>
            public event PropertyChangedEventHandler PropertyChanged;
            public void NotifyPropertyChanged(string propertyName)
            {
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }

            #endregion
        }
    }
}
