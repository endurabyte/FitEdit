using System.Reflection;
using Autofac;
using Avalonia.Controls.ApplicationLifetimes;
using Dauer.Adapters.GarminConnect;
using Dauer.Adapters.Sqlite;
using Dauer.Data;
using Dauer.Model;
using Dauer.Model.Clients;
using Dauer.Model.Data;
using Dauer.Model.GarminConnect;
using Dauer.Model.Storage;
using Dauer.Services;
using Dauer.Ui.Infra.Adapters.Storage;
using Dauer.Ui.Infra.Adapters.Windowing;
using Dauer.Ui.Infra.Authentication;
using Dauer.Ui.Infra.Supabase;
using Dauer.Ui.Model;
using Dauer.Ui.Model.Supabase;
using Microsoft.Extensions.Configuration;

namespace Dauer.Ui.Infra;

public class DauerModule : Autofac.Module
{
  private readonly IApplicationLifetime? lifetime_;
  private readonly IConfiguration? config_;

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

  public DauerModule(IApplicationLifetime? lifetime, IConfiguration? config)
  {
    lifetime_ = lifetime;
    config_ = config;
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
      "Dynastream",
      "RegexGenerator"
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

    string? apiUrl = config_?.GetValue<string>("Api:Url");
    string? projectId = config_?.GetValue<string>("Api:ProjectId");
    string? anonApiKey = config_?.GetValue<string>("Api:AnonKey");

    builder.RegisterType<SupabaseAdapter>().As<ISupabaseAdapter>()
      .WithParameter("url", $"https://{projectId}.supabase.co")
      .WithParameter("key", anonApiKey ?? "")
      .SingleInstance();
    builder.RegisterType<FitEditService>().As<IFitEditService>()
      .SingleInstance();
    builder.RegisterType<FitEditClient>().As<IFitEditClient>()
      .WithParameter("api", apiUrl ?? "")
      .SingleInstance();
    builder.RegisterType<GarminConnectClient>().As<IGarminConnectClient>();
    builder.RegisterType<SupabaseWebAuthenticator>().As<IWebAuthenticator>().SingleInstance();
    builder.RegisterInstance(Window_).As<IWindowAdapter>();
    builder.RegisterInstance(Storage_).As<IStorageAdapter>();

    string dbPath = Path.Combine(
      Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FitEdit-Data", "db", "fitedit.sqlite3");

    Directory.CreateDirectory(Directory.GetParent(dbPath)!.FullName);

    builder.RegisterType<SqliteAdapter>().As<IDatabaseAdapter>()
      .WithParameter("dbPath", dbPath)
      .SingleInstance();

    builder.RegisterType<FileService>().As<IFileService>()
      .SingleInstance();

    base.Load(builder);
  }

  private static IEnumerable<Assembly> GetAssemblies_(Func<Assembly, bool> predicate) =>
    AppDomain.CurrentDomain
      .GetAssemblies()
      .Where(assembly => predicate(assembly));
}