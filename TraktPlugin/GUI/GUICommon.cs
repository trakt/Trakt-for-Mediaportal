using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaPortal.GUI.Library;
using MediaPortal.GUI.Video;
using MediaPortal.Video.Database;
using TraktPlugin.TraktAPI;
using TraktPlugin.TraktAPI.DataStructures;

namespace TraktPlugin.GUI
{
    enum TraktGUIWindows
    {
        Main = 87258,
        Calendar = 87259,
        Friends = 87260,
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
        Shouts = 87280
    }

    enum TraktDashboardControls
    {
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
        MyTorrents = 5678
    }

    enum ExternalPluginControls
    {
        WatchList = 97258,
        Rate = 97259,
        Shouts = 97260,
        CustomList = 97261,
        RelatedItems = 97262
    }

    public class GUICommon
    {
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

        /// <summary>
        /// Checks if a selected movie exists locally and plays movie or
        /// jumps to corresponding plugin details view
        /// </summary>
        /// <param name="jumpTo">false if movie should be played directly</param>
        public static void CheckAndPlayMovie(bool jumpTo, string title, int year, string imdbid)
        {
            bool handled = false;

            if (TraktHelper.IsMovingPicturesAvailableAndEnabled)
            {
                int? movieid = null;

                // Find Movie ID in MovingPictures
                // Movie List is now cached internally in MovingPictures so it will be fast
                bool movieExists = TraktHandlers.MovingPictures.FindMovieID(title, year, imdbid, ref movieid);

                if (movieExists)
                {
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
                IMDBMovie movie = null;
                if (TraktHandlers.MyVideos.FindMovieID(title, year, imdbid, ref movie))
                {
                    // Open My Videos Video Info view so user can play movie
                    if (jumpTo)
                    {
                        GUIVideoInfo videoInfo = (GUIVideoInfo)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_VIDEO_INFO);
                        videoInfo.Movie = movie;
                        GUIWindowManager.ActivateWindow((int)GUIWindow.Window.WINDOW_VIDEO_INFO);
                    }
                    else
                    {
                        GUIVideoFiles.PlayMovie(movie.ID, false);
                    }
                    handled = true;
                }
            }

            // check if its in My Films database
            if (TraktHelper.IsMyFilmsAvailableAndEnabled && handled == false)
            {
                int? movieid = null;
                string config = null;
                if (TraktHandlers.MyFilmsHandler.FindMovie(title, year, imdbid, ref movieid, ref config))
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

            if (TraktHelper.IsOnlineVideosAvailableAndEnabled && handled == false)
            {
                string loadingParameter = string.Format("site:IMDb Movie Trailers|search:{0}|return:Locked", imdbid);
                GUIWindowManager.ActivateWindow((int)ExternalPluginWindows.OnlineVideos, loadingParameter);
                handled = true;
            }

        }

        /// <summary>
        /// Checks if a selected episode exists locally and plays episode
        /// </summary>
        /// <param name="seriesid">the series tvdb id of episode</param>
        /// <param name="imdbid">the series imdb id of episode</param>
        /// <param name="seasonidx">the season index of episode</param>
        /// <param name="episodeidx">the episode index of episode</param>
        public static void CheckAndPlayEpisode(int seriesid, string imdbid, int seasonidx, int episodeidx)
        {
            bool handled = false;

            // check if plugin is installed and enabled
            if (TraktHelper.IsMPTVSeriesAvailableAndEnabled)
            {
                // Play episode if it exists
                handled = TraktHandlers.TVSeries.PlayEpisode(seriesid, seasonidx, episodeidx);
            }

            if (TraktHelper.IsMyAnimeAvailableAndEnabled && handled == false)
            {
                handled = TraktHandlers.MyAnime.PlayEpisode(seriesid, seasonidx, episodeidx);
            }

            if (TraktHelper.IsOnlineVideosAvailableAndEnabled && handled == false)
            {
                TraktLogger.Info("No episodes found! Attempting Trailer lookup in IMDb Trailers.");
                string loadingParameter = string.Format("site:IMDb Movie Trailers|search:{0}|return:Locked", imdbid);
                GUIWindowManager.ActivateWindow((int)ExternalPluginWindows.OnlineVideos, loadingParameter);
                handled = true;
            }
        }

        /// <summary>
        /// Checks if a selected show exists locally and plays first unwatched episode
        /// </summary>
        /// <param name="seriesid">the series tvdb id of show</param>
        /// <param name="imdbid">the series imdb id of show</param>
        public static void CheckAndPlayFirstUnwatched(int seriesid, string imdbid)
        {
            TraktLogger.Info("Attempting to play TVDb: {0}, IMDb: {1}", seriesid.ToString(), imdbid);
            bool handled = false;

            // check if plugin is installed and enabled
            if (TraktHelper.IsMPTVSeriesAvailableAndEnabled)
            {
                // Play episode if it exists
                TraktLogger.Info("Checking if any episodes to watch in MP-TVSeries");
                handled = TraktHandlers.TVSeries.PlayFirstUnwatchedEpisode(seriesid);
            }

            if (TraktHelper.IsMyAnimeAvailableAndEnabled && handled == false)
            {
                TraktLogger.Info("Checking if any episodes to watch in My Anime");
                handled = TraktHandlers.MyAnime.PlayFirstUnwatchedEpisode(seriesid);
            }

            if (TraktHelper.IsOnlineVideosAvailableAndEnabled && handled == false)
            {
                TraktLogger.Info("No episodes found! Attempting Trailer lookup in IMDb Trailers.");
                string loadingParameter = string.Format("site:IMDb Movie Trailers|search:{0}|return:Locked", imdbid);
                GUIWindowManager.ActivateWindow((int)ExternalPluginWindows.OnlineVideos, loadingParameter);
                handled = true;
            }
        }

        #region Rate Movie

        internal static bool RateMovie(TraktMovie movie)
        {
            TraktRateMovie rateObject = new TraktRateMovie
            {
                IMDBID = movie.Imdb,
                TMDBID = movie.Tmdb,
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

        #region Common Skin Properties

        internal static void SetProperty(string property, string value)
        {
            string propertyValue = string.IsNullOrEmpty(value) ? "N/A" : value;
            GUIUtils.SetProperty(property, propertyValue);
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

        internal static void SetMovieProperties(TraktMovie movie)
        {
            SetProperty("#Trakt.Movie.Imdb", movie.Imdb);
            SetProperty("#Trakt.Movie.Certification", movie.Certification);
            SetProperty("#Trakt.Movie.Overview", string.IsNullOrEmpty(movie.Overview) ? Translation.NoMovieSummary : movie.Overview);
            SetProperty("#Trakt.Movie.Released", movie.Released.FromEpoch().ToShortDateString());
            SetProperty("#Trakt.Movie.Runtime", movie.Runtime.ToString());
            SetProperty("#Trakt.Movie.Tagline", movie.Tagline);
            SetProperty("#Trakt.Movie.Title", movie.Title);
            SetProperty("#Trakt.Movie.Tmdb", movie.Tmdb);
            SetProperty("#Trakt.Movie.Trailer", movie.Trailer);
            SetProperty("#Trakt.Movie.Url", movie.Url);
            SetProperty("#Trakt.Movie.Year", movie.Year);
            SetProperty("#Trakt.Movie.Genres", string.Join(", ", movie.Genres.ToArray()));
            SetProperty("#Trakt.Movie.PosterImageFilename", movie.Images.PosterImageFilename);
            SetProperty("#Trakt.Movie.FanartImageFilename", movie.Images.FanartImageFilename);
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

        internal static void ClearShowProperties()
        {
            GUIUtils.SetProperty("#Trakt.Show.Imdb", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.Tvdb", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.TvRage", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.Title", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.Url", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.AirDay", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.AirTime", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.Certification", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.Country", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.FirstAired", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.Network", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.Overview", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.Runtime", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.Year", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.Genres", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.InWatchList", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.Rating", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.RatingAdvanced", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.Ratings.Icon", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.Ratings.HatedCount", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.Ratings.LovedCount", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.Ratings.Percentage", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.Ratings.Votes", string.Empty);
            GUIUtils.SetProperty("#Trakt.Show.FanartImageFilename", string.Empty);
        }

        internal static void SetShowProperties(TraktShow show)
        {
            SetProperty("#Trakt.Show.Imdb", show.Imdb);
            SetProperty("#Trakt.Show.Tvdb", show.Tvdb);
            SetProperty("#Trakt.Show.TvRage", show.TvRage);
            SetProperty("#Trakt.Show.Title", show.Title);
            SetProperty("#Trakt.Show.Url", show.Url);
            SetProperty("#Trakt.Show.AirDay", show.AirDay);
            SetProperty("#Trakt.Show.AirTime", show.AirTime);
            SetProperty("#Trakt.Show.Certification", show.Certification);
            SetProperty("#Trakt.Show.Country", show.Country);
            SetProperty("#Trakt.Show.FirstAired", show.FirstAired.FromEpoch().ToShortDateString());
            SetProperty("#Trakt.Show.Network", show.Network);
            SetProperty("#Trakt.Show.Overview", string.IsNullOrEmpty(show.Overview) ? Translation.NoShowSummary : show.Overview);
            SetProperty("#Trakt.Show.Runtime", show.Runtime.ToString());
            SetProperty("#Trakt.Show.Year", show.Year.ToString());
            SetProperty("#Trakt.Show.Genres", string.Join(", ", show.Genres.ToArray()));
            SetProperty("#Trakt.Show.InWatchList", show.InWatchList.ToString());
            SetProperty("#Trakt.Show.Rating", show.Rating);
            SetProperty("#Trakt.Show.RatingAdvanced", show.RatingAdvanced.ToString());
            SetProperty("#Trakt.Show.Ratings.Icon", (show.Ratings.LovedCount > show.Ratings.HatedCount) ? "love" : "hate");
            SetProperty("#Trakt.Show.Ratings.HatedCount", show.Ratings.HatedCount.ToString());
            SetProperty("#Trakt.Show.Ratings.LovedCount", show.Ratings.LovedCount.ToString());
            SetProperty("#Trakt.Show.Ratings.Percentage", show.Ratings.Percentage.ToString());
            SetProperty("#Trakt.Show.Ratings.Votes", show.Ratings.Votes.ToString());
            SetProperty("#Trakt.Show.FanartImageFilename", show.Images.FanartImageFilename);
        }

        internal static void ClearEpisodeProperties()
        {
            GUIUtils.SetProperty("#Trakt.Episode.Number", string.Empty);
            GUIUtils.SetProperty("#Trakt.Episode.Season", string.Empty);
            GUIUtils.SetProperty("#Trakt.Episode.FirstAired", string.Empty);
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
            SetProperty("#Trakt.Episode.Number", episode.Number.ToString());
            SetProperty("#Trakt.Episode.Season", episode.Season.ToString());
            SetProperty("#Trakt.Episode.FirstAired", episode.FirstAired.FromEpoch().ToShortDateString());
            SetProperty("#Trakt.Episode.Title", string.IsNullOrEmpty(episode.Title) ? string.Format("{0} {1}", Translation.Episode, episode.Number.ToString()) : episode.Title);
            SetProperty("#Trakt.Episode.Url", episode.Url);
            SetProperty("#Trakt.Episode.Overview", string.IsNullOrEmpty(episode.Overview) ? Translation.NoEpisodeSummary : episode.Overview);
            SetProperty("#Trakt.Episode.Runtime", episode.Runtime.ToString());
            SetProperty("#Trakt.Episode.InWatchList", episode.InWatchList.ToString());
            SetProperty("#Trakt.Episode.InCollection", episode.InCollection.ToString());
            SetProperty("#Trakt.Episode.Plays", episode.Plays.ToString());
            SetProperty("#Trakt.Episode.Watched", episode.Watched.ToString());
            SetProperty("#Trakt.Episode.Rating", episode.Rating);
            SetProperty("#Trakt.Episode.RatingAdvanced", episode.RatingAdvanced.ToString());
            SetProperty("#Trakt.Episode.Ratings.Icon", (episode.Ratings.LovedCount > episode.Ratings.HatedCount) ? "love" : "hate");
            SetProperty("#Trakt.Episode.Ratings.HatedCount", episode.Ratings.HatedCount.ToString());
            SetProperty("#Trakt.Episode.Ratings.LovedCount", episode.Ratings.LovedCount.ToString());
            SetProperty("#Trakt.Episode.Ratings.Percentage", episode.Ratings.Percentage.ToString());
            SetProperty("#Trakt.Episode.Ratings.Votes", episode.Ratings.Votes.ToString());
            SetProperty("#Trakt.Episode.EpisodeImageFilename", episode.Images.EpisodeImageFilename);
        }

        internal static void ClearSeasonProperties()
        {
            GUIUtils.SetProperty("#Trakt.Season.Number", string.Empty);
        }

        #endregion

    }
}
