using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;

namespace Dauer.Ui;

public static class StyleExtensions
{
  public static void LoadStyles()
  {
    var style = (IStyle)AvaloniaXamlLoader.Load(new Uri("avares://Dauer.Ui/Styles.axaml"), baseUri: null);

    FitColor.RedCrayon = style.GetColor("FitRedCrayon");
    FitColor.BlueCrayon = style.GetColor("FitBlueCrayon");
    FitColor.PinkCrayon = style.GetColor("FitPinkCrayon");
    FitColor.LimeCrayon = style.GetColor("FitLimeCrayon");
    FitColor.PurpleCrayon = style.GetColor("FitPurpleCrayon");
  }

  public static Color GetColor(this IStyle style, string key) =>
    style.TryGetResource(key, null, out object? colorResource) && colorResource is Color c
      ? c
      : Colors.Gray;
}
