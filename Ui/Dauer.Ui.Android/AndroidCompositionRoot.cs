using Autofac;
using Dauer.Ui.Infra;

namespace Dauer.Ui.Android;

public class AndroidCompositionRoot : CompositionRoot
{
  protected override async Task ConfigureAsync(ContainerBuilder builder)
  {
    await base.ConfigureAsync(builder);
  }
}
