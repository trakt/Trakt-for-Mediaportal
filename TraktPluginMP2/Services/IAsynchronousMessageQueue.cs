using System.Threading;
using MediaPortal.Common.Messaging;

namespace TraktPluginMP2.Services
{
  public interface IAsynchronousMessageQueue
  {
    void Dispose();

    void DoWork();

    void HandleMessageAvailable(SystemMessage message);

    event MessageReceivedHandler MessageReceived;

    event MessageReceivedHandler PreviewMessage;

    bool IsTerminated { get; }

    ThreadPriority ThreadPriority { get; set; }

    void Start();

    bool Shutdown();

    void Terminate();
  }
}