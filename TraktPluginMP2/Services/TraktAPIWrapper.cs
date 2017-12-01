using TraktAPI.DataStructures;

namespace TraktPluginMP2.Services
{
  public class TraktAPIWrapper : ITraktAPI
  {
    public TraktSyncResponse AddMoviesToWatchedHistory(TraktSyncMoviesWatched movies)
    {
      return TraktAPI.TraktAPI.AddMoviesToWatchedHistory(movies);
    }
  }
}