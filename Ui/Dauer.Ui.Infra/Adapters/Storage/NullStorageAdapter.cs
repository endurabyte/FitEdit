using Dauer.Model;

namespace Dauer.Ui.Infra.Adapters.Storage;

public class NullStorageAdapter : IStorageAdapter
{
  public Task<BlobFile?> OpenFileAsync() => Task.FromResult((BlobFile?)null);
  public Task SaveAsync(BlobFile file) => Task.CompletedTask;
}
