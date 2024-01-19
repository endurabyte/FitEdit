namespace FitEdit.Model.Extensions;

public static class StreamExtensions
{
  public static byte[] ReadAllBytes<T>(this T s) where T : Stream
  {
    using var ms = new MemoryStream();
    s.CopyTo(ms);
    return ms.ToArray();
  }
}