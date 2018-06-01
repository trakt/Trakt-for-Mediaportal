using System;
using System.Collections;
using System.Collections.Generic;
using TraktApiSharp.Objects.Get.Shows;
using TraktApiSharp.Objects.Get.Syncs.Activities;
using TraktApiSharp.Objects.Get.Watched;

namespace Tests.TestData.Cache
{
  public class UnWatchedEpisodesTestData : IEnumerable<object[]>
  {
    public IEnumerator<object[]> GetEnumerator()
    {
      yield return new object[]
      {
        GetOnlineWatchedEpisodes_1(),
        new TraktSyncLastActivities {Episodes = new TraktSyncEpisodesLastActivities {WatchedAt = new DateTime(2018,04,20,20,00,00)}},
        0
      };
      yield return new object[]
      {
        GetOnlineWatchedEpisodes_2(),
        new TraktSyncLastActivities {Episodes = new TraktSyncEpisodesLastActivities {WatchedAt = new DateTime(2018,04,24,20,00,00)}},
        1
      };
    }

    private List<TraktWatchedShow> GetOnlineWatchedEpisodes_1()
    {
      return new List<TraktWatchedShow>
      {
        new TraktWatchedShow
        {
          Show = new TraktShow
          {
            Ids = new TraktShowIds {Tvdb = 80379, Imdb = "tt0898266"}
          },
          Seasons = new List<TraktWatchedShowSeason>
          {
            new TraktWatchedShowSeason
            {
              Number = 9,
              Episodes = new List<TraktWatchedShowEpisode>
              {
                new TraktWatchedShowEpisode {Number = 1},
                new TraktWatchedShowEpisode {Number = 2}
              }
            }
          }
        },
        new TraktWatchedShow
        {
          Show = new TraktShow
          {
            Ids = new TraktShowIds {Tvdb = 298901, Imdb = "tt4635276"}
          },
          Seasons = new List<TraktWatchedShowSeason>
          {
            new TraktWatchedShowSeason
            {
              Number = 1,
              Episodes = new List<TraktWatchedShowEpisode>
              {
                new TraktWatchedShowEpisode {Number = 1}
              }
            }
          }
        },
        new TraktWatchedShow
        {
          Show = new TraktShow
          {
            Ids = new TraktShowIds {Tvdb = 248682, Imdb = "tt1826940"}
          },
          Seasons = new List<TraktWatchedShowSeason>
          {
            new TraktWatchedShowSeason
            {
              Number = 1,
              Episodes = new List<TraktWatchedShowEpisode>
              {
                new TraktWatchedShowEpisode {Number = 1}
              }
            }
          }
        }
      };
    }

    private List<TraktWatchedShow> GetOnlineWatchedEpisodes_2()
    {
      return new List<TraktWatchedShow>
      {
        new TraktWatchedShow
        {
          Show = new TraktShow
          {
            Ids = new TraktShowIds {Tvdb = 80379, Imdb = "tt0898266"}
          },
          Seasons = new List<TraktWatchedShowSeason>
          {
            new TraktWatchedShowSeason
            {
              Number = 9,
              Episodes = new List<TraktWatchedShowEpisode>
              {
                new TraktWatchedShowEpisode {Number = 1}
              }
            }
          }
        },
        new TraktWatchedShow
        {
          Show = new TraktShow
          {
            Ids = new TraktShowIds {Tvdb = 298901, Imdb = "tt4635276"}
          },
          Seasons = new List<TraktWatchedShowSeason>
          {
            new TraktWatchedShowSeason
            {
              Number = 1,
              Episodes = new List<TraktWatchedShowEpisode>
              {
                new TraktWatchedShowEpisode {Number = 1}
              }
            }
          }
        },
        new TraktWatchedShow
        {
          Show = new TraktShow
          {
            Ids = new TraktShowIds {Tvdb = 248682, Imdb = "tt1826940"}
          },
          Seasons = new List<TraktWatchedShowSeason>
          {
            new TraktWatchedShowSeason
            {
              Number = 1,
              Episodes = new List<TraktWatchedShowEpisode>
              {
                new TraktWatchedShowEpisode {Number = 1}
              }
            }
          }
        },
      };
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }
  }
}