using System.IO;
using System.Reflection;

namespace Tests
{
  public class TestUtility
  {
    public static string GetTestDataPath(string filePath)
    {
      string assemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty;
      return !string.IsNullOrEmpty(filePath) ? Path.Combine(assemblyLocation, "TestData", filePath) : string.Empty;
    }
  }
}