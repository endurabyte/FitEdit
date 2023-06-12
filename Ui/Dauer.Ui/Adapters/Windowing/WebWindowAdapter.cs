using Avalonia.Controls;
using Dauer.Model;

namespace Dauer.Ui.Adapters.Windowing;

public class WebWindowAdapter : WindowAdapter, IWindowAdapter
{
  private static readonly WebControl main_ = new();

  public Control? Main => main_;

  static WebWindowAdapter()
  {
    if (!OperatingSystem.IsBrowser())
    {
      return;
    }

    WebWindowAdapterImpl.Resized.Subscribe(tuple =>
    {
      resized_.OnNext(tuple);
      main_.SetBounds(tuple.Item1, tuple.Item2);
    });

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
