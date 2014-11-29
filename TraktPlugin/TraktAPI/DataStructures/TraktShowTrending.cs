using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace TraktPlugin.TraktAPI.DataStructures
{
    [DataContract]
    public class TraktShowTrending
    {
        [DataMember(Name = "watchers")]
        public int Watchers { get; set; }

        [DataMember(Name = "show")]
        public TraktShowSummary Show { get; set; }
    }
}