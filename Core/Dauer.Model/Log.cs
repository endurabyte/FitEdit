#nullable enable

namespace Dauer.Model
{
  public static class Log
  {
    public static List<Action<string?>> Sinks { get; } = new()
    {
      s => System.Diagnostics.Debug.WriteLine(s),
      Console.WriteLine
    };

    private static void Write(string s)
    {
      foreach (var sink in Sinks)
      {
        sink(s);
      }
    }

    public static LogLevel Level { get; set; } = LogLevel.Debug;

    public static string Now => $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.ffffff}";

    public static void Error(object o) => RequireLevel(LogLevel.Error, () => Write($"{Now} [ERROR] {o}"));
    public static void Warn(object o) => RequireLevel(LogLevel.Warn, () => Write($"{Now} [WARN] {o}"));
    public static void Info(object o) => RequireLevel(LogLevel.Info, () => Write($"{Now} [INFO] {o}"));
    public static void Debug(object o) => RequireLevel(LogLevel.Debug, () => Write($"{Now} [Debug] {o}"));

    public static void RequireLevel(LogLevel ll, Action a)
    {
      if (Level < ll)
      {
        return;
      }

      a();
    }
  }
}
