using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Net;
using MediaPortal.Configuration;
using MediaPortal.Util;
using TraktPlugin.TmdbAPI.DataStructures;
using TraktAPI.DataStructures;

namespace TraktPlugin.GUI
{
    [Flags]
    public enum MainOverlayImage
    {
        None = 0,
        Watchlist = 1,
        Seenit = 2,
        Library = 4,
    }

    /// <summary>
    /// Support both Advanced and Simple rating overlays
    /// Order of enum values are important!
    /// </summary>
    public enum RatingOverlayImage
    {
        None,
        Heart1,
        Heart2,
        Heart3,
        Heart4,
        Heart5,
        Heart6,
        Heart7,
        Heart8,
        Heart9,
        Heart10,
        Love,
        Hate
    }
    
    /// <summary>
    /// Artwork Types used in image downloads
    /// </summary>
    public enum ArtworkType
    {
        MoviePoster,
        MovieFanart,
        MovieLogo,
        MovieClearArt,
        MovieBanner,
        MovieThumb,
        ShowPoster,
        ShowBanner,
        ShowFanart,
        ShowLogo,
        ShowClearArt,
        ShowThumb,
        SeasonPoster,
        SeasonThumb,
        EpisodeImage,
        Avatar,
        PersonHeadshot,
        PersonFanart
    }

    /// <summary>
    /// This object will typically hold images used in facade list items and window backgrounds
    /// </summary>
    public class GUITraktImage : INotifyPropertyChanged
    {
        public TraktUserImages UserImages { get; set; }

        /// <summary>
        /// raise event when property changes so we can know when a artwork
        /// download is complete and ready to be pushed to skin
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// This object will typically hold images used in facade list items and window backgrounds
    /// </summary>
    public class GUITmdbImage : INotifyPropertyChanged
    {
        public TmdbEpisodeImages EpisodeImages { get; set; }
        public TmdbShowImages ShowImages { get; set; }
        public TmdbMovieImages MovieImages { get; set; }
        public TmdbSeasonImages SeasonImages { get; set; }
        public TmdbPeopleImages PeopleImages { get; set; }

        /// <summary>
        /// raise event when property changes so we can know when a artwork
        /// download is complete and ready to be pushed to skin
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public static class GUIImageHandler
    {
        /// <summary>
        /// Get a overlay for images that represent a users rating
        /// </summary>
        internal static RatingOverlayImage GetRatingOverlay(int? userRating)
        {
            RatingOverlayImage ratingOverlay = RatingOverlayImage.None;
            
            if (userRating != null)
            {
                ratingOverlay = (RatingOverlayImage)userRating;
            }

            return ratingOverlay;
        }

        /// <summary>
        /// Gets the local filename of an image from a Trakt URL
        /// </summary>
        /// <param name="url">The online URL of the trakt image</param>
        /// <param name="type">The Type of image to get</param>
        /// <returns>Retruns the local filename of the image</returns>
        public static string LocalImageFilename(this TraktImage image, ArtworkType type)
        {
            if (image == null) return string.Empty;

            string filename = string.Empty;
            string folder = string.Empty;

            switch (type)
            {
                case ArtworkType.Avatar:
                    filename = image.FullSize.ToClearUrl();
                    folder = Config.GetSubFolder(Config.Dir.Thumbs, @"Trakt\Avatars");
                    break;

                case ArtworkType.PersonHeadshot:
                    filename = image.ThumbSize.ToClearUrl();
                    folder = Config.GetSubFolder(Config.Dir.Thumbs, @"Trakt\People");
                    break;

                case ArtworkType.PersonFanart:
                    filename = TraktSettings.DownloadFullSizeFanart ? image.FullSize.ToClearUrl() : image.MediumSize.ToClearUrl();
                    folder = Config.GetSubFolder(Config.Dir.Thumbs, @"Trakt\People\Fanart");
                    break;

                case ArtworkType.SeasonPoster:
                    filename = image.ThumbSize.ToClearUrl();
                    folder = Config.GetSubFolder(Config.Dir.Thumbs, @"Trakt\Shows\Seasons");
                    break;

                case ArtworkType.MoviePoster:
                    filename = image.ThumbSize.ToClearUrl();
                    folder = Config.GetSubFolder(Config.Dir.Thumbs, @"Trakt\Movies\Posters");
                    break;

                case ArtworkType.MovieFanart:
                    filename = TraktSettings.DownloadFullSizeFanart ? image.FullSize.ToClearUrl() : image.MediumSize.ToClearUrl();
                    folder = Config.GetSubFolder(Config.Dir.Thumbs, @"Trakt\Movies\Fanart");
                    break;

                case ArtworkType.MovieBanner:
                    filename = image.FullSize.ToClearUrl();
                    folder = Config.GetSubFolder(Config.Dir.Thumbs, @"Trakt\Movies\Banners");
                    break;

                case ArtworkType.ShowPoster:
                    filename = image.ThumbSize.ToClearUrl();
                    folder = Config.GetSubFolder(Config.Dir.Thumbs, @"Trakt\Shows\Posters");
                    break;

                case ArtworkType.ShowBanner:
                    filename = image.FullSize.ToClearUrl();
                    folder = Config.GetSubFolder(Config.Dir.Thumbs, @"Trakt\Shows\Banners");
                    break;

                case ArtworkType.ShowFanart:
                    filename = TraktSettings.DownloadFullSizeFanart ? image.FullSize.ToClearUrl() : image.MediumSize.ToClearUrl();
                    folder = Config.GetSubFolder(Config.Dir.Thumbs, @"Trakt\Shows\Fanart");
                    break;

                case ArtworkType.EpisodeImage:
                    filename = image.ThumbSize.ToClearUrl();
                    folder = Config.GetSubFolder(Config.Dir.Thumbs, @"Trakt\Episodes");
                    break;
            }

            if (string.IsNullOrEmpty(filename))
                return null;

            return Path.Combine(folder, Path.GetFileName(new Uri(filename).LocalPath));
        }

        /// <summary>
        /// Cleans a uri such that a friendly file system name can be derived
        /// </summary>
        public static string ToClearUrl(this string url)
        {
            if (string.IsNullOrEmpty(url))
                return string.Empty;

            // remove the file-extension from the midstring and add it to the end where it belongs         
            if (url.Contains("jpg?"))
                url = url.Replace("jpg?", string.Empty) + ".jpg";
            if (url.Contains("png?"))
                url = url.Replace("png?", string.Empty) + ".png";

            // if a gravatar
            if (url.Contains("gravatar"))
            {
                // grab the hash only (first param) and get rid of the extra parameters
                int defaultParam = url.IndexOf('?', 0);
                url = url.Substring(0, defaultParam) + ".jpg";
            }

            return url;
        }

        /// <summary>
        /// Returns the default Poster to display in the facade
        /// </summary>
        /// <param name="largePoster">return small are large image</param>
        internal static string GetDefaultPoster(bool largePoster = true)
        {
            if (DefaultPosterExists == false)
            {
                // return the MediaPortal default if not found
                return largePoster ? "defaultVideoBig.png" : "defaultVideo.png";
            }

            return largePoster ? "defaultTraktPosterBig.png" : "defaultTraktPoster.png";
        }

        static bool? DefaultPosterExists
        {
            get
            {
                if (_defaultPosterExists == null)
                {
                    try
                    {
                        _defaultPosterExists = File.Exists(TraktHelper.GetThemedSkinFile(SkinThemeType.Image, "defaultTraktPoster.png"));
                    }
                    catch
                    {
                        _defaultPosterExists = false;
                    }
                }
                return _defaultPosterExists;
            }
        } 
        static bool? _defaultPosterExists = null;

        /// <summary>
        /// Download an image if it does not exist locally
        /// </summary>
        /// <param name="url">Online URL of image to download</param>
        /// <param name="localFile">Local filename to save image</param>
        /// <returns>true if image downloads successfully or loads from disk successfully</returns>
        public static bool DownloadImage(string url, string localFile)
        {
            WebClient webClient = new WebClient();
            webClient.Headers.Add("user-agent", TraktSettings.UserAgent);

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(localFile));
                if (!File.Exists(localFile))
                {
                    TraktLogger.Debug("Downloading new image. Url = '{0}', Filename = '{1}'", url, localFile);
                    webClient.DownloadFile(url, localFile);
                }
                return true;
            }
            catch (Exception)
            {
                TraktLogger.Warning("Image download failed. Url = '{0}', Filename = '{1}'", url, localFile);
                try { if (File.Exists(localFile)) File.Delete(localFile); } catch { }
                return false;
            }
        }

        public static void LoadFanart(ImageSwapper backdrop, string filename)
        {
            // Dont activate and load if user does not want to download fanart
            if (!TraktSettings.DownloadFanart)
            {
                if (backdrop.Active) backdrop.Active = false;
                return;
            }
            
            // Activate Backdrop in Image Swapper
            if (!backdrop.Active) backdrop.Active = true;

            if (string.IsNullOrEmpty(filename) || !File.Exists(filename))
                filename = string.Empty;

            // Assign Fanart filename to Image Loader
            // Will display fanart in backdrop or reset to default background
            backdrop.Filename = filename;
        }

        /// <summary>
        /// Loads an image FAST from file
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static Image LoadImage(string file)
        {
            if (string.IsNullOrEmpty(file) || !File.Exists(file)) return null;
            
            Image img = null;

            try
            {
                img = ImageFast.FromFile(file);
            }
            catch
            {
                // Most likely a Zero Byte file but not always
                TraktLogger.Warning("Fast loading of texture '{0}' failed - trying safe fallback now", file);
                try { img = Image.FromFile(file); } catch { }
            }

            return img;
        }

        /// <summary>
        /// Gets a MediaPortal texture identifier from filename
        /// </summary>
        /// <param name="filename">Filename to generate texture</param>
        /// <returns>MediaPortal texture identifier</returns>
        public static string GetTextureIdentFromFile(string filename)
        {
            return GetTextureIdentFromFile(filename, string.Empty);
        }
        
        public static string GetTextureIdentFromFile(string filename, string suffix)
        {
            return "[Trakt:" + (filename + suffix).GetHashCode() + "]";
        }

        public static Bitmap DrawOverlayOnPoster(string origPoster, MainOverlayImage mainType, RatingOverlayImage ratingType)
        {
            return DrawOverlayOnPoster(origPoster, mainType, ratingType, new Size());
        }

        /// <summary>
        /// Draws a trakt overlay, library/seen/watchlist icon on a poster
        /// This is done in memory and wont touch the existing file
        /// </summary>
        /// <param name="origPoster">Filename of the untouched poster</param>
        /// <param name="type">Overlay type enum</param>
        /// <param name="size">Size of returned image</param>
        /// <returns>An image with overlay added to poster</returns>
        public static Bitmap DrawOverlayOnPoster(string origPoster, MainOverlayImage mainType, RatingOverlayImage ratingType, Size size)
        {
            Image image = GUIImageHandler.LoadImage(origPoster);
            if (image == null) return null;

            Bitmap poster = size.IsEmpty ? new Bitmap(image) : new Bitmap(image, size);
            Graphics gph = Graphics.FromImage(poster);

            string mainOverlayImage = TraktHelper.GetThemedSkinFile(SkinThemeType.Image, string.Format("trakt{0}.png", mainType.ToString().Replace(", ", string.Empty)));
            if (mainType != MainOverlayImage.None && File.Exists(mainOverlayImage))
            {
                Bitmap newPoster = new Bitmap(GUIImageHandler.LoadImage(mainOverlayImage));
                gph.DrawImage(newPoster, TraktSkinSettings.PosterMainOverlayPosX, TraktSkinSettings.PosterMainOverlayPosY);
            }

            string ratingOverlayImage = TraktHelper.GetThemedSkinFile(SkinThemeType.Image, string.Format("trakt{0}.png", Enum.GetName(typeof(RatingOverlayImage), ratingType)));
            if (ratingType != RatingOverlayImage.None && File.Exists(ratingOverlayImage))
            {
                Bitmap newPoster = new Bitmap(GUIImageHandler.LoadImage(ratingOverlayImage));
                gph.DrawImage(newPoster, TraktSkinSettings.PosterRatingOverlayPosX, TraktSkinSettings.PosterRatingOverlayPosY);
            }

            gph.Dispose();
            return poster;
        }

        /// <summary>
        /// Draws a trakt overlay, library/seen/watchlist icon on a episode thumb
        /// This is done in memory and wont touch the existing file
        /// </summary>
        /// <param name="origThumb">Filename of the untouched episode thumb</param>
        /// <param name="type">Overlay type enum</param>
        /// <param name="size">Size of returned image</param>
        /// <returns>An image with overlay added to episode thumb</returns>
        public static Bitmap DrawOverlayOnEpisodeThumb(string origThumb, MainOverlayImage mainType, RatingOverlayImage ratingType, Size size)
        {
            Image image = GUIImageHandler.LoadImage(origThumb);
            if (image == null) return null;

            Bitmap thumb = new Bitmap(image, size);
            Graphics gph = Graphics.FromImage(thumb);

            string mainOverlayImage = TraktHelper.GetThemedSkinFile(SkinThemeType.Image, string.Format("trakt{0}.png", mainType.ToString().Replace(", ", string.Empty)));
            if (mainType != MainOverlayImage.None && File.Exists(mainOverlayImage))
            {
                Bitmap newThumb = new Bitmap(GUIImageHandler.LoadImage(mainOverlayImage));
                gph.DrawImage(newThumb, TraktSkinSettings.EpisodeThumbMainOverlayPosX, TraktSkinSettings.EpisodeThumbMainOverlayPosY);
            }

            string ratingOverlayImage = TraktHelper.GetThemedSkinFile(SkinThemeType.Image, string.Format("trakt{0}.png", Enum.GetName(typeof(RatingOverlayImage), ratingType)));
            if (ratingType != RatingOverlayImage.None && File.Exists(ratingOverlayImage))
            {
                Bitmap newThumb = new Bitmap(GUIImageHandler.LoadImage(ratingOverlayImage));
                gph.DrawImage(newThumb, TraktSkinSettings.EpisodeThumbRatingOverlayPosX, TraktSkinSettings.EpisodeThumbRatingOverlayPosY);
            }

            gph.Dispose();
            return thumb;
        }

        /// <summary>
        /// Draws a trakt overlay, rating icon on a poster
        /// This is done in memory and wont touch the existing file
        /// </summary>
        /// <param name="origPoster">Filename of the untouched avatar</param>
        /// <param name="type">Overlay type enum</param>
        /// <param name="size">Size of returned image</param>
        /// <returns>An image with overlay added to avatar</returns>
        public static Bitmap DrawOverlayOnAvatar(string origAvartar, RatingOverlayImage ratingType, Size size)
        {
            Image image = GUIImageHandler.LoadImage(origAvartar);
            if (image == null) return null;

            Bitmap avatar = size.IsEmpty ? new Bitmap(image) : new Bitmap(image, size);
            Graphics gph = Graphics.FromImage(avatar);

            string ratingOverlayImage = TraktHelper.GetThemedSkinFile(SkinThemeType.Image, string.Format("trakt{0}.png", Enum.GetName(typeof(RatingOverlayImage), ratingType)));
            if (ratingType != RatingOverlayImage.None && File.Exists(ratingOverlayImage))
            {
                Bitmap newAvatar = new Bitmap(GUIImageHandler.LoadImage(ratingOverlayImage));
                gph.DrawImage(newAvatar, TraktSkinSettings.AvatarRatingOverlayPosX, TraktSkinSettings.AvatarRatingOverlayPosY);
            }

            gph.Dispose();
            return avatar;
        }
    }
}
