using TraktPlugin;
using System;

namespace ConfigLauncher
{
    public class TraktLauncher : PluginConfigLauncher
    {
        public override string FriendlyPluginName
        {
            get { return "Trakt"; }
        }

        public override void Launch()
        {
            ConfigConnector plugin = new ConfigConnector();
            plugin.ShowPlugin();
        }
    }
}


