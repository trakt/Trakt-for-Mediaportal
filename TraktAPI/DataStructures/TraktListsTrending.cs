using System.Collections.Generic;

namespace TraktAPI.DataStructures
{
    public class TraktListsTrending : TraktPagination
    {
        public IEnumerable<TraktListTrending> Lists { get; set; }
    }
}
