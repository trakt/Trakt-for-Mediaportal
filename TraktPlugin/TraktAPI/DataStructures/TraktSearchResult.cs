using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace TraktPlugin.TraktAPI.DataStructures
{
    /// <summary>
    /// this class is used to combine multiple search results
    /// </summary>
    public class TraktSearchResult
    {
        public IEnumerable<TraktMovieSummary> Movies = null;
        public IEnumerable<TraktShowSummary> Shows = null;
        public IEnumerable<TraktEpisodeSummary> Episodes = null;
        public IEnumerable<TraktPersonSummary> People = null;
        public IEnumerable<TraktUserSummary> Users = null;
        public IEnumerable<TraktList> Lists = null;

        public int Count
        {
            get
            {
                int retValue = 0;

                if (Movies != null) retValue += Movies.Count();
                if (Shows != null) retValue += Shows.Count();
                if (Episodes != null) retValue += Episodes.Count();
                if (People != null) retValue += People.Count();
                if (Users != null) retValue += Users.Count();
                if (Lists != null) retValue += Lists.Count();

                return retValue;

            }
        }
    }
}
