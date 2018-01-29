using System.Collections.Generic;
using System.Threading.Tasks;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.PathManager;
using MediaPortal.Common.Settings;
using MediaPortal.Common.Threading;
using MediaPortal.UI.ServerCommunication;
using MediaPortal.UI.Services.UserManagement;

namespace TraktPluginMP2.Services
{
  public interface IMediaPortalServices
  {
    ISettingsManager GetSettingsManager();

    IThreadPool GetThreadPool();

    IServerConnectionManager GetServerConnectionManager();

    IUserManagement GetUserManagement();

    ILogger GetLogger();

    IPathManager GetPathManager();

    //SettingsChangeWatcher<TraktPluginSettings> Watcher();

    IAsynchronousMessageQueue GetMessageQueue(object owner, IEnumerable<string> messageChannel);

    Task<bool> MarkAsWatched(MediaItem mediaItem);

    Task<bool> MarkAsUnWatched(MediaItem mediaItem);
  }
}