using Autofac;
using Dauer.Ui.Infra;

namespace Dauer.Ui.Desktop;

public class DesktopCompositionRoot : CompositionRoot
{
  protected override async Task ConfigureAsync(ContainerBuilder builder)
  {
    //builder.RegisterType<DesktopWebAuthenticator>().As<IWebAuthenticator>().SingleInstance();
    builder.RegisterType<SupabaseWebAuthenticator>().As<IWebAuthenticator>()
      .WithParameter("url", "https://rvhexrgaujaawhgsbzoa.supabase.co")
      .WithParameter("key", "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6InJ2aGV4cmdhdWphYXdoZ3Niem9hIiwicm9sZSI6ImFub24iLCJpYXQiOjE2OTA4ODIyNzEsImV4cCI6MjAwNjQ1ODI3MX0.motLGzxEKBK81K8C6Ll8-8szi6WgNPBT2ADkCn6jYTk")
      .SingleInstance();
    await base.ConfigureAsync(builder);
  }
}