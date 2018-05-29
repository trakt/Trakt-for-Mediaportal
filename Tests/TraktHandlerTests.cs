using System;
using System.Collections.Generic;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Common.Messaging;
using MediaPortal.Common.Settings;
using MediaPortal.Common.SystemCommunication;
using MediaPortal.UI.Presentation.Players;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Tests.TestData.Setup;
using TraktApiSharp.Authentication;
using TraktApiSharp.Exceptions;
using TraktApiSharp.Objects.Get.Movies;
using TraktApiSharp.Objects.Get.Shows;
using TraktApiSharp.Objects.Get.Shows.Episodes;
using TraktApiSharp.Objects.Post.Scrobbles.Responses;
using TraktPluginMP2.Handlers;
using TraktPluginMP2.Services;
using TraktPluginMP2.Settings;
using Xunit;

namespace Tests
{
  public class TraktHandlerTests
  {
    [Fact]
    public void SuccessfulStartedScrobbleForSeriesWhenPlayerStarted()
    {
      // Arrange
      IMediaPortalServices mediaPortalServices = Substitute.For<IMediaPortalServices>();

      // enabled trakt
      TraktPluginSettings settings = new TraktPluginSettings
      {
        EnableScrobble = true,
        RefreshToken = "7e7605b9103c1c1f7afcf18dabc583499155ea6318a9d7e49f06568866bd4adb"
      }; 

      ITraktSettingsChangeWatcher settingsChangeWatcher = Substitute.For<ITraktSettingsChangeWatcher>();
      settingsChangeWatcher.TraktSettings.Returns(settings); 
      mediaPortalServices.GetTraktSettingsWatcher().Returns(settingsChangeWatcher);

      ISettingsManager settingsManager = Substitute.For<ISettingsManager>();
      settingsManager.Load<TraktPluginSettings>().Returns(settings);

      mediaPortalServices.GetSettingsManager().Returns(settingsManager);

      // online response (trakt client)
      ITraktClient traktClient = Substitute.For<ITraktClient>();

         traktClient.RefreshAuthorization(Arg.Any<string>()).Returns(new TraktAuthorization
         {
           RefreshToken = "20f668873a00806dfc22136042f2eb81a44d98ab9b739acf68f2b46b784dcd0c"
         });
         traktClient.StartScrobbleEpisode(Arg.Any<TraktEpisode>(), Arg.Any<TraktShow>(), Arg.Any<float>()).Returns(
           new TraktEpisodeScrobblePostResponse
           {
             Episode = new TraktEpisode
             {
               Ids = new TraktEpisodeIds { Imdb = "tt12345", Tvdb = 289590 }, Number = 2, Title = "Title_1", SeasonNumber = 2
             }
           }); 


      // content director + player context (local data)
         IList<MediaItem> mediaItems = new List<MediaItem>();
         MockedDatabaseEpisode databaseEpisode = new MockedDatabaseEpisode("289590", 2, new List<int>(2), 1);
         mediaItems.Add(databaseEpisode.Episode);
         IContentDirectory contentDirectory = Substitute.For<IContentDirectory>();
         contentDirectory.SearchAsync(Arg.Any<MediaItemQuery>(), false, null, true).Returns(mediaItems); 
         
      mediaPortalServices.GetServerConnectionManager().ContentDirectory.Returns(contentDirectory);
      
     IPlayerContext playerContext = Substitute.For<IPlayerContext>();
         playerContext.CurrentMediaItem.Returns(databaseEpisode.Episode);
         IPlayer player = Substitute.For<IPlayer, IMediaPlaybackControl>();
         ((IMediaPlaybackControl)player).Duration.Returns(TimeSpan.FromMinutes(45));
         playerContext.CurrentPlayer.Returns(player); 

      mediaPortalServices.GetPlayerContext(Arg.Any<IPlayerSlotController>()).Returns(playerContext);


      // set queue stub
      IAsynchronousMessageQueue messageQueue = Substitute.For<IAsynchronousMessageQueue>();
      messageQueue.When(x => x.StartProxy()).Do(x => { /*nothing*/});
      mediaPortalServices.GetMessageQueue(Arg.Any<object>(), Arg.Any<string[]>()).Returns(messageQueue);

      SystemMessage playerStarted = GetSystemMessageForMessageType(PlayerManagerMessaging.MessageType.PlayerStarted);
      IFileOperations fileOperations = Substitute.For<IFileOperations>();
      TraktHandlerManager traktHandler = new TraktHandlerManager(mediaPortalServices, traktClient, fileOperations);

      // Act
      // raise start player event to start scrobble
      messageQueue.MessageReceivedProxy += Raise.Event<MessageReceivedHandler>(new AsynchronousMessageQueue(new object(), new[] { "PlayerManager" }), playerStarted);

      // Assert
      Assert.True(traktHandler.IsScrobbleStared);
      Assert.Null(traktHandler.IsScrobbleStopped);
      Assert.Equal("Title_1", traktHandler.ScrobbleTitle);
    }

    private SystemMessage GetSystemMessageForMessageType(PlayerManagerMessaging.MessageType msgType)
    {
      IPlayerSlotController psc = Substitute.For<IPlayerSlotController>();
      SystemMessage state = new SystemMessage(msgType)
      {
        ChannelName = "PlayerManager",
        MessageData = { ["PlayerSlotController"] = psc
        }
      };
      return state;
    }

    [Fact]
    public void FailedToStartScrobbleForSeriesWhenPlayerStarted()
    {
      // Arrange
      IMediaPortalServices mediaPortalServices = Substitute.For<IMediaPortalServices>();
      ITraktClient traktClient = Substitute.For<ITraktClient>();

      traktClient.RefreshAuthorization(Arg.Any<string>()).Throws(new TraktException("exception occurred"));

      TraktPluginSettings settings = new TraktPluginSettings
      {
        EnableScrobble = true,
        RefreshToken = "7e7605b9103c1c1f7afcf18dabc583499155ea6318a9d7e49f06568866bd4adb"
      };

      ITraktSettingsChangeWatcher settingsChangeWatcher = Substitute.For<ITraktSettingsChangeWatcher>();
      settingsChangeWatcher.TraktSettings.Returns(settings);
      mediaPortalServices.GetTraktSettingsWatcher().Returns(settingsChangeWatcher);

      ISettingsManager settingsManager = Substitute.For<ISettingsManager>();
      settingsManager.Load<TraktPluginSettings>().Returns(settings);
      mediaPortalServices.GetSettingsManager().Returns(settingsManager);

      IList<MediaItem> mediaItems = new List<MediaItem>();
      MockedDatabaseEpisode databaseEpisode = new MockedDatabaseEpisode("289590", 2, new List<int>(2), 1);
      mediaItems.Add(databaseEpisode.Episode);

      IContentDirectory contentDirectory = Substitute.For<IContentDirectory>();
      contentDirectory.SearchAsync(Arg.Any<MediaItemQuery>(), false, null, true).Returns(mediaItems);
      mediaPortalServices.GetServerConnectionManager().ContentDirectory.Returns(contentDirectory);

      IPlayerContext playerContext = Substitute.For<IPlayerContext>();
      playerContext.CurrentMediaItem.Returns(databaseEpisode.Episode);

      IPlayer player = Substitute.For<IPlayer, IMediaPlaybackControl>();
      ((IMediaPlaybackControl)player).Duration.Returns(TimeSpan.FromMinutes(45));
      playerContext.CurrentPlayer.Returns(player);
      mediaPortalServices.GetPlayerContext(Arg.Any<IPlayerSlotController>()).Returns(playerContext);

      IAsynchronousMessageQueue messageQueue = Substitute.For<IAsynchronousMessageQueue>();
      messageQueue.When(x => x.StartProxy()).Do(x => { /*nothing*/});
      mediaPortalServices.GetMessageQueue(Arg.Any<object>(), Arg.Any<string[]>()).Returns(messageQueue);

      IPlayerSlotController psc = Substitute.For<IPlayerSlotController>();

      SystemMessage startedState = new SystemMessage(PlayerManagerMessaging.MessageType.PlayerStarted)
      {
        ChannelName = "PlayerManager",
        MessageData = { ["PlayerSlotController"] = psc }
      };

      IFileOperations fileOperations = Substitute.For<IFileOperations>();
      TraktHandlerManager traktHandler = new TraktHandlerManager(mediaPortalServices, traktClient, fileOperations);

      // Act
      messageQueue.MessageReceivedProxy += Raise.Event<MessageReceivedHandler>(new AsynchronousMessageQueue(new object(), new[] { "PlayerManager" }), startedState);

      // Assert
      Assert.False(traktHandler.IsScrobbleStared);
      Assert.Null(traktHandler.IsScrobbleStopped);
      Assert.Null(traktHandler.ScrobbleTitle);
    }

    [Fact]
    public void SuccessfulStartedScrobbleForMovieWhenPlayerStarted()
    {
      // Arrange
      IMediaPortalServices mediaPortalServices = Substitute.For<IMediaPortalServices>();
      ITraktClient traktClient = Substitute.For<ITraktClient>();

      traktClient.RefreshAuthorization(Arg.Any<string>()).Returns(new TraktAuthorization
      {
        RefreshToken = "20f668873a00806dfc22136042f2eb81a44d98ab9b739acf68f2b46b784dcd0c"
      });
      traktClient.StartScrobbleMovie(Arg.Any<TraktMovie>(), Arg.Any<float>()).Returns(
        new TraktMovieScrobblePostResponse
        {
          Movie = new TraktMovie
          {
            Ids = new TraktMovieIds {Imdb = "tt1431045", Tmdb = 67890},
            Title = "Movie1",
            Year = 2016,
          }
        });

      TraktPluginSettings settings = new TraktPluginSettings
      {
        EnableScrobble = true, RefreshToken = "7e7605b9103c1c1f7afcf18dabc583499155ea6318a9d7e49f06568866bd4adb"
      };

      ITraktSettingsChangeWatcher settingsChangeWatcher = Substitute.For<ITraktSettingsChangeWatcher>();
      settingsChangeWatcher.TraktSettings.Returns(settings);
      mediaPortalServices.GetTraktSettingsWatcher().Returns(settingsChangeWatcher);

      ISettingsManager settingsManager = Substitute.For<ISettingsManager>();
      settingsManager.Load<TraktPluginSettings>().Returns(settings);
      mediaPortalServices.GetSettingsManager().Returns(settingsManager);

      IList<MediaItem> mediaItems = new List<MediaItem>();
      MockedDatabaseMovie databaseMovie = new MockedDatabaseMovie("tt1431045", "67890", "Movie1", 2016, 1);
      mediaItems.Add(databaseMovie.Movie);

      IContentDirectory contentDirectory = Substitute.For<IContentDirectory>();
      contentDirectory.SearchAsync(Arg.Any<MediaItemQuery>(), false, null, true).Returns(mediaItems);
      mediaPortalServices.GetServerConnectionManager().ContentDirectory.Returns(contentDirectory);

      IPlayerContext playerContext = Substitute.For<IPlayerContext>();
      playerContext.CurrentMediaItem.Returns(databaseMovie.Movie);

      IPlayer player = Substitute.For<IPlayer, IMediaPlaybackControl>();
      ((IMediaPlaybackControl)player).Duration.Returns(TimeSpan.FromMinutes(90));
      playerContext.CurrentPlayer.Returns(player);
      mediaPortalServices.GetPlayerContext(Arg.Any<IPlayerSlotController>()).Returns(playerContext);

      IAsynchronousMessageQueue messageQueue = Substitute.For<IAsynchronousMessageQueue>();
      messageQueue.When(x => x.StartProxy()).Do(x => { /*nothing*/});
      mediaPortalServices.GetMessageQueue(Arg.Any<object>(), Arg.Any<string[]>()).Returns(messageQueue);

      IPlayerSlotController psc = Substitute.For<IPlayerSlotController>();

      SystemMessage startedState = new SystemMessage(PlayerManagerMessaging.MessageType.PlayerStarted)
      {
        ChannelName = "PlayerManager",
        MessageData = { ["PlayerSlotController"] = psc }
      };

      IFileOperations fileOperations = Substitute.For<IFileOperations>();
      TraktHandlerManager traktHandler = new TraktHandlerManager(mediaPortalServices, traktClient, fileOperations);

      // Act
      messageQueue.MessageReceivedProxy += Raise.Event<MessageReceivedHandler>(new AsynchronousMessageQueue(new object(), new[] { "PlayerManager" }), startedState);

      // Assert
      Assert.True(traktHandler.IsScrobbleStared);
      Assert.Null(traktHandler.IsScrobbleStopped);
//      Assert.Equal("Movie1 ", traktHandler.ScrobbleTitle);
    }

    [Fact]
    public void FailedToStartScrobbleForMovieWhenPlayerStarted()
    {
      // Arrange
      IMediaPortalServices mediaPortalServices = Substitute.For<IMediaPortalServices>();
      ITraktClient traktClient = Substitute.For<ITraktClient>();

      traktClient.RefreshAuthorization(Arg.Any<string>()).Throws(new TraktException("exception occurred"));

      TraktPluginSettings settings = new TraktPluginSettings
      {
        EnableScrobble = true,
        RefreshToken = "7e7605b9103c1c1f7afcf18dabc583499155ea6318a9d7e49f06568866bd4adb"
      };

      ITraktSettingsChangeWatcher settingsChangeWatcher = Substitute.For<ITraktSettingsChangeWatcher>();
      settingsChangeWatcher.TraktSettings.Returns(settings);
      mediaPortalServices.GetTraktSettingsWatcher().Returns(settingsChangeWatcher);

      ISettingsManager settingsManager = Substitute.For<ISettingsManager>();
      settingsManager.Load<TraktPluginSettings>().Returns(settings);
      mediaPortalServices.GetSettingsManager().Returns(settingsManager);

      IList<MediaItem> mediaItems = new List<MediaItem>();
      MockedDatabaseMovie databaseMovie = new MockedDatabaseMovie("tt1431045", "67890", "Movie1", 2016, 1);
      mediaItems.Add(databaseMovie.Movie);

      IContentDirectory contentDirectory = Substitute.For<IContentDirectory>();
      contentDirectory.SearchAsync(Arg.Any<MediaItemQuery>(), false, null, true).Returns(mediaItems);
      mediaPortalServices.GetServerConnectionManager().ContentDirectory.Returns(contentDirectory);

      IPlayerContext playerContext = Substitute.For<IPlayerContext>();
      playerContext.CurrentMediaItem.Returns(databaseMovie.Movie);

      IPlayer player = Substitute.For<IPlayer, IMediaPlaybackControl>();
      ((IMediaPlaybackControl)player).Duration.Returns(TimeSpan.FromMinutes(90));
      playerContext.CurrentPlayer.Returns(player);
      mediaPortalServices.GetPlayerContext(Arg.Any<IPlayerSlotController>()).Returns(playerContext);

      IAsynchronousMessageQueue messageQueue = Substitute.For<IAsynchronousMessageQueue>();
      messageQueue.When(x => x.StartProxy()).Do(x => { /*nothing*/});
      mediaPortalServices.GetMessageQueue(Arg.Any<object>(), Arg.Any<string[]>()).Returns(messageQueue);

      IPlayerSlotController psc = Substitute.For<IPlayerSlotController>();

      SystemMessage startedState = new SystemMessage(PlayerManagerMessaging.MessageType.PlayerStarted)
      {
        ChannelName = "PlayerManager",
        MessageData = { ["PlayerSlotController"] = psc }
      };
      IFileOperations fileOperations = Substitute.For<IFileOperations>();
      TraktHandlerManager traktHandler = new TraktHandlerManager(mediaPortalServices, traktClient, fileOperations);

      // Act
      messageQueue.MessageReceivedProxy += Raise.Event<MessageReceivedHandler>(new AsynchronousMessageQueue(new object(), new[] { "PlayerManager" }), startedState);

      // Assert
      Assert.False(traktHandler.IsScrobbleStared);
      Assert.Null(traktHandler.IsScrobbleStopped);
      Assert.Null(traktHandler.ScrobbleTitle);
    }

    [Fact]
    public void SuccessfulStoppedScrobbleForSeriesWhenPlayerStopped()
    {
      // Arrange
      IMediaPortalServices mediaPortalServices = Substitute.For<IMediaPortalServices>();
      ITraktClient traktClient = Substitute.For<ITraktClient>();

      traktClient.RefreshAuthorization(Arg.Any<string>()).Returns(new TraktAuthorization
      {
        RefreshToken = "20f668873a00806dfc22136042f2eb81a44d98ab9b739acf68f2b46b784dcd0c"
      });
      traktClient.StartScrobbleEpisode(Arg.Any<TraktEpisode>(), Arg.Any<TraktShow>(), Arg.Any<float>()).Returns(
        new TraktEpisodeScrobblePostResponse
        {
          Episode = new TraktEpisode
          {
            Ids = new TraktEpisodeIds { Imdb = "tt12345", Tvdb = 289590 },
            Number = 2,
            Title = "Title_1",
            SeasonNumber = 2
          }
        });

      traktClient.StopScrobbleEpisode(Arg.Any<TraktEpisode>(), Arg.Any<TraktShow>(), Arg.Any<float>()).Returns(
        new TraktEpisodeScrobblePostResponse
        {
          Episode = new TraktEpisode
          {
            Ids = new TraktEpisodeIds { Imdb = "tt12345", Tvdb = 289590 },
            Number = 2,
            Title = "Title_1",
            SeasonNumber = 2
          }
        });

      TraktPluginSettings settings = new TraktPluginSettings
      {
        EnableScrobble = true,
        RefreshToken = "7e7605b9103c1c1f7afcf18dabc583499155ea6318a9d7e49f06568866bd4adb"
      };

      ITraktSettingsChangeWatcher settingsChangeWatcher = Substitute.For<ITraktSettingsChangeWatcher>();
      settingsChangeWatcher.TraktSettings.Returns(settings);
      mediaPortalServices.GetTraktSettingsWatcher().Returns(settingsChangeWatcher);

      ISettingsManager settingsManager = Substitute.For<ISettingsManager>();
      settingsManager.Load<TraktPluginSettings>().Returns(settings);
      mediaPortalServices.GetSettingsManager().Returns(settingsManager);

      IList<MediaItem> mediaItems = new List<MediaItem>();
      MockedDatabaseEpisode databaseEpisode = new MockedDatabaseEpisode("289590", 2, new List<int>(2), 1);
      mediaItems.Add(databaseEpisode.Episode);

      IContentDirectory contentDirectory = Substitute.For<IContentDirectory>();
      contentDirectory.SearchAsync(Arg.Any<MediaItemQuery>(), false, null, true).Returns(mediaItems);
      mediaPortalServices.GetServerConnectionManager().ContentDirectory.Returns(contentDirectory);

      IPlayerContext playerContext = Substitute.For<IPlayerContext>();
      playerContext.CurrentMediaItem.Returns(databaseEpisode.Episode);

      IPlayer player = Substitute.For<IPlayer, IMediaPlaybackControl>();
      ((IMediaPlaybackControl)player).Duration.Returns(TimeSpan.FromMinutes(45));
      playerContext.CurrentPlayer.Returns(player);
      mediaPortalServices.GetPlayerContext(Arg.Any<IPlayerSlotController>()).Returns(playerContext);

      IAsynchronousMessageQueue messageQueue = Substitute.For<IAsynchronousMessageQueue>();
      messageQueue.When(x => x.StartProxy()).Do(x => { /*nothing*/});
      mediaPortalServices.GetMessageQueue(Arg.Any<object>(), Arg.Any<string[]>()).Returns(messageQueue);

      IPlayerSlotController psc = Substitute.For<IPlayerSlotController>();

      SystemMessage startedState = new SystemMessage(PlayerManagerMessaging.MessageType.PlayerStarted)
      {
        ChannelName = "PlayerManager",
        MessageData = { ["PlayerSlotController"] = psc }
      };

      SystemMessage stoppedState = new SystemMessage(PlayerManagerMessaging.MessageType.PlayerStopped)
      {
        ChannelName = "PlayerManager",
        MessageData = { ["PlayerSlotController"] = psc }
      };
      IFileOperations fileOperations = Substitute.For<IFileOperations>();
      TraktHandlerManager traktHandler = new TraktHandlerManager(mediaPortalServices, traktClient, fileOperations);

      // Act
      // start player
      messageQueue.MessageReceivedProxy += Raise.Event<MessageReceivedHandler>(new AsynchronousMessageQueue(new object(), new[] { "PlayerManager" }), startedState);
      // stop player
      messageQueue.MessageReceivedProxy += Raise.Event<MessageReceivedHandler>(new AsynchronousMessageQueue(new object(), new[] { "PlayerManager" }), stoppedState);

      // Assert
      Assert.False(traktHandler.IsScrobbleStared);
      Assert.True(traktHandler.IsScrobbleStopped);
      Assert.Equal("Title_1", traktHandler.ScrobbleTitle);
    }

    [Fact]
    public void FailedToStopScrobbleForSeriesWhenPlayerStopped()
    {
      // Arrange
      IMediaPortalServices mediaPortalServices = Substitute.For<IMediaPortalServices>();
      ITraktClient traktClient = Substitute.For<ITraktClient>();

      traktClient.RefreshAuthorization(Arg.Any<string>()).Returns(new TraktAuthorization
      {
        RefreshToken = "20f668873a00806dfc22136042f2eb81a44d98ab9b739acf68f2b46b784dcd0c"
      });
      traktClient.StartScrobbleEpisode(Arg.Any<TraktEpisode>(), Arg.Any<TraktShow>(), Arg.Any<float>()).Returns(
        new TraktEpisodeScrobblePostResponse
        {
          Episode = new TraktEpisode
          {
            Ids = new TraktEpisodeIds { Imdb = "tt12345", Tvdb = 289590 },
            Number = 2,
            Title = "Title_1",
            SeasonNumber = 2
          }
        });

      traktClient.StopScrobbleEpisode(Arg.Any<TraktEpisode>(), Arg.Any<TraktShow>(), Arg.Any<float>()).Throws(new TraktException("exception occurred"));

      TraktPluginSettings settings = new TraktPluginSettings
      {
        EnableScrobble = true,
        RefreshToken = "7e7605b9103c1c1f7afcf18dabc583499155ea6318a9d7e49f06568866bd4adb"
      };

      ITraktSettingsChangeWatcher settingsChangeWatcher = Substitute.For<ITraktSettingsChangeWatcher>();
      settingsChangeWatcher.TraktSettings.Returns(settings);
      mediaPortalServices.GetTraktSettingsWatcher().Returns(settingsChangeWatcher);

      ISettingsManager settingsManager = Substitute.For<ISettingsManager>();
      settingsManager.Load<TraktPluginSettings>().Returns(settings);
      mediaPortalServices.GetSettingsManager().Returns(settingsManager);

      IList<MediaItem> mediaItems = new List<MediaItem>();
      MockedDatabaseEpisode databaseEpisode = new MockedDatabaseEpisode("289590", 2, new List<int>(2), 1);
      mediaItems.Add(databaseEpisode.Episode);

      IContentDirectory contentDirectory = Substitute.For<IContentDirectory>();
      contentDirectory.SearchAsync(Arg.Any<MediaItemQuery>(), false, null, true).Returns(mediaItems);
      mediaPortalServices.GetServerConnectionManager().ContentDirectory.Returns(contentDirectory);

      IPlayerContext playerContext = Substitute.For<IPlayerContext>();
      playerContext.CurrentMediaItem.Returns(databaseEpisode.Episode);

      IPlayer player = Substitute.For<IPlayer, IMediaPlaybackControl>();
      ((IMediaPlaybackControl)player).Duration.Returns(TimeSpan.FromMinutes(45));
      playerContext.CurrentPlayer.Returns(player);
      mediaPortalServices.GetPlayerContext(Arg.Any<IPlayerSlotController>()).Returns(playerContext);

      IAsynchronousMessageQueue messageQueue = Substitute.For<IAsynchronousMessageQueue>();
      messageQueue.When(x => x.StartProxy()).Do(x => { /*nothing*/});
      mediaPortalServices.GetMessageQueue(Arg.Any<object>(), Arg.Any<string[]>()).Returns(messageQueue);

      IPlayerSlotController psc = Substitute.For<IPlayerSlotController>();

      SystemMessage startedState = new SystemMessage(PlayerManagerMessaging.MessageType.PlayerStarted)
      {
        ChannelName = "PlayerManager",
        MessageData = { ["PlayerSlotController"] = psc }
      };

      SystemMessage stoppedState = new SystemMessage(PlayerManagerMessaging.MessageType.PlayerStopped)
      {
        ChannelName = "PlayerManager",
        MessageData = { ["PlayerSlotController"] = psc }
      };

      IFileOperations fileOperations = Substitute.For<IFileOperations>();
      TraktHandlerManager traktHandler = new TraktHandlerManager(mediaPortalServices, traktClient, fileOperations);

      // Act
      // start player
      messageQueue.MessageReceivedProxy += Raise.Event<MessageReceivedHandler>(new AsynchronousMessageQueue(new object(), new[] { "PlayerManager" }), startedState);
      // stop player
      messageQueue.MessageReceivedProxy += Raise.Event<MessageReceivedHandler>(new AsynchronousMessageQueue(new object(), new[] { "PlayerManager" }), stoppedState);


      // Assert
      Assert.True(traktHandler.IsScrobbleStared);
      Assert.False(traktHandler.IsScrobbleStopped);
      Assert.Equal("Title_1", traktHandler.ScrobbleTitle);
    }

    [Fact]
    public void SuccessfulStoppedScrobbleForMovieWhenPlayerStopped()
    {
      // Arrange
      IMediaPortalServices mediaPortalServices = Substitute.For<IMediaPortalServices>();
      ITraktClient traktClient = Substitute.For<ITraktClient>();

      traktClient.RefreshAuthorization(Arg.Any<string>()).Returns(new TraktAuthorization
      {
        RefreshToken = "20f668873a00806dfc22136042f2eb81a44d98ab9b739acf68f2b46b784dcd0c"
      });
      traktClient.StartScrobbleMovie(Arg.Any<TraktMovie>(), Arg.Any<float>()).Returns(
        new TraktMovieScrobblePostResponse
        {
          Movie = new TraktMovie
          {
            Ids = new TraktMovieIds { Imdb = "tt1431045", Tmdb = 67890 },
            Title = "Movie1",
            Year = 2016,
          }
        });

      traktClient.StopScrobbleMovie(Arg.Any<TraktMovie>(), Arg.Any<float>()).Returns(
        new TraktMovieScrobblePostResponse
        {
          Movie = new TraktMovie
          {
            Ids = new TraktMovieIds { Imdb = "tt1431045", Tmdb = 67890 },
            Title = "Movie1",
            Year = 2016,
          }
        });

      TraktPluginSettings settings = new TraktPluginSettings
      {
        EnableScrobble = true,
        RefreshToken = "7e7605b9103c1c1f7afcf18dabc583499155ea6318a9d7e49f06568866bd4adb"
      };

      ITraktSettingsChangeWatcher settingsChangeWatcher = Substitute.For<ITraktSettingsChangeWatcher>();
      settingsChangeWatcher.TraktSettings.Returns(settings);
      mediaPortalServices.GetTraktSettingsWatcher().Returns(settingsChangeWatcher);

      ISettingsManager settingsManager = Substitute.For<ISettingsManager>();
      settingsManager.Load<TraktPluginSettings>().Returns(settings);
      mediaPortalServices.GetSettingsManager().Returns(settingsManager);

      IList<MediaItem> mediaItems = new List<MediaItem>();
      MockedDatabaseMovie databaseMovie = new MockedDatabaseMovie("tt1431045", "67890", "Movie1", 2016, 1);
      mediaItems.Add(databaseMovie.Movie);

      IContentDirectory contentDirectory = Substitute.For<IContentDirectory>();
      contentDirectory.SearchAsync(Arg.Any<MediaItemQuery>(), false, null, true).Returns(mediaItems);
      mediaPortalServices.GetServerConnectionManager().ContentDirectory.Returns(contentDirectory);

      IPlayerContext playerContext = Substitute.For<IPlayerContext>();
      playerContext.CurrentMediaItem.Returns(databaseMovie.Movie);

      IPlayer player = Substitute.For<IPlayer, IMediaPlaybackControl>();
      ((IMediaPlaybackControl)player).Duration.Returns(TimeSpan.FromMinutes(90));
      playerContext.CurrentPlayer.Returns(player);
      mediaPortalServices.GetPlayerContext(Arg.Any<IPlayerSlotController>()).Returns(playerContext);

      IAsynchronousMessageQueue messageQueue = Substitute.For<IAsynchronousMessageQueue>();
      messageQueue.When(x => x.StartProxy()).Do(x => { /*nothing*/});
      mediaPortalServices.GetMessageQueue(Arg.Any<object>(), Arg.Any<string[]>()).Returns(messageQueue);

      IPlayerSlotController psc = Substitute.For<IPlayerSlotController>();

      SystemMessage startedState = new SystemMessage(PlayerManagerMessaging.MessageType.PlayerStarted)
      {
        ChannelName = "PlayerManager",
        MessageData = { ["PlayerSlotController"] = psc }
      };

      SystemMessage stoppedState = new SystemMessage(PlayerManagerMessaging.MessageType.PlayerStopped)
      {
        ChannelName = "PlayerManager",
        MessageData = { ["PlayerSlotController"] = psc }
      };
      IFileOperations fileOperations = Substitute.For<IFileOperations>();
      TraktHandlerManager traktHandler = new TraktHandlerManager(mediaPortalServices, traktClient, fileOperations);

      // Act
      // start player
      messageQueue.MessageReceivedProxy += Raise.Event<MessageReceivedHandler>(new AsynchronousMessageQueue(new object(), new[] { "PlayerManager" }), startedState);
      // stop player
      messageQueue.MessageReceivedProxy += Raise.Event<MessageReceivedHandler>(new AsynchronousMessageQueue(new object(), new[] { "PlayerManager" }), stoppedState);


      // Assert
      Assert.False(traktHandler.IsScrobbleStared);
      Assert.True(traktHandler.IsScrobbleStopped);
      Assert.Equal("Movie1", traktHandler.ScrobbleTitle);
    }

    [Fact]
    public void FailedToStopScrobbleForMovieWhenPlayerStopped()
    {
      // Arrange
      IMediaPortalServices mediaPortalServices = Substitute.For<IMediaPortalServices>();
      ITraktClient traktClient = Substitute.For<ITraktClient>();

      traktClient.RefreshAuthorization(Arg.Any<string>()).Returns(new TraktAuthorization
      {
        RefreshToken = "20f668873a00806dfc22136042f2eb81a44d98ab9b739acf68f2b46b784dcd0c"
      });
      traktClient.StartScrobbleMovie(Arg.Any<TraktMovie>(), Arg.Any<float>()).Returns(
        new TraktMovieScrobblePostResponse
        {
          Movie = new TraktMovie
          {
            Ids = new TraktMovieIds { Imdb = "tt1431045", Tmdb = 67890 },
            Title = "Movie1",
            Year = 2016,
          }
        });

      traktClient.StopScrobbleMovie(Arg.Any<TraktMovie>(), Arg.Any<float>()).Throws(new TraktException("exception occurred"));

      TraktPluginSettings settings = new TraktPluginSettings
      {
        EnableScrobble = true,
        RefreshToken = "7e7605b9103c1c1f7afcf18dabc583499155ea6318a9d7e49f06568866bd4adb"
      };

      ITraktSettingsChangeWatcher settingsChangeWatcher = Substitute.For<ITraktSettingsChangeWatcher>();
      settingsChangeWatcher.TraktSettings.Returns(settings);
      mediaPortalServices.GetTraktSettingsWatcher().Returns(settingsChangeWatcher);

      ISettingsManager settingsManager = Substitute.For<ISettingsManager>();
      settingsManager.Load<TraktPluginSettings>().Returns(settings);
      mediaPortalServices.GetSettingsManager().Returns(settingsManager);

      IList<MediaItem> mediaItems = new List<MediaItem>();
      MockedDatabaseMovie databaseMovie = new MockedDatabaseMovie("tt1431045", "67890", "Movie1", 2016, 1);
      mediaItems.Add(databaseMovie.Movie);

      IContentDirectory contentDirectory = Substitute.For<IContentDirectory>();
      contentDirectory.SearchAsync(Arg.Any<MediaItemQuery>(), false, null, true).Returns(mediaItems);
      mediaPortalServices.GetServerConnectionManager().ContentDirectory.Returns(contentDirectory);

      IPlayerContext playerContext = Substitute.For<IPlayerContext>();
      playerContext.CurrentMediaItem.Returns(databaseMovie.Movie);

      IPlayer player = Substitute.For<IPlayer, IMediaPlaybackControl>();
      ((IMediaPlaybackControl)player).Duration.Returns(TimeSpan.FromMinutes(90));
      playerContext.CurrentPlayer.Returns(player);
      mediaPortalServices.GetPlayerContext(Arg.Any<IPlayerSlotController>()).Returns(playerContext);

      IAsynchronousMessageQueue messageQueue = Substitute.For<IAsynchronousMessageQueue>();
      messageQueue.When(x => x.StartProxy()).Do(x => { /*nothing*/});
      mediaPortalServices.GetMessageQueue(Arg.Any<object>(), Arg.Any<string[]>()).Returns(messageQueue);

      IPlayerSlotController psc = Substitute.For<IPlayerSlotController>();

      SystemMessage startedState = new SystemMessage(PlayerManagerMessaging.MessageType.PlayerStarted)
      {
        ChannelName = "PlayerManager",
        MessageData = { ["PlayerSlotController"] = psc }
      };

      SystemMessage stoppedState = new SystemMessage(PlayerManagerMessaging.MessageType.PlayerStopped)
      {
        ChannelName = "PlayerManager",
        MessageData = { ["PlayerSlotController"] = psc }
      };

      IFileOperations fileOperations = Substitute.For<IFileOperations>();
      TraktHandlerManager traktHandler = new TraktHandlerManager(mediaPortalServices, traktClient, fileOperations);

      // Act
      // start player
      messageQueue.MessageReceivedProxy += Raise.Event<MessageReceivedHandler>(new AsynchronousMessageQueue(new object(), new[] { "PlayerManager" }), startedState);
      // stop player
      messageQueue.MessageReceivedProxy += Raise.Event<MessageReceivedHandler>(new AsynchronousMessageQueue(new object(), new[] { "PlayerManager" }), stoppedState);


      // Assert
      Assert.True(traktHandler.IsScrobbleStared);
      Assert.False(traktHandler.IsScrobbleStopped);
      Assert.Equal("Movie1", traktHandler.ScrobbleTitle);
    }

    [Fact]
    public void SuccessfulNotStartedScrobbleForMusicTrackWhenPlayerStarted()
    {
      // Arrange

      TraktHandlerManager traktHandler = new TraktHandlerManager(null, null, null);

      // Act


      // Assert
      Assert.False(traktHandler.IsScrobbleStared);
      Assert.Null(traktHandler.IsScrobbleStopped);
      Assert.Empty(traktHandler.ScrobbleTitle);
    }

    [Fact]
    public void FailedToNotStartScrobbleForMusicTrackWhenPlayerStarted()
    {
      // Arrange

      TraktHandlerManager traktHandler = new TraktHandlerManager(null, null, null);

      // Act


      // Assert
      Assert.True(traktHandler.IsScrobbleStared);
      Assert.Null(traktHandler.IsScrobbleStopped);
      Assert.NotEmpty(traktHandler.ScrobbleTitle);
    }

    [Fact]
    public void SettingsChanged()
    {
      // Arrange
      IMediaPortalServices mediaPortalServices = Substitute.For<IMediaPortalServices>();
      ITraktClient traktClient = Substitute.For<ITraktClient>();
      ITraktSettingsChangeWatcher settingsChangeWatcher = Substitute.For<ITraktSettingsChangeWatcher>();
      TraktPluginSettings settings = new TraktPluginSettings();
      settingsChangeWatcher.TraktSettings.Returns(settings);
      mediaPortalServices.GetTraktSettingsWatcher().Returns(settingsChangeWatcher);

      IFileOperations fileOperations = Substitute.For<IFileOperations>();
      TraktHandlerManager traktHandler = new TraktHandlerManager(mediaPortalServices, traktClient, fileOperations);

      // Act
      settings.EnableScrobble = true;
      settingsChangeWatcher.TraktSettingsChanged += Raise.Event();
      
      // Assert
      Assert.True(traktHandler.IsActive);
    }

  }
}