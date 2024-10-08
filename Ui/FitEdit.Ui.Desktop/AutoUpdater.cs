﻿using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using FitEdit.Model;
using FitEdit.Ui.Infra;
using FitEdit.Ui.ViewModels;
using NuGet.Versioning;
using Squirrel;
using Squirrel.SimpleSplat;

namespace FitEdit.Ui.Desktop;

public class AutoUpdater
{
  private readonly INotifyService notifier_;

  public AutoUpdater(INotifyService notifier)
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

    if (System.Diagnostics.Debugger.IsAttached)
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
      using var mgr = new UpdateManager($"https://fitedit-releases.s3.us-east-1.amazonaws.com/{GetOS()}-{GetArch()}");
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

  private static string GetOS()
  {
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) { return "win"; }
    if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) { return "osx"; }
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) { return "linux"; }
    return "";
  }

  private static string GetArch() => RuntimeInformation.ProcessArchitecture switch
  {
    Architecture.Arm64 => "arm64",
    Architecture.X86 => "x86",
    _ => "x64",
  };
}