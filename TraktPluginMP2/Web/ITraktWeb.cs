namespace TraktPluginMP2.Web
{
  public interface ITraktWeb
  {
    string PostToTrakt(string address, string postData, bool logRequest = true);
  }
}