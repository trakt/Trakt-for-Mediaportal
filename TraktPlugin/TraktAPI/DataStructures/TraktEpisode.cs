using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace TraktPlugin.TraktAPI.DataStructures
{
    [DataContract]
    public class TraktEpisode
    {
        [DataMember(Name = "season")]
        public int Season { get; set; }

        [DataMember(Name = "number")]
        public int Number { get; set; }

        [DataMember(Name = "title")]
        public string Title { get; set; }

        [DataMember(Name = "overview")]
        public string Overview { get; set; }

        [DataMember(Name = "url")]
        public string Url { get; set; }

        [DataMember(Name = "first_aired")]
        public long FirstAired { get; set; }

        [DataMember(Name = "first_aired_utc")]
        public long FirstAiredUtc { get; set; }

        [DataMember(Name = "first_aired_iso")]
        public string FirstAiredIso { get; set; }

        [DataMember(Name = "first_aired_localized")]
        public long FirstAiredLocalized { get; set; }

        [DataMember(Name = "runtime")]
        public int Runtime { get; set; }

        [DataMember(Name = "in_watchlist")]
        public bool InWatchList { get; set; }

        [DataMember(Name = "in_collection")]
        public bool InCollection { get; set; }

        [DataMember(Name = "watched")]
        public bool Watched { get; set; }

        [DataMember(Name = "plays")]
        public int Plays { get; set; }

        [DataMember(Name = "rating")]
        public string Rating { get; set; }

        [DataMember(Name = "rating_advanced")]
        public int RatingAdvanced { get; set; }

        [DataMember(Name = "ratings")]
        public TraktRatings Ratings { get; set; }

        [DataMember(Name = "images")]
        public ShowImages Images { get; set; }

        [DataContract]
        public class ShowImages : INotifyPropertyChanged
        {
            [DataMember(Name = "screen")]
            public string Screen { get; set; }

            #region INotifyPropertyChanged
            
            /// <summary>
            /// Path to local episode image
            /// </summary>
            public string EpisodeImageFilename
            {
                get
                {
                    string filename = string.Empty;
                    if (!string.IsNullOrEmpty(Screen))
                    {
                        string folder = MediaPortal.Configuration.Config.GetSubFolder(MediaPortal.Configuration.Config.Dir.Thumbs, @"Trakt\Episodes");
                        string epScreenUrl = Screen;
                        if (epScreenUrl.Contains("jpg?"))
                        {
                            epScreenUrl = epScreenUrl.Replace("jpg?", string.Empty) + ".jpg";
                        }
                        var uri = new Uri(epScreenUrl);
                        filename = System.IO.Path.Combine(folder, System.IO.Path.GetFileName(uri.LocalPath));
                    }
                    return filename;
                }
                set
                {
                    _EpisodeImageFilename = value;
                }
            }
            string _EpisodeImageFilename = string.Empty;

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
