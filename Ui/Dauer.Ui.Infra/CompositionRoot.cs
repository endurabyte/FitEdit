using System.Reflection;
using System.Runtime.InteropServices;
using Autofac;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;

namespace Dauer.Ui.Infra;

public interface ICompositionRoot
{
  IConfiguration Config { get; }

  T Get<T>() where T : notnull;
  ICompositionRoot Build();

  void Register(Type @interface, Type implementation, bool singleton = false);
  void Register(Type @interface, object implementation, bool singleton = false);
  void Register(Type @interface, Func<object> factory, bool singleton = false);

  ICompositionRoot RegisterModule(Autofac.Module module);
}

public class CompositionRoot : ICompositionRoot
{
  public IConfiguration Config { get; private set; }

  private readonly ContainerBuilder builder_;
  private IContainer? container_;

  public CompositionRoot(IConfiguration config)
  {
    builder_ = new ContainerBuilder();
    Config = config;
  }

  public static ICompositionRoot Create()
  {
    string os = RuntimeInformation.OSDescription;
    os = os switch
    {
      _ when os.Contains("Windows", StringComparison.OrdinalIgnoreCase) => "Windows",
      _ when os.Contains("mac", StringComparison.OrdinalIgnoreCase) => "macOS",
      _ => "Linux",
    };

    var a = Assembly.GetExecutingAssembly();
    using var stream = a.GetManifestResourceStream("Dauer.Ui.Infra.appsettings.json");

    // Load configuration
    IConfiguration config = new ConfigurationBuilder()
     .SetBasePath(AppContext.BaseDirectory) // exe directory
     .AddJsonFile("appsettings.json", true)
     .AddJsonStream(stream!)
     .AddJsonFile($"appsettings.{os}.json", true)
     .AddEnvironmentVariables()
     .Build();

    // On Android and iOS, load appsettings from this assembly instead of file
    string logDir = Path.Combine(
      Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FitEdit-Data", "Logs");

    // Substitute {LogDir} with log directory
    foreach (int i in Enumerable.Range(0, 10))
    {
      string key = $"Serilog:WriteTo:{i}:Args:path";
      string? value = config.GetValue<string>(key);
      if (value != null)
      {
        config[key] = value.Replace("{LogDir}", logDir);
      }
    }

    return new CompositionRoot(config);
  }

  public ICompositionRoot Build()
  {
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

    builder_.RegisterInstance(factory).As<ILoggerFactory>();
    builder_.RegisterGeneric(typeof(Logger<>)).As(typeof(ILogger<>)).SingleInstance();

    ConfigureAsync(builder_);
    container_ = builder_.Build();
    return this;
  }

  protected virtual Task ConfigureAsync(ContainerBuilder builder) => Task.CompletedTask;

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