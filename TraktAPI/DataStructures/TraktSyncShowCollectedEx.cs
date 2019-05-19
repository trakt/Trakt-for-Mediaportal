using System.Collections.Generic;
using System.Runtime.Serialization;

namespace TraktAPI.DataStructures
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
            public class Episode : TraktMediaInfo
            {
                [DataMember(Name = "number")]
                public int Number { get; set; }

                [DataMember(Name = "collected_at")]
                public string CollectedAt { get; set; }
            }
        }
    }
}
