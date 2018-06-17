using System.Collections;
using System.Collections.Generic;
using NSubstitute;
using Tests.TestData.Setup;
using TraktApiSharp.Authentication;
using TraktApiSharp.Objects.Get.Shows;
using TraktApiSharp.Objects.Get.Shows.Episodes;
using TraktApiSharp.Objects.Post.Scrobbles.Responses;
using TraktPluginMP2.Notifications;
using TraktPluginMP2.Services;
using TraktPluginMP2.Settings;

namespace Tests.TestData.Handler
{
  public class StopScrobbleSeriesTestData : IEnumerable<object[]>
  {
    public IEnumerator<object[]> GetEnumerator()
    {
      const string title = "Title_1";
      yield return new object[]
      {
        new TraktPluginSettings
        {
          IsScrobbleEnabled = true,
          ShowScrobbleStoppedNotifications = true
        },
        new MockedDatabaseEpisode("289590", 2, new List<int> {6}, 1).Episode,
        GetMockedTraktClientWithValidAuthorization(),
        new TraktScrobbleStoppedNotification(title, true), 
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
      traktClient.StartScrobbleEpisode(Arg.Any<TraktEpisode>(), Arg.Any<TraktShow>(), Arg.Any<float>()).Returns(
        new TraktEpisodeScrobblePostResponse
        {
          Episode = new TraktEpisode
          {
            Ids = new TraktEpisodeIds { Imdb = "tt12345", Tvdb = 289590 },
            Number = 2,
            Title = "Title_1",
            SeasonNumber = 2
          }
        });

      traktClient.StopScrobbleEpisode(Arg.Any<TraktEpisode>(), Arg.Any<TraktShow>(), Arg.Any<float>()).Returns(
        new TraktEpisodeScrobblePostResponse
        {
          Episode = new TraktEpisode
          {
            Ids = new TraktEpisodeIds { Imdb = "tt12345", Tvdb = 289590 },
            Number = 2,
            Title = "Title_1",
            SeasonNumber = 2
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