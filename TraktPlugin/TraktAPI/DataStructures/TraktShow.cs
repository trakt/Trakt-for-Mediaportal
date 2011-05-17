using System;
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

        [DataMember(Name = "images")]
        public ShowImages Images { get; set; }

        [DataContract]
        public class ShowImages
        {
            [DataMember(Name = "poster")]
            public string Poster { get; set; }

            [DataMember(Name = "fanart")]
            public string Fanart { get; set; }
        }
    }
}
