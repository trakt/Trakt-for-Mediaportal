using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace TraktPlugin.TraktAPI.DataStructures
{
    [DataContract]
    public class TraktShout : TraktResponse
    {
        [DataMember(Name = "inserted")]
        public long InsertedDate { get; set; }

        [DataMember(Name = "shout")]
        public string Shout { get; set; }

        [DataMember(Name = "user")]
        public TraktUser User { get; set; }

        [DataContract]
        public class TraktUser : INotifyPropertyChanged
        {
            [DataMember(Name = "username")]
            public string Username { get; set; }

            [DataMember(Name = "protected")]
            public string Protected { get; set; }

            [DataMember(Name = "full_name")]
            public string FullName { get; set; }

            [DataMember(Name = "gender")]
            public string Gender { get; set; }

            [DataMember(Name = "age")]
            public string Age { get; set; }

            [DataMember(Name = "location")]
            public string Location { get; set; }

            [DataMember(Name = "about")]
            public string About { get; set; }

            [DataMember(Name = "joined")]
            public long JoinDate { get; set; }

            [DataMember(Name = "avatar")]
            public string Avatar { get; set; }

            [DataMember(Name = "url")]
            public string Url { get; set; }

            #region INotifyPropertyChanged

            /// <summary>
            /// Path to local Avatar Image
            /// </summary>
            public string AvatarFilename
            {
                get
                {
                    string filename = string.Empty;
                    if (!string.IsNullOrEmpty(Avatar))
                    {
                        string folder = MediaPortal.Configuration.Config.GetSubFolder(MediaPortal.Configuration.Config.Dir.Thumbs, @"Trakt\Avatars");
                        filename = System.IO.Path.Combine(folder, string.Concat(Username, ".jpg"));
                    }
                    return filename;
                }
                set
                {
                    _AvatarFilename = value;
                }
            }
            string _AvatarFilename = string.Empty;

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
