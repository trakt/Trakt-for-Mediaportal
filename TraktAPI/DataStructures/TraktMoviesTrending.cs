using System.Collections.Generic;

namespace TraktAPI.DataStructures
{
    public class TraktMoviesTrending : TraktPagination
    {
        public int TotalWatchers { get; set; }
        public IEnumerable<TraktMovieTrending> Movies { get; set; }
    }
}
