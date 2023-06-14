using Avalonia.Controls.ApplicationLifetimes;

namespace Dauer.Ui.Infra;

public static class LifetimeExtensions
{
  public static bool IsDesktop(this IApplicationLifetime? lt, out IClassicDesktopStyleApplicationLifetime? desktop)
  {
    desktop = lt as IClassicDesktopStyleApplicationLifetime;
    return desktop is not null;
  }

  /// <summary>
  /// Return true if we are running on a mobile device or browser
  /// </summary>
  public static bool IsMobile(this IApplicationLifetime? lt, out ISingleViewApplicationLifetime? mobile)
  {
    mobile = lt as ISingleViewApplicationLifetime;
    return mobile is not null;
  }
}
