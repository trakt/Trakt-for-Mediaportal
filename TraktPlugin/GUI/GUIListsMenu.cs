using MediaPortal.GUI.Library;

namespace TraktPlugin.GUI
{
    public class GUIListsMenu : GUIWindow
    {
        #region Constructor

        public GUIListsMenu() { }

        #endregion

        #region Base Overrides

        public override int GetID
        {
            get
            {
                return 87502;
            }
        }

        public override bool Init()
        {
            return Load(GUIGraphicsContext.Skin + @"\Trakt.Lists.Menu.xml");
        }

        #endregion
    }
}
