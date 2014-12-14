using MediaPortal.GUI.Library;

namespace TraktPlugin.GUI
{
    public class GUIRecommendations : GUIWindow
    {
        #region Constructor

        public GUIRecommendations() { }

        #endregion

        #region Base Overrides

        public override int GetID
        {
            get
            {
                return 87261;
            }
        }

        public override bool Init()
        {
            return Load(GUIGraphicsContext.Skin + @"\Trakt.Recommendations.xml");
        }

        #endregion
    }
}
