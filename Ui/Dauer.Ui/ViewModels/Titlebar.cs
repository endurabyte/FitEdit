using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace Dauer.Ui.ViewModels;

public class Titlebar : ReactiveObject
{
  public static Titlebar Instance { get; } = new();
  [Reactive] public string? Message { get; set; }
}
