using System.Collections.Generic;

namespace TraktPlugin.TraktAPI.DataStructures
{
    public class TraktShowsUpdated : TraktPagination
    {
        public IEnumerable<TraktShowUpdate> Shows { get; set; }
    }
}
