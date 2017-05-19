using System.Runtime.Serialization;

namespace TraktAPI.DataStructures
{
    [DataContract]
    public class TraktCalendar : TraktEpisodeSummaryEx
    {
        [DataMember(Name = "airs_at")]
        public string AirsAt { get; set; }
    }
}
