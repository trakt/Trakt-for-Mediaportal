using System.Runtime.Serialization;

namespace TraktAPI.DataStructures
{
    [DataContract]
    public class TraktSocialConnection
    {
        [DataMember(Name = "facebook")]
        public bool Facebook { get; set; }

        [DataMember(Name = "twitter")]
        public bool Twitter { get; set; }

        [DataMember(Name = "google")]
        public bool Google { get; set; }

        [DataMember(Name = "tumblr")]
        public bool Tumblr { get; set; }

        [DataMember(Name = "medium")]
        public bool Medium { get; set; }

        [DataMember(Name = "slack")]
        public bool Slack { get; set; }
    }
}
