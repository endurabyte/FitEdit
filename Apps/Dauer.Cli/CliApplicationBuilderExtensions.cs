using Lamar;
using Lamar.Microsoft.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Typin;

namespace Dauer.Cli;

public static class CliApplicationBuilderExtensions
{
  public static CliApplicationBuilder UseLamar(this CliApplicationBuilder builder, Action<IServiceCollection> configure = null)
    => builder.UseLamar<ServiceRegistry>(configure);

  public static CliApplicationBuilder UseLamar<T>(this CliApplicationBuilder builder, Action<IServiceCollection> configure = null) where T : ServiceRegistry, new()
    => builder.UseServiceProviderFactory<ServiceRegistry>(new LamarServiceProviderFactory()).ConfigureServices(services =>
    {
      T registry = new();
      configure?.Invoke(registry);
      services.AddLamar(registry);
    });
}
