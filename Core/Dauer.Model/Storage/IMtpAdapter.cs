#nullable enable

using Dauer.Model.Services;

namespace Dauer.Model.Storage;

public interface IMtpAdapter
{
  void Scan();
  void GetFiles(PortableDevice dev, TimeSpan howFarBack = default);
}

public class NullMtpAdapter : IMtpAdapter
{
  private readonly IEventService events_;

  public NullMtpAdapter(IEventService events)
  {
    events_ = events;
  }

  public async void Scan() 
  {
    await Task.Delay(1000);
    events_.Publish(EventKey.MtpDeviceAdded, new PortableDevice("Fake Device", "123456-789"));
  }

  public void GetFiles(PortableDevice dev, TimeSpan howFarBack = default) 
  {
    events_.Publish(EventKey.MtpActivityFound, new LocalActivity
    {
      Id = $"{Guid.NewGuid()}",
      Name = "Fake activity"
    });
  }
}
