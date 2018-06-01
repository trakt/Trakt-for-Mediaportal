namespace TraktPluginMP2
{
  /// <summary>
  /// 
  /// </summary>
  public class FileName
  {
    private FileName(string value)
    {
      Value = value;
    }

    public string Value { get; set; }

    public static FileName LastActivity { get { return new FileName("last.sync.activities.json");} }
    public static FileName Authorization { get { return new FileName("authorization.json"); } }
    public static FileName WatchedMovies { get { return new FileName("watched.movies.json"); } }
    public static FileName CollectedMovies { get { return new FileName("collected.movies.json"); } }
    public static FileName WatchedEpisodes { get { return new FileName("watched.episodes.json"); } }
    public static FileName CollectedEpisodes { get { return new FileName("collected.episodes.json"); } }
    public static FileName UserSettings { get { return new FileName("user.settings.json"); } }
  }
}