using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace TraktPlugin.TraktAPI.DataStructures
{
    [DataContract]
    public class TraktSyncMoviesRated
    {
        [DataMember(Name = "movies")]
        public List<TraktSyncMovieRated> Movies { get; set; }
    }
}
