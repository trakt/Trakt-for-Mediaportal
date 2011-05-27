using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using MediaPortal.GUI.Library;

namespace TraktPlugin.GUI
{
    static class TraktSkinSettings
    {
        public static string CurrentSkin { get { return GUIGraphicsContext.Skin; } }
        public static string PreviousSkin { get; set; }

        public static int PosterMainOverlayPosX { get; set; }
        public static int PosterMainOverlayPosY { get; set; }
        public static int PosterRatingOverlayPosX { get; set; }
        public static int PosterRatingOverlayPosY { get; set; }

        public static void Init()
        {
            // Import Skin Settings
            string xmlSkinSettings = GUIGraphicsContext.Skin + @"\Trakt.SkinSettings.xml";
            Load(xmlSkinSettings);

            // Remember last skin used incase we need to reload
            PreviousSkin = CurrentSkin;
        }

        /// <summary>
        /// Reads all Skin Settings
        /// </summary>
        /// <param name="filename"></param>
        public static void Load(string filename)
        {
            // Check if File Exist
            if (!System.IO.File.Exists(filename))
            {
                TraktLogger.Warning("Trakt Skin Settings does not exist!");
                return;
            }

            XmlDocument doc = new XmlDocument();
            try
            {
                doc.Load(filename);
            }
            catch (XmlException e)
            {
                TraktLogger.Error("Cannot Load skin settings xml file!: {0}", e.Message);                
                return;
            }

            // Read and Import Skin Settings            
            GetOverlayPositions(doc);            
        }

        /// <summary>
        /// Get Position of overlays to add on posters in thumbs
        /// </summary>
        /// <param name="doc"></param>
        private static void GetOverlayPositions(XmlDocument doc)
        {
            TraktLogger.Info("Loading Settings for Overlay positions");

            int posx = 0;
            int posy = 0;

            // Load Main Overlay Positions
            XmlNode node = null;
            node = doc.DocumentElement.SelectSingleNode("/settings/mainoverlayicons/posters/posx");
            if (node != null)
            {
                int.TryParse(node.InnerText, out posx);
                PosterMainOverlayPosX = posx;
            }
            node = doc.DocumentElement.SelectSingleNode("/settings/mainoverlayicons/posters/posy");
            if (node != null)
            {
                int.TryParse(node.InnerText, out posy);
                PosterMainOverlayPosY = posy;
            }

            // Load Rating Overlay Positions
            node = doc.DocumentElement.SelectSingleNode("/settings/ratingoverlayicons/posters/posx");
            if (node != null)
            {
                int.TryParse(node.InnerText, out posx);
                PosterRatingOverlayPosX = posx;
            }
            node = doc.DocumentElement.SelectSingleNode("/settings/ratingoverlayicons/posters/posy");
            if (node != null)
            {
                int.TryParse(node.InnerText, out posy);
                PosterRatingOverlayPosY = posy;
            }
        }

    }
}
