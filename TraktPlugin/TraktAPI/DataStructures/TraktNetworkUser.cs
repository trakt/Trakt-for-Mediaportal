using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace TraktPlugin.TraktAPI.DataStructures
{
    [DataContract]
    public class TraktNetworkUser
    {
        [DataMember(Name = "followed_at")]
        public string FollowedAt { get; set; }

        [DataMember(Name = "user")]
        public TraktUserSummary User { get; set; }
    }
}
