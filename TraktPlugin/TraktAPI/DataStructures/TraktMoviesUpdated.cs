using System.Collections.Generic;

namespace TraktPlugin.TraktAPI.DataStructures
{
    public class TraktMoviesUpdated : TraktPagination
    {
        public IEnumerable<TraktMovieUpdate> Movies { get; set; }
    }
}
