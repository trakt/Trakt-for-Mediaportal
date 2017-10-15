using System.Collections.Generic;
using System.Runtime.Serialization;

namespace TraktAPI.DataStructures
{
    [DataContract]
    public class TraktSyncMovies
    {
        [DataMember(Name = "movies")]
        public List<TraktMovie> Movies { get; set; }
    }
}
