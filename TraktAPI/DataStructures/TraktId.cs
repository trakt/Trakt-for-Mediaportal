using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace TraktAPI.DataStructures
{
    [DataContract]
    public class TraktId
    {
        [DataMember(Name = "trakt")]
        public int? Trakt { get; set; }

        [DataMember(Name = "slug")]
        public string Slug { get; set; }
    }
}
