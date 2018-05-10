using System;
using System.Collections.Generic;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;

namespace Tests.TestData.Setup
{
  public class MockedDatabaseEpisode
  {
    public MediaItem Episode { get; }

    public MockedDatabaseEpisode(string tvDbId, int seasonIndex, List<int> episodeIndex, int playCount)
    {
      IDictionary<Guid, IList<MediaItemAspect>> episodeAspects = new Dictionary<Guid, IList<MediaItemAspect>>();
      MultipleMediaItemAspect resourceAspect = new MultipleMediaItemAspect(ProviderResourceAspect.Metadata);
      resourceAspect.SetAttribute(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH, "c:\\" + tvDbId + ".mkv");
      MediaItemAspect.AddOrUpdateAspect(episodeAspects, resourceAspect);
      MediaItemAspect.AddOrUpdateExternalIdentifier(episodeAspects, ExternalIdentifierAspect.SOURCE_TVDB, ExternalIdentifierAspect.TYPE_SERIES, tvDbId);
      MediaItemAspect.SetAttribute(episodeAspects, EpisodeAspect.ATTR_SEASON, seasonIndex);
      MediaItemAspect.SetCollectionAttribute(episodeAspects, EpisodeAspect.ATTR_EPISODE, episodeIndex);
      SingleMediaItemAspect smia = new SingleMediaItemAspect(MediaAspect.Metadata);
      smia.SetAttribute(MediaAspect.ATTR_PLAYCOUNT, playCount);
      MediaItemAspect.SetAspect(episodeAspects, smia);

      Episode = new MediaItem(Guid.NewGuid(), episodeAspects);
    }
  }
}