using System.ComponentModel;
using Dauer.Model;

namespace Dauer.Ui.Desktop;

public class SquirrelLogger : Squirrel.SimpleSplat.ILogger
{
  public Squirrel.SimpleSplat.LogLevel Level { get; set; } = Squirrel.SimpleSplat.LogLevel.Debug;

  public void Write([Localizable(false)] string message, Squirrel.SimpleSplat.LogLevel logLevel)
  {
    switch (logLevel)
    {
      case Squirrel.SimpleSplat.LogLevel.Debug:
        Log.Debug(message);
        break;
      case Squirrel.SimpleSplat.LogLevel.Info:
        Log.Info(message);
        break;
      case Squirrel.SimpleSplat.LogLevel.Warn:
        Log.Warn(message);
        break;
      case Squirrel.SimpleSplat.LogLevel.Error:
        Log.Error(message);
        break;
      case Squirrel.SimpleSplat.LogLevel.Fatal:
        Log.Error(message);
        break;
      default:
        Log.Debug(message);
        break;
    }
  }
}