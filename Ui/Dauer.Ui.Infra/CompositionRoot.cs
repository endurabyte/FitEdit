using System.Runtime.InteropServices;
using Autofac;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;

namespace Dauer.Ui.Infra;

public interface ICompositionRoot
{
  IConfiguration? Config { get; }

  T Get<T>() where T : notnull;
  ICompositionRoot Build();

  void Register(Type @interface, Type implementation, bool singleton = false);
  void Register(Type @interface, object implementation, bool singleton = false);
  void Register(Type @interface, Func<object> factory, bool singleton = false);

  ICompositionRoot RegisterModule(Autofac.Module module);
}

public class CompositionRoot : ICompositionRoot
{
  public IConfiguration? Config { get; set; }

  private readonly ContainerBuilder builder_ = new();
  private IContainer? container_;

  public ICompositionRoot Build()
  {
    ConfigureAsync(builder_);
    container_ = builder_.Build();
    return this;
  }

  protected virtual Task ConfigureAsync(ContainerBuilder builder)
  {
    if (Config is null) { return Task.CompletedTask; }

    // Setup logging
    var logger = new LoggerConfiguration()
        .ReadFrom.Configuration(Config)
        .Enrich.FromLogContext()
        .CreateLogger();

    ILoggerFactory factory = new LoggerFactory().AddSerilog(logger);
    Microsoft.Extensions.Logging.ILogger? log = factory.CreateLogger("CompositionRoot");
    Dauer.Model.Log.Logger = log;
    Dauer.Model.Log.Info($"BaseDirectory: {AppContext.BaseDirectory}");
    Dauer.Model.Log.Info($"OSDescription: {RuntimeInformation.OSDescription}");

    // For e.g. mitmproxy
    //HttpClient.DefaultProxy = new WebProxy("http://192.168.1.163:8082/", false);

    builder_.RegisterInstance(factory).As<ILoggerFactory>();
    builder_.RegisterGeneric(typeof(Logger<>)).As(typeof(ILogger<>)).SingleInstance();

    return Task.CompletedTask;
  }

  public void Register(Type @interface, Type implementation, bool singleton = false) => builder_?.RegisterType(implementation).As(@interface).SingletonIf(singleton);
  public void Register(Type @interface, object implementation, bool singleton = false) => builder_?.RegisterInstance(implementation).As(@interface).SingletonIf(singleton);
  public void Register(Type @interface, Func<object> factory, bool singleton = false) => builder_?.Register(ctx => factory()).As(@interface).SingletonIf(singleton);

  public ICompositionRoot RegisterModule(Autofac.Module module)
  {
    if (builder_ is null) { return this; }
    builder_.RegisterModule(module);
    return this;
  }

  public T Get<T>() where T : notnull
  {
    if (container_ == null) { throw new InvalidOperationException($"Call {nameof(Build)} before calling {nameof(Get)}"); };
    return container_.Resolve<T>();
  }
}