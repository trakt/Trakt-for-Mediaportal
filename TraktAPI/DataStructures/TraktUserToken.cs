using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace TraktAPI.DataStructures
{
    [DataContract]
    public class TraktUserToken : TraktStatus
    {
        [DataMember(Name = "token")]
        public string Token { get; set; }
    }
}
