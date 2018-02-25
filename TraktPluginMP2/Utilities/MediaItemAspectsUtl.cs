using System;
using System.Collections.Generic;
using System.Linq;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using TraktApiSharp.Enums;

namespace TraktPluginMP2.Utilities
{
  public static class MediaItemAspectsUtl
  {
    public static string GetMovieImdbId(MediaItem mediaItem)
    {
      string id;
      return MediaItemAspect.TryGetExternalAttribute(mediaItem.Aspects, ExternalIdentifierAspect.SOURCE_IMDB, ExternalIdentifierAspect.TYPE_MOVIE, out id) ? id : null;
    }

    public static uint? GetMovieTmdbId(MediaItem mediaItem)
    {
      string id;
      int tmdbId;
      return MediaItemAspect.TryGetExternalAttribute(mediaItem.Aspects, ExternalIdentifierAspect.SOURCE_TMDB, ExternalIdentifierAspect.TYPE_MOVIE, out id) && int.TryParse(id, out tmdbId) ? (uint?)tmdbId : null;
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

    public static DateTime GetLastPlayedDate(MediaItem mediaItem)
    {
      DateTime lastplayed;
      return MediaItemAspect.TryGetAttribute(mediaItem.Aspects, MediaAspect.ATTR_LASTPLAYED, out lastplayed) ? lastplayed.ToUniversalTime() : DateTime.Now;
    }

    public static DateTime GetDateAddedToDb(MediaItem mediaItem)
    {
      DateTime addedToDb;
      return MediaItemAspect.TryGetAttribute(mediaItem.Aspects, ImporterAspect.ATTR_DATEADDED, out addedToDb) ? addedToDb.ToUniversalTime() : DateTime.Now;
    }

    public static TraktMediaType GetVideoMediaType(MediaItem mediaItem)
    {
      bool isDvd;
      MediaItemAspect.TryGetAttribute(mediaItem.Aspects, VideoAspect.ATTR_ISDVD, out isDvd);
      return isDvd ? TraktMediaType.DVD : TraktMediaType.Digital;
    }

    public static TraktMediaResolution GetVideoResolution(MediaItem mediaItem)
    {
      List<int> widths;
      int width;

      if (MediaItemAspect.TryGetAttribute(mediaItem.Aspects, VideoStreamAspect.ATTR_WIDTH, out widths) && (width = widths.First()) > 0)

        switch (width)
        {
          case 1920:
            return TraktMediaResolution.HD_1080p;
          case 1280:
            return TraktMediaResolution.HD_720p;
          case 720:
            return TraktMediaResolution.SD_576p;
          case 640:
            return TraktMediaResolution.SD_480p;
          case 2160:
            return TraktMediaResolution.UHD_4k;
          default:
            return TraktMediaResolution.Unspecified;
        }

      return null;
    }

    public static TraktMediaAudio GetVideoAudioCodec(MediaItem mediaItem)
    {
      List<string> audioCodecs;
      string audioCodec;

      if (MediaItemAspect.TryGetAttribute(mediaItem.Aspects, VideoAudioStreamAspect.ATTR_AUDIOENCODING, out audioCodecs) && !string.IsNullOrWhiteSpace(audioCodec = audioCodecs.First()))
      {
        switch (audioCodec.ToLowerInvariant())
        {
          case "truehd":
            return TraktMediaAudio.DolbyTrueHD;
          case "dts":
            return TraktMediaAudio.DTS;
          case "dtshd":
            return TraktMediaAudio.DTS_MA;
          case "ac3":
            return TraktMediaAudio.DolbyDigital;
          case "aac":
            return TraktMediaAudio.AAC;
          case "mp2":
            return TraktMediaAudio.MP3;
          case "pcm":
            return TraktMediaAudio.LPCM;
          case "ogg":
            return TraktMediaAudio.OGG;
          case "wma":
            return TraktMediaAudio.WMA;
          default:
            return TraktMediaAudio.Unspecified;
        }
      }
      return null;
    }

    public static TraktMediaAudioChannel GetVideoAudioChannel(MediaItem mediaItem)
    {
      List<int> audioChannels;
      int audioChannel;
      if (MediaItemAspect.TryGetAttribute(mediaItem.Aspects, VideoAudioStreamAspect.ATTR_AUDIOCHANNELS, out audioChannels) && (audioChannel = audioChannels.First()) > 0)
      {
        switch (audioChannel)
        {
          case 1:
            return TraktMediaAudioChannel.Channels_1_0;
          case 2:
            return TraktMediaAudioChannel.Channels_2_0;
          case 3:
            return TraktMediaAudioChannel.Channels_2_1;
          case 4:
            return TraktMediaAudioChannel.Channels_4_0;
          case 5:
            return TraktMediaAudioChannel.Channels_5_0;
          case 6:
            return TraktMediaAudioChannel.Channels_5_1;
          case 7:
            return TraktMediaAudioChannel.Channels_6_1;
          case 8:
            return TraktMediaAudioChannel.Channels_7_1;
        }
      }

      return null;
    }

    public static uint GetTvdbId(MediaItem mediaItem)
    {
      string id;
      return MediaItemAspect.TryGetExternalAttribute(mediaItem.Aspects, ExternalIdentifierAspect.SOURCE_TVDB, ExternalIdentifierAspect.TYPE_SERIES, out id) ? Convert.ToUInt32(id) : 0;
    }

    public static int GetSeasonIndex(MediaItem mediaItem)
    {
      int value;
      return MediaItemAspect.TryGetAttribute(mediaItem.Aspects, EpisodeAspect.ATTR_SEASON, out value) ? value : 0;
    }

    public static int GetEpisodeIndex(MediaItem mediaItem)
    {
      // TODO: multi episode files?!
      List<int> intList;
      return MediaItemAspect.TryGetAttribute(mediaItem.Aspects, EpisodeAspect.ATTR_EPISODE, out intList) && intList.Any() ? intList.First() : intList.FirstOrDefault();
    }

    public static string GetSeriesImdbId(MediaItem mediaItem)
    {
      string id;
      return MediaItemAspect.TryGetExternalAttribute(mediaItem.Aspects, ExternalIdentifierAspect.SOURCE_IMDB, ExternalIdentifierAspect.TYPE_SERIES, out id) ? id : null;
    }

    public static string GetSeriesTitle(MediaItem mediaItem)
    {
      string value;
      return MediaItemAspect.TryGetAttribute(mediaItem.Aspects, EpisodeAspect.ATTR_SERIES_NAME, out value) ? value : null;
    }
  }
}