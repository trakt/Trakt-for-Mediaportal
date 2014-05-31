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

        #region API Helpers

        #region Movie Watchlist

        public static void AddMovieToWatchList(TraktMovie movie, bool updateMovingPicturesFilters)
        {
            AddMovieToWatchList(movie.Title, movie.Year, movie.IMDBID, updateMovingPicturesFilters);
        }

        public static void AddMovieToWatchList(string title, string year)
        {
            AddMovieToWatchList(title, year, null);
        }

        public static void AddMovieToWatchList(string title, string year, string imdbid)
        {
            AddMovieToWatchList(title, year, imdbid, false);
        }

        public static void AddMovieToWatchList(string title, string year, bool updateMovingPicturesFilters)
        {
            AddMovieToWatchList(title, year, null, updateMovingPicturesFilters);
        }

        public static void AddMovieToWatchList(string title, string year, string imdbid, bool updateMovingPicturesFilters)
        {
            AddMovieToWatchList(title, year, imdbid, null, updateMovingPicturesFilters);
        }

        /// <summary>
        /// Adds a movie to the current users Watch List
        /// </summary>
        /// <param name="title">title of movie</param>
        /// <param name="year">year of movie</param>
        /// <param name="imdbid">imdbid of movie</param>
        /// <param name="updateMovingPicturesFilters">set to true if movingpictures categories/filters should also be updated</param>
        public static void AddMovieToWatchList(string title, string year, string imdbid, string tmdb, bool updateMovingPicturesFilters)
        {
            if (!GUICommon.CheckLogin(false)) return;

            TraktMovieSync syncObject = BasicHandler.CreateMovieSyncData(title, year, imdbid, tmdb);
            if (syncObject == null) return;

            Thread syncThread = new Thread(delegate(object obj)
            {
                TraktSyncResponse response = TraktAPI.TraktAPI.SyncMovieLibrary(obj as TraktMovieSync, TraktSyncModes.watchlist);
                if (response == null || response.Status != "success") return;
                if (updateMovingPicturesFilters && IsMovingPicturesAvailableAndEnabled)
                {
                    // Update Categories & Filters menu(s)                    
                    MovingPictures.AddMovieCriteriaToWatchlistNode(imdbid);
                }
                GUI.GUIWatchListMovies.ClearCache(TraktSettings.Username);
            })
            {
                IsBackground = true,
                Name = "AddWatchList"
            };

            syncThread.Start(syncObject);
        }

        public static void RemoveMovieFromWatchList(TraktMovie movie, bool updateMovingPicturesFilters)
        {
            RemoveMovieFromWatchList(movie.Title, movie.Year, movie.IMDBID, updateMovingPicturesFilters);
        }
        public static void RemoveMovieFromWatchList(string title, string year, string imdbid, bool updateMovingPicturesFilters)
        {
            if (!GUICommon.CheckLogin(false)) return;

            TraktMovieSync syncObject = BasicHandler.CreateMovieSyncData(title, year, imdbid);
            if (syncObject == null) return;

            Thread syncThread = new Thread(delegate(object obj)
            {
                TraktSyncResponse response = TraktAPI.TraktAPI.SyncMovieLibrary(obj as TraktMovieSync, TraktSyncModes.unwatchlist);
                if (response == null || response.Status != "success") return;
                if (updateMovingPicturesFilters && IsMovingPicturesAvailableAndEnabled)
                {
                    // Update Categories & Filters
                    MovingPictures.RemoveMovieCriteriaFromWatchlistNode(imdbid);
                }
                GUI.GUIWatchListMovies.ClearCache(TraktSettings.Username);
            })
            {
                IsBackground = true,
                Name = "RemoveWatchList"
            };

            syncThread.Start(syncObject);
        }
        #endregion

        #region Show WatchList
        public static void AddShowToWatchList(TraktShow show)
        {
            AddShowToWatchList(show.Title, show.Year.ToString(), show.Tvdb);
        }
        public static void AddShowToWatchList(string title, string year, string tvdbid)
        {
            TraktShowSync syncObject = BasicHandler.CreateShowSyncData(title, year, tvdbid);
            if (syncObject == null) return;

            Thread syncThread = new Thread(delegate(object obj)
            {
                TraktResponse response = TraktAPI.TraktAPI.SyncShowWatchList((obj as TraktShowSync), TraktSyncModes.watchlist);
                if (response == null || response.Status != "success") return;
                GUI.GUIWatchListShows.ClearCache(TraktSettings.Username);
            })
            {
                IsBackground = true,
                Name = "AddWatchList"
            };

            syncThread.Start(syncObject);
        }

        public static void RemoveShowFromWatchList(TraktShow show)
        {
            RemoveShowFromWatchList(show.Title, show.Year.ToString(), show.Tvdb);
        }
        public static void RemoveShowFromWatchList(string title, string year, string tvdbid)
        {
            TraktShowSync syncObject = BasicHandler.CreateShowSyncData(title, year, tvdbid);
            if (syncObject == null) return;

            Thread syncThread = new Thread(delegate(object obj)
            {
                TraktResponse response = TraktAPI.TraktAPI.SyncShowWatchList((obj as TraktShowSync), TraktSyncModes.unwatchlist);
                if (response == null || response.Status != "success") return;
                GUI.GUIWatchListShows.ClearCache(TraktSettings.Username);
            })
            {
                IsBackground = true,
                Name = "RemoveWatchList"
            };

            syncThread.Start(syncObject);
        }
        #endregion

        #region Episode WatchList
        public static void AddEpisodeToWatchList(TraktShow show, TraktEpisode episode)
        {
            AddEpisodeToWatchList(show.Title, show.Year.ToString(), show.Tvdb, episode.Season.ToString(), episode.Number.ToString());
        }
        public static void AddEpisodeToWatchList(string title, string year, string tvdbid, string seasonidx, string episodeidx)
        {
            TraktEpisodeSync syncObject = BasicHandler.CreateEpisodeSyncData(title, year, tvdbid, seasonidx, episodeidx);
            if (syncObject == null) return;

            Thread syncThread = new Thread(delegate(object obj)
            {
                TraktResponse response = TraktAPI.TraktAPI.SyncEpisodeWatchList((obj as TraktEpisodeSync), TraktSyncModes.watchlist);
                if (response == null || response.Status != "success") return;
            })
            {
                IsBackground = true,
                Name = "AddWatchList"
            };

            syncThread.Start(syncObject);
        }

        public static void RemoveEpisodeFromWatchList(TraktShow show, TraktEpisode episode)
        {
            RemoveEpisodeFromWatchList(show.Title, show.Year.ToString(), show.Tvdb, episode.Season.ToString(), episode.Number.ToString());
        }
        public static void RemoveEpisodeFromWatchList(string title, string year, string tvdbid, string seasonidx, string episodeidx)
        {
            TraktEpisodeSync syncObject = BasicHandler.CreateEpisodeSyncData(title, year, tvdbid, seasonidx, episodeidx);
            if (syncObject == null) return;

            Thread syncThread = new Thread(delegate(object obj)
            {
                TraktResponse response = TraktAPI.TraktAPI.SyncEpisodeWatchList((obj as TraktEpisodeSync), TraktSyncModes.unwatchlist);
                if (response == null || response.Status != "success") return;
            })
            {
                IsBackground = true,
                Name = "RemoveWatchList"
            };

            syncThread.Start(syncObject);
        }
        #endregion

        #region Add/Remove Movie in List
        public static void AddRemoveMovieInUserList(TraktMovie movie, bool remove)
        {
            AddRemoveMovieInUserList(TraktSettings.Username, movie.Title, movie.Year, movie.IMDBID, remove);
        }

        public static void AddRemoveMovieInUserList(string title, string year, string imdbid, bool remove)
        {
            AddRemoveMovieInUserList(TraktSettings.Username, title, year, imdbid, remove);
        }

        public static void AddRemoveMovieInUserList(string username, string title, string year, string imdbid, bool remove)
        {
            if (!GUICommon.CheckLogin(false)) return;

            GUIBackgroundTask.Instance.ExecuteInBackgroundAndCallback(() =>
            {
                return TraktLists.GetListsForUser(username);
            },
            delegate(bool success, object result)
            {
                if (success)
                {
                    IEnumerable<TraktUserList> customlists = result as IEnumerable<TraktUserList>;

                    // get slug of lists selected
                    List<string> slugs = TraktLists.GetUserListSelections(customlists.ToList());
                    if (slugs == null || slugs.Count == 0) return;

                    TraktListItem item = new TraktListItem
                    {
                        Type = TraktItemType.movie.ToString(),
                        Title = title,
                        Year = Convert.ToInt32(year),
                        ImdbId = imdbid
                    };

                    AddRemoveItemInList(slugs, item, remove);
                }
            }, Translation.GettingLists, true);
        }

        #endregion

        #region Add/Remove Show in List
        public static void AddRemoveShowInUserList(TraktShow show, bool remove)
        {
            AddRemoveShowInUserList(TraktSettings.Username, show.Title, show.Year.ToString(), show.Tvdb, remove);
        }

        public static void AddRemoveShowInUserList(string title, string year, string tvdbid, bool remove)
        {
            AddRemoveShowInUserList(TraktSettings.Username, title, year, tvdbid, remove);
        }

        public static void AddRemoveShowInUserList(string username, string title, string year, string tvdbid, bool remove)
        {
            if (!GUICommon.CheckLogin(false)) return;

            GUIBackgroundTask.Instance.ExecuteInBackgroundAndCallback(() =>
            {
                return TraktLists.GetListsForUser(username);
            },
            delegate(bool success, object result)
            {
                if (success)
                {
                    IEnumerable<TraktUserList> customlists = result as IEnumerable<TraktUserList>;

                    // get slug of lists selected
                    List<string> slugs = TraktLists.GetUserListSelections(customlists.ToList());
                    if (slugs == null || slugs.Count == 0) return;

                    TraktListItem item = new TraktListItem
                    {
                        Type = TraktItemType.show.ToString(),
                        Title = title,
                        Year = string.IsNullOrEmpty(year) ? 0 : Convert.ToInt32(year),
                        TvdbId = tvdbid
                    };

                    AddRemoveItemInList(slugs, item, remove);
                }
            }, Translation.GettingLists, true);
        }

        #endregion

        #region Add/Remove Season in List

        public static void AddRemoveSeasonInUserList(string title, string year, string season, string tvdbid, bool remove)
        {
            AddRemoveSeasonInUserList(TraktSettings.Username, title, year, season, tvdbid, remove);
        }

        public static void AddRemoveSeasonInUserList(string username, string title, string year, string season, string tvdbid, bool remove)
        {
            if (!GUICommon.CheckLogin(false)) return;

            GUIBackgroundTask.Instance.ExecuteInBackgroundAndCallback(() =>
            {
                return TraktLists.GetListsForUser(username);
            },
            delegate(bool success, object result)
            {
                if (success)
                {
                    IEnumerable<TraktUserList> customlists = result as IEnumerable<TraktUserList>;

                    // get slug of lists selected
                    List<string> slugs = TraktLists.GetUserListSelections(customlists.ToList());
                    if (slugs == null || slugs.Count == 0) return;

                    TraktListItem item = new TraktListItem
                    {
                        Type = TraktItemType.season.ToString(),
                        Title = title,
                        Year = Convert.ToInt32(year),
                        Season = Convert.ToInt32(season),
                        TvdbId = tvdbid
                    };

                    AddRemoveItemInList(slugs, item, remove);
                }
            }, Translation.GettingLists, true);
        }

        #endregion

        #region Add/Remove Episode in List
        public static void AddRemoveEpisodeInUserList(TraktShow show, TraktEpisode episode, bool remove)
        {
            AddRemoveEpisodeInUserList(TraktSettings.Username, show.Title, show.Year.ToString(), episode.Season.ToString(), episode.Number.ToString(), show.Tvdb, remove);
        }

        public static void AddRemoveEpisodeInUserList(string title, string year, string season, string episode, string tvdbid, bool remove)
        {
            AddRemoveEpisodeInUserList(TraktSettings.Username, title, year, season, episode, tvdbid, remove);
        }

        public static void AddRemoveEpisodeInUserList(string username, string title, string year, string season, string episode, string tvdbid, bool remove)
        {
            if (!GUICommon.CheckLogin(false)) return;

            GUIBackgroundTask.Instance.ExecuteInBackgroundAndCallback(() =>
            {
                return TraktLists.GetListsForUser(username);
            },
            delegate(bool success, object result)
            {
                if (success)
                {
                    IEnumerable<TraktUserList> customlists = result as IEnumerable<TraktUserList>;

                    // get slug of lists selected
                    List<string> slugs = TraktLists.GetUserListSelections(customlists.ToList());
                    if (slugs == null || slugs.Count == 0) return;

                    TraktListItem item = new TraktListItem
                    {
                        Type = TraktItemType.episode.ToString(),
                        Title = title,
                        Year = Convert.ToInt32(year),
                        Season = Convert.ToInt32(season),
                        Episode = Convert.ToInt32(episode),
                        TvdbId = tvdbid
                    };

                    AddRemoveItemInList(slugs, item, remove);
                }
            }, Translation.GettingLists, true);
        }

        #endregion

        #region Related Movies
        public static void ShowRelatedMovies(TraktMovie movie)
        {
            ShowRelatedMovies(movie.IMDBID, movie.Title, movie.Year);
        }
        public static void ShowRelatedMovies(string imdbid, string title, string year)
        {
            if (!File.Exists(GUIGraphicsContext.Skin + @"\Trakt.Related.Movies.xml"))
            {
                // let user know they need to update skin or harass skin author
                GUIUtils.ShowOKDialog(GUIUtils.PluginName(), Translation.SkinOutOfDate);
                return;
            }
           
            RelatedMovie relatedMovie = new RelatedMovie
            {
                IMDbId = imdbid,
                Title = title,
                Year = Convert.ToInt32(string.IsNullOrEmpty(year) ? "0" : year)
            };
            GUIRelatedMovies.relatedMovie = relatedMovie;
            GUIWindowManager.ActivateWindow((int)TraktGUIWindows.RelatedMovies);
        }
        #endregion

        #region Related Shows
        public static void ShowRelatedShows(TraktShow show)
        {
            ShowRelatedShows(show.Tvdb, show.Title);
        }
        public static void ShowRelatedShows(string tvdbid, string title)
        {
            if (!File.Exists(GUIGraphicsContext.Skin + @"\Trakt.Related.Shows.xml"))
            {
                // let user know they need to update skin or harass skin author
                GUIUtils.ShowOKDialog(GUIUtils.PluginName(), Translation.SkinOutOfDate);
                return;
            }

            RelatedShow relatedShow = new RelatedShow
            {
                TVDbId = tvdbid,
                Title = title                
            };
            GUIRelatedShows.relatedShow = relatedShow;
            GUIWindowManager.ActivateWindow((int)TraktGUIWindows.RelatedShows);
        }
        #endregion

        #region Movie Shouts
        public static void ShowMovieShouts(TraktMovie movie)
        {
            ShowMovieShouts(movie.IMDBID, movie.Title, movie.Year, movie.Watched, movie.Images.Fanart.LocalImageFilename(ArtworkType.MovieFanart), movie.Images.Fanart);
        }
        public static void ShowMovieShouts(string imdb, string title, string year, string fanart)
        {
            ShowMovieShouts(imdb, title, year, fanart, null);
        }
        public static void ShowMovieShouts(string imdb, string title, string year, bool isWatched, string fanart)
        {
            ShowMovieShouts(imdb, title, year, false, fanart, null);
        }
        public static void ShowMovieShouts(string imdb, string title, string year, string fanart, string onlineFanart = null)
        {
            ShowMovieShouts(imdb, title, year, false, fanart, onlineFanart);
        }
        public static void ShowMovieShouts(string imdb, string title, string year, bool isWatched, string fanart, string onlineFanart)
        {
            if (!File.Exists(GUIGraphicsContext.Skin + @"\Trakt.Shouts.xml"))
            {
                // let user know they need to update skin or harass skin author
                GUIUtils.ShowOKDialog(GUIUtils.PluginName(), Translation.SkinOutOfDate);
                return;
            }

            MovieShout movieInfo = new MovieShout
            {
                IMDbId = imdb,
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
        public static void ShowTVShowShouts(TraktShow show)
        {
            ShowTVShowShouts(show.Tvdb, show.Title, show.Images.Fanart.LocalImageFilename(ArtworkType.ShowFanart), show.Images.Fanart);
        }
        public static void ShowTVShowShouts(string tvdb, string title, string fanart, string onlineFanart = null)
        {
            if (!File.Exists(GUIGraphicsContext.Skin + @"\Trakt.Shouts.xml"))
            {
                // let user know they need to update skin or harass skin author
                GUIUtils.ShowOKDialog(GUIUtils.PluginName(), Translation.SkinOutOfDate);
                return;
            }

            ShowShout seriesInfo = new ShowShout
            {
                TVDbId = tvdb,
                Title = title,
            };
            GUIShouts.ShoutType = GUIShouts.ShoutTypeEnum.show;
            GUIShouts.ShowInfo = seriesInfo;
            GUIShouts.Fanart = fanart;
            GUIShouts.OnlineFanart = onlineFanart;
            GUIWindowManager.ActivateWindow((int)TraktGUIWindows.Shouts);
        }
        #endregion

        #region Episode Shouts
        public static void ShowEpisodeShouts(TraktShow show, TraktEpisode episode)
        {
            ShowEpisodeShouts(show.Tvdb, show.Title, episode.Season.ToString(), episode.Number.ToString(), episode.Watched, show.Images.Fanart.LocalImageFilename(ArtworkType.ShowFanart), show.Images.Fanart);
        }
        public static void ShowEpisodeShouts(string tvdb, string title, string season, string episode, string fanart, string onlineFanart = null)
        {
            ShowEpisodeShouts(tvdb, title, season, episode, false, fanart, onlineFanart);
        }
        public static void ShowEpisodeShouts(string tvdb, string title, string season, string episode, bool isWatched, string fanart, string onlineFanart = null)
        {
            if (!File.Exists(GUIGraphicsContext.Skin + @"\Trakt.Shouts.xml"))
            {
                // let user know they need to update skin or harass skin author
                GUIUtils.ShowOKDialog(GUIUtils.PluginName(), Translation.SkinOutOfDate);
                return;
            }

            EpisodeShout episodeInfo = new EpisodeShout
            {
                TVDbId = tvdb,
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

        #region Movie Watched/UnWatched
        public static void MarkMovieAsWatched(TraktMovie movie)
        {
            MarkMovieAsWatched(movie.IMDBID, movie.Title, movie.Year, movie.TMDBID);
        }
        public static void MarkMovieAsWatched(string imdbid, string title, string year, string tmdbid = null)
        {
            TraktMovieSync syncObject = BasicHandler.CreateMovieSyncData(title, year, imdbid, tmdbid);
            if (syncObject == null) return;

            Thread syncThread = new Thread(delegate(object obj)
            {
                TraktAPI.TraktAPI.SyncMovieLibrary(obj as TraktMovieSync, TraktSyncModes.seen);
            })
            {
                IsBackground = true,
                Name = "MarkWatched"
            };

            syncThread.Start(syncObject);
        }

        public static void MarkMovieAsUnWatched(TraktMovie movie)
        {
            MarkMovieAsUnWatched(movie.IMDBID, movie.Title, movie.Year, movie.TMDBID);
        }
        public static void MarkMovieAsUnWatched(string imdbid, string title, string year, string tmdbid = null)
        {
            TraktMovieSync syncObject = BasicHandler.CreateMovieSyncData(title, year, imdbid, tmdbid);
            if (syncObject == null) return;

            Thread syncThread = new Thread(delegate(object obj)
            {
                TraktAPI.TraktAPI.SyncMovieLibrary(obj as TraktMovieSync, TraktSyncModes.unseen);
            })
            {
                IsBackground = true,
                Name = "MarkUnWatched"
            };

            syncThread.Start(syncObject);
        }
        #endregion

        #region Episode Watched/UnWatched
        public static void MarkEpisodeAsWatched(TraktShow show, TraktEpisode episode)
        {
            MarkEpisodeAsWatched(show.Title, show.Year.ToString(), show.Tvdb, episode.Season.ToString(), episode.Number.ToString());
        }
        public static void MarkEpisodeAsWatched(string title, string year, string tvdbid, string seasonidx, string episodeidx)
        {
            TraktEpisodeSync syncObject = BasicHandler.CreateEpisodeSyncData(title, year, tvdbid, seasonidx, episodeidx);
            if (syncObject == null) return;

            Thread syncThread = new Thread(delegate(object obj)
            {
                TraktResponse response = TraktAPI.TraktAPI.SyncEpisodeWatchList((obj as TraktEpisodeSync), TraktSyncModes.seen);
                if (response == null || response.Status != "success") return;
            })
            {
                IsBackground = true,
                Name = "MarkWatched"
            };

            syncThread.Start(syncObject);
        }

        public static void MarkEpisodeAsUnWatched(TraktShow show, TraktEpisode episode)
        {
            MarkEpisodeAsUnWatched(show.Title, show.Year.ToString(), show.Tvdb, episode.Season.ToString(), episode.Number.ToString());
        }
        public static void MarkEpisodeAsUnWatched(string title, string year, string tvdbid, string seasonidx, string episodeidx)
        {
            TraktEpisodeSync syncObject = BasicHandler.CreateEpisodeSyncData(title, year, tvdbid, seasonidx, episodeidx);
            if (syncObject == null) return;

            Thread syncThread = new Thread(delegate(object obj)
            {
                TraktResponse response = TraktAPI.TraktAPI.SyncEpisodeWatchList((obj as TraktEpisodeSync), TraktSyncModes.unseen);
                if (response == null || response.Status != "success") return;
            })
            {
                IsBackground = true,
                Name = "MarkUnWatched"
            };

            syncThread.Start(syncObject);
        }
        #endregion

        #region Movie Library/UnLibrary
        public static void AddMovieToLibrary(TraktMovie movie)
        {
            AddMovieToLibrary(movie.IMDBID, movie.Title, movie.Year, movie.TMDBID);
        }
        public static void AddMovieToLibrary(string imdbid, string title, string year, string tmdbid = null)
        {
            TraktMovieSync syncObject = BasicHandler.CreateMovieSyncData(title, year, imdbid);
            if (syncObject == null) return;

            Thread syncThread = new Thread(delegate(object obj)
            {
                TraktAPI.TraktAPI.SyncMovieLibrary(obj as TraktMovieSync, TraktSyncModes.library);
            })
            {
                IsBackground = true,
                Name = "AddLibrary"
            };

            syncThread.Start(syncObject);
        }

        public static void RemoveMovieFromLibrary(TraktMovie movie)
        {
            RemoveMovieFromLibrary(movie.IMDBID, movie.Title, movie.Year, movie.TMDBID);
        }
        public static void RemoveMovieFromLibrary(string imdbid, string title, string year, string tmdbid = null)
        {
            TraktMovieSync syncObject = BasicHandler.CreateMovieSyncData(title, year, imdbid);
            if (syncObject == null) return;

            Thread syncThread = new Thread(delegate(object obj)
            {
                TraktAPI.TraktAPI.SyncMovieLibrary(obj as TraktMovieSync, TraktSyncModes.unlibrary);
            })
            {
                IsBackground = true,
                Name = "RemoveLibrary"
            };

            syncThread.Start(syncObject);
        }
        #endregion

        #region Episode Library/UnLibrary
        public static void AddEpisodeToLibrary(TraktShow show, TraktEpisode episode)
        {
            AddEpisodeToLibrary(show.Title, show.Year.ToString(), show.Tvdb, episode.Season.ToString(), episode.Number.ToString());
        }
        public static void AddEpisodeToLibrary(string title, string year, string tvdbid, string seasonidx, string episodeidx)
        {
            TraktEpisodeSync syncObject = BasicHandler.CreateEpisodeSyncData(title, year, tvdbid, seasonidx, episodeidx);
            if (syncObject == null) return;

            Thread syncThread = new Thread(delegate(object obj)
            {
                TraktResponse response = TraktAPI.TraktAPI.SyncEpisodeWatchList((obj as TraktEpisodeSync), TraktSyncModes.library);
                if (response == null || response.Status != "success") return;
            })
            {
                IsBackground = true,
                Name = "AddLibrary"
            };

            syncThread.Start(syncObject);
        }

        public static void RemoveEpisodeFromLibrary(TraktShow show, TraktEpisode episode)
        {
            RemoveEpisodeFromLibrary(show.Title, show.Year.ToString(), show.Tvdb, episode.Season.ToString(), episode.Number.ToString());
        }
        public static void RemoveEpisodeFromLibrary(string title, string year, string tvdbid, string seasonidx, string episodeidx)
        {
            TraktEpisodeSync syncObject = BasicHandler.CreateEpisodeSyncData(title, year, tvdbid, seasonidx, episodeidx);
            if (syncObject == null) return;

            Thread syncThread = new Thread(delegate(object obj)
            {
                TraktResponse response = TraktAPI.TraktAPI.SyncEpisodeWatchList((obj as TraktEpisodeSync), TraktSyncModes.unlibrary);
                if (response == null || response.Status != "success") return;
            })
            {
                IsBackground = true,
                Name = "RemoveLibrary"
            };

            syncThread.Start(syncObject);
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
            Thread listThread = new Thread(delegate(object obj)
            {
                foreach (var slug in slugs)
                {
                    TraktList list = new TraktList
                    {
                        UserName = TraktSettings.Username,
                        Password = TraktSettings.Password,
                        Slug = slug,
                        Items = items
                    };

                    TraktSyncResponse response = null;
                    if (!remove)
                    {
                        response = TraktAPI.TraktAPI.ListAddItems(list);
                    }
                    else
                    {
                        response = TraktAPI.TraktAPI.ListDeleteItems(list);
                    }

                    TraktLogger.LogTraktResponse<TraktSyncResponse>(response);
                    if (response.Status == "success")
                    {
                        // clear current items in any lists
                        // list items will be refreshed online if we try to request them
                        TraktLists.ClearItemsInList(TraktSettings.Username, slug);                                               

                        // update MovingPictures Categories and Filters menu
                        if (IsMovingPicturesAvailableAndEnabled)
                        {
                            // we need the name of the list so get list from slug first
                            var userList = TraktLists.GetListForUser(TraktSettings.Username, slug);
                            if (userList == null) continue;

                            if (remove)
                            {
                                MovingPictures.RemoveMovieCriteriaFromCustomlistNode(userList.Name, items.First().ImdbId);
                            }
                            else
                            {
                                MovingPictures.AddMovieCriteriaToCustomlistNode(userList.Name, items.First().ImdbId);
                            }
                        }
                    }
                }
            })
            {
                Name = remove ? "RemoveList" : "AddList",
                IsBackground = true
            };

            listThread.Start();
        }

        #endregion

        #endregion
    }
}
