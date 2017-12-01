using TraktAPI.DataStructures;
using TraktPluginMP2.Services;
using TraktPluginMP2.Settings;

namespace TraktPluginMP2.Web
{
  public class TraktLogin : ITraktLogin
  {
    private readonly IMediaPortalServices _mediaPortalServices;
    private readonly ITraktAuth _traktAuth;

    public TraktLogin(ITraktAuth traktAuth, IMediaPortalServices mediaPortalServices)
    {
      _mediaPortalServices = mediaPortalServices;
      _traktAuth = traktAuth;
    }

    public bool Login(string key)
    {
      TraktPluginSettings settings = _mediaPortalServices.GetSettingsManager().Load<TraktPluginSettings>();

      string response = _traktAuth.Get0AuthToken(key);

      if (response == null || string.IsNullOrEmpty(response))
      {
        _mediaPortalServices.GetLogger().Error("Unable to login to trakt");
        return false;
      }

      settings.IsAuthorized = true;
      settings.TraktOAuthToken = response;
      _mediaPortalServices.GetSettingsManager().Save(settings);
      _mediaPortalServices.GetLogger().Error("Successfully logged in");

      return true;
    }
  }
}