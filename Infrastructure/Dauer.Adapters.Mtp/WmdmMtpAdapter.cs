using System.Data;
using Dauer.Model;
using Dauer.Model.Extensions;
using Dauer.Model.Services;
using Dauer.Model.Storage;
using MediaDevices;

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

  private readonly SemaphoreSlim scanSem_ = new(1, 1);
  private readonly Dictionary<string, (MediaDevice device, DateTime addedAt)> devices_ = new();

  public WmdmMtpAdapter(IEventService events)
  {
    events_ = events;

    events_.Subscribe<Usb.Events.UsbDevice>(EventKey.UsbDeviceAdded, HandleUsbDeviceAdded);
  }

  public void Scan() =>
    _ = Task.Run(async () =>
       await scanSem_.RunAtomically(async () =>
         await PollForDevices(TimeSpan.FromSeconds(30)), $"{nameof(WmdmMtpAdapter)}.{nameof(PollForDevices)}"));

  public void GetFiles(PortableDevice dev, TimeSpan howFarBack = default) 
  {
    if (!devices_.TryGetValue(dev.Id, out (MediaDevice device, DateTime addedAt) pair)) { return; }
    GetFiles(pair.device, howFarBack);
  }

  private async Task PollForDevices(TimeSpan timeout)
  {
    await Folly.RepeatAsync(() =>
    {
      List<MediaDevice> devices = GetSupportedDevices();

      if (!devices.Any())
      {
        return false;
      }

      foreach (MediaDevice device in devices)
      {
        HandleMtpDeviceAdded(device);
      }
      return true;
    }, timeout);
  }

  private void HandleUsbDeviceAdded(Usb.Events.UsbDevice e)
  {
    if (!UsbVendor.IsSupported(e.VendorID)) { return; }

    _ = Task.Run(Scan);
  }

  /// <summary>
  /// WMDM sometimes raises duplicate add notifications.
  /// Return true if the device of the given id was added recently.
  /// </summary>
  private bool Debounce(string id) => devices_.TryGetValue(id, out var pair) 
    && DateTime.UtcNow - pair.addedAt < TimeSpan.FromSeconds(15);

  private void HandleMtpDeviceAdded(MediaDevice device)
  {
    PortableDevice dev = new(device.FriendlyName, device.SerialNumber);

    // Added too recently
    if (Debounce(dev.Id)) { return; }

    devices_[dev.Id] = (device, DateTime.UtcNow);
    events_.Publish(EventKey.MtpDeviceAdded, dev);
  }

  private static List<MediaDevice> GetSupportedDevices() => MediaDevice.GetDevices()
    .Where(d => d.Manufacturer.ToLower() == "garmin")
    .Select(d =>
    {
      d.Connect();
      return d;
    })
    .Where(d => d.IsConnected)
    .ToList();

  private void GetFiles(MediaDevice device, TimeSpan howFarBack)
  {
    MediaDirectoryInfo activityDir = device.GetDirectoryInfo("\\Internal storage/GARMIN/Activity");
    IEnumerable<MediaFileInfo> fitFiles = activityDir.EnumerateFiles("*.fit");

    List<MediaFileInfo> files = fitFiles
      .Where(f => f.LastWriteTime > DateTime.UtcNow - howFarBack)
      .OrderByDescending(f => f.LastWriteTime)
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