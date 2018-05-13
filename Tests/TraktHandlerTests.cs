using System;
using MediaPortal.Common.Messaging;
using MediaPortal.Common.Services.Settings;
using MediaPortal.UI.Presentation.Players;
using NSubstitute;
using TraktPluginMP2.Handlers;
using TraktPluginMP2.Services;
using TraktPluginMP2.Settings;
using Xunit;

namespace Tests
{
  public class TraktHandlerTests
  {
    [Fact]
    public void StartScrobbleWhenPlayerStarted()
    {
      // Arrange
      IMediaPortalServices mediaPortalServices = Substitute.For<IMediaPortalServices>();
      ITraktClient traktClient = Substitute.For<ITraktClient>();
      ITraktSettingsChangeWatcher settingsChangeWatcher = Substitute.For<ITraktSettingsChangeWatcher>();
      TraktPluginSettings settings = new TraktPluginSettings{EnableTrakt = true};
      settingsChangeWatcher.TraktSettings.Returns(settings);
      mediaPortalServices.GetTraktSettingsWatcher().Returns(settingsChangeWatcher);

      IAsynchronousMessageQueue messageQueue = Substitute.For<IAsynchronousMessageQueue>();
      messageQueue.When(x => x.StartProxy()).Do(x => { /*nothing*/});
      mediaPortalServices.GetMessageQueue(Arg.Any<object>(), Arg.Any<string[]>()).Returns(messageQueue);
      IPlayerSlotController psc = Substitute.For<IPlayerSlotController>();
      SystemMessage startedState = new SystemMessage(PlayerManagerMessaging.MessageType.PlayerStarted)
      {
        ChannelName = "PlayerManager",
        MessageData = { ["PlayerSlotController"] = psc }
      };

      TraktHandlerManager trakthandler = new TraktHandlerManager(mediaPortalServices, traktClient);

      // Act
      messageQueue.MessageReceivedProxy += Raise.Event<MessageReceivedHandler>(new AsynchronousMessageQueue(new object(), new[] { "PlayerManager" }), startedState);

      // Assert
      Assert.True(trakthandler.StartedScrobble);
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
      
      TraktHandlerManager trakthandler = new TraktHandlerManager(mediaPortalServices, traktClient);

      // Act
      settings.EnableTrakt = true;
      settingsChangeWatcher.TraktSettingsChanged += Raise.Event();
      
      // Assert
      Assert.True(trakthandler.Active);
    }
  }
}