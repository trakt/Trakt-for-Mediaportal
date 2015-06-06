using System.Collections.Generic;

namespace TraktPlugin.TraktAPI.DataStructures
{
    public class TraktComments : TraktPagination
    {
        public IEnumerable<TraktCommentItem> Comments { get; set; }
    }
}
