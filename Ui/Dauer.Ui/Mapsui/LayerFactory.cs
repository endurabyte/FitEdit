using BruTile;
using BruTile.Predefined;
using BruTile.Web;
using Dauer.Ui.Views;
using Mapsui.Layers;
using Mapsui.Nts;
using Mapsui.Styles;
using Mapsui.Tiling;
using Mapsui.Tiling.Layers;
using NetTopologySuite.Geometries;
using TileSource = Dauer.Model.TileSource;

namespace Dauer.Ui.Mapsui;

public class LayerFactory
{
  public static PersistentCache? DefaultCache { get; set; }

  public static string UserAgent = "fitedit";

  private static readonly Dictionary<TileSource, string> tokens_ = new()
  {
    [TileSource.Jawg] = "vANNdIJHPNGMEQyIhxvoWGgKQKP4kPUdaOtMDxqaNDTxere8oUgFk9vhHdHjq0n5",
    [TileSource.MapBox] = "pk.eyJ1Ijoic2xhdGVyMCIsImEiOiJjbGllZnRwd3cxMHJxM2tuYmw4MmNtOTAzIn0.E6GxSlg70MogL-sla15bgA",
  };

  public static ILayer CreateTileLayer(TileSource ts) => ts switch
  {
    TileSource.OpenStreetMap => OpenStreetMap.CreateTileLayer("fitedit"),
    _ => new TileLayer(GetSource(UrlFor(ts))) { Name = "Base Map" },
  };

  private static ITileSource GetSource(string url) => new HttpTileSource(new GlobalSphericalMercator(), url, 
    userAgent: UserAgent, 
    persistentCache: DefaultCache);

  private static string UrlFor(TileSource ts) => ts switch
  {
    TileSource.Jawg => $"https://tile.jawg.io/jawg-dark/{{z}}/{{x}}/{{y}}.png?access-token={GetToken(ts)}",
    TileSource.MapBox => $"https://api.mapbox.com/v4/mapbox.satellite/{{z}}/{{x}}/{{y}}@2x.png?access_token={GetToken(ts)}",
    _ => "",
  };

  private static string GetToken(TileSource ts) => tokens_.TryGetValue(ts, out string? token) ? token : "";

  /// <summary>
  /// Create a background that covers the entire map.
  /// </summary>
  public static ILayer CreateCanvas()
  {
    const double extent = 20037508; // Mercator projection world extent
    return new MemoryLayer
    {
      Name = "Canvas",
      Features = new[] { new GeometryFeature { Geometry = new Polygon(new LinearRing(new[]
        {
            new Coordinate(-extent, -extent),
            new Coordinate(extent, -extent),
            new Coordinate(extent, extent),
            new Coordinate(-extent, extent),
            new Coordinate(-extent, -extent),
        }))}},
      Style = new VectorStyle { Fill = new Brush { Color = FitColor.LicoriceBlack.Map() } },
    };
  }

  public static ILayer CreateLineString(Coordinate[] coords, string name, Avalonia.Media.Color color, int lineWidth) => new MemoryLayer
  {
    Features = new[]
    {
      new GeometryFeature
      {
        Geometry = new LineString(coords)
      }
    },
    Name = name,
    Style = new VectorStyle
    {
      Line = new(color.Map(), lineWidth)
    }
  };

  public static ILayer CreateCoordinates(Coordinate[] coords, string name, Avalonia.Media.Color color, int lineWidth) => new MemoryLayer
  {
    Features = new[]
    {
      new GeometryFeature
      {
        Geometry = new GeometryCollection(coords.Select(c => c.MapNtsPoint()).ToArray())
      }
    },
    Name = name,
    Style = new VectorStyle
    {
      Line = new(color.Map(), lineWidth)
    }
  };
  
  public static bool GetHasCoordinates(IEnumerable<ILayer> layers) => layers.Any(GetHasCoordinates);

  private static bool GetHasCoordinates(ILayer l) => l is MemoryLayer ml && ml.Features.Any(feat =>
    feat is GeometryFeature geom && geom.Geometry is LineString ls && !ls.IsEmpty
  );
}
