using System.Collections.Generic;
using System.Runtime.Serialization;

namespace TraktAPI.DataStructures
{
    [DataContract]
    public class TraktPersonMovieCredits
    {
        [DataMember(Name = "cast")]
        public List<TraktPersonMovieCast> Cast { get; set; }

        [DataMember(Name = "crew")]
        public TraktPersonMovieCrew Crew { get; set; }
    }
}
