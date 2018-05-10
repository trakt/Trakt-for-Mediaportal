namespace TraktPluginMP2.Services
{
  public interface IFileOperations
  {
    string LoadFileCache(string file);

    void SaveFileToCache(string file, string value);
  }
}