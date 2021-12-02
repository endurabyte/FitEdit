namespace Dauer.Model
{
  public static class Log
  {
    public static string Now => $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.ffffff}";

    public static void Debug(object o) => Console.WriteLine($"{Now} [Debug] {o}");
    public static void Info(object o) => Console.WriteLine($"{Now} [INFO] {o}");
    public static void Warn(object o) => Console.WriteLine($"{Now} [WARN] {o}");
    public static void Error(object o) => Console.WriteLine($"{Now} [ERROR] {o}");
  }
}
