using System;
using System.Collections.Generic;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Common.Messaging;
using MediaPortal.Common.Settings;
using MediaPortal.Common.SystemCommunication;
using NSubstitute;
using Tests.TestData.Setup;
using TraktApiSharp.Objects.Get.Collection;
using TraktApiSharp.Objects.Get.Movies;
using TraktApiSharp.Objects.Get.Watched;
using TraktPluginMP2.Models;
using TraktPluginMP2.Services;
using TraktPluginMP2.Settings;
using TraktPluginMP2.Structures;
using Xunit;

namespace Tests
{
  public class TraktSetupTests
  {
    [Theory]
    [ClassData(typeof(WatchedMoviesTestData))]
    public void AddWatchedMovieToTraktIfMediaLibraryAvailable(List<MediaItem> databaseMovies, List<TraktWatchedMovie> traktMovies, int expectedMoviesCount)
    {
      // Arrange
      IMediaPortalServices mediaPortalServices = GetMockMediaPortalServices(databaseMovies);
      ITraktClient traktClient = Substitute.For<ITraktClient>();
      ITraktCache traktCache = Substitute.For<ITraktCache>();
      traktCache.GetWatchedMovies().Returns(traktMovies);
      TraktSetupManager traktSetup = new TraktSetupManager(mediaPortalServices, traktClient, traktCache);

      // Act
      bool isSynced = traktSetup.SyncMovies();

      // Assert
      int actualMoviesCount = traktSetup.SyncWatchedMovies;
      Assert.True(isSynced);
      Assert.Equal(expectedMoviesCount, actualMoviesCount);
    }

    [Theory]
    [ClassData(typeof(CollectedMoviesTestData))]
    public void AddCollectedMovieToTraktIfMediaLibraryAvailable(List<MediaItem> databaseMovies, List<TraktCollectionMovie> traktMovies, int expectedMoviesCount)
    {
      // Arrange
      IMediaPortalServices mediaPortalServices = GetMockMediaPortalServices(databaseMovies);
      ITraktClient traktClient = Substitute.For<ITraktClient>();
      ITraktCache traktCache = Substitute.For<ITraktCache>();
      traktCache.GetCollectedMovies().Returns(traktMovies);
      TraktSetupManager traktSetup = new TraktSetupManager(mediaPortalServices, traktClient, traktCache);

      // Act
      bool isSynced = traktSetup.SyncMovies();

      // Assert
      int actualMoviesCount = traktSetup.SyncCollectedMovies;
      Assert.True(isSynced);
      Assert.Equal(expectedMoviesCount, actualMoviesCount);
    }

    [Theory]
    [ClassData(typeof(TraktUnwatchedMoviesTestData))]
    public void MarkMovieAsUnwatchedIfMediaLibraryAvailable(List<MediaItem> databaseMovies, List<TraktMovie> traktMovies, int expectedMoviesCount)
    {
      // Arrange
      IMediaPortalServices mediaPortalServices = GetMockMediaPortalServices(databaseMovies);
      ITraktClient traktClient = Substitute.For<ITraktClient>();
      ITraktCache traktCache = Substitute.For<ITraktCache>();
      traktCache.GetUnWatchedMovies().Returns(traktMovies);
      TraktSetupManager traktSetup = new TraktSetupManager(mediaPortalServices, traktClient, traktCache);

      // Act
      bool isSynced = traktSetup.SyncMovies();

      // Assert
      int actualMoviesCount = traktSetup.MarkUnWatchedMovies;
      Assert.True(isSynced);
      Assert.Equal(expectedMoviesCount, actualMoviesCount);
    }

    [Theory]
    [ClassData(typeof(TraktWatchedMoviesTestData))]
    public void MarkMovieAsWatchedIfMediaLibraryAndCacheAvailable(List<MediaItem> databaseMovies, List<TraktWatchedMovie> traktMovies, int expectedMoviesCount)
    {
      // Arrange
      IMediaPortalServices mediaPortalServices = GetMockMediaPortalServices(databaseMovies);
      ITraktClient traktClient = Substitute.For<ITraktClient>();
      ITraktCache traktCache = Substitute.For<ITraktCache>();
      traktCache.GetWatchedMovies().Returns(traktMovies);
      TraktSetupManager traktSetup = new TraktSetupManager(mediaPortalServices, traktClient, traktCache);

      // Act
      bool isSynced = traktSetup.SyncMovies();

      // Assert
      int actualMoviesCount = traktSetup.MarkWatchedMovies;
      Assert.True(isSynced);
      Assert.Equal(expectedMoviesCount, actualMoviesCount);
    }

    [Theory]
    [ClassData(typeof(CollectedEpisodesTestData))]
    public void AddCollectedEpisodeToTraktIfMediaLibraryAvailable(IList<MediaItem> databaseEpisodes, IList<Episode> traktEpisodes, int expectedEpisodesCount)
    {
      // Arrange
      IMediaPortalServices mediaPortalServices = GetMockMediaPortalServices(databaseEpisodes);
      ITraktClient traktClient = Substitute.For<ITraktClient>();
      ITraktCache traktCache = Substitute.For<ITraktCache>();
      traktCache.GetUnWatchedEpisodes().Returns(traktEpisodes);
      TraktSetupManager traktSetup = new TraktSetupManager(mediaPortalServices, traktClient, traktCache);

      // Act
      bool isSynced = traktSetup.SyncSeries();

      // Assert
      int actualEpisodesCount = traktSetup.SyncCollectedEpisodes;
      Assert.True(isSynced);
      Assert.Equal(expectedEpisodesCount, actualEpisodesCount);
    }

    [Theory]
    [ClassData(typeof(WatchedEpisodesTestData))]
    public void AddWatchedEpisodeToTraktIfMediaLibraryAvailable(IList<MediaItem> databaseEpisodes, IList<EpisodeWatched> traktEpisodes, int expectedEpisodesCount)
    {
      // Arrange
      IMediaPortalServices mediaPortalServices = GetMockMediaPortalServices(databaseEpisodes);
      ITraktClient traktClient = Substitute.For<ITraktClient>();
      ITraktCache traktCache = Substitute.For<ITraktCache>();
      traktCache.GetWatchedEpisodes().Returns(traktEpisodes);
      TraktSetupManager traktSetup = new TraktSetupManager(mediaPortalServices, traktClient ,traktCache);

      // Act
      bool isSynced = traktSetup.SyncSeries();

      // Assert
      int actualEpisodesCount = traktSetup.SyncWatchedEpisodes;
      Assert.True(isSynced);
      Assert.Equal(expectedEpisodesCount, actualEpisodesCount);
    }

    [Theory]
    [ClassData(typeof(TraktUnWatchedEpisodesTestData))]
    public void MarkEpisodeAsUnwatchedIfMediaLibraryAvailable(List<MediaItem> databaseEpisodes, List<Episode> traktEpisodes, int expectedEpisodessCount)
    {
      // Arrange
      IMediaPortalServices mediaPortalServices = GetMockMediaPortalServices(databaseEpisodes);
      ITraktClient traktClient = Substitute.For<ITraktClient>();
      ITraktCache traktCache = Substitute.For<ITraktCache>();
      traktCache.GetUnWatchedEpisodes().Returns(traktEpisodes);
      TraktSetupManager traktSetup = new TraktSetupManager(mediaPortalServices, traktClient, traktCache);

      // Act
      bool isSynced = traktSetup.SyncSeries();

      // Assert
      int actualEpisodesCount = traktSetup.MarkUnWatchedEpisodes;
      Assert.True(isSynced);
      Assert.Equal(expectedEpisodessCount, actualEpisodesCount);
    }

    [Theory]
    [ClassData(typeof(TraktWatchedEpisodesTestData))]
    public void MarkEpisodeAsWatchedIfMediaLibraryAvailable(List<MediaItem> databaseEpisodes, List<EpisodeWatched> traktEpisodes, int expectedEpisodesCount)
    {
      // Arrange
      IMediaPortalServices mediaPortalServices = GetMockMediaPortalServices(databaseEpisodes);
      ITraktClient traktClient = Substitute.For<ITraktClient>();
      ITraktCache traktCache = Substitute.For<ITraktCache>();
      traktCache.GetWatchedEpisodes().Returns(traktEpisodes);
      TraktSetupManager traktSetup = new TraktSetupManager(mediaPortalServices, traktClient, traktCache);

      // Act
      bool isSynced = traktSetup.SyncSeries();

      // Assert
      int actualEpisodesCount = traktSetup.MarkWatchedEpisodes;
      Assert.True(isSynced);
      Assert.Equal(expectedEpisodesCount, actualEpisodesCount);
    }

    

    private IMediaPortalServices GetMockMediaPortalServices(IList<MediaItem> databaseMediaItems)
    {
      IMediaPortalServices mediaPortalServices = Substitute.For<IMediaPortalServices>();
      mediaPortalServices.MarkAsWatched(Arg.Any<MediaItem>()).Returns(true);
      mediaPortalServices.MarkAsUnWatched(Arg.Any<MediaItem>()).Returns(true);
      ISettingsManager settingsManager = Substitute.For<ISettingsManager>();
      TraktPluginSettings traktPluginSettings = new TraktPluginSettings { SyncBatchSize = 100 };
      settingsManager.Load<TraktPluginSettings>().Returns(traktPluginSettings);
      mediaPortalServices.GetSettingsManager().Returns(settingsManager);
      IContentDirectory contentDirectory = Substitute.For<IContentDirectory>();
      contentDirectory.SearchAsync(Arg.Any<MediaItemQuery>(), true, null, false).Returns(databaseMediaItems);
      mediaPortalServices.GetServerConnectionManager().ContentDirectory.Returns(contentDirectory);

      return mediaPortalServices;
    }
  }
}