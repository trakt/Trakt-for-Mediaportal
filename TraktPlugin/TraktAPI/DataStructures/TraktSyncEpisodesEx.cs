using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace TraktPlugin.TraktAPI.DataStructures
{
    [DataContract]
    public class TraktSyncEpisodesEx
    {
        [DataMember(Name = "shows")]
        public List<TraktSyncEpisodeEx> Shows { get; set; }
    }
}
