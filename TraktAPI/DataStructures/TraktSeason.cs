using System.Runtime.Serialization;

namespace TraktAPI.DataStructures
{
    [DataContract]
    public class TraktSeason
    {
        [DataMember(Name = "number")]
        public int Number { get; set; }

        [DataMember(Name = "ids")]
        public TraktSeasonId Ids { get; set; }
    }
}
