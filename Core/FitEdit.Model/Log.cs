#nullable enable

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FitEdit.Model;

public static class Log
{
  public static ILogger Logger { get; set; } = NullLogger.Instance;

  public static void Error(object o) => Logger.LogError($"{o}");
  public static void Warn(object o) => Logger.LogWarning($"{o}");
  public static void Info(object o) => Logger.LogInformation($"{o}");
  public static void Debug(object o) => Logger.LogDebug($"{o}");
}