using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace TraktPlugin.TraktAPI.DataStructures
{
    [DataContract]
    public class TraktUserStatistics
    {
        [DataMember(Name = "movies")]
        public MovieStats Movies { get; set; }

        [DataMember(Name = "shows")]
        public ShowStats Shows { get; set; }

        [DataMember(Name = "episodes")]
        public EpisodeStats Episodes { get; set; }

        [DataContract]
        public class Stats
        {
            [DataMember(Name = "watched")]
            public int Watched { get; set; }

            [DataMember(Name = "collected")]
            public int Collected { get; set; }
        }

        [DataContract]
        public class MovieStats : Stats { }

        [DataContract]
        public class ShowStats : Stats { }

        [DataContract]
        public class EpisodeStats : Stats { }
    }
}
