using TraktPluginMP2.Web;

namespace TraktPluginMP2.Services
{
  public class TraktServices : ITraktServices
  {
    private readonly ITraktLogin _traktLogin;
    private readonly ITraktAPI _traktApi;
    private readonly ITraktCache _traktCache;

    public TraktServices(ITraktCache traktCache, ITraktLogin traktLogin, ITraktAPI traktApi)
    {
      _traktLogin = traktLogin;
      _traktApi = traktApi;
      _traktCache = traktCache;
    }

    public ITraktCache GetTraktCache()
    {
      return _traktCache;
    }

    public ITraktAPI GetTraktApi()
    {
      return _traktApi;
    }

    public ITraktLogin GetTraktLogin()
    {
      return _traktLogin;
    }
  }
}