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
    public const string Version = "0.2.2";
}