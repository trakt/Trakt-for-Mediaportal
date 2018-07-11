using System.Collections.Generic;
using System.Runtime.Serialization;

namespace TraktPlugin.TmdbAPI.DataStructures
{
    [DataContract]
    public class TmdbSeasonImages : TmdbRequestAge
    {
        [DataMember(Name = "id")]
        public int? Id { get; set; }

        [DataMember(Name = "posters")]
        public List<TmdbImage> Posters { get; set; }

        [DataMember]
        public int Season { get; set; }

        public override bool Equals(object obj)
        {
            var other = obj as TmdbSeasonImages;
            return other != null && Id.Equals(other.Id) && Season.Equals(other.Season);
        }

        public override int GetHashCode()
        {
            return ((Id ?? -1).ToString() + "_" + Season).GetHashCode();
        }
    }
}
