using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace TraktAPI.DataStructures
{
    [DataContract]
    public class TraktNetworkApproval
    {
        [DataMember(Name = "approved_at")]
        public string ApprovedAt { get; set; }

        [DataMember(Name = "user")]
        public TraktUserSummary User { get; set; }
    }
}
