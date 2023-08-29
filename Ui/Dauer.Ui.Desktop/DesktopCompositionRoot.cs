using Autofac;
using Dauer.Ui.Infra;
using Microsoft.Extensions.Configuration;

namespace Dauer.Ui.Desktop;

public class DesktopCompositionRoot : CompositionRoot
{
  public DesktopCompositionRoot(IConfiguration config) : base(config)
  {
  }

  protected override async Task ConfigureAsync(ContainerBuilder builder)
  {
    await base.ConfigureAsync(builder);
  }
}