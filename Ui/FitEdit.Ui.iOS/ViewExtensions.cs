using Avalonia.iOS;

namespace FitEdit.Ui.iOS;

public static class ViewExtensions
{
  public static UIView? FindAvaloniaView(this UIView view)
  {
    var stack = new Stack<UIView>();
    stack.Push(view);

    while (stack.Count > 0)
    {
      UIView currentView = stack.Pop();

      if (currentView is AvaloniaView av) { return av; }

      foreach (UIView subView in currentView.Subviews)
      {
        stack.Push(subView);
      }
    }

    return null;
  }

  public static double GetViewRelativeBottom(this UIView view, UIView rootView)
  {
    CGPoint viewRelativeCoordinates = rootView.ConvertPointFromView(view.Frame.Location, view);
    double activeViewRoundedY = Math.Round(viewRelativeCoordinates.Y, 2);

    return activeViewRoundedY + view.Frame.Height;
  }

  public static bool IsKeyboardOverlapping(this UIView activeView, UIView rootView, CGRect keyboardFrame)
  {
    double activeViewBottom = activeView.GetViewRelativeBottom(rootView);
    nfloat pageHeight = rootView.Frame.Height;
    nfloat keyboardHeight = keyboardFrame.Height;

    bool isOverlapping = activeViewBottom >= (pageHeight - keyboardHeight);

    return isOverlapping;
  }
}