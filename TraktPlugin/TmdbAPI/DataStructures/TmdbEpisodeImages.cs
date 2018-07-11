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

        public override bool Equals(object obj)
        {
            var other = obj as TmdbEpisodeImages;
            return other != null && Id.Equals(other.Id) && Season.Equals(other.Season) && Episode.Equals(other.Episode);
        }

        public override int GetHashCode()
        {
            return ((Id ?? -1).ToString() + "_" + Season + "_" + Episode).GetHashCode();
        }
    }
}
