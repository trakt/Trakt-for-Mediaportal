using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace TraktPlugin.TraktAPI.DataStructures
{
    [DataContract]
    public class TraktSyncSeasonsRatedEx
    {
        [DataMember(Name = "shows")]
        public List<TraktSyncSeasonRatedEx> Shows { get; set; }
    }
}
