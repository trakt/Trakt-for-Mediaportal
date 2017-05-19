using System.Runtime.Serialization;

namespace TraktAPI.DataStructures
{
    [DataContract]
    public class TraktMovieTrending
    {
        [DataMember(Name = "watchers")]
        public int Watchers { get; set; }

        [DataMember(Name = "movie")]
        public TraktMovieSummary Movie { get; set; }
    }
}