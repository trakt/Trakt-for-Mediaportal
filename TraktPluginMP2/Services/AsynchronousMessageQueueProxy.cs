using System.Collections.Generic;
using MediaPortal.Common.Messaging;

namespace TraktPluginMP2.Services
{
  public class AsynchronousMessageQueueProxy : AsynchronousMessageQueue, IAsynchronousMessageQueue
  {
    public AsynchronousMessageQueueProxy(object owner, IEnumerable<string> messageChannels) : base(owner, messageChannels)
    {
    }

    public new void DoWork()
    {
      base.DoWork();
    }

    public new void HandleMessageAvailable(SystemMessage message)
    {
      base.HandleMessageAvailable(message);
    }
  }
}