using System.Reflection;
using Autofac;
using Autofac.Core;
using Avalonia.Controls.ApplicationLifetimes;
using Dauer.Adapters.GarminConnect;
using Dauer.Adapters.Mtp;
using Dauer.Adapters.Sqlite;
using Dauer.Adapters.Strava;
using Dauer.Data;
using Dauer.Model;
using Dauer.Model.Clients;
using Dauer.Model.Data;
using Dauer.Model.GarminConnect;
using Dauer.Model.Services;
using Dauer.Model.Storage;
using Dauer.Model.Strava;
using Dauer.Services;
using Dauer.Ui.Infra.Adapters.Storage;
using Dauer.Ui.Infra.Adapters.Windowing;
using Dauer.Ui.Infra.Authentication;
using Dauer.Ui.Infra.Supabase;
using Dauer.Ui.Model;
using Dauer.Ui.Model.Supabase;
using Microsoft.Extensions.Configuration;
using Usb.Events;

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
    string? cryptoPassword = config_?.GetValue<string>("Crypto:Password");
    string? storageRoot = config_?.GetValue<string>("StorageRoot") ?? ConfigurationRoot.DataDir;

    builder.RegisterType<SupabaseAdapter>().As<ISupabaseAdapter>()
      .WithParameter("url", $"https://{projectId}.supabase.co")
      .WithParameter("key", anonApiKey ?? "")
      .SingleInstance();
    builder.RegisterType<FitEditService>().As<IFitEditService>()
      .SingleInstance();
    builder.RegisterType<FitEditClient>().As<IFitEditClient>()
      .WithParameter("api", apiUrl ?? "")
      .SingleInstance();
    builder.RegisterType<CryptoService>().As<ICryptoService>()
      .WithParameter("password", cryptoPassword ?? "")
      .SingleInstance();
    builder.RegisterType<NullCryptoService>()
      .Named<ICryptoService>("NullCrypto");

    Type mtpAdapter = 0 switch
    {
      _ when OperatingSystem.IsWindows() && OperatingSystem.IsWindowsVersionAtLeast(7) => typeof(WmdmMtpAdapter),
      _ when OperatingSystem.IsLinux() || OperatingSystem.IsMacOS() => typeof(LibUsbMtpAdapter),
      _ => typeof(NullMtpAdapter),
    };
    builder.RegisterType(mtpAdapter).As<IMtpAdapter>().SingleInstance();
    builder.RegisterType<UsbEventWatcher>().As<IUsbEventWatcher>().SingleInstance();
    builder.RegisterType<UsbEventAdapter>().As<IUsbEventAdapter>().SingleInstance();
    builder.RegisterType<TaskService>().As<ITaskService>().SingleInstance();
    builder.RegisterType<EventService>().As<IEventService>().SingleInstance();
    builder.RegisterType<StravaClient>().As<IStravaClient>().SingleInstance();
    builder.RegisterType<GarminConnectClient>().As<IGarminConnectClient>().SingleInstance();
    builder.RegisterType<SupabaseWebAuthenticator>().As<IWebAuthenticator>().SingleInstance();
    builder.RegisterInstance(Window_).As<IWindowAdapter>();
    builder.RegisterInstance(Storage_).As<IStorageAdapter>();

    string dbPath = Path.Combine(storageRoot, "db", "fitedit.sqlite3");

    Directory.CreateDirectory(Directory.GetParent(dbPath)!.FullName);

    builder.RegisterType<SqliteAdapter>().As<IDatabaseAdapter>()
      .WithParameter("dbPath", dbPath)
      .SingleInstance();

    {
      bool encryptFiles = false;
      var fileService = builder.RegisterType<FileService>().As<IFileService>()
        .WithParameter("storageRoot", storageRoot);

      if (!encryptFiles)
      {
        fileService.WithParameter(new ResolvedParameter(
          (pi, ctx) => pi.ParameterType == typeof(ICryptoService),
          (pi, ctx) => ctx.ResolveNamed<ICryptoService>("NullCrypto")));
      }

      fileService.SingleInstance();
    }

    base.Load(builder);
  }

  private static IEnumerable<Assembly> GetAssemblies_(Func<Assembly, bool> predicate) =>
    AppDomain.CurrentDomain
      .GetAssemblies()
      .Where(assembly => predicate(assembly));
}