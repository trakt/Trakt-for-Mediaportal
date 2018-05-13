using System.Collections.Generic;
using MediaPortal.Common.Messaging;

namespace TraktPluginMP2.Services
{
  public class AsynchronousMessageQueueProxy : AsynchronousMessageQueue, IAsynchronousMessageQueue
  {
    public AsynchronousMessageQueueProxy(object owner, IEnumerable<string> messageChannels) : base(owner, messageChannels)
    {
    }

    public event MessageReceivedHandler MessageReceivedProxy
    {
      add { MessageReceived += value; }
      remove { MessageReceived -= value; }
    }

    public void StartProxy()
    {
      base.Start();
    }

    public bool ShutdownProxy()
    {
      return base.Shutdown();
    }
  }
}