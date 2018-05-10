using System;
using System.Collections.Generic;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;

namespace Tests.TestData.Setup
{
  public class MockedDatabaseMovie
  {
    public MediaItem Movie { get; }

    public MockedDatabaseMovie(string imdbId, string tmdbId, string title, int year, int playCount)
    {
      IDictionary<Guid, IList<MediaItemAspect>> movieAspects = new Dictionary<Guid, IList<MediaItemAspect>>();
      MultipleMediaItemAspect resourceAspect = new MultipleMediaItemAspect(ProviderResourceAspect.Metadata);
      resourceAspect.SetAttribute(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH, "c:\\" + title + ".mkv");
      MediaItemAspect.AddOrUpdateAspect(movieAspects, resourceAspect);
      MediaItemAspect.AddOrUpdateExternalIdentifier(movieAspects, ExternalIdentifierAspect.SOURCE_IMDB, ExternalIdentifierAspect.TYPE_MOVIE, imdbId);
      MediaItemAspect.AddOrUpdateExternalIdentifier(movieAspects, ExternalIdentifierAspect.SOURCE_TMDB, ExternalIdentifierAspect.TYPE_MOVIE, tmdbId);
      MediaItemAspect.SetAttribute(movieAspects, MovieAspect.ATTR_MOVIE_NAME, title);
      SingleMediaItemAspect smia = new SingleMediaItemAspect(MediaAspect.Metadata);
      smia.SetAttribute(MediaAspect.ATTR_PLAYCOUNT, playCount);
      smia.SetAttribute(MediaAspect.ATTR_RECORDINGTIME, new DateTime(year, 1, 1));
      MediaItemAspect.SetAspect(movieAspects, smia);

      Movie = new MediaItem(Guid.NewGuid(), movieAspects);
    }
  }
}