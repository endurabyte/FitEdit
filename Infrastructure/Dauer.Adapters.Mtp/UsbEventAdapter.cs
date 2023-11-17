using Dauer.Model;
using Dauer.Model.Services;
using Dauer.Model.Storage;
using Usb.Events;

namespace Dauer.Adapters.Mtp;

public class UsbEventAdapter : IUsbEventAdapter
{
  private readonly IEventService events_;
  private readonly IUsbEventWatcher watcher_;

  public UsbEventAdapter(IEventService events, IUsbEventWatcher watcher)
  {
    events_ = events;
    watcher_ = watcher;
    watcher_.UsbDeviceAdded += HandleUsbDeviceAdded;
  }

  ~UsbEventAdapter()
  {
    watcher_.Dispose();
  }

  private void HandleUsbDeviceAdded(object? sender, Usb.Events.UsbDevice e) => events_.Publish(EventKey.UsbDeviceAdded, e);
}
