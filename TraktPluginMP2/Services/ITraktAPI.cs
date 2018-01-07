using TraktAPI.DataStructures;

namespace TraktPluginMP2.Services
{
  public interface ITraktAPI
  {
    TraktSyncResponse AddMoviesToWatchedHistory(TraktSyncMoviesWatched movies);

    TraktSyncResponse AddMoviesToCollecton(TraktSyncMoviesCollected movies);

    TraktSyncResponse AddShowsToWatchedHistoryEx(TraktSyncShowsWatchedEx shows);

    TraktSyncResponse AddShowsToCollectonEx(TraktSyncShowsCollectedEx shows);
  }
}