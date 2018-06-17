using MediaPortal.Common.Configuration.ConfigurationClasses;
using MediaPortal.Common.Localization;

namespace TraktPluginMP2.Settings.Configuration
{
  public class ScrobbleStoppedNotificationSetting : SingleSelectionList
  {
    public ScrobbleStoppedNotificationSetting()
    {
      _items.Add(LocalizationHelper.CreateResourceString("[Settings.Plugins.Trakt.ShowScrobbleNotificationAlways]"));
      _items.Add(LocalizationHelper.CreateResourceString("[Settings.Plugins.Trakt.ShowScrobbleNotificationOnFailure]"));
      _items.Add(LocalizationHelper.CreateResourceString("[Settings.Plugins.Trakt.DisableScrobbleNotifications]"));
    }
    public override void Load()
    {
      TraktPluginSettings settings = SettingsManager.Load<TraktPluginSettings>();
      if (settings.ShowScrobbleStoppedNotifications && settings.ShowScrobbleStoppedNotificationsOnFailure)
      {
        Selected = 0;
      }
      else if (!settings.ShowScrobbleStoppedNotifications && settings.ShowScrobbleStoppedNotificationsOnFailure)
      {
        Selected = 1;
      }
      else
      {
        Selected = 2;
      }
    }

    public override void Save()
    {
      base.Save();
      TraktPluginSettings settings = SettingsManager.Load<TraktPluginSettings>();

      if (Selected == 0)
      {
        settings.ShowScrobbleStoppedNotifications = true;
        settings.ShowScrobbleStoppedNotificationsOnFailure = true;
      }
      else if (Selected == 1)
      {
        settings.ShowScrobbleStoppedNotificationsOnFailure = true;
        settings.ShowScrobbleStoppedNotifications = false;
      }
      else
      {
        settings.ShowScrobbleStoppedNotificationsOnFailure = false;
        settings.ShowScrobbleStoppedNotifications = false;
      }
      SettingsManager.Save(settings);
    }
  }
}