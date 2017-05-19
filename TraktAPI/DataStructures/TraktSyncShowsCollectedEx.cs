using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace TraktAPI.DataStructures
{
    [DataContract]
    public class TraktSyncShowsCollectedEx
    {
        [DataMember(Name = "shows")]
        public List<TraktSyncShowCollectedEx> Shows { get; set; }
    }
}
