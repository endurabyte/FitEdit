using Avalonia.Media;

namespace FitEdit.Ui.Mapsui;

public static class ColorMapper
{
  public static global::Mapsui.Styles.Color Map(this Color c) => new(c.R, c.G, c.B, c.A);
}
