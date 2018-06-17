using System;
using MediaPortal.Common;
using MediaPortal.Common.General;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.Presentation.Workflow;
using TraktPluginMP2.Notifications;

namespace TraktPluginMP2.Models
{
  public class TraktNotificationModel : ITraktNotificationModel
  {
    public const string MODEL_ID_STR = "FF91EFE4-120E-499B-8461-BB3C21DFB3E3";
    public static readonly Guid MODEL_ID = new Guid(MODEL_ID_STR);

    protected AbstractProperty _notificationProperty = new WProperty(typeof(ITraktNotification), null);

    public AbstractProperty NotificationProperty
    {
      get { return _notificationProperty; }
    }

    public ITraktNotification Notification
    {
      get { return (ITraktNotification)_notificationProperty.GetValue(); }
      set { _notificationProperty.SetValue(value); }
    }

    public void ShowNotification(ITraktNotification notification, TimeSpan duration)
    {
      Instance.Show(notification, duration);
    }

    private static TraktNotificationModel Instance
    {
      get { return (TraktNotificationModel)ServiceRegistration.Get<IWorkflowManager>().GetModel(MODEL_ID); }
    }

    private void Show(ITraktNotification notification, TimeSpan duration)
    {
      Notification = notification;
      ServiceRegistration.Get<ISuperLayerManager>().ShowSuperLayer(notification.SuperLayerScreenName, duration);
    }
  }
}