using System.Collections.Generic;

namespace TraktPlugin.TraktAPI.DataStructures
{
    public class TraktShowsPopular : TraktPagination
    {
        public IEnumerable<TraktShowSummary> Shows { get; set; }
    }
}
