namespace Dauer.Ui.Adapters.Storage;

public class NullStorageAdapter : IStorageAdapter
{
  public Task<Models.File?> OpenFileAsync() => Task.FromResult((Models.File?)null);
  public Task SaveAsync(Models.File file) => Task.CompletedTask;
}
