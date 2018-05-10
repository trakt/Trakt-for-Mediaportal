using MediaPortal.Common.Messaging;
using MediaPortal.UI.Presentation.Players;
using NSubstitute;
using TraktPluginMP2.Handlers;
using TraktPluginMP2.Services;
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
      IAsynchronousMessageQueue messageQueue = Substitute.For<IAsynchronousMessageQueue>();
      messageQueue.When(x => x.Start()).Do(x => { /*nothing*/});
      mediaPortalServices.GetMessageQueue(Arg.Any<object>(), Arg.Any<string[]>()).Returns(messageQueue);
      IPlayerSlotController psc = Substitute.For<IPlayerSlotController>();
      SystemMessage startedState = new SystemMessage(PlayerManagerMessaging.MessageType.PlayerStarted)
      {
        ChannelName = "PlayerManager",
        MessageData = { ["PlayerSlotController"] = psc }
      };

      TraktHandlerManager trakthandler = new TraktHandlerManager(mediaPortalServices, traktClient);

      // Act
      messageQueue.MessageReceived += Raise.Event<MessageReceivedHandler>(new AsynchronousMessageQueue(new object(), new[] { "PlayerManager" }), startedState);

      // Assert
      Assert.True(trakthandler.StartedScrobble);
    }
  }
}