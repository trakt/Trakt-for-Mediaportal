using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace TraktPlugin.TraktAPI.DataStructures
{
    [DataContract]
    public class TraktSyncShowCollectedEx : TraktShow
    {
        [DataMember(Name = "seasons")]
        public List<Season> Seasons { get; set; }

        [DataContract]
        public class Season
        {
            [DataMember(Name = "number")]
            public int Number { get; set; }

            [DataMember(Name = "episodes")]
            public List<Episode> Episodes { get; set; }

            [DataContract]
            public class Episode
            {
                [DataMember(Name = "number")]
                public int Number { get; set; }

                [DataMember(Name = "collected_at")]
                public string CollectedAt { get; set; }

                [DataMember(Name = "media_type")]
                public string MediaType { get; set; }

                [DataMember(Name = "resolution")]
                public string Resolution { get; set; }

                [DataMember(Name = "audio")]
                public string AudioCodec { get; set; }

                [DataMember(Name = "audio_channels")]
                public string AudioChannels { get; set; }

                [DataMember(Name = "3d")]
                public bool Is3D { get; set; }
            }
        }
    }
}
