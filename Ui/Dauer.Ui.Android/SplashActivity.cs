using Android.App;
using Android.Content;
using Android.OS;
using Application = Android.App.Application;
using Avalonia;
using Avalonia.Android;
using Avalonia.ReactiveUI;
using Dauer.Fuse.Secure;
using Dauer.Ui;

namespace Dauer.Ui.Android;

[Activity(Theme = "@style/MyTheme.Splash", MainLauncher = true, NoHistory = true)]
public class SplashActivity : AvaloniaSplashActivity<App>
{
    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
      try
      {
        AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
        {
          return args.Name.StartsWith("Dauer")
            ? Defuse.Redirect(args.Name, "/Dauer.Fuse.dll")
            : null;
        };
      }
      catch (Exception e)
      {
        Console.WriteLine(e);
      }

        return base.CustomizeAppBuilder(builder)
            .UseReactiveUI();
    }

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
    }

    protected override void OnResume()
    {
        base.OnResume();

        StartActivity(new Intent(Application.Context, typeof(MainActivity)));
    }
}
