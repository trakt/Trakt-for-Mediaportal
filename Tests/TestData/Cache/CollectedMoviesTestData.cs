using System;
using System.Collections;
using System.Collections.Generic;
using TraktApiSharp.Objects.Get.Collection;
using TraktApiSharp.Objects.Get.Movies;
using TraktApiSharp.Objects.Get.Syncs.Activities;

namespace Tests.TestData.Cache
{
  public class CollectedMoviesTestData : IEnumerable<object[]>
  {
    public IEnumerator<object[]> GetEnumerator()
    {
      yield return new object[]
      {
        GetOnlineCollectedMovies_1(),
        new TraktSyncLastActivities {Movies = new TraktSyncMoviesLastActivities {CollectedAt = new DateTime(2018,05,21,20,00,00)}},
        4
      };
      yield return new object[]
      {
        GetOnlineCollectedMovies_2(),
        new TraktSyncLastActivities {Movies = new TraktSyncMoviesLastActivities {CollectedAt = new DateTime(2018,05,20,17,00,00)}},
        3
      };
    }

    private List<TraktCollectionMovie> GetOnlineCollectedMovies_1()
    {
      return new List<TraktCollectionMovie>
      {
        new TraktCollectionMovie {Movie = new TraktMovie {Ids = new TraktMovieIds {Imdb = "tt0050083"}}},
        new TraktCollectionMovie {Movie = new TraktMovie {Ids = new TraktMovieIds {Imdb = "tt2179136"}}},
        new TraktCollectionMovie {Movie = new TraktMovie {Ids = new TraktMovieIds {Imdb = "tt3416828"}}},
        new TraktCollectionMovie {Movie = new TraktMovie {Ids = new TraktMovieIds {Imdb = "tt2080374"}}}
      };
    }

    private List<TraktCollectionMovie> GetOnlineCollectedMovies_2()
    {
      return new List<TraktCollectionMovie>
      {
        new TraktCollectionMovie {Movie = new TraktMovie {Ids = new TraktMovieIds {Imdb = "tt0050083"}}},
        new TraktCollectionMovie {Movie = new TraktMovie {Ids = new TraktMovieIds {Imdb = "tt2179136"}}},
        new TraktCollectionMovie {Movie = new TraktMovie {Ids = new TraktMovieIds {Imdb = "tt3416828"}}}
      };
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }
  }
}