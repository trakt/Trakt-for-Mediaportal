using System.Runtime.Serialization;

namespace TraktPlugin.TraktAPI.DataStructures
{
    [DataContract]
    public class TraktMovieUpdate
    {
        [DataMember(Name = "updated_at")]
        public string UpdatedAt { get; set; }

        [DataMember(Name = "movie")]
        public TraktMovie Movie { get; set; }
    }
}