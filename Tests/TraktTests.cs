using System;
using System.Collections.Generic;
using MediaPortal.Common.Messaging;
using MediaPortal.UI.Presentation.Players;
using NSubstitute;
using TraktPluginMP2.Handlers;
using TraktPluginMP2.Models;
using TraktPluginMP2.Services;
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
