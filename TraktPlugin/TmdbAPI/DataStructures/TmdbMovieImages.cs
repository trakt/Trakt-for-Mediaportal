using System.Collections.Generic;
using System.Runtime.Serialization;

namespace TraktPlugin.TmdbAPI.DataStructures
{
    [DataContract]
    public class TmdbMovieImages : TmdbRequestAge
    {
        [DataMember(Name = "id")]
        public int? Id { get; set; }

        [DataMember(Name = "backdrops")]
        public List<TmdbImage> Backdrops { get; set; }

        [DataMember(Name = "posters")]
        public List<TmdbImage> Posters { get; set; }
    }
}
