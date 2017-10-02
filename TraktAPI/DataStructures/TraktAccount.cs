using System.Runtime.Serialization;

namespace TraktAPI.DataStructures
{
    [DataContract]
    public class TraktAccount
    {
        [DataMember(Name = "timezone")]
        public string Timezone { get; set; }

        [DataMember(Name = "time_24hr")]
        public bool MiltaryTime { get; set; }

        [DataMember(Name = "cover_image")]
        public string CoverImage { get; set; }
    }
}
