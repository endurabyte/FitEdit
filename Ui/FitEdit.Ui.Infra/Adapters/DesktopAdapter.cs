using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;

namespace FitEdit.Ui.Infra.Adapters;

public class DesktopAdapter
{
  public static IClassicDesktopStyleApplicationLifetime? App => Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
}
