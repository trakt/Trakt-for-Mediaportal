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
    }
}