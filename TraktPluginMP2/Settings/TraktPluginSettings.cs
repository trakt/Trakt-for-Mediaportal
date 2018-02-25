using MediaPortal.Common.Settings;

namespace TraktPluginMP2.Settings
{
  public class TraktPluginSettings
  {
    [Setting(SettingScope.User)]
    public bool EnableTrakt { get; set; }

    [Setting(SettingScope.User)]
    public bool IsAuthorized { get; set; }

    [Setting(SettingScope.User, DefaultValue = "")]
    public string Username { get; set; }

    [Setting(SettingScope.User, DefaultValue = "")]
    public string TraktOAuthToken { get; set; }

    [Setting(SettingScope.User, DefaultValue = false)]
    public bool UseSSL { get; set; }

    [Setting(SettingScope.User, DefaultValue = false)]
    public bool KeepTraktLibraryClean { get; set; }

    [Setting(SettingScope.User, DefaultValue = 0)]
    public int TrendingMoviesDefaultLayout { get; set; }

    [Setting(SettingScope.User, DefaultValue = 15)]
    public int WebRequestCacheMinutes { get; set; }

    [Setting(SettingScope.User, DefaultValue = 5)]
    public int SyncPlaybackCacheExpiry { get; set; }

    [Setting(SettingScope.User, DefaultValue = 1)]
    public int LogLevel { get; set; }

   // [Setting(SettingScope.User)]
  //  public TraktLastSyncActivities LastSyncActivities { get; set; }

   // [Setting(SettingScope.User)]
  //  public IEnumerable<TraktCache.ListActivity> LastListActivities { get; set; }

    [Setting(SettingScope.User, DefaultValue = true)]
    public bool SkipMoviesWithNoIdsOnSync { get; set; }

    [Setting(SettingScope.User, DefaultValue = 100)]
    public int SyncBatchSize { get; set; }
  }
}
