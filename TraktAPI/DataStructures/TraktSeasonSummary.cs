using System.Collections.Generic;
using System.Runtime.Serialization;

namespace TraktAPI.DataStructures
{
    [DataContract]
    public class TraktSeasonSummary : TraktSeason
    {
        [DataMember(Name = "rating")]
        public double? Rating { get; set; }

        [DataMember(Name = "votes")]
        public int Votes { get; set; }

        [DataMember(Name = "episode_count")]
        public int EpisodeCount { get; set; }

        [DataMember(Name = "aired_episodes")]
        public int EpisodeAiredCount { get; set; }

        [DataMember(Name = "overview")]
        public string Overview { get; set; }

        [DataMember(Name = "title")]
        public string Title { get; set; }

        [DataMember(Name = "network")]
        public string Network { get; set; }

        [DataMember(Name = "first_aired")]
        public string FirstAired { get; set; }

        [DataMember(Name = "episodes")]
        public IEnumerable<TraktEpisodeSummary> Episodes { get; set; }
    }
}
