using System;

namespace Dauer.Model
{
  public static class Log
  {
    public static void Info(object o) => Console.WriteLine($"{DateTime.UtcNow} [INFO] {o}");
    public static void Warn(object o) => Console.WriteLine($"{DateTime.UtcNow} [WARN] {o}");
    public static void Error(object o) => Console.WriteLine($"{DateTime.UtcNow} [ERROR] {o}");
  }
}
