namespace Dauer.Ui.Infra.Adapters.Storage;

public interface IStorageAdapter
{
  Task<Model.File?> OpenFileAsync();
  Task SaveAsync(Model.File file);
}
