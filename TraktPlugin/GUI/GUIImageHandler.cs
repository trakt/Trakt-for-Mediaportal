using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Linq;
using System.Text;
using MediaPortal.Util;

namespace TraktPlugin.GUI
{
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
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(localFile));
                if (!File.Exists(localFile) || ImageFast.FromFile(localFile) == null)
                {
                    TraktLogger.Debug("Downloading new image from: {0}", url);
                    webClient.DownloadFile(url, localFile);
                }
                return true;
            }
            catch (WebException)
            {
                TraktLogger.Info("Image download failed from '{0}' to '{1}'", url, localFile);
                return false;
            }
        }

        /// <summary>
        /// Gets a MediaPortal texture identifier from filename
        /// </summary>
        /// <param name="filename">Filename to generate texture</param>
        /// <returns>MediaPortal texture identifier</returns>
        public static string GetTextureIdentFromFile(string filename)
        {
            return "[Trakt:" + filename.GetHashCode() + "]";
        }
    }
}
