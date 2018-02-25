using System.Collections.Generic;
using System.Threading.Tasks;
using TraktApiSharp;
using TraktApiSharp.Authentication;
using TraktApiSharp.Objects.Get.Watched;
using TraktApiSharp.Objects.Post.Syncs.Collection;
using TraktApiSharp.Objects.Post.Syncs.Collection.Responses;
using TraktApiSharp.Objects.Post.Syncs.History;
using TraktApiSharp.Objects.Post.Syncs.History.Responses;
using TraktApiSharp.Requests.Params;

namespace TraktPluginMP2.Services
{
  public class TraktClientProxy : TraktClient, ITraktClient
  {
    public TraktClientProxy(string clientId) : base(clientId)
    {
    }

    public TraktClientProxy(string clientId, string clientSecret) : base(clientId, clientSecret)
    {
    }

    public Task<TraktAuthorization> GetAuthorizationAsync(string code)
    {
      return base.OAuth.GetAuthorizationAsync(code);
    }

    public Task<IEnumerable<TraktWatchedMovie>> GetWatchedMoviesAsync(TraktExtendedInfo extendedInfo = null)
    {
      return base.Sync.GetWatchedMoviesAsync(extendedInfo);
    }

    public Task<TraktSyncHistoryPostResponse> AddWatchedHistoryItemsAsync(TraktSyncHistoryPost historyPost)
    {
      return base.Sync.AddWatchedHistoryItemsAsync(historyPost);
    }

    public Task<TraktSyncCollectionPostResponse> AddCollectionItemsAsync(TraktSyncCollectionPost collectionPost)
    {
      return base.Sync.AddCollectionItemsAsync(collectionPost);
    }
  }
}