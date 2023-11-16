using System.Reactive.Subjects;
using Dauer.Model;
using Dauer.Model.Services;
using Dauer.Model.Storage;
using MediaDevices;
using Usb.Events;

namespace Dauer.Adapters.Mtp;

/// <summary>
/// Interacts with MTP devices using Windows Media Device Manager (WMDM).
/// 
/// <para/>
/// On Windows, WMDM has a permanent connection to portable devices.
/// This blocks libmtp, so <see cref="LibUsbMtpAdapter"/> won't work.
/// </summary>
public class WmdmMtpAdapter : IMtpAdapter
{
#pragma warning disable CA1416 // Validate platform compatibility. We already validated at injection.

  private readonly IEventService events_;

  private static List<MediaDevice> GarminDevices_ => MediaDevice.GetDevices()
    .Where(d => d.Manufacturer.ToLower() == "garmin")
    .Select(d => 
    {
      d.Connect(); 
      return d; 
    })
    .Where(d => d.IsConnected)
    .ToList();

  public WmdmMtpAdapter(IEventService events)
  {
    events_ = events;

    events_.Subscribe<UsbDevice>(EventKey.UsbDeviceAdded, HandleUsbDeviceAdded);
    _ = Task.Run(Scan);
  }

  private void HandleUsbDeviceAdded(UsbDevice e)
  {
    if (!e.VendorID.ToLower().Contains("091e"))
    {
      return;
    }
    _ = Task.Run(Scan);
  }

  public void Scan()
  {
    foreach (MediaDevice device in GarminDevices_)
    {
      Log.Info($"Found Garmin device {device.FriendlyName ?? "(unknown)"}");
      Log.Info($"Found device serial # {device.SerialNumber ?? "unknown"}");
      events_.Publish(EventKey.MtpDeviceAdded, device.FriendlyName);

      GetFiles(device);
    }
  }

  private void GetFiles(MediaDevice device)
  {
    MediaDirectoryInfo activityDir = device.GetDirectoryInfo("\\Internal storage/GARMIN/Activity");
    IEnumerable<MediaFileInfo> fitFiles = activityDir.EnumerateFiles("*.fit");

    List<MediaFileInfo> files = fitFiles
      .Where(f => f.LastWriteTime > DateTime.UtcNow - TimeSpan.FromDays(7))
      .ToList();

    List<LocalActivity> activities = files
      .Select((MediaFileInfo file) =>
      {
        using var ms = new MemoryStream();
        device.DownloadFile(file.FullName, ms);

        return (file, bytes: ms.ToArray());
      })
      .Select(tup =>
      {
        var act = new LocalActivity
        {
          Id = $"{Guid.NewGuid()}",
          File = new FileReference(tup.file.Name, tup.bytes),
          Name = tup.file.Name,
          Source = ActivitySource.Device,
          LastUpdated = DateTime.UtcNow,
        };

        events_.Publish(EventKey.MtpActivityFound, act);
        return act;
      })
      .ToList();

    foreach (MediaFileInfo file in files)
    {
      using var fs = new MemoryStream();
      device.DownloadFile(file.FullName, fs);
    }
  }

#pragma warning restore CA1416 
}