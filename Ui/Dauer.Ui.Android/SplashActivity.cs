using Android.App;
using Android.Content;
using Android.OS;
using Application = Android.App.Application;
using Avalonia;
using Avalonia.Android;
using Avalonia.ReactiveUI;

namespace Dauer.Ui.Android;

[Activity(Theme = "@style/MyTheme.Splash", MainLauncher = true, NoHistory = true)]
public class SplashActivity : AvaloniaSplashActivity<App>
{
  protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
  {
    CompositionRoot.ServiceLocator.Register<IWebAuthenticator, AndroidWebAuthenticator>(new AndroidWebAuthenticator());

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

  protected override void OnNewIntent(Intent? intent)
  {
    base.OnNewIntent(intent);
  }

  protected override void OnDestroy()
  {
    base.OnDestroy();
  }
}
