using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Xml;
using System.Text.RegularExpressions;
using MediaPortal.Configuration;
using MediaPortal.GUI.Library;
using MediaPortal.Localisation;

namespace TraktPlugin.GUI
{
    public static class Translation
    {
        #region Private variables
        
        private static Dictionary<string, string> translations;
        private static Regex translateExpr = new Regex(@"\$\{([^\}]+)\}");
        private static string path = string.Empty;

        #endregion

        #region Constructor

        static Translation()
        {
            
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the translated strings collection in the active language
        /// </summary>
        public static Dictionary<string, string> Strings
        {
            get
            {
                if (translations == null)
                {
                    translations = new Dictionary<string, string>();
                    Type transType = typeof(Translation);
                    FieldInfo[] fields = transType.GetFields(BindingFlags.Public | BindingFlags.Static);
                    foreach (FieldInfo field in fields)
                    {
                        translations.Add(field.Name, field.GetValue(transType).ToString());
                    }
                }
                return translations;
            }
        }

        public static string CurrentLanguage
        {
            get
            {
                string language = string.Empty;
                try
                {
                    language = GUILocalizeStrings.GetCultureName(GUILocalizeStrings.CurrentLanguage());
                }
                catch (Exception)
                {
                    language = CultureInfo.CurrentUICulture.Name;
                }
                return language;
            }
        }
        public static string PreviousLanguage { get; set; }

        #endregion

        #region Public Methods

        public static void Init()
        {
            translations = null;
            TraktLogger.Info("Using language " + CurrentLanguage);

            path = Config.GetSubFolder(Config.Dir.Language, "Trakt");

            if (!System.IO.Directory.Exists(path))
                System.IO.Directory.CreateDirectory(path);

            string lang = PreviousLanguage = CurrentLanguage;
            LoadTranslations(lang);

            // publish all available translation strings
            // so skins have access to them
            foreach (string name in Strings.Keys)
            {
                GUIUtils.SetProperty("#Trakt.Translation." + name + ".Label", Translation.Strings[name]);
            }
        }

        public static int LoadTranslations(string lang)
        {
            XmlDocument doc = new XmlDocument();
            Dictionary<string, string> TranslatedStrings = new Dictionary<string, string>();
            string langPath = string.Empty;
            try
            {
                langPath = Path.Combine(path, lang + ".xml");
                doc.Load(langPath);
            }
            catch (Exception e)
            {
                if (lang == "en")
                    return 0; // otherwise we are in an endless loop!

                if (e.GetType() == typeof(FileNotFoundException))
                    TraktLogger.Warning("Cannot find translation file {0}. Falling back to English", langPath);
                else
                    TraktLogger.Error("Error in translation xml file: {0}. Falling back to English", lang);

                return LoadTranslations("en");
            }
            foreach (XmlNode stringEntry in doc.DocumentElement.ChildNodes)
            {
                if (stringEntry.NodeType == XmlNodeType.Element)
                    try
                    {
                        TranslatedStrings.Add(stringEntry.Attributes.GetNamedItem("Field").Value, stringEntry.InnerText);
                    }
                    catch (Exception ex)
                    {
                        TraktLogger.Error("Error in Translation Engine", ex.Message);
                    }
            }

            Type TransType = typeof(Translation);
            FieldInfo[] fieldInfos = TransType.GetFields(BindingFlags.Public | BindingFlags.Static);
            foreach (FieldInfo fi in fieldInfos)
            {
                if (TranslatedStrings != null && TranslatedStrings.ContainsKey(fi.Name))
                    TransType.InvokeMember(fi.Name, BindingFlags.SetField, null, TransType, new object[] { TranslatedStrings[fi.Name] });
                else
                    TraktLogger.Info("Translation not found for field: {0}.  Using hard-coded English default.", fi.Name);
            }
            return TranslatedStrings.Count;
        }

        public static string GetByName(string name)
        {
            if (!Strings.ContainsKey(name))
                return name;

            return Strings[name];
        }

        public static string GetByName(string name, params object[] args)
        {
            return String.Format(GetByName(name), args);
        }

        /// <summary>
        /// Takes an input string and replaces all ${named} variables with the proper translation if available
        /// </summary>
        /// <param name="input">a string containing ${named} variables that represent the translation keys</param>
        /// <returns>translated input string</returns>
        public static string ParseString(string input)
        {
            MatchCollection matches = translateExpr.Matches(input);
            foreach (Match match in matches)
            {
                input = input.Replace(match.Value, GetByName(match.Groups[1].Value));
            }
            return input;
        }

        #endregion

        #region Translations / Strings

        /// <summary>
        /// These will be loaded with the language files content
        /// if the selected lang file is not found, it will first try to load en(us).xml as a backup
        /// if that also fails it will use the hardcoded strings as a last resort.
        /// </summary>

        // A
        public static string AddToLibrary = "Add to Library";
        public static string AddToWatchList = "Add to Watch List";
        public static string AirDate = "Air Date";
        public static string AirDay = "Air Day";
        public static string AirTime = "Air Time";

        // B
        

        // C
        public static string Calendar = "Calendar";
        public static string CalendarMyShows = "My Shows";
        public static string CalendarPremieres = "Premieres";
        public static string Certification = "Certification";
        public static string ChangeView = "Change View...";
        public static string ChangeLayout = "Change Layout...";

        // D
        

        // E
        public static string Episode = "Episode";
        public static string Episodes = "Episodes";
        public static string Error = "Error";
        public static string ErrorCalendar = "Error getting calendar.";

        // F
        public static string FirstAired = "First Aired";
        public static string Friend = "Friend";
        public static string Friends = "Friends";
        public static string FullName = "Full Name";

        // G
        public static string Gender = "Gender";
        public static string GettingCalendar = "Getting Calendar";
        public static string GettingFriendsList = "Getting Friends List";
        public static string GettingFriendsWatchedHistory = "Getting Friends Watched History";
        public static string GettingTrendingMovies = "Getting Trending Movies";
        public static string GettingTrendingShows = "Getting Trending Shows";
        public static string GettingRecommendedMovies = "Getting Recommended Movies";
        public static string GettingRecommendedShows = "Getting Recommended Shows";

        // H
        public static string Hate = "Hate";
        public static string Hated = "Hated";

        // I

        // I

        // J
        public static string JoinDate = "Join Date";
        public static string Joined = "Joined";

        // L
        public static string Location = "Location";
        public static string Layout = "Layout";
        public static string Love = "Love";
        public static string Loved = "Loved";

        // M
        public static string MarkAsWatched = "Mark as Watched";
        public static string Movie = "Movie";
        public static string Movies = "Movies";

        // N
        public static string Name = "Name";
        public static string Network = "Network";
        public static string NextWeek = "Next Week";
        public static string NoEpisodeSummary = "Episode summary is currently not available.";
        public static string NoEpisodesThisWeek = "No episodes on this week";
        public static string NoFriends = "No Friends!";
        public static string NoFriendsTaunt = "You have no Friends!";
        public static string NoTrendingMovies = "No Movies current being watched!";
        public static string NoTrendingShows = "No Shows current being watched!";
        public static string NoMovieRecommendations = "No Movie Recommendations Found!";
        public static string NoShowRecommendations = "No Show Recommendations Found!";

        // O
        public static string OK = "OK";
        public static string Overview = "Overview";

        // P
        public static string Percentage = "Percentage";
        public static string Protected = "Protected";
        public static string PersonWatching = "1 Person Watching";
        public static string PeopleWatching = "{0} People Watching";

        // R
        public static string Rate = "Rate";
        public static string Rated = "Rated";
        public static string Rating = "Rating";
        public static string RateMovie = "Rate Movie...";
        public static string RateShow = "Rate Show...";
        public static string RateEpisode = "Rate Episode...";
        public static string RateDialog = "Trakt Rate Dialog";
        public static string RateHate = "Weak Sauce :(";
        public static string RateLove = "Totally Ninja!";
        public static string RateHeading = "What do you think?";
        public static string Recommendations = "Recommendations";
        public static string RecommendedMovies = "Recommended Movies";
        public static string RecommendedShows = "Recommended Shows";
        public static string Released = "Released";
        public static string ReleaseDate = "Release Date";
        public static string RemoveFromLibrary = "Remove from Library";
        public static string RemoveFromWatchList = "Remove from Watch List";
        public static string Runtime = "Runtime";

        // S
        public static string Score = "Score";
        public static string Scrobble = "Scrobble";
        public static string Season = "Season";        
        public static string Series = "Series";
        public static string SeriesPlural = "Series";        
        public static string Settings = "Settings";        

        // T
        public static string Timeout = "Timeout";
        public static string Trending = "Trending";
        public static string TrendingShows = "Trending Shows";
        public static string TrendingMovies = "Trending Movies";
        public static string TVShow = "TV Show";
        public static string TVShows = "TV Shows";
        public static string Tagline = "Tagline";
        public static string Title = "Title";
        public static string Trailer = "Trailer";
        
        // U
        public static string UserHasNotWatchedEpisodes = "User has not watched any episodes!";
        public static string UserHasNotWatchedMovies = "User has not watched any movies!";
        public static string User = "User";
        public static string Username = "Username";
        public static string UnAuthorized = "Authentication failed, please check username and password in configuration.";

        // V
        public static string View = "View";
        public static string Votes = "Votes";

        // W
        public static string WatchList = "Watch List";
        public static string Watched = "Watched";
        public static string WatchedMovies = "Watched Movies";
        public static string WatchedEpisodes = "Watched Episodes";
        public static string Watching = "Watching";

        // Y
        public static string Year = "Year";

        #endregion

    }

}