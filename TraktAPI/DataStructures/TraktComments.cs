using System.Collections.Generic;

namespace TraktAPI.DataStructures
{
    public class TraktComments : TraktPagination
    {
        public IEnumerable<TraktCommentItem> Comments { get; set; }
    }
}
