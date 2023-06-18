using Dauer.Model;

namespace Dauer.Ui.Infra.Adapters.Storage;

public interface IStorageAdapter
{
  Task<BlobFile?> OpenFileAsync();
  Task SaveAsync(BlobFile file);
}
