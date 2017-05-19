using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace TraktAPI.DataStructures
{
    [DataContract]
    public class TraktEpisodeWatchList : TraktEpisodeSummaryEx
    {
        [DataMember(Name = "listed_at")]
        public string ListedAt { get; set; }
    }
}
