using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace TraktPlugin.Trakt
{
    /// <summary>
    /// Data structure for Authorisation for Trakt
    /// </summary>
    [DataContract]
    public class TraktAuth
    {
        [DataMember(Name = "username")]
        public string UserName { get; set; }

        [DataMember(Name = "password")]
        public string Password { get; set; }
    }
}
