namespace FitEdit.Model.Storage;

public interface IStorageAdapter
{
  Task<FileReference> OpenFileAsync();
  Task SaveAsync(FileReference file);
}
