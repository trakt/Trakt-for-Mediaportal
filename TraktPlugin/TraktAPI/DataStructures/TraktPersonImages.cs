using System.Runtime.Serialization;

namespace TraktPlugin.TraktAPI.DataStructures
{
    [DataContract]
    public class TraktPersonImages
    {
        [DataMember(Name = "headshot")]
        public TraktImage HeadShot { get; set; }

        [DataMember(Name = "fanart")]
        public TraktImage Fanart { get; set; }
    }
}
