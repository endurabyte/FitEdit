namespace Dauer.Ui.Services;

public class Log
{
  public static void Info(string message)
  {
    //if (OperatingSystem.IsBrowser())
    //{
    //  WebConsole.Log(message);
    //}
    Model.Log.Info(message);
  }
}