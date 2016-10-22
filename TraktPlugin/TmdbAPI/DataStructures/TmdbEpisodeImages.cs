using System.Collections.Generic;
using System.Runtime.Serialization;

namespace TraktPlugin.TmdbAPI.DataStructures
{
    [DataContract]
    public class TmdbEpisodeImages : TmdbRequestAge
    {
        [DataMember(Name = "id")]
        public int? Id { get; set; }

        [DataMember(Name = "stills")]
        public List<TmdbImage> Stills { get; set; }

        [DataMember]
        public int Season { get; set; }

        [DataMember]
        public int Episode { get; set; }
        
        [DataMember]
        public string AirDate { get; set; }
    }
}
