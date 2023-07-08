using Autofac;
using Dauer.Ui.Infra;

namespace Dauer.Ui.Desktop;

public class DesktopCompositionRoot : CompositionRoot
{
  protected override async Task ConfigureAsync(ContainerBuilder builder)
  {
    builder.RegisterType<DesktopWebAuthenticator>().As<IWebAuthenticator>().SingleInstance();
    await base.ConfigureAsync(builder);
  }
}