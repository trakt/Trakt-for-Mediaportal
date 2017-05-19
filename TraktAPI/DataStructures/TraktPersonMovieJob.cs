using System.Runtime.Serialization;

namespace TraktAPI.DataStructures
{
    [DataContract]
    public class TraktPersonMovieJob
    {
        [DataMember(Name = "job")]
        public string Job { get; set; }

        [DataMember(Name = "movie")]
        public TraktMovieSummary Movie { get; set; }
    }
}
