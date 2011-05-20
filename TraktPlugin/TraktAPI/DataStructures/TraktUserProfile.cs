using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace TraktPlugin.TraktAPI.DataStructures
{
    [DataContract]
    public class TraktUserProfile : TraktResponse
    {
        [DataMember(Name = "username")]
        public string Username { get; set; }

        [DataMember(Name = "protected")]
        public string Protected { get; set; }

        [DataMember(Name = "full_name")]
        public string FullName { get; set; }

        [DataMember(Name = "gender")]
        public string Gender { get; set; }

        [DataMember(Name = "age")]
        public string Age { get; set; }

        [DataMember(Name = "location")]
        public string Location { get; set; }

        [DataMember(Name = "about")]
        public string About { get; set; }

        [DataMember(Name = "joined")]
        public long JoinDate { get; set; }

        [DataMember(Name = "avatar")]
        public string Avatar { get; set; }

        [DataMember(Name = "url")]
        public string Url { get; set; }

        [DataMember(Name = "stats")]
        public Statistics Stats { get; set; }

        #region Statistics

        [DataContract]
        public class Statistics
        {
            [DataMember(Name = "friends")]
            public string FriendCount { get; set; }

            [DataMember(Name = "shows")]
            public ShowStats Shows { get; set; }

            [DataMember(Name = "episodes")]
            public EpisodeStats Episodes { get; set; }

            [DataMember(Name = "movies")]
            public MovieStats Movies { get; set; }

            [DataContract]
            public class ShowStats
            {
                [DataMember(Name = "library")]
                public string Count { get; set; }
            }

            [DataContract]
            public class EpisodeStats
            {
                [DataMember(Name = "watched")]
                public string WatchedCount { get; set; }

                [DataMember(Name = "watched_unique")]
                public string WatchedUniqueCount { get; set; }

                [DataMember(Name = "watched_trakt")]
                public string WatchedTraktCount { get; set; }

                [DataMember(Name = "watched_trakt_unique")]
                public string WatchedTraktUniqueCount { get; set; }

                [DataMember(Name = "watched_elsewhere")]
                public string WatchedElseWhereCount { get; set; }

                [DataMember(Name = "unwatched")]
                public string UnWatchedCount { get; set; }
            }

            [DataContract]
            public class MovieStats
            {
                [DataMember(Name = "watched")]
                public string WatchedCount { get; set; }

                [DataMember(Name = "watched_trakt")]
                public string WatchedTraktCount { get; set; }

                [DataMember(Name = "watched_elsewhere")]
                public string WatchedElseWhereCount { get; set; }

                [DataMember(Name = "library")]
                public string MovieCount { get; set; }

                [DataMember(Name = "unwatched")]
                public string UnWatchedCount { get; set; }
            }
        }

        #endregion

        [DataMember(Name = "watching")]
        public WatchItem Watching { get; set; }

        [DataMember(Name = "watched")]
        public List<WatchItem> WatchedHistory { get; set; }

        [DataMember(Name = "watched_episodes")]
        public List<WatchItem> WatchedEpisodes { get; set; }

        [DataMember(Name = "watched_movies")]
        public List<WatchItem> WatchedMovies { get; set; }

        #region Watch Item

        [DataContract]
        public class WatchItem
        {
            [DataMember(Name = "episode")]
            public TraktEpisode Episode { get; set; }

            [DataMember(Name = "show")]
            public TraktShow Show { get; set; }

            [DataMember(Name = "movie")]
            public TraktMovie Movie { get; set; }

            [DataMember(Name = "type")]
            public string Type { get; set; }

            [DataMember(Name = "watched")]
            public long WatchedDate { get; set; }

            public override string ToString()
            {
                if (Type == "episode")
                {
                    return string.Format("{0} - {1}x{2}{3}", Show.Title, Episode.Season.ToString(), Episode.Number.ToString(), string.IsNullOrEmpty(Episode.Title) ? string.Empty : " - " + Episode.Title);
                }
                else
                    return Movie.Title;
            }
        }

        #endregion

    }
  
}