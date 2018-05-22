using System.Collections.Generic;
using System.Threading.Tasks;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.PathManager;
using MediaPortal.Common.Settings;
using MediaPortal.Common.Threading;
using MediaPortal.UI.ServerCommunication;
using MediaPortal.Common.UserManagement;
using MediaPortal.UI.Presentation.Players;

namespace TraktPluginMP2.Services
{
  public interface IMediaPortalServices
  {
    ISettingsManager GetSettingsManager();

    IThreadPool GetThreadPool();

    IServerConnectionManager GetServerConnectionManager();

    IUserManagement GetUserManagement();

    IUserMessageHandler GetUserMessageHandler();

    ILogger GetLogger();

    IPathManager GetPathManager();

    ITraktSettingsChangeWatcher GetTraktSettingsWatcher();

    IAsynchronousMessageQueue GetMessageQueue(object owner, IEnumerable<string> messageChannel);

    IPlayerContext GetPlayerContext(IPlayerSlotController psc);

    Task<bool> MarkAsWatched(MediaItem mediaItem);

    Task<bool> MarkAsUnWatched(MediaItem mediaItem);
  }
}