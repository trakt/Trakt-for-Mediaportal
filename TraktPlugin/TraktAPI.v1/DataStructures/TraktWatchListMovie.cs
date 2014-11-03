using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace TraktPlugin.TraktAPI.v1.DataStructures
{
    [DataContract]
    public class TraktWatchListMovie : TraktMovie
    {
        [DataMember(Name = "inserted")]
        public long Inserted { get; set; }
    }
}
