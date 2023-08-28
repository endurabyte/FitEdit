using Avalonia.Platform.Storage;
using Dauer.Model;
using Dauer.Model.Storage;

namespace Dauer.Ui.Infra.Adapters.Storage;

public abstract class StorageAdapter : IStorageAdapter
{
  protected abstract IStorageProvider? Provider_ { get; }

  public async Task<FileReference?> OpenFileAsync()
  {
    if (Provider_ == null) return null;
    IReadOnlyList<IStorageFile> files = await Provider_.OpenFilePickerAsync(new FilePickerOpenOptions { AllowMultiple = false });
    if (files.Count == 0) return null;
    using Stream stream = await files[0].OpenReadAsync();
    if (stream == null) return null;

    using var ms = new MemoryStream();
    await stream.CopyToAsync(ms);
    byte[] data = ms.ToArray();
    return new FileReference(files[0].Name, data);
  }

  public async Task SaveAsync(FileReference file)
  {
    if (Provider_ == null) return;
    using IStorageFile? sf = await Provider_.SaveFilePickerAsync(new FilePickerSaveOptions { });
    if (sf == null) return;
    using Stream stream = await sf.OpenWriteAsync();
    await stream.WriteAsync(file.Bytes.AsMemory(0, file.Bytes.Length));
  }
}