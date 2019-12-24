using MediaPortal.Configuration;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using TraktPlugin.Extensions;
using TraktPlugin.TmdbAPI.DataStructures;
using TraktPlugin.TmdbAPI.Extensions;

namespace TraktPlugin.Cache
{
    public static class TmdbCache
    {
        static string MovieCacheFile = Path.Combine(Config.GetFolder(Config.Dir.Config), string.Format(@"Trakt\TmdbCache\Movies.json"));
        static string ShowCacheFile = Path.Combine(Config.GetFolder(Config.Dir.Config), string.Format(@"Trakt\TmdbCache\Shows.json"));
        static string SeasonCacheFile = Path.Combine(Config.GetFolder(Config.Dir.Config), string.Format(@"Trakt\TmdbCache\Seasons.json"));
        static string EpisodeCacheFile = Path.Combine(Config.GetFolder(Config.Dir.Config), @"Trakt\TmdbCache\Episodes.json");        
        static string PersonCacheFile = Path.Combine(Config.GetFolder(Config.Dir.Config), @"Trakt\TmdbCache\People.json");
                
        // Create thread-safe dictionaries to cache images for each type
        // ID's are unique for each type only, seasons and episodes require an extra key to be unique
        static ConcurrentDictionary<int?, TmdbMovieImages> Movies = null;
        static ConcurrentDictionary<int?, TmdbShowImages> Shows = null;
        static ConcurrentDictionary<Tuple<int?, int, int>, TmdbEpisodeImages> Episodes = null;
        static ConcurrentDictionary<Tuple<int?, int>, TmdbSeasonImages> Seasons = null;
        static ConcurrentDictionary<int?, TmdbPeopleImages> People = null;
        
        public static void Init()
        {
            TraktLogger.Info("Loading TMDb request cache");

            // load cached images from files and convert them to a thread-safe dictionary keyed by ID (and season/episode)
            var movies = LoadFileCache(MovieCacheFile, "[]").FromJSONArray<TmdbMovieImages>().ToList();
            Movies = new ConcurrentDictionary<int?, TmdbMovieImages>(movies.Distinct().ToDictionary(m => m.Id));
            
            var shows = LoadFileCache(ShowCacheFile, "[]").FromJSONArray<TmdbShowImages>().ToList();
            Shows = new ConcurrentDictionary<int?, TmdbShowImages>(shows.Distinct().ToDictionary(s => s.Id));

            var seasons = LoadFileCache(SeasonCacheFile, "[]").FromJSONArray<TmdbSeasonImages>().ToList();
            Seasons = new ConcurrentDictionary<Tuple<int?, int>, TmdbSeasonImages>(seasons.Distinct().ToDictionary(s => Tuple.Create(s.Id, s.Season)));

            var episodes = LoadFileCache(EpisodeCacheFile, "[]").FromJSONArray<TmdbEpisodeImages>().ToList();
            Episodes = new ConcurrentDictionary<Tuple<int?, int, int>, TmdbEpisodeImages>(episodes.Distinct().ToDictionary(e => Tuple.Create(e.Id, e.Season, e.Episode)));

            var people = LoadFileCache(PersonCacheFile, "[]").FromJSONArray<TmdbPeopleImages>().ToList();
            People = new ConcurrentDictionary<int?, TmdbPeopleImages>(people.Distinct().ToDictionary(p => p.Id));

            // get updated configuration from TMDb
            GetTmdbConfiguration();
        }

        public static void DeInit()
        {
            TraktLogger.Info("Saving TMDb request cache");

            SaveFileCache(MovieCacheFile, Movies.Values.ToList().ToJSON());
            SaveFileCache(ShowCacheFile, Shows.Values.ToList().ToJSON());
            SaveFileCache(SeasonCacheFile, Seasons.Values.ToList().ToJSON());
            SaveFileCache(EpisodeCacheFile, Episodes.Values.ToList().ToJSON());
            SaveFileCache(PersonCacheFile, People.Values.ToList().ToJSON());
        }

        static void GetTmdbConfiguration()
        {
            var tmdbConfigThread = new Thread((o) =>
            {
                // determine age of the last requested TMDb configuration
                // if older than 2 weeks request again, should rarily change.

                DateTime lastRequestedDate = TraktSettings.TmdbConfigurationAge.ToDateTime();
                if (TraktSettings.TmdbConfiguration == null || TraktSettings.TmdbConfiguration.Images == null || DateTime.Now.Subtract(new TimeSpan(14, 0, 0, 0, 0)) > lastRequestedDate)
                {
                    var latestConfig = TmdbAPI.TmdbAPI.GetConfiguration();
                    if (latestConfig != null && latestConfig.Images != null && latestConfig.Images.BaseUrl != null)
                    {
                        TraktSettings.TmdbConfiguration = latestConfig;
                    }
                    else
                    {
                        TraktSettings.TmdbConfiguration = new TmdbConfiguration
                        {
                            Images = new TmdbConfiguration.ImageConfiguration { BaseUrl = "http://image.tmdb.org/t/p/" }
                        };
                    }
                    TraktSettings.TmdbConfigurationAge = DateTime.Now.ToString();
                }
            })
            {
                IsBackground = true,
                Name = "GetTmdbConfiguration"
            };

            tmdbConfigThread.Start();
        }

        #region File IO

        static void SaveFileCache(string filename, string value)
        {
            if (value == null)
                return;
            
            TraktLogger.Debug("Saving file to disk. Filename = '{0}'", filename);

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filename));
                File.WriteAllText(filename, value, Encoding.UTF8);
            }
            catch (Exception e)
            {
                TraktLogger.Error(string.Format("Error saving file. Filename = '{0}', Error = '{1}'", filename, e.Message));
            }
        }

        static string LoadFileCache(string filename, string defaultValue)
        {
            string returnValue = defaultValue;

            try
            {
                if (File.Exists(filename))
                {
                    TraktLogger.Debug("Loading file from disk. Filename = '{0}'", filename);
                    returnValue = File.ReadAllText(filename, Encoding.UTF8);
                    if (string.IsNullOrEmpty(returnValue) || returnValue.StartsWith("\0"))
                    {
                        TraktLogger.Warning("Unexpected contents in file '{0}', restoring default value", filename);
                        returnValue = defaultValue;
                    }
                }
            }
            catch (Exception e)
            {
                TraktLogger.Error(string.Format("Error loading file from disk. Filename = '{0}', Error = '{1}'", filename, e.Message));
                return defaultValue;
            }

            return returnValue;
        }

        #endregion

        #region Movies

        public static TmdbMovieImages GetMovieImages(int? id, bool forceUpdate = false)
        {
            if (id == null) return null;

            // if its in our cache return it
            TmdbMovieImages movieImages;
            if (Movies.TryGetValue(id, out movieImages))
            {
                if (forceUpdate)
                    return movieImages;

                // but only if the request is not very old
                if (DateTime.Now.Subtract(new TimeSpan(TraktSettings.TmdbMovieImageMaxCacheAge, 0, 0, 0, 0)) < movieImages.RequestAge.ToDateTime())
                {
                    return movieImages;
                }

                TraktLogger.Info("Movie image cache expired. TMDb ID = '{0}', Request Age = '{1}'", id, movieImages.RequestAge);
                RemoveMovieImagesFromCache(movieImages);
            }
            
            // get movie images from tmdb and add to the cache
            movieImages = TmdbAPI.TmdbAPI.GetMovieImages(id.ToString());
            AddMovieImagesToCache(movieImages);

            return movieImages;
        }

        public static string GetMoviePosterFilename(TmdbMovieImages images)
        {
            if (images == null || images.Posters == null)
                return null;

            var moviePoster = images.Posters.LocalisedImage();
            if (moviePoster == null)
                return null;

            // create filename based on desired resolution
            return Path.Combine(Config.GetFolder(Config.Dir.Thumbs), @"Trakt\Movies\Posters\") + 
                                images.Id + "_" + TraktSettings.TmdbPreferredPosterSize + "_" + moviePoster.FilePath.TrimStart('/');
        }

        public static string GetMoviePosterUrl(TmdbMovieImages images)
        {
            if (images == null || images.Posters == null)
                return null;

            var moviePoster = images.Posters.LocalisedImage();
            if (moviePoster == null)
                return null;

            // return the desired resolution
            return TraktSettings.TmdbConfiguration.Images.BaseUrl + TraktSettings.TmdbPreferredPosterSize + moviePoster.FilePath;
        }

        public static string GetMovieBackdropFilename(TmdbMovieImages images)
        {
            if (images == null || images.Backdrops == null)
                return null;

            var movieBackdrop = images.Backdrops.FirstOrDefault();
            if (movieBackdrop == null)
                return null;

            // create filename based on desired resolution
            return Path.Combine(Config.GetFolder(Config.Dir.Thumbs), @"Trakt\Movies\Backdrops\") +
                                images.Id + "_" + (TraktSettings.DownloadFullSizeFanart ? "original" : TraktSettings.TmdbPreferredBackdropSize) + "_" + movieBackdrop.FilePath.TrimStart('/');
        }

        public static string GetMovieBackdropUrl(TmdbMovieImages images)
        {
            if (images == null || images.Backdrops == null)
                return null;

            var movieBackdrop = images.Backdrops.FirstOrDefault();
            if (movieBackdrop == null)
                return null;

            // return the desired resolution
            return TraktSettings.TmdbConfiguration.Images.BaseUrl + (TraktSettings.DownloadFullSizeFanart ? "original" : TraktSettings.TmdbPreferredBackdropSize) + movieBackdrop.FilePath;
        }

        static void AddMovieImagesToCache(TmdbMovieImages images)
        {
            if (images != null)
            {
                images.RequestAge = DateTime.Now.ToString();
                Movies.TryAdd(images.Id, images);
            }
        }

        static void RemoveMovieImagesFromCache(TmdbMovieImages images)
        {
            if (images != null)
            {
                TmdbMovieImages ignored;
                Movies.TryRemove(images.Id, out ignored);
            }
        }

        #endregion

        #region Shows

        public static TmdbShowImages GetShowImages(int? id, bool forceUpdate = false)
        {
            if (id == null) return null;

            // if its in our cache return it
            TmdbShowImages showImages;
            if (Shows.TryGetValue(id, out showImages))
            {
                if (forceUpdate)
                    return showImages;

                // but only if the request is not very old
                if (DateTime.Now.Subtract(new TimeSpan(TraktSettings.TmdbShowImageMaxCacheAge, 0, 0, 0, 0)) < showImages.RequestAge.ToDateTime())
                {
                    return showImages;
                }

                TraktLogger.Info("Show image cache expired. TMDb ID = '{0}', Request Age = '{1}'", id, showImages.RequestAge);
                RemoveShowImagesFromCache(showImages);
            }

            // get movie images from tmdb and add to the cache
            showImages = TmdbAPI.TmdbAPI.GetShowImages(id.ToString());
            AddShowImagesToCache(showImages);

            return showImages;
        }

        public static string GetShowPosterFilename(TmdbShowImages images)
        {
            if (images == null || images.Posters == null)
                return null;

            var showPoster = images.Posters.LocalisedImage();
            if (showPoster == null)
                return null;

            // create filename based on desired resolution
            return Path.Combine(Config.GetFolder(Config.Dir.Thumbs), @"Trakt\Shows\Posters\") +
                                images.Id + "_" + TraktSettings.TmdbPreferredPosterSize + "_" + showPoster.FilePath.TrimStart('/');
        }

        public static string GetShowPosterUrl(TmdbShowImages images)
        {
            if (images == null || images.Posters == null)
                return null;

            var showPoster = images.Posters.LocalisedImage();
            if (showPoster == null)
                return null;

            // return the desired resolution
            return TraktSettings.TmdbConfiguration.Images.BaseUrl + TraktSettings.TmdbPreferredPosterSize + showPoster.FilePath;
        }

        public static string GetShowBackdropFilename(TmdbShowImages images, bool logo = false)
        {
            if (images == null || images.Backdrops == null)
                return null;

            string languagePath = string.Empty;
            TmdbImage showBackdrop = null;

            if (logo)
            {
                // get the highest rated backdrop with a language
                showBackdrop = images.Backdrops.LocalisedImage();
            }
            else
            {
                showBackdrop = images.Backdrops.FirstOrDefault();
            }

            if (showBackdrop == null)
                return null;

            // create filename based on desired resolution
            return Path.Combine(Config.GetFolder(Config.Dir.Thumbs), @"Trakt\Shows\Backdrops\") +
                images.Id + "_" + (TraktSettings.DownloadFullSizeFanart ? "original" : TraktSettings.TmdbPreferredBackdropSize) + "_" + showBackdrop.FilePath.TrimStart('/');
        }

        public static string GetShowBackdropUrl(TmdbShowImages images, bool logo = false)
        {
            if (images == null || images.Backdrops == null)
                return null;

            TmdbImage showBackdrop = null;

            if (logo)
            {
                // get the highest rated backdrop with a language
                showBackdrop = images.Backdrops.LocalisedImage();
            }
            else
            {
                showBackdrop = images.Backdrops.FirstOrDefault();
            }

            if (showBackdrop == null)
                return null;

            // return the desired resolution
            return TraktSettings.TmdbConfiguration.Images.BaseUrl + (TraktSettings.DownloadFullSizeFanart ? "original" : TraktSettings.TmdbPreferredBackdropSize) + showBackdrop.FilePath;
        }

        static void AddShowImagesToCache(TmdbShowImages images)
        {
            if (images != null)
            {
                images.RequestAge = DateTime.Now.ToString();
                Shows.TryAdd(images.Id, images);
            }
        }

        static void RemoveShowImagesFromCache(TmdbShowImages images)
        {
            if (images != null)
            {
                TmdbShowImages ignored;
                Shows.TryRemove(images.Id, out ignored);
            }
        }

        #endregion

        #region Episodes

        public static TmdbEpisodeImages GetEpisodeImages(int? id, int season, int episode, bool forceUpdate = false)
        {
            if (id == null) return null;

            // if its in our cache return it
            TmdbEpisodeImages episodeImages;
            if (Episodes.TryGetValue(Tuple.Create(id, season, episode), out episodeImages))
            {
                if (forceUpdate)
                    return episodeImages;

                // but only if the request is not very old
                if (DateTime.Now.Subtract(new TimeSpan(TraktSettings.TmdbEpisodeImageMaxCacheAge, 0, 0, 0, 0)) < episodeImages.RequestAge.ToDateTime())
                {
                    return episodeImages;
                }

                TraktLogger.Info("Episode image cache expired. TMDb ID = '{0}', Season = '{1}', Episode = '{2}', Request Age = '{3}'", id, season, episode, episodeImages.RequestAge);
                RemoveEpisodeImagesFromCache(episodeImages);
            }

            // get movie images from tmdb and add to the cache            
            episodeImages = TmdbAPI.TmdbAPI.GetEpisodeImages(id.ToString(), season, episode);
            AddEpisodeImagesToCache(episodeImages);

            return episodeImages;
        }

        public static string GetEpisodeThumbFilename(TmdbEpisodeImages images)
        {
            if (images == null || images.Stills == null)
                return null;

            var episodeThumb = images.Stills.FirstOrDefault();
            if (episodeThumb == null)
                return null;

            // create filename based on desired resolution
            return Path.Combine(Config.GetFolder(Config.Dir.Thumbs), @"Trakt\Episodes\Thumbs\") +
                                images.Id + "_" + TraktSettings.TmdbPreferredPosterSize + "_" + episodeThumb.FilePath.TrimStart('/');
        }

        public static string GetEpisodeThumbUrl(TmdbEpisodeImages images)
        {
            if (images == null || images.Stills == null)
                return null;

            var episodeThumb = images.Stills.FirstOrDefault();
            if (episodeThumb == null)
                return null;

            // return the desired resolution
            return TraktSettings.TmdbConfiguration.Images.BaseUrl + TraktSettings.TmdbPreferredPosterSize + episodeThumb.FilePath;
        }

        static void AddEpisodeImagesToCache(TmdbEpisodeImages images)
        {
            if (images != null)
            {
                images.RequestAge = DateTime.Now.ToString();
                Episodes.TryAdd(Tuple.Create(images.Id, images.Season, images.Episode), images);
            }
        }

        static void RemoveEpisodeImagesFromCache(TmdbEpisodeImages images)
        {
            if (images != null)
            {
                TmdbEpisodeImages ignored;
                Episodes.TryRemove(Tuple.Create(images.Id, images.Season, images.Episode), out ignored);
            }
        }

        #endregion

        #region Seasons

        public static TmdbSeasonImages GetSeasonImages(int? id, int season, bool forceUpdate = false)
        {
            if (id == null) return null;

            // if its in our cache return it
            TmdbSeasonImages seasonImages;
            if (Seasons.TryGetValue(Tuple.Create(id, season), out seasonImages))
            {
                if (forceUpdate)
                    return seasonImages;

                // but only if the request is not very old
                if (DateTime.Now.Subtract(new TimeSpan(TraktSettings.TmdbSeasonImageMaxCacheAge, 0, 0, 0, 0)) < seasonImages.RequestAge.ToDateTime())
                {
                    return seasonImages;
                }

                TraktLogger.Info("Season image cache expired. TMDb ID = '{0}', Season = '{1}', Request Age = '{2}'", id, seasonImages.Season, seasonImages.RequestAge);
                RemoveSeasonImagesFromCache(seasonImages);
            }

            // get movie images from tmdb and add to the cache
            seasonImages = TmdbAPI.TmdbAPI.GetSeasonImages(id.ToString(), season);
            AddSeasonImagesToCache(seasonImages);

            return seasonImages;
        }

        public static string GetSeasonPosterFilename(TmdbSeasonImages images)
        {
            if (images == null || images.Posters == null)
                return null;

            var seasonThumb = images.Posters.LocalisedImage();
            if (seasonThumb == null)
                return null;

            // create filename based on desired resolution
            return Path.Combine(Config.GetFolder(Config.Dir.Thumbs), @"Trakt\Seasons\Thumbs\") +
                                images.Id + "_" + TraktSettings.TmdbPreferredPosterSize + "_" + seasonThumb.FilePath.TrimStart('/');
        }

        public static string GetSeasonPosterUrl(TmdbSeasonImages images)
        {
            if (images == null || images.Posters == null)
                return null;

            var seasonThumb = images.Posters.LocalisedImage();
            if (seasonThumb == null)
                return null;

            // return the desired resolution
            return TraktSettings.TmdbConfiguration.Images.BaseUrl + TraktSettings.TmdbPreferredPosterSize + seasonThumb.FilePath;
        }

        static void AddSeasonImagesToCache(TmdbSeasonImages images)
        {
            if (images != null)
            {
                Seasons.TryAdd(Tuple.Create(images.Id, images.Season), images);
            }
        }

        static void RemoveSeasonImagesFromCache(TmdbSeasonImages images)
        {
            if (images != null)
            {
                TmdbSeasonImages ignored;
                Seasons.TryRemove(Tuple.Create(images.Id, images.Season), out ignored);
            }
        }

        #endregion

        #region People

        public static TmdbPeopleImages GetPersonImages(int? id, bool forceUpdate = false)
        {
            if (id == null) return null;

            // if its in our cache return it
            TmdbPeopleImages personImages;
            if (People.TryGetValue(id, out personImages))
            {
                if (forceUpdate)
                    return personImages;

                // but only if the request is not very old
                if (DateTime.Now.Subtract(new TimeSpan(TraktSettings.TmdbPersonImageMaxCacheAge, 0, 0, 0, 0)) < personImages.RequestAge.ToDateTime())
                {
                    return personImages;
                }

                TraktLogger.Info("People image cache expired. TMDb ID = '{0}', Request Age = '{1}'", id, personImages.RequestAge);
                RemovePeopleImagesFromCache(personImages);
            }

            // get movie images from tmdb and add to the cache            
            personImages = TmdbAPI.TmdbAPI.GetPeopleImages(id.ToString());
            AddPeopleImagesToCache(personImages);

            return personImages;
        }

        public static string GetPersonHeadshotFilename(TmdbPeopleImages images)
        {
            if (images == null || images.Profiles == null)
                return null;

            var personThumb = images.Profiles.FirstOrDefault();
            if (personThumb == null)
                return null;

            // create filename based on desired resolution
            return Path.Combine(Config.GetFolder(Config.Dir.Thumbs), @"Trakt\People\Headshots\") +
                                images.Id + "_" + TraktSettings.TmdbPreferredPosterSize + "_" + personThumb.FilePath.TrimStart('/');
        }

        public static string GetPersonHeadshotUrl(TmdbPeopleImages images)
        {
            if (images == null || images.Profiles == null)
                return null;

            var personThumb = images.Profiles.FirstOrDefault();
            if (personThumb == null)
                return null;

            // return the desired resolution
            return TraktSettings.TmdbConfiguration.Images.BaseUrl + TraktSettings.TmdbPreferredPosterSize + personThumb.FilePath;
        }

        static void AddPeopleImagesToCache(TmdbPeopleImages images)
        {
            if (images != null)
            {
                images.RequestAge = DateTime.Now.ToString();
                People.TryAdd(images.Id, images);
            }
        }

        static void RemovePeopleImagesFromCache(TmdbPeopleImages images)
        {
            if (images != null)
            {
                TmdbPeopleImages ignored;
                People.TryRemove(images.Id, out ignored);
            }
        }

        #endregion

        #region Helpers

        static TmdbImage LocalisedImage(this List<TmdbImage> images)
        {
            var image = images.FirstOrDefault(i => i.LanguageCode == TraktSettings.TmdbPreferredImageLanguage);

            if (image == null && TraktSettings.TmdbPreferredImageLanguage != "en")
                image = images.FirstOrDefault(i => i.LanguageCode == "en");

            if (image == null)
                image = images.FirstOrDefault();

            return image;
        }

        #endregion
    }
}
