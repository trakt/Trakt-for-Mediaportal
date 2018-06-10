using MediaPortal.Common.Configuration.ConfigurationClasses;

namespace TraktPluginMP2.Settings
{
  public class IsScrobbleEnabled : YesNo
  {
    public override void Load()
    {
      _yes = SettingsManager.Load<TraktPluginSettings>().IsScrobbleEnabled;
    }

    public override void Save()
    {
      base.Save();
      TraktPluginSettings settings = SettingsManager.Load<TraktPluginSettings>();
      settings.IsScrobbleEnabled = _yes;
      SettingsManager.Save(settings);
    }
  }
}