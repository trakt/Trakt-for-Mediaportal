using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using System.Linq;
using System.Text;
using MediaPortal.GUI.Library;
using MediaPortal.Util;

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

    public enum RatingOverlayImage
    {
        Love,
        Hate,
        None
    }

    public static class GUIImageHandler
    {
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

            // Ignore Image placeholders (series/movies with no artwork)
            // use skins default images instead
            if (url.Contains("poster-small") || url.Contains("fanart-summary")) return false;

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(localFile));
                if (!File.Exists(localFile))
                {
                    TraktLogger.Debug("Downloading new image from: {0}", url);
                    webClient.DownloadFile(url, localFile);
                }
                return true;
            }
            catch (Exception)
            {
                TraktLogger.Info("Image download failed from '{0}' to '{1}'", url, localFile);
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

            if (string.IsNullOrEmpty(filename) || filename.Contains("fanart-summary") || !File.Exists(filename))
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
                TraktLogger.Warning("Fast loading of texture {0} failed - trying safe fallback now", file);
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

        /// <summary>
        /// Draws a trakt overlay, library/seen/watchlist icon on a poster
        /// This is done in memory and wont touch the existing file
        /// </summary>
        /// <param name="origPoster">Filename of the untouched poster</param>
        /// <param name="type">Overlay type enum</param>
        /// <returns>An image with overlay added to poster</returns>
        public static Bitmap DrawOverlayOnPoster(string origPoster, MainOverlayImage mainType, RatingOverlayImage ratingType)
        {
            Bitmap poster = new Bitmap(GUIImageHandler.LoadImage(origPoster));
            Graphics gph = Graphics.FromImage(poster);

            string mainOverlayImage = GUIGraphicsContext.Skin + string.Format(@"\Media\trakt{0}.png", mainType.ToString().Replace(", ", string.Empty));
            if (mainType != MainOverlayImage.None && File.Exists(mainOverlayImage))
            {
                Bitmap newPoster = new Bitmap(GUIImageHandler.LoadImage(mainOverlayImage));
                gph.DrawImage(newPoster, TraktSkinSettings.PosterMainOverlayPosX, TraktSkinSettings.PosterMainOverlayPosY);
            }

            string ratingOverlayImage = GUIGraphicsContext.Skin + string.Format(@"\Media\trakt{0}.png", Enum.GetName(typeof(RatingOverlayImage), ratingType));
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

            string mainOverlayImage = GUIGraphicsContext.Skin + string.Format(@"\Media\trakt{0}.png", mainType.ToString().Replace(", ", string.Empty));
            if (mainType != MainOverlayImage.None && File.Exists(mainOverlayImage))
            {
                Bitmap newThumb = new Bitmap(GUIImageHandler.LoadImage(mainOverlayImage));
                gph.DrawImage(newThumb, TraktSkinSettings.EpisodeThumbMainOverlayPosX, TraktSkinSettings.EpisodeThumbMainOverlayPosY);
            }

            string ratingOverlayImage = GUIGraphicsContext.Skin + string.Format(@"\Media\trakt{0}.png", Enum.GetName(typeof(RatingOverlayImage), ratingType));
            if (ratingType != RatingOverlayImage.None && File.Exists(ratingOverlayImage))
            {
                Bitmap newThumb = new Bitmap(GUIImageHandler.LoadImage(ratingOverlayImage));
                gph.DrawImage(newThumb, TraktSkinSettings.EpisodeThumbRatingOverlayPosX, TraktSkinSettings.EpisodeThumbRatingOverlayPosY);
            }

            gph.Dispose();
            return thumb;
        }
    }
}
