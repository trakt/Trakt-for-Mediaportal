using System;
using MediaPortal.Common.Services.Settings;
using TraktPluginMP2.Settings;

namespace TraktPluginMP2.Services
{
  public class TraktSettingsChangeWatcher<T> : SettingsChangeWatcher<T>, ITraktSettingsChangeWatcher where T : TraktPluginSettings
  {
    public event EventHandler TraktSettingsChanged
    {
      add { SettingsChanged += value; }
      remove { SettingsChanged -= value; }
    }

    public TraktPluginSettings TraktSettings
    {
      get { return base.Settings; }
    }
  }
}