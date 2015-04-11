using System.Runtime.Serialization;

namespace TraktPlugin.TraktAPI.DataStructures
{
    [DataContract]
    public class TraktShowUpdate
    {
        [DataMember(Name = "updated_at")]
        public int Watchers { get; set; }

        [DataMember(Name = "show")]
        public TraktShow Show { get; set; }
    }
}