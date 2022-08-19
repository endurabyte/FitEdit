using Dauer.Model;
using Dauer.Model.Factories;
using FundLog.Model.Extensions;
using System.Runtime.InteropServices;
using System.Text;

namespace Dauer.Adapters.Selenium;

public class ChromeDriverProcess
{
  public static bool IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

  public static readonly string Input = IsWindows ? "chromedriver.in.exe" : "chromedriver.in";
  public static readonly string Output = IsWindows ? "chromedriver.exe" : "chromedriver";

  private static string ProcessDir => Path.GetDirectoryName(Environment.ProcessPath);

  public static string FullInput => Path.Combine(ProcessDir, Input);
  public static string FullOutput => Path.Combine(ProcessDir, Output);

  /// <summary>
  /// Kill the current chromedriver process, randomize its fingerprinting signature, and make it executable.
  /// </summary>
  public static async Task Setup()
  {
    Log.Info("Setting up ChromeDriver...");

    // chromedriver.exe tends to keep running. Kill it
    await Kill().AnyContext();
    await Randomize().AnyContext();
    await MakeExecutable().AnyContext();
  }

  /// <summary>
  /// Some bot detectors fingerprint the javascript injected by Selenium.
  /// Randomize certain javascript signatures in the chromedriver executable to avoid bot detection by fingerprinting.
  /// </summary>
  public static async Task Randomize()
  {
    string replacement = StringFactory.Random(3);

    // Replace the magic string "cdc" in the process executable with a random one.
    byte[] from = Encoding.ASCII.GetBytes("cdc_");
    byte[] to = Encoding.ASCII.GetBytes($"{replacement}_");
    byte[] data = await File.ReadAllBytesAsync(FullInput).AnyContext();

    data = data.Replace(from, to).ToArray();

    await File.WriteAllBytesAsync(FullOutput, data).AnyContext();
  }

  /// <summary>
  /// Kill all instances of chromedriver.exe.
  /// </summary>
  public static async Task Kill() => await ProcessFactory.KillAll(Output).AnyContext();

  /// <summary>
  /// On Linux, make the chromdriver file executable
  /// </summary>
  public static async Task MakeExecutable()
  {
    if (IsWindows)
    {
      return;
    }

    await ProcessFactory.Execute("chmod", $"+x {FullOutput}").AnyContext();
  }
}