using System.Collections.Generic;
using System.Threading.Tasks;
using TraktApiSharp.Authentication;
using TraktApiSharp.Modules;
using TraktApiSharp.Objects.Get.Collection;
using TraktApiSharp.Objects.Get.Syncs.Activities;
using TraktApiSharp.Objects.Get.Watched;
using TraktApiSharp.Objects.Post.Syncs.Collection;
using TraktApiSharp.Objects.Post.Syncs.Collection.Responses;
using TraktApiSharp.Objects.Post.Syncs.History;
using TraktApiSharp.Objects.Post.Syncs.History.Responses;

namespace TraktPluginMP2.Services
{
  public interface ITraktClient
  {
    TraktAuthorization GetAuthorization(string code);

    bool IsAuthorized { get; }

    string GetUsername();

    TraktAuthorization RefreshAuthorization(string refreshToken);

    TraktSyncHistoryPostResponse AddWatchedHistoryItems(TraktSyncHistoryPost historyPost);

    TraktSyncCollectionPostResponse AddCollectionItems(TraktSyncCollectionPost collectionPost);

    TraktSyncLastActivities GetLastActivities();

    IEnumerable<TraktWatchedMovie> GetWatchedMovies();

    IEnumerable<TraktCollectionMovie> GetCollectedMovies();

    IEnumerable<TraktWatchedShow> GetWatchedShows();

    IEnumerable<TraktCollectionShow> GetCollectedShows();

  }
}