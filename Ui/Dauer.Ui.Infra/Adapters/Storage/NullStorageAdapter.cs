using Dauer.Model;

namespace Dauer.Ui.Infra.Adapters.Storage;

public class NullStorageAdapter : IStorageAdapter
{
  public Task<FileReference?> OpenFileAsync() => Task.FromResult((FileReference?)null);
  public Task SaveAsync(FileReference file) => Task.CompletedTask;
}
