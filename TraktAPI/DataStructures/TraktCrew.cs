using System.Collections.Generic;
using System.Runtime.Serialization;

namespace TraktAPI.DataStructures
{
    [DataContract]
    public class TraktCrew
    {
        [DataMember(Name = "directing")]
        public List<TraktJob> Directing { get; set; }

        [DataMember(Name = "writing")]
        public List<TraktJob> Writing { get; set; }

        [DataMember(Name = "production")]
        public List<TraktJob> Production { get; set; }

        [DataMember(Name = "art")]
        public List<TraktJob> Art { get; set; }

        [DataMember(Name = "costume & make-up")]
        public List<TraktJob> CostumeAndMakeUp { get; set; }

        [DataMember(Name = "sound")]
        public List<TraktJob> Sound { get; set; }

        [DataMember(Name = "camera")]
        public List<TraktJob> Camera { get; set; }

        [DataMember(Name = "crew")]
        public List<TraktJob> Crew { get; set; }
    }

    [DataContract]
    public class TraktShowCrew
    {
        [DataMember(Name = "directing")]
        public List<TraktShowJob> Directing { get; set; }

        [DataMember(Name = "writing")]
        public List<TraktShowJob> Writing { get; set; }

        [DataMember(Name = "production")]
        public List<TraktShowJob> Production { get; set; }

        [DataMember(Name = "art")]
        public List<TraktShowJob> Art { get; set; }

        [DataMember(Name = "costume & make-up")]
        public List<TraktShowJob> CostumeAndMakeUp { get; set; }

        [DataMember(Name = "sound")]
        public List<TraktShowJob> Sound { get; set; }

        [DataMember(Name = "camera")]
        public List<TraktShowJob> Camera { get; set; }

        [DataMember(Name = "crew")]
        public List<TraktShowJob> Crew { get; set; }
    }
}
