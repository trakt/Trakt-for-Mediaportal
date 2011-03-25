using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace TraktPlugin.TraktAPI.DataStructures
{
    [DataContract]
    public class TraktRateResponse : TraktResponse
    {
        [DataMember(Name = "type")]
        public string Type { get; set; }

        [DataMember(Name = "rating")]
        public string Rating { get; set; }

        [DataMember(Name = "ratings")]
        public TraktRatings Ratings { get; set; }

        [DataMember(Name = "facebook")]
        public bool Facebook { get; set; }

        [DataMember(Name = "twitter")]
        public bool Twitter { get; set; }

        [DataMember(Name = "tumblr")]
        public bool Tumblr { get; set; }
    }

    [DataContract]
    public class TraktRatings
    {
        [DataMember(Name = "percentage")]
        public double Percentage { get; set; }

        [DataMember(Name = "votes")]
        public int Votes { get; set; }

        [DataMember(Name = "loved")]
        public int LovedCount { get; set; }

        [DataMember(Name = "hated")]
        public int HatedCount { get; set; }
    }
}
