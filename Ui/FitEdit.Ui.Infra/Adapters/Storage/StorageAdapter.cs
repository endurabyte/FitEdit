﻿using Avalonia.Platform.Storage;
using FitEdit.Model;
using FitEdit.Model.Storage;

namespace FitEdit.Ui.Infra.Adapters.Storage;

public abstract class StorageAdapter : IStorageAdapter
{
  protected abstract IStorageProvider? Provider_ { get; }

  public async Task<FileReference?> OpenFileAsync()
  {
    if (Provider_ == null) return null;
    IReadOnlyList<IStorageFile> files = await Provider_.OpenFilePickerAsync(new FilePickerOpenOptions { AllowMultiple = false });
    if (files.Count == 0) return null;
    using Stream stream = await files[0].OpenReadAsync();

    return await FileReference.FromStorage(files[0]);
  }

  public async Task SaveAsync(FileReference file)
  {
    if (Provider_ == null) return;
    using IStorageFile? sf = await Provider_.SaveFilePickerAsync(new FilePickerSaveOptions 
    { 
      SuggestedFileName = file.Name
    });
    if (sf == null) return;
    using Stream stream = await sf.OpenWriteAsync();
    await stream.WriteAsync(file.Bytes.AsMemory(0, file.Bytes.Length));
  }
}