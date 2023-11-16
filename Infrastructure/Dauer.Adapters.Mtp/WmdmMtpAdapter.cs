using System.Reactive.Subjects;
using Dauer.Model;
using Dauer.Model.Mtp;
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

  public IObservable<LocalActivity> ActivityFound => activityFound_;
  private readonly ISubject<LocalActivity> activityFound_ = new Subject<LocalActivity>();
  private readonly IUsbEventAdapter usbEvents_;

  private static List<MediaDevice> GarminDevices_ => MediaDevice.GetDevices()
    .Where(d => d.Manufacturer.ToLower() == "garmin")
    .ToList();

  public WmdmMtpAdapter(IUsbEventAdapter usbEvents)
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
    foreach (MediaDevice device in GarminDevices_)
    {
      device.Connect();
      if (!device.IsConnected) { continue; }

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

        activityFound_.OnNext(act);
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