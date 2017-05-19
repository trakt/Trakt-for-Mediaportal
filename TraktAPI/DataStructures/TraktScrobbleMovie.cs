using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace TraktAPI.DataStructures
{
    [DataContract]
    public class TraktScrobbleMovie : TraktScrobble
    {
        [DataMember(Name = "movie")]
        public TraktMovie Movie { get; set; }
    }
}
