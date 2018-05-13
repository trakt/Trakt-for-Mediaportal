using System;

namespace TraktPluginMP2.Services
{
  public interface IUserMessageHandler
  {
    event EventHandler UserChangedProxy;
  }
}