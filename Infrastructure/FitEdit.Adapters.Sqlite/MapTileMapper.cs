namespace FitEdit.Adapters.Sqlite;
#nullable enable

public static class MapTileMapper
{
  public static Model.MapTile? MapModel(this MapTile mt) => mt == null ? null : new()
  {
    Id = mt.Id,
    Bytes = mt.Bytes,
  };

  public static MapTile MapEntity(this Model.MapTile mt) => new()
  {
    Id = mt.Id,
    Bytes = mt.Bytes,
  };
}