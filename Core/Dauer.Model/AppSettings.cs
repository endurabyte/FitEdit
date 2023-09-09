#nullable enable

namespace Dauer.Model;

public class AppSettings
{
  public DateTime? LastSynced { get; set; }

  public string? GarminUsername { get; set; }
  public string? GarminPassword { get; set; }
  public Dictionary<string, Cookie>? GarminCookies { get; set; }

  public string? StravaUsername { get; set; }
  public string? StravaPassword { get; set; }
  public Dictionary<string, Cookie>? StravaCookies { get; set; }
}
