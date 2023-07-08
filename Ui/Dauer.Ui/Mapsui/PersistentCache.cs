using BruTile;
using BruTile.Cache;
using Dauer.Model;
using Dauer.Model.Data;
using Dauer.Model.Extensions;

namespace Dauer.Ui.Mapsui;

public class PersistentCache : IPersistentCache<byte[]>
{
  private readonly string key_;
  private readonly IDatabaseAdapter db_;

  public PersistentCache(string key, IDatabaseAdapter db)
  {
    key_ = key;
    db_ = db;
  }

  private string KeyFor(TileIndex index) => $"{key_}:{index.Col}-{index.Row}-{index.Level}";
  public void Add(TileIndex index, byte[] tile) => db_.InsertAsync(new MapTile { Id = KeyFor(index), Bytes = tile }).Await();

#nullable disable // Null return value means Mapsui will fetch the tile from the tile server
  public byte[] Find(TileIndex index)
#nullable enable
  {
    MapTile tile = db_.GetMapTileAsync(KeyFor(index)).Await();
    return tile?.Bytes;
  }

  public void Remove(TileIndex index) => db_.DeleteAsync(new MapTile { Id = KeyFor(index) }).Await();
}
