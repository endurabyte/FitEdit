using Dauer.Ui.Adapters;

namespace Dauer.Ui.Services;

public class Log
{
  public static void Info(string message)
  {
    // Not necessary; Console.WriteLine already writes to web browser console
    //if (OperatingSystem.IsBrowser())
    //{
    //  WebConsoleAdapter.Log(message);
    //}
    Model.Log.Info(message);
  }
}