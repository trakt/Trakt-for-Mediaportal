using System.Collections.Generic;
using TraktAPI.DataStructures;

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
  }
}