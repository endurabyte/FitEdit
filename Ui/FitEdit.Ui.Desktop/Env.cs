using System.Runtime.InteropServices;

namespace FitEdit.Ui.Desktop;

internal static class Env
{
  public static string GetOS()
  {
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) { return "win"; }
    if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) { return "osx"; }
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) { return "linux"; }
    return "";
  }

  public static string GetArch() => RuntimeInformation.ProcessArchitecture switch
  {
    Architecture.Arm64 => "arm64",
    Architecture.X86 => "x86",
    _ => "x64",
  };
}