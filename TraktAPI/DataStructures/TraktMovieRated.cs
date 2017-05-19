using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace TraktAPI.DataStructures
{
    [DataContract]
    public class TraktMovieRated
    {
        [DataMember(Name = "rating")]
        public int Rating { get; set; }

        [DataMember(Name = "rated_at")]
        public string RatedAt { get; set; }

        [DataMember(Name = "movie")]
        public TraktMovie Movie { get; set; }
    }
}
