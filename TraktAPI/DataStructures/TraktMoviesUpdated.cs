using System.Collections.Generic;

namespace TraktAPI.DataStructures
{
    public class TraktMoviesUpdated : TraktPagination
    {
        public IEnumerable<TraktMovieUpdate> Movies { get; set; }
    }
}
