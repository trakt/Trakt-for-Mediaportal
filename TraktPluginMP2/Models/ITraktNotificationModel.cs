using System;
using TraktPluginMP2.Notifications;

namespace TraktPluginMP2.Models
{
  public interface ITraktNotificationModel
  {
    void ShowNotification(ITraktNotification notification, TimeSpan duration);


  }
}