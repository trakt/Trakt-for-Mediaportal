using System.Collections.Generic;

namespace TraktAPI.DataStructures
{
    public class TraktLikes : TraktPagination
    {
        public IEnumerable<TraktLike> Likes { get; set; }
    }
}
