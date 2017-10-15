using System.Runtime.Serialization;

namespace TraktAPI.DataStructures
{
    [DataContract]
    public class TraktHiddenItem
    {
        [DataMember(Name = "hidden_at")]
        public string HiddenAt { get; set; }

        [DataMember(Name = "type")]
        public string Type { get; set; }

        [DataMember(Name = "movie")]
        public TraktMovieSummary Movie { get; set; }

        [DataMember(Name = "show")]
        public TraktShowSummary Show { get; set; }

        [DataMember(Name = "season")]
        public TraktSeasonSummary Season { get; set; }
    }
}
