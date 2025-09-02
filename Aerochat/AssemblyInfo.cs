using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;

[assembly: ThemeInfo(
    ResourceDictionaryLocation.None,            //where theme specific resource dictionaries are located
                                                //(used if a resource is not found in the page,
                                                // or application resource dictionaries)
    ResourceDictionaryLocation.SourceAssembly   //where the generic resource dictionary is located
                                                //(used if a resource is not found in the page,
                                                // app, or any theme specific resource dictionaries)
)]


[assembly: AssemblyFileVersion(AssemblyInfo.Version)]
[assembly: AssemblyVersion(AssemblyInfo.Version)]
[assembly: AssemblyTitle("Aerochat")]
[assembly: AssemblyDescription("Shows whether your friends are online and lets you have online conversations.")]
[assembly: Guid("8231C4FA-AD94-487A-BDBF-A936306AE009")]

static class AssemblyInfo
{
    public const string Version = "0.2.4";

#if AEROCHAT_RC
    /// <summary>
    /// The last version of Aerochat. Prerelease versions are treated as their previous version by the
    /// update checker so they get pushed to update to the final RTM build.
    /// </summary>
    public static readonly string RC_LAST_VERSION = "0.2.3";

    /// <summary>
    /// The revision string for the current prerelease version. This can be any arbitrary string such as
    /// "RC1" or "[User] Testing Release".
    /// </summary>
    public static readonly string RC_REVISION = "Stability Test Release";
#endif
}