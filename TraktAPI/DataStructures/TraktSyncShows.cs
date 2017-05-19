using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace TraktAPI.DataStructures
{
    [DataContract]
    public class TraktSyncShows
    {
        [DataMember(Name = "shows")]
        public List<TraktShow> Shows { get; set; }
    }
}
