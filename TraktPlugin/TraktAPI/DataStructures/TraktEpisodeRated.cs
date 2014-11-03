using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace TraktPlugin.TraktAPI.DataStructures
{
    [DataContract]
    public class TraktEpisodeRated
    {
        [DataMember(Name = "rated")]
        public int Rated { get; set; }

        [DataMember(Name = "rated_at")]
        public string RatedAt { get; set; }

        [DataMember(Name = "episode")]
        public TraktEpisode Episode { get; set; }
    }
}