using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace TraktPlugin.TraktAPI.DataStructures
{
    [DataContract]
    public class TraktShow
    {
        [DataMember(Name = "title")]
        public string Title { get; set; }

        [DataMember(Name = "year")]
        public int Year { get; set; }

        [DataMember(Name = "url")]
        public string Url { get; set; }

        [DataMember(Name = "first_aired")]
        public long FirstAired { get; set; }

        [DataMember(Name = "country")]
        public string Country { get; set; }

        [DataMember(Name = "overview")]
        public string Overview { get; set; }

        [DataMember(Name = "runtime")]
        public int Runtime { get; set; }

        [DataMember(Name = "network")]
        public string Network { get; set; }

        [DataMember(Name = "air_day")]
        public string AirDay { get; set; }

        [DataMember(Name = "air_time")]
        public string AirTime { get; set; }

        [DataMember(Name = "certification")]
        public string Certification { get; set; }

        [DataMember(Name = "imdb_id")]
        public string Imdb { get; set; }

        [DataMember(Name = "tvdb_id")]
        public string Tvdb { get; set; }

        [DataMember(Name = "tvrage_id")]
        public string TvRage { get; set; }

        [DataMember(Name = "in_watchlist")]
        public bool InWatchList { get; set; }

        [DataMember(Name = "rating")]
        public string Rating { get; set; }

        [DataMember(Name = "ratings")]
        public TraktRatings Ratings { get; set; }

        [DataMember(Name = "images")]
        public ShowImages Images { get; set; }

        [DataContract]
        public class ShowImages : INotifyPropertyChanged
        {
            [DataMember(Name = "fanart")]
            public string Fanart
            {
                get
                {
                    if (TraktSettings.DownloadFullSizeFanart)
                        return _fanart;
                    return _fanart.Replace(".jpg", "-940.jpg");
                }
                set
                {
                    _fanart = value;
                }
            }
            string _fanart = string.Empty;

            [DataMember(Name = "poster")]
            public string Poster
            {
                get
                {
                    return _poster.Replace(".jpg", "-300.jpg");
                }
                set
                {
                    _poster = value;
                }
            }
            string _poster = string.Empty;

            #region INotifyPropertyChanged

            /// <summary>
            /// Path to local poster image
            /// </summary>
            public string PosterImageFilename
            {
                get
                {
                    string filename = string.Empty;
                    if (!string.IsNullOrEmpty(Poster))
                    {
                        string folder = MediaPortal.Configuration.Config.GetSubFolder(MediaPortal.Configuration.Config.Dir.Thumbs, @"Trakt\Shows\Posters");
                        Uri uri = new Uri(Poster);
                        filename = System.IO.Path.Combine(folder, System.IO.Path.GetFileName(uri.LocalPath));
                    }
                    return filename;
                }
                set
                {
                    _PosterImageFilename = value;
                }
            }
            string _PosterImageFilename = string.Empty;

            public string FanartImageFilename
            {
                get
                {
                    string filename = string.Empty;
                    if (!string.IsNullOrEmpty(Fanart))
                    {
                        string folder = MediaPortal.Configuration.Config.GetSubFolder(MediaPortal.Configuration.Config.Dir.Thumbs, @"Trakt\Shows\Fanart");
                        Uri uri = new Uri(Fanart);
                        filename = System.IO.Path.Combine(folder, System.IO.Path.GetFileName(uri.LocalPath));
                    }
                    return filename;
                }
                set
                {
                    _FanartImageFilename = value;
                }
            }
            string _FanartImageFilename = string.Empty;

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
