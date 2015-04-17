using MediaPortal.GUI.Library;

namespace TraktPlugin.GUI
{
    public class GUITVMenu : GUIWindow
    {
        #region Constructor

        public GUITVMenu() { }

        #endregion

        #region Base Overrides

        public override int GetID
        {
            get
            {
                return 87500;
            }
        }

        public override bool Init()
        {
            return Load(GUIGraphicsContext.Skin + @"\Trakt.TV.Menu.xml");
        }

        #endregion
    }
}
