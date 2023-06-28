using Avalonia.Media;
using OxyPlot;

namespace Dauer.Ui;

public static class FitColor
{
  public static Color OceanBlue { get; set; }
  public static Color SkyBlue { get; set; }
  public static Color LicoriceBlack { get; set; }
  public static Color LeadBlack { get; set; }
  public static Color LeadBlack2 { get; set; }
  public static Color MercuryGrey { get; set; }
  public static Color SnowWhite { get; set; }

  public static Color TealCrayon { get; set; }
  public static Color BlueCrayon { get; set; }
  public static Color PurpleCrayon { get; set; }
  public static Color OrangeCrayon { get; set; }
  public static Color PinkCrayon { get; set; }
  public static Color RedCrayon { get; set; }
  public static Color GreenCrayon { get; set; }
  public static Color LimeCrayon { get; set; }
  public static Color TanCrayon { get; set; }
  public static Color PeachCrayon { get; set; }

  public static OxyColor MapOxyColor(this Color c) => OxyColor.FromArgb(c.A, c.R, c.G, c.B);
  public static OxyColor MapOxyColor(this Color c, byte alpha) => OxyColor.FromArgb(alpha, c.R, c.G, c.B);
}
