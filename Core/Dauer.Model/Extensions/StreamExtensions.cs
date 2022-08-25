namespace Dauer.Model.Extensions;

public static class StreamExtensions
{
  public static byte[] ReadAllBytes(this Stream s)
  {
    using var ms = new MemoryStream();
    s.CopyTo(ms);
    return ms.ToArray();
  }
}