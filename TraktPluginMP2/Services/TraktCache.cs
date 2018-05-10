using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using TraktApiSharp.Objects.Get.Collection;
using TraktApiSharp.Objects.Get.Movies;
using TraktApiSharp.Objects.Get.Syncs.Activities;
using TraktApiSharp.Objects.Get.Watched;
using TraktPluginMP2.Settings;
using TraktPluginMP2.Structures;

namespace TraktPluginMP2.Services
{
  public class TraktCache : ITraktCache
  {
    private readonly IMediaPortalServices _mediaPortalServices;
    private readonly ITraktClient _traktClient;
    private readonly IFileOperations _fileOperations;

    public TraktCache(IMediaPortalServices mediaPortalServices, ITraktClient traktClient, IFileOperations fileOperations)
    {
      _mediaPortalServices = mediaPortalServices;
      _traktClient = traktClient;
      _fileOperations = fileOperations;
    }

    public IEnumerable<TraktMovie> GetUnWatchedMovies()
    {
      IEnumerable<TraktWatchedMovie> previouslyWatched = GetCachedWatchedMovies();
      IEnumerable<TraktWatchedMovie> currentWatched = _traktClient.GetWatchedMovies();

      // anything not in the currentwatched that is previously watched
      // must be unwatched now.
      IEnumerable<TraktMovie> unWatchedMovies = from pw in previouslyWatched
        where !currentWatched.Any(m => (m.Movie.Ids.Trakt == pw.Movie.Ids.Trakt || m.Movie.Ids.Imdb == pw.Movie.Ids.Imdb))
        select new TraktMovie
        {
          Ids = pw.Movie.Ids,
          Title = pw.Movie.Title,
          Year = pw.Movie.Year
        };
      return unWatchedMovies;
    }

    public IEnumerable<TraktWatchedMovie> GetWatchedMovies()
    {
      IEnumerable <TraktWatchedMovie> watchedMovies;
      TraktPluginSettings settings = _mediaPortalServices.GetSettingsManager().Load<TraktPluginSettings>();
      TraktSyncLastActivities traktSyncLastActivities = _traktClient.GetLastActivitiesAsync();
      bool cacheIsUpToDate = traktSyncLastActivities?.Movies.WatchedAt == settings.LastSyncActivities.Movies.WatchedAt;

      if (cacheIsUpToDate)
      {
        watchedMovies = GetCachedWatchedMovies();
      }
      else
      {
        watchedMovies = _traktClient.GetWatchedMovies();
        string watchedMoviesPath = GetCacheFilePath("Watched", "Movies");
        string content = JsonConvert.SerializeObject(watchedMovies);
        _fileOperations.SaveFileToCache(watchedMoviesPath, content);

        settings.LastSyncActivities.Movies.WatchedAt = traktSyncLastActivities?.Movies?.WatchedAt;
        _mediaPortalServices.GetSettingsManager().Save(settings);
      }

      return watchedMovies;
    }

    public IEnumerable<TraktCollectionMovie> GetCollectedMovies()
    {
      IEnumerable<TraktCollectionMovie> collectedMovies;
      TraktPluginSettings settings = _mediaPortalServices.GetSettingsManager().Load<TraktPluginSettings>();
      TraktSyncLastActivities traktSyncLastActivities = _traktClient.GetLastActivitiesAsync();
      bool cacheIsUpToDate = traktSyncLastActivities?.Movies.CollectedAt == settings.LastSyncActivities.Movies.CollectedAt;

      if (cacheIsUpToDate)
      {
        collectedMovies = GetCachedCollectedMovies();
      }
      else
      {
        collectedMovies = _traktClient.GetCollectedMovies();
        string collectedMoviesPath = GetCacheFilePath("Collected", "Movies");
        string content = JsonConvert.SerializeObject(collectedMovies);
        _fileOperations.SaveFileToCache(collectedMoviesPath, content);

        settings.LastSyncActivities.Movies.CollectedAt = traktSyncLastActivities?.Movies.CollectedAt;
        _mediaPortalServices.GetSettingsManager().Save(settings);
      }

      return collectedMovies;
    }

    public IEnumerable<Episode> GetUnWatchedEpisodes()
    {
      IEnumerable<Episode> previouslyWatched = GetCachedWatchedEpisodes();
      IEnumerable<TraktWatchedShow> currentWatchedShows = _traktClient.GetWatchedShows();
      IList<EpisodeWatched> currentEpisodesWatched = new List<EpisodeWatched>();
      // convert to internal data structure
      foreach (var show in currentWatchedShows)
      {
        foreach (var season in show.Seasons)
        {
          foreach (var episode in season.Episodes)
          {
            currentEpisodesWatched.Add(new EpisodeWatched
            {
              ShowId = show.Show.Ids.Trakt,
              ShowTvdbId = show.Show.Ids.Tvdb,
              ShowImdbId = show.Show.Ids.Imdb,
              ShowTitle = show.Show.Title,
              ShowYear = show.Show.Year,
              Season = season.Number,
              Number = episode.Number,
              Plays = episode.Plays,
              WatchedAt = episode.LastWatchedAt
            });
          }
        }
      }
      // anything not in the currentwatched that is previously watched
      // must be unwatched now.
      // Note: we can add to internal cache from external events, so we can't always rely on trakt id for comparisons
      ILookup<string, EpisodeWatched> dictCurrWatched = currentEpisodesWatched.ToLookup(cwe => cwe.ShowTvdbId + "_" + cwe.Season + "_" + cwe.Number);

      IEnumerable<Episode> unWatchedEpisodes = from pwe in previouslyWatched
        where !dictCurrWatched[pwe.ShowTvdbId + "_" + pwe.Season + "_" + pwe.Number].Any()
        select new Episode
        {
          ShowId = pwe.ShowId,
          ShowTvdbId = pwe.ShowTvdbId,
          ShowImdbId = pwe.ShowImdbId,
          ShowTitle = pwe.ShowTitle,
          ShowYear = pwe.ShowYear,
          Season = pwe.Season,
          Number = pwe.Number
        };
      return unWatchedEpisodes;
    }

    public IEnumerable<EpisodeWatched> GetWatchedEpisodes()
    {
      IList<EpisodeWatched> episodesWatched = new List<EpisodeWatched>();
      TraktPluginSettings settings = _mediaPortalServices.GetSettingsManager().Load<TraktPluginSettings>();
      TraktSyncLastActivities traktSyncLastActivities = _traktClient.GetLastActivitiesAsync();
      bool cacheIsUpToDate = traktSyncLastActivities?.Episodes.WatchedAt == settings.LastSyncActivities.Episodes.WatchedAt;

      if (cacheIsUpToDate)
      {
        episodesWatched = GetCachedWatchedEpisodes().ToList();
      }
      else
      {
        IEnumerable<TraktWatchedShow> watchedShows = _traktClient.GetWatchedShows();
        
        // convert to internal data structure
        foreach (var show in watchedShows)
        {
          foreach (var season in show.Seasons)
          {
            foreach (var episode in season.Episodes)
            {
              episodesWatched.Add(new EpisodeWatched
              {
                ShowId = show.Show.Ids.Trakt,
                ShowTvdbId = show.Show.Ids.Tvdb,
                ShowImdbId = show.Show.Ids.Imdb,
                ShowTitle = show.Show.Title,
                ShowYear = show.Show.Year,
                Number = episode.Number,
                Plays = episode.Plays,
                WatchedAt = episode.LastWatchedAt
              });
            }
          }
        }
        string watchedEpisodesPath = GetCacheFilePath("Watched", "Episodes");
        string content = JsonConvert.SerializeObject(watchedEpisodesPath);
        _fileOperations.SaveFileToCache(watchedEpisodesPath, content);

        settings.LastSyncActivities.Episodes.WatchedAt = traktSyncLastActivities?.Episodes.WatchedAt;
        _mediaPortalServices.GetSettingsManager().Save(settings);
      }

      return episodesWatched;
    }

    public IEnumerable<EpisodeCollected> GetCollectedEpisodes()
    {
      IList<EpisodeCollected> episodesCollected = new List<EpisodeCollected>();
      TraktPluginSettings settings = _mediaPortalServices.GetSettingsManager().Load<TraktPluginSettings>();
      TraktSyncLastActivities traktSyncLastActivities = _traktClient.GetLastActivitiesAsync();
      bool cacheIsUpToDate = traktSyncLastActivities?.Episodes.CollectedAt == settings.LastSyncActivities.Episodes.CollectedAt;

      if (cacheIsUpToDate)
      {
        episodesCollected = GetCachedCollectedEpisodes().ToList();
      }
      else
      {
        IEnumerable<TraktCollectionShow> collectedShows = _traktClient.GetCollectedShows();
        
        // convert to internal data structure
        foreach (var show in collectedShows)
        {
          foreach (var season in show.Seasons)
          {
            foreach (var episode in season.Episodes)
            {
              episodesCollected.Add(new EpisodeCollected
              {
                ShowId = show.Show.Ids.Trakt,
                ShowTvdbId = show.Show.Ids.Tvdb,
                ShowImdbId = show.Show.Ids.Imdb,
                ShowTitle = show.Show.Title,
                ShowYear = show.Show.Year,
                Number = episode.Number,
                Season = season.Number,
                CollectedAt = episode.CollectedAt
              });
            }
          }
        }
        string collectedEpisodesPath = GetCacheFilePath("Collected", "Episodes");
        string content = JsonConvert.SerializeObject(collectedEpisodesPath);
        _fileOperations.SaveFileToCache(collectedEpisodesPath, content);

        settings.LastSyncActivities.Episodes.CollectedAt = traktSyncLastActivities?.Episodes.CollectedAt;
        _mediaPortalServices.GetSettingsManager().Save(settings);
      }

      return episodesCollected;
    }

    private IEnumerable<TraktWatchedMovie> GetCachedWatchedMovies()
    {
      IEnumerable<TraktWatchedMovie> result = new List<TraktWatchedMovie>();

      string watchedMoviesPath = GetCacheFilePath("Watched", "Movies");
      string cachedJson = _fileOperations.LoadFileCache(watchedMoviesPath);

      if (cachedJson != null)
      {
        result = JsonConvert.DeserializeObject<List<TraktWatchedMovie>>(cachedJson);
      }
      return result;
    }

    private IEnumerable<TraktCollectionMovie> GetCachedCollectedMovies()
    {
      IEnumerable<TraktCollectionMovie> result = new List<TraktCollectionMovie>();

      string watchedMoviesPath = GetCacheFilePath("Collected", "Movies");
      string cachedJson = _fileOperations.LoadFileCache(watchedMoviesPath);

      if (cachedJson != null)
      {
        result = JsonConvert.DeserializeObject<List<TraktCollectionMovie>>(cachedJson);
      }
      return result;
    }

    private IEnumerable<EpisodeWatched> GetCachedWatchedEpisodes()
    {
      IEnumerable<EpisodeWatched> result = new List<EpisodeWatched>();

      string watchedEpisodesPath = GetCacheFilePath("Watched", "Episodes");
      string cachedJson = _fileOperations.LoadFileCache(watchedEpisodesPath);

      if (cachedJson != null)
      {
        result = JsonConvert.DeserializeObject<List<EpisodeWatched>>(cachedJson);
      }
      return result;
    }

    private IEnumerable<EpisodeCollected> GetCachedCollectedEpisodes()
    {
      IEnumerable<EpisodeCollected> result = new List<EpisodeCollected>();

      string collectedEpisodesPath = GetCacheFilePath("Collected", "Episodes");
      string cachedJson = _fileOperations.LoadFileCache(collectedEpisodesPath);

      if (cachedJson != null)
      {
        result = JsonConvert.DeserializeObject<List<EpisodeCollected>>(cachedJson);
      }
      return result;
    }

    private string GetCacheFilePath(string fileName, string category)
    {
      string basePath = GetCacheBasePath();

      return Path.Combine(basePath, category, fileName) + ".json";
    }

    private string GetCacheBasePath()
    {
      string rootPath = _mediaPortalServices.GetPathManager().GetPath(@"<DATA>\Trakt\");
      string username = _traktClient.GetUsername();

      return Path.Combine(rootPath, username, "Library");
    }
  }
}