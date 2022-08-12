public static class StringConsoleExtensions
{
  private static readonly object lock_ = new();

  public static void WriteLine(this string s, ConsoleColor c) => WithForegroundColor(c, () => Console.WriteLine(s));

  public static void Write(this string s, ConsoleColor c) => WithForegroundColor(c, () => Console.Write(s));

  public static void WithForegroundColor(ConsoleColor c, Action a)
  {
    lock (lock_)
    {
      var fg = Console.ForegroundColor;
      Console.ForegroundColor = c;
      a();
      Console.ForegroundColor = fg;
    }
  }
}