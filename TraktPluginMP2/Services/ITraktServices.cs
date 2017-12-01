using TraktPluginMP2.Web;

namespace TraktPluginMP2.Services
{
  public interface ITraktServices
  {
    ITraktCache GetTraktCache();

    ITraktAPI GetTraktApi();

    ITraktLogin GetTraktLogin();
  }
}