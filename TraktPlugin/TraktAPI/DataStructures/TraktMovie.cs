using System.Runtime.Serialization;

namespace TraktPlugin.TraktAPI.DataStructures
{
    [DataContract]
    public class TraktMovie
    {
        [DataMember(Name = "title", EmitDefaultValue = false)]
        public string Title { get; set; }

        [DataMember(Name = "year", EmitDefaultValue = false)]
        public int? Year { get; set; }

        [DataMember(Name = "ids")]
        public TraktMovieId Ids { get; set; }
    }
}
