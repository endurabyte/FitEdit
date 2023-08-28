namespace Dauer.Model.Storage;

public interface IStorageAdapter
{
  Task<FileReference> OpenFileAsync();
  Task SaveAsync(FileReference file);
}
