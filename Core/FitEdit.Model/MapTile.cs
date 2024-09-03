namespace FitEdit.Model;

public class MapTile
{
  public required string Id { get; set; }
  public byte[] Bytes { get; set; } = [];
}
