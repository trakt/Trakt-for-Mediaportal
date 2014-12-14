using MediaPortal.GUI.Library;

namespace TraktPlugin.GUI
{
    public class GUIWatchList : GUIWindow
    {
        #region Constructor

        public GUIWatchList() { }

        #endregion

        #region Base Overrides

        public override int GetID
        {
            get
            {
                return 87267;
            }
        }

        public override bool Init()
        {
            return Load(GUIGraphicsContext.Skin + @"\Trakt.WatchList.xml");
        }

        #endregion
    }
}
