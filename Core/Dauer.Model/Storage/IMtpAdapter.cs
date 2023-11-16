#nullable enable

namespace Dauer.Model.Storage;

public interface IMtpAdapter
{
  void Scan();
}

public class NullMtpAdapter : IMtpAdapter
{
  public void Scan() { }
}