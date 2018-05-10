using System.Collections;
using System.Collections.Generic;
using MediaPortal.Common.MediaManagement;
using TraktPluginMP2.Structures;

namespace Tests.TestData.Setup
{
  public class TraktWatchedEpisodesTestData : IEnumerable<object[]>
  {
    public IEnumerator<object[]> GetEnumerator()
    {
      yield return new object[]
      {
        new List<MediaItem>
        {
          new MockedDatabaseEpisode("289590", 2, new List<int>{6}, 1).Episode,
          new MockedDatabaseEpisode("318493", 1, new List<int>{2}, 2).Episode,
          new MockedDatabaseEpisode("998201", 4, new List<int>{1}, 2).Episode
        },
        new List<EpisodeWatched>
        {
          new EpisodeWatched {ShowTvdbId = 289590, Season = 2, Number = 6, Plays = 1},
          new EpisodeWatched {ShowTvdbId = 318493, Season = 1, Number = 2, Plays = 3},
          new EpisodeWatched {ShowTvdbId = 998201, Season = 4, Number = 1, Plays = 1}
        },
        0
      };
      yield return new object[]
      {
        new List<MediaItem>
        {
          new MockedDatabaseEpisode("289590", 2, new List<int>{6}, 1).Episode,
          new MockedDatabaseEpisode("318493", 1, new List<int>{2}, 0).Episode,
          new MockedDatabaseEpisode("998201", 4, new List<int>{1}, 0).Episode
        },
        new List<EpisodeWatched>
        {
          new EpisodeWatched {ShowTvdbId = 998201, Season = 4, Number = 1, Plays = 1}
        },
        1
      };
      yield return new object[]
      {
        new List<MediaItem>
        {
          new MockedDatabaseEpisode("289123", 4, new List<int>{8}, 0).Episode,
          new MockedDatabaseEpisode("991493", 1, new List<int>{1}, 0).Episode,
          new MockedDatabaseEpisode("055201", 2, new List<int>{0}, 0).Episode
        },
        new List<EpisodeWatched>
        {
          new EpisodeWatched {ShowTvdbId = 289123, Season = 4, Number = 8, Plays = 1},
          new EpisodeWatched {ShowTvdbId = 991493, Season = 1, Number = 1, Plays = 3},
          new EpisodeWatched {ShowTvdbId = 055201, Season = 2, Number = 0, Plays = 1}
        },
        3
      };
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }
  }
}