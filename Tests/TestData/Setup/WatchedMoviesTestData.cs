using System.Collections;
using System.Collections.Generic;
using MediaPortal.Common.MediaManagement;
using TraktApiSharp.Objects.Get.Movies;
using TraktApiSharp.Objects.Get.Watched;

namespace Tests.TestData.Setup
{
  public class WatchedMoviesTestData : IEnumerable<object[]>
  {
    public IEnumerator<object[]> GetEnumerator()
    {
      yield return new object[]
      {
        new List<MediaItem>
        {
          new MockedDatabaseMovie("tt12345", "67890", "Movie_1", 2012, 1).Movie,
          new MockedDatabaseMovie("", "16729", "Movie_2", 2016, 2).Movie,
          new MockedDatabaseMovie("", "0", "Movie_3", 2011, 3).Movie
        },
        new List<TraktWatchedMovie>
        {
          new TraktWatchedMovie {Movie = new TraktMovie {Ids = new TraktMovieIds {Imdb = "tt12345", Tmdb = 67890 }, Title = "Movie_1", Year = 2012}},
          new TraktWatchedMovie {Movie = new TraktMovie {Ids = new TraktMovieIds {Imdb = "tt67804", Tmdb = 16729 }, Title = "Movie_2", Year = 2016}},
          new TraktWatchedMovie {Movie = new TraktMovie {Ids = new TraktMovieIds {Imdb = "tt03412", Tmdb = 34251 }, Title = "Movie_3", Year = 2011}}
        },
        0
      };
      yield return new object[]
      {
        new List<MediaItem>
        {
          new MockedDatabaseMovie("tt12345", "67890", "Movie_1", 2012, 1).Movie,
          new MockedDatabaseMovie("", "16729", "Movie_2", 2016, 2).Movie,
          new MockedDatabaseMovie("", "0", "Movie_3", 2011, 3).Movie
        },
        new List<TraktWatchedMovie>
        {
          new TraktWatchedMovie {Movie = new TraktMovie {Ids = new TraktMovieIds {Imdb = "tt12345", Tmdb = 67890 }, Title = "Movie_1", Year = 2012}},
          new TraktWatchedMovie {Movie = new TraktMovie {Ids = new TraktMovieIds {Imdb = "tt67804", Tmdb = 16729 }, Title = "Movie_2", Year = 2016}},
        },
        1
      };
      yield return new object[]
      {
        new List<MediaItem>
        {
          new MockedDatabaseMovie("tt12345", "67890", "Movie_1", 2012, 1).Movie,
          new MockedDatabaseMovie("", "67890", "Movie_2", 2016, 2).Movie,
          new MockedDatabaseMovie("", "0", "Movie_3", 2010, 3).Movie
        },
        new List<TraktWatchedMovie>(),
        3
      };
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }
  }
}