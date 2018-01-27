using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Common.Messaging;
using MediaPortal.Common.Settings;
using MediaPortal.Common.SystemCommunication;
using MediaPortal.Common.Threading;
using MediaPortal.UI.Presentation.Players;
using NSubstitute;
using TraktAPI.DataStructures;
using TraktPluginMP2.Handlers;
using TraktPluginMP2.Models;
using TraktPluginMP2.Services;
using TraktPluginMP2.Settings;
using TraktPluginMP2.Structures;
using Xunit;

namespace Tests
{
  public class TraktTests
  {
    [Fact]
    public void AuthorizeUserWhenLoggedToTrakt()
    {
      // Arrange
      IMediaPortalServices mediaPortalServices = Substitute.For<IMediaPortalServices>();
      ISettingsManager settingsManager = Substitute.For<ISettingsManager>();
      TraktPluginSettings traktPluginSettings = new TraktPluginSettings();
      settingsManager.Load<TraktPluginSettings>().Returns(traktPluginSettings);
      mediaPortalServices.GetSettingsManager().Returns(settingsManager);
      ITraktServices traktServices = Substitute.For<ITraktServices>();
      traktServices.GetTraktLogin().Login(Arg.Any<string>()).Returns(true);
      TraktSetupManager traktSetup = new TraktSetupManager(mediaPortalServices, traktServices)
      {
        PinCode = "12345678",
        Username = "User1"
      };

      // Act
      traktSetup.AuthorizeUser();

      // Assert
      Assert.Equal("[Trakt.LoggedIn]", traktSetup.TestStatus);
    }

    [Theory]
    [InlineData("1234567")]
    [InlineData("123456789")]
    [InlineData("")]
    public void UserNotAuthorizedWhenPinCodeInvalid(string invalidPinCode)
    {
      // Arrange
      IMediaPortalServices mediaPortalServices = Substitute.For<IMediaPortalServices>();
      ITraktServices traktServices = Substitute.For<ITraktServices>();
      TraktSetupManager traktSetup = new TraktSetupManager(mediaPortalServices, traktServices)
      {
        PinCode = invalidPinCode
      };

      // Act
      traktSetup.AuthorizeUser();

      // Assert
      Assert.Equal("[Trakt.WrongToken]", traktSetup.TestStatus);
    }

    [Fact]
    public void UserNotAuthorizedWhenLoginFailed()
    {
      // Arrange
      IMediaPortalServices mediaPortalServices = Substitute.For<IMediaPortalServices>();
      ITraktServices traktServices = Substitute.For<ITraktServices>();
      traktServices.GetTraktLogin().Login(Arg.Any<string>()).Returns(false);
      TraktSetupManager traktSetup = new TraktSetupManager(mediaPortalServices, traktServices)
      {
        PinCode = "12345678"
      };

      // Act
      traktSetup.AuthorizeUser();

      // Assert
      Assert.Equal("[Trakt.UnableLogin]", traktSetup.TestStatus);
    }

    [Fact]
    public void UserNotAuthorizedWhenUsernameInvalid()
    {
      // Arrange
      IMediaPortalServices mediaPortalServices = Substitute.For<IMediaPortalServices>();
      ITraktServices traktServices = Substitute.For<ITraktServices>();
      traktServices.GetTraktLogin().Login(Arg.Any<string>()).Returns(true);
      TraktSetupManager traktSetup = new TraktSetupManager(mediaPortalServices, traktServices)
      {
        PinCode = "12345678"
      };

      // Act
      traktSetup.AuthorizeUser();

      // Assert
      Assert.Equal("[Trakt.EmptyUsername]", traktSetup.TestStatus);
    }

    [Fact]
    public void StartSyncingMediaToTraktWhenUserAuthorized()
    {
      // Arrange
      IMediaPortalServices mediaPortalServices = Substitute.For<IMediaPortalServices>();
      ISettingsManager settingsManager = Substitute.For<ISettingsManager>();
      IThreadPool threadPool = Substitute.For<IThreadPool>();
      threadPool.Add(Arg.Any<DoWorkHandler>(), ThreadPriority.BelowNormal);
      TraktPluginSettings traktPluginSettings = new TraktPluginSettings {IsAuthorized = true};
      settingsManager.Load<TraktPluginSettings>().Returns(traktPluginSettings);
      mediaPortalServices.GetSettingsManager().Returns(settingsManager);
      mediaPortalServices.GetThreadPool().Returns(threadPool);
      ITraktServices traktServices = Substitute.For<ITraktServices>();
      traktServices.GetTraktCache().RefreshData().Returns(true);
      TraktSetupManager traktSetup = new TraktSetupManager(mediaPortalServices, traktServices);

      // Act
      traktSetup.SyncMediaToTrakt();

      // Assert
      Assert.True(traktSetup.IsSynchronizing);
    }

    [Fact]
    public void DoNotSyncMediaToTraktWhenUserNotAuthorized()
    {
      // Arrange
      IMediaPortalServices mediaPortalServices = Substitute.For<IMediaPortalServices>();
      ISettingsManager settingsManager = Substitute.For<ISettingsManager>();
      IThreadPool threadPool = Substitute.For<IThreadPool>();
      threadPool.Add(Arg.Any<DoWorkHandler>(), ThreadPriority.BelowNormal);
      TraktPluginSettings traktPluginSettings = new TraktPluginSettings { IsAuthorized = false };
      settingsManager.Load<TraktPluginSettings>().Returns(traktPluginSettings);
      mediaPortalServices.GetSettingsManager().Returns(settingsManager);
      mediaPortalServices.GetThreadPool().Returns(threadPool);
      ITraktServices traktServices = Substitute.For<ITraktServices>();
      traktServices.GetTraktCache().RefreshData().Returns(true);
      TraktSetupManager traktSetup = new TraktSetupManager(mediaPortalServices, traktServices);

      // Act
      traktSetup.SyncMediaToTrakt();

      // Assert
      Assert.False(traktSetup.IsSynchronizing);
    }

    [Fact]
    public void DoNotSyncMediaToTraktWhenCacheRefreshFalse()
    {
      // Arrange
      IMediaPortalServices mediaPortalServices = Substitute.For<IMediaPortalServices>();
      ISettingsManager settingsManager = Substitute.For<ISettingsManager>();
      IThreadPool threadPool = Substitute.For<IThreadPool>();
      threadPool.Add(Arg.Any<DoWorkHandler>(), ThreadPriority.BelowNormal);
      TraktPluginSettings traktPluginSettings = new TraktPluginSettings { IsAuthorized = true };
      settingsManager.Load<TraktPluginSettings>().Returns(traktPluginSettings);
      mediaPortalServices.GetSettingsManager().Returns(settingsManager);
      mediaPortalServices.GetThreadPool().Returns(threadPool);
      ITraktServices traktServices = Substitute.For<ITraktServices>();
      traktServices.GetTraktCache().RefreshData().Returns(false);
      TraktSetupManager traktSetup = new TraktSetupManager(mediaPortalServices, traktServices);

      // Act
      traktSetup.SyncMediaToTrakt();

      // Assert
      Assert.False(traktSetup.IsSynchronizing);
    }

    public static IEnumerable<object[]> WatchedMoviesTestData
    {
      get
      {
        return new List<object[]>
        {
          new object[]
          {
            new List<MediaItem>
            {
              new DatabaseMovie(ExternalIdentifierAspect.SOURCE_IMDB, ExternalIdentifierAspect.SOURCE_TMDB, "tt12345", "67890", "Movie_1", 2012, 1).Movie,
              new DatabaseMovie(ExternalIdentifierAspect.SOURCE_IMDB, ExternalIdentifierAspect.SOURCE_TMDB, "", "16729", "Movie_2", 2016, 2).Movie,
              new DatabaseMovie(ExternalIdentifierAspect.SOURCE_IMDB, ExternalIdentifierAspect.SOURCE_TMDB, "", "0", "Movie_3", 2011, 3).Movie
            },
            new List<TraktMovieWatched>
            {
              new TraktMovieWatched {Movie = new TraktMovie {Ids = new TraktMovieId {Imdb = "tt12345", Tmdb = 67890 }, Title = "Movie_1", Year = 2012}},
              new TraktMovieWatched {Movie = new TraktMovie {Ids = new TraktMovieId {Imdb = "tt67804", Tmdb = 16729 }, Title = "Movie_2", Year = 2016}},
              new TraktMovieWatched {Movie = new TraktMovie {Ids = new TraktMovieId {Imdb = "tt03412", Tmdb = 34251 }, Title = "Movie_3", Year = 2011}}
            },
            0
          },
          new object[]
          {
            new List<MediaItem>
            {
              new DatabaseMovie(ExternalIdentifierAspect.SOURCE_IMDB, ExternalIdentifierAspect.SOURCE_TMDB, "tt12345", "67890", "Movie_1", 2012, 1).Movie,
              new DatabaseMovie(ExternalIdentifierAspect.SOURCE_IMDB, ExternalIdentifierAspect.SOURCE_TMDB, "", "16729", "Movie_2", 2016, 2).Movie,
              new DatabaseMovie(ExternalIdentifierAspect.SOURCE_IMDB, ExternalIdentifierAspect.SOURCE_TMDB, "", "0", "Movie_3", 2011, 3).Movie
            },
            new List<TraktMovieWatched>
            {
              new TraktMovieWatched {Movie = new TraktMovie {Ids = new TraktMovieId {Imdb = "tt12345", Tmdb = 67890 }, Title = "Movie_1", Year = 2012}},
              new TraktMovieWatched {Movie = new TraktMovie {Ids = new TraktMovieId {Imdb = "tt67804", Tmdb = 16729 }, Title = "Movie_2", Year = 2016}},
            },
            1
          },
          new object[]
          {
            new List<MediaItem>
            {
              new DatabaseMovie(ExternalIdentifierAspect.SOURCE_IMDB, ExternalIdentifierAspect.SOURCE_TMDB, "tt12345", "67890", "Movie_1", 2012, 1).Movie,
              new DatabaseMovie(ExternalIdentifierAspect.SOURCE_IMDB, ExternalIdentifierAspect.SOURCE_TMDB, "", "67890", "Movie_2", 2016, 2).Movie,
              new DatabaseMovie(ExternalIdentifierAspect.SOURCE_IMDB, ExternalIdentifierAspect.SOURCE_TMDB, "", "0", "Movie_3", 2010, 3).Movie
            },
            new List<TraktMovieWatched>(),
            3
          }
        };
      }
    }

    [Theory]
    [MemberData(nameof(WatchedMoviesTestData))]
    public void AddWatchedMovieToTraktIfMediaLibraryAndCacheAvailable(List<MediaItem> databaseMovies, List<TraktMovieWatched> traktMovies, int expectedMoviesCount)
    {
      // Arrange
      ITraktServices traktServices = Substitute.For<ITraktServices>();
      traktServices.GetTraktCache().GetWatchedMoviesFromTrakt().Returns(traktMovies);
      IMediaPortalServices mediaPortalServices = GetMockMediaPortalServices(databaseMovies);
      TraktSetupManager traktSetup = new TraktSetupManager(mediaPortalServices, traktServices);

      // Act
      bool isSynced = traktSetup.SyncMovies();

      // Assert
      int actualMoviesCount = traktSetup.SyncWatchedMovies;
      Assert.True(isSynced);
      Assert.Equal(expectedMoviesCount, actualMoviesCount);
    }

    public static IEnumerable<object[]> CollectedMoviesTestData
    {
      get
      {
        return new List<object[]>
        {
          new object[]
          {
            new List<MediaItem>
            {
              new DatabaseMovie(ExternalIdentifierAspect.SOURCE_IMDB, ExternalIdentifierAspect.SOURCE_TMDB, "tt12345", "67890", "Movie_1", 2012, 0).Movie,
              new DatabaseMovie(ExternalIdentifierAspect.SOURCE_IMDB, ExternalIdentifierAspect.SOURCE_TMDB, "", "67890", "Movie_2", 2016, 0).Movie,
              new DatabaseMovie(ExternalIdentifierAspect.SOURCE_IMDB, ExternalIdentifierAspect.SOURCE_TMDB, "", "0", "Movie_3", 2010, 1).Movie
            },
            new List<TraktMovieCollected>(),
            3
          },
          new object[]
          {
            new List<MediaItem>
            {
              new DatabaseMovie(ExternalIdentifierAspect.SOURCE_IMDB, ExternalIdentifierAspect.SOURCE_TMDB, "tt12345", "67890", "Movie_1", 2012, 0).Movie,
              new DatabaseMovie(ExternalIdentifierAspect.SOURCE_IMDB, ExternalIdentifierAspect.SOURCE_TMDB, "", "16729", "Movie_2", 2016, 1).Movie,
              new DatabaseMovie(ExternalIdentifierAspect.SOURCE_IMDB, ExternalIdentifierAspect.SOURCE_TMDB, "", "0", "Movie_3", 2010, 2).Movie
            },
            new List<TraktMovieCollected>
            {
              new TraktMovieCollected {Movie = new TraktMovie {Ids = new TraktMovieId {Imdb = "tt12345", Tmdb = 67890 }, Title = "Movie_1", Year = 2012}, CollectedAt = "2015.01.01"},
            },
            2
          },
          new object[]
          {
            new List<MediaItem>
            {
              new DatabaseMovie(ExternalIdentifierAspect.SOURCE_IMDB, ExternalIdentifierAspect.SOURCE_TMDB, "tt12345", "67890", "Movie_1", 2012, 1).Movie,
              new DatabaseMovie(ExternalIdentifierAspect.SOURCE_IMDB, ExternalIdentifierAspect.SOURCE_TMDB, "", "16729", "Movie_2", 2008, 2).Movie,
              new DatabaseMovie(ExternalIdentifierAspect.SOURCE_IMDB, ExternalIdentifierAspect.SOURCE_TMDB, "", "0", "Movie_3", 2001, 3).Movie
            },
            new List<TraktMovieCollected>
            {
              new TraktMovieCollected {Movie = new TraktMovie {Ids = new TraktMovieId {Imdb = "tt12345", Tmdb = 67890 }, Title = "Movie_1", Year = 2012}, CollectedAt = "2015.01.01"},
              new TraktMovieCollected {Movie = new TraktMovie {Ids = new TraktMovieId {Imdb = "tt42690", Tmdb = 16729 }, Title = "Movie_2", Year = 2008}, CollectedAt = "2011.01.01"},
              new TraktMovieCollected {Movie = new TraktMovie {Ids = new TraktMovieId {Imdb = "tt00754", Tmdb = 34251 }, Title = "Movie_3", Year = 2001}, CollectedAt = "2009.01.01"}
            },
            0
          }
        };
      }
    }

    [Theory]
    [MemberData(nameof(CollectedMoviesTestData))]
    public void AddCollectedMovieToTraktIfMediaLibraryAndCacheAvailable(List<MediaItem> databaseMovies, List<TraktMovieCollected> traktMovies, int expectedMoviesCount)
    {
      // Arrange
      ITraktServices traktServices = Substitute.For<ITraktServices>();
      traktServices.GetTraktCache().GetCollectedMoviesFromTrakt().Returns(traktMovies);
      IMediaPortalServices mediaPortalServices = GetMockMediaPortalServices(databaseMovies);
      TraktSetupManager traktSetup = new TraktSetupManager(mediaPortalServices, traktServices);

      // Act
      bool isSynced = traktSetup.SyncMovies();

      // Assert
      int actualMoviesCount = traktSetup.SyncCollectedMovies;
      Assert.True(isSynced);
      Assert.Equal(expectedMoviesCount, actualMoviesCount);
    }

    public static IEnumerable<object[]> TraktUnwatchedMoviesTestData
    {
      get
      {
        return new List<object[]>
        {
          new object[]
          {
            new List<MediaItem>
            {
              new DatabaseMovie(ExternalIdentifierAspect.SOURCE_IMDB, ExternalIdentifierAspect.SOURCE_TMDB, "tt12345", "11290", "Movie_1", 2012, 1).Movie,
              new DatabaseMovie(ExternalIdentifierAspect.SOURCE_IMDB, ExternalIdentifierAspect.SOURCE_TMDB, "", "67890", "Movie_2", 2016, 1).Movie,
              new DatabaseMovie(ExternalIdentifierAspect.SOURCE_IMDB, ExternalIdentifierAspect.SOURCE_TMDB, "", "0", "Movie_3", 2010, 1).Movie
            },
            new List<TraktMovie>
            {
              new TraktMovie {Ids = new TraktMovieId {Imdb = "tt12345", Tmdb = 11290 }, Title = "Movie_1", Year = 2012},
              new TraktMovie {Ids = new TraktMovieId {Imdb = "tt11390", Tmdb = 67890 }, Title = "Movie_2", Year = 2016},
              new TraktMovie {Ids = new TraktMovieId {Imdb = "tt99821", Tmdb = 31139 }, Title = "Movie_3", Year = 2010}
            },
            3
          },
          new object[]
          {
            new List<MediaItem>
            {
              new DatabaseMovie(ExternalIdentifierAspect.SOURCE_IMDB, ExternalIdentifierAspect.SOURCE_TMDB, "tt12345", "11290", "Movie_1", 2012, 1).Movie,
              new DatabaseMovie(ExternalIdentifierAspect.SOURCE_IMDB, ExternalIdentifierAspect.SOURCE_TMDB, "", "67890", "Movie_2", 2016, 1).Movie,
              new DatabaseMovie(ExternalIdentifierAspect.SOURCE_IMDB, ExternalIdentifierAspect.SOURCE_TMDB, "", "0", "Movie_3", 2010, 1).Movie
            },
            new List<TraktMovie>
            {
              new TraktMovie {Ids = new TraktMovieId {Imdb = "tt12345", Tmdb = 11290 }, Title = "Movie_1", Year = 2012},
            },
            1
          },
          new object[]
          {
            new List<MediaItem>
            {
              new DatabaseMovie(ExternalIdentifierAspect.SOURCE_IMDB, ExternalIdentifierAspect.SOURCE_TMDB, "tt12128", "11290", "Movie_1", 2012, 2).Movie,
              new DatabaseMovie(ExternalIdentifierAspect.SOURCE_IMDB, ExternalIdentifierAspect.SOURCE_TMDB, "", "12390", "Movie_2", 2016, 2).Movie,
              new DatabaseMovie(ExternalIdentifierAspect.SOURCE_IMDB, ExternalIdentifierAspect.SOURCE_TMDB, "", "0", "Movie_4", 2011, 1).Movie
            },
            new List<TraktMovie>
            {
              new TraktMovie {Ids = new TraktMovieId {Imdb = "tt12345", Tmdb = 11290 }, Title = "Movie_1", Year = 2012},
              new TraktMovie {Ids = new TraktMovieId {Imdb = "tt67804", Tmdb = 67890 }, Title = "Movie_2", Year = 2016},
              new TraktMovie {Ids = new TraktMovieId {Imdb = "tt03412", Tmdb = 34251 }, Title = "Movie_3", Year = 2010}
            },
            0
          }
        };
      }
    }

    [Theory]
    [MemberData(nameof(TraktUnwatchedMoviesTestData))]
    public void MarkMovieAsUnwatchedIfMediaLibraryAndCacheAvailable(List<MediaItem> databaseMovies, List<TraktMovie> traktMovies, int expectedMoviesCount)
    {
      // Arrange
      ITraktServices traktServices = Substitute.For<ITraktServices>();
      traktServices.GetTraktCache().GetUnWatchedMoviesFromTrakt().Returns(traktMovies);
      IMediaPortalServices mediaPortalServices = GetMockMediaPortalServices(databaseMovies);
      TraktSetupManager traktSetup = new TraktSetupManager(mediaPortalServices, traktServices);

      // Act
      bool isSynced = traktSetup.SyncMovies();

      // Assert
      int actualMoviesCount = traktSetup.MarkUnWatchedMovies;
      Assert.True(isSynced);
      Assert.Equal(expectedMoviesCount, actualMoviesCount);
    }

    public static IEnumerable<object[]> TraktWatchedMoviesTestData
    {
      get
      {
        return new List<object[]>
        {
          new object[]
          {
            new List<MediaItem>
            {
              new DatabaseMovie(ExternalIdentifierAspect.SOURCE_IMDB, ExternalIdentifierAspect.SOURCE_TMDB, "tt1450", "67890", "Movie_1", 2012, 1).Movie,
              new DatabaseMovie(ExternalIdentifierAspect.SOURCE_IMDB, ExternalIdentifierAspect.SOURCE_TMDB, "", "50123", "Movie_2", 2016, 1).Movie,
              new DatabaseMovie(ExternalIdentifierAspect.SOURCE_IMDB, ExternalIdentifierAspect.SOURCE_TMDB, "", "0", "Movie_4", 2014, 1).Movie
            },
            new List<TraktMovieWatched>
            {
              new TraktMovieWatched {Movie = new TraktMovie {Ids = new TraktMovieId {Imdb = "tt12345", Tmdb = 67890 }, Title = "Movie_1", Year = 2012}},
              new TraktMovieWatched {Movie = new TraktMovie {Ids = new TraktMovieId {Imdb = "tt67804", Tmdb = 67890 }, Title = "Movie_2", Year = 2016}},
              new TraktMovieWatched {Movie = new TraktMovie {Ids = new TraktMovieId {Imdb = "tt03412", Tmdb = 34251 }, Title = "Movie_3", Year = 2010}}
            },
            0
          },
          new object[]
          {
            new List<MediaItem>
            {
              new DatabaseMovie(ExternalIdentifierAspect.SOURCE_IMDB, ExternalIdentifierAspect.SOURCE_TMDB, "tt12345", "67890", "Movie_1", 2012, 0).Movie,
              new DatabaseMovie(ExternalIdentifierAspect.SOURCE_IMDB, ExternalIdentifierAspect.SOURCE_TMDB, "", "67890", "Movie_2", 2016, 0).Movie,
              new DatabaseMovie(ExternalIdentifierAspect.SOURCE_IMDB, ExternalIdentifierAspect.SOURCE_TMDB, "", "0", "Movie_3", 2010, 0).Movie
            },
            new List<TraktMovieWatched>
            {
              new TraktMovieWatched {Movie = new TraktMovie {Ids = new TraktMovieId {Imdb = "tt12345", Tmdb = 67890 }, Title = "Movie_1", Year = 2012}},
              new TraktMovieWatched {Movie = new TraktMovie {Ids = new TraktMovieId {Imdb = "tt67804", Tmdb = 67890 }, Title = "Movie_2", Year = 2016}},
              new TraktMovieWatched {Movie = new TraktMovie {Ids = new TraktMovieId {Imdb = "tt03412", Tmdb = 34251 }, Title = "Movie_3", Year = 2010}}
            },
            3
          },
          new object[]
          {
            new List<MediaItem>
            {
              new DatabaseMovie(ExternalIdentifierAspect.SOURCE_IMDB, ExternalIdentifierAspect.SOURCE_TMDB, "tt12345", "67890", "Movie_1", 2012, 0).Movie,
              new DatabaseMovie(ExternalIdentifierAspect.SOURCE_IMDB, ExternalIdentifierAspect.SOURCE_TMDB, "", "67890", "Movie_2", 2016, 0).Movie,
              new DatabaseMovie(ExternalIdentifierAspect.SOURCE_IMDB, ExternalIdentifierAspect.SOURCE_TMDB, "", "0", "Movie_3", 2010, 0).Movie
            },
            new List<TraktMovieWatched>
            {
              new TraktMovieWatched {Movie = new TraktMovie {Ids = new TraktMovieId {Imdb = "tt03412", Tmdb = 34251 }, Title = "Movie_3", Year = 2010}}
            },
            1
          }
        };
      }
    }

    [Theory]
    [MemberData(nameof(TraktWatchedMoviesTestData))]
    public void MarkMovieAsWatchedIfMediaLibraryAndCacheAvailable(List<MediaItem> databaseMovies, List<TraktMovieWatched> traktMovies, int expectedMoviesCount)
    {
      // Arrange
      ITraktServices traktServices = Substitute.For<ITraktServices>();
      traktServices.GetTraktCache().GetWatchedMoviesFromTrakt().Returns(traktMovies);
      IMediaPortalServices mediaPortalServices = GetMockMediaPortalServices(databaseMovies);
      TraktSetupManager traktSetup = new TraktSetupManager(mediaPortalServices, traktServices);

      // Act
      bool isSynced = traktSetup.SyncMovies();

      // Assert
      int actualMoviesCount = traktSetup.MarkWatchedMovies;
      Assert.True(isSynced);
      Assert.Equal(expectedMoviesCount, actualMoviesCount);
    }

    public static IEnumerable<object[]> CollectedEpisodesTestData
    {
      get
      {
        return new List<object[]>
        {
          new object[]
          {
            new List<MediaItem>
            {
              new DatabaseEpisode(ExternalIdentifierAspect.SOURCE_TVDB, "289590", 2, new List<int> {6}, 1).Episode,
              new DatabaseEpisode(ExternalIdentifierAspect.SOURCE_TVDB, "318493", 1, new List<int> {2}, 0).Episode,
              new DatabaseEpisode(ExternalIdentifierAspect.SOURCE_TVDB, "998201", 4, new List<int> {1}, 1).Episode
            },
            new List<Episode>
            {
              new Episode {ShowTvdbId = 289590, Season = 2, Number = 6},
              new Episode {ShowTvdbId = 318493, Season = 1, Number = 2},
              new Episode {ShowTvdbId = 998201, Season = 4, Number = 1}
            },
            3 // TODO: should be 0?! syncCollectedShows.Shows.Sum(sh => sh.Seasons.Sum(se => se.Episodes.Count()));
          },
          new object[]
          {
            new List<MediaItem>
            {
              new DatabaseEpisode(ExternalIdentifierAspect.SOURCE_TVDB, "289590", 2, new List<int>{6}, 1).Episode,
              new DatabaseEpisode(ExternalIdentifierAspect.SOURCE_TVDB, "318493", 1, new List<int>{2}, 0).Episode,
              new DatabaseEpisode(ExternalIdentifierAspect.SOURCE_TVDB, "998201", 4, new List<int>{1}, 1).Episode
            },
            new List<Episode>(),
            3
          }
        };
      }
    }

    [Theory]
    [MemberData(nameof(CollectedEpisodesTestData))]
    public void AddCollectedEpisodeToTraktIfMediaLibraryAndCacheAvailable(IList<MediaItem> databaseEpisodes, IList<Episode> traktEpisodes, int expectedEpisodesCount)
    {
      // Arrange
      ITraktServices traktServices = Substitute.For<ITraktServices>();
      traktServices.GetTraktCache().GetUnWatchedEpisodesFromTrakt().Returns(traktEpisodes);
      IMediaPortalServices mediaPortalServices = GetMockMediaPortalServices(databaseEpisodes);
      TraktSetupManager traktSetup = new TraktSetupManager(mediaPortalServices, traktServices);

      // Act
      bool isSynced = traktSetup.SyncSeries();

      // Assert
      int actualEpisodesCount = traktSetup.SyncCollectedEpisodes;
      Assert.True(isSynced);
      Assert.Equal(expectedEpisodesCount, actualEpisodesCount);
    }

    public static IEnumerable<object[]> WatchedEpisodesTestData
    {
      get
      {
        return new List<object[]>
        {
          new object[]
          {
            new List<MediaItem>
            {
              new DatabaseEpisode(ExternalIdentifierAspect.SOURCE_TVDB, "289590", 2, new List<int>{6}, 1).Episode,
              new DatabaseEpisode(ExternalIdentifierAspect.SOURCE_TVDB, "318493", 1, new List<int>{2}, 3).Episode,
              new DatabaseEpisode(ExternalIdentifierAspect.SOURCE_TVDB, "998201", 4, new List<int>{1}, 1).Episode
            },
            new List<EpisodeWatched>
            {
              new EpisodeWatched {ShowTvdbId = 289590, Season = 2, Number = 6, Plays = 1},
              new EpisodeWatched {ShowTvdbId = 318493, Season = 1, Number = 2, Plays = 3},
              new EpisodeWatched {ShowTvdbId = 998201, Season = 4, Number = 1, Plays = 1}
            },
            0
          },
          new object[]
          {
            new List<MediaItem>
            {
              new DatabaseEpisode(ExternalIdentifierAspect.SOURCE_TVDB, "289590", 2, new List<int>{6}, 1).Episode,
              new DatabaseEpisode(ExternalIdentifierAspect.SOURCE_TVDB, "318493", 1, new List<int>{2}, 3).Episode,
              new DatabaseEpisode(ExternalIdentifierAspect.SOURCE_TVDB, "998201", 4, new List<int>{1}, 1).Episode
            },
            new List<EpisodeWatched>(),
            3
          },
          new object[]
          {
            new List<MediaItem>
            {
              new DatabaseEpisode(ExternalIdentifierAspect.SOURCE_TVDB, "289590", 2, new List<int>{6}, 1).Episode,
              new DatabaseEpisode(ExternalIdentifierAspect.SOURCE_TVDB, "318493", 1, new List<int>{2}, 3).Episode,
              new DatabaseEpisode(ExternalIdentifierAspect.SOURCE_TVDB, "998201", 4, new List<int>{1}, 1).Episode
            },
            new List<EpisodeWatched>
            {
              new EpisodeWatched {ShowTvdbId = 998201, Season = 4, Number = 1, Plays = 1}
            },
            2
          }
        };
      }
    }

    [Theory]
    [MemberData(nameof(WatchedEpisodesTestData))]
    public void AddWatchedEpisodeToTraktIfMediaLibraryAndCacheAvailable(IList<MediaItem> databaseEpisodes, IList<EpisodeWatched> traktEpisodes, int expectedEpisodesCount)
    {
      // Arrange
      ITraktServices traktServices = Substitute.For<ITraktServices>();
      traktServices.GetTraktCache().GetWatchedEpisodesFromTrakt().Returns(traktEpisodes);
      IMediaPortalServices mediaPortalServices = GetMockMediaPortalServices(databaseEpisodes);
      TraktSetupManager traktSetup = new TraktSetupManager(mediaPortalServices, traktServices);

      // Act
      bool isSynced = traktSetup.SyncSeries();

      // Assert
      int actualEpisodesCount = traktSetup.SyncWatchedEpisodes;
      Assert.True(isSynced);
      Assert.Equal(expectedEpisodesCount, actualEpisodesCount);
    }

    public static IEnumerable<object[]> TraktUnWatchedEpisodesTestData
    {
      get
      {
        return new List<object[]>
        {
          new object[]
          {
            new List<MediaItem>
            {
              new DatabaseEpisode(ExternalIdentifierAspect.SOURCE_TVDB, "318493", 1, new List<int>{2}, 3).Episode,
              new DatabaseEpisode(ExternalIdentifierAspect.SOURCE_TVDB, "998201", 4, new List<int>{1}, 1).Episode
            },
            new List<Episode>
            {
              new Episode {ShowTvdbId = 234593, Season = 4, Number = 6},
              new Episode {ShowTvdbId = 092101, Season = 3, Number = 8}
            },
            0
          },
          new object[]
          {
            new List<MediaItem>
            {
              new DatabaseEpisode(ExternalIdentifierAspect.SOURCE_TVDB, "289590", 2, new List<int>{6}, 1).Episode,
              new DatabaseEpisode(ExternalIdentifierAspect.SOURCE_TVDB, "318493", 1, new List<int>{2}, 3).Episode,
            },
            new List<Episode>
            {
              new Episode {ShowTvdbId = 318493, Season = 1, Number = 2}
            },
            1
          },
          new object[]
          {
            new List<MediaItem>
            {
              new DatabaseEpisode(ExternalIdentifierAspect.SOURCE_TVDB, "289590", 2, new List<int>{6}, 1).Episode,
              new DatabaseEpisode(ExternalIdentifierAspect.SOURCE_TVDB, "318493", 1, new List<int>{2}, 3).Episode,
              new DatabaseEpisode(ExternalIdentifierAspect.SOURCE_TVDB, "998201", 4, new List<int>{1}, 1).Episode
            },
            new List<Episode>
            {
              new Episode {ShowTvdbId = 289590, Season = 2, Number = 6},
              new Episode {ShowTvdbId = 318493, Season = 1, Number = 2},
              new Episode {ShowTvdbId = 998201, Season = 4, Number = 1}
            },
            3
          }
        };
      }
    }

    [Theory]
    [MemberData(nameof(TraktUnWatchedEpisodesTestData))]
    public void MarkEpisodeAsUnwatchedIfMediaLibraryAndCacheAvailable(List<MediaItem> databaseEpisodes, List<Episode> traktEpisodes, int expectedEpisodessCount)
    {
      // Arrange
      ITraktServices traktServices = Substitute.For<ITraktServices>();
      traktServices.GetTraktCache().GetUnWatchedEpisodesFromTrakt().Returns(traktEpisodes);
      IMediaPortalServices mediaPortalServices = GetMockMediaPortalServices(databaseEpisodes);
      TraktSetupManager traktSetup = new TraktSetupManager(mediaPortalServices, traktServices);

      // Act
      bool isSynced = traktSetup.SyncSeries();

      // Assert
      int actualEpisodesCount = traktSetup.MarkUnWatchedEpisodes;
      Assert.True(isSynced);
      Assert.Equal(expectedEpisodessCount, actualEpisodesCount);
    }

    public static IEnumerable<object[]> TraktWatchedEpisodesTestData
    {
      get
      {
        return new List<object[]>
        {
          new object[]
          {
            new List<MediaItem>
            {
              new DatabaseEpisode(ExternalIdentifierAspect.SOURCE_TVDB, "289590", 2, new List<int>{6}, 1).Episode,
              new DatabaseEpisode(ExternalIdentifierAspect.SOURCE_TVDB, "318493", 1, new List<int>{2}, 2).Episode,
              new DatabaseEpisode(ExternalIdentifierAspect.SOURCE_TVDB, "998201", 4, new List<int>{1}, 2).Episode
            },
            new List<EpisodeWatched>
            {
              new EpisodeWatched {ShowTvdbId = 289590, Season = 2, Number = 6, Plays = 1},
              new EpisodeWatched {ShowTvdbId = 318493, Season = 1, Number = 2, Plays = 3},
              new EpisodeWatched {ShowTvdbId = 998201, Season = 4, Number = 1, Plays = 1}
            },
            0
          },
          new object[]
          {
            new List<MediaItem>
            {
              new DatabaseEpisode(ExternalIdentifierAspect.SOURCE_TVDB, "289590", 2, new List<int>{6}, 1).Episode,
              new DatabaseEpisode(ExternalIdentifierAspect.SOURCE_TVDB, "318493", 1, new List<int>{2}, 0).Episode,
              new DatabaseEpisode(ExternalIdentifierAspect.SOURCE_TVDB, "998201", 4, new List<int>{1}, 0).Episode
            },
            new List<EpisodeWatched>
            {
              new EpisodeWatched {ShowTvdbId = 998201, Season = 4, Number = 1, Plays = 1}
            },
            1
          },
          new object[]
          {
            new List<MediaItem>
            {
              new DatabaseEpisode(ExternalIdentifierAspect.SOURCE_TVDB, "289123", 4, new List<int>{8}, 0).Episode,
              new DatabaseEpisode(ExternalIdentifierAspect.SOURCE_TVDB, "991493", 1, new List<int>{1}, 0).Episode,
              new DatabaseEpisode(ExternalIdentifierAspect.SOURCE_TVDB, "055201", 2, new List<int>{0}, 0).Episode
            },
            new List<EpisodeWatched>
            {
              new EpisodeWatched {ShowTvdbId = 289123, Season = 4, Number = 8, Plays = 1},
              new EpisodeWatched {ShowTvdbId = 991493, Season = 1, Number = 1, Plays = 3},
              new EpisodeWatched {ShowTvdbId = 055201, Season = 2, Number = 0, Plays = 1}
            },
            3
          }
        };
      }
    }

    [Theory]
    [MemberData(nameof(TraktWatchedEpisodesTestData))]
    public void MarkEpisodeAsWatchedIfMediaLibraryAndCacheAvailable(List<MediaItem> databaseEpisodes, List<EpisodeWatched> traktEpisodes, int expectedEpisodesCount)
    {
      // Arrange
      ITraktServices traktServices = Substitute.For<ITraktServices>();
      traktServices.GetTraktCache().GetWatchedEpisodesFromTrakt().Returns(traktEpisodes);
      IMediaPortalServices mediaPortalServices = GetMockMediaPortalServices(databaseEpisodes);
      TraktSetupManager traktSetup = new TraktSetupManager(mediaPortalServices, traktServices);

      // Act
      bool isSynced = traktSetup.SyncSeries();

      // Assert
      int actualEpisodesCount = traktSetup.MarkWatchedEpisodes;
      Assert.True(isSynced);
      Assert.Equal(expectedEpisodesCount, actualEpisodesCount);
    }

    // [Fact]
    public void Test()
    {
      // Arrange
      IMediaPortalServices mediaPortalServices = Substitute.For<IMediaPortalServices>();
      ISettingsManager settingsManager = Substitute.For<ISettingsManager>();
      TraktPluginSettings traktPluginSettings = new TraktPluginSettings {SyncBatchSize = 100};
      settingsManager.Load<TraktPluginSettings>().Returns(traktPluginSettings);
      mediaPortalServices.GetSettingsManager().Returns(settingsManager);
      IContentDirectory contentDirectory = Substitute.For<IContentDirectory>();

      // watched movies in DB
      IList<MediaItem> items = new List<MediaItem>();
      IDictionary<Guid, IList<MediaItemAspect>> movie1Aspects = new Dictionary<Guid, IList<MediaItemAspect>>();
      IDictionary<Guid, IList<MediaItemAspect>> movie2Aspects = new Dictionary<Guid, IList<MediaItemAspect>>();
      MediaItemAspect.AddOrUpdateExternalIdentifier(movie1Aspects, ExternalIdentifierAspect.SOURCE_TMDB, ExternalIdentifierAspect.TYPE_MOVIE, "123");
      MediaItemAspect.AddOrUpdateExternalIdentifier(movie1Aspects, ExternalIdentifierAspect.SOURCE_IMDB, ExternalIdentifierAspect.TYPE_MOVIE, "xtt01234");
      MediaItemAspect.AddOrUpdateExternalIdentifier(movie2Aspects, ExternalIdentifierAspect.SOURCE_TMDB, ExternalIdentifierAspect.TYPE_MOVIE, "456");
      MediaItemAspect.AddOrUpdateExternalIdentifier(movie2Aspects, ExternalIdentifierAspect.SOURCE_IMDB, ExternalIdentifierAspect.TYPE_MOVIE, "tt98765");
      SingleMediaItemAspect smia1 = new SingleMediaItemAspect(MediaAspect.Metadata);
      SingleMediaItemAspect smia2 = new SingleMediaItemAspect(MediaAspect.Metadata);
      smia1.SetAttribute(MediaAspect.ATTR_PLAYCOUNT, 1);
      smia2.SetAttribute(MediaAspect.ATTR_PLAYCOUNT, 1);
      MediaItemAspect.SetAspect(movie2Aspects, smia2);
      MediaItemAspect.SetAspect(movie1Aspects, smia1);
      MediaItem movie1 = new MediaItem(new Guid(), movie1Aspects);
      MediaItem movie2 = new MediaItem(new Guid(), movie2Aspects);
      items.Add(movie1);
      items.Add(movie2);

      contentDirectory.Search(Arg.Any<MediaItemQuery>(), true, null, false).Returns(items);
      mediaPortalServices.GetServerConnectionManager().ContentDirectory.Returns(contentDirectory);
      ITraktServices traktServices = Substitute.For<ITraktServices>();
      
      // watched movies at trakt
      IEnumerable<TraktMovieWatched> movies = new List<TraktMovieWatched>
      {
        new TraktMovieWatched
        {
          Movie = new TraktMovie
          { 
          Title = "Movie_1",
          Year = 2017,
          Ids = new TraktMovieId { Tmdb = 123, Imdb = "tt01234" }
          }
        },
        new TraktMovieWatched
        {
          Movie =  new TraktMovie
          {
            Title = "Movie_2",
            Year = 2018,
            Ids = new TraktMovieId { Tmdb = 456, Imdb = "tt98765" }
          }
        }
      };
      traktServices.GetTraktCache().GetWatchedMoviesFromTrakt().Returns(movies);
      TraktSetupManager traktSetup = new TraktSetupManager(mediaPortalServices, traktServices);

      // Act
      bool isSynced = traktSetup.SyncMovies();

      // Assert
      Assert.True(isSynced);
      Assert.Equal(0, traktSetup.SyncWatchedMovies);
    }

    [Fact]
    public void StartScrobbleWhenPlayerStarted()
    { 
      // Arrange
      IMediaPortalServices mediaPortalServices = Substitute.For<IMediaPortalServices>();
      ITraktServices traktServices = Substitute.For<ITraktServices>();
      IAsynchronousMessageQueue messageQueue = Substitute.For<IAsynchronousMessageQueue>();
      messageQueue.When(x => x.Start()).Do(x => { /*nothing*/});
      mediaPortalServices.GetMessageQueue(Arg.Any<object>(), Arg.Any<string[]>()).Returns(messageQueue);
      IPlayerSlotController psc = Substitute.For<IPlayerSlotController>();
      SystemMessage startedState = new SystemMessage(PlayerManagerMessaging.MessageType.PlayerStarted)
      {
        ChannelName = "PlayerManager",
        MessageData = {["PlayerSlotController"] = psc}
      };

      TraktHandlerManager trakthandler = new TraktHandlerManager(mediaPortalServices, traktServices);
      
      // Act
      messageQueue.MessageReceived += Raise.Event<MessageReceivedHandler>(new AsynchronousMessageQueue(new object(), new[] { "PlayerManager" }), startedState);
      
      // Assert
      Assert.True(trakthandler.StartedScrobble);
    }

    private IMediaPortalServices GetMockMediaPortalServices(IList<MediaItem> databaseMediaItems)
    {
      IMediaPortalServices mediaPortalServices = Substitute.For<IMediaPortalServices>();
      ISettingsManager settingsManager = Substitute.For<ISettingsManager>();
      TraktPluginSettings traktPluginSettings = new TraktPluginSettings { SyncBatchSize = 100 };
      settingsManager.Load<TraktPluginSettings>().Returns(traktPluginSettings);
      mediaPortalServices.GetSettingsManager().Returns(settingsManager);
      IContentDirectory contentDirectory = Substitute.For<IContentDirectory>();
      contentDirectory.Search(Arg.Any<MediaItemQuery>(), true, null, false).Returns(databaseMediaItems);
      mediaPortalServices.GetServerConnectionManager().ContentDirectory.Returns(contentDirectory);

      return mediaPortalServices;
    }
  }

  class DatabaseMovie
  {
    public MediaItem Movie { get; }

    public DatabaseMovie(string sourceImdb, string sourceTmbd, string imdbId, string tmdbId, string title, int year, int playCount )
    {
      IDictionary<Guid, IList<MediaItemAspect>> movieAspects = new Dictionary<Guid, IList<MediaItemAspect>>();
      MultipleMediaItemAspect resourceAspect = new MultipleMediaItemAspect(ProviderResourceAspect.Metadata);
      resourceAspect.SetAttribute(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH, "c:\\" + title + ".mkv");
      MediaItemAspect.AddOrUpdateAspect(movieAspects, resourceAspect);
      MediaItemAspect.AddOrUpdateExternalIdentifier(movieAspects, sourceImdb, ExternalIdentifierAspect.TYPE_MOVIE, imdbId);
      MediaItemAspect.AddOrUpdateExternalIdentifier(movieAspects, sourceTmbd, ExternalIdentifierAspect.TYPE_MOVIE, tmdbId);
      MediaItemAspect.SetAttribute(movieAspects, MovieAspect.ATTR_MOVIE_NAME, title);
      SingleMediaItemAspect smia = new SingleMediaItemAspect(MediaAspect.Metadata);
      smia.SetAttribute(MediaAspect.ATTR_PLAYCOUNT, playCount);
      smia.SetAttribute(MediaAspect.ATTR_RECORDINGTIME, new DateTime(year,1,1));
      MediaItemAspect.SetAspect(movieAspects, smia);

      Movie = new MediaItem(Guid.NewGuid(), movieAspects);
    }
  }

  class DatabaseEpisode
  {
    public MediaItem Episode { get; }

    public DatabaseEpisode(string sourceTvdb, string tvDbId, int seasonIndex, List<int> episodeIndex, int playCount)
    {
      IDictionary<Guid, IList<MediaItemAspect>> episodeAspects = new Dictionary<Guid, IList<MediaItemAspect>>();
      MultipleMediaItemAspect resourceAspect = new MultipleMediaItemAspect(ProviderResourceAspect.Metadata);
      resourceAspect.SetAttribute(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH, "c:\\" + tvDbId + ".mkv");
      MediaItemAspect.AddOrUpdateAspect(episodeAspects, resourceAspect);
      MediaItemAspect.AddOrUpdateExternalIdentifier(episodeAspects, sourceTvdb, ExternalIdentifierAspect.TYPE_SERIES, tvDbId);
      MediaItemAspect.SetAttribute(episodeAspects, EpisodeAspect.ATTR_SEASON, seasonIndex);
      MediaItemAspect.SetCollectionAttribute(episodeAspects, EpisodeAspect.ATTR_EPISODE, episodeIndex);
      SingleMediaItemAspect smia = new SingleMediaItemAspect(MediaAspect.Metadata);
      smia.SetAttribute(MediaAspect.ATTR_PLAYCOUNT, playCount);
      MediaItemAspect.SetAspect(episodeAspects, smia);

      Episode = new MediaItem(Guid.NewGuid(), episodeAspects);
    }
  }
}