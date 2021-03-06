﻿using System.Runtime.Serialization;

namespace TraktAPI.DataStructures
{
    [DataContract]
    public class TraktListPopular
    {
        [DataMember(Name = "like_count")]
        public int Likes { get; set; }

        [DataMember(Name = "comment_count")]
        public int Comments { get; set; }

        [DataMember(Name = "list")]
        public TraktListDetail List { get; set; }
    }
}
