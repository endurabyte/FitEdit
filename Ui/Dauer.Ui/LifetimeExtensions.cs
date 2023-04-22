using Avalonia.Controls.ApplicationLifetimes;

namespace Dauer.Ui;

public static class LifetimeExtensions
{
  public static bool IsDesktop(this IApplicationLifetime? lt, out IClassicDesktopStyleApplicationLifetime? desktop)
  {
    desktop = lt as IClassicDesktopStyleApplicationLifetime;
    return desktop is not null;
  }

  public static bool IsMobile(this IApplicationLifetime? lt, out ISingleViewApplicationLifetime? mobile)
  {
    mobile = lt as ISingleViewApplicationLifetime;
    return mobile is not null;
  }
}
