using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace TraktPlugin.TraktAPI.DataStructures
{
    [DataContract]
    public class TraktComment
    {
        [DataMember(Name = "id")]
        public int Id { get; set; }

        [DataMember(Name = "parent_id")]
        public int ParentId { get; set; }

        [DataMember(Name = "created_at")]
        public string CreatedAt { get; set; }

        [DataMember(Name = "comment")]
        public string Comment { get; set; }

        [DataMember(Name = "spoiler")]
        public bool IsSpoiler { get; set; }

        [DataMember(Name = "review")]
        public bool IsReview { get; set; }

        [DataMember(Name = "replies")]
        public int Replies { get; set; }

        [DataMember(Name = "likes")]
        public int Likes { get; set; }

        [DataMember(Name = "user")]
        public TraktUserSummary User { get; set; }
    }
}
