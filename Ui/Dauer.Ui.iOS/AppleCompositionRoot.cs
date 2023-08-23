using Autofac;

namespace Dauer.Ui.iOS;

public class AppleCompositionRoot : CompositionRoot
{
  protected override async Task ConfigureAsync(ContainerBuilder builder)
  {
    await base.ConfigureAsync(builder);
  }
}
