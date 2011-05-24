using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace TraktPlugin.TraktAPI.DataStructures
{
    /// <summary>
    /// Authenticated Call will return user collection info
    /// </summary>
    [DataContract]
    public class TraktTrendingMovie : TraktMovie
    {
        [DataMember(Name = "plays")]
        public int Plays { get; set; }

        [DataMember(Name = "in_collection")]
        public bool InCollection { get; set; }

        [DataMember(Name = "in_watchlist")]
        public bool InWatchList { get; set; }

        [DataMember(Name = "watchers")]
        public int Watchers { get; set; }
    }
}
