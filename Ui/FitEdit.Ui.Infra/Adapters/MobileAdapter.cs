using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.VisualTree;

namespace FitEdit.Ui.Infra.Adapters;

public class MobileAdapter
{
  public static ISingleViewApplicationLifetime? App => Application.Current?.ApplicationLifetime as ISingleViewApplicationLifetime;
  public static TopLevel? TopLevel => (App?.MainView?.GetVisualRoot() ?? null) as TopLevel;
}
