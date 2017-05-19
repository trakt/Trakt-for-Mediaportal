using System.Collections.Generic;

namespace TraktAPI.DataStructures
{
    public class TraktMoviesAnticipated : TraktPagination
    {
        public IEnumerable<TraktMovieAnticipated> Movies { get; set; }
    }
}
