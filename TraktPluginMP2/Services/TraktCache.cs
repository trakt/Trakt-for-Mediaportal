using System.Collections.Generic;
using System.IO;
using System.Linq;
using TraktAPI.DataStructures;
using TraktAPI.Extensions;

namespace TraktPluginMP2.Services
{
  public class TraktCache : ITraktCache
  {
    private IMediaPortalServices _mediaPortalServices;
    private ITraktAPI _traktApi;

    private string _cachePath;
    private string _moviesWatchedFile;

    public TraktCache(IMediaPortalServices mediaPortalServices, ITraktAPI traktApi)
    {
      _mediaPortalServices = mediaPortalServices;
      _traktApi = traktApi;
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

    private IEnumerable<TraktMovieWatched> GetWatchedMoviesFromTrakt()
    {
      throw new System.NotImplementedException();
    }

    /// <summary>
    /// returns the cached users watched movies on trakt.tv
    /// </summary>
    private IEnumerable<TraktMovieWatched> WatchedMovies
    {
      get
      {
        if (_WatchedMovies == null)
        {
          var persistedItems = LoadFileCache(_moviesWatchedFile, null);
          if (persistedItems != null)
            _WatchedMovies = persistedItems.FromJSONArray<TraktMovieWatched>();
        }
        return _WatchedMovies;
      }
    }

    private string LoadFileCache(string file, string defaultValue)
    {
      return string.Empty;
    }

    private IEnumerable<TraktMovieWatched> _WatchedMovies = null;
  }
}