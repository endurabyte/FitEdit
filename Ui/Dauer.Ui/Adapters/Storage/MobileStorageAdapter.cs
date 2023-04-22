using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;

namespace Dauer.Ui.Adapters.Storage;

public class MobileStorageAdapter : StorageAdapter
{
  private static ISingleViewApplicationLifetime? App_ => Application.Current?.ApplicationLifetime as ISingleViewApplicationLifetime;
  private static TopLevel? TopLevel_ => (App_?.MainView?.GetVisualRoot() ?? null) as TopLevel;
  protected override IStorageProvider? Provider_ => TopLevel_?.StorageProvider;
}
