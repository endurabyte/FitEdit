using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace FitEdit.Model;

public class AppSettings : ReactiveObject
{
  public DateTime? LastSynced { get; set; }

  public string? GarminUsername { get; set; }
  public string? GarminPassword { get; set; }
  public string? GarminSsoId { get; set; }
  public string? GarminSessionId { get; set; }
  [Reactive] public Dictionary<string, Cookie>? GarminCookies { get; set; }

  public string? StravaUsername { get; set; }
  public string? StravaPassword { get; set; }
  public Dictionary<string, Cookie>? StravaCookies { get; set; }
}
