using System.Collections.Generic;

namespace TraktAPI.DataStructures
{
    public class TraktListsPopular: TraktPagination
    {
        public IEnumerable<TraktListPopular> Lists { get; set; }
    }
}
