using System.Collections.Generic;
using System.Runtime.Serialization;

namespace TraktAPI.DataStructures
{
    [DataContract]
    public class TraktCharacter
    {
        [DataMember(Name = "characters")]
        public List<string> Characters { get; set; }

        [DataMember(Name = "person")]
        public TraktPersonSummary Person { get; set; }
    }

    [DataContract]
    public class TraktShowCharacter : TraktCharacter
    {
        [DataMember(Name = "episode_count")]
        public int EpisodeCount { get; set; }
    }
}
