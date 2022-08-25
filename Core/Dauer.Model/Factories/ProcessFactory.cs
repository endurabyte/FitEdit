using Dauer.Model.Extensions;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Dauer.Model.Factories;

public static class ProcessFactory
{
  private static string KillProc => RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "taskkill" : "pkill";
  private static string KillArgs => RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "/F /IM " : "";

  public static async Task Execute(string exe, string args)
  {
    var info = new ProcessStartInfo(exe, args)
    {
      CreateNoWindow = true,
      UseShellExecute = false
    };

    await Process.Start(info)!.WaitForExitAsync().AnyContext();
  }

  public static async Task KillAll(string processName) => await Execute(KillProc, $"{KillArgs} {processName}").AnyContext();
}