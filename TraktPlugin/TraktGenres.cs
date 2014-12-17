using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TraktPlugin
{
    class TraktGenres
    {
        /// <summary>
        /// Key: Translation Field. Value: Trakt Slug
        /// </summary>
        public static Dictionary<string, string> MovieGenres = new Dictionary<string, string>();
        public static Dictionary<string, string> ShowGenres = new Dictionary<string, string>();

        public static void Init()
        {
            #region load movie genre list
            MovieGenres.Add("Action", "action");
            MovieGenres.Add("Adventure", "adventure");
            MovieGenres.Add("All", null);
            MovieGenres.Add("Animation", "animation");
            MovieGenres.Add("Comedy", "comedy");
            MovieGenres.Add("Crime", "crime");
            MovieGenres.Add("Documentary", "documentary");
            MovieGenres.Add("Drama", "drama");
            MovieGenres.Add("Family", "family");
            MovieGenres.Add("Fantasy", "fantasy");
            MovieGenres.Add("FilmNoir", "film-noir");
            MovieGenres.Add("History", "history");
            MovieGenres.Add("Horror", "horror");
            MovieGenres.Add("Indie", "indie");
            MovieGenres.Add("Music", "music");
            MovieGenres.Add("Musical", "musical");
            MovieGenres.Add("Mystery", "mystery");
            MovieGenres.Add("None", "none");
            MovieGenres.Add("Romance", "romance");
            MovieGenres.Add("ScienceFiction", "science-fiction"); 
            MovieGenres.Add("Sport", "sport");
            MovieGenres.Add("Suspense", "suspense");
            MovieGenres.Add("Thriller", "thriller");
            MovieGenres.Add("War", "war");
            MovieGenres.Add("Western", "western");
            #endregion

            #region load show genre list
            ShowGenres.Add("Action", "action");
            ShowGenres.Add("Adventure", "adventure");
            ShowGenres.Add("All", null);
            ShowGenres.Add("Animation", "animation");
            ShowGenres.Add("Children", "children");
            ShowGenres.Add("Comedy", "comedy");
            ShowGenres.Add("Crime", "crime");
            ShowGenres.Add("Documentary", "documentary");
            ShowGenres.Add("Drama", "drama");
            ShowGenres.Add("Family", "family");
            ShowGenres.Add("Fantasy", "fantasy");
            ShowGenres.Add("GameShow", "game-show");
            ShowGenres.Add("HomeAndGarden", "home-and-garden");
            ShowGenres.Add("MiniSeries", "mini-series");
            ShowGenres.Add("News", "news");
            ShowGenres.Add("None", "none");
            ShowGenres.Add("Reality", "reality");
            ShowGenres.Add("ScienceFiction", "science-fiction");
            ShowGenres.Add("Soap", "soap");
            ShowGenres.Add("SpecialInterest", "special-interest");
            ShowGenres.Add("Sport", "sport");
            ShowGenres.Add("TalkShow", "talk-show");
            ShowGenres.Add("Western", "western");
            #endregion
        }

        public static string Translate(string genreKey)
        {
            return GUI.Translation.GetByName(string.Format("Genre{0}", genreKey));
        }

        public static List<string> Translate(List<string> genreKeys)
        {
            if (genreKeys == null)
                return null;

            List<string> translatedGenres = new List<string>();
            
            foreach (var genre in genreKeys)
            {
                var genreKey = ShowGenres.Union(MovieGenres).FirstOrDefault(g => g.Value == genre);
                if (genreKey.Key == null) continue;

                translatedGenres.Add(GUI.Translation.GetByName(string.Format("Genre{0}", genreKey.Key)));
            }

            return translatedGenres;
        }

        public static string ItemName(string genreKey)
        {
            return string.Format(GUI.Translation.GenreItem, Translate(genreKey));
        }
    }
}