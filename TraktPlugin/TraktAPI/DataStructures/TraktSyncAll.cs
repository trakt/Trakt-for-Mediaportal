using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace TraktPlugin.TraktAPI.DataStructures
{
    [DataContract]
    public class TraktSyncAll
    {
        [DataMember(Name = "movies")]
        public List<TraktMovie> Movies { get; set; }

        [DataMember(Name = "shows")]
        public List<TraktShow> Shows { get; set; }

        [DataMember(Name = "seasons")]
        public List<TraktSeason> Seasons { get; set; }

        [DataMember(Name = "episodes")]
        public List<TraktEpisode> Episodes { get; set; }

        [DataMember(Name = "people")]
        public List<TraktPerson> People { get; set; }
    }
}