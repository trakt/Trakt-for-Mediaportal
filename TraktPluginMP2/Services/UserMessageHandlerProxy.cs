using System;
using MediaPortal.UI.Services.UserManagement;

namespace TraktPluginMP2.Services
{
  public class UserMessageHandlerProxy : UserMessageHandler, IUserMessageHandler
  {
    public event EventHandler UserChangedProxy
    {
      add { UserChanged += value; }
      remove { UserChanged -= value; }
    }
  }
}