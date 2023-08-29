using Autofac;
using Dauer.Ui.Infra;
using Microsoft.Extensions.Configuration;

namespace Dauer.Ui.iOS;

public class AppleCompositionRoot : CompositionRoot
{
  public AppleCompositionRoot(IConfiguration config) : base(config)
  {
  }

  protected override async Task ConfigureAsync(ContainerBuilder builder)
  {
    await base.ConfigureAsync(builder);
  }
}
