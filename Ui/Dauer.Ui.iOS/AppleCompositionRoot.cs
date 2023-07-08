using Dauer.Ui.Infra;
using Autofac;

namespace Dauer.Ui.iOS;

public class AppleCompositionRoot : CompositionRoot
{
  protected override async Task ConfigureAsync(ContainerBuilder builder)
  {
    builder.RegisterType<AppleWebAuthenticator>().As<IWebAuthenticator>();
    await base.ConfigureAsync(builder);
  }
}
