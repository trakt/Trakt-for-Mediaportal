using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NSubstitute;
using Tests.TestData.Cache;
using TraktApiSharp.Objects.Get.Collection;
using TraktApiSharp.Objects.Get.Syncs.Activities;
using TraktApiSharp.Objects.Get.Watched;
using TraktPluginMP2.Services;
using TraktPluginMP2.Settings;
using Xunit;

namespace Tests
{
  public class TraktCacheTests
  {
    [Theory]
    [ClassData(typeof(UnWatchedMoviesTestData))]
    public void GetUnWatchedMovies(List<TraktWatchedMovie> onlineWatchedMovies, TraktSyncLastActivities onlineLastSyncActivities, 
      TraktSyncLastActivities savedLastSyncActivities, int expectedUnWatchedMoviesCount)
    {
      // Arrange
      ITraktClient traktClient = Substitute.For<ITraktClient>();
      traktClient.GetWatchedMovies().Returns(onlineWatchedMovies);
      traktClient.GetLastActivities().Returns(onlineLastSyncActivities);

      IFileOperations fileOperations = Substitute.For<IFileOperations>();
      fileOperations.FileReadAllText(Arg.Any<string>()).Returns(GetCacheJson("Movies.Watched"));

      IMediaPortalServices mediaPortalServices = GetMockedMpServices(savedLastSyncActivities);
      TraktCache traktCache = new TraktCache(mediaPortalServices, traktClient, fileOperations);

      // Act
      int actualUnWatchedMoviesCount = traktCache.GetUnWatchedMovies().Count();

      // Assert
      Assert.Equal(expectedUnWatchedMoviesCount, actualUnWatchedMoviesCount);
    }

    [Theory]
    [ClassData(typeof(WatchedMoviesTestData))]
    public void GetWatchedMovies(List<TraktWatchedMovie> onlineWatchedMovies, TraktSyncLastActivities onlineLastSyncActivities,
      TraktSyncLastActivities savedLastSyncActivities, int expectedWatchedMoviesCount)
    {
      // Arrange
      ITraktClient traktClient = Substitute.For<ITraktClient>();
      traktClient.GetWatchedMovies().Returns(onlineWatchedMovies);
      traktClient.GetLastActivities().Returns(onlineLastSyncActivities);

      IFileOperations fileOperations = Substitute.For<IFileOperations>();
      fileOperations.FileReadAllText(Arg.Any<string>()).Returns(GetCacheJson("Movies.Watched"));

      IMediaPortalServices mediaPortalServices = GetMockedMpServices(savedLastSyncActivities);
      TraktCache traktCache = new TraktCache(mediaPortalServices, traktClient, fileOperations);

      // Act
      int actualWatchedMoviesCount = traktCache.GetWatchedMovies().Count();

      // Assert
      Assert.Equal(expectedWatchedMoviesCount, actualWatchedMoviesCount);
    }

    [Theory]
    [ClassData(typeof(CollectedMoviesTestData))]
    public void GetCollectedMovies(List<TraktCollectionMovie> onlineCollectedMovies, TraktSyncLastActivities onlineLastSyncActivities, 
      TraktSyncLastActivities savedLastSyncActivities, int expectedCollectedMoviesCount)
    {
      // Arrange
      ITraktClient traktClient = Substitute.For<ITraktClient>();
      traktClient.GetCollectedMovies().Returns(onlineCollectedMovies);
      traktClient.GetLastActivities().Returns(onlineLastSyncActivities);

      IFileOperations fileOperations = Substitute.For<IFileOperations>();
      fileOperations.FileReadAllText(Arg.Any<string>()).Returns(GetCacheJson("Movies.Collected"));

      IMediaPortalServices mediaPortalServices = GetMockedMpServices(savedLastSyncActivities);
      TraktCache traktCache = new TraktCache(mediaPortalServices, traktClient, fileOperations);

      // Act
      int actualCollectedMoviesCount = traktCache.GetCollectedMovies().Count();

      // Assert
      Assert.Equal(expectedCollectedMoviesCount, actualCollectedMoviesCount);
    }

    [Theory]
    [ClassData(typeof(UnWatchedEpisodesTestData))]
    public void GetUnWatchedEpisodes(List<TraktWatchedShow> onlineWatchedShows, TraktSyncLastActivities onlineLastSyncActivities,
      TraktSyncLastActivities savedLastSyncActivities, int expectedUnWatchedEpisodesCount)
    {
      // Arrange
      ITraktClient traktClient = Substitute.For<ITraktClient>();
      traktClient.GetWatchedShows().Returns(onlineWatchedShows);
      traktClient.GetLastActivities().Returns(onlineLastSyncActivities);

      IFileOperations fileOperations = Substitute.For<IFileOperations>();
      fileOperations.FileReadAllText(Arg.Any<string>()).Returns(GetCacheJson("Episodes.Watched"));

      IMediaPortalServices mediaPortalServices = GetMockedMpServices(savedLastSyncActivities);
      TraktCache traktCache = new TraktCache(mediaPortalServices, traktClient, fileOperations);

      // Act
      int actualUnWatchedEpisodesCount = traktCache.GetUnWatchedEpisodes().Count();

      // Assert
      Assert.Equal(expectedUnWatchedEpisodesCount, actualUnWatchedEpisodesCount);
    }

    [Theory]
    [ClassData(typeof(WatchedEpisodesTestData))]
    public void GetWatchedEpisodes(List<TraktWatchedShow> onlineWatchedShows, TraktSyncLastActivities onlineLastSyncActivities,
      TraktSyncLastActivities savedLastSyncActivities, int expectedWatchedEpisodesCount)
    {
      // Arrange
      ITraktClient traktClient = Substitute.For<ITraktClient>();
      traktClient.GetWatchedShows().Returns(onlineWatchedShows);
      traktClient.GetLastActivities().Returns(onlineLastSyncActivities);

      IFileOperations fileOperations = Substitute.For<IFileOperations>();
      fileOperations.FileReadAllText(Arg.Any<string>()).Returns(GetCacheJson("Episodes.Watched"));

      IMediaPortalServices mediaPortalServices = GetMockedMpServices(savedLastSyncActivities);
      TraktCache traktCache = new TraktCache(mediaPortalServices, traktClient, fileOperations);

      // Act
      int actualWatchedEpisodesCount = traktCache.GetWatchedEpisodes().Count();

      // Assert
      Assert.Equal(expectedWatchedEpisodesCount, actualWatchedEpisodesCount);
    }

    [Theory]
    [ClassData(typeof(CollectedEpisodesTestData))]
    public void GetCollectedEpisodes(List<TraktCollectionShow> onlineCollectedShows, TraktSyncLastActivities onlineLastSyncActivities,
      TraktSyncLastActivities savedLastSyncActivities, int expectedCollectedEpisodesCount)
    {
      // Arrange
      ITraktClient traktClient = Substitute.For<ITraktClient>();
      traktClient.GetCollectedShows().Returns(onlineCollectedShows);
      traktClient.GetLastActivities().Returns(onlineLastSyncActivities);

      IFileOperations fileOperations = Substitute.For<IFileOperations>();
      fileOperations.FileReadAllText(Arg.Any<string>()).Returns(GetCacheJson("Episodes.Collected"));

      IMediaPortalServices mediaPortalServices = GetMockedMpServices(savedLastSyncActivities);
      TraktCache traktCache = new TraktCache(mediaPortalServices, traktClient, fileOperations);
      
      // Act
      int actualCollectedEpisodesCount = traktCache.GetCollectedEpisodes().Count();

      // Assert
      Assert.Equal(expectedCollectedEpisodesCount, actualCollectedEpisodesCount);
    }

    private IMediaPortalServices GetMockedMpServices(TraktSyncLastActivities lastSyncActivities)
    {
      IMediaPortalServices mediaPortalServices = Substitute.For<IMediaPortalServices>();
      TraktPluginSettings settings = new TraktPluginSettings
      {
        LastSyncActivities = lastSyncActivities
      };
      mediaPortalServices.GetSettingsManager().Load<TraktPluginSettings>().Returns(settings);

      return mediaPortalServices;
    }

    private static string GetCacheJson(string filename)
    {
      string result = String.Empty;
      Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Tests.TestData.Cache." + filename + ".json");
      if (stream != null)
      {
        StreamReader reader = new StreamReader(stream);
        result = reader.ReadToEnd();
      }

      return result;
    }
  }
}