#nullable enable

using Avalonia.Platform.Storage;

namespace Dauer.Model;

public class FileReference
{
  public string Id { get; set; }
  public string Name { get; set; }
  public byte[] Bytes { get; set; } = Array.Empty<byte>();

  public FileReference(string name, byte[]? bytes)
  {
    bool nameIsGuid = Guid.TryParse(name, out _);
    Id = nameIsGuid ? name : $"{Guid.NewGuid()}";
    Name = nameIsGuid ? "file.fit" : name;
    Bytes = bytes ?? Array.Empty<byte>();
  }

  public static async Task<FileReference?> FromStorage(IStorageFile? file)
  {
    if (file == null) { return null; }
    using Stream stream = await file.OpenReadAsync();
    if (stream == null) return null;

    using var ms = new MemoryStream();
    await stream.CopyToAsync(ms);
    byte[] data = ms.ToArray();
    return new FileReference(file.Name, data);
  }
}