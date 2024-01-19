#nullable enable

namespace FitEdit.Adapters.Sqlite;

public static class FileReferenceMapper
{
  public static Model.FileReference? MapModel(this FileReference? f) => f == null ? null : new(f.Name, null)
  {
    Id = f.Id,
  };

  public static FileReference? MapEntity(this Model.FileReference? f) => f == null ? null : new()
  {
    Id = f.Id,
    Name = f.Name,
  };
}