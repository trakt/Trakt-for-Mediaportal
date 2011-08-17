using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace TraktPlugin.TraktAPI.DataStructures
{
    /// <summary>
    /// Data structure for Syncing to Trakt
    /// </summary>
    [DataContract]
    public class TraktMovieSync
    {
        [DataMember(Name = "username")]
        public string UserName { get; set; }

        [DataMember(Name = "password")]
        public string Password { get; set; }

        [DataMember(Name = "movies")]
        public List<Movie> MovieList { get; set; }

        [DataContract]
        public class Movie : IEquatable<Movie>
        {
            [DataMember(Name = "imdb_id")]
            public string IMDBID { get; set; }

            [DataMember(Name = "tmdb_id")]
            public string TMDBID { get; set; }

            [DataMember(Name = "title")]
            public string Title { get; set; }

            [DataMember(Name = "year")]
            public string Year { get; set; }

            #region IEquatable
            public bool Equals(Movie other)
            {
                bool result = false;
                if (other != null)
                {
                    if (this.Title.Equals(other.Title) && this.Year.Equals(other.Year) && this.IMDBID.Equals(other.IMDBID))
                    {
                        result = true;
                    }
                }
                return result;
            }
            #endregion
        }
    }
}
