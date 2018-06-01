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

      // anything not in the current watched that is previously watched
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
        SaveWatchedMovies(watchedMovies);
        SaveLastSyncActivities(onlineSyncLastActivities);
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
        SaveCollectedMovies(collectedMovies);
        SaveLastSyncActivities(onlineSyncLastActivities);
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
      // anything not in the current watched that is previously watched
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
        SaveWatchedEpisodes(episodesWatched);
        SaveLastSyncActivities(onlineSyncLastActivities);
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
        SaveCollectedEpisodes(episodesCollected);
        SaveLastSyncActivities(onlineSyncLastActivities);
      }
      return episodesCollected;
    }

    private TraktSyncLastActivities GetSavedLastSyncActivities()
    {
      string traktUserHomePath = _mediaPortalServices.GetTraktUserHomePath();
      string savedSyncActivitiesFilePath = Path.Combine(traktUserHomePath, FileName.LastActivity.Value);
      if (!_fileOperations.FileExists(savedSyncActivitiesFilePath))
      {
        throw new Exception("Last sync activities file could not be found in: " + traktUserHomePath);
      }
      string savedSyncActivitiesJson = _fileOperations.FileReadAllText(savedSyncActivitiesFilePath);

      return JsonConvert.DeserializeObject<TraktSyncLastActivities>(savedSyncActivitiesJson);
    }

    private IEnumerable<TraktWatchedMovie> GetCachedWatchedMovies()
    {
      IEnumerable<TraktWatchedMovie> watchedMovies = new List<TraktWatchedMovie>();

      string watchedMoviesPath = Path.Combine(_mediaPortalServices.GetTraktUserHomePath(), FileName.WatchedMovies.Value);
      if (_fileOperations.FileExists(watchedMoviesPath))
      {
        string watchedMoviesJson = _fileOperations.FileReadAllText(watchedMoviesPath);
        watchedMovies = JsonConvert.DeserializeObject<List<TraktWatchedMovie>>(watchedMoviesJson);
      }
      return watchedMovies;
    }

    private IEnumerable<TraktCollectionMovie> GetCachedCollectedMovies()
    {
      IEnumerable<TraktCollectionMovie> collectedMovies = new List<TraktCollectionMovie>();

      string collectedMoviesPath = Path.Combine(_mediaPortalServices.GetTraktUserHomePath(), FileName.CollectedMovies.Value);
      if (_fileOperations.FileExists(collectedMoviesPath))
      {
        string collectedMoviesJson = _fileOperations.FileReadAllText(collectedMoviesPath);
        collectedMovies = JsonConvert.DeserializeObject<List<TraktCollectionMovie>>(collectedMoviesJson);
      }
      return collectedMovies;
    }

    private IEnumerable<EpisodeWatched> GetCachedWatchedEpisodes()
    {
      IEnumerable<EpisodeWatched> watchedEpisodes = new List<EpisodeWatched>();

      string watchedEpisodesPath = Path.Combine(_mediaPortalServices.GetTraktUserHomePath(), FileName.WatchedEpisodes.Value);
      if (_fileOperations.FileExists(watchedEpisodesPath))
      {
        string watchedEpisodesJson = _fileOperations.FileReadAllText(watchedEpisodesPath);
        watchedEpisodes = JsonConvert.DeserializeObject<List<EpisodeWatched>>(watchedEpisodesJson);
      }
      return watchedEpisodes;
    }

    private IEnumerable<EpisodeCollected> GetCachedCollectedEpisodes()
    {
      IEnumerable<EpisodeCollected> collectedEpisodes = new List<EpisodeCollected>();

      string collectedEpisodesPath = Path.Combine(_mediaPortalServices.GetTraktUserHomePath(), FileName.CollectedEpisodes.Value);
      if (_fileOperations.FileExists(collectedEpisodesPath))
      {
        string collectedEpisodesJson = _fileOperations.FileReadAllText(collectedEpisodesPath);
        collectedEpisodes = JsonConvert.DeserializeObject<List<EpisodeCollected>>(collectedEpisodesJson);
      }
      return collectedEpisodes;
    }

    private void SaveLastSyncActivities(TraktSyncLastActivities syncLastActivities)
    {
      string lastSyncActivitiesPath = Path.Combine(_mediaPortalServices.GetTraktUserHomePath(), FileName.LastActivity.Value);
      string lastSyncActivitiesJson = JsonConvert.SerializeObject(syncLastActivities);
      _fileOperations.FileWriteAllText(lastSyncActivitiesPath, lastSyncActivitiesJson, Encoding.UTF8);
    }

    private void SaveWatchedMovies(IEnumerable<TraktWatchedMovie> watchedMovies)
    {
      string watchedMoviesPath = Path.Combine(_mediaPortalServices.GetTraktUserHomePath(), FileName.WatchedMovies.Value);
      string watchedMoviesJson = JsonConvert.SerializeObject(watchedMovies);
      _fileOperations.FileWriteAllText(watchedMoviesPath, watchedMoviesJson, Encoding.UTF8);
    }

    private void SaveCollectedMovies(IEnumerable<TraktCollectionMovie> collectedMovies)
    {
      string collectedMoviesPath = Path.Combine(_mediaPortalServices.GetTraktUserHomePath(), FileName.CollectedMovies.Value);
      string collectedMoviesJson = JsonConvert.SerializeObject(collectedMovies);
      _fileOperations.FileWriteAllText(collectedMoviesPath, collectedMoviesJson, Encoding.UTF8);
    }

    private void SaveWatchedEpisodes(IList<EpisodeWatched> episodesWatched)
    {
      string watchedEpisodesPath = Path.Combine(_mediaPortalServices.GetTraktUserHomePath(), FileName.WatchedEpisodes.Value);
      string watchedEpisodesJson = JsonConvert.SerializeObject(episodesWatched);
      _fileOperations.FileWriteAllText(watchedEpisodesPath, watchedEpisodesJson, Encoding.UTF8);
    }

    private void SaveCollectedEpisodes(IList<EpisodeCollected> episodesCollected)
    {
      string collectedEpisodesPath = Path.Combine(_mediaPortalServices.GetTraktUserHomePath(), FileName.CollectedEpisodes.Value);
      string collectedEpisodesJson = JsonConvert.SerializeObject(episodesCollected);
      _fileOperations.FileWriteAllText(collectedEpisodesPath, collectedEpisodesJson, Encoding.UTF8);
    }
  }
}