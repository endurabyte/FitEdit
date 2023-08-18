using System.Runtime.InteropServices;
using Dauer.Model;
using Dauer.Ui.ViewModels;
using NuGet.Versioning;
using Squirrel;
using Squirrel.SimpleSplat;

namespace Dauer.Ui.Desktop;

public class AutoUpdater
{ 
  public AutoUpdater()
  {
    SquirrelAwareApp.HandleEvents(onInitialInstall: HandleAppInstalled);
    SquirrelLocator.CurrentMutable.Register(() => new SquirrelLogger(), typeof(ILogger));
  }

  private static void HandleAppInstalled(SemanticVersion version, IAppTools tools)
  {
    Log.Info($"App Installed to {tools.AppDirectory}");

    if (OperatingSystem.IsWindows())
    {
      tools.CreateShortcutForThisExe(ShortcutLocation.StartMenuRoot | ShortcutLocation.Desktop);
      tools.CreateUninstallerRegistryEntry();
    }
  }

  public void WatchForUpdates(CancellationToken ct = default)
  {
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
    {
      Log.Info("Auto update not supported on Linux. Please use your package manager.");
      return;
    }

    if (System.Diagnostics.Debugger.IsAttached)
    {
      Log.Info("Skipping auto update check because debugger is attached.");
      return;
    }

    _ = Task.Run(async () =>
    {
      await Task.Delay(TimeSpan.FromMinutes(1), ct);

      while (!ct.IsCancellationRequested)
      {
        await CheckForUpdates(ct);
        await Task.Delay(TimeSpan.FromHours(1), ct);
      }
    }, ct);
  }

  private static async Task CheckForUpdates(CancellationToken ct = default)
  {
    Log.Info($"Checking for updates...");

    try
    {
      using var mgr = new UpdateManager("https://fitedit-releases.s3.us-east-1.amazonaws.com/");
      UpdateInfo updateInfo = await mgr.CheckForUpdate();

      if (!ct.IsCancellationRequested && updateInfo.ReleasesToApply.Any())
      {
        Log.Info($"Found {updateInfo.ReleasesToApply.Count} updates, applying...");
        await mgr.UpdateApp();

        // Notify user of update
        Titlebar.Instance.Message = "| Please restart to apply updates";

        // TODO provide e.g. a button to restart
        //UpdateManager.RestartApp();
      }
    }
    catch (Exception e)
    {
      Log.Error($"Problem checking or applying updates: {e}");
    }
  }
}