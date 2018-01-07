namespace TraktAPI.Extensions
{
  public static class StringExtensions
  {
    public static string ToLogString(this string text)
    {
      return string.IsNullOrEmpty(text) ? "<empty>" : text;
    }
  }
}