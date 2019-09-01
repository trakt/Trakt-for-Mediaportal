using System.Collections.Generic;
using System.Runtime.Serialization;

namespace TraktAPI.DataStructures
{
    [DataContract]
    public class TraktPersonShowCast
    {
        [DataMember(Name = "characters")]
        public List<string> Characters { get; set; }

        [DataMember(Name = "show")]
        public TraktShowSummary Show { get; set; }
        
        [DataMember(Name = "episode_count")]
        public int EpisodeCount { get; set; }

        [DataMember(Name = "series_regular")]
        public bool IsSeriesRegular { get; set; }
    }
}
