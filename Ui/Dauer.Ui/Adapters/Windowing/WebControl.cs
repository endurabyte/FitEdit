using Avalonia;
using Avalonia.Controls;

namespace Dauer.Ui.Adapters.Windowing;

public class WebControl : Control
{
  public void SetBounds(double width, double height)
  {
    Bounds = new Rect(new Size(width, height));
  }
}
