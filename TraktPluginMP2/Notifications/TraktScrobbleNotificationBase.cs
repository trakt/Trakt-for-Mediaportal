namespace TraktPluginMP2.Notifications
{
  public abstract class TraktScrobbleNotificationBase : ITraktNotification
  {
    protected string _title;
    protected bool _isSuccess;

    public TraktScrobbleNotificationBase(string title, bool isSuccess)
    {
      _title = title;
      _isSuccess = isSuccess;
    }

    public string Title
    {
      get { return _title; }
    }

    public bool IsSuccess
    {
      get { return _isSuccess; }
    }

    public abstract string SuperLayerScreenName { get; }
  }
}