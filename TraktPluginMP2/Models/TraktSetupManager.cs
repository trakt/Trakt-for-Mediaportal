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
using MediaPortal.Common.UserManagement;
using Newtonsoft.Json;
using TraktApiSharp.Authentication;
using TraktApiSharp.Objects.Basic;
using TraktApiSharp.Objects.Get.Movies;
using TraktApiSharp.Objects.Get.Shows.Episodes;
using TraktApiSharp.Objects.Get.Syncs.Activities;
using TraktApiSharp.Objects.Get.Users;
using TraktApiSharp.Objects.Get.Watched;
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

    public int SyncWatchedMovies { get; private set; }

    public int SyncCollectedMovies { get; private set; }

    public int MarkWatchedMovies { get; private set; }

    public int MarkUnWatchedMovies { get; private set; }

    public int SyncWatchedEpisodes { get; private set; }

    public int SyncCollectedEpisodes { get; private set; }

    public int MarkWatchedEpisodes { get; private set; }

    public int MarkUnWatchedEpisodes { get; private set; }

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
      string serializedSyncActivities = JsonConvert.SerializeObject(traktSyncLastActivities,Formatting.Indented);
      string syncActivitiesFilePath = Path.Combine(path, FileName.LastActivity.Value);
      _fileOperations.FileWriteAllText(syncActivitiesFilePath, serializedSyncActivities, Encoding.UTF8);
    }

    public void SyncMediaToTrakt()
    {
      if (!IsSynchronizing)
      {
        string authFilePath = Path.Combine(_mediaPortalServices.GetTraktUserHomePath(), FileName.Authorization.Value);
        string savedAuthorization = _fileOperations.FileReadAllText(authFilePath);
        TraktAuthorization savedAuth = JsonConvert.DeserializeObject<TraktAuthorization>(savedAuthorization);
        try
        {
          if (!_traktClient.TraktAuthorization.IsValid)
          {
            TraktAuthorization refreshedAuth = _traktClient.RefreshAuthorization(savedAuth.RefreshToken);

            string serializedAuth = JsonConvert.SerializeObject(refreshedAuth);
            _fileOperations.FileWriteAllText(authFilePath, serializedAuth, Encoding.UTF8);
          }
          IsSynchronizing = true;
          IThreadPool threadPool = _mediaPortalServices.GetThreadPool();
          threadPool.Add(SyncMediaToTrakt_Async, ThreadPriority.BelowNormal);
        }
        catch (Exception ex)
        {
          TestStatus = "[Trakt.SyncingFailed]";
          _mediaPortalServices.GetLogger().Error(ex.Message);
        }
      }
    }

    public bool SyncMovies()
    {
      #region Get online data from cache

      #region Get unwatched / watched movies from trakt.tv
      IEnumerable<TraktWatchedMovie> traktWatchedMovies = null;

      var traktUnWatchedMovies = _traktCache.GetUnWatchedMovies();
      if (traktUnWatchedMovies == null)
      {
        _mediaPortalServices.GetLogger().Error("Error getting unwatched movies from trakt server, unwatched and watched sync will be skipped");
      }
      else
      {
        _mediaPortalServices.GetLogger().Info("There are {0} unwatched movies since the last sync with trakt.tv", traktUnWatchedMovies.Count());

        traktWatchedMovies = _traktCache.GetWatchedMovies();
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
      var traktCollectedMovies = _traktCache.GetCollectedMovies();
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

      TestStatus = "[Trakt.SyncMovies]";
      Guid[] types =
      {
        MediaAspect.ASPECT_ID, MovieAspect.ASPECT_ID, VideoAspect.ASPECT_ID, ImporterAspect.ASPECT_ID,
        ExternalIdentifierAspect.ASPECT_ID, ProviderResourceAspect.ASPECT_ID
      };
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

      var collectedMovies = contentDirectory.SearchAsync(new MediaItemQuery(types, null, null), true, userProfile, false).Result;

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

          _mediaPortalServices.GetLogger().Info(
            "Marking movie as unwatched in local database, movie is not watched on trakt.tv. Title = '{0}', Year = '{1}', IMDb ID = '{2}', TMDb ID = '{3}'",
            movie.Title, movie.Year.HasValue ? movie.Year.ToString() : "<empty>", movie.Ids.Imdb ?? "<empty>",
            movie.Ids.Tmdb.HasValue ? movie.Ids.Tmdb.ToString() : "<empty>");

          if (_mediaPortalServices.MarkAsUnWatched(localMovie).Result)
          {
            MarkUnWatchedMovies++;
          }
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

          _mediaPortalServices.GetLogger().Info(
            "Updating local movie watched state / play count to match trakt.tv. Plays = '{0}', Title = '{1}', Year = '{2}', IMDb ID = '{3}', TMDb ID = '{4}'",
            twm.Plays, twm.Movie.Title, twm.Movie.Year.HasValue ? twm.Movie.Year.ToString() : "<empty>",
            twm.Movie.Ids.Imdb ?? "<empty>", twm.Movie.Ids.Tmdb.HasValue ? twm.Movie.Ids.Tmdb.ToString() : "<empty>");

          if (_mediaPortalServices.MarkAsWatched(localMovie).Result)
          {
            MarkWatchedMovies++;
          }
        }
      }

      #endregion

      #region Add movies to watched history at trakt.tv

      if (traktWatchedMovies != null)
      {
        _mediaPortalServices.GetLogger().Info("Finding movies to add to trakt.tv watched history");

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

        _mediaPortalServices.GetLogger().Info("Adding {0} movies to trakt.tv watched history", syncWatchedMovies.Count);
        SyncWatchedMovies = syncWatchedMovies.Count;

        if (SyncWatchedMovies > 0)
        {
          TraktSyncHistoryPostResponse watchedResponse = _traktClient.AddWatchedHistoryItems(new TraktSyncHistoryPost {Movies = syncWatchedMovies});

          if (SyncWatchedMovies != watchedResponse?.Added?.Movies)
          {
            // log not found episodes
          }
        }
      }

      #endregion

      #region Add movies to collection at trakt.tv

      if (traktCollectedMovies != null)
      {
        _mediaPortalServices.GetLogger().Info("Finding movies to add to trakt.tv collection");

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

        _mediaPortalServices.GetLogger().Info("Adding {0} movies to trakt.tv collection", syncCollectedMovies.Count);
        SyncCollectedMovies = syncCollectedMovies.Count;

        if (SyncCollectedMovies > 0)
        {
          TraktSyncCollectionPostResponse collectionResponse =
            _traktClient.AddCollectionItems(new TraktSyncCollectionPost {Movies = syncCollectedMovies});

          if (SyncCollectedMovies != collectionResponse?.Added?.Movies)
          {
            // log not found episodes
          }
        }

        return true;
      }

      #endregion

      return false;

    }

    public bool SyncSeries()
    {
      _mediaPortalServices.GetLogger().Info("Series Library Starting Sync");

      #region Get online data from cache

      #region UnWatched / Watched
      
      List<EpisodeWatched> traktWatchedEpisodes = null;

      // get all episodes on trakt that are marked as 'unseen'
      var traktUnWatchedEpisodes = _traktCache.GetUnWatchedEpisodes().ToNullableList();
      if (traktUnWatchedEpisodes == null)
      {
        _mediaPortalServices.GetLogger().Error("Error getting tv shows unwatched from trakt.tv server, unwatched and watched sync will be skipped");
      }
      else
      {
        _mediaPortalServices.GetLogger().Info("Found {0} unwatched tv episodes in trakt.tv library", traktUnWatchedEpisodes.Count());

        // now get all episodes on trakt that are marked as 'seen' or 'watched' (this will be cached already when working out unwatched)
        traktWatchedEpisodes = _traktCache.GetWatchedEpisodes().ToNullableList();
        if (traktWatchedEpisodes == null)
        {
          _mediaPortalServices.GetLogger().Error("Error getting tv shows watched from trakt.tv server, watched sync will be skipped");
        }
        else
        {
          _mediaPortalServices.GetLogger().Info("Found {0} watched tv episodes in trakt.tv library", traktWatchedEpisodes.Count());
        }
      }

      #endregion

      #region Collection

      // get all episodes on trakt that are marked as in 'collection'
      var traktCollectedEpisodes = _traktCache.GetCollectedEpisodes().ToNullableList();
      if (traktCollectedEpisodes == null)
      {
        _mediaPortalServices.GetLogger().Error("Error getting tv episode collection from trakt.tv server");
      }
      else
      {
        _mediaPortalServices.GetLogger().Info("Found {0} tv episodes in trakt.tv collection", traktCollectedEpisodes.Count());
      }

      #endregion


      #endregion

      if (traktCollectedEpisodes != null)
      {
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
          return false;
        }

        Guid? userProfile = null;
        IUserManagement userProfileDataManagement = _mediaPortalServices.GetUserManagement();
        if (userProfileDataManagement != null && userProfileDataManagement.IsValidUser)
        {
          userProfile = userProfileDataManagement.CurrentUser.ProfileId;
        }

        #region Get data from local database

        var localEpisodes = contentDirectory.SearchAsync(new MediaItemQuery(types, null, null), true, userProfile, false).Result;
        int episodeCount = localEpisodes.Count;

        _mediaPortalServices.GetLogger().Info("Found {0} total episodes in local database", episodeCount);

        // get the episodes that we have watched
        var localWatchedEpisodes = localEpisodes.Where(IsWatched).ToList();
        var localUnWatchedEpisodes = localEpisodes.Except(localWatchedEpisodes).ToList();

        _mediaPortalServices.GetLogger().Info("Found {0} episodes watched in tvseries database", localWatchedEpisodes.Count);

        #endregion

        #region Mark episodes as unwatched in local database

        _mediaPortalServices.GetLogger().Info("Start sync of tv episode unwatched state to local database");
        if (traktUnWatchedEpisodes != null && traktUnWatchedEpisodes.Count() > 0)
        {
          // create a unique key to lookup and search for faster
          var localLookupEpisodes = localWatchedEpisodes.ToLookup(twe => CreateLookupKey(twe), twe => twe);

          foreach (var episode in traktUnWatchedEpisodes)
          {
            string tvdbKey = CreateLookupKey(episode);

            var watchedEpisode = localLookupEpisodes[tvdbKey].FirstOrDefault();
            if (watchedEpisode != null)
            {
              _mediaPortalServices.GetLogger().Info(
                "Marking episode as unwatched in local database, episode is not watched on trakt.tv. Title = '{0}', Year = '{1}', Season = '{2}', Episode = '{3}', Show TVDb ID = '{4}', Show IMDb ID = '{5}'",
                episode.ShowTitle, episode.ShowYear.HasValue ? episode.ShowYear.ToString() : "<empty>", episode.Season,
                episode.Number, episode.ShowTvdbId.HasValue ? episode.ShowTvdbId.ToString() : "<empty>",
                episode.ShowImdbId ?? "<empty>");

              if (_mediaPortalServices.MarkAsUnWatched(watchedEpisode).Result)
              {
                MarkUnWatchedEpisodes++;
              }

              // update watched episodes
              localWatchedEpisodes.Remove(watchedEpisode);
            }
          }
        }

        #endregion

        #region Mark episodes as watched in local database

        _mediaPortalServices.GetLogger().Info("Start sync of tv episode watched state to local database");
        if (traktWatchedEpisodes != null && traktWatchedEpisodes.Count() > 0)
        {
          // create a unique key to lookup and search for faster
          var onlineEpisodes = traktWatchedEpisodes.ToLookup(twe => CreateLookupKey(twe), twe => twe);

          foreach (var episode in localUnWatchedEpisodes)
          {
            string tvdbKey = CreateLookupKey(episode);

            var traktEpisode = onlineEpisodes[tvdbKey].FirstOrDefault();
            if (traktEpisode != null)
            {
              _mediaPortalServices.GetLogger().Info(
                "Marking episode as watched in local database, episode is watched on trakt.tv. Plays = '{0}', Title = '{1}', Year = '{2}', Season = '{3}', Episode = '{4}', Show TVDb ID = '{5}', Show IMDb ID = '{6}', Last Watched = '{7}'",
                traktEpisode.Plays, traktEpisode.ShowTitle,
                traktEpisode.ShowYear.HasValue ? traktEpisode.ShowYear.ToString() : "<empty>", traktEpisode.Season,
                traktEpisode.Number, traktEpisode.ShowTvdbId.HasValue ? traktEpisode.ShowTvdbId.ToString() : "<empty>",
                traktEpisode.ShowImdbId ?? "<empty>", traktEpisode.WatchedAt);

              if (_mediaPortalServices.MarkAsWatched(episode).Result)
              {
                MarkWatchedEpisodes++;
              }
            }
          }
        }

        #endregion

        #region Add episodes to watched history at trakt.tv

        if (traktWatchedEpisodes != null)
        {
          TraktSyncHistoryPost watchedEpisodesForSync =
            GetWatchedShowsForSync(localWatchedEpisodes, traktWatchedEpisodes);
          TraktSyncHistoryPostResponse watchedResponse = _traktClient.AddWatchedHistoryItems(watchedEpisodesForSync);
          if (SyncWatchedEpisodes != watchedResponse?.Added?.Episodes)
          {
            // log not found episodes
          }
        }

        #endregion

        #region Add episodes to collection at trakt.tv

        TraktSyncCollectionPost collectedEpisodesForSync =
          GetCollectedEpisodesForSync(localEpisodes, traktCollectedEpisodes);
        TraktSyncCollectionPostResponse collectionResponse = _traktClient.AddCollectionItems(collectedEpisodesForSync);
        if (SyncCollectedEpisodes != collectionResponse?.Added?.Episodes)
        {
          // log not found episodes
        }

        #endregion

        return true;
      }
      return false;
    }

    private void SyncMediaToTrakt_Async()
    {
      if (SyncMovies() && SyncSeries())
      {
        TestStatus = "[Trakt.SyncFinished]";
      }
      IsSynchronizing = false;
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

    private TraktSyncHistoryPost GetWatchedShowsForSync(IList<MediaItem> localWatchedEpisodes, List<EpisodeWatched> traktEpisodesWatched)
    {
      _mediaPortalServices.GetLogger().Info("Finding local episodes to add to trakt watched history");
      TraktSyncHistoryPostBuilder builder = new TraktSyncHistoryPostBuilder();
      ILookup<string, EpisodeWatched> onlineEpisodes = traktEpisodesWatched.ToLookup(twe => CreateLookupKey(twe), twe => twe);

      foreach (var episode in localWatchedEpisodes)
      {
        string tvdbKey = CreateLookupKey(episode);
        EpisodeWatched traktEpisode = onlineEpisodes[tvdbKey].FirstOrDefault();

        if (traktEpisode == null)
        {
          builder.AddEpisode(new TraktEpisode
            {
              Ids = new TraktEpisodeIds
              {
                Tvdb = MediaItemAspectsUtl.GetTvdbId(episode),
                Imdb = MediaItemAspectsUtl.GetSeriesImdbId(episode)
              },
              Title = MediaItemAspectsUtl.GetSeriesTitle(episode),
              SeasonNumber = MediaItemAspectsUtl.GetSeasonIndex(episode),
              Number = MediaItemAspectsUtl.GetSeasonIndex(episode)
            },
            MediaItemAspectsUtl.GetLastPlayedDate(episode)
          );
          SyncWatchedEpisodes++;
        }
      }
      return builder.Build();
    }

    private TraktSyncCollectionPost GetCollectedEpisodesForSync(IList<MediaItem> localCollectedEpisodes, List<EpisodeCollected> traktEpisodesCollected)
    {
      _mediaPortalServices.GetLogger().Info("Finding local episodes to add to trakt.tv collection");
      TraktSyncCollectionPostBuilder builder = new TraktSyncCollectionPostBuilder();
      ILookup<string, EpisodeCollected> onlineEpisodes = traktEpisodesCollected.ToLookup(tce => CreateLookupKey(tce), tce => tce);

      foreach (var episode in localCollectedEpisodes)
      {
        string tvdbKey = CreateLookupKey(episode);
        EpisodeCollected traktEpisode = onlineEpisodes[tvdbKey].FirstOrDefault();

        if (traktEpisode == null)
        {
          builder.AddEpisode(new TraktEpisode
            {
              Ids = new TraktEpisodeIds
              {
                Tvdb = MediaItemAspectsUtl.GetTvdbId(episode),
                Imdb = MediaItemAspectsUtl.GetSeriesImdbId(episode)
              },
              Title = MediaItemAspectsUtl.GetSeriesTitle(episode),
              SeasonNumber = MediaItemAspectsUtl.GetSeasonIndex(episode),
              Number = MediaItemAspectsUtl.GetSeasonIndex(episode),
            },
            new TraktMetadata
            {
              MediaType = MediaItemAspectsUtl.GetVideoMediaType(episode),
              MediaResolution = MediaItemAspectsUtl.GetVideoResolution(episode),
              Audio = MediaItemAspectsUtl.GetVideoAudioCodec(episode),
              AudioChannels = MediaItemAspectsUtl.GetVideoAudioChannel(episode),
              ThreeDimensional = false
            },
            MediaItemAspectsUtl.GetDateAddedToDb(episode));
          SyncCollectedEpisodes++;
        }
      }
      return builder.Build();
    }
  }
}