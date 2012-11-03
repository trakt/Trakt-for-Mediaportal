using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace TraktPlugin.TraktAPI.DataStructures
{
    [DataContract]
    public class TraktActor
    {
        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "url")]
        public string Url { get; set; }

        [DataMember(Name = "biography")]
        public string Biography { get; set; }

        [DataMember(Name = "birthday")]
        public string Birthday { get; set; }

        [DataMember(Name = "birthplace")]
        public string Birthplace { get; set; }

        [DataMember(Name = "tmdb_id")]
        public int TmdbId { get; set; }

        [DataMember(Name = "images")]
        public ActorImages Images { get; set; }

        [DataContract]
        public class ActorImages
        {
            [DataMember(Name = "headshot")]
            public string Headshot { get; set; }
        }
    }
}
