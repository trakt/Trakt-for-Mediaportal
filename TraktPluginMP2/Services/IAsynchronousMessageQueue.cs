using System.Threading;
using MediaPortal.Common.Messaging;

namespace TraktPluginMP2.Services
{
  public interface IAsynchronousMessageQueue
  {
    event MessageReceivedHandler MessageReceivedProxy;

    void StartProxy();

    bool ShutdownProxy();
  }
}