using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace TraktPlugin.TraktAPI.DataStructures
{
    [DataContract]
    public class TraktEpisodeWatchList
    {
        [DataMember(Name = "listed_at")]
        public string ListedAt { get; set; }

        [DataMember(Name = "show")]
        public TraktShowSummary Show { get; set; }

        [DataMember(Name = "episode")]
        public TraktEpisodeSummary Episode { get; set; }

        public override string ToString()
        {
            return string.Format("{0} - {1}x{2} - {3}", this.Show.Title, Episode.Season, Episode.Number, Episode.Title ?? "TBA");
        }
    }
}
