using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using MediaPortal.Dialogs;
using MediaPortal.GUI.Library;
using MediaPortal.GUI.Video;
using MediaPortal.Video.Database;
using Trailers.Providers;
using TraktPlugin.Extensions;
using TraktPlugin.TraktAPI.DataStructures;
using TraktPlugin.TraktAPI.Enums;
using TraktPlugin.TraktAPI.Extensions;

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

    enum ActivityContextMenuItem
    {
        ChangeView,
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
        MyVideosDb = 25,
        MyVideosShares = 6,
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
            if (TraktSettings.AccountStatus != ConnectionState.Connected)
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
        internal static void CheckAndPlayMovie(bool jumpTo, TraktMovieSummary movie)
        {
            if (movie == null) return;

            TraktLogger.Info("Attempting to play movie. Title = '{0}', Year = '{1}', IMDb ID = '{2}'", movie.Title, movie.Year.ToLogString(), movie.Ids.Imdb.ToLogString());
            bool handled = false;

            if (TraktHelper.IsMovingPicturesAvailableAndEnabled)
            {
                TraktLogger.Info("Checking if any movie to watch in MovingPictures");
                int? movieid = null;

                // Find Movie ID in MovingPictures
                // Movie List is now cached internally in MovingPictures so it will be fast
                bool movieExists = TraktHandlers.MovingPictures.FindMovieID(movie.Title, movie.Year.GetValueOrDefault(), movie.Ids.Imdb, ref movieid);

                if (movieExists)
                {
                    TraktLogger.Info("Found movie in MovingPictures with movie ID '{0}'", movieid.ToString());
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
                if (TraktHandlers.MyVideos.FindMovieID(movie.Title, movie.Year.GetValueOrDefault(), movie.Ids.Imdb, ref imdbMovie))
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
                if (TraktHandlers.MyFilmsHandler.FindMovie(movie.Title, movie.Year.GetValueOrDefault(), movie.Ids.Imdb, ref movieid, ref config))
                {
                    // Open My Films Details view so user can play movie
                    if (jumpTo)
                    {
                        string loadingParameter = string.Format("config:{0}|movieid:{1}", config, movieid);
                        GUIWindowManager.ActivateWindow((int)ExternalPluginWindows.MyFilms, loadingParameter);
                    }
                    else
                    {
                        // TraktHandlers.MyFilms.PlayMovie(config, movieid); // TODO: Add Player Class to MyFilms
                        string loadingParameter = string.Format("config:{0}|movieid:{1}|play:{2}", config, movieid, "true");
                        GUIWindowManager.ActivateWindow((int)ExternalPluginWindows.MyFilms, loadingParameter);
                    }
                    handled = true;
                }
            }

            if (TraktHelper.IsTrailersAvailableAndEnabled && handled == false)
            {
                TraktLogger.Info("There were no movies found in local plugin databases. Attempting to search and/or play trailer(s) from the Trailers plugin");
                ShowMovieTrailersPluginMenu(movie);
                handled = true;
            }            
        }
        #endregion

        #region PlayEpisode
        internal static void CheckAndPlayEpisode(TraktShowSummary show, TraktEpisodeSummary episode)
        {
            if (show == null || episode == null) return;
        
            bool handled = false;

            // check if plugin is installed and enabled
            if (TraktHelper.IsMPTVSeriesAvailableAndEnabled)
            {
                // Play episode if it exists
                handled = TraktHandlers.TVSeries.PlayEpisode(show.Ids.Tvdb.GetValueOrDefault(), episode.Season, episode.Number);
            }

            if (TraktHelper.IsTrailersAvailableAndEnabled && handled == false)
            {
                TraktLogger.Info("There were no episodes found in local plugin databases. Attempting to search and/or play trailer(s) from the Trailers plugin");
                ShowTVEpisodeTrailersPluginMenu(show, episode);
                handled = true;
            }
        }
        
        internal static void CheckAndPlayFirstUnwatchedEpisode(TraktShowSummary show, bool jumpTo)
        {
            if (show == null) return;

            TraktLogger.Info("Attempting to play episodes for tv show. TVDb ID = '{0}', IMDb ID = '{1}'", show.Ids.Tvdb.ToLogString(), show.Ids.Imdb.ToLogString());
            bool handled = false;

            // check if plugin is installed and enabled
            if (TraktHelper.IsMPTVSeriesAvailableAndEnabled)
            {
                if (jumpTo)
                {
                    TraktLogger.Info("Looking for tv shows in MP-TVSeries database");
                    if (TraktHandlers.TVSeries.SeriesExists(show.Ids.Tvdb.GetValueOrDefault()))
                    {
                        string loadingParameter = string.Format("seriesid:{0}", show.Ids.Tvdb.GetValueOrDefault());
                        GUIWindowManager.ActivateWindow((int)ExternalPluginWindows.TVSeries, loadingParameter);
                        handled = true;
                    }
                }
                else
                {
                    // Play episode if it exists
                    TraktLogger.Info("Checking if any episodes to watch in MP-TVSeries");
                    handled = TraktHandlers.TVSeries.PlayFirstUnwatchedEpisode(show.Ids.Tvdb.GetValueOrDefault());
                }
            }

            if (TraktHelper.IsTrailersAvailableAndEnabled && handled == false)
            {
                TraktLogger.Info("There were no episodes found in local plugin databases. Attempting to search and/or play trailer(s) from the Trailers plugin");
                ShowTVShowTrailersPluginMenu(show);
                handled = true;
            }
        }
        #endregion

        #region Rate Movie

        internal static bool RateMovie(TraktMovieSummary movie)
        {
            var rateObject = new TraktSyncMovieRated
            {
                Ids = new TraktMovieId
                { 
                    Trakt = movie.Ids.Trakt,
                    Imdb = movie.Ids.Imdb.ToNullIfEmpty(),
                    Tmdb = movie.Ids.Tmdb
                },
                Title = movie.Title,
                Year = movie.Year,
                RatedAt = DateTime.UtcNow.ToISO8601()
            };

            int? prevRating = movie.UserRating();
            int newRating = 0;

            newRating = GUIUtils.ShowRateDialog<TraktSyncMovieRated>(rateObject);
            if (newRating == -1) return false;

            // If previous rating not equal to current rating then 
            // update skin properties to reflect changes
            if (prevRating == newRating)
                return false;

            if (prevRating == null || prevRating == 0)
            {
                // add to ratings
                TraktCache.AddMovieToRatings(movie, newRating);
                movie.Votes++;
            }
            else if (newRating == 0)
            {
                // remove from ratings
                TraktCache.RemoveMovieFromRatings(movie);
                movie.Votes--;
            }
            else
            {
                // rating changed, remove then add
                TraktCache.RemoveMovieFromRatings(movie);
                TraktCache.AddMovieToRatings(movie, newRating);
            }

            // update ratings until next online update
            // if we have the ratings distribution we could calculate correctly
            if (movie.Votes == 0)
            {
                movie.Rating = 0;
            }
            else if (movie.Votes == 1 && newRating > 0)
            {
                movie.Rating = newRating;
            }

            return true;
        }

        #endregion

        #region Rate Show

        internal static bool RateShow(TraktShowSummary show)
        {
            var rateObject = new TraktSyncShowRated
            {
                Ids = new TraktShowId
                {
                    Trakt = show.Ids.Trakt,
                    Imdb = show.Ids.Imdb.ToNullIfEmpty(),
                    Tmdb = show.Ids.Tmdb,
                    TvRage = show.Ids.TvRage,
                    Tvdb = show.Ids.Tvdb
                },
                Title = show.Title,
                Year = show.Year,
                RatedAt = DateTime.UtcNow.ToISO8601()
            };

            int? prevRating = show.UserRating();
            int newRating = 0;

            newRating = GUIUtils.ShowRateDialog<TraktSyncShowRated>(rateObject);
            if (newRating == -1) return false;

            // If previous rating not equal to current rating then 
            // update skin properties to reflect changes
            if (prevRating == newRating)
                return false;

            if (prevRating == null || prevRating == 0)
            {
                // add to ratings
                TraktCache.AddShowToRatings(show, newRating);
                show.Votes++;
            }
            else if (newRating == 0)
            {
                // remove from ratings
                TraktCache.RemoveShowFromRatings(show);
                show.Votes--;
            }
            else
            {
                // rating changed, remove then add
                TraktCache.RemoveShowFromRatings(show);
                TraktCache.AddShowToRatings(show, newRating);
            }

            // update ratings until next online update
            // if we have the ratings distribution we could calculate correctly
            if (show.Votes == 0)
            {
                show.Rating = 0;
            }
            else if (show.Votes == 1 && newRating > 0)
            {
                show.Rating = newRating;
            }

            return true;
        }

        #endregion

        #region Rate Episode

        internal static bool RateEpisode(TraktShowSummary show, TraktEpisodeSummary episode)
        {
            var rateObject = new TraktSyncEpisodeRated
            {
                Ids = new TraktEpisodeId
                {
                    Trakt = episode.Ids.Trakt,
                    Imdb = episode.Ids.Imdb.ToNullIfEmpty(),
                    Tmdb = episode.Ids.Tmdb,
                    Tvdb = episode.Ids.Tvdb,
                    TvRage = episode.Ids.TvRage
                },
                Title = episode.Title,
                Season = episode.Season,
                Number = episode.Number,
                RatedAt = DateTime.UtcNow.ToISO8601()
            };

            int? prevRating = episode.UserRating(show);
            int newRating = 0;

            newRating = GUIUtils.ShowRateDialog<TraktSyncEpisodeRated>(rateObject);
            if (newRating == -1) return false;

            // If previous rating not equal to current rating then 
            // update skin properties to reflect changes
            if (prevRating == newRating)
                return false;

            if (prevRating == null || prevRating == 0)
            {
                // add to ratings
                TraktCache.AddEpisodeToRatings(show, episode, newRating);
                episode.Votes++;
            }
            else if (newRating == 0)
            {
                // remove from ratings
                TraktCache.RemoveEpisodeFromRatings(episode);
                episode.Votes--;
            }
            else
            {
                // rating changed, remove then add
                TraktCache.RemoveEpisodeFromRatings(episode);
                TraktCache.AddEpisodeToRatings(show, episode, newRating);
            }

            // update ratings until next online update
            // if we have the ratings distribution we could calculate correctly
            if (episode.Votes == 0)
            {
                episode.Rating = 0;
            }
            else if (episode.Votes == 1 && newRating > 0)
            {
                episode.Rating = newRating;
            }

            return true;
        }

        #endregion

        #region Mark all Episodes in Show as Watched

        public static void MarkShowAsWatched(TraktShow show)
        {
            if (!GUICommon.CheckLogin(false)) return;

            var seenThread = new Thread(obj =>
            {
                var objShow = obj as TraktShow;

                var syncData = new TraktShow
                {
                    Ids = new TraktShowId
                    {
                        Trakt = objShow.Ids.Trakt,
                        Imdb = objShow.Ids.Imdb.ToNullIfEmpty(),
                        Tmdb = objShow.Ids.Tmdb,
                        Tvdb = objShow.Ids.Tvdb,
                        TvRage = objShow.Ids.TvRage
                    },
                    Title = show.Title,
                    Year = show.Year
                };

                TraktLogger.Info("Adding all episodes from show to trakt.tv watched history. Title = '{0}', Year = '{1}', IMDb ID = '{2}', TVDb ID = '{3}', TMDb ID = '{4}'", 
                                    show.Title, show.Year.ToLogString(), show.Ids.Imdb.ToLogString(), show.Ids.Tvdb.ToLogString(), show.Ids.Tmdb.ToLogString());

                var response = TraktAPI.TraktAPI.AddShowToWatchedHistory(syncData);
                TraktLogger.LogTraktResponse(response);
            })
            {
                IsBackground = true,
                Name = "MarkWatched"
            };

            seenThread.Start(show);
        }

        #endregion

        #region Mark all Episodes in a Shows Season as Watched

        public static void MarkSeasonAsWatched(TraktShow show, int season)
        {
            if (!GUICommon.CheckLogin(false)) return;

            var seenThread = new Thread(obj =>
            {
                var objShow = obj as TraktShow;

                var syncData = new TraktSyncShowEx
                {
                    Ids = new TraktShowId
                    {
                        Trakt = objShow.Ids.Trakt,
                        Imdb = objShow.Ids.Imdb.ToNullIfEmpty(),
                        Tmdb = objShow.Ids.Tmdb,
                        Tvdb = objShow.Ids.Tvdb,
                        TvRage = objShow.Ids.TvRage
                    },
                    Title = show.Title,
                    Year = show.Year,
                    Seasons = new List<TraktSyncShowEx.Season>()
                };

                var seasonObj = new TraktSyncShowEx.Season
                {
                    Number = season
                };
                syncData.Seasons.Add(seasonObj);

                TraktLogger.Info("Adding all episodes in season from show to trakt.tv watched history. Title = '{0}', Year = '{1}', IMDb ID = '{2}', TVDb ID = '{3}', TMDb ID = '{4}', Season = '{5}'", 
                                    show.Title, show.Year.ToLogString(), show.Ids.Imdb.ToLogString(), show.Ids.Tvdb.ToLogString(), show.Ids.Tmdb.ToLogString(), season);

                var response = TraktAPI.TraktAPI.AddShowToWatchedHistoryEx(syncData);
                TraktLogger.LogTraktResponse(response);
            })
            {
                IsBackground = true,
                Name = "MarkWatched"
            };

            seenThread.Start(show);
        }

        #endregion

        #region Add Show to Library

        public static void AddShowToCollection(TraktShow show)
        {
            if (!GUICommon.CheckLogin(false)) return;

            var collectionThread = new Thread(obj =>
            {
                var objShow = obj as TraktShow;

                var syncData = new TraktShow
                {
                    Ids = new TraktShowId
                    {
                        Trakt = objShow.Ids.Trakt,
                        Imdb = objShow.Ids.Imdb.ToNullIfEmpty(),
                        Tmdb = objShow.Ids.Tmdb,
                        Tvdb = objShow.Ids.Tvdb,
                        TvRage = objShow.Ids.TvRage
                    },
                    Title = show.Title,
                    Year = show.Year
                };

                TraktLogger.Info("Adding all episodes from show to trakt.tv collection. Title = '{0}', Year = '{1}', IMDb ID = '{2}', TVDb ID = '{3}', TMDb ID = '{4}'",
                                    show.Title, show.Year.ToLogString(), show.Ids.Imdb.ToLogString(), show.Ids.Tvdb.ToLogString(), show.Ids.Tmdb.ToLogString());

                var response = TraktAPI.TraktAPI.AddShowToCollection(syncData);
                TraktLogger.LogTraktResponse(response);
            })
            {
                IsBackground = true,
                Name = "AddCollection"
            };

            collectionThread.Start(show);
        }

        #endregion

        #region Add Season to Library

        public static void AddSeasonToLibrary(TraktShow show, int season)
        {
            if (!GUICommon.CheckLogin(false)) return;

            var seenThread = new Thread(obj =>
            {
                var objShow = obj as TraktShow;

                var syncData = new TraktSyncShowEx
                {
                    Ids = new TraktShowId
                    {
                        Trakt = objShow.Ids.Trakt,
                        Imdb = objShow.Ids.Imdb.ToNullIfEmpty(),
                        Tmdb = objShow.Ids.Tmdb,
                        Tvdb = objShow.Ids.Tvdb,
                        TvRage = objShow.Ids.TvRage
                    },
                    Title = show.Title,
                    Year = show.Year,
                    Seasons = new List<TraktSyncShowEx.Season>()
                };

                var seasonObj = new TraktSyncShowEx.Season
                {
                    Number = season
                };
                syncData.Seasons.Add(seasonObj);

                TraktLogger.Info("Adding all episodes in season from show to trakt.tv collection. Title = '{0}', Year = '{1}', IMDb ID = '{2}', TVDb ID = '{3}', TMDb ID = '{4}', Season = '{5}'",
                                    show.Title, show.Year.ToLogString(), show.Ids.Imdb.ToLogString(), show.Ids.Tvdb.ToLogString(), show.Ids.Tmdb.ToLogString(), season);

                var response = TraktAPI.TraktAPI.AddShowToCollectionEx(syncData);
                TraktLogger.LogTraktResponse(response);
            })
            {
                IsBackground = true,
                Name = "AddCollection"
            };

            seenThread.Start(show);
        }

        #endregion

        #region Common Skin Properties

        internal static string GetProperty(string property)
        {
            string propertyVal = GUIPropertyManager.GetProperty(property);
            return propertyVal ?? string.Empty;
        }

        internal static void SetProperty(string property, string value)
        {
            string propertyValue = string.IsNullOrEmpty(value) ? "--" : value;
            GUIUtils.SetProperty(property, propertyValue);
        }

        internal static void SetProperty(string property, List<string> value)
        {
            string propertyValue = value == null ? "--" : string.Join(", ", value.ToArray());
            GUIUtils.SetProperty(property, propertyValue);
        }

        internal static void SetProperty(string property, int? value)
        {
            string propertyValue = value == null ? "--" : value.ToString();
            GUIUtils.SetProperty(property, propertyValue);
        }

        internal static void SetProperty(string property, bool value)
        {
            GUIUtils.SetProperty(property, value.ToString().ToLower());
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
        
        internal static void SetUserProperties(TraktUserSummary user)
        {
            SetProperty("#Trakt.User.Username", user.Username);
            SetProperty("#Trakt.User.Protected", user.IsPrivate.ToString().ToLower());
            SetProperty("#Trakt.User.VIP", user.IsVip.ToString().ToLower());
            SetProperty("#Trakt.User.About", user.About.RemapHighOrderChars());
            SetProperty("#Trakt.User.Age", user.Age.ToString());
            if (user.Images != null)
            {
                SetProperty("#Trakt.User.Avatar", user.Images.Avatar.FullSize);
                SetProperty("#Trakt.User.AvatarFileName", user.Images.Avatar.LocalImageFilename(ArtworkType.Avatar));
            }
            SetProperty("#Trakt.User.FullName", user.FullName);
            SetProperty("#Trakt.User.Gender", string.IsNullOrEmpty(user.Gender) ? null : Translation.GetByName(string.Format("Gender{0}", user.Gender)));
            SetProperty("#Trakt.User.JoinDate", user.JoinedAt.FromISO8601().ToLongDateString());
            SetProperty("#Trakt.User.Location", user.Location);            
            SetProperty("#Trakt.User.Url", string.Format("http://trakt.tv/users/{0}", user.Username));
        }

        internal static void ClearListProperties()
        {
            GUIUtils.SetProperty("#Trakt.List.Name", string.Empty);
            GUIUtils.SetProperty("#Trakt.List.Description", string.Empty);
            GUIUtils.SetProperty("#Trakt.List.Privacy", string.Empty);
            GUIUtils.SetProperty("#Trakt.List.Slug", string.Empty);
            GUIUtils.SetProperty("#Trakt.List.Url", string.Empty);
            GUIUtils.SetProperty("#Trakt.List.AllowShouts", string.Empty);
            GUIUtils.SetProperty("#Trakt.List.ShowNumbers", string.Empty);
            GUIUtils.SetProperty("#Trakt.List.UpdatedAt", string.Empty);
            GUIUtils.SetProperty("#Trakt.List.ItemCount", string.Empty);
            GUIUtils.SetProperty("#Trakt.List.Likes", string.Empty);
            GUIUtils.SetProperty("#Trakt.List.Id", string.Empty);
            GUIUtils.SetProperty("#Trakt.List.Slug", string.Empty);
        }

        internal static void SetListProperties(TraktListDetail list, string username)
        {
            SetProperty("#Trakt.List.Name", list.Name);
            SetProperty("#Trakt.List.Description", list.Description);
            SetProperty("#Trakt.List.Privacy", list.Privacy);
            SetProperty("#Trakt.List.Slug", list.Ids.Slug);
            SetProperty("#Trakt.List.Url", string.Format("http://trakt.tv/users/{0}/lists/{1}", username, list.Ids.Trakt));
            SetProperty("#Trakt.List.AllowShouts", list.AllowComments);
            SetProperty("#Trakt.List.ShowNumbers", list.DisplayNumbers);
            SetProperty("#Trakt.List.UpdatedAt", list.UpdatedAt.FromISO8601().ToShortDateString());
            SetProperty("#Trakt.List.ItemCount", list.ItemCount);
            SetProperty("#Trakt.List.Likes", list.Likes);
            SetProperty("#Trakt.List.Id", list.Ids.Trakt);
            SetProperty("#Trakt.List.Slug", list.Ids.Slug);
        }

        internal static void ClearStatisticProperties()
        {
            #region Friends Statistics
            GUIUtils.SetProperty("#Trakt.Statistics.Friends", string.Empty);
            GUIUtils.SetProperty("#Trakt.Statistics.Followers", string.Empty);
            GUIUtils.SetProperty("#Trakt.Statistics.Following", string.Empty);
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
            GUIUtils.SetProperty("#Trakt.Statistics.Episodes.Ratings", string.Empty);
            GUIUtils.SetProperty("#Trakt.Statistics.Episodes.Hated", string.Empty);
            GUIUtils.SetProperty("#Trakt.Statistics.Episodes.Loved", string.Empty);
            GUIUtils.SetProperty("#Trakt.Statistics.Episodes.Scrobbles", string.Empty);
            GUIUtils.SetProperty("#Trakt.Statistics.Episodes.ScrobblesUnique", string.Empty);
            GUIUtils.SetProperty("#Trakt.Statistics.Episodes.Seen", string.Empty);
            GUIUtils.SetProperty("#Trakt.Statistics.Episodes.Shouts", string.Empty);
            GUIUtils.SetProperty("#Trakt.Statistics.Episodes.UnWatched", string.Empty);
            GUIUtils.SetProperty("#Trakt.Statistics.Episodes.Plays", string.Empty);
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
            GUIUtils.SetProperty("#Trakt.Statistics.Movies.Ratings", string.Empty);
            GUIUtils.SetProperty("#Trakt.Statistics.Movies.Hated", string.Empty);
            GUIUtils.SetProperty("#Trakt.Statistics.Movies.Library", string.Empty);
            GUIUtils.SetProperty("#Trakt.Statistics.Movies.Loved", string.Empty);
            GUIUtils.SetProperty("#Trakt.Statistics.Movies.Scrobbles", string.Empty);
            GUIUtils.SetProperty("#Trakt.Statistics.Movies.ScrobblesUnique", string.Empty);
            GUIUtils.SetProperty("#Trakt.Statistics.Movies.Seen", string.Empty);
            GUIUtils.SetProperty("#Trakt.Statistics.Movies.Shouts", string.Empty);
            GUIUtils.SetProperty("#Trakt.Statistics.Movies.UnWatched", string.Empty);
            GUIUtils.SetProperty("#Trakt.Statistics.Movies.Plays", string.Empty);
            GUIUtils.SetProperty("#Trakt.Statistics.Movies.Watched", string.Empty);
            GUIUtils.SetProperty("#Trakt.Statistics.Movies.WatchedElseWhere", string.Empty);
            GUIUtils.SetProperty("#Trakt.Statistics.Movies.WatchedTrakt", string.Empty);
            GUIUtils.SetProperty("#Trakt.Statistics.Movies.WatchedTraktUnique", string.Empty);
            GUIUtils.SetProperty("#Trakt.Statistics.Movies.WatchedUnique", string.Empty);
            #endregion
        }

        internal static void SetStatisticProperties(TraktUserStatistics stats)
        {
            if (stats == null) return;

            #region Network Statistics
            if (stats.Network != null)
            {
                SetProperty("#Trakt.Statistics.Friends", stats.Network.Friends);
                SetProperty("#Trakt.Statistics.Followers", stats.Network.Followers);
                SetProperty("#Trakt.Statistics.Following", stats.Network.Following);
            }
            #endregion

            #region Shows Statistics
            if (stats.Shows != null)
            {
                SetProperty("#Trakt.Statistics.Shows.Library", stats.Shows.Collected);
                SetProperty("#Trakt.Statistics.Shows.Collection", stats.Shows.Collected);
                SetProperty("#Trakt.Statistics.Shows.Watched", stats.Shows.Watched);
                SetProperty("#Trakt.Statistics.Shows.Shouts", stats.Shows.Comments);
                SetProperty("#Trakt.Statistics.Shows.Loved", stats.Shows.Ratings); //TODO
                SetProperty("#Trakt.Statistics.Shows.Hated", "0"); //TODO
                SetProperty("#Trakt.Statistics.Shows.Ratings", stats.Shows.Ratings);
            }
            #endregion

            #region Episodes Statistics
            if (stats.Episodes != null)
            {
                //SetProperty("#Trakt.Statistics.Episodes.Checkins", stats.Episodes.Checkins);
                //SetProperty("#Trakt.Statistics.Episodes.CheckinsUnique", stats.Episodes.CheckinsUnique);
                SetProperty("#Trakt.Statistics.Episodes.Library", stats.Episodes.Collected);
                SetProperty("#Trakt.Statistics.Episodes.Collection", stats.Episodes.Collected);
                SetProperty("#Trakt.Statistics.Episodes.Hated", "0"); //TODO
                SetProperty("#Trakt.Statistics.Episodes.Loved", stats.Episodes.Ratings); //TODO
                SetProperty("#Trakt.Statistics.Episodes.Ratings", stats.Episodes.Ratings);
                //SetProperty("#Trakt.Statistics.Episodes.Scrobbles", stats.Episodes.Scrobbles);
                //SetProperty("#Trakt.Statistics.Episodes.ScrobblesUnique", stats.Episodes.ScrobblesUnique);
                //SetProperty("#Trakt.Statistics.Episodes.Seen", stats.Episodes.Seen);
                SetProperty("#Trakt.Statistics.Episodes.Shouts", stats.Episodes.Comments);
                //SetProperty("#Trakt.Statistics.Episodes.UnWatched", stats.Episodes.UnWatched);
                SetProperty("#Trakt.Statistics.Episodes.Plays", stats.Episodes.Plays);
                SetProperty("#Trakt.Statistics.Episodes.Watched", stats.Episodes.Plays);
                //SetProperty("#Trakt.Statistics.Episodes.WatchedElseWhere", stats.Episodes.WatchedElseWhere);
                //SetProperty("#Trakt.Statistics.Episodes.WatchedTrakt", stats.Episodes.WatchedTrakt);
                //SetProperty("#Trakt.Statistics.Episodes.WatchedTraktUnique", stats.Episodes.WatchedTraktUnique);
                SetProperty("#Trakt.Statistics.Episodes.WatchedUnique", stats.Episodes.Watched);
            }
            #endregion

            #region Movies Statistics
            if (stats.Movies != null)
            {
                //SetProperty("#Trakt.Statistics.Movies.Checkins", stats.Movies.Checkins);
                //SetProperty("#Trakt.Statistics.Movies.CheckinsUnique", stats.Movies.CheckinsUnique);
                SetProperty("#Trakt.Statistics.Movies.Collection", stats.Movies.Collected);
                SetProperty("#Trakt.Statistics.Movies.Library", stats.Movies.Collected);
                SetProperty("#Trakt.Statistics.Movies.Ratings", stats.Movies.Ratings);
                SetProperty("#Trakt.Statistics.Movies.Loved", stats.Movies.Ratings); //TODO
                SetProperty("#Trakt.Statistics.Movies.Hated", "0"); //TODO
                //SetProperty("#Trakt.Statistics.Movies.Scrobbles", stats.Movies.Scrobbles);
                //SetProperty("#Trakt.Statistics.Movies.ScrobblesUnique", stats.Movies.ScrobblesUnique);
                //SetProperty("#Trakt.Statistics.Movies.Seen", stats.Movies.Seen);
                SetProperty("#Trakt.Statistics.Movies.Shouts", stats.Movies.Comments);
                //SetProperty("#Trakt.Statistics.Movies.UnWatched", stats.Movies.UnWatched);
                SetProperty("#Trakt.Statistics.Movies.Plays", stats.Movies.Plays);
                SetProperty("#Trakt.Statistics.Movies.Watched", stats.Movies.Plays);
                //SetProperty("#Trakt.Statistics.Movies.WatchedElseWhere", stats.Movies.WatchedElseWhere);
                //SetProperty("#Trakt.Statistics.Movies.WatchedTrakt", stats.Movies.WatchedTrakt);
                //SetProperty("#Trakt.Statistics.Movies.WatchedTraktUnique", stats.Movies.WatchedTraktUnique);
                SetProperty("#Trakt.Statistics.Movies.WatchedUnique", stats.Movies.Watched);
            }
            #endregion
        }

        internal static void ClearShoutProperties()
        {
            GUIUtils.SetProperty("#Trakt.Shout.Id", string.Empty);
            GUIUtils.SetProperty("#Trakt.Shout.Inserted", string.Empty);
            GUIUtils.SetProperty("#Trakt.Shout.Spoiler", "false");
            GUIUtils.SetProperty("#Trakt.Shout.Review", "false");
            GUIUtils.SetProperty("#Trakt.Shout.Text", string.Empty);
            GUIUtils.SetProperty("#Trakt.Shout.UserRating", string.Empty);
            GUIUtils.SetProperty("#Trakt.Shout.Type", string.Empty);            
            GUIUtils.SetProperty("#Trakt.Shout.Likes", string.Empty);
            GUIUtils.SetProperty("#Trakt.Shout.Replies", string.Empty);
        }

        internal static void SetShoutProperties(TraktComment shout, bool isWatched = false)
        {
            SetProperty("#Trakt.Shout.Id", shout.Id);
            SetProperty("#Trakt.Shout.Inserted", shout.CreatedAt.FromISO8601().ToLongDateString());
            SetProperty("#Trakt.Shout.Spoiler", shout.IsSpoiler);
            SetProperty("#Trakt.Shout.Review", shout.IsReview);
            SetProperty("#Trakt.Shout.Type", shout.IsReview ? "review" : "shout");
            SetProperty("#Trakt.Shout.Likes", shout.Likes);
            SetProperty("#Trakt.Shout.Replies", shout.Replies);
            SetProperty("#Trakt.Shout.UserRating", shout.UserRating);

            // don't hide spoilers if watched
            if (TraktSettings.HideSpoilersOnShouts && shout.IsSpoiler && !isWatched)
                SetProperty("#Trakt.Shout.Text", Translation.HiddenToPreventSpoilers);
            else
                SetProperty("#Trakt.Shout.Text", System.Web.HttpUtility.HtmlDecode(shout.Comment.RemapHighOrderChars()).StripHTML());
        }

        internal static void ClearMovieProperties()
        {
            GUIUtils.SetProperty("#Trakt.Movie.Id", string.Empty);
            GUIUtils.SetProperty("#Trakt.Movie.Tmdb", string.Empty);
            GUIUtils.SetProperty("#Trakt.Movie.Slug", string.Empty);
            GUIUtils.SetProperty("#Trakt.Movie.Imdb", string.Empty);
            GUIUtils.SetProperty("#Trakt.Movie.Certification", string.Empty);
            GUIUtils.SetProperty("#Trakt.Movie.Language", string.Empty);
            GUIUtils.SetProperty("#Trakt.Movie.Overview", string.Empty);
            GUIUtils.SetProperty("#Trakt.Movie.Released", string.Empty);
            GUIUtils.SetProperty("#Trakt.Movie.Runtime", string.Empty);
            GUIUtils.SetProperty("#Trakt.Movie.Tagline", string.Empty);
            GUIUtils.SetProperty("#Trakt.Movie.Title", string.Empty);
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
            GUIUtils.SetProperty("#Trakt.Movie.Ratings.Icon", string.Empty);
            GUIUtils.SetProperty("#Trakt.Movie.Ratings.Percentage", string.Empty);
            GUIUtils.SetProperty("#Trakt.Movie.Ratings.Votes", string.Empty);
        }

        internal static void SetMovieProperties(TraktMovieSummary movie)
        {
            if (movie == null) return;

            SetProperty("#Trakt.Movie.Id", movie.Ids.Trakt);
            SetProperty("#Trakt.Movie.ImdbId", movie.Ids.Imdb);
            SetProperty("#Trakt.Movie.TmdbId", movie.Ids.Tmdb);
            SetProperty("#Trakt.Movie.Slug", movie.Ids.Slug);
            SetProperty("#Trakt.Movie.Certification", movie.Certification);
            SetProperty("#Trakt.Movie.Overview", string.IsNullOrEmpty(movie.Overview) ? Translation.NoMovieSummary : movie.Overview.RemapHighOrderChars());
            SetProperty("#Trakt.Movie.Released", movie.Released);
            SetProperty("#Trakt.Movie.Language", Translation.GetLanguageFromISOCode(movie.Language));
            SetProperty("#Trakt.Movie.Runtime", movie.Runtime);
            SetProperty("#Trakt.Movie.Tagline", movie.Tagline);
            SetProperty("#Trakt.Movie.Title", movie.Title.RemapHighOrderChars());
            SetProperty("#Trakt.Movie.Trailer", movie.Trailer);
            SetProperty("#Trakt.Movie.Url", string.Format("http://trakt.tv/movies/{0}", movie.Ids.Slug));
            SetProperty("#Trakt.Movie.Year", movie.Year);
            SetProperty("#Trakt.Movie.Genres", TraktGenres.Translate(movie.Genres));
            SetProperty("#Trakt.Movie.PosterImageFilename", movie.Images.Poster.LocalImageFilename(ArtworkType.MoviePoster));
            SetProperty("#Trakt.Movie.FanartImageFilename", movie.Images.Fanart.LocalImageFilename(ArtworkType.MovieFanart));
            SetProperty("#Trakt.Movie.InCollection", movie.IsCollected());
            SetProperty("#Trakt.Movie.InWatchList", movie.IsWatchlisted());
            SetProperty("#Trakt.Movie.Plays", movie.Plays());
            SetProperty("#Trakt.Movie.Watched", movie.IsWatched());
            SetProperty("#Trakt.Movie.Rating", movie.UserRating());
            SetProperty("#Trakt.Movie.Ratings.Percentage", movie.Rating.ToPercentage());
            SetProperty("#Trakt.Movie.Ratings.Votes", movie.Votes);
            SetProperty("#Trakt.Movie.Ratings.Icon", (movie.Rating >= 6) ? "love" : "hate");
        }

        internal static void ClearSeasonProperties()
        {
            GUIUtils.SetProperty("#Trakt.Season.TmdbId", string.Empty);
            GUIUtils.SetProperty("#Trakt.Season.TvdbId", string.Empty);
            GUIUtils.SetProperty("#Trakt.Season.TvRageId", string.Empty);
            GUIUtils.SetProperty("#Trakt.Season.Number", string.Empty);
            GUIUtils.SetProperty("#Trakt.Season.EpisodeCount", string.Empty);
            GUIUtils.SetProperty("#Trakt.Season.Watched", string.Empty);
            GUIUtils.SetProperty("#Trakt.Season.InCollection", string.Empty);
            GUIUtils.SetProperty("#Trakt.Season.InWatchList", string.Empty);
            GUIUtils.SetProperty("#Trakt.Season.Plays", string.Empty);
            GUIUtils.SetProperty("#Trakt.Season.Rating", string.Empty);
            GUIUtils.SetProperty("#Trakt.Season.Ratings.Icon", string.Empty);
            GUIUtils.SetProperty("#Trakt.Season.Ratings.Percentage", string.Empty);
            GUIUtils.SetProperty("#Trakt.Season.Ratings.Votes", string.Empty);
            GUIUtils.SetProperty("#Trakt.Season.Url", string.Empty);
            GUIUtils.SetProperty("#Trakt.Season.PosterImageFilename", string.Empty);
        }

        internal static void SetSeasonProperties(TraktShowSummary show, TraktSeasonSummary season)
        {
            SetProperty("#Trakt.Season.TmdbId", season.Ids.Tmdb);
            SetProperty("#Trakt.Season.TvdbId", season.Ids.Tvdb);
            SetProperty("#Trakt.Season.TvRageId", season.Ids.TvRage);
            SetProperty("#Trakt.Season.Number", season.Number);            
            SetProperty("#Trakt.Season.Url", string.Format("http://trakt.tv/shows/{0}/seasons/{1}", show.Ids.Slug, season.Number));
            SetProperty("#Trakt.Season.PosterImageFilename", season.Images == null ? string.Empty : season.Images.Poster.LocalImageFilename(ArtworkType.SeasonPoster));
            SetProperty("#Trakt.Season.EpisodeCount", season.EpisodeCount);
            SetProperty("#Trakt.Season.Overview", season.Overview ?? show.Overview);
            SetProperty("#Trakt.Season.Watched", season.IsWatched(show));
            SetProperty("#Trakt.Season.Plays", season.Plays(show));
            SetProperty("#Trakt.Season.InCollection", season.IsCollected(show));
            SetProperty("#Trakt.Season.InWatchList", season.IsWatchlisted(show));
            SetProperty("#Trakt.Season.Rating", season.UserRating());
            SetProperty("#Trakt.Season.Ratings.Percentage", season.Rating.ToPercentage());
            SetProperty("#Trakt.Season.Ratings.Votes", season.Votes);
            SetProperty("#Trakt.Season.Ratings.Icon", (season.Rating >= 6) ? "love" : "hate");
        }

        internal static void ClearShowProperties()
        {
            GUIUtils.SetProperty("#Trakt.Show.Id", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.ImdbId", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.TvdbId", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.TmdbId", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.TvRageId", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.Title", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.Language", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.Url", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.AirDay", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.AirDayLocalized", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.AirTime", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.AirTimeLocalized", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.AirTimezone", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.AirTimezoneWindows", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.Certification", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.Country", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.FirstAired", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.Network", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.Overview", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.Runtime", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.Year", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.Status", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.Genres", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.InWatchList", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.InCollection", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.Watched", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.AiredEpisodes", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.Plays", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.Rating", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.Ratings.Icon", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.Ratings.Percentage", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.Ratings.Votes", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.FanartImageFilename", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.PosterImageFilename", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.BannerImageFilename", string.Empty);
        }

        internal static void SetShowProperties(TraktShowSummary show)
        {
            if (show == null) return;

            SetProperty("#Trakt.Show.Id", show.Ids.Trakt);
            SetProperty("#Trakt.Show.ImdbId", show.Ids.Imdb);
            SetProperty("#Trakt.Show.TvdbId", show.Ids.Tvdb);
            SetProperty("#Trakt.Show.TmdbId", show.Ids.Tmdb);
            SetProperty("#Trakt.Show.TvRageId", show.Ids.TvRage);
            SetProperty("#Trakt.Show.Title", show.Title.RemapHighOrderChars());
            SetProperty("#Trakt.Show.Language", Translation.GetLanguageFromISOCode(show.Language));
            SetProperty("#Trakt.Show.Url", string.Format("http://trakt.tv/shows/{0}", show.Ids.Slug));
            if (show.Airs != null)
            {
                SetProperty("#Trakt.Show.AirDay", show.FirstAired.FromISO8601().ToLocalisedDayOfWeek());
                SetProperty("#Trakt.Show.AirDayLocalized", show.FirstAired.FromISO8601().ToLocalTime().ToLocalisedDayOfWeek());
                SetProperty("#Trakt.Show.AirTime", show.FirstAired.FromISO8601().ToShortTimeString());
                SetProperty("#Trakt.Show.AirTimeLocalized", show.FirstAired.FromISO8601().ToLocalTime().ToShortTimeString());
                SetProperty("#Trakt.Show.AirTimezone", show.Airs.Timezone);
                SetProperty("#Trakt.Show.AirTimezoneWindows", show.Airs.Timezone.OlsenToWindowsTimezone());
            }
            SetProperty("#Trakt.Show.Certification", show.Certification);
            SetProperty("#Trakt.Show.Country", show.Country.ToCountryName());
            SetProperty("#Trakt.Show.FirstAired", show.FirstAired.FromISO8601().ToShortDateString());
            SetProperty("#Trakt.Show.FirstAiredLocalized", show.FirstAired.FromISO8601().ToLocalTime().ToShortDateString());
            SetProperty("#Trakt.Show.Network", show.Network);
            SetProperty("#Trakt.Show.Overview", string.IsNullOrEmpty(show.Overview) ? Translation.NoShowSummary : show.Overview.RemapHighOrderChars());
            SetProperty("#Trakt.Show.Runtime", show.Runtime);
            SetProperty("#Trakt.Show.Year", show.Year);
            SetProperty("#Trakt.Show.Status", show.Status);
            SetProperty("#Trakt.Show.TranslatedStatus", (show.Status ?? "").Replace(" " ,"").Translate());
            SetProperty("#Trakt.Show.Genres", TraktGenres.Translate(show.Genres));
            SetProperty("#Trakt.Show.InWatchList", show.IsWatchlisted());
            SetProperty("#Trakt.Show.InCollection", show.IsCollected());
            SetProperty("#Trakt.Show.Watched", show.IsWatched());
            SetProperty("#Trakt.Show.AiredEpisodes", show.AiredEpisodes);
            SetProperty("#Trakt.Show.Plays", show.Plays());
            SetProperty("#Trakt.Show.Rating", show.UserRating());
            SetProperty("#Trakt.Show.Ratings.Percentage", show.Rating.ToPercentage());
            SetProperty("#Trakt.Show.Ratings.Votes", show.Votes);
            SetProperty("#Trakt.Show.Ratings.Icon", (show.Rating > 6) ? "love" : "hate");
            if (show.Images != null)
            {
                SetProperty("#Trakt.Show.FanartImageFilename", show.Images.Fanart.LocalImageFilename(ArtworkType.ShowFanart));
                SetProperty("#Trakt.Show.PosterImageFilename", show.Images.Poster.LocalImageFilename(ArtworkType.ShowPoster));
                SetProperty("#Trakt.Show.BannerImageFilename", show.Images.Banner.LocalImageFilename(ArtworkType.ShowBanner));
            }
        }

        internal static void ClearEpisodeProperties()
        {
            GUIUtils.SetProperty("#Trakt.Episode.Id", string.Empty);
            GUIUtils.SetProperty("#Trakt.Episode.TvdbId", string.Empty);
            GUIUtils.SetProperty("#Trakt.Episode.ImdbId", string.Empty);
            GUIUtils.SetProperty("#Trakt.Episode.TmdbId", string.Empty);
            GUIUtils.SetProperty("#Trakt.Episode.Number", string.Empty);
            GUIUtils.SetProperty("#Trakt.Episode.Season", string.Empty);
            GUIUtils.SetProperty("#Trakt.Episode.Season", string.Empty);
            GUIUtils.SetProperty("#Trakt.Episode.FirstAired", string.Empty);
            GUIUtils.SetProperty("#Trakt.Episode.FirstAiredLocalized", string.Empty);
            GUIUtils.SetProperty("#Trakt.Episode.FirstAiredLocalizedDayOfWeek", string.Empty);
            GUIUtils.SetProperty("#Trakt.Episode.FirstAiredLocalizedTime", string.Empty);
            GUIUtils.SetProperty("#Trakt.Episode.Title", string.Empty);
            GUIUtils.SetProperty("#Trakt.Episode.Url", string.Empty);
            GUIUtils.SetProperty("#Trakt.Episode.Overview", string.Empty);
            GUIUtils.SetProperty("#Trakt.Episode.Runtime", string.Empty);
            GUIUtils.SetProperty("#Trakt.Episode.InWatchList", string.Empty);
            GUIUtils.SetProperty("#Trakt.Episode.InCollection", string.Empty);
            GUIUtils.SetProperty("#Trakt.Episode.Plays", string.Empty);
            GUIUtils.SetProperty("#Trakt.Episode.Watched", string.Empty);
            GUIUtils.SetProperty("#Trakt.Episode.Rating", string.Empty);
            GUIUtils.SetProperty("#Trakt.Episode.Ratings.Icon", string.Empty);
            GUIUtils.SetProperty("#Trakt.Episode.Ratings.HatedCount", string.Empty);
            GUIUtils.SetProperty("#Trakt.Episode.Ratings.LovedCount", string.Empty);
            GUIUtils.SetProperty("#Trakt.Episode.Ratings.Percentage", string.Empty);
            GUIUtils.SetProperty("#Trakt.Episode.Ratings.Votes", string.Empty);
            GUIUtils.SetProperty("#Trakt.Episode.EpisodeImageFilename", string.Empty);
        }

        internal static void SetEpisodeProperties(TraktShowSummary show, TraktEpisodeSummary episode)
        {
            if (episode == null) return;

            SetProperty("#Trakt.Episode.Id", episode.Ids.Trakt);
            SetProperty("#Trakt.Episode.TvdbId", episode.Ids.Tvdb);
            SetProperty("#Trakt.Episode.ImdbId", episode.Ids.Imdb);
            SetProperty("#Trakt.Episode.TmdbId", episode.Ids.Imdb);
            SetProperty("#Trakt.Episode.Number", episode.Number);
            SetProperty("#Trakt.Episode.Season", episode.Season);
            if (episode.FirstAired != null)
            {
                // FirsAired is converted to UTC from original countrys timezaone on trakt
                SetProperty("#Trakt.Episode.FirstAired", episode.FirstAired.FromISO8601().ToShortDateString());
                SetProperty("#Trakt.Episode.FirstAiredLocalized", episode.FirstAired.FromISO8601().ToLocalTime().ToShortDateString());
                SetProperty("#Trakt.Episode.FirstAiredLocalizedDayOfWeek", episode.FirstAired.FromISO8601().ToLocalTime().ToLocalisedDayOfWeek());
                SetProperty("#Trakt.Episode.FirstAiredLocalizedTime", episode.FirstAired.FromISO8601().ToLocalTime().ToShortTimeString());
            }
            SetProperty("#Trakt.Episode.Title", string.IsNullOrEmpty(episode.Title) ? string.Format("{0} {1}", Translation.Episode, episode.Number.ToString()) : episode.Title.RemapHighOrderChars());
            SetProperty("#Trakt.Episode.Url", string.Format("http://trakt.tv/shows/{0}/seasons/{1}/episodes/{2}", show.Ids.Slug, episode.Season, episode.Number));
            SetProperty("#Trakt.Episode.Overview", string.IsNullOrEmpty(episode.Overview) ? Translation.NoEpisodeSummary : episode.Overview.RemapHighOrderChars());
            SetProperty("#Trakt.Episode.Runtime", show.Runtime);
            SetProperty("#Trakt.Episode.InWatchList", episode.IsWatchlisted());
            SetProperty("#Trakt.Episode.InCollection", episode.IsCollected(show));
            SetProperty("#Trakt.Episode.Plays", episode.Plays(show));
            SetProperty("#Trakt.Episode.Watched", episode.IsWatched(show));
            SetProperty("#Trakt.Episode.Rating", episode.UserRating(show));
            SetProperty("#Trakt.Episode.Ratings.Percentage", episode.Rating.ToPercentage());
            SetProperty("#Trakt.Episode.Ratings.Votes", episode.Votes);
            SetProperty("#Trakt.Episode.Ratings.Icon", (episode.Rating >= 6) ? "love" : "hate");
            SetProperty("#Trakt.Episode.EpisodeImageFilename", episode.Images.ScreenShot.LocalImageFilename(ArtworkType.EpisodeImage));
        }

        internal static void ClearPersonProperties()
        {
            GUIUtils.SetProperty("#Trakt.Person.Id", string.Empty);
            GUIUtils.SetProperty("#Trakt.Person.TmdbId", string.Empty);
            GUIUtils.SetProperty("#Trakt.Person.ImdbId", string.Empty);
            GUIUtils.SetProperty("#Trakt.Person.Name", string.Empty);
            GUIUtils.SetProperty("#Trakt.Person.HeadshotUrl", string.Empty);
            GUIUtils.SetProperty("#Trakt.Person.HeadshotFilename", string.Empty);
            GUIUtils.SetProperty("#Trakt.Person.Url", string.Empty);
            GUIUtils.SetProperty("#Trakt.Person.Biography", string.Empty);
            GUIUtils.SetProperty("#Trakt.Person.Birthday", string.Empty);
            GUIUtils.SetProperty("#Trakt.Person.Birthplace", string.Empty);
            GUIUtils.SetProperty("#Trakt.Person.Death", string.Empty);
        }
        
        internal static void SetPersonProperties(TraktPersonSummary person)
        {
            SetProperty("#Trakt.Person.Id", person.Ids.Trakt);
            SetProperty("#Trakt.Person.ImdbId", person.Ids.ImdbId);
            SetProperty("#Trakt.Person.TmdbId", person.Ids.TmdbId);
            SetProperty("#Trakt.Person.Name", person.Name);
            SetProperty("#Trakt.Person.HeadshotUrl", person.Images.HeadShot.FullSize);
            SetProperty("#Trakt.Person.HeadshotFilename", person.Images.HeadShot.LocalImageFilename(ArtworkType.Headshot));
            SetProperty("#Trakt.Person.Url", string.Format("http://trakt.tv/people/{0}", person.Ids.Slug));
            SetProperty("#Trakt.Person.Biography", person.Biography ?? Translation.NoPersonBiography.RemapHighOrderChars());
            SetProperty("#Trakt.Person.Birthday", person.Birthday);
            SetProperty("#Trakt.Person.Birthplace", person.Birthplace);
            SetProperty("#Trakt.Person.Death", person.Death);
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

            // Add Watchlist  
            listItem = new GUIListItem(Translation.AddToWatchList);
            listItem.ItemId = (int)ActivityContextMenuItem.AddToWatchList;
            listItems.Add(listItem);

            // Add to Custom list
            listItem = new GUIListItem(Translation.AddToList);
            listItem.ItemId = (int)ActivityContextMenuItem.AddToList;
            listItems.Add(listItem);

            // Shouts
            listItem = new GUIListItem(Translation.Comments);
            listItem.ItemId = (int)ActivityContextMenuItem.Shouts;
            listItems.Add(listItem);

            // Rate
            listItem = new GUIListItem(Translation.Rate + "...");
            listItem.ItemId = (int)ActivityContextMenuItem.Rate;
            listItems.Add(listItem);

            // Trailers
            if (TraktHelper.IsTrailersAvailableAndEnabled)
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
            if (!movie.IsWatched())
            {
                listItem = new GUIListItem(Translation.MarkAsWatched);
                dlg.Add(listItem);
                listItem.ItemId = (int)TrendingContextMenuItem.MarkAsWatched;
            }

            // Mark As UnWatched
            if (movie.IsWatched())
            {
                listItem = new GUIListItem(Translation.MarkAsUnWatched);
                dlg.Add(listItem);
                listItem.ItemId = (int)TrendingContextMenuItem.MarkAsUnWatched;
            }

            // Add/Remove Watchlist            
            if (!movie.IsWatchlisted())
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
            if (!movie.IsCollected() && !TraktSettings.KeepTraktLibraryClean)
            {
                listItem = new GUIListItem(Translation.AddToLibrary);
                dlg.Add(listItem);
                listItem.ItemId = (int)TrendingContextMenuItem.AddToLibrary;
            }

            if (movie.IsCollected())
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
            listItem = new GUIListItem(Translation.Comments);
            dlg.Add(listItem);
            listItem.ItemId = (int)TrendingContextMenuItem.Shouts;

            // Trailers
            if (TraktHelper.IsTrailersAvailableAndEnabled)
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

            if (!movie.IsCollected() && TraktHelper.IsMpNZBAvailableAndEnabled)
            {
                // Search for movie with mpNZB
                listItem = new GUIListItem(Translation.SearchWithMpNZB);
                dlg.Add(listItem);
                listItem.ItemId = (int)TrendingContextMenuItem.SearchWithMpNZB;
            }

            if (!movie.IsCollected() && TraktHelper.IsMyTorrentsAvailableAndEnabled)
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

            // Add/Remove Watchlist            
            if (!show.IsWatchlisted())
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

            if (TraktHelper.IsTrailersAvailableAndEnabled)
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
            listItem = new GUIListItem(Translation.Comments);
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
        public static void ShowMovieTrailersPluginMenu(TraktMovieSummary movie)
        {
            var trailerItem = new MediaItem
            {
                IMDb = movie.Ids.Imdb.ToNullIfEmpty(),
                TMDb = movie.Ids.Tmdb.ToString(),
                Plot = movie.Overview,
                Poster = movie.Images.Poster.LocalImageFilename(ArtworkType.MoviePoster),
                Title = movie.Title,
                Year = movie.Year.GetValueOrDefault(0)
            };
            Trailers.Trailers.SearchForTrailers(trailerItem);
        }

        public static void ShowMovieTrailersMenu(TraktMovieSummary movie)
        {
            if (TraktHelper.IsTrailersAvailableAndEnabled)
            {
                ShowMovieTrailersPluginMenu(movie);
                return;
            }
        }

        #endregion

        #region TV Show Trailers
        public static void ShowTVShowTrailersPluginMenu(TraktShowSummary show)
        {
            var trailerItem = new MediaItem
            {
                MediaType = MediaItemType.Show,
                IMDb = show.Ids.Imdb.ToNullIfEmpty(),
                TVDb = show.Ids.Tvdb.ToString(),
                TVRage = show.Ids.TvRage.ToString(),
                TMDb = show.Ids.Tmdb.ToString(),
                Plot = show.Overview,
                Poster = show.Images.Poster.LocalImageFilename(ArtworkType.ShowPoster),
                Title = show.Title,                
                Year = show.Year.GetValueOrDefault(0),
                AirDate = show.FirstAired.FromISO8601().ToString("yyyy-MM-dd")
            };
            Trailers.Trailers.SearchForTrailers(trailerItem);
        }

        public static void ShowTVShowTrailersMenu(TraktShowSummary show, TraktEpisodeSummary episode = null)
        {
            if (TraktHelper.IsTrailersAvailableAndEnabled)
            {
                if (episode == null)
                    ShowTVShowTrailersPluginMenu(show);
                else
                    ShowTVEpisodeTrailersPluginMenu(show, episode);

                return;
            }
        }
        #endregion

        #region TV Season Trailers
        public static void ShowTVSeasonTrailersPluginMenu(TraktShowSummary show, int season)
        {
            var trailerItem = new MediaItem
            {
                MediaType = MediaItemType.Season,
                IMDb = show.Ids.Imdb.ToNullIfEmpty(),
                TMDb = show.Ids.Tmdb.ToString(),
                TVDb = show.Ids.Tvdb.ToString(),
                TVRage = show.Ids.TvRage.ToString(),
                Plot = show.Overview,
                Poster = show.Images.Poster.LocalImageFilename(ArtworkType.ShowPoster),
                Title = show.Title,
                Year = show.Year.GetValueOrDefault(0),
                AirDate = show.FirstAired.FromISO8601().ToString("yyyy-MM-dd"),
                Season = season
            };
            Trailers.Trailers.SearchForTrailers(trailerItem);
        }
        #endregion

        #region TV Episode Trailers
        public static void ShowTVEpisodeTrailersPluginMenu(TraktShowSummary show, TraktEpisodeSummary episode)
        {
            var trailerItem = new MediaItem
            {
                MediaType = MediaItemType.Episode,
                IMDb = show.Ids.Imdb.ToNullIfEmpty(),
                TMDb = show.Ids.Tmdb.ToString(),
                TVDb = show.Ids.Tvdb.ToString(),
                TVRage = show.Ids.TvRage.ToString(),
                Plot = show.Overview,
                Poster = show.Images.Poster.LocalImageFilename(ArtworkType.ShowPoster),
                Title = show.Title,
                Year = show.Year.GetValueOrDefault(0),
                AirDate = show.FirstAired.FromISO8601().ToString("yyyy-MM-dd"),
                Season = episode.Season,
                Episode = episode.Number,
                EpisodeName = episode.Title
            };
            Trailers.Trailers.SearchForTrailers(trailerItem);
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
            return ShowTraktExtMovieMenu(title, year, imdbid, false, fanart, people, showAll);
        }
        public static bool ShowTraktExtMovieMenu(string title, string year, string imdbid, bool isWatched, string fanart, SearchPeople people, bool showAll)
        {
            var dlg = (IDialogbox)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
            dlg.Reset();
            dlg.SetHeading(GUIUtils.PluginName());

            GUIListItem pItem = new GUIListItem(Translation.Comments);
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
                    TraktLogger.Info("Displaying rate dialog for movie. Title = '{0}', Year = '{1}', IMDb ID = '{2}'", title, year.ToLogString(), imdbid.ToLogString());
                    GUIUtils.ShowRateDialog<TraktSyncMovieRated>(new TraktSyncMovieRated
                        {
                            Ids = new TraktMovieId { Imdb = imdbid.ToNullIfEmpty() },
                            Title = title,
                            Year = year.ToNullableInt32()
                        });
                    break;

                case ((int)TraktMenuItems.Shouts):
                    TraktLogger.Info("Displaying Shouts for movie. Title = '{0}', Year = '{1}', IMDb ID = '{2}'", title, year.ToLogString(), imdbid.ToLogString());
                    TraktHelper.ShowMovieShouts(imdbid, title, year, fanart);
                    break;

                case ((int)TraktMenuItems.Related):
                    TraktLogger.Info("Displaying Related Movies for. Title = '{0}', Year = '{1}', IMDb ID = '{2}'", title, year.ToLogString(), imdbid.ToLogString());
                    TraktHelper.ShowRelatedMovies(title, year, imdbid);
                    break;

                case ((int)TraktMenuItems.AddToWatchList):
                    TraktLogger.Info("Adding movie to Watchlist. Title = '{0}', Year = '{1}', IMDb ID = '{2}'", title, year.ToLogString(), imdbid.ToLogString());
                    TraktHelper.AddMovieToWatchList(title, year, imdbid, true);
                    break;

                case ((int)TraktMenuItems.AddToCustomList):
                    TraktLogger.Info("Adding movie to Custom List. Title = '{0}', Year = '{1}', IMDb ID = '{2}'", title, year.ToLogString(), imdbid.ToLogString());
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
            return ShowTraktExtTVShowMenu(title, year, tvdbid, null, fanart, null, showAll);
        }
        public static bool ShowTraktExtTVShowMenu(string title, string year, string tvdbid, string fanart, SearchPeople people, bool showAll)
        {
            return ShowTraktExtTVShowMenu(title, year, tvdbid, null, fanart, people, showAll);
        }
        public static bool ShowTraktExtTVShowMenu(string title, string year, string tvdbid, string imdbid, string fanart, SearchPeople people, bool showAll)
        {
            var dlg = (IDialogbox)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
            dlg.Reset();
            dlg.SetHeading(GUIUtils.PluginName());

            GUIListItem pItem = new GUIListItem(Translation.Comments);
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
                    TraktLogger.Info("Displaying rate dialog for tv show. Title = '{0}', Year = '{1}', TVDb ID = '{2}'", title, year.ToLogString(), tvdbid.ToLogString());
                    GUIUtils.ShowRateDialog<TraktSyncShowRated>(new TraktSyncShowRated
                    {
                        Ids = new TraktShowId { Tvdb = tvdbid.ToNullableInt32(), Imdb = imdbid.ToNullIfEmpty() },
                        Title = title,
                        Year = year.ToNullableInt32()
                    });
                    break;

                case ((int)TraktMenuItems.Shouts):
                    TraktLogger.Info("Displaying Shouts for tv show. Title = '{0}', Year = '{1}', TVDb ID = '{2}', IMDb ID = '{3}'", title, year.ToLogString(), tvdbid.ToLogString(), imdbid.ToLogString());
                    TraktHelper.ShowTVShowShouts(title, year.ToNullableInt32(), tvdbid.ToNullableInt32(), null, imdbid, false, fanart);
                    break;

                case ((int)TraktMenuItems.Related):
                    TraktLogger.Info("Displaying Related shows for tv show. Title = '{0}', Year = '{1}', TVDb ID = '{2}'", title, year.ToLogString(), tvdbid.ToLogString());
                    TraktHelper.ShowRelatedShows(title, year.ToNullableInt32(), tvdbid.ToNullableInt32(), imdbid.ToNullIfEmpty(), null, null);
                    break;

                case ((int)TraktMenuItems.AddToWatchList):
                    TraktLogger.Info("Adding tv show to Watchlist. Title = '{0}', Year = '{1}', TVDb ID = '{2}'", title, year.ToLogString(), tvdbid.ToLogString());
                    TraktHelper.AddShowToWatchList(title, year.ToNullableInt32(), tvdbid.ToNullableInt32(), imdbid.ToNullIfEmpty(), null, null);
                    break;

                case ((int)TraktMenuItems.AddToCustomList):
                    TraktLogger.Info("Adding tv show to Custom List. Title = '{0}', Year = '{1}', TVDb ID = '{2}'", title, year.ToLogString(), tvdbid.ToLogString());
                    TraktHelper.AddRemoveShowInUserList(title, year, tvdbid, false);
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

        #region Seasons
        //TODO: Add support for Season related items e.g. Rate,List,Watchlist...
        public static bool ShowTraktExtTVSeasonMenu(string title, string year, string tvdbid, string season, string seasonid, string fanart, SearchPeople people, bool showAll)
        {
            return ShowTraktExtTVSeasonMenu(title, year, tvdbid, null, season, seasonid, fanart, people, showAll);
        }
        public static bool ShowTraktExtTVSeasonMenu(string title, string year, string tvdbid, string imdbid, string season, string seasonid, string fanart, SearchPeople people, bool showAll)
        {
            var dlg = (IDialogbox)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
            dlg.Reset();
            dlg.SetHeading(GUIUtils.PluginName());

            GUIListItem pItem = new GUIListItem(Translation.Comments);
            dlg.Add(pItem);
            pItem.ItemId = (int)TraktMenuItems.Shouts;

            pItem = new GUIListItem(Translation.Rate);
            dlg.Add(pItem);
            pItem.ItemId = (int)TraktMenuItems.Rate;

            //pItem = new GUIListItem(Translation.AddToWatchList);
            //dlg.Add(pItem);
            //pItem.ItemId = (int)TraktMenuItems.AddToWatchList;

            //pItem = new GUIListItem(Translation.AddToList);
            //dlg.Add(pItem);
            //pItem.ItemId = (int)TraktMenuItems.AddToCustomList;

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

            int seasonNumber = 0;
            if (!int.TryParse(season, out seasonNumber))
                return false;

            // Show Context Menu
            dlg.DoModal(GUIWindowManager.ActiveWindow);
            if (dlg.SelectedId < 0) return false;

            switch (dlg.SelectedId)
            {
                case ((int)TraktMenuItems.Rate):
                    TraktLogger.Info("Displaying rate dialog for tv season. Title = '{0}', Year = '{1}', TVDb ID = '{2}', Season = '{3}'", title, year.ToLogString(), tvdbid.ToLogString(), season);
                    GUIUtils.ShowRateDialog<TraktSyncSeasonRatedEx>(new TraktSyncSeasonRatedEx
                    {
                        Ids = new TraktShowId { Tvdb = tvdbid.ToNullableInt32(), Imdb = imdbid.ToNullIfEmpty() },
                        Title = title,
                        Year = year.ToNullableInt32(),
                        Seasons = new List<TraktSyncSeasonRatedEx.Season>
                        {
                            new TraktSyncSeasonRatedEx.Season
                            {
                                Number = seasonNumber,
                                RatedAt = DateTime.UtcNow.ToISO8601()
                            }
                        }
                    });
                    break;

                case ((int)TraktMenuItems.Shouts):
                    TraktLogger.Info("Displaying Shouts for tv season. Title = '{0}', Year = '{1}', TVDb ID = '{2}', IMDb ID = '{3}', Season = '{4}'", title, year.ToLogString(), tvdbid.ToLogString(), imdbid.ToLogString(), season);
                    TraktHelper.ShowTVSeasonShouts(title, year.ToNullableInt32(), tvdbid.ToNullableInt32(), null, imdbid, seasonNumber, false, fanart);
                    break;

                case ((int)TraktMenuItems.AddToWatchList):
                    TraktLogger.Info("Adding tv season to Watchlist. Title = '{0}', Year = '{1}', TVDb ID = '{2}' Season = '{3}'", title, year.ToLogString(), tvdbid.ToLogString(), season);
                    //TraktHelper.AddSeasonToWatchList(title, year.ToNullableInt32(), tvdbid.ToNullableInt32(), imdbid.ToNullIfEmpty(), null, null);
                    break;

                case ((int)TraktMenuItems.AddToCustomList):
                    TraktLogger.Info("Adding tv season to Custom List. Title = '{0}', Year = '{1}', TVDb ID = '{2}', Season = '{3}'", title, year.ToLogString(), tvdbid.ToLogString(), season);
                    //TraktHelper.AddRemoveSeasonInUserList(title, year, tvdbid, false);
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
            return ShowTraktExtEpisodeMenu(title, year, season, episode, tvdbid, false, fanart, people, showAll);
        }
        public static bool ShowTraktExtEpisodeMenu(string title, string year, string season, string episode, string tvdbid, bool isWatched, string fanart, SearchPeople people, bool showAll)
        {
            return ShowTraktExtEpisodeMenu(title, year, season, episode, tvdbid, null, isWatched, fanart, people, showAll);
        }
        public static bool ShowTraktExtEpisodeMenu(string title, string year, string season, string episode, string tvdbid, string episodetvdbid, bool isWatched, string fanart, SearchPeople people, bool showAll)
        {
            return ShowTraktExtEpisodeMenu(title, year, season, episode, tvdbid, null, episodetvdbid, isWatched, fanart, people, showAll);
        }
        public static bool ShowTraktExtEpisodeMenu(string title, string year, string season, string episode, string tvdbid, string imdbid, string episodetvdbid, bool isWatched, string fanart, SearchPeople people, bool showAll)
        {
            var dlg = (IDialogbox)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
            dlg.Reset();
            dlg.SetHeading(GUIUtils.PluginName());

            var pItem = new GUIListItem(Translation.Comments);
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
                    TraktLogger.Info("Displaying rate dialog for tv episode. Title = '{0}', Year = '{1}', Season = '{2}', Episode = '{3}', Show ID = '{4}', Episode ID = '{5}'", title, year.ToLogString(), season, episode, tvdbid.ToLogString(), episodetvdbid.ToLogString());
                    //GUIUtils.ShowRateDialog<TraktSyncEpisodeRated>(new TraktSyncEpisodeRated
                    //    {
                    //        Ids = new TraktEpisodeId
                    //        {
                    //            Tvdb = episodetvdbid.ToNullableInt32()
                    //        },
                    //        RatedAt = DateTime.UtcNow.ToISO8601(),
                    //        Season = season.ToInt(),
                    //        Number = episode.ToInt()
                    //    });
                    
                    GUIUtils.ShowRateDialog<TraktSyncShowRatedEx>(new TraktSyncShowRatedEx
                    {
                        Ids = new TraktShowId { Tvdb = tvdbid.ToNullableInt32(), Imdb = imdbid.ToNullIfEmpty() },
                        Title = title,
                        Year = year.ToNullableInt32(),
                        Seasons = new List<TraktSyncShowRatedEx.Season>
                                      {
                                          new TraktSyncShowRatedEx.Season
                                              {
                                                Number = season.ToInt(),
                                                Episodes = new List<TraktSyncShowRatedEx.Season.Episode>
                                                               {
                                                                    new TraktSyncShowRatedEx.Season.Episode
                                                                    {
                                                                        Number = episode.ToInt(),
                                                                        RatedAt = DateTime.UtcNow.ToISO8601()
                                                                    }
                                                               }
                                            }
                                    }
                    });
                    break;

                case ((int)TraktMenuItems.Shouts):
                    TraktLogger.Info("Displaying Shouts for tv episode. Title = '{0}', Year = '{1}', Season = '{2}', Episode = '{3}'", title, year.ToLogString(), season, episode);
                    TraktHelper.ShowEpisodeShouts(title, year.ToNullableInt32(), tvdbid.ToNullableInt32(), null, imdbid.ToNullIfEmpty(), season.ToInt(), episode.ToInt(), isWatched, fanart);
                    break;

                case ((int)TraktMenuItems.AddToWatchList):
                    TraktLogger.Info("Adding tv episode to Watchlist. Title = '{0}', Year = '{1}', Season = '{2}', Episode = '{3}'", title, year.ToLogString(), season, episode);
                    TraktHelper.AddEpisodeToWatchList(title, season.ToInt(), episode.ToInt(), tvdbid.ToNullableInt32(), null, null, null);
                    break;

                case ((int)TraktMenuItems.AddToCustomList):
                    TraktLogger.Info("Adding tv episode to Custom List. Title = '{0}', Year = '{1}', Season = '{2}', Episode = '{3}', Episode ID = '{4}'", title, year.ToLogString(), season, episode, episodetvdbid.ToLogString());
                    if (string.IsNullOrEmpty(episodetvdbid))
                    {
                        TraktHelper.AddRemoveEpisodeInUserList(new TraktEpisode { Ids = new TraktEpisodeId { Tvdb = episodetvdbid.ToNullableInt32() } }, false);
                    }
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

        internal static IEnumerable<TraktMovieTrending> FilterTrendingMovies(IEnumerable<TraktMovieTrending> moviesToFilter)
        {
            if (TraktSettings.TrendingMoviesHideWatched)
                moviesToFilter = moviesToFilter.Where(t => !t.Movie.IsWatched());

            if (TraktSettings.TrendingMoviesHideWatchlisted)
                moviesToFilter = moviesToFilter.Where(t => !t.Movie.IsWatchlisted());

            if (TraktSettings.TrendingMoviesHideCollected)
                moviesToFilter = moviesToFilter.Where(t => !t.Movie.IsCollected());

            if (TraktSettings.TrendingMoviesHideRated)
                moviesToFilter = moviesToFilter.Where(t => t.Movie.UserRating() == null);

            return moviesToFilter;
        }

        internal static IEnumerable<TraktShowTrending> FilterTrendingShows(IEnumerable<TraktShowTrending> showsToFilter)
        {
            if (TraktSettings.TrendingShowsHideWatched)
                showsToFilter = showsToFilter.Where(t => !t.Show.IsWatched());

            if (TraktSettings.TrendingShowsHideWatchlisted)
                showsToFilter = showsToFilter.Where(t => !t.Show.IsWatchlisted());

            if (TraktSettings.TrendingShowsHideCollected)
                showsToFilter = showsToFilter.Where(t => !t.Show.IsCollected());

            if (TraktSettings.TrendingShowsHideRated)
                showsToFilter = showsToFilter.Where(t => t.Show.UserRating() == null);

            return showsToFilter;
        }
        #endregion

        #region Activity Helpers
        internal static string GetActivityListItemTitle(TraktActivity.Activity activity)
        {
            //if (activity == null) return string.Empty;

            //string itemName = GetActivityItemName(activity);
            //string userName = activity.User.Username;
            //string title = string.Empty;

            //if (string.IsNullOrEmpty(activity.Action) || string.IsNullOrEmpty(activity.Type))
            //    return string.Empty;

            //ActivityAction action = (ActivityAction)Enum.Parse(typeof(ActivityAction), activity.Action);
            //ActivityType type = (ActivityType)Enum.Parse(typeof(ActivityType), activity.Type);

            //switch (action)
            //{
            //    case ActivityAction.watching:
            //        title = string.Format(Translation.ActivityWatching, userName, itemName);
            //        break;

            //    case ActivityAction.scrobble:
            //        title = string.Format(Translation.ActivityWatched, userName, itemName);
            //        break;

            //    case ActivityAction.checkin:
            //        title = string.Format(Translation.ActivityCheckedIn, userName, itemName);
            //        break;

            //    case ActivityAction.seen:
            //        if (type == ActivityType.episode && activity.Episodes.Count > 1)
            //        {
            //            title = string.Format(Translation.ActivitySeenEpisodes, userName, activity.Episodes.Count, itemName);
            //        }
            //        else
            //        {
            //            title = string.Format(Translation.ActivitySeen, userName, itemName);
            //        }
            //        break;

            //    case ActivityAction.collection:
            //        if (type == ActivityType.episode && activity.Episodes.Count > 1)
            //        {
            //            title = string.Format(Translation.ActivityCollectedEpisodes, userName, activity.Episodes.Count, itemName);
            //        }
            //        else
            //        {
            //            title = string.Format(Translation.ActivityCollected, userName, itemName);
            //        }
            //        break;

            //    case ActivityAction.rating:
            //        if (activity.UseRatingAdvanced)
            //        {
            //            title = string.Format(Translation.ActivityRatingAdvanced, userName, itemName, activity.RatingAdvanced);
            //        }
            //        else
            //        {
            //            title = string.Format(Translation.ActivityRating, userName, itemName);
            //        }
            //        break;

            //    case ActivityAction.watchlist:
            //        title = string.Format(Translation.ActivityWatchlist, userName, itemName);
            //        break;

            //    case ActivityAction.review:
            //        title = string.Format(Translation.ActivityReview, userName, itemName);
            //        break;

            //    case ActivityAction.shout:
            //        title = string.Format(Translation.ActivityShouts, userName, itemName);
            //        break;

            //    case ActivityAction.created: // created list
            //        title = string.Format(Translation.ActivityCreatedList, userName, itemName);
            //        break;

            //    case ActivityAction.item_added: // added item to list
            //        title = string.Format(Translation.ActivityAddToList, userName, itemName, activity.List.Name);
            //        break;
            //}

            //return title;
            return "TODO";
        }

        internal static string GetActivityItemName(TraktActivity.Activity activity)
        {
            //TODO
            //string name = string.Empty;

            //try
            //{
            //    ActivityType type = (ActivityType)Enum.Parse(typeof(ActivityType), activity.Type);
            //    ActivityAction action = (ActivityAction)Enum.Parse(typeof(ActivityAction), activity.Action);

            //    switch (type)
            //    {
            //        case ActivityType.episode:
            //            if (action == ActivityAction.seen || action == ActivityAction.collection)
            //            {
            //                if (activity.Episodes.Count > 1)
            //                {
            //                    // just return show name
            //                    name = activity.Show.Title;
            //                }
            //                else
            //                {
            //                    //  get the first and only item in collection of episodes
            //                    string episodeIndex = activity.Episodes.First().Number.ToString();
            //                    string seasonIndex = activity.Episodes.First().Season.ToString();
            //                    string episodeName = activity.Episodes.First().Title;

            //                    if (string.IsNullOrEmpty(episodeName))
            //                        episodeName = string.Format("{0} {1}", Translation.Episode, episodeIndex);

            //                    name = string.Format("{0} - {1}x{2} - {3}", activity.Show.Title, seasonIndex, episodeIndex, episodeName);
            //                }
            //            }
            //            else
            //            {
            //                string episodeName = activity.Episode.Title;

            //                if (string.IsNullOrEmpty(episodeName))
            //                    episodeName = string.Format("{0} {1}", Translation.Episode, activity.Episode.Number.ToString());

            //                name = string.Format("{0} - {1}x{2} - {3}", activity.Show.Title, activity.Episode.Season.ToString(), activity.Episode.Number.ToString(), episodeName);
            //            }
            //            break;

            //        case ActivityType.show:
            //            name = activity.Show.Title;
            //            break;

            //        case ActivityType.movie:
            //            name = string.Format("{0} ({1})", activity.Movie.Title, activity.Movie.Year);
            //            break;

            //        case ActivityType.list:
            //            if (action == ActivityAction.item_added)
            //            {
            //                // return the name of the item added to the list
            //                switch (activity.ListItem.Type)
            //                {
            //                    case "show":
            //                        name = activity.ListItem.Show.Title;
            //                        break;

            //                    case "episode":
            //                        string episodeIndex = activity.ListItem.Episode.Number.ToString();
            //                        string seasonIndex = activity.ListItem.Episode.Season.ToString();
            //                        string episodeName = activity.ListItem.Episode.Title;

            //                        if (string.IsNullOrEmpty(episodeName))
            //                            episodeName = string.Format("{0} {1}", Translation.Episode, episodeIndex);

            //                        name = string.Format("{0} - {1}x{2} - {3}", activity.ListItem.Show.Title, seasonIndex, episodeIndex, episodeName);
            //                        break;

            //                    case "movie":
            //                        name = string.Format("{0} ({1})", activity.ListItem.Movie.Title, activity.ListItem.Movie.Year);
            //                        break;
            //                }
            //            }
            //            else if (action == ActivityAction.created)
            //            {
            //                // return the list name
            //                name = activity.List.Name;
            //            }
            //            break;
            //    }
            //}
            //catch
            //{
            //    // most likely trakt returned a null object
            //    name = string.Empty;
            //}

            //return name;


            return "TODO";
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
