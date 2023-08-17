#nullable enable

namespace Dauer.Model;

public class BlobFile
{
  public long Id { get; set; }
  public string Name { get; set; } = string.Empty;
  public byte[] Bytes { get; set; } = Array.Empty<byte>();

  public BlobFile() { }

  public BlobFile(string name, byte[] bytes)
  {
    Name = name;
    Bytes = bytes;
  }
}