using System.Collections.Generic;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.PathManager;
using MediaPortal.Common.Settings;
using MediaPortal.Common.Threading;
using MediaPortal.UI.ServerCommunication;
using MediaPortal.UI.Services.UserManagement;

namespace TraktPluginMP2.Services
{
  public class MediaPortalServices : IMediaPortalServices
  {
    public ISettingsManager GetSettingsManager()
    {
      return ServiceRegistration.Get<ISettingsManager>();
    }

    public IThreadPool GetThreadPool()
    {
      return ServiceRegistration.Get<IThreadPool>();
    }

    public IServerConnectionManager GetServerConnectionManager()
    {
      return ServiceRegistration.Get<IServerConnectionManager>();
    }

    public IUserManagement GetUserManagement()
    {
      return ServiceRegistration.Get<IUserManagement>();
    }

    public ILogger GetLogger()
    {
      return ServiceRegistration.Get<ILogger>();
    }

    public IPathManager GetPathManager()
    {
      return ServiceRegistration.Get<IPathManager>();
    }

    //public SettingsChangeWatcher<TraktPluginSettings> Watcher()
    //{
    //  return new SettingsChangeWatcher<TraktPluginSettings>();
    //}

    public IAsynchronousMessageQueue GetMessageQueue(object owner, IEnumerable<string> messageChannel)
    {
      return new AsynchronousMessageQueueProxy(owner, messageChannel);
    }
  }
}