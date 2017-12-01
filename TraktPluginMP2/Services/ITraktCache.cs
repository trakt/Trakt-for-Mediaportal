using System.Collections.Generic;
using TraktAPI.DataStructures;

namespace TraktPluginMP2.Services
{
  public interface ITraktCache
  {
    bool RefreshData();

    void Save();

    IEnumerable<TraktMovie> GetUnWatchedMoviesFromTrakt();
  }
}