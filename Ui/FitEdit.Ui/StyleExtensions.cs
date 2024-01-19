using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;

namespace FitEdit.Ui;

public static class StyleExtensions
{
  public static void LoadStyles()
  {
    var style = (IStyle)AvaloniaXamlLoader.Load(new Uri("avares://FitEdit.Ui/Styles.axaml"), baseUri: null);

    FitColor.OceanBlue = style.GetColor("FitOceanBlue");
    FitColor.SkyBlue = style.GetColor("FitSkyBlue");
    FitColor.LicoriceBlack = style.GetColor("FitLicoriceBlack");
    FitColor.LeadBlack2 = style.GetColor("FitLeadBlack2");
    FitColor.LeadBlack = style.GetColor("FitLeadBlack");
    FitColor.MercuryGrey = style.GetColor("FitMercuryGrey");
    FitColor.SnowWhite = style.GetColor("FitSnowWhite");

    FitColor.TealCrayon = style.GetColor("FitTealCrayon");
    FitColor.BlueCrayon = style.GetColor("FitBlueCrayon");
    FitColor.PurpleCrayon = style.GetColor("FitPurpleCrayon");
    FitColor.OrangeCrayon = style.GetColor("FitOrangeCrayon");
    FitColor.PinkCrayon = style.GetColor("FitPinkCrayon");
    FitColor.RedCrayon = style.GetColor("FitRedCrayon");
    FitColor.GreenCrayon = style.GetColor("FitGreenCrayon");
    FitColor.LimeCrayon = style.GetColor("FitLimeCrayon");
    FitColor.TanCrayon = style.GetColor("FitTanCrayon");
    FitColor.PeachCrayon = style.GetColor("FitPeachCrayon");
  }

  public static Color GetColor(this IStyle style, string key) =>
    style.TryGetResource(key, null, out object? colorResource) && colorResource is Color c
      ? c
      : throw new ArgumentException($"Could not find style {key}");
}
