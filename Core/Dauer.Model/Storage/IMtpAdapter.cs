#nullable enable

namespace Dauer.Model.Storage;

public interface IMtpAdapter
{
  void Scan();
  void GetFiles(PortableDevice dev);
}

public class NullMtpAdapter : IMtpAdapter
{
  public void Scan() { }
  public void GetFiles(PortableDevice dev) { }
}
