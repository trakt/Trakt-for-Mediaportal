using MediaPortal.Common.Settings;
using TraktApiSharp.Objects.Get.Syncs.Activities;

namespace TraktPluginMP2.Settings
{
  public class TraktPluginSettings
  {
    [Setting(SettingScope.User)]
    public bool EnableTrakt { get; set; }

    [Setting(SettingScope.User)]
    public bool IsAuthorized { get; set; }

    [Setting(SettingScope.User, DefaultValue = "")]
    public string AccessToken { get; set; }

    [Setting(SettingScope.User, DefaultValue = "")]
    public string RefreshToken { get; set; }

    [Setting(SettingScope.User)]
    public TraktSyncLastActivities LastSyncActivities { get; set; }
  }
}
