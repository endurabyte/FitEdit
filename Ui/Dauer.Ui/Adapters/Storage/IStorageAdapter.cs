namespace Dauer.Ui.Adapters.Storage;

public interface IStorageAdapter
{
  Task<Models.File?> OpenFileAsync();
  Task SaveAsync(Models.File file);
}
