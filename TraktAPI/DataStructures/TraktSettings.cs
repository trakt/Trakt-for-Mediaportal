using System.Runtime.Serialization;

namespace TraktAPI.DataStructures
{
    [DataContract]
    public class TraktSettings : TraktStatus
    {
        [DataMember(Name = "user")]
        public TraktUserSummary User { get; set; }

        [DataMember(Name = "account")]
        public TraktAccount Account { get; set; }

        [DataMember(Name = "connections")]
        public TraktSocialConnection Connections { get; set; }

        [DataMember(Name = "sharing_text")]
        public TraktSharingText SharingText { get; set; }
    }
}
