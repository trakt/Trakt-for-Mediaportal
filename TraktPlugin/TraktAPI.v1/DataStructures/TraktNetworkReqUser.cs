using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace TraktPlugin.TraktAPI.v1.DataStructures
{
    [DataContract]
    public class TraktNetworkReqUser : TraktUser
    {
        [DataMember(Name = "requested")]
        public long RequestDate { get; set; }
    }
}
