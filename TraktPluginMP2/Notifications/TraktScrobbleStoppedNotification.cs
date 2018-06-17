namespace TraktPluginMP2.Notifications
{
  public class TraktScrobbleStoppedNotification : TraktScrobbleNotificationBase
  {
    const string SUPER_LAYER_SCREEN = "TraktScrobbleStoppedNotification";

    public TraktScrobbleStoppedNotification(string title, bool isSuccess) : base(title, isSuccess)
    {
    }

    public override string SuperLayerScreenName
    {
      get { return SUPER_LAYER_SCREEN; }
    }
  }
}