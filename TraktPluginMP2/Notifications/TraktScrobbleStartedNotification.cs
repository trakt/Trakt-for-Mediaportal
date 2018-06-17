namespace TraktPluginMP2.Notifications
{
  public class TraktScrobbleStartedNotification : TraktScrobbleNotificationBase
  {
    const string SUPER_LAYER_SCREEN = "TraktScrobbleStartedNotification";

    public TraktScrobbleStartedNotification(string title, bool isSuccess) : base(title, isSuccess)
    {
    }

    public override string SuperLayerScreenName
    {
      get { return SUPER_LAYER_SCREEN; }
    }
  }
}