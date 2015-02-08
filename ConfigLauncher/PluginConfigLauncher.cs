using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.Win32;

namespace ConfigLauncher
{
    /// <summary>
    /// This class can be extended to easily create an EXE that will launch a specific 
    /// MediaPortal plugin by helping .NET automatically find MediaPortal and MediaPortal 
    /// related Assemblies such as your plugin DLL in the users plugin/Windows folder. 
    /// The resulting executable can be placed anywhere on the users system.
    /// 
    /// This class requires no modifications aside from the namespace, simply subclass 
    /// it, you should not need to modify it.
    /// </summary>
    public abstract class PluginConfigLauncher
    {
        // Information needed from base class, implement in your subclass.
        public abstract string FriendlyPluginName { get; }
        public abstract void Launch();

        #region Private Stuff

        // error messages
        private static string ErrorDialogTitleLabel = "Something Went Wrong";
        private static string MissingMediaPortalLabel = "MediaPortal must be installed to use this configuration launcher. A reference to the MediaPortal installation directory could not be found in the registry.";
        private static string MissingPathLabel = "MediaPortal must be installed to use this configuration launcher. The following path does not exist:\n\n{0}";
        private static string MissingDllLabel = "{0} must be installed to use this configuration launcher. The following component could not be found:\n\n{1}";
        private static string UnexpectedErrorLabel = "There is a problem and the {0} config launcher doesn't know how to recover:\n\n{1}";
        private static string NoLauncherLabel = "Could not find a class that extends PluginConfigLauncher.";

        private List<string> paths = new List<string>();

        /// <summary>
        /// Ensures MediaPortal is installed and connects our assembly finding logic to the application domain.
        /// </summary>
        private bool InitAssemblyFinder()
        {
            string mepoDir = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\MediaPortal", "InstallPath", null) as string;
            if (mepoDir == null)
            {
                MessageBox.Show(MissingMediaPortalLabel, ErrorDialogTitleLabel);
                return false;
            }

            if (!Directory.Exists(mepoDir))
            {
                MessageBox.Show(String.Format(MissingPathLabel, mepoDir), ErrorDialogTitleLabel);
                return false;
            }

            string pluginDir = Path.Combine(mepoDir, @"plugins\Windows\");
            if (!Directory.Exists(pluginDir))
            {
                MessageBox.Show(String.Format(MissingPathLabel, pluginDir), ErrorDialogTitleLabel);
                return false;
            }

            string processDir = Path.Combine(mepoDir, @"plugins\process\");
            if (!Directory.Exists(processDir))
            {
                MessageBox.Show(String.Format(MissingPathLabel, processDir), ErrorDialogTitleLabel);
                return false;
            }

            paths.Add(mepoDir);
            paths.Add(pluginDir);
            paths.Add(processDir);

            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(LoadFromMepoFolders);
            return true;
        }

        /// <summary>
        /// Listener method that helps the application domain find MediaPortal and plugin assemblies.
        /// </summary>
        private Assembly LoadFromMepoFolders(object sender, ResolveEventArgs args)
        {
            string assemblyPath = null;
            foreach (string currPath in paths)
            {
                assemblyPath = Path.Combine(currPath, new AssemblyName(args.Name).Name + ".dll");
                if (File.Exists(assemblyPath)) return Assembly.LoadFrom(assemblyPath);
            }

            return null;
        }


        [STAThreadAttribute]
        static void Main(string[] args)
        {
            PluginConfigLauncher plugin = null;

            try
            {
                System.Windows.Forms.Application.EnableVisualStyles();

                // find the type that extends this class
                Type[] types = Assembly.GetExecutingAssembly().GetTypes();
                foreach (Type type in types)
                {
                    if (type.IsClass && !type.IsAbstract && type.IsSubclassOf(typeof(PluginConfigLauncher)))
                    {
                        plugin = Activator.CreateInstance(type) as PluginConfigLauncher;
                        break;
                    }
                }

                // if nothing was found we have a problem
                if (plugin == null)
                {
                    MessageBox.Show(String.Format(UnexpectedErrorLabel, "", NoLauncherLabel), ErrorDialogTitleLabel);
                    return;
                }

                // connect or assembly finder and attempt to launch plugin
                bool pathsExists = plugin.InitAssemblyFinder();
                if (pathsExists) plugin.Launch();
            }
            catch (FileNotFoundException e)
            {
                MessageBox.Show(String.Format(MissingDllLabel, plugin.FriendlyPluginName, e.Message), ErrorDialogTitleLabel);
            }
            catch (Exception e)
            {
                MessageBox.Show(String.Format(UnexpectedErrorLabel, plugin.FriendlyPluginName, e.Message), ErrorDialogTitleLabel);
            }
        }

        #endregion
    }
}
