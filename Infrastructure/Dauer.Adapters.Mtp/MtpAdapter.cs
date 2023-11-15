using Dauer.Model;
using Dauer.Model.Mtp;
using MediaDevices;
using Nmtp;

namespace Dauer.Adapters.Mtp;

public class MtpAdapter : IMtpAdapter
{
  public MtpAdapter()
  { 
    _ = Task.Run(() =>
    {
      //if (OperatingSystem.IsWindows())
      //{
      //  ReadMtpDevicesWindows();
      //}
      //else
      {
        ReadMtpDevices();
      }
    });
  }

  // On Windows, WDM has a permanent connection to MTP devices so we can't connect.
  // We use a library which uses WDM to interact with MTP devices
  private static void ReadMtpDevicesWindows()
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

  private static void ReadMtpDevices()
  {
    //const ushort GARMIN = 0x091e;
    var deviceList = new RawDeviceList();
    //var garminDevices = deviceList.Where(d => d.DeviceEntry.VendorId == GARMIN).ToList();
    var garminDevices = deviceList.ToList();
    foreach (RawDevice rawDevice in garminDevices)
    {
      using var device = new Device();
      RawDevice rd = rawDevice;
      if (!device.TryOpen(ref rd, cached: true)) { continue; }

      Log.Info($"Found Garmin device {device.GetModelName() ?? "(unknown)"}");
      Log.Info($"Found device serial # {device.GetSerialNumber() ?? "unknown"}");

      IEnumerable<Nmtp.DeviceStorage> storages = device.GetStorages();

      foreach (var storage in storages)
      {
        IEnumerable<Nmtp.Folder> folders = device.GetFolderList(storage.Id);
        var activityFolder = folders.FirstOrDefault(folder => folder.Name == "Activity");

        if (activityFolder.FolderId <= 0) { continue; }

        List<Nmtp.File> files = device
          .GetFiles(progress =>
          {
            Log.Info($"List files progress: {progress * 100:##.#}%");
            return true;
          })
          .Where(file => file.ParentId == activityFolder.FolderId)
          .Where(file => file.FileName.EndsWith(".fit"))
          .Where(file => DateTime.UnixEpoch + TimeSpan.FromSeconds(file.ModificationDate) > DateTime.UtcNow - TimeSpan.FromDays(7))
          .ToList();

        string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FitEdit-Data", "MTP");
        Directory.CreateDirectory(dir);

        foreach (Nmtp.File file in files)
        {
          Console.WriteLine($"Found file {file.FileName}");

          using var fs = new FileStream($"{dir}/{file.FileName}", FileMode.Create);
          device.GetFile(file.ItemId, progress =>
          {
            Log.Info($"Download progress {file.FileName} {progress * 100:##.#}%");
            return false;
          }, fs);
        }
      }
    }
  }
}