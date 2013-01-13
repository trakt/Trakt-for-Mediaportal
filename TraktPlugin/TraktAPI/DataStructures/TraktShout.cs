using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace TraktPlugin.TraktAPI.DataStructures
{
    [DataContract]
    public class TraktShout : TraktResponse
    {
        [DataMember(Name = "inserted")]
        public long InsertedDate { get; set; }

        [DataMember(Name = "shout")]
        public string Shout { get; set; }

        [DataMember(Name = "spoiler")]
        public bool Spoiler { get; set; }

        [DataMember(Name = "user")]
        public TraktUser User { get; set; }
    }
}
