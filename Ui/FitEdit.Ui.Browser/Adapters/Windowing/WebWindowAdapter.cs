using Avalonia.Controls;
using FitEdit.Model;
using FitEdit.Ui.Model;

namespace FitEdit.Ui.Browser.Adapters.Windowing;

public class WebWindowAdapter : WindowAdapter, IWindowAdapter
{
  public Control? Main => null;

  static WebWindowAdapter()
  {
    if (!OperatingSystem.IsBrowser())
    {
      return;
    }

    WebWindowAdapterImpl.Resized.Subscribe(resized_.OnNext);

    WebWindowAdapterImpl.MessageReceived.Subscribe(msg =>
    {
      Log.Info($"Received message: {msg}");

      if (msg == "login:success")
      {
        CloseWindow("login");
      }
    });
  }

  public static void OpenWindow(string url, string windowName)
  {
    if (!OperatingSystem.IsBrowser())
    {
      return;
    }

    WebWindowAdapterImpl.OpenWindow(url, windowName);
  }

  public static void CloseWindow(string windowName)
  {
    if (!OperatingSystem.IsBrowser())
    {
      return;
    }

    WebWindowAdapterImpl.CloseWindow(windowName);
  }
}
