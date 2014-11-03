using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace TraktPlugin.TraktAPI.DataStructures
{
    [DataContract]
    public class TraktSyncEpisodes
    {
        [DataMember(Name = "episodes")]
        public List<TraktEpisode> Episodes { get; set; }
    }
}
