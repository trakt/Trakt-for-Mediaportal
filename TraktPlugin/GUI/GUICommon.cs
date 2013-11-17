using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using MediaPortal.Dialogs;
using MediaPortal.GUI.Library;
using MediaPortal.GUI.Video;
using MediaPortal.Video.Database;
using TraktPlugin.TraktAPI;
using TraktPlugin.TraktAPI.DataStructures;
using Trailers.Providers;
using Trailers;

namespace TraktPlugin.GUI
{
    #region Enums

    public enum Layout 
    {
        List = 0,
        SmallIcons = 1,
        LargeIcons = 2,
        Filmstrip = 3,
    }

    enum TrailerSiteMovies
    {
        IMDb,
        iTunes,
        YouTube
    }

    enum TrailerSiteShows
    {
        IMDb,
        YouTube
    }

    enum ActivityContextMenuItem
    {
        ShowCommunityActivity,
        ShowFriendActivity,
        IncludeMeInFriendsActivity,
        DontIncludeMeInFriendsActivity,
        ShowSeasonInfo,
        MarkAsWatched,
        AddToWatchList,
        AddToList,
        Related,
        Rate,
        Shouts,
        UserProfile,
        FollowUser,
        Trailers,
    }

    public enum TrendingContextMenuItem
    {
        MarkAsWatched,
        MarkAsUnWatched,
        AddToWatchList,
        RemoveFromWatchList,
        Filters,
        AddToList,
        AddToLibrary,
        RemoveFromLibrary,
        Related,
        Rate,
        Shouts,
        ChangeLayout,
        Trailers,
        SearchWithMpNZB,
        SearchTorrent,
        ShowSeasonInfo
    }

    public enum TraktGUIControls 
    {
        Layout = 2,
        Facade = 50,
    }

    enum TraktGUIWindows
    {
        Main = 87258,
        Calendar = 87259,
        Friends = 87260, // removed
        Recommendations = 87261,
        RecommendationsShows = 87262,
        RecommendationsMovies = 87263,
        Trending = 87264,
        TrendingShows = 87265,
        TrendingMovies = 87266,
        WatchedList = 87267,
        WatchedListShows = 87268,
        WatchedListEpisodes = 87269,
        WatchedListMovies = 87270,
        Settings = 87271,
        SettingsAccount = 87272,
        SettingsPlugins = 87273,
        SettingsGeneral = 87274,
        Lists = 87275,
        ListItems = 87276,
        RelatedMovies = 87277,
        RelatedShows = 87278,
        Shouts = 87280,
        ShowSeasons = 87281,
        SeasonEpisodes = 87282,
        Network = 87283,
        RecentWatchedMovies = 87284,
        RecentWatchedEpisodes = 87285,
        RecentAddedMovies = 87286,
        RecentAddedEpisodes = 87287,
        RecentShouts = 87288,
        UserProfile = 87400,
        Search = 874001,
        SearchEpisodes = 874002,
        SearchShows = 874003,
        SearchMovies = 874004,
        SearchPeople = 874005,
        SearchUsers = 874006
    }

    enum TraktDashboardControls
    {
        ToggleTrendingCheckButton = 98298,
        DashboardAnimation = 98299,
        ActivityFacade = 98300,
        TrendingShowsFacade = 98301,
        TrendingMoviesFacade = 98302
    }

    enum ExternalPluginWindows
    {
        OnlineVideos = 4755,
        VideoInfo = 2003,
        MovingPictures = 96742,
        TVSeries = 9811,
        MyFilms = 7986,
        MyAnime = 6001,
        MpNZB = 3847,
        MPEISettings = 803,
        MyTorrents = 5678,
        Showtimes = 7111992,
        MPSkinSettings = 705
    }

    enum ExternalPluginControls
    {
        WatchList = 97258,
        Rate = 97259,
        Shouts = 97260,
        CustomList = 97261,
        RelatedItems = 97262,
        SearchBy = 97263,
        TraktMenu = 97270
    }

    enum TraktMenuItems
    {
        AddToWatchList,
        AddToCustomList,
        Rate,
        Shouts,
        Related,
        UserProfile,
        Calendar,
        Network,
        Recommendations,
        Trending,
        WatchList,
        Lists,
        SearchBy
    }

    enum TraktSearchByItems
    {
        Actors,
        Directors,
        Producers,
        Writers,
        GuestStars
    }

    public enum SortingFields
    {
        Title,
        ReleaseDate,
        Score,
        Votes,
        Runtime,
        PeopleWatching,
        WatchListInserted
    }

    public enum SortingDirections
    {
        Ascending,
        Descending
    }

    public enum Filters
    {
        Watched,
        Watchlisted,
        Collected,
        Rated
    }

    #endregion

    public class GUICommon
    {
        #region Check Login
        public static bool CheckLogin()
        {
            return CheckLogin(true);
        }

        /// <summary>
        /// Checks if user is logged in, if not the user is presented with
        /// a choice to jump to Account settings and signup/login.
        /// </summary>
        public static bool CheckLogin(bool showPreviousWindow)
        {
            if (TraktSettings.AccountStatus != TraktAPI.ConnectionState.Connected)
            {
                if (GUIUtils.ShowYesNoDialog(Translation.Login, Translation.NotLoggedIn, true))
                {
                    GUIWindowManager.ActivateWindow((int)TraktGUIWindows.SettingsAccount);
                    return false;
                }
                if (showPreviousWindow) GUIWindowManager.ShowPreviousWindow();
                return false;
            }
            return true;
        }
        #endregion

        #region Play Movie
        /// <summary>
        /// Checks if a selected movie exists locally and plays movie or
        /// jumps to corresponding plugin details view
        /// </summary>
        /// <param name="jumpTo">false if movie should be played directly</param>
        internal static void CheckAndPlayMovie(bool jumpTo, TraktMovie movie)
        {
            if (movie == null) return;

            TraktLogger.Info("Attempting to play movie: {0} ({1}) [{2}]", movie.Title, movie.Year, movie.IMDBID);
            bool handled = false;

            if (TraktHelper.IsMovingPicturesAvailableAndEnabled)
            {
                TraktLogger.Info("Checking if any movie to watch in MovingPictures");
                int? movieid = null;

                // Find Movie ID in MovingPictures
                // Movie List is now cached internally in MovingPictures so it will be fast
                bool movieExists = TraktHandlers.MovingPictures.FindMovieID(movie.Title, int.Parse(movie.Year), movie.IMDBID, ref movieid);

                if (movieExists)
                {
                    TraktLogger.Info("Found movie in MovingPictures with movieId '{0}'", movieid.ToString());
                    if (jumpTo)
                    {
                        string loadingParameter = string.Format("movieid:{0}", movieid);
                        // Open MovingPictures Details view so user can play movie
                        GUIWindowManager.ActivateWindow((int)ExternalPluginWindows.MovingPictures, loadingParameter);
                    }
                    else
                    {
                        TraktHandlers.MovingPictures.PlayMovie(movieid);
                    }
                    handled = true;
                }
            }

            // check if its in My Videos database
            if (TraktSettings.MyVideos >= 0 && handled == false)
            {
                TraktLogger.Info("Checking if any movie to watch in My Videos");
                IMDBMovie imdbMovie = null;
                if (TraktHandlers.MyVideos.FindMovieID(movie.Title, int.Parse(movie.Year), movie.IMDBID, ref imdbMovie))
                {
                    // Open My Videos Video Info view so user can play movie
                    if (jumpTo)
                    {
                        GUIVideoInfo videoInfo = (GUIVideoInfo)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_VIDEO_INFO);
                        videoInfo.Movie = imdbMovie;
                        GUIWindowManager.ActivateWindow((int)GUIWindow.Window.WINDOW_VIDEO_INFO);
                    }
                    else
                    {
                        GUIVideoFiles.PlayMovie(imdbMovie.ID, false);
                    }
                    handled = true;
                }
            }

            // check if its in My Films database
            if (TraktHelper.IsMyFilmsAvailableAndEnabled && handled == false)
            {
                TraktLogger.Info("Checking if any movie to watch in My Films");
                int? movieid = null;
                string config = null;
                if (TraktHandlers.MyFilmsHandler.FindMovie(movie.Title, int.Parse(movie.Year), movie.IMDBID, ref movieid, ref config))
                {
                    // Open My Films Details view so user can play movie
                    if (jumpTo)
                    {
                        string loadingParameter = string.Format("config:{0}|movieid:{1}", config, movieid);
                        GUIWindowManager.ActivateWindow((int)ExternalPluginWindows.MyFilms, loadingParameter);
                    }
                    else
                    {
                        // TraktHandlers.MyFilms.PlayMovie(config, movieid); // ToDo: Add Player Class to MyFilms
                        string loadingParameter = string.Format("config:{0}|movieid:{1}|play:{2}", config, movieid, "true");
                        GUIWindowManager.ActivateWindow((int)ExternalPluginWindows.MyFilms, loadingParameter);
                    }
                    handled = true;
                }
            }

            if (TraktSettings.UseTrailersPlugin && TraktHelper.IsTrailersAvailableAndEnabled && handled == false)
            {
                TraktLogger.Info("No movies matched in local plugin databases! Attempting to search/play trailer(s) from Trailers Plugin.");
                ShowMovieTrailersPluginMenu(movie);
                handled = true;
            }

            if (TraktHelper.IsOnlineVideosAvailableAndEnabled && handled == false)
            {
                if (!string.IsNullOrEmpty(movie.Trailer))
                {
                    TraktLogger.Info("No movies matched in local plugin databases! Attempting to play trailer '{0}' in OnlineVideos.", movie.Trailer);
                    TraktHandlers.OnlineVideos.Play(movie.Trailer);
                    return;
                }

                SearchMovieTrailer(movie);
                handled = true;
            }
        }
        #endregion

        #region PlayEpisode
        internal static void CheckAndPlayEpisode(TraktShow show, TraktEpisode episode)
        {
            if (show == null || episode == null) return;
        
            bool handled = false;

            // check if plugin is installed and enabled
            if (TraktHelper.IsMPTVSeriesAvailableAndEnabled)
            {
                // Play episode if it exists
                handled = TraktHandlers.TVSeries.PlayEpisode(Convert.ToInt32(show.Tvdb), episode.Season, episode.Number);
            }

            if (TraktHelper.IsMyAnimeAvailableAndEnabled && handled == false)
            {
                handled = TraktHandlers.MyAnime.PlayEpisode(Convert.ToInt32(show.Tvdb), episode.Season, episode.Number);
            }

            if (TraktHelper.IsOnlineVideosAvailableAndEnabled && handled == false)
            {
                SearchEpisodeTrailer(show, episode);
                handled = true;
            }
        }
        
        internal static void CheckAndPlayFirstUnwatched(TraktShow show, bool jumpTo)
        {
            if (show == null) return;

            TraktLogger.Info("Attempting to play TVDb: {0}, IMDb: {1}", show.Tvdb, show.Imdb);
            bool handled = false;

            // check if plugin is installed and enabled
            if (TraktHelper.IsMPTVSeriesAvailableAndEnabled)
            {
                if (jumpTo)
                {
                    TraktLogger.Info("Looking for series in MP-TVSeries database");
                    if (TraktHandlers.TVSeries.SeriesExists(Convert.ToInt32(show.Tvdb)))
                    {
                        string loadingParameter = string.Format("seriesid:{0}", show.Tvdb);
                        GUIWindowManager.ActivateWindow((int)ExternalPluginWindows.TVSeries, loadingParameter);
                        handled = true;
                    }
                }
                else
                {
                    // Play episode if it exists
                    TraktLogger.Info("Checking if any episodes to watch in MP-TVSeries");
                    handled = TraktHandlers.TVSeries.PlayFirstUnwatchedEpisode(Convert.ToInt32(show.Tvdb));
                }
            }

            if (TraktHelper.IsMyAnimeAvailableAndEnabled && handled == false)
            {
                TraktLogger.Info("Checking if any episodes to watch in My Anime");
                handled = TraktHandlers.MyAnime.PlayFirstUnwatchedEpisode(Convert.ToInt32(show.Tvdb));
            }

            if (TraktHelper.IsOnlineVideosAvailableAndEnabled && handled == false)
            {
                SearchShowTrailer(show);
                handled = true;
            }
        }
        #endregion

        #region Search Trailers
        internal static void SearchEpisodeTrailer(TraktShow show, TraktEpisode episode)
        {
            string searchTerm = string.Empty;
            switch (TraktSettings.DefaultTVShowTrailerSite)
            {
                case "IMDb Movie Trailers":
                    // can't really do an episode search on IMDb so just do a show search
                    searchTerm = !string.IsNullOrEmpty(show.Imdb) ? show.Imdb : show.Title;
                    break;

                case "YouTube":
                    // if the episode is a special, search by episode title
                    if (episode.Season != 0)
                    {
                        searchTerm = string.Format("{0} S{1}E{2}", show.Title, episode.Season.ToString("D2"), episode.Number.ToString("D2"));
                    }
                    else
                    {
                        searchTerm = string.Format("{0} {1}", show.Title, episode.Title);
                    }
                    break;
            }
            string loadingParameter = string.Format("site:{0}|search:{1}|return:Locked", TraktSettings.DefaultTVShowTrailerSite, searchTerm);

            TraktLogger.Info(string.Format("No episode found! Attempting tv episode trailer lookup in OnlineVideos '{0}' site util with search term '{1}'", TraktSettings.DefaultTVShowTrailerSite, searchTerm));
            GUIWindowManager.ActivateWindow((int)ExternalPluginWindows.OnlineVideos, loadingParameter);
        }

        internal static void SearchShowTrailer(TraktShow show)
        {
            string searchTerm = string.Empty;
            switch (TraktSettings.DefaultTVShowTrailerSite)
            {
                case "IMDb Movie Trailers":
                    searchTerm = !string.IsNullOrEmpty(show.Imdb) ? show.Imdb : show.Title;
                    break;

                case "YouTube":
                    searchTerm = show.Title;
                    break;
            }
            string loadingParameter = string.Format("site:{0}|search:{1}|return:Locked", TraktSettings.DefaultTVShowTrailerSite, searchTerm);

            TraktLogger.Info(string.Format("No tv show found! Attempting tv show trailer lookup in OnlineVideos '{0}' site util with search term '{1}'", TraktSettings.DefaultTVShowTrailerSite, searchTerm));
            GUIWindowManager.ActivateWindow((int)ExternalPluginWindows.OnlineVideos, loadingParameter);
        }

        internal static void SearchMovieTrailer(TraktMovie movie)
        {
            string searchTerm = string.Empty;
            switch (TraktSettings.DefaultMovieTrailerSite)
            {
                case "IMDb Movie Trailers":
                    searchTerm = !string.IsNullOrEmpty(movie.IMDBID) ? movie.IMDBID : movie.Title;
                    break;

                case "iTunes Movie Trailers":
                    searchTerm = movie.Title;
                    break;

                case "YouTube":
                    searchTerm = string.Format("{0} {1}", movie.Title, movie.Year);
                    break;
            }
            string loadingParameter = string.Format("site:{0}|search:{1}|return:Locked", TraktSettings.DefaultMovieTrailerSite, searchTerm);

            TraktLogger.Info(string.Format("No movie found! Attempting movie trailer lookup in OnlineVideos '{0}' site util with search term '{1}'", TraktSettings.DefaultMovieTrailerSite, searchTerm));
            GUIWindowManager.ActivateWindow((int)ExternalPluginWindows.OnlineVideos, loadingParameter);
        }
        #endregion

        #region Rate Movie

        internal static bool RateMovie(TraktMovie movie)
        {
            TraktRateMovie rateObject = new TraktRateMovie
            {
                IMDBID = movie.IMDBID,
                TMDBID = movie.TMDBID,
                Title = movie.Title,
                Year = movie.Year,
                Rating = movie.RatingAdvanced.ToString(),
                UserName = TraktSettings.Username,
                Password = TraktSettings.Password
            };

            int prevRating = movie.RatingAdvanced;
            int newRating = int.Parse(GUIUtils.ShowRateDialog<TraktRateMovie>(rateObject));
            if (newRating == -1) return false;

            // If previous rating not equal to current rating then 
            // update skin properties to reflect changes
            // This is not really needed but saves waiting for response
            // from server to calculate fields...we can do it ourselves

            if (prevRating != newRating)
            {
                movie.RatingAdvanced = newRating;

                // if not rated previously bump up the votes
                if (prevRating == 0)
                {
                    movie.Ratings.Votes++;
                    if (movie.RatingAdvanced > 5)
                    {
                        movie.Rating = "love";
                        movie.Ratings.LovedCount++;
                    }
                    else
                    {
                        movie.Rating = "hate";
                        movie.Ratings.HatedCount++;
                    }
                }

                if (prevRating != 0 && prevRating > 5 && newRating <= 5)
                {
                    movie.Rating = "hate";
                    movie.Ratings.LovedCount--;
                    movie.Ratings.HatedCount++;
                }

                if (prevRating != 0 && prevRating <= 5 && newRating > 5)
                {
                    movie.Rating = "love";
                    movie.Ratings.LovedCount++;
                    movie.Ratings.HatedCount--;
                }

                if (newRating == 0)
                {
                    if (prevRating <= 5) movie.Ratings.HatedCount++;
                    movie.Ratings.Votes--;
                    movie.Rating = "false";
                }

                // Could be in-accurate, best guess
                if (prevRating == 0)
                {
                    movie.Ratings.Percentage = (int)Math.Round(((movie.Ratings.Percentage * (movie.Ratings.Votes - 1)) + (10 * newRating)) / (float)movie.Ratings.Votes);
                }
                else
                {
                    movie.Ratings.Percentage = (int)Math.Round(((movie.Ratings.Percentage * (movie.Ratings.Votes)) + (10 * newRating) - (10 * prevRating)) / (float)movie.Ratings.Votes);
                }

                return true;
            }

            return false;
        }

        #endregion

        #region Rate Show

        internal static bool RateShow(TraktShow show)
        {
            TraktRateSeries rateObject = new TraktRateSeries
            {
                SeriesID = show.Tvdb,
                Title = show.Title,
                Year = show.Year.ToString(),
                Rating = show.RatingAdvanced.ToString(),
                UserName = TraktSettings.Username,
                Password = TraktSettings.Password
            };

            int prevRating = show.RatingAdvanced;
            int newRating = int.Parse(GUIUtils.ShowRateDialog<TraktRateSeries>(rateObject));
            if (newRating == -1) return false;

            // If previous rating not equal to current rating then 
            // update skin properties to reflect changes
            // This is not really needed but saves waiting for response
            // from server to calculate fields...we can do it ourselves

            if (prevRating != newRating)
            {
                show.RatingAdvanced = newRating;

                // if not rated previously bump up the votes
                if (prevRating == 0)
                {
                    show.Ratings.Votes++;
                    if (show.RatingAdvanced > 5)
                    {
                        show.Rating = "love";
                        show.Ratings.LovedCount++;
                    }
                    else
                    {
                        show.Rating = "hate";
                        show.Ratings.HatedCount++;
                    }
                }

                if (prevRating != 0 && prevRating > 5 && newRating <= 5)
                {
                    show.Rating = "hate";
                    show.Ratings.LovedCount--;
                    show.Ratings.HatedCount++;
                }

                if (prevRating != 0 && prevRating <= 5 && newRating > 5)
                {
                    show.Rating = "love";
                    show.Ratings.LovedCount++;
                    show.Ratings.HatedCount--;
                }

                if (newRating == 0)
                {
                    if (prevRating <= 5) show.Ratings.HatedCount++;
                    show.Ratings.Votes--;
                    show.Rating = "false";
                }

                // Could be in-accurate, best guess
                if (prevRating == 0)
                {
                    show.Ratings.Percentage = (int)Math.Round(((show.Ratings.Percentage * (show.Ratings.Votes - 1)) + (10 * newRating)) / (float)show.Ratings.Votes);
                }
                else
                {
                    show.Ratings.Percentage = (int)Math.Round(((show.Ratings.Percentage * (show.Ratings.Votes)) + (10 * newRating) - (10 * prevRating)) / (float)show.Ratings.Votes);
                }

                return true;
            }

            return false;
        }

        #endregion

        #region Rate Episode

        internal static bool RateEpisode(TraktShow show, TraktEpisode episode)
        {
            TraktRateEpisode rateObject = new TraktRateEpisode
            {
                SeriesID = show.Tvdb,
                Title = show.Title,
                Year = show.Year.ToString(),
                Episode = episode.Number.ToString(),
                Season = episode.Season.ToString(),
                Rating = episode.RatingAdvanced.ToString(),
                UserName = TraktSettings.Username,
                Password = TraktSettings.Password
            };

            int prevRating = episode.RatingAdvanced;
            int newRating = int.Parse(GUIUtils.ShowRateDialog<TraktRateEpisode>(rateObject));
            if (newRating == -1) return false;

            // If previous rating not equal to current rating then 
            // update skin properties to reflect changes
            // This is not really needed but saves waiting for response
            // from server to calculate fields...we can do it ourselves

            if (prevRating != newRating)
            {
                episode.RatingAdvanced = newRating;

                if (episode.Ratings == null)
                    episode.Ratings = new TraktRatings();

                // if not rated previously bump up the votes
                if (prevRating == 0)
                {
                    episode.Ratings.Votes++;
                    if (episode.RatingAdvanced > 5)
                    {
                        episode.Rating = "love";
                        episode.Ratings.LovedCount++;
                    }
                    else
                    {
                        episode.Rating = "hate";
                        episode.Ratings.HatedCount++;
                    }
                }

                if (prevRating != 0 && prevRating > 5 && newRating <= 5)
                {
                    episode.Rating = "hate";
                    episode.Ratings.LovedCount--;
                    episode.Ratings.HatedCount++;
                }

                if (prevRating != 0 && prevRating <= 5 && newRating > 5)
                {
                    episode.Rating = "love";
                    episode.Ratings.LovedCount++;
                    episode.Ratings.HatedCount--;
                }

                if (newRating == 0)
                {
                    if (prevRating <= 5) show.Ratings.HatedCount++;
                    episode.Ratings.Votes--;
                    episode.Rating = "false";
                }

                // Could be in-accurate, best guess
                if (prevRating == 0)
                {
                    episode.Ratings.Percentage = (int)Math.Round(((show.Ratings.Percentage * (show.Ratings.Votes - 1)) + (10 * newRating)) / (float)show.Ratings.Votes);
                }
                else
                {
                    episode.Ratings.Percentage = (int)Math.Round(((show.Ratings.Percentage * (show.Ratings.Votes)) + (10 * newRating) - (10 * prevRating)) / (float)show.Ratings.Votes);
                }

                return true;
            }

            return false;
        }

        #endregion

        #region Mark Show As Seen

        public static void MarkShowAsSeen(TraktShow show)
        {
            TraktShowSeen seenShow = new TraktShowSeen
            {
                Tvdb = show.Tvdb,
                Imdb = show.Imdb,
                Title = show.Title,
                Year = show.Year,
                Username = TraktSettings.Username,
                Password = TraktSettings.Password
            };

            Thread seenThread = new Thread(o =>
                {
                    var oShow = o as TraktShowSeen;
                    TraktLogger.Info("Marking {0} as seen", oShow.Title);
                    var response = TraktAPI.TraktAPI.SyncShowAsSeen(oShow);
                    TraktLogger.LogTraktResponse(response);
                })
                {
                    IsBackground = true,
                    Name = "MarkWatched"
                };

            seenThread.Start(seenShow);
        }

        #endregion

        #region Mark Season As Seen

        public static void MarkSeasonAsSeen(TraktShow show, int season)
        {
            TraktSeasonSeen seenSeason = new TraktSeasonSeen
            {
                Season = season,
                Tvdb = show.Tvdb,
                Imdb = show.Imdb,
                Title = show.Title,
                Year = show.Year,
                Username = TraktSettings.Username,
                Password = TraktSettings.Password
            };

            Thread seenThread = new Thread(o =>
            {
                var oSeason = o as TraktSeasonSeen;
                TraktLogger.Info("Marking {0} season {1} as seen", oSeason.Title, oSeason.Season);
                var response = TraktAPI.TraktAPI.SyncSeasonAsSeen(oSeason);
                TraktLogger.LogTraktResponse(response);
            })
            {
                IsBackground = true,
                Name = "MarkWatched"
            };

            seenThread.Start(seenSeason);
        }

        #endregion

        #region Add Show to Library

        public static void AddShowToLibrary(TraktShow show)
        {
            TraktShowLibrary libShow = new TraktShowLibrary
            {
                Tvdb = show.Tvdb,
                Imdb = show.Imdb,
                Title = show.Title,
                Year = show.Year,
                Username = TraktSettings.Username,
                Password = TraktSettings.Password
            };

            Thread libThread = new Thread(o =>
            {
                var oShow = o as TraktShowLibrary;
                TraktLogger.Info("Adding {0} to library", oShow.Title);
                var response = TraktAPI.TraktAPI.SyncShowAsLibrary(oShow);
                TraktLogger.LogTraktResponse(response);
            })
            {
                IsBackground = true,
                Name = "AddLibrary"
            };

            libThread.Start(libShow);
        }

        #endregion

        #region Add Season to Library

        public static void AddSeasonToLibrary(TraktShow show, int season)
        {
            TraktSeasonLibrary libSeason = new TraktSeasonLibrary
            {
                Season = season,
                Tvdb = show.Tvdb,
                Imdb = show.Imdb,
                Title = show.Title,
                Year = show.Year,
                Username = TraktSettings.Username,
                Password = TraktSettings.Password
            };

            Thread libThread = new Thread(o =>
            {
                var oSeason = o as TraktSeasonLibrary;
                TraktLogger.Info("Adding {0} season {1} to library", oSeason.Title, oSeason.Season);
                var response = TraktAPI.TraktAPI.SyncSeasonAsLibrary(oSeason);
                TraktLogger.LogTraktResponse(response);
            })
            {
                IsBackground = true,
                Name = "AddLibrary"
            };

            libThread.Start(libSeason);
        }

        #endregion

        #region Common Skin Properties

        internal static void SetProperty(string property, string value)
        {
            string propertyValue = string.IsNullOrEmpty(value) ? "N/A" : value;
            GUIUtils.SetProperty(property, propertyValue);
        }

        internal static void ClearUserProperties()
        {
            GUIUtils.SetProperty("#Trakt.User.About", string.Empty);
            GUIUtils.SetProperty("#Trakt.User.Age", string.Empty);
            GUIUtils.SetProperty("#Trakt.User.Avatar", string.Empty);
            GUIUtils.SetProperty("#Trakt.User.AvatarFileName", string.Empty);
            GUIUtils.SetProperty("#Trakt.User.FullName", string.Empty);
            GUIUtils.SetProperty("#Trakt.User.Gender", string.Empty);
            GUIUtils.SetProperty("#Trakt.User.JoinDate", string.Empty);
            GUIUtils.SetProperty("#Trakt.User.ApprovedDate", string.Empty);
            GUIUtils.SetProperty("#Trakt.User.Location", string.Empty);
            GUIUtils.SetProperty("#Trakt.User.Protected", string.Empty);
            GUIUtils.SetProperty("#Trakt.User.Url", string.Empty);
            GUIUtils.SetProperty("#Trakt.User.Username", string.Empty);
            GUIUtils.SetProperty("#Trakt.User.VIP", string.Empty);
        }

        internal static void SetUserProperties(TraktUser user)
        {
            SetProperty("#Trakt.User.About", user.About.RemapHighOrderChars());
            SetProperty("#Trakt.User.Age", user.Age);
            SetProperty("#Trakt.User.Avatar", user.Avatar);
            SetProperty("#Trakt.User.AvatarFileName", user.Avatar.LocalImageFilename(ArtworkType.Avatar));
            SetProperty("#Trakt.User.FullName", user.FullName);
            SetProperty("#Trakt.User.Gender", user.Gender);
            SetProperty("#Trakt.User.JoinDate", user.JoinDate.FromEpoch().ToLongDateString());
            SetProperty("#Trakt.User.Location", user.Location);
            SetProperty("#Trakt.User.Protected", user.Protected.ToString().ToLower());
            SetProperty("#Trakt.User.Url", user.Url);
            SetProperty("#Trakt.User.Username", user.Username);
            SetProperty("#Trakt.User.VIP", user.VIP.ToString().ToLower());
        }

        internal static void ClearMovieProperties()
        {
            GUIUtils.SetProperty("#Trakt.Movie.Imdb", string.Empty);
            GUIUtils.SetProperty("#Trakt.Movie.Certification", string.Empty);
            GUIUtils.SetProperty("#Trakt.Movie.Overview", string.Empty);
            GUIUtils.SetProperty("#Trakt.Movie.Released", string.Empty);
            GUIUtils.SetProperty("#Trakt.Movie.Runtime", string.Empty);
            GUIUtils.SetProperty("#Trakt.Movie.Tagline", string.Empty);
            GUIUtils.SetProperty("#Trakt.Movie.Title", string.Empty);
            GUIUtils.SetProperty("#Trakt.Movie.Tmdb", string.Empty);
            GUIUtils.SetProperty("#Trakt.Movie.Trailer", string.Empty);
            GUIUtils.SetProperty("#Trakt.Movie.Url", string.Empty);
            GUIUtils.SetProperty("#Trakt.Movie.Year", string.Empty);
            GUIUtils.SetProperty("#Trakt.Movie.Genres", string.Empty);
            GUIUtils.SetProperty("#Trakt.Movie.PosterImageFilename", string.Empty);
            GUIUtils.SetProperty("#Trakt.Movie.FanartImageFilename", string.Empty);
            GUIUtils.SetProperty("#Trakt.Movie.InCollection", string.Empty);
            GUIUtils.SetProperty("#Trakt.Movie.InWatchList", string.Empty);
            GUIUtils.SetProperty("#Trakt.Movie.Plays", string.Empty);
            GUIUtils.SetProperty("#Trakt.Movie.Watched", string.Empty);
            GUIUtils.SetProperty("#Trakt.Movie.Rating", string.Empty);
            GUIUtils.SetProperty("#Trakt.Movie.RatingAdvanced", string.Empty);
            GUIUtils.SetProperty("#Trakt.Movie.Ratings.Icon", string.Empty);
            GUIUtils.SetProperty("#Trakt.Movie.Ratings.HatedCount", string.Empty);
            GUIUtils.SetProperty("#Trakt.Movie.Ratings.LovedCount", string.Empty);
            GUIUtils.SetProperty("#Trakt.Movie.Ratings.Percentage", string.Empty);
            GUIUtils.SetProperty("#Trakt.Movie.Ratings.Votes", string.Empty);
        }

        internal static void ClearShoutProperties()
        {
            GUIUtils.SetProperty("#Trakt.Shout.Inserted", string.Empty);
            GUIUtils.SetProperty("#Trakt.Shout.Spoiler", string.Empty);
            GUIUtils.SetProperty("#Trakt.Shout.UserAdvancedRating", string.Empty);
            GUIUtils.SetProperty("#Trakt.Shout.UserRating", string.Empty);
            GUIUtils.SetProperty("#Trakt.Shout.Type", string.Empty);
            GUIUtils.SetProperty("#Trakt.Shout.Id", string.Empty);
            GUIUtils.SetProperty("#Trakt.Shout.Likes", string.Empty);
            GUIUtils.SetProperty("#Trakt.Shout.Replies", string.Empty);
            GUIUtils.SetProperty("#Trakt.Shout.Text", string.Empty);
        }

        internal static void ClearStatisticProperties()
        {
            #region Friends Statistics
            GUIUtils.SetProperty("#Trakt.Statistics.Friends", string.Empty);
            #endregion

            #region Shows Statistics
            GUIUtils.SetProperty("#Trakt.Statistics.Shows.Library", string.Empty);
            GUIUtils.SetProperty("#Trakt.Statistics.Shows.Watched", string.Empty);
            GUIUtils.SetProperty("#Trakt.Statistics.Shows.Collection", string.Empty);
            GUIUtils.SetProperty("#Trakt.Statistics.Shows.Shouts", string.Empty);
            GUIUtils.SetProperty("#Trakt.Statistics.Shows.Loved", string.Empty);
            GUIUtils.SetProperty("#Trakt.Statistics.Shows.Hated", string.Empty);
            #endregion

            #region Episodes Statistics
            GUIUtils.SetProperty("#Trakt.Statistics.Episodes.Checkins", string.Empty);
            GUIUtils.SetProperty("#Trakt.Statistics.Episodes.CheckinsUnique", string.Empty);
            GUIUtils.SetProperty("#Trakt.Statistics.Episodes.Collection", string.Empty);
            GUIUtils.SetProperty("#Trakt.Statistics.Episodes.Hated", string.Empty);
            GUIUtils.SetProperty("#Trakt.Statistics.Episodes.Loved", string.Empty);
            GUIUtils.SetProperty("#Trakt.Statistics.Episodes.Scrobbles", string.Empty);
            GUIUtils.SetProperty("#Trakt.Statistics.Episodes.ScrobblesUnique", string.Empty);
            GUIUtils.SetProperty("#Trakt.Statistics.Episodes.Seen", string.Empty);
            GUIUtils.SetProperty("#Trakt.Statistics.Episodes.Shouts", string.Empty);
            GUIUtils.SetProperty("#Trakt.Statistics.Episodes.UnWatched", string.Empty);
            GUIUtils.SetProperty("#Trakt.Statistics.Episodes.Watched", string.Empty);
            GUIUtils.SetProperty("#Trakt.Statistics.Episodes.WatchedElseWhere", string.Empty);
            GUIUtils.SetProperty("#Trakt.Statistics.Episodes.WatchedTrakt", string.Empty);
            GUIUtils.SetProperty("#Trakt.Statistics.Episodes.WatchedTraktUnique", string.Empty);
            GUIUtils.SetProperty("#Trakt.Statistics.Episodes.WatchedUnique", string.Empty);
            #endregion

            #region Movies Statistics
            GUIUtils.SetProperty("#Trakt.Statistics.Movies.Checkins", string.Empty);
            GUIUtils.SetProperty("#Trakt.Statistics.Movies.CheckinsUnique", string.Empty);
            GUIUtils.SetProperty("#Trakt.Statistics.Movies.Collection", string.Empty);
            GUIUtils.SetProperty("#Trakt.Statistics.Movies.Hated", string.Empty);
            GUIUtils.SetProperty("#Trakt.Statistics.Movies.Library", string.Empty);
            GUIUtils.SetProperty("#Trakt.Statistics.Movies.Loved", string.Empty);
            GUIUtils.SetProperty("#Trakt.Statistics.Movies.Scrobbles", string.Empty);
            GUIUtils.SetProperty("#Trakt.Statistics.Movies.ScrobblesUnique", string.Empty);
            GUIUtils.SetProperty("#Trakt.Statistics.Movies.Seen", string.Empty);
            GUIUtils.SetProperty("#Trakt.Statistics.Movies.Shouts", string.Empty);
            GUIUtils.SetProperty("#Trakt.Statistics.Movies.UnWatched", string.Empty);
            GUIUtils.SetProperty("#Trakt.Statistics.Movies.Watched", string.Empty);
            GUIUtils.SetProperty("#Trakt.Statistics.Movies.WatchedElseWhere", string.Empty);
            GUIUtils.SetProperty("#Trakt.Statistics.Movies.WatchedTrakt", string.Empty);
            GUIUtils.SetProperty("#Trakt.Statistics.Movies.WatchedTraktUnique", string.Empty);
            GUIUtils.SetProperty("#Trakt.Statistics.Movies.WatchedUnique", string.Empty);
            #endregion
        }

        internal static void SetStatisticProperties(TraktUserProfile.Statistics stats)
        {
            if (stats == null) return;

            #region Friends Statistics
            if (stats.Friends != null)
            {
                GUIUtils.SetProperty("#Trakt.Statistics.Friends", stats.Friends);
            }
            #endregion

            #region Shows Statistics
            if (stats.Shows != null)
            {
                GUIUtils.SetProperty("#Trakt.Statistics.Shows.Library", stats.Shows.Library);
                GUIUtils.SetProperty("#Trakt.Statistics.Shows.Watched", stats.Shows.Watched);
                GUIUtils.SetProperty("#Trakt.Statistics.Shows.Collection", stats.Shows.Collection);
                GUIUtils.SetProperty("#Trakt.Statistics.Shows.Shouts", stats.Shows.Shouts);
                GUIUtils.SetProperty("#Trakt.Statistics.Shows.Loved", stats.Shows.Loved);
                GUIUtils.SetProperty("#Trakt.Statistics.Shows.Hated", stats.Shows.Hated);
            }
            #endregion

            #region Episodes Statistics
            if (stats.Episodes != null)
            {
                GUIUtils.SetProperty("#Trakt.Statistics.Episodes.Checkins", stats.Episodes.Checkins);
                GUIUtils.SetProperty("#Trakt.Statistics.Episodes.CheckinsUnique", stats.Episodes.CheckinsUnique);
                GUIUtils.SetProperty("#Trakt.Statistics.Episodes.Collection", stats.Episodes.Collection);
                GUIUtils.SetProperty("#Trakt.Statistics.Episodes.Hated", stats.Episodes.Hated);
                GUIUtils.SetProperty("#Trakt.Statistics.Episodes.Loved", stats.Episodes.Loved);
                GUIUtils.SetProperty("#Trakt.Statistics.Episodes.Scrobbles", stats.Episodes.Scrobbles);
                GUIUtils.SetProperty("#Trakt.Statistics.Episodes.ScrobblesUnique", stats.Episodes.ScrobblesUnique);
                GUIUtils.SetProperty("#Trakt.Statistics.Episodes.Seen", stats.Episodes.Seen);
                GUIUtils.SetProperty("#Trakt.Statistics.Episodes.Shouts", stats.Episodes.Shouts);
                GUIUtils.SetProperty("#Trakt.Statistics.Episodes.UnWatched", stats.Episodes.UnWatched);
                GUIUtils.SetProperty("#Trakt.Statistics.Episodes.Watched", stats.Episodes.Watched);
                GUIUtils.SetProperty("#Trakt.Statistics.Episodes.WatchedElseWhere", stats.Episodes.WatchedElseWhere);
                GUIUtils.SetProperty("#Trakt.Statistics.Episodes.WatchedTrakt", stats.Episodes.WatchedTrakt);
                GUIUtils.SetProperty("#Trakt.Statistics.Episodes.WatchedTraktUnique", stats.Episodes.WatchedTraktUnique);
                GUIUtils.SetProperty("#Trakt.Statistics.Episodes.WatchedUnique", stats.Episodes.WatchedUnique);
            }
            #endregion

            #region Movies Statistics
            if (stats.Movies != null)
            {
                GUIUtils.SetProperty("#Trakt.Statistics.Movies.Checkins", stats.Movies.Checkins);
                GUIUtils.SetProperty("#Trakt.Statistics.Movies.CheckinsUnique", stats.Movies.CheckinsUnique);
                GUIUtils.SetProperty("#Trakt.Statistics.Movies.Collection", stats.Movies.Collection);
                GUIUtils.SetProperty("#Trakt.Statistics.Movies.Hated", stats.Movies.Hated);
                GUIUtils.SetProperty("#Trakt.Statistics.Movies.Library", stats.Movies.Library);
                GUIUtils.SetProperty("#Trakt.Statistics.Movies.Loved", stats.Movies.Loved);
                GUIUtils.SetProperty("#Trakt.Statistics.Movies.Scrobbles", stats.Movies.Scrobbles);
                GUIUtils.SetProperty("#Trakt.Statistics.Movies.ScrobblesUnique", stats.Movies.ScrobblesUnique);
                GUIUtils.SetProperty("#Trakt.Statistics.Movies.Seen", stats.Movies.Seen);
                GUIUtils.SetProperty("#Trakt.Statistics.Movies.Shouts", stats.Movies.Shouts);
                GUIUtils.SetProperty("#Trakt.Statistics.Movies.UnWatched", stats.Movies.UnWatched);
                GUIUtils.SetProperty("#Trakt.Statistics.Movies.Watched", stats.Movies.Watched);
                GUIUtils.SetProperty("#Trakt.Statistics.Movies.WatchedElseWhere", stats.Movies.WatchedElseWhere);
                GUIUtils.SetProperty("#Trakt.Statistics.Movies.WatchedTrakt", stats.Movies.WatchedTrakt);
                GUIUtils.SetProperty("#Trakt.Statistics.Movies.WatchedTraktUnique", stats.Movies.WatchedTraktUnique);
                GUIUtils.SetProperty("#Trakt.Statistics.Movies.WatchedUnique", stats.Movies.WatchedUnique);
            }
            #endregion
        }

        internal static void SetShoutProperties(TraktShout shout)
        {
            GUIUtils.SetProperty("#Trakt.Shout.Inserted", shout.InsertedDate.FromEpoch().ToLongDateString());
            GUIUtils.SetProperty("#Trakt.Shout.Spoiler", shout.Spoiler.ToString());
            GUIUtils.SetProperty("#Trakt.Shout.UserAdvancedRating", shout.UserRatings.AdvancedRating.ToString());
            GUIUtils.SetProperty("#Trakt.Shout.UserRating", shout.UserRatings.Rating.ToString());
            GUIUtils.SetProperty("#Trakt.Shout.Type", shout.Type);
            GUIUtils.SetProperty("#Trakt.Shout.Id", shout.Id.ToString());
            GUIUtils.SetProperty("#Trakt.Shout.Likes", shout.Likes.ToString());
            GUIUtils.SetProperty("#Trakt.Shout.Replies", shout.Replies.ToString());

            if (TraktSettings.HideSpoilersOnShouts && shout.Spoiler)
                GUIUtils.SetProperty("#Trakt.Shout.Text", Translation.HiddenToPreventSpoilers);
            else
                GUIUtils.SetProperty("#Trakt.Shout.Text", System.Web.HttpUtility.HtmlDecode(shout.Shout.RemapHighOrderChars()).StripHTML());
        }

        internal static void SetMovieProperties(TraktMovie movie)
        {
            if (movie == null) return;

            SetProperty("#Trakt.Movie.Imdb", movie.IMDBID);
            SetProperty("#Trakt.Movie.Certification", movie.Certification);
            SetProperty("#Trakt.Movie.Overview", string.IsNullOrEmpty(movie.Overview) ? Translation.NoMovieSummary : movie.Overview.RemapHighOrderChars());
            SetProperty("#Trakt.Movie.Released", movie.Year == "0" ? "N/A" : movie.Released.FromEpoch().ToShortDateString());
            SetProperty("#Trakt.Movie.Runtime", movie.Runtime.ToString());
            SetProperty("#Trakt.Movie.Tagline", movie.Tagline);
            SetProperty("#Trakt.Movie.Title", movie.Title.RemapHighOrderChars());
            SetProperty("#Trakt.Movie.Tmdb", movie.TMDBID);
            SetProperty("#Trakt.Movie.Trailer", movie.Trailer);
            SetProperty("#Trakt.Movie.Url", movie.Url);
            SetProperty("#Trakt.Movie.Year", movie.Year);
            SetProperty("#Trakt.Movie.Genres", string.Join(", ", movie.Genres.ToArray()));
            SetProperty("#Trakt.Movie.PosterImageFilename", movie.Images.Poster.LocalImageFilename(ArtworkType.MoviePoster));
            SetProperty("#Trakt.Movie.FanartImageFilename", movie.Images.Fanart.LocalImageFilename(ArtworkType.MovieFanart));
            SetProperty("#Trakt.Movie.InCollection", movie.InCollection.ToString());
            SetProperty("#Trakt.Movie.InWatchList", movie.InWatchList.ToString());
            SetProperty("#Trakt.Movie.Plays", movie.Plays.ToString());
            SetProperty("#Trakt.Movie.Watched", movie.Watched.ToString());
            SetProperty("#Trakt.Movie.Rating", movie.Rating);
            SetProperty("#Trakt.Movie.RatingAdvanced", movie.RatingAdvanced.ToString());
            SetProperty("#Trakt.Movie.Ratings.Icon", (movie.Ratings.LovedCount > movie.Ratings.HatedCount) ? "love" : "hate");
            SetProperty("#Trakt.Movie.Ratings.HatedCount", movie.Ratings.HatedCount.ToString());
            SetProperty("#Trakt.Movie.Ratings.LovedCount", movie.Ratings.LovedCount.ToString());
            SetProperty("#Trakt.Movie.Ratings.Percentage", movie.Ratings.Percentage.ToString());
            SetProperty("#Trakt.Movie.Ratings.Votes", movie.Ratings.Votes.ToString());
        }

        internal static void ClearSeasonProperties()
        {
            GUIUtils.SetProperty("#Trakt.Season.Number", string.Empty);
            GUIUtils.SetProperty("#Trakt.Season.EpisodeCount", string.Empty);
            GUIUtils.SetProperty("#Trakt.Season.Url", string.Empty);
            GUIUtils.SetProperty("#Trakt.Season.PosterImageFilename", string.Empty);
        }

        internal static void SetSeasonProperties(TraktShowSeason season)
        {
            GUIUtils.SetProperty("#Trakt.Season.Number", season.Season.ToString());
            GUIUtils.SetProperty("#Trakt.Season.EpisodeCount", season.EpisodeCount.ToString());
            GUIUtils.SetProperty("#Trakt.Season.Url", season.Url);
            GUIUtils.SetProperty("#Trakt.Season.PosterImageFilename", season.Images.Poster.LocalImageFilename(ArtworkType.SeasonPoster));
        }

        internal static void ClearShowProperties()
        {
            GUIUtils.SetProperty("#Trakt.Show.Imdb", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.Tvdb", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.TvRage", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.Title", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.Url", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.AirDay", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.AirTime", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.AirTimeLocalized", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.Certification", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.Country", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.FirstAired", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.Network", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.Overview", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.Runtime", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.Year", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.Genres", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.InWatchList", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.Watched", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.Plays", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.Rating", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.RatingAdvanced", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.Ratings.Icon", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.Ratings.HatedCount", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.Ratings.LovedCount", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.Ratings.Percentage", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.Ratings.Votes", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.FanartImageFilename", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.PosterImageFilename", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.BannerImageFilename", string.Empty);
        }

        internal static void SetShowProperties(TraktShow show)
        {
            if (show == null) return;

            SetProperty("#Trakt.Show.Imdb", show.Imdb);
            SetProperty("#Trakt.Show.Tvdb", show.Tvdb);
            SetProperty("#Trakt.Show.TvRage", show.TvRage);
            SetProperty("#Trakt.Show.Title", show.Title.RemapHighOrderChars());
            SetProperty("#Trakt.Show.Url", show.Url);
            SetProperty("#Trakt.Show.AirDay", show.AirDay);
            SetProperty("#Trakt.Show.AirTime", show.AirTime);
            SetProperty("#Trakt.Show.AirTimeLocalized", show.AirTimeLocalized);
            SetProperty("#Trakt.Show.Certification", show.Certification);
            SetProperty("#Trakt.Show.Country", show.Country);
            SetProperty("#Trakt.Show.FirstAired", show.FirstAired.FromEpoch().ToShortDateString());
            SetProperty("#Trakt.Show.Network", show.Network);
            SetProperty("#Trakt.Show.Overview", string.IsNullOrEmpty(show.Overview) ? Translation.NoShowSummary : show.Overview.RemapHighOrderChars());
            SetProperty("#Trakt.Show.Runtime", show.Runtime.ToString());
            SetProperty("#Trakt.Show.Year", show.Year.ToString());
            SetProperty("#Trakt.Show.Genres", string.Join(", ", show.Genres.ToArray()));
            SetProperty("#Trakt.Show.InWatchList", show.InWatchList.ToString());
            SetProperty("#Trakt.Show.Watched", show.Watched.ToString());
            SetProperty("#Trakt.Show.Plays", show.Plays.ToString());
            SetProperty("#Trakt.Show.Rating", show.Rating);
            SetProperty("#Trakt.Show.RatingAdvanced", show.RatingAdvanced.ToString());
            // Ratings not returned for all API methods that contain a show object! 
            if (show.Ratings != null)
            {
                SetProperty("#Trakt.Show.Ratings.Icon", (show.Ratings.LovedCount > show.Ratings.HatedCount) ? "love" : "hate");
                SetProperty("#Trakt.Show.Ratings.HatedCount", show.Ratings.HatedCount.ToString());
                SetProperty("#Trakt.Show.Ratings.LovedCount", show.Ratings.LovedCount.ToString());
                SetProperty("#Trakt.Show.Ratings.Percentage", show.Ratings.Percentage.ToString());
                SetProperty("#Trakt.Show.Ratings.Votes", show.Ratings.Votes.ToString());
            }
            SetProperty("#Trakt.Show.FanartImageFilename", show.Images.Fanart.LocalImageFilename(ArtworkType.ShowFanart));
            SetProperty("#Trakt.Show.PosterImageFilename", show.Images.Poster.LocalImageFilename(ArtworkType.ShowPoster));
            SetProperty("#Trakt.Show.BannerImageFilename", show.Images.Banner.LocalImageFilename(ArtworkType.ShowBanner));
        }

        internal static void ClearEpisodeProperties()
        {
            GUIUtils.SetProperty("#Trakt.Episode.Number", string.Empty);
            GUIUtils.SetProperty("#Trakt.Episode.Season", string.Empty);
            GUIUtils.SetProperty("#Trakt.Episode.TVDbID", string.Empty);
            GUIUtils.SetProperty("#Trakt.Episode.IMDbID", string.Empty);
            GUIUtils.SetProperty("#Trakt.Episode.Season", string.Empty);
            GUIUtils.SetProperty("#Trakt.Episode.FirstAired", string.Empty);
            GUIUtils.SetProperty("#Trakt.Episode.FirstAiredLocalized", string.Empty);
            GUIUtils.SetProperty("#Trakt.Episode.FirstAiredLocalizedDayOfWeek", string.Empty);
            GUIUtils.SetProperty("#Trakt.Episode.Title", string.Empty);
            GUIUtils.SetProperty("#Trakt.Episode.Url", string.Empty);
            GUIUtils.SetProperty("#Trakt.Episode.Overview", string.Empty);
            GUIUtils.SetProperty("#Trakt.Episode.Runtime", string.Empty);
            GUIUtils.SetProperty("#Trakt.Episode.InWatchList", string.Empty);
            GUIUtils.SetProperty("#Trakt.Episode.InCollection", string.Empty);
            GUIUtils.SetProperty("#Trakt.Episode.Plays", string.Empty);
            GUIUtils.SetProperty("#Trakt.Episode.Watched", string.Empty);
            GUIUtils.SetProperty("#Trakt.Episode.Rating", string.Empty);
            GUIUtils.SetProperty("#Trakt.Episode.RatingAdvanced", string.Empty);
            GUIUtils.SetProperty("#Trakt.Episode.Ratings.Icon", string.Empty);
            GUIUtils.SetProperty("#Trakt.Episode.Ratings.HatedCount", string.Empty);
            GUIUtils.SetProperty("#Trakt.Episode.Ratings.LovedCount", string.Empty);
            GUIUtils.SetProperty("#Trakt.Episode.Ratings.Percentage", string.Empty);
            GUIUtils.SetProperty("#Trakt.Episode.Ratings.Votes", string.Empty);
            GUIUtils.SetProperty("#Trakt.Episode.EpisodeImageFilename", string.Empty);
        }

        internal static void SetEpisodeProperties(TraktEpisode episode)
        {
            if (episode == null) return;

            SetProperty("#Trakt.Episode.Number", episode.Number.ToString());
            SetProperty("#Trakt.Episode.Season", episode.Season.ToString());
            SetProperty("#Trakt.Episode.TVDbID", episode.TVDbID.ToString());
            SetProperty("#Trakt.Episode.IMDbID", episode.IMDbID ?? string.Empty);
            SetProperty("#Trakt.Episode.FirstAired", episode.FirstAired == 0 ? " " : episode.FirstAired.FromEpoch().ToShortDateString());
            SetProperty("#Trakt.Episode.FirstAiredLocalized", episode.FirstAiredLocalized == 0 ? " " : episode.FirstAiredLocalized.FromEpoch().ToShortDateString());
            SetProperty("#Trakt.Episode.FirstAiredLocalizedDayOfWeek", episode.FirstAiredLocalized == 0 ? " " : episode.FirstAiredLocalized.FromEpoch().DayOfWeek.ToString());
            SetProperty("#Trakt.Episode.Title", string.IsNullOrEmpty(episode.Title) ? string.Format("{0} {1}", Translation.Episode, episode.Number.ToString()) : episode.Title.RemapHighOrderChars());
            SetProperty("#Trakt.Episode.Url", episode.Url);
            SetProperty("#Trakt.Episode.Overview", string.IsNullOrEmpty(episode.Overview) ? Translation.NoEpisodeSummary : episode.Overview.RemapHighOrderChars());
            SetProperty("#Trakt.Episode.Runtime", episode.Runtime.ToString());
            SetProperty("#Trakt.Episode.InWatchList", episode.InWatchList.ToString());
            SetProperty("#Trakt.Episode.InCollection", episode.InCollection.ToString());
            SetProperty("#Trakt.Episode.Plays", episode.Plays.ToString());
            SetProperty("#Trakt.Episode.Watched", episode.Watched.ToString());
            SetProperty("#Trakt.Episode.Rating", episode.Rating);
            SetProperty("#Trakt.Episode.RatingAdvanced", episode.RatingAdvanced.ToString());
            SetProperty("#Trakt.Episode.Ratings.Icon", ((episode.Ratings != null) && (episode.Ratings.LovedCount > episode.Ratings.HatedCount)) ? "love" : "hate");
            SetProperty("#Trakt.Episode.Ratings.HatedCount", episode.Ratings != null ? episode.Ratings.HatedCount.ToString() : "0");
            SetProperty("#Trakt.Episode.Ratings.LovedCount", episode.Ratings != null ? episode.Ratings.LovedCount.ToString() : "0");
            SetProperty("#Trakt.Episode.Ratings.Percentage", episode.Ratings != null ? episode.Ratings.Percentage.ToString() : "0");
            SetProperty("#Trakt.Episode.Ratings.Votes", episode.Ratings != null ? episode.Ratings.Votes.ToString() : "0");
            SetProperty("#Trakt.Episode.EpisodeImageFilename", episode.Images.Screen.LocalImageFilename(ArtworkType.EpisodeImage));
        }

        internal static void ClearPersonProperties()
        {
            GUIUtils.SetProperty("#Trakt.Person.Name", string.Empty);
            GUIUtils.SetProperty("#Trakt.Person.HeadshotUrl", string.Empty);
            GUIUtils.SetProperty("#Trakt.Person.HeadshotFilename", string.Empty);
            GUIUtils.SetProperty("#Trakt.Person.Url", string.Empty);
            GUIUtils.SetProperty("#Trakt.Person.Biography", string.Empty);
            GUIUtils.SetProperty("#Trakt.Person.Birthday", string.Empty);
            GUIUtils.SetProperty("#Trakt.Person.Birthplace", string.Empty);
            GUIUtils.SetProperty("#Trakt.Person.TmdbId", string.Empty);
        }

        internal static void SetPersonProperties(TraktPersonSummary person)
        {
            GUIUtils.SetProperty("#Trakt.Person.Name", person.Name);
            GUIUtils.SetProperty("#Trakt.Person.HeadshotUrl", person.Images.Headshot);
            GUIUtils.SetProperty("#Trakt.Person.HeadshotFilename", person.Images.Headshot.LocalImageFilename(ArtworkType.Headshot));
            GUIUtils.SetProperty("#Trakt.Person.Url", person.Url);
            GUIUtils.SetProperty("#Trakt.Person.Biography", person.Biography ?? Translation.NoPersonBiography.RemapHighOrderChars());
            GUIUtils.SetProperty("#Trakt.Person.Birthday", person.Birthday);
            GUIUtils.SetProperty("#Trakt.Person.Birthplace", person.Birthplace);
            GUIUtils.SetProperty("#Trakt.Person.TmdbId", person.TmdbId.ToString());
        }

        #endregion

        #region GUI Context Menus

        #region Activity

        /// <summary>
        /// Returns a list of context menu items for a selected item in the Activity Dashboard
        /// Activity API does not return authenticated data such as InWatchlist, InCollection, Rating etc.
        /// </summary>
        internal static List<GUIListItem> GetContextMenuItemsForActivity()
        {
            GUIListItem listItem = null;
            List<GUIListItem> listItems = new List<GUIListItem>();

            // Add Watch List  
            listItem = new GUIListItem(Translation.AddToWatchList);
            listItem.ItemId = (int)ActivityContextMenuItem.AddToWatchList;
            listItems.Add(listItem);

            // Add to Custom list
            listItem = new GUIListItem(Translation.AddToList);
            listItem.ItemId = (int)ActivityContextMenuItem.AddToList;
            listItems.Add(listItem);

            // Shouts
            listItem = new GUIListItem(Translation.Shouts);
            listItem.ItemId = (int)ActivityContextMenuItem.Shouts;
            listItems.Add(listItem);

            // Rate
            listItem = new GUIListItem(Translation.Rate + "...");
            listItem.ItemId = (int)ActivityContextMenuItem.Rate;
            listItems.Add(listItem);

            // Trailers
            if (TraktHelper.IsOnlineVideosAvailableAndEnabled)
            {
                listItem = new GUIListItem(Translation.Trailers);
                listItem.ItemId = (int)ActivityContextMenuItem.Trailers;
                listItems.Add(listItem);
            }

            return listItems;
        }

        internal static void CreateTrendingMoviesContextMenu(ref IDialogbox dlg, TraktMovie movie, bool dashboard)
        {
            GUIListItem listItem = null;

            // Mark As Watched
            if (!movie.Watched)
            {
                listItem = new GUIListItem(Translation.MarkAsWatched);
                dlg.Add(listItem);
                listItem.ItemId = (int)TrendingContextMenuItem.MarkAsWatched;
            }

            // Mark As UnWatched
            if (movie.Watched)
            {
                listItem = new GUIListItem(Translation.MarkAsUnWatched);
                dlg.Add(listItem);
                listItem.ItemId = (int)TrendingContextMenuItem.MarkAsUnWatched;
            }

            // Add/Remove Watch List            
            if (!movie.InWatchList)
            {
                listItem = new GUIListItem(Translation.AddToWatchList);
                dlg.Add(listItem);
                listItem.ItemId = (int)TrendingContextMenuItem.AddToWatchList;
            }
            else
            {
                listItem = new GUIListItem(Translation.RemoveFromWatchList);
                dlg.Add(listItem);
                listItem.ItemId = (int)TrendingContextMenuItem.RemoveFromWatchList;
            }

            // Add to Custom list
            listItem = new GUIListItem(Translation.AddToList);
            dlg.Add(listItem);
            listItem.ItemId = (int)TrendingContextMenuItem.AddToList;

            // Add to Library
            // Don't allow if it will be removed again on next sync
            // movie could be part of a DVD collection
            if (!movie.InCollection && !TraktSettings.KeepTraktLibraryClean)
            {
                listItem = new GUIListItem(Translation.AddToLibrary);
                dlg.Add(listItem);
                listItem.ItemId = (int)TrendingContextMenuItem.AddToLibrary;
            }

            if (movie.InCollection)
            {
                listItem = new GUIListItem(Translation.RemoveFromLibrary);
                dlg.Add(listItem);
                listItem.ItemId = (int)TrendingContextMenuItem.RemoveFromLibrary;
            }

            // Filters
            if (TraktSettings.FilterTrendingOnDashboard || !dashboard)
            {
                listItem = new GUIListItem(Translation.Filters + "...");
                dlg.Add(listItem);
                listItem.ItemId = (int)TrendingContextMenuItem.Filters;
            }

            // Related Movies
            listItem = new GUIListItem(Translation.RelatedMovies);
            dlg.Add(listItem);
            listItem.ItemId = (int)TrendingContextMenuItem.Related;

            // Rate Movie
            listItem = new GUIListItem(Translation.RateMovie);
            dlg.Add(listItem);
            listItem.ItemId = (int)TrendingContextMenuItem.Rate;

            // Shouts
            listItem = new GUIListItem(Translation.Shouts);
            dlg.Add(listItem);
            listItem.ItemId = (int)TrendingContextMenuItem.Shouts;

            // Trailers
            if (TraktHelper.IsOnlineVideosAvailableAndEnabled)
            {
                listItem = new GUIListItem(Translation.Trailers);
                dlg.Add(listItem);
                listItem.ItemId = (int)TrendingContextMenuItem.Trailers;
            }

            // Change Layout
            if (GUIWindowManager.ActiveWindow == (int)TraktGUIWindows.TrendingMovies)
            {
                listItem = new GUIListItem(Translation.ChangeLayout);
                dlg.Add(listItem);
                listItem.ItemId = (int)TrendingContextMenuItem.ChangeLayout;
            }

            if (!movie.InCollection && TraktHelper.IsMpNZBAvailableAndEnabled)
            {
                // Search for movie with mpNZB
                listItem = new GUIListItem(Translation.SearchWithMpNZB);
                dlg.Add(listItem);
                listItem.ItemId = (int)TrendingContextMenuItem.SearchWithMpNZB;
            }

            if (!movie.InCollection && TraktHelper.IsMyTorrentsAvailableAndEnabled)
            {
                // Search for movie with MyTorrents
                listItem = new GUIListItem(Translation.SearchTorrent);
                dlg.Add(listItem);
                listItem.ItemId = (int)TrendingContextMenuItem.SearchTorrent;
            }

        }

        internal static void CreateTrendingShowsContextMenu(ref IDialogbox dlg, TraktShow show, bool dashboard)
        {
            GUIListItem listItem = null;

            // Add/Remove Watch List            
            if (!show.InWatchList)
            {
                listItem = new GUIListItem(Translation.AddToWatchList);
                dlg.Add(listItem);
                listItem.ItemId = (int)TrendingContextMenuItem.AddToWatchList;
            }
            else
            {
                listItem = new GUIListItem(Translation.RemoveFromWatchList);
                dlg.Add(listItem);
                listItem.ItemId = (int)TrendingContextMenuItem.RemoveFromWatchList;
            }

            // Show Season Information
            listItem = new GUIListItem(Translation.ShowSeasonInfo);
            dlg.Add(listItem);
            listItem.ItemId = (int)TrendingContextMenuItem.ShowSeasonInfo;

            // Mark Show as Watched
            listItem = new GUIListItem(Translation.MarkAsWatched);
            dlg.Add(listItem);
            listItem.ItemId = (int)TrendingContextMenuItem.MarkAsWatched;

            // Add Show to Library
            listItem = new GUIListItem(Translation.AddToLibrary);
            dlg.Add(listItem);
            listItem.ItemId = (int)TrendingContextMenuItem.AddToLibrary;

            // Add to Custom List
            listItem = new GUIListItem(Translation.AddToList);
            dlg.Add(listItem);
            listItem.ItemId = (int)TrendingContextMenuItem.AddToList;

            if (TraktHelper.IsOnlineVideosAvailableAndEnabled)
            {
                listItem = new GUIListItem(Translation.Trailers);
                dlg.Add(listItem);
                listItem.ItemId = (int)TrendingContextMenuItem.Trailers;
            }

            // Filters
            if (TraktSettings.FilterTrendingOnDashboard || !dashboard)
            {
                listItem = new GUIListItem(Translation.Filters + "...");
                dlg.Add(listItem);
                listItem.ItemId = (int)TrendingContextMenuItem.Filters;
            }

            // Related Shows
            listItem = new GUIListItem(Translation.RelatedShows);
            dlg.Add(listItem);
            listItem.ItemId = (int)TrendingContextMenuItem.Related;

            // Rate Show
            listItem = new GUIListItem(Translation.RateShow);
            dlg.Add(listItem);
            listItem.ItemId = (int)TrendingContextMenuItem.Rate;

            // Shouts
            listItem = new GUIListItem(Translation.Shouts);
            dlg.Add(listItem);
            listItem.ItemId = (int)TrendingContextMenuItem.Shouts;

            // Change Layout
            if (GUIWindowManager.ActiveWindow == (int)TraktGUIWindows.TrendingShows)
            {
                listItem = new GUIListItem(Translation.ChangeLayout);
                dlg.Add(listItem);
                listItem.ItemId = (int)TrendingContextMenuItem.ChangeLayout;
            }

            if (TraktHelper.IsMpNZBAvailableAndEnabled)
            {
                // Search for show with mpNZB
                listItem = new GUIListItem(Translation.SearchWithMpNZB);
                dlg.Add(listItem);
                listItem.ItemId = (int)TrendingContextMenuItem.SearchWithMpNZB;
            }

            if (TraktHelper.IsMyTorrentsAvailableAndEnabled)
            {
                // Search for show with MyTorrents
                listItem = new GUIListItem(Translation.SearchTorrent);
                dlg.Add(listItem);
                listItem.ItemId = (int)TrendingContextMenuItem.SearchTorrent;
            }
        }

        #endregion

        #region Layout
        internal static Layout ShowLayoutMenu(Layout currentLayout, int itemToSelect)
        {
            Layout newLayout = currentLayout;

            IDialogbox dlg = (IDialogbox)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
            dlg.Reset();
            dlg.SetHeading(GetLayoutTranslation(currentLayout));

            foreach (Layout layout in Enum.GetValues(typeof(Layout)))
            {
                string menuItem = GetLayoutTranslation(layout);
                GUIListItem pItem = new GUIListItem(menuItem);
                if (layout == currentLayout) pItem.Selected = true;
                dlg.Add(pItem);
            }

            dlg.DoModal(GUIWindowManager.ActiveWindow);

            if (dlg.SelectedLabel >= 0)
            {
                var facade = GUIWindowManager.GetWindow(GUIWindowManager.ActiveWindow).GetControl((int)TraktGUIControls.Facade) as GUIFacadeControl;

                newLayout = (Layout)dlg.SelectedLabel;
                facade.SetCurrentLayout(Enum.GetName(typeof(Layout), newLayout));
                GUIControl.SetControlLabel(GUIWindowManager.ActiveWindow, (int)TraktGUIControls.Layout, GetLayoutTranslation(newLayout));
                // when loosing focus from the facade the current selected index is lost
                // e.g. changing layout from skin side menu
                facade.SelectIndex(itemToSelect);
            }
            return newLayout;
        }

        internal static string GetLayoutTranslation(Layout layout)
        {
            bool mp12 = TraktSettings.MPVersion <= new Version(1, 2, 0, 0);

            string strLine = string.Empty;
            switch (layout)
            {
                case Layout.List:
                    strLine = GUILocalizeStrings.Get(101);
                    break;
                case Layout.SmallIcons:
                    strLine = GUILocalizeStrings.Get(100);
                    break;
                case Layout.LargeIcons:
                    strLine = GUILocalizeStrings.Get(417);
                    break;
                case Layout.Filmstrip:
                    strLine = GUILocalizeStrings.Get(733);
                    break;
            }
            return mp12 ? strLine : GUILocalizeStrings.Get(95) + strLine;
        }
        #endregion

        #region SortBy

        internal static SortBy ShowSortMenu(SortBy currentSortBy)
        {
            var newSortBy = new SortBy();

            GUIDialogMenu dlg = (GUIDialogMenu)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
            if (dlg == null) return null;

            dlg.Reset();
            dlg.SetHeading(495); // Sort options

            // Add generic sortby fields
            GUIListItem pItem = new GUIListItem(Translation.Title);
            dlg.Add(pItem);
            pItem.ItemId = (int)SortingFields.Title;

            pItem = new GUIListItem(Translation.ReleaseDate);
            dlg.Add(pItem);
            pItem.ItemId = (int)SortingFields.ReleaseDate;

            pItem = new GUIListItem(Translation.Score);
            dlg.Add(pItem);
            pItem.ItemId = (int)SortingFields.Score;

            pItem = new GUIListItem(Translation.Votes);
            dlg.Add(pItem);
            pItem.ItemId = (int)SortingFields.Votes;

            pItem = new GUIListItem(Translation.Runtime);
            dlg.Add(pItem);
            pItem.ItemId = (int)SortingFields.Runtime;

            // Trending
            if (GUIWindowManager.ActiveWindow == (int)TraktGUIWindows.TrendingMovies || 
                GUIWindowManager.ActiveWindow == (int)TraktGUIWindows.TrendingShows) {
                pItem = new GUIListItem(Translation.Watchers);
                dlg.Add(pItem);
                pItem.ItemId = (int)SortingFields.PeopleWatching;
            }

            // Watchlist
            if (GUIWindowManager.ActiveWindow == (int)TraktGUIWindows.WatchedListMovies || 
                GUIWindowManager.ActiveWindow == (int)TraktGUIWindows.WatchedListShows) {
                pItem = new GUIListItem(Translation.Inserted);
                dlg.Add(pItem);
                pItem.ItemId = (int)SortingFields.WatchListInserted;
            }

            // set the focus to currently used sort method
            dlg.SelectedLabel = (int)currentSortBy.Field;

            // show dialog and wait for result
            dlg.DoModal(GUIWindowManager.ActiveWindow);
            if (dlg.SelectedId == -1) return null;
            
            switch (dlg.SelectedId)
            {
                case (int)SortingFields.Title:
                    newSortBy.Field = SortingFields.Title;
                    break;

                case (int)SortingFields.ReleaseDate:
                    newSortBy.Field = SortingFields.ReleaseDate;
                    newSortBy.Direction = SortingDirections.Descending;
                    break;

                case (int)SortingFields.Score:
                    newSortBy.Field = SortingFields.Score;
                    newSortBy.Direction = SortingDirections.Descending;
                    break;

                case (int)SortingFields.Votes:
                    newSortBy.Field = SortingFields.Votes;
                    newSortBy.Direction = SortingDirections.Descending;
                    break;

                case (int)SortingFields.Runtime:
                    newSortBy.Field = SortingFields.Runtime;
                    break;

                case (int)SortingFields.PeopleWatching:
                    newSortBy.Field = SortingFields.PeopleWatching;
                    newSortBy.Direction = SortingDirections.Descending;
                    break;

                case (int)SortingFields.WatchListInserted:
                    newSortBy.Field = SortingFields.WatchListInserted;
                    break;

                default:
                    newSortBy.Field = SortingFields.Title;
                    break;
            }

            return newSortBy;
        }

        internal static string GetSortByString(SortBy currentSortBy)
        {
            string strLine = string.Empty;

            switch (currentSortBy.Field)
            {
                case SortingFields.Title:
                    strLine = Translation.Title;
                    break;

                case SortingFields.ReleaseDate:
                    strLine = Translation.ReleaseDate;
                    break;

                case SortingFields.Score:
                    strLine = Translation.Score;
                    break;

                case SortingFields.Votes:
                    strLine = Translation.Votes;
                    break;

                case SortingFields.Runtime:
                    strLine = Translation.Runtime;
                    break;

                case SortingFields.PeopleWatching:
                    strLine = Translation.Watchers;
                    break;

                case SortingFields.WatchListInserted:
                    strLine = Translation.Inserted;
                    break;
                
                default:
                    strLine = Translation.Title;
                    break;
            }

            return string.Format(Translation.SortBy, strLine);
        }

        #endregion

        #region Movie Trailers
        public static void ShowMovieTrailersPluginMenu(TraktMovie movie)
        {
            MediaItem trailerItem = new MediaItem
            {
                IMDb = movie.IMDBID,
                Plot = movie.Overview,
                Poster = movie.Images.Poster,
                Title = movie.Title,
                TMDb = movie.TMDBID,
                Year = int.Parse(movie.Year)
            };
            Trailers.Trailers.SearchForTrailers(trailerItem);
        }

        public static void ShowMovieTrailersMenu(TraktMovie movie)
        {
            if (TraktSettings.UseTrailersPlugin && TraktHelper.IsTrailersAvailableAndEnabled)
            {
                ShowMovieTrailersPluginMenu(movie);
                return;
            }

            var dlg = (IDialogbox)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
            dlg.Reset();
            dlg.SetHeading(Translation.Trailer);

            if (!string.IsNullOrEmpty(movie.Trailer))
            {
                // trailer can be played without searching
                GUIListItem pItem = new GUIListItem(Translation.PlayTrailer);
                dlg.Add(pItem);
            }

            foreach (var site in Enum.GetValues(typeof(TrailerSiteMovies)))
            {
                string menuItem = Enum.GetName(typeof(TrailerSiteMovies), site);
                GUIListItem pItem = new GUIListItem(menuItem);
                dlg.Add(pItem);
            }
            
            dlg.DoModal(GUIWindowManager.ActiveWindow);

            if (dlg.SelectedLabel >= 0)
            {
                string siteUtil = string.Empty;
                string searchParam = string.Empty;

                switch (dlg.SelectedLabelText)
                {
                    case ("IMDb"):
                        siteUtil = "IMDb Movie Trailers";
                        if (!string.IsNullOrEmpty(movie.IMDBID))
                            // Exact search
                            searchParam = movie.IMDBID;
                        else
                            searchParam = movie.Title;
                        break;

                    case ("iTunes"):
                        siteUtil = "iTunes Movie Trailers";
                        searchParam = movie.Title;
                        break;

                    case ("YouTube"):
                        siteUtil = "YouTube";
                        searchParam = movie.Title;
                        break;

                    default:
                        if (TraktHelper.IsOnlineVideosAvailableAndEnabled)
                        {
                            TraktHandlers.OnlineVideos.Play(movie.Trailer);
                        }
                        return;
                }

                string loadingParam = string.Format("site:{0}|search:{1}|return:Locked", siteUtil, searchParam);
                
                // Launch OnlineVideos Trailer search
                GUIWindowManager.ActivateWindow((int)ExternalPluginWindows.OnlineVideos, loadingParam);
            }
        }

        #endregion

        #region TV Show Trailers
        public static void ShowTVShowTrailersMenu(TraktShow show, TraktEpisode episode = null)
        {
            IDialogbox dlg = (IDialogbox)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
            dlg.Reset();
            dlg.SetHeading(Translation.Trailer);

            foreach (TrailerSiteShows site in Enum.GetValues(typeof(TrailerSiteShows)))
            {
                string menuItem = Enum.GetName(typeof(TrailerSiteShows), site);
                GUIListItem pItem = new GUIListItem(menuItem);
                dlg.Add(pItem);
            }

            dlg.DoModal(GUIWindowManager.ActiveWindow);

            if (dlg.SelectedLabel >= 0)
            {
                string siteUtil = string.Empty;
                string searchParam = string.Empty;

                switch (dlg.SelectedLabelText)
                {
                    case ("IMDb"):
                        siteUtil = "IMDb Movie Trailers";
                        if (!string.IsNullOrEmpty(show.Imdb))
                            // Exact search
                            searchParam = show.Imdb;
                        else
                            searchParam = show.Title;
                        break;

                    case ("YouTube"):
                        siteUtil = "YouTube";
                        searchParam = show.Title;
                        if (episode != null)
                        {
                            searchParam += string.Format(" S{0}E{1}", episode.Season.ToString("D2"), episode.Number.ToString("D2"));
                        }
                        break;
                }

                string loadingParam = string.Format("site:{0}|search:{1}|return:Locked", siteUtil, searchParam);

                // Launch OnlineVideos Trailer search
                GUIWindowManager.ActivateWindow((int)ExternalPluginWindows.OnlineVideos, loadingParam);
            }
        }
        #endregion

        #region Trakt External Menu

        #region SearchBy Menu
        public static bool ShowSearchByMenu(SearchPeople people, string title, string fanart)
        {
            IDialogbox dlg = (IDialogbox)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
            dlg.Reset();
            dlg.SetHeading(Translation.SearchBy);

            GUIListItem pItem = null;

            if (people.Actors.Count > 0)
            {
                pItem = new GUIListItem(Translation.Actors);
                dlg.Add(pItem);
                pItem.ItemId = (int)TraktSearchByItems.Actors;
                pItem.Label2 = people.Actors.Count.ToString();
            }

            if (people.Directors.Count > 0)
            {
                pItem = new GUIListItem(Translation.Directors);
                dlg.Add(pItem);
                pItem.ItemId = (int)TraktSearchByItems.Directors;
                pItem.Label2 = people.Directors.Count.ToString();
            }

            if (people.Producers.Count > 0)
            {
                pItem = new GUIListItem(Translation.Producers);
                dlg.Add(pItem);
                pItem.ItemId = (int)TraktSearchByItems.Producers;
                pItem.Label2 = people.Producers.Count.ToString();
            }

            if (people.Writers.Count > 0)
            {
                pItem = new GUIListItem(Translation.Writers);
                dlg.Add(pItem);
                pItem.ItemId = (int)TraktSearchByItems.Writers;
                pItem.Label2 = people.Writers.Count.ToString();
            }

            if (people.GuestStars.Count > 0)
            {
                pItem = new GUIListItem(Translation.GuestStars);
                dlg.Add(pItem);
                pItem.ItemId = (int)TraktSearchByItems.GuestStars;
                pItem.Label2 = people.GuestStars.Count.ToString();
            }

            // Show Context Menu
            dlg.DoModal(GUIWindowManager.ActiveWindow);
            if (dlg.SelectedId < 0) return false;

            bool retCode = false;

            if (dlg.SelectedLabelText == Translation.Actors)
                retCode = ShowSearchByPersonMenu(people.Actors, title, fanart);
            if (dlg.SelectedLabelText == Translation.Directors)
                retCode = ShowSearchByPersonMenu(people.Directors, title, fanart);
            if (dlg.SelectedLabelText == Translation.Producers)
                retCode = ShowSearchByPersonMenu(people.Producers, title, fanart);
            if (dlg.SelectedLabelText == Translation.Writers)
                retCode = ShowSearchByPersonMenu(people.Writers, title, fanart);
            if (dlg.SelectedLabelText == Translation.GuestStars)
                retCode = ShowSearchByPersonMenu(people.GuestStars, title, fanart);

            return retCode;
        }

        public static bool ShowSearchByPersonMenu(List<string> people, string title, string fanart)
        {
            var dlg = (IDialogbox)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
            dlg.Reset();
            dlg.SetHeading(Translation.SearchBy);

            GUIListItem pItem = null;
            int itemId = 0;

            pItem = new GUIListItem(Translation.SearchAll);
            dlg.Add(pItem);
            pItem.ItemId = itemId++;

            foreach (var person in people)
            {
                pItem = new GUIListItem(person);
                dlg.Add(pItem);
                pItem.ItemId = itemId++;
            }

            // Show Context Menu
            dlg.DoModal(GUIWindowManager.ActiveWindow);
            if (dlg.SelectedId < 0) return false;

            // Trigger Search
            // If Search By 'All', the parse along list of all people
            if (dlg.SelectedLabelText == Translation.SearchAll)
            {
                var peopleInItem = new PersonSearch { People = people, Title = title, Fanart = fanart };
                GUIWindowManager.ActivateWindow((int)TraktGUIWindows.SearchPeople, peopleInItem.ToJSON());
            }
            else
            {
                GUIWindowManager.ActivateWindow((int)TraktGUIWindows.SearchPeople, dlg.SelectedLabelText);
            }

            return true;
        }

        #endregion

        #region Movies
        public static bool ShowTraktExtMovieMenu(string title, string year, string imdbid, string fanart)
        {
            return ShowTraktExtMovieMenu(title, year, imdbid, fanart, false);
        }
        public static bool ShowTraktExtMovieMenu(string title, string year, string imdbid, string fanart, bool showAll)
        {
            return ShowTraktExtMovieMenu(title, year, imdbid, fanart, null, showAll);
        }
        public static bool ShowTraktExtMovieMenu(string title, string year, string imdbid, string fanart, SearchPeople people, bool showAll)
        {
            IDialogbox dlg = (IDialogbox)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
            dlg.Reset();
            dlg.SetHeading(GUIUtils.PluginName());

            GUIListItem pItem = new GUIListItem(Translation.Shouts);
            dlg.Add(pItem);
            pItem.ItemId = (int)TraktMenuItems.Shouts;

            pItem = new GUIListItem(Translation.Rate);
            dlg.Add(pItem);
            pItem.ItemId = (int)TraktMenuItems.Rate;

            pItem = new GUIListItem(Translation.RelatedMovies);
            dlg.Add(pItem);
            pItem.ItemId = (int)TraktMenuItems.Related;

            pItem = new GUIListItem(Translation.AddToWatchList);
            dlg.Add(pItem);
            pItem.ItemId = (int)TraktMenuItems.AddToWatchList;

            pItem = new GUIListItem(Translation.AddToList);
            dlg.Add(pItem);
            pItem.ItemId = (int)TraktMenuItems.AddToCustomList;

            // Show Search By...
            if (people != null && people.Count != 0)
            {
                pItem = new GUIListItem(Translation.SearchBy + "...");
                dlg.Add(pItem);
                pItem.ItemId = (int)TraktMenuItems.SearchBy;
            }

            // also show non-context sensitive items related to movies
            if (showAll)
            {
                // might want to check your recently watched, stats etc
                pItem = new GUIListItem(Translation.UserProfile);
                dlg.Add(pItem);
                pItem.ItemId = (int)TraktMenuItems.UserProfile;

                pItem = new GUIListItem(Translation.Network);
                dlg.Add(pItem);
                pItem.ItemId = (int)TraktMenuItems.Network;

                pItem = new GUIListItem(Translation.Recommendations);
                dlg.Add(pItem);
                pItem.ItemId = (int)TraktMenuItems.Recommendations;

                pItem = new GUIListItem(Translation.Trending);
                dlg.Add(pItem);
                pItem.ItemId = (int)TraktMenuItems.Trending;

                pItem = new GUIListItem(Translation.WatchList);
                dlg.Add(pItem);
                pItem.ItemId = (int)TraktMenuItems.WatchList;

                pItem = new GUIListItem(Translation.Lists);
                dlg.Add(pItem);
                pItem.ItemId = (int)TraktMenuItems.Lists;
            }

            // Show Context Menu
            dlg.DoModal(GUIWindowManager.ActiveWindow);
            if (dlg.SelectedId < 0) return false;

            switch (dlg.SelectedId)
            {
                case ((int)TraktMenuItems.Rate):
                    TraktLogger.Info("Rating movie '{0} ({1}) [{2}]'", title, year, imdbid);
                    GUIUtils.ShowRateDialog<TraktRateMovie>(TraktHandlers.BasicHandler.CreateMovieRateData(title, year, imdbid));
                    break;

                case ((int)TraktMenuItems.Shouts):
                    TraktLogger.Info("Searching Shouts for movie '{0} ({1}) [{2}]'", title, year, imdbid);
                    TraktHelper.ShowMovieShouts(imdbid, title, year, fanart);
                    break;

                case ((int)TraktMenuItems.Related):
                    TraktLogger.Info("Show Related Movies for '{0} ({1}) [{2}]'", title, year, imdbid);
                    TraktHelper.ShowRelatedMovies(imdbid, title, year);
                    break;

                case ((int)TraktMenuItems.AddToWatchList):
                    TraktLogger.Info("Adding movie '{0} ({1}) [{2}]' to Watch List", title, year, imdbid);
                    TraktHelper.AddMovieToWatchList(title, year, imdbid, true);
                    break;

                case ((int)TraktMenuItems.AddToCustomList):
                    TraktLogger.Info("Adding movie '{0} ({1}) [{2}]' to Custom List", title, year, imdbid);
                    TraktHelper.AddRemoveMovieInUserList(title, year, imdbid, false);
                    break;

                case ((int)TraktMenuItems.SearchBy):
                    ShowSearchByMenu(people, title, fanart);
                    break;

                case ((int)TraktMenuItems.UserProfile):
                    GUIWindowManager.ActivateWindow((int)TraktGUIWindows.UserProfile);
                    break;

                case ((int)TraktMenuItems.Network):
                    GUIWindowManager.ActivateWindow((int)TraktGUIWindows.Network);
                    break;

                case ((int)TraktMenuItems.Recommendations):
                    GUIWindowManager.ActivateWindow((int)TraktGUIWindows.RecommendationsMovies);
                    break;

                case ((int)TraktMenuItems.Trending):
                    GUIWindowManager.ActivateWindow((int)TraktGUIWindows.TrendingMovies);
                    break;

                case ((int)TraktMenuItems.WatchList):
                    GUIWindowManager.ActivateWindow((int)TraktGUIWindows.WatchedListMovies);
                    break;

                case ((int)TraktMenuItems.Lists):
                    GUIWindowManager.ActivateWindow((int)TraktGUIWindows.Lists);
                    break;
            }
            return true;
        }

        #endregion

        #region Shows
        public static bool ShowTraktExtTVShowMenu(string title, string year, string tvdbid, string fanart)
        {
            return ShowTraktExtTVShowMenu(title, year, tvdbid, fanart, false);
        }
        public static bool ShowTraktExtTVShowMenu(string title, string year, string tvdbid, string fanart, bool showAll)
        {
            return ShowTraktExtTVShowMenu(title, year, tvdbid, fanart, null, showAll);
        }
        public static bool ShowTraktExtTVShowMenu(string title, string year, string tvdbid, string fanart, SearchPeople people, bool showAll)
        {
            IDialogbox dlg = (IDialogbox)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
            dlg.Reset();
            dlg.SetHeading(GUIUtils.PluginName());

            GUIListItem pItem = new GUIListItem(Translation.Shouts);
            dlg.Add(pItem);
            pItem.ItemId = (int)TraktMenuItems.Shouts;

            pItem = new GUIListItem(Translation.Rate);
            dlg.Add(pItem);
            pItem.ItemId = (int)TraktMenuItems.Rate;

            pItem = new GUIListItem(Translation.RelatedShows);
            dlg.Add(pItem);
            pItem.ItemId = (int)TraktMenuItems.Related;

            pItem = new GUIListItem(Translation.AddToWatchList);
            dlg.Add(pItem);
            pItem.ItemId = (int)TraktMenuItems.AddToWatchList;

            pItem = new GUIListItem(Translation.AddToList);
            dlg.Add(pItem);
            pItem.ItemId = (int)TraktMenuItems.AddToCustomList;

            // Show SearchBy menu...
            if (people != null && people.Count != 0)
            {
                pItem = new GUIListItem(Translation.SearchBy + "...");
                dlg.Add(pItem);
                pItem.ItemId = (int)TraktMenuItems.SearchBy;
            }

            // also show non-context sensitive items related to shows
            if (showAll)
            {
                // might want to check your recently watched, stats etc
                pItem = new GUIListItem(Translation.UserProfile);
                dlg.Add(pItem);
                pItem.ItemId = (int)TraktMenuItems.UserProfile;

                pItem = new GUIListItem(Translation.Network);
                dlg.Add(pItem);
                pItem.ItemId = (int)TraktMenuItems.Network;

                pItem = new GUIListItem(Translation.Calendar);
                dlg.Add(pItem);
                pItem.ItemId = (int)TraktMenuItems.Calendar;

                pItem = new GUIListItem(Translation.Recommendations);
                dlg.Add(pItem);
                pItem.ItemId = (int)TraktMenuItems.Recommendations;

                pItem = new GUIListItem(Translation.Trending);
                dlg.Add(pItem);
                pItem.ItemId = (int)TraktMenuItems.Trending;

                pItem = new GUIListItem(Translation.WatchList);
                dlg.Add(pItem);
                pItem.ItemId = (int)TraktMenuItems.WatchList;

                pItem = new GUIListItem(Translation.Lists);
                dlg.Add(pItem);
                pItem.ItemId = (int)TraktMenuItems.Lists;
            }

            // Show Context Menu
            dlg.DoModal(GUIWindowManager.ActiveWindow);
            if (dlg.SelectedId < 0) return false;

            switch (dlg.SelectedId)
            {
                case ((int)TraktMenuItems.Rate):
                    TraktLogger.Info("Rating show '{0}'", title);
                    GUIUtils.ShowRateDialog<TraktRateSeries>(TraktHandlers.BasicHandler.CreateShowRateData(title, tvdbid));
                    break;

                case ((int)TraktMenuItems.Shouts):
                    TraktLogger.Info("Searching Shouts for show '{0}'", title);
                    TraktHelper.ShowTVShowShouts(tvdbid, title, fanart);
                    break;

                case ((int)TraktMenuItems.Related):
                    TraktLogger.Info("Show Related Shows for '{0}'", title);
                    TraktHelper.ShowRelatedShows(tvdbid, title);
                    break;

                case ((int)TraktMenuItems.AddToWatchList):
                    TraktLogger.Info("Adding show '{0}' to Watch List", title);
                    TraktHelper.AddShowToWatchList(title, null, tvdbid);
                    break;

                case ((int)TraktMenuItems.AddToCustomList):
                    TraktLogger.Info("Adding show '{0}' to Custom List", title);
                    TraktHelper.AddRemoveShowInUserList(title, null, tvdbid, false);
                    break;

                case ((int)TraktMenuItems.SearchBy):
                    ShowSearchByMenu(people, title, fanart);
                    break;

                case ((int)TraktMenuItems.UserProfile):
                    GUIWindowManager.ActivateWindow((int)TraktGUIWindows.UserProfile);
                    break;

                case ((int)TraktMenuItems.Network):
                    GUIWindowManager.ActivateWindow((int)TraktGUIWindows.Network);
                    break;

                case ((int)TraktMenuItems.Calendar):
                    GUIWindowManager.ActivateWindow((int)TraktGUIWindows.Calendar);
                    break;

                case ((int)TraktMenuItems.Recommendations):
                    GUIWindowManager.ActivateWindow((int)TraktGUIWindows.RecommendationsShows);
                    break;

                case ((int)TraktMenuItems.Trending):
                    GUIWindowManager.ActivateWindow((int)TraktGUIWindows.TrendingShows);
                    break;

                case ((int)TraktMenuItems.WatchList):
                    GUIWindowManager.ActivateWindow((int)TraktGUIWindows.WatchedListShows);
                    break;

                case ((int)TraktMenuItems.Lists):
                    GUIWindowManager.ActivateWindow((int)TraktGUIWindows.Lists);
                    break;
            }
            return true;
        }
        #endregion

        #region Episodes
        public static bool ShowTraktExtEpisodeMenu(string title, string year, string season, string episode, string tvdbid, string fanart)
        {
            return ShowTraktExtEpisodeMenu(title, year, season, episode, tvdbid, fanart, false);
        }
        public static bool ShowTraktExtEpisodeMenu(string title, string year, string season, string episode, string tvdbid, string fanart, bool showAll)
        {
            return ShowTraktExtEpisodeMenu(title, year, season, episode, tvdbid, fanart, null, showAll);
        }
        public static bool ShowTraktExtEpisodeMenu(string title, string year, string season, string episode, string tvdbid, string fanart, SearchPeople people, bool showAll)
        {
            var dlg = (IDialogbox)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
            dlg.Reset();
            dlg.SetHeading(GUIUtils.PluginName());

            var pItem = new GUIListItem(Translation.Shouts);
            dlg.Add(pItem);
            pItem.ItemId = (int)TraktMenuItems.Shouts;

            pItem = new GUIListItem(Translation.Rate);
            dlg.Add(pItem);
            pItem.ItemId = (int)TraktMenuItems.Rate;

            pItem = new GUIListItem(Translation.AddToWatchList);
            dlg.Add(pItem);
            pItem.ItemId = (int)TraktMenuItems.AddToWatchList;

            pItem = new GUIListItem(Translation.AddToList);
            dlg.Add(pItem);
            pItem.ItemId = (int)TraktMenuItems.AddToCustomList;

            // Show Search By menu...
            if (people != null && people.Count != 0)
            {
                pItem = new GUIListItem(Translation.SearchBy + "...");
                dlg.Add(pItem);
                pItem.ItemId = (int)TraktMenuItems.SearchBy;
            }

            // also show non-context sensitive items related to episodes
            if (showAll)
            {
                // might want to check your recently watched, stats etc
                pItem = new GUIListItem(Translation.UserProfile);
                dlg.Add(pItem);
                pItem.ItemId = (int)TraktMenuItems.UserProfile;

                pItem = new GUIListItem(Translation.Network);
                dlg.Add(pItem);
                pItem.ItemId = (int)TraktMenuItems.Network;

                pItem = new GUIListItem(Translation.Calendar);
                dlg.Add(pItem);
                pItem.ItemId = (int)TraktMenuItems.Calendar;

                pItem = new GUIListItem(Translation.WatchList);
                dlg.Add(pItem);
                pItem.ItemId = (int)TraktMenuItems.WatchList;

                pItem = new GUIListItem(Translation.Lists);
                dlg.Add(pItem);
                pItem.ItemId = (int)TraktMenuItems.Lists;
            }

            // Show Context Menu
            dlg.DoModal(GUIWindowManager.ActiveWindow);
            if (dlg.SelectedId < 0) return false;

            switch (dlg.SelectedId)
            {
                case ((int)TraktMenuItems.Rate):
                    TraktLogger.Info("Rating episode '{0} - {1}x{2}'", title, season, episode);
                    GUIUtils.ShowRateDialog<TraktRateEpisode>(TraktHandlers.BasicHandler.CreateEpisodeRateData(title, tvdbid, season, episode));
                    break;

                case ((int)TraktMenuItems.Shouts):
                    TraktLogger.Info("Searching Shouts for episode '{0} - {1}x{2}'", title, season, episode);
                    TraktHelper.ShowEpisodeShouts(tvdbid, title, season, episode, fanart);
                    break;

                case ((int)TraktMenuItems.AddToWatchList):
                    TraktLogger.Info("Adding episode '{0} - {1}x{2}' to Watch List", title, season, episode);
                    TraktHelper.AddEpisodeToWatchList(title, year, tvdbid, season, episode);
                    break;

                case ((int)TraktMenuItems.AddToCustomList):
                    TraktLogger.Info("Adding episode '{0} - {1}x{2}' to Custom List", title, season, episode);
                    TraktHelper.AddRemoveEpisodeInUserList(title, year, season, episode, tvdbid, false);
                    break;

                case ((int)TraktMenuItems.SearchBy):
                    ShowSearchByMenu(people, title, fanart);
                    break;

                case ((int)TraktMenuItems.UserProfile):
                    GUIWindowManager.ActivateWindow((int)TraktGUIWindows.UserProfile);
                    break;

                case ((int)TraktMenuItems.Calendar):
                    GUIWindowManager.ActivateWindow((int)TraktGUIWindows.Calendar);
                    break;

                case ((int)TraktMenuItems.Network):
                    GUIWindowManager.ActivateWindow((int)TraktGUIWindows.Network);
                    break;

                case ((int)TraktMenuItems.WatchList):
                    GUIWindowManager.ActivateWindow((int)TraktGUIWindows.WatchedListEpisodes);
                    break;

                case ((int)TraktMenuItems.Lists):
                    GUIWindowManager.ActivateWindow((int)TraktGUIWindows.Lists);
                    break;
            }
            return true;
        }
        #endregion

        #endregion

        #endregion

        #region Filters
        static List<MultiSelectionItem> GetFilterListItems(Dictionary<Filters, bool> filters)
        {
            List<MultiSelectionItem> selectItems = new List<MultiSelectionItem>();

            foreach (var filter in filters)
            {
                MultiSelectionItem multiSelectItem = new MultiSelectionItem
                {
                    ItemID = filter.Key.ToString(),
                    ItemTitle = Translation.GetByName(string.Format("Hide{0}", filter.Key)),
                    ItemTitle2 = filter.Value ? Translation.On : Translation.Off,
                    IsToggle = true,
                    Selected = false
                };
                selectItems.Add(multiSelectItem);
            }

            return selectItems;
        }

        internal static bool ShowMovieFiltersMenu()
        {
            Dictionary<Filters, bool> filters = new Dictionary<Filters, bool>();

            filters.Add(Filters.Watched, TraktSettings.TrendingMoviesHideWatched);
            filters.Add(Filters.Watchlisted, TraktSettings.TrendingMoviesHideWatchlisted);
            filters.Add(Filters.Collected, TraktSettings.TrendingMoviesHideCollected);
            filters.Add(Filters.Rated, TraktSettings.TrendingMoviesHideRated);

            var selectedItems = GUIUtils.ShowMultiSelectionDialog(Translation.Filters, GetFilterListItems(filters));
            if (selectedItems == null) return false;

            foreach (var item in selectedItems.Where(l => l.Selected == true))
            {
                // toggle state of all selected items
                switch ((Filters)Enum.Parse(typeof(Filters), item.ItemID, true))
                {
                    case Filters.Watched:
                        TraktSettings.TrendingMoviesHideWatched = !TraktSettings.TrendingMoviesHideWatched;
                        break;
                    case Filters.Watchlisted:
                        TraktSettings.TrendingMoviesHideWatchlisted = !TraktSettings.TrendingMoviesHideWatchlisted;
                        break;
                    case Filters.Collected:
                        TraktSettings.TrendingMoviesHideCollected = !TraktSettings.TrendingMoviesHideCollected;
                        break;
                    case Filters.Rated:
                        TraktSettings.TrendingMoviesHideRated = !TraktSettings.TrendingMoviesHideRated;
                        break;
                }
            }

            return true;
        }

        internal static bool ShowTVShowFiltersMenu()
        {
            Dictionary<Filters, bool> filters = new Dictionary<Filters, bool>();

            filters.Add(Filters.Watched, TraktSettings.TrendingShowsHideWatched);
            filters.Add(Filters.Watchlisted, TraktSettings.TrendingShowsHideWatchlisted);
            filters.Add(Filters.Collected, TraktSettings.TrendingShowsHideCollected);
            filters.Add(Filters.Rated, TraktSettings.TrendingShowsHideRated);

            var selectedItems = GUIUtils.ShowMultiSelectionDialog(Translation.Filters, GetFilterListItems(filters));
            if (selectedItems == null) return false;

            foreach (var item in selectedItems.Where(l => l.Selected == true))
            {
                // toggle state of all selected items
                switch ((Filters)Enum.Parse(typeof(Filters), item.ItemID, true))
                {
                    case Filters.Watched:
                        TraktSettings.TrendingShowsHideWatched = !TraktSettings.TrendingShowsHideWatched;
                        break;
                    case Filters.Watchlisted:
                        TraktSettings.TrendingShowsHideWatchlisted = !TraktSettings.TrendingShowsHideWatchlisted;
                        break;
                    case Filters.Collected:
                        TraktSettings.TrendingShowsHideCollected = !TraktSettings.TrendingShowsHideCollected;
                        break;
                    case Filters.Rated:
                        TraktSettings.TrendingShowsHideRated = !TraktSettings.TrendingShowsHideRated;
                        break;
                }
            }

            return true;
        }

        internal static IEnumerable<TraktTrendingMovie> FilterTrendingMovies(IEnumerable<TraktTrendingMovie> moviesToFilter)
        {
            if (TraktSettings.TrendingMoviesHideWatched)
                moviesToFilter = moviesToFilter.Where(m => !m.Watched);

            if (TraktSettings.TrendingMoviesHideWatchlisted)
                moviesToFilter = moviesToFilter.Where(m => !m.InWatchList);

            if (TraktSettings.TrendingMoviesHideCollected)
                moviesToFilter = moviesToFilter.Where(m => !m.InCollection);

            if (TraktSettings.TrendingMoviesHideRated)
                moviesToFilter = moviesToFilter.Where(m => m.RatingAdvanced == 0 || m.Rating == "false");

            return moviesToFilter;
        }

        internal static IEnumerable<TraktTrendingShow> FilterTrendingShows(IEnumerable<TraktTrendingShow> showsToFilter)
        {
            if (TraktSettings.TrendingShowsHideWatched)
                showsToFilter = showsToFilter.Where(s => !s.Watched);

            if (TraktSettings.TrendingShowsHideWatchlisted)
                showsToFilter = showsToFilter.Where(s => !s.InWatchList);

            if (TraktSettings.TrendingShowsHideCollected)
                showsToFilter = showsToFilter.Where(s => !TraktSettings.ShowsInCollection.Contains(s.Tvdb));

            if (TraktSettings.TrendingShowsHideRated)
                showsToFilter = showsToFilter.Where(s => s.Rating == null || s.Rating == "false");

            return showsToFilter;
        }
        #endregion

        #region Activity Helpers
        internal static string GetActivityListItemTitle(TraktActivity.Activity activity)
        {
            if (activity == null) return string.Empty;

            string itemName = GetActivityItemName(activity);
            string userName = activity.User.Username;
            string title = string.Empty;

            if (string.IsNullOrEmpty(activity.Action) || string.IsNullOrEmpty(activity.Type))
                return string.Empty;

            ActivityAction action = (ActivityAction)Enum.Parse(typeof(ActivityAction), activity.Action);
            ActivityType type = (ActivityType)Enum.Parse(typeof(ActivityType), activity.Type);

            switch (action)
            {
                case ActivityAction.watching:
                    title = string.Format(Translation.ActivityWatching, userName, itemName);
                    break;

                case ActivityAction.scrobble:
                    title = string.Format(Translation.ActivityWatched, userName, itemName);
                    break;

                case ActivityAction.checkin:
                    title = string.Format(Translation.ActivityCheckedIn, userName, itemName);
                    break;

                case ActivityAction.seen:
                    if (type == ActivityType.episode && activity.Episodes.Count > 1)
                    {
                        title = string.Format(Translation.ActivitySeenEpisodes, userName, activity.Episodes.Count, itemName);
                    }
                    else
                    {
                        title = string.Format(Translation.ActivitySeen, userName, itemName);
                    }
                    break;

                case ActivityAction.collection:
                    if (type == ActivityType.episode && activity.Episodes.Count > 1)
                    {
                        title = string.Format(Translation.ActivityCollectedEpisodes, userName, activity.Episodes.Count, itemName);
                    }
                    else
                    {
                        title = string.Format(Translation.ActivityCollected, userName, itemName);
                    }
                    break;

                case ActivityAction.rating:
                    if (activity.UseRatingAdvanced)
                    {
                        title = string.Format(Translation.ActivityRatingAdvanced, userName, itemName, activity.RatingAdvanced);
                    }
                    else
                    {
                        title = string.Format(Translation.ActivityRating, userName, itemName);
                    }
                    break;

                case ActivityAction.watchlist:
                    title = string.Format(Translation.ActivityWatchlist, userName, itemName);
                    break;

                case ActivityAction.review:
                    title = string.Format(Translation.ActivityReview, userName, itemName);
                    break;

                case ActivityAction.shout:
                    title = string.Format(Translation.ActivityShouts, userName, itemName);
                    break;

                case ActivityAction.created: // created list
                    title = string.Format(Translation.ActivityCreatedList, userName, itemName);
                    break;

                case ActivityAction.item_added: // added item to list
                    title = string.Format(Translation.ActivityAddToList, userName, itemName, activity.List.Name);
                    break;
            }

            return title;
        }

        internal static string GetActivityItemName(TraktActivity.Activity activity)
        {
            string name = string.Empty;

            try
            {
                ActivityType type = (ActivityType)Enum.Parse(typeof(ActivityType), activity.Type);
                ActivityAction action = (ActivityAction)Enum.Parse(typeof(ActivityAction), activity.Action);

                switch (type)
                {
                    case ActivityType.episode:
                        if (action == ActivityAction.seen || action == ActivityAction.collection)
                        {
                            if (activity.Episodes.Count > 1)
                            {
                                // just return show name
                                name = activity.Show.Title;
                            }
                            else
                            {
                                //  get the first and only item in collection of episodes
                                string episodeIndex = activity.Episodes.First().Number.ToString();
                                string seasonIndex = activity.Episodes.First().Season.ToString();
                                string episodeName = activity.Episodes.First().Title;

                                if (string.IsNullOrEmpty(episodeName))
                                    episodeName = string.Format("{0} {1}", Translation.Episode, episodeIndex);

                                name = string.Format("{0} - {1}x{2} - {3}", activity.Show.Title, seasonIndex, episodeIndex, episodeName);
                            }
                        }
                        else
                        {
                            string episodeName = activity.Episode.Title;

                            if (string.IsNullOrEmpty(episodeName))
                                episodeName = string.Format("{0} {1}", Translation.Episode, activity.Episode.Number.ToString());

                            name = string.Format("{0} - {1}x{2} - {3}", activity.Show.Title, activity.Episode.Season.ToString(), activity.Episode.Number.ToString(), episodeName);
                        }
                        break;

                    case ActivityType.show:
                        name = activity.Show.Title;
                        break;

                    case ActivityType.movie:
                        name = string.Format("{0} ({1})", activity.Movie.Title, activity.Movie.Year);
                        break;

                    case ActivityType.list:
                        if (action == ActivityAction.item_added)
                        {
                            // return the name of the item added to the list
                            switch (activity.ListItem.Type)
                            {
                                case "show":
                                    name = activity.ListItem.Show.Title;
                                    break;

                                case "episode":
                                    string episodeIndex = activity.ListItem.Episode.Number.ToString();
                                    string seasonIndex = activity.ListItem.Episode.Season.ToString();
                                    string episodeName = activity.ListItem.Episode.Title;

                                    if (string.IsNullOrEmpty(episodeName))
                                        episodeName = string.Format("{0} {1}", Translation.Episode, episodeIndex);

                                    name = string.Format("{0} - {1}x{2} - {3}", activity.ListItem.Show.Title, seasonIndex, episodeIndex, episodeName);
                                    break;

                                case "movie":
                                    name = string.Format("{0} ({1})", activity.ListItem.Movie.Title, activity.ListItem.Movie.Year);
                                    break;
                            }
                        }
                        else if (action == ActivityAction.created)
                        {
                            // return the list name
                            name = activity.List.Name;
                        }
                        break;
                }
            }
            catch
            {
                // most likely trakt returned a null object
                name = string.Empty;
            }

            return name;
        }
        #endregion
    }

    /// <summary>
    /// Used to collect a list of people to SearchBy in External Plugins
    /// </summary>
    public class SearchPeople
    {
        public List<string> Actors = new List<string>();
        public List<string> Directors = new List<string>();
        public List<string> Producers = new List<string>();
        public List<string> Writers = new List<string>();
        public List<string> GuestStars = new List<string>();
        
        public int Count
        {
            get
            {
                int peopleCount = 0;
                peopleCount += Actors.Count();
                peopleCount += Directors.Count();
                peopleCount += Producers.Count();
                peopleCount += Writers.Count();
                peopleCount += GuestStars.Count();
                return peopleCount;
            }
        }
    }
}
