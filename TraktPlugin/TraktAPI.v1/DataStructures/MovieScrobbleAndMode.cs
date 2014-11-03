using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TraktPlugin.TraktAPI.v1.DataStructures
{
    /// <summary>
    /// Class to pass scrobbling data and state to background worker
    /// </summary>
    class MovieScrobbleAndMode
    {
        public TraktMovieScrobble MovieScrobble { get; set; }
        public TraktScrobbleStates ScrobbleState { get; set; }
    }
}
