using System.Reactive.Subjects;
using Dauer.Model;
using Dauer.Model.Services;
using Dauer.Model.Storage;
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
  private readonly IEventService events_;

  private static List<Device> GetGarminDevices(RawDeviceList deviceList)
  {
    const ushort GARMIN = 0x091e;

    return deviceList
      .Where(d => d.DeviceEntry.VendorId == GARMIN)
      .Select((RawDevice rd) =>
      {
        var device = new Device();
        return (device, isOpen: device.TryOpen(ref rd, cached: true));
      })
      .Where(pair => pair.isOpen)
      .Select(pair => pair.device)
      .ToList();
  }

  public LibUsbMtpAdapter(IEventService events)
  {
    events_ = events;

    events_.Subscribe<Usb.Events.UsbDevice>(EventKey.UsbDeviceAdded, HandleUsbDeviceAdded);
    _ = Task.Run(Scan);
  }

  private void HandleUsbDeviceAdded(Usb.Events.UsbDevice e)
  {
    // Linux notifies of every usb device on app startup.
    // macOS only reports the vendor ID.
    // So we filter by vendor ID.
    if (!UsbVendor.IsSupported(e.VendorID)) { return; }

    _ = Task.Run(Scan);
  }

  public void Scan()
  {
    using var deviceList = new RawDeviceList();
    var devices = GetGarminDevices(deviceList);
    foreach (Device device in devices)
    {
      Log.Info($"Found Garmin device {device.GetModelName() ?? "(unknown)"}");
      Log.Info($"Found device serial # {device.GetSerialNumber() ?? "unknown"}");
      events_.Publish(EventKey.MtpDeviceAdded, device.GetModelName());

      GetFiles(device);
      device.Dispose();
    }
  }

  private void GetFiles(Device device)
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

      List<LocalActivity> activities = files
        .Select((Nmtp.File file) =>
        {
          using var ms = new MemoryStream();
          bool ok = device.GetFile(file.ItemId, progress =>
          {
            Log.Info($"Download progress {file.FileName} {progress * 100:##.#}%");
            return false; // false => continue, true => cancel
          }, ms);

          return (file, bytes: ms.ToArray(), ok);
        })
        .Where(tup => tup.ok)
        .Select(tup =>
        {
          var act = new LocalActivity
          {
            Id = $"{Guid.NewGuid()}",
            File = new FileReference(tup.file.FileName, tup.bytes),
            Name = tup.file.FileName,
            Source = ActivitySource.Device,
            LastUpdated = DateTime.UtcNow,
          };

          events_.Publish(EventKey.MtpActivityFound, act);
          return act;
        })
        .ToList();
    }
  }
}