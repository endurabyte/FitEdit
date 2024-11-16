using System.Diagnostics;
using System.Runtime.InteropServices;
using FitEdit.Model;
using FitEdit.Ui.Infra;
using NuGet.Versioning;
using Squirrel;
using Squirrel.SimpleSplat;

namespace FitEdit.Ui.Desktop;

public class SquirrelAutoUpdater
{
  private readonly INotifyService notifier_;

  public SquirrelAutoUpdater(INotifyService notifier)
  {
    notifier_ = notifier;
    
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
    {
      Log.Info("Auto update not supported on Linux. Please use your package manager.");
      return;
    }

    SquirrelAwareApp.HandleEvents(onInitialInstall: HandleAppInstalled);
    SquirrelLocator.CurrentMutable.Register(() => new SquirrelLogger(), typeof(ILogger));
  }

  private void NotifyUser(string message, Action restartApp) => notifier_.NotifyUser(message, "", restartApp);

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

    if (Debugger.IsAttached)
    {
      Log.Info("Skipping auto update check because debugger is attached.");
      return;
    }

    _ = Task.Run(async () =>
    {
      await Task.Delay(TimeSpan.FromSeconds(30), ct);

      while (!ct.IsCancellationRequested)
      {
        await CheckForUpdates(ct);
        await Task.Delay(TimeSpan.FromMinutes(30), ct);
      }
    }, ct);
  }

  private async Task CheckForUpdates(CancellationToken ct = default)
  {
    Log.Info($"Checking for updates...");

    try
    {
      var mgr = new UpdateManager($"https://fitedit-releases.s3.us-east-1.amazonaws.com/{Env.GetOS()}-{Env.GetArch()}");
      UpdateInfo updateInfo = await mgr.CheckForUpdate();

      if (!ct.IsCancellationRequested && updateInfo.ReleasesToApply.Any())
      {
        Log.Info($"Found {updateInfo.ReleasesToApply.Count} updates, applying...");
        ReleaseEntry? entry = await mgr.UpdateApp();

        if (entry == null) { return; }

        // Notify user of update
        NotifyUser($"Please relaunch to update to version {entry.Version}.", () =>
        {
          Process.Start(Environment.ProcessPath!);
          Environment.Exit(0);
        });
      }
    }
    catch (Exception e)
    {
      Log.Error($"Problem checking or applying updates: {e}");
    }
  }
}