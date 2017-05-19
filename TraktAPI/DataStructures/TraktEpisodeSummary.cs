using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace TraktAPI.DataStructures
{
    [DataContract]
    public class TraktEpisodeSummary : TraktEpisode
    {
        [DataMember(Name = "number_abs")]
        public int? AbsoluteNumber { get; set; }

        [DataMember(Name = "first_aired")]
        public string FirstAired { get; set; }

        [DataMember(Name = "updated_at")]
        public string UpdatedAt { get; set; }

        [DataMember(Name = "overview")]
        public string Overview { get; set; }

        [DataMember(Name = "rating")]
        public double? Rating { get; set; }

        [DataMember(Name = "votes")]
        public int Votes { get; set; }

        [DataMember(Name = "available_translations")]
        public List<string> AvailableTranslations { get; set; }
    }
}
