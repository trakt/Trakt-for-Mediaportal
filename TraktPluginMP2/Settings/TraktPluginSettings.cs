using MediaPortal.Common.Settings;

namespace TraktPluginMP2.Settings
{
  public class TraktPluginSettings
  {
    [Setting(SettingScope.User, DefaultValue = false)]
    public bool IsScrobbleEnabled { get; set; }

    [Setting(SettingScope.User, DefaultValue = true)]
    public bool ShowScrobbleStartedNotifications { get; set; }

    [Setting(SettingScope.User, DefaultValue = true)]
    public bool ShowScrobbleStartedNotificationsOnFailure { get; set; }

    [Setting(SettingScope.User, DefaultValue = true)]
    public bool ShowScrobbleStoppedNotifications { get; set; }

    [Setting(SettingScope.User, DefaultValue = true)]
    public bool ShowScrobbleStoppedNotificationsOnFailure { get; set; }
  }
}
