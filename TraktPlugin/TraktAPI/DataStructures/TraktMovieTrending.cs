using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace TraktPlugin.TraktAPI.DataStructures
{
    [DataContract]
    public class TraktMovieTrending
    {
        [DataMember(Name = "watchers")]
        public int Watchers { get; set; }

        [DataMember(Name = "movie")]
        public TraktMovieSummary Movie { get; set; }
    }
}