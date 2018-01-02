using System;
using System.Collections.Generic;
using System.Linq;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using TraktAPI.Enums;
using TraktAPI.Extensions;

namespace TraktPluginMP2.Utilities
{
  public static class MediaItemAspectsUtl
  {
    public static string GetMovieImdbId(MediaItem mediaItem)
    {
      string id;
      return MediaItemAspect.TryGetExternalAttribute(mediaItem.Aspects, ExternalIdentifierAspect.SOURCE_IMDB, ExternalIdentifierAspect.TYPE_MOVIE, out id) ? id : null;
    }

    public static int? GetMovieTmdbId(MediaItem mediaItem)
    {
      string id;
      int tmdbId;
      return MediaItemAspect.TryGetExternalAttribute(mediaItem.Aspects, ExternalIdentifierAspect.SOURCE_TMDB, ExternalIdentifierAspect.TYPE_MOVIE, out id) && int.TryParse(id, out tmdbId) ? (int?)tmdbId : null;
    }

    public static string GetMovieTitle(MediaItem currMediaItem)
    {
      string value;
      return MediaItemAspect.TryGetAttribute(currMediaItem.Aspects, MovieAspect.ATTR_MOVIE_NAME, out value) ? value : null;
    }

    public static int GetMovieYear(MediaItem mediaItem)
    {
      DateTime dtValue;
      return MediaItemAspect.TryGetAttribute(mediaItem.Aspects, MediaAspect.ATTR_RECORDINGTIME, out dtValue) ? dtValue.Year : 0;
    }

    public static string GetLastPlayedDate(MediaItem mediaItem)
    {
      DateTime lastplayed;
      return MediaItemAspect.TryGetAttribute(mediaItem.Aspects, MediaAspect.ATTR_LASTPLAYED, out lastplayed) ? lastplayed.ToUniversalTime().ToISO8601() : string.Empty;
    }

    public static string GetDateAddedToDb(MediaItem mediaItem)
    {
      DateTime addedToDb;
      return MediaItemAspect.TryGetAttribute(mediaItem.Aspects, ImporterAspect.ATTR_DATEADDED, out addedToDb) ? addedToDb.ToUniversalTime().ToISO8601() : string.Empty;
    }

    public static string GetVideoMediaType(MediaItem mediaItem)
    {
      bool isDvd;
      MediaItemAspect.TryGetAttribute(mediaItem.Aspects, VideoAspect.ATTR_ISDVD, out isDvd);
      return isDvd ? TraktMediaType.dvd.ToString() : TraktMediaType.digital.ToString();
    }

    public static string GetVideoResolution(MediaItem mediaItem)
    {
      List<int> widths;
      int width;

      if (MediaItemAspect.TryGetAttribute(mediaItem.Aspects, VideoStreamAspect.ATTR_WIDTH, out widths) && (width = widths.First()) > 0)

        switch (width)
        {
          case 1920:
            return TraktResolution.hd_1080p.ToString();
          case 1280:
            return TraktResolution.hd_720p.ToString();
          case 720:
            return TraktResolution.sd_576p.ToString();
          case 640:
            return TraktResolution.sd_480p.ToString();
          case 2160:
            return TraktResolution.uhd_4k.ToString();
          default:
            return TraktResolution.hd_720p.ToString();
        }

      return null;
    }

    public static string GetVideoAudioCodec(MediaItem mediaItem)
    {
      List<string> audioCodecs;
      string audioCodec;

      if (MediaItemAspect.TryGetAttribute(mediaItem.Aspects, VideoAudioStreamAspect.ATTR_AUDIOENCODING, out audioCodecs) && !string.IsNullOrWhiteSpace(audioCodec = audioCodecs.First()))
      {
        switch (audioCodec.ToLowerInvariant())
        {
          case "truehd":
            return TraktAudio.dolby_truehd.ToString();
          case "dts":
            return TraktAudio.dts.ToString();
          case "dtshd":
            return TraktAudio.dts_ma.ToString();
          case "ac3":
            return TraktAudio.dolby_digital.ToString();
          case "aac":
            return TraktAudio.aac.ToString();
          case "mp2":
            return TraktAudio.mp3.ToString();
          case "pcm":
            return TraktAudio.lpcm.ToString();
          case "ogg":
            return TraktAudio.ogg.ToString();
          case "wma":
            return TraktAudio.wma.ToString();
          case "flac":
            return TraktAudio.flac.ToString();
          default:
            return null;
        }
      }
      return null;
    }
  }
}