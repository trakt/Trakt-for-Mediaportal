using System.Collections.Generic;

namespace TraktAPI.DataStructures
{
    public class TraktMoviesPopular : TraktPagination
    {
        public IEnumerable<TraktMovieSummary> Movies { get; set; }
    }
}
