using Autofac;
using Dauer.Ui.Infra;

namespace Dauer.Ui.Desktop;

public class DesktopCompositionRoot : CompositionRoot
{
  protected override async Task ConfigureAsync(ContainerBuilder builder)
  {
    await base.ConfigureAsync(builder);
  }
}