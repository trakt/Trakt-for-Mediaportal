namespace TraktPluginMP2
{
  public class TraktSyncMoviesResult
  {
    public int WatchedInLibrary { get; set; }

    public int CollectedInLibrary { get; set; }

    public int? AddedToTraktWatchedHistory { get; set; }

    public int? AddedToTraktCollection { get; set; }

    public int MarkedAsUnWatchedInLibrary { get; set; }

    public int MarkedAsWatchedInLibrary { get; set; }
  }
}