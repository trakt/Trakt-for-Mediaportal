using System.Collections.Generic;
using System.Runtime.Serialization;

namespace TraktPlugin.TmdbAPI.DataStructures
{
    [DataContract]
    public class TmdbImage
    {
        [DataMember(Name = "aspect_ratio")]
        public double AspectRatio { get; set; }

        [DataMember(Name = "file_path")]
        public string FilePath { get; set; }

        [DataMember(Name = "height")]
        public int Height { get; set; }

        [DataMember(Name = "iso_639_1")]
        public string LanguageCode { get; set; }

        [DataMember(Name = "vote_average")]
        public double Score { get; set; }

        [DataMember(Name = "vote_count")]
        public int Votes { get; set; }

        [DataMember(Name = "width")]
        public int Width { get; set; }
    }
}
