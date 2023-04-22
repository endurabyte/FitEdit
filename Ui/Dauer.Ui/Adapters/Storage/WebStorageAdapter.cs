namespace Dauer.Ui.Adapters.Storage;

public class WebStorageAdapter : IStorageAdapter
{
  public async Task<Models.File?> OpenFileAsync() => true switch
  {
    true when OperatingSystem.IsBrowser() => (await WebStorageAdapterImpl.OpenFileAsync()).Map(),
    _ => null,
  };

  public async Task SaveAsync(Models.File file)
  {
    await Task.Run(() =>
    {
      if (!OperatingSystem.IsBrowser())
      {
        return;
      }
      WebStorageAdapterImpl.DownloadByteArray(file.Name, file.Bytes);
    });
  }
}