using System.Runtime.Serialization;

namespace TraktAPI.DataStructures
{
    [DataContract]
    public class TraktMediaInfo
    {
        [DataMember(Name = "media_type")]
        public string MediaType { get; set; }

        [DataMember(Name = "resolution")]
        public string Resolution { get; set; }

        [DataMember(Name = "audio")]
        public string AudioCodec { get; set; }

        [DataMember(Name = "audio_channels")]
        public string AudioChannels { get; set; }

        [DataMember(Name = "hdr")]
        public string HdrType { get; set; }

        [DataMember(Name = "3d")]
        public bool Is3D { get; set; }
    }
}
