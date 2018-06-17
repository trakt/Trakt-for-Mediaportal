using System;
using System.Collections.Generic;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Common.SystemCommunication;
using NSubstitute;
using Tests.TestData.Setup;
using TraktApiSharp.Authentication;
using TraktApiSharp.Objects.Get.Collection;
using TraktApiSharp.Objects.Get.Movies;
using TraktApiSharp.Objects.Get.Watched;
using TraktApiSharp.Objects.Post.Syncs.Collection;
using TraktApiSharp.Objects.Post.Syncs.Collection.Responses;
using TraktApiSharp.Objects.Post.Syncs.History;
using TraktApiSharp.Objects.Post.Syncs.History.Responses;
using TraktApiSharp.Objects.Post.Syncs.Responses;
using TraktPluginMP2;
using TraktPluginMP2.Models;
using TraktPluginMP2.Services;
using TraktPluginMP2.Structures;
using Xunit;

namespace Tests
{
  public class TraktSetupTests
  {
    [Theory]
    [ClassData(typeof(WatchedMoviesTestData))]
    public void AddWatchedMovieToTraktIfMediaLibraryAvailable(List<MediaItem> databaseMovies, List<TraktWatchedMovie> traktMovies, int? expectedMoviesCount)
    {
      // Arrange
      IMediaPortalServices mediaPortalServices = GetMockMediaPortalServices(databaseMovies);
      ITraktClient traktClient = Substitute.For<ITraktClient>();
      traktClient.AddCollectionItems(Arg.Any<TraktSyncCollectionPost>()).Returns(new TraktSyncCollectionPostResponse());
      traktClient.AddWatchedHistoryItems(Arg.Any<TraktSyncHistoryPost>()).Returns(
        new TraktSyncHistoryPostResponse {Added = new TraktSyncPostResponseGroup {Movies = expectedMoviesCount}});
      TraktAuthorization authorization = Substitute.For<TraktAuthorization>();
      authorization.AccessToken = "ValidToken";
      traktClient.TraktAuthorization.Returns(authorization);
      ITraktCache traktCache = Substitute.For<ITraktCache>();
      traktCache.GetWatchedMovies().Returns(traktMovies);
      IFileOperations fileOperations = Substitute.For<IFileOperations>();
      TraktSetupModelManager traktSetup = new TraktSetupModelManager(mediaPortalServices, traktClient, traktCache, fileOperations);

      // Act
      TraktSyncMoviesResult result = traktSetup.SyncMovies();

      // Assert
      Assert.Equal(expectedMoviesCount, result.AddedToTraktWatchedHistory);
    }

    [Theory]
    [ClassData(typeof(CollectedMoviesTestData))]
    public void AddCollectedMovieToTraktIfMediaLibraryAvailable(List<MediaItem> databaseMovies, List<TraktCollectionMovie> traktMovies, int? expectedMoviesCount)
    {
      // Arrange
      IMediaPortalServices mediaPortalServices = GetMockMediaPortalServices(databaseMovies);
      ITraktClient traktClient = Substitute.For<ITraktClient>();
      traktClient.AddCollectionItems(Arg.Any<TraktSyncCollectionPost>()).Returns(
        new TraktSyncCollectionPostResponse { Added = new TraktSyncPostResponseGroup { Movies = expectedMoviesCount } });
      traktClient.AddWatchedHistoryItems(Arg.Any<TraktSyncHistoryPost>()).Returns(
        new TraktSyncHistoryPostResponse());
      TraktAuthorization authorization = Substitute.For<TraktAuthorization>();
      authorization.AccessToken = "ValidToken";
      traktClient.TraktAuthorization.Returns(authorization);
      ITraktCache traktCache = Substitute.For<ITraktCache>();
      traktCache.GetCollectedMovies().Returns(traktMovies);
      IFileOperations fileOperations = Substitute.For<IFileOperations>();
      TraktSetupModelManager traktSetup = new TraktSetupModelManager(mediaPortalServices, traktClient, traktCache, fileOperations);

      // Act
      TraktSyncMoviesResult result = traktSetup.SyncMovies();

      // Assert
      Assert.Equal(expectedMoviesCount, result.AddedToTraktCollection);
    }

    [Theory]
    [ClassData(typeof(TraktUnwatchedMoviesTestData))]
    public void MarkMovieAsUnwatchedIfMediaLibraryAvailable(List<MediaItem> databaseMovies, List<TraktMovie> traktMovies, int expectedMoviesCount)
    {
      // Arrange
      IMediaPortalServices mediaPortalServices = GetMockMediaPortalServices(databaseMovies);
      ITraktClient traktClient = Substitute.For<ITraktClient>();
      traktClient.AddCollectionItems(Arg.Any<TraktSyncCollectionPost>()).Returns(new TraktSyncCollectionPostResponse());
      traktClient.AddWatchedHistoryItems(Arg.Any<TraktSyncHistoryPost>()).Returns(new TraktSyncHistoryPostResponse());
      TraktAuthorization authorization = Substitute.For<TraktAuthorization>();
      authorization.AccessToken = "ValidToken";
      traktClient.TraktAuthorization.Returns(authorization);
      ITraktCache traktCache = Substitute.For<ITraktCache>();
      traktCache.GetUnWatchedMovies().Returns(traktMovies);
      IFileOperations fileOperations = Substitute.For<IFileOperations>();
      TraktSetupModelManager traktSetup = new TraktSetupModelManager(mediaPortalServices, traktClient, traktCache, fileOperations);

      // Act
      TraktSyncMoviesResult result = traktSetup.SyncMovies();

      // Assert
      Assert.Equal(expectedMoviesCount, result.MarkedAsUnWatchedInLibrary);
    }

    [Theory]
    [ClassData(typeof(TraktWatchedMoviesTestData))]
    public void MarkMovieAsWatchedIfMediaLibraryAndCacheAvailable(List<MediaItem> databaseMovies, List<TraktWatchedMovie> traktMovies, int expectedMoviesCount)
    {
      // Arrange
      IMediaPortalServices mediaPortalServices = GetMockMediaPortalServices(databaseMovies);
      ITraktClient traktClient = Substitute.For<ITraktClient>();
      traktClient.AddCollectionItems(Arg.Any<TraktSyncCollectionPost>()).Returns(new TraktSyncCollectionPostResponse());
      traktClient.AddWatchedHistoryItems(Arg.Any<TraktSyncHistoryPost>()).Returns(new TraktSyncHistoryPostResponse());
      TraktAuthorization authorization = Substitute.For<TraktAuthorization>();
      authorization.AccessToken = "ValidToken";
      traktClient.TraktAuthorization.Returns(authorization);
      ITraktCache traktCache = Substitute.For<ITraktCache>();
      traktCache.GetWatchedMovies().Returns(traktMovies);
      IFileOperations fileOperations = Substitute.For<IFileOperations>();
      TraktSetupModelManager traktSetup = new TraktSetupModelManager(mediaPortalServices, traktClient, traktCache, fileOperations);

      // Act
      TraktSyncMoviesResult result = traktSetup.SyncMovies();

      // Assert
      Assert.Equal(expectedMoviesCount, result.MarkedAsWatchedInLibrary);
    }

    [Theory]
    [ClassData(typeof(CollectedEpisodesTestData))]
    public void AddCollectedEpisodeToTraktIfMediaLibraryAvailable(IList<MediaItem> databaseEpisodes, IList<EpisodeCollected> traktEpisodes, int? expectedEpisodesCount)
    {
      // Arrange
      IMediaPortalServices mediaPortalServices = GetMockMediaPortalServices(databaseEpisodes);
      ITraktClient traktClient = Substitute.For<ITraktClient>();
      traktClient.AddCollectionItems(Arg.Any<TraktSyncCollectionPost>()).Returns(
        new TraktSyncCollectionPostResponse {Added = new TraktSyncPostResponseGroup {Episodes = expectedEpisodesCount} });
      traktClient.AddWatchedHistoryItems(Arg.Any<TraktSyncHistoryPost>()).Returns(new TraktSyncHistoryPostResponse());
      TraktAuthorization authorization = Substitute.For<TraktAuthorization>();
      authorization.AccessToken = "ValidToken";
      traktClient.TraktAuthorization.Returns(authorization);
      ITraktCache traktCache = Substitute.For<ITraktCache>();
      traktCache.GetCollectedEpisodes().Returns(traktEpisodes);
      IFileOperations fileOperations = Substitute.For<IFileOperations>();
      TraktSetupModelManager traktSetup = new TraktSetupModelManager(mediaPortalServices, traktClient, traktCache, fileOperations);

      // Act
      TraktSyncEpisodesResult result = traktSetup.SyncSeries();

      // Assert
      Assert.Equal(expectedEpisodesCount, result.AddedToTraktCollection);
    }

    [Theory]
    [ClassData(typeof(WatchedEpisodesTestData))]
    public void AddWatchedEpisodeToTraktIfMediaLibraryAvailable(IList<MediaItem> databaseEpisodes, IList<EpisodeWatched> traktEpisodes, int? expectedEpisodesCount)
    {
      // Arrange
      IMediaPortalServices mediaPortalServices = GetMockMediaPortalServices(databaseEpisodes);
      ITraktClient traktClient = Substitute.For<ITraktClient>();
      traktClient.AddCollectionItems(Arg.Any<TraktSyncCollectionPost>()).Returns(new TraktSyncCollectionPostResponse());
      traktClient.AddWatchedHistoryItems(Arg.Any<TraktSyncHistoryPost>()).Returns(
        new TraktSyncHistoryPostResponse { Added = new TraktSyncPostResponseGroup { Episodes = expectedEpisodesCount } });
      TraktAuthorization authorization = Substitute.For<TraktAuthorization>();
      authorization.AccessToken = "ValidToken";
      traktClient.TraktAuthorization.Returns(authorization);
      ITraktCache traktCache = Substitute.For<ITraktCache>();
      traktCache.GetWatchedEpisodes().Returns(traktEpisodes);
      IFileOperations fileOperations = Substitute.For<IFileOperations>();
      TraktSetupModelManager traktSetup = new TraktSetupModelManager(mediaPortalServices, traktClient, traktCache, fileOperations);

      // Act
      TraktSyncEpisodesResult result = traktSetup.SyncSeries();

      // Assert
      Assert.Equal(expectedEpisodesCount, result.AddedToTraktWatchedHistory);
    }

    [Theory]
    [ClassData(typeof(TraktUnWatchedEpisodesTestData))]
    public void MarkEpisodeAsUnwatchedIfMediaLibraryAvailable(List<MediaItem> databaseEpisodes, List<Episode> traktEpisodes, int expectedEpisodessCount)
    {
      // Arrange
      IMediaPortalServices mediaPortalServices = GetMockMediaPortalServices(databaseEpisodes);
      ITraktClient traktClient = Substitute.For<ITraktClient>();
      traktClient.AddCollectionItems(Arg.Any<TraktSyncCollectionPost>()).Returns(new TraktSyncCollectionPostResponse());
      traktClient.AddWatchedHistoryItems(Arg.Any<TraktSyncHistoryPost>()).Returns(new TraktSyncHistoryPostResponse());
      TraktAuthorization authorization = Substitute.For<TraktAuthorization>();
      authorization.AccessToken = "ValidToken";
      traktClient.TraktAuthorization.Returns(authorization);
      ITraktCache traktCache = Substitute.For<ITraktCache>();
      traktCache.GetUnWatchedEpisodes().Returns(traktEpisodes);
      IFileOperations fileOperations = Substitute.For<IFileOperations>();
      TraktSetupModelManager traktSetup = new TraktSetupModelManager(mediaPortalServices, traktClient, traktCache, fileOperations);

      // Act
      TraktSyncEpisodesResult result = traktSetup.SyncSeries();

      // Assert
      Assert.Equal(expectedEpisodessCount, result.MarkedAsUnWatchedInLibrary);
    }

    [Theory]
    [ClassData(typeof(TraktWatchedEpisodesTestData))]
    public void MarkEpisodeAsWatchedIfMediaLibraryAvailable(List<MediaItem> databaseEpisodes, List<EpisodeWatched> traktEpisodes, int expectedEpisodesCount)
    {
      // Arrange
      IMediaPortalServices mediaPortalServices = GetMockMediaPortalServices(databaseEpisodes);
      ITraktClient traktClient = Substitute.For<ITraktClient>();
      traktClient.AddCollectionItems(Arg.Any<TraktSyncCollectionPost>()).Returns(new TraktSyncCollectionPostResponse());
      traktClient.AddWatchedHistoryItems(Arg.Any<TraktSyncHistoryPost>()).Returns(new TraktSyncHistoryPostResponse());
      TraktAuthorization authorization = Substitute.For<TraktAuthorization>();
      authorization.AccessToken = "ValidToken";
      traktClient.TraktAuthorization.Returns(authorization);
      ITraktCache traktCache = Substitute.For<ITraktCache>();
      traktCache.GetWatchedEpisodes().Returns(traktEpisodes);
      IFileOperations fileOperations = Substitute.For<IFileOperations>();
      TraktSetupModelManager traktSetup = new TraktSetupModelManager(mediaPortalServices, traktClient, traktCache, fileOperations);

      // Act
      TraktSyncEpisodesResult result = traktSetup.SyncSeries();

      // Assert
      Assert.Equal(expectedEpisodesCount, result.MarkedAsWatchedInLibrary);
    }

    private IMediaPortalServices GetMockMediaPortalServices(IList<MediaItem> databaseMediaItems)
    {
      IMediaPortalServices mediaPortalServices = Substitute.For<IMediaPortalServices>();
      mediaPortalServices.MarkAsWatched(Arg.Any<MediaItem>()).Returns(true);
      mediaPortalServices.MarkAsUnWatched(Arg.Any<MediaItem>()).Returns(true);
      
      IContentDirectory contentDirectory = Substitute.For<IContentDirectory>();
      contentDirectory.SearchAsync(Arg.Any<MediaItemQuery>(), true, null, false).Returns(databaseMediaItems);
      mediaPortalServices.GetServerConnectionManager().ContentDirectory.Returns(contentDirectory);

      return mediaPortalServices;
    }
  }
}