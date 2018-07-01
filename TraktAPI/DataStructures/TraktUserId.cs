using System.Runtime.Serialization;

namespace TraktAPI.DataStructures
{
    [DataContract]
    public class TraktUserId
    {
        [DataMember(Name = "slug")]
        public string Slug { get; set; }
    }
}
