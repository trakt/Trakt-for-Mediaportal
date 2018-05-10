using System.Collections;
using System.Collections.Generic;
using MediaPortal.Common.MediaManagement;
using TraktApiSharp.Objects.Get.Movies;

namespace Tests.TestData.Setup
{
  public class TraktUnwatchedMoviesTestData : IEnumerable<object[]>
  {
    public IEnumerator<object[]> GetEnumerator()
    {
      yield return new object[]
      {
        new List<MediaItem>
        {
          new MockedDatabaseMovie("tt12345", "11290", "Movie_1", 2012, 1).Movie,
          new MockedDatabaseMovie("", "67890", "Movie_2", 2016, 1).Movie,
          new MockedDatabaseMovie("", "0", "Movie_3", 2010, 1).Movie
        },
        new List<TraktMovie>
        {
          new TraktMovie {Ids = new TraktMovieIds {Imdb = "tt12345", Tmdb = 11290 }, Title = "Movie_1", Year = 2012},
          new TraktMovie {Ids = new TraktMovieIds {Imdb = "tt11390", Tmdb = 67890 }, Title = "Movie_2", Year = 2016},
          new TraktMovie {Ids = new TraktMovieIds {Imdb = "tt99821", Tmdb = 31139 }, Title = "Movie_3", Year = 2010}
        },
        3
      };
      yield return new object[]
      {
        new List<MediaItem>
        {
          new MockedDatabaseMovie("tt12345", "11290", "Movie_1", 2012, 1).Movie,
          new MockedDatabaseMovie("", "67890", "Movie_2", 2016, 1).Movie,
          new MockedDatabaseMovie("", "0", "Movie_3", 2010, 1).Movie
        },
        new List<TraktMovie>
        {
          new TraktMovie {Ids = new TraktMovieIds {Imdb = "tt12345", Tmdb = 11290 }, Title = "Movie_1", Year = 2012},
        },
        1
      };
      yield return new object[]
      {
        new List<MediaItem>
        {
          new MockedDatabaseMovie("tt12128", "11290", "Movie_1", 2012, 2).Movie,
          new MockedDatabaseMovie("", "12390", "Movie_2", 2016, 2).Movie,
          new MockedDatabaseMovie("", "0", "Movie_4", 2011, 1).Movie
        },
        new List<TraktMovie>
        {
          new TraktMovie {Ids = new TraktMovieIds {Imdb = "tt12345", Tmdb = 11290 }, Title = "Movie_1", Year = 2012},
          new TraktMovie {Ids = new TraktMovieIds {Imdb = "tt67804", Tmdb = 67890 }, Title = "Movie_2", Year = 2016},
          new TraktMovie {Ids = new TraktMovieIds {Imdb = "tt03412", Tmdb = 34251 }, Title = "Movie_3", Year = 2010}
        },
        0
      };
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }
  }
}