using System.Runtime.Serialization;

namespace TraktPlugin.TraktAPI.DataStructures
{
    [DataContract]
    public class TraktUserSummary : TraktUser
    {
        [DataMember(Name = "joined_at")]
        public string JoinedAt { get; set; }

        [DataMember(Name = "location")]
        public string Location { get; set; }

        [DataMember(Name = "about")]
        public string About { get; set; }

        [DataMember(Name = "gender")]
        public string Gender { get; set; }

        [DataMember(Name = "age")]
        public int? Age { get; set; }

        [DataMember(Name = "images")]
        public TraktUserImages Images { get; set; }
    }
}
