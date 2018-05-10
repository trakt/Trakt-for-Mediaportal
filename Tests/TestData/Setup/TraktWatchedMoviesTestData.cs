using System.Collections;
using System.Collections.Generic;
using MediaPortal.Common.MediaManagement;
using TraktApiSharp.Objects.Get.Movies;
using TraktApiSharp.Objects.Get.Watched;

namespace Tests.TestData.Setup
{
  public class TraktWatchedMoviesTestData : IEnumerable<object[]>
  {
    public IEnumerator<object[]> GetEnumerator()
    {
      yield return new object[]
      {
        new List<MediaItem>
        {
          new MockedDatabaseMovie("tt1450", "67890", "Movie_1", 2012, 1).Movie,
          new MockedDatabaseMovie("", "50123", "Movie_2", 2016, 1).Movie,
          new MockedDatabaseMovie("", "0", "Movie_4", 2014, 1).Movie
        },
        new List<TraktWatchedMovie>
        {
          new TraktWatchedMovie {Movie = new TraktMovie {Ids = new TraktMovieIds {Imdb = "tt12345", Tmdb = 67890 }, Title = "Movie_1", Year = 2012}},
          new TraktWatchedMovie {Movie = new TraktMovie {Ids = new TraktMovieIds {Imdb = "tt67804", Tmdb = 67890 }, Title = "Movie_2", Year = 2016}},
          new TraktWatchedMovie {Movie = new TraktMovie {Ids = new TraktMovieIds {Imdb = "tt03412", Tmdb = 34251 }, Title = "Movie_3", Year = 2010}}
        },
        0
      };
      yield return new object[]
      {
        new List<MediaItem>
        {
          new MockedDatabaseMovie("tt12345", "67890", "Movie_1", 2012, 0).Movie,
          new MockedDatabaseMovie("", "67890", "Movie_2", 2016, 0).Movie,
          new MockedDatabaseMovie("", "0", "Movie_3", 2010, 0).Movie
        },
        new List<TraktWatchedMovie>
        {
          new TraktWatchedMovie {Movie = new TraktMovie {Ids = new TraktMovieIds {Imdb = "tt12345", Tmdb = 67890 }, Title = "Movie_1", Year = 2012}},
          new TraktWatchedMovie {Movie = new TraktMovie {Ids = new TraktMovieIds {Imdb = "tt67804", Tmdb = 67890 }, Title = "Movie_2", Year = 2016}},
          new TraktWatchedMovie {Movie = new TraktMovie {Ids = new TraktMovieIds {Imdb = "tt03412", Tmdb = 34251 }, Title = "Movie_3", Year = 2010}}
        },
        3
      };
      yield return new object[]
      {
        new List<MediaItem>
        {
          new MockedDatabaseMovie("tt12345", "67890", "Movie_1", 2012, 0).Movie,
          new MockedDatabaseMovie("", "67890", "Movie_2", 2016, 0).Movie,
          new MockedDatabaseMovie("", "0", "Movie_3", 2010, 0).Movie
        },
        new List<TraktWatchedMovie>
        {
          new TraktWatchedMovie {Movie = new TraktMovie {Ids = new TraktMovieIds {Imdb = "tt03412", Tmdb = 34251 }, Title = "Movie_3", Year = 2010}}
        },
        1
      };
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }
  }
}