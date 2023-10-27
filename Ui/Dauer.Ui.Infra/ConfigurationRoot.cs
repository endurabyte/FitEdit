using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Configuration;

namespace Dauer.Ui.Infra;

public static class ConfigurationRoot
{
  public static string DataDir => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FitEdit-Data");

  private const string exampleAppSettings = @"{
  // Uncomment and set this path to change where FitEdit storages its data
  // This can be useful if you want to use a file sync service 
  // e.g. DropBox, Google Drive, OneDrive, etc.
  //""StorageRoot"": ""C:/Users/<User>/FitEdit""
}";

  static ConfigurationRoot()
  {
    _ = Task.Run(WriteExampleAppSettings);
  }

  public static ICompositionRoot Bootstrap(CompositionRoot root)
  {
    string os = RuntimeInformation.OSDescription;
    os = os switch
    {
      _ when os.Contains("Windows", StringComparison.OrdinalIgnoreCase) => "Windows",
      _ when os.Contains("mac", StringComparison.OrdinalIgnoreCase) => "macOS",
      _ => "Linux",
    };

    Directory.CreateDirectory(DataDir);

    var a = Assembly.GetExecutingAssembly();
    using var stream = a.GetManifestResourceStream("Dauer.Ui.Infra.appsettings.json");

    // Load configuration
    IConfiguration config = new ConfigurationBuilder()
     .AddJsonStream(stream!)
     .SetBasePath(DataDir)
     .AddJsonFile("appsettings.json", true)
     .AddJsonFile($"appsettings.{os}.json", true)
     .AddEnvironmentVariables()
     .Build();

    string storageRoot = config.GetValue<string>("StorageRoot") ?? DataDir;

    // On Android and iOS, load appsettings from this assembly instead of file
    string logDir = Path.Combine(storageRoot, "Logs");

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

  private static async Task WriteExampleAppSettings()
  {
    string path = Path.Combine(DataDir, "appsettings.json");

    try
    {
      if (File.Exists(path)) { return; }
      await File.WriteAllTextAsync(path, exampleAppSettings);
    }
    catch (Exception)
    {
    }
  }
}

