using System;
using TraktPluginMP2.Settings;

namespace TraktPluginMP2.Services
{
  public interface ITraktSettingsChangeWatcher
  {
    event EventHandler TraktSettingsChanged;

    TraktPluginSettings TraktSettings { get; }
  }
}