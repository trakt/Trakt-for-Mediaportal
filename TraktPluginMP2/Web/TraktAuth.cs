using TraktAPI.DataStructures;
using TraktAPI.Extensions;

namespace TraktPluginMP2.Web
{
  public class TraktAuth : ITraktAuth
  {
    private readonly ITraktWeb _traktWeb;

    public TraktAuth(ITraktWeb traktWeb)
    {
      _traktWeb = traktWeb;
    }
    //public TraktOAuthToken Get0AuthToken(string key)
    //{
    //  string response = _traktWeb.PostToTrakt("", "");
    //  TraktOAuthToken loginResponse = response.FromJSON<TraktOAuthToken>();

    //  return loginResponse;
    //}
    public string Get0AuthToken(string key)
    {
      return string.Empty;
    }
  }
}
