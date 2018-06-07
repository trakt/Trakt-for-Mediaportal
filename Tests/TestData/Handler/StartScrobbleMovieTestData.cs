using System.Collections;
using System.Collections.Generic;
using NSubstitute;
using Tests.TestData.Setup;
using TraktApiSharp.Authentication;
using TraktApiSharp.Objects.Get.Movies;
using TraktApiSharp.Objects.Post.Scrobbles.Responses;
using TraktPluginMP2.Services;
using TraktPluginMP2.Settings;

namespace Tests.TestData.Handler
{
  public class StartScrobbleMovieTestData : IEnumerable<object[]>
  {
    public IEnumerator<object[]> GetEnumerator()
    {
      yield return new object[]
      {
        new TraktPluginSettings
        {
          EnableScrobble = true,
          UserAuthorized = true
        },
        new MockedDatabaseMovie("tt12345", "67890", "Movie1", 2012, 0).Movie,
        GetMockedTraktClientWithValidAuthorization(),
        "Movie1"
      };
    }

    private ITraktClient GetMockedTraktClientWithValidAuthorization()
    {
      ITraktClient traktClient = Substitute.For<ITraktClient>();
      traktClient.TraktAuthorization.Returns(new TraktAuthorization
      {
        RefreshToken = "ValidToken",
        AccessToken = "ValidToken"
      });

      traktClient.RefreshAuthorization(Arg.Any<string>()).Returns(new TraktAuthorization
      {
        RefreshToken = "ValidToken"
      });
      traktClient.StartScrobbleMovie(Arg.Any<TraktMovie>(), Arg.Any<float>()).Returns(
        new TraktMovieScrobblePostResponse
        {
          Movie = new TraktMovie
          {
            Ids = new TraktMovieIds { Imdb = "tt1431045", Tmdb = 67890 },
            Title = "Movie1",
            Year = 2016,
          }
        });

      return traktClient;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }
  }
}