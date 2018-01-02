using System;
using System.Collections.Generic;
using System.Linq;
using MediaPortal.Common.General;
using MediaPortal.Common.Settings;
using MediaPortal.Common.Threading;
using System.Threading;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.UI.Services.UserManagement;
using TraktAPI.DataStructures;
using TraktAPI.Extensions;
using TraktPluginMP2.Services;
using TraktPluginMP2.Settings;
using TraktPluginMP2.Utilities;

namespace TraktPluginMP2.Models
{
  public class TraktSetupManager
  {
    private readonly IMediaPortalServices _mediaPortalServices;
    private readonly ITraktServices _traktServices;

    private readonly AbstractProperty _isEnabledProperty = new WProperty(typeof(bool), false);
    private readonly AbstractProperty _testStatusProperty = new WProperty(typeof(string), string.Empty);
    private readonly AbstractProperty _usermameProperty = new WProperty(typeof(string), null);
    private readonly AbstractProperty _pinCodeProperty = new WProperty(typeof(string), null);
    private readonly AbstractProperty _isSynchronizingProperty = new WProperty(typeof(bool), false);


    public TraktSetupManager(IMediaPortalServices mediaPortalServices, ITraktServices traktServices)
    {
      _traktServices = traktServices;
      _mediaPortalServices = mediaPortalServices;
    }

    public AbstractProperty IsEnabledProperty
    {
      get { return _isEnabledProperty; }
    }

    public bool IsEnabled
    {
      get { return (bool)_isEnabledProperty.GetValue(); }
      set { _isEnabledProperty.SetValue(value); }
    }

    public AbstractProperty TestStatusProperty
    {
      get { return _testStatusProperty; }
    }

    public string TestStatus
    {
      get { return (string)_testStatusProperty.GetValue(); }
      set { _testStatusProperty.SetValue(value); }
    }

    public AbstractProperty UsernameProperty
    {
      get { return _usermameProperty; }
    }

    public string Username
    {
      get { return (string)_usermameProperty.GetValue(); }
      set { _usermameProperty.SetValue(value); }
    }

    public AbstractProperty PinCodeProperty
    {
      get { return _pinCodeProperty; }
    }

    public string PinCode
    {
      get { return (string)_pinCodeProperty.GetValue(); }
      set { _pinCodeProperty.SetValue(value); }
    }

    public AbstractProperty IsSynchronizingProperty
    {
      get { return _isSynchronizingProperty; }
    }

    public bool IsSynchronizing
    {
      get { return (bool)_isSynchronizingProperty.GetValue(); }
      set { _isSynchronizingProperty.SetValue(value); }
    }

    public void AuthorizeUser()
    {
      if (string.IsNullOrEmpty(PinCode) || PinCode.Length != 8)
      {
        TestStatus = "[Trakt.WrongToken]";
        _mediaPortalServices.GetLogger().Error("Wrong pin entered");
        return;
      }

      if (!_traktServices.GetTraktLogin().Login(PinCode))
      {
        TestStatus = "[Trakt.UnableLogin]";
        return;
      }
        
      if (string.IsNullOrEmpty(Username))
      {
        TestStatus = "[Trakt.EmptyUsername]";
        _mediaPortalServices.GetLogger().Error("Username is missing");
        return;
      }

      ISettingsManager settingsManager = _mediaPortalServices.GetSettingsManager();
      TraktPluginSettings settings = settingsManager.Load<TraktPluginSettings>();

      settings.EnableTrakt = IsEnabled;
      settings.Username = Username;
      settingsManager.Save(settings);

      TestStatus = "[Trakt.LoggedIn]";
    }

    public void SyncMediaToTrakt()
    {
      if (!IsSynchronizing)
      {
        ISettingsManager settingsManager = _mediaPortalServices.GetSettingsManager();
        TraktPluginSettings settings = settingsManager.Load<TraktPluginSettings>();

        if (!settings.IsAuthorized)
        {
          TestStatus = "[Trakt.NotAuthorized]";
          _mediaPortalServices.GetLogger().Error("User not authorized");
          return;
        }

        if (!_traktServices.GetTraktCache().RefreshData())
        {
          _mediaPortalServices.GetLogger().Error("Failed to refresh data from cache");
          return;
        }

        IsSynchronizing = true;
        IThreadPool threadPool = _mediaPortalServices.GetThreadPool();
        threadPool.Add(SyncMediaToTrakt_Async, ThreadPriority.BelowNormal);
      }
    }

    public bool SyncMovies()
    {
      ISettingsManager settingsManager = _mediaPortalServices.GetSettingsManager();
      TraktPluginSettings settings = settingsManager.Load<TraktPluginSettings>();

      #region Get online data from cache

      #region Get unwatched / watched movies from trakt.tv
      IEnumerable<TraktMovieWatched> traktWatchedMovies = null;

      var traktUnWatchedMovies = _traktServices.GetTraktCache().GetUnWatchedMoviesFromTrakt();
      if (traktUnWatchedMovies == null)
      {
        _mediaPortalServices.GetLogger().Error("Error getting unwatched movies from trakt server, unwatched and watched sync will be skipped");
      }
      else
      {
        _mediaPortalServices.GetLogger().Info("There are {0} unwatched movies since the last sync with trakt.tv", traktUnWatchedMovies.Count());

        traktWatchedMovies = _traktServices.GetTraktCache().GetWatchedMoviesFromTrakt();
        if (traktWatchedMovies == null)
        {
          _mediaPortalServices.GetLogger().Error("Error getting watched movies from trakt server, watched sync will be skipped");
        }
        else
        {
          _mediaPortalServices.GetLogger().Info("There are {0} watched movies in trakt.tv library", traktWatchedMovies.Count().ToString());
        }
      }
      #endregion

      #region Get collected movies from trakt.tv
      var traktCollectedMovies = _traktServices.GetTraktCache().GetCollectedMoviesFromTrakt();
      if (traktCollectedMovies == null)
      {
        _mediaPortalServices.GetLogger().Error("Error getting collected movies from trakt server");
      }
      else
      {
        _mediaPortalServices.GetLogger().Info("There are {0} collected movies in trakt.tv library", traktCollectedMovies.Count());
      }
      #endregion

      #endregion

      try
      {
        TestStatus = "[Trakt.SyncMovies]";
        Guid[] types = { MediaAspect.ASPECT_ID, MovieAspect.ASPECT_ID, VideoAspect.ASPECT_ID, ImporterAspect.ASPECT_ID, ExternalIdentifierAspect.ASPECT_ID, ProviderResourceAspect.ASPECT_ID };
        var contentDirectory = _mediaPortalServices.GetServerConnectionManager().ContentDirectory;
        if (contentDirectory == null)
        {
          TestStatus = "[Trakt.MediaLibraryNotConnected]";
          return false;
        }

        Guid? userProfile = null;
        IUserManagement userProfileDataManagement = _mediaPortalServices.GetUserManagement();
        if (userProfileDataManagement != null && userProfileDataManagement.IsValidUser)
        {
          userProfile = userProfileDataManagement.CurrentUser.ProfileId;
        }
          
        #region Get local database info

        var collectedMovies = contentDirectory.Search(new MediaItemQuery(types, null, null), true, userProfile, false);

        _mediaPortalServices.GetLogger().Info("Found {0} movies available to sync in local database", collectedMovies.Count);

        // get the movies that we have watched
        var watchedMovies = collectedMovies.Where(IsWatched).ToList();
        _mediaPortalServices.GetLogger().Info("Found {0} watched movies available to sync in local database", watchedMovies.Count);

        #endregion

        #region Mark movies as unwatched in local database

        if (traktUnWatchedMovies != null && traktUnWatchedMovies.Count() > 0)
        {
          foreach (var movie in traktUnWatchedMovies)
          {
            var localMovie = collectedMovies.FirstOrDefault(m => MovieMatch(m, movie));
            if (localMovie == null)
            {
              continue;
            }

            _mediaPortalServices.GetLogger().Info("Marking movie as unwatched in local database, movie is not watched on trakt.tv. Title = '{0}', Year = '{1}', IMDb ID = '{2}', TMDb ID = '{3}'",
              movie.Title, movie.Year.HasValue ? movie.Year.ToString() : "<empty>", movie.Ids.Imdb ?? "<empty>", movie.Ids.Tmdb.HasValue ? movie.Ids.Tmdb.ToString() : "<empty>");

            MarkAsUnWatched(localMovie);
          }
          // update watched set
          watchedMovies = collectedMovies.Where(IsWatched).ToList();
        }

        #endregion

        #region Mark movies as watched in local database

        if (traktWatchedMovies != null && traktWatchedMovies.Count() > 0)
        {
          foreach (var twm in traktWatchedMovies)
          {
            var localMovie = collectedMovies.FirstOrDefault(m => MovieMatch(m, twm.Movie));
            if (localMovie == null)
            {
              continue;
            }

            _mediaPortalServices.GetLogger().Info("Updating local movie watched state / play count to match trakt.tv. Plays = '{0}', Title = '{1}', Year = '{2}', IMDb ID = '{3}', TMDb ID = '{4}'",
                              twm.Plays, twm.Movie.Title, twm.Movie.Year.HasValue ? twm.Movie.Year.ToString() : "<empty>", twm.Movie.Ids.Imdb ?? "<empty>", twm.Movie.Ids.Tmdb.HasValue ? twm.Movie.Ids.Tmdb.ToString() : "<empty>");

            MarkAsWatched(localMovie);
          }
        }

        #endregion

        #region Add movies to watched history at trakt.tv
        if (traktWatchedMovies != null)
        {
          var syncWatchedMovies = new List<TraktSyncMovieWatched>();
          _mediaPortalServices.GetLogger().Info("Finding movies to add to trakt.tv watched history");

          syncWatchedMovies = (from movie in watchedMovies
                               where !traktWatchedMovies.ToList().Exists(c => MovieMatch(movie, c.Movie))
                               select new TraktSyncMovieWatched
                               {
                                 Ids = new TraktMovieId { Imdb = MediaItemAspectsUtl.GetMovieImdbId(movie), Tmdb = MediaItemAspectsUtl.GetMovieTmdbId(movie) },
                                 Title = MediaItemAspectsUtl.GetMovieTitle(movie),
                                 Year = MediaItemAspectsUtl.GetMovieYear(movie),
                                 WatchedAt = MediaItemAspectsUtl.GetLastPlayedDate(movie),
                               }).ToList();

          _mediaPortalServices.GetLogger().Info("Adding {0} movies to trakt.tv watched history", syncWatchedMovies.Count);

          if (syncWatchedMovies.Count > 0)
          {
            // update internal cache
            _traktServices.GetTraktCache().AddMoviesToWatchHistory(syncWatchedMovies);

            int pageSize = settings.SyncBatchSize;
            int pages = (int)Math.Ceiling((double)syncWatchedMovies.Count / pageSize);
            for (int i = 0; i < pages; i++)
            {
              _mediaPortalServices.GetLogger().Info("Adding movies [{0}/{1}] to trakt.tv watched history", i + 1, pages);

              var pagedMovies = syncWatchedMovies.Skip(i * pageSize).Take(pageSize).ToList();

              pagedMovies.ForEach(s => _mediaPortalServices.GetLogger().Info("Adding movie to trakt.tv watched history. Title = '{0}', Year = '{1}', IMDb ID = '{2}', TMDb ID = '{3}', Date Watched = '{4}'",
                                                               s.Title, s.Year.HasValue ? s.Year.ToString() : "<empty>", s.Ids.Imdb ?? "<empty>", s.Ids.Tmdb.HasValue ? s.Ids.Tmdb.ToString() : "<empty>", s.WatchedAt));

              // remove title/year such that match against online ID only
              if (settings.SkipMoviesWithNoIdsOnSync)
              {
                pagedMovies.ForEach(m => { m.Title = null; m.Year = null; });
              }

              var response = _traktServices.GetTraktApi().AddMoviesToWatchedHistory(new TraktSyncMoviesWatched { Movies = pagedMovies });
              if (response != null)
              {
                _mediaPortalServices.GetLogger().Info("Sync Response: {0}", response.ToJSON());
              }
              

              // remove movies from cache which didn't succeed
              if (response != null && response.NotFound != null && response.NotFound.Movies.Count > 0)
              {
                _traktServices.GetTraktCache().RemoveMoviesFromWatchHistory(response.NotFound.Movies);
              }
            }
          }
        }
        #endregion

        #region Add movies to collection at trakt.tv
        if (traktCollectedMovies != null)
        {
          var syncCollectedMovies = new List<TraktSyncMovieCollected>();
          _mediaPortalServices.GetLogger().Info("Finding movies to add to trakt.tv collection");

          syncCollectedMovies = (from movie in collectedMovies
                                 where !traktCollectedMovies.ToList().Exists(c => MovieMatch(movie, c.Movie))
                                 select new TraktSyncMovieCollected
                                 {
                                   Ids = new TraktMovieId { Imdb = MediaItemAspectsUtl.GetMovieImdbId(movie), Tmdb = MediaItemAspectsUtl.GetMovieTmdbId(movie) },
                                   Title = MediaItemAspectsUtl.GetMovieTitle(movie),
                                   Year = MediaItemAspectsUtl.GetMovieYear(movie),
                                   CollectedAt = MediaItemAspectsUtl.GetDateAddedToDb(movie),
                                   MediaType = MediaItemAspectsUtl.GetVideoMediaType(movie),
                                   Resolution = MediaItemAspectsUtl.GetVideoResolution(movie),
                                   AudioCodec = MediaItemAspectsUtl.GetVideoAudioCodec(movie),
                                   AudioChannels = "",
                                   Is3D = false
                                 }).ToList();

          _mediaPortalServices.GetLogger().Info("Adding {0} movies to trakt.tv collection", syncCollectedMovies.Count);


          if (syncCollectedMovies.Count > 0)
          {
            //update internal cache
            _traktServices.GetTraktCache().AddMoviesToCollection(syncCollectedMovies);
            int pageSize = settings.SyncBatchSize;
            int pages = (int)Math.Ceiling((double)syncCollectedMovies.Count / pageSize);
            for (int i = 0; i < pages; i++)
            {
              _mediaPortalServices.GetLogger().Info("Adding movies [{0}/{1}] to trakt.tv collection", i + 1, pages);

              var pagedMovies = syncCollectedMovies.Skip(i * pageSize).Take(pageSize).ToList();

              pagedMovies.ForEach(s => _mediaPortalServices.GetLogger().Info("Adding movie to trakt.tv collection. Title = '{0}', Year = '{1}', IMDb ID = '{2}', TMDb ID = '{3}', Date Added = '{4}', MediaType = '{5}', Resolution = '{6}', Audio Codec = '{7}', Audio Channels = '{8}'",
                                               s.Title, s.Year.HasValue ? s.Year.ToString() : "<empty>", s.Ids.Imdb ?? "<empty>", s.Ids.Tmdb.HasValue ? s.Ids.Tmdb.ToString() : "<empty>",
                                              s.CollectedAt, s.MediaType ?? "<empty>", s.Resolution ?? "<empty>", s.AudioCodec ?? "<empty>", s.AudioChannels ?? "<empty>"));

              //// remove title/year such that match against online ID only
              if (settings.SkipMoviesWithNoIdsOnSync)
              {
                pagedMovies.ForEach(m =>
                {
                  m.Title = null;
                  m.Year = null;
                });
              }

              var response = _traktServices.GetTraktApi().AddMoviesToCollecton(new TraktSyncMoviesCollected { Movies = pagedMovies });
              if (response != null)
              {
                _mediaPortalServices.GetLogger().Info("Sync Response: {0}", response.ToJSON());
              }

              // remove movies from cache which didn't succeed
              if (response != null && response.NotFound != null && response.NotFound.Movies.Count > 0)
              {
                _traktServices.GetTraktCache().RemoveMoviesFromCollection(response.NotFound.Movies);
              }
            }
          }
        }
        #endregion
        return true;
      }
      catch (Exception ex)
      {
        _mediaPortalServices.GetLogger().Error("Exception while synchronizing media library.", ex);
      }
      return false;
    }

    public bool SyncSeries()
    {
      return true;
    }

    private void SyncMediaToTrakt_Async()
    {
      if (SyncMovies() && SyncSeries())
      {
        TestStatus = "[Trakt.SyncFinished]";
      }
      IsSynchronizing = false;
      _traktServices.GetTraktCache().Save();
    }

    private static bool IsWatched(MediaItem mediaItem)
    {
      int playCount;
      return (MediaItemAspect.TryGetAttribute(mediaItem.Aspects, MediaAspect.ATTR_PLAYCOUNT, 0, out playCount) && playCount > 0);
    }

    /// <summary>
    /// Checks if a local movie is the same as an online movie
    /// </summary>
    private bool MovieMatch(MediaItem localMovie, TraktMovie traktMovie)
    {
      // IMDb comparison
      if (!string.IsNullOrEmpty(traktMovie.Ids.Imdb) && !string.IsNullOrEmpty(MediaItemAspectsUtl.GetMovieImdbId(localMovie)))
      {
        return String.Compare(MediaItemAspectsUtl.GetMovieImdbId(localMovie), traktMovie.Ids.Imdb, StringComparison.OrdinalIgnoreCase) == 0;
      }

      // TMDb comparison
      if ((MediaItemAspectsUtl.GetMovieTmdbId(localMovie) != 0) && traktMovie.Ids.Tmdb.HasValue)
      {
        return MediaItemAspectsUtl.GetMovieTmdbId(localMovie) == traktMovie.Ids.Tmdb.Value;
      }

      // Title & Year comparison
      {
        return string.Compare(MediaItemAspectsUtl.GetMovieTitle(localMovie), traktMovie.Title, true) == 0 && (MediaItemAspectsUtl.GetMovieYear(localMovie) == traktMovie.Year);
      }
    }

    private void MarkAsUnWatched(MediaItem mediaItem)
    {
      //SetUnwatched setUnwatchedAction = new SetUnwatched();
      //if (setUnwatchedAction.IsAvailable(mediaItem))
      //{
      //  try
      //  {
      //    ContentDirectoryMessaging.MediaItemChangeType changeType;
      //    if (setUnwatchedAction.Process(mediaItem, out changeType) && changeType != ContentDirectoryMessaging.MediaItemChangeType.None)
      //    {
      //      ContentDirectoryMessaging.SendMediaItemChangedMessage(mediaItem, changeType);
      //      _mediaPortalServices.GetLogger().Info("Marking media item '{0}' as unwatched", mediaItem.GetType());
      //    }
      //  }
      //  catch (Exception ex)
      //  {
      //    _mediaPortalServices.GetLogger().Error("Marking media item '{0}' as unwatched failed:", mediaItem.GetType(), ex);
      //  }
      //}
    }

    private void MarkAsWatched(MediaItem mediaItem)
    {
      //SetWatched setWatchedAction = new SetWatched();
      //if (setWatchedAction.IsAvailable(mediaItem))
      //{
      //  try
      //  {
      //    ContentDirectoryMessaging.MediaItemChangeType changeType;
      //    if (setWatchedAction.Process(mediaItem, out changeType) && changeType != ContentDirectoryMessaging.MediaItemChangeType.None)
      //    {
      //      ContentDirectoryMessaging.SendMediaItemChangedMessage(mediaItem, changeType);
      //      TraktLogger.Info("Marking media item '{0}' as watched", mediaItem.GetType());
      //    }
      //  }
      //  catch (Exception ex)
      //  {
      //    TraktLogger.Error("Marking media item '{0}' as watched failed:", mediaItem.GetType(), ex);
      //  }
      //}
    }
  }
}