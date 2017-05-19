using System.Runtime.Serialization;

namespace TraktAPI.DataStructures
{
    [DataContract]
    public class TraktPersonShowJob
    {
        [DataMember(Name = "job")]
        public string Job { get; set; }

        [DataMember(Name = "show")]
        public TraktShowSummary Show { get; set; }
    }
}
