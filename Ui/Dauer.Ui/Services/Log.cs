namespace Dauer.Ui.Services;

public class Log
{
  public static void Write(string message)
  {
    //if (OperatingSystem.IsBrowser())
    //{
    //  WebConsole.Log(message);
    //}
    Console.WriteLine(message);
  }
}