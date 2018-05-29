using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using TraktApiSharp.Objects.Get.Collection;
using TraktApiSharp.Objects.Get.Movies;
using TraktApiSharp.Objects.Get.Syncs.Activities;
using TraktApiSharp.Objects.Get.Watched;
using TraktPluginMP2.Structures;

namespace TraktPluginMP2.Services
{
  public class TraktCache : ITraktCache
  {
    private readonly IMediaPortalServices _mediaPortalServices;
    private readonly ITraktClient _traktClient;
    private readonly IFileOperations _fileOperations;

    private const string LastSyncActivitiesFileName = "last.sync.activities.json";
    private const string WatchedMoviesFileName = "Watched.Movies.json";
    private const string CollectedMoviesFileName = "Collected.Movies.json";
    private const string WatchedEpisodesFileName = "Watched.Episodes.json";
    private const string CollectedEpisodesFileName = "Collected.Episodes.json";

    public TraktCache(IMediaPortalServices mediaPortalServices, ITraktClient traktClient, IFileOperations fileOperations)
    {
      _mediaPortalServices = mediaPortalServices;
      _traktClient = traktClient;
      _fileOperations = fileOperations;
    }

    public IEnumerable<TraktMovie> GetUnWatchedMovies()
    {
      IEnumerable<TraktMovie> unWatchedMovies = new List<TraktMovie>();
      IEnumerable<TraktWatchedMovie> previouslyWatched = GetCachedWatchedMovies();
      IEnumerable<TraktWatchedMovie> currentWatched = _traktClient.GetWatchedMovies();

      // anything not in the currentwatched that is previously watched
      // must be unwatched now.
      if (previouslyWatched != null)
      {
        unWatchedMovies = from pw in previouslyWatched
          where !currentWatched.Any(m => (m.Movie.Ids.Trakt == pw.Movie.Ids.Trakt || m.Movie.Ids.Imdb == pw.Movie.Ids.Imdb))
          select new TraktMovie
          {
            Ids = pw.Movie.Ids,
            Title = pw.Movie.Title,
            Year = pw.Movie.Year
          };
      }

      return unWatchedMovies;
    }

    public IEnumerable<TraktWatchedMovie> GetWatchedMovies()
    {
      IEnumerable <TraktWatchedMovie> watchedMovies;
      TraktSyncLastActivities onlineSyncLastActivities = _traktClient.GetLastActivities();
      TraktSyncLastActivities savedSyncLastActivities = GetSavedLastSyncActivities();
      bool cacheIsUpToDate = onlineSyncLastActivities.Movies.WatchedAt == savedSyncLastActivities.Movies.WatchedAt;

      if (cacheIsUpToDate)
      {
        watchedMovies = GetCachedWatchedMovies();
      }
      else
      {
        watchedMovies = _traktClient.GetWatchedMovies();
        string watchedMoviesPath = Path.Combine(GetCachePath(), WatchedMoviesFileName);
        string watchedMoviesContent = JsonConvert.SerializeObject(watchedMovies);
        _fileOperations.FileWriteAllText(watchedMoviesPath, watchedMoviesContent, Encoding.UTF8);
      }

      return watchedMovies;
    }

    public IEnumerable<TraktCollectionMovie> GetCollectedMovies()
    {
      IEnumerable<TraktCollectionMovie> collectedMovies;
      TraktSyncLastActivities onlineSyncLastActivities = _traktClient.GetLastActivities();
      TraktSyncLastActivities savedSyncLastActivities = GetSavedLastSyncActivities();
      bool cacheIsUpToDate = onlineSyncLastActivities.Movies.CollectedAt == savedSyncLastActivities.Movies.CollectedAt;

      if (cacheIsUpToDate)
      {
        collectedMovies = GetCachedCollectedMovies();
      }
      else
      {
        collectedMovies = _traktClient.GetCollectedMovies();
        string collectedMoviesPath = Path.Combine(GetCachePath(), CollectedMoviesFileName);
        string content = JsonConvert.SerializeObject(collectedMovies);
        _fileOperations.FileWriteAllText(collectedMoviesPath, content, Encoding.UTF8);
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
      TraktSyncLastActivities onlineSyncLastActivities = _traktClient.GetLastActivities();
      TraktSyncLastActivities savedSyncLastActivities = GetSavedLastSyncActivities();
      bool cacheIsUpToDate = onlineSyncLastActivities.Episodes.WatchedAt == savedSyncLastActivities.Episodes.WatchedAt;

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
        string watchedEpisodesPath = Path.Combine(GetCachePath(), WatchedEpisodesFileName);
        string content = JsonConvert.SerializeObject(watchedEpisodesPath);
        _fileOperations.FileWriteAllText(watchedEpisodesPath, content, Encoding.UTF8);
      }

      return episodesWatched;
    }

    public IEnumerable<EpisodeCollected> GetCollectedEpisodes()
    {
      IList<EpisodeCollected> episodesCollected = new List<EpisodeCollected>();
      TraktSyncLastActivities onlineSyncLastActivities = _traktClient.GetLastActivities();
      TraktSyncLastActivities savedSyncLastActivities = GetSavedLastSyncActivities();
      bool cacheIsUpToDate = onlineSyncLastActivities.Episodes.CollectedAt == savedSyncLastActivities.Episodes.CollectedAt;

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
        string collectedEpisodesPath = Path.Combine(GetCachePath(), CollectedEpisodesFileName);
        string collectedEpisodesContent = JsonConvert.SerializeObject(collectedEpisodesPath);
        _fileOperations.FileWriteAllText(collectedEpisodesPath, collectedEpisodesContent, Encoding.UTF8);
      }

      return episodesCollected;
    }

    private TraktSyncLastActivities GetSavedLastSyncActivities()
    {
      string savedSyncActivitiesFilePath = Path.Combine(GetCachePath(), LastSyncActivitiesFileName);
      if (!_fileOperations.FileExists(savedSyncActivitiesFilePath))
      {
        throw new Exception("file not found");
      }
      string savedSyncActivitiesFileContent = _fileOperations.FileReadAllText(savedSyncActivitiesFilePath);
      return JsonConvert.DeserializeObject<TraktSyncLastActivities>(savedSyncActivitiesFileContent);
    }

    private string GetCachePath()
    {
      string rootPath = _mediaPortalServices.GetPathManager().GetPath(@"<DATA>\Trakt\");
      string userProfileId = _mediaPortalServices.GetUserManagement().CurrentUser.ProfileId.ToString();
      return Path.Combine(rootPath, userProfileId);
    }

    private IEnumerable<TraktWatchedMovie> GetCachedWatchedMovies()
    {
      IEnumerable<TraktWatchedMovie> result;

      string watchedMoviesPath = Path.Combine(GetCachePath(), WatchedMoviesFileName);
      if (!_fileOperations.FileExists(watchedMoviesPath))
      {
        result = null;
      }
      else
      {
        string cachedJson = _fileOperations.FileReadAllText(watchedMoviesPath);
        result = JsonConvert.DeserializeObject<List<TraktWatchedMovie>>(cachedJson);
      }
      return result;
    }

    private IEnumerable<TraktCollectionMovie> GetCachedCollectedMovies()
    {
      IEnumerable<TraktCollectionMovie> result;

      string collectedMoviesPath = Path.Combine(GetCachePath(), CollectedMoviesFileName);
      if (!_fileOperations.FileExists(collectedMoviesPath))
      {
        result = null;
      }
      else
      {
        string cachedJson = _fileOperations.FileReadAllText(collectedMoviesPath);
        result = JsonConvert.DeserializeObject<List<TraktCollectionMovie>>(cachedJson);
      }
      return result;
    }

    private IEnumerable<EpisodeWatched> GetCachedWatchedEpisodes()
    {
      IEnumerable<EpisodeWatched> result;

      string watchedEpisodesPath = Path.Combine(GetCachePath(), WatchedEpisodesFileName);
      if (!_fileOperations.FileExists(watchedEpisodesPath))
      {
        result = null;
      }
      else
      {
        string cachedJson = _fileOperations.FileReadAllText(watchedEpisodesPath);
        result = JsonConvert.DeserializeObject<List<EpisodeWatched>>(cachedJson);
      }
      return result;
    }

    private IEnumerable<EpisodeCollected> GetCachedCollectedEpisodes()
    {
      IEnumerable<EpisodeCollected> result;

      string collectedEpisodesPath = Path.Combine(GetCachePath(), CollectedEpisodesFileName);
      if (!_fileOperations.FileExists(collectedEpisodesPath))
      {
        result = null;
      }
      else
      {
        string cachedJson = _fileOperations.FileReadAllText(collectedEpisodesPath);
        result = JsonConvert.DeserializeObject<List<EpisodeCollected>>(cachedJson);
      }
      return result;
    }
  }
}