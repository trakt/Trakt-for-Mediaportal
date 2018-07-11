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

        public override bool Equals(object obj)
        {
            var other = obj as TmdbMovieImages;
            return other != null && Id.Equals(other.Id);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
}
