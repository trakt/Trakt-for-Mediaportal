using System;
using MediaPortal.Common.Logging;

namespace TraktPluginMP2.Web
{
  public class TraktWeb : ITraktWeb
  {
    private IWebRequestExt _webRequestExt;
    private ILogger _logger;

    public TraktWeb(IWebRequestExt webRequestExt, ILogger logger)
    {
      _webRequestExt = webRequestExt;
      _logger = logger;
    }
    public string PostToTrakt(string address, string postData, bool logRequest = true)
    {
      throw new NotImplementedException();
    }
  }
}
