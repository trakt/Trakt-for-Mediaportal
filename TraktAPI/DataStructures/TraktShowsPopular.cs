using System.Collections.Generic;

namespace TraktAPI.DataStructures
{
    public class TraktShowsPopular : TraktPagination
    {
        public IEnumerable<TraktShowSummary> Shows { get; set; }
    }
}
