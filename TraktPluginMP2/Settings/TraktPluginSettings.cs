using MediaPortal.Common.Settings;

namespace TraktPluginMP2.Settings
{
  public class TraktPluginSettings
  {
    [Setting(SettingScope.User)]
    public bool IsScrobbleEnabled { get; set; }
  }
}
