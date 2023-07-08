using System.Reflection;
using Autofac;
using Avalonia.Controls.ApplicationLifetimes;
using Dauer.Adapters.Sqlite;
using Dauer.Model;
using Dauer.Model.Data;
using Dauer.Ui.Infra;
using Dauer.Ui.Infra.Adapters.Storage;
using Dauer.Ui.Infra.Adapters.Windowing;
using Dauer.Ui.ViewModels;

namespace Dauer.Ui;

public class DauerModule : Autofac.Module
{
  private readonly IApplicationLifetime? lifetime_;

  private IStorageAdapter Storage_ => true switch
  {
    _ when lifetime_.IsDesktop(out _) => new DesktopStorageAdapter(),
    _ when lifetime_.IsMobile(out _) => new MobileStorageAdapter(),
    _ => new NullStorageAdapter(),
  };

  private IWindowAdapter Window_ => true switch
  {
    _ when lifetime_.IsDesktop(out _) => new DesktopWindowAdapter(),
    _ when lifetime_.IsMobile(out _) => new MobileWindowAdapter(),
    _ => new NullWindowAdapter(),
  };

  public DauerModule(IApplicationLifetime? lifetime)
  {
    lifetime_ = lifetime;
  }

  protected override void Load(ContainerBuilder builder)
  {
    Log.Debug($"In {typeof(DauerModule).FullName}");
    Log.Debug("Scanning assemblies:");

    IEnumerable<Assembly> assemblies = GetAssemblies_(assembly => assembly.FullName?.StartsWith("Dauer") ?? false);

    foreach (Assembly assembly in assemblies)
    {
      Log.Debug($" {assembly}");
    }

    var ignoreList = new string[] 
    {
      "CompiledAvaloniaXaml",
      "DynamicSetters",
      "XamlClosure",
      "ProcessedByFody",
      "Dynastream"
    };

    // In matching assemblies...
    var types = builder
      .RegisterAssemblyTypes(assemblies.ToArray())
      .Where(t => t.IsClass)
      .Where(t => ignoreList.All(ignoreStr => !t.FullName?.Contains(ignoreStr) ?? true));

    // ...bind IThing to Thing
    types.As(t =>
    {
      Type? iface = t.GetInterfaces().FirstOrDefault(i => i.Name == "I" + t.Name);

      if (iface != null)
      {
        Log.Debug($"Binding {iface} to {t}");
      }

      return iface ?? t;
    });

    builder.RegisterType<NullWebAuthenticator>().As<IWebAuthenticator>();
    builder.RegisterInstance(Window_).As<IWindowAdapter>();
    builder.RegisterInstance(Storage_).As<IStorageAdapter>();

    string dbPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "fitedit", "fitedit.sqlite3");

    Directory.CreateDirectory(Directory.GetParent(dbPath)!.FullName);

    builder.RegisterType<SqliteAdapter>().As<IDatabaseAdapter>()
      .WithParameter("dbPath", dbPath)
      .SingleInstance();

    builder.RegisterType<FileService>().As<IFileService>()
      .SingleInstance();
    builder.RegisterType<MapViewModel>().As<IMapViewModel>()
      .WithParameter("tileSource", TileSource.Jawg);

    base.Load(builder);
  }

  private static IEnumerable<Assembly> GetAssemblies_(Func<Assembly, bool> predicate) =>
    AppDomain.CurrentDomain
      .GetAssemblies()
      .Where(assembly => predicate(assembly));
}