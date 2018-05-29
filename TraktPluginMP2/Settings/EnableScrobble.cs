using MediaPortal.Common.Configuration.ConfigurationClasses;

namespace TraktPluginMP2.Settings
{
  public class EnableScrobble : YesNo
  {
    public override void Load()
    {
      _yes = SettingsManager.Load<TraktPluginSettings>().EnableScrobble;
    }

    public override void Save()
    {
      base.Save();
      TraktPluginSettings settings = SettingsManager.Load<TraktPluginSettings>();
      settings.EnableScrobble = _yes;
      SettingsManager.Save(settings);
    }
  }
}