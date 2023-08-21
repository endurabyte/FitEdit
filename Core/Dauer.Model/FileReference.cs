#nullable enable

namespace Dauer.Model;

public class FileReference
{
  public string Id { get; set; }
  public string Name { get; set; }
  public byte[] Bytes { get; set; } = Array.Empty<byte>();

  public string Path => System.IO.Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FitEdit-Data", "Files", $"{Id}", Name);

  public FileReference(string name, byte[]? bytes)
  {
    bool nameIsGuid = Guid.TryParse(name, out _);
    Id = nameIsGuid ? name : $"{Guid.NewGuid()}";
    Name = nameIsGuid ? "file.fit" : name;
    Bytes = bytes ?? Array.Empty<byte>();
  }
}