using Avalonia;
using Avalonia.iOS;
using Avalonia.ReactiveUI;
using Foundation;
using UIKit;
using Microsoft.Maui.ApplicationModel;
using System.Runtime.InteropServices;
using Dauer.Ui.Infra;

namespace Dauer.Ui.iOS;

// The UIApplicationDelegate for the application. This class is responsible for launching the 
// User Interface of the application, as well as listening (and optionally responding) to 
// application events from iOS.
[Register("AppDelegate")]
public partial class AppDelegate : AvaloniaAppDelegate<App>
{
  private bool _pageWasShiftedUp;
  private double _activeViewBottom;
  private bool _isKeyboardShown;
  protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
  {
    App.Root = ConfigurationRoot.Bootstrap(new AppleCompositionRoot());

    UIKeyboard.Notifications.ObserveWillShow((sender, args) => HandleKeyboardWillShow(args));
    UIKeyboard.Notifications.ObserveWillHide((sender, args) => HandleKeyboardWillHide(args));

    return base.CustomizeAppBuilder(builder)
      .AfterSetup(_ => Platform.Init(() => Window.RootViewController!))
      .UseReactiveUI();
  }

  // Based on https://github.com/adamped/SoftInput/blob/1b1029052f9d0f25b514c80c366d481f7b2d668f/SoftInput/SoftInput.iOS/Render/KeyboardRender.cs
  private void HandleKeyboardWillShow(UIKeyboardEventArgs args)
  {
    if (_isKeyboardShown) { return; }
    _isKeyboardShown = true;

    UIView? view = Window.FindAvaloniaView();
    if (view == null) { return; }

    bool isOverlapping = view.IsKeyboardOverlapping(Window, args.FrameEnd);
    if (!isOverlapping) { return; }

    ShiftViewUp(args.FrameEnd.Height, view);

    _pageWasShiftedUp = true;
  }

  private void HandleKeyboardWillHide(UIKeyboardEventArgs args)
  {
    if (!_isKeyboardShown) { return; }

    _isKeyboardShown = false;

    if (_pageWasShiftedUp)
    {
      UIView? activeView = Window.FindAvaloniaView();
      if (activeView == null) { return; }
      ShiftPageDown(activeView);
    }

    _pageWasShiftedUp = false;
  }

  /// <summary>
  /// Resize the view so that the keyboard does not cover it.
  /// </summary>
  private void ShiftViewUp(nfloat keyboardHeight, UIView view)
  {
    CGRect rect = view.Frame;

    _activeViewBottom = view.GetViewRelativeBottom(Window);
    double newHeight = rect.Height + GetShift(rect.Height, keyboardHeight, _activeViewBottom);

    view.Frame = new CGRect(rect.X, rect.Y, rect.Width, new NFloat(newHeight));
  }

  /// <summary>
  /// Restore the view to its full height.
  /// </summary>
  private void ShiftPageDown(UIView view) => view.Frame = Window.Frame;

  private static double GetShift(double pageHeight, nfloat keyboardHeight, double activeViewBottom) => 
    pageHeight - activeViewBottom - keyboardHeight;
}