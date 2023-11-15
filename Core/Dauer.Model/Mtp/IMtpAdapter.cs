#nullable enable

namespace Dauer.Model.Mtp;

public interface IMtpAdapter
{
  void Scan();
}

public class NullMtpAdapter : IMtpAdapter
{
  public void Scan() { }
}