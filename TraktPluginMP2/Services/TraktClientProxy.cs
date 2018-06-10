using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TraktApiSharp;
using TraktApiSharp.Authentication;
using TraktApiSharp.Exceptions;
using TraktApiSharp.Objects.Get.Collection;
using TraktApiSharp.Objects.Get.Movies;
using TraktApiSharp.Objects.Get.Shows;
using TraktApiSharp.Objects.Get.Shows.Episodes;
using TraktApiSharp.Objects.Get.Syncs.Activities;
using TraktApiSharp.Objects.Get.Users;
using TraktApiSharp.Objects.Get.Watched;
using TraktApiSharp.Objects.Post.Scrobbles.Responses;
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

    public TraktAuthorization TraktAuthorization
    {
      get { return base.Authorization; }
    }

    public TraktAuthorization GetAuthorization(string code)
    {
      TraktAuthorization result = null;
      try
      {
        result = Task.Run(() => base.OAuth.GetAuthorizationAsync(code)).Result;
      }
      catch (AggregateException aggregateException)
      {
        UnwrapAggregateException(aggregateException);
      }

      return result;
    }

    public TraktAuthorization RefreshAuthorization(string refreshToken)
    {
      TraktAuthorization result = null;
      try
      {
        result = Task.Run(() => base.OAuth.RefreshAuthorizationAsync(refreshToken)).Result;
      }
      catch (AggregateException aggregateException)
      {
        UnwrapAggregateException(aggregateException);
      }

      return result;
    }

    public TraktSyncHistoryPostResponse AddWatchedHistoryItems(TraktSyncHistoryPost historyPost)
    {
      TraktSyncHistoryPostResponse result = null;
      try
      {
        result = Task.Run(() => base.Sync.AddWatchedHistoryItemsAsync(historyPost)).Result;
      }
      catch (AggregateException aggregateException)
      {
        UnwrapAggregateException(aggregateException);
      }

      return result;
    }

    public TraktSyncCollectionPostResponse AddCollectionItems(TraktSyncCollectionPost collectionPost)
    {
      TraktSyncCollectionPostResponse result = new TraktSyncCollectionPostResponse();
      try
      {
        result = Task.Run(() => base.Sync.AddCollectionItemsAsync(collectionPost)).Result;
      }
      catch (AggregateException aggregateException)
      {
        UnwrapAggregateException(aggregateException);
      }

      return result;
    }

    public TraktSyncLastActivities GetLastActivities()
    {
      TraktSyncLastActivities result = null;
      try
      {
        result = Task.Run(() => base.Sync.GetLastActivitiesAsync()).Result;
      }
      catch (AggregateException aggregateException)
      {
        UnwrapAggregateException(aggregateException);
      }

      return result;
    }

    public IEnumerable<TraktWatchedMovie> GetWatchedMovies()
    {
      IEnumerable<TraktWatchedMovie> result = new List<TraktWatchedMovie>();
      try
      {
        result = Task.Run(() => base.Sync.GetWatchedMoviesAsync()).Result;
      }
      catch (AggregateException aggregateException)
      {
        UnwrapAggregateException(aggregateException);
      }

      return result;
    }

    public IEnumerable<TraktCollectionMovie> GetCollectedMovies()
    {
      IEnumerable<TraktCollectionMovie> result = new List<TraktCollectionMovie>();
      try
      {
        result = Task.Run(() => base.Sync.GetCollectionMoviesAsync()).Result;
      }
      catch (AggregateException aggregateException)
      {
        UnwrapAggregateException(aggregateException);
      }

      return result;
    }

    public IEnumerable<TraktWatchedShow> GetWatchedShows()
    {
      IEnumerable<TraktWatchedShow> result = null;
      try
      {
        result = Task.Run(() => base.Sync.GetWatchedShowsAsync()).Result;
      }
      catch (AggregateException aggregateException)
      {
        UnwrapAggregateException(aggregateException);
      }

      return result;
    }

    public IEnumerable<TraktCollectionShow> GetCollectedShows()
    {
      IEnumerable<TraktCollectionShow> result = null;
      try
      {
        result = Task.Run(() => base.Sync.GetCollectionShowsAsync()).Result;
      }
      catch (AggregateException aggregateException)
      {
        UnwrapAggregateException(aggregateException);
      }

      return result;
    }

    public TraktMovieScrobblePostResponse StartScrobbleMovie(TraktMovie movie, float progress, string appVersion = null,
      DateTime? appBuildDate = null)
    {
      TraktMovieScrobblePostResponse result = null;
      try
      {
        result = Task.Run(() => base.Scrobble.StartMovieAsync(movie, progress, appVersion, appBuildDate)).Result;
      }
      catch (AggregateException aggregateException)
      {
        UnwrapAggregateException(aggregateException);
      }

      return result;
    }

    public TraktMovieScrobblePostResponse StopScrobbleMovie(TraktMovie movie, float progress, string appVersion = null,
      DateTime? appBuildDate = null)
    {
      TraktMovieScrobblePostResponse result = null;
      try
      {
        result = Task.Run(() => base.Scrobble.StopMovieAsync(movie, progress, appVersion, appBuildDate)).Result;
      }
      catch (AggregateException aggregateException)
      {
        UnwrapAggregateException(aggregateException);
      }

      return result;
    }

    public TraktEpisodeScrobblePostResponse StartScrobbleEpisode(TraktEpisode episode, TraktShow traktShow, float progress, string appVersion = null,
      DateTime? appBuildDate = null)
    {
      TraktEpisodeScrobblePostResponse result = null;
      try
      {
        result = Task.Run(() => base.Scrobble.StartEpisodeWithShowAsync(episode, traktShow, progress, appVersion, appBuildDate)).Result;
      }
      catch (AggregateException aggregateException)
      {
        UnwrapAggregateException(aggregateException);
      }

      return result;
    }

    public TraktEpisodeScrobblePostResponse StopScrobbleEpisode(TraktEpisode episode, TraktShow traktShow, float progress, string appVersion = null,
      DateTime? appBuildDate = null)
    {
      TraktEpisodeScrobblePostResponse result = null;
      try
      {
        result = Task.Run(() => base.Scrobble.StopEpisodeWithShowAsync(episode, traktShow, progress, appVersion, appBuildDate)).Result;
      }
      catch (AggregateException aggregateException)
      {
        UnwrapAggregateException(aggregateException);
      }

      return result;
    }

    public TraktUserSettings GetTraktUserSettings()
    {
      TraktUserSettings result = null;
      try
      {
        result = Task.Run(() => base.Users.GetSettingsAsync()).Result;
      }
      catch (AggregateException aggregateException)
      {
        UnwrapAggregateException(aggregateException);
      }

      return result;
    }

    private void UnwrapAggregateException(AggregateException aggregateException)
    {
      aggregateException.Handle((x) =>
      {
        if (x is TraktException)
        {
          throw new TraktException(x.Message);
        }
        throw new TraktException("Unknown error in TraktApiSharp.");
      });
    }
  }
}