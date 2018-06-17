using System;
using System.Collections.Generic;
using System.IO;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Common.Messaging;
using MediaPortal.Common.Settings;
using MediaPortal.Common.SystemCommunication;
using MediaPortal.UI.Presentation.Players;
using NSubstitute;
using Tests.TestData.Handler;
using TraktPluginMP2;
using TraktPluginMP2.Handlers;
using TraktPluginMP2.Notifications;
using TraktPluginMP2.Services;
using TraktPluginMP2.Settings;
using Xunit;

namespace Tests
{
  public class TraktHandlerTests
  {
    private const string DataPath = @"C:\FakeTraktUserHomePath\";

    [Fact]
    public void EnableTraktHandlerWhenSettingsChanged()
    {
      // Arrange
      IMediaPortalServices mediaPortalServices = Substitute.For<IMediaPortalServices>();
      mediaPortalServices.GetTraktUserHomePath().Returns(DataPath);
      TraktPluginSettings settings = new TraktPluginSettings
      {
        IsScrobbleEnabled = false
      };
      ITraktSettingsChangeWatcher settingsChangeWatcher = Substitute.For<ITraktSettingsChangeWatcher>();
      settingsChangeWatcher.TraktSettings.Returns(settings);
      mediaPortalServices.GetTraktSettingsWatcher().Returns(settingsChangeWatcher);

      ISettingsManager settingsManager = Substitute.For<ISettingsManager>();
      settingsManager.Load<TraktPluginSettings>().Returns(settings);
      mediaPortalServices.GetSettingsManager().Returns(settingsManager);

      ITraktClient traktClient = Substitute.For<ITraktClient>();
      IFileOperations fileOperations = Substitute.For<IFileOperations>();
      fileOperations.FileExists(Path.Combine(DataPath, FileName.Authorization.Value)).Returns(true);

      TraktHandlerManager traktHandler = new TraktHandlerManager(mediaPortalServices, traktClient, fileOperations);

      // Act
      settings.IsScrobbleEnabled = true;
      settingsChangeWatcher.TraktSettingsChanged += Raise.Event();

      // Assert
      Assert.True(traktHandler.IsActive);
    }

    [Fact]
    public void EnableTraktHandlerWhenUserChanged()
    {
      // Arrange
      IMediaPortalServices mediaPortalServices = Substitute.For<IMediaPortalServices>();
      mediaPortalServices.GetTraktUserHomePath().Returns(DataPath);
      TraktPluginSettings settings = new TraktPluginSettings
      {
        IsScrobbleEnabled = false
      };
      ITraktSettingsChangeWatcher settingsChangeWatcher = Substitute.For<ITraktSettingsChangeWatcher>();
      settingsChangeWatcher.TraktSettings.Returns(settings);
      mediaPortalServices.GetTraktSettingsWatcher().Returns(settingsChangeWatcher);

      IUserMessageHandler userMessageHandler = Substitute.For<IUserMessageHandler>();
      mediaPortalServices.GetUserMessageHandler().Returns(userMessageHandler);

      ITraktClient traktClient = Substitute.For<ITraktClient>();
      IFileOperations fileOperations = Substitute.For<IFileOperations>();
      fileOperations.FileExists(Path.Combine(DataPath, FileName.Authorization.Value)).Returns(true);

      TraktHandlerManager traktHandler = new TraktHandlerManager(mediaPortalServices, traktClient, fileOperations);

      // Act
      settings.IsScrobbleEnabled = true;
      userMessageHandler.UserChangedProxy += Raise.Event();

      // Assert
      Assert.True(traktHandler.IsActive);
    }


    [Theory]
    [ClassData(typeof(StartScrobbleMovieTestData))]
    [ClassData(typeof(StartScrobbleSeriesTestData))]
    public void StartScrobble(TraktPluginSettings settings, MediaItem mediaItem, ITraktClient traktClient, ITraktNotification notification)
    {
      // Arrange
      IMediaPortalServices mediaPortalServices = Substitute.For<IMediaPortalServices>();
      mediaPortalServices.GetTraktUserHomePath().Returns(DataPath);
      SetSettings(mediaPortalServices, settings);
      SetPlayerAndContentDirForMovie(mediaPortalServices, mediaItem);

      IAsynchronousMessageQueue messageQueue = GetMockedMsgQueue(mediaPortalServices);

      IFileOperations fileOperations = Substitute.For<IFileOperations>();
      fileOperations.FileExists(Path.Combine(DataPath, FileName.Authorization.Value)).Returns(true);

      TraktHandlerManager traktHandler = new TraktHandlerManager(mediaPortalServices, traktClient, fileOperations);
      TraktScrobbleStartedNotification expectedNotification = (TraktScrobbleStartedNotification)notification;

      // Act
      // start the player
      messageQueue.MessageReceivedProxy += Raise.Event<MessageReceivedHandler>(new AsynchronousMessageQueue(new object(), new[] { "PlayerManager" }),
        GetSystemMessageForMessageType(PlayerManagerMessaging.MessageType.PlayerStarted));

      // Assert
      mediaPortalServices.GetTraktNotificationModel().Received()
        .ShowNotification(Arg.Is<TraktScrobbleStartedNotification>(x => x.IsSuccess == expectedNotification.IsSuccess && 
                                                                        x.Title == expectedNotification.Title && 
                                                                        x.SuperLayerScreenName == expectedNotification.SuperLayerScreenName), 
                                                                        Arg.Any<TimeSpan>());
    }

    [Theory]
    [ClassData(typeof(StopScrobbleMovieTestData))]
    [ClassData(typeof(StopScrobbleSeriesTestData))]
    public void StopScrobble(TraktPluginSettings settings, MediaItem mediaItem, ITraktClient traktClient, ITraktNotification notification)
    {
      // Arrange
      IMediaPortalServices mediaPortalServices = Substitute.For<IMediaPortalServices>();
      mediaPortalServices.GetTraktUserHomePath().Returns(DataPath);
      SetSettings(mediaPortalServices, settings);
      SetPlayerAndContentDirForMovie(mediaPortalServices, mediaItem);

      IAsynchronousMessageQueue messageQueue = GetMockedMsgQueue(mediaPortalServices);

      IFileOperations fileOperations = Substitute.For<IFileOperations>();
      fileOperations.FileExists(Path.Combine(DataPath, FileName.Authorization.Value)).Returns(true);

      TraktHandlerManager traktHandler = new TraktHandlerManager(mediaPortalServices, traktClient, fileOperations);
      TraktScrobbleStoppedNotification expectedNotification = (TraktScrobbleStoppedNotification)notification;

      // Act
      // start player
      messageQueue.MessageReceivedProxy += Raise.Event<MessageReceivedHandler>(new AsynchronousMessageQueue(new object(), new[] { "PlayerManager" }),
        GetSystemMessageForMessageType(PlayerManagerMessaging.MessageType.PlayerStarted));

      // stop player
      messageQueue.MessageReceivedProxy += Raise.Event<MessageReceivedHandler>(new AsynchronousMessageQueue(new object(), new[] { "PlayerManager" }),
        GetSystemMessageForMessageType(PlayerManagerMessaging.MessageType.PlayerStopped));

      // Assert
      mediaPortalServices.GetTraktNotificationModel().Received()
        .ShowNotification(Arg.Is<TraktScrobbleStoppedNotification>(x => x.IsSuccess == expectedNotification.IsSuccess &&
                                                                        x.Title == expectedNotification.Title &&
                                                                        x.SuperLayerScreenName == expectedNotification.SuperLayerScreenName),
                                                                        Arg.Any<TimeSpan>());
    }

    private void SetSettings(IMediaPortalServices mediaPortalServices, TraktPluginSettings settings)
    {
      ITraktSettingsChangeWatcher settingsChangeWatcher = Substitute.For<ITraktSettingsChangeWatcher>();
      settingsChangeWatcher.TraktSettings.Returns(settings);
      mediaPortalServices.GetTraktSettingsWatcher().Returns(settingsChangeWatcher);

      ISettingsManager settingsManager = Substitute.For<ISettingsManager>();
      settingsManager.Load<TraktPluginSettings>().Returns(settings);
      mediaPortalServices.GetSettingsManager().Returns(settingsManager);
    }

    private void SetPlayerAndContentDirForMovie(IMediaPortalServices mediaPortalServices, MediaItem mediaItem)
    {
      IList<MediaItem> mediaItems = new List<MediaItem>();
      mediaItems.Add(mediaItem);

      IContentDirectory contentDirectory = Substitute.For<IContentDirectory>();
      contentDirectory.SearchAsync(Arg.Any<MediaItemQuery>(), false, null, true).Returns(mediaItems);
      mediaPortalServices.GetServerConnectionManager().ContentDirectory.Returns(contentDirectory);

      IPlayerContext playerContext = Substitute.For<IPlayerContext>();
      playerContext.CurrentMediaItem.Returns(mediaItem);

      IPlayer player = Substitute.For<IPlayer, IMediaPlaybackControl>();
      ((IMediaPlaybackControl)player).Duration.Returns(TimeSpan.FromMinutes(90));
      playerContext.CurrentPlayer.Returns(player);
      mediaPortalServices.GetPlayerContext(Arg.Any<IPlayerSlotController>()).Returns(playerContext);
    }

    private IAsynchronousMessageQueue GetMockedMsgQueue(IMediaPortalServices mediaPortalServices)
    {
      IAsynchronousMessageQueue messageQueue = Substitute.For<IAsynchronousMessageQueue>();
      messageQueue.When(x => x.StartProxy()).Do(x => { /*nothing*/});
      mediaPortalServices.GetMessageQueue(Arg.Any<object>(), Arg.Any<string[]>()).Returns(messageQueue);
      return messageQueue;
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
  }
}