using Autofac;
using Avalonia.Controls.ApplicationLifetimes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;

namespace Dauer.Ui;

public interface ICompositionRoot
{
  T Get<T>() where T : notnull;
  ICompositionRoot Build(IApplicationLifetime? lifetime);

  void Register(Type @interface, Type implementation, bool singleton = false);
  void Register(Type @interface, object implementation, bool singleton = false);
  void Register(Type @interface, Func<object> factory, bool singleton = false);
}

public class CompositionRoot : ICompositionRoot
{
  public static ICompositionRoot? Instance { get; set; }

  private ContainerBuilder? builder_;
  private IContainer? container_;

  public ICompositionRoot Build(IApplicationLifetime? lifetime)
  {
    builder_ = new ContainerBuilder();

    // Load configuration
    IConfiguration configuration = new ConfigurationBuilder()
       .SetBasePath(AppContext.BaseDirectory) // exe directory
       .AddJsonFile("appsettings.json")
       .AddEnvironmentVariables()
       .Build();

    // Setup logging
    var logger = new LoggerConfiguration()
        .ReadFrom.Configuration(configuration)
        .Enrich.FromLogContext()
        .CreateLogger();

    ILoggerFactory factory = new LoggerFactory().AddSerilog(logger);
    Microsoft.Extensions.Logging.ILogger? log = factory.CreateLogger("Log");
    Dauer.Model.Log.Logger = log;

    builder_.RegisterInstance(factory).As<ILoggerFactory>();
    builder_.RegisterGeneric(typeof(Logger<>)).As(typeof(ILogger<>)).SingleInstance();

    builder_.AddDauer(lifetime);

    ConfigureAsync(builder_);
    container_ = builder_.Build();
    return this;
  }

  protected virtual Task ConfigureAsync(ContainerBuilder builder) => Task.CompletedTask;

  public void Register(Type @interface, Type implementation, bool singleton = false) => builder_?.RegisterType(implementation).As(@interface).SingletonIf(singleton);
  public void Register(Type @interface, object implementation, bool singleton = false) => builder_?.RegisterInstance(implementation).As(@interface).SingletonIf(singleton);
  public void Register(Type @interface, Func<object> factory, bool singleton = false) => builder_?.Register(ctx => factory()).As(@interface).SingletonIf(singleton);

  public T Get<T>() where T : notnull
  {
    if (container_ == null) { throw new InvalidOperationException($"Call {nameof(Build)} before calling {nameof(Get)}"); };
    return container_.Resolve<T>();
  }
}