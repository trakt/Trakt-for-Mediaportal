using MediaPortal.GUI.Library;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace TraktPlugin.GUI
{
    public class DashboardTrendingSettings
    {
        public List<string> TVShowWindows { get; set; }
        public List<string> MovieWindows { get; set; }
        public string FacadeType { get; set; }
        public int FacadeMaxItems { get; set; }
        public int PropertiesMaxItems { get; set; }
    }

    static class TraktSkinSettings
    {
        public static string CurrentSkin { get { return GUIGraphicsContext.Skin; } }
        public static string PreviousSkin { get; set; }

        public static int PosterMainOverlayPosX { get; set; }
        public static int PosterMainOverlayPosY { get; set; }
        public static int PosterRatingOverlayPosX { get; set; }
        public static int PosterRatingOverlayPosY { get; set; }

        public static int EpisodeThumbMainOverlayPosX { get; set; }
        public static int EpisodeThumbMainOverlayPosY { get; set; }
        public static int EpisodeThumbRatingOverlayPosX { get; set; }
        public static int EpisodeThumbRatingOverlayPosY { get; set; }

        public static int AvatarRatingOverlayPosX { get; set; }
        public static int AvatarRatingOverlayPosY { get; set; }

        public static List<string> DashBoardActivityWindows { get; set; }
        public static int DashboardActivityPropertiesMaxItems { get; set; }
        public static int DashboardActivityFacadeMaxItems { get; set; }
        public static string DashboardActivityFacadeType { get; set; }

        public static bool HasDashboardStatistics { get; set; }

        public static List<DashboardTrendingSettings> DashboardTrendingCollection { get; set; }

        public static void Init()
        {
            // Import Skin Settings
            string xmlSkinSettings = TraktHelper.GetThemedSkinFile(SkinThemeType.File, "Trakt.SkinSettings.xml");
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

            // Read Dashboard Skin Setings
            GetDashboardSkinSettings(doc);
        }

        /// <summary>
        /// Gets the dashboard settings e.g. windows supported
        /// </summary>
        /// <param name="doc"></param>
        private static void GetDashboardSkinSettings(XmlDocument doc)
        {
            TraktLogger.Info("Loading Settings for Dashboard");

            // skinner can define different trending dashboards
            DashboardTrendingCollection = new List<DashboardTrendingSettings>();

            DashBoardActivityWindows = new List<string>();
            DashboardActivityPropertiesMaxItems = 0;
            DashboardActivityFacadeMaxItems = 25;
            DashboardActivityFacadeType = "None";

            XmlNode rootNode = null;
            XmlNode node = null;

            rootNode = doc.DocumentElement.SelectSingleNode("/settings/dashboard/statistics");
            if (rootNode != null)
            {
                HasDashboardStatistics = rootNode.InnerText.ToLowerInvariant() == "true";
            }

            rootNode = doc.DocumentElement.SelectSingleNode("/settings/dashboard/activities");
            if (rootNode != null)
            {
                node = rootNode.SelectSingleNode("windows");
                if (node != null)
                {
                    DashBoardActivityWindows = node.InnerText.Split('|').ToList();
                }

                node = rootNode.SelectSingleNode("facadetype");
                if (node != null)
                {
                    DashboardActivityFacadeType = ValidateLayoutType(node.InnerText);
                }

                node = rootNode.SelectSingleNode("facademaxitems");
                if (node != null)
                {
                    int maxItems;
                    if (int.TryParse(node.InnerText, out maxItems))
                        DashboardActivityFacadeMaxItems = maxItems;
                }
                node = rootNode.SelectSingleNode("propertiesmaxitems");
                if (node != null)
                {
                    int maxItems;
                    if (int.TryParse(node.InnerText, out maxItems))
                        DashboardActivityPropertiesMaxItems = maxItems;
                }
            }

            MaxTrendingItems = 10;

            var trendingNodes = doc.DocumentElement.SelectNodes("/settings/dashboard/trending");
            if (trendingNodes != null)
            {
                foreach (XmlNode trendingNode in trendingNodes)
                {
                    var trendingItem = new DashboardTrendingSettings
                    {
                        TVShowWindows = new List<string>(),
                        MovieWindows = new List<string>(),
                        PropertiesMaxItems = 0,
                        FacadeMaxItems = 10,
                        FacadeType = "None"
                    };
                    
                    node = trendingNode.SelectSingleNode("facadetype");
                    if (node != null)
                    {
                        trendingItem.FacadeType = ValidateLayoutType(node.InnerText);
                    }

                    node = trendingNode.SelectSingleNode("facademaxitems");
                    if (node != null)
                    {
                        int maxItems;
                        if (int.TryParse(node.InnerText, out maxItems))
                        {
                            trendingItem.FacadeMaxItems = maxItems;
                            if (maxItems > MaxTrendingItems) MaxTrendingItems = maxItems;
                        }
                    }

                    node = trendingNode.SelectSingleNode("propertiesmaxitems");
                    if (node != null)
                    {
                        int maxItems;
                        if (int.TryParse(node.InnerText, out maxItems))
                            trendingItem.PropertiesMaxItems = maxItems;
                    }

                    node = trendingNode.SelectSingleNode("shows");
                    if (node != null)
                    {
                        node = node.SelectSingleNode("windows");
                        if (node != null)
                        {
                            trendingItem.TVShowWindows = node.InnerText.Split('|').ToList();
                        }
                    }

                    node = trendingNode.SelectSingleNode("movies");
                    if (node != null)
                    {
                        node = node.SelectSingleNode("windows");
                        if (node != null)
                        {
                            trendingItem.MovieWindows = node.InnerText.Split('|').ToList();
                        }
                    }

                    // add to the collection
                    DashboardTrendingCollection.Add(trendingItem);
                }
            }
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
            PosterMainOverlayPosX = 222;
            node = doc.DocumentElement.SelectSingleNode("/settings/mainoverlayicons/posters/posx");
            if (node != null)
            {
                int.TryParse(node.InnerText, out posx);
                if (posx == 178) posx = 222; // upgrade step, new poster sizes
                PosterMainOverlayPosX = posx;
            }
            PosterMainOverlayPosY = 0;
            node = doc.DocumentElement.SelectSingleNode("/settings/mainoverlayicons/posters/posy");
            if (node != null)
            {
                int.TryParse(node.InnerText, out posy);
                PosterMainOverlayPosY = posy;
            }

            node = null;
            EpisodeThumbMainOverlayPosX = 278;
            node = doc.DocumentElement.SelectSingleNode("/settings/mainoverlayicons/episodethumbs/posx");
            if (node != null)
            {
                int.TryParse(node.InnerText, out posx);
                EpisodeThumbMainOverlayPosX = posx;
            }
            EpisodeThumbMainOverlayPosY = 0;
            node = doc.DocumentElement.SelectSingleNode("/settings/mainoverlayicons/episodethumbs/posy");
            if (node != null)
            {
                int.TryParse(node.InnerText, out posy);
                EpisodeThumbMainOverlayPosY = posy;
            }

            // Load Rating Overlay Positions
            PosterRatingOverlayPosX = 222;
            node = doc.DocumentElement.SelectSingleNode("/settings/ratingoverlayicons/posters/posx");
            if (node != null)
            {
                int.TryParse(node.InnerText, out posx);
                if (posx == 178) posx = 222; // upgrade step, new poster sizes
                PosterRatingOverlayPosX = posx;
            }
            PosterRatingOverlayPosY = 0;
            node = doc.DocumentElement.SelectSingleNode("/settings/ratingoverlayicons/posters/posy");
            if (node != null)
            {
                int.TryParse(node.InnerText, out posy);
                PosterRatingOverlayPosY = posy;
            }

            EpisodeThumbRatingOverlayPosX = 278;
            node = doc.DocumentElement.SelectSingleNode("/settings/ratingoverlayicons/episodethumbs/posx");
            if (node != null)
            {
                int.TryParse(node.InnerText, out posx);
                EpisodeThumbRatingOverlayPosX = posx;
            }
            EpisodeThumbRatingOverlayPosY = 0;
            node = doc.DocumentElement.SelectSingleNode("/settings/ratingoverlayicons/episodethumbs/posy");
            if (node != null)
            {
                int.TryParse(node.InnerText, out posy);
                EpisodeThumbRatingOverlayPosY = posy;
            }

            AvatarRatingOverlayPosX = 18;
            node = doc.DocumentElement.SelectSingleNode("/settings/ratingoverlayicons/avatar/posx");
            if (node != null)
            {
                int.TryParse(node.InnerText, out posx);
                AvatarRatingOverlayPosX = posx;
            }
            AvatarRatingOverlayPosY = 0;
            node = doc.DocumentElement.SelectSingleNode("/settings/ratingoverlayicons/avatar/posy");
            if (node != null)
            {
                int.TryParse(node.InnerText, out posy);
                AvatarRatingOverlayPosY = posy;
            }
        }

        private static string ValidateLayoutType(string layout)
        {
            switch (layout.ToLowerInvariant())
            {
                case "list":
                    return "List";
                case "smallicons":
                    return "SmallIcons";
                case "largeicons":
                    return "LargeIcons";
                case "filmstrip":
                    return "Filmstrip";
                default:
                    TraktLogger.Warning("Invalid MediaPortal layout '{0}' defined in the skin settings", layout);
                    return "None";
            }
        }

        public static int MaxTrendingItems { get; set; }
    }
}
