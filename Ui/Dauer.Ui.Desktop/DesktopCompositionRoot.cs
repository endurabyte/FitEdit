using Autofac;
using Dauer.Ui.Infra;

namespace Dauer.Ui.Desktop;

public class DesktopCompositionRoot : CompositionRoot
{
  protected override async Task ConfigureAsync(ContainerBuilder builder)
  {
    if (UseSupabase)
    {
      builder.RegisterType<SupabaseWebAuthenticator>().As<IWebAuthenticator>().SingleInstance();
    }
    else
    {
      builder.RegisterType<DesktopWebAuthenticator>().As<IWebAuthenticator>().SingleInstance();
    }

    await base.ConfigureAsync(builder);
  }
}