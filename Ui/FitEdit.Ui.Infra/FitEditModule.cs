using System.Reflection;
using Autofac;
using Autofac.Core;
using Avalonia.Controls.ApplicationLifetimes;
using FitEdit.Adapters.GarminConnect;
using FitEdit.Adapters.Mtp;
using FitEdit.Adapters.Sqlite;
using FitEdit.Adapters.Strava;
using FitEdit.Data;
using FitEdit.Model;
using FitEdit.Model.Clients;
using FitEdit.Model.Data;
using FitEdit.Model.GarminConnect;
using FitEdit.Model.Services;
using FitEdit.Model.Storage;
using FitEdit.Model.Strava;
using FitEdit.Services;
using FitEdit.Ui.Infra.Adapters.Storage;
using FitEdit.Ui.Infra.Adapters.Windowing;
using FitEdit.Ui.Infra.Authentication;
using FitEdit.Ui.Infra.Supabase;
using FitEdit.Ui.Model;
using FitEdit.Ui.Model.Supabase;
using Microsoft.Extensions.Configuration;
using Usb.Events;

namespace FitEdit.Ui.Infra;

public class FitEditModule : Autofac.Module
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

  public FitEditModule(IApplicationLifetime? lifetime, IConfiguration? config)
  {
    lifetime_ = lifetime;
    config_ = config;
  }

  protected override void Load(ContainerBuilder builder)
  {
    Log.Debug($"In {typeof(FitEditModule).FullName}");
    Log.Debug("Scanning assemblies:");

    IEnumerable<Assembly> assemblies = GetAssemblies_(assembly => assembly.FullName?.StartsWith("FitEdit") ?? false);

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
      "RegexGenerator",
      "Resource+",
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
      //_ => typeof(FakeMtpAdapter), // For debugging
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