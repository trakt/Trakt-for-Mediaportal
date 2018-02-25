using System.Collections.Generic;
using System.IO;
using System.Linq;
using TraktApiSharp.Objects.Get.Collection;
using TraktApiSharp.Objects.Get.Movies;
using TraktApiSharp.Objects.Get.Syncs.Activities;
using TraktApiSharp.Objects.Get.Watched;
using TraktApiSharp.Objects.Post.Syncs.Collection;
using TraktApiSharp.Objects.Post.Syncs.History;
using TraktApiSharp.Objects.Post.Syncs.Responses;
using TraktPluginMP2.Structures;

namespace TraktPluginMP2.Services
{
  public class TraktCache : ITraktCache
  {
    private IMediaPortalServices _mediaPortalServices;
    private ITraktClient _traktClient;

    private string _cachePath;
    private string _moviesWatchedFile;

    public TraktCache(IMediaPortalServices mediaPortalServices, ITraktClient traktClient)
    {
      _mediaPortalServices = mediaPortalServices;
      _traktClient = traktClient;
      _cachePath = _mediaPortalServices.GetPathManager().GetPath(@"<DATA>\Trakt\");
      _moviesWatchedFile = Path.Combine(_cachePath, @"{username}\Library\Movies\Watched.json");
    }
    private IEnumerable<TraktMovie> UnWatchedMovies { get; set; }

    public bool RefreshData()
    {
      return true;
    }

    public void Save()
    {
      
    }

    public IEnumerable<TraktMovie> GetUnWatchedMoviesFromTrakt()
    {
      if (UnWatchedMovies != null)
        return UnWatchedMovies;

      _mediaPortalServices.GetLogger().Info("Getting current user unwatched movies from trakt");

      // trakt.tv does not provide an unwatched API
      // There are plans after initial launch of v2 to have a re-watch API.

      // First we need to get the previously cached watched movies
      var previouslyWatched = WatchedMovies;
      if (previouslyWatched == null)
        return new List<TraktMovie>();

      // now get the latest watched
      var currentWatched = GetWatchedMoviesFromTrakt();
      if (currentWatched == null)
        return null;

      _mediaPortalServices.GetLogger().Debug("Comparing previous watched movies against current watched movies such that unwatched can be determined");

      // anything not in the currentwatched that is previously watched
      // must be unwatched now.
      var unwatchedMovies = from pw in previouslyWatched
        where !currentWatched.Any(m => (m.Movie.Ids.Trakt == pw.Movie.Ids.Trakt || m.Movie.Ids.Imdb == pw.Movie.Ids.Imdb))
        select new TraktMovie
        {
          Ids = pw.Movie.Ids,
          Title = pw.Movie.Title,
          Year = pw.Movie.Year
        };

      UnWatchedMovies = unwatchedMovies ?? new List<TraktMovie>();

      return UnWatchedMovies;
    }

    public IEnumerable<TraktWatchedMovie> GetWatchedMoviesFromTrakt()
    {
      throw new System.NotImplementedException();
    }

    public IEnumerable<TraktCollectionMovie> GetCollectedMoviesFromTrakt()
    {
      throw new System.NotImplementedException();
    }

    public void AddMoviesToWatchHistory(List<TraktSyncHistoryPostMovie> movies)
    {
      throw new System.NotImplementedException();
    }

    public void RemoveMoviesFromWatchHistory(IEnumerable<TraktSyncPostResponseNotFoundItem<TraktMovieIds>> movies)
    {
      throw new System.NotImplementedException();
    }

    public void AddMoviesToCollection(List<TraktSyncCollectionPostMovie> movies)
    {
      throw new System.NotImplementedException();
    }

    public void RemoveMoviesFromCollection(IEnumerable<TraktSyncPostResponseNotFoundItem<TraktMovieIds>> movies)
    {
      throw new System.NotImplementedException();
    }

    public IEnumerable<Episode> GetUnWatchedEpisodesFromTrakt()
    {
      throw new System.NotImplementedException();
    }

    public IEnumerable<EpisodeWatched> GetWatchedEpisodesFromTrakt(bool ignoreLastSyncTime = false)
    {
      throw new System.NotImplementedException();
    }

    public IEnumerable<EpisodeCollected> GetCollectedEpisodesFromTrakt(bool ignoreLastSyncTime = false)
    {
      throw new System.NotImplementedException();
    }

    public void AddEpisodesToWatchHistory(TraktSyncHistoryPostShow show)
    {
      throw new System.NotImplementedException();
    }

    public void AddEpisodesToCollection(TraktSyncCollectionPostShow show)
    {
      throw new System.NotImplementedException();
    }

    /// <summary>
    /// returns the cached users watched movies on trakt.tv
    /// </summary>
    private IEnumerable<TraktWatchedMovie> WatchedMovies
    {
      get
      {
        if (_WatchedMovies == null)
        {
          var persistedItems = LoadFileCache(_moviesWatchedFile, null);
         // if (persistedItems != null)
         //   _WatchedMovies = persistedItems.FromJSONArray<TraktMovieWatched>();
        }
        return _WatchedMovies;
      }
    }

    private string LoadFileCache(string file, string defaultValue)
    {
      return string.Empty;
    }

    private IEnumerable<TraktWatchedMovie> _WatchedMovies = null;
  }
}