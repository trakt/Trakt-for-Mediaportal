using System.Collections.Generic;

namespace TraktPlugin.TraktAPI.DataStructures
{
    public class TraktMoviesPopular : TraktPagination
    {
        public IEnumerable<TraktMovieSummary> Movies { get; set; }
    }
}
