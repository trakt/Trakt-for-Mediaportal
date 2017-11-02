using MediaPortal.Configuration;
using System;
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
        // create locks for each media type, lists can have multiple types
        // we add images and retrieve on different threads so try to be thread safe
        static object lockShowObject = new object();
        static object lockMovieObject = new object();
        static object lockSeasonObject = new object();
        static object lockEpisodeObject = new object();
        static object lockPersonObject = new object();

        static string MovieCacheFile = Path.Combine(Config.GetFolder(Config.Dir.Config), string.Format(@"Trakt\TmdbCache\Movies.json"));
        static string ShowCacheFile = Path.Combine(Config.GetFolder(Config.Dir.Config), string.Format(@"Trakt\TmdbCache\Shows.json"));
        static string SeasonCacheFile = Path.Combine(Config.GetFolder(Config.Dir.Config), string.Format(@"Trakt\TmdbCache\Seasons.json"));
        static string EpisodeCacheFile = Path.Combine(Config.GetFolder(Config.Dir.Config), @"Trakt\TmdbCache\Episodes.json");        
        static string PersonCacheFile = Path.Combine(Config.GetFolder(Config.Dir.Config), @"Trakt\TmdbCache\People.json");

        static List<TmdbMovieImages> Movies
        {
            get
            {
                lock (lockMovieObject)
                {
                    return _Movies;
                }
            }
            set
            {
                lock (lockMovieObject)
                {
                    _Movies = value;
                }
            }
        }
        static List<TmdbMovieImages> _Movies = null;

        static List<TmdbShowImages> Shows
        {
            get
            {
                lock (lockShowObject)
                {
                    return _Shows;
                }
            }
            set
            {
                lock (lockShowObject)
                {
                    _Shows = value;
                }
            }
        }
        static List<TmdbShowImages> _Shows = null;

        static List<TmdbEpisodeImages> Episodes
        {
            get
            {
                lock (lockEpisodeObject)
                {
                    return _Episodes;
                }
            }
            set
            {
                lock (lockEpisodeObject)
                {
                    _Episodes = value;
                }
            }
        }
        static List<TmdbEpisodeImages> _Episodes = null;

        static List<TmdbSeasonImages> Seasons
        {
            get
            {
                lock (lockSeasonObject)
                {
                    return _Seasons;
                }
            }
            set
            {
                lock (lockSeasonObject)
                {
                    _Seasons = value;
                }
            }
        }
        static List<TmdbSeasonImages> _Seasons = null;

        static List<TmdbPeopleImages> People
        {
            get
            {
                lock (lockPersonObject)
                {
                    return _People;
                }
            }
            set
            {
                lock (lockPersonObject)
                {
                    _People = value;
                }
            }
        }
        static List<TmdbPeopleImages> _People = null;


        public static void Init()
        {
            TraktLogger.Info("Loading TMDb request cache");

            _Movies = LoadFileCache(MovieCacheFile, "[]").FromJSONArray<TmdbMovieImages>().ToList();
            _Shows = LoadFileCache(ShowCacheFile, "[]").FromJSONArray<TmdbShowImages>().ToList();
            _Seasons = LoadFileCache(SeasonCacheFile, "[]").FromJSONArray<TmdbSeasonImages>().ToList();
            _Episodes = LoadFileCache(EpisodeCacheFile, "[]").FromJSONArray<TmdbEpisodeImages>().ToList();
            _People = LoadFileCache(PersonCacheFile, "[]").FromJSONArray<TmdbPeopleImages>().ToList();

            // get updated configuration from TMDb
            GetTmdbConfiguration();
        }

        public static void DeInit()
        {
            TraktLogger.Info("Saving TMDb request cache");

            SaveFileCache(MovieCacheFile, Movies.ToJSON());
            SaveFileCache(ShowCacheFile, Shows.ToJSON());
            SaveFileCache(SeasonCacheFile, Seasons.ToJSON());
            SaveFileCache(EpisodeCacheFile, Episodes.ToJSON());            
            SaveFileCache(PersonCacheFile, People.ToJSON());
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
                    if (string.IsNullOrEmpty(returnValue))
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
            var movieImages = Movies.FirstOrDefault(m => m.Id == id);
            if (movieImages != null)
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
                lock (lockMovieObject)
                {
                    images.RequestAge = DateTime.Now.ToString();
                    Movies.Add(images);
                }
            }
        }

        static void RemoveMovieImagesFromCache(TmdbMovieImages images)
        {
            if (images != null)
            {
                lock (lockMovieObject)
                {
                    Movies.RemoveAll(m => m.Id == images.Id);
                }
            }
        }

        #endregion

        #region Shows

        public static TmdbShowImages GetShowImages(int? id, bool forceUpdate = false)
        {
            if (id == null) return null;

            // if its in our cache return it
            var showImages = Shows.FirstOrDefault(s => s.Id == id);
            if (showImages != null)
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
                lock (lockShowObject)
                {
                    images.RequestAge = DateTime.Now.ToString();
                    Shows.Add(images);
                }
            }
        }

        static void RemoveShowImagesFromCache(TmdbShowImages images)
        {
            if (images != null)
            {
                lock (lockShowObject)
                {
                    Shows.RemoveAll(s => s.Id == images.Id);
                }
            }
        }

        #endregion

        #region Episodes

        public static TmdbEpisodeImages GetEpisodeImages(int? id, int season, int episode, bool forceUpdate = false)
        {
            if (id == null) return null;

            // if its in our cache return it
            var episodeImages = Episodes.FirstOrDefault(e => e.Id == id && e.Season == season && e.Episode == episode);
            if (episodeImages != null)
            {
                if (forceUpdate)
                    return episodeImages;

                // but only if the request is not very old
                if (DateTime.Now.Subtract(new TimeSpan(TraktSettings.TmdbEpisodeImageMaxCacheAge, 0, 0, 0, 0)) < episodeImages.RequestAge.ToDateTime())
                {
                    return episodeImages;
                }

                TraktLogger.Info("Episode image cache expired. TMDb ID = '{0}', Season = '{1}', Episode = '{2}', Request Age = '{3}'", id, season, episode, episodeImages.RequestAge);
                RemoveEpisodeImagesFromCache(episodeImages, season, episode);
            }

            // get movie images from tmdb and add to the cache            
            episodeImages = TmdbAPI.TmdbAPI.GetEpisodeImages(id.ToString(), season, episode);
            AddEpisodeImagesToCache(episodeImages, id, season, episode);

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

        static void AddEpisodeImagesToCache(TmdbEpisodeImages images, int? id, int season, int episode)
        {
            if (images != null)
            {
                lock (lockEpisodeObject)
                {
                    images.RequestAge = DateTime.Now.ToString();
                    images.Season = season;
                    images.Episode = episode;
                    images.Id = id;
                    Episodes.Add(images);
                }
            }
        }

        static void RemoveEpisodeImagesFromCache(TmdbEpisodeImages images, int season, int episode)
        {
            if (images != null)
            {
                lock (lockEpisodeObject)
                {
                    Episodes.RemoveAll(e => e.Id == images.Id && e.Season == season && e.Episode == episode);
                }
            }
        }

        #endregion

        #region Seasons

        public static TmdbSeasonImages GetSeasonImages(int? id, int season, bool forceUpdate = false)
        {
            if (id == null) return null;
            
            // if its in our cache return it
            var seasonImages = Seasons.FirstOrDefault(s => s.Id == id && s.Season == season);
            if (seasonImages != null)
            {
                if (forceUpdate)
                    return seasonImages;

                // but only if the request is not very old
                if (DateTime.Now.Subtract(new TimeSpan(TraktSettings.TmdbSeasonImageMaxCacheAge, 0, 0, 0, 0)) < seasonImages.RequestAge.ToDateTime())
                {
                    return seasonImages;
                }

                TraktLogger.Info("Season image cache expired. TMDb ID = '{0}', Season = '{1}', Request Age = '{2}'", id, season, seasonImages.RequestAge);
                RemoveSeasonImagesFromCache(seasonImages, season);
            }

            // get movie images from tmdb and add to the cache
            seasonImages = TmdbAPI.TmdbAPI.GetSeasonImages(id.ToString(), season);
            AddSeasonImagesToCache(seasonImages, id, season);

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

        static void AddSeasonImagesToCache(TmdbSeasonImages images, int? id, int season)
        {
            if (images != null)
            {
                lock (lockSeasonObject)
                {
                    // the id on the request (show) is different on the response (season)
                    images.RequestAge = DateTime.Now.ToString();
                    images.Season = season;
                    images.Id = id;
                    Seasons.Add(images);
                }
            }
        }

        static void RemoveSeasonImagesFromCache(TmdbSeasonImages images, int season)
        {
            if (images != null)
            {
                lock (lockSeasonObject)
                {
                    Seasons.RemoveAll(s => s.Id == images.Id && s.Season == season);
                }
            }
        }

        #endregion

        #region People

        public static TmdbPeopleImages GetPersonImages(int? id, bool forceUpdate = false)
        {
            if (id == null) return null;

            // if its in our cache return it
            var personImages = People.FirstOrDefault(m => m.Id == id);
            if (personImages != null)
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
                lock (lockPersonObject)
                {
                    images.RequestAge = DateTime.Now.ToString();
                    People.Add(images);
                }
            }
        }

        static void RemovePeopleImagesFromCache(TmdbPeopleImages images)
        {
            if (images != null)
            {
                lock (lockPersonObject)
                {
                    People.RemoveAll(p => p.Id == images.Id);
                }
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
