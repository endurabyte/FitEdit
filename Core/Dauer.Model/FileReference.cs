#nullable enable

namespace Dauer.Model;

public class FileReference
{
  public string Id { get; set; } = $"{Guid.NewGuid()}";
  public string Name { get; set; }
  public byte[] Bytes { get; set; } = Array.Empty<byte>();

  public string Path => System.IO.Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FitEdit", "Files", $"{Id}", Name);

  public FileReference(string name, byte[]? bytes)
  {
    Name = name;
    Bytes = bytes ?? Array.Empty<byte>();
  }
}