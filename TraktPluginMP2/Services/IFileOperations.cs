using System.Text;

namespace TraktPluginMP2.Services
{
  public interface IFileOperations
  {
    bool FileExists(string path);

    string FileReadAllText(string file);

    void FileWriteAllText(string path, string contents, Encoding encoding);
  }
}