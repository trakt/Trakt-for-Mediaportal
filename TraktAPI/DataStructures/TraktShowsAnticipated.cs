using System.Collections.Generic;

namespace TraktAPI.DataStructures
{
    public class TraktShowsAnticipated : TraktPagination
    {
        public IEnumerable<TraktShowAnticipated> Shows { get; set; }
    }
}
