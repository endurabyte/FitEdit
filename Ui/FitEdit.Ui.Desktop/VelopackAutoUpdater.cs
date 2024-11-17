using System.Diagnostics;
using System.Runtime.InteropServices;
using FitEdit.Model;
using FitEdit.Ui.Infra;
using Velopack;

namespace FitEdit.Ui.Desktop;

public class VelopackAutoUpdater(INotifyService notifier)
{
  private void NotifyUser(string message, Action restartApp) => notifier.NotifyUser(message, "", restartApp);

  public void WatchForUpdates(CancellationToken ct = default)
  {
    if (Debugger.IsAttached)
    {
      Log.Info("Skipping auto update check because debugger is attached.");
      return;
    }

    _ = Task.Run(async () =>
    {
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
      UpdateInfo? updateInfo = await mgr.CheckForUpdatesAsync();
      
      if (updateInfo is null) { return; }

      Log.Info($"Found updates, applying...");

      await mgr.DownloadUpdatesAsync(updateInfo, cancelToken: ct);

      // Notify user of update
      NotifyUser($"Please relaunch to update to version {updateInfo.TargetFullRelease.Version}.", () =>
      {
        Process.Start(Environment.ProcessPath!);
        Environment.Exit(0);
      });
    }
    catch (Exception e)
    {
      Log.Error($"Problem checking or applying updates: {e}");
    }
  }
}
