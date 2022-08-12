using Dauer.Infrastructure;
using Lamar.Microsoft.DependencyInjection;
using Typin;

namespace Dauer.Cli;

public class Program
{
  public static async Task Main() => await new CliApplicationBuilder()
    .AddCommandsFromThisAssembly()
    .UseLamar(services =>
    {
      var root = new CompositionRoot();
      services.AddLamar(root.Registry);
    })
    .UseExceptionHandler<ExceptionHandler>()
    .Build()
    .RunAsync();
}
