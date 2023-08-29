﻿using System.Diagnostics;
using Avalonia.Threading;
using Dauer.Model;
using Microsoft.Maui.ApplicationModel;

namespace Dauer.Ui.Infra;

public class Browser : Dauer.Model.Web.IBrowser
{
  public async Task OpenAsync(string? url)
  {
    if (url == null) { return; }

    try
    {
      Process.Start(url);
    }
    catch (Exception e)
    {
      // hack because of this: https://github.com/dotnet/corefx/issues/10361
      if (OperatingSystem.IsWindows())
      {
        url = url.Replace("&", "^&");
        Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
      }
      else if (OperatingSystem.IsLinux())
      {
        Process.Start("xdg-open", url);
      }
      else if (OperatingSystem.IsMacOS())
      {
        Process.Start("open", url);
      }
      else if (OperatingSystem.IsAndroid())
      {
#pragma warning disable CA1416 // Validate platform compatibility
        await Microsoft.Maui.ApplicationModel.Browser.Default.OpenAsync(url);
#pragma warning restore CA1416 // Validate platform compatibility

      }
      else if (OperatingSystem.IsIOS())
      {
        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
#pragma warning disable CA1416 // Validate platform compatibility
          await Microsoft.Maui.ApplicationModel.Browser.Default.OpenAsync(url, new BrowserLaunchOptions
          {
            Flags = BrowserLaunchFlags.PresentAsPageSheet
          });
#pragma warning restore CA1416 // Validate platform compatibility
        });
      }
      else
      {
        Log.Error(e);
      }
    }
  }

}