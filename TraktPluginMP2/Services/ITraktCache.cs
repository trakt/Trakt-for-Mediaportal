using System.Collections.Generic;
using TraktApiSharp.Objects.Get.Collection;
using TraktApiSharp.Objects.Get.Movies;
using TraktApiSharp.Objects.Get.Watched;
using TraktApiSharp.Objects.Post.Syncs.Collection;
using TraktApiSharp.Objects.Post.Syncs.History;
using TraktApiSharp.Objects.Post.Syncs.Responses;
using TraktPluginMP2.Structures;

namespace TraktPluginMP2.Services
{
  public interface ITraktCache
  {
    bool RefreshData();

    void Save();

    IEnumerable<TraktMovie> GetUnWatchedMoviesFromTrakt();

    IEnumerable<TraktWatchedMovie> GetWatchedMoviesFromTrakt();

    IEnumerable<TraktCollectionMovie> GetCollectedMoviesFromTrakt();

    void AddMoviesToWatchHistory(List<TraktSyncHistoryPostMovie> movies);

    void RemoveMoviesFromWatchHistory(IEnumerable<TraktSyncPostResponseNotFoundItem<TraktMovieIds>> movies);

    void AddMoviesToCollection(List<TraktSyncCollectionPostMovie> movies);

    void RemoveMoviesFromCollection(IEnumerable<TraktSyncPostResponseNotFoundItem<TraktMovieIds>> movies);

    IEnumerable<Episode> GetUnWatchedEpisodesFromTrakt();

    IEnumerable<EpisodeWatched> GetWatchedEpisodesFromTrakt(bool ignoreLastSyncTime = false);

    IEnumerable<EpisodeCollected> GetCollectedEpisodesFromTrakt(bool ignoreLastSyncTime = false);

    void AddEpisodesToWatchHistory(TraktSyncHistoryPostShow show);

    void AddEpisodesToCollection(TraktSyncCollectionPostShow show);
  }
}