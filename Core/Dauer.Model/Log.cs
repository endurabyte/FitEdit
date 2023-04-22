namespace Dauer.Model
{
  public static class Log
  {
    public static Action<string> Sink => Console.WriteLine;

    public static LogLevel Level { get; set; } = LogLevel.Debug;

    public static string Now => $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.ffffff}";

    public static void Error(object o) => RequireLevel(LogLevel.Error, () => Sink($"{Now} [ERROR] {o}"));
    public static void Warn(object o) => RequireLevel(LogLevel.Warn, () => Sink($"{Now} [WARN] {o}"));
    public static void Info(object o) => RequireLevel(LogLevel.Info, () => Sink($"{Now} [INFO] {o}"));
    public static void Debug(object o) => RequireLevel(LogLevel.Debug, () => Sink($"{Now} [Debug] {o}"));

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
