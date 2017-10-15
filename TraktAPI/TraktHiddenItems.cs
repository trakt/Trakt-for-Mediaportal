using System.Collections.Generic;

namespace TraktAPI.DataStructures
{
    public class TraktHiddenItems : TraktPagination
    {
        public IEnumerable<TraktHiddenItem> HiddenItems { get; set; }
    }
}
