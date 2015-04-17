using MediaPortal.GUI.Library;

namespace TraktPlugin.GUI
{
    public class GUIMoviesMenu : GUIWindow
    {
        #region Constructor

        public GUIMoviesMenu() { }

        #endregion

        #region Base Overrides

        public override int GetID
        {
            get
            {
                return 87501;
            }
        }

        public override bool Init()
        {
            return Load(GUIGraphicsContext.Skin + @"\Trakt.Movies.Menu.xml");
        }

        #endregion
    }
}
