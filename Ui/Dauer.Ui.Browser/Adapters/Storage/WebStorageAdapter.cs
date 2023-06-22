﻿using Dauer.Model;
using Dauer.Ui.Infra.Adapters.Storage;

namespace Dauer.Ui.Browser.Adapters.Storage;

public class WebStorageAdapter : IStorageAdapter
{
  public async Task<BlobFile?> OpenFileAsync() => true switch
  {
    true when OperatingSystem.IsBrowser() => (await WebStorageAdapterImpl.OpenFileAsync()).Map(),
    _ => null,
  };

  public async Task SaveAsync(BlobFile file)
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