using Dauer.Model.Mtp;
using MediaDevices;

namespace Dauer.Adapters.Mtp;

public class WdmMtpAdapter : IMtpAdapter
{
  public WdmMtpAdapter()
  {
    _ = Task.Run(Scan);
  }

  // On Windows, WDM has a permanent connection to MTP devices so we can't connect.
  // We use a library which uses WDM to interact with MTP devices
  public void Scan()
  {
    if (!OperatingSystem.IsWindows() || !OperatingSystem.IsWindowsVersionAtLeast(7)) { return; }

#pragma warning disable CA1416 // Validate platform compatibility. We already validated.
    List<MediaDevice> devices = MediaDevice.GetDevices().Where(d => d.Manufacturer.ToLower() == "garmin").ToList();

    string targetDir = $"C:/Users/doug/AppData/Local/FitEdit-Data/MTP";
    Directory.CreateDirectory(targetDir);

    foreach (MediaDevice device in devices)
    {
      device.Connect();
      if (!device.IsConnected) { continue; }

      MediaDirectoryInfo activityDir = device.GetDirectoryInfo("\\Internal storage/GARMIN/Activity");
      IEnumerable<MediaFileInfo> fitFiles = activityDir.EnumerateFiles("*.fit");

      //string[] files = device.GetFiles(@"\\Internal storage/GARMIN/Activity");
      //foreach (string file in files)
      //{
      //  using var fs = new FileStream($"{targetDir}/{Path.GetFileName(file)}", FileMode.Create);
      //  device.DownloadFile(file, fs);
      //}

      List<MediaFileInfo> files = fitFiles
        .Where(f => f.LastWriteTime > DateTime.UtcNow - TimeSpan.FromDays(7))
        .ToList();

      foreach (var file in files)
      {
        using var fs = new FileStream($"{targetDir}/{file.Name}", FileMode.Create);
        device.DownloadFile(file.FullName, fs);
      }
    }

#pragma warning restore CA1416 
  }
}