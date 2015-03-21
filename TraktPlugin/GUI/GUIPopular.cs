using MediaPortal.GUI.Library;

namespace TraktPlugin.GUI
{
    public class GUIPopular : GUIWindow
    {
        #region Constructor

        public GUIPopular() { }

        #endregion

        #region Base Overrides

        public override int GetID
        {
            get
            {
                return 87100;
            }
        }

        public override bool Init()
        {
            return Load(GUIGraphicsContext.Skin + @"\Trakt.Popular.xml");
        }

        #endregion
    }
}
