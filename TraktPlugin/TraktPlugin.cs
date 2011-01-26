using System;
using MediaPortal.GUI.Library;
using System.Windows.Forms;

namespace TraktPlugin
{
    public class TraktPlugin : ISetupForm, IPlugin
    {

        #region ISetupFrom

        public string Author()
        {
            return "Technicolour";
        }

        public bool CanEnable()
        {
            return true;
        }

        public bool DefaultEnabled()
        {
            return false;
        }

        public string Description()
        {
            return "Adds Trakt scrobbling to Mediaportal";
        }

        public bool GetHome(out string strButtonText, out string strButtonImage, out string strButtonImageFocus, out string strPictureImage)
        {
            strButtonImage = null;
            strButtonText = null;
            strButtonImageFocus = null;
            strPictureImage = null;
            return false;
        }

        public int GetWindowId()
        {
            return -1;
        }

        public bool HasSetup()
        {
            return true;
        }

        public string PluginName()
        {
            return "Trakt";
        }

        public void ShowPlugin()
        {
            MessageBox.Show("Hi there is no setup at the moment please have some candy and be patient");
        }

        #endregion

        #region IPlugin

        public void Start()
        {
            //Start
        }

        public void Stop()
        {
            //Stop
        }

        #endregion
    }
}
