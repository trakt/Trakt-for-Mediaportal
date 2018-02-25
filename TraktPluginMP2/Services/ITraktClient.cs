using System.Collections.Generic;
using System.Threading.Tasks;
using TraktApiSharp.Attributes;
using TraktApiSharp.Authentication;
using TraktApiSharp.Objects.Get.Watched;
using TraktApiSharp.Objects.Post.Syncs.Collection;
using TraktApiSharp.Objects.Post.Syncs.Collection.Responses;
using TraktApiSharp.Objects.Post.Syncs.History;
using TraktApiSharp.Objects.Post.Syncs.History.Responses;
using TraktApiSharp.Requests.Params;

namespace TraktPluginMP2.Services
{
  public interface ITraktClient
  {
    Task<TraktAuthorization> GetAuthorizationAsync(string code);

    Task<IEnumerable<TraktWatchedMovie>> GetWatchedMoviesAsync(TraktExtendedInfo extendedInfo = null);

    Task<TraktSyncHistoryPostResponse> AddWatchedHistoryItemsAsync(TraktSyncHistoryPost historyPost);

    Task<TraktSyncCollectionPostResponse> AddCollectionItemsAsync(TraktSyncCollectionPost collectionPost);
  }
}