using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace TraktPlugin.TraktAPI.DataStructures
{
    [DataContract]
    public class TraktUserList
    {
        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "slug")]
        public string Slug { get; set; }

        [DataMember(Name = "url")]
        public string Url { get; set; }

        [DataMember(Name = "description")]
        public string Description { get; set; }

        [DataMember(Name = "privacy")]
        public string Privacy { get; set; }

        [DataMember(Name = "items")]
        public List<TraktUserListItem> Items { get; set; }

        public int SortOrder { get; set; }
    }

    [DataContract]
    public class TraktUserListItem
    {
        [DataMember(Name = "type")]
        public string Type { get; set; }

        [DataMember(Name = "movie")]
        public TraktMovie Movie { get; set; }

        [DataMember(Name = "show")]
        public TraktShow Show { get; set; }

        [DataMember(Name = "episode")]
        public TraktEpisode Episode { get; set; }

        [DataMember(Name = "season")]
        public string SeasonNumber { get; set; }

        [DataMember(Name = "episode_num")]
        public string EpisodeNumber { get; set; }

        #region Helpers

        public object Images
        {
            get
            {
                object retValue = null;

                switch (Type)
                {
                    case "movie":
                        retValue = Movie.Images;
                        break;

                    case "show":
                    case "season":
                    case "episode":
                        retValue = Show.Images;
                        break;
                }
                return retValue;
            }
        }

        public TraktRatings Ratings
        {
            get
            {
                TraktRatings retValue = null;

                switch (Type)
                {
                    case "movie":
                        retValue = Movie.Ratings;
                        break;

                    case "show":
                        retValue = Show.Ratings;
                        break;

                    case "episode":
                        retValue = Episode.Ratings;
                        break;
                }
                return retValue;
            }
            set
            {
                switch (Type)
                {
                    case "movie":
                        Movie.Ratings = value;
                        break;

                    case "show":
                        Show.Ratings = value;
                        break;

                    case "episode":
                        Episode.Ratings = value;
                        break;
                }
            }
        }

        public string Rating
        {
            get
            {
                string retValue = string.Empty;

                switch (Type)
                {
                    case "movie":
                        retValue = Movie.Rating;
                        break;
                    
                    case "show":
                        retValue = Show.Rating;
                        break;

                    case "episode":
                        retValue = Episode.Rating;
                        break;
                }
                return retValue;
            }
            set
            {
                switch (Type)
                {
                    case "movie":
                        Movie.Rating = value;
                        break;

                    case "show":
                        Show.Rating = value;
                        break;

                    case "episode":
                        Episode.Rating = value;
                        break;
                }
            }
        }

        public bool InWatchList
        {
            get
            {
                bool retValue = false;

                switch (Type)
                {
                    case "movie":
                        retValue = Movie.InWatchList;
                        break;

                    case "show":
                        retValue = Show.InWatchList;
                        break;

                    case "episode":
                        retValue = Episode.InWatchList;
                        break;
                }
                return retValue;
            }
            set
            {
                switch (Type)
                {
                    case "movie":
                        Movie.InWatchList = value;
                        break;

                    case "show":
                        Show.InWatchList = value;
                        break;

                    case "episode":
                        Episode.InWatchList = value;
                        break;
                }
            }
        }

        public bool InCollection
        {
            get
            {
                bool retValue = false;

                switch (Type)
                {
                    case "movie":
                        retValue = Movie.InCollection;
                        break;

                    case "episode":
                        retValue = Episode.InCollection;
                        break;
                }
                return retValue;
            }
            set
            {
                switch (Type)
                {
                    case "movie":
                        Movie.InCollection = value;
                        break;

                    case "episode":
                        Episode.InCollection = value;
                        break;
                }
            }
        }

        public int Plays
        {
            get
            {
                int retValue = 0;

                switch (Type)
                {
                    case "movie":
                        retValue = Movie.Plays;
                        break;

                    case "episode":
                        retValue = Episode.Plays;
                        break;
                }
                return retValue;
            }
            set
            {
                switch (Type)
                {
                    case "movie":
                        Movie.Plays = value;
                        break;

                    case "episode":
                        Episode.Plays = value;
                        break;
                }
            }
        }

        public bool Watched
        {
            get
            {
                bool retValue = false;

                switch (Type)
                {
                    case "movie":
                        retValue = Movie.Watched;
                        break;

                    case "episode":
                        retValue = Episode.Watched;
                        break;
                }
                return retValue;
            }
            set
            {
                switch (Type)
                {
                    case "movie":
                        Movie.Watched = value;
                        break;

                    case "episode":
                        Episode.Watched = value;
                        break;
                }
            }
        }

        public string Year
        {
            get
            {
                string retValue = string.Empty;

                switch (Type)
                {
                    case "movie":
                        retValue = Movie.Year;
                        break;

                    case "show":
                    case "season":
                    case "episode":
                        retValue = Show.Year.ToString();
                        break;
                }
                return retValue;
            }
        }

        public string Title
        {
            get
            {
                string retValue = string.Empty;

                switch (Type)
                {
                    case "movie":
                        retValue = Movie.Title;
                        break;

                    case "show":
                    case "season":
                    case "episode":
                        retValue = Show.Title;
                        break;
                }
                return retValue;
            }
        }

        public string ImdbId
        {
            get
            {
                string retValue = string.Empty;

                switch (Type)
                {
                    case "movie":
                        retValue = Movie.Imdb;
                        break;

                    case "show":
                    case "season":
                    case "episode":
                        retValue = Show.Imdb;
                        break;
                }
                return retValue;
            }
        }

        #endregion

        #region Overrides
        public override string ToString()
        {
            string retValue = string.Empty;

            switch (Type)
            {
                case "movie":
                    retValue = Movie.Title;
                    break;

                case "show":
                    retValue = Show.Title;
                    break;

                case "season":
                    retValue = string.Format("{0} {1} {2}", Show.Title, GUI.Translation.Season, SeasonNumber);
                    break;

                case "episode":
                    retValue = string.Format("{0} - {1}x{2}{3}", Show.Title, SeasonNumber, EpisodeNumber, string.IsNullOrEmpty(Episode.Title) ? string.Empty : " - " + Episode.Title);
                    break;
            }
            return retValue;
        }
        #endregion
    }
}