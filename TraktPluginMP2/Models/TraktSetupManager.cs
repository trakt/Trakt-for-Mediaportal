using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using MediaPortal.Common.General;
using MediaPortal.Common.Threading;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Common.SystemCommunication;
using MediaPortal.Common.UserManagement;
using Newtonsoft.Json;
using TraktApiSharp.Authentication;
using TraktApiSharp.Objects.Basic;
using TraktApiSharp.Objects.Get.Collection;
using TraktApiSharp.Objects.Get.Movies;
using TraktApiSharp.Objects.Get.Shows;
using TraktApiSharp.Objects.Get.Syncs.Activities;
using TraktApiSharp.Objects.Get.Users;
using TraktApiSharp.Objects.Get.Watched;
using TraktApiSharp.Objects.Post;
using TraktApiSharp.Objects.Post.Syncs.Collection;
using TraktApiSharp.Objects.Post.Syncs.Collection.Responses;
using TraktApiSharp.Objects.Post.Syncs.History;
using TraktApiSharp.Objects.Post.Syncs.History.Responses;
using TraktPluginMP2.Services;
using TraktPluginMP2.Structures;
using TraktPluginMP2.Utilities;

namespace TraktPluginMP2.Models
{
  public class TraktSetupManager
  {
    private readonly IMediaPortalServices _mediaPortalServices;
    private readonly ITraktClient _traktClient;
    private readonly ITraktCache _traktCache;
    private readonly IFileOperations _fileOperations;

    private readonly AbstractProperty _isEnabledProperty = new WProperty(typeof(bool), false);
    private readonly AbstractProperty _testStatusProperty = new WProperty(typeof(string), string.Empty);
    private readonly AbstractProperty _pinCodeProperty = new WProperty(typeof(string), null);
    private readonly AbstractProperty _isSynchronizingProperty = new WProperty(typeof(bool), false);

    public TraktSetupManager(IMediaPortalServices mediaPortalServices, ITraktClient traktClient, ITraktCache traktCache, IFileOperations fileOperations)
    {
      _mediaPortalServices = mediaPortalServices;
      _traktClient = traktClient;
      _traktCache = traktCache;
      _fileOperations = fileOperations;
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

    public void Initialize()
    {
      // clear the PIN code text box, necessary when entering the plugin
      PinCode = string.Empty;

      string authFilePath = Path.Combine(_mediaPortalServices.GetTraktUserHomePath(), FileName.Authorization.Value);
      bool savedAuthFileExists = _fileOperations.FileExists(authFilePath);
      if (!savedAuthFileExists)
      {
        TestStatus = "[Trakt.NotAuthorized]";
        // set sync button to false
      }
      else
      {
        string savedAuthorization = _fileOperations.FileReadAllText(authFilePath);
        TraktAuthorization savedAuthFile = JsonConvert.DeserializeObject<TraktAuthorization>(savedAuthorization);
        if (savedAuthFile.IsRefreshPossible)
        {
          TestStatus = "[Trakt.AlreadyAuthorized]";
          
          // set sync button to true
        }
        else
        {
          TestStatus = "[Trakt.SavedAuthIsNotValid]";
          
          // set sync button to false
        }
      }
    }

    public void AuthorizeUser()
    {
      try
      {
        TraktAuthorization authorization = _traktClient.GetAuthorization(PinCode);
        TraktUserSettings traktUserSettings = _traktClient.GetTraktUserSettings();
        TraktSyncLastActivities traktSyncLastActivities = _traktClient.GetLastActivities();

        string traktUserHomePath = _mediaPortalServices.GetTraktUserHomePath();
        if (!Directory.Exists(traktUserHomePath))
        {
          Directory.CreateDirectory(traktUserHomePath);
        }

        SaveTraktAuthorization(authorization, traktUserHomePath);
        SaveTraktUserSettings(traktUserSettings, traktUserHomePath);
        SaveLastSyncActivities(traktSyncLastActivities, traktUserHomePath);

        TestStatus = "[Trakt.AuthorizationSucceed]";
      }
      catch (Exception ex)
      {
        TestStatus = "[Trakt.AuthorizationFailed]";
        _mediaPortalServices.GetLogger().Error(ex);
      }
    }

    public void SyncMediaToTrakt()
    {
      if (!IsSynchronizing)
      {
        try
        {
          IsSynchronizing = true;
          IThreadPool threadPool = _mediaPortalServices.GetThreadPool();
          threadPool.Add(() =>
          {
            SyncMovies();
            SyncSeries();
            IsSynchronizing = false;
          },ThreadPriority.BelowNormal);
        }
        catch (Exception ex)
        {
          TestStatus = "[Trakt.SyncingFailed]";
          _mediaPortalServices.GetLogger().Error(ex.Message);
        }
      }
    }

    public TraktSyncMoviesResult SyncMovies()
    {
      _mediaPortalServices.GetLogger().Info("Trakt: start sync movies");

      ValidateAuthorization();

      TraktSyncMoviesResult syncMoviesResult = new TraktSyncMoviesResult();
      IList<TraktMovie> traktUnWatchedMovies = _traktCache.GetUnWatchedMovies().ToList();
      IList<TraktWatchedMovie> traktWatchedMovies = _traktCache.GetWatchedMovies().ToList();
      IList<TraktCollectionMovie> traktCollectedMovies = _traktCache.GetCollectedMovies().ToList();

      TestStatus = "[Trakt.SyncMovies]";
      Guid[] types =
      {
        MediaAspect.ASPECT_ID, MovieAspect.ASPECT_ID, VideoAspect.ASPECT_ID, ImporterAspect.ASPECT_ID,
        ExternalIdentifierAspect.ASPECT_ID, ProviderResourceAspect.ASPECT_ID
      };

      IContentDirectory contentDirectory = _mediaPortalServices.GetServerConnectionManager().ContentDirectory;
      if (contentDirectory == null)
      {
        TestStatus = "[Trakt.MediaLibraryNotConnected]";
        throw new Exception("Media library not connected.");
      }

      Guid? userProfile = null;
      IUserManagement userProfileDataManagement = _mediaPortalServices.GetUserManagement();
      if (userProfileDataManagement != null && userProfileDataManagement.IsValidUser)
      {
        userProfile = userProfileDataManagement.CurrentUser.ProfileId;
      }

      #region Get local database info

      IList<MediaItem> collectedMovies = contentDirectory.SearchAsync(new MediaItemQuery(types, null, null), true, userProfile, false).Result;

      if (collectedMovies.Any())
      {
        syncMoviesResult.CollectedInLibrary = collectedMovies.Count;
        _mediaPortalServices.GetLogger().Info("Trakt: found {0} collected movies available to sync in media library", collectedMovies.Count);
      }
      
      List<MediaItem> watchedMovies = collectedMovies.Where(MediaItemAspectsUtl.IsWatched).ToList();

      if (watchedMovies.Any())
      {
        syncMoviesResult.WatchedInLibrary = watchedMovies.Count;
        _mediaPortalServices.GetLogger().Info("Trakt: found {0} watched movies available to sync in media library", watchedMovies.Count);
      }

      #endregion

      #region Mark movies as unwatched in local database

      _mediaPortalServices.GetLogger().Info("Trakt: start marking movies as unwatched in media library");
      if (traktUnWatchedMovies.Any())
      {
        foreach (var movie in traktUnWatchedMovies)
        {
          var localMovie = collectedMovies.FirstOrDefault(m => MovieMatch(m, movie));
          if (localMovie == null)
          {
            continue;
          }

          _mediaPortalServices.GetLogger().Info(
            "Marking movie as unwatched in library, movie is not watched on trakt. Title = '{0}', Year = '{1}', IMDb ID = '{2}', TMDb ID = '{3}'",
            movie.Title, movie.Year.HasValue ? movie.Year.ToString() : "<empty>", movie.Ids.Imdb ?? "<empty>",
            movie.Ids.Tmdb.HasValue ? movie.Ids.Tmdb.ToString() : "<empty>");

          if (_mediaPortalServices.MarkAsUnWatched(localMovie).Result)
          {
            syncMoviesResult.MarkedAsUnWatchedInLibrary++;
          }
        }

        // update watched set
        watchedMovies = collectedMovies.Where(MediaItemAspectsUtl.IsWatched).ToList();
      }

      #endregion

      #region Mark movies as watched in local database

      _mediaPortalServices.GetLogger().Info("Trakt: start marking movies as watched in media library");
      if (traktWatchedMovies.Any())
      {
        foreach (var twm in traktWatchedMovies)
        {
          var localMovie = collectedMovies.FirstOrDefault(m => MovieMatch(m, twm.Movie));
          if (localMovie == null)
          {
            continue;
          }

          _mediaPortalServices.GetLogger().Info(
            "Marking movie as watched in library, movie is watched on trakt. Plays = '{0}', Title = '{1}', Year = '{2}', IMDb ID = '{3}', TMDb ID = '{4}'",
            twm.Plays, twm.Movie.Title, twm.Movie.Year.HasValue ? twm.Movie.Year.ToString() : "<empty>",
            twm.Movie.Ids.Imdb ?? "<empty>", twm.Movie.Ids.Tmdb.HasValue ? twm.Movie.Ids.Tmdb.ToString() : "<empty>");

          if (_mediaPortalServices.MarkAsWatched(localMovie).Result)
          {
            syncMoviesResult.MarkedAsWatchedInLibrary++;
          }
        }
      }

      #endregion

      #region Add movies to watched history at trakt.tv

      _mediaPortalServices.GetLogger().Info("Trakt: finding movies to add to watched history");

      List<TraktSyncHistoryPostMovie> syncWatchedMovies = (from movie in watchedMovies
        where !traktWatchedMovies.ToList().Exists(c => MovieMatch(movie, c.Movie))
        select new TraktSyncHistoryPostMovie
        {
          Ids = new TraktMovieIds
          {
            Imdb = MediaItemAspectsUtl.GetMovieImdbId(movie),
            Tmdb = MediaItemAspectsUtl.GetMovieTmdbId(movie)
          },
          Title = MediaItemAspectsUtl.GetMovieTitle(movie),
          Year = MediaItemAspectsUtl.GetMovieYear(movie),
          WatchedAt = MediaItemAspectsUtl.GetLastPlayedDate(movie),
        }).ToList();

      if (syncWatchedMovies.Any())
      {
        _mediaPortalServices.GetLogger().Info("Trakt: trying to add {0} watched movies to trakt watched history", syncWatchedMovies.Count);

        TraktSyncHistoryPostResponse watchedResponse = _traktClient.AddWatchedHistoryItems(new TraktSyncHistoryPost { Movies = syncWatchedMovies });
        syncMoviesResult.AddedToTraktWatchedHistory = watchedResponse.Added?.Movies;

        if (watchedResponse.Added?.Movies != null)
        {
          _mediaPortalServices.GetLogger().Info("Trakt: successfully added {0} watched movies to trakt watched history", watchedResponse.Added.Movies.Value);
        }
      }

      #endregion

      #region Add movies to collection at trakt.tv

      _mediaPortalServices.GetLogger().Info("Trakt: finding movies to add to collection");

      List<TraktSyncCollectionPostMovie> syncCollectedMovies = (from movie in collectedMovies
        where !traktCollectedMovies.ToList().Exists(c => MovieMatch(movie, c.Movie))
        select new TraktSyncCollectionPostMovie
        {
          Metadata = new TraktMetadata
          {
            MediaType = MediaItemAspectsUtl.GetVideoMediaType(movie),
            MediaResolution = MediaItemAspectsUtl.GetVideoResolution(movie),
            Audio = MediaItemAspectsUtl.GetVideoAudioCodec(movie),
            AudioChannels = MediaItemAspectsUtl.GetVideoAudioChannel(movie),
            ThreeDimensional = false
          },
          Ids = new TraktMovieIds
          {
            Imdb = MediaItemAspectsUtl.GetMovieImdbId(movie),
            Tmdb = MediaItemAspectsUtl.GetMovieTmdbId(movie)
          },
          Title = MediaItemAspectsUtl.GetMovieTitle(movie),
          Year = MediaItemAspectsUtl.GetMovieYear(movie),
          CollectedAt = MediaItemAspectsUtl.GetDateAddedToDb(movie)
        }).ToList();

      if (syncCollectedMovies.Any())
      {
        _mediaPortalServices.GetLogger().Info("Trakt: trying to add {0} collected movies to trakt collection", syncCollectedMovies.Count);

        TraktSyncCollectionPostResponse collectionResponse = _traktClient.AddCollectionItems(new TraktSyncCollectionPost { Movies = syncCollectedMovies });
        syncMoviesResult.AddedToTraktCollection = collectionResponse.Added?.Movies;

        if (collectionResponse.Added?.Movies != null)
        {
          _mediaPortalServices.GetLogger().Info("Trakt: successfully added {0} collected movies to trakt collection", collectionResponse.Added.Movies.Value);
        }
      }
      #endregion

      return syncMoviesResult;
    }

    public TraktSyncEpisodesResult SyncSeries()
    {
      _mediaPortalServices.GetLogger().Info("Trakt: start sync series");

      ValidateAuthorization();

      TraktSyncEpisodesResult syncEpisodesResult = new TraktSyncEpisodesResult();
      IList<Episode> traktUnWatchedEpisodes = _traktCache.GetUnWatchedEpisodes().ToList();
      IList<EpisodeWatched> traktWatchedEpisodes = _traktCache.GetWatchedEpisodes().ToList();
      IList<EpisodeCollected> traktCollectedEpisodes = _traktCache.GetCollectedEpisodes().ToList();

      TestStatus = "[Trakt.SyncSeries]";
      Guid[] types =
      {
        MediaAspect.ASPECT_ID, EpisodeAspect.ASPECT_ID, VideoAspect.ASPECT_ID, ImporterAspect.ASPECT_ID,
        ProviderResourceAspect.ASPECT_ID, ExternalIdentifierAspect.ASPECT_ID
      };
      var contentDirectory = _mediaPortalServices.GetServerConnectionManager().ContentDirectory;
      if (contentDirectory == null)
      {
        TestStatus = "[Trakt.MediaLibraryNotConnected]";
        throw new Exception("Media library not connected.");
      }

      Guid? userProfile = null;
      IUserManagement userProfileDataManagement = _mediaPortalServices.GetUserManagement();
      if (userProfileDataManagement != null && userProfileDataManagement.IsValidUser)
      {
        userProfile = userProfileDataManagement.CurrentUser.ProfileId;
      }

      #region Get data from local database

      IList<MediaItem> localEpisodes = contentDirectory.SearchAsync(new MediaItemQuery(types, null, null), true, userProfile, false).Result;

      if (localEpisodes.Any())
      {
        syncEpisodesResult.CollectedInLibrary = localEpisodes.Count;
        _mediaPortalServices.GetLogger().Info("Trakt: found {0} total episodes in library", localEpisodes.Count);
      }

      List<MediaItem> localWatchedEpisodes = localEpisodes.Where(MediaItemAspectsUtl.IsWatched).ToList();

      if (localWatchedEpisodes.Any())
      {
        _mediaPortalServices.GetLogger().Info("Trakt: found {0} episodes watched in library", localWatchedEpisodes.Count);
      }


      #endregion

      #region Mark episodes as unwatched in local database

      _mediaPortalServices.GetLogger().Info("Trakt: start marking series episodes as unwatched in media library");
      if (traktUnWatchedEpisodes.Any())
      {
        // create a unique key to lookup and search for faster
        ILookup<string, MediaItem> localLookupEpisodes = localWatchedEpisodes.ToLookup(twe => CreateLookupKey(twe), twe => twe);

        foreach (var episode in traktUnWatchedEpisodes)
        {
          string tvdbKey = CreateLookupKey(episode);

          var watchedEpisode = localLookupEpisodes[tvdbKey].FirstOrDefault();
          if (watchedEpisode != null)
          {
            _mediaPortalServices.GetLogger().Info(
              "Marking episode as unwatched in library, episode is not watched on trakt. Title = '{0}', Year = '{1}', Season = '{2}', Episode = '{3}', Show TVDb ID = '{4}', Show IMDb ID = '{5}'",
              episode.ShowTitle, episode.ShowYear.HasValue ? episode.ShowYear.ToString() : "<empty>", episode.Season,
              episode.Number, episode.ShowTvdbId.HasValue ? episode.ShowTvdbId.ToString() : "<empty>",
              episode.ShowImdbId ?? "<empty>");

            if (_mediaPortalServices.MarkAsUnWatched(watchedEpisode).Result)
            {
              syncEpisodesResult.MarkedAsUnWatchedInLibrary++;
            }

            // update watched episodes
            localWatchedEpisodes.Remove(watchedEpisode);
          }
        }
      }

      #endregion

      #region Mark episodes as watched in local database

      _mediaPortalServices.GetLogger().Info("Trakt: start marking series episodes as watched in media library");
      if (traktWatchedEpisodes.Any())
      {
        // create a unique key to lookup and search for faster
        ILookup<string, EpisodeWatched> onlineEpisodes = traktWatchedEpisodes.ToLookup(twe => CreateLookupKey(twe), twe => twe);
        List<MediaItem> localUnWatchedEpisodes = localEpisodes.Except(localWatchedEpisodes).ToList();
        foreach (var episode in localUnWatchedEpisodes)
        {
          string tvdbKey = CreateLookupKey(episode);

          var traktEpisode = onlineEpisodes[tvdbKey].FirstOrDefault();
          if (traktEpisode != null)
          {
            _mediaPortalServices.GetLogger().Info(
              "Marking episode as watched in library, episode is watched on trakt. Plays = '{0}', Title = '{1}', Year = '{2}', Season = '{3}', Episode = '{4}', Show TVDb ID = '{5}', Show IMDb ID = '{6}', Last Watched = '{7}'",
              traktEpisode.Plays, traktEpisode.ShowTitle,
              traktEpisode.ShowYear.HasValue ? traktEpisode.ShowYear.ToString() : "<empty>", traktEpisode.Season,
              traktEpisode.Number, traktEpisode.ShowTvdbId.HasValue ? traktEpisode.ShowTvdbId.ToString() : "<empty>",
              traktEpisode.ShowImdbId ?? "<empty>", traktEpisode.WatchedAt);

            if (_mediaPortalServices.MarkAsWatched(episode).Result)
            {
              syncEpisodesResult.MarkedAsWatchedInLibrary++;
            }
          }
        }
      }

      #endregion

      #region Add episodes to watched history at trakt.tv

      TraktSyncHistoryPost syncHistoryPost = GetWatchedShowsForSync(localWatchedEpisodes, traktWatchedEpisodes);
      if (syncHistoryPost.Shows != null && syncHistoryPost.Shows.Any())
      {
        _mediaPortalServices.GetLogger().Info("Trakt: trying to add {0} watched episodes to trakt watched history", syncHistoryPost.Shows.Count());
        TraktSyncHistoryPostResponse response = _traktClient.AddWatchedHistoryItems(syncHistoryPost);
        syncEpisodesResult.AddedToTraktWatchedHistory = response.Added?.Episodes;

        if (response.Added?.Episodes != null)
        {
          _mediaPortalServices.GetLogger().Info("Trakt: successfully added {0} watched episodes to trakt watched history", response.Added.Episodes.Value);
        }
      }
     
      #endregion

      #region Add episodes to collection at trakt.tv

      TraktSyncCollectionPost syncCollectionPost = GetCollectedShowsForSync(localEpisodes, traktCollectedEpisodes);
      if (syncCollectionPost.Shows != null && syncCollectionPost.Shows.Any())
      {
        _mediaPortalServices.GetLogger().Info("Trakt: trying to add {0} collected episodes to trakt collection", syncCollectionPost.Shows.Count());
        TraktSyncCollectionPostResponse response = _traktClient.AddCollectionItems(syncCollectionPost);
        syncEpisodesResult.AddedToTraktCollection = response.Added?.Episodes;

        if (response.Added?.Episodes != null)
        {
          _mediaPortalServices.GetLogger().Info("Trakt: successfully added {0} collected episodes to trakt collection", response.Added.Episodes.Value);
        }
      }

      #endregion

      return syncEpisodesResult;
    }

    private void ValidateAuthorization()
    {
      if (!_traktClient.TraktAuthorization.IsValid)
      {
        string authFilePath = Path.Combine(_mediaPortalServices.GetTraktUserHomePath(), FileName.Authorization.Value);
        string savedAuthorization = _fileOperations.FileReadAllText(authFilePath);
        TraktAuthorization savedAuth = JsonConvert.DeserializeObject<TraktAuthorization>(savedAuthorization);

        if (!savedAuth.IsRefreshPossible)
        {
          throw new Exception("Saved authorization is not valid.");
        }

        TraktAuthorization refreshedAuth = _traktClient.RefreshAuthorization(savedAuth.RefreshToken);
        string serializedAuth = JsonConvert.SerializeObject(refreshedAuth);
        _fileOperations.FileWriteAllText(authFilePath, serializedAuth, Encoding.UTF8);
      }
    }

    private void SaveTraktAuthorization(TraktAuthorization authorization, string path)
    {
      string serializedAuthorization = JsonConvert.SerializeObject(authorization);
      string authorizationFilePath = Path.Combine(path, FileName.Authorization.Value);
      _fileOperations.FileWriteAllText(authorizationFilePath, serializedAuthorization, Encoding.UTF8);
    }

    private void SaveTraktUserSettings(TraktUserSettings traktUserSettings, string path)
    {
      string serializedSettings = JsonConvert.SerializeObject(traktUserSettings);
      string settingsFilePath = Path.Combine(path, FileName.UserSettings.Value);
      _fileOperations.FileWriteAllText(settingsFilePath, serializedSettings, Encoding.UTF8);
    }

    private void SaveLastSyncActivities(TraktSyncLastActivities traktSyncLastActivities, string path)
    {
      string serializedSyncActivities = JsonConvert.SerializeObject(traktSyncLastActivities, Formatting.Indented);
      string syncActivitiesFilePath = Path.Combine(path, FileName.LastActivity.Value);
      _fileOperations.FileWriteAllText(syncActivitiesFilePath, serializedSyncActivities, Encoding.UTF8);
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
      return String.Compare(MediaItemAspectsUtl.GetMovieTitle(localMovie), traktMovie.Title, StringComparison.OrdinalIgnoreCase) == 0 && (MediaItemAspectsUtl.GetMovieYear(localMovie) == traktMovie.Year);
    }

    private string CreateLookupKey(MediaItem episode)
    {
      var tvdid = MediaItemAspectsUtl.GetTvdbId(episode);
      var seasonIndex = MediaItemAspectsUtl.GetSeasonIndex(episode);
      var episodeIndex = MediaItemAspectsUtl.GetEpisodeIndex(episode);

      return string.Format("{0}_{1}_{2}", tvdid, seasonIndex, episodeIndex);
    }

    private string CreateLookupKey(Episode episode)
    {
      string show;

      if (episode.ShowTvdbId != null)
      {
        show = episode.ShowTvdbId.Value.ToString();
      }
      else if (episode.ShowImdbId != null)
      {
        show = episode.ShowImdbId;
      }
      else
      {
        if (episode.ShowTitle == null)
          return episode.GetHashCode().ToString();

        show = episode.ShowTitle + "_" + episode.ShowYear ?? string.Empty;
      }

      return string.Format("{0}_{1}_{2}", show, episode.Season, episode.Number);
    }

    private TraktSyncHistoryPost GetWatchedShowsForSync(IList<MediaItem> localWatchedEpisodes, IEnumerable<EpisodeWatched> traktEpisodesWatched)
    {
      _mediaPortalServices.GetLogger().Info("Trakt: finding local shows to add to trakt watched history");
      TraktSyncHistoryPostBuilder builder = new TraktSyncHistoryPostBuilder();
      ILookup<string, EpisodeWatched> onlineEpisodes = traktEpisodesWatched.ToLookup(twe => CreateLookupKey(twe), twe => twe);

      foreach (var episode in localWatchedEpisodes)
      {
        string tvdbKey = CreateLookupKey(episode);
        EpisodeWatched traktEpisode = onlineEpisodes[tvdbKey].FirstOrDefault();

        if (traktEpisode == null)
        {
          TraktShow show = new TraktShow
          {
            Title = MediaItemAspectsUtl.GetSeriesTitle(episode),
            Ids = new TraktShowIds
            {
              Tvdb = MediaItemAspectsUtl.GetTvdbId(episode),
              Imdb = MediaItemAspectsUtl.GetSeriesImdbId(episode)
            }
          };

          DateTime watchedAt = MediaItemAspectsUtl.GetLastPlayedDate(episode);

          builder.AddShow(show, watchedAt, new PostHistorySeasons
          {
            {
              MediaItemAspectsUtl.GetSeasonIndex(episode),
              new PostHistoryEpisodes {MediaItemAspectsUtl.GetEpisodeIndex(episode)}
            }
          });
        }
      }
      return builder.Build();
    }

    private TraktSyncCollectionPost GetCollectedShowsForSync(IList<MediaItem> localCollectedEpisodes, IEnumerable<EpisodeCollected> traktEpisodesCollected)
    {
      _mediaPortalServices.GetLogger().Info("Trakt: finding local episodes to add to trakt collection");
      TraktSyncCollectionPostBuilder builder = new TraktSyncCollectionPostBuilder();
      ILookup<string, EpisodeCollected> onlineEpisodes = traktEpisodesCollected.ToLookup(tce => CreateLookupKey(tce), tce => tce);

      foreach (var episode in localCollectedEpisodes)
      {
        string tvdbKey = CreateLookupKey(episode);
        EpisodeCollected traktEpisode = onlineEpisodes[tvdbKey].FirstOrDefault();

        if (traktEpisode == null)
        {
          TraktShow show = new TraktShow
          {
            Title = MediaItemAspectsUtl.GetSeriesTitle(episode),
            Ids = new TraktShowIds
            {
              Tvdb = MediaItemAspectsUtl.GetTvdbId(episode),
              Imdb = MediaItemAspectsUtl.GetSeriesImdbId(episode)
            }
          };

          TraktMetadata metadata = new TraktMetadata
          {
            Audio = MediaItemAspectsUtl.GetVideoAudioCodec(episode),
            AudioChannels = MediaItemAspectsUtl.GetVideoAudioChannel(episode),
            MediaResolution = MediaItemAspectsUtl.GetVideoResolution(episode),
            MediaType = MediaItemAspectsUtl.GetVideoMediaType(episode),
            ThreeDimensional = false
          };

          DateTime collectedAt = MediaItemAspectsUtl.GetDateAddedToDb(episode);

          builder.AddShow(show, metadata, collectedAt,
            new PostSeasons
            {
              {
                MediaItemAspectsUtl.GetSeasonIndex(episode),
                new PostEpisodes {MediaItemAspectsUtl.GetEpisodeIndex(episode)}
              }
            });
        }
      }
      return builder.Build();
    }
  }
}