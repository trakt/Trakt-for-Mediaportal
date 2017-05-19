using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace TraktAPI.DataStructures
{
    [DataContract]
    public class TraktFollowerRequest
    {
        [DataMember(Name = "id")]
        public int Id { get; set; }

        [DataMember(Name = "requested_at")]
        public string RequestedAt { get; set; }

        [DataMember(Name = "user")]
        public TraktUserSummary User { get; set; }
    }
}
