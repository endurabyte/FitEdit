using Dauer.Model;

namespace Dauer.Ui.Infra.Adapters.Storage;

public interface IStorageAdapter
{
  Task<FileReference?> OpenFileAsync();
  Task SaveAsync(FileReference file);
}
