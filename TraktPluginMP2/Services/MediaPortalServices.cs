using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.PathManager;
using MediaPortal.Common.Settings;
using MediaPortal.Common.Threading;
using MediaPortal.UI.ServerCommunication;
using MediaPortal.UI.Services.UserManagement;
using  MediaPortal.UiComponents.Media.MediaItemActions;

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

    public async Task<bool> MarkAsWatched(MediaItem mediaItem)
    {
      bool result = false;
      SetWatched setWatchedAction = new SetWatched();
      if (await setWatchedAction.IsAvailableAsync(mediaItem))
      {
        try
        {
          var processResult = await setWatchedAction.ProcessAsync(mediaItem);
          if (processResult.Success && processResult.Result != ContentDirectoryMessaging.MediaItemChangeType.None)
          {
            ContentDirectoryMessaging.SendMediaItemChangedMessage(mediaItem, processResult.Result);
            GetLogger().Info("Marking media item '{0}' as watched", mediaItem.GetType());
            result = true;
          }
        }
        catch (Exception ex)
        {
          GetLogger().Error("Marking media item '{0}' as watched failed:", mediaItem.GetType(), ex);
          result = false;
        }
      }

      return result;
    }

    public async Task<bool> MarkAsUnWatched(MediaItem mediaItem)
    {
      bool result = false;
      SetUnwatched setUnwatchedAction = new SetUnwatched();
      if (await setUnwatchedAction.IsAvailableAsync(mediaItem))
      {
        try
        {
          var processResult = await setUnwatchedAction.ProcessAsync(mediaItem);
          if (processResult.Success && processResult.Result != ContentDirectoryMessaging.MediaItemChangeType.None)
          {
            ContentDirectoryMessaging.SendMediaItemChangedMessage(mediaItem, processResult.Result);
            GetLogger().Info("Marking media item '{0}' as unwatched", mediaItem.GetType());
            result = true;
          }
        }
        catch (Exception ex)
        {
          GetLogger().Error("Marking media item '{0}' as unwatched failed:", mediaItem.GetType(), ex);
          result = false;
        }
      }

      return result;
    }
  }
}