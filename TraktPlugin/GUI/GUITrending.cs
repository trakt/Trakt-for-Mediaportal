using MediaPortal.GUI.Library;

namespace TraktPlugin.GUI
{
    public class GUITrending : GUIWindow
    {
        #region Constructor

        public GUITrending() { }

        #endregion

        #region Base Overrides

        public override int GetID
        {
            get
            {
                return 87264;
            }
        }

        public override bool Init()
        {
            return Load(GUIGraphicsContext.Skin + @"\Trakt.Trending.xml");
        }

        #endregion
    }
}
