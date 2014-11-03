using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace TraktPlugin.TraktAPI.DataStructures
{
    [DataContract]
    public class TraktScrobble
    {
        [DataMember(Name = "progress")]
        public double Progress { get; set; }

        [DataMember(Name = "app_version")]
        public string AppVersion { get; set; }

        [DataMember(Name = "app_date")]
        public string AppDate { get; set; }
    }
}
