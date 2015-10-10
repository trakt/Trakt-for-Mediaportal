using System.Collections.Generic;

namespace TraktPlugin.TraktAPI.DataStructures
{
    public class TraktMoviesAnticipated : TraktPagination
    {
        public IEnumerable<TraktMovieAnticipated> Movies { get; set; }
    }
}
