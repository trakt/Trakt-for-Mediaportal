using System.Collections.Generic;
using System.Runtime.Serialization;

namespace TraktAPI.DataStructures
{
    [DataContract]
    public class TraktJob
    {
        [DataMember(Name = "jobs")]
        public List<string> Jobs { get; set; }

        [DataMember(Name = "person")]
        public TraktPersonSummary Person { get; set; }
    }

    [DataContract]
    public class TraktShowJob : TraktJob
    {
        [DataMember(Name = "episode_count")]
        public int EpisodeCount { get; set; }
    }
}
