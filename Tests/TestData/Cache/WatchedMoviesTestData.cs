using System;
using System.Collections;
using System.Collections.Generic;
using TraktApiSharp.Objects.Get.Movies;
using TraktApiSharp.Objects.Get.Syncs.Activities;
using TraktApiSharp.Objects.Get.Watched;

namespace Tests.TestData.Cache
{
  public class WatchedMoviesTestData : IEnumerable<object[]>
  {
    public IEnumerator<object[]> GetEnumerator()
    {
      yield return new object[]
      {
        GetOnlineWatchedMovies_1(),
        new TraktSyncLastActivities {Movies = new TraktSyncMoviesLastActivities {WatchedAt = new DateTime(2018, 01, 01)}},
        new TraktSyncLastActivities {Movies = new TraktSyncMoviesLastActivities {WatchedAt = new DateTime(2018, 01, 01)}},
        3
      };
      yield return new object[]
      {
        GetOnlineWatchedMovies_2(),
        new TraktSyncLastActivities {Movies = new TraktSyncMoviesLastActivities {WatchedAt = new DateTime(2018, 02, 01)}},
        new TraktSyncLastActivities {Movies = new TraktSyncMoviesLastActivities {WatchedAt = new DateTime(2018, 01, 01)}},
        4
      };
    }

    private List<TraktWatchedMovie> GetOnlineWatchedMovies_1()
    {
      return new List<TraktWatchedMovie>
      {
        new TraktWatchedMovie {Movie = new TraktMovie {Ids = new TraktMovieIds {Imdb = "tt3416828"}}},
        new TraktWatchedMovie {Movie = new TraktMovie {Ids = new TraktMovieIds {Imdb = "tt2179136"}}},
        new TraktWatchedMovie {Movie = new TraktMovie {Ids = new TraktMovieIds {Imdb = "tt0050083"}}}
      };
    }

    private List<TraktWatchedMovie> GetOnlineWatchedMovies_2()
    {
      return new List<TraktWatchedMovie>
      {
        new TraktWatchedMovie {Movie = new TraktMovie {Ids = new TraktMovieIds {Imdb = "tt3416828"}}},
        new TraktWatchedMovie {Movie = new TraktMovie {Ids = new TraktMovieIds {Imdb = "tt2179136"}}},
        new TraktWatchedMovie {Movie = new TraktMovie {Ids = new TraktMovieIds {Imdb = "tt0050083"}}},
        new TraktWatchedMovie {Movie = new TraktMovie {Ids = new TraktMovieIds {Imdb = "tt2080374"}}}
      };
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }
  }
}