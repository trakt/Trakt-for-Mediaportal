using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace TraktPlugin.TraktAPI.DataStructures
{
    [DataContract]
    public class TraktLastSyncActivities
    {
        [DataMember(Name = "all")]
        public string All { get; set; }

        [DataMember(Name = "movies")]
        public MovieActivities Movies { get; set; }

        [DataContract]
        public class MovieActivities
        {
            [DataMember(Name = "watched_at")]
            public string Watched { get; set; }

            [DataMember(Name = "collected_at")]
            public string Collection { get; set; }

            [DataMember(Name = "rated_at")]
            public string Rating { get; set; }

            [DataMember(Name = "watchlisted_at")]
            public string Watchlist { get; set; }

            [DataMember(Name = "commented_at")]
            public string Comment { get; set; }

            [DataMember(Name = "paused_at")]
            public string PausedAt { get; set; }
        }

        [DataMember(Name = "episodes")]
        public EpisodeActivities Episodes { get; set; }

        [DataContract]
        public class EpisodeActivities
        {
            [DataMember(Name = "watched_at")]
            public string Watched { get; set; }

            [DataMember(Name = "collected_at")]
            public string Collection { get; set; }

            [DataMember(Name = "rated_at")]
            public string Rating { get; set; }

            [DataMember(Name = "watchlisted_at")]
            public string Watchlist { get; set; }

            [DataMember(Name = "commented_at")]
            public string Comment { get; set; }

            [DataMember(Name = "paused_at")]
            public string PausedAt { get; set; }
        }

        [DataMember(Name = "shows")]
        public ShowActivities Shows { get; set; }

        [DataContract]
        public class ShowActivities
        {
            [DataMember(Name = "rated_at")]
            public string Rating { get; set; }

            [DataMember(Name = "watchlisted_at")]
            public string Watchlist { get; set; }

            [DataMember(Name = "commented_at")]
            public string Comment { get; set; }
        }

        [DataMember(Name = "seasons")]
        public SeasonActivities Season { get; set; }

        [DataContract]
        public class SeasonActivities
        {
            [DataMember(Name = "rated_at")]
            public string Rating { get; set; }

            [DataMember(Name = "commented_at")]
            public string Comment { get; set; }
        }
    }
}
