using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Configuration;

namespace Dauer.Ui.Infra;

public static class ConfigurationRoot
{
  public static ICompositionRoot Bootstrap(CompositionRoot root)
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

    root.Config = config;
    return root;
  }
}
