using Dauer.Model;
using Dauer.Model.Mtp;
using Nmtp;
using Usb.Events;

namespace Dauer.Adapters.Mtp;

/// <summary>
/// Interacts with MTP devices using <c>libmtp</c>.
/// 
/// <para/>
/// On Windows, WMDM has a permanent connection to portable devices.
/// This blocks <c>libmtp</c>, so <see cref="LibUsbMtpAdapter"/> won't work.
/// Instead, use <see cref="WmdmMtpAdapter"/>.
/// </summary>
public class LibUsbMtpAdapter : IMtpAdapter
{
  private readonly IUsbEventAdapter usbEvents_;

  private static List<Device> GarminDevices_
  {
    get
    {
      const ushort GARMIN = 0x091e;

      var deviceList = new RawDeviceList();

      return deviceList
        .Where(d => d.DeviceEntry.VendorId == GARMIN)
        .Select((RawDevice rd) =>
        {
          using var device = new Device();
          return (device, isOpen: device.TryOpen(ref rd, cached: true));
        })
        .Where(pair => pair.isOpen)
        .Select(pair => pair.device)
        .ToList();
    }
  }

  public LibUsbMtpAdapter(IUsbEventAdapter usbEvents)
  {
    usbEvents_ = usbEvents;
    usbEvents_.UsbDeviceAdded.Subscribe(e => HandleUsbDeviceAdded((UsbDevice)e));

    _ = Task.Run(Scan);
  }

  private void HandleUsbDeviceAdded(UsbDevice e)
  {
    _ = Task.Run(Scan);
  }

  public void Scan()
  {
    foreach (Device device in GarminDevices_)
    {
      Log.Info($"Found Garmin device {device.GetModelName() ?? "(unknown)"}");
      Log.Info($"Found device serial # {device.GetSerialNumber() ?? "unknown"}");

      GetFiles(device);
    }
  }

  private static void GetFiles(Device device)
  {
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