using System.Runtime.Serialization;

namespace TraktAPI.DataStructures
{
    [DataContract]
    public class TraktShowUpdate
    {
        [DataMember(Name = "updated_at")]
        public string UpdatedAt { get; set; }

        [DataMember(Name = "show")]
        public TraktShow Show { get; set; }
    }
}