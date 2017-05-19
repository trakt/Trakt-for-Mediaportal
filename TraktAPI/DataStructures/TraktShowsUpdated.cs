using System.Collections.Generic;

namespace TraktAPI.DataStructures
{
    public class TraktShowsUpdated : TraktPagination
    {
        public IEnumerable<TraktShowUpdate> Shows { get; set; }
    }
}
