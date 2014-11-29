using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using MediaPortal.Configuration;
using MediaPortal.Profile;
using MediaPortal.GUI.Library;
using TraktPlugin.GUI;
using TraktPlugin.TraktHandlers;
using TraktPlugin.TraktAPI;
using TraktPlugin.TraktAPI.DataStructures;
using TraktPlugin.TraktAPI.Extensions;

namespace TraktPlugin
{
    public class TraktHelper
    {
        #region Plugin Helpers

        public static bool IsPluginEnabled(string name)
        {
            using (Settings xmlreader = new MPSettings())
            {
                return xmlreader.GetValueAsBool("plugins", name, false);
            }
        }

        public static bool IsOnlineVideosAvailableAndEnabled
        {
            get
            {
                return File.Exists(Path.Combine(Config.GetSubFolder(Config.Dir.Plugins, "Windows"), "OnlineVideos.MediaPortal1.dll")) && (IsPluginEnabled("Online Videos") || IsPluginEnabled("OnlineVideos"));
            }
        }

        public static bool IsMovingPicturesAvailableAndEnabled
        {
            get
            {
                return File.Exists(Path.Combine(Config.GetSubFolder(Config.Dir.Plugins, "Windows"), "MovingPictures.dll")) && IsPluginEnabled("Moving Pictures");
            }
        }

        public static bool IsMPTVSeriesAvailableAndEnabled
        {
            get
            {
                return File.Exists(Path.Combine(Config.GetSubFolder(Config.Dir.Plugins, "Windows"), "MP-TVSeries.dll")) && IsPluginEnabled("MP-TV Series");
            }
        }

        public static bool IsMyFilmsAvailableAndEnabled
        {
            get
            {
                return File.Exists(Path.Combine(Config.GetSubFolder(Config.Dir.Plugins, "Windows"), "MyFilms.dll")) && IsPluginEnabled("MyFilms");
            }
        }

        public static bool IsMyAnimeAvailableAndEnabled
        {
            get
            {
                return File.Exists(Path.Combine(Config.GetSubFolder(Config.Dir.Plugins, "Windows"), "Anime2.dll")) && IsPluginEnabled("My Anime");
            }
        }

        public static bool IsMpNZBAvailableAndEnabled
        {
            get
            {
                return File.Exists(Path.Combine(Config.GetSubFolder(Config.Dir.Plugins, "Windows"), "mpNZB.dll")) && IsPluginEnabled("mpNZB");
            }
        }

        public static bool IsMyTorrentsAvailableAndEnabled
        {
            get
            {
                return File.Exists(Path.Combine(Config.GetSubFolder(Config.Dir.Plugins, "Windows"), "MyTorrents.dll")) && IsPluginEnabled("MyTorrents");
            }
        }

        public static bool IsTrailersAvailableAndEnabled
        {
            get
            {
                return File.Exists(Path.Combine(Config.GetSubFolder(Config.Dir.Plugins, "Windows"), "Trailers.dll")) && IsPluginEnabled("Trailers");
            }
        }

        #endregion

        #region Public API Helpers

        #region Movie Watchlist

        public static void AddMovieToWatchList(TraktMovie movie, bool updatePluginFilters)
        {
            AddMovieToWatchList(movie.Title, movie.Year, movie.Ids.ImdbId, movie.Ids.TmdbId, movie.Ids.Id, updatePluginFilters);
        }

        public static void AddMovieToWatchList(string title, string year)
        {
            AddMovieToWatchList(title, year, null);
        }

        public static void AddMovieToWatchList(string title, string year, string imdbid)
        {
            AddMovieToWatchList(title, year, imdbid, false);
        }

        public static void AddMovieToWatchList(string title, string year, bool updatePluginFilters)
        {
            AddMovieToWatchList(title, year, null, updatePluginFilters);
        }

        public static void AddMovieToWatchList(string title, string year, string imdbid, bool updatePluginFilters)
        {
            AddMovieToWatchList(title, year.ToNullableInt32(), imdbid, null, updatePluginFilters);
        }

        public static void AddMovieToWatchList(string title, int? year, string imdbid, int? tmdbid, bool updatePluginFilters)
        {
            AddMovieToWatchList(title, year, imdbid, tmdbid, null, updatePluginFilters);
        }

        public static void AddMovieToWatchList(string title, int? year, string imdbid, int? tmdbid, int? traktid, bool updatePluginFilters)
        {
            if (!GUICommon.CheckLogin(false)) return;

            var movie = new TraktMovie
            {
                Ids = new TraktMovieId
                {
                    Id = traktid,
                    ImdbId = imdbid,
                    TmdbId = tmdbid
                },
                Title = title,
                Year = year
            };
            
            var syncThread = new Thread((objSyncData) =>
            {
                var response = TraktAPI.TraktAPI.AddMovieToWatchlist(objSyncData as TraktMovie);
                if (response == null) return;

                if (updatePluginFilters && IsMovingPicturesAvailableAndEnabled)
                {
                    // update categories & filters menu in MovingPictures
                    MovingPictures.AddMovieCriteriaToWatchlistNode(imdbid);
                }
                GUI.GUIWatchListMovies.ClearCache(TraktSettings.Username);
            })
            {
                IsBackground = true,
                Name = "Watchlist"
            };

            syncThread.Start(movie);
        }

        public static void RemoveMovieFromWatchList(TraktMovie movie, bool updateMovingPicturesFilters)
        {
            RemoveMovieFromWatchList(movie.Title, movie.Year, movie.Ids.ImdbId, movie.Ids.TmdbId, movie.Ids.Id, updateMovingPicturesFilters);
        }

        public static void RemoveMovieFromWatchList(string title, int? year, string imdbid, bool updateMovingPicturesFilters)
        {
            RemoveMovieFromWatchList(title, year, imdbid, null, null, updateMovingPicturesFilters);
        }

        public static void RemoveMovieFromWatchList(string title, int? year, string imdbid, int? tmdbid, int? traktid, bool updatePluginFilters)
        {
            if (!GUICommon.CheckLogin(false)) return;

            var movie = new TraktMovie
            {
                Ids = new TraktMovieId
                {
                    Id = traktid,
                    ImdbId = imdbid,
                    TmdbId = tmdbid
                },
                Title = title,
                Year = year
            };

            var syncThread = new Thread((objSyncData) =>
            {
                var response = TraktAPI.TraktAPI.RemoveMovieFromWatchlist(objSyncData as TraktMovie);
                if (response == null) return;

                if (updatePluginFilters && IsMovingPicturesAvailableAndEnabled)
                {
                    // update categories & filters menu in MovingPictures              
                    MovingPictures.RemoveMovieCriteriaFromWatchlistNode(imdbid);
                }
                GUI.GUIWatchListMovies.ClearCache(TraktSettings.Username);
            })
            {
                IsBackground = true,
                Name = "Watchlist"
            };

            syncThread.Start(movie);
        }

        #endregion

        #region Show WatchList

        public static void AddShowToWatchList(TraktShow show)
        {
            AddShowToWatchList(show.Title, show.Year, show.Ids.TvdbId, show.Ids.ImdbId, show.Ids.TmdbId, show.Ids.Id);
        }

        public static void AddShowToWatchList(string title, string year, string tvdbid)
        {
            AddShowToWatchList(title, year.ToNullableInt32(), tvdbid.ToNullableInt32(), null, null, null);
        }

        public static void AddShowToWatchList(string title, int? year, int? tvdbid, string imdbid, int? tmdbid, int? traktid)
        {
            if (!GUICommon.CheckLogin(false)) return;

            var show = new TraktShow
            {
                Ids = new TraktShowId
                {
                    Id = traktid,
                    ImdbId = imdbid,
                    TmdbId = tmdbid,
                    TvdbId = tvdbid
                },
                Title = title,
                Year = year
            };

            var syncThread = new Thread((objSyncData) =>
            {
                var response = TraktAPI.TraktAPI.AddShowToWatchlist(objSyncData as TraktShow);
            })
            {
                IsBackground = true,
                Name = "Watchlist"
            };

            syncThread.Start(show);
        }

        public static void RemoveShowFromWatchList(TraktShow show)
        {
            RemoveShowFromWatchList(show.Title, show.Year, show.Ids.TvdbId, show.Ids.ImdbId, show.Ids.TmdbId, show.Ids.Id);
        }

        public static void RemoveShowFromWatchList(string title, string year, string tvdbid)
        {
            RemoveShowFromWatchList(title, year.ToNullableInt32(), tvdbid.ToNullableInt32(), null, null, null);
        }

        public static void RemoveShowFromWatchList(string title, int? year, int? tvdbid, string imdbid, int? tmdbid, int? traktid)
        {
            if (!GUICommon.CheckLogin(false)) return;

            var show = new TraktShow
            {
                Ids = new TraktShowId
                {
                    Id = traktid,
                    ImdbId = imdbid,
                    TmdbId = tmdbid,
                    TvdbId = tvdbid
                },
                Title = title,
                Year = year
            };

            var syncThread = new Thread((objSyncData) =>
            {
                var response = TraktAPI.TraktAPI.RemoveShowFromWatchlist(objSyncData as TraktShow);
            })
            {
                IsBackground = true,
                Name = "Watchlist"
            };

            syncThread.Start(show);
        }

        #endregion

        #region Episode WatchList

        public static void AddEpisodeToWatchList(TraktEpisode episode)
        {
            AddEpisodeToWatchList(episode.Title, episode.Season, episode.Number, episode.Ids.TvdbId, episode.Ids.ImdbId, episode.Ids.TmdbId, episode.Ids.Id);
        }

        public static void AddEpisodeToWatchList(string title, int season, int number, int? tvdbid, string imdbid, int? tmdbid, int? traktid)
        {
            if (!GUICommon.CheckLogin(false)) return;

            var episode = new TraktEpisode
            {
                Ids = new TraktEpisodeId
                {
                    Id = traktid,
                    ImdbId = imdbid,
                    TmdbId = tmdbid,
                    TvdbId = tvdbid
                },
                Title = title,
                Season = season,
                Number = number
            };

            var syncThread = new Thread((objSyncData) =>
            {
                var response = TraktAPI.TraktAPI.AddEpisodeToWatchlist(objSyncData as TraktEpisode);
            })
            {
                IsBackground = true,
                Name = "Watchlist"
            };

            syncThread.Start(episode);
        }

        public static void RemoveEpisodeFromWatchList(TraktEpisode episode)
        {
            RemoveEpisodeFromWatchList(episode.Title, episode.Season, episode.Number, episode.Ids.TvdbId, episode.Ids.ImdbId, episode.Ids.TmdbId, episode.Ids.Id);
        }

        public static void RemoveEpisodeFromWatchList(string title, int season, int number, int? tvdbid, string imdbid, int? tmdbid, int? traktid)
        {
            if (!GUICommon.CheckLogin(false)) return;

            var episode = new TraktEpisode
            {
                Ids = new TraktEpisodeId
                {
                    Id = traktid,
                    ImdbId = imdbid,
                    TmdbId = tmdbid,
                    TvdbId = tvdbid
                },
                Title = title,
                Season = season,
                Number = number
            };

            var syncThread = new Thread((objSyncData) =>
            {
                var response = TraktAPI.TraktAPI.RemoveEpisodeFromWatchlist(objSyncData as TraktEpisode);
            })
            {
                IsBackground = true,
                Name = "Watchlist"
            };

            syncThread.Start(episode);
        }

        #endregion

        #region Add/Remove Movie in List

        public static void AddRemoveMovieInUserList(TraktMovie movie, bool remove)
        {
            AddRemoveMovieInUserList(TraktSettings.Username, movie.Title, movie.Year, movie.Ids.ImdbId, movie.Ids.TmdbId, movie.Ids.Id, remove);
        }

        public static void AddRemoveMovieInUserList(string title, string year, string imdbid, bool remove)
        {
            AddRemoveMovieInUserList(TraktSettings.Username, title, year.ToNullableInt32(), imdbid, null, null, remove);
        }

        public static void AddRemoveMovieInUserList(string username, string title, int? year, string imdbid, int? tmdbid, int? traktid, bool remove)
        {
            // TODO
            //if (!GUICommon.CheckLogin(false)) return;

            //GUIBackgroundTask.Instance.ExecuteInBackgroundAndCallback(() =>
            //{
            //    return TraktLists.GetListsForUser(username);
            //},
            //delegate(bool success, object result)
            //{
            //    if (success)
            //    {
            //        IEnumerable<TraktUserList> customlists = result as IEnumerable<TraktUserList>;

            //        // get slug of lists selected
            //        List<string> slugs = TraktLists.GetUserListSelections(customlists.ToList());
            //        if (slugs == null || slugs.Count == 0) return;

            //        TraktListItem item = new TraktListItem
            //        {
            //            Type = TraktItemType.movie.ToString(),
            //            Title = title,
            //            Year = Convert.ToInt32(year),
            //            ImdbId = imdbid
            //        };

            //        AddRemoveItemInList(slugs, item, remove);
            //    }
            //}, Translation.GettingLists, true);
        }

        #endregion

        #region Add/Remove Show in List

        public static void AddRemoveShowInUserList(TraktShow show, bool remove)
        {
            AddRemoveShowInUserList(TraktSettings.Username, show.Title, show.Year, show.Ids.TvdbId, show.Ids.ImdbId, show.Ids.TmdbId, show.Ids.Id, remove);
        }

        public static void AddRemoveShowInUserList(string title, string year, string tvdbid, bool remove)
        {
            AddRemoveShowInUserList(TraktSettings.Username, title, year.ToNullableInt32(), tvdbid.ToNullableInt32(), null, null, null, remove);
        }

        public static void AddRemoveShowInUserList(string username, string title, int? year, int? tvdbid, string imdbid, int? tmdbid, int? traktid, bool remove)
        {
            //TODO
            //if (!GUICommon.CheckLogin(false)) return;

            //GUIBackgroundTask.Instance.ExecuteInBackgroundAndCallback(() =>
            //{
            //    return TraktLists.GetListsForUser(username);
            //},
            //delegate(bool success, object result)
            //{
            //    if (success)
            //    {
            //        IEnumerable<TraktUserList> customlists = result as IEnumerable<TraktUserList>;

            //        // get slug of lists selected
            //        List<string> slugs = TraktLists.GetUserListSelections(customlists.ToList());
            //        if (slugs == null || slugs.Count == 0) return;

            //        TraktListItem item = new TraktListItem
            //        {
            //            Type = TraktItemType.show.ToString(),
            //            Title = title,
            //            Year = string.IsNullOrEmpty(year) ? 0 : Convert.ToInt32(year),
            //            TvdbId = tvdbid
            //        };

            //        AddRemoveItemInList(slugs, item, remove);
            //    }
            //}, Translation.GettingLists, true);
        }

        #endregion

        #region Add/Remove Season in List

        public static void AddRemoveSeasonInUserList(string title, string year, string season, string tvdbid, bool remove)
        {
            //TODO
            //AddRemoveSeasonInUserList(TraktSettings.Username, title, year, season, tvdbid, remove);
        }

        public static void AddRemoveSeasonInUserList(string username, string title, string year, string season, string tvdbid, bool remove)
        {
            //TODO
            //if (!GUICommon.CheckLogin(false)) return;

            //GUIBackgroundTask.Instance.ExecuteInBackgroundAndCallback(() =>
            //{
            //    return TraktLists.GetListsForUser(username);
            //},
            //delegate(bool success, object result)
            //{
            //    if (success)
            //    {
            //        IEnumerable<TraktUserList> customlists = result as IEnumerable<TraktUserList>;

            //        // get slug of lists selected
            //        List<string> slugs = TraktLists.GetUserListSelections(customlists.ToList());
            //        if (slugs == null || slugs.Count == 0) return;

            //        TraktListItem item = new TraktListItem
            //        {
            //            Type = TraktItemType.season.ToString(),
            //            Title = title,
            //            Year = Convert.ToInt32(year),
            //            Season = Convert.ToInt32(season),
            //            TvdbId = tvdbid
            //        };

            //        AddRemoveItemInList(slugs, item, remove);
            //    }
            //}, Translation.GettingLists, true);
        }

        #endregion

        #region Add/Remove Episode in List

        public static void AddRemoveEpisodeInUserList(TraktEpisode episode, bool remove)
        {
            AddRemoveEpisodeInUserList(TraktSettings.Username, episode.Title, episode.Season, episode.Number, episode.Ids.TvdbId, episode.Ids.ImdbId, episode.Ids.TmdbId, episode.Ids.Id, remove);
        }

        public static void AddRemoveEpisodeInUserList(string title, int season, int number, string tvdbid, bool remove)
        {
            AddRemoveEpisodeInUserList(TraktSettings.Username, title, season, number, tvdbid.ToNullableInt32(), null, null, null, remove);
        }

        public static void AddRemoveEpisodeInUserList(string username, string title, int season, int number, int? tvdbid, string imdbid, int? tmdbid, int? traktid, bool remove)
        {
            //TODO
            //if (!GUICommon.CheckLogin(false)) return;

            //GUIBackgroundTask.Instance.ExecuteInBackgroundAndCallback(() =>
            //{
            //    return TraktLists.GetListsForUser(username);
            //},
            //delegate(bool success, object result)
            //{
            //    if (success)
            //    {
            //        IEnumerable<TraktUserList> customlists = result as IEnumerable<TraktUserList>;

            //        // get slug of lists selected
            //        List<string> slugs = TraktLists.GetUserListSelections(customlists.ToList());
            //        if (slugs == null || slugs.Count == 0) return;

            //        TraktListItem item = new TraktListItem
            //        {
            //            Type = TraktItemType.episode.ToString(),
            //            Title = title,
            //            Year = Convert.ToInt32(year),
            //            Season = Convert.ToInt32(season),
            //            Episode = Convert.ToInt32(episode),
            //            TvdbId = tvdbid
            //        };

            //        AddRemoveItemInList(slugs, item, remove);
            //    }
            //}, Translation.GettingLists, true);
        }

        #endregion

        #region Related Movies

        public static void ShowRelatedMovies(TraktMovie movie)
        {
            ShowRelatedMovies(movie.Title, movie.Year, movie.Ids.ImdbId, movie.Ids.TmdbId, movie.Ids.Id);
        }

        public static void ShowRelatedMovies(string title, string year, string imdbid)
        {
            ShowRelatedMovies(title, year.ToNullableInt32(), imdbid, null, null);
        }

        public static void ShowRelatedMovies(string title, int? year, string imdbid, int? tmdbid, int? traktid)
        {
            if (!File.Exists(GUIGraphicsContext.Skin + @"\Trakt.Related.Movies.xml"))
            {
                // let user know they need to update skin or harass skin author
                GUIUtils.ShowOKDialog(GUIUtils.PluginName(), Translation.SkinOutOfDate);
                return;
            }
           
            RelatedMovie relatedMovie = new RelatedMovie
            {
                ImdbId = imdbid,
                TmdbId = tmdbid,
                TraktId = traktid,
                Title = title,
                Year = year
            };
            GUIRelatedMovies.relatedMovie = relatedMovie;
            GUIWindowManager.ActivateWindow((int)TraktGUIWindows.RelatedMovies);
        }

        #endregion

        #region Related Shows

        public static void ShowRelatedShows(TraktShow show)
        {
            ShowRelatedShows(show.Title, show.Ids.TvdbId, show.Ids.TmdbId, show.Ids.Id);
        }

        public static void ShowRelatedShows(string title, string tvdbid)
        {
            ShowRelatedShows(title, tvdbid.ToNullableInt32(), null, null);
        }

        public static void ShowRelatedShows(string title, int? tvdbid, int? tmdbid, int? traktid)
        {
            if (!File.Exists(GUIGraphicsContext.Skin + @"\Trakt.Related.Shows.xml"))
            {
                // let user know they need to update skin or harass skin author
                GUIUtils.ShowOKDialog(GUIUtils.PluginName(), Translation.SkinOutOfDate);
                return;
            }

            RelatedShow relatedShow = new RelatedShow
            {
                TvdbId = tvdbid,
                TmdbId = tmdbid,
                TraktId = traktid,
                Title = title                
            };
            GUIRelatedShows.relatedShow = relatedShow;
            GUIWindowManager.ActivateWindow((int)TraktGUIWindows.RelatedShows);
        }

        #endregion

        #region Movie Shouts

        public static void ShowMovieShouts(TraktMovieSummary movie)
        {
            ShowMovieShouts(movie.Title, movie.Year, movie.Ids.ImdbId, movie.Ids.Id, movie.IsWatched(), movie.Images.Fanart.LocalImageFilename(ArtworkType.MovieFanart), TraktSettings.DownloadFullSizeFanart ? movie.Images.Fanart.FullSize : movie.Images.Fanart.MediumSize);
        }

        public static void ShowMovieShouts(string imdbid, string title, string year, string fanart)
        {
            ShowMovieShouts(title, year, imdbid, fanart, null);
        }

        public static void ShowMovieShouts(string title, string year, string imdbid, bool isWatched, string fanart)
        {
            ShowMovieShouts(title, year.ToNullableInt32(), imdbid, null, false, fanart, null);
        }

        public static void ShowMovieShouts(string title, string imdbid, string year, string fanart, string onlineFanart = null)
        {
            ShowMovieShouts(title, year.ToNullableInt32(), imdbid, null, false, fanart, onlineFanart);
        }

        public static void ShowMovieShouts(string title, int? year, string imdbid, int? traktid, bool isWatched, string fanart, string onlineFanart)
        {
            if (!File.Exists(GUIGraphicsContext.Skin + @"\Trakt.Shouts.xml"))
            {
                // let user know they need to update skin or harass skin author
                GUIUtils.ShowOKDialog(GUIUtils.PluginName(), Translation.SkinOutOfDate);
                return;
            }

            MovieShout movieInfo = new MovieShout
            {
                ImdbId = imdbid,
                Title = title,
                Year = year
            };
            GUIShouts.ShoutType = GUIShouts.ShoutTypeEnum.movie;
            GUIShouts.MovieInfo = movieInfo;
            GUIShouts.Fanart = fanart;
            GUIShouts.OnlineFanart = onlineFanart;
            GUIShouts.IsWatched = isWatched;
            GUIWindowManager.ActivateWindow((int)TraktGUIWindows.Shouts);
        }

        #endregion

        #region Show Shouts

        public static void ShowTVShowShouts(TraktShowSummary show)
        {
            ShowTVShowShouts(show.Title, show.Ids.TvdbId, show.Ids.Id, show.IsWatched(), show.Images.Fanart.LocalImageFilename(ArtworkType.ShowFanart), TraktSettings.DownloadFullSizeFanart ? show.Images.Fanart.FullSize : show.Images.Fanart.MediumSize);
        }

        public static void ShowTVShowShouts(string title, int? tvdbid, int? traktid, bool isWatched, string fanart, string onlineFanart = null)
        {
            if (!File.Exists(GUIGraphicsContext.Skin + @"\Trakt.Shouts.xml"))
            {
                // let user know they need to update skin or harass skin author
                GUIUtils.ShowOKDialog(GUIUtils.PluginName(), Translation.SkinOutOfDate);
                return;
            }

            ShowShout seriesInfo = new ShowShout
            {
                TvdbId = tvdbid,
                TraktId = traktid,
                Title = title,
            };
            GUIShouts.ShoutType = GUIShouts.ShoutTypeEnum.show;
            GUIShouts.ShowInfo = seriesInfo;
            GUIShouts.Fanart = fanart;
            GUIShouts.IsWatched = isWatched;
            GUIShouts.OnlineFanart = onlineFanart;
            GUIWindowManager.ActivateWindow((int)TraktGUIWindows.Shouts);
        }

        #endregion

        #region Episode Shouts

        public static void ShowEpisodeShouts(TraktShowSummary show, TraktEpisodeSummary episode)
        {
            ShowEpisodeShouts(show.Title, show.Ids.TvdbId, show.Ids.Id, episode.Season, episode.Number, episode.IsWatched(show), show.Images.Fanart.LocalImageFilename(ArtworkType.ShowFanart), TraktSettings.DownloadFullSizeFanart ? show.Images.Fanart.FullSize : show.Images.Fanart.MediumSize);
        }

        public static void ShowEpisodeShouts(string title, string tvdbid, string season, string episode, bool isWatched, string fanart, string onlineFanart = null)
        {
            ShowEpisodeShouts(title, tvdbid.ToNullableInt32(), null, season.ToInt(), episode.ToInt(), isWatched, fanart, onlineFanart);
        }

        public static void ShowEpisodeShouts(string title, int? tvdbid, int? traktid, int season, int episode, bool isWatched, string fanart, string onlineFanart = null)
        {
            if (!File.Exists(GUIGraphicsContext.Skin + @"\Trakt.Shouts.xml"))
            {
                // let user know they need to update skin or harass skin author
                GUIUtils.ShowOKDialog(GUIUtils.PluginName(), Translation.SkinOutOfDate);
                return;
            }

            EpisodeShout episodeInfo = new EpisodeShout
            {
                TvdbId = tvdbid,
                TraktId = traktid,
                Title = title,
                SeasonIdx = season,
                EpisodeIdx = episode
            };

            GUIShouts.ShoutType = GUIShouts.ShoutTypeEnum.episode;
            GUIShouts.EpisodeInfo = episodeInfo;
            GUIShouts.Fanart = fanart;
            GUIShouts.OnlineFanart = onlineFanart;
            GUIShouts.IsWatched = isWatched;
            GUIWindowManager.ActivateWindow((int)TraktGUIWindows.Shouts);
        }

        #endregion

        #region Movie Watched History

        public static void AddMovieToWatchHistory(TraktMovie movie)
        {
            AddMovieToWatchHistory(movie.Title, movie.Year, movie.Ids.ImdbId, movie.Ids.TmdbId, movie.Ids.Id);
        }

        public static void AddMovieToWatchHistory(string title, int? year, string imdbid, int? tmdbid, int? traktid)
        {
            var movie = new TraktSyncMovieWatched
            {
                Ids = new TraktMovieId { ImdbId = imdbid, TmdbId = tmdbid, Id = traktid },
                Title = title,
                Year = year,
                WatchedAt = DateTime.UtcNow.ToISO8601()
            };

            var syncThread = new Thread((objMovie) =>
            {
                TraktAPI.TraktAPI.AddMovieToWatchedHistory(objMovie as TraktSyncMovieWatched);
            })
            {
                IsBackground = true,
                Name = "MarkWatched"
            };

            syncThread.Start(movie);
        }

        public static void RemoveMovieFromWatchHistory(TraktMovie movie)
        {
            RemoveMovieFromWatchHistory(movie.Title, movie.Year, movie.Ids.ImdbId, movie.Ids.TmdbId, movie.Ids.Id);
        }

        public static void RemoveMovieFromWatchHistory(string title, int? year, string imdbid, int? tmdbid, int? traktid)
        {
            var movie = new TraktMovie
            {
                Ids = new TraktMovieId { ImdbId = imdbid, TmdbId = tmdbid, Id = traktid },
                Title = title,
                Year = year
            };

            var syncThread = new Thread((objMovie) =>
            {
                var syncData = objMovie as TraktMovie;
                TraktAPI.TraktAPI.RemoveMovieFromWatchedHistory(syncData);
            })
            {
                IsBackground = true,
                Name = "MarkUnWatched"
            };

            syncThread.Start(movie);
        }

        #endregion

        #region Episode Watched History

        public static void AddEpisodeToWatchedHistory(TraktEpisode episode)
        {
            AddEpisodeToWatchedHistory(episode.Title, episode.Season, episode.Number, episode.Ids.TvdbId, episode.Ids.ImdbId, episode.Ids.TmdbId, episode.Ids.Id);
        }

        public static void AddEpisodeToWatchedHistory(string title, int season, int number, int? tvdbid, string imdbid, int? tmdbid, int? traktid)
        {
            var episode = new TraktSyncEpisodeWatched
            {
                Ids = new TraktEpisodeId
                {
                    Id = traktid,
                    ImdbId = imdbid,
                    TmdbId = tmdbid,
                    TvdbId = tvdbid
                },
                Title = title,
                Season = season,
                Number = number,
                WatchedAt = DateTime.UtcNow.ToISO8601()
            };

            var syncThread = new Thread((objSyncData) =>
            {
                var response = TraktAPI.TraktAPI.AddEpisodeToWatchedHistory(objSyncData as TraktSyncEpisodeWatched);
            })
            {
                IsBackground = true,
                Name = "MarkWatched"
            };

            syncThread.Start(episode);
        }

        public static void RemoveEpisodeFromWatchedHistory(TraktEpisode episode)
        {
            RemoveEpisodeFromWatchedHistory(episode.Title, episode.Season, episode.Number, episode.Ids.TvdbId, episode.Ids.ImdbId, episode.Ids.TmdbId, episode.Ids.Id);
        }

        public static void RemoveEpisodeFromWatchedHistory(string title, int season, int number, int? tvdbid, string imdbid, int? tmdbid, int? traktid)
        {
            var episode = new TraktEpisode
            {
                Ids = new TraktEpisodeId
                {
                    Id = traktid,
                    ImdbId = imdbid,
                    TmdbId = tmdbid,
                    TvdbId = tvdbid
                },
                Title = title,
                Season = season,
                Number = number
            };

            var syncThread = new Thread((objSyncData) =>
            {
                var response = TraktAPI.TraktAPI.RemoveEpisodeFromWatchedHistory(objSyncData as TraktEpisode);
            })
            {
                IsBackground = true,
                Name = "MarkUnWatched"
            };

            syncThread.Start(episode);
        }

        #endregion

        #region Movie Collection

        public static void AddMovieToCollection(TraktMovie movie)
        {
            AddMovieToLibrary(movie.Title, movie.Year, movie.Ids.ImdbId, movie.Ids.TmdbId, movie.Ids.Id);
        }

        public static void AddMovieToLibrary(string title, int? year, string imdbid, int? tmdbid, int? traktid)
        {
            var movie = new TraktSyncMovieCollected
            {
                Ids = new TraktMovieId
                {
                    Id = traktid,
                    ImdbId = imdbid,
                    TmdbId = tmdbid
                },
                Title = title,
                Year = year
            };

            var syncThread = new Thread((objSyncData) =>
            {
                var response = TraktAPI.TraktAPI.AddMovieToCollection(objSyncData as TraktSyncMovieCollected);
            })
            {
                IsBackground = true,
                Name = "AddCollection"
            };

            syncThread.Start(movie);
        }

        public static void RemoveMovieFromCollection(TraktMovie movie)
        {
            RemoveMovieFromLibrary(movie.Title, movie.Year, movie.Ids.ImdbId, movie.Ids.TmdbId, movie.Ids.Id);
        }

        public static void RemoveMovieFromLibrary(string title, int? year, string imdbid, int? tmdbid, int? traktid)
        {
            var movie = new TraktMovie
            {
                Ids = new TraktMovieId
                {
                    Id = traktid,
                    ImdbId = imdbid,
                    TmdbId = tmdbid
                },
                Title = title,
                Year = year
            };

            var syncThread = new Thread((objSyncData) =>
            {
                var response = TraktAPI.TraktAPI.RemoveMovieFromCollection(objSyncData as TraktMovie);
            })
            {
                IsBackground = true,
                Name = "RemoveCollection"
            };

            syncThread.Start(movie);
        }

        #endregion

        #region Episode Collection

        public static void AddEpisodeToCollection(TraktEpisode episode)
        {
            AddEpisodeToCollection(episode.Title, episode.Season, episode.Number, episode.Ids.TvdbId, episode.Ids.ImdbId, episode.Ids.TmdbId, episode.Ids.Id);
        }

        public static void AddEpisodeToCollection(string title, int season, int number, int? tvdbid, string imdbid, int? tmdbid, int? traktid)
        {
            var episode = new TraktSyncEpisodeCollected
            {
                Ids = new TraktEpisodeId
                {
                    Id = traktid,
                    ImdbId = imdbid,
                    TmdbId = tmdbid,
                    TvdbId = tvdbid
                },
                Title = title,
                Season = season,
                Number = number
            };

            var syncThread = new Thread((objSyncData) =>
            {
                var response = TraktAPI.TraktAPI.AddEpisodeToCollection(objSyncData as TraktSyncEpisodeCollected);
            })
            {
                IsBackground = true,
                Name = "AddCollection"
            };

            syncThread.Start(episode);
        }

        public static void RemoveEpisodeFromCollection(TraktEpisode episode)
        {
            RemoveEpisodeFromCollection(episode.Title, episode.Season, episode.Number, episode.Ids.TvdbId, episode.Ids.ImdbId, episode.Ids.TmdbId, episode.Ids.Id);
        }

        public static void RemoveEpisodeFromCollection(string title, int season, int number, int? tvdbid, string imdbid, int? tmdbid, int? traktid)
        {
            var episode = new TraktEpisode
            {
                Ids = new TraktEpisodeId
                {
                    Id = traktid,
                    ImdbId = imdbid,
                    TmdbId = tmdbid,
                    TvdbId = tvdbid
                },
                Title = title,
                Season = season,
                Number = number
            };

            var syncThread = new Thread((objSyncData) =>
            {
                var response = TraktAPI.TraktAPI.RemoveEpisodeFromCollection(objSyncData as TraktEpisode);
            })
            {
                IsBackground = true,
                Name = "RemoveCollection"
            };

            syncThread.Start(episode);
        }

        #endregion

        #endregion

        #region Internal Helpers

        #region Add / Remove List Items

        internal static void AddRemoveItemInList(string slug, TraktListItem item, bool remove)
        {
            AddRemoveItemInList(new List<string> { slug }, new List<TraktListItem>() { item }, remove);
        }

        internal static void AddRemoveItemInList(List<string> slugs, TraktListItem item, bool remove)
        {
            AddRemoveItemInList(slugs, new List<TraktListItem>() { item }, remove);
        }

        internal static void AddRemoveItemInList(List<string> slugs, List<TraktListItem> items, bool remove)
        {
            // TODO
            //Thread listThread = new Thread(delegate(object obj)
            //{
            //    foreach (var slug in slugs)
            //    {
            //        TraktList list = new TraktList
            //        {
            //            UserName = TraktSettings.Username,
            //            Password = TraktSettings.Password,
            //            Slug = slug,
            //            Items = items
            //        };

            //        TraktSyncResponse response = null;
            //        if (!remove)
            //        {
            //            response = TraktAPI.v1.TraktAPI.ListAddItems(list);
            //        }
            //        else
            //        {
            //            response = TraktAPI.v1.TraktAPI.ListDeleteItems(list);
            //        }

            //        TraktLogger.LogTraktResponse<TraktSyncResponse>(response);
            //        if (response.Status == "success")
            //        {
            //            // clear current items in any lists
            //            // list items will be refreshed online if we try to request them
            //            TraktLists.ClearItemsInList(TraktSettings.Username, slug);                                               

            //            // update MovingPictures Categories and Filters menu
            //            if (IsMovingPicturesAvailableAndEnabled)
            //            {
            //                // we need the name of the list so get list from slug first
            //                var userList = TraktLists.GetListForUser(TraktSettings.Username, slug);
            //                if (userList == null) continue;

            //                if (remove)
            //                {
            //                    MovingPictures.RemoveMovieCriteriaFromCustomlistNode(userList.Name, items.First().ImdbId);
            //                }
            //                else
            //                {
            //                    MovingPictures.AddMovieCriteriaToCustomlistNode(userList.Name, items.First().ImdbId);
            //                }
            //            }
            //        }
            //    }
            //})
            //{
            //    Name = remove ? "RemoveList" : "AddList",
            //    IsBackground = true
            //};

            //listThread.Start();
        }

        #endregion

        #endregion
    }
}
