using System.Reflection;
using System.Runtime.InteropServices;

#if DEBUG

[assembly: AssemblyConfiguration("Debug")]
[assembly: AssemblyDescription("Debug build, https://github.com/jjw24/Wox")]
#else
[assembly: AssemblyConfiguration("Release")]
[assembly: AssemblyDescription("Release build, https://github.com/jjw24/Wox")]
#endif

[assembly: AssemblyCompany("Wox")]
[assembly: AssemblyProduct("Wox")]
[assembly: AssemblyCopyright("The MIT License (MIT)")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]
[assembly: ComVisible(false)]
[assembly: AssemblyVersion("1.53.24")]
[assembly: AssemblyFileVersion("1.53.24")]
[assembly: AssemblyInformationalVersion("1.53.24")]