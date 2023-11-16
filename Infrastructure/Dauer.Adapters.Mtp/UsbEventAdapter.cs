using System.Reactive.Subjects;
using Usb.Events;

namespace Dauer.Adapters.Mtp;

public interface IUsbEventAdapter
{
  IObservable<object> UsbDeviceAdded { get; }
}

public class UsbEventAdapter : IUsbEventAdapter
{
  private readonly IUsbEventWatcher _usbEvents = new UsbEventWatcher();

  public IObservable<object> UsbDeviceAdded => usbDeviceAdded_;
  private readonly ISubject<object> usbDeviceAdded_ = new Subject<object>();

  public UsbEventAdapter()
  {
    _usbEvents.UsbDeviceAdded += HandleUsbDeviceAdded;
  }

  ~UsbEventAdapter()
  {
    _usbEvents.Dispose();
  }

  private void HandleUsbDeviceAdded(object? sender, UsbDevice e) => usbDeviceAdded_.OnNext(e);
}
