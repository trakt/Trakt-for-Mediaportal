using System.Collections.Generic;

namespace TraktPlugin.TraktAPI.DataStructures
{
    public class TraktShowsAnticipated : TraktPagination
    {
        public IEnumerable<TraktShowAnticipated> Shows { get; set; }
    }
}
