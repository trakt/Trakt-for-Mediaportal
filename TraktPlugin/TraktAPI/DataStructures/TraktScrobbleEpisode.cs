using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace TraktPlugin.TraktAPI.DataStructures
{
    [DataContract]
    public class TraktScrobbleEpisode : TraktScrobble
    {
        [DataMember(Name = "episode")]
        public TraktEpisode Episode { get; set; }
    }
}
