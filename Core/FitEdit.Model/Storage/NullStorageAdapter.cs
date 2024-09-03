using FitEdit.Model;

namespace FitEdit.Model.Storage;

public class NullStorageAdapter : IStorageAdapter
{
  public Task<FileReference?> OpenFileAsync() => Task.FromResult((FileReference?)new FileReference("fake", []));
  public Task SaveAsync(FileReference file) => Task.CompletedTask;
}
