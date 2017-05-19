using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace TraktAPI.DataStructures
{
    [DataContract]
    public class TraktSyncEpisodesCollected
    {
        [DataMember(Name = "episodes")]
        public List<TraktSyncEpisodeCollected> Episodes { get; set; }
    }
}
