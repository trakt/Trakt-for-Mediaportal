using System;
using MediaPortal.Common.Messaging;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UI.Presentation.Players.ResumeState;
using TraktApiSharp;
using TraktPluginMP2.Services;

namespace TraktPluginMP2.Handlers
{
  public class TraktHandlerManager : IDisposable
  {
    private readonly IMediaPortalServices _mediaPortalServices;
    private readonly ITraktClient _traktClient;
    private IAsynchronousMessageQueue _messageQueue;
    private TimeSpan _duration;
    private double _progress;


    public TraktHandlerManager(IMediaPortalServices mediaPortalServices, ITraktClient traktClient)
    {
      _mediaPortalServices = mediaPortalServices;
      _traktClient = traktClient;
      _mediaPortalServices.GetTraktSettingsWatcher().TraktSettingsChanged += ConfigureHandler;
      _mediaPortalServices.GetUserMessageHandler().UserChangedProxy += ConfigureHandler;
      ConfigureHandler();
    }

    private void ConfigureHandler(object sender, EventArgs e)
    {
      ConfigureHandler();
    }

    public bool Active { get; private set; }

    public bool StartedScrobble { get; private set; }

    public bool? StoppedScrobble { get; private set; }

    private void ConfigureHandler()
    {
      if (_mediaPortalServices.GetTraktSettingsWatcher().TraktSettings.EnableTrakt)
      {
        SubscribeToMessages();
      }
      else
      {
        UnsubscribeFromMessages();
      }
    }

    private void SubscribeToMessages()
    {
      if (_messageQueue == null)
      {
        _messageQueue = _mediaPortalServices.GetMessageQueue(this, new string[]
        {
          PlayerManagerMessaging.CHANNEL
        });
        _messageQueue.MessageReceivedProxy += OnMessageReceived;
        _messageQueue.StartProxy();
        Active = true;
      }
    }

    private void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
    {
      if (message.ChannelName == PlayerManagerMessaging.CHANNEL)
      {
        PlayerManagerMessaging.MessageType messageType = (PlayerManagerMessaging.MessageType) message.MessageType;
        switch (messageType)
        {
          case PlayerManagerMessaging.MessageType.PlayerResumeState:
            IResumeState resumeState = (IResumeState)message.MessageData[PlayerManagerMessaging.KEY_RESUME_STATE];
            PositionResumeState positionResume = resumeState as PositionResumeState;
            if (positionResume != null)
            {
              TimeSpan resumePosition = positionResume.ResumePosition;
              _progress = Math.Min((int)(resumePosition.TotalSeconds * 100 / _duration.TotalSeconds), 100);
            }
            break;
          case PlayerManagerMessaging.MessageType.PlayerError:
          case PlayerManagerMessaging.MessageType.PlayerEnded:
          case PlayerManagerMessaging.MessageType.PlayerStopped:
            StopScrobble();
            break;
          case PlayerManagerMessaging.MessageType.PlayerStarted:
            var psc = (IPlayerSlotController)message.MessageData[PlayerManagerMessaging.PLAYER_SLOT_CONTROLLER];
            StartScrobble(psc);
            break;
        }
      }
    }

    private void StartScrobble(IPlayerSlotController psc)
    {
      // TODO: implement!
      StartedScrobble = true;
    }

    private void StopScrobble()
    {
      // TODO: implement!
      StoppedScrobble = true;
    }

    private void UnsubscribeFromMessages()
    {
      if (_messageQueue != null)
      {
        _messageQueue.ShutdownProxy();
        _messageQueue = null;
        Active = false;
      }
    }

    public void Dispose()
    {
      UnsubscribeFromMessages();
    }
  }
}