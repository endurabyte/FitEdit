using Autofac;
using Dauer.Ui.Infra;
using Microsoft.Extensions.Configuration;

namespace Dauer.Ui.Android;

public class AndroidCompositionRoot : CompositionRoot
{
  public AndroidCompositionRoot(IConfiguration config) : base(config)
  {
  }

  protected override async Task ConfigureAsync(ContainerBuilder builder)
  {
    await base.ConfigureAsync(builder);
  }
}
