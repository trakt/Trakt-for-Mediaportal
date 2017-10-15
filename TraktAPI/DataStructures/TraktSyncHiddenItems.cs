using System.Collections.Generic;
using System.Runtime.Serialization;

namespace TraktAPI.DataStructures
{
    [DataContract]
    public class TraktSyncHiddenItems
    {
        [DataMember(Name = "movies", EmitDefaultValue = false)]
        public List<TraktMovie> Movies { get; set; }

        [DataMember(Name = "shows", EmitDefaultValue = false)]
        public List<TraktShow> Shows { get; set; }

        [DataMember(Name = "seasons", EmitDefaultValue = false)]
        public List<TraktSeason> Seasons { get; set; }
    }
}