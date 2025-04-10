﻿#nullable enable

using FitEdit.Model.Services;

namespace FitEdit.Model.Storage;

public interface IMtpAdapter
{
  void Scan();
  void GetFiles(PortableDevice dev, TimeSpan howFarBack = default);
}

public class FakeMtpAdapter : IMtpAdapter
{
  private readonly IEventService events_;

  public FakeMtpAdapter(IEventService events)
  {
    events_ = events;
  }

  public async void Scan() 
  {
    await Task.Delay(1000);
    events_.Publish(EventKey.MtpDeviceAdded, new PortableDevice("Fake Device", "123456-789"));
  }

  public void GetFiles(PortableDevice _, TimeSpan __) 
  {
    events_.Publish(EventKey.MtpActivityFound, new LocalActivity
    {
      Id = $"{Guid.NewGuid()}",
      Name = "Fake activity"
    });
  }
}

public class NullMtpAdapter : IMtpAdapter
{
  public NullMtpAdapter()
  {
  }

  public void Scan() 
  {
  }

  public void GetFiles(PortableDevice dev, TimeSpan howFarBack = default) 
  {
  }
}
