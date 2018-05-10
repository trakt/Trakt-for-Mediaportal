using System.Collections.Generic;
using System.Threading.Tasks;
using TraktApiSharp;
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

    public Task<TraktSyncHistoryPostResponse> AddWatchedHistoryItemsAsync(TraktSyncHistoryPost historyPost)
    {
      return base.Sync.AddWatchedHistoryItemsAsync(historyPost);
    }

    public Task<TraktSyncCollectionPostResponse> AddCollectionItemsAsync(TraktSyncCollectionPost collectionPost)
    {
      return base.Sync.AddCollectionItemsAsync(collectionPost);
    }

    public TraktSyncLastActivities GetLastActivitiesAsync()
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