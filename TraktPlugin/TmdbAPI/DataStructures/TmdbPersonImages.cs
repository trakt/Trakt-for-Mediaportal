using System.Collections.Generic;
using System.Runtime.Serialization;

namespace TraktPlugin.TmdbAPI.DataStructures
{
    [DataContract]
    public class TmdbPeopleImages : TmdbRequestAge
    {
        [DataMember(Name = "id")]
        public int? Id { get; set; }

        [DataMember(Name = "profiles")]
        public List<TmdbImage> Profiles { get; set; }
        
        public override bool Equals(object obj)
        {
            var other = obj as TmdbPeopleImages;
            return other != null && Id.Equals(other.Id);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
}
