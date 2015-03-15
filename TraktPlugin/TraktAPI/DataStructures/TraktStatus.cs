using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace TraktPlugin.TraktAPI.DataStructures
{
    [DataContract]
    public class TraktStatus
    {
        [DataMember(Name = "reason", EmitDefaultValue = false)]
        public string Description { get; set; }

        [DataMember(Name = "code", EmitDefaultValue = false)]
        public int Code { get; set; }
    }
}
