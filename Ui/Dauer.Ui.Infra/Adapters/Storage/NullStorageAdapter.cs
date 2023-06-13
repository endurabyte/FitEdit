namespace Dauer.Ui.Infra.Adapters.Storage;

public class NullStorageAdapter : IStorageAdapter
{
  public Task<Model.File?> OpenFileAsync() => Task.FromResult((Model.File?)null);
  public Task SaveAsync(Model.File file) => Task.CompletedTask;
}
