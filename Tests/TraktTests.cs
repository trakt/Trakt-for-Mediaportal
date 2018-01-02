using System;
using System.Collections.Generic;
using System.Threading;
using MediaPortal.Common.Messaging;
using MediaPortal.Common.Settings;
using MediaPortal.Common.Threading;
using MediaPortal.UI.Presentation.Players;
using NSubstitute;
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
}