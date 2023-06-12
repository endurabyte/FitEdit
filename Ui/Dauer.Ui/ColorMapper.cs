using Avalonia.Media;

namespace Dauer.Ui;

public static class ColorMapper
{
  public static Mapsui.Styles.Color Map(this Color c) => new(c.R, c.G, c.B, c.A);
}
