using System.Runtime.Serialization;

namespace TraktAPI.DataStructures
{
    [DataContract]
    public class TraktJob
    {
        [DataMember(Name = "job")]
        public string Job { get; set; }

        [DataMember(Name = "person")]
        public TraktPersonSummary Person { get; set; }
    }
}
