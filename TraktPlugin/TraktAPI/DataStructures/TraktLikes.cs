using System.Collections.Generic;

namespace TraktPlugin.TraktAPI.DataStructures
{
    public class TraktLikes : TraktPagination
    {
        public IEnumerable<TraktLike> Likes { get; set; }
    }
}
