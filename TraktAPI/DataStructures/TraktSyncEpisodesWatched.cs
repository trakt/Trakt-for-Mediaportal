using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace TraktAPI.DataStructures
{
    [DataContract]
    public class TraktSyncEpisodesWatched
    {
        [DataMember(Name = "episodes")]
        public List<TraktSyncEpisodeWatched> Episodes { get; set; }
    }
}
