using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TraktApiSharp.Attributes;
using TraktApiSharp.Authentication;
using TraktApiSharp.Modules;
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
  public interface ITraktClient
  {
    TraktAuthorization TraktAuthorization { get; }

    TraktAuthorization GetAuthorization(string code);

    TraktUserSettings GetTraktUserSettings();

    TraktAuthorization RefreshAuthorization(string refreshToken);

    TraktSyncHistoryPostResponse AddWatchedHistoryItems(TraktSyncHistoryPost historyPost);

    TraktSyncCollectionPostResponse AddCollectionItems(TraktSyncCollectionPost collectionPost);

    TraktSyncLastActivities GetLastActivities();

    IEnumerable<TraktWatchedMovie> GetWatchedMovies();

    IEnumerable<TraktCollectionMovie> GetCollectedMovies();

    IEnumerable<TraktWatchedShow> GetWatchedShows();

    IEnumerable<TraktCollectionShow> GetCollectedShows();

    TraktMovieScrobblePostResponse StartScrobbleMovie(TraktMovie movie, float progress, string appVersion = null, DateTime? appBuildDate = null);

    TraktMovieScrobblePostResponse StopScrobbleMovie(TraktMovie movie, float progress, string appVersion = null, DateTime? appBuildDate = null);

    TraktEpisodeScrobblePostResponse StartScrobbleEpisode(TraktEpisode episode, TraktShow traktShow, float progress, string appVersion = null, DateTime? appBuildDate = null);

    TraktEpisodeScrobblePostResponse StopScrobbleEpisode(TraktEpisode episode, TraktShow traktShow, float progress, string appVersion = null, DateTime? appBuildDate = null);
  }
}