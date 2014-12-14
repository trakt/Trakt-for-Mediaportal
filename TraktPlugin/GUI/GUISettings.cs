using MediaPortal.GUI.Library;

namespace TraktPlugin.GUI
{
    public class GUISettings : GUIWindow
    {
        #region Constructor

        public GUISettings() { }

        #endregion

        #region Base Overrides

        public override int GetID
        {
            get
            {
                return 87271;
            }
        }

        public override bool Init()
        {
            return Load(GUIGraphicsContext.Skin + @"\Trakt.Settings.xml");
        }

        #endregion
    }
}
