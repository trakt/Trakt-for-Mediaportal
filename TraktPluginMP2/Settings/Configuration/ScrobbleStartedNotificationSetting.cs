using MediaPortal.Common.Configuration.ConfigurationClasses;
using MediaPortal.Common.Localization;

namespace TraktPluginMP2.Settings.Configuration
{
  public class ScrobbleStartedNotificationSetting : SingleSelectionList
  {
    public ScrobbleStartedNotificationSetting()
    {
      _items.Add(LocalizationHelper.CreateResourceString("[Settings.Plugins.Trakt.ShowScrobbleNotificationAlways]"));
      _items.Add(LocalizationHelper.CreateResourceString("[Settings.Plugins.Trakt.ShowScrobbleNotificationOnFailure]"));
      _items.Add(LocalizationHelper.CreateResourceString("[Settings.Plugins.Trakt.DisableScrobbleNotifications]"));
    }
    public override void Load()
    {
      TraktPluginSettings settings = SettingsManager.Load<TraktPluginSettings>();
      if (settings.ShowScrobbleStartedNotifications && settings.ShowScrobbleStartedNotificationsOnFailure)
      {
        Selected = 0;
      }
      else if (!settings.ShowScrobbleStartedNotifications && settings.ShowScrobbleStartedNotificationsOnFailure)
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
        settings.ShowScrobbleStartedNotifications = true;
        settings.ShowScrobbleStartedNotificationsOnFailure = true;
      }
      else if (Selected == 1)
      {
        settings.ShowScrobbleStartedNotificationsOnFailure = true;
        settings.ShowScrobbleStartedNotifications = false;
      }
      else
      {
        settings.ShowScrobbleStartedNotificationsOnFailure = false;
        settings.ShowScrobbleStartedNotifications = false;
      }
      SettingsManager.Save(settings);
    }
  }
}