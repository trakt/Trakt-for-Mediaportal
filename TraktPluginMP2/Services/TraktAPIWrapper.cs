using TraktAPI.DataStructures;

namespace TraktPluginMP2.Services
{
  public class TraktAPIWrapper : ITraktAPI
  {
    public TraktSyncResponse AddMoviesToWatchedHistory(TraktSyncMoviesWatched movies)
    {
      return TraktAPI.TraktAPI.AddMoviesToWatchedHistory(movies);
    }

    public TraktSyncResponse AddMoviesToCollecton(TraktSyncMoviesCollected movies)
    {
      return TraktAPI.TraktAPI.AddMoviesToCollecton(movies);
    }

    public TraktSyncResponse AddShowsToWatchedHistoryEx(TraktSyncShowsWatchedEx shows)
    {
      return TraktAPI.TraktAPI.AddShowsToWatchedHistoryEx(shows);
    }

    public TraktSyncResponse AddShowsToCollectonEx(TraktSyncShowsCollectedEx shows)
    {
      return TraktAPI.TraktAPI.AddShowsToCollectonEx(shows);
    }
  }
}