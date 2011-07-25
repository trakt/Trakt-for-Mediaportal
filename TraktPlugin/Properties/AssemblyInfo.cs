using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
#if MP12
using MediaPortal.Common.Utils;
#endif

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("TraktPlugin")]
[assembly: AssemblyDescription("Adds Trakt Support to Mediaportal")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("TraktPlugin")]
[assembly: AssemblyCopyright("GNU General Public License v3")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("1d05cab8-cca7-4ab1-86de-8aed403d407c")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers 
// by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]
// Increment revision for MediaPortal 1.2 plugin so we dont have any issues with upgrading from MP1.1.
#if !MP12
[assembly: AssemblyVersion("1.0.4.0")]
[assembly: AssemblyFileVersion("1.0.4.0")]
#else
[assembly: AssemblyVersion("1.0.5.0")]
[assembly: AssemblyFileVersion("1.0.5.0")]
#endif

// MediaPortal Version Compatibility
#if MP12
[assembly: CompatibleVersion("1.1.6.27644")]
#endif