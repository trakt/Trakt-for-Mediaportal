using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace TraktPlugin.TraktAPI.DataStructures
{
    [DataContract]
    public class TraktNetworkFriend : TraktUserSummary
    {
        [DataMember(Name = "friends_at")]
        public string FriendsAt { get; set; }
    }
}
