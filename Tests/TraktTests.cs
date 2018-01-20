using System;
using System.Collections.Generic;
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

    public static IEnumerable<object[]> UnsyncedWatchedMoviesTestData
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
    [MemberData(nameof(UnsyncedWatchedMoviesTestData))]
    public void AddWatchedMovieToTraktIfMovieDbUnsyced(List<MediaItem> databaseMovies, List<TraktMovieWatched> traktMovies, int expectedMoviesCount)
    {
      // Arrange
      TraktSetupManager traktSetup = GetMockTraktSetup(databaseMovies, traktMovies);

      // Act
      bool isSynced = traktSetup.SyncMovies();

      // Assert
      int actualMoviesCount = traktSetup.SyncWatchedMovies;
      Assert.True(isSynced);
      Assert.Equal(expectedMoviesCount, actualMoviesCount);
    }

    public static IEnumerable<object[]> SyncedWatchedMoviesTestData
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
          }
        };
      }
    }

    [Theory]
    [MemberData(nameof(SyncedWatchedMoviesTestData))]
    public void DoNotAddWatchedMovieToTraktIfMovieDbSynced(List<MediaItem> databaseMovies, List<TraktMovieWatched> traktMovies, int expectedMoviesCount)
    {
      // Arrange
      TraktSetupManager traktSetup = GetMockTraktSetup(databaseMovies, traktMovies);

      // Act
      bool isSynced = traktSetup.SyncMovies();

      // Assert
      int actualMoviesCount = traktSetup.SyncWatchedMovies;
      Assert.True(isSynced);
      Assert.Equal(expectedMoviesCount, actualMoviesCount);
    }

    public static IEnumerable<object[]> UnsyncedCollectedMoviesTestData
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
          }
        };
      }
    }

    [Theory]
    [MemberData(nameof(UnsyncedCollectedMoviesTestData))]
    public void AddCollectedMovieToTraktIfMovieDbUnsynced(List<MediaItem> databaseMovies, List<TraktMovieCollected> traktMovies, int expectedMoviesCount)
    {
      // Arrange
      TraktSetupManager traktSetup = GetMockTraktSetup(databaseMovies, traktMovies);

      // Act
      bool isSynced = traktSetup.SyncMovies();

      // Assert
      int actualMoviesCount = traktSetup.SyncCollectedMovies;
      Assert.True(isSynced);
      Assert.Equal(expectedMoviesCount, actualMoviesCount);
    }

    public static IEnumerable<object[]> SyncedCollectedMoviesTestData
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
              new DatabaseMovie(ExternalIdentifierAspect.SOURCE_IMDB, ExternalIdentifierAspect.SOURCE_TMDB, "", "16729", "Movie_2", 2008, 2).Movie,
              new DatabaseMovie(ExternalIdentifierAspect.SOURCE_IMDB, ExternalIdentifierAspect.SOURCE_TMDB, "", "0", "Movie_3", 2001, 3).Movie
            },
            new List<TraktMovieCollected>
            {
              new TraktMovieCollected {Movie = new TraktMovie {Ids = new TraktMovieId {Imdb = "tt12345", Tmdb = 67890 }, Title = "Movie_1", Year = 2012}, CollectedAt = "2015.01.01"},
              new TraktMovieCollected {Movie = new TraktMovie {Ids = new TraktMovieId {Imdb = "tt12345", Tmdb = 16729 }, Title = "Movie_2", Year = 2008}, CollectedAt = "2011.01.01"},
              new TraktMovieCollected {Movie = new TraktMovie {Ids = new TraktMovieId {Imdb = "tt12345", Tmdb = 34251 }, Title = "Movie_3", Year = 2001}, CollectedAt = "2009.01.01"}
            },
            0
          }
        };
      }
    }

    [Theory]
    [MemberData(nameof(SyncedCollectedMoviesTestData))]
    public void DoNotAddCollectedMovieToTraktIfMovieDbSynced(List<MediaItem> databaseMovies, List<TraktMovieCollected> traktMovies, int expectedMoviesCount)
    {
      // Arrange
      TraktSetupManager traktSetup = GetMockTraktSetup(databaseMovies, traktMovies);

      // Act
      bool isSynced = traktSetup.SyncMovies();

      // Assert
      int actualMoviesCount = traktSetup.SyncCollectedMovies;
      Assert.True(isSynced);
      Assert.Equal(expectedMoviesCount, actualMoviesCount);
    }

    private TraktSetupManager GetMockTraktSetup(List<MediaItem> databaseMovies, List<TraktMovieWatched> traktMovies)
    {
      IMediaPortalServices mediaPortalServices = Substitute.For<IMediaPortalServices>();
      ISettingsManager settingsManager = Substitute.For<ISettingsManager>();
      TraktPluginSettings traktPluginSettings = new TraktPluginSettings { SyncBatchSize = 100 };
      settingsManager.Load<TraktPluginSettings>().Returns(traktPluginSettings);
      mediaPortalServices.GetSettingsManager().Returns(settingsManager);
      IContentDirectory contentDirectory = Substitute.For<IContentDirectory>();
      contentDirectory.Search(Arg.Any<MediaItemQuery>(), true, null, false).Returns(databaseMovies);
      mediaPortalServices.GetServerConnectionManager().ContentDirectory.Returns(contentDirectory);
      ITraktServices traktServices = Substitute.For<ITraktServices>();
      traktServices.GetTraktCache().GetWatchedMoviesFromTrakt().Returns(traktMovies);

      return new TraktSetupManager(mediaPortalServices, traktServices);
    }

    private TraktSetupManager GetMockTraktSetup(List<MediaItem> databaseMovies, List<TraktMovieCollected> traktMovies)
    {
      IMediaPortalServices mediaPortalServices = Substitute.For<IMediaPortalServices>();
      ISettingsManager settingsManager = Substitute.For<ISettingsManager>();
      TraktPluginSettings traktPluginSettings = new TraktPluginSettings { SyncBatchSize = 100 };
      settingsManager.Load<TraktPluginSettings>().Returns(traktPluginSettings);
      mediaPortalServices.GetSettingsManager().Returns(settingsManager);
      IContentDirectory contentDirectory = Substitute.For<IContentDirectory>();
      contentDirectory.Search(Arg.Any<MediaItemQuery>(), true, null, false).Returns(databaseMovies);
      mediaPortalServices.GetServerConnectionManager().ContentDirectory.Returns(contentDirectory);
      ITraktServices traktServices = Substitute.For<ITraktServices>();
      traktServices.GetTraktCache().GetCollectedMoviesFromTrakt().Returns(traktMovies);

      return new TraktSetupManager(mediaPortalServices, traktServices);
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
  }

  class DatabaseMovie
  {
    public MediaItem Movie { get; }

    public DatabaseMovie(string sourceImdb, string sourceTmbd, string imdbId, string tmdbId, string title, int year, int playCount )
    {
      IDictionary<Guid, IList<MediaItemAspect>> movieAspects = new Dictionary<Guid, IList<MediaItemAspect>>();
      MediaItemAspect.AddOrUpdateExternalIdentifier(movieAspects, sourceImdb, ExternalIdentifierAspect.TYPE_MOVIE, imdbId);
      MediaItemAspect.AddOrUpdateExternalIdentifier(movieAspects, sourceTmbd, ExternalIdentifierAspect.TYPE_MOVIE, tmdbId);
      MediaItemAspect.SetAttribute(movieAspects, MovieAspect.ATTR_MOVIE_NAME, title);
      SingleMediaItemAspect smia = new SingleMediaItemAspect(MediaAspect.Metadata);
      smia.SetAttribute(MediaAspect.ATTR_PLAYCOUNT, playCount);
      smia.SetAttribute(MediaAspect.ATTR_RECORDINGTIME, new DateTime(year,1,1));
      MediaItemAspect.SetAspect(movieAspects, smia);

      Movie = new MediaItem(new Guid(), movieAspects);
    }
  }
}