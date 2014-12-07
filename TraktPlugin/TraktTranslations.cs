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
                    translations = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
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
                {
                    try
                    {
                        string key = stringEntry.Attributes.GetNamedItem("name").Value;
                        if (!TranslatedStrings.ContainsKey(key))
                        {
                            TranslatedStrings.Add(key, stringEntry.InnerText.NormalizeTranslation());
                        }
                        else
                        {
                            TraktLogger.Error("Error in Translation Engine, the translation key '{0}' already exists.", key);
                        }
                        
                    }
                    catch (Exception ex)
                    {
                        TraktLogger.Error("Error in Translation Engine: {0}", ex.Message);
                    }
                }
            }

            Type TransType = typeof(Translation);
            FieldInfo[] fieldInfos = TransType.GetFields(BindingFlags.Public | BindingFlags.Static);
            foreach (FieldInfo fi in fieldInfos)
            {
                if (TranslatedStrings != null && TranslatedStrings.ContainsKey(fi.Name))
                    TransType.InvokeMember(fi.Name, BindingFlags.SetField, null, TransType, new object[] { TranslatedStrings[fi.Name] });
                else
                    TraktLogger.Info("Translation not found for field: {0}. Using hard-coded English default.", fi.Name);
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

        /// <summary>
        /// Temp workaround to remove unwanted chars from Transifex
        /// </summary>
        public static string NormalizeTranslation(this string input)
        {
            input = input.Replace("\\'", "'");
            input = input.Replace("\\\"", "\"");
            return input;
        }

        /// <summary>
        /// Extension Method to get translations
        /// </summary>
        public static string Translate(this string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            return GetByName(key);
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
        public static string Activity = "Activity";
        public static string Actor = "Actor";
        public static string Actors = "Actors";
        public static string ActivityFriendsDesc = "See what your friends are up to...";
        public static string ActivityFriendsAndMeDesc = "See what you and your friends are up to...";
        public static string ActivityCommunityDesc = "See what the community is up to...";
        public static string ActivityFollowersDesc = "See what your followers are up to...";
        public static string ActivityFollowingDesc = "See activity for people you're following...";
        public static string ActivityWatching = "{0} is watching {1}";
        public static string ActivityWatched = "{0} watched {1}";
        public static string ActivityCheckedIn = "{0} checked into {1}";
        public static string ActivityCollected = "{0} collected {1}";
        public static string ActivityCollectedEpisodes = "{0} collected {1} episodes of {2}";
        public static string ActivitySeen = "{0} seen {1}";
        public static string ActivitySeenEpisodes = "{0} seen {1} episodes of {2}";
        public static string ActivityRating = "{0} rated {1}";
        public static string ActivityRatingAdvanced = "{0} rated {1} [{2}/10]";
        public static string ActivityWatchlist = "{0} added {1} to their watchlist";
        public static string ActivityAddToList = "{0} added {1} to {2}";
        public static string ActivityCreatedList = "{0} created list {1}";
        public static string ActivityReview = "{0} reviewed {1}";
        public static string ActivityShouts = "{0} shouted on {1}";
        public static string Activities = "Activities";
        public static string AddFriend = "Add Friend...";
        public static string AddToLibrary = "Add to Collection";
        public static string AddToList = "Add to List...";
        public static string AddToWatchList = "Add to Watchlist";
        public static string AddShowToWatchList = "Add Show to Watchlist";
        public static string AddEpisodeToWatchList = "Add Episode to Watchlist";
        public static string AddShowToList = "Add Show to List";
        public static string AddEpisodeToList = "Add Episode to List";
        public static string AddThisItemToWatchList = "Add this item to your watchlist?";
        public static string Age = "Age";
        public static string AirDate = "Air Date";
        public static string AirDay = "Air Day";
        public static string AirTime = "Air Time";
        public static string Approve = "Approve";
        public static string ApproveAndFollowBack = "Approve and Follow Back";
        public static string ApprovedDate = "Approved Date";
        public static string ApproveFriendMessage = "Would you like to add user {0}\nas a friend?";
        public static string ApproveFollowerMessage = "Would you like to allow user {0}\nto follow you?";
        public static string ApproveFollowerAndFollowBackMessage = "Would you like to allow user {0}\nto follow you and follow user back?";
        public static string AdvancedSettings = "Advanced Settings";

        // B
        public static string BufferingTrailer = "Buffering Trailer";
        public static string Biography = "Biography";
        public static string Birthday = "Birthday";
        public static string BirthDate = "Birth Date";
        public static string Born = "Born";
        public static string BirthPlace = "Birth Place";

        // C
        public static string Cancel = "Cancel";
        public static string Canceled = "Cancelled";
        public static string Calendar = "Calendar";
        public static string CalendarMyShows = "My Shows";
        public static string CalendarPremieres = "Premieres";
        public static string CalendarAllShows = "All Shows";
        public static string Certification = "Certification";
        public static string Cast = "Cast";
        public static string ChangeView = "Change View...";
        public static string ChangeLayout = "Change Layout...";
        public static string Create = "Create";
        public static string CreateAccount = "Create Account";
        public static string CreateNewAccount = "Create New Account...";
        public static string CreatingAccount = "Creating Account...";
        public static string CreateMovingPicturesCategories = "Create Moving Pictures Categories";
        public static string CreateMovingPicturesFilters = "Create Moving Pictures Filters";
        public static string CreateMyFilmsCategories = "Create My Films Categories";
        public static string CreatingCategories = "Creating Categories";
        public static string CreatingFilters = "Creating Filters";
        public static string CreatingList = "Creating List";
        public static string CreateList = "Create a new List...";
        public static string Community = "Community";
        public static string CommunityActivity = "Community Activity";
        public static string ConfirmDeleteList = "Are you sure you want to delete\nthis list?";
        public static string ConfirmDeleteListItem = "Are you sure you want to delete\nthis item from the list?";
        public static string Continuing = "Continuing";
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
        public static string Dead = "Dead";
        public static string Deny = "Deny";
        public static string DenyFollowRequest = "Deny Follow Request from user {0}?";
        public static string DeleteFriend = "Delete Friend";
        public static string DeleteFriendMessage = "Are you sure you want to delete\n{0} as a friend?";
        public static string Died = "Died";
        public static string Director = "Director";
        public static string Directors = "Directors";
        public static string DisconnectAccount = "Disconnect Account: {0}";
        public static string DismissRecommendation = "Dismiss this Recommendation";
        public static string DontIncludeMeInFriendsActivity = "Don't Include Me in Friends Activity";
        public static string DownloadFanart = "Download Fanart";
        public static string DownloadFullSizeFanart = "Download Original Size Fanart";

        // E
        public static string EditList = "Edit List...";
        public static string EditingList = "Editing List";
        public static string Email = "Email";
        public static string Ended = "Ended";
        public static string EndYear = "End Year: {0}";
        public static string EnterSearchTerm = "Enter Search Term";
        public static string Episode = "Episode";
        public static string Episodes = "Episodes";
        public static string Error = "Trakt Error";
        public static string ErrorCalendar = "Error getting calendar";
        public static string ExecutiveProducer = "Executive Producer";

        // F
        public static string FailedCreateList = "Failed to create list from online";
        public static string FailedDeleteList = "Failed to delete list from online";
        public static string FailedUpdateList = "Failed to update list online";
        public static string Filters = "Filters";
        public static string FirstAired = "First Aired";
        public static string Follow = "Follow";
        public static string Followed = "Followed";
        public static string Follower = "Follower";
        public static string Followers = "Followers";
        public static string Following = "Following";
        public static string FollowUser = "Follow User";
        public static string FollowPendingApproval = "Follow request has been sent to {0} and is pending approval";
        public static string Friend = "Friend";
        public static string Friends = "Friends";
        public static string FriendsAndMe = "Friends and Me";
        public static string FriendActivity = "Friend Activity";
        public static string FriendRequest = "Friend Request";
        public static string FollowerRequest = "Follower Request";
        public static string FollowerRequests = "Follower Requests";
        public static string FriendRequestMessage = "You have {0} friend requests, approve or deny from friends window";
        public static string FollowerRequestMessage = "You have {0} follower requests, approve or deny from network window";
        public static string FullName = "Full Name";

        // G
        public static string Gender = "Gender";
        public static string General = "General";
        public static string GeneralSettings = "General Settings";
        public static string Genre = "Genre";
        public static string GenreItem = "Genre: {0}";
        public static string GenreAction = "Action";
        public static string GenreAdventure = "Adventure";
        public static string GenreAll = "All";
        public static string GenreAnimation = "Animation";
        public static string GenreChildren = "Children";
        public static string GenreComedy = "Comedy";
        public static string GenreCrime = "Crime";
        public static string GenreDocumentary = "Documentary";
        public static string GenreDrama = "Drama";
        public static string GenreFamily = "Family";
        public static string GenreFantasy = "Fantasy";
        public static string GenreGameShow = "Game Show";
        public static string GenreFilmNoir = "Film Noir";
        public static string GenreHistory = "History";
        public static string GenreHomeAndGarden = "Home And Garden";
        public static string GenreHorror = "Horror";
        public static string GenreIndie = "Indie";
        public static string GenreMiniSeries = "Mini Series";
        public static string GenreMusic = "Music";
        public static string GenreMusical = "Musical";
        public static string GenreMystery = "Mystery";
        public static string GenreNews = "News";
        public static string GenreNone = "None";
        public static string GenreReality = "Reality";
        public static string GenreRomance = "Romance";
        public static string GenreScienceFiction = "Science Fiction";
        public static string GenreSoap = "Soap";
        public static string GenreSpecialInterest = "Special Interest";
        public static string GenreSport = "Sport";
        public static string GenreSuspense = "Suspense";
        public static string GenreTalkShow = "Talk Show";
        public static string GenreThriller = "Thriller";
        public static string GenreWar = "War";
        public static string GenreWestern = "Western";
        public static string GetFriendRequestsOnStartup = "Get Friend Requests on Startup";
        public static string GetFollowerRequestsOnStartup = "Get Follower Requests on Startup";
        public static string GettingActivity = "Getting Activity";
        public static string GettingCalendar = "Getting Calendar";
        public static string GettingEpisodes = "Getting Episodes";
        public static string GettingFriendsList = "Getting Friends List";
        public static string GettingFriendsRequests = "Getting Friends Requests";
        public static string GettingFollowerRequests = "Getting Follower Requests";
        public static string GettingFollowerList = "Getting Follower List";
        public static string GettingFollowingList = "Getting Following List";
        public static string GettingUserWatchedHistory = "Getting User Watched History";
        public static string GettingUserRecentAdded = "Getting User Recently Added";
        public static string GettingUserRecentShouts = "Getting User Recent Shouts";
        public static string GettingFriendsWatchedHistory = "Getting Friends Watched History";
        public static string GettingLists = "Getting Lists";
        public static string GettingListItems = "Getting List Items";
        public static string GettingSearchResults = "Getting Search Results";
        public static string GettingTrendingMovies = "Getting Trending Movies";
        public static string GettingTrendingShows = "Getting Trending Shows";
        public static string GettingRecommendedMovies = "Getting Recommended Movies";
        public static string GettingRecommendedShows = "Getting Recommended Shows";
        public static string GettingShouts = "Getting Shouts";
        public static string GettingShowSeasons = "Getting Show Seasons";
        public static string GettingWatchListMovies = "Getting Watchlist Movies";
        public static string GettingWatchListShows = "Getting Watchlist Shows";
        public static string GettingWatchListEpisodes = "Getting Watchlist Episodes";
        public static string GettingRelatedMovies = "Getting Related Movies";
        public static string GettingRelatedShows = "Getting Related Shows";
        public static string GettingTrailerUrls = "Getting Trailer Urls";
        public static string GettingUserProfile = "Getting User Profile";
        public static string GuestStar = "Gueststar";
        public static string GuestStars = "Gueststars";

        // H
        public static string Hate = "Hate";
        public static string Hated = "Hated";
        public static string HideTVShowsInWatchlist = "Hide TV Shows in Watchlist";
        public static string HideCollected = "Hide Collected";
        public static string HideWatched = "Hide Watched";
        public static string HideRated = "Hide Rated";
        public static string HideSpoilers = "Hide Spoilers";
        public static string HideWatchlisted = "Hide Watchlisted";
        public static string HiddenToPreventSpoilers = "This shout has been hidden to prevent spoilers, you can change this option from menu.";

        // I
        public static string IncludeMeInFriendsActivity = "Include Me in Friends Activity";
        public static string Inserted = "Inserted";
        public static string InProduction = "In Production";
        public static string Item = "Item";
        public static string Items = "Items";        

        // J
        public static string JoinDate = "Join Date";
        public static string Joined = "Joined";

        // K

        // L
        public static string Location = "Location";
        public static string Layout = "Layout";
        public static string Like = "Like";
        public static string Likes = "Likes";
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
        public static string Me = "Me";
        public static string Movie = "Movie";
        public static string Movies = "Movies";
        public static string MultiSelectDialog = "Multi-Select Dialog";

        // N
        public static string Name = "Name";
        public static string Network = "Network";
        public static string NextEpisode = "Next Episode";
        public static string NextWeek = "Next Week";
        public static string NoActivities = "No Activity Found.";
        public static string NoPersonBiography = "There is no biography entered for this person.";
        public static string NoEpisodesInSeason = "No Episodes are available in Season.";
        public static string NoEpisodeSummary = "Episode summary is currently not available.";
        public static string NoEpisodesThisWeek = "No episodes on this week";
        public static string NoMovieSummary = "Movie summary is currently not available.";
        public static string NoShowSummary = "Show summary is currently not available.";
        public static string NoFriends = "No Friends!";
        public static string NoFriendsTaunt = "You have no friends!";
        public static string NoFollowersTaunt = "You have no followers!";
        public static string NoFollowingTaunt = "You are not following anyone!";
        public static string NoFollowerReqTaunt = "You have no follower requests!";
        public static string NoTrendingMovies = "No Movies current being watched!";
        public static string NoTrendingShows = "No Shows current being watched!";
        public static string NoMovieRecommendations = "No Movie Recommendations Found!";
        public static string NoShowRecommendations = "No Show Recommendations Found!";
        public static string NoMovieWatchList = "{0} has no movies in Watchlist!";
        public static string NoShowWatchList = "{0} has no shows in Watchlist!";
        public static string NoEpisodeWatchList = "{0} has no episodes in Watchlist!";
        public static string NoSearchTypesSelected = "You must first select one or more search types\nbefore performing an online search.";
        public static string NoShoutsForItem = "No Shouts for {0}!";
        public static string NoPeopleToSearch = "There are no people to search by.";
        public static string NoPluginsEnabled = "You have defined no plugins in configuration.\nWould you like to configure plugins now?";
        public static string NotLoggedIn = "You can not access this area without being\nlogged in. Would you like to Signup or Login\nto trakt.tv now?";
        public static string NoSearchResultsFound = "No Search Results Found";
        public static string NoMovingPictures = "Moving Pictures is Not Installed or Enabled";
        public static string NoListsFound = "No Lists Found, would you like to\ncreate a list now?";
        public static string NoUserLists = "{0} has not created any lists!";
        public static string NoListItemsFound = "No items found in this list!";
        public static string NoSeasonsForShow = "No Seasons found for show!";
        public static string NoRelatedMovies = "No Related movies found for {0}!";
        public static string NoRelatedShows = "No Related shows found for {0}!";
        public static string No = "No";

        // O
        public static string Off = "Off";
        public static string OK = "OK";
        public static string On = "On";
        public static string Overview = "Overview";

        // P
        public static string Password = "Password";
        public static string People = "People";
        public static string Percentage = "Percentage";
        public static string Protected = "Protected";
        public static string Person = "Person";
        public static string PersonWatching = "1 Person Watching";
        public static string PeopleWatching = "{0} People Watching";
        public static string PlayTrailer = "Play Trailer";
        public static string Plugins = "Plugins";
        public static string Plugin = "Plugin";
        public static string Public = "Public";
        public static string PreviousEpisode = "Previous Episode";
        public static string Private = "Private";
        public static string Privacy = "Privacy";
        public static string PrivacyPublic = "Anyone can view this list";
        public static string PrivacyFriends = "Only friends can view this list";
        public static string PrivacyPrivate = "Only you can view this list";
        public static string Producer = "Producer";
        public static string Producers = "Producers";
        public static string Profile = "Profile";
        
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
        public static string RateTwo = "Terrible";
        public static string RateThree = "Bad";
        public static string RateFour = "Poor";
        public static string RateFive = "Meh";
        public static string RateSix = "Fair";
        public static string RateSeven = "Good";
        public static string RateEight = "Great";
        public static string RateNine = "Superb";
        public static string Recommendations = "Recommendations";
        public static string RecommendedMovies = "Recommended Movies";
        public static string RecommendedShows = "Recommended Shows";
        public static string Refresh = "Refresh";
        public static string RelatedMovies = "Related Movies";
        public static string RelatedShows = "Related Shows";
        public static string Released = "Released";
        public static string ReleaseDate = "Release Date";
        public static string RecentWatchedMovies = "Recently Watched Movies";
        public static string RecentWatchedEpisodes = "Recently Watched Episodes";
        public static string RecentAddedMovies = "Recently Added Movies";
        public static string RecentAddedEpisodes = "Recently Added Episodes";
        public static string RecentShouts = "Recent Shouts";
        public static string RemoveFromLibrary = "Remove from Collection";
        public static string RemoveFromWatchList = "Remove from Watchlist";
        public static string RemoveShowFromWatchList = "Remove Show from Watchlist";
        public static string RemoveEpisodeFromWatchList = "Remove Episode from Watchlist";
        public static string RemoveFromList = "Remove from List...";
        public static string Reply = "Reply";
        public static string Replies = "Replies";
        public static string Requests = "Requests";
        public static string ReturningSeries = "Returning Series";
        public static string Runtime = "Runtime";

        // S
        public static string Score = "Score";
        public static string Scrobble = "Scrobble";
        public static string SearchWithMpNZB = "Search NZB";
        public static string SearchTorrent = "Search Torrent";
        public static string Season = "Season";
        public static string Seasons = "Seasons";
        public static string Search = "Search";
        public static string SearchAll = "Search All";
        public static string SearchBy = "Search By";
        public static string SearchMovies = "Search Movies";
        public static string SearchShows = "Search Shows";
        public static string SearchEpisodes = "Search Episodes";
        public static string SearchPeople = "Search People";
        public static string SearchUsers = "Search Users";
        public static string SearchForFriend = "Search for Friend...";
        public static string SearchForUser = "Search for User...";
        public static string SearchTypes = "Search Types";
        public static string SendFriendRequest = "Send friend request to {0}?";
        public static string SendFollowRequest = "Send follow request to {0}?";
        public static string SelectLists = "Select Lists";
        public static string SelectUser = "Select User";
        public static string Series = "Series";
        public static string SeriesPlural = "Series";        
        public static string Settings = "Settings";
        public static string Shout = "Shout";
        public static string Shouts = "Shouts";
        public static string ShowTVShowsInWatchlist = "Show TV Shows in WatchList";
        public static string ShowSpoilers = "Show Spoilers";
        public static string ShowFriendActivity = "Show Friend Activity";
        public static string ShowCommunityActivity = "Show Community Activity";
        public static string SkinPluginsOutOfDate = "Error loading window, skin is out of date!\nExit MediaPortal and enter Configuration to\nenable plugins handlers.";
        public static string SkinOutOfDate = "This feature is not available for your\nskin. See if update is Available.";
        public static string SigningIntoAccount = "Signing Into Account...";
        public static string StartDate = "Start Date";
        public static string Synchronize = "Synchronize";
        public static string SynchronizeNow = "New Plugin Handlers have been added.\nWould you like to Synchronize your\nlibraries now?";
        public static string ShowRateDialogOnWatched = "Show Rate Dialog On Item Watched";
        public static string ShowSeasonInfo = "Season Information...";
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
        public static string SettingSyncStartDelayDescription = "Delay (in seconds) before Library Synchronization starts on MediaPortal Startup. Default is 0 seconds.";
        public static string SettingWebRequestCacheName = "Web Request Cache Time";
        public static string SettingWebRequestCacheDescription = "Set the period of time (in minutes) that web data is cached in gui windows such as Calendar, Trending and Recommendations. Default is 15 minutes.";
        public static string SettingWebRequestTimeoutName = "Web Request Timeout";
        public static string SettingWebRequestTimeoutDescription = "Set the period of time (in seconds) before cancelling any web requests. Default is 30 seconds.";
        public static string SettingSyncRatingsName = "Synchronize Ratings";
        public static string SettingSyncRatingsDescription = "Enable this setting for 2-way synchronization of ratings during Library Sync. If an item has been rated locally or remotely then it wont be overwritten to avoid any rounding issues. It's recommended that items online are rated using advanced ratings before enabling this setting.";
        public static string SettingShowRateDialogOnWatchedName = "Show Rate Dialog on Watched";
        public static string SettingShowRateDialogOnWatchedDescription = "Enable this setting to show the Trakt rate dialog after a movie or episode has finished and considered watched. The Trakt rate dialog will not be shown if the item has already being rated.";
        public static string SettingShowRateDialogInPlaylistsName = "Show Rate Dialog In Playlists";
        public static string SettingShowRateDialogInPlaylistsDescription = "If Trakt Rate Dialog is enabled, disable this setting if you wish to not display it from playlist views";
        public static string SettingActivityPollIntervalName = "Dashboard Activity Poll Interval";
        public static string SettingActivityPollIntervalDescription = "Set the interval (in seconds) that the trakt community/friends activity is updated on the dashboard.";
        public static string SettingTrendingPollIntervalName = "Dashboard Trending Poll Interval";
        public static string SettingTrendingPollIntervalDescription = "Set the interval (in minutes) that the trakt trending shows and movies are updated on the dashboard.";
        public static string SettingDashboardLoadDelayName = "Dashboard Loading Delay";
        public static string SettingDashboardLoadDelayDescription = "This setting is to control how long in milliseconds until the dashboard starts to load data after the GUI window has opened. Changing this value too low can cause errors and fail to load window.";
        public static string SettingEnableJumpToForTVShowsName = "Enable TV Show Jump To Feature";
        public static string SettingEnableJumpToForTVShowsDescription = "Enable this setting to allow user to Jump directly to the MP-TVSeries plugin when pressing Enter or OK on a Trakt TV Show. When disabled it will display Season Information from trakt.tv.";
        public static string SettingRememberLastSelectedActivityName = "Remember Last Selected Activity on Dashboard";
        public static string SettingRememberLastSelectedActivityDescription = "Enable this setting to re-select the last selected activity when the Trakt dashboard is re-loaded, if disabled the first item will always be selected on re-load.";
        public static string SettingSyncLibraryName = "Enable Library Sync";
        public static string SettingSyncLibraryDescription = "Enable this setting to synchronise your collection and watched states to and from trakt.tv. If disabled, only scrobbling will be active.";
        public static string SettingShowSearchResultsBreakdownName = "Show Search Results Breakdown";
        public static string SettingShowSearchResultsBreakdownDescription = "Set this setting to control the behaviour when a single search type is set in the search window. If disabled the user will jump directly to the corresponding search result when a single search type is set.";
        public static string SettingMaxSearchResultsName = "Max Search Results";
        public static string SettingMaxSearchResultsDescription = "Set the maximum number of results per type to retrieve from an online search.";
        public static string SettingFilterTrendingOnDashboardName = "Filter Trending On Dashboard";
        public static string SettingFilterTrendingOnDashboardDescription = "Enable this setting to apply trending filters on the dashboard e.g. Hide Watched, Hide Watchlisted etc. These can be set from the corresponding trending GUI or context menu.";
        public static string SettingIgnoreWatchedPercentOnDVDName = "Ignore Watched Percent On DVDs";
        public static string SettingIgnoreWatchedPercentOnDVDDescription = "Enable this setting to ignore the percentage watched of a DVD when stopped. DVD and Bluray's do not always indicate the correct duration of the main feature video, this will always ensure it is scrobbled (i.e. send watched state) to trakt.tv on stop regardless of how much you have watched.";
        public static string StartYear = "Start Year: {0}";
        public static string SortBy = "Sort By: {0}";
        public static string SortSeasonsAscending = "Sort Seasons in Ascending order";
        public static string SortSeasonsDescending = "Sort Seasons in Descending order";
        public static string Specials = "Specials";
        public static string Status = "Status";

        // T
        public static string Timeout = "Timeout";
        public static string Trending = "Trending";
        public static string TrendingShows = "Trending Shows";
        public static string TrendingMovies = "Trending Movies";
        public static string TrendingShowsAndMovies = "Trending TV Shows and Movies";
        public static string TrendingMoviePeople = "There are {0} people watching {1} movies right now!";
        public static string TrendingTVShowsPeople = "There are {0} people watching {1} tv shows right now!";
        public static string TVShow = "TV Show";
        public static string TVShows = "TV Shows";
        public static string Tagline = "Tagline";
        public static string Today = "Today";
        public static string Tomorrow = "Tomorrow";
        public static string Title = "Title";
        public static string Trailer = "Trailer";
        public static string Trailers = "Trailers...";
        
        // U
        public static string UserHasNotWatchedEpisodes = "User has not watched any episodes!";
        public static string UserHasNotWatchedMovies = "User has not watched any movies!";
        public static string UserHasNoRecentAddedEpisodes = "User has no recently added episodes!";
        public static string UserHasNoRecentAddedMovies = "User has no recently added movies!";
        public static string UserHasNoRecentShouts = "User has not recently shouted or reviewed anything!";
        public static string User = "User";
        public static string Users = "Users";
        public static string Username = "Username";
        public static string UnAuthorized = "Authentication failed, please check username and password in settings.";
        public static string UnFollow = "UnFollow";
        public static string UnFollowMessage = "Are you sure you want to unfollow\nuser {0}?";
        public static string UnRate = "UnRate";
        public static string UpdatingCategories = "Updating Categories";
        public static string UpdatingCategoriesMenuMovingPics = "Updating Categories Menu in MovingPictures.";
        public static string UpdatingCategoriesMenuMovingPicsWarning = "You can't remove trakt from the categories menu whilst they're still being updated!";
        public static string UpdatingFilters = "Updating Filters";
        public static string UpdatingFiltersMenuMovingPics = "Updating Filters Menu in MovingPictures.";
        public static string UpdatingFiltersMenuMovingPicsWarning = "You can't remove trakt from the filters menu whilst they're still being updated!";
        public static string UnableToPlayTrailer = "Unable to play trailer.";
        public static string UserIsProtected = "The current user is protected!\nProtected users can only be viewed\nfrom the 'Friends' view.";
        public static string UserProfile = "User Profile";        

        // V
        public static string View = "View";
        public static string Votes = "Votes";
        public static string ValidUsername = "You must enter a valid username!";
        public static string ValidPassword = "You must enter a valid password!";
        public static string ValidEmail = "You must enter a valid email!";

        // W
        public static string WatchList = "Watchlist";
        public static string Watched = "Watched";
        public static string WatchedMovies = "Watched Movies";
        public static string WatchedEpisodes = "Watched Episodes";
        public static string Watchers = "Watchers";
        public static string Watching = "Watching";
        public static string WatchListMovies = "Movie Watchlist";
        public static string WatchListShows = "Show Watchlist";
        public static string WatchListEpisodes = "Episode Watchlist";
        public static string Writer = "Writer";
        public static string Writers = "Writers";

        // Y
        public static string Year = "Year";
        public static string Yes = "Yes";
        public static string Yesterday = "Yesterday";

        #endregion

    }
}