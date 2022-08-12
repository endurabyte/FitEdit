namespace Dauer.Model
{
  public static class Log
  {
    public static LogLevel Level { get; set; } = LogLevel.Debug;

    public static string Now => $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.ffffff}";

    public static void Error(object o) => RequireLevel(LogLevel.Error, () => Console.WriteLine($"{Now} [ERROR] {o}"));
    public static void Warn(object o) => RequireLevel(LogLevel.Warn, () => Console.WriteLine($"{Now} [WARN] {o}"));
    public static void Info(object o) => RequireLevel(LogLevel.Info, () => Console.WriteLine($"{Now} [INFO] {o}"));
    public static void Debug(object o) => RequireLevel(LogLevel.Debug, () => Console.WriteLine($"{Now} [Debug] {o}"));

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
