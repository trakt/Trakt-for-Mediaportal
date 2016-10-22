using System.Collections.Generic;
using System.Runtime.Serialization;

namespace TraktPlugin.TmdbAPI.DataStructures
{
    [DataContract]
    public class TmdbRequestAge
    {
        [DataMember]
        public string RequestAge { get; set; }
    }
}
