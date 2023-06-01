using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;

namespace Dauer.Ui.Adapters;

public class DesktopAdapter
{
  public static IClassicDesktopStyleApplicationLifetime? App => Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
}
