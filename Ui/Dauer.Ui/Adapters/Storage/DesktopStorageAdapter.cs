using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;

namespace Dauer.Ui.Adapters.Storage;

public class DesktopStorageAdapter : StorageAdapter
{
  private static IClassicDesktopStyleApplicationLifetime? App_ => Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
  protected override IStorageProvider? Provider_ => App_?.MainWindow?.StorageProvider;

}
