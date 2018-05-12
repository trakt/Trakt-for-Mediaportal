using System.Collections.Generic;
using System.Threading.Tasks;
using TraktApiSharp;
using TraktApiSharp.Authentication;
using TraktApiSharp.Objects.Get.Collection;
using TraktApiSharp.Objects.Get.Syncs.Activities;
using TraktApiSharp.Objects.Get.Watched;
using TraktApiSharp.Objects.Post.Syncs.Collection;
using TraktApiSharp.Objects.Post.Syncs.Collection.Responses;
using TraktApiSharp.Objects.Post.Syncs.History;
using TraktApiSharp.Objects.Post.Syncs.History.Responses;

namespace TraktPluginMP2.Services
{
  public class TraktClientProxy : TraktClient, ITraktClient
  {
    public TraktClientProxy(string clientId, string clientSecret) : base(clientId, clientSecret)
    {
    }

    public bool IsAuthorized
    {
      get { return base.Authentication.IsAuthorized; }
    }

    public TraktAuthorization GetAuthorization(string code)
    {
      return base.OAuth.GetAuthorizationAsync(code).Result;
    }

    public TraktAuthorization RefreshAuthorization(string refreshToken)
    {
      return base.OAuth.RefreshAuthorizationAsync(refreshToken).Result;
    }

    public TraktSyncHistoryPostResponse AddWatchedHistoryItems(TraktSyncHistoryPost historyPost)
    {
      return base.Sync.AddWatchedHistoryItemsAsync(historyPost).Result;
    }

    public TraktSyncCollectionPostResponse AddCollectionItems(TraktSyncCollectionPost collectionPost)
    {
      return base.Sync.AddCollectionItemsAsync(collectionPost).Result;
    }

    public TraktSyncLastActivities GetLastActivities()
    {
      return base.Sync.GetLastActivitiesAsync().Result;
    }

    public IEnumerable<TraktWatchedMovie> GetWatchedMovies()
    {
      return base.Sync.GetWatchedMoviesAsync().Result;
    }

    public IEnumerable<TraktCollectionMovie> GetCollectedMovies()
    {
      return base.Sync.GetCollectionMoviesAsync().Result;
    }

    public IEnumerable<TraktWatchedShow> GetWatchedShows()
    {
      return base.Sync.GetWatchedShowsAsync().Result;
    }

    public IEnumerable<TraktCollectionShow> GetCollectedShows()
    {
      return base.Sync.GetCollectionShowsAsync().Result;
    }

    public string GetUsername()
    {
      return base.Users.GetSettingsAsync().Result.User.Username;
    }
  }
}