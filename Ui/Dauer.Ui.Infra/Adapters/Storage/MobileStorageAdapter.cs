using Avalonia.Platform.Storage;

namespace Dauer.Ui.Infra.Adapters.Storage;

public class MobileStorageAdapter : StorageAdapter
{
  protected override IStorageProvider? Provider_ => MobileAdapter.TopLevel?.StorageProvider;
}