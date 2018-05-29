using System.IO;
using System.Text;

namespace TraktPluginMP2.Services
{
  public class FileOperations : IFileOperations
  {
    public bool FileExists(string path)
    {
      return File.Exists(path);
    }

    public string FileReadAllText(string path)
    {
      return File.ReadAllText(path);
    }

    public void FileWriteAllText(string path, string contents, Encoding encoding)
    {
      File.WriteAllText(path, contents, encoding);
    }
  }
}