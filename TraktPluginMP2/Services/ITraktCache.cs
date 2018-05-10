using System.Collections.Generic;
using TraktApiSharp.Objects.Get.Collection;
using TraktApiSharp.Objects.Get.Movies;
using TraktApiSharp.Objects.Get.Watched;
using TraktPluginMP2.Structures;

namespace TraktPluginMP2.Services
{
  public interface ITraktCache
  {
    IEnumerable<TraktMovie> GetUnWatchedMovies();

    IEnumerable<TraktWatchedMovie> GetWatchedMovies();

    IEnumerable<TraktCollectionMovie> GetCollectedMovies();

    IEnumerable<Episode> GetUnWatchedEpisodes();

    IEnumerable<EpisodeWatched> GetWatchedEpisodes();

    IEnumerable<EpisodeCollected> GetCollectedEpisodes();
  }
}