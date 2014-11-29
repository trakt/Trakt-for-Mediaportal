using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace TraktPlugin.TraktAPI.DataStructures
{
    [DataContract]
    public class TraktSeasonSummary : TraktSeason
    {
        [DataMember(Name = "rating")]
        public double? Rating { get; set; }

        [DataMember(Name = "images")]
        public TraktSeasonImages Images { get; set; }
    }
}
