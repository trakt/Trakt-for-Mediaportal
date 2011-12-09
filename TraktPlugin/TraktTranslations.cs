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
        public static string Account = "Account";
        public static string AccountSetup = "Account Setup";
        public static string AccountDialog = "Account Dialog";
        public static string AddFriend = "Add Friend...";
        public static string AddToLibrary = "Add to Library";
        public static string AddToList = "Add to List";
        public static string AddToWatchList = "Add to Watch List";
        public static string AddShowToWatchList = "Add Show to Watch List";
        public static string AddEpisodeToWatchList = "Add Episode to Watch List";
        public static string AddShowToList = "Add Show to List";
        public static string AddEpisodeToList = "Add Episode to List";
        public static string AddThisItemToWatchList = "Add this item to your watch list?";
        public static string Age = "Age";
        public static string AirDate = "Air Date";
        public static string AirDay = "Air Day";
        public static string AirTime = "Air Time";
        public static string Approve = "Approve";
        public static string ApprovedDate = "Approved Date";
        public static string ApproveFriendMessage = "Would you like to add user {0}\nas a friend?";
        public static string AdvancedSettings = "Advanced Settings";

        // B
        

        // C
        public static string Cancel = "Cancel";
        public static string Calendar = "Calendar";
        public static string CalendarMyShows = "My Shows";
        public static string CalendarPremieres = "Premieres";
        public static string CalendarAllShows = "All Shows";
        public static string Certification = "Certification";
        public static string ChangeView = "Change View...";
        public static string ChangeLayout = "Change Layout...";
        public static string Create = "Create";
        public static string CreateAccount = "Create Account";
        public static string CreateNewAccount = "Create New Account...";
        public static string CreatingAccount = "Creating Account...";
        public static string CreateMovingPicturesCategories = "Create Moving Pictures Categories";
        public static string CreateMovingPicturesFilters = "Create Moving Pictures Filters";
        public static string CreatingCategories = "Creating Categories";
        public static string CreatingFilters = "Creating Filters";
        public static string CreatingList = "Creating List";
        public static string CreateList = "Create a new List...";
        public static string ConfirmDeleteList = "Are you sure you want to delete\nthis list?";
        public static string ConfirmDeleteListItem = "Are you sure you want to delete\nthis item from the list?";
        public static string CopyList = "Copy List...";        

        // D
        public static string DateToday = "Today";
        public static string DateYesterday = "Yesterday";
        public static string DateOneWeekAgo = "1 Week Ago";
        public static string DateTwoWeeksAgo = "2 Weeks Ago";
        public static string DateOneMonthAgo = "1 Month Ago";
        public static string DeleteList = "Delete List";
        public static string DeleteListItem = "Delete List Item";
        public static string DeletingList = "Deleting List";
        public static string Deny = "Deny";
        public static string DeleteFriend = "Delete Friend";
        public static string DeleteFriendMessage = "Are you sure you want to delete\n{0} as a friend?";
        public static string DisconnectAccount = "Disconnect Account: {0}";
        public static string DismissRecommendation = "Dismiss this Recommendation";
        public static string DownloadFanart = "Download Fanart";
        public static string DownloadFullSizeFanart = "Download Original Size Fanart";

        // E
        public static string EditList = "Edit List...";
        public static string EditingList = "Editing List";
        public static string Email = "Email";
        public static string Episode = "Episode";
        public static string Episodes = "Episodes";
        public static string Error = "Trakt Error";
        public static string ErrorCalendar = "Error getting calendar.";        

        // F
        public static string FirstAired = "First Aired";
        public static string Friend = "Friend";
        public static string Friends = "Friends";
        public static string FriendRequest = "Friend Request";
        public static string FriendRequestMessage = "You have {0} friend requests, approve or deny from friends window";
        public static string FullName = "Full Name";

        // G
        public static string Gender = "Gender";
        public static string General = "General";
        public static string GeneralSettings = "General Settings";
        public static string GetFriendRequestsOnStartup = "Get Friend Requests on Startup";
        public static string GettingCalendar = "Getting Calendar";
        public static string GettingFriendsList = "Getting Friends List";
        public static string GettingFriendsRequests = "Getting Friends Requests";
        public static string GettingFriendsWatchedHistory = "Getting Friends Watched History";
        public static string GettingLists = "Getting Lists";
        public static string GettingListItems = "Getting List Items";
        public static string GettingSearchResults = "Getting Search Results";
        public static string GettingTrendingMovies = "Getting Trending Movies";
        public static string GettingTrendingShows = "Getting Trending Shows";
        public static string GettingRecommendedMovies = "Getting Recommended Movies";
        public static string GettingRecommendedShows = "Getting Recommended Shows";
        public static string GettingShouts = "Getting Shouts";
        public static string GettingWatchListMovies = "Getting Watch List Movies";
        public static string GettingWatchListShows = "Getting Watch List Shows";
        public static string GettingWatchListEpisodes = "Getting Watch List Episodes";
        public static string GettingRelatedMovies = "Getting Related Movies";
        public static string GettingRelatedShows = "Getting Related Shows";

        // H
        public static string Hate = "Hate";
        public static string Hated = "Hated";
        public static string HideTVShowsInWatchlist = "Hide TV Shows in Watch List";
        public static string HideWatched = "Hide Watched";
        public static string HideSpoilers = "Hide Spoilers";
        public static string HiddenToPreventSpoilers = "This shout has been hidden to prevent spoilers, you can change this option from menu.";

        // I
        public static string Inserted = "Inserted";
        public static string Item = "Item";
        public static string Items = "Items";

        // I

        // J
        public static string JoinDate = "Join Date";
        public static string Joined = "Joined";

        // L
        public static string Location = "Location";
        public static string Layout = "Layout";
        public static string List = "List";
        public static string Lists = "Lists";
        public static string Love = "Love";
        public static string Loved = "Loved";
        public static string LoginExistingAccount = "Login to Existing Account...";
        public static string LoggedIn = "Logged In";
        public static string Login = "Login";
        public static string ListNameAlreadyExists = "List with this name already exists!";

        // M
        public static string MarkAsWatched = "Mark as Watched";
        public static string MarkAsUnWatched = "Mark as UnWatched";
        public static string Movie = "Movie";
        public static string Movies = "Movies";
        public static string MultiSelectDialog = "Multi-Select Dialog";

        // N
        public static string Name = "Name";
        public static string Network = "Network";
        public static string NextWeek = "Next Week";
        public static string NoEpisodeSummary = "Episode summary is currently not available.";
        public static string NoEpisodesThisWeek = "No episodes on this week";
        public static string NoMovieSummary = "Movie summary is currently not available.";
        public static string NoShowSummary = "Show summary is currently not available.";
        public static string NoFriends = "No Friends!";
        public static string NoFriendsTaunt = "You have no Friends!";
        public static string NoTrendingMovies = "No Movies current being watched!";
        public static string NoTrendingShows = "No Shows current being watched!";
        public static string NoMovieRecommendations = "No Movie Recommendations Found!";
        public static string NoShowRecommendations = "No Show Recommendations Found!";
        public static string NoMovieWatchList = "{0} has no movies in Watch List!";
        public static string NoShowWatchList = "{0} has no shows in Watch List!";
        public static string NoEpisodeWatchList = "{0} has no episodes in Watch List!";
        public static string NoShoutsForItem = "No Shouts for {0}!";
        public static string NoPluginsEnabled = "You have defined no plugins in configuration.\nWould you like to configure plugins now?";
        public static string NotLoggedIn = "You can not access this area without being\nlogged in. Would you like to Signup or Login\nto trakt.tv now?";
        public static string NoSearchResultsFound = "No Search Results Found";
        public static string NoMovingPictures = "Moving Pictures is Not Installed or Enabled";
        public static string NoListsFound = "No Lists Found, would you like to\ncreate a list now?";
        public static string NoUserLists = "{0} has not created any lists!";
        public static string NoListItemsFound = "No items found in this list!";
        public static string NoRelatedMovies = "No Related movies found for {0}!";
        public static string NoRelatedShows = "No Related shows found for {0}!";
        public static string No = "No";

        // O
        public static string OK = "OK";
        public static string Overview = "Overview";

        // P
        public static string Password = "Password";
        public static string People = "People";
        public static string Percentage = "Percentage";
        public static string Protected = "Protected";
        public static string PersonWatching = "1 Person Watching";
        public static string PeopleWatching = "{0} People Watching";
        public static string Plugins = "Plugins";
        public static string Plugin = "Plugin";
        public static string Public = "Public";
        public static string Private = "Private";
        public static string Privacy = "Privacy";
        public static string PrivacyPublic = "Anyone can view this list";
        public static string PrivacyFriends = "Only friends can view this list";
        public static string PrivacyPrivate = "Only you can view this list";
        
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
        public static string RelatedMovies = "Related Movies";
        public static string RelatedShows = "Related Shows";
        public static string Released = "Released";
        public static string ReleaseDate = "Release Date";
        public static string RemoveFromLibrary = "Remove from Library";
        public static string RemoveFromWatchList = "Remove from Watch List";
        public static string RemoveShowFromWatchList = "Remove Show from Watch List";
        public static string RemoveEpisodeFromWatchList = "Remove Episode from Watch List";
        public static string RemoveFromList = "Remove from List...";
        public static string Runtime = "Runtime";

        // S
        public static string Score = "Score";
        public static string Scrobble = "Scrobble";
        public static string SearchWithMpNZB = "Search NZB";
        public static string SearchTorrent = "Search Torrent";
        public static string Season = "Season";
        public static string SearchForFriend = "Search for Friend...";
        public static string SendFriendRequest = "Send friend request to {0}?";
        public static string SelectLists = "Select Lists";
        public static string SelectUser = "Select User";
        public static string Series = "Series";
        public static string SeriesPlural = "Series";        
        public static string Settings = "Settings";
        public static string Shout = "Shout";
        public static string Shouts = "Shouts";
        public static string ShowTVShowsInWatchlist = "Show TV Shows in Watch List";
        public static string SkinPluginsOutOfDate = "Error loading window, skin is out of date!\nExit MediaPortal and enter Configuration to\nenable plugins handlers.";
        public static string SkinOutOfDate = "This feature is not available for your\nskin. See if update is Available.";
        public static string SigningIntoAccount = "Signing Into Account...";
        public static string StartDate = "Start Date";
        public static string Synchronize = "Synchronize";
        public static string SynchronizeNow = "New Plugin Handlers have been added.\nWould you like to Synchronize your\nlibraries now?";
        public static string ShowWatched = "Show Watched";
        public static string SettingPluginEnabledName = "Plugin Enabled";
        public static string SettingPluginEnabledDescription = "Enable / Disable this setting to control if the Trakt plugin is loaded with MediaPortal.";
        public static string SettingListedHomeName = "Listed in Home";
        public static string SettingListedHomeDescription = "Enable this setting for the Trakt plugin to appear in the main Home screen menu items.";
        public static string SettingListedPluginsName = "Listed in My Plugins";
        public static string SettingListedPluginsDescription = "Enable this setting for the Trakt plugin to appear in the My Plugins screen menu items.";
        public static string SettingSyncTimerName = "Library Synchronization Period";
        public static string SettingSyncTimerDescription = "Set the period of time (in hours) between each Library Synchronization. Default is every 24 hours.";
        public static string SettingSyncStartDelayName = "Library Synchronization Start Delay";
        public static string SettingSyncStartDelayDescription = "Delay (in seconds) before Library Synchronization starts on MediaPortal Startup. Default is every 0 seconds.";
        public static string SettingWebRequestCacheName = "Web Request Cache Time";
        public static string SettingWebRequestCacheDescription = "Set the period of time (in minutes) that web data is cached in gui windows such as Calendar, Trending and Recommendations. Default is 15 minutes.";
        public static string SettingWebRequestTimeoutName = "Web Request Timeout";
        public static string SettingWebRequestTimeoutDescription = "Set the period of time (in seconds) before cancelling any web requests. Default is 30 seconds.";

        // T
        public static string Timeout = "Timeout";
        public static string Trending = "Trending";
        public static string TrendingShows = "Trending Shows";
        public static string TrendingMovies = "Trending Movies";
        public static string TrendingMoviePeople = "There are {0} people watching {1} movies right now!";
        public static string TrendingTVShowsPeople = "There are {0} people watching {1} tv shows right now!";
        public static string TVShow = "TV Show";
        public static string TVShows = "TV Shows";
        public static string Tagline = "Tagline";
        public static string Title = "Title";
        public static string Trailer = "Trailer";
        public static string Trailers = "Trailers...";
        
        // U
        public static string UserHasNotWatchedEpisodes = "User has not watched any episodes!";
        public static string UserHasNotWatchedMovies = "User has not watched any movies!";
        public static string User = "User";
        public static string Users = "Users";
        public static string Username = "Username";
        public static string UnAuthorized = "Authentication failed, please check username and password in settings.";
        public static string UpdatingCategories = "Updating Categories";
        public static string UpdatingFilters = "Updating Filters";

        // V
        public static string View = "View";
        public static string Votes = "Votes";
        public static string ValidUsername = "You must enter a valid username!";
        public static string ValidPassword = "You must enter a valid password!";
        public static string ValidEmail = "You must enter a valid email!";

        // W
        public static string WatchList = "Watch List";
        public static string Watched = "Watched";
        public static string WatchedMovies = "Watched Movies";
        public static string WatchedEpisodes = "Watched Episodes";
        public static string Watching = "Watching";
        public static string WatchListMovies = "Movie Watch List";
        public static string WatchListShows = "Show Watch List";
        public static string WatchListEpisodes = "Episode Watch List";

        // Y
        public static string Year = "Year";
        public static string Yes = "Yes";

        #endregion

    }

}