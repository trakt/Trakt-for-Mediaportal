using System.Collections;
using System.Collections.Generic;
using MediaPortal.Common.MediaManagement;
using TraktPluginMP2.Structures;

namespace Tests.TestData.Setup
{
  public class WatchedEpisodesTestData : IEnumerable<object[]>
  {
    public IEnumerator<object[]> GetEnumerator()
    {
      yield return new object[]
      {
        new List<MediaItem>
        {
          new MockedDatabaseEpisode("289590", 2, new List<int>{6}, 1).Episode,
          new MockedDatabaseEpisode("318493", 1, new List<int>{2}, 3).Episode,
          new MockedDatabaseEpisode("998201", 4, new List<int>{1}, 1).Episode
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
          new MockedDatabaseEpisode("318493", 1, new List<int>{2}, 3).Episode,
          new MockedDatabaseEpisode("998201", 4, new List<int>{1}, 1).Episode
        },
        new List<EpisodeWatched>(),
        3
      };
      yield return new object[]
      {
        new List<MediaItem>
        {
          new MockedDatabaseEpisode("289590", 2, new List<int>{6}, 1).Episode,
          new MockedDatabaseEpisode("318493", 1, new List<int>{2}, 3).Episode,
          new MockedDatabaseEpisode("998201", 4, new List<int>{1}, 1).Episode
        },
        new List<EpisodeWatched>
        {
          new EpisodeWatched {ShowTvdbId = 998201, Season = 4, Number = 1, Plays = 1}
        },
        2
      };
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }
  }
}