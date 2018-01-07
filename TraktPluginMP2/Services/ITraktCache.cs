using System.Collections.Generic;
using TraktAPI.DataStructures;
using TraktPluginMP2.Structures;

namespace TraktPluginMP2.Services
{
  public interface ITraktCache
  {
    bool RefreshData();

    void Save();

    IEnumerable<TraktMovie> GetUnWatchedMoviesFromTrakt();

    IEnumerable<TraktMovieWatched> GetWatchedMoviesFromTrakt();

    IEnumerable<TraktMovieCollected> GetCollectedMoviesFromTrakt();

    void AddMoviesToWatchHistory(List<TraktSyncMovieWatched> movies);

    void RemoveMoviesFromWatchHistory(List<TraktMovie> movies);

    void AddMoviesToCollection(List<TraktSyncMovieCollected> movies);

    void RemoveMoviesFromCollection(List<TraktMovie> movies);

    IEnumerable<Episode> GetUnWatchedEpisodesFromTrakt();

    IEnumerable<EpisodeWatched> GetWatchedEpisodesFromTrakt(bool ignoreLastSyncTime = false);

    IEnumerable<EpisodeCollected> GetCollectedEpisodesFromTrakt(bool ignoreLastSyncTime = false);

    void AddEpisodesToWatchHistory(TraktSyncShowWatchedEx show);

    void AddEpisodesToCollection(TraktSyncShowCollectedEx show);
  }
}