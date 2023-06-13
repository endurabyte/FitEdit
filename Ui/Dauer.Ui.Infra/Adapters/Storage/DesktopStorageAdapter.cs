using Avalonia.Platform.Storage;

namespace Dauer.Ui.Infra.Adapters.Storage;

public class DesktopStorageAdapter : StorageAdapter
{
  protected override IStorageProvider? Provider_ => DesktopAdapter.App?.MainWindow?.StorageProvider;
}
