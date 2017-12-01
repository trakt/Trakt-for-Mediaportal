using TraktAPI.DataStructures;

namespace TraktPluginMP2.Services
{
  public interface ITraktAPI
  {
    TraktSyncResponse AddMoviesToWatchedHistory(TraktSyncMoviesWatched movies);
  }
}