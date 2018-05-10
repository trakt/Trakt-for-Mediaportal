using System;
using System.Collections;
using System.Collections.Generic;
using TraktApiSharp.Objects.Get.Collection;
using TraktApiSharp.Objects.Get.Shows;
using TraktApiSharp.Objects.Get.Syncs.Activities;

namespace Tests.TestData.Cache
{
  public class CollectedEpisodesTestData : IEnumerable<object[]>
  {
    public IEnumerator<object[]> GetEnumerator()
    {
      yield return new object[]
      {
        GetOnlineCollectedEpisodes_1(),
        new TraktSyncLastActivities {Episodes = new TraktSyncEpisodesLastActivities {CollectedAt = new DateTime(2018, 01, 01)}},
        new TraktSyncLastActivities {Episodes = new TraktSyncEpisodesLastActivities {CollectedAt = new DateTime(2018, 01, 01)}},
        3
      };
      yield return new object[]
      {
        GetOnlineCollectedEpisodes_2(),
        new TraktSyncLastActivities {Episodes = new TraktSyncEpisodesLastActivities {CollectedAt = new DateTime(2018, 02, 01)}},
        new TraktSyncLastActivities {Episodes = new TraktSyncEpisodesLastActivities {CollectedAt = new DateTime(2018, 01, 01)}},
        6
      };
    }

    private List<TraktCollectionShow> GetOnlineCollectedEpisodes_1()
    {
      return new List<TraktCollectionShow>
      {
        new TraktCollectionShow
        {
          Show = new TraktShow
          {
            Ids = new TraktShowIds {Tvdb = 80379, Imdb = "tt0898266"}
          },
          Seasons = new List<TraktCollectionShowSeason>
          {
            new TraktCollectionShowSeason
            {
              Number = 9,
              Episodes = new List<TraktCollectionShowEpisode>
              {
                new TraktCollectionShowEpisode {Number = 1}
              }
            }
          }
        },
        new TraktCollectionShow
        {
          Show = new TraktShow
          {
            Ids = new TraktShowIds {Tvdb = 248682, Imdb = "tt1826940"}
          },
          Seasons = new List<TraktCollectionShowSeason>
          {
            new TraktCollectionShowSeason
            {
              Number = 1,
              Episodes = new List<TraktCollectionShowEpisode>
              {
                new TraktCollectionShowEpisode {Number = 3}
              }
            }
          }
        },
        new TraktCollectionShow
        {
          Show = new TraktShow
          {
            Ids = new TraktShowIds {Tvdb = 298901, Imdb = "tt4635276"}
          },
          Seasons = new List<TraktCollectionShowSeason>
          {
            new TraktCollectionShowSeason
            {
              Number = 1,
              Episodes = new List<TraktCollectionShowEpisode>
              {
                new TraktCollectionShowEpisode {Number = 1}
              }
            }
          }
        }
      };
    }

    private List<TraktCollectionShow> GetOnlineCollectedEpisodes_2()
    {
      return new List<TraktCollectionShow>
      {
        new TraktCollectionShow
        {
          Show = new TraktShow
          {
            Ids = new TraktShowIds {Tvdb = 80379, Imdb = "tt0898266"}
          },
          Seasons = new List<TraktCollectionShowSeason>
          {
            new TraktCollectionShowSeason
            {
              Number = 9,
              Episodes = new List<TraktCollectionShowEpisode>
              {
                new TraktCollectionShowEpisode {Number = 1},
                new TraktCollectionShowEpisode {Number = 2}
              }
            }
          }
        },
        new TraktCollectionShow
        {
          Show = new TraktShow
          {
            Ids = new TraktShowIds {Tvdb = 248682, Imdb = "tt1826940"}
          },
          Seasons = new List<TraktCollectionShowSeason>
          {
            new TraktCollectionShowSeason
            {
              Number = 1,
              Episodes = new List<TraktCollectionShowEpisode>
              {
                new TraktCollectionShowEpisode {Number = 3}
              }
            }
          }
        },
        new TraktCollectionShow
        {
          Show = new TraktShow
          {
            Ids = new TraktShowIds {Tvdb = 298901, Imdb = "tt4635276"}
          },
          Seasons = new List<TraktCollectionShowSeason>
          {
            new TraktCollectionShowSeason
            {
              Number = 1,
              Episodes = new List<TraktCollectionShowEpisode>
              {
                new TraktCollectionShowEpisode {Number = 1},
                new TraktCollectionShowEpisode {Number = 2},
                new TraktCollectionShowEpisode {Number = 3}
              }
            }
          }
        }
      };
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }
  }
}