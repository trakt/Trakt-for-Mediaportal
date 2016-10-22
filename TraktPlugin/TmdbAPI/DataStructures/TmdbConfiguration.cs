using System.Collections.Generic;
using System.Runtime.Serialization;

namespace TraktPlugin.TmdbAPI.DataStructures
{
    [DataContract]
    public class TmdbConfiguration
    {
        [DataMember(Name = "images")]
        public ImageConfiguration Images { get; set; }

        [DataMember(Name = "changes_keys")]
        public string ChangeKeys { get; set; }

        [DataContract]
        public class ImageConfiguration
        {
            [DataMember(Name = "base_url")]
            public string BaseUrl { get; set; }

            [DataMember(Name = "secure_base_url")]
            public string SecureBaseUrl { get; set; }

            [DataMember(Name = "backdrop_sizes")]
            public List<string> BackdropSizes { get; set; }

            [DataMember(Name = "logo_sizes")]
            public List<string> LogoSizes { get; set; }

            [DataMember(Name = "poster_sizes")]
            public List<string> PosterSizes { get; set; }

            [DataMember(Name = "profile_sizes")]
            public List<string> ProfileSizes { get; set; }
        }
    }
}
